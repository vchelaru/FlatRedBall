using System;
using System.Threading.Tasks;
using FlatRedBall.Glue.Managers;
using GameCommunicationPlugin.GlueControl.Dtos;
using GameCommunicationPlugin.GlueControl.Managers;
using GlueUnitTests.TestSupport;
using Newtonsoft.Json;
using OfficialPlugins.Compiler.CommandReceiving;
using Shouldly;
using Xunit;

namespace GlueUnitTests.GlueControlTests;

/// <summary>
/// Investigation for GitHub issue #2139: a screen recording showed the running game sending selection
/// commands back to Glue that Glue never reacted to. The wire path for "the game selected an object" is
/// a GlueStateDto with SetPropertyName == "CurrentNamedObjectSaves", applied by
/// CommandReceiver.HandleFacadeCommand via reflection (property.SetValue). That call runs on whatever
/// thread processes the incoming socket message - TaskManager's own background STA thread in
/// production, not the WPF dispatcher thread that owns the tree/tab view models. Explorer-driven
/// selection works because it already starts on the UI thread; game-driven selection does not, and
/// nothing marshals it there (contrast with PluginManager.CallMethodOnPlugin, which defaults
/// doOnUiThread: true for exactly this reason).
/// </summary>
public class GameSelectionUiThreadTests
{
    private sealed class RecordingUiThreadMarshaller : IUiThreadMarshaller
    {
        public int InvokeCount;
        public void Invoke(Action action) { InvokeCount++; action(); }
        public T Invoke<T>(Func<T> func) { InvokeCount++; return func(); }
        public Task Invoke(Func<Task> func) { InvokeCount++; return func(); }
        public Task<T> Invoke<T>(Func<Task<T>> func) { InvokeCount++; return func(); }
        public void BeginInvoke(Action action) { InvokeCount++; action(); }
    }

    [Fact]
    public async Task HandleCommandsFromGame_ShouldMarshalPropertySetToUiThread_WhenGameSendsSelectionChange()
    {
        GlueTestBootstrap.EnsureInitialized();

        var originalMarshaller = TaskManager.UiThreadMarshaller;
        var recordingMarshaller = new RecordingUiThreadMarshaller();
        TaskManager.UiThreadMarshaller = recordingMarshaller;
        try
        {
            var refreshManager = new RefreshManager((_, _) => Task.FromResult(""), (_, _) => { });
            var sendingManager = new VariableSendingManager(refreshManager);
            var receiver = new CommandReceiver(refreshManager, sendingManager)
            {
                CompilerViewModel = CompilerLibrary.ViewModels.CompilerViewModel.Self
            };

            // An empty selection (the game deselecting everything) round-trips through Convert()
            // without needing a real loaded GlueProject/element to resolve references against.
            var dto = new GlueStateDto { SetPropertyName = "CurrentNamedObjectSaves" };
            dto.Parameters.Add(Array.Empty<object>());
            var message = $"{nameof(GlueStateDto)}:{JsonConvert.SerializeObject(dto)}";

            await receiver.HandleCommandsFromGame(message, 0);

            recordingMarshaller.InvokeCount.ShouldBeGreaterThan(0,
                "a GlueState property change that came from the game must be applied via the UI thread marshaller, the same as Explorer-driven selection");
        }
        finally
        {
            TaskManager.UiThreadMarshaller = originalMarshaller;
        }
    }
}
