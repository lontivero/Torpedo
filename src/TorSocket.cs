using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Torpedo
{
    class TorSocket
    {
        private byte[] _protocolVersion;
        public OnionRouter GuardRelay { get; }
        private SslStream _stream;

        public TorSocket(OnionRouter guardRelay)
        {
            GuardRelay = guardRelay;
        }

        public void Connect()
        {
            var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(GuardRelay.TorEndPoint);
            var networkStream = new NetworkStream(socket, false);

            _stream = new SslStream(networkStream, true,
                new RemoteCertificateValidationCallback(ValidateServerCertificate), null);
            _stream.AuthenticateAsClient(GuardRelay.TorEndPoint.Address.ToString());
                // This is where you read and send data

            SendVersions();
            RetrieveVersions();
        }

        public static bool ValidateServerCertificate(object sender, 
            X509Certificate certificate,
            X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }

        internal void SendVersions()
        {
            var cell = new Cell(0, CommandType.Versions, Packer.Pack("^3S", 3, 4, 5));
            cell.WriteTo(_stream);
        }

        private void RetrieveVersions()
        {
            var cell = new Cell();
            cell.ReadFrom(_stream, 0);

            _protocolVersion = cell.Payload;
            
        }

#if false


        private void RetrieveCerts()
        {
            self.retrieve_cell(ignore_response=True)

            log.debug("Retrieving AUTH_CHALLENGE cell...")
            self.retrieve_cell(ignore_response=True)
        }

        private void RetrieveNetInfo()
        {
            self.retrieve_cell(ignore_response=True)
        }

        private void SendNetInfo()
        {
            self._socket.write(Cell(0, CommandType.NETINFO, [
                time(),
                0x04, 0x04, self._guard_relay.ip,
                0x01,
                0x04, 0x04, 0
            ]).get_bytes())        
        }
#endif
    }
}
