using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Torpedo
{

    enum StatusEntryR
    {
        Nickname = 1,
        Identity = 2,
        Digest = 3,
        PublicationDate=4,
        PublicationTime=5,
        Ip=6,
        TorPort=7,
        DirPort=8,
        ItemCount=9,
    }

    [Flags]
    enum StatusEntryS
    {
        None            = 0x0000,
        Authority       = 0x0001,
        BadExit         = 0x0002,
        Exit            = 0x0004,
        Fast            = 0x0008,
        Guard           = 0x0010,
        hsdir           = 0x0020,
        Named           = 0x0040,
        NoEdConsensus   = 0x0080,
        Stable          = 0x0100,
        StaleDesc       = 0x0200,
        Running         = 0x0400,
        Unnamed         = 0x0800,
        Valid           = 0x1000,
        V2dir           = 0x2000
    };

    internal class Consensus
    {
        private Logger logger = Logger.GetLogger<Consensus>();

        private List<OnionRouter> _parsed = new List<OnionRouter>();

        public async Task ParseAsync(Stream content, CancellationToken cancellationToken)
        {
            var expectedFlags = StatusEntryS.Running | StatusEntryS.Valid | StatusEntryS.Fast | StatusEntryS.Stable;

            OnionRouter onionRouter = null;
            
            using var reader = new StreamReader(content);
            while(!reader.EndOfStream && _parsed.Count < 200)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var line = await reader.ReadLineAsync().ConfigureAwait(false);
                if(line.StartsWith("r "))
                {
                    onionRouter = OnionRouter.FromConsensus(line);
                }
                else if(line.StartsWith("s "))
                {
                    foreach(var parsedFlag in line.Substring(2).Split(' ', StringSplitOptions.RemoveEmptyEntries))
                    {
                        onionRouter.Flags |= (StatusEntryS)Enum.Parse(typeof(StatusEntryS), parsedFlag, true);
                    }
                    if(onionRouter.Flags.HasFlag(expectedFlags))
                    {
                        _parsed.Add(onionRouter);
                    }
                }
            }
        }

        public IEnumerable<OnionRouter> OnionRouters
            => _parsed;

        public IEnumerable<OnionRouter> GuardRelays
            => OnionRouters.Where(o=>o.Flags.HasFlag(StatusEntryS.Guard));
    }
}
