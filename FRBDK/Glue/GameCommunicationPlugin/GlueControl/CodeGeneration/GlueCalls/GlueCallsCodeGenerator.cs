using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameCommunicationPlugin.GlueControl.CodeGeneration.GlueCalls.GenerationConfiguration;
using FlatRedBall.Glue.CodeGeneration.CodeBuilder;

namespace GameCommunicationPlugin.GlueControl.CodeGeneration.GlueCalls
{
    internal class GlueCallsCodeGenerator
    {
        static string glueControlFolder => GlueState.Self.CurrentGlueProjectDirectory + "GlueControl/";

        public static readonly Dictionary<string, GenerationOptions> GlueCallsGenerated = new Dictionary<string, GenerationOptions>()
        {
            { "Editing.Managers.GluxCommands.cs", Editing_Managers_GluxCommands.GetGenerationOptions() },
            { "Editing.Managers.GlueState.cs", Editing_Managers_GlueState.GetGenerationOptions() }
        };

        public static void GenerateAll()
        {
            foreach (var item in GlueCallsGenerated)
            {
                SaveGlueCommunicationGeneratedFile(item.Key, item.Value);
            }
        }

        private static void SaveGlueCommunicationGeneratedFile(string resourcePath, GenerationOptions generationOptions)
        {
            var split = resourcePath.Split(".").ToArray();
            split = split.Take(split.Length - 1).ToArray(); // take off the .cs
            var combined = string.Join('/', split) + ".Generated.cs";
            var relativeDestinationFilePath = combined;

            string glueControlManagerCode = GenerateGlueCommunicationClass(generationOptions);
            FilePath destinationFilePath = glueControlFolder + relativeDestinationFilePath;
            GlueCommands.Self.ProjectCommands.CreateAndAddCodeFile(destinationFilePath);
            GlueCommands.Self.TryMultipleTimes(() => System.IO.File.WriteAllText(destinationFilePath.FullPath, glueControlManagerCode));
        }

        private static string GenerateGlueCommunicationClass(GenerationOptions generationOptions)
        {
            var bldr = new StringBuilder();

            //Defines
            foreach (var define in generationOptions.Defines)
            {
                bldr.AppendLine($"#define {define}");
            }

            //Usings
            bldr.AppendLine("");
            bldr.AppendLine("using GlueControl;");
            bldr.AppendLine("using GlueControl.Dtos;");
            bldr.AppendLine("using GlueControl.Models;");
            bldr.AppendLine("using System;");
            bldr.AppendLine("using System.Collections.Generic;");
            bldr.AppendLine("using System.Text;");
            bldr.AppendLine("using System.Threading.Tasks;");
            bldr.AppendLine("using Newtonsoft.Json.Linq;");
            bldr.AppendLine("using System.Reflection;");
            bldr.AppendLine("using System.Collections;");
            foreach (var u in generationOptions.Usings ?? new string[0])
            {
                bldr.AppendLine($"using {u};");
            }

            bldr.AppendLine();

            bldr.AppendLine($"namespace {generationOptions.Namespace}");
            bldr.AppendLine("{");
            bldr.AppendLine();

            bldr.Append($"   internal partial class {generationOptions.Name}");
            if (string.IsNullOrEmpty(generationOptions.BaseClass))
                bldr.Append($" : {generationOptions.BaseClass}");
            bldr.AppendLine();
            bldr.AppendLine("   {");

            if (generationOptions.AddStaticSelfReference)
            {
                bldr.AppendLine($"       public static {generationOptions.Name} Self {{ get; }}");
                bldr.AppendLine($"       static {generationOptions.Name}() => Self = new {generationOptions.Name}();");
            }

            foreach (var prop in generationOptions.Properties)
            {

                if (!string.IsNullOrEmpty(prop.GetSimpleBody) || !string.IsNullOrEmpty(prop.GetBody) || prop.IsAutomaticProperty)
                {
                    var propertyBlock = new CodeBlockProperty(null, $"public {prop.ReturnType}", prop.Name);
                    if (prop.IsAutomaticProperty)
                    {
                        propertyBlock.AutoGet();
                        propertyBlock.AutoSet();

                    }
                    else if (!string.IsNullOrEmpty(prop.GetSimpleBody))
                    {
                        propertyBlock.Get().Line($"return {prop.GetSimpleBody};");
                        //bldr.AppendLine($"          get => {prop.GetSimpleBody};");
                    }
                    else
                    {
                        propertyBlock.Get().Line(prop.GetBody);
                    }


                    bldr.AppendLine(propertyBlock.ToString());
                }



                if (prop.SetMethod != null)
                {
                    var m = prop.SetMethod;

                    bldr.Append("       public ");

                    //Return Type
                    bldr.Append("async Task<object> ");

                    //Name
                    bldr.Append($"{m.Name}(");

                    bool first = true;
                    foreach (var p in m.Parameters)
                    {
                        if (!first)
                            bldr.Append(", ");

                        bldr.Append($"{p.Type} ");
                        bldr.Append($"{p.Name}");

                        if (p.DefaultValue != null)
                        {
                            bldr.Append($" = {p.DefaultValue}");
                        }

                        first = false;
                    }

                    bldr.AppendLine($")");
                    bldr.AppendLine($"      {{");
                    bldr.AppendLine($"          var parameter = new GlueCallsClassGenerationManager.GlueParameters {{ Value = {m.Parameters[0].Name}");

                    if (m.Parameters[0].Dependencies?.Length > 0)
                    {
                        bldr.Append($", Dependencies = new Dictionary<string, object> {{");

                        bool firstD = true;
                        foreach (var d in m.Parameters[0].Dependencies)
                        {
                            if (!firstD)
                                bldr.Append($",");
                            bldr.Append($"{{ \"{d}\", {d} }}");

                            firstD = false;
                        }

                        bldr.Append($"}}");
                    }

                    bldr.AppendLine($"}};");
                    bldr.AppendLine();
                    bldr.AppendLine();

                    bldr.AppendLine($"          return await GlueCallsClassGenerationManager.ConvertToPropertyCallToGame(nameof({prop.Name}), typeof({prop.ReturnType}), parameter, new GlueCallsClassGenerationManager.CallPropertyParameters");
                    bldr.AppendLine($"          {{");
                    bldr.AppendLine($"              ReturnToPropertyType = {prop.ReturnToPropertyType.ToString().ToLowerInvariant()}");
                    bldr.AppendLine($"          }});");
                    bldr.AppendLine($"      }}");
                }

            }

            foreach (var m in generationOptions.Methods)
            {
                bldr.Append("       public ");

                //Return Type
                bldr.Append("async Task ");

                if (!string.IsNullOrEmpty(m.ReturnType))
                    bldr.Append($"<{m.ReturnType}> ");

                //Name
                bldr.Append($"{m.Name}(");

                bool first = true;
                foreach (var p in m.Parameters)
                {
                    if (!first)
                        bldr.Append(", ");

                    bldr.Append($"{p.Type} ");
                    bldr.Append($"{p.Name}");

                    if (p.DefaultValue != null)
                    {
                        bldr.Append($" = {p.DefaultValue}");
                    }

                    first = false;
                }

                if (m.AddEchoToGame)
                    bldr.Append(", bool echoToGame = false");

                bldr.AppendLine(")");
                bldr.AppendLine("       {");
                bldr.Append($"           var currentMethod = typeof({generationOptions.Name}).GetMethod(\"{m.Name}\", new Type[] {{");
                first = true;
                foreach (var p in m.Parameters)
                {
                    if (!first)
                        bldr.Append(", ");

                    bldr.Append($"typeof({p.Type})");

                    first = false;
                }

                if (m.AddEchoToGame)
                    bldr.Append(", typeof(bool) ");

                bldr.AppendLine(" });");

                bldr.AppendLine($"           var parameters = new Dictionary<string, GlueCallsClassGenerationManager.GlueParameters>");
                bldr.AppendLine("           {");
                foreach (var p in m.Parameters.Where(item => item.IsParameterUsedByGlue))
                {
                    bldr.Append($"               {{ \"{p.Name}\", new GlueCallsClassGenerationManager.GlueParameters {{ Value = {p.Name}");

                    if (p.Dependencies != null)
                    {
                        bldr.Append(", Dependencies = new Dictionary<string, object> { ");

                        bool firstD = true;
                        foreach (var d in p.Dependencies)
                        {
                            if (!firstD)
                                bldr.Append(", ");

                            bldr.Append($"{{ \"{d}\", {d} }}");

                            firstD = false;
                        }

                        bldr.Append(" } ");
                    }

                    bldr.AppendLine(" } },");
                }
                bldr.AppendLine("           };");

                bldr.AppendLine($"           {(!string.IsNullOrEmpty(m.ReturnType) ? $"return ({m.ReturnType}) " : "")}await GlueCallsClassGenerationManager.ConvertToMethodCallToGame(currentMethod, parameters, new GlueCallsClassGenerationManager.CallMethodParameters");
                bldr.AppendLine("           {");
                bldr.AppendLine($"              EchoToGame = {(m.AddEchoToGame ? "echoToGame" : "false")}");
                bldr.AppendLine($"           }});");
                bldr.AppendLine("      }");
                bldr.AppendLine();
            }

            bldr.AppendLine("   }");
            bldr.AppendLine("}");

            return bldr.ToString();
        }
    }
}
