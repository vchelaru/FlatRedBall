using System;
using System.Runtime.Serialization;
using FlatRedBall.Graphics.Animation;
using Microsoft.Xna.Framework.Graphics;
using Shouldly;

namespace EngineUnitTests.Graphics.Animation;

public class VerifyNoRemainingTextureReferencesTests
{
    // See ReplaceTextureTests.cs for why an uninitialized Texture2D stands in for a "real" one here:
    // this codepath only needs reference-equality and a settable Name, and this repo's CI excludes
    // GraphicsDevice-dependent tests as unreliable on hosted runners.
    static Texture2D FakeTexture() =>
        (Texture2D)FormatterServices.GetUninitializedObject(typeof(Texture2D));

    static bool s_initialized;
    static void EnsureInitialized()
    {
        // See ReplaceTextureTests.cs: FlatRedBallServices initialization is process-wide static state
        // that can only run once per process, so every test in this class shares one call.
        if (!s_initialized)
        {
            FlatRedBall.FlatRedBallServices.InitializeCommandLine();
            s_initialized = true;
        }
    }

    [Fact]
    public void ShouldThrow_WhenALiveSpriteTextureStillReferencesTheOldTexture()
    {
        EnsureInitialized();

        var oldTexture = FakeTexture();
        var sprite = FlatRedBall.SpriteManager.AddSprite(oldTexture);
        sprite.Name = "ShadowSprite";

        try
        {
            var exception = Should.Throw<InvalidOperationException>(
                () => FlatRedBall.FlatRedBallServices.VerifyNoRemainingTextureReferences(oldTexture));

            exception.Message.ShouldContain("ShadowSprite");
        }
        finally
        {
            FlatRedBall.SpriteManager.RemoveSprite(sprite);
        }
    }

    [Fact]
    public void ShouldThrow_WhenALiveSpriteAnimationChainFrameStillReferencesTheOldTexture()
    {
        EnsureInitialized();

        var oldTexture = FakeTexture();
        var newTexture = FakeTexture();

        // Give the sprite a *different* current texture so only the AnimationChains frame is stale -
        // isolates the AnimationChains check from the plain sprite.Texture check above.
        var sprite = FlatRedBall.SpriteManager.AddSprite(newTexture);
        sprite.Name = "ShadowSprite";

        var animationChainList = new AnimationChainList();
        var chain = new FlatRedBall.Graphics.Animation.AnimationChain { Name = "Shadow" };
        chain.Add(new AnimationFrame { Texture = oldTexture });
        animationChainList.Add(chain);
        sprite.AnimationChains = animationChainList;

        try
        {
            var exception = Should.Throw<InvalidOperationException>(
                () => FlatRedBall.FlatRedBallServices.VerifyNoRemainingTextureReferences(oldTexture));

            exception.Message.ShouldContain("ShadowSprite");
            exception.Message.ShouldContain("Shadow");
        }
        finally
        {
            FlatRedBall.SpriteManager.RemoveSprite(sprite);
        }
    }

    [Fact]
    public void ShouldNotThrow_WhenNoLiveSpriteReferencesTheOldTexture()
    {
        EnsureInitialized();

        var oldTexture = FakeTexture();
        var newTexture = FakeTexture();
        var sprite = FlatRedBall.SpriteManager.AddSprite(newTexture);

        try
        {
            Should.NotThrow(
                () => FlatRedBall.FlatRedBallServices.VerifyNoRemainingTextureReferences(oldTexture));
        }
        finally
        {
            FlatRedBall.SpriteManager.RemoveSprite(sprite);
        }
    }

    [Fact]
    public void FullReplaceTexturePipeline_ShouldNotThrow_ForALiveSpriteWithAnimationChains()
    {
        // The happy path through the actual public entry point (not just the diagnostic in isolation):
        // confirms the automatic DEBUG-time check added to ReplaceTexture doesn't false-positive once
        // SpriteManager.ReplaceTexture has done its job.
        EnsureInitialized();

        var oldTexture = FakeTexture();
        var newTexture = FakeTexture();
        var sprite = FlatRedBall.SpriteManager.AddSprite(oldTexture);

        var animationChainList = new AnimationChainList();
        var chain = new FlatRedBall.Graphics.Animation.AnimationChain { Name = "Shadow" };
        chain.Add(new AnimationFrame { Texture = oldTexture });
        animationChainList.Add(chain);
        sprite.AnimationChains = animationChainList;

        try
        {
            Should.NotThrow(() => FlatRedBall.FlatRedBallServices.ReplaceTexture(oldTexture, newTexture));

            sprite.Texture.ShouldBe(newTexture);
            sprite.AnimationChains[0][0].Texture.ShouldBe(newTexture);
        }
        finally
        {
            FlatRedBall.SpriteManager.RemoveSprite(sprite);
        }
    }
}
