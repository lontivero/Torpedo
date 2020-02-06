using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Security.Cryptography;

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
        public static byte[] DecodeWithoutPadding(string str, bool bigEndian = false)
        {
            var fill = 4 - (str.Length % 4);
            if( fill < 4)
                str += new String('=', fill);
            var byteArray = Convert.FromBase64String(str);
            if( bigEndian)  Array.Reverse(byteArray);
            return byteArray;
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

    static class ByteArrayHelpers
    {
        public static byte[] Combine(params byte[][] arrays)
        {
            byte[] ret = new byte[arrays.Sum(x => x.Length)];
            int offset = 0;
            foreach (byte[] data in arrays)
            {
                Buffer.BlockCopy(data, 0, ret, offset, data.Length);
                offset += data.Length;
            }
            return ret;
        }

        public static byte[] Sha512(this byte[] m)
        {
            using var sha512 = SHA512Managed.Create();
            return sha512.ComputeHash(m);
        }

        public static int GetBit(this byte[] h, int i)
        {
            return h[i / 8] >> (i % 8) & 1;
        }

        public static byte[] CopyOfRange(this byte[] original, int from, int to)
        {
            int length = to - from;
            var result = new byte[length];
            Array.Copy(original, from, result, 0, length);
            return result;
        }
    }

    static class StringConverter
    {
        public static byte[] ToByteArray(String hexString)
        {
            var retval = new byte[hexString.Length / 2];
            for (int i = 0; i < hexString.Length; i += 2)
            {
                retval[i / 2] = Convert.ToByte(hexString.Substring(i, 2), 16);
            }
            return retval;
        }
    }

    static class BigIntegerExtensions
    {
        public static byte[] Encode(this BigInteger val)
        {
            byte[] nin = val.ToByteArray();
            var nout = new byte[Math.Max(nin.Length, 32)];
            Array.Copy(nin, nout, nin.Length);
            return nout;
        }

        public static BigInteger Mod(this BigInteger num, BigInteger modulo)
        {
            var result = num % modulo;
            return result.Sign == -1  ? result + modulo : result;
        }
    }
}