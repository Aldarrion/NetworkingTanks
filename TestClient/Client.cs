using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace TestClient
{
    internal class Client
    {
        private static readonly string HOST = "127.0.0.1";
        private static readonly int PORT = 9989;

        public void Run()
        {
            var client = new TcpClient(HOST, PORT);

            NetworkStream stream = client.GetStream();

            string message = "hello";
            byte[] data = Encoding.ASCII.GetBytes(message);

            stream.Write(data, 0, data.Length);

            Console.WriteLine("Sent: {0}", message);

            // Buffer to store the response bytes.
            data = new byte[256];

            // Read the first batch of the TcpServer response bytes.
            int bytes = stream.Read(data, 0, data.Length);
            string responseData = Encoding.ASCII.GetString(data, 0, bytes);
            Console.WriteLine("Received: {0}", responseData);

            // Close everything.
            stream.Close();
            client.Close();
        }
    }
}
