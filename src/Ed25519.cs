using System;
using System.Linq;
using System.Numerics;

namespace Torpedo;

public record Ed25519Point(BigInteger X, BigInteger Y)
{
    private static readonly Ed25519Point Zero = new (BigInteger.Zero, BigInteger.One);

    public byte[] EncodePoint()
    {
        var nout = Y.Encode();
        nout[^1] |= (X.IsEven ? (byte)0 : (byte)0x80);
        return nout;
    }

    public static Ed25519Point DecodePoint(byte[] pointBytes)
    {
        var y = new BigInteger(pointBytes) & Ed25519.Un;
        var x = RecoverX(y);
        if ((x.IsEven ? 0 : 1) != pointBytes.GetBit(256 - 1))
        {
            x = Ed25519.Q - x;
        }
        var point = new Ed25519Point(x, y);
        // if (!point.IsOnCurve()) throw new ArgumentException("Decoding point that is not on curve");
        return point;
    }

    public static Ed25519Point operator *(Ed25519Point p, BigInteger e)
    {
        if (e.Equals(BigInteger.Zero))
        {
            return Ed25519Point.Zero;
        }
        var q = EdwardsSquare(p * (e / 2));
        return e.IsEven ? q : Edwards(q, p); 
    }

    public static Ed25519Point Edwards(Ed25519Point p, Ed25519Point q)
    {
        var xx = p.X * q.X;
        var yy = p.Y * q.Y;
        var dxxyy = Ed25519.D * xx * yy;
        var x3 = (p.X * q.Y + q.X * p.Y) * Inv(BigInteger.One + dxxyy);
        var y3 = (p.Y * q.Y + xx) * Inv(BigInteger.One - dxxyy);
        return new Ed25519Point(x3.Mod(Ed25519.Q), y3.Mod(Ed25519.Q));
    }

    private static Ed25519Point EdwardsSquare(Ed25519Point p)
    {
        var xx = p.X * p.X;
        var yy = p.Y * p.Y;
        var dxxyy = Ed25519.D * xx * yy;
        var x3 = (2 * p.X * p.Y) * Inv(BigInteger.One + dxxyy);
        var y3 = (yy + xx) * Inv(BigInteger.One - dxxyy);
        return new Ed25519Point(x3.Mod(Ed25519.Q), y3.Mod(Ed25519.Q));
    }

    public static BigInteger Inv(BigInteger x)
    {
        return BigInteger.ModPow(x, Ed25519.Qm2, Ed25519.Q);
    }

    private bool IsOnCurve()
    {
        var xx = X * X;
        var yy = Y * Y;
        var dxxyy = Ed25519.D * yy * xx;
        return (yy - xx - dxxyy - BigInteger.One).Mod(Ed25519.Q).Equals(BigInteger.Zero);
    }

    private static BigInteger RecoverX(BigInteger y)
    {
        var yy = y * y;
        var xx = (yy - BigInteger.One) * Inv(Ed25519.D * yy + BigInteger.One);
        var x = BigInteger.ModPow(xx, Ed25519.Qp3 / 8, Ed25519.Q);

        if (!(x * x - xx).Mod(Ed25519.Q).Equals(BigInteger.Zero))
        {
            x = (x * Ed25519.I).Mod(Ed25519.Q);
        }
        if (!x.IsEven)
        {
            x = Ed25519.Q - x;
        }
        return x;
    }
}

public static class Ed25519
{
    public static Ed25519Point PublicKey(byte[] signingKey)
    {
        var h = signingKey.Sha512();
        var a = TwoPowBitLengthMinusTwo;
        for (int i = 3; i < (256 - 2); i++)
        {
            var bit = h.GetBit(i);
            if (bit == 1)
            {
                a += TwoPowCache[i];
            }
        }
        return B * a;
    }

    private static BigInteger HashInt(byte[] m)
    {
        var h = m.Sha512();
        var hsum = BigInteger.Zero;
        for (int i = 0; i < 2 * 256; i++)
        {
            var bit = h.GetBit(i);
            if (bit == 1)
            {
                hsum += TwoPowCache[i];
            }
        }
        return hsum;
    }

    public static byte[] Signature(byte[] message, byte[] signingKey, Ed25519Point publicKey)
    {
        var h = signingKey.Sha512();
        var a = TwoPowBitLengthMinusTwo;
        for (int i = 3; i < (256 - 2); i++)
        {
            var bit = h.GetBit(i);
            if (bit == 1)
            {
                a += TwoPowCache[i];
            }
        }

        var rsub = ByteArrayHelpers.Combine(h[32..64], message);
        var r = HashInt(rsub);
            
        var encodedBigR = (B * r).EncodePoint();
        var stemp = ByteArrayHelpers.Combine(encodedBigR, publicKey.EncodePoint(), message);
        var s = (r + HashInt(stemp) * a).Mod(L);

        return ByteArrayHelpers.Combine(encodedBigR, s.Encode());
    }


    public static BigInteger DecodeInt(byte[] s)
    {
        return new BigInteger(s) & Un;
    }

    public static bool CheckValid(byte[] signature, byte[] message, Ed25519Point publicKey)
    {
        if (signature.Length != 64) throw new ArgumentException("Signature length is wrong");

        var r = Ed25519Point.DecodePoint(signature[0..32]);
        var s = DecodeInt(signature[32..64]);

        var stemp = ByteArrayHelpers.Combine(r.EncodePoint(), publicKey.EncodePoint(), message);
        var h = HashInt(stemp);
            
        var ra = B * s;
        var ah = publicKey * h;
        var rb = Ed25519Point.Edwards(r, ah);
        if (!ra.X.Equals(rb.X) || !ra.Y.Equals(rb.Y))
            return false;
        return true;
    }

    private static readonly BigInteger TwoPowBitLengthMinusTwo = BigInteger.Pow(2, 256 - 2);
    private static readonly BigInteger[] TwoPowCache = Enumerable.Range(0, 512).Select(i => BigInteger.Pow(2, i)).ToArray();

    public static readonly BigInteger Q =
        BigInteger.Parse("57896044618658097711785492504343953926634992332820282019728792003956564819949");

    public static readonly BigInteger Bx =
        BigInteger.Parse("15112221349535400772501151409588531511454012693041857206046113283949847762202");

    public static readonly BigInteger By =
        BigInteger.Parse("46316835694926478169428394003475163141307993866256225615783033603165251855960");

    public static readonly Ed25519Point B = new Ed25519Point(Bx.Mod(Q), By.Mod(Q));

    public static readonly BigInteger Qm2 =
        BigInteger.Parse("57896044618658097711785492504343953926634992332820282019728792003956564819947");

    public static readonly BigInteger Qp3 =
        BigInteger.Parse("57896044618658097711785492504343953926634992332820282019728792003956564819952");

    public static readonly BigInteger L =
        BigInteger.Parse("7237005577332262213973186563042994240857116359379907606001950938285454250989");

    public static readonly BigInteger D =
        BigInteger.Parse("-4513249062541557337682894930092624173785641285191125241628941591882900924598840740");

    public static readonly BigInteger I =
        BigInteger.Parse("19681161376707505956807079304988542015446066515923890162744021073123829784752");

    public static readonly BigInteger Un =
        BigInteger.Parse("57896044618658097711785492504343953926634992332820282019728792003956564819967");
}