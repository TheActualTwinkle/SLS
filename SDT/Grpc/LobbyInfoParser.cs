using SDT;
using SDT.Grpc;

public static class LobbyInfoParser
{
    /// <summary>
    /// Parsing PostLobbyInfoRequest to LobbyInfo
    /// </summary>
    /// <returns>Lobby info</returns>
    public static LobbyInfo? Parse(PostLobbyInfoRequest request)
    {
        if (ushort.TryParse(request.Port.ToString(), out ushort port) == false)
        {
            return null;
        }
        
        return new LobbyInfo(request.PublicIpAddress, port, request.MaxSeats, request.PlayersCount, request.LobbyName);
    }

    public static LobbyInfo? Parse(GetLobbyInfoResponse response)
    {
        if (ushort.TryParse(response.Port.ToString(), out ushort port) == false)
        {
            return null;
        }

        return new LobbyInfo(response.PublicIpAddress, (ushort)response.Port, response.MaxSeats, response.PlayersCount, response.LobbyName);
    }
    
    public static PostLobbyInfoRequest? ToRequest(LobbyInfo lobbyInfo)
    {
        PostLobbyInfoRequest request = new()
        {
            PublicIpAddress = lobbyInfo.PublicIpAddress,
            Port = lobbyInfo.Port,
            MaxSeats = lobbyInfo.MaxSeats,
            PlayersCount = lobbyInfo.PlayersCount,
            LobbyName = lobbyInfo.LobbyName
        };

        return request;
    }
}