using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;

namespace Torpedo;

class TorSocket
{
    private readonly Logger logger = Logger.GetLogger<TorSocket>();

    private readonly List<int> _protocolVersions = new List<int>();

    public int ProtocolVersion => _protocolVersions.Any() ?  _protocolVersions.Max() : 0;
    private SslStream _stream;

    public OnionRouter GuardRelay { get; }
    public IPAddress MyIPAddress { get; private set; }

    public TorSocket(OnionRouter guardRelay)
    {
        GuardRelay = guardRelay;
    }

    public void Connect()
    {
        logger.Debug($"Connecting guard relay {GuardRelay.TorEndPoint}");
        var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
        socket.Connect(GuardRelay.TorEndPoint);
        var networkStream = new NetworkStream(socket, false);

        _stream = new SslStream(networkStream, true,
            new RemoteCertificateValidationCallback(ValidateServerCertificate), null);
        _stream.AuthenticateAsClient(GuardRelay.TorEndPoint.Address.ToString());

        Handshake();
    }

    private void Handshake()
    {
        logger.Debug($"Handshaking....");
        SendVersions();
        RetrieveVersions();

        RetrieveCerts();
        RetrieveNetInfo();
        SendNetInfo();
    }

    private static bool ValidateServerCertificate(object sender, 
        X509Certificate certificate,
        X509Chain chain, SslPolicyErrors sslPolicyErrors)
    {
        return true;
    }

    private void SendVersions()
    {
        logger.Debug($"...Sending version +4");
        SendCell(new VersionsCell(0, 4));
    }

    private void RetrieveVersions()
    {
        var cell =  RetrieveCell<VersionsCell>();
        foreach(int ver in cell.Versions)
        {
            _protocolVersions.Add(ver);
        }

        logger.Debug($"...Received versions [{string.Join(", ", _protocolVersions)}]" );
    }

    private void RetrieveCerts()
    {
        logger.Debug("Retrieving CERTS cell...");
        Cell.ReadFrom(_stream, ProtocolVersion);
        logger.Debug("Retrieving AUTH_CHALLENGE cell...");
        Cell.ReadFrom(_stream, ProtocolVersion);
    }

    private void RetrieveNetInfo()
    {
        logger.Debug("Retrieving NET_INFO cell...");
        var netInfo = RetrieveCell<NetInfoCell>();
        MyIPAddress = netInfo.MyIPAddress;

        logger.Debug($"...Received Timestamp {netInfo.Timestamp} {netInfo.MyIPAddress}");
    }

    private void SendNetInfo()
    {
        var netInfo = new NetInfoCell(0);
        netInfo.Timestamp = DateTimeOffset.UtcNow;
        netInfo.MyIPAddress = this.GuardRelay.TorEndPoint.Address;
        netInfo.OtherIPs.Add(MyIPAddress);
        SendCell(netInfo);
        logger.Debug($"...Sending Timestamp {netInfo.Timestamp} {netInfo.MyIPAddress}");
    }

    internal void SendCell(Cell cell)
    {
        cell.WriteTo(_stream, ProtocolVersion);
    }

    internal T RetrieveCell<T>() where T: Cell
    {
        return Cell.ReadFrom(_stream, ProtocolVersion) as T;
    }
}