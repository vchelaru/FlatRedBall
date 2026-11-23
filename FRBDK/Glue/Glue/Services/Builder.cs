using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlatRedBall.Glue.EditorProperties;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.Managers;
using GlueFormsCore.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Plugins.ExportedImplementations.CommandInterfaces;
using FlatRedBall.Glue.Plugins.ExportedInterfaces;
using FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces;
using FlatRedBall.Glue.Projects;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.SetVariable;
using Microsoft.Extensions.DependencyInjection;

namespace FlatRedBall.Glue.Services;
public class Builder
{
    public static IServiceProvider Services { get; private set; }

    public static T Get<T>() => Services.GetRequiredService<T>();

    public void Build()
    {
        // A plain container: Glue uses none of the generic host's logging, configuration or hosted
        // services, and building that host costs a noticeable part of every startup.
        var services = new ServiceCollection();

        services.AddSingleton<NameVerifier>();
        services.AddSingleton<ReferencedFileSaveSetPropertyManager>();
        services.AddSingleton<EventResponseSaveSetVariableLogic>();
        services.AddSingleton<StateSaveSetVariableLogic>();
        services.AddSingleton<NamedObjectSetVariableLogic>();
        services.AddSingleton<CustomVariableSaveSetPropertyLogic>();
        services.AddSingleton<EntitySaveSetPropertyLogic>();
        services.AddSingleton<ScreenSaveSetVariableLogic>();
        services.AddSingleton<GlobalContentSetVariableLogic>();
        services.AddSingleton<ElementCommands>();
        services.AddSingleton<NewProjectHelper>();
        services.AddSingleton<ProjectLoader>(ProjectLoader.Self);
        services.AddSingleton<IProjectCommands>(sp => GlueCommands.Self.ProjectCommands);
        services.AddSingleton<FileReferenceManager>(FileReferenceManager.Self);
        services.AddSingleton<DragDropManager>(DragDropManager.Self);
        services.AddSingleton<IDialogCommands>(sp => GlueCommands.Self.DialogCommands);
        services.AddSingleton<IFileCommands>(sp => GlueCommands.Self.FileCommands);
        services.AddSingleton<IGlueCommands>(sp => GlueCommands.Self);
        services.AddSingleton<IGlueState>(sp => GlueState.Self);
        services.AddSingleton<ProjectLoadOperations>();
        services.AddSingleton<CustomVariablePropertyModel>();
        services.AddSingleton(sp => new ReferencedFileSavePropertyModel(
            AvailableAssetTypes.Self, EditorData.FileAssociationSettings, sp.GetRequiredService<IGlueCommands>(),
            PluginManager.GetAvailableAssetTypes));
        services.AddSingleton(sp => new NamedObjectSavePropertyModel(
            sp.GetRequiredService<IGlueState>(), AvailableAssetTypes.Self, PluginManager.AdjustNamedObjectProperties));
        services.AddSingleton(sp => new EntitySavePropertyModel(
            sp.GetRequiredService<IGlueState>(), sp.GetRequiredService<IGlueCommands>(), AvailableAssetTypes.Self,
            PluginManager.AdjustEntityProperties));
        services.AddSingleton(sp => new ScreenSavePropertyModel(
            sp.GetRequiredService<IGlueState>(), sp.GetRequiredService<IGlueCommands>(), PluginManager.AdjustScreenProperties));

        Services = services.BuildServiceProvider();

        // These methods are needed until we kill singletons:
        InheritanceManager.Self = new InheritanceManager(ReferenceService.Self);
        ProjectLoader.Self.Initialize(Get<IProjectCommands>(), Get<ProjectLoadOperations>());
        (GlueCommands.Self.ProjectCommands as ProjectCommands).Initialize(Get<FileReferenceManager>());


    }
}
