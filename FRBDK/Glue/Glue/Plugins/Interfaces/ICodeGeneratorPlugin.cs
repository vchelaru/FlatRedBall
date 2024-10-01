using System;
using System.Collections.Generic;
using FlatRedBall.Glue.CodeGeneration;
using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.SaveClasses;

namespace FlatRedBall.Glue.Plugins.Interfaces
{

    public enum CodeLocation
    {
        BeforeStandardGenerated,
        StandardGenerated,
        AfterStandardGenerated
    }

    struct ComponentPluginPair
    {
        public ICodeGeneratorPlugin Plugin;
        public ElementComponentCodeGenerator Generator;

        public ComponentPluginPair(ICodeGeneratorPlugin plugin, ElementComponentCodeGenerator generator)
        {
            Plugin = plugin;
            Generator = generator;
        }

    }

    public interface ICodeGeneratorPlugin : IPlugin
    {

        void CodeGenerationStart(GlueElement element);

        #region XML Docs
        /// <summary>
        /// A list which can contain multiple
        /// ElementComponentCodeGenerators.  If
        /// this is null, Glue will ignore this member.
        /// </summary>
        #endregion
        IEnumerable<ElementComponentCodeGenerator> CodeGeneratorList
        {
            get;
        }
    }

    public static class CodeGeneratorPluginMethods
    {

        public static void CallCodeGenerationStart(PluginManager pluginManager, GlueElement element)
        {
            foreach (ICodeGeneratorPlugin plugin in pluginManager.CodeGeneratorPlugins)
            {
                plugin.CodeGenerationStart(element);

            }
        }




        public static void GenerateActivityPluginCode(CodeLocation codeLocation, PluginManager pluginManager,
            ICodeBlock codeBlock, GlueElement element)
        {

            foreach (ComponentPluginPair cpp in GetPluginsIn(pluginManager, codeLocation))
            {
                GenerateWithException(cpp.Generator.GenerateActivity, codeBlock, element, cpp.Plugin, pluginManager);
            }
        }


        public static void GenerateAdditionalMethodsPluginCode(PluginManager pluginManager,
            ICodeBlock codeBlock, GlueElement element)
        {
            foreach (ComponentPluginPair cpp in GetPluginsIn(pluginManager))
            {
                GenerateWithException(cpp.Generator.GenerateAdditionalMethods, codeBlock, element, cpp.Plugin, pluginManager);
            }
        }

        //TODO:  Add more generation types here

        delegate ICodeBlock GenerateDelegate(ICodeBlock codeBlock, GlueElement element);

        static void GenerateWithException(GenerateDelegate method, ICodeBlock codeBlock, GlueElement element, IPlugin plugin, PluginManager pluginManager)
        {
            try
            {
                method(codeBlock, element);
            }
            catch (Exception e)
            {
                PluginContainer pluginContainer = pluginManager.PluginContainers[plugin];
                pluginContainer.Fail(e, "Failed during code generation");
            }

        }

        static IEnumerable<ComponentPluginPair> GetPluginsIn(PluginManager pluginManager)
        {
            foreach (ICodeGeneratorPlugin plugin in pluginManager.CodeGeneratorPlugins)
            {

                PluginContainer pluginContainer = pluginManager.PluginContainers[plugin];

                if (pluginContainer.IsEnabled)
                {
                    if (plugin.CodeGeneratorList != null)
                    {
                        IEnumerable<ElementComponentCodeGenerator> codeGenerators =
                            plugin.CodeGeneratorList;

                        foreach (ElementComponentCodeGenerator eccg in codeGenerators)
                        {
                            yield return new ComponentPluginPair(plugin, eccg);
                        }
                    }
                }
            }
        }

        static IEnumerable<ComponentPluginPair> GetPluginsIn(PluginManager pluginManager, CodeLocation codeLocation)
        {
            foreach (ICodeGeneratorPlugin plugin in pluginManager.CodeGeneratorPlugins)
            {

                PluginContainer pluginContainer = pluginManager.PluginContainers[plugin];

                if (pluginContainer.IsEnabled)
                {
                    if (plugin.CodeGeneratorList != null)
                    {
                        IEnumerable<ElementComponentCodeGenerator> codeGenerators =
                            plugin.CodeGeneratorList;

                        foreach (ElementComponentCodeGenerator eccg in codeGenerators)
                        {
                            if (eccg.CodeLocation == codeLocation)
                            {
                                yield return new ComponentPluginPair(plugin, eccg);
                            }
                        }
                    }
                }
            }
        }
    }


}
