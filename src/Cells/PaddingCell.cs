using System;
using System.IO;

namespace Torpedo
{
    class PaddingCell : VariableLengthCell
    {
        private static Random Random = new Random();

        public PaddingCell(uint circuitId)
            : base(circuitId, CommandType.Padding)
        {
        }

        protected override byte[] GetPayload()
        {
            var len = Random.Next(10, MaxPayloadSize);
            var buffer = new byte[len];
            Random.NextBytes(buffer);
            return buffer;
        }

        protected override void ReadPayload(BinaryReader reader)
        {
            var payloadLength = reader.ReadUInt16();
            reader.ReadBytes(payloadLength); // ignore it;
        }
    }
}