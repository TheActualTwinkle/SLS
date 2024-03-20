using System.Net;
using System.Net.Sockets;
using Newtonsoft.Json;
using SDT.TcpIp;
using SDT.TcpIp.Commands;

namespace SDT.Basic.Tests;

[TestFixture]
public class ClientTests
{
    private ClientsHandler? _clientsHandler;

    private NetworkStream NetworkStream => _tcpClient.GetStream();
    private TcpClient _tcpClient;
    
    private const ushort Port = 47921;

    [SetUp]
    public async Task Setup()
    {
        _clientsHandler = new ClientsHandler(IPAddress.Parse("127.0.0.1"), Port);
        _clientsHandler.Run();
        
        _tcpClient = await Tools.Connect(IPAddress.Parse("127.0.0.1"), Port);
    }

    [Test]
    public void Connect()
    {
        Assert.That(_tcpClient.Connected == true && _clientsHandler?.HasClients() == true, Is.True);
    }
    
    // Handle Close command.
    [Test]
    public async Task Disconnect()
    {
        await Tools.WriteCommandAsync(new Command(CommandType.Close), NetworkStream);

        Assert.That(_clientsHandler?.HasClients(), Is.False);
    }
    
    [Test]
    public async Task DropConnection()
    {
        await Tools.Disconnect(_tcpClient);
        
        Assert.That(_clientsHandler?.HasClients(), Is.False);
    }
    
    [Test]
    public async Task GetStatus()
    {
        await Tools.WriteCommandAsync(new Command(CommandType.GetStatus), NetworkStream);
        
        string response = await Tools.ReadAsync(NetworkStream, new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token);
        
        Assert.That(response, Is.EqualTo(ClientsHandler.GetStatusSuccessResponse));
    }
    
    [Test]
    public async Task UnknownCommand()
    {
        await Tools.WriteCommandAsync(new Command(), NetworkStream);
        
        string response = await Tools.ReadAsync(NetworkStream, new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token);
        
        Assert.That(response, Is.EqualTo(ClientsHandler.UnknownCommandResponse));
    }
    
    [Test]
    public async Task GetLobbyGuids()
    {
        List<Guid> guids = Tools.RegisterRandomLobbyInfo(5);

        await Tools.WriteCommandAsync(new Command(CommandType.GetLobbyGuids), NetworkStream);
        
        // Get lobby guids.
        string lobbyGuidsJson = await Tools.ReadAsync(NetworkStream, new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token);
        List<Guid>? lobbyGuids = JsonConvert.DeserializeObject<List<Guid>>(lobbyGuidsJson);

        if (lobbyGuids == null)
        {
            Assert.Fail();
        }

        for (var i = 0; i < Program.LobbyInfos.Keys.Count; i++)
        {
            if (guids.Contains(lobbyGuids![i]) == true)
            {
                continue;
            }
            
            Assert.Fail();
        }
    }
    
    [Test]
    public async Task GetLobbyInfo_CorrectRequest_ArrayOfCorrectLobbyInfo()
    {
        const uint randomLobbiesCount = 5;
        List<Guid> guids = Tools.RegisterRandomLobbyInfo(randomLobbiesCount);

        List<LobbyInfo> lobbyInfosByRequest = await GetLobbyInfosByRequest(guids);
        
        for (var i = 0; i < guids.Count; i++)
        {
            if (Tools.LobbyInfoValuesEquals(lobbyInfosByRequest[i], Program.LobbyInfos[guids[i]]) == false)
            {
                Assert.Fail();
            }
        }
    }

    [Test]
    public async Task GetLobbyInfo_RequestWithCorruptedGuids_ArrayOfNull()
    {
        await Tools.WriteCommandAsync(new Command(CommandType.GetLobbyInfo, "bad-guid"), NetworkStream);
        string response = await Tools.ReadAsync(NetworkStream, new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token);

        try
        {
            JsonConvert.DeserializeObject<LobbyInfo?>(response);
            Assert.Fail();
        }
        catch (JsonSerializationException)
        {
            Assert.Pass();
        }
    }

    [Test]
    public async Task GetLobbyInfo_RequestWithFakeGuid_OneNullInfo()
    {
        const uint randomLobbiesCount = 5;
        List<Guid> guids = Tools.RegisterRandomLobbyInfo(randomLobbiesCount);

        // Generate randomLobbiesCount fake guid.
        guids[0] = Guid.NewGuid();
        
        List<LobbyInfo> lobbyInfosByRequest = await GetLobbyInfosByRequest(guids);
        
        // All elements should be null.
        Assert.That(lobbyInfosByRequest[0] == null! && lobbyInfosByRequest[1..] != null!, Is.True);
    }
    
    [Test]
    public async Task UnsupportedCommand()
    {
        await Tools.WriteCommandAsync(new Command(CommandType.PostLobbyInfo), NetworkStream);
        
        Assert.That(_clientsHandler!.HasClients() == true && Program.LobbyInfos.IsEmpty, Is.True);
    }

    [TearDown]
    public async Task Cleanup()
    {
        _clientsHandler?.Stop();
        Program.LobbyInfos.Clear();
        await Tools.Disconnect(_tcpClient);
    }

    #region Helpers

    /// <summary>
    /// Helper method to get lobby infos by request.
    /// </summary>
    /// <param name="guids">Guids array to be pasted in '{get-info}{separator}{guid}' request</param>
    /// <returns></returns>
    private async Task<List<LobbyInfo>> GetLobbyInfosByRequest(IEnumerable<Guid> guids)
    {
        List<LobbyInfo> lobbyInfos = [];

        foreach (Guid guid in guids)
        {
            await Tools.WriteCommandAsync(new Command(CommandType.GetLobbyInfo, guid), NetworkStream);
                
            string response = await Tools.ReadAsync(NetworkStream, new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token);

            try
            {
                LobbyInfo? lobbyInfo = JsonConvert.DeserializeObject<LobbyInfo>(response);
                lobbyInfos.Add(lobbyInfo!);
            }
            catch (Exception)
            {
                lobbyInfos.Add(null!);
            }
        }

        return lobbyInfos;
    }

    #endregion
}