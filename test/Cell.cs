using System;
using System.IO;
using Torpedo;
using Xunit;

namespace test
{
    public class CellTests
    {
        [Fact]
        public void VersionsCellTest()
        {
            var versionsPayload = Packer.Pack("^3S", 3, 4, 5);
            var versions = new Cell(0, CommandType.Versions, versionsPayload);
            var serializedCell = versions.ToByteArray();
            var expected = new byte[]{0, 0, 7, 0, 6, 0, 3, 0, 4, 0, 5};

            Assert.Equal(expected, serializedCell);
        }

        [Fact]
        public void CreateCellTest()
        {
            var createPayload = Packer.Pack("^3S", "tap");
            var versions = new Cell(34, CommandType.Create, createPayload);
            var serializedCell = versions.ToByteArray();
            var expected = new byte[]{0, 17, 7, 0, 6, 0, 3, 0, 4, 0, 5};

            Assert.Equal(expected, serializedCell);
        }
    }
}
