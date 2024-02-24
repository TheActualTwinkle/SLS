using System.Net.Sockets;
using System.Text;
using System.Xml;
using Newtonsoft.Json;

namespace SDT.Tests;

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
        _clientsHandler = new ClientsHandler("127.0.0.1", Port);
        _clientsHandler.Start();
        
        _tcpClient = await Tools.Connect(Port);
    }

    [Test]
    public void Connect()
    {
        Assert.IsTrue(_tcpClient.Connected == true && _clientsHandler?.HasClients == true);
    }
    
    // Handle Close command.
    [Test]
    public async Task Disconnect()
    {
        await Tools.WriteAsync(ClientsHandler.CloseCommand, NetworkStream);

        Assert.IsFalse(_clientsHandler?.HasClients);
    }
    
    [Test]
    public async Task DropConnection()
    {
        await Tools.Disconnect(_tcpClient);
        
        Assert.IsFalse(_clientsHandler?.HasClients);
    }
    
    [Test]
    public async Task GetStatus()
    {
        await Tools.WriteAsync(ClientsHandler.GetStatusCommand, NetworkStream);
        
        string response = await Tools.ReadAsync(NetworkStream, new CancellationTokenSource(10 * 1000).Token);
        
        Assert.IsTrue(response == ClientsHandler.GetStatusCommandResponse);
    }
    
    [Test]
    public async Task UnknownCommand()
    {
        await Tools.WriteAsync("some-shit", NetworkStream);
        
        string response = await Tools.ReadAsync(NetworkStream, new CancellationTokenSource(10 * 1000).Token);
        
        Assert.IsTrue(response == ClientsHandler.UnknownCommandResponse);
    }
    
    [Test]
    public async Task GetLobbyGuids()
    {
        List<Guid> guids = Tools.RegisterRandomLobbyInfo(5);

        await Tools.WriteAsync(ClientsHandler.GetGuidsCommand, NetworkStream);
        
        // Get lobby guids.
        string lobbyGuidsJson = await Tools.ReadAsync(NetworkStream, new CancellationTokenSource(10 * 1000).Token);
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

        List<LobbyInfo> lobbyInfosByRequest = await GetLobbyInfosByRequest(ClientsHandler.GetInfoCommand, " ", guids);
        
        for (var i = 0; i < guids.Count; i++)
        {
            if (lobbyInfosByRequest[i].ValuesEquals(Program.LobbyInfos[guids[i]]) == false)
            {
                Assert.Fail();
            }
        }
    }

    [Test]
    public async Task GetLobbyInfo_RequestWithMissingSeparator_ArrayOfNull()
    {
        const uint randomLobbiesCount = 5;
        List<Guid> guids = Tools.RegisterRandomLobbyInfo(randomLobbiesCount);

        List<LobbyInfo> lobbyInfosByRequest = await GetLobbyInfosByRequest(ClientsHandler.GetInfoCommand, string.Empty, guids);
        
        // All elements should be null.
        Assert.IsTrue(lobbyInfosByRequest.All(x => x == null!));
    }

    [Test]
    public async Task GetLobbyInfo_RequestWithCorruptedGuids_ArrayOfNull()
    {
        const uint randomLobbiesCount = 5;
        List<Guid> guids = Tools.RegisterRandomLobbyInfo(randomLobbiesCount);

        List<LobbyInfo> lobbyInfosByRequest = await GetLobbyInfosByRequest(ClientsHandler.GetInfoCommand, " shit-", guids);
        
        // Corrupted element should be null.
        Assert.IsTrue(lobbyInfosByRequest.All(x => x == null!));
    }

    [Test]
    public async Task GetLobbyInfo_RequestWithFakeGuid_OneNullInfo()
    {
        const uint randomLobbiesCount = 5;
        List<Guid> guids = Tools.RegisterRandomLobbyInfo(randomLobbiesCount);

        // Generate randomLobbiesCount fake guidd.
        guids[0] = Guid.NewGuid();
        
        List<LobbyInfo> lobbyInfosByRequest = await GetLobbyInfosByRequest(ClientsHandler.GetInfoCommand, " ", guids);
        
        // All elements should be null.
        Assert.IsTrue(lobbyInfosByRequest[0] == null! && lobbyInfosByRequest[1..] != null!);
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
    /// <param name="requestTemplate">'First part' of the Get Info request. e.g. {requestTemplate} = 'get-info')</param>
    /// <param name="separator">Defines a separator must be applied between {requestTemplate} and {guid}</param>
    /// <param name="guids">Guids array to be pasted in '{get-info}{separator}{guid}' request</param>
    /// <returns></returns>
    private async Task<List<LobbyInfo>> GetLobbyInfosByRequest(string requestTemplate, string separator, IEnumerable<Guid> guids)
    {
        List<LobbyInfo> lobbyInfos = new();

        foreach (Guid guid in guids)
        {
            var request = $"{requestTemplate}{separator}{guid}";
            await Tools.WriteAsync(request, NetworkStream);
                
            string response = await Tools.ReadAsync(NetworkStream, new CancellationTokenSource(10 * 1000).Token);

            try
            {
                LobbyInfo? lobbyInfo = JsonConvert.DeserializeObject<LobbyInfo>(response);
                lobbyInfos.Add(lobbyInfo!);
            }
            catch (Exception e)
            {
                lobbyInfos.Add(null!);
            }
        }

        return lobbyInfos;
    }

    #endregion
}