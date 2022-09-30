using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Torpedo;
using Xunit;

namespace test;

public class ConsensusTests
{
    [Fact]
    public async Task Test1()
    {
        var document = File.OpenRead("data/consensus.txt");
        var consensus = new Consensus();
        await consensus.ParseAsync(document, CancellationToken.None);
    }
}