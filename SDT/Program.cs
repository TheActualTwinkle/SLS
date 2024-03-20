using System.Collections.Concurrent;

namespace SDT;

public static class Program
{
    public static readonly ConcurrentDictionary<Guid, LobbyInfo> LobbyInfos = new();
    
    public static async Task Main()
    {
        await ProjectContext.InitializeAsync();
        
        // Start handlers.
        Task? serverHandler = ProjectContext.ServersHandler?.Run();
        Task? clientsHandler = ProjectContext.ClientsHandler?.Run();
        
        if (serverHandler is null || clientsHandler is null)
        {
            Console.WriteLine("Error: Handlers are not initialized.");
            return;
        }
        
        await Task.WhenAll(serverHandler, clientsHandler);
    }
}
