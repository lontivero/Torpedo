using System;
using System.Threading;
using System.Threading.Tasks;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("TorpedoTests")]
namespace Torpedo
{
    class Program
    {
        private static CancellationTokenSource cts;
        
        public static async Task Main(string[] args) 
        {
            Console.WriteLine("Hello World!");
            var torpedo = new Torpedo();
            cts = new CancellationTokenSource();
            await torpedo.InitializeAsync(cts.Token);
            await torpedo.GetAsync("http://ljxhgchpkhjbaioeaijwejewxnap.onion", cts.Token);
        }
    }
}
