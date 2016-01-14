using System.Collections.Generic;
using System.Xml.Serialization;

namespace FlatRedBall.Glue.SaveClasses
{
    interface ITaggable
    {
        List<string> Tags { get; }
        string Source { get; }
    }
}
