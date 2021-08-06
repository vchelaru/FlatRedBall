using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.Parsing;
using System.Threading.Tasks;
using FlatRedBall.Glue;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Elements;

namespace GlueCsvEditor.Data
{
    public class CachedTypes
    {
        protected readonly object _cacheLock = new object();
        protected bool _cacheReady;

        // Cached values
        protected List<ParsedEnum> _parsedProjectEnums;
        protected List<ParsedClass> _parsedPrjectClasses;
        protected List<EntitySave> _entities;
        protected List<ScreenSave> _screens;

        protected List<Type> _assemblyClasses;
        protected List<Type> _assemblyEnums;

        public delegate void CachedTypesReadyHandler();

        public CachedTypes(CachedTypesReadyHandler typesReadyHandler = null)
        {
            StartCacheTask(typesReadyHandler);
        }

        public bool IsCacheReady
        {
            get
            {
                lock (_cacheLock)
                {
                    return _cacheReady;
                }
            }
        }

        public IEnumerable<ParsedEnum> ProjectEnums
        { 
            get 
            {
                if (!IsCacheReady)
                    return new ParsedEnum[0];

                return _parsedProjectEnums; 
            } 
        }

        public IEnumerable<ParsedClass> ProjectClasses
        {
            get
            {
                if (!IsCacheReady)
                    return new ParsedClass[0];

                return _parsedPrjectClasses;
            }
        }

        public IEnumerable<EntitySave> ProjectEntities
        {
            get
            {
                if (!IsCacheReady)
                    return new EntitySave[0];

                return _entities;
            }
        }

        public IEnumerable<ScreenSave> ProjectScreens
        {
            get
            {
                if (!IsCacheReady)
                    return new ScreenSave[0];

                return _screens;
            }
        }

        public IEnumerable<string> BaseTypes
        {
            get
            {
                if (!IsCacheReady)
                    return new string[0];

                return new string[]
                {
                    "bool", "double", "float", "int", "Matrix", "string", "Texture2D", "Vector2", "Vector3",
                    "Vector4", "Color"
                };
            }
        }

        public IEnumerable<Type> AssemblyEnums
        {
            get
            {
                if (!IsCacheReady)
                    return new Type[0];

                return _assemblyEnums;
            }
        }

        public IEnumerable<Type> AssemblyClasses
        {
            get
            {
                if (!IsCacheReady)
                    return new Type[0];

                return _assemblyClasses;
            }
        }

        public IEnumerable<string> KnownTypes
        {
            get
            {
                if (!IsCacheReady)
                    return new string[0];

                return BaseTypes.Union(_assemblyEnums.Select(x => x.FullName))
                                .Union(_assemblyClasses.Select(x => x.FullName))
                                .Union(_entities.SelectMany(x => GetGlueStateNamespaces(x)))
                                .Union(_screens.SelectMany(x => GetGlueStateNamespaces(x)))
                                .Union(_parsedPrjectClasses.Select(x => string.Concat(x.Namespace, ".", x.Name)))
                                .Union(_parsedProjectEnums.Select(x => string.Concat(x.Namespace, ".", x.Name)));
            }
        }

        protected void StartCacheTask(CachedTypesReadyHandler typesReadyHandler)
        {
            FlatRedBall.Glue.Managers.TaskManager.Self.Add(
                () => PerformCache(typesReadyHandler),
                "Caching CSV types");
            //);
            //}).Start();
        }

        private void PerformCache(CachedTypesReadyHandler typesReadyHandler)
        {
            lock (_cacheLock)
            {
                _cacheReady = false;
            }

            try
            {
                PluginManager.ReceiveOutput("Caching of project types for CSV editor has begun.  " +
                                            "Some functionality will not be available until this is complete");

                // Save all the entity screens and 
                _entities = ObjectFinder.Self.GlueProject.Entities;
                _screens = ObjectFinder.Self.GlueProject.Screens;

                // Go through all the code in the project and generate a list of enums and classes
                var items = ProjectManager.ProjectBase.EvaluatedItems.ToArray().Where(x => x.ItemType == "Compile");
                string baseDirectory = ProjectManager.ProjectBase.Directory;

                _parsedPrjectClasses = new List<ParsedClass>();
                _parsedProjectEnums = new List<ParsedEnum>();

                foreach (var item in items)
                {
                    var file = new ParsedFile(baseDirectory + item.EvaluatedInclude);
                    foreach (var ns in file.Namespaces)
                    {
                        _parsedProjectEnums.AddRange(ns.Enums);
                        _parsedPrjectClasses.AddRange(ns.Classes);
                    }
                }

                // Get a list of all enums via reflection
                _assemblyEnums = AppDomain.CurrentDomain
                                          .GetAssemblies()
                                          .SelectMany(x => x.GetTypes())
                                          .Where(x => x.IsEnum)
                                          .ToList();

                _assemblyClasses = AppDomain.CurrentDomain
                                          .GetAssemblies()
                                          .SelectMany(x => x.GetTypes())
                                          .Where(x => !x.IsEnum)
                                          .ToList();
            }
            catch (Exception ex)
            {
                PluginManager.ReceiveOutput(
                    string.Concat(
                        "Exception occurred while caching project types: ",
                        ex.GetType(),
                        ":",
                        ex.Message));

                return;
            }

            lock (_cacheLock)
            {
                _cacheReady = true;
            }

            PluginManager.ReceiveOutput("Caching of project types completed");

            // Run the CachedTypesHandler delegate
            if (typesReadyHandler != null)
                typesReadyHandler();
        }

        protected IEnumerable<string> GetGlueStateNamespaces(EntitySave entity)
        {
            string ns = string.Concat(ProjectManager.ProjectNamespace,
                                      ".",
                                      entity.Name.Replace("\\", "."),
                                      ".");

            var states = new List<string>() { ns + "VariableState" };
            states.AddRange(entity.StateCategoryList
                                  .Where(x => !x.SharesVariablesWithOtherCategories)
                                  .Select(x => ns + x.Name)
                                  .ToArray());

            return states;
        }

        protected IEnumerable<string> GetGlueStateNamespaces(ScreenSave entity)
        {
            string ns = string.Concat(ProjectManager.ProjectNamespace,
                                      ".",
                                      entity.Name.Replace("\\", "."),
                                      ".");

            var states = new List<string>() { ns + "VariableState" };
            states.AddRange(entity.StateCategoryList
                                  .Where(x => !x.SharesVariablesWithOtherCategories)
                                  .Select(x => ns + x.Name)
                                  .ToArray());

            return states;
        }
    }
}
