using EngineUnitTests.TestSupport;
using FlatRedBall.Content.AnimationChain;
using FlatRedBall.Graphics.Animation;
using Shouldly;

namespace EngineUnitTests.Content.AnimationChain;

/// <summary>
/// File-format side of per-frame keyframe interpolation: the new fields load from .achx (reflection and
/// manual paths) and .achj, round-trip through .achx unchanged (including names this engine doesn't
/// understand), and are omitted when unset so existing files stay byte-identical.
/// </summary>
[Collection(nameof(EngineStatefulTestCollection))]
public class AnimationFrameSaveKeyframeFormatTests : IDisposable
{
    readonly string _directory = Path.Combine(Path.GetTempPath(), "achx-keyframe-" + Guid.NewGuid());

    public AnimationFrameSaveKeyframeFormatTests()
    {
        Directory.CreateDirectory(_directory);
    }

    public void Dispose()
    {
        Directory.Delete(_directory, recursive: true);
    }

    string Write(string fileName, string contents)
    {
        string path = Path.Combine(_directory, fileName);
        File.WriteAllText(path, contents);
        return path;
    }

    const string FullyPopulatedAchx = """
        <?xml version="1.0" encoding="utf-8"?>
        <AnimationChainArraySave xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema">
          <FileRelativeTextures>true</FileRelativeTextures>
          <TimeMeasurementUnit>Second</TimeMeasurementUnit>
          <CoordinateType>UV</CoordinateType>
          <AnimationChain>
            <Name>c</Name>
            <Frame>
              <TextureName></TextureName>
              <FrameLength>0.1</FrameLength>
              <LeftCoordinate>0</LeftCoordinate>
              <RightCoordinate>1</RightCoordinate>
              <TopCoordinate>0</TopCoordinate>
              <BottomCoordinate>1</BottomCoordinate>
              <SkipOffsetKeyframe>true</SkipOffsetKeyframe>
              <SkipColorKeyframe>true</SkipColorKeyframe>
              <OffsetInterpolation>Cubic</OffsetInterpolation>
              <OffsetEasing>InOut</OffsetEasing>
              <ColorInterpolation>SomethingFromTheFuture</ColorInterpolation>
              <ColorEasing>SidewaysEase</ColorEasing>
            </Frame>
          </AnimationChain>
        </AnimationChainArraySave>
        """;

    static void AssertPreserved(AnimationFrameSave frame)
    {
        frame.SkipOffsetKeyframe.ShouldBeTrue();
        frame.SkipColorKeyframe.ShouldBeTrue();
        frame.OffsetInterpolation.ShouldBe("Cubic");
        frame.OffsetEasing.ShouldBe("InOut");
        frame.ColorInterpolation.ShouldBe("SomethingFromTheFuture");
        frame.ColorEasing.ShouldBe("SidewaysEase");
    }

    static AnimationFrameSave FirstFrame(AnimationChainListSave save) => save.AnimationChains[0].Frames[0];

    // ---- Loading ----

    [Fact]
    public void FromFile_Achx_ReadsKeyframeFields()
    {
        var save = AnimationChainListSave.FromFile(Write("a.achx", FullyPopulatedAchx));

        AssertPreserved(FirstFrame(save));
    }

    [Fact]
    public void FromFile_AchxManualDeserialization_ReadsKeyframeFields()
    {
        bool original = AnimationChainListSave.ManualDeserialization;
        AnimationChainListSave.ManualDeserialization = true;
        try
        {
            var save = AnimationChainListSave.FromFile(Write("a.achx", FullyPopulatedAchx));

            AssertPreserved(FirstFrame(save));
        }
        finally
        {
            AnimationChainListSave.ManualDeserialization = original;
        }
    }

    [Fact]
    public void FromFile_Achj_ReadsKeyframeFields()
    {
        string path = Write("a.achj", """
            { "animationChains": [ { "name": "c", "frames": [ {
                "textureName": "a.png", "frameLength": 0.1,
                "skipOffsetKeyframe": true, "skipColorKeyframe": true,
                "offsetInterpolation": "Cubic", "offsetEasing": "InOut",
                "colorInterpolation": "SomethingFromTheFuture", "colorEasing": "SidewaysEase"
            } ] } ] }
            """);

        AssertPreserved(FirstFrame(AnimationChainListSave.FromFile(path)));
    }

    [Fact]
    public void FromFile_AchjNonStringCurveName_IsIgnoredWithoutThrowing()
    {
        string path = Write("a.achj", """
            { "animationChains": [ { "name": "c", "frames": [ {
                "textureName": "", "frameLength": 1, "offsetInterpolation": 5, "colorEasing": { "a": 1 }
            } ] } ] }
            """);

        var frame = FirstFrame(AnimationChainListSave.FromFile(path));

        frame.OffsetInterpolation.ShouldBeNull();
        frame.ColorEasing.ShouldBeNull();
    }

    [Fact]
    public void FromFile_OldFileWithoutFields_LoadsWithDefaults()
    {
        string path = Write("a.achx", """
            <AnimationChainArraySave><AnimationChain><Name>c</Name>
            <Frame><TextureName>a.png</TextureName><FrameLength>0.1</FrameLength></Frame>
            </AnimationChain></AnimationChainArraySave>
            """);

        var frame = FirstFrame(AnimationChainListSave.FromFile(path));

        frame.SkipOffsetKeyframe.ShouldBeFalse();
        frame.SkipColorKeyframe.ShouldBeFalse();
        frame.OffsetInterpolation.ShouldBeNull();
        frame.OffsetEasing.ShouldBeNull();
        frame.ColorInterpolation.ShouldBeNull();
        frame.ColorEasing.ShouldBeNull();
    }

    [Fact]
    public void FromFile_UnknownFrameElement_IsIgnored()
    {
        string path = Write("a.achx", """
            <AnimationChainArraySave><AnimationChain><Name>c</Name>
            <Frame><TextureName>a.png</TextureName><FrameLength>0.25</FrameLength>
            <SomethingNew>x</SomethingNew><OffsetInterpolation>Linear</OffsetInterpolation></Frame>
            </AnimationChain></AnimationChainArraySave>
            """);

        var frame = FirstFrame(AnimationChainListSave.FromFile(path));

        frame.FrameLength.ShouldBe(0.25f);
        frame.OffsetInterpolation.ShouldBe("Linear");
    }

    // ---- Saving ----

    [Fact]
    public void Save_PreservesEveryValueIncludingUnknownNames()
    {
        var loaded = AnimationChainListSave.FromFile(Write("a.achx", FullyPopulatedAchx));
        string savedPath = Path.Combine(_directory, "saved.achx");

        loaded.Save(savedPath);
        var reloaded = AnimationChainListSave.FromFile(savedPath);

        AssertPreserved(FirstFrame(reloaded));
    }

    [Fact]
    public void Save_FromAchj_PreservesEveryValueInAchx()
    {
        string path = Write("a.achj", """
            { "animationChains": [ { "name": "c", "frames": [ {
                "textureName": "a.png", "frameLength": 0.1,
                "skipOffsetKeyframe": true, "skipColorKeyframe": true,
                "offsetInterpolation": "Cubic", "offsetEasing": "InOut",
                "colorInterpolation": "SomethingFromTheFuture", "colorEasing": "SidewaysEase"
            } ] } ] }
            """);
        string savedPath = Path.Combine(_directory, "saved.achx");

        AnimationChainListSave.FromFile(path).Save(savedPath);

        AssertPreserved(FirstFrame(AnimationChainListSave.FromFile(savedPath)));
    }

    [Fact]
    public void Save_UsesDocumentedElementNames()
    {
        string savedPath = Path.Combine(_directory, "saved.achx");

        AnimationChainListSave.FromFile(Write("a.achx", FullyPopulatedAchx)).Save(savedPath);
        string xml = File.ReadAllText(savedPath);

        xml.ShouldContain("<SkipOffsetKeyframe>true</SkipOffsetKeyframe>");
        xml.ShouldContain("<SkipColorKeyframe>true</SkipColorKeyframe>");
        xml.ShouldContain("<OffsetInterpolation>Cubic</OffsetInterpolation>");
        xml.ShouldContain("<OffsetEasing>InOut</OffsetEasing>");
        xml.ShouldContain("<ColorInterpolation>SomethingFromTheFuture</ColorInterpolation>");
        xml.ShouldContain("<ColorEasing>SidewaysEase</ColorEasing>");
    }

    [Fact]
    public void Save_SaveLoadSave_IsStable()
    {
        string first = Path.Combine(_directory, "first.achx");
        string second = Path.Combine(_directory, "second.achx");

        AnimationChainListSave.FromFile(Write("a.achx", FullyPopulatedAchx)).Save(first);
        AnimationChainListSave.FromFile(first).Save(second);

        File.ReadAllBytes(second).ShouldBe(File.ReadAllBytes(first));
    }

    [Fact]
    public void Save_OnlySkipOffsetSet_WritesOnlyThatElement()
    {
        var save = new AnimationChainListSave();
        var chain = new AnimationChainSave { Name = "c" };
        chain.Frames.Add(new AnimationFrameSave { FrameLength = 0.1f, TextureName = "a.png", SkipOffsetKeyframe = true });
        save.AnimationChains.Add(chain);
        string path = Path.Combine(_directory, "saved.achx");

        save.Save(path);
        string xml = File.ReadAllText(path);

        xml.ShouldContain("<SkipOffsetKeyframe>true</SkipOffsetKeyframe>");
        xml.ShouldNotContain("SkipColorKeyframe");
        xml.ShouldNotContain("Interpolation");
        xml.ShouldNotContain("Easing");
    }

    [Fact]
    public void Save_DefaultFrame_WritesNoKeyframeElements()
    {
        var save = new AnimationChainListSave();
        var chain = new AnimationChainSave { Name = "c" };
        chain.Frames.Add(new AnimationFrameSave { FrameLength = 0.1f, TextureName = "a.png" });
        save.AnimationChains.Add(chain);
        string path = Path.Combine(_directory, "saved.achx");

        save.Save(path);
        string xml = File.ReadAllText(path);

        xml.ShouldNotContain("Skip");
        xml.ShouldNotContain("Interpolation");
        xml.ShouldNotContain("Easing");
    }

    [Fact]
    public void Save_ExistingSampleFile_IsByteIdenticalToItself()
    {
        string sample = FindSample("Samples/Platformer/Animations/PlatformerAnimations.achx");
        string copy = Path.Combine(_directory, "PlatformerAnimations.achx");
        File.Copy(sample, copy);
        string saved = Path.Combine(_directory, "saved.achx");

        AnimationChainListSave.FromFile(copy).Save(saved);

        File.ReadAllBytes(saved).ShouldBe(File.ReadAllBytes(sample));
    }

    static string FindSample(string repoRelativePath)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            string candidate = Path.Combine(directory.FullName, repoRelativePath);
            if (File.Exists(candidate)) return candidate;
            directory = directory.Parent;
        }
        throw new FileNotFoundException(repoRelativePath);
    }

    // ---- Runtime conversion ----

    [Fact]
    public void ToAnimationFrame_CopiesKeyframeFieldsToRuntimeFrame()
    {
        var loaded = AnimationChainListSave.FromFile(Write("a.achx", FullyPopulatedAchx));

        var frame = loaded.ToAnimationChainList("Test", throwError: true)[0][0];

        frame.SkipOffsetKeyframe.ShouldBeTrue();
        frame.SkipColorKeyframe.ShouldBeTrue();
        frame.OffsetInterpolation.ShouldBe("Cubic");
        frame.OffsetEasing.ShouldBe("InOut");
        frame.OffsetCurve.Type.ShouldBe(KeyframeInterpolationType.Cubic);
        frame.OffsetCurve.Easing.ShouldBe(KeyframeEasing.InOut);
        frame.ColorInterpolation.ShouldBe("SomethingFromTheFuture");
        frame.ColorCurve.IsSnap.ShouldBeTrue();
    }

    [Fact]
    public void SaveFromRuntimeFrame_PreservesKeyframeFields()
    {
        // The save ctor reads Texture.Name; an uninitialized Texture2D is enough since only the name is used.
        var texture = (Microsoft.Xna.Framework.Graphics.Texture2D)
            System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(Microsoft.Xna.Framework.Graphics.Texture2D));
        texture.Name = "a.png";
        var template = new AnimationFrame
        {
            Texture = texture,
            SkipOffsetKeyframe = true,
            SkipColorKeyframe = true,
            OffsetInterpolation = "Cubic",
            OffsetEasing = "InOut",
            ColorInterpolation = "SomethingFromTheFuture",
            ColorEasing = "SidewaysEase",
        };

        var save = new AnimationFrameSave(template);

        AssertPreserved(save);
    }

    [Fact]
    public void ToAnimationFrame_OldFrame_HasSnapCurvesAndNoSkips()
    {
        var frame = new AnimationFrameSave { FrameLength = 1 }.ToAnimationFrame("Test", loadTexture: false);

        frame.OffsetCurve.IsSnap.ShouldBeTrue();
        frame.ColorCurve.IsSnap.ShouldBeTrue();
        frame.SkipOffsetKeyframe.ShouldBeFalse();
        frame.SkipColorKeyframe.ShouldBeFalse();
    }

    [Fact]
    public void ToAnimationChain_MillisecondUnits_FrameLengthsConvertedToSeconds()
    {
        var chain = new AnimationChainSave { Name = "c" };
        chain.Frames.Add(new AnimationFrameSave { FrameLength = 250, OffsetInterpolation = "Linear" });

        var runtime = chain.ToAnimationChain("Test", FlatRedBall.TimeMeasurementUnit.Millisecond);

        runtime[0].FrameLength.ShouldBe(0.25f);
    }
}
