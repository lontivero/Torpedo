using System;
using System.IO;
using System.Linq;

namespace Torpedo;

class Created2Cell : FixedLengthCell
{
    public Ed25519Point Y { get; private set; }
    public byte[] Auth { get; private set; }

    public Created2Cell(uint circuitId)
        : base(circuitId, CommandType.Created2)
    {
    }

    protected override byte[] GetPayload()
    {
        using var mem = new MemoryStream();
        using var writer = new BEBinaryWriter(mem);
        var length = 32 + Auth.Length;

        writer.Write((short)length);
        writer.Write(Y.EncodePoint());
        writer.Write(Auth);
        return mem.ToArray();
    }

    protected override void ReadPayload(BinaryReader reader)
    {
        var payload = reader.ReadBytes(MaxPayloadSize);
        using var preader = new BEBinaryReader(new MemoryStream(payload)); 
        var len = preader.ReadUInt16();
        Y = Ed25519Point.DecodePoint(preader.ReadBytes(32));
        Auth = preader.ReadBytes(len - 32);
    }
}