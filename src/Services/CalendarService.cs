using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using DuaBot.Data;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DuaBot.Services
{
    public class CalendarService : BackgroundService
    {
        private readonly ILogger<CalendarService> _logger;
        private readonly IHttpClientFactory _clientFactory;

        public CalendarService(ILogger<CalendarService> logger, IHttpClientFactory clientFactory)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _clientFactory = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var httpClient = _clientFactory.CreateClient(nameof(CalendarService));
            var calendarEventSink = CreateCalendarSink(httpClient, stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                using (var db = new DuaBotContext())
                {
                    foreach (var token in db.UserTokens)
                    {
                        if (!token.IsValidToken)
                        {
                            try
                            {
                                _logger.LogInformation("[CalendarService]: Re-authorizing user {0}", token.SlackId);
                                await httpClient.ReAuthorizeGraphUser(token, stoppingToken);
                                db.UserTokens.Update(token);
                                await db.SaveChangesAsync(stoppingToken);
                            }
                            catch (Exception ex)
                            {
                                // We cannot do anything here,
                                // just continue and hope that is goes better the next time we try
                                _logger.LogError(ex, "[CalendarService]: Failed to re-authorizing user {0}", token.SlackId);
                                continue;
                            }
                        }

                        // Process the tokens in parallel
                        await calendarEventSink.SendAsync(token);
                    }
                }

                _logger.LogInformation("[CalendarService]: Waiting for {0} minutes..",
                    Options.Default.CalendarServiceInterval.TotalMinutes);
                await Task.Delay(Options.Default.CalendarServiceInterval, stoppingToken);
            }

            calendarEventSink.Complete();
        }

        private ActionBlock<UserTokenMap> CreateCalendarSink(HttpClient httpClient, CancellationToken ct) =>
            new ActionBlock<UserTokenMap>(async (userToken) =>
                {
                    try
                    {
                        var events = await httpClient.GetCalendarEvents(userToken, ct);
                        var attending = events.value.Where(x => x.ResponseStatus.IsAttending);
                        var createTasks = attending.Select(calendar => calendar.FromCalendarValue(userToken));

                        if (createTasks.Any())
                        {
                            using (var db = new DuaBotContext())
                            {
                                await db.SlackUpdateTasks.AddRangeAsync(createTasks, ct);
                                await db.SaveChangesAsync(ct);
                            }

                            _logger.LogInformation("[CalendarService]: Queued up slack status update for user {0}", userToken.SlackId);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "[CalendarService]: Error when processing calendar events for user {0}", userToken.SlackId);
                    }
                },
                new ExecutionDataflowBlockOptions()
                {
                    BoundedCapacity = 10,
                    CancellationToken = ct,
                    MaxDegreeOfParallelism = Environment.ProcessorCount,
                });
    }
}
