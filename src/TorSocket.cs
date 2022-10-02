using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;

namespace Torpedo;

class TorSocket
{
    private readonly Logger _logger = Logger.GetLogger<TorSocket>();

    private readonly List<int> _protocolVersions = new ();

    public int ProtocolVersion => _protocolVersions.Any() ?  _protocolVersions.Max() : 0;
    private SslStream _stream;
    private TorSreamReader _reader;
    private TorStreamWriter _writer;

    public OnionRouter GuardRelay { get; }
    public IPAddress MyIPAddress { get; private set; }

    public TorSocket(OnionRouter guardRelay)
    {
        GuardRelay = guardRelay;
    }

    public void Connect()
    {
        _logger.Debug($"Connecting guard relay {GuardRelay.TorEndPoint}");
        var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
        socket.Connect(GuardRelay.TorEndPoint);
        var networkStream = new NetworkStream(socket, false);

        _stream = new SslStream(networkStream, true,
            new RemoteCertificateValidationCallback(ValidateServerCertificate), null);
        _stream.AuthenticateAsClient(GuardRelay.TorEndPoint.Address.ToString());

        _reader = new TorSreamReader(_stream, 0);
        _writer = new TorStreamWriter(_stream, 0);
        Handshake();
    }

    private void Handshake()
    {
        _logger.Debug($"Handshaking....");
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
        _logger.Debug($"...Sending version +4");
        SendCell(new VersionsCell(0, 4));
    }

    private void RetrieveVersions()
    {
        var cell =  RetrieveCell<VersionsCell>();
        foreach(int ver in cell.Versions)
        {
            _protocolVersions.Add(ver);
        }

        _logger.Debug($"...Received versions [{string.Join(", ", _protocolVersions)}]" );
    }

    private void RetrieveCerts()
    {
        _logger.Debug("Retrieving CERTS cell...");
        RetrieveCell<CertsCell>();
        _logger.Debug("Retrieving AUTH_CHALLENGE cell...");
        RetrieveCell<AuthChallengeCell>();
    }

    private void RetrieveNetInfo()
    {
        _logger.Debug("Retrieving NET_INFO cell...");
        var netInfo = RetrieveCell<NetInfoCell>();
        MyIPAddress = netInfo.MyIPAddress;

        _logger.Debug($"...Received Timestamp {netInfo.Timestamp} {netInfo.MyIPAddress}");
    }

    private void SendNetInfo()
    {
        var netInfo = new NetInfoCell(
            CircuitId: 0, 
            Timestamp: DateTimeOffset.UtcNow,
            MyIPAddress: GuardRelay.TorEndPoint.Address,
            OtherIPs: ImmutableList.Create(MyIPAddress));
        SendCell(netInfo);
        _logger.Debug($"...Sending Timestamp {netInfo.Timestamp} {netInfo.MyIPAddress}");
    }

    internal void SendCell(ICell cell)
    {
        _writer.Write(cell);
    }

    internal T RetrieveCell<T>() where T: class, ICell
    {
        return _reader.ReadCell() as T;
    }
}