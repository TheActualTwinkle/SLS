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
}