using System.IO;

namespace Torpedo;

abstract class VariableLengthCell : Cell
{
    protected VariableLengthCell(uint circuitId, CommandType command)
        : base(circuitId, command)
    {
    }

    protected override void WritePayload(BinaryWriter writer)
    {
        var payload = GetPayload();
        writer.Write((ushort)payload.Length);
        writer.Write(payload);
    }
}