using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Protobufs.NetworkTanks.Game;

namespace Client.Entities
{
    public class LocalPlayer : Player
    {
        public LocalPlayer(TanksGame game, int playerId)
            : base(game, playerId)
        {
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
                    Position = new Position {X = 123, Y = 42 }
                };
                var wm = new WrapperMessage
                {
                    MoveMessage = moveMessage
                };
            }
        }
    }
}
