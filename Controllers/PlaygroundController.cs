using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Hangfire;
using Hangfire.Server;
using Hangfire.States;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace WebApplicationHangfire.Controllers
{
  [ApiController]
  [Route("[controller]")]
  public class PlaygroundController : ControllerBase
  {
    private readonly IBackgroundJobClient _client;
    private readonly IStuff _stuff;
    private readonly IBackgroundProcessingServer _server;
    private readonly IServiceCollection _serviceCollection;
    private readonly IApplicationBuilder _applicationBuilder;

    public PlaygroundController(IBackgroundJobClient client, IStuff stuff, IBackgroundProcessingServer server, IServiceCollection serviceCollection, IApplicationBuilder applicationBuilder)
    {
      _client = client;
      _stuff = stuff;
      _server = server;
      _serviceCollection = serviceCollection;
      _applicationBuilder = applicationBuilder;
    }

    [HttpGet]
    public IEnumerable<string> GetEnqueueStuff()
    {
      var enqueuedJobs = new List<string>();
      var monitoringApi = JobStorage.Current.GetMonitoringApi();
      foreach (var queue in monitoringApi.Queues())
      {
        for (var i = 0; i < Math.Ceiling(queue.Length / 1000d); i++)
        {
          monitoringApi.EnqueuedJobs(queue.Name, 1000 * i, 1000)
              .ForEach(x => enqueuedJobs.Add($"{x.Key} {queue.Name}"));
        }
      }

      return enqueuedJobs;
    }

    [HttpPost]
    [Route("[controller]/[action]")]
    public async Task<string> InvokeLongRunningStuff(bool useHangfire)
    {
      if (useHangfire)
      {
        var id = _client.Enqueue<IStuff>(stuff => stuff.LongRunningStuff());
        return id;
      }
      else
      {
        await _stuff.LongRunningStuff();
        return "Processed without hangfire";
      }
    }

    [HttpPost]
    [Route("[controller]/[action]")]
    public async Task<string> InvokeLongRunningStuffCustomQuename()
    {
      var queue = new EnqueuedState("customqueuename");
      var id = new BackgroundJobClient().Create<IStuff>(c => c.LongRunningStuff(), queue);
      return id;
    }

    [HttpPost]
    [Route("[controller]/[action]")]
    public async Task<string> StopServer()
    {
      _server.SendStop();
      await _server.WaitForShutdownAsync(new System.Threading.CancellationToken());
      _server.Dispose();
      return "done";
    }

    [HttpPost]
    [Route("[controller]/[action]")]
    public void StartServer()
    {
      // Clean up Servers List
      var monitoringApi = JobStorage.Current.GetMonitoringApi();
      JobStorage.Current.GetConnection().RemoveTimedOutServers(new TimeSpan(0, 0, 15));

      _applicationBuilder.UseHangfireServer(new BackgroundJobServerOptions
      {
        WorkerCount = 1,
        Queues = new[] { "customqueuename" },
        ServerName = "Custom server 2",
      });
    }
  }
}