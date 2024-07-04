using Newtonsoft.Json;

namespace SLS.TcpIp.Commands;

public static class CommandParser
{
    public static Command? FromJson(string commandJson)
    {
        try
        {
            return JsonConvert.DeserializeObject<Command>(commandJson);
        }
        catch (Exception e)
        {
            Console.WriteLine("Can`t parse command: " + e.Message);
            return null;
        }
    }

    public static string ToJson(Command command)
    {
        return JsonConvert.SerializeObject(command);
    }
}