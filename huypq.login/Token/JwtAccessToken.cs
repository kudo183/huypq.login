using System;

namespace huypq.login.Token
{
    public class JwtAccessToken
    {
        static long duration = 172800;//3600*24*2
        static string payload = "{{\"iss\":\"{0}\",\"sub\":\"{1}\",\"aud\":\"{2}\",\"email\":\"{3}\",\"scope\":\"{4}\",\"iat\":{5},\"exp\":{6}}}";

        public string Iss { get; set; }
        public string Sub { get; set; }
        public string Aud { get; set; }
        public long Iat { get; set; }
        public long Exp { get; set; }
        public string Email { get; set; }
        public string Scope { get; set; }

        public JwtAccessToken()
        {
            Iss = "https://sigin.xyz";
            Iat = (DateTime.UtcNow.Ticks - JwtTokenHelper.TicksFromEpoch) / TimeSpan.TicksPerSecond;
            Exp = Iat + duration;
        }

        public bool FromJwtString(string jwtString)
        {
            var jsonString = JwtTokenHelper.Verify(jwtString);
            if (jsonString == null)
            {
                return false;
            }

            int parameterIndex = 0;
            for (int i = 0; i < jsonString.Length; i++)
            {
                var c = jsonString[i];
                if (c == ':')
                {
                    switch (parameterIndex)
                    {
                        case 0://Iss
                            {
                                i = i + 2;
                                for (int j = i; j < jsonString.Length; j++)
                                {
                                    if (jsonString[j] == '"')
                                    {
                                        Iss = jsonString.Substring(i, j - i);
                                        parameterIndex++;
                                        i = j;
                                        break;
                                    }
                                }
                            }
                            break;
                        case 1://Sub
                            {
                                i = i + 2;
                                for (int j = i; j < jsonString.Length; j++)
                                {
                                    if (jsonString[j] == '"')
                                    {
                                        Aud = jsonString.Substring(i, j - i);
                                        parameterIndex++;
                                        i = j;
                                        break;
                                    }
                                }
                            }
                            break;
                        case 2://Aud
                            {
                                i = i + 2;
                                for (int j = i; j < jsonString.Length; j++)
                                {
                                    if (jsonString[j] == '"')
                                    {
                                        Aud = jsonString.Substring(i, j - i);
                                        parameterIndex++;
                                        i = j;
                                        break;
                                    }
                                }
                            }
                            break;
                        case 3://Email
                            {
                                i = i + 2;
                                for (int j = i; j < jsonString.Length; j++)
                                {
                                    if (jsonString[j] == '"')
                                    {
                                        Email = jsonString.Substring(i, j - i);
                                        parameterIndex++;
                                        i = j;
                                        break;
                                    }
                                }
                            }
                            break;
                        case 4://Scope
                            {
                                i = i + 2;
                                for (int j = i; j < jsonString.Length; j++)
                                {
                                    if (jsonString[j] == '"')
                                    {
                                        Scope = jsonString.Substring(i, j - i);
                                        parameterIndex++;
                                        i = j;
                                        break;
                                    }
                                }
                            }
                            break;
                        case 5://Iat
                            {
                                i = i + 1;
                                for (int j = i; j < jsonString.Length; j++)
                                {
                                    if (jsonString[j] == ',')
                                    {
                                        Iat = long.Parse(jsonString.Substring(i, j - i));
                                        parameterIndex++;
                                        i = j;
                                        break;
                                    }
                                }
                            }
                            break;
                        case 6://Exp
                            {
                                i = i + 1;
                                for (int j = i; j < jsonString.Length; j++)
                                {
                                    if (jsonString[j] == ',')
                                    {
                                        Exp = long.Parse(jsonString.Substring(i, j - i));
                                        parameterIndex++;
                                        i = j;
                                        break;
                                    }
                                }
                            }
                            break;
                    }
                }
            }

            return true;
        }

        public string ToJwtString()
        {
            return JwtTokenHelper.Create(string.Format(payload, Iss, Sub, Aud, Email, Scope, Iat, Exp));
        }

        public bool IsRevoked(long tokenValidTimeInTick)
        {
            var iatInTick = (Iat * TimeSpan.TicksPerSecond) + JwtTokenHelper.TicksFromEpoch;
            return iatInTick < tokenValidTimeInTick;
        }

        public bool IsExpired()
        {
            return Exp < (DateTime.UtcNow.Ticks - JwtTokenHelper.TicksFromEpoch) / TimeSpan.TicksPerSecond;
        }
    }
}
