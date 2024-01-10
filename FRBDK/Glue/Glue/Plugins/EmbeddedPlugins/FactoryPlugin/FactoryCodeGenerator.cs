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
using FlatRedBall.Glue.Parsing;

namespace FlatRedBall.Glue.Plugins.EmbeddedPlugins.FactoryPlugin
{
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

            classProperties.Members = new List<TypedMemberBase>();

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
            if (EntitySave.PooledByFactory)
            {
                codeBlock.Line($"static PoolList<{entityClassName}> mPool = new PoolList<{entityClassName}>();");
                codeBlock.Line("public static int PoolCount = mPool.Count;");
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
            if (!string.IsNullOrEmpty(baseClassName))
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
            if (hasEditMode)
            {
                functionBlock.Line("// If in edit mode and viewing a screen, don't pre-allocate because the content manager may not be set which would cause a crash");
                functionBlock.If("FlatRedBall.Screens.ScreenManager.IsInEditMode && FlatRedBall.Screens.ScreenManager.CurrentScreen?.GetType().Name == \"EntityViewingScreen\"")
                    .Line("numberToPreAllocate = 0;");
            }

            if (entity.PooledByFactory)
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
