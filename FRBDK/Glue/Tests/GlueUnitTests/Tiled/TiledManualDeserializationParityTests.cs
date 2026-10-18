using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Serialization;
using FlatRedBall.IO;
using Shouldly;
using TMXGlueLib;
using Xunit;

namespace GlueUnitTests.Tiled;

/// <summary>
/// The manual (reflection-free) .tmx/.tsx loader is what runs under Native AOT, where XmlSerializer can't be
/// used (#2365). It must produce exactly what the XmlSerializer path produces, public member for public member.
/// </summary>
public class TiledManualDeserializationParityTests : IDisposable
{
    readonly string _directory = Path.Combine(Path.GetTempPath(), "tmx-parity-" + Guid.NewGuid());
    readonly string _originalRelativeDirectory = FileManager.RelativeDirectory;
    readonly bool _originalManual = TiledMapSave.ManualDeserialization;
    readonly bool _originalShouldLoadValuesFromSource = Tileset.ShouldLoadValuesFromSource;
    readonly bool _originalShouldThrowOnMissingSource = Tileset.ShouldThrowOnMissingSource;

    public TiledManualDeserializationParityTests()
    {
        Directory.CreateDirectory(_directory);
    }

    public void Dispose()
    {
        FileManager.RelativeDirectory = _originalRelativeDirectory;
        TiledMapSave.ManualDeserialization = _originalManual;
        Tileset.ShouldLoadValuesFromSource = _originalShouldLoadValuesFromSource;
        Tileset.ShouldThrowOnMissingSource = _originalShouldThrowOnMissingSource;
        Directory.Delete(_directory, recursive: true);
    }

    #region Repo files

    static string RepoRoot()
    {
        string directory = AppContext.BaseDirectory;
        while (directory != null &&
            !(Directory.Exists(Path.Combine(directory, "Engines")) && Directory.Exists(Path.Combine(directory, "Tests"))))
        {
            directory = Path.GetDirectoryName(directory);
        }
        directory.ShouldNotBeNull("could not find the repo root above " + AppContext.BaseDirectory);
        return directory;
    }

    static IEnumerable<string> RepoFiles(string extension)
    {
        string root = RepoRoot();
        foreach (string folder in new[] { "Samples", "Tests", "FRBDK", "Tiled" })
        {
            foreach (string file in Directory.EnumerateFiles(Path.Combine(root, folder), "*" + extension, SearchOption.AllDirectories))
            {
                string relative = Path.GetRelativePath(root, file);
                string[] parts = relative.Split(Path.DirectorySeparatorChar);
                if (parts.Contains("bin") || parts.Contains("obj"))
                {
                    continue;
                }
                yield return relative.Replace('\\', '/');
            }
        }
    }

    public static IEnumerable<object[]> RepoTmxFiles() => RepoFiles(".tmx").Select(item => new object[] { item });

    public static IEnumerable<object[]> RepoTsxFiles() => RepoFiles(".tsx").Select(item => new object[] { item });

    [Theory]
    [MemberData(nameof(RepoTmxFiles))]
    public void FromFile_RepoTmx_ManualMatchesXmlSerializer(string relativePath)
    {
        AssertMapParity(Path.Combine(RepoRoot(), relativePath));
    }

    [Theory]
    [MemberData(nameof(RepoTsxFiles))]
    public void LoadTileset_RepoTsx_ManualMatchesXmlSerializer(string relativePath)
    {
        AssertTilesetParity(Path.Combine(RepoRoot(), relativePath));
    }

    [Fact]
    public void RepoFiles_ShouldBeFound()
    {
        // The two theories above silently pass with zero rows if the repo layout changes underneath them.
        RepoTmxFiles().Count().ShouldBeGreaterThan(20);
        RepoTsxFiles().Count().ShouldBeGreaterThan(5);
    }

    [Fact]
    public void RepoFiles_MostShouldLoadSuccessfully()
    {
        // A file that fails to load on both paths passes the parity theories, so make sure that is the exception.
        int loaded = 0;
        foreach (object[] row in RepoTmxFiles())
        {
            var outcome = Capture(() => WithManual(true, () => TiledMapSave.FromFile(Path.Combine(RepoRoot(), (string)row[0]))));
            if (outcome.Exception == null)
            {
                loaded++;
            }
        }

        // A few checked-in fixtures do not load at all, for example a wrong xmlns or a missing tsx.
        loaded.ShouldBeGreaterThan((int)(RepoTmxFiles().Count() * 0.85));
    }

    #endregion

    #region Synthetic files

    // No repo file has image layers, groups, infinite chunks, point/text objects, tile objects or zlib data,
    // so a hand-built map covers every member of the Save classes at least once.
    [Fact]
    public void FromFile_EveryFeature_ManualMatchesXmlSerializer()
    {
        WriteSyntheticFiles();

        AssertMapParity(Path.Combine(_directory, "main.tmx"));
    }

    [Fact]
    public void LoadTileset_EveryFeature_ManualMatchesXmlSerializer()
    {
        WriteSyntheticFiles();

        AssertTilesetParity(Path.Combine(_directory, "ext.tsx"));
    }

    [Fact]
    public void FromFile_InfiniteMapChunks_ManualMatchesXmlSerializer()
    {
        // The save classes have no chunk member, so XmlSerializer drops chunk elements. The manual loader must
        // drop them the same way rather than inventing members.
        File.WriteAllText(Path.Combine(_directory, "infinite.tmx"),
            "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" +
            "<map version=\"1.10\" orientation=\"orthogonal\" renderorder=\"right-down\" width=\"0\" height=\"0\" tilewidth=\"16\" tileheight=\"16\" infinite=\"1\">\n" +
            " <layer id=\"1\" name=\"Chunked\" width=\"0\" height=\"0\">\n" +
            "  <data encoding=\"csv\">\n" +
            "   <chunk x=\"0\" y=\"0\" width=\"2\" height=\"2\">\n1,2,\n3,4\n</chunk>\n" +
            "   <chunk x=\"2\" y=\"0\" width=\"2\" height=\"2\">\n5,6,\n7,8\n</chunk>\n" +
            "  </data>\n" +
            " </layer>\n" +
            "</map>");

        AssertMapParity(Path.Combine(_directory, "infinite.tmx"));
    }

    [Fact]
    public void FromFile_ExternalTilesetSourceBeforeOtherAttributes_ManualMatchesXmlSerializer()
    {
        // Setting Source loads the tsx immediately, so attributes written after it overwrite the tsx values and
        // attributes written before it are overwritten. Document order has to be honored.
        WriteSyntheticFiles();
        File.WriteAllText(Path.Combine(_directory, "order.tmx"),
            "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" +
            "<map version=\"1.10\" orientation=\"orthogonal\" width=\"1\" height=\"1\" tilewidth=\"16\" tileheight=\"16\">\n" +
            " <tileset name=\"before\" tilewidth=\"1\" firstgid=\"1\" source=\"ext.tsx\" tileheight=\"99\" spacing=\"7\"/>\n" +
            " <tileset firstgid=\"9\" source=\"ext.tsx\"><image source=\"ignored.png\" width=\"1\" height=\"1\"/><tile id=\"77\"/><wangsets><wangset name=\"Ignored\"/></wangsets></tileset>\n" +
            "</map>");

        AssertMapParity(Path.Combine(_directory, "order.tmx"));
    }

    [Fact]
    public void FromFile_MissingExternalTileset_ManualMatchesXmlSerializer()
    {
        File.WriteAllText(Path.Combine(_directory, "missing.tmx"),
            "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" +
            "<map version=\"1.10\" orientation=\"orthogonal\" width=\"1\" height=\"1\" tilewidth=\"16\" tileheight=\"16\">\n" +
            " <tileset firstgid=\"1\" source=\"DoesNotExist.tsx\"/>\n" +
            "</map>");

        AssertMapParity(Path.Combine(_directory, "missing.tmx"));

        Tileset.ShouldThrowOnMissingSource = false;
        AssertMapParity(Path.Combine(_directory, "missing.tmx"));
    }

    [Fact]
    public void FromFile_MissingFile_ManualThrowsSameAsXmlSerializer()
    {
        AssertMapParity(Path.Combine(_directory, "NotThere.tmx"));
    }

    [Fact]
    public void FromFile_WrongRootElement_ManualThrowsSameAsXmlSerializer()
    {
        File.WriteAllText(Path.Combine(_directory, "wrongroot.tmx"), "<?xml version=\"1.0\"?><notamap/>");

        AssertMapParity(Path.Combine(_directory, "wrongroot.tmx"));
    }

    [Fact]
    public void FromFile_ManualMode_ShouldRetryAndSucceed_WhenFileIsTransientlyEmpty()
    {
        string path = Path.Combine(_directory, "racy.tmx");
        File.WriteAllText(path, "");
        TiledMapSave.ManualDeserialization = true;

        var writer = System.Threading.Tasks.Task.Run(() =>
        {
            System.Threading.Thread.Sleep(50);
            File.WriteAllText(path, "<map width=\"1\" height=\"1\" tilewidth=\"16\" tileheight=\"16\"></map>");
        });
        TiledMapSave tms = TiledMapSave.FromFile(path);
        writer.Wait();

        tms.Width.ShouldBe(1);
    }

    [Fact]
    public void FromFile_ManualMode_ShouldStillThrow_WhenFileIsPermanentlyMalformed()
    {
        string path = Path.Combine(_directory, "malformed.tmx");
        File.WriteAllText(path, "<map width=\"1\"");
        TiledMapSave.ManualDeserialization = true;

        Should.Throw<InvalidOperationException>(() => TiledMapSave.FromFile(path));
    }

    [Fact]
    public void ManualDeserialization_DefaultsToFalseWhenDynamicCodeIsSupported()
    {
        // The test host is a JIT runtime, so the default must leave the XmlSerializer path in place.
        System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported.ShouldBeTrue();
        _originalManual.ShouldBeFalse();
    }

    void WriteSyntheticFiles()
    {
        File.WriteAllText(Path.Combine(_directory, "ext.tsx"), ExternalTsx);
        File.WriteAllText(Path.Combine(_directory, "main.tmx"), MainTmx());
    }

    const string ExternalTsx =
        "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" +
        "<tileset version=\"1.10\" tiledversion=\"1.10.2\" name=\"ext\" tilewidth=\"16\" tileheight=\"17\" spacing=\"1\" margin=\"2\" tilecount=\"4\" columns=\"2\">\n" +
        " <tileoffset x=\"1\" y=\"2\"/>\n" +
        " <image source=\"images/ext.png\" width=\"40\" height=\"41\"/>\n" +
        " <tile id=\"0\" type=\"Solid\" probability=\"0.5\">\n" +
        "  <properties>\n" +
        "   <property name=\"HasCollision\" type=\"bool\" value=\"true\"/>\n" +
        "   <property name=\"Speed (float)\" type=\"float\" value=\"1.5\"/>\n" +
        "  </properties>\n" +
        "  <animation>\n" +
        "   <frame tileid=\"0\" duration=\"100\"/>\n" +
        "   <frame tileid=\"1\" duration=\"200\"/>\n" +
        "  </animation>\n" +
        " </tile>\n" +
        " <tile id=\"1\" class=\"Ladder\">\n" +
        "  <objectgroup draworder=\"index\" id=\"2\" name=\"shapes\" opacity=\"0.5\">\n" +
        "   <object id=\"1\" name=\"box\" x=\"1.5\" y=\"2.5\" width=\"8\" height=\"9\"/>\n" +
        "   <object id=\"2\" x=\"0\" y=\"0\"><polygon points=\"0,0 4,0 4,4\"/></object>\n" +
        "  </objectgroup>\n" +
        " </tile>\n" +
        " <tile id=\"2\" type=\"First\" class=\"Second\"/>\n" +
        " <tile id=\"3\" class=\"First\" type=\"Second\"/>\n" +
        " <wangsets>\n" +
        "  <wangset name=\"Terrain\" type=\"corner\" tile=\"-1\">\n" +
        "   <wangcolor name=\"Grass\" color=\"#00ff00\" tile=\"0\" probability=\"0.5\"/>\n" +
        "   <wangcolor name=\"Dirt\" color=\"#ff0000\" tile=\"1\" probability=\"1\"/>\n" +
        "   <wangtile tileid=\"0\" wangid=\"0,1,0,1,0,1,0,1\"/>\n" +
        "   <wangtile tileid=\"1\" wangid=\"0,2,0,2,0,2,0,2\"/>\n" +
        "  </wangset>\n" +
        "  <wangset name=\"Second\" type=\"edge\" tile=\"3\"/>\n" +
        " </wangsets>\n" +
        "</tileset>";

    static string Base64Ids(uint[] ids, string compression)
    {
        var raw = new MemoryStream();
        using (var writer = new BinaryWriter(raw, Encoding.UTF8, leaveOpen: true))
        {
            foreach (uint id in ids)
            {
                writer.Write(id);
            }
        }

        if (compression == "gzip" || compression == "zlib")
        {
            // Zlib isn't supported by the runtime reader on either path, so the payload only has to be valid base64.
            var compressed = new MemoryStream();
            using (var gzip = new GZipStream(compressed, CompressionLevel.Optimal, leaveOpen: true))
            {
                gzip.Write(raw.ToArray(), 0, (int)raw.Length);
            }
            return Convert.ToBase64String(compressed.ToArray());
        }

        return Convert.ToBase64String(raw.ToArray());
    }

    static string MainTmx()
    {
        uint[] ids = { 1, 2, 3, 2147483649, 0, 4 };

        return
        "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" +
        "<map version=\"1.10\" tiledversion=\"1.10.2\" orientation=\"isometric\" renderorder=\"left-up\" compressionlevel=\"-1\" backgroundcolor=\"#112233\" width=\"3\" height=\"2\" tilewidth=\"16\" tileheight=\"17\" infinite=\"0\" nextlayerid=\"20\" nextobjectid=\"12\">\n" +
        " <editorsettings><export target=\"x.json\" format=\"json\"/></editorsettings>\n" +
        " <properties>\n" +
        "  <property name=\"Name\" value=\"MyMap\"/>\n" +
        "  <property name=\"Count (int)\" type=\"int\" value=\"3\"/>\n" +
        "  <property name=\"Flag\" type=\"bool\" value=\"true\"/>\n" +
        "  <property name=\"Tint\" type=\"color\" value=\"#ff112233\"/>\n" +
        "  <property name=\"File\" type=\"file\" value=\"other.png\"/>\n" +
        "  <property name=\"Multiline\">line one\nline two</property>\n" +
        "  <property name=\"Duplicate\" value=\"first\"/>\n" +
        "  <property name=\"Duplicate\" value=\"second\"/>\n" +
        " </properties>\n" +
        " <tileset firstgid=\"1\" source=\"ext.tsx\"/>\n" +
        " <tileset firstgid=\"5\" name=\"inline\" tilewidth=\"8\" tileheight=\"9\" spacing=\"1\" margin=\"1\" tilecount=\"2\" columns=\"2\">\n" +
        "  <tileoffset x=\"3\" y=\"-4\"/>\n" +
        "  <image source=\"inline/sheet.png\" width=\"20\" height=\"10\"/>\n" +
        "  <terraintypes><mapTilesetTerrain><name>Grass</name><tile>4</tile></mapTilesetTerrain><mapTilesetTerrain name=\"Attribute\" tile=\"1\"/><terrain name=\"Ignored\" tile=\"1\"/></terraintypes>\n" +
        "  <tile id=\"0\" type=\"Door\"><properties><property name=\"Locked\" type=\"bool\" value=\"false\"/></properties></tile>\n" +
        "  <tile id=\"1\"><animation><frame tileid=\"1\" duration=\"50\"/></animation></tile>\n" +
        "  <wangsets><wangset name=\"W\" type=\"edge\" tile=\"2\"><wangcolor name=\"c\" color=\"#fff\" tile=\"1\" probability=\"0.25\"/><wangtile tileid=\"3\" wangid=\"1,2,3,4\"/></wangset></wangsets>\n" +
        " </tileset>\n" +
        " <layer id=\"1\" name=\"Csv\" width=\"3\" height=\"2\" opacity=\"0.5\" visible=\"0\" tintcolor=\"#80ff0000\" offsetx=\"4\" offsety=\"-5.5\" parallaxx=\"0.5\" parallaxy=\"0.75\">\n" +
        "  <properties><property name=\"VisibleBehavior\" value=\"Skip\"/><property name=\"Extra\" value=\"1\"/></properties>\n" +
        "  <data encoding=\"csv\">\n1,2,3,\n2147483649,0,4\n</data>\n" +
        " </layer>\n" +
        " <layer id=\"2\" name=\"Gzip\" width=\"3\" height=\"2\"><data encoding=\"base64\" compression=\"gzip\">\n   " + Base64Ids(ids, "gzip") + "\n  </data></layer>\n" +
        " <layer id=\"3\" name=\"Raw\" width=\"3\" height=\"2\"><data encoding=\"base64\">" + Base64Ids(ids, null) + "</data></layer>\n" +
        " <layer id=\"4\" name=\"Zlib\" width=\"3\" height=\"2\"><data encoding=\"base64\" compression=\"zlib\">" + Base64Ids(ids, "zlib") + "</data></layer>\n" +
        " <layer id=\"5\" name=\"XmlTiles\" width=\"3\" height=\"2\">\n  <data>\n   <tile gid=\"1\"/>\n   <tile gid=\"2\"/>\n   <tile gid=\"0\"/>\n   <tile gid=\"3\"/>\n   <tile/>\n   <tile gid=\"9\"/>\n  </data>\n </layer>\n" +
        " <layer id=\"6\" name=\"UnknownEncoding\" width=\"3\" height=\"2\"><data encoding=\"hex\">00</data></layer>\n" +
        " <layer id=\"7\" name=\"NoData\" width=\"3\" height=\"2\"/>\n" +
        " <layer id=\"8\" name=\"EmptyData\" width=\"3\" height=\"2\"><data encoding=\"csv\"/></layer>\n" +
        " <layer id=\"9\" name=\"WhitespaceData\" width=\"3\" height=\"2\"><data encoding=\"csv\">\n</data></layer>\n" +
        " <layer id=\"10\" name=\"CdataData\" width=\"3\" height=\"2\"><data encoding=\"csv\"><![CDATA[1,2,3,4,5,6]]></data></layer>\n" +
        " <layer width=\"3\" height=\"2\" name=\"SizeBeforeData\"><data encoding=\"csv\">6,5,4,3,2,1</data></layer>\n" +
        " <objectgroup id=\"11\" name=\"Objects\" draworder=\"topdown\" opacity=\"0.3\" visible=\"0\" offsetx=\"1\" offsety=\"2\" tintcolor=\"#ff00ff\" parallaxx=\"2\" parallaxy=\"3\" color=\"#a0a0a4\">\n" +
        "  <properties><property name=\"G\" value=\"1\"/></properties>\n" +
        "  <object id=\"1\" name=\"Rect\" type=\"Spawn\" x=\"16\" y=\"32.5\" width=\"8\" height=\"9.5\" rotation=\"45\">\n" +
        "   <properties><property name=\"Speed\" type=\"float\" value=\"2\"/><property name=\"Target\" type=\"object\" value=\"3\"/></properties>\n" +
        "  </object>\n" +
        "  <object id=\"2\" name=\"Ellipse\" class=\"Zone\" x=\"1\" y=\"2\" width=\"3\" height=\"4\" visible=\"0\"><ellipse/></object>\n" +
        "  <object id=\"3\" x=\"0\" y=\"0\"><polygon points=\"0,0 10,0 10,10\"/></object>\n" +
        "  <object id=\"4\" x=\"0\" y=\"0\"><polyline points=\"0,0 5,5\"/></object>\n" +
        "  <object id=\"5\" x=\"7\" y=\"8\"><point/></object>\n" +
        "  <object id=\"6\" x=\"1\" y=\"1\" width=\"50\" height=\"20\"><text fontfamily=\"Arial\" pixelsize=\"12\" wrap=\"1\">Hello</text></object>\n" +
        "  <object id=\"7\" gid=\"2147483650\" x=\"9\" y=\"10\" width=\"16\" height=\"16\"/>\n" +
        "  <object id=\"8\" name=\"TypeThenClass\" type=\"A\" class=\"B\" x=\"1\" y=\"1\"/>\n" +
        "  <object id=\"9\" name=\"ClassThenType\" class=\"B\" type=\"A\" x=\"1\" y=\"1\"/>\n" +
        "  <object id=\"10\" x=\"0\" y=\"0\" visible=\"1\"><polygon points=\"1,1 2,2 3,1\" visible=\"0\"/><polyline points=\"0,0 1,1\" visible=\"0\"/><ellipse visible=\"0\"/></object>\n" +
        " </objectgroup>\n" +
        " <objectgroup id=\"12\" name=\"EmptyGroup\"/>\n" +
        " <imagelayer id=\"13\" name=\"Backdrop\" offsetx=\"3\" opacity=\"0.9\" repeatx=\"1\"><properties><property name=\"P\" value=\"v\"/></properties><image source=\"back.png\" width=\"100.5\" height=\"50\"/></imagelayer>\n" +
        " <imagelayer id=\"14\" name=\"NoImage\"/>\n" +
        " <group id=\"15\" name=\"Group\" opacity=\"0.5\">\n" +
        "  <layer id=\"16\" name=\"InGroup\" width=\"3\" height=\"2\"><data encoding=\"csv\">1,2,3,4,5,6</data></layer>\n" +
        "  <objectgroup id=\"17\" name=\"InGroupObjects\"><object id=\"11\" x=\"1\" y=\"2\"/></objectgroup>\n" +
        "  <imagelayer id=\"18\" name=\"InGroupImage\"><image source=\"x.png\"/></imagelayer>\n" +
        "  <group id=\"19\" name=\"Nested\"><layer id=\"20\" name=\"Deep\" width=\"3\" height=\"2\"><data encoding=\"csv\">0,0,0,0,0,0</data></layer></group>\n" +
        " </group>\n" +
        " <group id=\"21\" name=\"Second\"/>\n" +
        "</map>";
    }

    #endregion

    #region Parity helpers

    static T WithManual<T>(bool manual, Func<T> load)
    {
        bool original = TiledMapSave.ManualDeserialization;
        TiledMapSave.ManualDeserialization = manual;
        try
        {
            return load();
        }
        finally
        {
            TiledMapSave.ManualDeserialization = original;
        }
    }

    static Outcome<T> Capture<T>(Func<T> load)
    {
        try
        {
            return new Outcome<T> { Value = load() };
        }
        catch (Exception e)
        {
            return new Outcome<T> { Exception = e };
        }
    }

    sealed class Outcome<T>
    {
        public T Value;
        public Exception Exception;
    }

    void AssertMapParity(string path)
    {
        var expected = Capture(() => WithManual(false, () => TiledMapSave.FromFile(path)));
        var actual = Capture(() => WithManual(true, () => TiledMapSave.FromFile(path)));
        AssertSameOutcome(expected, actual);
    }

    void AssertTilesetParity(string path)
    {
        string directory = FileManager.GetDirectory(path.Replace('\\', '/'));
        FileManager.RelativeDirectory = directory;

        // The tsx root type (tileset) is what Tileset copies its values from when Source is set, so the copy
        // is compared as well as the raw tileset.
        var expected = Capture(() => WithManual(false, () => (FileManager.XmlDeserialize<tileset>(path), new Tileset { Source = path })));
        var actual = Capture(() => WithManual(true, () => (TiledManualDeserializer.LoadTileset(path), new Tileset { Source = path })));
        AssertSameOutcome(expected, actual);
    }

    static void AssertSameOutcome<T>(Outcome<T> expected, Outcome<T> actual)
    {
        if (expected.Exception != null || actual.Exception != null)
        {
            actual.Exception.ShouldNotBeNull("XmlSerializer threw " + expected.Exception + " but the manual loader did not");
            expected.Exception.ShouldNotBeNull("the manual loader threw " + actual.Exception + " but XmlSerializer did not");
            actual.Exception.GetType().ShouldBe(expected.Exception.GetType(), actual.Exception.ToString());
            return;
        }

        var differences = new List<string>();
        Compare(expected.Value, actual.Value, "root", differences, 0);
        differences.ShouldBeEmpty(string.Join(Environment.NewLine, differences.Take(20)));
    }

    #endregion

    #region Reflection comparison over the Save types

    static void Compare(object expected, object actual, string path, List<string> differences, int depth)
    {
        if (expected == null || actual == null)
        {
            if (expected != null || actual != null)
            {
                differences.Add($"{path}: expected {expected ?? "null"} but was {actual ?? "null"}");
            }
            return;
        }

        if (depth > 40)
        {
            differences.Add($"{path}: nested deeper than 40 levels, probably a cycle");
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

        if (expected is Array expectedArray && type.GetElementType().IsPrimitive)
        {
            var actualArray = (Array)actual;
            if (expectedArray.Length != actualArray.Length)
            {
                differences.Add($"{path}: expected {expectedArray.Length} items but was {actualArray.Length}");
                return;
            }
            for (int i = 0; i < expectedArray.Length; i++)
            {
                if (!expectedArray.GetValue(i).Equals(actualArray.GetValue(i)))
                {
                    differences.Add($"{path}[{i}]: expected {expectedArray.GetValue(i)} but was {actualArray.GetValue(i)}");
                    return;
                }
            }
            return;
        }

        if (expected is IDictionary expectedDictionary)
        {
            // Property dictionaries are filled in parallel, so their enumeration order is not stable.
            var actualDictionary = (IDictionary)actual;
            var expectedKeys = expectedDictionary.Keys.Cast<object>().OrderBy(key => key.ToString(), StringComparer.Ordinal).ToList();
            var actualKeys = actualDictionary.Keys.Cast<object>().OrderBy(key => key.ToString(), StringComparer.Ordinal).ToList();
            Compare(expectedKeys, actualKeys, path + ".Keys", differences, depth + 1);
            foreach (object key in expectedKeys)
            {
                if (actualDictionary.Contains(key))
                {
                    Compare(expectedDictionary[key], actualDictionary[key], $"{path}[{key}]", differences, depth + 1);
                }
            }
            return;
        }

        if (expected is IEnumerable expectedItems)
        {
            var expectedList = expectedItems.Cast<object>().ToList();
            var actualList = ((IEnumerable)actual).Cast<object>().ToList();
            if (expectedList.Count != actualList.Count)
            {
                differences.Add($"{path}: expected {expectedList.Count} items but was {actualList.Count}");
                return;
            }
            for (int i = 0; i < expectedList.Count; i++)
            {
                Compare(expectedList[i], actualList[i], $"{path}[{i}]", differences, depth + 1);
            }
            return;
        }

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ValueTuple<,>))
        {
            foreach (FieldInfo field in type.GetFields())
            {
                Compare(field.GetValue(expected), field.GetValue(actual), $"{path}.{field.Name}", differences, depth + 1);
            }
            return;
        }

        foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
        {
            Compare(field.GetValue(expected), field.GetValue(actual), $"{path}.{field.Name}", differences, depth + 1);
        }

        foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (property.GetIndexParameters().Length != 0 || !property.CanRead)
            {
                continue;
            }

            // Computed members (tiles, StrippedNameLower, ...) can throw for the same reason on both sides.
            object expectedValue = ReadMember(property, expected, out string expectedError);
            object actualValue = ReadMember(property, actual, out string actualError);
            if (expectedError != null || actualError != null)
            {
                if (expectedError != actualError)
                {
                    differences.Add($"{path}.{property.Name}: expected {expectedError ?? "a value"} but was {actualError ?? "a value"}");
                }
                continue;
            }

            Compare(expectedValue, actualValue, $"{path}.{property.Name}", differences, depth + 1);
        }
    }

    static object ReadMember(PropertyInfo property, object instance, out string error)
    {
        try
        {
            error = null;
            return property.GetValue(instance);
        }
        catch (TargetInvocationException e)
        {
            error = e.InnerException.GetType().Name + ": " + e.InnerException.Message;
            return null;
        }
    }

    #endregion
}
