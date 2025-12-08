using System.Collections.Concurrent;
using Consul;
using Microsoft.Extensions.Logging;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Feast.Extensions.ServiceDiscovery.Yarp.Consul;

internal class ConsulNotifyWorkersPlane : IAsyncDisposable
{
    private readonly ILogger                 logger;
    private readonly CancellationTokenSource stop   = new();
    private          CancellationTokenSource change = new();
    private          Task?                   initTask;

    public bool Initialized => initTask is { IsCompleted: true };

    private readonly ConcurrentDictionary<ConsulNotifyWorker, ServiceEntry[]> clients = [];
    
    public IEnumerable<ServiceEntry> Entries => clients.Values.SelectMany(x => x);

    public CancellationToken ChangeToken => change.Token;

    public ConsulNotifyWorkersPlane(
        ILogger<ConsulNotifyWorkersPlane> logger,
        IEnumerable<IConsulClient> clients,
        Func<ConsulClusterContext, ConsulQueryOptions> optionsFactory,
        ConsulClusterContext context)
    {
        this.logger = logger;
        foreach (var client in clients) 
            this.clients.TryAdd(new ConsulNotifyWorker(client, context, ClientOnChanged, optionsFactory), []);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        lock (this) initTask ??= StartInternal(cancellationToken);
        await initTask;
    }

    private async Task StartInternal(CancellationToken cancellationToken)
    {
        List<TaskCompletionSource> inits = [];
        cancellationToken.Register(() =>
        {
            foreach (var source in inits) source.SetCanceled(cancellationToken);
        });
        foreach (var client in clients.Keys)
        {
            var init = new TaskCompletionSource();
            client.Start(init, stop.Token);
            inits.Add(init);
        }
        await Task.WhenAll(inits.Select(x => x.Task));
    }
    
    
    private async Task ClientOnChanged(ConsulNotifyWorker sender, ServiceEntry[] entries, Exception? error)
    {
        if (error is not null)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.LogError(error, error.Message);
            }
            return;
        }
        clients[sender] = entries;
        var stash = change;
        change = new CancellationTokenSource();
#if NET10_0
        await stash.CancelAsync();
#else 
        stash.Cancel();
#endif
        stash.Dispose();
        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("{Client} detect a health change with available entries:{Count}", sender, entries.Length);
        }
    }

    public async ValueTask DisposeAsync()
    {
        initTask?.Dispose();
#if NET10_0
        await stop.CancelAsync();
#else 
        stop.Cancel();
#endif
        foreach (var client in clients.Keys) await client.DisposeAsync();
    }
}