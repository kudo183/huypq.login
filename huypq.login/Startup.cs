using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using huypq.login.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection;
using huypq.Crypto;
using System.Text;
using System.IO;
using huypq.login.Token;
using static huypq.login.Token.JwtAccessToken;
using System.Net;
using System.Collections.Specialized;
using huypq.login.Helper;

namespace huypq.login
{
    public class Result
    {
        public int StatusCode { get; set; }
        public string ResponseText { get; set; }

        public Result()
        {
            StatusCode = (int)System.Net.HttpStatusCode.OK;
            ResponseText = "OK";
        }

        public void BadRequest(string message)
        {
            StatusCode = (int)System.Net.HttpStatusCode.BadRequest;
            ResponseText = message;
        }
    }

    public class Startup
    {
        static string EmailFolderPath = @"C:\email";

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            if (Directory.Exists(EmailFolderPath) == false)
            {
                Directory.CreateDirectory(EmailFolderPath);
            }

            services.AddCors();

            var connection = string.Format(@"Server=.;Database={0};Trusted_Connection=True;", "SigIn");
            services.AddDbContext<SqlDbContext>(options => options.UseSqlServer(connection), ServiceLifetime.Scoped);
            services.AddDataProtection()
                .PersistKeysToFileSystem(new System.IO.DirectoryInfo(@"c:\Server.key\huypq.login"))
                .ProtectKeysWithDpapi();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseDefaultFiles();
                app.UseStaticFiles();
            }

            app.UseCors(builder => builder.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());

            InternalToken.ServiceProvider = app.ApplicationServices;

            app.Run(async (context) =>
            {
                var parameters = GetRequestParameter(context.Request);
                var dbContext = GetDbContext(context);
                var host = context.Request.Host.Host;

                var result = new Result();
                if (context.Request.Method != "POST" && IsPostOnly(context.Request.Path))
                {
                    result.BadRequest("only accept POST method.");
                }
                else
                {
                    result = await ProcessRequest(context.Request, dbContext, parameters);
                }

                context.Response.StatusCode = result.StatusCode;
                context.Response.ContentType = "text/plain";
                await context.Response.WriteAsync(result.ResponseText);
            });
        }

        private bool IsPostOnly(string path)
        {
            return (path == "/login" || path == "/resetpass" || path == "/token");
        }

        private async Task<Result> ProcessRequest(HttpRequest request, SqlDbContext dbContext, Dictionary<string, string> parameters)
        {
            var result = new Result();
            switch (request.Path)
            {
                case "/login":
                    {
                        result = await Login(dbContext, parameters);
                    }
                    break;
                case "/authorizecode":
                    {
                        result = await AuthorizeCode(dbContext, parameters);
                    }
                    break;
                case "/token":
                    {
                        result = await Token(dbContext, parameters);
                    }
                    break;
                case "/userinfo":
                    {
                        if (request.Headers.ContainsKey("Authorization") == false)
                        {
                            result.BadRequest("Authorization header is required.");
                        }
                        else
                        {
                            result = await UserInfo(dbContext, request.Headers["Authorization"]);
                        }
                    }
                    break;
                case "/logout":
                    {
                        result = await Logout(dbContext, parameters);
                    }
                    break;
                case "/register":
                    {
                        result = await Register(dbContext, parameters);
                    }
                    break;
                case "/confirmemail":
                    {
                        result = await ConfirmEmail(dbContext, parameters);
                    }
                    break;
                case "/requestconfirmemail":
                    {
                        result = await RequestConfirmEmail(dbContext, parameters);
                    }
                    break;
                case "/requestresetpass":
                    {
                        result = await RequestResetPassword(dbContext, parameters);
                    }
                    break;
                case "/resetpass":
                    {
                        result = await ResetPassword(dbContext, parameters);
                    }
                    break;
                case "/verify":
                    {
                        result = await Verify(dbContext, parameters);
                    }
                    break;
                default:
                    result.BadRequest("request path not found.");
                    break;
            }

            return result;
        }

        private async Task<Result> Login(SqlDbContext dbContext, Dictionary<string, string> parameters)
        {
            var result = new Result();

            if (parameters.TryGetValue("email", out string email) == false)
            {
                result.BadRequest("email is required.");
                return result;
            }

            if (parameters.TryGetValue("password", out string password) == false)
            {
                result.BadRequest("pass is required.");
                return result;
            }

            var user = await dbContext.User.FirstOrDefaultAsync(p => p.Email == email);
            if (user == null)
            {
                result.BadRequest("email not exist.");
                return result;
            }

            if (user.IsConfirmed == false)
            {
                result.BadRequest("user not confirmed.");
                return result;
            }

            if (user.IsLocked == true)
            {
                result.BadRequest("user is locked.");
                return result;
            }

            if (PasswordHash.VerifyHashedPassword(user.PasswordHash, password) == false)
            {
                result.BadRequest("wrong pass.");
                return result;
            }

            var now = DateTime.UtcNow;
            result.ResponseText = string.Format("{{\"token\":\"{0}\",\"email\":\"{1}\"}}", new InternalToken()
            {
                Purpose = "login",
                Email = email,
                CreateTime = now,
                ExpireTime = now.AddYears(1)
            }.ToTokenString(), email);

            user.LastLogin = now;
            dbContext.User.Update(user);
            await dbContext.SaveChangesAsync();

            return result;
        }

        private async Task<Result> AuthorizeCode(SqlDbContext dbContext, Dictionary<string, string> parameters)
        {
            var result = new Result();

            if (parameters.TryGetValue("logintoken", out string logintoken) == false)
            {
                result.BadRequest("logintoken is required.");
                return result;
            }

            if (parameters.TryGetValue("client_id", out string client_id) == false)
            {
                result.BadRequest("client_id is required.");
                return result;
            }

            if (parameters.TryGetValue("redirect_uri", out string redirect_uri) == false)
            {
                result.BadRequest("redirect_uri is required.");
                return result;
            }

            if (parameters.TryGetValue("response_type", out string response_type) == false)
            {
                result.BadRequest("response_type is required.");
                return result;
            }

            if (response_type != "code")
            {
                result.BadRequest("response_type Value MUST be set to \"code\".");
                return result;
            }

            //if (parameters.TryGetValue("scope", out string scope) == false)
            //{
            //    result.BadRequest("scope is required.");
            //    return result;
            //}

            parameters.TryGetValue("state", out string state);
            parameters.TryGetValue("nonce", out string nonce);
            parameters.TryGetValue("code_challenge", out string code_challenge);
            parameters.TryGetValue("code_challenge_method", out string code_challenge_method);

            var token = InternalToken.VerifyTokenString(logintoken, "login");

            if (token == null)
            {
                result.BadRequest("Invalid logintoken.");
                return result;
            }

            if (token.IsExpired() == true)
            {
                result.BadRequest("logintoken is expired.");
                return result;
            }

            var user = await dbContext.User.FirstOrDefaultAsync(p => p.Email == token.Email);

            if (user == null)
            {
                result.BadRequest(string.Format("user [{0}] not exist.", token.Email));
                return result;
            }

            if (token.CreateTime.Ticks < user.TokenValidTime)
            {
                result.BadRequest("logintoken is revoked.");
                return result;
            }

            var appID = int.Parse(client_id);
            var app = await dbContext.Application.FirstOrDefaultAsync(p => p.ID == appID);

            if (app == null)
            {
                result.BadRequest(string.Format("client_id [{0}] not exist.", appID));
                return result;
            }

            if (dbContext.RedirectUri.Any(p => p.ApplicationID == appID && p.Uri == redirect_uri) == false)
            {
                result.BadRequest(string.Format("redirect_uri [{0}] not exist.", redirect_uri));
                return result;
            }

            var now = DateTime.UtcNow;
            var key = Guid.NewGuid().ToString("N");

            var customData = new List<string>()
            {
                key, client_id, redirect_uri, nonce??"", code_challenge??""
            };

            var code = new InternalToken()
            {
                Purpose = "authorize_code",
                Email = token.Email,
                CustomData = Helper.ListStringHelper.ToByteArray(customData),
                CreateTime = now,
                ExpireTime = now.AddMinutes(5)
            }.ToTokenString();

            OneTimeCodeManager.AddEntry(key, code);
            result.ResponseText = string.Format("{{\"code\":\"{0}\",\"state\":\"{1}\"}}", code, state);
            return result;
        }

        private async Task<Result> Token(SqlDbContext dbContext, Dictionary<string, string> parameters)
        {
            var result = new Result();

            if (parameters.TryGetValue("grant_type", out string grant_type) == false)
            {
                result.BadRequest("grant_type is required.");
                return result;
            }

            if (grant_type != "authorization_code")
            {
                result.BadRequest("grant_type Value MUST be set to \"authorization_code\".");
                return result;
            }

            if (parameters.TryGetValue("code", out string code) == false)
            {
                result.BadRequest("code is required.");
                return result;
            }

            if (parameters.TryGetValue("redirect_uri", out string redirect_uri) == false)
            {
                result.BadRequest("redirect_uri is required.");
                return result;
            }

            if (parameters.TryGetValue("client_id", out string client_id) == false)
            {
                result.BadRequest("client_id is required.");
                return result;
            }

            var token = InternalToken.VerifyTokenString(code, "authorize_code");
            if (token == null)
            {
                result.BadRequest("Invalid code.");
                return result;
            }

            if (token.IsExpired() == true)
            {
                result.BadRequest("code is expired.");
                return result;
            }

            var customData = Helper.ListStringHelper.FromByteArray(token.CustomData);
            if (client_id != customData[1])
            {
                result.BadRequest("client_id is not match code client_id.");
                return result;
            }
            if (redirect_uri != customData[2])
            {
                result.BadRequest("client_id is not match code redirect_uri.");
                return result;
            }
            var nonce = customData[3];
            var code_challenge = customData[4];

            if (string.IsNullOrEmpty(code_challenge) == false)
            {
                if (parameters.TryGetValue("code_verifier", out string code_verifier) == false)
                {
                    result.BadRequest("code_verifier is required.");
                    return result;
                }

                if (SHA256Utils.ComputeBase64UrlEncodeHash(code_verifier) != code_challenge)
                {
                    result.BadRequest("Invalid code_verifier.");
                    return result;
                }
            }

            var entry = OneTimeCodeManager.FindEntry(customData[0]);

            if (entry == null)
            {
                result.BadRequest("code is already used.");
                return result;
            }

            var user = await dbContext.User.FirstOrDefaultAsync(p => p.Email == token.Email);

            if (token.CreateTime.Ticks < user.TokenValidTime)
            {
                result.BadRequest("code is revoked.");
                return result;
            }

            string accessToken = string.Empty;
            string idToken = new JwtIDToken()
            {
                Sub = user.ID.ToString(),
                Aud = client_id,
                Email = token.Email,
                Nonce = nonce
            }.ToJwtString();

            var appID = int.Parse(client_id);
            var app = await dbContext.Application.FirstOrDefaultAsync(p => p.ID == appID);
            if (app == null)
            {
                accessToken = new JwtAccessToken()
                {
                    Sub = user.ID.ToString(),
                    Aud = client_id,
                    Email = token.Email,
                    Scope = ""
                }.ToJwtString();
            }
            else
            {
                var userScopes = await dbContext.UserScope.Where(p => p.UserID == user.ID).Select(p => p.ScopeID).ToListAsync();
                var scopes = await dbContext.Scope.Where(p => p.ApplicationID == app.ID && userScopes.Contains(p.ID)).ToListAsync();

                var sb = new StringBuilder();
                foreach (var scope in scopes)
                {
                    sb.AppendFormat("{0} ", scope.ScopeName);
                }

                accessToken = new JwtAccessToken()
                {
                    Sub = user.ID.ToString(),
                    Aud = client_id,
                    Email = token.Email,
                    Scope = (sb.Length > 1) ? sb.ToString(0, sb.Length - 1) : ""
                }.ToJwtString();
            }
            result.ResponseText = string.Format("{{\"access_token\":\"{0}\",\"id_token\":\"{1}\",\"token_type\":\"bearer\"}}", accessToken, idToken);
            return result;
        }

        private async Task<Result> UserInfo(SqlDbContext dbContext, string accessTokenString)
        {
            var result = new Result();
            accessTokenString = accessTokenString.Substring("bearer ".Length);
            var accessToken = new JwtAccessToken();
            if (accessToken.FromJwtString(accessTokenString) == false)
            {
                result.BadRequest("Invalid access token.");
                return result;
            }

            result.ResponseText = string.Format("{{\"sub\":\"{0}\",\"name\":\"{1}\",\"email\":\"{2}\"}}", "sub", "name", accessToken.Email);
            return result;
        }

        private async Task<Result> Register(SqlDbContext dbContext, Dictionary<string, string> parameters)
        {
            var result = new Result();
            if (parameters.TryGetValue("email", out string email) == false)
            {
                result.BadRequest("email is required.");
                return result;
            }
            if (parameters.TryGetValue("password", out string password) == false)
            {
                result.BadRequest("password is required.");
                return result;
            }
            if (parameters.TryGetValue("g-recaptcha-response", out string recaptcha) == false)
            {
                result.BadRequest("g-recaptcha-response is required.");
                return result;
            }

            if (await ReCaptchaHelper.Verify(recaptcha) == false)
            {
                result.BadRequest("invalid captcha.");
                return result;
            }

            if (await dbContext.User.AnyAsync(p => p.Email == email) == true)
            {
                result.BadRequest("email is already registered.");
                return result;
            }

            var now = DateTime.UtcNow;
            dbContext.User.Add(new User()
            {
                Email = email,
                PasswordHash = PasswordHash.HashedBase64String(password),
                CreateDate = now,
                IsConfirmed = true,
                IsLocked = false,
                LastLogin = new DateTime(),
                TokenValidTime = now.Ticks
            });

            await dbContext.SaveChangesAsync();

            SendConfirmEmail(email, (now.Ticks % 1000000).ToString());

            return result;
        }

        private async Task<Result> ConfirmEmail(SqlDbContext dbContext, Dictionary<string, string> parameters)
        {
            var result = new Result();

            if (parameters.TryGetValue("token", out string token) == false)
            {
                result.BadRequest("token is required.");
                return result;
            }

            var internalToken = InternalToken.VerifyTokenString(token, "confirmemail");
            if (internalToken == null)
            {
                result.BadRequest("Invalid token.");
                return result;
            }

            if (internalToken.IsExpired() == true)
            {
                result.BadRequest("token is expired.");
                return result;
            }

            var user = await dbContext.User.FirstOrDefaultAsync(p => p.Email == internalToken.Email);
            if (user == null)
            {
                result.BadRequest("email is not registered.");
                return result;
            }

            if (internalToken.IsRevoked(user.TokenValidTime) == true)
            {
                result.BadRequest("token is revoked.");
                return result;
            }

            user.IsConfirmed = true;
            dbContext.User.Update(user);
            await dbContext.SaveChangesAsync();

            return result;
        }

        private async Task<Result> RequestConfirmEmail(SqlDbContext dbContext, Dictionary<string, string> parameters)
        {
            var result = new Result();

            if (parameters.TryGetValue("email", out string email) == false)
            {
                result.BadRequest("email is required.");
                return result;
            }

            var user = await dbContext.User.FirstOrDefaultAsync(p => p.Email == email);
            if (user == null)
            {
                result.BadRequest("email is not registered.");
                return result;
            }

            if (user.IsConfirmed == true)
            {
                result.BadRequest("email is confirmed.");
                return result;
            }

            var now = DateTime.UtcNow;
            SendConfirmEmail(user.Email, JwtTokenHelper.Create(new InternalToken()
            {
                Purpose = "confirmemail",
                Email = email,
                CreateTime = now,
                ExpireTime = now.AddYears(1)
            }.ToTokenString()));

            return result;
        }

        private async Task<Result> RequestResetPassword(SqlDbContext dbContext, Dictionary<string, string> parameters)
        {
            var result = new Result();

            if (parameters.TryGetValue("email", out string email) == false)
            {
                result.BadRequest("email is required.");
                return result;
            }

            var user = await dbContext.User.FirstOrDefaultAsync(p => p.Email == email);
            if (user == null)
            {
                result.BadRequest("email is not registered.");
                return result;
            }

            var now = DateTime.UtcNow;
            SendResetPassword(user.Email, new InternalToken()
            {
                Purpose = "resetpassword",
                Email = email,
                CreateTime = now,
                ExpireTime = now.AddYears(1)
            }.ToTokenString());

            return result;
        }

        private async Task<Result> ResetPassword(SqlDbContext dbContext, Dictionary<string, string> parameters)
        {
            var result = new Result();

            if (parameters.TryGetValue("token", out string token) == false)
            {
                result.BadRequest("token is required.");
                return result;
            }

            if (parameters.TryGetValue("password", out string password) == false)
            {
                result.BadRequest("password is required.");
                return result;
            }

            var internalToken = InternalToken.VerifyTokenString(token, "resetpassword");
            if (internalToken == null)
            {
                result.BadRequest("Invalid token.");
                return result;
            }

            if (internalToken.IsExpired() == true)
            {
                result.BadRequest("token is expired.");
                return result;
            }

            var user = await dbContext.User.FirstOrDefaultAsync(p => p.Email == internalToken.Email);

            if (internalToken.IsRevoked(user.TokenValidTime) == true)
            {
                result.BadRequest("token is revoked.");
                return result;
            }

            user.PasswordHash = PasswordHash.HashedBase64String(password);
            user.TokenValidTime = DateTime.UtcNow.Ticks;
            dbContext.User.Update(user);
            await dbContext.SaveChangesAsync();

            return result;
        }

        private async Task<Result> Verify(SqlDbContext dbContext, Dictionary<string, string> parameters)
        {
            var result = new Result();
            if (parameters.TryGetValue("token", out string token) == false)
            {
                result.BadRequest("token is required.");
                return result;
            }

            if (parameters.TryGetValue("token_type", out string token_type) == false)
            {
                result.BadRequest("token_type is required.");
                return result;
            }

            if (token_type == "id_token")
            {

            }
            else if (token_type == "access_token")
            {
                var accessToken = new JwtAccessToken();
                if (accessToken.FromJwtString(token) == false)
                {
                    result.BadRequest("Invalid token.");
                    return result;
                }

                var user = await dbContext.User.FirstOrDefaultAsync(p => p.Email == accessToken.Email);

                if (accessToken.IsRevoked(user.TokenValidTime) == true)
                {
                    result.BadRequest("token is revoked.");
                    return result;
                }

            }

            result.ResponseText = "Invalid token_type";

            return result;
        }

        private async Task<Result> Logout(SqlDbContext dbContext, Dictionary<string, string> parameters)
        {
            var result = new Result();
            if (parameters.TryGetValue("logintoken", out string logintoken) == false)
            {
                result.BadRequest("logintoken is required.");
                return result;
            }

            var token = InternalToken.VerifyTokenString(logintoken, "login");

            if (token == null)
            {
                result.BadRequest("Invalid logintoken.");
                return result;
            }

            if (token.IsExpired() == true)
            {
                result.BadRequest("logintoken is expired.");
                return result;
            }

            var user = await dbContext.User.FirstOrDefaultAsync(p => p.Email == token.Email);

            if (token.IsRevoked(user.TokenValidTime) == true)
            {
                result.BadRequest("logintoken is revoked.");
                return result;
            }

            user.TokenValidTime = DateTime.UtcNow.Ticks;
            dbContext.User.Update(user);
            await dbContext.SaveChangesAsync();

            return result;
        }

        private void SendConfirmEmail(string email, string token)
        {
            var sb = new StringBuilder();
            sb.AppendLine("ConfirmEmail");
            sb.AppendLine(string.Format("$user\t{0}", email));
            sb.AppendLine(string.Format("$token\t{0}", token));
            var content = sb.ToString();

            File.WriteAllText(Path.Combine(EmailFolderPath, string.Format("{0}.txt", DateTime.UtcNow.Ticks)), content);
        }

        private void SendResetPassword(string email, string token)
        {
            var sb = new StringBuilder();
            sb.AppendLine("ResetPassword");
            sb.AppendLine(string.Format("$user\t{0}", email));
            sb.AppendLine(string.Format("$token\t{0}", token));
            var content = sb.ToString();

            File.WriteAllText(Path.Combine(EmailFolderPath, string.Format("{0}.txt", DateTime.UtcNow.Ticks)), content);
        }

        private SqlDbContext GetDbContext(HttpContext context)
        {
            return (SqlDbContext)context.RequestServices.GetService(typeof(SqlDbContext));
        }

        private Dictionary<string, string> GetRequestParameter(HttpRequest request)
        {
            var parameter = new Dictionary<string, string>();

            foreach (var q in request.Query)
            {
                parameter.Add(q.Key, q.Value);
            }

            if (request.HasFormContentType)
            {
                foreach (var f in request.Form)
                {
                    parameter.Add(f.Key, f.Value);
                }
            }

            return parameter;
        }
    }
}
