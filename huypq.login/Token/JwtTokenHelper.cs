using huypq.Crypto;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;

namespace huypq.login.Token
{
    public class JwtTokenHelper
    {
        public static long TicksFromEpoch = 621355968000000000;

        static string header = "{\"typ\":\"JWT\",\"alg\":\"RS256\",\"kid\":\"D567C3DA-0FB8-4DBE-AF86-CBF41C837FC1\"}";

        static RSAParameters key;

        static JwtTokenHelper()
        {
            var xml = XDocument.Parse(System.IO.File.ReadAllText(@"c:\keys\rsakey.xml"));
            key = new RSAParameters();
            foreach (var item in xml.Elements())
            {
                key.D = Convert.FromBase64String(item.Element("D").Value);
                key.DP = Convert.FromBase64String(item.Element("DP").Value);
                key.DQ = Convert.FromBase64String(item.Element("DQ").Value);
                key.Exponent = Convert.FromBase64String(item.Element("Exponent").Value);
                key.InverseQ = Convert.FromBase64String(item.Element("InverseQ").Value);
                key.Modulus = Convert.FromBase64String(item.Element("Modulus").Value);
                key.P = Convert.FromBase64String(item.Element("P").Value);
                key.Q = Convert.FromBase64String(item.Element("Q").Value);
            }
        }

        public static string Create(string payload)
        {
            var raw = Base64UrlEncoder.Encode(header) + "." + Base64UrlEncoder.Encode(payload);

            var sign = Rsa.SignSHA256(Encoding.UTF8.GetBytes(raw), key);

            return raw + "." + Base64UrlEncoder.Encode(sign);
        }

        public static string Verify(string token)
        {
            var text = token.Split(new[] { "." }, StringSplitOptions.RemoveEmptyEntries);

            if (text.Length != 3)
            {
                return null;
            }

            var sign = Base64UrlEncoder.DecodeBytes(text[2]);

            if (Rsa.VerifySignSHA256(Encoding.UTF8.GetBytes(text[0] + "." + text[1]), sign, key) == true)
            {
                return Base64UrlEncoder.Decode(text[1]);
            }

            return null;
        }
    }
}
