using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json;

namespace SDT.Tests;

public class ServerTests
{
    private ServersHandler? _serversHandler;
    
    private NetworkStream NetworkStream => _tcpClient.GetStream();
    private TcpClient _tcpClient;
    
    private static uint BufferSize => ServersHandler.BufferSize;

    private const ushort Port = 47920;

    [SetUp]
    public void Setup()
    {
        _serversHandler = new ServersHandler("127.0.0.1", 47920);
        _serversHandler.Start();
    }
    
    [Test]
    public async Task Connect()
    {
        _tcpClient = await Tools.Connect(Port);

        Assert.IsTrue(_tcpClient.Connected);
    }
    
    // Handle Close command.
    [Test]
    public async Task Disconnect()
    {
        _tcpClient = await Tools.Connect(Port);

        await Tools.WriteAsync(ServersHandler.CloseCommand, NetworkStream);
        
        Assert.IsTrue(Program.LobbyInfos.Count == 0);
    }

    [Test]
    public async Task AddToLobbyInfos_LobbyInfoAsJson_LobbyInfoArrayContainsEntry()
    {
        _tcpClient = await Tools.Connect(Port);

        LobbyInfo randomLobbyInfo = Tools.GetRandomLobbyInfo();
        string jsonLobbyInfo = JsonConvert.SerializeObject(randomLobbyInfo);
        await Tools.WriteAsync(jsonLobbyInfo, NetworkStream);

        await Task.Delay(500);
        
        Assert.IsTrue(Program.LobbyInfos[0].ValuesEquals(randomLobbyInfo));
    }

    [Test]
    public async Task AddToLobbyInfos_CorruptedJson_LobbyInfoArrayIsEmpty()
    {
        _tcpClient = await Tools.Connect(Port);

        await Tools.WriteAsync("corrupted...json", NetworkStream);
        
        await Task.Delay(500);
        
        Assert.IsTrue(Program.LobbyInfos.Count == 0);
    }

    [TearDown]
    public void DisposeTcpClient()
    {
        Program.LobbyInfos.Clear();
        Tools.DisposeTcpClient(_tcpClient);
    }
}