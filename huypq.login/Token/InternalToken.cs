using huypq.Crypto;
using Microsoft.AspNetCore.DataProtection;
using System;

namespace huypq.login.Token
{
    public class InternalToken
    {
        public static IServiceProvider ServiceProvider;

        public string Purpose { get; set; }
        public string Email { get; set; }
        public byte[] CustomData { get; set; }
        public DateTime CreateTime { get; set; }
        public DateTime ExpireTime { get; set; }

        public InternalToken()
        {
            Email = string.Empty;
            CustomData = new byte[0];
        }

        public bool IsExpired()
        {
            return ExpireTime < DateTime.UtcNow;
        }

        public bool IsRevoked(long tokenValidTimeInTick)
        {
            return CreateTime.Ticks < tokenValidTimeInTick;
        }

        public string ToTokenString()
        {
            var protector = GetProtector(Purpose);
            return Base64UrlEncoder.Encode(protector.Protect(ToByteArray()));
        }

        public static InternalToken VerifyTokenString(string token, string purpose)
        {
            try
            {
                var protector = GetProtector(purpose);

                return FromByteArray(protector.Unprotect(Base64UrlEncoder.DecodeBytes(token)));
            }
            catch
            {
                return null;
            }
        }

        private byte[] ToByteArray()
        {
            using (var ms = new System.IO.MemoryStream())
            using (var bw = new System.IO.BinaryWriter(ms))
            {
                bw.Write(CreateTime.Ticks);
                bw.Write(ExpireTime.Ticks);
                bw.Write(Email);
                bw.Write(CustomData.Length);
                bw.Write(CustomData);
                bw.Flush();
                return ms.ToArray();
            }
        }

        private static InternalToken FromByteArray(byte[] bytes)
        {
            var result = new InternalToken();

            using (var ms = new System.IO.MemoryStream(bytes))
            using (var br = new System.IO.BinaryReader(ms))
            {
                result.CreateTime = new DateTime(br.ReadInt64());
                result.ExpireTime = new DateTime(br.ReadInt64());
                result.Email = br.ReadString();
                result.CustomData = br.ReadBytes(br.ReadInt32());
                return result;
            }
        }
        
        private static IDataProtector GetProtector(string purpose)
        {
            return ServiceProvider.GetDataProtector(purpose);
        }
    }
}
