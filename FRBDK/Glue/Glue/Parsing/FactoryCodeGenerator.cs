using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.IO;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Instructions.Reflection;
//using FlatRedBall.Math;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.CodeGeneration;
using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Plugins.Interfaces;

namespace FlatRedBall.Glue.Parsing
{
    #region ElementComponentCodeGenerators

    public class FactoryCodeGeneratorEarly : ElementComponentCodeGenerator
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

    public class FactoryCodeGenerator : ElementComponentCodeGenerator
    {
        static bool IsAbstract(IElement element) => element.AllNamedObjects.Any(item => item.SetByDerived);
        static FactoryEntireClassGenerator mEntireClassGenerator = new FactoryEntireClassGenerator();

        #region CodeGenerator methods (for generating code in an Element)

        public override ICodeBlock GenerateFields(ICodeBlock codeBlock, IElement element)
        {
            return codeBlock;
        }

        public override ICodeBlock GenerateInitialize(ICodeBlock codeBlock, IElement element)
        {
            return codeBlock;
        }

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

                if(shouldConsiderAssociateWithFactory)
                {
                    isEligibleForAdding = listNos.AssociateWithFactory;
                }

                if(isEligibleForAdding)
                {
                    // Find all factories of this type, or of derived type
                    EntitySave listEntityType = ObjectFinder.Self.GetEntitySave(listNos.SourceClassGenericType);

                    if(listEntityType != null)
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
                                        $"{factoryName}.SortAxis = FlatRedBall.Math.Axis.{(FlatRedBall.Math.Axis)asInt};");
                                }
                                else if(sortAxis is long asLong)
                                {
                                    codeBlock.Line(
                                        $"{factoryName}.SortAxis = FlatRedBall.Math.Axis.{(FlatRedBall.Math.Axis)asLong};");

                                }

                            }
                        }

                    }
                }

            }

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
                    foreach(EntitySave derived in allDerived)
                    {
                        if(derived.CreatedByOtherEntities && !IsAbstract(derived))
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
            if(element is ScreenSave)
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

                    if(listEntityType != null)
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

                foreach(var entityFactory in entityFactoriesToDestroy)
                {
                    string entityClassName = FileManager.RemovePath(entityFactory.Name);
                    string line = $"Factories.{entityClassName}Factory.Destroy();";

                    codeBlock.Line(line);

                }
            }

            return codeBlock;
        }

        public override ICodeBlock GenerateActivity(ICodeBlock codeBlock, IElement element)
        {
            return codeBlock;
        }

        public override ICodeBlock GenerateAdditionalMethods(ICodeBlock codeBlock, IElement element)
        {
            return codeBlock;
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
            var thisAssembly = typeof(FactoryCodeGenerator).Assembly;

            var byteArray = FileManager.GetByteArrayFromEmbeddedResource(thisAssembly, embeddedResourcePrefix + "PoolList.cs");
            var contents = System.Text.Encoding.Default.GetString(byteArray);
            contents = CodeWriter.ReplaceNamespace(contents, ProjectManager.ProjectNamespace + ".Performance");
            FileManager.SaveText(contents, poolListFileName.FullPath);

            byteArray = FileManager.GetByteArrayFromEmbeddedResource(thisAssembly, embeddedResourcePrefix + "IEntityFactory.cs");
            contents = System.Text.Encoding.Default.GetString(byteArray);
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

    #endregion

    public class FactoryEntireClassGenerator : EntireClassCodeGenerator
    {
        #region Fields/Properties

        public bool ShouldPoolObjects => EntitySave.PooledByFactory;
        

        public EntitySave EntitySave
        {
            get;
            set;
        }

        public override string ClassName
        {
            get => FileManager.RemovePath(EntitySave.Name) + "Factory"; 
        }

        public override string Namespace
        {
            get { return "Factories"; }
        }

        #endregion

        public override string GetCode()
        {

            string entityClassName = FileManager.RemovePath(FileManager.RemoveExtension(EntitySave.Name));

            string baseEntityName = null;
            if (!string.IsNullOrEmpty(EntitySave.BaseEntity))
            {
                EntitySave rootEntitySave = EntitySave.GetRootBaseEntitySave();

                // There could be an invalid inheritance chain.  We don't want Glue to bomb if so, so
                // we'll check for this.
                if (rootEntitySave != null && rootEntitySave != EntitySave)
                {
                    baseEntityName = rootEntitySave.Name;
                }
            }


            string factoryClassName = ClassName;

            ClassProperties classProperties = new ClassProperties();

            classProperties.NamespaceName = ProjectManager.ProjectNamespace + ".Factories";
            classProperties.ClassName = factoryClassName + " : IEntityFactory";

            classProperties.Members = new List<FlatRedBall.Instructions.Reflection.TypedMemberBase>();

            classProperties.UntypedMembers = new Dictionary<string, string>();
            string positionedObjectListType = string.Format("FlatRedBall.Math.PositionedObjectList<{0}>", entityClassName);

            // Factories used to be always static but we're going to make them singletons instead
            //classProperties.IsStatic = true;

            classProperties.UsingStatements = new List<string>();
            classProperties.UsingStatements.Add(GlueCommands.Self.GenerateCodeCommands.GetNamespaceForElement(EntitySave));
            classProperties.UsingStatements.Add("System");

            if (!string.IsNullOrEmpty(baseEntityName))
            {
                EntitySave baseEntity = ObjectFinder.Self.GetEntitySave(baseEntityName);
                classProperties.UsingStatements.Add(GlueCommands.Self.GenerateCodeCommands.GetNamespaceForElement(baseEntity));
            }


            classProperties.UsingStatements.Add("FlatRedBall.Math");
            classProperties.UsingStatements.Add("FlatRedBall.Graphics");
            classProperties.UsingStatements.Add(ProjectManager.ProjectNamespace + ".Performance");


            ICodeBlock codeContent = CodeWriter.CreateClass(classProperties);

            const int numberOfInstancesToPool = 20;

            var methodTag = codeContent.GetTag("Methods")[0];

            var methodBlock = GetAllFactoryMethods(factoryClassName, baseEntityName, numberOfInstancesToPool,
                                                ShouldPoolObjects, EntitySave);
            methodTag.InsertBlock(methodBlock);

            var codeBlock = new CodeBlockBase(null);
            codeBlock.Line("static string mContentManagerName;");
            codeBlock.Line("static System.Collections.Generic.List<System.Collections.IList> ListsToAddTo = new System.Collections.Generic.List<System.Collections.IList>();");
            codeBlock.Line("public static int NewInstancesCreatedThisScreen;");
            codeBlock.Line("int IEntityFactory.NewInstancesCreatedThisScreen => NewInstancesCreatedThisScreen;");
            // August 19, 2022
            // Let's only add this if it's pooled
            if(EntitySave.PooledByFactory)
            {
                codeBlock.Line(string.Format("static PoolList<{0}> mPool = new PoolList<{0}>();", entityClassName));
            }

            // January 9, 2022
            // Vic asks - why is
            // this a non-event delegate?
            // This allows the caller to null
            // it out which could break the level
            // editor. I am going to switch it to an
            // event to see if this works okay...
            // Update - I think it's not an event to prevent
            // common memory leaks from events, but...it should
            // mainly be used by gencode so I'm switching to an event.
            //codeBlock.Line(string.Format("public static Action<{0}> EntitySpawned;", entityClassName));
            codeBlock.Line($"/// <summary> Event raised whenever an instance is created through this factory.");
            codeBlock.Line($"/// These events are cleared out whenever a Screen is destroyed, so there is ");
            codeBlock.Line($"/// no reason to explicitly remove added events. </summary>");
            codeBlock.Line($"public static event Action<{entityClassName}> EntitySpawned;");

            ImplementIEntityFactory(factoryClassName, codeBlock);

            #region Self and mSelf
            codeBlock.Line("static " + factoryClassName + " mSelf;");

            var selfProperty = codeBlock.Property("public static " + factoryClassName, "Self");
            var selfGet = selfProperty.Get();
            selfGet.If("mSelf == null")
                .Line("mSelf = new " + entityClassName + "Factory" + "();");
            selfGet.Line("return mSelf;");
            #endregion

            ((codeContent.BodyCodeLines.Last() as CodeBlockBase).BodyCodeLines.Last() as CodeBlockBase).InsertBlock(codeBlock);
            return codeContent.ToString();
        }

        private static void ImplementIEntityFactory(string factoryClassName, CodeBlockBase codeBlock)
        {
            codeBlock.Function("object", "IEntityFactory.CreateNew", "float x = 0, float y = 0")
                .Line($"return {factoryClassName}.CreateNew(x, y);");


            codeBlock.Function("object", "IEntityFactory.CreateNew", "Microsoft.Xna.Framework.Vector3 position")
                .Line($"return {factoryClassName}.CreateNew(position);");

            codeBlock.Function("object", "IEntityFactory.CreateNew", "Layer layer")
                .Line($"return {factoryClassName}.CreateNew(layer);");

            codeBlock.Function("void", "IEntityFactory.Initialize", "string contentManagerName")
                .Line($"{factoryClassName}.Initialize(contentManagerName);");

            codeBlock.Function("void", "IEntityFactory.ClearListsToAddTo", "")
                .Line($"{factoryClassName}.ClearListsToAddTo();");

            codeBlock.Line(
                $"System.Collections.Generic.List<System.Collections.IList> IEntityFactory.ListsToAddTo => {factoryClassName}.ListsToAddTo;");
        }

        private static ICodeBlock GetAllFactoryMethods(string factoryClassName, string baseClassName, int numberToPreAllocate, bool poolObjects, EntitySave entitySave)
        {
            string className = factoryClassName.Substring(0, factoryClassName.Length - "Factory".Length);

            ICodeBlock codeBlock = new CodeDocument();

            codeBlock.Line("public static FlatRedBall.Math.Axis? SortAxis { get; set;}");

            GetCreateNewFactoryMethods(codeBlock, factoryClassName, poolObjects, baseClassName);
            codeBlock._();
            GetInitializeFactoryMethod(codeBlock, className, poolObjects, "mScreenListReference");
            codeBlock._();

            GetDestroyFactoryMethod(codeBlock, factoryClassName, entitySave);
            codeBlock._();
            GetFactoryInitializeMethod(codeBlock, factoryClassName, numberToPreAllocate, entitySave);
            codeBlock._();
            GetMakeUnusedFactory(codeBlock, factoryClassName, poolObjects);
            codeBlock._();


            string whereClass = className;
            if(!string.IsNullOrEmpty(baseClassName))
            {
                whereClass = baseClassName.Replace("\\", ".");
            }
            AddAddListMethod(codeBlock, whereClass);
            AddRemoveListMethod(codeBlock, whereClass);
            AddClearListsToAddTo(codeBlock);

            return codeBlock;
        }

        private static void AddAddListMethod(ICodeBlock codeBlock, string entityClassName)
        {
            var method = codeBlock.Function("public static void", "AddList<T>", "System.Collections.Generic.IList<T> newList", $"where T : {entityClassName}");
            method.Line("ListsToAddTo.Add(newList as System.Collections.IList);");
        }

        private static void AddRemoveListMethod(ICodeBlock codeBlock, string entityClassName)
        {
            var method = codeBlock.Function("public static void", "RemoveList<T>", "System.Collections.Generic.IList<T> listToRemove", $"where T : {entityClassName}");
            method.Line("ListsToAddTo.Remove(listToRemove as System.Collections.IList);");
        }

        private static void AddClearListsToAddTo(ICodeBlock codeBlock)
        {
            var method = codeBlock.Function("public static void", "ClearListsToAddTo", "");
            method.Line("ListsToAddTo.Clear();");
        }

        private static ICodeBlock GetCreateNewFactoryMethods(ICodeBlock codeBlock, string className, bool poolObjects, string baseEntityName)
        {
            className = className.Substring(0, className.Length - "Factory".Length);

            // no tabs needed on first line
            codeBlock
                .Function(StringHelper.SpaceStrings("public", "static", className), "CreateNew", "float x = 0, float y = 0, float z = 0")
                    .Line("return CreateNew(null, x, y, z);")
                .End();

            codeBlock
                .Function(StringHelper.SpaceStrings("public", "static", className), "CreateNew", "Microsoft.Xna.Framework.Vector3 position")
                    .Line("return CreateNew(null, position.X, position.Y, position.Z);")
                .End();

            codeBlock
                .Function(StringHelper.SpaceStrings("public", "static", className), "CreateNew", "Microsoft.Xna.Framework.Vector2 position")
                    .Line("return CreateNew(null, position.X, position.Y, 0);")
                .End();


            /*
             * public static EnemyBullet CreateNew(Layer layer, Vector3 vector3)
                {
                    return CreateNew(layer, vector3.X, vector3.Y, vector3.Z);
                }
            */

            codeBlock = codeBlock
                .Function(StringHelper.SpaceStrings("public", "static", className), "CreateNew", "Layer layer, Microsoft.Xna.Framework.Vector3 position")
                    .Line("return CreateNew(layer , position.X, position.Y, position.Z);")
                .End();

            codeBlock = codeBlock
                .Function(StringHelper.SpaceStrings("public", "static", className), "CreateNew", "Layer layer, float x = 0, float y = 0, float z = 0");

            codeBlock.Line(className + " instance = null;");

            if (poolObjects)
            {

                // only throw exception if pooled. This requires the user to Init the factory.
                // But do we want to have an explicit "IsInitialized" value? Maybe if this causes problems in the future...
                // Update June 5, 2022
                // This code was modified 
                // in December 2017 to only 
                // throw exceptions if pooled.
                // But why? Pooled entities should
                // be destroyed when a screen unloads,
                // and the factory will be using the same
                // content manager as the screen in most cases.
                // So why not just tolerate it?
                // I think we should:
                //codeBlock.If("string.IsNullOrEmpty(mContentManagerName)")
                //            .Line("throw new System.Exception(\"You must first initialize the factory for this type because it is pooled. " +
                //            "You can either add PositionedObjectList of type " +
                //                className + " (the most common solution) or call Initialize in custom code\");")
                //        .End();
                codeBlock
                    .Line("instance = mPool.GetNextAvailable();")
                    .If("instance == null")
                        .Line("mPool.AddToPool(new " + className + "(mContentManagerName ?? FlatRedBall.Screens.ScreenManager.CurrentScreen.ContentManagerName, false));")
                        .Line("instance =  mPool.GetNextAvailable();")
                        .Line("NewInstancesCreatedThisScreen++;")
                    .End()
                    .Line("instance.AddToManagers(layer);");
            }
            else
            {
                // If not pooled don't require a content manager, can use the current screen's, so that init isn't required:

                //instance = new FactoryEntityWithNoList(mContentManagerName ?? FlatRedBall.Screens.ScreenManager.CurrentScreen.ContentManagerName, false);

                codeBlock
                    .Line($"instance = new {className}(mContentManagerName ?? FlatRedBall.Screens.ScreenManager.CurrentScreen.ContentManagerName, false);")
                    .Line("instance.AddToManagers(layer);")
                    .Line("NewInstancesCreatedThisScreen++;");

            }

            codeBlock.Line("instance.X = x;");
            codeBlock.Line("instance.Y = y;");
            codeBlock.Line("instance.Z = z;");

            CreateAddToListCode(codeBlock, className);

            codeBlock = codeBlock
                .If("EntitySpawned != null")
                    .Line("EntitySpawned(instance);")
                .End()
                .Line("return instance;")
            .End();

            return codeBlock;
        }

        private static ICodeBlock CreateAddToListCode(ICodeBlock codeBlock, string className)
        {

            codeBlock
                .ForEach("var list in ListsToAddTo")
                    .If($"SortAxis == FlatRedBall.Math.Axis.X && list is PositionedObjectList<{className}>")
                        .Line($"var index = (list as PositionedObjectList<{className}>).GetFirstAfter(x, Axis.X, 0, list.Count);")
                        .Line($"list.Insert(index, instance);")
                    .End().ElseIf($"SortAxis == FlatRedBall.Math.Axis.Y && list is PositionedObjectList<{className}>")
                        .Line($"var index = (list as PositionedObjectList<{className}>).GetFirstAfter(y, Axis.Y, 0, list.Count);")
                        .Line($"list.Insert(index, instance);")
                    .End().Else()
                        .Line("// Sort Z not supported")
                        .Line("list.Add(instance);")
                    .End()
                .End();

            return codeBlock;
        }

        private static ICodeBlock GetDestroyFactoryMethod(ICodeBlock codeBlock, string className, EntitySave entity)
        {
            className = className.Substring(0, className.Length - "Factory".Length);

            var functionBlock = codeBlock
                .Function("public static void", "Destroy", "");

            functionBlock
                    .Line("mContentManagerName = null;")
                    .Line("ListsToAddTo.Clear();")
                    .Line("SortAxis = null;")
                    .Line("NewInstancesCreatedThisScreen = 0;");
            
            if (entity.PooledByFactory)
            {
                functionBlock
                        .Line("mPool.Clear();");
            }

            functionBlock
                    .Line("EntitySpawned = null;");

            codeBlock.Line("void IEntityFactory.Destroy() => Destroy();");

            return codeBlock;
        }

        private static ICodeBlock GetFactoryInitializeMethod(ICodeBlock codeBlock, string factoryClassName, int numberToPreAllocate, EntitySave entity)
        {
            string entityClassName = factoryClassName.Substring(0, factoryClassName.Length - "Factory".Length);

            var functionBlock =
            codeBlock
                .Function("private static void", "FactoryInitialize", "");

            functionBlock.Line("int numberToPreAllocate = " + numberToPreAllocate + ";");

            var glueProject = GlueState.Self.CurrentGlueProject;
            var hasEditMode = glueProject.FileVersion >= (int)GlueProjectSave.GluxVersions.SupportsEditMode;
            if(hasEditMode)
            {
                functionBlock.Line("// If in edit mode and viewing a screen, don't pre-allocate because the content manager may not be set which would cause a crash");
                functionBlock.If("FlatRedBall.Screens.ScreenManager.IsInEditMode && FlatRedBall.Screens.ScreenManager.CurrentScreen?.GetType().Name == \"EntityViewingScreen\"")
                    .Line("numberToPreAllocate = 0;");
            }

            if(entity.PooledByFactory)
            {
                functionBlock
                        .For("int i = 0; i < numberToPreAllocate; i++")
                            .Line(string.Format("{0} instance = new {0}(mContentManagerName, false);", entityClassName))
                            .Line("mPool.AddToPool(instance);")
                        .End()
                    .End();
            }

            return codeBlock;
        }

        private static ICodeBlock GetInitializeFactoryMethod(ICodeBlock codeBlock, string className, bool poolObjects, string listToAssign)
        {
            codeBlock = codeBlock
                .Function("public static void", "Initialize", string.Format("string contentManager", className))
                    .Line("mContentManagerName = contentManager;");

            if (poolObjects)
            {
                codeBlock.Line("FactoryInitialize();");
            }

            codeBlock = codeBlock.End();

            return codeBlock;
        }

        private static ICodeBlock GetMakeUnusedFactory(ICodeBlock codeBlock, string factoryClassName, bool poolObjects)
        {
            string className = factoryClassName.Substring(0, factoryClassName.Length - "Factory".Length);

            codeBlock.Line("/// <summary>");
            codeBlock.Line("/// Makes the argument objectToMakeUnused marked as unused.  This method is generated to be used");
            codeBlock.Line("/// by generated code.  Use Destroy instead when writing custom code so that your code will behave");
            codeBlock.Line("/// the same whether your Entity is pooled or not.");
            codeBlock.Line("/// </summary>");

            codeBlock = codeBlock
                .Function("public static void", "MakeUnused", className + " objectToMakeUnused")
                    .Line("MakeUnused(objectToMakeUnused, true);")
                .End()
                ._()
                .Line("/// <summary>")
                .Line("/// Makes the argument objectToMakeUnused marked as unused.  This method is generated to be used")
                .Line("/// by generated code.  Use Destroy instead when writing custom code so that your code will behave")
                .Line("/// the same whether your Entity is pooled or not.")
                .Line("/// </summary>")
                .Function("public static void", "MakeUnused", className + " objectToMakeUnused, bool callDestroy");

            if (poolObjects)
            {
                codeBlock
                    .Line("mPool.MakeUnused(objectToMakeUnused);")
                    ._()
                    .If("callDestroy")
                        .Line("objectToMakeUnused.Destroy();")
                    .End();
            }
            else
            {
                // We still need to check if we should call destroy even if not pooled, because the parent may be pooled, in which case an infinite loop
                // can occur if we don't check the callDestroy value. More info on this bug:
                // http://www.hostedredmine.com/issues/413966
                codeBlock
                    .If("callDestroy")
                        .Line("objectToMakeUnused.Destroy();");
            }

            codeBlock = codeBlock.End();

            return codeBlock;
        }


    }



}
