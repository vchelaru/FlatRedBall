using FlatRedBall;
using FlatRedBall.Content.AnimationChain;
using FlatRedBall.Graphics.Animation;
using Shouldly;

namespace EngineUnitTests.Content.AnimationChain;

/// <summary>
/// A chain's Loop flag is data only: it round-trips through every format, and the Sprite keeps ignoring it (it always
/// loops). The sequence player is what reads it.
/// </summary>
public class AnimationChainLoopTests : IDisposable
{
    readonly string _directory = Path.Combine(Path.GetTempPath(), "loop-" + Guid.NewGuid());

    public AnimationChainLoopTests()
    {
        Directory.CreateDirectory(_directory);
    }

    public void Dispose()
    {
        Directory.Delete(_directory, recursive: true);
    }

    [Fact]
    public void Loop_DefaultsToTrue_OnRuntimeChainAndSave()
    {
        new FlatRedBall.Graphics.Animation.AnimationChain().Loop.ShouldBeTrue();
        new AnimationChainSave().Loop.ShouldBeTrue();
    }

    [Fact]
    public void Clone_CopiesLoop()
    {
        var chain = new FlatRedBall.Graphics.Animation.AnimationChain { Loop = false };

        chain.Clone().Loop.ShouldBeFalse();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void FromFile_Achx_ReadsLoop_AndDefaultsToTrue(bool manual)
    {
        string path = Path.Combine(_directory, "a.achx");
        File.WriteAllText(path, """
            <?xml version="1.0"?>
            <AnimationChainArraySave>
              <AnimationChain><Name>A</Name><Loop>false</Loop></AnimationChain>
              <AnimationChain><Name>B</Name></AnimationChain>
            </AnimationChainArraySave>
            """);

        bool original = AnimationChainListSave.ManualDeserialization;
        AnimationChainListSave.ManualDeserialization = manual;
        try
        {
            var chains = AnimationChainListSave.FromFile(path).AnimationChains;

            chains[0].Loop.ShouldBeFalse();
            chains[1].Loop.ShouldBeTrue();
        }
        finally
        {
            AnimationChainListSave.ManualDeserialization = original;
        }
    }

    [Fact]
    public void FromFile_Achj_ReadsLoop_AndDefaultsToTrue()
    {
        string path = Path.Combine(_directory, "a.achj");
        File.WriteAllText(path, """
            { "animationChains": [ { "name": "A", "loop": false }, { "name": "B" } ] }
            """);

        var chains = AnimationChainListSave.FromFile(path).AnimationChains;

        chains[0].Loop.ShouldBeFalse();
        chains[1].Loop.ShouldBeTrue();
    }

    [Fact]
    public void Save_Achx_WritesLoopOnlyWhenFalse_AndRoundTrips()
    {
        var list = new AnimationChainListSave { FileRelativeTextures = false };
        list.AnimationChains.Add(new AnimationChainSave { Name = "A", Loop = false });
        list.AnimationChains.Add(new AnimationChainSave { Name = "B" });
        string path = Path.Combine(_directory, "saved.achx");

        list.Save(path);

        File.ReadAllText(path).Split("<Loop>").Length.ShouldBe(2);
        var loaded = AnimationChainListSave.FromFile(path).AnimationChains;
        loaded[0].Loop.ShouldBeFalse();
        loaded[1].Loop.ShouldBeTrue();
    }

    [Fact]
    public void RuntimeConversion_RoundTripsLoop()
    {
        var save = new AnimationChainSave { Name = "A", Loop = false };

        var runtime = save.ToAnimationChain(null, TimeMeasurementUnit.Second);
        var back = AnimationChainListSave.FromAnimationChainList(
            new AnimationChainList { runtime }).AnimationChains[0];

        runtime.Loop.ShouldBeFalse();
        back.Loop.ShouldBeFalse();
    }

    [Fact]
    public void Sprite_StillLoopsWhenTheChainsLoopIsFalse()
    {
        // Existing FRB2-authored files with Loop off must keep looping in FRB1 sprites.
        var chain = new FlatRedBall.Graphics.Animation.AnimationChain { Name = "c", Loop = false };
        chain.Add(new AnimationFrame { FrameLength = 1 });
        chain.Add(new AnimationFrame { FrameLength = 1 });
        var sprite = new FlatRedBall.Sprite { AnimationChains = new AnimationChainList { chain } };
        sprite.CurrentChainIndex = 0;

        sprite.TimeIntoAnimation = 2.5;

        sprite.CurrentFrameIndex.ShouldBe(0);
    }
}
