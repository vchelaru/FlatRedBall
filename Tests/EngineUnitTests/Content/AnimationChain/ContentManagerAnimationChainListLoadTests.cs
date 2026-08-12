using System;
using System.IO;
using FlatRedBall.Graphics.Animation;
using Shouldly;

namespace EngineUnitTests.Content.AnimationChain;

class NullServiceProvider : IServiceProvider
{
    public object? GetService(Type serviceType) => null;
}

public class ContentManagerAnimationChainListLoadTests
{
    const string AchxContents =
        "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" +
        "<AnimationChainArraySave>\n" +
        "  <AnimationChain>\n" +
        "    <Name>Animation1</Name>\n" +
        "  </AnimationChain>\n" +
        "</AnimationChainArraySave>\n";

    [Fact]
    public void Load_AnimationChainList_ShouldRegisterItAsDisposableOnly()
    {
        var achxPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".achx");
        File.WriteAllText(achxPath, AchxContents);

        try
        {
            var contentManager = new FlatRedBall.Content.ContentManager("Test", new NullServiceProvider());

            var loaded = contentManager.Load<AnimationChainList>(achxPath);

            // AnimationChainList implements IDisposable, so it should be filed under the disposable
            // dictionary only. FlatRedBallServices.ReplaceTexture (and anything else that walks
            // DisposableObjects) depends on this to find achx-loaded animation chains. Filing it in
            // the non-disposable dictionary too is a wasted, confusing duplicate registration - assert
            // on ToString()'s counts (the only public window into both dictionaries at once) rather
            // than the key format, since disposable/non-disposable keys are built differently and a
            // key-based lookup can't tell "wrong dictionary" apart from "wrong key".
            contentManager.DisposableObjects.ShouldContain(loaded);
            contentManager.ToString().ShouldBe("Test with 1 disposables, 0 non disposables, 0 assets");
        }
        finally
        {
            File.Delete(achxPath);
        }
    }
}
