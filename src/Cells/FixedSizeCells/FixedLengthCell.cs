using System;
using System.IO;

namespace Torpedo
{
    abstract class FixedLengthCell : Cell
    {
        public const int MaxPayloadSize = 509;

        protected FixedLengthCell(uint circuitId, CommandType command)
            : base(circuitId, command)
        {
        }

        protected override void WritePayload(BinaryWriter writer)
        {
            var payload = GetPayload();
            writer.Write(payload);
            var fillLength = FixedLengthCell.MaxPayloadSize - payload.Length;
            var fillBuffer = new byte[fillLength];
            Array.Fill(fillBuffer, (byte)0);
            writer.Write(fillBuffer);
        }
    }
}