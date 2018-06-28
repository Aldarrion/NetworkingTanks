using Microsoft.Xna.Framework;
using Protobufs.NetworkTanks.Game;

namespace Client.Extensions
{
    internal static class PositionExtensions
    {
        public static Vector2 ToVector(this Position position)
        {
            return new Vector2(position.X, position.Y);
        }

        public static Position ToPosition(this Vector2 vector)
        {
            return new Position {X = vector.X, Y = vector.Y};
        }
    }
}
