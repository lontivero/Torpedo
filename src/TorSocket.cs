using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;

namespace Torpedo
{
    class TorSocket
    {
        private Logger logger = Logger.GetLogger<TorSocket>();

        private List<int> _protocolVersions = new List<int>();

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
            logger.Debug($"Connectiong guard relay {GuardRelay.TorEndPoint}");
            var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(GuardRelay.TorEndPoint);
            var networkStream = new NetworkStream(socket, false);

            _stream = new SslStream(networkStream, true,
                new RemoteCertificateValidationCallback(ValidateServerCertificate), null);
            _stream.AuthenticateAsClient(GuardRelay.TorEndPoint.Address.ToString());

            Handshake();
        }

        public void Handshake()
        {
            logger.Debug($"Handshaking....");
            SendVersions();
            RetrieveVersions();

            RetrieveCerts();
            RetrieveNetInfo();
            SendNetInfo();
        }

        public static bool ValidateServerCertificate(object sender, 
            X509Certificate certificate,
            X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }

        internal void SendVersions()
        {
            logger.Debug($"...Sending version +4");

            var cell = new VersionsCell(0, 4);
            cell.WriteTo(_stream, ProtocolVersion);
        }

        private void RetrieveVersions()
        {
            var cell =  (VersionsCell)Cell.ReadFrom(_stream, ProtocolVersion);
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
            var netInfo = (NetInfoCell)Cell.ReadFrom(_stream, ProtocolVersion);
            MyIPAddress = netInfo.MyIPAddress;

            logger.Debug($"...Received Timestamp {netInfo.Timestamp} {netInfo.MyIPAddress}");
        }

        private void SendNetInfo()
        {
            var netInfo = new NetInfoCell(0);
            netInfo.Timestamp = DateTimeOffset.UtcNow;
            netInfo.MyIPAddress = this.GuardRelay.TorEndPoint.Address;
            netInfo.OtherIPs.Add(MyIPAddress);
            netInfo.WriteTo(_stream, ProtocolVersion);
            logger.Debug($"...Sending Timestamp {netInfo.Timestamp} {netInfo.MyIPAddress}");
        }
    }
}
