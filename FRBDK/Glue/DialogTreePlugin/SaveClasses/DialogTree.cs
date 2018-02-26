namespace DialogTreePlugin.SaveClasses
{
    public class DialogTreeRaw
    {

        public class Rootobject
        {
            public Passage[] passages { get; set; }
            public string name { get; set; }
            public string startnode { get; set; }
            public string creatorversion { get; set; }
            public string ifid { get; set; }
        }

        public class Passage
        {
            public string text { get; set; }
            public Link[] links { get; set; }
            public string name { get; set; }
            public string pid { get; set; }
            public string[] tags { get; set; }

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
            public string[] tags { get; set; }
        }

        public class Link
        {
            public string stringid { get; set; }
            public int pid { get; set; }
        }
    }
}
