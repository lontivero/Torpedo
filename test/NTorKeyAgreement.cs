using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Torpedo;
using Xunit;

namespace test
{
    public class NTorKeyAgreementTests
    {
        [Fact]
        public void Test1()
        {
            var ntorOnionKey = Convert.FromBase64String("7OA7JENkTlp4FJfe7GEaugR7dqrXoKJ8SxjQPxQlzU4=");
            Ed25519Point.DecodePoint(ntorOnionKey);

            ntorOnionKey = Convert.FromBase64String("1BNsB+LiErrPOp7B6m+UrIOTBQXRhCmPHvGCd+98xGA=");
            Ed25519Point.DecodePoint(ntorOnionKey);
            
            ntorOnionKey = Convert.FromBase64String("LQRS+A7tAXWE5GyUergqWM0VEqpFCSllw7UZsEOMb0g=");
            Ed25519Point.DecodePoint(ntorOnionKey);
        }

        [Fact]
        public void Test2()
        {
            var ntorOnionKey = Convert.FromBase64String("P7rjTiseJKrdRh2YWCR1m1hm8DUsBuUklP5SKT/+mmw=").Reverse().ToArray();
            Ed25519Point.DecodePoint(ntorOnionKey);


            ntorOnionKey = Convert.FromBase64String("goEOCP/GZGOyTWxHA7EYQ519jKFqLck4ooeD+Dn52HA=").Reverse().ToArray();
            Ed25519Point.DecodePoint(ntorOnionKey);

            ntorOnionKey = Convert.FromBase64String("O39eel27DiJyXlZ/DzBuEnggpiHUGawpgb0WMRmoU3I=").Reverse().ToArray();
            Ed25519Point.DecodePoint(ntorOnionKey);

            //ntorOnionKey = Convert.FromBase64String("chviMzOdkiJYNoVMJx+24PB4usS4dvrqX31R+/fXETA=");
            //Ed25519Point.DecodePoint(ntorOnionKey);

            ntorOnionKey = Convert.FromBase64String("1BNsB+LiErrPOp7B6m+UrIOTBQXRhCmPHvGCd+98xGA=");
            Ed25519Point.DecodePoint(ntorOnionKey);
            //d4136c07e2e212bacf3a9ec1ea6f94ac83930505d184298f1ef18277ef7cc460
        }

        private static byte[] ConvertFromBase64String(string input) {
    if (String.IsNullOrWhiteSpace(input)) return null;
    try {
        string working = input.Replace('-', '+').Replace('_', '/'); ;
        while (working.Length % 4 != 0) {
            working += '=';
        }
        return Convert.FromBase64String(working);
    } catch(Exception) {
        return null;
    }
}
    }
}

