{CompilerDirectives}

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

    public class PropertySave
    {
        public string Name;

        public object Value;

        public override string ToString()
        {
            return $"{Name} = {Value}";
        }
    }

    public static class PropertySaveListExtensions
    {
        public static object GetValue(this List<PropertySave> propertySaveList, string nameToSearchFor)
        {
            foreach (PropertySave propertySave in propertySaveList)
            {
                if (propertySave.Name == nameToSearchFor)
                {
                    return propertySave.Value;
                }
            }
            return null;
        }

        public static T GetValue<T>(this List<PropertySave> propertySaveList, string nameToSearchFor)
        {
            var copy = propertySaveList.ToArray();
            foreach (PropertySave propertySave in copy)
            {
                if (propertySave.Name == nameToSearchFor)
                {
                    var uncastedValue = propertySave.Value;
                    if (typeof(T) == typeof(int) && uncastedValue is long asLong)
                    {
                        return (T)((object)(int)asLong);
                    }
                    else if (typeof(T) == typeof(float) && uncastedValue is double asDouble)
                    {
                        return (T)((object)(float)asDouble);
                    }
                    else
                    {
                        return (T)propertySave.Value;
                    }
                }
            }
            return default(T);
        }
    }

    public class NamedObjectSave
    {
        public List<PropertySave> Properties
        {
            get;
            set;
        } = new List<PropertySave>();

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

        public bool AddToManagers
        {
            get; set;
        } = true; // true is the default and won't serialize if true


        public List<InstructionSave> InstructionSaves = new List<InstructionSave>();

    }
}
