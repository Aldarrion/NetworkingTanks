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
using NetworkingTanks.Utils;

namespace Server
{
    internal class Server
    {
        //private static readonly string HOST = "127.0.0.1";
        private static readonly int PORT = 14241;
        private static readonly string CONNECTION_NAME = "TanksNetworking";
        private static readonly int DEFAULT_SEQUENCE_CHANNEL = 0;

        private NetServer _server;
        private Dictionary<NetConnection, int> _clients = new Dictionary<NetConnection, int>();
        private Dictionary<int, PlayerInfo> _players = new Dictionary<int, PlayerInfo>();

        private int _playerId;

        private static readonly float TICK_RATE = 30f;
        private static readonly float TICK_TIME = 1f / TICK_RATE;

        private Queue<MoveMessage> _commands = new Queue<MoveMessage>();

        private readonly Random _rnd = new Random(42);

        public void Run()
        {
            InitServer();
            Task.Run((Action)ProcessMessages);

            while (true)
            {
                continue;
                Thread.Sleep(2000);
                foreach (NetConnection client in _clients.Keys)
                {
                    Console.WriteLine("Sending move");
                    var moveMessage = new MoveMessage
                    {
                        PlayerId = 1,
                        Position = new Position { X = 10, Y = 10 }
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

        private void InitServer()
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
                throw new Exception("Server not started...");
            }
        }

        private void ProcessMessages()
        {
            try
            {
                Console.WriteLine("Listening for connections");
                while (true)
                {
                    NetIncomingMessage msg;
                    if ((msg = _server.ReadMessage()) == null)
                    {
                        continue;
                    }

                    Console.WriteLine($"{msg.MessageType}");
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
                                PlayerDisconencted(msg);
                            }
                            break;
                        }
                        case NetIncomingMessageType.UnconnectedData:
                            break;
                        case NetIncomingMessageType.ConnectionApproval:
                        {
                            int newPlayerId = Interlocked.Increment(ref _playerId);
                            var newPlayerPosition = new Position {X = _rnd.Next(500), Y = _rnd.Next(500)};
                            var newPlayerInfo = new PlayerInfo {Id = newPlayerId, Position = newPlayerPosition};
                            // Send player info
                            var playerSpawnMsg = new PlayerSpawnMessage
                            {
                                NewPlayer = newPlayerInfo,
                                OtherPlayers = { _players.Values }
                            };
                            var wrapper = new WrapperMessage {PlayerSpawnMessage = playerSpawnMsg};
                            NetOutgoingMessage playerInfoMsg = _server.CreateMessage();
                            playerInfoMsg.Write(wrapper.CalculateSize());
                            playerInfoMsg.Write(Protobufs.Utils.GetBinaryData(wrapper));
                            msg.SenderConnection.Approve(playerInfoMsg);

                            Console.WriteLine($"  Added player with ID: {newPlayerId}");

                            // Send info about new player to other players
                            lock (_clients)
                            {
                                if (_clients.Count > 0)
                                {
                                    var newPlayerMsg = new NewPlayerMessage
                                    {
                                        PlayerInfo = new PlayerInfo {Id = newPlayerId, Position = newPlayerPosition}
                                    };
                                    var clientWrapper = new WrapperMessage {NewPlayerMessage = newPlayerMsg};
                                    NetOutgoingMessage clientMsg = _server.CreateMessage();
                                    clientMsg.Write(clientWrapper.CalculateSize());
                                    clientMsg.Write(Protobufs.Utils.GetBinaryData(clientWrapper));

                                    _server.SendMessage(
                                        clientMsg,
                                        _clients.Keys.ToList(),
                                        NetDeliveryMethod.ReliableOrdered,
                                        DEFAULT_SEQUENCE_CHANNEL
                                    );
                                }
                            }

                            // Add new player to database
                            lock (_clients)
                            {
                                _clients.Add(msg.SenderConnection, newPlayerId);
                                _players.Add(newPlayerId, newPlayerInfo);
                            }

                            break;
                        }
                        case NetIncomingMessageType.Data:
                            HandleDataMessage(msg);
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

        private void PlayerDisconencted(NetIncomingMessage msg)
        {
            lock (_clients) // TODO solve lock on players too
            {
                if (_clients.TryGetValue(msg.SenderConnection, out int id))
                {
                    _clients.Remove(msg.SenderConnection);
                    _players.Remove(id);

                    if (_clients.Count > 0)
                    {
                        var disconnectMsg = new PlayerDisconnectMessage {Id = id};
                        var wrapper = new WrapperMessage {PlayerDisconnectMessage = disconnectMsg};
                        NetOutgoingMessage outMessage = _server.CreateMessage();
                        outMessage.Write(wrapper.CalculateSize());
                        outMessage.Write(Protobufs.Utils.GetBinaryData(wrapper));
                        _server.SendMessage(
                            outMessage, 
                            _clients.Keys.ToList(), 
                            NetDeliveryMethod.ReliableOrdered,
                            DEFAULT_SEQUENCE_CHANNEL
                        );
                    }
                }
            }
        }

        private void HandleDataMessage(NetIncomingMessage msg)
        {
            Console.WriteLine($"  Received data message");
            int size = msg.ReadInt32();
            byte[] protoMsg = msg.ReadBytes(size);
            WrapperMessage wrapperMsg = WrapperMessage.Parser.ParseFrom(protoMsg);

            switch (wrapperMsg.MessageCase)
            {
                case WrapperMessage.MessageOneofCase.MoveMessage:
                {
                    MoveMessage moveMsg = wrapperMsg.MoveMessage;
                    lock (_clients)
                    {
                        if (_players.TryGetValue(moveMsg.PlayerId, out PlayerInfo playerInfo))
                        {
                            playerInfo.Position = moveMsg.Position;

                            List<NetConnection> otherPlayers = _clients
                                .Where(x => x.Value != moveMsg.PlayerId)
                                .Select(x => x.Key)
                                .ToList();

                            if (otherPlayers.Count > 0)
                            {
                                NetOutgoingMessage outMsg = wrapperMsg.ToNetOutMsg(_server);
                                _server.SendMessage(
                                    outMsg,
                                    otherPlayers,
                                    NetDeliveryMethod.UnreliableSequenced,
                                    DEFAULT_SEQUENCE_CHANNEL
                                );
                            }
                        }
                    }
                    break;
                }
                case WrapperMessage.MessageOneofCase.None:
                case WrapperMessage.MessageOneofCase.PlayerSpawnMessage:
                case WrapperMessage.MessageOneofCase.NewPlayerMessage:
                case WrapperMessage.MessageOneofCase.PlayerDisconnectMessage:
                default:
                    throw new ArgumentOutOfRangeException(nameof(wrapperMsg.MessageCase));
            }
        }

        //private void OldRun()
        //{
        //    var socketReceive = new UdpClient(new IPEndPoint(IPAddress.Parse(HOST), PORT));

        //    Task.Run(() =>
        //    {
        //        var socketSend = new UdpClient();
        //        socketSend.Connect(new IPEndPoint(IPAddress.Parse(HOST), 9988));
        //        while (true)
        //        {
        //            try
        //            {
        //                Thread.Sleep(3000);
        //                Console.WriteLine("Sending move");
        //                var moveMessage = new MoveMessage
        //                {
        //                    PlayerId = 1,
        //                    X = 10,
        //                    Y = 10
        //                };
        //                var wm = new WrapperMessage
        //                {
        //                    MoveMessage = moveMessage
        //                };
        //                socketSend.Send(Protobufs.Utils.GetBinaryData(wm), wm.CalculateSize());
        //                Console.WriteLine("Move sent");
        //            }
        //            catch (Exception e)
        //            {
        //                Console.WriteLine(e.Message);
        //                Console.WriteLine(e.StackTrace);
        //            }
        //        }
        //    });

        //    Console.WriteLine("Server running");

        //    while (true)
        //    {
        //        var msg = socketReceive.ReceiveAsync();

        //        WrapperMessage wm = WrapperMessage.Parser.ParseFrom(msg.Result.Buffer);
        //        if (wm.MessageCase == WrapperMessage.MessageOneofCase.MoveMessage)
        //        {
        //            MoveMessage moveMsg = wm.MoveMessage;
        //            Console.WriteLine($"Move recieved. Player: {moveMsg.PlayerId}, X: {moveMsg.X}, Y: {moveMsg.Y}");
        //        }
        //    }
        //}

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
