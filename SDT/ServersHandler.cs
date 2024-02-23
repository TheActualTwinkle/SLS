using System.Net;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json;

namespace SDT;

/// <summary>
/// Handler of SnaP servers.
/// </summary>
public class ServersHandler
{
    private const uint BufferSize = 512;

    public bool HasServers
    {
        get
        {
            lock (_serversListLock)
            {
                return _servers.Any();
            }
        }
    }

    public const string CloseCommand = "close";
    private const string CheckCommand = "check";

    private readonly string _ipAddress;
    private readonly int _port;
    
    // Local list of connected SnaP Servers.
    private readonly List<Guid> _servers = new();

    private readonly object _serversListLock = new();

    private TcpListener? _server;
    
    public ServersHandler(string ipAddress, int port)
    {
        _ipAddress = ipAddress;
        _port = port;
    }

    public async void Start()
    { 
        try
        {
            IPAddress ipAddress = IPAddress.Parse(_ipAddress);

            // TcpListener is used to wait for a connection from a client.
            _server = new TcpListener(ipAddress, _port);

            // Start listening for client requests.
            _server.Start();

            Console.WriteLine($"[SH] Server for SnaP SERVERS started at {_ipAddress}:{_port}. Waiting for connections...");

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

        TcpClient tcpClient;
        try
        {
            tcpClient = (TcpClient)obj!;
        }
        catch (Exception e)
        {
            Console.WriteLine($"[SH/{guid}] Error on parse obj to TcpClient. {e}");
            throw;
        }
        
        NetworkStream clientStream = tcpClient.GetStream();

        Console.WriteLine($"[SH/{guid}] Client connected!");
        
        lock (_serversListLock)
        {
            _servers.Add(guid);
        }
        
        // Buffer to store the response bytes.
        var message = new byte[BufferSize];

        LobbyInfo lobbyInfo = new(string.Empty, 0, 0, 0, "Initializing...");

        while (true)
        {
            int bytesRead;
            
            // Send check command to client.
            // If cant write to stream the exception will be raised - client will be closed.
            try
            {
                await clientStream.WriteAsync(Encoding.ASCII.GetBytes(CheckCommand).AsMemory(0, CheckCommand.Length));
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                break;
            }

            // Read the incoming message. Expecting json lobby info.
            try
            {
                // Read the incoming message.
                bytesRead = await clientStream.ReadAsync(message.AsMemory(0, (int)BufferSize));
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                break;
            }
            
            if (bytesRead <= 0)
            {
                Console.WriteLine($"[SH/{guid}] Client closed connection.");
                break;
            }

            // Convert bytes to a string and print it.
            string clientMessage = Encoding.ASCII.GetString(message, 0, bytesRead);
            Console.WriteLine($"[SH/{guid}] Received: {clientMessage}");

            if (clientMessage == CloseCommand)
            {
                break;
            }
            
            // Parsing json to LobbyInfo.
            try
            {
                LobbyInfo lobbyInfoCopy = JsonConvert.DeserializeObject<LobbyInfo>(clientMessage)!;
                lobbyInfoCopy.Deconstruct(ref lobbyInfo); // Update info.
            }
            catch (Exception e)
            {
                Console.WriteLine($"[SH/{guid}] Can`t deserialize json to LobbyInfo. " + e);
                continue;
            }

            if (Program.LobbyInfos.ContainsKey(guid) == true)
            {
                continue;
            }
            
            Console.WriteLine($"[SH/{guid}] Added new lobby info.");
            Program.LobbyInfos.TryAdd(guid, lobbyInfo);
        }

        Program.LobbyInfos.TryRemove(guid, out _);
        
        lock (_serversListLock)
        {
            _servers.Remove(guid);
        }

        Console.WriteLine($"[SH/{guid}] Closing connection.");
        tcpClient.Close();
    }

    public void Stop()
    {
        _server?.Stop();
    }
}