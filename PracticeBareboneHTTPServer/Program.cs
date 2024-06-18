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

            // Buffer for reading data
            byte[] bytes = new byte[2048];

            while (true)
            {
                Console.Write("Waiting for a connection... ");

                using TcpClient client = server.AcceptTcpClient();
                Console.WriteLine("Connected!");

                // Loop to receive all the data sent by the client.
                StringBuilder sb = new();
                int i;
                NetworkStream stream = client.GetStream();
                while (stream.DataAvailable && (i = stream.Read(bytes, 0, bytes.Length)) != 0)
                {
                    // Translate data bytes to a ASCII string.
                    string data = Encoding.UTF8.GetString(bytes, 0, i);
                    Console.WriteLine($"Received: {data}");
                    sb.Append(data);
                }

                string requestHeader = sb.ToString().TrimEnd();
                Console.WriteLine($"Complete message: {requestHeader}");

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
                      <p>Hello World!</>
                      <script src="scripts.js"></script>
                    </body>

                    </html>
                    """;
                string replyHeader = $"""
                    HTTP/1.1 200 OK
                    Content-Length: {body.Length}
                    Content-Type: text/html; charset=utf-8
                    """;
                string reply = $"""
                    {replyHeader}
                    
                    {body}
                    """;
                byte[] replyData = Encoding.UTF8.GetBytes(reply);
                stream.Write(replyData);
            }
        }
    }
}
