using FlatRedBall.Graphics.Animation;
using Shouldly;
using Xunit;

namespace EngineUnitTests.Sprites;

public class SpriteAnimationFrameTests
{
    [Fact]
    public void TimeIntoAnimation_WithAllZeroLengthFrames_ShouldNotHang()
    {
        var chain = new AnimationChain { Name = "ZeroLengthChain" };
        chain.Add(new AnimationFrame(texture: null, frameLength: 0f));
        chain.Add(new AnimationFrame(texture: null, frameLength: 0f));
        chain.Add(new AnimationFrame(texture: null, frameLength: 0f));

        var animationChains = new AnimationChainList { chain };

        var sprite = new FlatRedBall.Sprite
        {
            AnimationChains = animationChains,
            CurrentChain = chain
        };

        sprite.TimeIntoAnimation = 5.0;

        sprite.CurrentFrameIndex.ShouldBe(0);
    }
}
