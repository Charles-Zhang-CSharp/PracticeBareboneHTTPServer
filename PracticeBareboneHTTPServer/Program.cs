using System.Net.Sockets;
using System.Text;

namespace PracticeBareboneHTTPServer
{
    internal class Program
    {
        static void Main(string[] args)
        {
            int port = args.Length != 0 ? int.Parse(args.First()) : 9000;
            TcpListener server = new(System.Net.IPAddress.Any, port);
            server.Start();
            Console.WriteLine($"http://0.0.0.0:{port}");

            while (true)
            {
                Console.Write("Waiting for a connection... ");

                TcpClient client = server.AcceptTcpClient();
                Console.WriteLine("Connected!");

                // Serve each connection on its own task so a slow or idle client (e.g. a browser's speculative preconnect) can't stall the accept loop.
                _ = Task.Run(() => HandleClient(client));
            }
        }

        static void HandleClient(TcpClient client)
        {
            using (client)
            {
                client.ReceiveTimeout = 5000;
                NetworkStream stream = client.GetStream();

                // Buffer for reading data
                byte[] bytes = new byte[2048];

                // Loop until the whole request header (terminated by an empty line) has arrived.
                // Note: NetworkStream.DataAvailable only reports what's already buffered and is often still false right after accepting, so it can't be used to decide when the request is complete.
                StringBuilder sb = new();
                int i;
                try
                {
                    while (!sb.ToString().Contains("\r\n\r\n") && (i = stream.Read(bytes, 0, bytes.Length)) != 0)
                    {
                        // Translate data bytes to a string.
                        string data = Encoding.UTF8.GetString(bytes, 0, i);
                        Console.WriteLine($"Received: {data}");
                        sb.Append(data);
                    }
                }
                catch (IOException)
                {
                    Console.WriteLine("Client timed out or disconnected before sending a full request.");
                    return;
                }

                string requestHeader = sb.ToString().TrimEnd();
                Console.WriteLine($"Complete message: {requestHeader}");
                if (requestHeader.Length == 0)
                    return; // Client closed without sending anything

                // Send reply
                string body = $"""
                    <!DOCTYPE html>
                    <html lang="en">

                    <head>
                      <meta charset="utf-8">
                      <meta name="viewport" content="width=device-width, initial-scale=1">
                      <title>HTTP Server Welcome!</title>
                      <link rel="stylesheet" href="styles.css">
                    </head>

                    <body>
                      <h1>Barebone HTTP Server</h1>
                      <p>Hello World!</p>
                      <script src="scripts.js"></script>
                    </body>

                    </html>
                    """;
                byte[] bodyData = Encoding.UTF8.GetBytes(body);
                // HTTP requires CRLF line endings in the header, and Content-Length counts bytes (not chars).
                // We close the connection after each response, so tell the client not to reuse it.
                string replyHeader =
                    "HTTP/1.1 200 OK\r\n" +
                    $"Content-Length: {bodyData.Length}\r\n" +
                    "Content-Type: text/html; charset=utf-8\r\n" +
                    "Connection: close\r\n" +
                    "\r\n";
                stream.Write(Encoding.ASCII.GetBytes(replyHeader));
                stream.Write(bodyData);

                // Close gracefully: signal end of response, then drain whatever the client still sends.
                // Closing a socket with unread incoming data makes the OS send a TCP RST instead of a FIN, which browsers report as a connection reset and may discard the response.
                client.Client.Shutdown(SocketShutdown.Send);
                try
                {
                    while (stream.Read(bytes, 0, bytes.Length) != 0) { }
                }
                catch (IOException) { }
            }
        }
    }
}
