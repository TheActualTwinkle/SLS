namespace SDT
{
    public class LobbyInfo(string publicIpAddress, ushort port, int maxSeats, int playersCount, string lobbyName)
    {
        public string PublicIpAddress = publicIpAddress;
        public ushort Port = port;
        
        public int MaxSeats = maxSeats;
        public int PlayersCount = playersCount;

        public string LobbyName = lobbyName;

        public void CopyValuesTo(ref LobbyInfo lobbyInfo)
        {
            lobbyInfo.PublicIpAddress = PublicIpAddress;
            lobbyInfo.Port = Port;
            lobbyInfo.MaxSeats = MaxSeats;
            lobbyInfo.PlayersCount = PlayersCount;
            lobbyInfo.LobbyName = LobbyName;
        }
    }
}