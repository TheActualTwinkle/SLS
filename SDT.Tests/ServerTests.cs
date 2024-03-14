using System.Net.Sockets;
using SDT.Commands;
using SDT.Servers;

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
        await Tools.WriteCommandAsync(new Command(CommandType.Close), NetworkStream);
        
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
        await Tools.WriteCommandAsync(new Command(CommandType.GetStatus), NetworkStream);
        
        string response = await Tools.ReadAsync(NetworkStream, new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token);

        if (_serversHandler?.HasServers() == false)
        {
            Assert.Fail();
        }

        if (Program.LobbyInfos.IsEmpty == false)
        {
            Assert.Fail();
        }
        
        Assert.That(response, Is.EqualTo(ServersHandler.GetStatusSuccessResponse));
    }
    
    [Test]
    public async Task UnknownCommand()
    {
        await Tools.WriteCommandAsync(new Command(), NetworkStream);
        
        string response = await Tools.ReadAsync(NetworkStream, new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token);
        
        Assert.That(response, Is.EqualTo(ServersHandler.UnknownCommandResponse));
    }
    
    [Test]
    public async Task PostLobbyInfo_LobbyInfoAsJson_LobbyInfoArrayContainsEntry()
    {
        LobbyInfo randomLobbyInfo = await PostRandomLobbyInfo();

        LobbyInfo lobbyInfo = Program.LobbyInfos.Values.First();

        Assert.That(lobbyInfo.ValuesEquals(randomLobbyInfo), Is.True);
    }

    [Test]
    public async Task PostLobbyInfo_CorruptedLobbyInfo_ClientDropped()
    {
        await Tools.WriteCommandAsync(new Command(CommandType.PostLobbyInfo, "corrupted...lobby/info"), NetworkStream);
        
        Assert.That(Program.LobbyInfos.IsEmpty && _serversHandler!.HasServers() == false, Is.True);
    }

    [Test]
    public async Task EditLobbyInfo_LobbyInfoAsJson_LobbyInfoArrayChangesEntry()
    {
        LobbyInfo randomLobbyInfo1 = await PostRandomLobbyInfo();

        LobbyInfo lobbyInfo = Program.LobbyInfos.Values.First();

        if (lobbyInfo.ValuesEquals(randomLobbyInfo1) == false)
        {
            Assert.Fail();
        }

        LobbyInfo randomLobbyInfo2 = await PostRandomLobbyInfo();

        Assert.That(lobbyInfo.ValuesEquals(randomLobbyInfo2), Is.True);
    }
    
    [Test]
    public async Task UnsupportedCommand()
    {
        await Tools.WriteCommandAsync(new Command(CommandType.GetLobbyGuids), NetworkStream);
        
        Assert.That(_serversHandler!.HasServers() == true && Program.LobbyInfos.IsEmpty, Is.True);
    }

    [TearDown]
    public async Task Cleanup()
    {
        _serversHandler?.Stop();
        Program.LobbyInfos.Clear();
        await Tools.Disconnect(_tcpClient);
    }

    private async Task<LobbyInfo> PostRandomLobbyInfo()
    {
        LobbyInfo randomLobbyInfo = Tools.GetRandomLobbyInfo();
        await Tools.WriteCommandAsync(new Command(CommandType.PostLobbyInfo, randomLobbyInfo), NetworkStream);
        return randomLobbyInfo;
    }
}