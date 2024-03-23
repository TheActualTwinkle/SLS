using System.Net;
using System.Net.Sockets;

namespace SDT;

public static class ProjectContext
{
    public static IServersHandler? ServersHandler { get; private set; } 
    public static IClientsHandler? ClientsHandler { get; private set; }

    public const ushort ServersPort = 47920;
    public const ushort ClientsPort = 47921;

    private static HandlerType _handlerType = HandlerType.TcpIp;
    
    public static void InitializeAsync()
    {
        string[] args = Environment.GetCommandLineArgs();

        if (args.Length > 1 && args[1].Contains("-g"))
        {
            _handlerType = HandlerType.Grpc;
        }
        
        // DI.
        ServersHandler = HandlersFactory.GetServers(_handlerType);
        ClientsHandler = HandlersFactory.GetClients(_handlerType);

        Console.WriteLine("[Context] Handler type: " + _handlerType);
    }
}

public enum HandlerType : byte
{
    TcpIp,
    Grpc
}

public static class HandlersFactory
{
    private static readonly IPAddress? LocalIp;
    
    static HandlersFactory()
    {
        LocalIp = Dns.GetHostEntry(Dns.GetHostName()).AddressList.FirstOrDefault(f => f.AddressFamily == AddressFamily.InterNetwork);
    }
    
    
    public static IServersHandler GetServers(HandlerType handlerType)
    {
        return handlerType switch
        {
            HandlerType.TcpIp => new TcpIp.ServersHandler(LocalIp!, ProjectContext.ServersPort),
            HandlerType.Grpc => new Grpc.ServersHandlerService($"https://{LocalIp}:{ProjectContext.ServersPort}"),
            _ => throw new ArgumentOutOfRangeException(nameof(handlerType), handlerType, null)
        };
    }
    
    public static IClientsHandler GetClients(HandlerType handlerType)
    {
        return handlerType switch
        {
            HandlerType.TcpIp => new TcpIp.ClientsHandler(LocalIp!, ProjectContext.ClientsPort),
            HandlerType.Grpc => new Grpc.ClientsHandlerService($"https://{LocalIp}:{ProjectContext.ClientsHandler}"),
            _ => throw new ArgumentOutOfRangeException(nameof(handlerType), handlerType, null)
        };
    }
}

