using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using DuaBot.Data;
using System.Threading;

namespace DuaBot.Controllers
{
    [ApiController]
    public class SlackController : ControllerBase
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<SlackController> _logger;

        public SlackController(ILogger<SlackController> logger, IMemoryCache cache)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpPost]
        [Route("api/slack/slashcommand")]
        public async Task<IActionResult> HandleCommand([FromForm] Slack.SlashCommand payload)
        {
            var options = Options.Default;

            // Check if rigth slack app token
            if (payload.token != options.SlackAppToken)
            {
                return BadRequest();
            }

            if (payload.command == "/register")
            {
                using (var db = new DuaBotContext())
                {
                    var token = await db.UserTokens.FirstOrDefaultAsync(x => x.SlackId == payload.user_id);
                    if (token == null)
                    {
                        _logger.LogInformation("User {0} not registered", payload.user_id);

                        var id = Guid.NewGuid().ToString();
                        var cache = _cache.Set(id, payload, TimeSpan.FromMinutes(2));

                        var url = "https://login.microsoftonline.com/common/oauth2/v2.0/authorize";
                        url += "?client_id=" + options.MsGraphClientId;
                        url += "&response_type=code";
                        url += "&response_mode=query";
                        url += "&redirect_uri=" + options.MsGraphRedirectUri;
                        url += "&scope=offline_access%20" + options.MsGraphScopes.Replace(" ", "%20");
                        url += "&state=" + id;

                        return Ok($"Please register here {url}");
                    }
                }

                // User is already registered
                return Ok("You are already registered😲");
            }

            if (payload.command == "/unregister")
            {
                using (var db = new DuaBotContext())
                {
                    var token = await db.UserTokens.FirstOrDefaultAsync(x => x.SlackId == payload.user_id);
                    if (token == null)
                    {
                        return Ok("🤔");
                    }

                    _logger.LogInformation("User {0} unregistered", payload.user_id);

                    db.UserTokens.Remove(token);
                    if (await db.SaveChangesAsync() > 0)
                    {
                        return Ok("You are unregistered now 😪");
                    }
                }
            }

            return Ok($"Command '{payload.command}' not supported 😔");
        }

        [HttpGet]
        [EnableCors]
        [Route("api/msgraph/authenticate")]
        public async Task<IActionResult> Auth(CancellationToken ct)
        {
            var code = HttpContext.Request.Query["code"].ToString();
            var state = HttpContext.Request.Query["state"].ToString();

            using (var db = new DuaBotContext())
            using (var httpClient = new HttpClient())
            {
                if (!_cache.TryGetValue(state, out Slack.SlashCommand cmd))
                {
                    if (await db.UserTokens.AnyAsync(x => x.SlackId == cmd.user_id, ct))
                        await httpClient.SendSlackWebHookMessage(cmd, "You are already registered.", ct).ConfigureAwait(false);
                    else
                        await httpClient.SendSlackWebHookMessage(cmd, "Something went wrong, please try again.", ct).ConfigureAwait(false);

                    return BadRequest();
                }

                _cache.Remove(state);

                var graphTokenMapping = await httpClient.AuthorizeGraphUser(code, cmd.user_id, ct).ConfigureAwait(false);
                await db.UserTokens.AddAsync(graphTokenMapping, ct).ConfigureAwait(false);
                await db.SaveChangesAsync(ct).ConfigureAwait(false);

                _logger.LogInformation("User {0} registered", cmd.user_id);

                await httpClient.SendSlackWebHookMessage(cmd, "You are now registered :rocket:", ct).ConfigureAwait(false);

                return new ContentResult
                {
                    StatusCode = 200,
                    // This oddly works in safari only :(
                    Content = "<body><script>(function(){window.open('','_self').close(); })();</script><h1><p>You can close the window now</p></h1></body>",
                    ContentType = "text/html",
                };
            }
        }
    }
}
