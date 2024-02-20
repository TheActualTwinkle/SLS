using System.Net;
using System.Net.Sockets;

namespace SDT;

public class Program
{
    public static readonly List<LobbyInfo> LobbyInfos = new();
    
    private const ushort ServerPort = 47920;
    private const ushort ClientPort = 47921;
    
    public static async Task Main()
    {
        IPAddress localIpAddress = await GetLocalIPsAsync();

        Console.WriteLine($"Starting on: {localIpAddress}\n Clients port: {ClientPort}\n Servers port: {ServerPort}");
        
        // Start dedicated server handler.
        ServersHandler serversHandler = new(localIpAddress.ToString(), ServerPort);
        Thread dedicatedServerThread = new(serversHandler.Start);
        dedicatedServerThread.Start();

        // Start standalone game handler.
        ClientsHandler clientsHandler = new(localIpAddress.ToString(), ClientPort);
        Thread standaloneThread = new(clientsHandler.Start);
        standaloneThread.Start();
        
        while (true)
        {
            Console.WriteLine($"LobbyInfos: {LobbyInfos.Count}");
            await Task.Delay(5000);
        }
        // ReSharper disable once FunctionNeverReturns
    }  
    
    private static async Task<IPAddress> GetLocalIPsAsync()
    {
        return (await Dns.GetHostEntryAsync(Dns.GetHostName()))
            .AddressList.First(
                f => f.AddressFamily == AddressFamily.InterNetwork);
    }
}
