using System.Net;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json;
using SLS.TcpIp.Commands;

namespace SLS.TcpIp;

/// <summary>
/// Handler of SnaP clients.
/// </summary>
public class ClientsHandler(IPAddress ipAddress, ushort port) : IClientsHandler
{
    private const uint BufferSize = 512;

    private readonly Semaphore _clientsListSemaphore = new(1, 1);
    
    public const string GetStatusResponse = "OK";
    public const string UnknownCommandResponse = "Unknown Command";

    // Local list of connected SnaP Clients.
    private readonly List<Guid> _clients = [];
    
    private TcpListener? _server;
    
    public async Task Run()
    {
        try
        {
            // TcpListener is used to wait for a connection from a client.
            _server = new TcpListener(ipAddress, port);

            // Start listening for client requests.
            _server.Start();

            Console.WriteLine($"[CH] Server for SnaP CLIENTS started at {ipAddress}:{port}. Waiting for connections...");

            while (true)
            {
                // Blocks until a client has connected to the server.
                TcpClient client = await _server.AcceptTcpClientAsync();

#pragma warning disable CS4014
                Task.Run(() => Handle(client));
#pragma warning restore CS4014
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("[CH] Exception: " + e.Message);
        }
        finally
        {
            // Stop listening for new clients.
            await Stop();
        }

        Console.WriteLine("[CH] Server closing...");
    }

    private async void Handle(TcpClient tcpClient)
    {
        Guid guid = Guid.NewGuid();
        
        NetworkStream clientStream = tcpClient.GetStream();

        // Check if version is same.
        
        Console.WriteLine($"[CH/{guid}] Client connected!");

        _clientsListSemaphore.WaitOne();
        _clients.Add(guid);
        _clientsListSemaphore.Release();

        // Buffer to store the response bytes.
        var message = new byte[BufferSize];

        while (true)
        {
            int bytesRead;
            
            // Send check command to client.
            // If cant write to stream the exception will be raised - client will be closed.
            try
            {
                bytesRead = await clientStream.ReadAsync(message.AsMemory());
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                HandleCloseCommand(guid, tcpClient);
                return;
            }
            
            // If cant read from stream the exception will be raised - client will be closed.
            if (bytesRead <= 0)
            {
                HandleCloseCommand(guid, tcpClient);
                return;
            }
            
            // Convert bytes to a string and print it.
            string clientMessage = Encoding.ASCII.GetString(message, 0, bytesRead);

            Console.WriteLine($"[CH/{guid}] Received command: {clientMessage}");

            Command? command = CommandParser.FromJson(clientMessage);
            CommandType? commandType = command?.Type;
            
            switch (commandType)
            {
                case CommandType.Close:
                    HandleCloseCommand(guid, tcpClient);
                    return;
                case CommandType.GetStatus:
                    await HandleGetStatusCommand(clientStream, guid);
                    break;
                case CommandType.GetLobbyGuids:
                    await HandleGetGuidsCommand(clientStream, guid);
                    break;
                case CommandType.GetLobbyInfo:
                    await HandleGetLobbyInfoCommand(command?.Content, clientStream, guid);
                    break;
                case null:
                    HandleUnknownCommand(clientMessage, clientStream, guid);
                    break;
                default:
                    HandleUnsupportedCommand(commandType.Value, clientStream, guid);
                    break;
            }
        }
    }

    public Task Stop()
    {
        _server?.Stop();
        return Task.CompletedTask;
    }

    public bool HasClients()
    {
        _clientsListSemaphore.WaitOne();
        bool any = _clients.Count != 0;
        _clientsListSemaphore.Release();

        return any;
    }
    
    private void DropClient(Guid guid, TcpClient tcpClient)
    {
        _clientsListSemaphore.WaitOne();
        _clients.Remove(guid);
        _clientsListSemaphore.Release();
        
        tcpClient.Close();
        Console.WriteLine($"[CH/{guid}] Client dropped.");
    }
    
    private async Task SendErrorMessage(NetworkStream clientStream, string errorMessage)
    {
        Console.WriteLine(errorMessage);
        
        byte[] errorMessageBytes = Encoding.ASCII.GetBytes(errorMessage);
        await clientStream.WriteAsync(errorMessageBytes);
    }

    #region CommandHandlers

    private async Task HandleGetStatusCommand(NetworkStream clientStream, Guid chGuid)
    {
        try
        {
            byte[] response = Encoding.ASCII.GetBytes(GetStatusResponse);
            await clientStream.WriteAsync(response.ToArray());
        
            Console.WriteLine($"[CH/{chGuid}] Sent status.");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
    
    private async Task HandleGetGuidsCommand(NetworkStream clientStream, Guid chGuid)
    {
        try
        {
            string keysJson = JsonConvert.SerializeObject(Program.LobbyInfos.Keys);
            byte[] keysAsBytes = Encoding.ASCII.GetBytes(keysJson);
        
            await clientStream.WriteAsync(keysAsBytes);
        
            Console.WriteLine($"[CH/{chGuid}] Sent lobbies Guids. Count: {Program.LobbyInfos.Count}.");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    private async Task HandleGetLobbyInfoCommand(object? content, NetworkStream clientStream, Guid chGuid)
    {
        if (Guid.TryParse(content?.ToString(), out Guid guid) == false)
        {
            var errorMessage = $"[CH/{chGuid}] Can't parse guid from message: {content}";
            await SendErrorMessage(clientStream, errorMessage);
            return;
        }

        if (Program.LobbyInfos.ContainsKey(guid) == false)
        {
            var errorMessage = $"[CH/{chGuid}] Can't find entry with wanted guid: {guid}.";
            await SendErrorMessage(clientStream, errorMessage);
            return;
        }

        try
        {
            LobbyDto clientState = Program.LobbyInfos[guid];

            string stateJson = JsonConvert.SerializeObject(clientState);
            byte[] reply = Encoding.ASCII.GetBytes(stateJson);

            await clientStream.WriteAsync(reply);
            Console.WriteLine($"[CH/{chGuid}] Sent {reply.Length} bytes.");
        }
        catch (Exception e)
        {
            var errorMessage = $"[CH/{chGuid}] Unexpected error: {e}.";
            await SendErrorMessage(clientStream, errorMessage);
        }
    }

    private void HandleCloseCommand(Guid guid, TcpClient client)
    {
        Console.WriteLine($"[CH/{guid}] Client closed connection.");
        DropClient(guid, client);
    }
    
    private async void HandleUnknownCommand(string message, NetworkStream clientStream, Guid guid)
    {
        byte[] length = Encoding.ASCII.GetBytes(UnknownCommandResponse);
        await clientStream.WriteAsync(length.AsMemory());
        
        Console.WriteLine($"[CH/{guid}] Unknown command: {message}");
    }

    private async void HandleUnsupportedCommand(CommandType type, NetworkStream clientStream, Guid guid)
    {
        byte[] length = Encoding.ASCII.GetBytes($"{type} is unsupported for ClientsHandler!");
        await clientStream.WriteAsync(length.AsMemory());
        
        Console.WriteLine($"[CH/{guid}] {type} is unsupported command.");
    }

    #endregion
}