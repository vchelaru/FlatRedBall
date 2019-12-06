using System.IO;
using System.Runtime.Serialization.Json;

namespace DialogTreePlugin.SaveClasses
{
    public class DialogTreeRaw
    {
        public class Rootobject
        {
            public Passage[] passages { get; set; }
            public string name { get; set; }
            public string startnode { get; set; }
            public string creator { get; set; }
            public string creatorversion { get; set; }
            public string ifid { get; set; }

            public static Rootobject FromJson(string fileName)
            {
                DialogTreeRaw.Rootobject deserializedDialogTree;

                using(Stream openStream = new FileStream(fileName, FileMode.Open))
                {
                    var serializer = new DataContractJsonSerializer(typeof(DialogTreeRaw.Rootobject));
                    deserializedDialogTree = (DialogTreeRaw.Rootobject)serializer.ReadObject(openStream);
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

            public DialogTreeConverted.Passage ToConvertedPassage()
            {
                return new DialogTreeConverted.Passage()
                {
                    pid = int.Parse(this.pid),
                    tags = (string[])this.tags?.Clone()
                };
            }
        }

        public class Link
        {
            public string name { get; set; }
            public string link { get; set; }
            public string pid { get; set; }

            public DialogTreeConverted.Link ToConvertedLink()
            {
                return new DialogTreeConverted.Link()
                {
                    pid = int.Parse(this.pid)
                };
            }
        }

        public class Position
        {
            public int x { get; set; }
            public int y { get; set; }
        }


    }

    public class DialogTreeConverted
    {
        public class Rootobject
        {
            public Passage[] passages { get; set; }
            public string name { get; set; }
            public int startnodepid { get; set; }
            public string pluginversion { get; set; }
        }

        public class Passage
        {
            public string stringid { get; set; }
            public Link[] links { get; set; }
            public int pid { get; set; }
            private string[] mTags;
            public string[] tags
            {
                get => mTags ?? new string[0];
                set { mTags = value; }
            }
        }

        public class Link
        {
            public string stringid { get; set; }
            public int pid { get; set; }
        }
    }
}
