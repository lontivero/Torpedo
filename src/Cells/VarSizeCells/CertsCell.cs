using System;
using System.IO;

namespace Torpedo;

class CertsCell : VariableLengthCell
{
    public CertsCell(uint circuitId)
        : base(circuitId, CommandType.Certs)
    {
    }

    protected override byte[] GetPayload()
    {
        return Array.Empty<byte>();
    }

    protected override void ReadPayload(BinaryReader reader)
    {
        var payloadLength = reader.ReadUInt16();
        var certs = reader.ReadBytes(payloadLength); // ignore it;
    }
}