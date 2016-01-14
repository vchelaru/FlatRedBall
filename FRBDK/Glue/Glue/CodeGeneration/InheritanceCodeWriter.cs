using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Parsing;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using FlatRedBall.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlatRedBall.Glue.CodeGeneration
{
    class InheritanceCodeWriter : Singleton<InheritanceCodeWriter>
    {



        public List<string> GetInheritanceList(IElement element, EntitySave entitySave, out bool inheritsFromEntity, out EntitySave rootEntitySave)
        {

            inheritsFromEntity = !string.IsNullOrEmpty(element.BaseElement) && element.BaseElement != "<NONE>" && !element.InheritsFromFrbType();
            rootEntitySave = null;

            if (inheritsFromEntity)
            {
                rootEntitySave = entitySave.GetRootBaseEntitySave();
                inheritsFromEntity = inheritsFromEntity && entitySave.GetRootBaseEntitySave() != null;
            }



            List<string> inheritanceList = new List<string>();

            if (inheritsFromEntity)
            {
                inheritanceList.Add(
                    ProjectManager.ProjectNamespace + "." + entitySave.BaseEntity.Replace("\\", "."));
            }
            else if (entitySave.InheritsFromFrbType())
            {
                AssetTypeInfo ati = AvailableAssetTypes.Self.GetAssetTypeFromRuntimeType(entitySave.BaseEntity);

                if (ati != null)
                {
                    inheritanceList.Add(ati.QualifiedRuntimeTypeName.QualifiedType);
                }
                else
                {
                    inheritanceList.Add(entitySave.BaseEntity);
                }
            }
            else
            {
                inheritanceList.Add("FlatRedBall.PositionedObject");
            }


            inheritanceList.Add("FlatRedBall.Graphics.IDestroyable");


            foreach (ElementComponentCodeGenerator eccg in CodeWriter.CodeGenerators)
            {
                eccg.AddInheritedTypesToList(inheritanceList, element);
            }

            StringFunctions.RemoveDuplicates(inheritanceList);
            
            return inheritanceList;
        }




        internal void RemoveCallsForInheritance(EntitySave entitySave, bool inheritsFromEntity, EntitySave rootEntitySave,  ref string fileContents, ref bool shouldSave)
        {


            if (inheritsFromEntity)
            {
                CodeWriter.EliminateCall("FlatRedBall.SpriteManager.AddPositionedObject(this);", ref fileContents);
                CodeWriter.EliminateCall("\tInitializeEntity(addToManagers);", ref fileContents);
                CodeWriter.EliminateCall(" InitializeEntity(addToManagers);", ref fileContents);

                //EliminateCall("\tContentManagerName = contentManagerName;", ref fileContents);
                //EliminateCall(" ContentManagerName = contentManagerName;", ref fileContents);



                #region Set the call to base(ContentManagerName)

                if (fileContents.Contains("base()"))
                {
                    // use the lower-case contentManagerName since that's the argument that's given to
                    // the base class' constructor.
                    fileContents = fileContents.Replace("base()", "base(contentManagerName, addToManagers)");

                }

                #endregion

                #region Fake a form of inheitance for the static ContentManager

                const string stringToReplace = @"        public static string ContentManagerName
        {
            get;
            set;
        }";


                try
                {
                    // If the base Entity is in a different folder than the derived
                    // and if the base is named the same as its containing folder name
                    // then we need to have the full path.  Since it's generated code anyway
                    // let's always use the full anem.
                    //string rootName = FileManager.RemovePath(rootEntitySave.Name);
                    string rootName = FileManager.RemovePath(rootEntitySave.Name.Replace("\\", "."));

                    string toReplaceWith = string.Format(@"        public static new string ContentManagerName
        {{
            get{{ return {0}.ContentManagerName;}}
            set{{ {0}.ContentManagerName = value;}}
        }}", rootName);


                    fileContents = fileContents.Replace(stringToReplace, toReplaceWith);
                }
                catch
                {
                    int m = 3;
                }
                #endregion

                shouldSave = true;
            }
        }

        internal void WriteBaseInitialize(IElement saveObject, ICodeBlock codeBlock)
        {
            // August 29, 2011
            // We used to only call
            // base.Initialize if the
            // given Element (mSaveObject)
            // had a base.  Now the Screen code
            // sets a value used for timing so we
            // want to call Initialize on the base.
            // March 26, 2012
            // The Screen no longer
            // instantiates its timing
            // in Initialize, but we still
            // want to call base.Initialize
            // just in case we add functionality
            // in the base Screen.cs's Initialize
            // at some point in the future.
            if (saveObject.InheritsFromElement() || saveObject is ScreenSave)
            {
                if (saveObject is EntitySave && !saveObject.InheritsFromFrbType())
                {
                    codeBlock.Line("base.InitializeEntity(addToManagers);");
                }
                else
                {
                    codeBlock.Line("base.Initialize(addToManagers);");
                }
            }
        }
    }
}
