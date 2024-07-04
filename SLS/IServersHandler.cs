namespace SLS;

public interface IServersHandler
{
    Task Run();
    Task Stop();
}