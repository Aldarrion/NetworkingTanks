using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Client.Entities
{
    internal class Player : Entity
    {
        private Texture2D _square;
        private float _speed;
        private Vector2 _position;

        public Player(TanksGame game)
            : base(game)
        {
            _speed = 128.0f;
            int x = Game.GraphicsManager.PreferredBackBufferWidth / 2;
            int y = Game.GraphicsManager.PreferredBackBufferHeight / 2;
            _position = new Vector2(x, y);
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
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(_square, _position, Color.White);
        }
    }
}
