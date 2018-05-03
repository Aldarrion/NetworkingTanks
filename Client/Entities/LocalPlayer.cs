using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Protobufs.NetworkTanks.Game;

namespace Client.Entities
{
    public class LocalPlayer : Player
    {
        protected UdpClient _sendSocket;
        protected UdpClient _receiveSocket;
        protected Task<UdpReceiveResult> _receiveTask;

        private static readonly string SERVER_HOST = "127.0.0.1";
        private static readonly int SERVER_PORT = 9900;

        public LocalPlayer(TanksGame game)
            : base(game)
        {
            _sendSocket = new UdpClient();
            _sendSocket.Connect(SERVER_HOST, SERVER_PORT);

            _receiveSocket = new UdpClient(9988);
            _receiveTask = _receiveSocket.ReceiveAsync();
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

                _sendSocket.SendAsync(Protobufs.Utils.GetBinaryData(wm), wm.CalculateSize());
            }

            if (_receiveTask.IsCompleted)
            {
                
                var wm = WrapperMessage.Parser.ParseFrom(_receiveTask.Result.Buffer);
                if (wm.MessageCase == WrapperMessage.MessageOneofCase.MoveMessage)
                {
                    _position.X = wm.MoveMessage.X;
                    _position.Y = wm.MoveMessage.Y;
                }

                _receiveTask = _receiveSocket.ReceiveAsync();
            }
        }
    }
}
