using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace DuaBot.Data
{
    /// <summary>
    /// MsGraph calendar event.
    /// </summary>
    public class CalendarEvent
    {
        public Value[] value { get; set; }

        public class Value
        {
            public string Subject { get; set; }
            public End Start { get; set; }
            public End End { get; set; }
            public Status ResponseStatus { get; set; }
        }

        public class Status
        {
            public Response Response { get; set; }
            public DateTimeOffset Time { get; set; }
            public bool IsAttending => Response == Response.Organizer || Response == Response.Accepted;
        }

        public class End
        {
            public string TimeZone { get; set; }
            public DateTimeOffset DateTime { get; set; }
        }
    }

    public enum Response { Accepted, None, NotResponded, Organizer, TentativelyAccepted };

    public static class CalendarEventExtensions
    {
        public static SlackUpdateTask FromCalendarValue(this CalendarEvent.Value cevent, UserTokenMap userToken)
        {
            return new SlackUpdateTask
            {
                End = cevent.End.DateTime,
                Start = cevent.Start.DateTime,
                SlackUserId = userToken.SlackId,
                TimeZone = cevent.Start.TimeZone == cevent.End.TimeZone ? cevent.Start.TimeZone : "UTC",
            };
        }

        public static async Task<CalendarEvent> GetCalendarEvents(this HttpClient httpClient, UserTokenMap token, CancellationToken ct)
        {
            var url = "https://graph.microsoft.com/v1.0/me/calendarview";
            url += "?startdatetime=" + DateTime.UtcNow.ToString("o");
            url += "&enddatetime=" + (DateTime.UtcNow + Options.Default.CalendarServiceInterval).ToString("o");

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Authorization", "Bearer " + token.AccessToken);
            var response = await httpClient.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsAsync<CalendarEvent>(ct);
        }
    }
}
