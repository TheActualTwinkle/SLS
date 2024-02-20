using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json;

namespace SDT.Tests;

public class ClientTests
{
    private ClientsHandler? _clientsHandler;

    private NetworkStream NetworkStream => _tcpClient.GetStream();
    private TcpClient _tcpClient;

    private static uint BufferSize => ClientsHandler.BufferSize;
    
    private const ushort Port = 47921;

    [SetUp]
    public void Setup()
    {
        _clientsHandler = new ClientsHandler("127.0.0.1", 47921);
        _clientsHandler.Start();
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
        
        await Tools.WriteAsync(ClientsHandler.CloseCommand, NetworkStream);

        Assert.Pass();
        // Assert.IsFalse(_clientsHandler?.HasClients); todo: implement HasClients.
    }
    
    [Test]
    public async Task UnknownCommand()
    {
        _tcpClient = await Tools.Connect(Port);

        await Tools.WriteAsync("some-shit", NetworkStream);
        
        string response = await Tools.ReadAsync(BufferSize, NetworkStream, new CancellationTokenSource(10 * 1000).Token);
        
        Assert.IsTrue(response == ClientsHandler.UnknownCommandResponse);
    }
    
    [Test]
    public async Task GetLobbyCount()
    {
        _tcpClient = await Tools.Connect(Port);

        await Tools.WriteAsync(ClientsHandler.GetCountCommand, NetworkStream);
        
        // Get lobby count.
        string lobbyCountString = await Tools.ReadAsync(BufferSize, NetworkStream, new CancellationTokenSource(10 * 1000).Token);
        int lobbyCount = int.Parse(lobbyCountString);

        Assert.True(lobbyCount == 0);
    }
    
    [Test]
    public async Task GetLobbyInfo()
    {
        _tcpClient = await Tools.Connect(Port);

        const int lobbiesToAdd = 4;
        for (var i = 0; i < lobbiesToAdd; i++)
        {
            LobbyInfo lobbyInfo = Tools.GetRandomLobbyInfo();
            Tools.RegisterLobbyInfo(lobbyInfo);
        }
        
        for (var i = 0; i < lobbiesToAdd; i++)
        {
            await Tools.WriteAsync($"{ClientsHandler.GetInfoCommand} {i}", NetworkStream);
                
            string response = await Tools.ReadAsync(BufferSize, NetworkStream, new CancellationTokenSource(10 * 1000).Token);

            // Parsing json to LobbyInfo[] and return it.
            LobbyInfo? lobbyInfo = JsonConvert.DeserializeObject<LobbyInfo>(response);

            if (lobbyInfo == null)
            {
                Assert.Fail();
                return;
            }
            
            if (lobbyInfo.ValuesEquals(Program.LobbyInfos[i]) == true)
            {
                Assert.Pass();
            }
            else
            {
                Assert.Fail();
            }
        }
    }

    [TearDown]
    public void DisposeTcpClient()
    {
        Program.LobbyInfos.Clear();
        Tools.DisposeTcpClient(_tcpClient);
    }
}