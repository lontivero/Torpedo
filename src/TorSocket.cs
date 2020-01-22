using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Torpedo
{
    class TorSocket
    {
        private Logger logger = Logger.GetLogger<TorSocket>();

        private List<int> _protocolVersions = new List<int>();

        public OnionRouter GuardRelay { get; }
        private SslStream _stream;

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

            var cell = new Cell(0, CommandType.Versions, new byte[] { 0, 4 });
            cell.WriteTo(_stream);
        }

        private void RetrieveVersions()
        {
            var cell = Cell.ReadFrom(_stream, 0);

            using(var reader = new BEBinaryReader(new MemoryStream(cell.Payload)))
            {
                _protocolVersions.Add(reader.ReadInt16());
            }

            logger.Debug($"...Received versions [{string.Join(", ", _protocolVersions)}]" );
        }

        private void RetrieveCerts()
        {
            Cell.ReadFrom(_stream, 0);
            logger.Debug("Retrieving AUTH_CHALLENGE cell...");
            Cell.ReadFrom(_stream, 0);
        }

        private void RetrieveNetInfo()
        {
            Cell.ReadFrom(_stream, 0);
        }

        
        // If version 2 or higher is negotiated, each party sends the other a NETINFO cell.
        // The cell's payload is:
        // 
        // - Timestamp              [4 bytes]
        // - Other OR's address     [variable]
        // - Number of addresses    [1 byte]
        // - This OR's addresses    [variable]
        // 
        // Address format:
        // 
        // - Type   (1 octet)
        // - Length (1 octet)
        // - Value  (variable-width)
        // "Length" is the length of the Value field.
        // "Type" is one of:
        // - 0x00 -- Hostname
        // - 0x04 -- IPv4 address
        // - 0x06 -- IPv6 address
        // - 0xF0 -- Error, transient
        // - 0xF1 -- Error, nontransient

        private void SendNetInfo()
        {
            var ip = this.GuardRelay.TorEndPoint.Address;
            using (var mem = new MemoryStream())
            using (var writer = new BEBinaryWriter(mem))
            {
                writer.Write(time );
                /* Address */
                writer.Write(ip);

                writer.Write(0x01b);
                writer.Write(IPAddress.None);

                var cell = new Cell(0, CommandType.NetInfo, mem.ToArray());
                cell.WriteTo(_stream);
            }
        }
    }
}
