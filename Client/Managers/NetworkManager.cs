using System;
using System.Threading;
using Client.Extensions;
using Lidgren.Network;
using Microsoft.Xna.Framework;
using Protobufs.NetworkTanks.Game;

namespace Client.Managers
{
    internal class NetworkManager
    {
        private static readonly string SERVER_HOST = "127.0.0.1";
        private static readonly int SERVER_PORT = 14241;
        private static readonly string CONNECTION_NAME = "TanksNetworking";

        private NetClient _client;

        public int LocalPlayerId { get; private set; }
        public Vector2 LocalPlayerPosition { get; private set; }

        public event Action<PlayerInfo> OnNewPlayerConnected;

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

            EsablishInfo();
        }

        private void EsablishInfo()
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
                            PlayerInfo newPlayer = playerSpawnMsg.NewPlayer;

                            LocalPlayerId = newPlayer.Id;
                            LocalPlayerPosition = newPlayer.Position.ToVector();
                            Console.WriteLine($"  Received id: {LocalPlayerId} and position: {LocalPlayerPosition} from server");
                            
                            _client.RegisterReceivedCallback(ReceiveMessage);

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
                                //_position = new Vector2(move.Position.X, move.Position.Y);
                            }
                        }
                        else if (wm.MessageCase == WrapperMessage.MessageOneofCase.NewPlayerMessage)
                        {
                            OnNewPlayerConnected?.Invoke(wm.NewPlayerMessage.PlayerInfo);
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

        public void Destroy()
        {
            _client.Disconnect(string.Empty);
        }
    }
}
