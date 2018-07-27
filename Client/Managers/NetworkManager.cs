using System;
using System.Threading;
using Client.Extensions;
using Lidgren.Network;
using Microsoft.Xna.Framework;
using NetworkingTanks.Utils;
using Protobufs.NetworkTanks.Game;

namespace Client.Managers
{
    internal class NetworkManager
    {
        public float TickDurationSeconds { get; private set; }

        public int LastTickNumber { get; private set; }
        public int PreviousTickNumber { get; private set; }

        private static readonly string SERVER_HOST = "127.0.0.1";
        private static readonly int SERVER_PORT = 14241;
        private static readonly string CONNECTION_NAME = "TanksNetworking";

        private readonly NetClient _client;

        public int LocalPlayerId { get; private set; }
        public Vector2 LocalPlayerPosition { get; private set; }

        public event Action<PlayerInfo> OnNewPlayerConnected;
        public event Action<int> OnPlayerDisconnected;
        public event Action<SnapshotMessage> OnServerTick;

        public NetworkManager()
        {
            SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
            var config = new NetPeerConfiguration(CONNECTION_NAME);
            config.EnableMessageType(NetIncomingMessageType.ConnectionApproval);
            _client = new NetClient(config);
            _client.Start();
        }

        public void Connect()
        {
            _client.Connect(SERVER_HOST, SERVER_PORT);
            Console.WriteLine("Client created");

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
                    case NetIncomingMessageType.StatusChanged:
                    {
                        Console.WriteLine($"  {msg.SenderConnection.Status}");
                        if (msg.SenderConnection.Status == NetConnectionStatus.Connected)
                        {
                            NetIncomingMessage hailMsg = msg.SenderConnection.RemoteHailMessage;
                            int size = hailMsg.ReadInt32();
                            byte[] protoMsg = hailMsg.ReadBytes(size);

                            WrapperMessage wrapper = WrapperMessage.Parser.ParseFrom(protoMsg);
                            if (wrapper.MessageCase != WrapperMessage.MessageOneofCase.PlayerSpawnMessage)
                            {
                                throw new Exception("Invalid server protocol");
                            }

                            PlayerSpawnMessage playerSpawnMsg = wrapper.PlayerSpawnMessage;
                            LastTickNumber = playerSpawnMsg.LastTickNumber;

                            PlayerInfo newPlayer = playerSpawnMsg.NewPlayer;
                            TickDurationSeconds = playerSpawnMsg.ServerSettings.TickDurationSeconds;

                            LocalPlayerId = newPlayer.Id;
                            LocalPlayerPosition = newPlayer.Position.ToVector();
                            Console.WriteLine($"  Received id: {LocalPlayerId} and position: {LocalPlayerPosition} from server");

                            _client.RegisterReceivedCallback(ReceiveMessage);

                            Console.WriteLine($"--- Last Tick: {LastTickNumber}");

                            foreach (PlayerInfo otherPlayer in playerSpawnMsg.OtherPlayers)
                            {
                                OnNewPlayerConnected?.Invoke(otherPlayer);
                            }

                            return;
                        }
                        break;
                    }
                }

                _client.Recycle(msg);
            }
        }

        public void ReceiveMessage(object peer)
        {
            var netClient = (NetClient) peer;
            NetIncomingMessage msg = netClient.ReadMessage();

            //Console.WriteLine($"Message received: {msg.MessageType}");
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
                    HanldeDataMessage(msg);
                    break;
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

        private void HanldeDataMessage(NetIncomingMessage msg)
        {
            int size = msg.ReadInt32();
            byte[] protoMsg = msg.ReadBytes(size);
            var wrapperMessage = WrapperMessage.Parser.ParseFrom(protoMsg);

            switch (wrapperMessage.MessageCase)
            {
                case WrapperMessage.MessageOneofCase.None:
                    break;
                case WrapperMessage.MessageOneofCase.NewPlayerMessage:
                    OnNewPlayerConnected?.Invoke(wrapperMessage.NewPlayerMessage.PlayerInfo);
                    break;
                case WrapperMessage.MessageOneofCase.PlayerDisconnectMessage:
                    OnPlayerDisconnected?.Invoke(wrapperMessage.PlayerDisconnectMessage.Id);
                    break;
                case WrapperMessage.MessageOneofCase.SnapshotMessage:
                    var snapshotMessage = wrapperMessage.SnapshotMessage;
                    if (snapshotMessage.TickNumber <= LastTickNumber)
                    {
                        Console.WriteLine("ERROR: Tick out of order received!");
                    }
                    PreviousTickNumber = LastTickNumber;
                    LastTickNumber = snapshotMessage.TickNumber;
                    OnServerTick?.Invoke(snapshotMessage);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(wrapperMessage.MessageCase));
            }
        }

        public void SendPlayerMove(int id, Vector2 position)
        {
            var moveMsg = new MoveMessage
            {
                PlayerId = id,
                Position = position.ToPosition()
            };
            var wrapperMsg = new WrapperMessage {MoveMessage = moveMsg};

            NetOutgoingMessage outMsg = wrapperMsg.ToNetOutMsg(_client);
            _client.SendMessage(outMsg, NetDeliveryMethod.UnreliableSequenced);
        }

        public void Destroy()
        {
            _client.Disconnect(string.Empty);
        }
    }
}
