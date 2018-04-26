using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Client.Entities
{
    internal abstract class Entity
    {
        protected TanksGame Game { get; }

        protected Entity(TanksGame game)
        {
            Game = game;
        }

        public abstract void Update(GameTime time);
        public abstract void Draw(SpriteBatch spriteBatch);
    }
}
