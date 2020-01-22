using System.Net;

namespace Torpedo
{
    class DirectoryAuthority
    {
        public readonly static DirectoryAuthority[] KnownAuthorities = {
            new DirectoryAuthority("maatuska", "171.25.193.9", 443, 80),
            new DirectoryAuthority("tor26", "86.59.21.38", 80, 443),
            new DirectoryAuthority("longclaw", "199.58.81.140", 80, 443),
            new DirectoryAuthority("dizum", "194.109.206.212", 80, 443),
            new DirectoryAuthority("bastet", "204.13.164.118", 80, 443),
            new DirectoryAuthority("gabelmoo", "131.188.40.189", 80, 443),
            new DirectoryAuthority("moria1", "128.31.0.34", 9131, 9101),
            new DirectoryAuthority("dannenberg", "193.23.244.244", 80, 443),
            new DirectoryAuthority("faravahar", "154.35.175.225", 80, 443)
        };

        public string Nickname { get; }
        public IPEndPoint DirectoryEndPoint { get; }

        public IPEndPoint RouterEndPoint { get; }

        public string Url => $"http://{DirectoryEndPoint}/tor/status-vote/current/consensus";
        public DirectoryAuthority(string nickname, string ip, ushort dirPort, ushort orPort)
        {
            Nickname = nickname;
            DirectoryEndPoint = new IPEndPoint(IPAddress.Parse(ip), dirPort);
            RouterEndPoint = new IPEndPoint(IPAddress.Parse(ip), orPort);
        }

        public override string ToString()
        {
            return $"Trusted Authority(Nickname: {Nickname}  Directory: {DirectoryEndPoint}  Onion Router: {RouterEndPoint})";
        }
    }
}
