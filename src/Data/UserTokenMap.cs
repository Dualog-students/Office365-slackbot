using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace DuaBot.Data
{
    public class UserTokenMap
    {
        [Key]
        public int Id { get; set; }
        public string SlackId { get; set; }
        public DateTime DateAdded { get; set; }
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }

        [NotMapped]
        public bool IsValidToken => (DateTime.UtcNow - DateAdded).TotalHours < 1;
    }

    public static class UserTokenMapExtensions
    {
        public static async Task<UserTokenMap> AuthorizeGraphUser(this HttpClient httpClient, string code, string userId, CancellationToken ct)
        {
            var options = Options.Default;
            var url = $"https://login.microsoftonline.com/common/oauth2/v2.0/token";
            var parameters = new Dictionary<string, string>()
            {
                {"code", code},
                {"scope", options.MsGraphScopes},
                {"grant_type" , "authorization_code"},
                {"client_id", options.MsGraphClientId},
                {"redirect_uri", options.MsGraphRedirectUri},
                {"client_secret", options.MsGraphClientSecret},
            };

            var request = new FormUrlEncodedContent(parameters);
            var response = await httpClient.PostAsync(url, request, ct);

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsAsync<MsGraphToken>(ct);
            return new UserTokenMap()
            {
                SlackId = userId,
                AccessToken = content.access_token,
                RefreshToken = content.refresh_token,
                DateAdded = DateTime.UtcNow,
            };
        }

        public static async Task<UserTokenMap> ReAuthorizeGraphUser(this HttpClient httpClient, UserTokenMap token, CancellationToken ct)
        {
            var options = Options.Default;
            var url = $"https://login.microsoftonline.com/common/oauth2/v2.0/token";
            var parameters = new Dictionary<string, string>()
            {
                {"scope", options.MsGraphScopes},
                {"grant_type" , "refresh_token"},
                {"refresh_token", token.RefreshToken},
                {"client_id", options.MsGraphClientId},
                {"redirect_uri", options.MsGraphRedirectUri},
                {"client_secret", options.MsGraphClientSecret},
            };

            var request = new FormUrlEncodedContent(parameters);
            var response = await httpClient.PostAsync(url, request, ct);

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsAsync<MsGraphToken>(ct);

            token.DateAdded = DateTime.UtcNow;
            token.AccessToken = content.access_token;
            token.RefreshToken = content.refresh_token;

            return token;
        }
    }
}
