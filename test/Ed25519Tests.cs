using System.Collections.Generic;
using System.IO;
using Torpedo;
using Xunit;

namespace test
{
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
                    var signature = Ed25519.Signature(testData["MSG"], testData["SK"], Ed25519Point.DecodePoint(testData["PK"]));
                    Assert.Equal(testData["SIG"], signature);
                }
            }
        }
    }
}