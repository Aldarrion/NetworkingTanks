using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Protobufs.NetworkTanks.Game;

namespace Client.Entities
{
    public class LocalPlayer : Player
    {
        private Vector2? _lastPosition;

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

            if (_lastPosition.HasValue && _position != _lastPosition.Value)
            {
                Game.NetworkManager.SendPlayerMove(PlayerId, _position);
            }

            _lastPosition = _position;
        }
    }
}
