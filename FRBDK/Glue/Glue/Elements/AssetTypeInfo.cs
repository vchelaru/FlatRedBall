using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using FlatRedBall.IO;
using System.Reflection;

using FlatRedBall.Glue.Parsing;
using FlatRedBall.Instructions.Reflection;
using System.Xml.Serialization;
using FlatRedBall.Glue.SaveClasses;

using FlatRedBall.Instructions;
using Newtonsoft.Json;

namespace FlatRedBall.Glue.Elements
{

    public class AssetTypeInfo
	{
		#region Fields

        [XmlIgnore]
        public List<MemberWithType> CachedExtraVariables = new List<MemberWithType>();

        private string mSaveTypeName;

		public string Extension;

        public string RuntimeTypeName
        {
            get
            {
                if(QualifiedRuntimeTypeName.PlatformFunc != null)
                {
                    return QualifiedRuntimeTypeName.PlatformFunc(null);
                }
                else
                {
                    if (string.IsNullOrEmpty(QualifiedRuntimeTypeName.QualifiedType))
                    {
                        return null;
                    }
                    else if (QualifiedRuntimeTypeName.QualifiedType.Contains('.'))
                    {
                        int lastPeriod = QualifiedRuntimeTypeName.QualifiedType.LastIndexOf('.');

                        int startingIndex = lastPeriod + 1;
                        int length = QualifiedRuntimeTypeName.QualifiedType.Length - startingIndex;

                        return QualifiedRuntimeTypeName.QualifiedType.Substring(startingIndex, length);
                    }
                    else
                    {
                        return QualifiedRuntimeTypeName.QualifiedType;
                    }
                }
            }
        }

        // This used to be a string, but different
        // platforms may have different types or namespaces
        // for different types.  For example, FSB is built on
        // the namespace structure of XNA 3 which has Color in 
        // a different namespace than XNA 4.
        public PlatformSpecificType QualifiedRuntimeTypeName;

        public string AdjustRelativeZ;
        public string QualifiedSaveTypeName
        {
            get
            {
                return mSaveTypeName;
            }
            set
            {
                mSaveTypeName = value;
                if (!string.IsNullOrEmpty(mSaveTypeName))
                {
                    string name = Assembly.CreateQualifiedName("FlatRedBall", mSaveTypeName);
                    mSaveType = Type.GetType(name);

                    if (mSaveType == null)
                    {
                        name = Assembly.CreateQualifiedName("FlatRedBallMdx", mSaveTypeName);
                        mSaveType = Type.GetType(name);

                        if (mSaveType == null)
                        {
                            // We used to throw an exception here, but now it's okay - we allow custom types
                            //throw new Exception();
                        }
                    }
                }
            }
        }
		public string FriendlyName;
        /// <summary>
        /// Func which can be used to provide a custom AddToManagers method. The parameters are
        /// * IElement - the containing element (screen or entity), 
        /// * NamedObjectSave - the NamedObjectSave to add add to managers which may be null, 
        /// * ReferencedFileSave - The associated ReferencedFileSave which may be null
        /// * string - the name of the layer (such as "layerToAddTo")
        /// 
        /// The returned string is the code for adding.
        /// </summary>
        [XmlIgnore]
        public Func<IElement, NamedObjectSave, ReferencedFileSave, string, string> AddToManagersFunc;

        /// <summary>
        /// Returns a custom function to instantiate objects. This is null by default, which means the object will be instantiated using a standard constructor call.
        /// If the object is a nameable, the construction function is responsible for assigning the name.
        /// </summary>
        [XmlIgnore]
        public Func<IElement, NamedObjectSave, ReferencedFileSave, string> ConstructorFunc;

        [XmlIgnore]
        public Func<IElement, NamedObjectSave, ReferencedFileSave, string, string> GetObjectFromFileFunc;

        /// <summary>
        /// Func which can be used to perform custom loading of an asset. Parameters are:
        /// * IElement - The Screen or Entity containing the file
        /// * NamedObjectSave - the NamedObjectSave associated with the load
        /// * ReferencedFileSave - the file being loaded
        /// * string - the content manager in context
        /// * string - the return value which is the code for loading
        /// </summary>
        /// <remarks>This takes priority over CustomLoadMethod</remarks>
        [XmlIgnore]
        public Func<IElement, NamedObjectSave, ReferencedFileSave, string, string> CustomLoadFunc;

        /// <summary>
        /// Func which is used to generate a reload function if the file is in GlobalContent.
        /// * IElement - The Screen or Entity containing the file
        /// * NamedObjectSave - the NamedObjectSave associated with the load
        /// * ReferencedFileSave - the file being loaded
        /// * string - the content manager in context
        /// * string - the return value which is the code for loading
        /// </summary>
        [XmlIgnore]
        public Func<IElement, NamedObjectSave, ReferencedFileSave, string, string> CustomReloadFunc;

        /// <summary>
        /// The generated code to include to add the object to managers.
        /// </summary>
        /// <remarks>
        /// This list of properties should be used if the object does not support layers. If the object
        /// does support layers, the LayeredAddToManagersMethod should be used.
        /// </remarks>
        public List<string> AddToManagersMethod = new List<string>();

        /// <summary>
        /// Adds an object to managers on the specified FlatRedBall layer. Glue will
        /// generate code using the variable mLayer.
        /// </summary>
		public List<string> LayeredAddToManagersMethod = new List<string>();
        public string MakeManuallyUpdatedMethod;

        /// <summary>
        /// The method used to directly add the engine to be manually updated, rather than being converted
        /// </summary>
        public string AddManuallyUpdatedMethod;
        /// <summary>
        /// A string used to generate every-frame activity. The string replaces "this" with the object 
        /// name, and automatically adds a semicolon at the end.
        /// </summary>
        /// <example>"this?.AnimateSelf()"</example>
        public string ActivityMethod;
        public string AfterCustomActivityMethod;

        /// <summary>
        /// The code to generate when the object associated with this AssetTypeInfo should be destroyed. 
        /// The generated code replaces the string "this" with the name of the named object.
        /// </summary>
        /// <remarks>
        /// This code can be multiple statements. If it's a single statement, then the code does not need 
        /// to end in a semicolon. If the code has multiple statements then all statements except the last
        /// should end in a semicolon. 
        /// 
        /// For example, a single statement may look like this:
        /// assetTypeInfo.DestroyMethod = "this.Destroy()";
        /// 
        /// The following example shows how to generate multiple statements:
        /// assetTypeInfo.DestroyMethod = "this.RemoveFromManagers(); this.BroadcastRemoval()";
        /// 
        /// Note that the world "this" will get replaced with the named object name, so the generated code
        /// may actually end up as shown in the following code:
        /// "MyObjectInstance.RemoveFromManagers(); MyObjectInstance.BroadcastRemoval();"
        /// </remarks>
		public string DestroyMethod;
        public string RecycledDestroyMethod;
        public string SetFromOtherCode;

        /// <summary>
        /// The list of variables available on this type. This should replace ExtraVariablesPattern and 
        /// and Type-based (reflection) variable lists.
        /// </summary>
        public List<VariableDefinition> VariableDefinitions = new List<VariableDefinition>();

        public string PostInitializeCode;
        public bool ShouldAttach;
        public bool IsPositionedObject;
        public string AttachToNullOnlyMethod;
        public bool HasCursorIsOn;
        public bool HasVisibleProperty;
        public bool ImplementsICollidable;
        public string ContentImporter;
        public string ContentProcessor;

        public bool SupportsMakeOneWay;

        public string RemoveFromLayerMethod;

        public string FindByNameSyntax;

        public bool CanBeObject;
        public bool DefaultPublic;

		Type mSaveType;

        public bool MustBeAddedToContentPipeline;
        public bool ShouldBeDisposed;

        public bool CanBeCloned;
        public string CustomCloneMethod;

        public bool CanIgnorePausing;

        /// <summary>
        /// Line of code used to load a given piece of content. This is not used if CustomLoadFunc is not null.
        /// If this is blank, then FlatRedBallServices.Load will be used.
        /// </summary>
        /// <remarks>
        /// Supported parameters:
        /// 
        /// * {THIS}
        /// * {TYPE}
        /// * {FILE_NAME}
        /// * {CONTENT_MANAGER_NAME}
        /// </remarks>
        public string CustomLoadMethod;

        public string CustomBuildToolName;

        public bool ImplementsIWindow;

        // Making this a field for now because CSV deserializer can't deserialize list properties
        public List<FlatRedBall.Glue.CodeGeneration.ConversionCodeGenInfo> Conversion;

        /// <summary>
        /// If this is true, 
        /// then Glue will not 
        /// show this object in 
        /// the New File Window regardless 
        /// of other settings (such as whether 
        /// a runtime type and sample file exist)
        /// </summary>
        public bool HideFromNewFileWindow;

        /// <summary>
        /// If true, Glue will not add this file to the content project when it is regenerated.
        /// By default this is false, which means that files added to Glue will be added to the content project.
        /// </summary>
        public bool ExcludeFromContentProject;

        /// <summary>
        /// Additional data which may be added by a plugin, such as the Gum plugin adding the ElementSave.
        /// </summary>
        [XmlIgnore]
        [JsonIgnore]
        public object Tag { get; set; }

        #endregion

        #region Properties

        [XmlIgnore]
        public Type SaveType
        {
            get { return mSaveType; }
        }

        // Returns whether this type has to be instantiated by the engine (like Layer), 
        // so it will stay null until AddToManagers
        public bool IsInstantiatedInAddToManagers
        {
            get
            {
                return 

                    AddToManagersMethod?.Count > 0 &&
                    !string.IsNullOrEmpty(AddToManagersMethod[0]) &&
                    AddToManagersMethod[0].StartsWith("this =");
            }
        }


        #endregion

        #region Methods

        public AssetTypeInfo()
        {
            Conversion = new List<CodeGeneration.ConversionCodeGenInfo>();
        }

		public AssetTypeInfo(string extension, 
			string qualifiedRuntimeTypeName,
			Type saveType, string friendlyName, string addToManagersMethod,
			string destroyMethod)
		{
            Conversion = new List<CodeGeneration.ConversionCodeGenInfo>();
            Extension = extension;

			QualifiedRuntimeTypeName = new PlatformSpecificType();
            QualifiedRuntimeTypeName.QualifiedType = qualifiedRuntimeTypeName;

			mSaveType = saveType;
            if (mSaveType != null)
            {
                QualifiedSaveTypeName = mSaveType.FullName;
            }
			FriendlyName = friendlyName;
			AddToManagersMethod = new List<string>(){addToManagersMethod};
			DestroyMethod = destroyMethod;

		}

		public override string ToString()
		{
			return FriendlyName;
		}

		#endregion
	}
}
