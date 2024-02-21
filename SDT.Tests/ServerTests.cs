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
        
        await Task.Delay(25);
    }
    
    [Test]
    public void Connect()
    {
        Assert.IsTrue(_tcpClient.Connected == true && _serversHandler?.HasServers == true);
    }
    
    // Handle Close command.
    [Test]
    public async Task Disconnect()
    {
        await Tools.WriteAsync(ClientsHandler.CloseCommand, NetworkStream);

        await Task.Delay(25);

        Assert.IsFalse(_serversHandler?.HasServers);
    }

    [Test]
    public async Task EditLobbyInfo_LobbyInfoAsJson_LobbyInfoArrayChangesEntry()
    {
        LobbyInfo randomLobbyInfo1 = await SendRandomLobbyInfo();

        await Task.Delay(25);

        LobbyInfo lobbyInfo = Program.LobbyInfos.Values.First();

        if (lobbyInfo.ValuesEquals(randomLobbyInfo1) == false)
        {
            Assert.Fail();
        }

        LobbyInfo randomLobbyInfo2 = await SendRandomLobbyInfo();
        
        await Task.Delay(25);

        Assert.IsTrue(lobbyInfo.ValuesEquals(randomLobbyInfo2));
    }
    
    [Test]
    public async Task AddToLobbyInfos_LobbyInfoAsJson_LobbyInfoArrayContainsEntry()
    {
        LobbyInfo randomLobbyInfo = await SendRandomLobbyInfo();

        await Task.Delay(25);

        LobbyInfo lobbyInfo = Program.LobbyInfos.Values.First();

        Assert.IsTrue(lobbyInfo.ValuesEquals(randomLobbyInfo));
    }

    [Test]
    public async Task AddToLobbyInfos_CorruptedJson_LobbyInfoArrayIsEmpty()
    {
        await Tools.WriteAsync("corrupted...json", NetworkStream);
        
        await Task.Delay(25);
        
        Assert.IsTrue(Program.LobbyInfos.Count == 0);
    }

    [TearDown]
    public void DisposeTcpClient()
    {
        _serversHandler?.Stop();
        Program.LobbyInfos.Clear();
        Tools.CloseTcpClient(_tcpClient);
    }

    private async Task<LobbyInfo> SendRandomLobbyInfo()
    {
        LobbyInfo randomLobbyInfo = Tools.GetRandomLobbyInfo();
        string jsonLobbyInfo = JsonConvert.SerializeObject(randomLobbyInfo);
        await Tools.WriteAsync(jsonLobbyInfo, NetworkStream);
        return randomLobbyInfo;
    }
}