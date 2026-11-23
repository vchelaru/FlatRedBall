using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FlatRedBall.Content.Particle;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.GuiDisplay.Facades;
using FlatRedBall.Glue.Plugins.ExportedInterfaces;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using Glue.IO;

namespace FlatRedBall.Glue.EditorProperties
{
    /// <summary>
    /// The properties the editor shows for a selected file (<see cref="ReferencedFileSave"/>): which apply to the file
    /// in its container, which can change, and the values offered. The WinForms Properties grid
    /// (<c>ReferencedFileSavePropertyGridDisplayer</c>) renders them and GlueCli's <c>set-file-property</c> sets them.
    /// Container, extension and state rules come from <see cref="ReferencedFileSaveElementExtensions.GetHiddenMemberNames"/>,
    /// which the WPF "Settings (Preview)" grid shares.
    /// </summary>
    public class ReferencedFileSavePropertyModel
    {
        /// <summary>The choice OpensWith offers that opens a file picker for an application.</summary>
        public const string NewApplication = "New Application...";

        public const string FileDetailsCategory = "File Details";

        public static readonly IReadOnlyList<string> UniformRowTypes = new[] { "<NONE>", "string[]", "string" };

        readonly IAvailableAssetTypesCore assetTypes;
        readonly IApplicationSettings applicationSettings;
        readonly IGlueCommands glueCommands;
        readonly Func<ReferencedFileSave, IEnumerable<AssetTypeInfo>> pluginAssetTypes;

        /// <param name="pluginAssetTypes">The extra asset types plugins offer for a file (PluginManager.GetAvailableAssetTypes).</param>
        public ReferencedFileSavePropertyModel(IAvailableAssetTypesCore assetTypes, IApplicationSettings applicationSettings,
            IGlueCommands glueCommands, Func<ReferencedFileSave, IEnumerable<AssetTypeInfo>> pluginAssetTypes)
        {
            this.assetTypes = assetTypes;
            this.applicationSettings = applicationSettings;
            this.glueCommands = glueCommands;
            this.pluginAssetTypes = pluginAssetTypes;
        }

        public IReadOnlyList<EditorProperty> GetProperties(ReferencedFileSave rfs)
        {
            var builder = new EditorPropertyListBuilder(rfs);

            foreach (var hidden in rfs.GetHiddenMemberNames())
            {
                builder.Exclude(hidden);
            }

            var extension = FileManager.GetExtension(rfs.Name);
            if (extension == "csv" || rfs.TreatAsCsv)
            {
                builder.Include(nameof(ReferencedFileSave.UniformRowType), () => UniformRowTypes);
            }

            if (extension == "png")
            {
                AddFileDetail(builder, rfs, "ImageWidth", typeof(int), () => ImageHeader.GetDimensions(glueCommands.GetAbsoluteFileName(rfs)).Width);
                AddFileDetail(builder, rfs, "ImageHeight", typeof(int), () => ImageHeader.GetDimensions(glueCommands.GetAbsoluteFileName(rfs)).Height);
            }

            if (extension == "emix")
            {
                AddFileDetail(builder, rfs, "EquilibriumParticleCount", typeof(float), () =>
                {
                    var fileName = glueCommands.GetAbsoluteFileName(rfs);
                    return File.Exists(fileName) ? EmitterSaveList.FromFile(fileName).GetEquilibriumParticleCount() : 0;
                });
                AddFileDetail(builder, rfs, "BurstParticleCount", typeof(float), () =>
                    EmitterSaveList.FromFile(glueCommands.GetAbsoluteFileName(rfs)).GetBurstParticleCount());
            }

            builder.Include(nameof(ReferencedFileSave.OpensWith), () => GetApplicationOptions(applicationSettings));

            if (rfs.LoadedAtRuntime)
            {
                builder.Include(nameof(ReferencedFileSave.RuntimeType), () => GetRuntimeTypeOptions(rfs));
            }
            else
            {
                builder.Exclude(nameof(ReferencedFileSave.RuntimeType));
            }

            if (rfs.IsCreatedByWildcard)
            {
                // A file from a wildcard follows the wildcard, so its properties are read-only. The grid has always
                // left fields and the properties picked from a list above (RuntimeType, OpensWith...) editable.
                foreach (var property in builder.Build().Where(item => item.Name != nameof(ReferencedFileSave.IsCreatedByWildcard) &&
                    item.IsOwnerMember && item.Options == null && typeof(ReferencedFileSave).GetProperty(item.Name) != null))
                {
                    builder.SetReadOnly(property.Name);
                }
            }
            else
            {
                builder.Exclude(nameof(ReferencedFileSave.IsCreatedByWildcard));
            }

            builder.SetOptions(nameof(ReferencedFileSave.BuildTool), () => GetBuildToolOptions(rfs));

            return builder.Build();
        }

        static void AddFileDetail(EditorPropertyListBuilder builder, ReferencedFileSave rfs, string name, Type type, Func<object?> getter) =>
            builder.Set(new EditorProperty(rfs, name, type) { IsReadOnly = true, Getter = getter, Category = FileDetailsCategory });

        /// <summary>The applications a file can open with, plus <see cref="NewApplication"/>.</summary>
        public static IReadOnlyList<string> GetApplicationOptions(IApplicationSettings settings) =>
            settings.AvailableApplications.Append(NewApplication).ToList();

        IReadOnlyList<string> GetRuntimeTypeOptions(ReferencedFileSave rfs)
        {
            var extension = FileManager.GetExtension(rfs.Name);
            return assetTypes.AllAssetTypes
                .Where(item => item.Extension == extension)
                .Concat(pluginAssetTypes(rfs))
                .Select(item => item.QualifiedRuntimeTypeName.QualifiedType)
                .ToList();
        }

        /// <summary>
        /// The build tools for the file's source file extension, with &lt;None&gt; when the file's type also loads
        /// without one.
        /// </summary>
        IReadOnlyList<string> GetBuildToolOptions(ReferencedFileSave rfs)
        {
            var extension = FileManager.GetExtension(rfs.Name);
            IEnumerable<string> tools = applicationSettings.AvailableBuildTools;
            if (!string.IsNullOrEmpty(rfs.SourceFile))
            {
                var whatToStartWith = "*." + FileManager.GetExtension(rfs.SourceFile);
                tools = tools.Where(item => item.StartsWith(whatToStartWith));
            }
            var toReturn = tools.ToList();
            if (assetTypes.AllAssetTypes.Any(item => item.Extension == extension && string.IsNullOrEmpty(item.CustomBuildToolName)))
            {
                toReturn.Insert(0, "<None>");
            }
            return toReturn;
        }
    }
}
