using System.Threading.Tasks;
using GameCommunicationPlugin.GlueControl.Dtos;
using GlueUnitTests.TestSupport;
using Shouldly;
using Xunit;

namespace GlueUnitTests.Projects;

/// <summary>
/// The socket-level tests use a fake game, so they can only prove Glue reads a reply correctly - not that
/// a real game sends the reply they assume. These drive an actual running game process to pin what it
/// really answers, which is the half of issue #2051 that no unit test can reach (the game-side
/// GameConnectionManager is an embedded resource that only compiles inside a game project).
///
/// Tagged "LiveGame" like <see cref="LiveGameProcessTests"/> - launches a real MonoGame window, so it is
/// developer-machine-only: `dotnet test ... --filter "Category=LiveGame"`.
/// </summary>
[Trait("Category", "LiveGame")]
public class LiveGameCommandResponseTests
{
    static Task<LiveGameProcess> StartAsync() => LiveGameProcess.StartAsync(
        "Samples/EditorTest1",
        csprojRelativeToProjectRoot: "EditorTest1/EditorTest1.csproj",
        exeRelativeToProjectRoot: "EditorTest1/bin/Debug/net9.0/EditorTest1.exe");

    /// <summary>
    /// SetBorderlessDto's handler returns void, so the game answers with an empty body. That has to read as
    /// success: it is how the editor decides the game is ready to be embedded, and the same shape covers
    /// most of live edit's commands.
    /// </summary>
    [StaFact]
    public async Task VoidHandlerCommand_AgainstARunningGame_ReportsSuccess()
    {
        GlueTestBootstrap.EnsureGameProjectPluginsRegistered();

        using var game = await StartAsync();

        var response = await game.Send(new SetBorderlessDto { IsBorderless = true });

        response.Succeeded.ShouldBeTrue(
            "the game handled the command; a void handler having nothing to return is not a failure");
    }

    /// <summary>
    /// The other side of the same reply, and the reason the old "did the game send a non-empty string?"
    /// check could not tell a handled command from a dropped one: a real game handling a void command sends
    /// back nothing at all.
    /// </summary>
    [StaFact]
    public async Task VoidHandlerCommand_AgainstARunningGame_SendsBackNoPayload()
    {
        GlueTestBootstrap.EnsureGameProjectPluginsRegistered();

        using var game = await StartAsync();

        var response = await game.Send(new SetBorderlessDto { IsBorderless = true });

        response.Data.ShouldBeNullOrEmpty();
    }

    /// <summary>
    /// A command whose handler does return data still comes back as data, so the classification above did
    /// not swallow real payloads.
    /// </summary>
    [StaFact]
    public async Task CommandWithAResponse_AgainstARunningGame_CarriesThePayload()
    {
        GlueTestBootstrap.EnsureGameProjectPluginsRegistered();

        using var game = await StartAsync();

        var response = await game.Send(new GetCameraPosition());

        response.Succeeded.ShouldBeTrue();
        response.Data.ShouldNotBeNullOrEmpty();
    }
}
