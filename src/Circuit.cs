using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace Torpedo
{
    class Circuit
    {
        private Logger logger = Logger.GetLogger<Circuit>();

        public uint Id { get; }
        public List<OnionRouter> OnionRouters { get; } = new List<OnionRouter>();
        public TorSocket TorSocket { get; }

        public Circuit(TorSocket torSocket)
        {
            var nextId = (uint)new Random().Next(); 
            Id = nextId | 0x80000000u;
            TorSocket = torSocket;
        }
        
        public void Create(OnionRouter guardRelay)
        {
            logger.Debug("Creating new circuit...");
            var keyAgreement = new NTorKeyAgreement(guardRelay);
            TorSocket.SendCell(new Create2Cell(Id, HandshakeType.NTor, keyAgreement.Handshake) );

            var cell = TorSocket.RetrieveCell<Created2Cell>();

            keyAgreement.CompleteHandshake(cell.Y, cell.Auth);

            OnionRouters.Add(guardRelay);
        }
    }
}