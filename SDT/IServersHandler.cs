namespace SDT;

public interface IServersHandler
{
    Task Run();
    Task Stop();
}