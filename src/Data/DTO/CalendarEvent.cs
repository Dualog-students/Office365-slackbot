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
            public string subject { get; set; }
            public End start { get; set; }
            public End end { get; set; }
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
            public DateTimeOffset dateTime { get; set; }
            public string timeZone { get; set; }
        }
    }

    public enum Response { Accepted, None, NotResponded, Organizer, TentativelyAccepted };

    public static class CalendarEventExtensions
    {
        public static SlackUpdateTask FromCalendarValue(
            this CalendarEvent.Value cevent, UserTokenMap userToken)
        {
            return new SlackUpdateTask
            {
                End = cevent.end.dateTime,
                Start = cevent.start.dateTime,
                SlackUserId = userToken.SlackId,
                TimeZone = cevent.start.timeZone == cevent.end.timeZone ? cevent.start.timeZone : "UTC",
            };
        }

        public static async Task<CalendarEvent> GetCalendarEvents(
            this HttpClient httpClient, UserTokenMap token, CancellationToken ct)
        {
            var request = new HttpRequestMessage(HttpMethod.Get,
            "https://graph.microsoft.com/v1.0/me/calendarview?startdatetime=2019-03-01T18:54:43.926Z&enddatetime=2019-03-02T18:54:43.926Z");
            request.Headers.Add("Authorization", "Bearer " + token.AccessToken);
            var response = await httpClient.SendAsync(request, ct).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsAsync<CalendarEvent>(ct).ConfigureAwait(false);
        }
    }
}
