{CompilerDirectives}

using FlatRedBall.Content.Instructions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;


namespace GlueControl.Models
{
    #region Enums

    public enum SourceType
    {
        File,
        Entity,
        FlatRedBallType,
        CustomType
    }

    #endregion

    #region PropertySave class

    public class PropertySave
    {
        public string Name;

        public object Value;

        public string Type { get; set; }


        public override string ToString()
        {
            return $"{Name} = {Value}";
        }
    }

    #endregion

    #region PropertySaveListExtensions

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
                    else if (typeof(T).IsEnum && uncastedValue is long asLong2)
                    {
                        return (T)((object)(int)asLong2);
                    }
                    else if (typeof(T).IsEnum && uncastedValue is int asInt)
                    {
                        return (T)((object)asInt);
                    }
                    else
                    {
                        return (T)propertySave.Value;
                    }
                }
            }
            return default(T);
        }

        public static void SetValue<T>(this List<PropertySave> propertySaveList, string nameToSearchFor, T value)
        {

            bool isDefault = IsValueDefault(value);

            if (isDefault)
            {
                propertySaveList.RemoveAll(item => item.Name == nameToSearchFor);
            }
            else
            {
                var existingProperty = propertySaveList.FirstOrDefault(item => item.Name == nameToSearchFor);
                if (existingProperty != null)
                {

                    existingProperty.Value = value;
                }
                else
                {
                    // If we got here then that means there isn't already something in place for this
                    PropertySave newPropertySave = new PropertySave();
                    newPropertySave.Name = nameToSearchFor;
                    newPropertySave.Value = value;

                    if (typeof(T) == typeof(int))
                    {
                        newPropertySave.Type = "int";
                    }
                    else if (typeof(T) == typeof(float))
                    {
                        newPropertySave.Type = "float";
                    }
                    else if (typeof(T) == typeof(decimal))
                    {
                        newPropertySave.Type = "decimal";
                    }
                    else
                    {
                        newPropertySave.Type = typeof(T).Name;
                    }


                    propertySaveList.Add(newPropertySave);
                }
            }
        }


        static bool IsValueDefault(object value)
        {
            if (value is bool)
            {
                return ((bool)value) == false;
            }
            if (value is byte)
            {
                return ((byte)value) == (byte)0;
            }
            if (value is double)
            {
                return ((double)value) == (double)0;
            }

            if (value is float)
            {
                return ((float)value) == 0.0f;
            }
            if (value is int)
            {
                return ((int)value) == 0;
            }

            if (value is long)
            {
                return ((long)value) == (long)0;
            }

            if (value is string)
            {
                return string.IsNullOrEmpty((string)value);
            }

            return false;


        }
    }
    #endregion

    public class NamedObjectSave
    {
        public string InstanceName
        {
            get;
            set;
        }

        public string SourceClassType
        {
            get;
            set;
        }

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

        public string SourceFile
        {
            get;
            set;
        }

        string mSourceClassGenericType;

        public string SourceClassGenericType
        {
            get { return mSourceClassGenericType; }
            set
            {
                mSourceClassGenericType = value;

                if (mSourceClassGenericType == "<NONE>")
                {
                    mSourceClassGenericType = null;
                }
            }
        }

        public List<InstructionSave> InstructionSaves = new List<InstructionSave>();

        public bool AddToManagers
        {
            get; set;
        }

        public bool AttachToContainer
        {
            get;
            set;
        }

        public bool AttachToCamera
        {
            get;
            set;
        }

        public bool IncludeInIVisible
        {
            get;
            set;
        }

        public bool IncludeInICollidable
        {
            get;
            set;
        }

        public bool IncludeInIClickable
        {
            get;
            set;
        }

        [System.Xml.Serialization.XmlIgnore]
        [JsonIgnore]
        public bool IsList
        {
            get
            {
                return SourceType == SourceType.FlatRedBallType &&
                    (SourceClassType == "PositionedObjectList<T>" || SourceClassType == "FlatRedBall.Math.PositionedObjectList<T>");
            }
        }

        [System.Xml.Serialization.XmlIgnore]
        [JsonIgnore]
        public bool IsEditingLocked
        {
            get => Properties.GetValue<bool>(nameof(IsEditingLocked));
            set => Properties.SetValue(nameof(IsEditingLocked), value);
        }

        public string LayerOn
        {
            get; set;
        }

        string mSourceName;

        public string SourceName
        {
            get
            {
                return mSourceName;
            }
            set
            {
                mSourceName = value;
            }
        }

        public string InstanceType
        {
            get
            {
                if (SourceType == SourceType.File)
                {
                    if (string.IsNullOrEmpty(SourceName) || SourceName == "<NONE>")
                    {
                        return "";
                    }
                    else
                    {
                        int openParenthesis = SourceName.LastIndexOf("(");
                        int length = SourceName.Length - 1 - openParenthesis;

                        return SourceName.Substring(openParenthesis + 1, length - 1);
                    }
                }
                else if (SourceType == SourceType.Entity)
                {
                    if (SourceClassType == null)
                    {
                        return null;
                    }
                    else
                    {
                        // Entities are stored as "Entity\WhateverEntity".  Let's remove tghe Entity part
                        return FlatRedBall.IO.FileManager.RemovePath(SourceClassType);
                    }
                }
                else
                {
                    return SourceClassType;
                }
            }
        }

        public string FieldName
        {
            get
            {

                //if (HasPublicProperty || SetByContainer)
                //{
                //    return "m" + InstanceName;
                //}
                //else
                {
                    return InstanceName;
                }

            }
        }

        public string ClassType
        {
            get
            {
                if (SourceType == SourceType.FlatRedBallType && !string.IsNullOrEmpty(InstanceType) &&
                    InstanceType.Contains("<T>"))
                {
                    string genericType = SourceClassGenericType;

                    if (genericType == null)
                    {
                        return null;
                    }
                    else
                    {

                        if (genericType.Contains("\\"))
                        {
                            // The namespace is part of it, so let's remove it
                            int lastSlash = genericType.LastIndexOf('\\');
                            genericType = genericType.Substring(lastSlash + 1);
                        }

                        return InstanceType.Replace("<T>", "<" + genericType + ">");
                    }
                }
                else
                {
                    return InstanceType;
                }
            }
        }

        public bool DefinedByBase
        {
            get;
            set;
        }

        public List<NamedObjectSave> ContainedObjects
        {
            get;
            set;
        } = new List<NamedObjectSave>();

        public NamedObjectSave()
        {
            //GenerateTimedEmit = true;
            //Instantiate = true;
            //mTypedMembersReadOnly = new ReadOnlyCollection<TypedMemberBase>(mTypedMembers);
            ////Events = new List<EventSave>();

            IncludeInIVisible = true;
            IncludeInIClickable = true;
            IncludeInICollidable = true;
            //CallActivity = true;

            //AttachToContainer = true;
            AddToManagers = true;

            //FulfillsRequirement = "<NONE>";

            //ContainedObjects = new List<NamedObjectSave>();

            //// Sept 25, 2020
            //// This used to be 
            //// true, but this causes
            //// unexpected behavior when 
            //// 2D games are resized. If we
            //// set this to false, then layers
            //// will automatically match the camera,
            //// which probably matches what the user expects
            ////IndependentOfCamera = true;
            //IndependentOfCamera = false;
        }

        public override string ToString()
        {
            //if (ToStringDelegate != null)
            //{
            //    return ToStringDelegate(this);
            //}
            //else
            {
                return ClassType + " " + FieldName;
            }
        }
    }
}
