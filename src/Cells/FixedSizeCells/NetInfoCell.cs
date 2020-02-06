using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace Torpedo
{
    class NetInfoCell : FixedLengthCell
    {
        public DateTimeOffset Timestamp { get; set; }
        public IPAddress MyIPAddress { get; set; }
        public List<IPAddress> OtherIPs { get; set; }

        public NetInfoCell(uint circuitId)
            : base(circuitId, CommandType.NetInfo)
        {
            OtherIPs = new List<IPAddress>();
            Timestamp = DateTimeOffset.UtcNow;
        }

        protected override void ReadPayload(BinaryReader reader)
        {
            var payload = reader.ReadBytes(MaxPayloadSize);
            using var preader = new BEBinaryReader(new MemoryStream(payload)); 
            Timestamp = DateTimeOffset.FromUnixTimeSeconds(preader.ReadUInt32());
            MyIPAddress = preader.ReadIPAddress();
            var numOfAddresses  = preader.ReadByte();
            for(var i=0; i< numOfAddresses; i++)
            {
                var remote = preader.ReadIPAddress();
                if( remote != null)
                {
                    OtherIPs.Add( remote );
                }
                else
                {
                    break;
                }
            }
        }

        protected override byte[] GetPayload()
        {
            using var mem = new MemoryStream();
            using var writer = new BEBinaryWriter(mem);
            writer.Write((uint)Timestamp.ToUnixTimeSeconds());
            writer.Write(MyIPAddress);
            writer.Write((byte)OtherIPs.Count);
            foreach(var remote in OtherIPs)
            {
                writer.Write(remote);
            }
            return mem.ToArray();
        }
    }
}