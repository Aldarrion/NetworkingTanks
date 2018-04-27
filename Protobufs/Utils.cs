using Google.Protobuf;

namespace Protobufs
{
    public static class Utils
    {
        public static byte[] GetBinaryData(IMessage protobuf)
        {
            var binaryData = new byte[protobuf.CalculateSize()];
            var stream = new CodedOutputStream(binaryData);
            protobuf.WriteTo(stream);

            return binaryData;
        }
    }
}
