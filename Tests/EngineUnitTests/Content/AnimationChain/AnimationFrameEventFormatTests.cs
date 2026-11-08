using FlatRedBall.Content.AnimationChain;
using FlatRedBall.Graphics.Animation;
using Shouldly;

namespace EngineUnitTests.Content.AnimationChain;

/// <summary>
/// A frame's named events round-trip through .achx and .achj and survive cloning, so a sequence can raise them. A Sprite
/// ignores them.
/// </summary>
public class AnimationFrameEventFormatTests : IDisposable
{
    readonly string _directory = Path.Combine(Path.GetTempPath(), "events-" + Guid.NewGuid());

    public AnimationFrameEventFormatTests()
    {
        Directory.CreateDirectory(_directory);
    }

    public void Dispose()
    {
        Directory.Delete(_directory, recursive: true);
    }

    const string Achx = """
        <?xml version="1.0"?>
        <AnimationChainArraySave>
          <AnimationChain>
            <Name>A</Name>
            <Frame>
              <FrameLength>0.1</FrameLength>
              <Events>
                <Event><Name>Footstep</Name></Event>
                <Event><Name>Spawn</Name><Data>meteors</Data></Event>
              </Events>
            </Frame>
            <Frame><FrameLength>0.1</FrameLength></Frame>
          </AnimationChain>
        </AnimationChainArraySave>
        """;

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void FromFile_Achx_ReadsEvents(bool manual)
    {
        string path = Path.Combine(_directory, "a.achx");
        File.WriteAllText(path, Achx);

        bool original = AnimationChainListSave.ManualDeserialization;
        AnimationChainListSave.ManualDeserialization = manual;
        try
        {
            var frames = AnimationChainListSave.FromFile(path).AnimationChains[0].Frames;

            frames[0].Events.Count.ShouldBe(2);
            frames[0].Events[0].Name.ShouldBe("Footstep");
            frames[0].Events[0].Data.ShouldBeNull();
            frames[0].Events[1].Name.ShouldBe("Spawn");
            frames[0].Events[1].Data.ShouldBe("meteors");
            frames[1].Events.ShouldBeEmpty();
        }
        finally
        {
            AnimationChainListSave.ManualDeserialization = original;
        }
    }

    [Fact]
    public void FromFile_Achj_ReadsEvents()
    {
        string path = Path.Combine(_directory, "a.achj");
        File.WriteAllText(path, """
            { "animationChains": [ { "name": "A", "frames": [
              { "frameLength": 0.1, "events": [ { "name": "Footstep" }, { "name": "Spawn", "data": "meteors" } ] },
              { "frameLength": 0.1 } ] } ] }
            """);

        var frames = AnimationChainListSave.FromFile(path).AnimationChains[0].Frames;

        frames[0].Events.Select(item => (item.Name, item.Data)).ShouldBe(new[] { ("Footstep", (string?)null), ("Spawn", "meteors") });
        frames[1].Events.ShouldBeEmpty();
    }

    [Fact]
    public void Save_Achx_WritesEventsOnlyWhenPresent_AndRoundTrips()
    {
        var list = new AnimationChainListSave { FileRelativeTextures = false };
        var chain = new AnimationChainSave { Name = "A" };
        var withEvent = new AnimationFrameSave { FrameLength = 0.1f };
        withEvent.Events.Add(new AnimationFrameEvent { Name = "Spawn", Data = "meteors" });
        chain.Frames.Add(withEvent);
        chain.Frames.Add(new AnimationFrameSave { FrameLength = 0.1f });
        list.AnimationChains.Add(chain);
        string path = Path.Combine(_directory, "saved.achx");

        list.Save(path);

        File.ReadAllText(path).Split("<Events>").Length.ShouldBe(2);
        var frames = AnimationChainListSave.FromFile(path).AnimationChains[0].Frames;
        frames[0].Events.Single().Name.ShouldBe("Spawn");
        frames[0].Events.Single().Data.ShouldBe("meteors");
        frames[1].Events.ShouldBeEmpty();
    }

    [Fact]
    public void ToAnimationFrame_AndBack_CarriesTheEvents()
    {
        var save = new AnimationFrameSave { FrameLength = 0.1f };
        save.Events.Add(new AnimationFrameEvent { Name = "Spawn", Data = "x" });

        var frame = save.ToAnimationFrame(null, loadTexture: false);

        frame.Events.Single().Name.ShouldBe("Spawn");
        frame.Events.Single().Data.ShouldBe("x");
    }

    [Fact]
    public void Clone_GivesTheCloneItsOwnEvents()
    {
        var frame = new AnimationFrame();
        frame.Events.Add(new AnimationFrameEvent { Name = "Spawn" });

        var clone = frame.Clone();
        clone.Events.Add(new AnimationFrameEvent { Name = "Other" });

        clone.Events.Select(item => item.Name).ShouldBe(new[] { "Spawn", "Other" });
        frame.Events.Select(item => item.Name).ShouldBe(new[] { "Spawn" });
    }
}
