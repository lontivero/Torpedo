using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace Torpedo
{
    class NTorKeyAgreement
    {
        private readonly static byte[] PROTOCOL_ID = Encoding.ASCII.GetBytes("ntor-curve25519-sha256-1");
        private readonly static byte[] t_mac = Encoding.ASCII.GetBytes("ntor-curve25519-sha256-1:mac");
        private readonly static byte[] t_key = Encoding.ASCII.GetBytes("ntor-curve25519-sha256-1:key_extract");
        private readonly static byte[] t_verify = Encoding.ASCII.GetBytes("ntor-curve25519-sha256-1:verify");
        private readonly static byte[] m_expand = Encoding.ASCII.GetBytes("ntor-curve25519-sha256-1:key_expand");

        private Logger logger = Logger.GetLogger<NTorKeyAgreement>();
        public OnionRouter OnionRouter { get; }
        public byte[] Key { get; }
        public Ed25519Point PubKey { get; }
        public Ed25519Point B { get; }
        public byte[] Handshake { get; }

        public NTorKeyAgreement(OnionRouter onionRouter)
        {
            OnionRouter = onionRouter;
            var buffer = new byte[32];
            RNGCryptoServiceProvider.Create().GetBytes(buffer);
            Key = buffer;
            PubKey = Ed25519.PublicKey(Key); 
            B = Ed25519Point.DecodePoint(Convert.FromBase64String(onionRouter.NTorKey));

            var identity = StringConverter.ToByteArray(onionRouter.Fingerprint);
            Handshake = ByteArrayHelpers.Combine(identity, B.EncodePoint(), PubKey.EncodePoint());
        }

        public void CompleteHandshake(Ed25519Point Y, byte[] auth)
        {
            // :type Y: bytes
            // :type auth: bytes
            // 
            // The server's handshake reply is:
            //     SERVER_PK   Y                       [G_LENGTH bytes]
            //     AUTH        H(auth_input, t_mac)    [H_LENGTH bytes]
            // 
            // Updates the onion router's shared secret with the computed key.
            // 
            // # The client then checks Y is in G^* [see NOTE below], and computes
            // # secret_input = EXP(Y,x) | EXP(B,x) | ID | B | X | Y | PROTOID

            var x = Ed25519.DecodeInt(Key);
            var p1 = Y * x;
            var p2 = B * x;
            var identity = StringConverter.ToByteArray(OnionRouter.Fingerprint);
            var secretInput = ByteArrayHelpers.Combine(p1.EncodePoint(), p2.EncodePoint(), identity, B.EncodePoint(), PubKey.EncodePoint(), Y.EncodePoint(), PROTOCOL_ID);

            // KEY_SEED = H(secret_input, t_key) -- Not used.
            // verify = H(secret_input, t_verify)
            var verify = new HMACSHA256(t_verify).ComputeHash(secretInput);

            // auth_input = verify | ID | B | Y | X | PROTOID | "Server"
            var authInput = ByteArrayHelpers.Combine(verify, identity, B.EncodePoint(), Y.EncodePoint(), PubKey.EncodePoint(), PROTOCOL_ID, Encoding.ASCII.GetBytes("Server"));
            var verify2 = new HMACSHA256(t_mac).ComputeHash(authInput);

            if( !auth.SequenceEqual(verify2) )
            {
                logger.Error("Server handshake doesn't match verification.");
                throw new  Exception("Server handshake doesn't match verificaiton.");
            }
        }
    }
}
/*
    def complete_handshake(self, Y, auth):
        """
        :type Y: bytes
        :type auth: bytes

        The server's handshake reply is:
            SERVER_PK   Y                       [G_LENGTH bytes]
            AUTH        H(auth_input, t_mac)    [H_LENGTH bytes]

        Updates the onion router's shared secret with the computed key.
        """
        # The client then checks Y is in G^* [see NOTE below], and computes
        # secret_input = EXP(Y,x) | EXP(B,x) | ID | B | X | Y | PROTOID
        secret_input = self._ed25519.smult_curve25519(self._x, Y)
        secret_input += self._ed25519.smult_curve25519(self._x, self._B)
        secret_input += b16decode(self._onion_router.identity.encode())
        secret_input += self._B
        secret_input += self._X
        secret_input += Y
        secret_input += b'ntor-curve25519-sha256-1'

        # KEY_SEED = H(secret_input, t_key) -- Not used.
        # verify = H(secret_input, t_verify)
        verify = self._hmac_sha256(KeyAgreementNTOR.t_verify, secret_input)

        # auth_input = verify | ID | B | Y | X | PROTOID | "Server"
        auth_input = verify
        auth_input += b16decode(self._onion_router.identity.encode())
        auth_input += self._B
        auth_input += Y
        auth_input += self._X
        auth_input += KeyAgreementNTOR.PROTOCOL_ID
        auth_input += b'Server'

        # The client verifies that AUTH == H(auth_input, t_mac).
        if auth != self._hmac_sha256(KeyAgreementNTOR.t_mac, auth_input):
            log.error("Server handshake doesn't match verification")
            raise Exception("Server handshake doesn't match verificaiton.")

        self._onion_router.set_shared_secret(self._kdf_rfc5869(secret_input, 72))
        log.debug("Handshake verified, onion router's shared secret has been set.")


class KeyAgreementNTOR:
    """Handles performing NTOR handshakes."""

    PROTOCOL_ID = b'ntor-curve25519-sha256-1'
    t_mac = PROTOCOL_ID + b':mac'
    t_key = PROTOCOL_ID + b':key_extract'
    t_verify = PROTOCOL_ID + b':verify'
    m_expand = PROTOCOL_ID + b':key_expand'

    def __init__(self, onion_router):
        """:type onion_router: OnionRouter"""
        self._onion_router = onion_router

        # To perform the handshake, the client needs to know an identity key
        # digest for the server, and an NTOR onion key (a curve25519 public
        # key) for that server. Call the NTOR onion key "B".  The client
        # generates a temporary key-pair:
        #     x,X = KEYGEN()
        self._ed25519 = Ed25519()
        self._x = self._ed25519.create_secret_key()
        self._X = self._ed25519.get_public_key(self._x)

        self._B = b64decode(self._onion_router.key_ntor.encode())

        # and generates a client-side handshake with contents:
        #     NODE_ID     Server identity digest  [ID_LENGTH bytes]
        #     KEYID       KEYID(B)                [H_LENGTH bytes]
        #     CLIENT_PK   X                       [G_LENGTH bytes]
        self._handshake = b16decode(self._onion_router.identity.encode())
        self._handshake += self._B
        self._handshake += self._X

    def get_onion_skin(self):
        """:rtype: bytes"""
        return self._handshake

    @staticmethod
    def _hmac_sha256(key, msg):
        h = hmac.HMAC(key, digestmod=hashlib.sha256)
        h.update(msg)
        return h.digest()

    def _kdf_rfc5869(self, key, n):
        """
        In RFC5869's vocabulary, this is HKDF-SHA256 with info == m_expand,
        salt == t_key, and IKM == secret_input.

        See tor-spec.txt 5.2.2. "KDF-RFC5869"

        :type key: bytes
        :type n: int
        :return: The shared key.
        """
        prk = self._hmac_sha256(KeyAgreementNTOR.t_key, key)
        out = b""
        last = b""
        i = 1

        while len(out) < n:
            m = last + KeyAgreementNTOR.m_expand + int2byte(i)
            last = h = self._hmac_sha256(prk, m)
            out += h
            i = i + 1

        return out[:n]

*/