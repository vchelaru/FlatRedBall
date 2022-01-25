using FlatRedBall.Content.Instructions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlatRedBall.Glue.SaveClasses
{
    /// <summary>
    /// Identifies a custom class defined in Glue which is populated during code gen from columns in CSVs.
    /// </summary>
    public class CustomClassSave
    {
        public string Name;
        public bool GenerateCode { get; set; }
        public string CustomNamespace { get; set; }

        public List<string> CsvFilesUsingThis = new List<string>();

        public List<InstructionSave> RequiredProperties = new List<InstructionSave>();

        public CustomClassSave()
        {
            GenerateCode = true;
        }

        public override string ToString()
        {
            return $"{Name} ({RequiredProperties.Count} props)";
        }

    }
}
