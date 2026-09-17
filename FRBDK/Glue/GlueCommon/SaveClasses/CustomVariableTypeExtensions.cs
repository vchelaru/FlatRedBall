using System;
using System.Linq;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Parsing;
using static FlatRedBall.Glue.SaveClasses.GlueProjectSave;
using FlatRedBall.Instructions;

namespace FlatRedBall.Glue.SaveClasses
{
    /// <summary>
    /// Split out of <c>CustomVariableExtensionMethods</c> (in <c>Glue.csproj</c>, net8.0-windows): the
    /// type/state/file classification half of that class, which only needed <c>TypeManager.GetTypeFromString</c>
    /// (via <see cref="TypeResolutionCore"/>), <c>AvailableAssetTypes.Self.AllAssetTypes</c> (via
    /// <see cref="AvailableAssetTypesCore"/>) and <c>ObjectFinder.Self</c> (via <see cref="ObjectFinderCore"/>).
    /// Lives here (net8.0, no WPF) so it and its tests can build and run on Linux/macOS. See #2276. The
    /// non-extension static overloads (<c>GetIsFile(string)</c> etc.) needed their call sites updated to
    /// this class name; the extension methods resolved unchanged.
    /// </summary>
    public static class CustomVariableTypeExtensions
    {
        public static bool GetIsVariableState(this CustomVariable customVariable, GlueElement containingElement = null)
        {
            var result = ObjectFinderCore.Self.GetStateSaveCategory(customVariable, containingElement);

            return result.IsState;
        }

        public static (bool isState, StateSaveCategory category) GetIsVariableStateAndCategory(this CustomVariable customVariable, GlueElement containingElement = null)
        {
            var result = ObjectFinderCore.Self.GetStateSaveCategory(customVariable, containingElement);

            return result;
        }

        public static bool GetIsBaseElementType(this CustomVariable customVariable)
        {
            return GetIsBaseElementType(customVariable, out _);
        }

        public static bool GetIsBaseElementType(this CustomVariable customVariable, out GlueElement element)
        {
            var type = customVariable.Type;

            return GetIsBaseElementType(type, out element);
        }

        public static bool GetIsBaseElementType(string type, out GlueElement element)
        {
            element = null;
            var glueProject = ObjectFinderCore.Self.GlueProject;
            if (!glueProject.SuppressBaseTypeGeneration && type.Contains("."))
            {
                string typeOrVariant = glueProject.FileVersion >= (int)GluxVersions.VariantsInsteadOfTypes
                    ? "Variant"
                    : "Type";

                if (type.StartsWith("Entities.") && type.EndsWith(typeOrVariant))
                {
                    // strip of Entities. and Type and see if there's an entity with a matching name:
                    var entityName = type.Substring(0, type.Length - typeOrVariant.Length).Replace(".", "\\");

                    element = glueProject.Entities.FirstOrDefault(item => item.Name == entityName);

                    return element != null;
                }
                if(type.StartsWith("Screens.") && type.EndsWith(typeOrVariant))
                {
                    var screenName = type.Substring(0, type.Length - typeOrVariant.Length).Replace(".", "\\");

                    element = glueProject.Screens.FirstOrDefault(item => item.Name == screenName);

                    return element != null;
                }
            }

            return false;
        }

        public static string GetEntityNameDefiningThisTypeCategory(this CustomVariable customVariable)
        {
            if(customVariable.Type.StartsWith("Entities."))
            {
                var lastPeriod = customVariable.Type.LastIndexOf('.');
                var entityNameWithPeriods = customVariable.Type.Substring(0, lastPeriod);
                var entityName = entityNameWithPeriods.Replace('.', '\\');

                return entityName;
            }
            else
            {
                return null;
            }
        }

        public static void SetDefaultValueAccordingToType(this CustomVariable customVariable, string typeAsString)
        {

            object newValue = "";

            // This method checks the Type and OverridingType so it can't rely just on GetDefaultValueAccordingToType;
            if (customVariable.GetIsFile())
            {
                newValue = "";
            }
            else
            {
                newValue = GetDefaultValueAccordingToType(typeAsString);

            }
            customVariable.DefaultValue = newValue;

            customVariable.FixEnumerationTypes();
        }

        public static object GetDefaultValueAccordingToType(string typeAsString)
        {
            Type type = TypeResolutionCore.Self.GetTypeFromString(typeAsString);
            object newValue = "";

            if (type == typeof(string))
            {
                newValue = "";
            }
            else if (type == null && typeAsString == "VariableState")
            {
                newValue = "";
            }
            else if(GetIsFile(typeAsString))
            {
                newValue = "";
            }
            else if (type == typeof(Microsoft.Xna.Framework.Color))
            {
                newValue = "";
            }
            else if (type == typeof(byte))
            {
                newValue = (byte)0;
            }
            else if (type == typeof(short))
            {
                newValue = (short)0;
            }
            else if (type == typeof(int))
            {
                newValue = (int)0;
            }
            else if (type == typeof(long))
            {
                newValue = (long)0;
            }
            else if (type == typeof(char))
            {
                newValue = ' ';
            }
            else if (type == typeof(float))
            {
                newValue = 0.0f;
            }
            else if (type == typeof(double))
            {
                newValue = 0.0;
            }
            else if (type == typeof(bool))
            {
                newValue = false;
            }
            else if (type == typeof(bool?))
            {
                newValue = (bool?)null;
            }

            return newValue;
        }

        public static void FixEnumerationTypes(this CustomVariable customVariable)
        {
            if (customVariable.GetIsEnumeration() && customVariable.DefaultValue != null)
            {
                Type runtimeType = customVariable.GetRuntimeType();

                if(customVariable.DefaultValue?.GetType() != runtimeType)
                {
                    Array array = Enum.GetValues(runtimeType);

                    int valueAsInt = 0;
                    if (customVariable.DefaultValue is int asInt)
                    {
                        valueAsInt = asInt;
                    }
                    else if(customVariable.DefaultValue is long asLong)
                    {
                        valueAsInt = (int)asLong;
                    }


                    try
                    {
                        string name = Enum.GetName(runtimeType, valueAsInt);

                        foreach (object enumValue in array)
                        {
                            if (name == enumValue.ToString())
                            {
                                customVariable.DefaultValue = enumValue;
                                break;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        throw new Exception("Could not set the integer value" + valueAsInt + "to an enumeration of type " + runtimeType.FullName);
                    }
                }


            }
        }

        public static void ConvertEnumerationValuesToInts(this CustomVariable customVariable)
        {
            if(customVariable.DefaultValue?.GetType()?.IsEnum == true)
            {
                customVariable.DefaultValue = (int)customVariable.DefaultValue;

                foreach(var property in customVariable.Properties)
                {
                    if(property.Value?.GetType()?.IsEnum == true)
                    {
                        property.Value = (int)property.Value;
                    }
                }
            }
        }


        public static bool GetIsEnumeration(this CustomVariable customVariable)
        {

            if (string.IsNullOrEmpty(customVariable.Type))
            {
                return false;
            }

            Type type = TypeResolutionCore.Self.GetTypeFromString(customVariable.Type);

            if (type == null)
            {
                return false;
            }
            else
            {
                return type.IsEnum;
            }
        }

        public static bool GetIsAnimationChain(this CustomVariable variable)
        {
            Type runtimeType = variable.GetRuntimeType();

            return runtimeType != null &&
                runtimeType == typeof(string) && variable.SourceObjectProperty == "CurrentChainName";
        }

        public static bool GetIsFile(this CustomVariable customVariable)
        {
            string typeString = null;

            if (string.IsNullOrEmpty(customVariable.OverridingPropertyType))
            {
                typeString = customVariable.Type;
            }
            else
            {
                typeString = customVariable.OverridingPropertyType;
            }

            // this can be expanded to support setting more file-based variables in Glue
            return GetIsFile(typeString);
        }

        public static bool GetIsFile(Type runtimeType)
        {
            return runtimeType != null && GetIsFile(runtimeType.Name);

                //runtimeType == typeof(Microsoft.Xna.Framework.Graphics.Texture2D) ||
                //runtimeType == typeof(FlatRedBall.Graphics.Animation.AnimationChainList) ||
                //runtimeType == typeof(FlatRedBall.Graphics.BitmapFont)
                //;
        }

        public static bool GetIsFile(string typeName)
        {
            /////////////////Early Out//////////////////////////
            // I was able to get
            // Glue to generate really
            // bad code by adding a type
            // to a CSV that deserialized
            // to a string.  In this case, every
            // string member was being treated as
            // if it was a file, and having its quotes
            // removed when code was generated for it.  We
            // don't want this to happen so we're going to always
            // treat strings as non-files...for now at least.
            if (String.Equals(typeName, "string", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            ////////////// End Early out ///////////////////////




            bool isDefaultFileType =
                typeName == "Texture2D" ||
                (typeName != null && typeName.EndsWith(".Texture2D")) ||
                typeName == "AnimationChainList" ||
                typeName == "BitmapFont" ||
                typeName == "ShapeCollection" ||
                typeName == "Scene"
                ;
            bool isFileTypeDefinedInCsv = false;

            bool isFileTypeFromAti = false;
            if (!isDefaultFileType && typeName != null)
            {
                foreach (var assetType in AvailableAssetTypesCore.Self.AllAssetTypes)
                {
                    if (!string.IsNullOrEmpty(assetType.Extension) && assetType.RuntimeTypeName == typeName)
                    {
                        isFileTypeDefinedInCsv = true;
                        break;
                    }

                    if(assetType.QualifiedRuntimeTypeName.QualifiedType == typeName &&
                        !string.IsNullOrEmpty(assetType.Extension ))
                    {
                        isFileTypeFromAti = true;
                        break;
                    }

                }
            }


            return isDefaultFileType || isFileTypeDefinedInCsv || isFileTypeFromAti;

        }

        public static bool GetIsObjectType(this CustomVariable customVariable)
        {
            string typeString = null;

            if (string.IsNullOrEmpty(customVariable.OverridingPropertyType))
            {
                typeString = customVariable.Type;
            }
            else
            {
                typeString = customVariable.OverridingPropertyType;
            }

            return GetIsObjectType(typeString);
        }

        public static bool GetIsObjectType(string typeString)
        {
            if (typeString != null)
            {
                return AvailableAssetTypesCore.Self.AllAssetTypes.Any(item =>
                {
                    return item.CanBeObject && (item.RuntimeTypeName == typeString || item.QualifiedRuntimeTypeName.QualifiedType == typeString);
                });
            }

            return false;
        }

        public static Type GetRuntimeType(this CustomVariable customVariable)
        {
            if (customVariable.GetIsVariableState())
            {
                return null;
            }
            else if (string.IsNullOrEmpty(customVariable.OverridingPropertyType))
            {
                var type = TypeResolutionCore.Self.GetTypeFromString(customVariable.Type);
                return type;
            }
            else
            {
                return TypeResolutionCore.Self.GetTypeFromString(customVariable.OverridingPropertyType);
            }
        }

        /// <summary>
        /// If a CustomVariable has its SetByDerived to true, then any Elements that inherit from the Element
        /// that has this CustomVariable will also have CustomVariables with matching name that have their
        /// DefinedByBase set to true.  The variable with DefinedByBase often has incomplete information (like
        /// whether it is tunneling or not) so sometimes we need the defining variable for information like state types.
        /// </summary>
        /// <param name="customVariable">The variable to get the defining variable for.</param>
        /// <returns>The defining variable.  If an error occurs in finding base types, null is returned.</returns>
        public static CustomVariable GetDefiningCustomVariable(this CustomVariable customVariable)
        {
            if(customVariable == null)
            {
                throw new ArgumentNullException("customVariable");
            }
            if (!customVariable.DefinedByBase)
            {
                return customVariable;
            }
            else
            {
                var container = ObjectFinderCore.Self.GetElementContaining(customVariable);

                if (container != null && !string.IsNullOrEmpty(container.BaseElement))
                {
                    var baseElement = ObjectFinderCore.Self.GlueProject.GetElement(container.BaseElement);
                    if (baseElement != null)
                    {
                        CustomVariable customVariableInBase = baseElement.GetCustomVariableRecursively(customVariable.Name);

                        if (customVariableInBase != null)
                        {
                            return customVariableInBase.GetDefiningCustomVariable();
                        }
                    }
                }
                return null;
            }

        }

        public static bool GetIsTunneling(this CustomVariable customVariable)
        {
            return !string.IsNullOrEmpty(customVariable.SourceObject) && !string.IsNullOrEmpty(customVariable.SourceObjectProperty);

        }

        public static string CustomVariableToString(CustomVariable cv)
        {
            IElement container = ObjectFinderCore.Self.GetElementContaining(cv);
            string containerName = " (Uncontained)";
            if (container != null)
            {
                containerName = " in " + container.ToString();
            }

            if (string.IsNullOrEmpty(cv.SourceObject))
            {
                return "" + cv.Type + " " + cv.Name + " = " + cv.DefaultValue + containerName;

            }
            else
            {
                return "" + cv.Type + " " + cv.SourceObject + "." + cv.Name + " = " + cv.DefaultValue + containerName;
            }

        }

        public static bool HasAccompanyingVelocityConsideringTunneling(this CustomVariable variable, IElement container, int maxDepth = 0)
        {
            if (variable.HasAccompanyingVelocityProperty)
            {
                return true;
            }
            else if (!string.IsNullOrEmpty(variable.SourceObject) && !string.IsNullOrEmpty(variable.SourceObjectProperty) && maxDepth > 0)
            {
                NamedObjectSave nos = container.GetNamedObjectRecursively(variable.SourceObject);

                if (nos != null)
                {
                    // If it's a FRB
                    if (nos.SourceType == SourceType.FlatRedBallType || nos.SourceType == SourceType.File)
                    {
                        return !string.IsNullOrEmpty(InstructionManager.GetVelocityForState(variable.SourceObjectProperty));
                    }
                    else if(nos.SourceType == SourceType.Entity)
                    {
                        EntitySave entity = ObjectFinderCore.Self.GlueProject.GetEntitySave(nos.SourceClassType);

                        if (entity != null)
                        {
                            CustomVariable variableInEntity = entity.GetCustomVariable(variable.SourceObjectProperty);

                            if (variableInEntity != null)
                            {
                                if (!string.IsNullOrEmpty(InstructionManager.GetVelocityForState(variableInEntity.Name)))
                                {
                                    return true;
                                }
                                else
                                {

                                    return variableInEntity.HasAccompanyingVelocityConsideringTunneling(entity, maxDepth - 1);
                                }
                            }
                            else
                            {
                                // There's no variable for this, so let's see if it's a variable that has velocity in FRB
                                return !string.IsNullOrEmpty(InstructionManager.GetVelocityForState(variable.SourceObjectProperty));

                            }
                        }
                    }
                }
            }
            return false;

        }

        public static bool GetIsSourceFile(this CustomVariable customVariable, IElement container)
        {
            NamedObjectSave referencedNos = null;
            if(!string.IsNullOrEmpty(customVariable.SourceObject) )
            {
                referencedNos = container.GetNamedObjectRecursively(customVariable.SourceObject);
            }

            return referencedNos != null && customVariable.SourceObjectProperty == "SourceFile" && referencedNos.SourceType == SourceType.FlatRedBallType;


        }
    }
}
