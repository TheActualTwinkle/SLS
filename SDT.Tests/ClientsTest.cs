using System.Net.Sockets;
using System.Text;
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

        await Task.Delay(25);
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

        await Task.Delay(25);

        Assert.IsFalse(_clientsHandler?.HasClients);
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
            return;
        }

        for (var i = 0; i < Program.LobbyInfos.Keys.Count; i++)
        {
            if (guids.Contains(lobbyGuids[i]) == true)
            {
                continue;
            }
            
            Assert.Fail();
        }
    }
    
    [Test]
    public async Task GetLobbyInfo()
    {
        List<Guid> guids = Tools.RegisterRandomLobbyInfo(5);

        foreach (Guid uid in guids)
        {
            await Tools.WriteAsync($"{ClientsHandler.GetInfoCommand} {uid}", NetworkStream);
                
            string response = await Tools.ReadAsync(NetworkStream, new CancellationTokenSource(10 * 1000).Token);

            // Parsing json to LobbyInfo[] and return it.
            LobbyInfo? lobbyInfo = JsonConvert.DeserializeObject<LobbyInfo>(response);

            if (lobbyInfo == null)
            {
                Assert.Fail();
                return;
            }

            Assert.IsTrue(lobbyInfo.ValuesEquals(Program.LobbyInfos[uid]) == true);
        }
    }

    [TearDown]
    public void DisposeTcpClient()
    {
        _clientsHandler?.Stop();
        Program.LobbyInfos.Clear();
        Tools.CloseTcpClient(_tcpClient);
    }
}