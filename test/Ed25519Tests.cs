using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Torpedo;
using Xunit;

namespace test;

public static class LinqExtensions
{
    public static IEnumerable<IEnumerable<T>> SplitBy<T>(this IEnumerable<T> source, Func<T, bool> when)
    {
        int grouper = 0;
        return source
            .GroupBy(x => grouper += when(x) ? 1 : 0)
            .Select(x => x.AsEnumerable().Where(x => !when(x)))
            .Where(x => x.Any());
    }
}

public class Ed25519Tests
{
    [Fact]
    public void Test1()
    {
        var vectors = File
            .ReadAllLines("data/ed25519-vectors.txt")
            .Where(line => !line.StartsWith("#"))
            .SplitBy(line => string.IsNullOrWhiteSpace(line))
            .Select(tst => tst
                .Select(line => line.Split(':'))
                .Select(parts => (parts[0].Trim(), parts[1].Trim()))
                .ToDictionary(x => x.Item1, x => x.Item2)
            )
            .Select(x => new
            {
                Name = x["TST"],
                SecretKey = StringConverter.ToByteArray(x["SK"]),
                PublicKey = Ed25519Point.DecodePoint(StringConverter.ToByteArray(x["PK"])),
                Message = StringConverter.ToByteArray(x["MSG"]),
                Signature = StringConverter.ToByteArray(x["SIG"])
            });
            
        foreach (var vector in vectors)
        {
            var signature = Ed25519.Signature(vector.Message, vector.SecretKey, vector.PublicKey);
            Assert.Equal(vector.Signature, signature);
        }
    }


    private byte[] Version = new byte[]{ 3 };

    [Fact]
    public void xxx()
    {
        var pkBytes = StringConverter.ToByteArray("b82b69e96f886f7bc417894b6ece47d606f178b5f872411024e51fb27cb4a961");
        var pub = Ed25519.PublicKey(pkBytes).EncodePoint();

        var checkdigits = GetCheckdigits(pub);
        var all = pub.Concat(checkdigits).Concat(Version).ToArray();
        var serviceId = Base32.ToBase32String(all).ToLower();
        var url = serviceId;

        var y = Base32.FromBase32String("6qoibdde2qea7aruts3rft64pqg2bm6oa5jvgsobm6cn2cggdjphk7qd".ToUpper());
    }

    private byte[] GetCheckdigits(byte[] pubKey)
    {
        var salt = ".onion checksum";
        var x = Encoding.UTF8.GetBytes(salt).Concat(pubKey).Concat(Version).ToArray();
        return  x.Sha256().TakeLast(2).ToArray();
    }
/*
func getCheckdigits(pub ed25519.PublicKey) []byte {
	// Calculate checksum sha3(".onion checksum" || publicKey || version)
	checkstr := []byte(salt)
	checkstr = append(checkstr, pub...)
	checkstr = append(checkstr, version)
	checksum := sha3.Sum256(checkstr)
	return checksum[:2]
}
*/        
}