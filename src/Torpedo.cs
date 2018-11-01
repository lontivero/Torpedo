using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Torpedo
{
    class Torpedo
    {
        private Consensus Consensus { get; } = new Consensus();
        private bool _isInitialized = false;

        public Torpedo()
        {
        }

        public async Task InitializeAsync()
        {
            var da = DirectoryAuthority.KnownAuthorities.PickOne();

            using(var http = new HttpClient())
            {
                var response = await http.GetAsync(da.Url, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();
                using(var contentStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                {
                    Consensus.Parse(contentStream);
                    contentStream.Close();
                }
            }
            _isInitialized = true;
        }

        public async Task<string> GetAsync(string url)
        {
            if(!_isInitialized)
                throw new InvalidOperationException("Torix instance is not initialized");
            
            var guardRelay = Consensus.GetRandomGuardRelay();
            
            using(var http = new HttpClient())
            {
                http.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (X11; Ubuntu; Linux x86_64; rv:62.0) Gecko/20100101 Firefox/62.0"); 
                var response = await http.GetAsync(guardRelay.DescriptorUrl, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();

                using(var content = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                {
                    guardRelay.ParseDescriptor(content);
                    content.Close();
                }
            }
            var socket = new TorSocket(guardRelay);
            socket.Connect();
            return null;
        }
    }
}