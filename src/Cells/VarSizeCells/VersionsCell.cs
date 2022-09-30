using System.Collections.Generic;
using System.IO;

namespace Torpedo;

class VersionsCell : VariableLengthCell
{
    public ushort[] Versions { get; private set; }

    public VersionsCell(uint circuitId, params ushort[] versions)
        : base(circuitId, CommandType.Versions)
    {
        Versions = versions;
    }

    protected override void ReadPayload(BinaryReader reader)
    {
        var payloadLength = reader.ReadUInt16();
        var verCount = payloadLength / sizeof(ushort);
        var vers = new List<ushort>(verCount);
        for(var i=0; i < verCount; i++)
        {
            vers.Add(reader.ReadUInt16());
        }
        Versions = vers.ToArray();
    }

    protected override byte[] GetPayload()
    {
        using var mem = new MemoryStream(Versions.Length * sizeof(ushort));
        using var writer = new BEBinaryWriter(mem);
        foreach(var ver in Versions)
        {
            writer.Write(ver);
        }
        return mem.ToArray();
    }
}