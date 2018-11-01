using System;
using System.IO;
using System.Text;

namespace Torpedo
{
    class BEBinaryReader : BinaryReader
    {
        public BEBinaryReader(Stream input) : base(input, Encoding.ASCII, leaveOpen: true)
        {
        }

        public override byte ReadByte()
        {
            return base.ReadByte();
        }

        public override short ReadInt16()
        {
            var data = base.ReadBytes(sizeof(short));
            Array.Reverse(data);
            return BitConverter.ToInt16(data);
        }

        public override int ReadInt32()
        {
            var data = base.ReadBytes(sizeof(int));
            Array.Reverse(data);
            return BitConverter.ToInt32(data);
        }

        public override long ReadInt64()
        {
            var data = base.ReadBytes(sizeof(long));
            Array.Reverse(data);
            return BitConverter.ToInt64(data);
        }

        public override ushort ReadUInt16()
        {
            var data = base.ReadBytes(sizeof(ushort));
            Array.Reverse(data);
            return BitConverter.ToUInt16(data);
        }

        public override uint ReadUInt32()
        {
            var data = base.ReadBytes(sizeof(uint));
            Array.Reverse(data);
            return BitConverter.ToUInt32(data);
        }

        public override ulong ReadUInt64()
        {
            var data = base.ReadBytes(sizeof(ulong));
            Array.Reverse(data);
            return BitConverter.ToUInt64(data);
        }
    }
}