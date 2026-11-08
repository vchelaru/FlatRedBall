using System.Text.Json;
using FlatRedBall.Content.AnimationChain;
using Shouldly;

namespace EngineUnitTests.Content.AnimationChain;

/// <summary>Reading a .seqj. Mirrors the FRB2 reader's rules: unknown fields are ignored, defaults match the spec.</summary>
public class SequenceListSaveTests
{
    const string Full = """
        {
          "version": 1,
          "sources": [ "body/body.achj", "fx/fx.achj" ],
          "sequences": [
            {
              "name": "Attack",
              "loop": true,
              "tracks": [
                {
                  "name": "Body", "x": 10.5, "y": -4,"flipHorizontal": true, "visible": false,
                  "chains": [
                    { "source": "body/body.achj", "chain": "Walk", "start": 0.25, "loops": 2.5 }
                  ]
                }
              ]
            }
          ]
        }
        """;

    [Fact]
    public void FromJsonString_ReadsEveryKnownField()
    {
        var save = SequenceListSave.FromJsonString(Full);

        save.Version.ShouldBe(1);
        save.Sources.ShouldBe(new[] { "body/body.achj", "fx/fx.achj" });
        var sequence = save.Sequences.Single();
        sequence.Name.ShouldBe("Attack");
        sequence.Loop.ShouldBeTrue();
        var track = sequence.Tracks.Single();
        track.Name.ShouldBe("Body");
        track.X.ShouldBe(10.5f);
        track.Y.ShouldBe(-4f);
        track.FlipHorizontal.ShouldBeTrue();
        track.Visible.ShouldBeFalse();
        var chain = track.Chains.Single();
        chain.Source.ShouldBe("body/body.achj");
        chain.Chain.ShouldBe("Walk");
        chain.Start.ShouldBe(0.25);
        chain.Loops.ShouldBe(2.5f);
    }

    [Fact]
    public void FromJsonString_MissingFields_UseSpecDefaults()
    {
        var save = SequenceListSave.FromJsonString("""
            { "sequences": [ { "name": "A", "tracks": [ { "name": "T", "chains": [ { "source": "s", "chain": "c" } ] } ] } ] }
            """);

        save.Version.ShouldBe(1);
        save.Sources.ShouldBeEmpty();
        var sequence = save.Sequences.Single();
        sequence.Loop.ShouldBeFalse();
        var track = sequence.Tracks.Single();
        (track.X, track.Y).ShouldBe((0f, 0f));
        track.FlipHorizontal.ShouldBeFalse();
        track.Visible.ShouldBeTrue();
        var chain = track.Chains.Single();
        chain.Start.ShouldBe(0);
        chain.Loops.ShouldBe(1f);
    }

    [Fact]
    public void FromJsonString_UnknownFieldsAtEveryLevel_AreIgnored()
    {
        var save = SequenceListSave.FromJsonString("""
            {
              "future": 1,
              "sequences": [ { "name": "A", "extra": [1, 2], "tracks": [ { "name": "T", "note": "x",
                "chains": [ { "source": "s", "chain": "c", "speed": 2 } ] } ] } ]
            }
            """);

        save.Sequences.Single().Tracks.Single().Chains.Single().Chain.ShouldBe("c");
    }

    [Fact]
    public void FromJsonString_LeadingByteOrderMark_IsAllowed()
    {
        SequenceListSave.FromJsonString("﻿" + Full).Sequences.Count.ShouldBe(1);
    }

    [Fact]
    public void FromJsonString_NewerVersion_StillLoadsAndIsFlagged()
    {
        var save = SequenceListSave.FromJsonString("""{ "version": 7, "sequences": [ { "name": "A" } ] }""");

        save.Version.ShouldBe(7);
        save.IsFromNewerVersion.ShouldBeTrue();
        save.Sequences.Count.ShouldBe(1);
        SequenceListSave.FromJsonString(Full).IsFromNewerVersion.ShouldBeFalse();
    }

    [Theory]
    [InlineData("""{ "version": 0 }""", "version")]
    [InlineData("""{ "version": "one" }""", "version")]
    [InlineData("""{ "sources": [ 5 ] }""", "sources")]
    [InlineData("""{ "sequences": [ { "tracks": [ { "x": "far" } ] } ] }""", "x")]
    [InlineData("""{ "sequences": [ { "loop": "yes" } ] }""", "loop")]
    [InlineData("""[ ]""", "")]
    public void FromJsonString_WrongKindOfField_ThrowsJsonExceptionNamingIt(string json, string property)
    {
        var exception = Should.Throw<JsonException>(() => SequenceListSave.FromJsonString(json));

        exception.Message.ShouldContain(property);
    }

    [Fact]
    public void FromFile_ReadsTheFile()
    {
        string path = Path.Combine(Path.GetTempPath(), "seqj-" + Guid.NewGuid() + ".seqj");
        File.WriteAllText(path, Full);
        try
        {
            SequenceListSave.FromFile(path).Sequences.Single().Name.ShouldBe("Attack");
        }
        finally
        {
            File.Delete(path);
        }
    }
}
