using System;
using System.IO;
using System.Runtime.Serialization;
using EngineUnitTests.TestSupport;
using FlatRedBall.Graphics.Animation;
using Microsoft.Xna.Framework.Graphics;
using Shouldly;

namespace EngineUnitTests.Graphics.Animation;

[Collection(nameof(EngineStatefulTestCollection))]
public class ReplaceTextureTests
{
    // FlatRedBallServices.ReplaceTexture is only ever exercised through a full MonoGame-backed game
    // in this repo (Tests/TestProjectDesktopNet6/Screens/ReloadContentScreen.cs), whose generated code
    // is gitignored and needs Glue + a real display to run. These tests cover it without a
    // GraphicsDevice: Texture2D only needs to support reference-equality and a settable Name for this
    // codepath, so an uninitialized instance stands in for a "real" loaded texture.
    static Texture2D FakeTexture() =>
        (Texture2D)FormatterServices.GetUninitializedObject(typeof(Texture2D));

    static string WriteTempAchxWithOneEmptyChain(string chainName)
    {
        var achxPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".achx");
        File.WriteAllText(achxPath,
            "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" +
            "<AnimationChainArraySave>\n" +
            "  <AnimationChain>\n" +
            $"    <Name>{chainName}</Name>\n" +
            "  </AnimationChain>\n" +
            "</AnimationChainArraySave>\n");
        return achxPath;
    }

    [Fact]
    public void ReplaceTexture_ShouldUpdateAnimationChainListLoadedIntoContentManager()
    {
        EngineTestBootstrap.EnsureInitialized();

        var achxPath = WriteTempAchxWithOneEmptyChain("Shadow");
        try
        {
            var oldTexture = FakeTexture();
            var newTexture = FakeTexture();

            var animationChainList = FlatRedBall.FlatRedBallServices.Load<AnimationChainList>(achxPath, "Global");
            animationChainList[0].Add(new AnimationFrame { Texture = oldTexture });

            FlatRedBall.FlatRedBallServices.ReplaceTexture(oldTexture, newTexture);

            animationChainList[0][0].Texture.ShouldBe(newTexture);
        }
        finally
        {
            File.Delete(achxPath);
        }
    }

    [Fact]
    public void ReplaceTexture_ShouldUpdateAnimationChainsOnALiveSprite()
    {
        // Regression test for the fix in commit 59b2d1ea6d ("ReplaceTexture automatically replaces
        // AnimationChains textures"): SpriteManager.ReplaceTexture walks every live sprite's own
        // AnimationChains and fixes frame textures directly, independent of ContentManager tracking.
        // This was previously uncovered by any test.
        EngineTestBootstrap.EnsureInitialized();

        var oldTexture = FakeTexture();
        var newTexture = FakeTexture();

        var sprite = FlatRedBall.SpriteManager.AddSprite(oldTexture);
        try
        {
            var animationChainList = new AnimationChainList();
            var chain = new FlatRedBall.Graphics.Animation.AnimationChain { Name = "Shadow" };
            chain.Add(new AnimationFrame { Texture = oldTexture });
            animationChainList.Add(chain);
            sprite.AnimationChains = animationChainList;

            FlatRedBall.FlatRedBallServices.ReplaceTexture(oldTexture, newTexture);

            sprite.AnimationChains[0][0].Texture.ShouldBe(newTexture);
        }
        finally
        {
            FlatRedBall.SpriteManager.RemoveSprite(sprite);
        }
    }
}
