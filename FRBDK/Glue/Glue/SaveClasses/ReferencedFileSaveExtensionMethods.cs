using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall.IO;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Content;

using EditorObjects.Parsing;
using FlatRedBall.Glue.Errors;
using System.Windows.Forms;
using EditorObjects.SaveClasses;
using System.IO;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;

namespace FlatRedBall.Glue.SaveClasses
{
    public static class ReferencedFileSaveExtensionMethods
    {
        // FixAllTypes moved to GlueCommon.SaveClasses.ReferencedFileSavePropertyExtensions (#2276) -
        // only touches the ReferencedFileSave's own Properties list and the now-GlueCommon
        // CustomVariableCommonExtensions.FixValue.

        // GetUnqualifiedTypeForCsv and GetTypeForCsvFile moved to
        // GlueCommon.SaveClasses.ReferencedFileSaveTypeExtensions (#2276) - needed the new
        // IGlueStateCore seam over GlueState.Self.ProjectNamespace, plus the existing
        // IObjectFinderCore.GlueProject.

        // Both IsFileSourceForThis overloads moved to
        // GlueCommon.SaveClasses.ReferencedFileSaveSourceExtensions (#2276) - needed
        // IObjectFinderCore widened with MakeAbsoluteContent.

        // GetInstanceName, GetContainer, GetContainerType, ReferencedFileSaveToString,
        // GetIsSharedStaticEditable, both GetIsLinkedOutsideContainerFolder overloads, and
        // GetIsFileOutsideContainerFolder moved to GlueCommon.SaveClasses.ReferencedFileSaveElementExtensions
        // (#2276) - needed IObjectFinderCore.GetElementContaining widened with a ReferencedFileSave overload.

        // GetIsFileOutOfDate moved to GlueCommon.SaveClasses.ReferencedFileSaveBuildExtensions (#2276) -
        // needed the new IBuildToolAssociationCore seam over BuildToolAssociationManager.Self, plus
        // IGlueStateCore widened with CurrentMainProjectDirectory.

        // GetAssetTypeInfo, GetCanUseContentPipeline, and GetGeneratesMember moved to
        // GlueCommon.SaveClasses.ReferencedFileSaveAssetTypeExtensions (#2276) - GetAssetTypeInfo needed
        // the IAvailableAssetTypesCore seam over AvailableAssetTypes.Self; the other two only called it
        // indirectly, through GetAssetTypeInfo.

        // GetProperty<T> and SetProperty moved to
        // GlueCommon.SaveClasses.ReferencedFileSavePropertyExtensions (#2276) - zero coupling, only
        // touch the ReferencedFileSave's own Properties list.
    }
}
