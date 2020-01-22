using System;
using System.Threading.Tasks;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("TorpedoTests")]
namespace Torpedo
{
    class Program
    {
        public static async Task Main(string[] args) 
        {
            
            Console.WriteLine("Hello World!");
            var torpedo = new Torpedo();
            await torpedo.InitializeAsync();
            await torpedo.GetAsync("http://ljxhgchpkhjbaioeaijwejewxnap.onion");
        }
    }
}
