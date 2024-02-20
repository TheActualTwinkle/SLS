using System.Net.Sockets;
using System.Text;

namespace SDT.Tests;

public static class Tools
{
    public static async Task WriteAsync(string message, NetworkStream stream)
    {
        byte[] data = Encoding.ASCII.GetBytes(message);
            
        await stream.WriteAsync(data, 0, data.Length);
    }

    public static async Task<string> ReadAsync(uint bufferSize, NetworkStream stream, CancellationToken ct)
    {
        var buffer = new byte[bufferSize];

        int read = await stream.ReadAsync(buffer, ct);
        string message = Encoding.ASCII.GetString(buffer, 0, read);

        return message;
    }
    
    public static async Task<TcpClient> Connect(ushort port)
    {
        TcpClient tcpClient = new();
        await tcpClient.ConnectAsync("127.0.0.1", port);

        return tcpClient;
    }

    public static void DisposeTcpClient(TcpClient tcpClient)
    {
        tcpClient.Close();
        tcpClient.Dispose();
    }
    
    public static void RegisterLobbyInfo(LobbyInfo lobbyInfo)
    {
        Program.LobbyInfos.Add(lobbyInfo);
    }
    
    public static LobbyInfo GetRandomLobbyInfo()
    {
        Random random = new();
        
        const string ipAddress = "127.0.0.1";
        var port = (ushort)random.Next(0, 10000);
        
        int maxSeats = random.Next(0, 100);
        int playerCount = random.Next(0, maxSeats);
        
        string name = "TestLobby_" + random.Next(-100, 100);

        return new LobbyInfo(ipAddress, port, maxSeats, playerCount, name);
    }
}