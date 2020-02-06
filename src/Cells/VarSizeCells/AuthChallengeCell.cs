using System.IO;

namespace Torpedo
{

    class AuthChallengeCell : VariableLengthCell
    {
        public AuthChallengeCell(uint circuitId)
            : base(circuitId, CommandType.AuthChallenge)
        {
        }

        protected override byte[] GetPayload()
        {
            return new byte[0];
        }

        protected override void ReadPayload(BinaryReader reader)
        {
            var payloadLength = reader.ReadUInt16();
            var certs = reader.ReadBytes(payloadLength); // ignore it;
        }
    }
}