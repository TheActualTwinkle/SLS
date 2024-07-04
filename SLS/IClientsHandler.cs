namespace SLS;

public interface IClientsHandler
{
    Task Run();
    Task Stop();
}