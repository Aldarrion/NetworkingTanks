using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Client.Entities
{
    public class TickInfo
    {
        public int TickNumber { get; set; }
        public Vector2 NextPosition { get; set; }
        public float InterpDuration { get; set; }
    }

    public class RemotePlayer : Player
    {
        public LinkedList<TickInfo> Ticks { get; } = new LinkedList<TickInfo>();

        private Vector2 _previousPosition;
        private Vector2 _nextPosition;
        private Vector2 NextPosition
        {
            get => _nextPosition;
            set
            {
                MoveTo(_nextPosition);
                _previousPosition = _nextPosition;
                _nextPosition = value;
            }
        }

        private float _interpDurationRemaining;
        private float _currentInterpDuration;
        private float CurrentInterpDuration
        {
            get => _currentInterpDuration;
            set
            {
                _currentInterpDuration = value;
                _interpDurationRemaining = value;
            }
        }

        public RemotePlayer(TanksGame game, int playerId) 
            : base(game, playerId)
        {
        }
        
        public override void MoveTo(Vector2 newPosition)
        {
            base.MoveTo(newPosition);
            _previousPosition = newPosition;
            _nextPosition = newPosition;
        }

        public override void Update(GameTime time)
        {
            // Lerp is finished
            if (_interpDurationRemaining <= 0f)
            {
                lock (Ticks)
                {
                    // Last interp position
                    TickInfo lastTick = Ticks.First.Value;
                    Ticks.RemoveFirst();

                    // No snapshot received yet
                    if (Ticks.First == null)
                    {
                        // Insert dummy
                        Ticks.AddLast(new TickInfo
                        {
                            NextPosition = NextPosition,
                            InterpDuration = lastTick.InterpDuration,
                            TickNumber = lastTick.TickNumber + 1
                        });
                    }

                    // Set next position and time to interpolate
                    TickInfo next = Ticks.First.Value;
                    NextPosition = next.NextPosition;
                    CurrentInterpDuration = next.InterpDuration * (next.TickNumber - lastTick.TickNumber);
                }
            }

            HandleMovement(time);
        }

        private void HandleMovement(GameTime time)
        {
            _interpDurationRemaining -= (float) time.ElapsedGameTime.TotalSeconds;
            if (_position == _nextPosition)
            {
                // Nowhere to lerp
                return;
            }

            float distanceToNextPos = (_nextPosition - _position).LengthSquared();
            if (distanceToNextPos > 0.001f) 
            {
                // TODO solve overshooting - if framerate is bad, we can go over the goal and the distance will increase
                Vector2 distance = _nextPosition - _previousPosition;
                Vector2 distancePerSecond = distance / CurrentInterpDuration;

                Move(distancePerSecond * (float)time.ElapsedGameTime.TotalSeconds);
            }
            else // Lerp is not finished but we are pretty much there
            {
                MoveTo(_nextPosition);
            }
        }

        
    }
}
