using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using Protobufs.NetworkTanks.Game;

namespace Client.Entities
{
    internal class Player : Entity
    {
        private Texture2D _square;
        private float _speed;
        private Vector2 _position;
        private UdpClient _sendSocket;
        private UdpClient _receiveSocket;
        private Task<UdpReceiveResult> _receiveTask;

        private static readonly string HOST = "127.0.0.1";
        private readonly int PORT = 9988;

        public Player(TanksGame game)
            : base(game)
        {
            _speed = 128.0f;
            int x = Game.GraphicsManager.PreferredBackBufferWidth / 2;
            int y = Game.GraphicsManager.PreferredBackBufferHeight / 2;
            _position = new Vector2(x, y);
            _sendSocket = new UdpClient();
            _sendSocket.Connect("localhost", 9989);

            _receiveSocket = new UdpClient(new IPEndPoint(IPAddress.Parse(HOST), 9988));
            _receiveTask = _receiveSocket.ReceiveAsync();
        }

        public void LoadContent(GraphicsDevice device)
        {
            int dimensions = 32;
            _square = new Texture2D(device, dimensions, dimensions);
            var data = new Color[dimensions * dimensions];
            for (int i = 0; i < data.Length; ++i)
                data[i] = Color.Black;
            _square.SetData(data);
        }

        public override void Update(GameTime time)
        {
            var kState = Keyboard.GetState();
            if(kState.IsKeyDown(Keys.W))
            {
                _position.Y += -_speed * (float)time.ElapsedGameTime.TotalSeconds;
            }
            else if (kState.IsKeyDown(Keys.S))
            {
                _position.Y += _speed * (float)time.ElapsedGameTime.TotalSeconds;
            }

            if (kState.IsKeyDown(Keys.A))
            {
                _position.X += -_speed * (float)time.ElapsedGameTime.TotalSeconds;
            }
            else if (kState.IsKeyDown(Keys.D))
            {
                _position.X += _speed * (float)time.ElapsedGameTime.TotalSeconds;
            }
            
            if (Game.IsKeyDownNew(Keys.E))
            {
                _position.X += _speed * (float)time.ElapsedGameTime.TotalSeconds;
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

        public override void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(_square, _position, Color.White);
        }
    }
}
