using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlatRedBall.Glue.Parsing
{
    public class ParsedField
    {
        public Scope Scope;
        public ParsedType Type;
        public string Name;
        public string ValueToAssignTo;

        public bool IsConst = false;

        public bool IsStatic;

        public ParsedField()
        {

        }

        public ParsedField(Scope scope, string type, string name)
        {
            Scope = scope;
            Type = new ParsedType(type);
            Name = name;
        }

        public ParsedField Clone()
        {
            ParsedField newField = (ParsedField)this.MemberwiseClone();
            newField.Type = Type.Clone();

            return newField;
        }


        public override string ToString()
        {
            string constString = "";

            if (IsConst)
            {
                constString = "const ";
            }

            string assignmentString = "";

            if (ValueToAssignTo != null)
            {
                assignmentString = " = " + ValueToAssignTo;
            }

                return Scope + " " + constString + Type + " " + Name + assignmentString + ";";
        }

        internal static ParsedField FromFieldInfo(System.Reflection.FieldInfo field)
        {
            ParsedField toReturn = new ParsedField();
            toReturn.Name = field.Name;
            toReturn.Type = new ParsedType( field.FieldType.Name);
            toReturn.IsStatic = field.IsStatic;

            return toReturn;
        }
    }
}
