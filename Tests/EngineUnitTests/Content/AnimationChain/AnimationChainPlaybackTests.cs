using EngineUnitTests.TestSupport;
using FlatRedBall;
using FlatRedBall.Content.AnimationChain;
using FlatRedBall.Graphics.Animation;
using Shouldly;

namespace EngineUnitTests.Content.AnimationChain;

[Collection(nameof(EngineStatefulTestCollection))]
public class AnimationChainPlaybackTests : IDisposable
{
    readonly string _directory = Path.Combine(Path.GetTempPath(), "achx-playback-" + Guid.NewGuid());

    public AnimationChainPlaybackTests()
    {
        Directory.CreateDirectory(_directory);
    }

    public void Dispose()
    {
        Directory.Delete(_directory, recursive: true);
    }

    // Three frames of 1s, 2s, 1s: T = 4.
    static FlatRedBall.Graphics.Animation.AnimationChain CreateChain(bool pingPong = false, bool reverse = false)
    {
        var chain = new FlatRedBall.Graphics.Animation.AnimationChain { PingPong = pingPong, Reverse = reverse };
        foreach (float length in new[] { 1f, 2f, 1f })
        {
            chain.Add(new AnimationFrame { FrameLength = length });
        }
        return chain;
    }

    static int FrameAt(FlatRedBall.Graphics.Animation.AnimationChain chain, double time)
    {
        chain.GetPlaybackFrame(time, out int frameIndex, out _);
        return frameIndex;
    }

    [Fact]
    public void PlaybackLength_PingPong_ShouldBeTwiceTotalLength()
    {
        CreateChain().PlaybackLength.ShouldBe(4f);
        CreateChain(pingPong: true).PlaybackLength.ShouldBe(8f);
        CreateChain(reverse: true).PlaybackLength.ShouldBe(4f);
    }

    [Theory]
    [InlineData(0.0, 0)]
    [InlineData(0.5, 0)]
    [InlineData(1.0, 1)]
    [InlineData(2.9, 1)]
    [InlineData(3.0, 2)]
    [InlineData(3.9, 2)]
    public void GetPlaybackFrame_Plain_ShouldPlayForward(double time, int expected)
    {
        FrameAt(CreateChain(), time).ShouldBe(expected);
    }

    [Theory]
    [InlineData(0.0, 2)]
    [InlineData(0.9, 2)]
    [InlineData(1.0, 1)]
    [InlineData(2.9, 1)]
    [InlineData(3.0, 0)]
    [InlineData(3.9, 0)]
    public void GetPlaybackFrame_Reverse_ShouldPlayMirrored(double time, int expected)
    {
        FrameAt(CreateChain(reverse: true), time).ShouldBe(expected);
    }

    [Theory]
    [InlineData(0.0, 0)]
    [InlineData(3.9, 2)]
    [InlineData(4.0, 2)] // the end frame shows double across the turnaround
    [InlineData(4.9, 2)]
    [InlineData(5.0, 1)]
    [InlineData(6.9, 1)]
    [InlineData(7.0, 0)]
    [InlineData(7.9, 0)]
    public void GetPlaybackFrame_PingPong_ShouldPlayForwardThenMirrored(double time, int expected)
    {
        FrameAt(CreateChain(pingPong: true), time).ShouldBe(expected);
    }

    [Theory]
    [InlineData(0.0, 2)]
    [InlineData(1.0, 1)]
    [InlineData(3.9, 0)]
    [InlineData(4.0, 0)] // the first frame shows double across the turnaround
    [InlineData(4.9, 0)]
    [InlineData(5.0, 1)]
    [InlineData(7.0, 2)]
    [InlineData(7.9, 2)]
    public void GetPlaybackFrame_PingPongReverse_ShouldPlayMirroredFirst(double time, int expected)
    {
        FrameAt(CreateChain(pingPong: true, reverse: true), time).ShouldBe(expected);
    }

    // Pins the boundary convention shared with plain chains: a time exactly on a frame boundary shows the frame being
    // entered, in both directions. FRB2 and Gum match this (#2400).
    [Fact]
    public void GetPlaybackFrame_ExactBoundary_ShouldEnterTheNextFrameInPlaybackOrder()
    {
        // Frames are 0.5s, binary-exact so the boundaries are exact.
        var chain = new FlatRedBall.Graphics.Animation.AnimationChain { PingPong = true };
        chain.Add(new AnimationFrame { FrameLength = 0.5f });
        chain.Add(new AnimationFrame { FrameLength = 0.5f });

        FrameAt(chain, 0.5).ShouldBe(1);  // forward: A -> B
        FrameAt(chain, 1.0).ShouldBe(1);  // turnaround: B stays, now mirrored
        FrameAt(chain, 1.5).ShouldBe(0);  // backward: B -> A

        chain.PingPong = false;
        chain.Reverse = true;
        FrameAt(chain, 0.0).ShouldBe(1);  // reverse starts on the last frame
        FrameAt(chain, 0.5).ShouldBe(0);  // backward: B -> A
    }

    [Fact]
    public void GetPlaybackFrame_Reverse_ShouldReportForwardElapsedTime()
    {
        var chain = CreateChain(reverse: true);

        // 0.25s into the mirrored timeline is 0.25s before the end of the last (1s) frame.
        chain.GetPlaybackFrame(0.25, out int frameIndex, out double elapsed);

        frameIndex.ShouldBe(2);
        elapsed.ShouldBe(0.75, 1e-9);
    }

    [Fact]
    public void Clone_ShouldCopyFlags()
    {
        var clone = CreateChain(pingPong: true, reverse: true).Clone();

        clone.PingPong.ShouldBeTrue();
        clone.Reverse.ShouldBeTrue();
    }

    [Fact]
    public void Sprite_PingPong_ShouldShowFramesAcrossTheCycle()
    {
        var sprite = new Sprite();
        var chain = CreateChain(pingPong: true);
        sprite.AnimationChains = new AnimationChainList { chain };
        sprite.CurrentChainIndex = 0;

        sprite.TimeIntoAnimation = 5.5;
        sprite.CurrentFrameIndex.ShouldBe(1);

        sprite.TimeIntoAnimation = 7.5;
        sprite.CurrentFrameIndex.ShouldBe(0);
    }

    [Fact]
    public void Sprite_Reverse_ShouldStartOnLastFrame()
    {
        var sprite = new Sprite();
        sprite.AnimationChains = new AnimationChainList { CreateChain(reverse: true) };
        sprite.CurrentChainIndex = 0;

        sprite.TimeIntoAnimation = 0;

        sprite.CurrentFrameIndex.ShouldBe(2);
    }

    [Fact]
    public void FromFile_Achx_ShouldReadFlags()
    {
        string path = Path.Combine(_directory, "a.achx");
        File.WriteAllText(path, """
            <?xml version="1.0"?>
            <AnimationChainArraySave>
              <AnimationChain><Name>A</Name><PingPong>true</PingPong><Reverse>true</Reverse></AnimationChain>
              <AnimationChain><Name>B</Name></AnimationChain>
            </AnimationChainArraySave>
            """);

        foreach (bool manual in new[] { true, false })
        {
            bool original = AnimationChainListSave.ManualDeserialization;
            AnimationChainListSave.ManualDeserialization = manual;
            try
            {
                var chains = AnimationChainListSave.FromFile(path).AnimationChains;

                chains[0].PingPong.ShouldBeTrue();
                chains[0].Reverse.ShouldBeTrue();
                chains[1].PingPong.ShouldBeFalse();
                chains[1].Reverse.ShouldBeFalse();
            }
            finally
            {
                AnimationChainListSave.ManualDeserialization = original;
            }
        }
    }

    [Fact]
    public void FromFile_Achj_ShouldReadFlags()
    {
        string path = Path.Combine(_directory, "a.achj");
        File.WriteAllText(path, """
            { "animationChains": [ { "name": "A", "pingPong": true, "reverse": true }, { "name": "B" } ] }
            """);

        var chains = AnimationChainListSave.FromFile(path).AnimationChains;

        chains[0].PingPong.ShouldBeTrue();
        chains[0].Reverse.ShouldBeTrue();
        chains[1].PingPong.ShouldBeFalse();
        chains[1].Reverse.ShouldBeFalse();
    }

    [Fact]
    public void Save_Achx_ShouldRoundTripFlagsAndOmitThemWhenFalse()
    {
        var list = new AnimationChainListSave { FileRelativeTextures = false };
        list.AnimationChains.Add(new AnimationChainSave { Name = "A", PingPong = true, Reverse = true });
        list.AnimationChains.Add(new AnimationChainSave { Name = "B" });
        string path = Path.Combine(_directory, "saved.achx");

        list.Save(path);

        string xml = File.ReadAllText(path);
        xml.Split("<PingPong>").Length.ShouldBe(2);
        xml.Split("<Reverse>").Length.ShouldBe(2);

        var loaded = AnimationChainListSave.FromFile(path).AnimationChains;
        loaded[0].PingPong.ShouldBeTrue();
        loaded[0].Reverse.ShouldBeTrue();
        loaded[1].PingPong.ShouldBeFalse();
        loaded[1].Reverse.ShouldBeFalse();
    }

    [Fact]
    public void RuntimeConversion_ShouldRoundTripFlags()
    {
        var save = new AnimationChainSave { Name = "A", PingPong = true, Reverse = true };

        var runtime = save.ToAnimationChain(null, TimeMeasurementUnit.Second);
        var back = AnimationChainListSave.FromAnimationChainList(new AnimationChainList { runtime }).AnimationChains[0];

        runtime.PingPong.ShouldBeTrue();
        runtime.Reverse.ShouldBeTrue();
        back.PingPong.ShouldBeTrue();
        back.Reverse.ShouldBeTrue();
    }
}
