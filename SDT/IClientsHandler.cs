namespace SDT;

public interface IClientsHandler
{
    Task Run();
    Task Stop();
}