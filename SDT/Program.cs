using System.Collections.Concurrent;

namespace SDT;

public static class Program
{
    public static readonly ConcurrentDictionary<Guid, LobbyInfo> LobbyInfos = new();
    
    public static async Task Main()
    {
        await ProjectContext.InitializeAsync();
        
        // Start handlers.
        Task serverHandler = Task.Run(() => ProjectContext.ServersHandler!.Run());
        Task clientsHandler = Task.Run(() => ProjectContext.ClientsHandler!.Run());

        await Task.WhenAll(serverHandler, clientsHandler);

        await ProjectContext.ServersHandler!.Stop();
        await ProjectContext.ClientsHandler!.Stop();
    }
}