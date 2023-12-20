using System.Net;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json;
using SDT;

namespace SDT;

public class ServerHandler
{
    private const uint BufferSize = 512;

    private readonly string _ipAddress;
    private readonly int _port;

    public ServerHandler(string ipAddress, int port)
    {
        _ipAddress = ipAddress;
        _port = port;
    }

    public async void Start()
    {
        TcpListener server = null;
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
    
    private async void Handle(object obj)
    {
        TcpClient tcpClient;
        try
        {
            tcpClient = (TcpClient)obj;
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
                await clientStream.WriteAsync("check"u8.ToArray().AsMemory(0, 5));
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                throw;
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

            if (clientMessage == "close")
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

            Console.WriteLine($"[SERVER-{Environment.CurrentManagedThreadId}] Checking if SnaP Server port is forwarded...");
            if (await CheckUdpPort(lobbyInfo.PublicIpAddress, lobbyInfo.Port) == false)
            {
                Console.WriteLine($"[SERVER-{Environment.CurrentManagedThreadId}] Port Forward check failed. Closing connection and Removing from list.");
                break;
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

    private async Task<bool> CheckUdpPort(string ip, ushort port)
    {
        if (Program.PublicIpAddress == ip)
        {
            Console.WriteLine($"[SERVER-{Environment.CurrentManagedThreadId}] Seems like we are on the same network. Skipping port check.");
            return true;
        }

        UdpClient udpServer = new(ip, port);

        CancellationTokenSource cancellationToken = new(10000);

        try
        {
            await udpServer.ReceiveAsync(cancellationToken.Token);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
}