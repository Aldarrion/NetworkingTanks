using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace Client.Entities
{
    public class RemotePlayer : Player
    {
        private Vector2 _previousPosition;
        private Vector2 _nextPosition;
        public Vector2 NextPosition
        {
            get => _nextPosition;
            set
            {
                MoveTo(_nextPosition);
                _previousPosition = _nextPosition;
                _nextPosition = value;
            }
        }
        public float InterpTime { get; set; }

        public RemotePlayer(TanksGame game, int playerId) 
            : base(game, playerId)
        {
        }

        public override void Update(GameTime time)
        {
            HandleMovement(time);
        }

        private void HandleMovement(GameTime time)
        {
            if (InterpTime < 0.001f)
            {
                MoveTo(_nextPosition);
            }

            float distanceToNextPos = (_nextPosition - _position).LengthSquared();
            if (distanceToNextPos > 0.001f)
            {
                Vector2 distance = _nextPosition - _previousPosition;
                Vector2 distancePerSecond = distance / InterpTime;

                Move(distancePerSecond * (float)time.ElapsedGameTime.TotalSeconds);
            }
            else
            {
                MoveTo(_nextPosition);
            }
        }

        public override void MoveTo(Vector2 newPosition)
        {
            base.MoveTo(newPosition);
            _previousPosition = newPosition;
            _nextPosition = newPosition;
        }
    }
}
