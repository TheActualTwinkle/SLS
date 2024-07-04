using SLS;
using SLS.Grpc;

public static class LobbyInfoParser
{
    /// <summary>
    /// Parsing PostLobbyInfoRequest to LobbyInfo
    /// </summary>
    /// <returns>Lobby info</returns>
    public static LobbyDto? Parse(PostLobbyInfoRequest request)
    {
        if (ushort.TryParse(request.Port.ToString(), out ushort port) == false)
        {
            return null;
        }
        
        return new LobbyDto(request.PublicIpAddress, port, request.MaxSeats, request.PlayersCount, request.LobbyName);
    }

    public static LobbyDto? Parse(GetLobbyInfoResponse response)
    {
        if (ushort.TryParse(response.Port.ToString(), out ushort port) == false)
        {
            return null;
        }

        return new LobbyDto(response.PublicIpAddress, port, response.MaxSeats, response.PlayersCount, response.LobbyName);
    }
    
    public static PostLobbyInfoRequest ToRequest(LobbyDto lobbyDto)
    {
        PostLobbyInfoRequest request = new()
        {
            PublicIpAddress = lobbyDto.PublicIpAddress,
            Port = lobbyDto.Port,
            MaxSeats = lobbyDto.MaxSeats,
            PlayersCount = lobbyDto.PlayersCount,
            LobbyName = lobbyDto.LobbyName
        };

        return request;
    }
}