using System;
using System.IO;
using Torpedo;
using Xunit;

namespace test
{
    public class ConsensusTests
    {
        [Fact]
        public void Test1()
        {
            var document = File.OpenRead("data/consensus.txt");
            var consensus = new Consensus();
            consensus.Parse(document);
        }
    }
}
