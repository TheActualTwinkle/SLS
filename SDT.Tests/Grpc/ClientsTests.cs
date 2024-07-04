using Google.Protobuf.WellKnownTypes;
using Grpc.Net.Client;
using SDT.Grpc;

namespace SDT.Tests.Grpc;

[TestFixture]
public class ClientsTests
{
    private ClientsHandlerService _clientsHandler = null!;

    private GrpcChannel _channel = null!;
    private ClientsHandler.ClientsHandlerClient _client = null!;
    
    private const string ClientsUrl = "https://localhost:47921";
    
    [SetUp]
    public void Setup()
    {
        _clientsHandler = new ClientsHandlerService(ClientsUrl);
#pragma warning disable CS4014
        _clientsHandler.Run();
#pragma warning restore CS4014
        
        _channel = GrpcChannel.ForAddress(ClientsUrl);
        _client = new ClientsHandler.ClientsHandlerClient(_channel);
    }

    [Test]
    public async Task GetGuids()
    {
        Guid expectedGuids = Tools.RegisterRandomLobbyInfo(1).First();

        GetGuidsResponse response = await _client.GetGuidsAsync(new Empty());

        Guid actualGuids = response.Guids.Select(Guid.Parse).First();
        
        Assert.That(expectedGuids, Is.EqualTo(actualGuids));
    }
    
    [Test]
    public async Task GetLobbyInfo()
    {
        Guid guid = Tools.RegisterRandomLobbyInfo(1).First();
        
        GetLobbyInfoResponse response = await _client.GetLobbyInfoAsync(new GetLobbyInfoRequest { Guid = guid.ToString() });

        LobbyDto? actualLobbyInfo = LobbyInfoParser.Parse(response);

        if (actualLobbyInfo == null)
        {
            Assert.Fail("Can`t parse GetLobbyInfoResponse to LobbyInfo.");
        }
        
        LobbyDto expectedLobbyDto = Program.LobbyInfos[guid];

        Assert.That(Tools.LobbyInfoValuesEquals(expectedLobbyDto, actualLobbyInfo!), Is.True);
    }
    
    [TearDown]
    public async Task Cleanup()
    {
        _channel.Dispose();
        Program.LobbyInfos.Clear();
        await _clientsHandler.Stop();
    }
}