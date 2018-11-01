using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        Running         = 0x0200,
        Unnamed         = 0x0400,
        Valid           = 0x0800,
        V2dir           = 0x1000
    };

    internal class Consensus
    {
        public DirectoryAuthority RandomDirectoryAuthority()
            => DirectoryAuthority.KnownAuthorities.PickOne();

        private List<OnionRouter> _parsed = new List<OnionRouter>();

        public void Parse(Stream content)
        {
            var expectedFlags = StatusEntryS.Running | StatusEntryS.Valid | StatusEntryS.Fast | StatusEntryS.Stable;

            OnionRouter onionRouter = null;
            
            using(var reader = new StreamReader(content))
            {
                while(!reader.EndOfStream && _parsed.Count < 200)
                {
                    var line = reader.ReadLine();
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
        }

        public OnionRouter GetRandomGuardRelay()
            => _parsed.First(x=>x.Nickname.ToLower() == "harutorrelay");
        //    => _parsed.Where(o=>o.Flags.HasFlag(StatusEntryS.Guard)).PickOne();
    }
}
