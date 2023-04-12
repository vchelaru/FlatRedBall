using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlatRedBall.Glue.SaveClasses
{
    // A reference to a ReferencedFileSave.  This is
    // needed by objects which must generate code against
    // a specific file in a specific container - such as variables
    // which pull values from a specific file.
    public class ReferencedFileReference
    {
        public string ContainerName { get; set; }
        public string RfsName { get; set; }
    }
}
