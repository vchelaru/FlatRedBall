using FlatRedBall;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Plugins.Interfaces;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Math;
using OfficialPlugins.Common.Controls;
using OfficialPlugins.SpritePlugin.CodeGenerators;
using OfficialPlugins.SpritePlugin.Errors;
using OfficialPlugins.SpritePlugin.Managers;
using OfficialPlugins.SpritePlugin.Views;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Drawing;
using System.Linq;
using System.Text;

namespace OfficialPlugins.SpritePlugin
{
    [Export(typeof(PluginBase))]
    internal class MainSpritePlugin : PluginBase
    {
        public override string FriendlyName => "Sprite Plugin";

        public override void StartUp()
        {
            AssetTypeInfoManager.HandleStartup();

            this.RegisterCodeGenerator(new SpriteCodeGenerator());

            this.AddErrorReporter(new SpriteMissingTextureErrorReporter());

            AssignEvents();
        }

        private void AssignEvents()
        {
            // This should be early so the variable can be added before codegen:
            this.ReactToLoadedGluxEarly += HandleGluxLoaded;

            // hide/remove available properties here...

            this.ReactToNamedObjectChangedValueList += HandleVariableChangeList;
        }

        private void HandleVariableChangeList(List<VariableChangeArguments> variableList)
        {
            foreach(var variable in variableList)
            {
                TextureVariableManager.Self.HandleChange(variable);
            }

            // Refresh reactively so a missing-Texture warning shows up immediately rather than
            // only after a manual Refresh click or project reload.
            GlueCommands.Self.RefreshCommands.RefreshErrors();
        }

        private void HandleGluxLoaded()
        {

            AssetTypeInfoManager.HandleGluxLoaded();

        }
    }

}

