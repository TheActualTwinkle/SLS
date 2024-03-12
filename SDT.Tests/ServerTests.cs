using System.Net.Sockets;
using Newtonsoft.Json;

namespace SDT.Tests;

public class ServerTests
{
    private ServersHandler? _serversHandler;
    
    private NetworkStream NetworkStream => _tcpClient.GetStream();
    private TcpClient _tcpClient;

    private const ushort Port = 47920;

    [SetUp]
    public async Task Setup()
    {
        _serversHandler = new ServersHandler("127.0.0.1", Port);
        _serversHandler.Start();        
        
        _tcpClient = await Tools.Connect(Port);
    }
    
    [Test]
    public void Connect()
    {
        Assert.That(_tcpClient.Connected == true && _serversHandler?.HasServers() == true, Is.True);
    }
    
    // Handle Close command.
    [Test]
    public async Task Disconnect()
    {
        await Tools.WriteAsync(ServersHandler.CloseCommand, NetworkStream);
        
        Assert.That(_serversHandler?.HasServers(), Is.False);
    }

    [Test]
    public async Task DropConnection()
    {
        await Tools.Disconnect(_tcpClient);
        
        Assert.That(_serversHandler?.HasServers(), Is.False);
    }

    
    [Test]
    public async Task GetStatus()
    {
        await Tools.WriteAsync(ServersHandler.GetStatusCommand, NetworkStream);
        
        string response = await Tools.ReadAsync(NetworkStream, new CancellationTokenSource(10 * 1000).Token);

        if (_serversHandler?.HasServers() == false)
        {
            Assert.Fail();
        }

        if (Program.LobbyInfos.IsEmpty == false)
        {
            Assert.Fail();
        }
        
        Assert.That(response, Is.EqualTo(ServersHandler.GetStatusCommandResponse));
    }
    
    [Test]
    public async Task AddToLobbyInfos_LobbyInfoAsJson_LobbyInfoArrayContainsEntry()
    {
        LobbyInfo randomLobbyInfo = await SendRandomLobbyInfo();

        LobbyInfo lobbyInfo = Program.LobbyInfos.Values.First();

        Assert.That(lobbyInfo.ValuesEquals(randomLobbyInfo), Is.True);
    }

    [Test]
    public async Task AddToLobbyInfos_CorruptedJson_LobbyInfoArrayIsEmpty()
    {
        await Tools.WriteAsync("corrupted...json", NetworkStream);
        
        Assert.That(Program.LobbyInfos.IsEmpty, Is.True);
    }

    [Test]
    public async Task EditLobbyInfo_LobbyInfoAsJson_LobbyInfoArrayChangesEntry()
    {
        LobbyInfo randomLobbyInfo1 = await SendRandomLobbyInfo();

        LobbyInfo lobbyInfo = Program.LobbyInfos.Values.First();

        if (lobbyInfo.ValuesEquals(randomLobbyInfo1) == false)
        {
            Assert.Fail();
        }

        LobbyInfo randomLobbyInfo2 = await SendRandomLobbyInfo();

        Assert.That(lobbyInfo.ValuesEquals(randomLobbyInfo2), Is.True);
    }

    [TearDown]
    public async Task Cleanup()
    {
        _serversHandler?.Stop();
        Program.LobbyInfos.Clear();
        await Tools.Disconnect(_tcpClient);
    }

    private async Task<LobbyInfo> SendRandomLobbyInfo()
    {
        LobbyInfo randomLobbyInfo = Tools.GetRandomLobbyInfo();
        string jsonLobbyInfo = JsonConvert.SerializeObject(randomLobbyInfo);
        await Tools.WriteAsync(jsonLobbyInfo, NetworkStream);
        return randomLobbyInfo;
    }
}