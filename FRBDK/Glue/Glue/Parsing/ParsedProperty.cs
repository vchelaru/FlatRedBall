using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.Parsing;

namespace FlatRedBall.Glue.Parsing
{
    public class ParsedProperty
    {
        public Scope Scope;
        public ParsedType Type;
        public string Name;
        public bool IsVirtual;
        public bool IsStatic;

        public string GetContents;
        public string SetContents;

        public bool HasAutomaticGetter;
        public bool HasAutomaticSetter;

        public override string ToString()
        {
            return Scope + " " + Type + " " + Name + ";";
        }

        public ParsedProperty()
        {

        }


        public ParsedProperty(Scope scope, string type, string name)
        {
            Scope = scope;
            Type = new ParsedType(type);
            Name = name;
        }

        public ParsedProperty Clone()
        {
            ParsedProperty parsedPropety = (ParsedProperty)this.MemberwiseClone();

            parsedPropety.Type = Type.Clone();

            return parsedPropety;
        }

        internal static ParsedProperty FromPropertyInfo(System.Reflection.PropertyInfo property)
        {
            ParsedProperty toReturn = new ParsedProperty();
            toReturn.Name = property.Name;
            toReturn.Type = new ParsedType(property.PropertyType.Name);
            
            return toReturn;
        }
    }
}
