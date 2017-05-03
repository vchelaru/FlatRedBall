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

namespace FlatRedBall.Glue.Parsing
{
    public class FactoryCodeGenerator : ElementComponentCodeGenerator
    {
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
            foreach (NamedObjectSave nos in element.NamedObjects)
            {
                if (!nos.InstantiatedByBase &&
                    nos.SourceType == SourceType.FlatRedBallType &&
                    nos.IsList &&
                    nos.IsDisabled == false &&
                    !string.IsNullOrEmpty(nos.SourceClassGenericType) &&
                    (nos.SourceClassGenericType.StartsWith("Entities\\") || nos.SourceClassGenericType.StartsWith("Entities/")))
                {

                    EntitySave sourceEntitySave = ObjectFinder.Self.GetEntitySave(nos.SourceClassGenericType);

                    if (sourceEntitySave == null)
                    {
                        Plugins.PluginManager.ReceiveError("Could not find the Entity " + nos.SourceClassGenericType + " which is referenced by " +
                            nos + ", so no code will be generated for this object");
                        return codeBlock;
                    }
                    string objectName = nos.FieldName;

                    if (sourceEntitySave.CreatedByOtherEntities)
                    {

                        string entityClassName = FileManager.RemovePath(nos.SourceClassGenericType);
                        string line = entityClassName + "Factory.Initialize(" + objectName + ", ContentManagerName);";

                        codeBlock.Line(line);
                    }

                    // If this Entity type is the base type, then the user
                    // may be using this list as the base type which gets populated
                    // by derived factories.
                    if (string.IsNullOrEmpty(sourceEntitySave.BaseEntity))
                    {
                        List<EntitySave> derivedList = ObjectFinder.Self.GetAllEntitiesThatInheritFrom(sourceEntitySave);

                        foreach (EntitySave derivedEntity in derivedList)
                        {
                            if (derivedEntity.CreatedByOtherEntities)
                            {
                                string derivedName = FileManager.RemovePath(derivedEntity.Name);
                                string replaceLine = derivedName + "Factory.Initialize(" + objectName + ", ContentManagerName);";

                                codeBlock.Line(replaceLine);
                            }
                        }
                    }


                }

            }
            return codeBlock;
        }

        public override ICodeBlock GenerateDestroy(ICodeBlock codeBlock, IElement element)
        {
            if (element is EntitySave && (element as EntitySave).CreatedByOtherEntities)
            {
                codeBlock
                    .If("Used")
                        .Line(string.Format("Factories.{0}Factory.MakeUnused(this, false);", FileManager.RemovePath(element.Name)));
            }



            foreach (NamedObjectSave namedObject in element.NamedObjects)
            {

                if (namedObject.SourceClassType == "PositionedObjectList<T>" && !string.IsNullOrEmpty(namedObject.SourceClassGenericType) &&
                    namedObject.SourceClassGenericType.StartsWith("Entities\\"))
                {
                    EntitySave sourceEntitySave = ObjectFinder.Self.GetEntitySave(namedObject.SourceClassGenericType);

                    if (sourceEntitySave == null)
                    {
                        Plugins.PluginManager.ReceiveError("Could not find the Entity " + namedObject.SourceClassGenericType + " which is referenced by " +
                            namedObject);
                        return codeBlock;
                    }

                    // August 23, 2012
                    // We don't Initialize
                    // factories inside of Entities,
                    // but for some reason we still destroyed
                    // them.  Added the check for element is ScreenSave.
                    if(element is ScreenSave)
                    {
                        if (sourceEntitySave.CreatedByOtherEntities)
                        {
                            string entityClassName = FileManager.RemovePath(namedObject.SourceClassGenericType);
                            string line = entityClassName + "Factory.Destroy();";

                            codeBlock.Line(line);
                        }

                        if (string.IsNullOrEmpty(sourceEntitySave.BaseEntity))
                        {
                            List<EntitySave> derivedList = ObjectFinder.Self.GetAllEntitiesThatInheritFrom(sourceEntitySave);

                            foreach (EntitySave derivedEntity in derivedList)
                            {
                                if (derivedEntity.CreatedByOtherEntities)
                                {
                                    string derivedName = FileManager.RemovePath(derivedEntity.Name);
                                    string replaceLine = derivedName + "Factory.Destroy();";

                                    codeBlock.Line(replaceLine);
                                }
                            }
                        }
                    }

                    
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

            if (isEntity && string.IsNullOrEmpty(element.BaseElement))
            {


                EntitySave asEntitySave = element as EntitySave;

                shouldCallPostInitializeBecauseIsPooled =
                    asEntitySave.CreatedByOtherEntities && asEntitySave.PooledByFactory;

                if (!shouldCallPostInitializeBecauseIsPooled)
                {
                    List<EntitySave> entities =
                        ObjectFinder.Self.GetAllEntitiesThatInheritFrom(asEntitySave);

                    foreach (EntitySave derivedEntity in entities)
                    {
                        if (derivedEntity.CreatedByOtherEntities && derivedEntity.PooledByFactory)
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
                if (System.IO.File.Exists(mEntireClassGenerator.ProjectSpecificFullFileName))
                {
                    FileHelper.DeleteFile(mEntireClassGenerator.ProjectSpecificFullFileName);
                }
                mEntireClassGenerator.RemoveSelfFromProject();
            }
            catch
            {
                System.Windows.Forms.MessageBox.Show("Error trying to remove the Factory " + mEntireClassGenerator.ClassName);

            }
        }

        public static void UpdateFactoryClass(EntitySave entitySave)
        {
            mEntireClassGenerator.EntitySave = entitySave;

            mEntireClassGenerator.GenerateAndAddToProjectIfNecessary();
        }

        public static void AddGeneratedPerformanceTypes()
        {
            string poolListFileName = FileManager.RelativeDirectory + @"Performance\PoolList.Generated.cs";
            string iEntityFactoryFileName = FileManager.RelativeDirectory + @"Performance\IEntityFactory.Generated.cs";

            // April 28, 2015
            // We killed IPoolable
            // from generated code as
            // it moved into the engine.
            // If anyone has old code we want
            // to re-generate it to overwrite the
            // file so the generated code uses the new
            // namespace. Eventually we should bring this
            // if-check back in, but for now, we'll leave it out:
            //if (!FileManager.FileExists(poolListFileName) || !FileManager.FileExists(iEntityFactoryFileName))
            {
                // Vic says:  This could be optimized, but it might not be worth the extra complexity
                // since this method is likely really fast.
                string contents = Resources.Resource1.PoolList;
                contents = CodeWriter.ReplaceNamespace(contents, ProjectManager.ProjectNamespace + ".Performance");
                FileManager.SaveText(contents, poolListFileName);

                contents = Resources.Resource1.IEntityFactory;
                contents = CodeWriter.ReplaceNamespace(contents, ProjectManager.ProjectNamespace + ".Performance");
                FileManager.SaveText(contents, iEntityFactoryFileName);

            }

            // These files may exist, but not be part of the project, so let's make sure that they are
            // part of the project
            bool wasPoolListAdded = ProjectManager.UpdateFileMembershipInProject(ProjectManager.ProjectBase, poolListFileName, false, false);
            bool wasEntityFactoryAdded = ProjectManager.UpdateFileMembershipInProject(ProjectManager.ProjectBase, iEntityFactoryFileName, false, false);
            if (wasPoolListAdded || wasEntityFactoryAdded)
            {
                Managers.TaskManager.Self.AddAsyncTask( ProjectManager.SaveProjects, "Saving Project because of performance file adds");
            }

        }

    }

    public class FactoryEntireClassGenerator : EntireClassCodeGenerator
    {
        public bool ShouldPoolObjects
        {
            get{ return EntitySave.PooledByFactory;}
        }


        public EntitySave EntitySave
        {
            get;
            set;
        }

        public override string ClassName
        {
            get { return FileManager.RemovePath(EntitySave.Name) + "Factory"; }
        }

        public override string Namespace
        {
            get { return "Factories"; }
        }

        public override string GetCode()
        {

            string entityClassName = FileManager.RemovePath(FileManager.RemoveExtension(EntitySave.Name));
            if (entityClassName.StartsWith("I"))
            {
                int m = 3;
            }
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
                                                ShouldPoolObjects);
            methodTag.InsertBlock(methodBlock);

            var codeBlock = new CodeBlockBase(null);
            codeBlock.Line("static string mContentManagerName;");
            codeBlock.Line("static PositionedObjectList<" + entityClassName + "> mScreenListReference;");
            
            if (!string.IsNullOrEmpty(baseEntityName))
            {
                codeBlock.Line("static PositionedObjectList<" + FileManager.RemovePath(baseEntityName) + "> mBaseScreenListReference;");

            }
            codeBlock.Line(string.Format("static PoolList<{0}> mPool = new PoolList<{0}>();", entityClassName));

            codeBlock.Line(string.Format("public static Action<{0}> EntitySpawned;", entityClassName));

            codeBlock.Function("object", "IEntityFactory.CreateNew", "")
                .Line(string.Format("return {0}.CreateNew();", factoryClassName));

            codeBlock.Function("object", "IEntityFactory.CreateNew", "Layer layer")
                .Line(string.Format("return {0}.CreateNew(layer);", factoryClassName));

            #region ScreenListReference property
            var propertyBlock = new CodeBlockProperty(codeBlock, "public static " + positionedObjectListType, "ScreenListReference");

            //codeContent.Property("ScreenListReference", Public: true, Static: true, Type: positionedObjectListType);
            propertyBlock.Get().Line("return mScreenListReference;").End();
            propertyBlock.Set().Line("mScreenListReference = value;").End();
            #endregion


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

        private static ICodeBlock GetAllFactoryMethods(string factoryClassName, string baseClassName, int numberToPreAllocate, bool poolObjects)
        {
            string className = factoryClassName.Substring(0, factoryClassName.Length - "Factory".Length);

            ICodeBlock codeBlock = new CodeDocument();

            GetCreateNewFactoryMethod(codeBlock, factoryClassName, poolObjects, baseClassName);
            codeBlock._();
            GetInitializeFactoryMethod(codeBlock, className, poolObjects, "mScreenListReference");
            codeBlock._();

            if (!string.IsNullOrEmpty(baseClassName))
            {
                GetInitializeFactoryMethod(codeBlock, FileManager.RemovePath(baseClassName), poolObjects, "mBaseScreenListReference");
                codeBlock._();
            }

            GetDestroyFactoryMethod(codeBlock, factoryClassName);
            codeBlock._();
            GetFactoryInitializeMethod(codeBlock, factoryClassName, numberToPreAllocate);
            codeBlock._();
            GetMakeUnusedFactory(codeBlock, factoryClassName, poolObjects);

            return codeBlock;
        }

        private static ICodeBlock GetCreateNewFactoryMethod(ICodeBlock codeBlock, string className, bool poolObjects, string baseEntityName)
        {
            className = className.Substring(0, className.Length - "Factory".Length);

            // no tabs needed on first line
            codeBlock
                .Function(StringHelper.SpaceStrings("public", "static", className), "CreateNew", "")
                    .Line("return CreateNew(null);")
                .End();

            codeBlock = codeBlock
                .Function(StringHelper.SpaceStrings("public", "static", className), "CreateNew", "Layer layer")
                    .If("string.IsNullOrEmpty(mContentManagerName)")
                        .Line("throw new System.Exception(\"You must first initialize the factory to use it. You can either add PositionedObjectList of type " +
                            className + " (the most common solution) or call Initialize in custom code\");")
                    .End()

                    .Line(className + " instance = null;");

            if (poolObjects)
            {
                codeBlock
                    .Line("instance = mPool.GetNextAvailable();")
                    .If("instance == null")
                        .Line("mPool.AddToPool(new " + className + "(mContentManagerName, false));")
                        .Line("instance =  mPool.GetNextAvailable();")
                    .End()
                    .Line("instance.AddToManagers(layer);");
            }
            else
            {
                codeBlock
                    .Line(string.Format("instance = new {0}(mContentManagerName, false);", className))
                    .Line("instance.AddToManagers(layer);");
            }

            CreateAddToListIfNotNullCode(codeBlock, "mScreenListReference");

            if (!string.IsNullOrEmpty(baseEntityName))
            {
                CreateAddToListIfNotNullCode(codeBlock, "mBaseScreenListReference");
            }

            codeBlock = codeBlock
                .If("EntitySpawned != null")
                    .Line("EntitySpawned(instance);")
                .End()
                .Line("return instance;")
            .End();

            return codeBlock;
        }

        private static ICodeBlock CreateAddToListIfNotNullCode(ICodeBlock codeBlock, string listName)
        {
            codeBlock
                .If(listName + " != null")
                    .Line(listName + ".Add(instance);")
                .End();

            return codeBlock;
        }

        private static ICodeBlock GetDestroyFactoryMethod(ICodeBlock codeBlock, string className)
        {
            className = className.Substring(0, className.Length - "Factory".Length);

            codeBlock
                .Function("public static void", "Destroy", "")
                    .Line("mContentManagerName = null;")
                    .Line("mScreenListReference = null;")
                    .Line("mPool.Clear();")
                    .Line("EntitySpawned = null;")
                .End();

            return codeBlock;
        }

        private static ICodeBlock GetFactoryInitializeMethod(ICodeBlock codeBlock, string factoryClassName, int numberToPreAllocate)
        {
            string entityClassName = factoryClassName.Substring(0, factoryClassName.Length - "Factory".Length);

            codeBlock
                .Function("private static void", "FactoryInitialize", "")
                    .Line("const int numberToPreAllocate = " + numberToPreAllocate + ";")
                    .For("int i = 0; i < numberToPreAllocate; i++")
                        .Line(string.Format("{0} instance = new {0}(mContentManagerName, false);", entityClassName))
                        .Line("mPool.AddToPool(instance);")
                    .End()
                .End();

            return codeBlock;
        }

        private static ICodeBlock GetInitializeFactoryMethod(ICodeBlock codeBlock, string className, bool poolObjects, string listToAssign)
        {
            codeBlock = codeBlock
                .Function("public static void", "Initialize", string.Format("FlatRedBall.Math.PositionedObjectList<{0}> listFromScreen, string contentManager", className))
                    .Line("mContentManagerName = contentManager;")
                    .Line(listToAssign + " = listFromScreen;");

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
