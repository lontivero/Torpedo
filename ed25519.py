class Ed25519:
    """
    Python implementation of Ed25519, used by the NTOR handshake.
    "Ed25519 is both a signature scheme and a use case for Edwards-form Curve25519."

    References:
        - https://ed25519.cr.yp.to/python/ed25519.py
        - https://github.com/itdaniher/slownacl/blob/master/curve25519.py
        - https://gitweb.torproject.org/tor.git/tree/src/test/ed25519_exts_ref.py
        - https://monero.stackexchange.com/questions/9820/recursionerror-in-ed25519-py
        - https://crypto.stackexchange.com/questions/47147/ed25519-is-a-signature-or-just-elliptic-curve
        - https://github.com/Marten4n6/TinyTor/pull/4
    """

    def __init__(self):
        self._P = 2 ** 255 - 19
        self._A = 486662

        self._b = 256
        self._q = 2 ** 255 - 19
        self._l = 2 ** 252 + 27742317777372353535851937790883648493

        self._d = -121665 * self._inv(121666)
        self._I = self._exp_mod(2, (self._q - 1) // 4, self._q)

        self._By = 4 * self._inv(5)
        self._Bx = self._x_recover(self._By)
        self._B = [self._Bx % self._q, self._By % self._q]

    def _exp_mod(self, b, e, m):
        if e == 0:
            return 1
        t = self._exp_mod(b, e // 2, m) ** 2 % m
        if e & 1:
            t = (t * b) % m
        return t

    def _inv(self, x):
        return self._exp_mod(x, self._P - 2, self._P)

    def _x_recover(self, y):
        xx = (y * y - 1) * self._inv(self._d * y * y + 1)
        x = self._exp_mod(xx, (self._q + 3) // 8, self._q)
        if (x * x - xx) % self._q != 0:
            x = (x * self._I) % self._q
        if x % 2 != 0:
            x = self._q - x
        return x

    def _edwards(self, P, Q):
        x1 = P[0]
        y1 = P[1]
        x2 = Q[0]
        y2 = Q[1]
        x3 = (x1 * y2 + x2 * y1) * self._inv(1 + self._d * x1 * x2 * y1 * y2)
        y3 = (y1 * y2 + x1 * x2) * self._inv(1 - self._d * x1 * x2 * y1 * y2)
        return [x3 % self._q, y3 % self._q]

    def _scalar_mult(self, P, e):
        if e == 0:
            return [0, 1]
        Q = self._scalar_mult(P, e // 2)
        Q = self._edwards(Q, Q)
        if e & 1:
            Q = self._edwards(Q, P)
        return Q

    def get_public_key(self, sk):
        sk = self.clamp(self.unpack(sk))
        return self.pack(self.exp(sk, 9))

    @staticmethod
    def create_secret_key():
        return urandom(32)

    def add(self, n, m, d):
        (xn, zn), (xm, zm), (xd, zd) = n, m, d
        x = 4 * (xm * xn - zm * zn) ** 2 * zd
        z = 4 * (xm * zn - zm * xn) ** 2 * xd
        return x % self._P, z % self._P

    def double(self, n):
        (xn, zn) = n
        x = (xn ** 2 - zn ** 2) ** 2
        z = 4 * xn * zn * (xn ** 2 + self._A * xn * zn + zn ** 2)
        return x % self._P, z % self._P

    def exp(self, n, base):
        one = (base, 1)
        two = self.double(one)

        def f(m):
            if m == 1:
                return one, two
            (pm, pm1) = f(m // 2)
            if m & 1:
                return self.add(pm, pm1, one), self.double(pm1)
            return self.double(pm), self.add(pm, pm1, one)

        ((x, z), _) = f(n)
        return (x * self._inv(z)) % self._P

    def b2i(self, c):
        return c

    def i2b(self, i):
        return i

    def ba2bs(self, ba):
        return bytes(ba)

    @staticmethod
    def clamp(n):
        n &= ~7
        n &= ~(128 << 8 * 31)
        n |= 64 << 8 * 31
        return n

    def unpack(self, s):
        if len(s) != 32:
            raise ValueError("Invalid Curve25519 argument.")
        return sum(self.b2i(s[i]) << (8 * i) for i in range(32))

    def pack(self, n):
        return self.ba2bs([self.i2b((n >> (8 * i)) & 255) for i in range(32)])

    def smult_curve25519(self, n, p):
        n = self.clamp(self.unpack(n))
        p = self.unpack(p)
        return self.pack(self.exp(n, p))