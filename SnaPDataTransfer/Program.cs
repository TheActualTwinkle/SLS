using Open.Nat;
using SDT;

namespace SnaPDataTransfer;

using System.Net;
using System.Net.Sockets;
using System.Threading;

internal class Program
{
    public static readonly List<LobbyInfo> ClientStates = new();

    public static string PublicIpAddress => _publicIpAddress;
    
    private static string _publicIpAddress = "127.0.0.1";
    private static string _localIpAddress = "127.0.0.1";
    
    private const int DedicatedPort = 47920;
    private const int StandalonePort = 47921;

    private static async Task Main()
    {
        _localIpAddress = await GetLocalIpAsync();
        _publicIpAddress = (await GetExternalIp()).ToString();

        Console.WriteLine($"Local IP Address is: {_localIpAddress}");
        Console.WriteLine($"Public IP Address is: {_publicIpAddress}");
        
        // Start dedicated server handler.
        ServerHandler serverHandler = new(_localIpAddress, DedicatedPort);
        Thread dedicatedServerThread = new(serverHandler.Start);
        dedicatedServerThread.Start();

        // Start standalone game handler.
        ClientHandler clientHandler = new(_localIpAddress, StandalonePort);
        Thread standaloneThread = new(clientHandler.Start);
        standaloneThread.Start();

        while (true)
        {
        }
        
        // ReSharper disable once FunctionNeverReturns
    }

    /// <summary>
    /// Try to get External IP (provided by FAI), pass this IP to Client
    /// </summary>
    /// <returns>Async Task, IPAddress</returns>
    private static async Task<IPAddress> GetExternalIp()
    {
        NatDevice router = await GetInterDevice();

        if (router == null)
        {
            throw new NullReferenceException();
        }
		
        return await router.GetExternalIPAsync();
    }    
    
    private static async Task<string> GetLocalIpAsync()
    {
        return (await Dns.GetHostEntryAsync(Dns.GetHostName()))
            .AddressList.First(
                f => f.AddressFamily == AddressFamily.InterNetwork)
            .ToString();
    }
    
    /// <summary>
    /// try to retrieve a UPnP compatible Device on the Route
    /// </summary>
    /// <returns>Async Task, NatDevice</returns>
    private static async Task<NatDevice> GetInterDevice()
    {
        var timeoutMs = 500;
        const int timeoutMaxMs = 15000;
        const int discoveryIntervalMs = 500;
		
        NatDiscoverer discoverer = new();
        List<NatDevice> devices;

        while (true)
        {
            try
            {
                CancellationTokenSource cts = new(timeoutMs);
                devices = new List<NatDevice>(await discoverer.DiscoverDevicesAsync(PortMapper.Upnp, cts));

                if (devices.Count > 0)
                {
                    break;
                }
            }
            catch (TaskCanceledException)
            {
                Console.WriteLine("Can`t find UPnP device. Trying again with double timeout...");
            }
			
            timeoutMs *= 2;
            if (timeoutMs >= timeoutMaxMs)
            {
                return null!;
            }

            await Task.Delay(discoveryIntervalMs);
        }
		
        foreach (NatDevice device in devices)
        {
            if (device.LocalAddress.AddressFamily == AddressFamily.InterNetwork)
            {
                return device;
            }
        }

        return null!;
    }
}
