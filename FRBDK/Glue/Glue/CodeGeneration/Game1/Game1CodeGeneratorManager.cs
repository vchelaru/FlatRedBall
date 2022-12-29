using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlatRedBall.Glue.CodeGeneration.Game1
{
    internal class Game1CodeGeneratorManager
    {
        public static List<Game1CodeGenerator> Generators { get; private set; } = new List<Game1CodeGenerator>();
        public static string GetGame1GeneratedContents()
        {
            var topBlock = new CodeDocument(0);
            topBlock.Line("using System.Linq;");

            var namespaceBlock = topBlock.Namespace( GlueState.Self.ProjectNamespace);
            var classBlock = namespaceBlock.Class("public partial", "Game1");

            GenerateClassScope(classBlock);

            GenerateGeneratedInitialize(classBlock);

            GenerateGeneratedUpdate(classBlock);

            GenerateGeneratedDraw(classBlock);

            return topBlock.ToString();
        }

        private static void GenerateClassScope(ICodeBlock classBlock)
        {
            foreach(var generator in Generators)
            {
                generator.GenerateClassScope(classBlock);
            }
        }

        private static void GenerateGeneratedInitialize(ICodeBlock codeBlock)
        {
            var method = codeBlock.Function("partial void", "GeneratedInitialize", null);

            foreach (var generator in Generators.Where(item => item.CodeLocation == Plugins.Interfaces.CodeLocation.BeforeStandardGenerated))
            {
                generator.GenerateInitialize(method);
            }

            foreach (var generator in Generators.Where(item => item.CodeLocation == Plugins.Interfaces.CodeLocation.StandardGenerated))
            {
                generator.GenerateInitialize(method);
            }

            foreach (var generator in Generators.Where(item => item.CodeLocation == Plugins.Interfaces.CodeLocation.AfterStandardGenerated))
            {
                generator.GenerateInitialize(method);
            }
        }


        private static void GenerateGeneratedUpdate(ICodeBlock codeBlock)
        {
            var method = codeBlock.Function("partial void", "GeneratedUpdate", "Microsoft.Xna.Framework.GameTime gameTime");

            foreach (var generator in Generators.Where(item => item.CodeLocation == Plugins.Interfaces.CodeLocation.BeforeStandardGenerated))
            {
                generator.GenerateUpdate(method);
            }

            foreach (var generator in Generators.Where(item => item.CodeLocation == Plugins.Interfaces.CodeLocation.StandardGenerated))
            {
                generator.GenerateUpdate(method);
            }

            foreach (var generator in Generators.Where(item => item.CodeLocation == Plugins.Interfaces.CodeLocation.AfterStandardGenerated))
            {
                generator.GenerateUpdate(method);
            }
        }

        private static void GenerateGeneratedDraw(ICodeBlock codeBlock)
        {
            var method = codeBlock.Function("partial void", "GeneratedDraw", "Microsoft.Xna.Framework.GameTime gameTime");

            foreach (var generator in Generators.Where(item => item.CodeLocation == Plugins.Interfaces.CodeLocation.BeforeStandardGenerated))
            {
                generator.GenerateDraw(method);
            }

            foreach (var generator in Generators.Where(item => item.CodeLocation == Plugins.Interfaces.CodeLocation.StandardGenerated))
            {
                generator.GenerateDraw(method);
            }

            foreach (var generator in Generators.Where(item => item.CodeLocation == Plugins.Interfaces.CodeLocation.AfterStandardGenerated))
            {
                generator.GenerateDraw(method);
            }
        }
    }
}
