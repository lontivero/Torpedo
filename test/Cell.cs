using System;
using System.IO;
using System.Linq;
using System.Net;
using Torpedo;
using Xunit;

namespace test
{
    public class CellTests
    {
        [Fact]
        public void VersionsCell()
        {
            var versions = new VersionsCell(circuitId: 0, 3, 4, 5);
            // serialize 
            var serializedCell = versions.ToByteArray(protocolVersion: 0);
            var expected = new byte[]{0, 0, 7, 0, 6, 0, 3, 0, 4, 0, 5};
            Assert.Equal(expected, serializedCell);

            // deserialize
            var stream = new MemoryStream(serializedCell);
            var deserializedCell = (VersionsCell)Cell.ReadFrom(stream, protocolVersion: 0);
            Assert.Equal(versions.Command, deserializedCell.Command);
            Assert.Equal(versions.CircuitId, deserializedCell.CircuitId);
            Assert.Equal(versions.Versions, deserializedCell.Versions);
        }

        [Fact]
        public void NetInfoCell()
        {
            var netInfo = new NetInfoCell(circuitId: 0);
            netInfo.Timestamp = DateTimeOffset.FromUnixTimeSeconds(1898763926);
            netInfo.MyIPAddress = IPAddress.Loopback;
            netInfo.OtherIPs.Add(IPAddress.Parse("186.56.221.91"));

            // serialize 
            var serializedCell = netInfo.ToByteArray(protocolVersion: 4);
            var expected = new byte[]{0, 0, 0, 0, 8, 113, 44, 214, 150, 4, 4, 127, 0, 0, 1, 1, 4, 4, 186, 56, 221, 91};
            Assert.Equal(expected, serializedCell);

            // deserialize
            var stream = new MemoryStream(serializedCell);
            var deserializedCell = (NetInfoCell)Cell.ReadFrom(stream, protocolVersion: 4);
            Assert.Equal(netInfo.Command, deserializedCell.Command);
            Assert.Equal(netInfo.CircuitId, deserializedCell.CircuitId);
            Assert.Equal(netInfo.MyIPAddress.ToString(), deserializedCell.MyIPAddress.ToString());
            var other = Assert.Single(deserializedCell.OtherIPs);
            Assert.Equal(netInfo.OtherIPs.First(), other);
        }

/*
        [Fact]
        public void CreateCellTest()
        {
            var Tap = 0x2;
            var createPayload = Packer.Pack("S", Tap);
            var versions = new Cell(34, CommandType.Create, createPayload);
            var serializedCell = versions.ToByteArray();
            var expected = new byte[]{0, 34, 7, 0, 6};

            Assert.Equal(expected, serializedCell);
        }
*/
    }
}
