using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Parsing;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
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



        public List<string> GetInheritanceList(EntitySave entitySave, out EntitySave rootEntitySave)
        {

            var inheritsFromEntity = !string.IsNullOrEmpty(entitySave.BaseElement) &&
                entitySave.BaseElement != "<NONE>" && !entitySave.InheritsFromFrbType();

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
                AssetTypeInfo ati = AvailableAssetTypes.Self.GetAssetTypeFromRuntimeType(entitySave.BaseEntity, entitySave);

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


            if(GlueState.Self.CurrentGlueProject.FileVersion >= (int)GlueProjectSave.GluxVersions.IEntityInFrb)
            {
                inheritanceList.Add("FlatRedBall.Entities.IEntity");
            }

            foreach (ElementComponentCodeGenerator eccg in CodeWriter.CodeGenerators)
            {
                eccg.AddInheritedTypesToList(inheritanceList, entitySave);
            }

            StringFunctions.RemoveDuplicates(inheritanceList);
            
            return inheritanceList;
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
