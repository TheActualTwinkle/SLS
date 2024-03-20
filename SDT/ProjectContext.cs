using System.Net;
using System.Net.Sockets;

namespace SDT;

public static class ProjectContext
{
    public static IServersHandler? ServersHandler { get; private set; }
    public static IClientsHandler? ClientsHandler { get; private set; }

    private const ushort ServerPort = 47920;
    private const ushort ClientPort = 47921;

    private static IPAddress? _localIpAddress;

    public static async Task InitializeAsync()
    {
        _localIpAddress = await GetLocalIPsAsync();
        
        // DI.
        ServersHandler = new TcpIp.ServersHandler(_localIpAddress!, ServerPort);
        ClientsHandler = new TcpIp.ClientsHandler(_localIpAddress!, ClientPort);
    }
    
    private static async Task<IPAddress?> GetLocalIPsAsync()
    {
        return (await Dns.GetHostEntryAsync(Dns.GetHostName()))
            .AddressList.FirstOrDefault(
                f => f.AddressFamily == AddressFamily.InterNetwork);
    }
}