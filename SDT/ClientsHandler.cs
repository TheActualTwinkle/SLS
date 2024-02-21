using System.Net;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json;

namespace SDT;

/// <summary>
/// Handler of SnaP clients.
/// </summary>
public class ClientsHandler
{
    private const uint BufferSize = 512;

    public bool HasClients
    {
        get
        {
            lock (_clientsListLock)
            {
                return _clients.Any();
            }
        }
    }
    
    public const string GetGuidsCommand = "get-guids";
    public const string GetInfoCommand = "get-info";
    public const string CloseCommand = "close";
    public const string UnknownCommandResponse = "Unknown command.";

    private readonly string _ipAddress;
    private readonly int _port;
    
    // Local list of connected SnaP Clients.
    private readonly List<Guid> _clients = new();
    
    private readonly object _clientsListLock = new();
    
    private TcpListener? _server;

    public ClientsHandler(string ipAddress, int port)
    {
        _ipAddress = ipAddress;
        _port = port;
    }

    public async void Start()
    {
        try
        {
            // TcpListener is used to wait for a connection from a client.
            _server = new TcpListener(IPAddress.Parse(_ipAddress), _port);

            // Start listening for client requests.
            _server.Start();

            Console.WriteLine($"[CH] Server for SnaP CLIENTS started at {_ipAddress}:{_port}. Waiting for connections...");

            while (true)
            {
                // Blocks until a client has connected to the server.
                TcpClient client = await _server.AcceptTcpClientAsync();

                // Create a thread to handle the client communication.
                Thread clientThread = new(Handle);
                clientThread.Start(client);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("[CH] Exception: " + e.Message);
        }
        finally
        {
            // Stop listening for new clients.
            Stop();
        }

        Console.WriteLine("[CH] Server closing...");
    }

    private async void Handle(object obj)
    {
        Guid guid = Guid.NewGuid();

        TcpClient tcpClient;
        try
        {
            tcpClient = (TcpClient)obj!;
        }
        catch (Exception e)
        {
            Console.WriteLine($"[CH/{guid}] Error on parse obj to TcpClient. {e}");
            throw;
        }
        
        NetworkStream clientStream = tcpClient.GetStream();

        Console.WriteLine($"[CH/{guid}] Client connected!");

        lock (_clientsListLock)
        {
            _clients.Add(guid);
        }

        // Buffer to store the response bytes.
        var message = new byte[BufferSize];

        while (true)
        {
            int bytesRead;
            
            // Send check command to client.
            // If cant write to stream the exception will be raised - client will be closed.
            try
            {
                bytesRead = await clientStream.ReadAsync(message);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                break;
            }
            
            // If cant read from stream the exception will be raised - client will be closed.
            if (bytesRead <= 0)
            {
                Console.WriteLine($"[CH/{guid}] Client closed connection.");
                break;
            }
            
            string messageString = Encoding.ASCII.GetString(message, 0, bytesRead).ToLower();
            
            if (messageString == CloseCommand)
            {
                HandleCloseCommand(guid);
                break;
            }
            
            if (messageString == GetGuidsCommand)
            {
                try
                {
                    await HandleGetGuidsCommand(clientStream, guid);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    break;
                }
            }
            else if (messageString.Contains(GetInfoCommand) == true)
            {
                if (await HandleGetInfoCommand(messageString, clientStream, guid) == false)
                {
                    break;
                }
            }
            else
            {
                HandleUnknownCommand(messageString, clientStream, guid);
            }
        }

        lock (_clientsListLock)
        {
            _clients.Remove(guid);
        }
        
        Console.WriteLine($"[CH/{guid}] Closing connection.");
        tcpClient.Close();
    }

    public void Stop()
    {
        _server?.Stop();
    }

    #region CommandHandlers

    private static async Task HandleGetGuidsCommand(NetworkStream clientStream, Guid chGuid)
    {
        string keysJson = JsonConvert.SerializeObject(Program.LobbyInfos.Keys);
        byte[] keysAsBytes = Encoding.ASCII.GetBytes(keysJson);
        
        await clientStream.WriteAsync(keysAsBytes);
        
        Console.WriteLine($"[CH/{chGuid}] Sent lobbies Guids. Count: {Program.LobbyInfos.Count}.");
    }

    private static async Task<bool> HandleGetInfoCommand(string messageString, NetworkStream clientStream, Guid chGuid)
    {
        int indexOfSeparator = messageString.IndexOf(' ');
        indexOfSeparator++;

        if (indexOfSeparator == -1)
        {
            Console.WriteLine($"[CH/{chGuid}] Can`t find index of separator in message.");
            return false;
        }

        string guidString = messageString[indexOfSeparator..];

        if (Guid.TryParse(guidString, out Guid guid) == false)
        {
            Console.WriteLine($"[CH/{chGuid}] Can`t parse guid from message: {guidString}");
            return false;
        }

        if (Program.LobbyInfos.ContainsKey(guid) == false)
        {
            Console.WriteLine($"[CH/{chGuid}] Can`t find entry with wanted guid: {guid}.");
            return false;
        }

        try
        {
            LobbyInfo clientState = Program.LobbyInfos[guid];

            string stateJson = JsonConvert.SerializeObject(clientState);
            byte[] reply = Encoding.ASCII.GetBytes(stateJson);

            await clientStream.WriteAsync(reply);
            Console.WriteLine($"[CH/{chGuid}] Sent {reply.Length} bytes.");
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return false;
        }

        return true;
    }

    private static void HandleCloseCommand(Guid chGuid)
    {
        Console.WriteLine($"[CH/{chGuid}] Client closed connection.");
    }
    
    private static async void HandleUnknownCommand(string messageString, NetworkStream clientStream, Guid chGuid)
    {
        byte[] length = "Unknown command."u8.ToArray();
        await clientStream.WriteAsync(length);
        
        Console.WriteLine($"[CH/{chGuid}] Unknown command: {messageString}");
    }

    #endregion
}