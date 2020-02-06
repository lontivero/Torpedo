using System;
using System.IO;

namespace Torpedo
{
    class PaddingCell : FixedLengthCell
    {
        public PaddingCell(uint circuitId)
            : base(circuitId, CommandType.Padding)
        {
        }

        protected override byte[] GetPayload()
        {
            return new byte[0];
        }

        protected override void ReadPayload(BinaryReader reader)
        {
            var payloadLength = reader.ReadUInt16();
            reader.ReadBytes(payloadLength); // ignore it;
        }
    }
}