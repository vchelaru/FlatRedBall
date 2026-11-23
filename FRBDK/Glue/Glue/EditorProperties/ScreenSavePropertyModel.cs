using System;
using System.Collections.Generic;
using System.Linq;
using FlatRedBall.Glue.Plugins.ExportedInterfaces;
using FlatRedBall.Glue.SaveClasses;

namespace FlatRedBall.Glue.EditorProperties
{
    /// <summary>
    /// The properties the editor shows for a selected screen: which apply, which can change, and the values offered.
    /// Plugins add or hide properties through <c>PluginBase.AdjustScreenProperties</c>. The WinForms Properties grid
    /// (<c>ScreenSavePropertyGridDisplayer</c>) renders them and GlueCli's <c>set-property</c> sets them.
    /// </summary>
    public class ScreenSavePropertyModel : ElementPropertyModel
    {
        public const string NameCategory = "\tScreen";

        readonly IGlueState glueState;
        readonly Action<ScreenSave, EditorPropertyListBuilder> adjustByPlugins;

        /// <param name="adjustByPlugins">Lets plugins add or hide properties (PluginManager.AdjustScreenProperties).</param>
        public ScreenSavePropertyModel(IGlueState glueState, IGlueCommands glueCommands,
            Action<ScreenSave, EditorPropertyListBuilder> adjustByPlugins) : base(glueCommands)
        {
            this.glueState = glueState;
            this.adjustByPlugins = adjustByPlugins;
        }

        public IReadOnlyList<EditorProperty> GetProperties(ScreenSave screen)
        {
            var builder = CreateBuilder(screen);

            builder.Include(nameof(ScreenSave.BaseScreen), () => GetOtherScreens(screen));
            builder.Include(nameof(ScreenSave.NextScreen), () => GetOtherScreens(screen));

            AddName(builder, screen, NameCategory);

            adjustByPlugins(screen, builder);

            return builder.Build();
        }

        IReadOnlyList<string> GetOtherScreens(ScreenSave screen) =>
            new[] { "<NONE>" }
                .Concat(glueState.CurrentGlueProject.Screens.Where(item => item.Name != screen.Name).Select(item => item.Name))
                .ToList();
    }
}
