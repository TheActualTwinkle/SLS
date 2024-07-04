using SDT.TcpIp.Commands;

namespace SDT.Tests.TcpIp;

[TestFixture]
public class CommandsTests
{
    [Test]
    public void ParseAndUnparse()
    {
        // Create expected command
        LobbyDto randomLobbyDto = Tools.GetRandomLobbyInfo();
        Command expectedCommand = new(CommandType.PostLobbyInfo, randomLobbyDto);
        string? expectedJson = CommandParser.ToJson(expectedCommand);

        // Unparse actual command
        Command? actualCommand = CommandParser.FromJson(expectedJson!);

        // Parse actual command
        string? actualJson = CommandParser.ToJson(actualCommand!.Value);

        // Compare
        Assert.That(actualJson, Is.EqualTo(expectedJson));
    }

    [Test]
    public void ParseInvalid()
    {
        const string invalidJson = "invalid json";

        Command? actualCommand = CommandParser.FromJson(invalidJson);

        Assert.That(actualCommand, Is.Null);
    }
}