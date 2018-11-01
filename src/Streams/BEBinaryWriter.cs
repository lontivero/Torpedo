using System;
using System.IO;
using System.Text;

namespace Torpedo
{
    class BEBinaryWriter : BinaryWriter
    {
        public BEBinaryWriter(Stream output) : base(output, Encoding.ASCII, leaveOpen: true)
        {
        }

        public override void Write(short value)
        {
            var data = BitConverter.GetBytes(value);
            Array.Reverse(data);
            base.Write(data);
        }

        public override void Write(int value)
        {
            var data = BitConverter.GetBytes(value);
            Array.Reverse(data);
            base.Write(data);
        }
        public override void Write(long value)
        {
            var data = BitConverter.GetBytes(value);
            Array.Reverse(data);
            base.Write(data);
        }

        public override void Write(ushort value)
        {
            var data = BitConverter.GetBytes(value);
            Array.Reverse(data);
            base.Write(data);
        }

        public override void Write(uint value)
        {
            var data = BitConverter.GetBytes(value);
            Array.Reverse(data);
            base.Write(data);
        }
        public override void Write(ulong value)
        {
            var data = BitConverter.GetBytes(value);
            Array.Reverse(data);
            base.Write(data);
        }
    }
}