using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using Microsoft.Xna.Framework;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.Parsing;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Graphics.Animation;
using Microsoft.Xna.Framework.Graphics;
using FlatRedBall.Math;
using FlatRedBall.ManagedSpriteGroups;
using FlatRedBall.Graphics;
using System.Windows.Forms;

using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.AutomatedGlue;
using System.Collections.ObjectModel;
using FlatRedBall.Utilities;
using FlatRedBall.Instructions.Reflection;
using FlatRedBall.Math.Geometry;


namespace FlatRedBall.Glue.Reflection
{
    public static class ExposedVariableManager
    {
        #region Fields

        static string[] mAvailablePrimitives = new string[]{
            "<none>",
            "float",
            "float?",
            "decimal",
            "decimal?",
            "string",
            "bool",

            "int",
            "int?",
            "double",


            "byte",
            "long",

            "Microsoft.Xna.Framework.Graphics.Texture2D",
            "Microsoft.Xna.Framework.Input.Keys"

        };

        static List<MemberWithType> mPositionedObjectMembers;

        // Leave this as null until Initialize is called
        static List<Type> mTypesNotSupported;

        static List<string> mPositionedObjectReservedMembers = new List<string>();

        #endregion

        #region Properties

        public static List<MemberWithType> PositionedObjectMembers
        {
            get { return mPositionedObjectMembers; }
        }

        public static string[] AvailablePrimitives
        {
            get { return mAvailablePrimitives; }
        }

        #endregion

        public static void Initialize()
        {
            try
            {
                InitializeTypesNotSupported();


                mPositionedObjectMembers = new List<MemberWithType>();

                Type type = typeof(PositionedObject);

                var listToFill = mPositionedObjectMembers;

                FillListWithAvailableVariablesInType(type, listToFill);


                RemoveUnwantedVariables();

                ReorganizeVariables();

            }
            catch (Exception e)
            {
                GlueGui.ShowMessageBox("Error in ExposedVariableManager " + e.ToString(), "Error");
                throw new Exception("Error in ExposedVariableManager.Initialize", e);
            }
        }

        private static bool ShouldIncludeField(FieldInfo fieldInfo, Type owner)
        {
            if(mTypesNotSupported.Contains(fieldInfo.FieldType))
            {
                return false;
            }
            if (fieldInfo.FieldType.GetMethod("Invoke") != null)
            {
                return false;
            }

            var name = fieldInfo.Name;
            if(fieldInfo.DeclaringType == typeof(PositionedObject) )
            {

                if(name.EndsWith("Velocity") || name.EndsWith("Acceleration") || name.StartsWith("Relative"))
                {
                    return false;
                }
            }
            else if(fieldInfo.DeclaringType == typeof(Circle))
            {
                if (name == "LastMoveCollisionReposition")
                {
                    return false;
                }
            }



            return true;
        }

        private static bool ShouldIncludeProperty(PropertyInfo propertyInfo, Type owner)
        {
            if (propertyInfo.CanWrite == false)
            {
                return false;
            }
            
            if(mTypesNotSupported.Contains(propertyInfo.PropertyType))
            {
                return false;
            }
            if( propertyInfo.Name == "Name" // This isn't exposable because you change this on the object itself rather than on the underlying FRB type
                )
            {
                return false;
            }

            var declaringType = propertyInfo.DeclaringType;
            var name = propertyInfo.Name;



            if(name == "CursorSelectable")
            {
                if(declaringType == typeof(Sprite))
                {
                    return false;
                }
            }

            //if(name == "ScaleXVelocity" || name == "ScaleYVelocity")
            //{
            //    if(declaringType == typeof(Sprite) || declaringType == typeof(AxisAlignedRectangle))
            //    {
            //        return false;
            //    }
            //}
            
            if (owner == typeof(Circle))
            {
                if (name == "RotationX" || name == "RotationY" || name == "RotationZ" || name == "RadiusVelocity"
                     || name == "ParentRotationChangesRotation")
                {
                    return false;
                }
            }
            if (owner == typeof(AxisAlignedRectangle))
            {
                if (
                    // AARects expose color as well as red/green/blue, but we 
                    // should simplify it to color in Glue:
                    name == "Red" || name == "Green" || name == "Blue" ||
                    name == "RelativeTop" || name == "RelativeBottom" || name == "RelativeLeft" || name == "RelativeRight" ||
                    name == "RotationX" || name == "RotationY" || name == "RotationZ" || name == "ParentRotationChangesRotation")
                {
                    return false;
                }
            }
            if(owner == typeof(Text))
            {
                if (
                    name == "CameraToAdjustPixelPerfectTo" || name == "ContentManager" ||
                    name == "CursorSelectable" || name == "LayerToAdjustPixelPerfectTo" ||
                    name == "PreRenderedTexture" || name == "PreRenderedTexture" || 
                    
                    name == "SpriteFont" || name == "RenderOnTexture"
             
                    )
                {
                    return false;
                }
            }

            return true;
        }

        private static void FillListWithAvailableVariablesInType(Type type, List<MemberWithType> listToFill)
        {
            if (mTypesNotSupported == null)
            {
                throw new Exception("The ExposedVariableManager must first be Initialized before used.");
            }
            FieldInfo[] fields = type.GetFields(
                BindingFlags.Instance | BindingFlags.Public);

            PropertyInfo[] properties = type.GetProperties(
                BindingFlags.Instance | BindingFlags.Public);

            List<MemberInfo> membersWithExplicitOrdering = new List<MemberInfo>();

            foreach (FieldInfo field in fields)
            {
                if (ShouldIncludeField(field, type))
                {
                    // If there's a export order property, don't add them just yet, sort them here then add them
                    if (field.GetCustomAttributes(typeof(ExportOrderAttribute), true).Length != 0)
                    {
                        membersWithExplicitOrdering.Add(field);
                    }
                    else
                    {
                        listToFill.Add(new MemberWithType() { Member = field.Name, Type = field.FieldType.Name });
                    }
                }
            }

            foreach (PropertyInfo property in properties)
            {
                if (ShouldIncludeProperty(property, type))
                {
                    if (property.GetCustomAttributes(typeof(ExportOrderAttribute), true).Length != 0)
                    {
                        membersWithExplicitOrdering.Add(property);
                    }
                    else 
                    {
                        listToFill.Add(new MemberWithType() { Member = property.Name, Type = property.PropertyType.Name });
                    }
                }
            }

            membersWithExplicitOrdering.Sort(SortByAttribute);

            // I found out that
            // the properties and
            // fields returned through
            // reflection are not in any
            // guaranteed order.  Therefore
            // we should enforce an order.
            listToFill.Sort((a,b)=>a.Member.CompareTo(b.Member));

            for (int i = 0; i < membersWithExplicitOrdering.Count; i++)
            {
                var orderedMember = membersWithExplicitOrdering[i];

                string name = membersWithExplicitOrdering[i].Name;
                string orderedType = "string";
                if (orderedMember is FieldInfo)
                {
                    orderedType = (orderedMember as FieldInfo).FieldType.Name;
                }
                else if (orderedMember is PropertyInfo)
                {
                    orderedType = (orderedMember as PropertyInfo).PropertyType.Name;
                }

                listToFill.Insert(i, new MemberWithType { Member = name, Type = orderedType });
            }
        }

        private static int SortByAttribute(MemberInfo first, MemberInfo second)
        {
            ExportOrderAttribute firstAttribute = (ExportOrderAttribute)
                first.GetCustomAttributes(typeof(ExportOrderAttribute), true).FirstOrDefault();
            ExportOrderAttribute secondAttribute = (ExportOrderAttribute)
                second.GetCustomAttributes(typeof(ExportOrderAttribute), true).FirstOrDefault();

            return firstAttribute.OrderValue.CompareTo(secondAttribute.OrderValue);
        }

        private static Type GetTypeForMemberInType(Type type, string memberName)
        {
            FieldInfo field = type.GetField(memberName,
                BindingFlags.Instance | BindingFlags.Public);

            if (field != null)
            {
                return field.FieldType;
            }



            PropertyInfo property = type.GetProperty(memberName,
                BindingFlags.Instance | BindingFlags.Public);

            if (property != null)
            {
                return property.PropertyType;
            }


            return null;
        }

        public static string GetMemberTypeForEntity(string memberName, EntitySave entitySave)
        {
            string toReturn = "";
            if(memberName == "Visible")
            {
                return "bool";
            }
            else if (memberName == "Enabled")
            {
                return "bool";
            }
            else if (memberName == "CurrentState")
            {
                return "VariableState";
            }
            else if (TryGetStateInCategory(memberName, entitySave, out toReturn))
            {
                return toReturn;
            }
            else
            {
                return GetMemberTypeForPositionedObject(memberName);
            }
        }

        private static bool TryGetStateInCategory(string memberName, IElement entitySave, out string foundType)
        {
            foundType = "";
            if (entitySave.StateCategoryList.Count != 0 && memberName.StartsWith("Current") && memberName.EndsWith("State"))
            {
                string possibleCategory = StateSaveExtensionMethods.GetStateTypeFromCurrentVariableName(memberName);

                // See if there is a matching category
                StateSaveCategory category = entitySave.GetStateCategory(possibleCategory);


                if (category != null)
                {
                    foundType = category.Name;
                    return true;
                }
            }
            return false;
        }


        public static string GetMemberTypeForScreen(string memberName, ScreenSave screen)
        {
            string toReturn;

            if (memberName == "CurrentState")
            {
                return "VariableState";
            }
            else if (TryGetStateInCategory(memberName, screen, out toReturn))
            {
                return toReturn;
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        public static string GetMemberTypeForPositionedObject(string memberName)
        {
            Type positionedObjectType = typeof(PositionedObject);

            FieldInfo fieldInfo = positionedObjectType.GetField(memberName);

            if (fieldInfo != null)
            {
                return fieldInfo.FieldType.ToString();
            }
            else
            {
                PropertyInfo propertyInfo = positionedObjectType.GetProperty(memberName);

                if (propertyInfo != null)
                {
                    return propertyInfo.PropertyType.ToString();
                }
            }

            throw new ArgumentException();
        }

        public static string GetMemberTypeForNamedObject(NamedObjectSave namedObject, string variableName)
        {
            Type typeOfNamedObject = null;

            string foundType = null;

            var ati = namedObject.GetAssetTypeInfo();

            switch (namedObject.SourceType)
            {
                case SourceType.Entity:

                    EntitySave entitySave = ObjectFinder.Self.GetEntitySave(namedObject.SourceClassType);
                    typeOfNamedObject = typeof(PositionedObject);
                    if (entitySave != null)
                    {
                        CustomVariable customVariable = entitySave.GetCustomVariable(variableName);

                        if (customVariable == null)
                        {
                            if (variableName.StartsWith("Current") && variableName.EndsWith("State"))
                            {
                                if (variableName == "CurrentState")
                                {
                                    foundType = "VariableState";
                                }
                                else
                                {
                                    foundType = variableName.Substring("Current".Length, variableName.Length - ("Current".Length + "State".Length));
                                }
                            }
                                
                            else if (variableName == "Visible" && 
                                // Should this check recursively?
                                (entitySave.ImplementsIVisible || entitySave.InheritsFromFrbType()))
                            {
                                foundType = "bool";
                            }
                            else if (variableName == "Enabled" &&
                                entitySave.ImplementsIWindow)
                            {
                                foundType = "bool";
                            }

                        }
                        else if (entitySave.ContainsCustomVariable(variableName))
                        {
                            CustomVariable foundVariable = entitySave.GetCustomVariable(variableName);
                            if (!string.IsNullOrEmpty(foundVariable.OverridingPropertyType))
                            {
                                foundType = foundVariable.OverridingPropertyType;
                            }
                            else
                            {
                                foundType = foundVariable.Type;
                            }
                        }

                    }



                    break;
                case SourceType.File:
                    {
                        if(ati != null && ati.VariableDefinitions.Any(item => item.Name == variableName))
                        {
                            foundType = ati.VariableDefinitions.First(item => item.Name == variableName).Type;
                        }
                        else
                        {
                            string typeAsString = namedObject.InstanceType;

                            typeOfNamedObject = TypeManager.GetTypeFromString(
                                typeAsString);
                        }
                    }
                    break;
                case SourceType.FlatRedBallType:
                case SourceType.Gum:

                    if (variableName == "SourceFile")
                    {
                        // This may not be qualified, so let's try to get the asset type info:
                        if(ati != null)
                        {
                            return ati.QualifiedRuntimeTypeName.QualifiedType;
                        }
                        else
                        {
                            return namedObject.ClassType;
                        }
                    }
                    else if(ati != null && ati.VariableDefinitions.Any(item =>item.Name == variableName))
                    {
                        foundType = ati.VariableDefinitions.First(item => item.Name == variableName).Type;
                    }
                    else if (ati != null &&
                        !string.IsNullOrEmpty(ati.QualifiedRuntimeTypeName.QualifiedType))
                    {
                        typeOfNamedObject =
                            TypeManager.GetTypeFromString(ati.QualifiedRuntimeTypeName.QualifiedType);
                    }
                    else if (namedObject.IsList && variableName == "Visible")
                    {
                        foundType = "bool";
                    }


                    break;
            }

            if (!string.IsNullOrEmpty(foundType))
            {
                return foundType;
            }
            else if(typeOfNamedObject != null)
            {
                Type type = GetTypeForMemberInType(typeOfNamedObject, variableName);
                if(type == null)
                {
                    type = typeof(string);
                }

                string toReturn = type.Name;

                if (type.IsGenericType)
                {
                    var split = type.Name.Split('`');

                    string firstPart = split[0];

                    int numberOfParams = int.Parse(split[1]);

                    if (numberOfParams > 1)
                    {
                        // need to add support
                        //throw new Exception();
                    }
                    else
                    {
                        var result = type.GetGenericArguments();

                        toReturn = firstPart + "<" + result[0].Name + ">";
                    }
                }

                return toReturn;
            }
            else
            {
                GlueCommands.Self.PrintError($"Could not identify the variable type for {variableName} in {namedObject}");
                return null;
            }
        }

        private static List<MemberWithType> GetExposableMembersForEntity(EntitySave entitySave, bool removeAlreadyExposed,
            List<MemberWithType> returnValues)
        {
            if (entitySave.GetImplementsIVisibleRecursively())
            {
                returnValues.Add(new MemberWithType { Member = "Visible", Type = "bool" });
            }
            if (entitySave.GetImplementsIWindowRecursively())
            {
                returnValues.Add(new MemberWithType { Member = "Enabled", Type = "bool" });
            }

            returnValues.AddRange(mPositionedObjectMembers);

            return returnValues;
        }

        private static void AddStateVariables(IElement element, List<MemberWithType> returnValues)
        {
            bool shouldAddCurrentState = false;
            if (element.States.Count != 0)
            {
                shouldAddCurrentState = true;
            }
            foreach (StateSaveCategory category in element.StateCategoryList)
            {
                // That means this thing is its own variable:
                returnValues.Add(new MemberWithType { Member = "Current" + category.Name + "State", Type = "string" });
            }

            if (shouldAddCurrentState)
            {
                returnValues.Add(new MemberWithType { Member = "CurrentState", Type = "string" });
            }
        }

        public static List<MemberWithType> GetTunnelableMembersFor(EntitySave entitySave, bool removeAlreadyExposed)
        {
            List<MemberWithType> variables = GetExposableMembersFor(entitySave, removeAlreadyExposed);

            // March 4, 2012
            // I think this code
            // is a mistake - exposable
            // varibles are variables defined
            // by PositionedObject as well as variables
            // that come from IVisible and CurrentState.
            // I don't know why this is here....maybe because
            // at one point I had wanted variables defined in a
            // base class to be exposable?  Not sure.
            foreach (CustomVariable customVariable in entitySave.CustomVariables)
            {
                variables.Add(new MemberWithType { Member = customVariable.Name, Type = customVariable.Type });
            }

            variables = variables.Distinct(new MemberTypeComparer()).ToList();

            return variables;
        }

        private static void RemoveAlreadyExposedIfNecessary(bool removeAlreadyExposed, List<MemberWithType> returnValues, List<CustomVariable> customVariableList)
        {
            if (removeAlreadyExposed)
            {
                foreach (CustomVariable variable in customVariableList)
                {
                    if (string.IsNullOrEmpty(variable.SourceObject))
                    {
                        // This means it's not a tunneled variable.  Let's see if the returnValues contains this variable
                        if (returnValues.Any(item=>item.Member == variable.Name))
                        {
                            returnValues.RemoveAll(item=>item.Member == variable.Name);
                        }
                    }

                }
            }
        }

        private static List<MemberWithType> GetExposableMembersForScreen(ScreenSave screenSave, bool removeAlreadyExposed, 
            List<MemberWithType> returnValues)
        {
            // Currently there's nothing unique to Screens over Entities, so this method does nothing



            return returnValues;
        }

        public static List<MemberWithType> GetExposableMembersFor(IElement element, bool removeAlreadyExposed)
        {
            List<MemberWithType> toReturn = new List<MemberWithType>();

            if (element != null)
            {
                AddStateVariables(element, toReturn);

                if (element is ScreenSave)
                {
                    GetExposableMembersForScreen((ScreenSave)element, removeAlreadyExposed, toReturn);
                }
                else
                {
                    GetExposableMembersForEntity((EntitySave)element, removeAlreadyExposed, toReturn);
                }

                // The "Visible" property is automatically added if this guy is an IVisible.
                // This will eliminate double-entry if it's also exposed.
                //FlatRedBall.Utilities.StringFunctions.RemoveDuplicates(toReturn);
                toReturn = toReturn.Distinct(new MemberTypeComparer()).ToList();

                List<CustomVariable> customVariableList = element.CustomVariables;


                RemoveAlreadyExposedIfNecessary(removeAlreadyExposed, toReturn, customVariableList);
            }

            return toReturn;

        }

        public static bool IsExposedVariable(string variableName, NamedObjectSave namedObjectSave)
        {
            // Here we want variables that are explicitly exposed. If it's an Entity we don't want to 
            // look at what's exposable.  We want to get the actual variable and see if it exists:
            if (namedObjectSave.SourceType == SourceType.Entity)
            {
                EntitySave entitySave = ObjectFinder.Self.GetEntitySave(namedObjectSave.SourceClassType);
                if (entitySave != null)
                {
                    return entitySave.GetCustomVariableRecursively(variableName) != null;

                }
            }
            else
            {
                return GetExposableMembersFor(namedObjectSave).Any(item=>item.Member == variableName);
            }
            return false;
        }

        public static List<MemberWithType> GetExposableMembersFor(NamedObjectSave namedObjectSave)
        {
            List<MemberWithType> returnValue = new List<MemberWithType>();


            if (namedObjectSave != null)
            {
                switch (namedObjectSave.SourceType)
                {
                    case SourceType.Entity:
                        EntitySave entitySave = ObjectFinder.Self.GetEntitySave(namedObjectSave.SourceClassType);
                        if (entitySave == null)
                        {
                            return returnValue;
                        }
                        else
                        {
                            return GetTunnelableMembersFor(entitySave, false);
                        }
                    //break;
                    case SourceType.File:
                        Type type = null;

                        string typeAsString = namedObjectSave.InstanceType;

                        type = TypeManager.GetTypeFromString(
                            typeAsString);
                        if (type != null)
                        {
                            FillListWithAvailableVariablesInType(type, returnValue);

                        }

                        var assetTypeInfo = namedObjectSave.GetAssetTypeInfo();
                        if (assetTypeInfo != null)
                        {
                            FillFromVariableDefinitions(returnValue, assetTypeInfo);
                        }
                        break;
                    case SourceType.FlatRedBallType:
                    case SourceType.Gum:
                        FillWithExposableMembersForFlatRedBallType(namedObjectSave, returnValue);




                        break;
                }
            }

            return returnValue;
        }

        private static void FillWithExposableMembersForFlatRedBallType(NamedObjectSave namedObjectSave, List<MemberWithType> returnValue)
        {
            AssetTypeInfo assetTypeInfo = namedObjectSave.GetAssetTypeInfo();
            if (assetTypeInfo != null && !string.IsNullOrEmpty(assetTypeInfo.Extension))
            {
                returnValue.Add(new MemberWithType { Member = "SourceFile", Type = "string" });
            }

            // slowly move away from reflection:
            // To do this, the CSV has to include 
            // types for variables. Until it does, we
            // are going to continue to rely on reflection.
            if (assetTypeInfo != null && 
                    (assetTypeInfo.FriendlyName == "Sprite" || 
                    assetTypeInfo.FriendlyName == "AxisAlignedRectangle" ||
                    assetTypeInfo.FriendlyName == "Circle" ||
                    assetTypeInfo.FriendlyName == "Polygon" || 
                    assetTypeInfo.FriendlyName == "Layer"
                    
                    ))
            {

                if(assetTypeInfo.VariableDefinitions.Any(definition=> string.IsNullOrEmpty( definition.Type)))
                {
                    throw new InvalidOperationException("The type " + assetTypeInfo.FriendlyName + " has variables without a type");
                }

                var toAdd = assetTypeInfo.VariableDefinitions
                    .Select(definition => new MemberWithType { Member = definition.Name, Type = definition.Type });
                returnValue.AddRange(toAdd);
            } else 
            if (assetTypeInfo != null &&
                !string.IsNullOrEmpty(assetTypeInfo.QualifiedRuntimeTypeName.QualifiedType))
            {
                var type =
                    TypeManager.GetTypeFromString(assetTypeInfo.QualifiedRuntimeTypeName.QualifiedType);

                // We'll fall back to reflection, but eventually I'd like to see this go away
                if (type != null)
                {
                    FillListWithAvailableVariablesInType(type, returnValue);

                    AddSpecialCasePropertiesFor(type, returnValue);
                }

                FillFromVariableDefinitions(returnValue, assetTypeInfo);

            }
            else if (namedObjectSave.IsList && !string.IsNullOrEmpty(namedObjectSave.SourceClassGenericType))
            {
                // special case - if the list is of IVisibles, then
                // let's allow the user to set visibility on the whole
                // list.
                EntitySave entityTypeInList = ObjectFinder.Self.GetEntitySave(namedObjectSave.SourceClassGenericType);

                if (entityTypeInList != null && entityTypeInList.ImplementsIVisible)
                {
                    returnValue.Add(new MemberWithType { Member = "Visible", Type = "bool" });
                }
            }

            // June 23, 2013
            // I don't think we
            // want to sort here 
            // because the properties
            // will already be sorted in
            // a particular way from the function
            // FillListWithAvailableVariablesInType.
            // I'm going to comment this out to see if
            // it causes any problems.
            //returnValue.Sort();
        }

        private static void FillFromVariableDefinitions(List<MemberWithType> returnValue, AssetTypeInfo assetTypeInfo)
        {
            foreach (var variable in assetTypeInfo.VariableDefinitions)
            {
                bool isAlreadyHandled = returnValue.Any(item => item.Member == variable.Name);

                if (!isAlreadyHandled)
                {
                    returnValue.Add(new MemberWithType
                    {
                        Member = variable.Name,
                        Type = variable.Type

                    });
                }

                bool needsCustomType = isAlreadyHandled &&
                    !string.IsNullOrEmpty(variable.Type) &&
                    returnValue.Any(item => item.Member == variable.Name && AreTypesEquivalent(item.Type, variable.Type) == false);

                if (needsCustomType)
                {
                    var existing = returnValue.FirstOrDefault(item => item.Member == variable.Name);

                    existing.Type = variable.Type;
                }
            }
        }

        static bool AreTypesEquivalent(string type1, string type2)
        {
            if(type1 == type2)
            {
                return true;
            }

            if( (type1 == "float" && type2 == "Single") || (type2 == "float" && type1 == "Single"))
            {
                return true;
            }

            return false;
        }

        private static void AddSpecialCasePropertiesFor(Type type, List<MemberWithType> returnValue)
        {

        }

        public static bool IsMemberDefinedByEntity(string memberName, EntitySave entitySave)
        {
            if (memberName == "Visible" && entitySave.ImplementsIVisible)
            {
                return true;
            }
            else if (memberName == "Enabled" && entitySave.ImplementsIWindow)
            {
                return true;
            }
            else if (memberName == "CurrentState" && entitySave.HasStates)
            {
                return true;
            }
            else
            {
                return IsMemberDefinedByPositionedObject(memberName);
            }

        }

        public static bool IsMemberDefinedByPositionedObject(string memberName)
        {
            return mPositionedObjectMembers.Any(item=>item.Member == memberName);
        }

        public static bool IsReservedPositionedPositionedObjectMember(string memberName)
        {
            return mPositionedObjectReservedMembers.Contains(memberName);
        }

        private static void ReorganizeVariables()
        {
            mPositionedObjectMembers.RemoveAll(item => item.Member == "X");
            mPositionedObjectMembers.Insert(0, new MemberWithType { Member = "X", Type = "float" });

            mPositionedObjectMembers.RemoveAll(item => item.Member == "Y");
            mPositionedObjectMembers.Insert(1, new MemberWithType { Member = "Y", Type = "float" });

            mPositionedObjectMembers.RemoveAll(item => item.Member == "Z");
            mPositionedObjectMembers.Insert(2, new MemberWithType { Member = "Z", Type = "float" });

            mPositionedObjectMembers.RemoveAll(item => item.Member == "RelativeX");
            mPositionedObjectMembers.Insert(3, new MemberWithType { Member = "RelativeX", Type = "float" });

            mPositionedObjectMembers.RemoveAll(item => item.Member == "RelativeY");
            mPositionedObjectMembers.Insert(4, new MemberWithType { Member = "RelativeY", Type = "float" });

            mPositionedObjectMembers.RemoveAll(item => item.Member == "RelativeZ");
            mPositionedObjectMembers.Insert(5, new MemberWithType { Member = "RelativeZ", Type = "float" });
        }

        private static void RemoveUnwantedVariables()
        {
            // Update March 30, 2012
            // We used to just manually
            // define which variables we
            // don't want included, but this
            // is not as good as using reflection
            // to add all variables that we didn't
            // add to the mPositionedObjectMembers.
            // In other words, mPositionedObjectMembers
            // is the "authority" on what can be exposed
            // in Glue, and if it doesn't appear there, then
            // we should assume that it's because Glue can't handle
            // (or shouldn't handle) that member and we should mark it
            // as a reserved member.
            Type type = typeof(PositionedObject);
            FieldInfo[] fields = type.GetFields(
                    BindingFlags.Instance | BindingFlags.Public);

            PropertyInfo[] properties = type.GetProperties(
                BindingFlags.Instance | BindingFlags.Public);


            mPositionedObjectReservedMembers.Add("mLastDependencyUpdate");
            mPositionedObjectReservedMembers.Add("Name");

            mPositionedObjectMembers.RemoveAll(item=>item.Member=="mLastDependencyUpdate");
            mPositionedObjectMembers.RemoveAll(item=>item.Member == "Name");

            foreach (FieldInfo field in fields)
            {
                if (!mPositionedObjectMembers.Any(item=>item.Member==field.Name))
                {
                    mPositionedObjectReservedMembers.Add(field.Name);
                }
            }

            foreach (PropertyInfo property in properties)
            {
                if (!mPositionedObjectMembers.Any(item=>item.Member==property.Name))
                {
                    mPositionedObjectReservedMembers.Add(property.Name);
                }
            }

        }

        private static void InitializeTypesNotSupported()
        {
            mTypesNotSupported = new List<Type>();
            // Old not-supported types that are now supported
            // mTypesNotSupported.Add(typeof(Texture2D));
            //mTypesNotSupported.Add(typeof(Color));
            //mTypesNotSupported.Add(typeof(AnimationChainList));
            mTypesNotSupported.Add(typeof(TextureGrid<Texture>));
            mTypesNotSupported.Add(typeof(TextureGrid<AnimationChain>));
            mTypesNotSupported.Add(typeof(TextureGrid<FlatRedBall.Math.Geometry.FloatRectangle>));
            
            mTypesNotSupported.Add(typeof(Vector3));
            mTypesNotSupported.Add(typeof(Matrix));
            mTypesNotSupported.Add(typeof(AnimationChain));
            

            // Handle types in Scenes
            mTypesNotSupported.Add(typeof(SpriteList));
            mTypesNotSupported.Add(typeof(PositionedObjectList<SpriteFrame>));
            mTypesNotSupported.Add(typeof(PositionedObjectList<Text>));
            mTypesNotSupported.Add(typeof(List<SpriteGrid>));

            mTypesNotSupported.Add(typeof(ReadOnlyCollection<Sprite>));
            mTypesNotSupported.Add(typeof(ReadOnlyCollection<Text>));
            mTypesNotSupported.Add(typeof(ReadOnlyCollection<SpriteFrame>));


        }

        public static List<string> GetPositionedObjectRateVariables()
        {
            List<string> toReturn = new List<string>();

            foreach (var s in mPositionedObjectMembers.Select(item=>item.Member))
            {
                if (s.Contains("Velocity") || s.Contains("Acceleration"))
                {
                    toReturn.Add(s);
                }
            }

            return toReturn;
        }

        public static List<string> GetPositionedObjectRelativeValues()
        {
            List<string> toReturn = new List<string>();
            foreach (string s in mPositionedObjectMembers.Select(item=>item.Member))
            {
                if (s.StartsWith("Relative"))
                {
                    toReturn.Add(s);
                }
            }
            return toReturn;
        }

        public static List<string> GetAvailableNewVariableTypes(bool allowNone = true, bool includeStateCategories = false)
        {
            List<string> toReturn = new List<string>();

            toReturn.AddRange(mAvailablePrimitives);

            if (allowNone == false)
            {
                toReturn.Remove("<none>");
            }

            void TryAddRfsType(ReferencedFileSave rfs)
            {
                if (rfs.IsCsvOrTreatedAsCsv && !rfs.IsDatabaseForLocalizing)
                {
                    string type = rfs.GetTypeForCsvFile();
                    // Multiple CSVs may reference the same type, so make sure thsi isn't already added:
                    if(toReturn.Contains(type) == false)
                    {
                        toReturn.Add(type);
                    }
                }
            }
            foreach (ReferencedFileSave rfs in ObjectFinder.Self.GlueProject.GlobalFiles)
            {
                TryAddRfsType(rfs);
            }

            // We used to only include the CSVs for the current
            // element, but any CSV in the project will make a class
            // that is accessible from outside of that class, and we want
            // to make sure that we can do the same thing
            foreach (var element in ObjectFinder.Self.GlueProject.AllElements())
            {
                foreach(var rfs in element.ReferencedFiles)
                {
                    TryAddRfsType(rfs);
                }
            }

            // June 17, 2018
            // Currently only
            // Entity State Categories
            // are included here. Not sure
            // if screens are needed...if so
            // add them later.

            if (includeStateCategories)
            {
                foreach (IElement entity in ObjectFinder.Self.GlueProject.Entities)
                {
                    if (entity != null)
                    {
                        foreach(var category in entity.StateCategoryList)
                        {
                            string name = $"{entity.Name.Replace("\\", ".")}.{category.Name}";
                            toReturn.Add(name);
                        }
                    }
                }
            }

            return toReturn;

        }
    }
}
