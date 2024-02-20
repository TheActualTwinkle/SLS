using System.Net;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json;
using SDT;

namespace SDT;

/// <summary>
/// Handler of SnaP clients.
/// </summary>
public class ClientsHandler
{
    public const uint BufferSize = 512;

    public bool HasClients => throw new NotImplementedException();
    
    public const string GetCountCommand = "get-count";
    public const string GetInfoCommand = "get-info";
    public const string CloseCommand = "close";
    public const string UnknownCommandResponse = "Unknown command.";

    private readonly string _ipAddress;
    private readonly int _port;

    public ClientsHandler(string ipAddress, int port)
    {
        _ipAddress = ipAddress;
        _port = port;
    }

    public async void Start()
    {
        TcpListener server = null!;

        try
        {
            // TcpListener is used to wait for a connection from a client.
            server = new TcpListener(IPAddress.Parse(_ipAddress), _port);

            // Start listening for client requests.
            server.Start();

            Console.WriteLine($"[CLIENT] Server for SnaP CLIENTS started at {_ipAddress}:{_port}. Waiting for connections...");

            while (true)
            {
                // Blocks until a client has connected to the server.
                TcpClient client = await server.AcceptTcpClientAsync();
                Console.WriteLine("[CLIENT] Client connected!");

                // Create a thread to handle the client communication.
                Thread clientThread = new(Handle);
                clientThread.Start(client);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("[CLIENT] Exception: " + e.Message);
        }
        finally
        {
            // Stop listening for new clients.
            server.Stop();
        }

        Console.WriteLine("[CLIENT] Server closing...");
    }

    private async void Handle(object obj)
    {
        TcpClient tcpClient = (TcpClient)obj;
        NetworkStream clientStream = tcpClient.GetStream();

        // Buffer to store the response bytes.
        var message = new byte[BufferSize];

        while (true)
        {
            int readAsync;
            try
            {
                readAsync = await clientStream.ReadAsync(message);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                break;
            }
            
            // If cant read from stream the exception will be raised - client will be closed.
            if (readAsync <= 0)
            {
                Console.WriteLine($"[CLIENT-{Environment.CurrentManagedThreadId}] Client closed connection.");
                break;
            }
            
            string messageString = Encoding.ASCII.GetString(message, 0, readAsync).ToLower();
            
            if (messageString == CloseCommand)
            {
                HandleCloseCommand();
                break;
            }
            
            if (messageString == GetCountCommand)
            {
                try
                {
                    await HandleGetCountCommand(clientStream);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    break;
                }
            }
            else if (messageString.Contains(GetInfoCommand) == true)
            {
                if (await HandleGetInfoCommand(messageString, clientStream))
                {
                    break;
                }
            }
            else
            {
                HandleUnknownCommand(messageString, clientStream);
            }
        }
        
        Console.WriteLine($"[CLIENT-{Environment.CurrentManagedThreadId}] Closing connection.");
        tcpClient.Close();
    }

    #region CommandHandlers

    private static async Task HandleGetCountCommand(NetworkStream clientStream)
    {
        byte[] length = Encoding.ASCII.GetBytes(Program.LobbyInfos.Count.ToString());
        await clientStream.WriteAsync(length);

        Console.WriteLine($"[CLIENT-{Environment.CurrentManagedThreadId}] Sent lobbies count: {Program.LobbyInfos.Count}.");
    }

    private static async Task<bool> HandleGetInfoCommand(string messageString, NetworkStream clientStream)
    {
        int indexOfIndex = messageString.IndexOf(' ');
        indexOfIndex++;

        if (indexOfIndex == -1)
        {
            Console.WriteLine($"[CLIENT-{Environment.CurrentManagedThreadId}] Can`t find index of client state array.");
            return true;
        }

        string indexString = messageString[indexOfIndex..];

        if (int.TryParse(indexString, out int index) == false)
        {
            Console.WriteLine("[CLIENT-{Environment.CurrentManagedThreadId}] Can`t parse index of client state array.");
            return true;
        }

        if (index >= Program.LobbyInfos.Count)
        {
            Console.WriteLine("[CLIENT-{Environment.CurrentManagedThreadId}] Index of client state array is out of range.");
            return true;
        }

        try
        {
            LobbyInfo clientState = Program.LobbyInfos[index];

            string stateJson = JsonConvert.SerializeObject(clientState);
            byte[] reply = Encoding.ASCII.GetBytes(stateJson);

            await clientStream.WriteAsync(reply);
            Console.WriteLine($"[CLIENT-{Environment.CurrentManagedThreadId}] Sent {reply.Length} bytes.");
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return true;
        }

        return false;
    }

    private static void HandleCloseCommand()
    {
        Console.WriteLine($"[CLIENT-{Environment.CurrentManagedThreadId}] Client closed connection.");
    }
    
    private static async void HandleUnknownCommand(string messageString, NetworkStream clientStream)
    {
        byte[] length = "Unknown command."u8.ToArray();
        await clientStream.WriteAsync(length);
        
        Console.WriteLine($"[CLIENT-{Environment.CurrentManagedThreadId}] Unknown command: {messageString}");
    }

    #endregion
}