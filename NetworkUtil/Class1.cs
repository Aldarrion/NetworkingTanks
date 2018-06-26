using System;
using System.Collections.Generic;
using System.IO;

namespace NetworkUtil
{
    public static class NetworkUtil
    {
        private static byte[] ReadMessage(Stream stream)
        {
            var message = new List<byte>();
            var data = new byte[256];

            while (stream.Read(data, 0, data.Length) > 0)
            {
                message.AddRange(data);
            }

            return message.ToArray();
        }
    }
}
