using FlatRedBall.IO.Csv;
using Shouldly;
using System.Text;

namespace EngineUnitTests.Utilities;

/// <summary>
/// Pins the reflection-based CSV deserialization that Glue-generated code calls, so the
/// trimming annotations and the key lookup stay behavior-preserving.
/// </summary>
public class RuntimeCsvRepresentationTests
{
    public class CarData
    {
        public string Name = "";
        public int Cylinders;
        public List<string>? Tags;
    }

    public class PropertyKeyedCarData
    {
        public string Name { get; set; } = "";
        public float TopSpeed { get; set; }
    }

    static RuntimeCsvRepresentation Load(string csv)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        return CsvFileManager.CsvDeserializeToRuntime<RuntimeCsvRepresentation>(stream);
    }

    [Fact]
    public void FillObjectDictionary_KeysByRequiredField_AndFoldsContinuationRowsIntoList()
    {
        var rcr = Load(
            "\"Name (string, required)\",Cylinders (int),Tags (List<string>)\n" +
            "Sedan,4,family\n" +
            ",,commuter\n" +
            "Truck,8,work\n");

        var dictionary = new Dictionary<string, CarData>();
        rcr.FillObjectDictionary(dictionary);

        dictionary.Keys.ShouldBe(new[] { "Sedan", "Truck" }, ignoreOrder: true);
        dictionary["Sedan"].Cylinders.ShouldBe(4);
        dictionary["Sedan"].Tags.ShouldBe(new[] { "family", "commuter" });
        dictionary["Truck"].Tags.ShouldBe(new[] { "work" });
    }

    [Fact]
    public void FillObjectDictionary_KeysByRequiredProperty()
    {
        var rcr = Load(
            "\"Name (string, required)\",TopSpeed (float)\n" +
            "Fast,200.5\n");

        var dictionary = new Dictionary<string, PropertyKeyedCarData>();
        rcr.FillObjectDictionary(dictionary);

        dictionary["Fast"].TopSpeed.ShouldBe(200.5f);
    }

    [Fact]
    public void FillObjectDictionary_KeyColumnWithoutMatchingMember_Throws()
    {
        var rcr = Load(
            "\"Missing (string, required)\",Cylinders (int)\n" +
            "Sedan,4\n");

        var dictionary = new Dictionary<string, CarData>();
        Should.Throw<InvalidOperationException>(() => rcr.FillObjectDictionary(dictionary));
    }

    [Fact]
    public void CreateObjectList_FillsFieldsFromRows()
    {
        var rcr = Load(
            "Name,Cylinders (int)\n" +
            "Sedan,4\n" +
            "Truck,8\n");

        var list = new List<CarData>();
        rcr.CreateObjectList(typeof(CarData), list);

        list.Select(c => c.Name).ShouldBe(new[] { "Sedan", "Truck" });
        list.Select(c => c.Cylinders).ShouldBe(new[] { 4, 8 });
    }
}
