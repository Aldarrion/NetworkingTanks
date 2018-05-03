using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Client.Entities
{
    public abstract class Player : Entity
    {
        protected readonly float _updateRate = 30;
        protected readonly float _tickRate = 30;

        private Texture2D _square;
        public float Speed { get; }
        protected Vector2 _position;

        protected Player(TanksGame game) 
            : base(game)
        {
            Speed = 128.0f;
            int x = Game.GraphicsManager.PreferredBackBufferWidth / 2;
            int y = Game.GraphicsManager.PreferredBackBufferHeight / 2;
            _position = new Vector2(x, y);
        }

        public virtual void LoadContent(GraphicsDevice device)
        {
            int dimensions = 32;
            _square = new Texture2D(device, dimensions, dimensions);
            var data = new Color[dimensions * dimensions];
            for (int i = 0; i < data.Length; ++i)
                data[i] = Color.Black;
            _square.SetData(data);
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(_square, _position, Color.White);
        }

        public void Move(Vector2 deltaPosition)
        {
            _position += deltaPosition;
        }

        public void MoveTo(Vector2 newPosition)
        {
            _position = newPosition;
        }
    }
}
