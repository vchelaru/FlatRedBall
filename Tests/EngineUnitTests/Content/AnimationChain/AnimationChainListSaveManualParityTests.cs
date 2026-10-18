using System.Collections;
using System.Reflection;
using System.Xml.Serialization;
using EngineUnitTests.TestSupport;
using FlatRedBall.Content.AnimationChain;
using FlatRedBall.Content.Math.Geometry;
using FlatRedBall.Content.Polygon;
using FlatRedBall.IO;
using FlatRedBall.Math.Geometry;
using Shouldly;

namespace EngineUnitTests.Content.AnimationChain;

/// <summary>
/// The manual (reflection-free) .achx loader is what runs under Native AOT, where XmlSerializer can't be
/// used (#2365). It must produce exactly what the XmlSerializer path produces, member for member.
/// </summary>
[Collection(nameof(EngineStatefulTestCollection))]
public class AnimationChainListSaveManualParityTests : IDisposable
{
    readonly string _directory = Path.Combine(Path.GetTempPath(), "achx-parity-" + Guid.NewGuid());

    public AnimationChainListSaveManualParityTests()
    {
        Directory.CreateDirectory(_directory);
    }

    public void Dispose()
    {
        Directory.Delete(_directory, recursive: true);
    }

    static string RepoRoot()
    {
        string? directory = AppContext.BaseDirectory;
        while (directory != null &&
            !(Directory.Exists(Path.Combine(directory, "Engines")) && Directory.Exists(Path.Combine(directory, "Tests"))))
        {
            directory = Path.GetDirectoryName(directory);
        }
        directory.ShouldNotBeNull("could not find the repo root above " + AppContext.BaseDirectory);
        return directory!;
    }

    public static IEnumerable<object[]> RepoAchxFiles()
    {
        string root = RepoRoot();
        foreach (string folder in new[] { "Samples", "Tests", "FRBDK" })
        {
            foreach (string file in Directory.EnumerateFiles(Path.Combine(root, folder), "*.achx", SearchOption.AllDirectories))
            {
                string relative = Path.GetRelativePath(root, file);
                string[] parts = relative.Split(Path.DirectorySeparatorChar);
                if (parts.Contains("bin") || parts.Contains("obj"))
                {
                    continue;
                }
                yield return new object[] { relative.Replace('\\', '/') };
            }
        }
    }

    static AnimationChainListSave Load(string path, bool manual)
    {
        bool original = AnimationChainListSave.ManualDeserialization;
        AnimationChainListSave.ManualDeserialization = manual;
        try
        {
            return AnimationChainListSave.FromFile(path);
        }
        finally
        {
            AnimationChainListSave.ManualDeserialization = original;
        }
    }

    static void AssertManualMatchesXmlSerializer(string path)
    {
        var expected = Load(path, manual: false);
        var actual = Load(path, manual: true);

        var differences = new List<string>();
        Compare(expected, actual, "root", differences);
        differences.ShouldBeEmpty(string.Join(Environment.NewLine, differences.Take(20)));
    }

    [Theory]
    [MemberData(nameof(RepoAchxFiles))]
    public void FromFile_RepoAchx_ManualMatchesXmlSerializer(string relativePath)
    {
        AssertManualMatchesXmlSerializer(Path.Combine(RepoRoot(), relativePath));
    }

    [Fact]
    public void RepoAchxFiles_ShouldIncludeShapeCollectionsAndProjectFile()
    {
        var shapeFiles = 0;
        var projectFileFiles = 0;
        foreach (object[] row in RepoAchxFiles())
        {
            string text = File.ReadAllText(Path.Combine(RepoRoot(), (string)row[0]));
            // No repo file has a populated shape list, only the empty collections the animation editor writes.
            // Populated shapes are covered by FromFile_EveryFieldSet_ManualMatchesXmlSerializer.
            if (text.Contains("<ShapeCollectionSave>")) shapeFiles++;
            if (text.Contains("<ProjectFile>")) projectFileFiles++;
        }

        shapeFiles.ShouldBeGreaterThan(0);
        projectFileFiles.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void FromFile_EveryFieldSet_ManualMatchesXmlSerializer()
    {
        var list = new AnimationChainListSave
        {
            ProjectFile = "../Game.gluj",
            FileRelativeTextures = false,
            TimeMeasurementUnit = FlatRedBall.TimeMeasurementUnit.Millisecond,
            CoordinateType = FlatRedBall.Graphics.TextureCoordinateType.Pixel,
        };

        var chain = new AnimationChainSave { Name = "Walk" };        chain.Frames.Add(new AnimationFrameSave
        {
            TextureName = "sheet.png",
            FrameLength = 250,
            LeftCoordinate = 1.5f,
            RightCoordinate = 33.25f,
            TopCoordinate = 2.5f,
            BottomCoordinate = 17f,
            FlipHorizontal = true,
            FlipVertical = true,
            FlipDiagonal = true,
            RelativeX = 3.5f,
            RelativeY = -2.25f,
            Red = -10,
            Green = 128,
            Blue = 255,
            Alpha = 200,
            ColorOperation = AnimationFrameColorOperation.Add,
            SkipOffsetKeyframe = true,
            SkipColorKeyframe = true,
            OffsetInterpolation = "Bounce",
            OffsetEasing = "InOut",
            ColorInterpolation = "SomeFutureCurve",
            ColorEasing = "In",
            ShapeCollectionSave = new ShapeCollectionSave
            {
                AxisAlignedRectangleSaves =
                {
                    new AxisAlignedRectangleSave
                    {
                        Name = "Hit", Parent = "Body", X = 1, Y = 2, Z = 3, ScaleX = 4, ScaleY = 5,
                        Alpha = 0.5f, Red = 0.25f, Green = 0.75f, Blue = 0.125f,
                        SkipOffsetKeyframe = true, OffsetInterpolation = "Linear", OffsetEasing = "Out",
                        SkipSizeKeyframe = true, SizeInterpolation = "Cubic", SizeEasing = "InOut",
                    },
                },
                CircleSaves =
                {
                    new CircleSave
                    {
                        Name = "Ball", Parent = "Body", X = 6, Y = 7, Z = 8, Radius = 9,
                        Alpha = 0.5f, Red = 0.25f, Green = 0.75f, Blue = 0.125f,
                        SkipOffsetKeyframe = true, OffsetInterpolation = "Elastic", OffsetEasing = "In",
                        SkipSizeKeyframe = true, SizeInterpolation = "Back", SizeEasing = "Out",
                    },
                },
                PolygonSaves =
                {
                    new PolygonSave
                    {
                        Name = "Tri", X = 10, Y = 11, Z = 12, RotationZ = 0.5f,
                        Alpha = 0.5f, Red = 0.25f, Green = 0.75f, Blue = 0.125f,
                        Points = new[] { new Point(0, 0), new Point(4, 0), new Point(0, 4) },
                        SkipOffsetKeyframe = true, OffsetInterpolation = "Quartic", OffsetEasing = "Out",
                        SkipPointsKeyframe = true, PointsInterpolation = "Sinusoidal", PointsEasing = "InOut",
                    },
                },
            },
        });
        chain.Frames.Add(new AnimationFrameSave { TextureName = "sheet.png", FrameLength = 100, ColorOperation = AnimationFrameColorOperation.Multiply });
        list.AnimationChains.Add(chain);
        list.AnimationChains.Add(new AnimationChainSave { Name = "Empty" });

        string path = Path.Combine(_directory, "everything.achx");
        FileManager.XmlSerialize(list, path);

        AssertManualMatchesXmlSerializer(path);
    }

    #region Reflection comparison over the Save types

    static void Compare(object? expected, object? actual, string path, List<string> differences)
    {
        if (expected == null || actual == null)
        {
            if (expected != null || actual != null)
            {
                differences.Add($"{path}: expected {expected ?? "null"} but was {actual ?? "null"}");
            }
            return;
        }

        Type type = expected.GetType();
        if (type != actual.GetType())
        {
            differences.Add($"{path}: expected type {type} but was {actual.GetType()}");
            return;
        }

        if (type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal))
        {
            if (!expected.Equals(actual))
            {
                differences.Add($"{path}: expected {expected} but was {actual}");
            }
            return;
        }

        if (expected is IEnumerable expectedItems)
        {
            var expectedList = expectedItems.Cast<object?>().ToList();
            var actualList = ((IEnumerable)actual).Cast<object?>().ToList();
            if (expectedList.Count != actualList.Count)
            {
                differences.Add($"{path}: expected {expectedList.Count} items but was {actualList.Count}");
                return;
            }
            for (int i = 0; i < expectedList.Count; i++)
            {
                Compare(expectedList[i], actualList[i], $"{path}[{i}]", differences);
            }
            return;
        }

        foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
        {
            if (field.GetCustomAttribute<XmlIgnoreAttribute>() == null)
            {
                Compare(field.GetValue(expected), field.GetValue(actual), $"{path}.{field.Name}", differences);
            }
        }

        foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (property.GetIndexParameters().Length == 0 && property.CanRead &&
                property.GetCustomAttribute<XmlIgnoreAttribute>() == null)
            {
                Compare(property.GetValue(expected), property.GetValue(actual), $"{path}.{property.Name}", differences);
            }
        }
    }

    #endregion
}
