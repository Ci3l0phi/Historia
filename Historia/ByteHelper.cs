using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Historia
{
    /// <summary>
    /// Provides for utility methods dealing with byte arrays.
    /// </summary>
    public static class ByteHelper
    {
        /// <summary>
        /// Returns a string of bytes represented in hexadecimal.
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public static string ConvertToHex(byte[] buffer)
        {
            StringBuilder hex = new StringBuilder();
            foreach (var b in buffer)
            {
                hex.AppendFormat("{0:x2}", b);
                hex.Append(" ");
            }
            return hex.ToString();
        }

        /// <summary>
        /// Creates and returns a copy of the supplied byte array.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static byte[] Copy(byte[] buffer, int length)
        {
            byte[] copy = new byte[length];
            Buffer.BlockCopy(buffer, 0, copy, 0, length);
            return copy;
        }

        /// <summary>
        /// Decompresses a byte array.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static byte[] Decompress(byte[] buffer)
        {
            using (var ms = new MemoryStream(buffer))
            using (var ds = new DeflateStream(ms, CompressionMode.Decompress))
            {
                ds.Read(buffer, 0, buffer.Length);
                return ms.ToArray();
            }
        }

        public static byte[] Prepend(byte[] first, byte[] last)
        {
            var buffer = new byte[first.Length + last.Length];
            Buffer.BlockCopy(first, 0, buffer, 0, first.Length);
            Buffer.BlockCopy(last, 0, buffer, first.Length, last.Length);
            return buffer;
        }
    }
}
