using System.Collections.Concurrent;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace SDT.Grpc;

public class ServersHandlerService(string? url = null) : ServersHandler.ServersHandlerBase, IServersHandler
{
    private class LobbyStatus(Guid guid)
    {
        public readonly Guid Guid = guid;
        public bool IsFetched;
    }

    /// <summary>
    /// Key: Peer remote endpoint in URI format. Value.Guid: Guid of associated LobbyInfo.
    /// </summary>
    private static ConcurrentDictionary<string, LobbyStatus> Peers { get; } = new();
    
    public static TimeSpan PollingInterval = TimeSpan.FromMinutes(1);

    private WebApplication? _app;
    
    public async Task Run()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();

        // Add services to the container.
        builder.Services.AddGrpc();

        _app = builder.Build();
        
        // Configure the HTTP request pipeline.
        _app.MapGrpcService<ServersHandlerService>();
        _app.MapGet("/",
            () =>
                "Communication with gRPC endpoints must be made through a gRPC client.");

#pragma warning disable CS4014
        Task.Run(async () =>
#pragma warning restore CS4014
        {
            while (true)
            {
                RemoveDeadLobbies();
                await Task.Delay(PollingInterval);
            }
            // ReSharper disable once FunctionNeverReturns
        });
        
        await _app.RunAsync(url);
    }
    
    public async Task Stop()
    {
        Peers.Clear();
        
        if (_app == null)
        {
            return;
        }
        
        await _app.StopAsync();
    }

    public override Task<Empty> PostLobbyInfo(PostLobbyInfoRequest request, ServerCallContext context)
    {
        LobbyStatus lobbyStatus = Peers.GetOrAdd(context.Peer, new LobbyStatus(Guid.NewGuid()));
        
        LobbyDto? lobbyInfo = LobbyInfoParser.Parse(request);

        if (lobbyInfo == null)
        {
            return Task.FromResult(new Empty());
        }

        Program.LobbyInfos.AddOrUpdate(lobbyStatus.Guid, lobbyInfo, (_, _) => lobbyInfo);
        lobbyStatus.IsFetched = true;

        Console.WriteLine($"[SH/{lobbyStatus.Guid}] Added/Updated new lobby info.");

        return Task.FromResult(new Empty());
    }

    public override Task<Empty> DropLobby(Empty _, ServerCallContext context)
    {
        if (TryRemoveLobby(context.Peer, out Guid guid) == false)
        {
            return Task.FromResult(new Empty());
        }

        Console.WriteLine($"[SH/{guid}] Lobby info removed.");

        return Task.FromResult(new Empty());
    }
    
    private void RemoveDeadLobbies()
    {
        Peers.Where(x => x.Value.IsFetched == false)
            .Select(x => x.Key)
            .ToList()
            .ForEach(peer =>
            {
                if (TryRemoveLobby(peer, out Guid guid) == true)
                {
                    Console.WriteLine($"[SH/{guid}] Lobby info is dead, removing...");
                }
            });

        // Mark all as not-fetched.
        foreach (LobbyStatus status in Peers.Values)
        {
            status.IsFetched = false;
        }
    }

    private bool TryRemoveLobby(string peer, out Guid guid)
    {
        if (Peers.TryRemove(peer, out LobbyStatus? status) == false)
        {
            guid = Guid.Empty;
            return false;
        }

        guid = status.Guid;

        if (Program.LobbyInfos.TryRemove(guid, out _) == false)
        {
            return false;
        }

        return true;
    }
}