using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        App = builder.Build();

    }
}
