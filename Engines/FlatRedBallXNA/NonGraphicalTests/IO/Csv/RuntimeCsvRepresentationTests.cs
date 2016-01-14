using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using FlatRedBall.IO.Csv;
using FlatRedBall.IO;

namespace NonGraphicalTests.IO.Csv
{
    public class TypeToSerialize
    {
        public float X;
        public bool Visible;

    }

    public class TypeToDeserialize
    {
        public float X;
        public bool Visible;
        public List<string> AvailableEnemies = new List<string>();
    }

    public class Container
    {
        public TypeToDeserialize Contained;

    }

    [TestFixture]
    public class RuntimeCsvRepresentationTests
    {



        [Test]
        public void TestSerialization()
        {
            RuntimeCsvRepresentation rcr = new RuntimeCsvRepresentation();

            rcr.Headers = new CsvHeader[4];
            rcr.Headers[0] = new CsvHeader("Header1");
            rcr.Headers[1] = new CsvHeader("Header2");
            rcr.Headers[2] = new CsvHeader("Newline Test1");
            rcr.Headers[3] = new CsvHeader("Something");
            rcr.Headers[3].IsRequired = true;

            rcr.Records = new List<string[]>();
            rcr.Records.Add(new string[4]);
            rcr.Records[0][0] = "NoQuotes \"Quotes\" NoQuotes";
            rcr.Records[0][1] = "Value = \"Something\"";
            rcr.Records[0][2] = string.Concat("Line 1", Environment.NewLine, "Line 2");
            rcr.Records[0][3] = "";

            string result = rcr.GenerateCsvString();
            if (!result.Contains("NoQuotes \"\"Quotes\"\" NoQuotes"))
            {
                throw new Exception("rcr.GenerateCsvString is not properly adding double quotes for quotes");
            }

            if (!result.Contains("\"Value = \"\"Something\"\"\""))
            {
                throw new Exception("rcr.GenerateCsvString is not properly wrapping text in quotes when it should");
            }
            
            if (!result.Contains(string.Concat("\"", rcr.Records[0][2], "\"")))
            {
                throw new Exception("rcr.GenerateCsvString is not properly wrapping newlined text in quotes");
            }

            FileManager.SaveText(result, "SavedCsv.csv");

            RuntimeCsvRepresentation loaded = CsvFileManager.CsvDeserializeToRuntime("SavedCsv.csv");

            for (int i = 0; i < loaded.Records.Count; i++)
            {
                for (int j = 0; j < loaded.Records[i].Length; j++)
                {
                    var loadedAtIandJ = loaded.Records[i][j];
                    var rcrAtIandJ = rcr.Records[i][j];
                    if (loadedAtIandJ != rcrAtIandJ)
                    {
                        throw new Exception("Loaded RCR doesn't equal what was saved");
                    }
                }
            }

        }

        [Test]
        public void TestCreateObjectList()
        {
            RuntimeCsvRepresentation rcr = new RuntimeCsvRepresentation();

            rcr.Headers = new CsvHeader[3];
            rcr.Headers[0] = new CsvHeader("X (float)");
            rcr.Headers[1] = new CsvHeader("Visible (bool)");
            rcr.Headers[2] = new CsvHeader("AvailableEnemies (List<string>)");

            rcr.Records = new List<string[]>();
            rcr.Records.Add(new string[3]);
            rcr.Records[0][0] = "3";
            rcr.Records[0][1] = "true";
            rcr.Records[0][2] = "Dragon";

            List<TypeToDeserialize> listToPopulate = new List<TypeToDeserialize>();

            rcr.CreateObjectList(typeof(TypeToDeserialize), listToPopulate);

            if (listToPopulate[0].AvailableEnemies.Count != 1 || listToPopulate[0].AvailableEnemies[0] != "Dragon")
            {
                throw new Exception("Lists are not being deserialized properly");
            }

        }

        [Test]
        public void TestComplexTypes()
        {
            RuntimeCsvRepresentation rcr = new RuntimeCsvRepresentation();

            rcr.Headers = new CsvHeader[1];
            rcr.Headers[0] = new CsvHeader("Contained (NonGraphicalTests.IO.Csv.TypeToDeserialize)");

            rcr.Records = new List<string[]>();
            rcr.Records.Add(new string[1]);
            rcr.Records[0][0] = "X = 4";

            List<Container> listToPopulate = new List<Container>();

            rcr.CreateObjectList(typeof(Container), listToPopulate);

            if (listToPopulate[0].Contained.X != 4)
            {
                throw new Exception("Complex types are not being deserialized properly");
            }


            rcr.Records[0][0] = "AvailableEnemies = (\"Dragon\")";
            listToPopulate = new List<Container>();

            rcr.CreateObjectList(typeof(Container), listToPopulate);

            if (listToPopulate[0].Contained.AvailableEnemies[0] != "Dragon")
            {
                throw new Exception("Inline lists in complex objects are not deserializing properly");
            }
        }


    }
}
