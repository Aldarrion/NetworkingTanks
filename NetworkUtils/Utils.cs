using System;
using System.Collections.Generic;
using System.IO;

namespace NetworkingTanks.Utils
{
    public class Utils
    {
        public static byte[] ReadMessage(Stream stream, int bufferSize)
        {
            var data = new byte[bufferSize];

            int readCount = stream.Read(data, 0, data.Length);

            var message = new List<byte>(data);
            return message.GetRange(0, readCount).ToArray();
        }
    }
}
