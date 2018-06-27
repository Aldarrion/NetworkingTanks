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
using ProtoBuf;
using Lidgren.Network;
using NetworkUtils;

namespace Server
{
    internal class Server
    {
        private static readonly string HOST = "127.0.0.1";
        private static readonly int PORT = 14241;
        private static readonly string CONNECTION_NAME = "TanksNetworking";

        private static readonly IPEndPoint LOCAL_ENDPOINT = new IPEndPoint(IPAddress.Parse(HOST), PORT);

        //private List<IPEndPoint> _clients = new List<IPEndPoint>();
        //private Dictionary<IPEndPoint, UdpClient> _senders = new Dictionary<IPEndPoint, UdpClient>();
        //private Dictionary<IPEndPoint, UdpClient> _receivers = new Dictionary<IPEndPoint, UdpClient>();
        //private UdpClient _receiver = new UdpClient(LOCAL_ENDPOINT);

        private NetServer _server;
        private List<NetConnection> _clients = new List<NetConnection>();

        private int _playerId;

        private static readonly float TICK_RATE = 30f;
        private static readonly float TICK_TIME = 1f / TICK_RATE;

        private Queue<MoveMessage> _commands = new Queue<MoveMessage>();

        public void Run()
        {
            Task.Run((Action)ListenForConnections);

            while (true)
            {
                Thread.Sleep(2000);
                foreach (NetConnection client in _clients)
                {
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
                    NetOutgoingMessage msg = _server.CreateMessage();
                    msg.Write(wm.CalculateSize());
                    msg.Write(Protobufs.Utils.GetBinaryData(wm));

                    _server.SendMessage(msg, client, NetDeliveryMethod.Unreliable);
                    Console.WriteLine("Move sent");
                }
            }
        }

        private void ReceiveCommands()
        {
            //_receiver.ReceiveAsync()
            
        }

        private void ListenForConnections()
        {
            try
            {
                var config = new NetPeerConfiguration(CONNECTION_NAME)
                {
                    Port = PORT
                };
                config.EnableMessageType(NetIncomingMessageType.ConnectionApproval);

                _server = new NetServer(config);
                _server.Start();

                if (_server.Status == NetPeerStatus.Running)
                {
                    Console.WriteLine("Server is running on port " + config.Port);
                }
                else
                {
                    Console.WriteLine("Server not started...");
                }

                Console.WriteLine("Listening for connections");
                while (true)
                {
                    NetIncomingMessage msg;
                    if ((msg = _server.ReadMessage()) == null)
                    {
                        continue;
                    }

                    Console.WriteLine($"Message received: {msg.MessageType}");
                    switch (msg.MessageType)
                    {
                        case NetIncomingMessageType.VerboseDebugMessage:
                        case NetIncomingMessageType.DebugMessage:
                        case NetIncomingMessageType.WarningMessage:
                        case NetIncomingMessageType.ErrorMessage:
                            Console.WriteLine($"  {msg.ReadString()}");
                            break;
                        case NetIncomingMessageType.Error:
                            break;
                        case NetIncomingMessageType.StatusChanged:
                        {
                            Console.WriteLine($"  {msg.SenderConnection.Status}");
                            if (msg.SenderConnection.Status == NetConnectionStatus.Disconnected)
                            {
                                _clients.Remove(msg.SenderConnection);
                            }
                            break;
                        }
                        case NetIncomingMessageType.UnconnectedData:
                            break;
                        case NetIncomingMessageType.ConnectionApproval:
                        {
                            // Send player info
                            var srm = new ServerReadyMessage
                            {
                                PlayerId = Interlocked.Increment(ref _playerId)
                            };
                            var wrapper = new WrapperMessage {ServerReadyMessage = srm};
                            NetOutgoingMessage playerInfoMsg = _server.CreateMessage();
                            playerInfoMsg.Write(wrapper.CalculateSize());
                            playerInfoMsg.Write(Protobufs.Utils.GetBinaryData(wrapper));
                            Console.WriteLine($"  Added player with ID: {srm.PlayerId}");

                            msg.SenderConnection.Approve(playerInfoMsg);
                            lock (_clients)
                            {
                                _clients.Add(msg.SenderConnection);
                            }

                            break;
                        }
                        case NetIncomingMessageType.Data:
                            Console.WriteLine($"  Received data message");
                            int size = msg.ReadInt32();
                            byte[] protoMsg = msg.ReadBytes(size);

                            break;
                        case NetIncomingMessageType.Receipt:
                            break;
                        case NetIncomingMessageType.DiscoveryRequest:
                            break;
                        case NetIncomingMessageType.DiscoveryResponse:
                            break;
                        case NetIncomingMessageType.NatIntroductionSuccess:
                            break;
                        case NetIncomingMessageType.ConnectionLatencyUpdated:
                            break;
                        default:
                            Console.WriteLine("  Unhandled type: " + msg.MessageType);
                            break;
                    }

                    _server.Recycle(msg);
                }

                Console.WriteLine("Server quitting");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
            }

            //var listener = new TcpListener(IPAddress.Parse(HOST), PORT);
            //listener.Start();
            //Console.WriteLine("Listening for connections...");

            //while (true)
            //{
            //    TcpClient client = listener.AcceptTcpClient();
            //    Console.WriteLine("New client connected!");
            //    Task.Run(() => HandleClient(client));
            //}
        }

        private void OldRun()
        {
            var socketReceive = new UdpClient(new IPEndPoint(IPAddress.Parse(HOST), PORT));

            Task.Run(() =>
            {
                var socketSend = new UdpClient();
                socketSend.Connect(new IPEndPoint(IPAddress.Parse(HOST), 9988));
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
                        socketSend.Send(Protobufs.Utils.GetBinaryData(wm), wm.CalculateSize());
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
                var msg = socketReceive.ReceiveAsync();

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
