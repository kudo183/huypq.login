using System;
using System.Collections.Specialized;
using System.Net;
using System.Threading.Tasks;

namespace huypq.login.Helper
{
    public static class ReCaptchaHelper
    {
        static string secretKey;

        static ReCaptchaHelper()
        {
            secretKey = System.IO.File.ReadAllText(KeyFilePath.ReCaptchaKey);
        }

        public static async Task<bool> Verify(string recaptcha)
        {
            var webClient = new WebClient();

            var response = await webClient.UploadValuesTaskAsync(new Uri("https://www.google.com/recaptcha/api/siteverify"), new NameValueCollection()
            {
                {"secret", secretKey },
                {"response", recaptcha }
            });

            string jsonStringResult = System.Text.Encoding.UTF8.GetString(response);

            int parameterIndex = 0;

            bool success = false;
            string challenge_ts;
            string hostname;
            for (int i = 0; i < jsonStringResult.Length; i++)
            {
                var c = jsonStringResult[i];
                if (c == ':')
                {
                    switch (parameterIndex)
                    {
                        case 0://success
                            {
                                i = i + 2;
                                for (int j = i; j < jsonStringResult.Length; j++)
                                {
                                    if (jsonStringResult[j] == ',')
                                    {
                                        success = bool.Parse(jsonStringResult.Substring(i, j - i));
                                        parameterIndex++;
                                        i = j;
                                        break;
                                    }
                                }
                            }
                            break;
                        case 1://challenge_ts
                            {
                                i = i + 3;
                                for (int j = i; j < jsonStringResult.Length; j++)
                                {
                                    if (jsonStringResult[j] == '"')
                                    {
                                        challenge_ts = jsonStringResult.Substring(i, j - i);
                                        parameterIndex++;
                                        i = j;
                                        break;
                                    }
                                }
                            }
                            break;
                        case 2://hostname
                            {
                                i = i + 3;
                                for (int j = i; j < jsonStringResult.Length; j++)
                                {
                                    if (jsonStringResult[j] == '"')
                                    {
                                        hostname = jsonStringResult.Substring(i, j - i);
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

            return success;
        }
    }
}
