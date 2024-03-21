using System.Net;
using System.Net.Sockets;
using System.Text;
using SDT.TcpIp;
using SDT.TcpIp.Commands;

namespace SDT.Tests.TcpIp;

public static class Tools
{
    private const int EndDelayMs = 25;    
    
    public static async Task<TcpClient> Connect(IPAddress ipAddress, ushort port)
    {
        TcpClient tcpClient = new();
        await tcpClient.ConnectAsync(ipAddress, port);

        await Task.Delay(EndDelayMs);
        
        return tcpClient;
    }

    public static async Task Disconnect(TcpClient tcpClient)
    {
        if (tcpClient.Connected == true)
        {
            tcpClient.Close();
        }
        
        await Task.Delay(EndDelayMs);
    }
    
    public static async Task WriteCommandAsync(Command command, NetworkStream stream)
    {
        string message = CommandParser.ToJson(command)!;
        byte[] data = Encoding.ASCII.GetBytes(message);
            
        await stream.WriteAsync(data, 0, data.Length);
        
        await Task.Delay(EndDelayMs);
    }

    public static async Task<string> ReadAsync(NetworkStream stream, CancellationToken ct)
    {
        // Start with a reasonable initial buffer size
        const int initialBufferSize = 1024;
    
        // Initialize a MemoryStream to store the incoming data
        using MemoryStream memoryStream = new();
        
        // Create a temporary buffer for reading data
        var buffer = new byte[initialBufferSize];
        int bytesRead;
        
        // Loop until the end of the message is reached
        while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, ct)) > 0)
        {
            // Write the read bytes to the memory stream
            memoryStream.Write(buffer, 0, bytesRead);
            
            // If there's more data available, resize the buffer and continue reading
            if (stream.DataAvailable)
            {
                Array.Resize(ref buffer, buffer.Length * 2);
            }
            else
            {
                // No more data available, break out of the loop
                break;
            }
        }
        
        await Task.Delay(EndDelayMs, ct);
        
        // Convert the accumulated bytes to a string using ASCII encoding
        return Encoding.ASCII.GetString(memoryStream.ToArray());
    }

    /// <summary>
    /// Creates list of random lobby infos and registers them in Program.LobbyInfos dictionary.
    /// </summary>
    /// <param name="count">Count of lobby to create</param>
    /// <returns>Guid list of created lobby infos</returns>
    public static List<Guid> RegisterRandomLobbyInfo(uint count)
    {
        List<Guid> uids = [];

        for (var i = 0; i < count; i++)
        {
            uids.Add(Guid.NewGuid());
        }

        foreach (Guid uid in uids)
        {
            LobbyInfo randomLobbyInfo = GetRandomLobbyInfo();
            RegisterLobbyInfo(uid, randomLobbyInfo);
        }

        return uids;
    }

    /// <summary>
    /// Creates lobby info with random values and registers it in Program.LobbyInfos dictionary.
    /// </summary>
    /// <returns>Created lobby info</returns>
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

    /// <summary>
    /// Compares two lobby info values.
    /// </summary>
    /// <param name="lobbyInfo1">Info 1</param>
    /// <param name="lobbyInfo2">Info 2</param>
    /// <returns>True if values are equal</returns>
    public static bool LobbyInfoValuesEquals(LobbyInfo lobbyInfo1, LobbyInfo lobbyInfo2)
    {
        return lobbyInfo1.PublicIpAddress == lobbyInfo2.PublicIpAddress &&
               lobbyInfo1.Port == lobbyInfo2.Port &&
               lobbyInfo1.MaxSeats == lobbyInfo2.MaxSeats &&
               lobbyInfo1.PlayersCount == lobbyInfo2.PlayersCount &&
               lobbyInfo1.LobbyName == lobbyInfo2.LobbyName;
    }

    private static void RegisterLobbyInfo(Guid guid, LobbyInfo lobbyInfo)
    {
        Program.LobbyInfos.TryAdd(guid, lobbyInfo);
    }
}