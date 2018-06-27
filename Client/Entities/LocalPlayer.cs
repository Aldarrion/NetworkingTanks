using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Protobufs.NetworkTanks.Game;
using System.IO;
using System.Threading;
using Lidgren.Network;
using NetworkUtils;

namespace Client.Entities
{
    public class LocalPlayer : Player
    {
        private static readonly string SERVER_HOST = "127.0.0.1";
        private static readonly int SERVER_PORT = 14241;

        private static readonly string CLIENT_HOST = "127.0.0.1";
        private static readonly int CLIENT_PORT = 9988;

        private static readonly string CONNECTION_NAME = "TanksNetworking";

        private int _playerId;
        private NetClient _client;

        public LocalPlayer(TanksGame game)
            : base(game)
        {
            var config = new NetPeerConfiguration(CONNECTION_NAME);
            config.EnableMessageType(NetIncomingMessageType.ConnectionApproval);
            _client = new NetClient(config);
            _client.Start();

            //_client.DiscoverLocalPeers(SERVER_PORT);

            //NetOutgoingMessage outmsg = _client.CreateMessage();
            //outmsg.Write(1);

            //Console.WriteLine(IPAddress.Loopback.ToString());
            //var sep = new IPEndPoint(IPAddress.Loopback, SERVER_PORT);
            NetConnection connection = _client.Connect(SERVER_HOST, SERVER_PORT);
            Console.WriteLine("Client created");

            EsablishInfo();
            // Send ready message
            //var clientReadyMsg = new ClientReadyMessage
            //{
            //    Ip = CLIENT_HOST,
            //    PortNumber = CLIENT_PORT
            //};
            //var wrapper = new WrapperMessage { ClientReadyMessage = clientReadyMsg };

            //NetOutgoingMessage msg = _client.CreateMessage();
            //msg.Write(wrapper.CalculateSize());
            //msg.Write(Protobufs.Utils.GetBinaryData(wrapper));
            //_client.SendMessage(msg, NetDeliveryMethod.ReliableOrdered);




            //_tcpClient = new TcpClient();
            //_tcpClient.Connect(SERVER_HOST, SERVER_PORT);
            //NetworkStream stream = _tcpClient.GetStream();

            //// Send ready message
            //var crm = new ClientReadyMessage()
            //{
            //    Ip = CLIENT_HOST,
            //    PortNumber = CLIENT_PORT
            //};
            //var wrapper = new WrapperMessage { ClientReadyMessage = crm };
            //stream.Write(Protobufs.Utils.GetBinaryData(wrapper), 0, wrapper.CalculateSize());
            //stream.Flush();

            //// Receive server ready message
            //byte[] message = Utils.ReadMessage(stream, _tcpClient.ReceiveBufferSize);
            //WrapperMessage wm = WrapperMessage.Parser.ParseFrom(message);
            //if (wm.MessageCase != WrapperMessage.MessageOneofCase.ServerReadyMessage)
            //{
            //    throw new Exception("Invalid protocol!");
            //}

            //_playerId = wm.ServerReadyMessage.PlayerId;
            //Console.WriteLine($"Connected to server. Id assigned: {_playerId}");

            //_sendSocket = new UdpClient();
            //_sendSocket.Connect(SERVER_HOST, SERVER_PORT);

            //_receiveSocket = new UdpClient(9988);
            //_receiveTask = _receiveSocket.ReceiveAsync();
        }

        public void GotMessage(object peer)
        {
            var netClient = (NetClient)peer;
            NetIncomingMessage msg = netClient.ReadMessage();
            
            Console.WriteLine($"Message received: {msg.MessageType}");
            switch (msg.MessageType)
            {
                case NetIncomingMessageType.Error:
                    break;
                case NetIncomingMessageType.StatusChanged:
                    break;
                case NetIncomingMessageType.UnconnectedData:
                    break;
                case NetIncomingMessageType.ConnectionApproval:
                    break;
                case NetIncomingMessageType.Data:
                {
                    int size = msg.ReadInt32();
                    byte[] protoMsg = msg.ReadBytes(size);
                    var wm = WrapperMessage.Parser.ParseFrom(protoMsg);
                    if (wm.MessageCase == WrapperMessage.MessageOneofCase.MoveMessage)
                    {
                        MoveMessage move = wm.MoveMessage;
                        lock (this)
                        {
                            _position = new Vector2(move.X, move.Y);
                        }
                    }
                    break;
                }
                case NetIncomingMessageType.Receipt:
                    break;
                case NetIncomingMessageType.DiscoveryRequest:
                    break;
                case NetIncomingMessageType.DiscoveryResponse:
                    break;
                case NetIncomingMessageType.VerboseDebugMessage:
                case NetIncomingMessageType.DebugMessage:
                case NetIncomingMessageType.WarningMessage:
                case NetIncomingMessageType.ErrorMessage:
                    Console.WriteLine($"  {msg.ReadString()}");
                    break;
                case NetIncomingMessageType.NatIntroductionSuccess:
                    break;
                case NetIncomingMessageType.ConnectionLatencyUpdated:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private bool EsablishInfo()
        {
            while (true)
            {
                NetIncomingMessage msg;
                if ((msg = _client.ReadMessage()) == null)
                {
                    continue;
                }

                Console.WriteLine($"Message: {msg.MessageType}");
                switch (msg.MessageType)
                {
                    //case NetIncomingMessageType.ConnectionApproval:

                    case NetIncomingMessageType.StatusChanged:
                    {
                        Console.WriteLine($"  {msg.SenderConnection.Status}");
                        if (msg.SenderConnection.Status == NetConnectionStatus.Connected)
                        {
                            NetIncomingMessage hailMsg = msg.SenderConnection.RemoteHailMessage;
                            int size = hailMsg.ReadInt32();
                            byte[] protoMsg = hailMsg.ReadBytes(size);

                            WrapperMessage wrapper = WrapperMessage.Parser.ParseFrom(protoMsg);
                            if (wrapper.MessageCase != WrapperMessage.MessageOneofCase.ServerReadyMessage)
                            {
                                throw new Exception("Invalid server protocol");
                            }

                            _playerId = wrapper.ServerReadyMessage.PlayerId;
                            Console.WriteLine($"  Received id: {_playerId} from server");

                            SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
                            _client.RegisterReceivedCallback(GotMessage);

                            return true;
                        }
                        break;
                    }
                }

                _client.Recycle(msg);
            }
        }

        private void HandleInput(GameTime time)
        {
            var moveVec = Vector2.Zero;
            var kState = Keyboard.GetState();
            if (kState.IsKeyDown(Keys.W))
            {
                moveVec.Y += -Speed * (float)time.ElapsedGameTime.TotalSeconds;
            }
            else if (kState.IsKeyDown(Keys.S))
            {
                moveVec.Y += Speed * (float)time.ElapsedGameTime.TotalSeconds;
            }

            if (kState.IsKeyDown(Keys.A))
            {
                moveVec.X += -Speed * (float)time.ElapsedGameTime.TotalSeconds;
            }
            else if (kState.IsKeyDown(Keys.D))
            {
                moveVec.X += Speed * (float)time.ElapsedGameTime.TotalSeconds;
            }

            Move(moveVec);
        }

        public override void Update(GameTime time)
        {
            HandleInput(time);

            if (Game.IsKeyDownNew(Keys.E))
            {
                _position.X += Speed * (float)time.ElapsedGameTime.TotalSeconds;
                var moveMessage = new MoveMessage
                {
                    PlayerId = 1,
                    X = 123,
                    Y = 42
                };
                var wm = new WrapperMessage
                {
                    MoveMessage = moveMessage
                };
            }

            //if (_receiveTask.IsCompleted)
            {

                //var wm = WrapperMessage.Parser.ParseFrom(_receiveTask.Result.Buffer);
                //if (wm.MessageCase == WrapperMessage.MessageOneofCase.MoveMessage)
                //{
                //    _position.X = wm.MoveMessage.X;
                //    _position.Y = wm.MoveMessage.Y;
                //}

                //_receiveTask = _receiveSocket.ReceiveAsync();
            }
        }

        public void Destroy()
        {
            _client.Disconnect(string.Empty);
        }
    }
}
