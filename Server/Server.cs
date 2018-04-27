using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Protobufs.NetworkTanks.Game;

namespace Server
{
    internal class Server
    {
        private static readonly string HOST = "127.0.0.1";
        private static readonly int PORT = 9989;

        public void Run()
        {
            var socketSend = new UdpClient(new IPEndPoint(IPAddress.Parse(HOST), PORT));

            Task.Run(() =>
            {
                var socketReceive = new UdpClient();
                socketReceive.Connect(new IPEndPoint(IPAddress.Parse(HOST), 9988));
                while (true)
                {
                    try
                    {
                        Thread.Sleep(3000);
                        Console.WriteLine("Sending move");
                        var moveMessage = new MoveMessage
                        {
                            PlayerId = 1,
                            X = 10,
                            Y = 10
                        };
                        var wm = new WrapperMessage
                        {
                            MoveMessage = moveMessage
                        };
                        socketReceive.Send(Protobufs.Utils.GetBinaryData(wm), wm.CalculateSize());
                        Console.WriteLine("Move sent");
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                        Console.WriteLine(e.StackTrace);
                    }
                }
            });

            Console.WriteLine("Server running");

            while (true)
            {
                var msg = socketSend.ReceiveAsync();

                WrapperMessage wm = WrapperMessage.Parser.ParseFrom(msg.Result.Buffer);
                if (wm.MessageCase == WrapperMessage.MessageOneofCase.MoveMessage)
                {
                    MoveMessage moveMsg = wm.MoveMessage;
                    Console.WriteLine($"Move recieved. Player: {moveMsg.PlayerId}, X: {moveMsg.X}, Y: {moveMsg.Y}");
                }
            }
        }

        //public void Run()
        //{
        //    var server = new TcpListener(IPAddress.Parse(HOST), PORT);
        //    try
        //    {
        //        server.Start();

        //        var bytes = new byte[256];

        //        while (true)
        //        {
        //            Console.WriteLine("Waiting for a connection... ");
        //            TcpClient client = server.AcceptTcpClient();
        //            //Socket socket = server.AcceptSocket();

        //            Console.WriteLine("Connected!");

        //            // Get a stream object for reading and writing
        //            NetworkStream stream = client.GetStream();

        //            using (var sr = new StreamReader(stream, Encoding.ASCII))
        //            {

        //                string data = sr.ReadToEnd();
        //                Console.WriteLine("Received: {0}", data);

        //                // Process the data sent by the client.
        //                data = data.ToUpper();

        //                byte[] msg = Encoding.ASCII.GetBytes(data);

        //                // Send back a response.
        //                stream.Write(msg, 0, msg.Length);
        //                Console.WriteLine("Sent: {0}", data);
        //            }

        //            // Shutdown and end connection
        //            client.Close();
        //        }
        //    }
        //    catch (SocketException e)
        //    {
        //        Console.WriteLine("SocketException: {0}", e);
        //    }
        //    finally
        //    {
        //        // Stop listening for new clients.
        //        server.Stop();
        //    }
        //}
    }
}
