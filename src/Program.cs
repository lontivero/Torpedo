using System;
using System.Threading;
using System.Threading.Tasks;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("TorpedoTests")]
namespace Torpedo;

class Program
{
    public static async Task Main(string[] args) 
    {
        var torpedo = new Torpedo();
        var cts = new CancellationTokenSource();
        await torpedo.GetAsync("http://wasabiwallet.io", cts.Token);
    }
}