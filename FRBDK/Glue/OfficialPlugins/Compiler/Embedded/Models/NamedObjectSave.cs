using FlatRedBall.Content.Instructions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace {ProjectNamespace}.GlueControl.Models
{
    public enum SourceType
    {
        File,
        Entity,
        FlatRedBallType,
        CustomType
    }


    class NamedObjectSave
    {
        public SourceType SourceType
        {
            get;
            set;
        }

        public string SourceClassType
        {
            get;
            set;
        }

        public string InstanceName
        {
            get;
            set;
        }

        public List<InstructionSave> InstructionSaves = new List<InstructionSave>();

    }
}
