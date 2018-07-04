//#define USE_RECEIVE_CALLBACK

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
        private readonly Dictionary<NetConnection, int> _clients = new Dictionary<NetConnection, int>();
        private readonly Dictionary<int, PlayerInfo> _players = new Dictionary<int, PlayerInfo>();

        private int _playerId;

        private static readonly float TICK_RATE_PER_SECOND = 1f;
        private static readonly float TICK_DURATION_SECONDS = 1f / TICK_RATE_PER_SECOND;
        private static readonly TimeSpan TICK_DURATION = TimeSpan.FromSeconds(TICK_DURATION_SECONDS);

        private DateTime _lastTickTime;
        private int _tickNumber;

        private Queue<MoveMessage> _commands = new Queue<MoveMessage>();

        private readonly Random _rnd = new Random(42);

        public void Run()
        {
            InitServer();

#if USE_RECEIVE_CALLBACK
            // Not necessary when callback is not used
            while (true)
            {
                Thread.Sleep(1000);
            }
#else
            ProcessMessages();
#endif
        }

        private void InitServer()
        {
            SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
            var config = new NetPeerConfiguration(CONNECTION_NAME)
            {
                Port = PORT
            };
            config.EnableMessageType(NetIncomingMessageType.ConnectionApproval);

            _server = new NetServer(config);
            _server.Start();

            if (_server.Status != NetPeerStatus.Running)
            {
                throw new Exception("Server not started...");
            }

            Console.WriteLine("Server is running on port " + config.Port);

            Task.Factory.StartNew(SendInfoToPlayers, TaskCreationOptions.LongRunning);

#if USE_RECEIVE_CALLBACK
// Callback is also supported, this causes the game to lag more
            _server.RegisterReceivedCallback(ReceiveMessage);
#endif
        }

        private void SendInfoToPlayers()
        {
            while (true)
            {
                _lastTickTime = DateTime.Now;

                if (_clients.Count > 1)
                {
                    // Execute tick
                    lock (_clients)
                    {
                        foreach (KeyValuePair<NetConnection, int> client in _clients)
                        {
                            List<PlayerInfo> otherPlayerInfos = _players
                                .Where(x => x.Key != client.Value)
                                .Select(x => x.Value)
                                .ToList();

                            var snapshotMessage = new SnapshotMessage
                            {
                                TickNumber = _tickNumber,
                                OtherPlayers = {otherPlayerInfos}
                            };
                            var wrapperMsg = new WrapperMessage {SnapshotMessage = snapshotMessage};
                            NetOutgoingMessage outMsg = wrapperMsg.ToNetOutMsg(_server);
                            _server.SendMessage(
                                outMsg,
                                client.Key,
                                NetDeliveryMethod.UnreliableSequenced,
                                DEFAULT_SEQUENCE_CHANNEL
                            );
                        }

                        ++_tickNumber;
                    }
                }

                TimeSpan elapsedTickDuration = DateTime.Now - _lastTickTime;
                var sleepTime = (int) (TICK_DURATION - elapsedTickDuration).TotalMilliseconds;
                if (sleepTime > 0)
                    Thread.Sleep(sleepTime);
                DateTime nextTickTime = _lastTickTime + TICK_DURATION;
                while (DateTime.Now < nextTickTime)
                {
                    // Active wait until next tick
                    continue;
                }
            }
        }

        private void ProcessMessages()
        {
            while (true)
            {
                NetIncomingMessage msg;
                if ((msg = _server.ReadMessage()) == null)
                {
                    continue;
                }

                HandleMessage(msg);

                _server.Recycle(msg);
            }
        }

        private void ReceiveMessage(object peer)
        {
            var server = (NetServer) peer;
            NetIncomingMessage msg = server.ReadMessage();
            HandleMessage(msg);
        }

        private void HandleMessage(NetIncomingMessage msg)
        {
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
                    var newPlayerPosition = new Position { X = _rnd.Next(500), Y = _rnd.Next(500) };
                    var newPlayerInfo = new PlayerInfo { Id = newPlayerId, Position = newPlayerPosition };
                    // Send player info
                    var playerSpawnMsg = new PlayerSpawnMessage
                    {
                        NewPlayer = newPlayerInfo,
                        OtherPlayers = { _players.Values }
                    };
                    var wrapper = new WrapperMessage { PlayerSpawnMessage = playerSpawnMsg };
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
                                PlayerInfo = new PlayerInfo { Id = newPlayerId, Position = newPlayerPosition }
                            };
                            var clientWrapper = new WrapperMessage { NewPlayerMessage = newPlayerMsg };
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
