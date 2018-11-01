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
            var torix = new Torix();
            await torix.InitializeAsync();
            await torix.GetAsync("http://ljxhgchpkhjbaioeaijwejewxnap.onion");
        }
    }
}
