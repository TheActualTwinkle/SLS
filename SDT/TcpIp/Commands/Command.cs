namespace SDT.TcpIp.Commands;

public readonly struct Command(CommandType? type, object? content = null)
{
    public CommandType? Type { get; } = type;
    public object? Content { get; } = content;
}