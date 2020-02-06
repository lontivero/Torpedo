using System;
using System.IO;

namespace Torpedo
{
    enum HandshakeType
    {
        Tap = 1,
        NTor = 2
    };

    class Create2Cell : FixedLengthCell
    {
        public HandshakeType HandshakeType { get; }
        public byte[] Handshake { get; }

        public Create2Cell(uint circuitId, HandshakeType handshakeType, byte[] handshake)
            : base(circuitId, CommandType.Create2)
        {
            HandshakeType = handshakeType;
            Handshake = handshake;
        }

        protected override byte[] GetPayload()
        {
            using var mem = new MemoryStream();
            using var writer = new BEBinaryWriter(mem);

            writer.Write((short)HandshakeType);
            writer.Write((short)Handshake.Length);
            writer.Write(Handshake);
            return mem.ToArray();
        }

        protected override void ReadPayload(BinaryReader reader)
        {
            var payloadLength = reader.ReadUInt16();
            reader.ReadBytes(payloadLength); // ignore it;
        }
    }
}