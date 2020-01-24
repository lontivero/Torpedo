namespace Torpedo
{
    enum CommandType
    {
        Padding = 0,
        Create = 1,
        Created = 2,
        Relay = 3,
        Destroy = 4,
        CreateFast = 5,
        CreatedFast = 6,
        NetInfo = 8,
        RelayEarly = 9,
        Create2 = 10,
        Created2 = 11,
        Versions = 7,
        VPadding = 128,
        Certs = 129,
        AuthChallenge = 130,
        Authenticate = 131
    }
}