using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Torpedo
{
    class Torpedo
    {
        private Logger logger = Logger.GetLogger<Torpedo>();

        private Consensus Consensus { get; } = new Consensus();
        private bool _isInitialized = false;

        public Torpedo()
        {
        }

        public async Task InitializeAsync()
        {
            logger.Info("Initializing...");
            var da = DirectoryAuthority.KnownAuthorities.Random();

            logger.Debug($"Using {da}");
            logger.Debug($"Downloading consensus document from {da.Url}");

            using(var http = new HttpClient())
            {
                var response = await http.GetAsync(da.Url, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                
                using(var contentStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                {
                    logger.Info("Parsisng consensus document");
                    var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                    await Consensus.ParseAsync(contentStream, cts.Token);
                    contentStream.Close();
                }
            }

            _isInitialized = true;
            logger.Info("Initialized");
        }

        public async Task<string> GetAsync(string url)
        {
            if(!_isInitialized)
                throw new InvalidOperationException("Tornado instance is not initialized");
            
            logger.Info($"GET {url}");

            var guardRelay = Consensus.GuardRelays.Random();
            logger.Debug($"Using {guardRelay}");

            using(var http = new HttpClient())
            {
                http.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (X11; Ubuntu; Linux x86_64; rv:62.0) Gecko/20100101 Firefox/62.0"); 
                var response = await http.GetAsync(guardRelay.DescriptorUrl, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();

                using(var content = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                {
                    logger.Debug("Parsing descriptor");
                    guardRelay.ParseDescriptor(content);
                    content.Close();

                    logger.Debug($"ntor-onion-key: {guardRelay.NTorKey}");
                }
            }
            var socket = new TorSocket(guardRelay);
            socket.Connect();
            return null;
        }
    }
}