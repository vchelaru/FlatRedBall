using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.CodeGeneration;
using FlatRedBall.Glue.Parsing;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Plugins.Interfaces;
using FlatRedBall.Glue.SaveClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlatRedBall.Glue.IO;
using FlatRedBall.IO;
using FlatRedBall.Glue.Elements;
using System.Windows.Shapes;

namespace FlatRedBall.Glue.Plugins.EmbeddedPlugins.FactoryPlugin
{
    #region FactoryElementGeneratorEarly

    public class FactoryElementGeneratorEarly : ElementComponentCodeGenerator
    {
        public override CodeLocation CodeLocation
        {
            get
            {
                return CodeLocation.BeforeStandardGenerated;
            }
        }

        bool IsAbstract(IElement element) => element.AllNamedObjects.Any(item => item.SetByDerived);


        public override ICodeBlock GenerateDestroy(ICodeBlock codeBlock, IElement element)
        {
            // This needs to be before the base.Destroy(); call so that the derived class can make itself unused before the base class get a chance
            if (element is EntitySave entity && entity.CreatedByOtherEntities && entity.PooledByFactory)
            {
                // Other generators rely on wasUsed being declared in the method whether or not the type is abstract
                codeBlock.Line("var wasUsed = this.Used;");

                if (!IsAbstract(element))
                {
                    codeBlock
                        .If("Used")
                            .Line(string.Format("Factories.{0}Factory.MakeUnused(this, false);", FileManager.RemovePath(element.Name)));
                }
            }

            return codeBlock;
        }
    }

    #endregion

    public class FactoryElementCodeGenerator : ElementComponentCodeGenerator
    {
        static bool IsAbstract(IElement element) => element.AllNamedObjects.Any(item => item.SetByDerived);
        static FactoryEntireClassGenerator mEntireClassGenerator = new FactoryEntireClassGenerator();

        #region CodeGenerator methods (for generating code in an Element)


        public override ICodeBlock GenerateAddToManagers(ICodeBlock codeBlock, IElement element)
        {
            // September 9, 2011
            // I think factories should
            // only initialize on a Screen
            // and not Entity.  Otherwise Entities
            // that contain lists of Factoried objects
            // will initialize the list, and this could
            // cause the list to become the one that the
            // factory fills.
            // Update May 30, 2022 - Now that we are supporting
            // the concept of "rooms", these rooms may instantiate
            // entities which should end up in the lists of the rooms.
            // Therefore, we need to have lists be able to add themselves
            // to the factories.
            // This addition will be temporary, only when the TileEntityInstantiator
            // creates the entities. Therefore, the TileEntityInstantor code generator
            // will also be modifying the factory lists.
            /////////////////////EARLY OUT///////////////////////
            if (!(element is ScreenSave))
            {
                return codeBlock;
            }
            /////////////////END EARLY OUT//////////////////////

            // June 5, 2011
            // We used to instantiate
            // Factories in the Initialize
            // method, but that caused a bug.
            // If a factory is used in 2 consecutive
            // Screens, and if the first screen loads
            // the next screen asynchronously, and if the
            // two Screens use different ContentManagers, then
            // the Destroy call on the first screen will wipe out
            // the Initialize call that was asynchronously called from
            // the second Screen.  We fix this by moving the Initialize
            // method into AddToManagers so that it's not called asynchronously -
            // instead it's called after the first screen has finished unloading itself.

            codeBlock.Line("InitializeFactoriesAndSorting();");


            return codeBlock;
        }

        private static void GetEntitiesToInstantiateFactoriesFor(IElement element, out List<NamedObjectSave> entityLists, out List<EntitySave> entitiesToInitializeFactoriesFor)
        {
            entityLists = element.NamedObjects
                .Where(nos => !nos.InstantiatedByBase &&
                    nos.SourceType == SourceType.FlatRedBallType &&
                    nos.IsList &&
                    nos.IsDisabled == false &&
                    !string.IsNullOrEmpty(nos.SourceClassGenericType) &&
                    (nos.SourceClassGenericType.StartsWith("Entities\\") || nos.SourceClassGenericType.StartsWith("Entities/")))
                .ToList();
            HashSet<EntitySave> entitiesToInitializeFactoriesForHash = new HashSet<EntitySave>();

            foreach (var listNos in entityLists)
            {
                EntitySave sourceEntitySave = ObjectFinder.Self.GetEntitySave(listNos.SourceClassGenericType);

                if (sourceEntitySave != null)
                {
                    if (sourceEntitySave.CreatedByOtherEntities && !IsAbstract(sourceEntitySave))
                    {
                        entitiesToInitializeFactoriesForHash.Add(sourceEntitySave);
                    }
                    var allDerived = ObjectFinder.Self.GetAllDerivedElementsRecursive(sourceEntitySave);
                    foreach (EntitySave derived in allDerived)
                    {
                        if (derived.CreatedByOtherEntities && !IsAbstract(derived))
                        {
                            entitiesToInitializeFactoriesForHash.Add(derived);
                        }
                    }
                }
            }

            entitiesToInitializeFactoriesFor = entitiesToInitializeFactoriesForHash.ToList();
        }

        public override ICodeBlock GenerateDestroy(ICodeBlock codeBlock, IElement element)
        {
            // but for some reason we still destroyed
            // them.  Added the check for element is ScreenSave.
            if (element is ScreenSave)
            {
                List<NamedObjectSave> entityLists;
                List<EntitySave> entityFactoriesToDestroy;
                GetEntitiesToInstantiateFactoriesFor(element, out entityLists, out entityFactoriesToDestroy);


                var allEntitiesWithFactories = GlueState.Self.CurrentGlueProject.Entities
                    .Where(item => item.CreatedByOtherEntities && !IsAbstract(item));

                foreach (var listNos in entityLists)
                {
                    // Find all factories of this type, or of derived type
                    EntitySave listEntityType = ObjectFinder.Self.GetEntitySave(listNos.SourceClassGenericType);

                    if (listEntityType != null)
                    {

                        var factoryTypesToCallAddListOn = allEntitiesWithFactories.Where(item =>
                        {
                            return item == listEntityType || item.InheritsFrom(listEntityType.Name);
                        });

                        // find any lists of entities that are of this type, or of a derived type.
                        foreach (var factoryEntityType in factoryTypesToCallAddListOn)
                        {
                            entityFactoriesToDestroy.Add(factoryEntityType);
                        }
                    }
                }

                entityFactoriesToDestroy = entityFactoriesToDestroy.Distinct().ToList();

                foreach (var entityFactory in entityFactoriesToDestroy)
                {
                    string entityClassName = FileManager.RemovePath(entityFactory.Name);
                    string line = $"Factories.{entityClassName}Factory.Destroy();";

                    codeBlock.Line(line);

                }
            }

            return codeBlock;
        }


        public override ICodeBlock GenerateAdditionalMethods(ICodeBlock codeBlock, IElement element)
        {
            GenerateInitializeFactoriesAndSorting(codeBlock, element as GlueElement);
            return codeBlock;
        }

        private void GenerateInitializeFactoriesAndSorting(ICodeBlock codeBlock, GlueElement element)
        {

            if (!(element is ScreenSave))
            {
                return;
            }

            codeBlock = codeBlock.Function("private void", "InitializeFactoriesAndSorting", "");


            List<NamedObjectSave> entityLists;
            List<EntitySave> entitiesToInitializeFactoriesFor;
            GetEntitiesToInstantiateFactoriesFor(element, out entityLists, out entitiesToInitializeFactoriesFor);

            foreach (var entity in entitiesToInitializeFactoriesFor)
            {
                // initialize the factory:
                string entityClassName = FileManager.RemovePath(entity.Name);
                string factoryName = $"Factories.{entityClassName}Factory";
                codeBlock.Line(factoryName + ".Initialize(ContentManagerName);");
            }

            var allEntitiesWithFactories = GlueState.Self.CurrentGlueProject.Entities
                .Where(item => item.CreatedByOtherEntities && !IsAbstract(item));

            var shouldConsiderAssociateWithFactory =
                GlueState.Self.CurrentGlueProject.FileVersion >= (int)GlueProjectSave.GluxVersions.ListsHaveAssociateWithFactoryBool;

            foreach (var listNos in entityLists)
            {
                T Get<T>(string propName) =>
                    listNos.Properties.GetValue<T>(propName);

                var isEligibleForAdding = true;

                if (shouldConsiderAssociateWithFactory)
                {
                    isEligibleForAdding = listNos.AssociateWithFactory;
                }

                if (isEligibleForAdding)
                {
                    // Find all factories of this type, or of derived type
                    EntitySave listEntityType = ObjectFinder.Self.GetEntitySave(listNos.SourceClassGenericType);

                    if (listEntityType != null)
                    {
                        var factoryTypesToCallAddListOn = allEntitiesWithFactories.Where(item =>
                        {
                            return item == listEntityType || item.InheritsFrom(listEntityType.Name);
                        });

                        // find any lists of entities that are of this type, or of a derived type.
                        foreach (var factoryEntityType in factoryTypesToCallAddListOn)
                        {
                            string entityClassName = FileManager.RemovePath(factoryEntityType.Name);

                            string factoryName = $"Factories.{entityClassName}Factory";
                            codeBlock.Line($"{factoryName}.AddList({listNos.FieldName});");

                            if (Get<bool>("PerformCollisionPartitioning"))
                            {
                                //var sortAxis = Get<FlatRedBall.Math.Axis?>("SortAxis");
                                var sortAxis = listNos.Properties.GetValue("SortAxis");
                                if (sortAxis is int asInt)
                                {
                                    codeBlock.Line(
                                        $"{factoryName}.SortAxis = FlatRedBall.Math.Axis.{(Math.Axis)asInt};");
                                }
                                else if (sortAxis is long asLong)
                                {
                                    codeBlock.Line(
                                        $"{factoryName}.SortAxis = FlatRedBall.Math.Axis.{(Math.Axis)asLong};");

                                }

                            }
                        }

                    }
                }

            }
        }

        public override ICodeBlock GenerateLoadStaticContent(ICodeBlock codeBlock, IElement element)
        {
            return codeBlock;
            // TODO:  We should load static content for factories here
        }
        #endregion

        public static void CallPostInitializeIfNecessary(IElement element, ICodeBlock currentBlock)
        {
            bool isEntity = element is EntitySave;

            bool shouldCallPostInitializeBecauseIsPooled = false;

            // It may inherit from a FRB type, in which case we still want to add PostInitialize
            //if (isEntity && string.IsNullOrEmpty(element.BaseElement))
            if (isEntity && !element.InheritsFromElement())
            {


                EntitySave asEntitySave = element as EntitySave;

                shouldCallPostInitializeBecauseIsPooled =
                    asEntitySave.CreatedByOtherEntities && asEntitySave.PooledByFactory && !IsAbstract(asEntitySave);

                if (!shouldCallPostInitializeBecauseIsPooled)
                {
                    List<EntitySave> entities =
                        ObjectFinder.Self.GetAllEntitiesThatInheritFrom(asEntitySave);

                    foreach (EntitySave derivedEntity in entities)
                    {
                        if (derivedEntity.CreatedByOtherEntities && derivedEntity.PooledByFactory && !IsAbstract(derivedEntity))
                        {
                            shouldCallPostInitializeBecauseIsPooled = true;
                            break;
                        }
                    }
                }

            }


            if (shouldCallPostInitializeBecauseIsPooled)
            {
                currentBlock.Line("PostInitialize();");
            }

        }

        public static void RemoveFactory(EntitySave entitySave)
        {
            mEntireClassGenerator.EntitySave = entitySave;

            try
            {
                // Delete the file and remove it from the project
                if (mEntireClassGenerator.ProjectSpecificFullFileName.Exists())
                {
                    FileHelper.MoveToRecycleBin(mEntireClassGenerator.ProjectSpecificFullFileName.FullPath);
                }
                mEntireClassGenerator.RemoveSelfFromProject();
            }
            catch
            {
                System.Windows.Forms.MessageBox.Show("Error trying to remove the Factory " + mEntireClassGenerator.ClassName);

            }
        }

        public static void GenerateAndAddFactoryToProjectClass(EntitySave entitySave)
        {
            mEntireClassGenerator.EntitySave = entitySave;

            mEntireClassGenerator.GenerateAndAddToProjectIfNecessary();
        }

        public static void AddGeneratedPerformanceTypes()
        {
            FilePath poolListFileName = GlueState.Self.CurrentGlueProjectDirectory + @"Performance\PoolList.Generated.cs";
            FilePath iEntityFactoryFileName = GlueState.Self.CurrentGlueProjectDirectory + @"Performance\IEntityFactory.Generated.cs";


            string embeddedResourcePrefix = "FlatRedBall.Glue.Resources.";
            var thisAssembly = typeof(FactoryElementCodeGenerator).Assembly;

            var byteArray = FileManager.GetByteArrayFromEmbeddedResource(thisAssembly, embeddedResourcePrefix + "PoolList.cs");
            var contents = Encoding.Default.GetString(byteArray);
            contents = CodeWriter.ReplaceNamespace(contents, ProjectManager.ProjectNamespace + ".Performance");
            FileManager.SaveText(contents, poolListFileName.FullPath);

            byteArray = FileManager.GetByteArrayFromEmbeddedResource(thisAssembly, embeddedResourcePrefix + "IEntityFactory.cs");
            contents = Encoding.Default.GetString(byteArray);
            contents = CodeWriter.ReplaceNamespace(contents, ProjectManager.ProjectNamespace + ".Performance");
            FileManager.SaveText(contents, iEntityFactoryFileName.FullPath);



            // These files may exist, but not be part of the project, so let's make sure that they are
            // part of the project
            bool wasPoolListAdded = GlueCommands.Self.ProjectCommands.UpdateFileMembershipInProject(ProjectManager.ProjectBase, poolListFileName, false, false);
            bool wasEntityFactoryAdded = GlueCommands.Self.ProjectCommands.UpdateFileMembershipInProject(ProjectManager.ProjectBase, iEntityFactoryFileName, false, false);
            if (wasPoolListAdded || wasEntityFactoryAdded)
            {
                GlueCommands.Self.ProjectCommands.SaveProjects();
            }

        }

    }
}
