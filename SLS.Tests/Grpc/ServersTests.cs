using Google.Protobuf.WellKnownTypes;
using Grpc.Net.Client;
using SLS.Grpc;

namespace SLS.Tests.Grpc;

[TestFixture]
public class ServersTests
{
    private ServersHandlerService _serversHandler = null!;

    private GrpcChannel _channel = null!;
    private ServersHandler.ServersHandlerClient _client = null!;

    private const string ServersUrl = "https://localhost:47920";

    [SetUp]
    public void Setup()
    {
        _serversHandler = new ServersHandlerService(ServersUrl);
#pragma warning disable CS4014
        _serversHandler.Run();
#pragma warning restore CS4014

        _channel = GrpcChannel.ForAddress(ServersUrl);
        _client = new ServersHandler.ServersHandlerClient(_channel);
    }

    [Test]
    public async Task GetGuids()
    {
        LobbyDto expectedLobbyDto = await PostRandomLobbyInfo();

        LobbyDto actualLobbyDto = Program.LobbyInfos.First().Value;

        Assert.That(Tools.LobbyInfoValuesEquals(expectedLobbyDto, actualLobbyDto) && Program.LobbyInfos.IsEmpty == false, Is.True);
    }

    [Test]
    public async Task DropLobby()
    {
        await PostRandomLobbyInfo();

        await _client.DropLobbyAsync(new Empty());

        Assert.That(Program.LobbyInfos.IsEmpty, Is.EqualTo(true));
    }

    [Test]
    public async Task AutoDropLobby()
    {
        await Cleanup();
        
        ServersHandlerService.PollingInterval = TimeSpan.FromMilliseconds(100);
        
        Setup();
        
        await PostRandomLobbyInfo();
        
        if (Program.LobbyInfos.IsEmpty)
        {
            Assert.Fail("Lobby info was not added.");
        }
        
        await Task.Delay(TimeSpan.FromSeconds(1));
        
        Assert.That(Program.LobbyInfos.IsEmpty, Is.True);
    }

    [TearDown]
    public async Task Cleanup()
    {
        _channel.Dispose();
        Program.LobbyInfos.Clear();
        await _serversHandler.Stop();
    }

    private async Task<LobbyDto> PostRandomLobbyInfo()
    {
        LobbyDto expectedLobbyDto = Tools.GetRandomLobbyInfo();
        PostLobbyInfoRequest request = LobbyInfoParser.ToRequest(expectedLobbyDto);

        await _client.PostLobbyInfoAsync(request);
        return expectedLobbyDto;
    }
}