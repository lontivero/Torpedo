using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace Torpedo;

record OnionRouter(string Nickname, IPEndPoint DirEndPoint, IPEndPoint TorEndPoint, string Fingerprint)
{
    public string KeyTap { get; private set; }
    public string NTorKey { get; private set; }
    public StatusEntryS Flags { get; internal set; }

    public string DescriptorUrl => 
        $"http://{DirEndPoint}/tor/server/fp/{Fingerprint}";

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
        $"OnionRouter(Nickname={Nickname}, DirEndPoint={DirEndPoint}, TorEndPoint={TorEndPoint}, Fingerprint={Fingerprint}, Flags={Flags}, ntor_key={NTorKey})";

    enum DescriptorParsingState
    {
        None,
        IdentityEd25519,
        MasterKeyEd25519,
        OnionKey,
        SigningKey
    }
    
    public void ParseDescriptor(Stream content)
    {
        var pasrsingState = DescriptorParsingState.None;
        StringBuilder sb = null;

        using(var reader = new StreamReader(content))
        { 
            while(!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                if(line.StartsWith("onion-key"))
                {
                    pasrsingState = DescriptorParsingState.OnionKey;
                    sb = new StringBuilder();
                    continue;
                }

                if(pasrsingState == DescriptorParsingState.OnionKey)
                {
                    sb.Append(line);
                    if(line.Contains("END RSA PUBLIC KEY") ) 
                    {
                        pasrsingState = DescriptorParsingState.None;
                        KeyTap = sb.ToString();
                    }
                }
                if(line.StartsWith("ntor-onion-key "))
                    NTorKey = line.Substring(line.IndexOf(" ")).Trim();
            }
        }
    }
}