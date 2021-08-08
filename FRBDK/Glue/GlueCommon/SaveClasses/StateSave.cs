using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Content.Instructions;
using System.ComponentModel;
using System.Xml.Serialization;
using Newtonsoft.Json;
using GlueCommon.Converters;

namespace FlatRedBall.Glue.SaveClasses
{

    [TypeConverter(typeof(SerializableExpandableObjectConverter))]
    public class StateSave
    {
        #region Fields

        public List<InstructionSave> InstructionSaves = new List<InstructionSave>();

        #endregion

        #region Properties


        public string Name
        {
            get;
            set;
        }

        #endregion

        [XmlIgnore]
        [JsonIgnore]
        public static Func<StateSave, string> ToStringDelegate;

        #region Methods

        public StateSave Clone()
        {
            StateSave newStateSave = this.MemberwiseClone() as StateSave;

            newStateSave.InstructionSaves = new List<InstructionSave>();

            foreach (InstructionSave instructionSave in InstructionSaves)
            {
                newStateSave.InstructionSaves.Add(instructionSave.Clone());
            }

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
