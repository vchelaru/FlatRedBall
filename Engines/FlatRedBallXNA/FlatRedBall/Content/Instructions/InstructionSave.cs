using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

using FlatRedBall.Utilities;
using FlatRedBall.Instructions;

#if !FRB_RAW
using GenericInstruction = FlatRedBall.Instructions.GenericInstruction;
using System.ComponentModel;
using Microsoft.Xna.Framework;
#endif

namespace FlatRedBall.Content.Instructions
{
    public class InstructionSave
    {
        #region Fields
        public string Type;
        public string TargetName;
        public string Member;

        [XmlElement("ValueAsString", typeof(string))]
        [XmlElement("ValueAsFloat", typeof(float))]

        [XmlElement("ValueAsInt", typeof(int))]
        [XmlElement("ValueAsBool", typeof(bool))]
        [XmlElement("ValueAsLong", typeof(long))]
        [XmlElement("ValueAsDouble", typeof(double))]
        [XmlElement("ValueAsByte", typeof(byte))]
        [XmlElement("ValueAsObject", typeof(object))]
        [XmlElement("ValueAsVector2", typeof(Vector2))]
        [XmlElement("ValueAsVector3", typeof(Vector3))]
        [XmlElement("ValueAsListOfVector2", typeof(List<Vector2>))]
        [XmlElement("ValueAsListOfVector3", typeof(List<Vector3>))]

        // Referencing a List of Points causes load errors
        // in Glue becaus there's ambiguity between System.Drawing.Point
        // and FlatRedBall.math.Geometry.Points.  
        // I don't know why the deserializer can't keep track of the type, but
        // instead of looking into it I'm just going to support lists of Vectors.
        //[XmlElement("ValueAsListOfPoints", typeof(List<FlatRedBall.Math.Geometry.Point>))]

        // Can't do this because it's an abstract class
        //[XmlElement("ValueAsEnum", typeof(Enum))]
        public object Value;

        [DefaultValue(0)]
        public double Time;
        #endregion

        #region Methods


        public InstructionSave Clone()
        {
            return (InstructionSave)this.MemberwiseClone();
        }

        public T Clone<T>() where T : InstructionSave, new()
        {
            T newInstance = new T();

            newInstance.Type = this.Type;
            newInstance.TargetName = this.TargetName;
            newInstance.Member = this.Member;

            newInstance.Value = this.Value;
            newInstance.Time = this.Time;

            return newInstance;
        }

#if !FRB_RAW
        public static InstructionSave FromInstructionBlueprint(FlatRedBall.Instructions.InstructionBlueprint template)
        {
            InstructionSave toReturn = new InstructionSave();

            toReturn.SetValuesFromTemplate(template);

            return toReturn;
        }

        public static InstructionSave FromInstruction(FlatRedBall.Instructions.GenericInstruction instruction)
        {
            InstructionSave instructionSaveToReturn = new InstructionSave();

            instructionSaveToReturn.SetValuesFromInstruction(instruction);

            return instructionSaveToReturn;
        }

        public void SetValuesFromTemplate(FlatRedBall.Instructions.InstructionBlueprint template)
        {
            Type = template.MemberType.FullName;
            Member = template.MemberName;
            Value = template.MemberValue as object;
            Time = template.Time;
            TargetName = template.TargetType.FullName;
            
        }

        public void SetValuesFromInstruction(FlatRedBall.Instructions.GenericInstruction instruction)
        {
            Type = instruction.TypeAsString;
            
            if(instruction.Target is INameable)
            {
                TargetName = ((INameable)(instruction.Target)).Name;
            }
            else
            {
                throw new NotSupportedException("Attempting to save an instruction that references an object that is not INameable");
            }

            Member = instruction.Member;
            Value = instruction.MemberValueAsObject;
            Time = instruction.TimeToExecute;
            
        }

        public InstructionBlueprint ToInstructionBlueprint()
        {
            InstructionBlueprint toReturn = new InstructionBlueprint();

            toReturn.MemberType = System.Type.GetType(Type);
            toReturn.MemberName = Member;
            toReturn.MemberValue = Value;
            toReturn.Time = Time;
            toReturn.TargetType = System.Type.GetType(TargetName);

            return toReturn;
        }

        public GenericInstruction ToInstruction(object target)
        {
            // Get the type of the property.  It could be null so can't do Value.GetType()
            Type typeOfTarget = target.GetType();

            MemberInfo[] memberInfos = typeOfTarget.GetMember(Member);

            Type typeOfMember = null;

            FieldInfo fieldInfo = typeOfTarget.GetField(Member);
            if (fieldInfo != null)
                typeOfMember = fieldInfo.FieldType;
            if (typeOfMember == null)
            {
                PropertyInfo propertyInfo = typeOfTarget.GetProperty(Member);

                typeOfMember = propertyInfo.PropertyType;
            }

            Type t = typeof(Instruction< , >).MakeGenericType(
                target.GetType(),
                typeOfMember);

            ConstructorInfo ctor = t.GetConstructor(
                BindingFlags.Instance | BindingFlags.Public, 
                null,
                new Type[] { target.GetType(), typeof(string), typeOfMember, typeof(double) }, 
                null);

            GenericInstruction instruction = (GenericInstruction)ctor.Invoke(
                    new Object[]                 {
                        target,
                        Member,
                        Value,
                        Time
                    }
                );

            return instruction;
        }
#endif

        public override string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder(30);
            
            if(!string.IsNullOrEmpty(TargetName))
            {
                stringBuilder.Append(TargetName).Append(".");
            }

            stringBuilder.Append(Member).Append(" = ");

            if (Value != null)
            {
                stringBuilder.Append(Value.ToString());
            }
            else
            {
                stringBuilder.Append("<NULL>");
            }

                
            return stringBuilder.ToString();

        }

        #endregion
    }
}
