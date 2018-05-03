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
        protected UdpClient _receiveSocket;
        public RemotePlayer(TanksGame game) : base(game)
        {
        }

        public override void Update(GameTime time)
        {
            throw new NotImplementedException();
        }
    }
}
