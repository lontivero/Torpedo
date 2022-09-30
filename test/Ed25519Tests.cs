using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Torpedo;
using Xunit;

namespace test;

public class Ed25519Tests
{
    [Fact]
    public void Test1()
    {
        var vectors = File.ReadAllLines("data/ed25519-vectors.txt");
        var testData = new Dictionary<string, byte[]>();
        var curTest = string.Empty;
            
        foreach(var line in vectors)
        {
            if (string.IsNullOrWhiteSpace(line) 
                || line.StartsWith("#"))
                continue;

            if (line.StartsWith("TST"))
            {
                curTest = line;
                continue;
            }

            var keyValuePair = line.Split(':');
            var (key, value) = (keyValuePair[0].Trim(), StringConverter.ToByteArray(keyValuePair[1].Trim()));
            testData[key] = value;

            if(key == "SIG")
            {
                var (sk, pk, msg) = (testData["SK"], Ed25519Point.DecodePoint(testData["PK"]), testData["MSG"]);
                var signature = Ed25519.Signature(msg, sk, pk);
                Assert.Equal(testData["SIG"], signature);
            }
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