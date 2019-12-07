using FlatRedBall.Glue.Plugins.CodeGenerators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DialogTreePlugin.Generators
{
    public class RootObjectCodeGenerator : FullFileCodeGenerator
    {
        static RootObjectCodeGenerator mSelf;

        public static RootObjectCodeGenerator Self
        {
            get
            {
                if (mSelf == null) mSelf = new RootObjectCodeGenerator();
                return mSelf;
            }
        }

        public override string RelativeFile => "DialogTreePlugin/RootObject.Generated.cs";

        protected override string GenerateFileContents()
        {
            var contents = @"

using System.IO;
using System.Runtime.Serialization.Json;

namespace DialogTreePlugin.SaveClasses
{
    public class DialogTreeRaw
    {
        public class RootObject
        {
            public Passage[] passages { get; set; }
            public string name { get; set; }
            public string startnode { get; set; }
            public string creator { get; set; }
            public string creatorversion { get; set; }
            public string ifid { get; set; }

            public static RootObject FromJson(string fileName)
            {
                DialogTreeRaw.RootObject deserializedDialogTree;

                using(Stream openStream = new FileStream(fileName, FileMode.Open))
                {
                    var serializer = new DataContractJsonSerializer(typeof(DialogTreeRaw.RootObject));
                    deserializedDialogTree = (DialogTreeRaw.RootObject)serializer.ReadObject(openStream);
                }

                return deserializedDialogTree;
            }
        }

        public class Passage
        {
            public string text { get; set; }
            public Link[] links { get; set; }
            public string name { get; set; }
            public string pid { get; set; }
            public string[] tags { get; set; }

            public Position position { get; set; }
        }

        public class Link
        {
            public string name { get; set; }
            public string link { get; set; }
            public string pid { get; set; }
        }

        public class Position
        {
            public int x { get; set; }
            public int y { get; set; }
        }

    }
}

";

            return contents;
        }
    }
}
