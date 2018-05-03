using Google.Protobuf;

namespace Protobufs
{
    public static class Utils
    {
        public static byte[] GetBinaryData(IMessage msg)
        {
            var binaryData = new byte[msg.CalculateSize()];
            var stream = new CodedOutputStream(binaryData);
            msg.WriteTo(stream);

            return binaryData;
        }
    }
}
