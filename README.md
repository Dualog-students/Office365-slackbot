# Office365 slack bot

Currently it only updates peoples slack statuses when they are in a meeting (via Outlook Calendar).
But many features can be added in the future.

## Requirements

The settings class must be set with various these parameters:

```csharp
public class Options
{
    public string SlackAppToken { get; set; }
    public string SlackAuthToken { get; set; }

    public string MsGraphScopes { get; set; }
    public string MsGraphClientId { get; set; }
    public string MsGraphRedirectUri { get; set; }
    public string MsGraphClientSecret { get; set; }

    public bool UseCalendarSubject { get; set; }
}
```

## How it works

### Registration

1. User registers via slack command `/register me` within any Slack channel.
2. Server responds with an OAuth2 authentcation flow URL (only valid for 2 min)
3. User is asked to consent to permissions needed by the bot (_user.read calendars.read_)
4. Then the user is redirected to the server, and the server stores a correlation between the Slack id and Office365 id (token).
5. A message is then sent back to the Slack channel the user started within, with information about success/failure etc.

_A user can unregister at any time by using `/unregister me` from any slack channel_

### Slack status

1. The `CalendarService` continously checks all upcoming calendar events for registered users
    - It automatically checks if a token is expired and re-authenticates that user.
    - Queues up a new Slack status update in the database for calendar events where the user is either an _organizer_ or is _attending_.

2. The `SlackService` continously checks for queued tasks (with a given interval)
    - Invalidates all tasks that are old and deletes them (based on time)
    - If a meeting is starting in one minute or is started it queues up the Status update

## Limitations

The current code only supports one Slack org per instance as of now.

## TODOS

* Dequeue all pending tasks if user unregisters
* Ability to specify which DB to store data in
* Support multiple Slack Orgs (so that Mikael can sell it)
* Subscribe to MsGraph [change notifications](https://docs.microsoft.com/en-us/graph/webhooks) (some people create meetings right before a meeting).
