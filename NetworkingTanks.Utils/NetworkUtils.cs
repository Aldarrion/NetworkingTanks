using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lidgren.Network;
using Protobufs.NetworkTanks.Game;

namespace NetworkingTanks.Utils
{
    public static class NetworkUtils
    {
        public static NetOutgoingMessage ToNetOutMsg(this WrapperMessage wrapperMessage, NetPeer peer)
        {
            NetOutgoingMessage outMsg = peer.CreateMessage();
            outMsg.Write(wrapperMessage.CalculateSize());
            outMsg.Write(Protobufs.Utils.GetBinaryData(wrapperMessage));
            return outMsg;
        }
    }
}
