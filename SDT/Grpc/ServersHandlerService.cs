using System.Collections.Concurrent;
using Grpc.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace SDT.Grpc;

public static class LobbyInfoConverter
{
    public static LobbyInfo? FromRequest(PostLobbyInfoRequest request)
    {
        if (ushort.TryParse(request.Port.ToString(), out ushort port) == false)
        {
            return null;
        }
        
        return new LobbyInfo(request.PublicIpAddress, port, request.MaxSeats, request.PlayersCount, request.LobbyName);
    }
}

public class ServersHandlerService(string? url = null) : ServersHandler.ServersHandlerBase, IServersHandler
{
    /// <summary>
    /// Key: Peer remote endpoint in URI format. Value: Guid of associated LobbyInfo.
    /// </summary>
    private static readonly ConcurrentDictionary<string, Guid> Peers = new();
    
    public async Task Run()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();

        // Add services to the container.
        builder.Services.AddGrpc();

        WebApplication app = builder.Build();

        // Configure the HTTP request pipeline.
        app.MapGrpcService<ServersHandlerService>();
        app.MapGet("/",
            () =>
                "Communication with gRPC endpoints must be made through a gRPC client.");
        
        await app.RunAsync(url);
    }

    // TODO: Implement Unity server shutdown handling. (Delete client`s lobby info.)
    public override Task<PostLobbyInfoResponse> PostLobbyInfo(PostLobbyInfoRequest request, ServerCallContext context)
    {
        Guid guid = Peers.GetOrAdd(context.Peer, Guid.NewGuid());

        LobbyInfo? lobbyInfo = LobbyInfoConverter.FromRequest(request);

        if (lobbyInfo == null)
        {
            return null!;
        }

        Program.LobbyInfos.AddOrUpdate(guid, lobbyInfo, (_, _) => lobbyInfo);

        Console.WriteLine($"[SH/{guid}] Added/Updated new lobby info.");

        return Task.FromResult(new PostLobbyInfoResponse());
    }
}