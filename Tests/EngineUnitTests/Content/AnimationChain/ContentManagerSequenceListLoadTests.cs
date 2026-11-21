using System;
using System.IO;
using FlatRedBall.Graphics.Animation;
using Shouldly;

namespace EngineUnitTests.Content.AnimationChain;

public class ContentManagerSequenceListLoadTests
{
    const string SeqjContents = """
        {
          "version": 1,
          "sources": [ "missing.achj" ],
          "sequences": [ { "name": "Idle", "loop": true, "tracks": [] } ]
        }
        """;

    [Fact]
    public void Load_SequenceList_ReadsTheSeqjAndCachesIt()
    {
        var seqjPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".seqj");
        File.WriteAllText(seqjPath, SeqjContents);

        try
        {
            var contentManager = new FlatRedBall.Content.ContentManager("Test", new NullServiceProvider());

            var first = contentManager.Load<SequenceList>(seqjPath);
            var second = contentManager.Load<SequenceList>(seqjPath);

            first.Find("Idle").ShouldNotBeNull();
            second.ShouldBeSameAs(first);
            contentManager.ToString().ShouldBe("Test with 0 disposables, 1 non disposables, 0 assets");
        }
        finally
        {
            File.Delete(seqjPath);
        }
    }
}
