using System;
using System.IO;
using System.Net.Security;
using System.Text;

namespace Torpedo
{
    enum CommandType
    {
        Padding = 0,
        Create = 1,
        Created = 2,
        Relay = 3,
        Destroy = 4,
        CreateFast = 5,
        CreatedFast = 6,
        NetInfo = 8,
        RelayEarly = 9,
        Create2 = 10,
        Created2 = 11,
        Versions = 7,
        VPadding = 128,
        Certs = 129,
        AuthChallenge = 130,
        Authenticate = 131
    }


    class Cell
    {
        private static int FixLengthCellSize = 514;
        private static int MaxPayloadSize = 509;


        public uint CircuitId { get; private set; }
        public CommandType Command { get; private set; }
        public byte[] Payload { get; private set; }

        internal Cell(){}

        public Cell(uint circuitId, CommandType command, params byte[] payload)
        {
            CircuitId = circuitId;
            Command = command;
            Payload = payload;
        }

        public byte[] ToByteArray()
        {
            using(var mem = new MemoryStream())
            {
                WriteTo(mem);
                return mem.ToArray();
            }
        }

        public Cell FromByteArray(byte[] data, int protocolVersion=4)
        {
            using(var mem = new MemoryStream(data))
            {
                return Cell.ReadFrom(mem, protocolVersion);
            }
        }

        public void WriteTo(Stream stream)
        {
            using(var w = new BEBinaryWriter(stream))
            {
                var payloadBytes = (Command == CommandType.Versions) 
                    ? Payload
                    : new byte[0];
                
                w.Write((ushort)CircuitId);
                w.Write((byte)Command);
                w.Write((ushort)payloadBytes.Length);
                w.Write(payloadBytes);
                w.Flush();
            }
        }

        public static Cell ReadFrom(Stream stream, int protocolVersion)
        {
            using(var r = new BEBinaryReader(stream))
            {
                var circuitId = (protocolVersion >= 4) 
                    ? (uint)r.ReadUInt32()
                    : (uint)r.ReadUInt16();

                var command = (CommandType)r.ReadByte();
                var len = Cell.IsVariableLengthCommand(command)
                    ? r.ReadUInt16()
                    : MaxPayloadSize;
                var payload = r.ReadBytes(len);

                return new Cell(circuitId, command, payload);
            }
        }

        public static bool IsVariableLengthCommand(CommandType command)
        {
            return command == CommandType.Versions || command >= CommandType.VPadding;
        }
    }
}