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
    public class SlackService : BackgroundService
    {
        private readonly ILogger<SlackService> _logger;
        private readonly IHttpClientFactory _clientFactory;

        public SlackService(ILogger<SlackService> logger, IHttpClientFactory clientFactory)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _clientFactory = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var httpClient = _clientFactory.CreateClient(nameof(SlackService));
            var slackEventSink = CreateSlackUpdateSink(httpClient, stoppingToken);

            // Cleans up the finished tasks automatically
            var cleanUpTask = CreateCleanupTask(stoppingToken);

            // Process all pending calendar events
            while (!stoppingToken.IsCancellationRequested)
            {
                using (var db = new DuaBotContext())
                {
                    foreach (var task in db.SlackUpdateTasks.Where(x => x.InMeeting && !x.ShouldBeDeleted))
                    {
                        await slackEventSink.SendAsync(task);
                    }
                }

                _logger.LogInformation("[SlackService]: Waiting for {0} minutes..",
                    Options.Default.SlackServiceInterval.TotalMinutes);
                await Task.Delay(Options.Default.SlackServiceInterval, stoppingToken);
            }

            cleanUpTask.Dispose();
            slackEventSink.Complete();
        }

        private ActionBlock<SlackUpdateTask> CreateSlackUpdateSink(HttpClient httpClient, CancellationToken ct) =>
            new ActionBlock<SlackUpdateTask>(async (task) =>
                {
                    try
                    {
                        await httpClient.UpdateUserSlackStatus(task, ct);
                        _logger.LogInformation("[SlackService]: Update slack status for {0}", task.SlackUserId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "[SlackService]: Something went wrong when update slack status for user {0}", task.SlackUserId);
                    }
                },
                new ExecutionDataflowBlockOptions()
                {
                    BoundedCapacity = 10,
                    CancellationToken = ct,
                    MaxDegreeOfParallelism = Environment.ProcessorCount,
                });

        private Task CreateCleanupTask(CancellationToken ct) =>
            Task.Run(async () =>
            {
                var deleteDelay = Options.Default.SlackServiceDeleteInterval;

                using (var db = new DuaBotContext())
                {
                    while (!ct.IsCancellationRequested)
                    {
                        var tasksToDelete = db.SlackUpdateTasks.Where(x => x.ShouldBeDeleted);
                        var hey = db.SlackUpdateTasks.ToArray();
                        if (!tasksToDelete.Any())
                        {
                            _logger.LogInformation("[DeleteService]: Waiting for {0} minutes..",
                                deleteDelay.TotalMinutes);
                            await Task.Delay(deleteDelay, ct);
                            continue;
                        }

                        db.SlackUpdateTasks.RemoveRange(tasksToDelete);
                        var result = await db.SaveChangesAsync(ct);
                        _logger.LogInformation("[DeleteService]: {0} tasks removed from the db", result);
                    }
                }
            }, ct);
    }
}
