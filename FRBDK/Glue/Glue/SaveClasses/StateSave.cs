using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Content.Instructions;
using System.ComponentModel;
using System.Xml.Serialization;


#if GLUE
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.Controls.PropertyGridControls;
using FlatRedBall.Glue.Controls;
using System.Windows.Forms;
using FlatRedBall.Instructions.Reflection;
using FlatRedBall.Glue.GuiDisplay;
#endif

namespace FlatRedBall.Glue.SaveClasses
{

    [TypeConverter(typeof(ExpandableObjectConverter))]
    public class StateSave
    {
        #region Fields

        public List<InstructionSave> InstructionSaves = new List<InstructionSave>();

        // As the class NamedObjectPropertyOverride indicates, I don't think we're 
        // using this anymore.  It's causing Glue to take a long time in reloading .glux
        // files because of the comparisons that must be made.  Therefore, I'm going to remove
        // it to make reloads faster.
        //public List<NamedObjectPropertyOverride> NamedObjectPropertyOverrides = new List<NamedObjectPropertyOverride>();
        //public bool ShouldSerializeNamedObjectPropertyOverrides()
        //{
        //    return NamedObjectPropertyOverrides != null && NamedObjectPropertyOverrides.Count != 0;
        //}

        #endregion

        #region Properties


        public string Name
        {
            get;
            set;
        }

        #endregion

        [XmlIgnore]
        public static Func<StateSave, string> ToStringDelegate;

        #region Methods

        public StateSave Clone()
        {
            StateSave newStateSave = this.MemberwiseClone() as StateSave;

            newStateSave.InstructionSaves = new List<InstructionSave>();
            // I don't think we're going to use these anymore
            //newStateSave.NamedObjectPropertyOverrides = new List<NamedObjectPropertyOverride>();

            foreach (InstructionSave instructionSave in InstructionSaves)
            {
                newStateSave.InstructionSaves.Add(instructionSave.Clone());
            }

            // I don't think we're going to use these anymore
            //foreach (NamedObjectPropertyOverride nopo in NamedObjectPropertyOverrides)
            //{
            //    newStateSave.NamedObjectPropertyOverrides.Add(nopo.Clone());
            //}

            return newStateSave;
        }

        public bool AssignsVariable(CustomVariable variable)
        {
            foreach (InstructionSave instructionSave in this.InstructionSaves)
            {
                if (instructionSave.Value != null && instructionSave.Member == variable.Name)
                {
                    return true;
                }
            }
            return false;
        }

        // I don't think we're going to use these anymore
        //public NamedObjectPropertyOverride GetNamedObjectOverride(string namedObjectName)
        //{
        //    foreach (NamedObjectPropertyOverride propertyOverride in NamedObjectPropertyOverrides)
        //    {
        //        if (propertyOverride.Name == namedObjectName)
        //        {
        //            return propertyOverride;
        //        }
        //    }
        //    return null;
        //}            

        
        public void SortInstructionSaves(List<CustomVariable> customVariables)
        {
            int insertLocation = 0;

            for (int i = InstructionSaves.Count - 1; i > -1; i--)
            {
                InstructionSave instruction = InstructionSaves[i];
                bool wasFound = false;
                for (int j = 0; j < customVariables.Count; j++)
                {
                    if (customVariables[j].Name == instruction.Member)
                    {
                        wasFound = true;
                        break;
                    }
                }

                if (!wasFound)
                {
                    InstructionSaves.RemoveAt(i);
                }
            }


            for (int i = 0; i < customVariables.Count; i++)
            {
                string name = customVariables[i].Name;

                for (int j = insertLocation; j < InstructionSaves.Count; j++)
                {
                    InstructionSave instruction = InstructionSaves[j];

                    if (instruction.Member == name)
                    {
                        InstructionSaves.Remove(instruction);
                        InstructionSaves.Insert(insertLocation, instruction);
                        insertLocation++;
                        break;
                    }

                }
            }

        }

        public override string ToString()
        {
            if (ToStringDelegate != null)
            {
                return ToStringDelegate(this);
            }
            else
            {
                return Name + " (State)";
            }
        }

        #endregion
    }
}
