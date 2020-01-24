using System;
using System.IO;

namespace Torpedo
{
    abstract class Cell
    {
        public const int FixLengthCellSize = 514;
        public const int MaxPayloadSize = 509;

        public uint CircuitId { get; }
        public CommandType Command { get; }

        protected Cell(uint circuitId, CommandType command)
        {
            CircuitId = circuitId;
            Command = command;
        }

        public byte[] ToByteArray(int protocolVersion)
        {
            using(var mem = new MemoryStream())
            {
                WriteTo(mem, protocolVersion);
                return mem.ToArray();
            }
        }

        public void WriteTo(Stream stream, int protocolVersion)
        {
            using(var w = new BEBinaryWriter(stream))
            {
                if(protocolVersion >= 4)
                {
                    w.Write((uint)CircuitId);
                }
                else
                {
                    w.Write((ushort)CircuitId);
                }
                w.Write((byte)Command);
                WritePayload(w);
                w.Flush();
            }
        }

        protected abstract void WritePayload(BinaryWriter writer);
        protected abstract void ReadPayload(BinaryReader reader);

        public static Cell ReadFrom(Stream stream, int protocolVersion)
        {
            using(var reader = new BEBinaryReader(stream))
            {
                var circuitId = (protocolVersion >= 4) 
                    ? (uint)reader.ReadUInt32()
                    : (uint)reader.ReadUInt16();

                var command = (CommandType)reader.ReadByte();
                var cell = CreateFromCommand(circuitId, command);
                cell.ReadPayload(reader);

                return cell;
            }
        }

        private static Cell CreateFromCommand(uint circuitId, CommandType command)
        {
            return command switch
            {
                CommandType.Padding => new PaddingCell(circuitId),
                CommandType.Create  => null,
                CommandType.Created => null,
                CommandType.Relay   => null,
                CommandType.Destroy => null,
                CommandType.CreateFast  => null,
                CommandType.CreatedFast => null,
                CommandType.NetInfo     => new NetInfoCell(circuitId),
                CommandType.RelayEarly  => null,
                CommandType.Create2  => null,
                CommandType.Created2 => null,
                CommandType.Versions => new VersionsCell(circuitId),
                CommandType.VPadding => null,
                CommandType.Certs => new CertsCell(circuitId),
                CommandType.AuthChallenge => new AuthChallengeCell(circuitId),
                CommandType.Authenticate => null,
                _ => throw new InvalidOperationException($"Unknown command {command} received.")
            };
        }

        public static bool IsVariableLengthCommand(CommandType command)
        {
            return command == CommandType.Versions || command >= CommandType.VPadding;
        }

    }
}