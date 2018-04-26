using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace TestClient
{
    //internal class Program
    //{
    //    private static void Main(string[] args)
    //    {
    //        var client = new Client();
    //        client.Run();
    //    }
    //}

    public struct Received
    {
        public IPEndPoint Sender;
        public string Message;
    }

    internal abstract class UdpBase
    {
        protected UdpClient Client;

        public async Task<Received> Receive()
        {
            UdpReceiveResult result = await Client.ReceiveAsync();
            return new Received
            {
                Message = Encoding.ASCII.GetString(result.Buffer, 0, result.Buffer.Length),
                Sender = result.RemoteEndPoint
            };
        }
    }

    //Server
    internal class UdpListener : UdpBase
    {
        private static readonly string HOST = "127.0.0.1";
        private static readonly int PORT = 9989;

        public UdpListener() 
            : this(new IPEndPoint(IPAddress.Parse(HOST), PORT))
        {
        }

        public UdpListener(IPEndPoint endpoint)
        {
            Client = new UdpClient(endpoint);
        }

        public void Reply(string message, IPEndPoint endpoint)
        {
            var datagram = Encoding.ASCII.GetBytes(message);
            Client.Send(datagram, datagram.Length, endpoint);
        }

    }

    //Client
    internal class UdpUser : UdpBase
    {
        private UdpUser() { }

        public static UdpUser ConnectTo(string hostname, int port)
        {
            var connection = new UdpUser();
            connection.Client.Connect(hostname, port);
            return connection;
        }

        public void Send(string message)
        {
            var datagram = Encoding.ASCII.GetBytes(message);
            Client.Send(datagram, datagram.Length);
        }

    }

    internal class Program
    {
        private static void Main(string[] args)
        {
            //create a new server
            var server = new UdpListener();

            //start listening for messages and copy the messages back to the client
            Task.Factory.StartNew(async () => {
                while (true)
                {
                    Received received = await server.Receive();
                    server.Reply("copy " + received.Message, received.Sender);
                    if (received.Message == "quit")
                        break;
                }
            });

            //create a new client
            UdpUser client = UdpUser.ConnectTo("127.0.0.1", 9989);

            //wait for reply messages from server and send them to console 
            Task.Factory.StartNew(async () => {
                while (true)
                {
                    try
                    {
                        Received received = await client.Receive();
                        Console.WriteLine(received.Message);
                        if (received.Message.Contains("quit"))
                            break;
                    }
                    catch (Exception ex)
                    {
                        Debug.Write(ex);
                    }
                }
            });

            //type ahead :-)
            string read;
            do
            {
                read = Console.ReadLine();
                client.Send(read);
            } while (read != "quit");
        }
    }
}
