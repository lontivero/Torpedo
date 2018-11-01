using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace Torpedo
{
    class OnionRouter
    {
        public string Nickname { get; }
        public IPEndPoint DirEndPoint { get; }
        public IPEndPoint TorEndPoint { get; }
        public string Fingerprint { get; }
        public string KeyTap { get; private set; }
        public StatusEntryS Flags { get; internal set; }

        public string DescriptorUrl => 
            $"http://{DirEndPoint}/tor/server/fp/{Fingerprint}";

        public OnionRouter(string nickname, IPEndPoint dirEndpoint, IPEndPoint torEndpoint, string fingerprint )
        {
            Nickname = nickname;
            DirEndPoint = dirEndpoint;
            TorEndPoint = torEndpoint;
            Fingerprint = fingerprint;
        }

        internal static OnionRouter FromConsensus(string line)
        {
            var parts = line.Split(' ');
            var nickname= parts[(int)StatusEntryR.Nickname];
            var fingerprint = BitConverter.ToString(Base64.DecodeWithoutPadding(parts[(int)StatusEntryR.Identity])).Replace("-", "");
            var ip = IPAddress.Parse(parts[(int)StatusEntryR.Ip]);
            var torPort = ushort.Parse(parts[(int)StatusEntryR.TorPort]);
            var dirPort = ushort.Parse(parts[(int)StatusEntryR.DirPort]); 
            return new OnionRouter(nickname, new IPEndPoint(ip, dirPort), new IPEndPoint(ip, torPort), fingerprint);
        }

        public override string ToString() =>
            $"OnionRouter(Nickname={Nickname}, DirEndPoint={DirEndPoint}, TorEndPoint={TorEndPoint}, Fingerprint={Fingerprint}, Flags={Flags}, ntor_key=%s)";

        public void ParseDescriptor(Stream content)
        {
            var parsingKey = false;
            var sb = new StringBuilder();
            using(var reader = new StreamReader(content))
            { 
                while(!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    if(line.StartsWith("onion-key"))
                    {
                        parsingKey = true;
                        continue;
                    }

                    if(parsingKey)
                    {
                        sb.Append(line);
                        if(line.Contains("END RSA PUBLIC KEY") ) break;
                    }
                }
            }
            KeyTap = sb.ToString();
        }
    }
}
