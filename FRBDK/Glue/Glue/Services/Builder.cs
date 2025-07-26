using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Plugins.ExportedImplementations.CommandInterfaces;
using FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces;
using FlatRedBall.Glue.Projects;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FlatRedBall.Glue.Services;
public class Builder
{
    public static IHost App { get; private set; }

    public static T Get<T>() => App.Services.GetRequiredService<T>();

    public void Build()
    {
        var builder = Host.CreateApplicationBuilder();

        builder.Services.AddSingleton<NewProjectHelper>();
        builder.Services.AddSingleton<ProjectLoader>(ProjectLoader.Self);
        builder.Services.AddSingleton<IProjectCommands>(GlueCommands.Self.ProjectCommands);
        builder.Services.AddSingleton<FileReferenceManager>(FileReferenceManager.Self);
        builder.Services.AddSingleton<DragDropManager>(DragDropManager.Self);

        App = builder.Build();

        // These methods are needed until we kill singletons:
        ProjectLoader.Self.Initialize(Get<IProjectCommands>());
        (GlueCommands.Self.ProjectCommands as ProjectCommands).Initialize(Get<FileReferenceManager>());


    }
}
