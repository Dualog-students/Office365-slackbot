using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace DuaBot.Data
{
    public class Slack
    {
        public Profile profile { get; set; }
        public class Profile
        {
            public string status_text { get; set; }
            public string status_emoji { get; set; }
            public long status_expiration { get; set; }
        }

        public class SlashCommand
        {
            public string text { get; set; }
            public string token { get; set; }
            public string command { get; set; }
            public string team_id { get; set; }
            public string user_id { get; set; }
            public string user_name { get; set; }
            public string trigger_id { get; set; }
            public string channel_id { get; set; }
            public string team_domain { get; set; }
            public string channel_name { get; set; }
            public string response_url { get; set; }
            public string enterprise_id { get; set; }
            public string enterprise_name { get; set; }
        }
    }

    public static class SlackExtensions
    {
        public static async Task<string> UpdateUserSlackStatus(
            this HttpClient httpClient, SlackUpdateTask task, CancellationToken ct)
        {
            var options = Options.Default;
            var request = new HttpRequestMessage(HttpMethod.Post, "https://slack.com/api/users.profile.set");
            request.Headers.Add("Authorization", "Bearer " + options.SlackAuthToken);
            request.Headers.Add("X-Slack-User", task.SlackUserId);
            request.Content = new StringContent(JsonConvert.SerializeObject(new
            {
                profile = new Slack.Profile
                {
                    status_text = "In a meeting" +
                        (options.UseCalendarSubject ? ": " + task.Subject : ""),
                    status_emoji = ":spiral_calendar_pad:",
                    status_expiration = task.End.ToUnixTimeSeconds(),
                }
            }), System.Text.Encoding.UTF8, "application/json");

            var response = await httpClient.SendAsync(request, ct).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        }

        public static async Task<bool> SendSlackWebHookMessage(
            this HttpClient httpClient, Slack.SlashCommand command, string msg, CancellationToken ct)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Post, command.response_url)
                {
                    Content = new StringContent("{\"text\" : \"" + msg + "\"}",
                        System.Text.Encoding.UTF8, "application/json")
                };

                var response = await httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
