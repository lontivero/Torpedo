using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace Torpedo
{
    static class ListExtensions
    {
        private static Random rnd = new Random();

        public static T Random<T>(this IEnumerable<T> me)
        {
            var arr = me.ToArray();
            return arr[rnd.Next(arr.Length)];
        }
    }

    static class Base64
    {
        public static byte[] DecodeWithoutPadding(string str)
        {
            str += new String('=', 4 - (str.Length % 4));
            return Convert.FromBase64String(str);
        }
    }

    static class BinaryReaderExtensions
    {
        public static byte[] ReadVariableBytes(this BinaryReader reader)
        {
            var len = reader.ReadByte();
            return reader.ReadBytes(len);
        } 
    }

   static class BinaryWriterExtensions
    {
        public static void WriteVariableBytes(this BinaryWriter writer, byte[] data)
        {
            writer.Write((byte)data.Length);
            writer.Write(data);
        }

        public static void Write(this BinaryWriter writer, IPAddress ip)
        {
            writer.Write((byte)(ip.AddressFamily == AddressFamily.InterNetwork ? 0x04 /* IPv4 */ : 0x06 /* IPv6 */ ));
            var ipBuffer = ip.GetAddressBytes();
            writer.Write((byte)ipBuffer.Length);
            writer.Write(ipBuffer);
        }
    }
}