using System.Net;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json;
using SDT.Commands;

namespace SDT.Servers;

/// <summary>
/// Handler of SnaP servers.
/// </summary>
public class ServersHandler(string address, int port)
{
    public const string GetStatusSuccessResponse = "OK";
    public const string UnknownCommandResponse = "Unknown Command";
    
    private const uint BufferSize = 512;
    
    private readonly Semaphore _serversListSemaphore = new(1, 1);

    // Local list of connected SnaP Servers.
    private readonly List<Guid> _servers = [];
    
    private TcpListener? _server;

    public async void Start()
    { 
        try
        {
            IPAddress ipAddress = IPAddress.Parse(address);

            // TcpListener is used to wait for a connection from a client.
            _server = new TcpListener(ipAddress, port);

            // Start listening for client requests.
            _server.Start();

            Console.WriteLine($"[SH] Server for SnaP SERVERS started at {address}:{port}. Waiting for connections...");

            while (true)
            {
                // Blocks until a client has connected to the server.
                TcpClient client = await _server.AcceptTcpClientAsync();

                Task.Run(() => Handle(client));
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("[SH] Exception: " + e.Message);
        }
        finally
        {
            // Stop listening for new clients.
            Stop();
        }
        
        Console.WriteLine("[SH] Server closing...");
    }
    
    private async void Handle(object? obj)
    {
        Guid guid = Guid.NewGuid();

        TcpClient tcpClient = (TcpClient)obj!;
        
        NetworkStream clientStream = tcpClient.GetStream();
        
        // Check if version is same.

        Console.WriteLine($"[SH/{guid}] Client connected!");
        
        _serversListSemaphore.WaitOne();
        _servers.Add(guid);
        _serversListSemaphore.Release();
        
        // Buffer to store the response bytes.
        var message = new byte[BufferSize];

        LobbyInfo lobbyInfo = new(string.Empty, 0, 0, 0, "Initializing...");

        while (true)
        {
            int bytesRead;

            // Read the incoming message. Expecting json lobby info.
            try
            {
                // Read the incoming message.
                bytesRead = await clientStream.ReadAsync(message.AsMemory());
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                HandleCloseCommand(guid, tcpClient);
                return;
            }
            
            if (bytesRead <= 0)
            {
                HandleCloseCommand(guid, tcpClient);
                return;
            }

            // Convert bytes to a string and print it.
            string clientMessage = Encoding.ASCII.GetString(message, 0, bytesRead);

            Console.WriteLine($"[SH/{guid}] Received command: {clientMessage}");

            Command? command = CommandParser.FromJson(clientMessage);
            CommandType? commandType = command?.Type;
            
            switch (commandType)
            {
                case CommandType.PostLobbyInfo:
                    LobbyInfo newLobbyInfo;
                    try
                    {
                        newLobbyInfo = JsonConvert.DeserializeObject<LobbyInfo>(command?.Content?.ToString()!)!;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"[SH/{guid}] Can`t deserialize json to LobbyInfo. " + e); 
                        DropClient(guid, tcpClient); 
                        return; 
                    }
                    
                    HandlePostLobbyInfoCommand(guid, lobbyInfo, newLobbyInfo);
                    break;
                case CommandType.GetStatus:
                    await HandleGetStatusCommand(tcpClient.GetStream());
                    break;
                case CommandType.Close:
                    HandleCloseCommand(guid, tcpClient);
                    return;
                case null:
                    HandleUnknownCommand(clientMessage, clientStream, guid);
                    break;
                default:
                    HandleUnsupportedCommand(commandType.Value, clientStream, guid);
                    break;
            }
        }
    }

    public void Stop()
    {
        _server?.Stop();
    }
    
    public bool HasServers()
    {
        _serversListSemaphore.WaitOne();
        bool any = _servers.Count != 0;
        _serversListSemaphore.Release();

        return any;
    }
    
    private void DropClient(Guid guid, TcpClient tcpClient)
    {
        _serversListSemaphore.WaitOne();
        _servers.Remove(guid);
        _serversListSemaphore.Release();
        
        tcpClient.Close();
        
        Program.LobbyInfos.TryRemove(guid, out _);
        
        Console.WriteLine($"[SH/{guid}] Client dropped.");
    }

    #region CommandHandlers

    private void HandlePostLobbyInfoCommand(Guid guid, LobbyInfo lobbyInfo, LobbyInfo newLobbyInfo)
    {
        newLobbyInfo.CopyValuesTo(ref lobbyInfo); // Update info.

        if (Program.LobbyInfos.ContainsKey(guid) == true)
        {
            return;
        }
            
        Console.WriteLine($"[SH/{guid}] Added new lobby info.");
        Program.LobbyInfos.TryAdd(guid, lobbyInfo);
    }

    private async Task HandleGetStatusCommand(NetworkStream clientStream)
    {
        Memory<byte> response = Encoding.ASCII.GetBytes(GetStatusSuccessResponse).AsMemory();
        await clientStream.WriteAsync(response);
    }
    
    private void HandleCloseCommand(Guid guid, TcpClient client)
    {
        Console.WriteLine($"[SH/{guid}] Client closed connection.");
        DropClient(guid, client);
    }
    
    private async void HandleUnknownCommand(string messageString, NetworkStream clientStream, Guid guid)
    {
        byte[] length = Encoding.ASCII.GetBytes(UnknownCommandResponse);
        await clientStream.WriteAsync(length.AsMemory());
        
        Console.WriteLine($"[SH/{guid}] Unknown command: {messageString}");
    }

    private async void HandleUnsupportedCommand(CommandType type, NetworkStream clientStream, Guid guid)
    {
        byte[] length = Encoding.ASCII.GetBytes($"{type} is unsupported for ServersHandler!");
        await clientStream.WriteAsync(length.AsMemory());
        
        Console.WriteLine($"[SH/{guid}] {type} is unsupported command.");
    }

    #endregion
}