using System;
using System.IO;

namespace Torpedo
{
    abstract class FixedLengthCell : Cell
    {
        protected FixedLengthCell(uint circuitId, CommandType command)
            : base(circuitId, command)
        {
        }

        protected override void WritePayload(BinaryWriter writer)
        {
            throw new NotImplementedException();
        }
    }
}