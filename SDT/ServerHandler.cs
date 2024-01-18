using System.Net;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json;

namespace SDT;

public class ServerHandler
{
    private const uint BufferSize = 512;

    private const string CheckCommand = "check";
    private const string CloseCommand = "close";

    private readonly string _ipAddress;
    private readonly int _port;

    public ServerHandler(string ipAddress, int port)
    {
        _ipAddress = ipAddress;
        _port = port;
    }

    public async void Start()
    {
        TcpListener server = null!;
        try
        {
            IPAddress ipAddress = IPAddress.Parse(_ipAddress);

            // TcpListener is used to wait for a connection from a client.
            server = new TcpListener(ipAddress, _port);

            // Start listening for client requests.
            server.Start();

            Console.WriteLine($"[SERVER] Server for SnaP SERVERS started at {_ipAddress}:{_port}. Waiting for connections...");

            while (true)
            {
                // Blocks until a client has connected to the server.
                TcpClient client = await server.AcceptTcpClientAsync();
                Console.WriteLine("[SERVER] Client connected!");

                // Create a thread to handle the client communication.
                Thread clientThread = new(Handle);
                clientThread.Start(client); 
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
        finally
        {
            // Stop listening for new clients.
            server?.Stop();
        }
    }
    
    private async void Handle(object? obj)
    {
        TcpClient tcpClient;
        try
        {
            tcpClient = (TcpClient)obj!;
        }
        catch (Exception e)
        {
            Console.WriteLine($"[SERVER-{Environment.CurrentManagedThreadId}] Error on parse obj to TcpClient. {e}");
            throw;
        }
        
        NetworkStream clientStream = tcpClient.GetStream();

        // Buffer to store the response bytes.
        var message = new byte[BufferSize];

        LobbyInfo lobbyInfo = new(string.Empty, 0, 0, 0, "Initializing...");

        while (true)
        {
            int bytesRead;
            
            // If cant write to stream the exception will be raised - client will be closed.
            try
            {
                await clientStream.WriteAsync(Encoding.ASCII.GetBytes(CheckCommand).ToArray().AsMemory(0, CheckCommand.Length));
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                break;
            }

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
                Console.WriteLine($"[SERVER-{Environment.CurrentManagedThreadId}] Client closed connection.");
                break;
            }

            // Convert the bytes to a string and display it.
            string clientMessage = Encoding.ASCII.GetString(message, 0, bytesRead);
            Console.WriteLine($"[SERVER-{Environment.CurrentManagedThreadId}] Received: {clientMessage}");

            if (clientMessage == CloseCommand)
            {
                break;
            }
            
            // Parsing json to LobbyInfo.
            try
            {
                LobbyInfo lobbyInfoCopy = JsonConvert.DeserializeObject<LobbyInfo>(clientMessage)!;
                lobbyInfoCopy.Deconstruct(ref lobbyInfo);
            }
            catch (Exception e)
            {
                Console.WriteLine($"[SERVER-{Environment.CurrentManagedThreadId}] Can`t deserialize json to LobbyInfo. " + e);
            }

            if (Program.LobbyInfos.Contains(lobbyInfo) == true)
            {
                continue;
            }

            Console.WriteLine($"[SERVER-{Environment.CurrentManagedThreadId}] Added new lobby info.");
            Program.LobbyInfos.Add(lobbyInfo);
        }

        Program.LobbyInfos.Remove(lobbyInfo);

        Console.WriteLine($"[SERVER-{Environment.CurrentManagedThreadId}] Closing connection.");
        tcpClient.Close();
    }
}