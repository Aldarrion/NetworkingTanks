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
        public RemotePlayer(TanksGame game, int playerId) 
            : base(game, playerId)
        {
        }

        public override void Update(GameTime time)
        {
        }
    }
}
