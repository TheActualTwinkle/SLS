using System.Net;

namespace SDT
{
    public class LobbyDto(string publicIpAddress, ushort port, int maxSeats, int playersCount, string lobbyName)
    {
        public string PublicIpAddress = publicIpAddress;
        public ushort Port = port;
        
        public int MaxSeats = maxSeats;
        public int PlayersCount = playersCount;

        public string LobbyName = lobbyName;

        public void CopyValuesTo(ref LobbyDto lobbyDto)
        {
            lobbyDto.PublicIpAddress = PublicIpAddress;
            lobbyDto.Port = Port;
            lobbyDto.MaxSeats = MaxSeats;
            lobbyDto.PlayersCount = PlayersCount;
            lobbyDto.LobbyName = LobbyName;
        }
    }
}