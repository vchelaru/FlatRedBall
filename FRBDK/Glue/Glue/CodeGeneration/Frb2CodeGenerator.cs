using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;

namespace FlatRedBall.Glue.CodeGeneration
{
    /// <summary>
    /// Generates FindByName-style typed accessors for FlatRedBall 2 Screens/Entities. The generated
    /// class binds to an object tree FlatRedBall2's own GlueScreen/GlueEntity already built from the
    /// loaded .glsj/.glej JSON via reflection - it never constructs anything itself, unlike FRB1's
    /// generated code.
    /// </summary>
    /// <remarks>
    /// Deliberately a separate pipeline from FRB1's <see cref="CodeWriter"/>, not a branch inside it.
    /// Most of what CodeWriter's generators do - factories, pooling, states, events, Game1, camera
    /// setup - either has no FRB2 equivalent yet or is owned entirely by FRB2's reflection-based
    /// loader, so running that pipeline against an FRB2 project would generate code against APIs that
    /// do not exist there.
    /// <para>
    /// Only covers <see cref="NamedObjectSave"/> instances whose type FRB2 can already build - see
    /// <see cref="MappedTypesByGlueName"/>. Everything else (lists, tile maps, and any other type a
    /// later phase of FRB2's own object builder owns) is intentionally left untyped: the generated
    /// class still runs, the object is still reachable through the inherited <c>Objects</c>
    /// dictionary, and a generated field naming a type that cannot build would not compile.
    /// </para>
    /// </remarks>
    public static class Frb2CodeGenerator
    {
        // Mirrors FlatRedBall2/src/Glue/GlueTypeMap.cs's own small, deliberately incomplete type
        // table (that file's own comment: "covers only the types this phase can build"). Glue does
        // not reference the FlatRedBall2 assembly, so this is a second copy kept in sync by hand -
        // the same tradeoff the embedded GlueProjectSave copy already makes for live-edit (see the
        // glue-project-codegen skill's note on that file). A type missing here is not a gap in this
        // generator: it means FRB2's own runtime cannot build that type yet either.
        private static readonly Dictionary<string, string> MappedTypesByGlueName = new(StringComparer.Ordinal)
        {
            ["FlatRedBall.Sprite"] = "FlatRedBall2.Rendering.Sprite",
            ["FlatRedBall.Math.Geometry.AxisAlignedRectangle"] = "FlatRedBall2.Collision.AARect",
            ["FlatRedBall.Math.Geometry.Circle"] = "FlatRedBall2.Collision.Circle",
            ["FlatRedBall.Math.Geometry.Polygon"] = "FlatRedBall2.Collision.Polygon",
            ["FlatRedBall.Entities.CameraControllingEntity"] = "FlatRedBall2.Entities.CameraControllingEntity",
        };

        /// <summary>
        /// Generates (or regenerates) <c>&lt;Name&gt;.Generated.cs</c> for a Screen or Entity, and
        /// seeds the sibling custom <c>&lt;Name&gt;.cs</c> once if it does not exist yet. Safe to call
        /// whenever the loaded project is FRB2 and has opted into code generation - callers are not
        /// expected to check that first, since the <c>IGenerateCodeCommands</c> seam
        /// (<see cref="Plugins.ExportedImplementations.CodeWritePolicy.GeneratesFrb2Code"/>) already
        /// gates whether generation runs at all.
        /// </summary>
        /// <remarks>
        /// Writes both files directly rather than through <c>FileCommands.SaveIfDiffers</c> /
        /// <c>CodeProjectHelper.CreateAndAddPartialGeneratedCodeFile</c>, which is what FRB1's CodeWriter
        /// uses. Those two exist to add the file to the .csproj as a nested item and to honour
        /// <c>CodeWritePolicy.WritesCodeForCurrentProject</c> - and an FRB2 project wants neither. Its
        /// .csproj is not Glue's to write, and the SDK-style project already globs <c>**/*.cs</c>, so
        /// there is no project item to add; going through those seams is what previously let Glue
        /// rewrite an FRB2 .csproj on every generate.
        /// </remarks>
        public static Task GenerateCode(GlueElement element)
        {
            if (element == null)
            {
                throw new ArgumentNullException(nameof(element));
            }

            string elementNamespace = GlueCommands.Self.GenerateCodeCommands.GetNamespaceForElement(element);

            WriteIfDiffers(
                GlueCommands.Self.FileCommands.GetGeneratedCodeFilePath(element),
                GenerateGeneratedFileContents(element, elementNamespace));

            CreateCustomCodeFileIfMissing(element, elementNamespace);

            return Task.CompletedTask;
        }

        /// <summary>
        /// Writes <paramref name="contents"/> to <paramref name="filePath"/>, creating the directory,
        /// and does nothing at all when the file already says exactly that.
        /// </summary>
        /// <remarks>
        /// The unchanged-content check is not just an optimisation: rewriting identical bytes still
        /// raises a file-watcher event and still marks the file dirty for anything watching the project
        /// folder. It is the same behaviour <c>FileCommands.SaveIfDiffers</c> provides for FRB1.
        /// </remarks>
        private static void WriteIfDiffers(FilePath filePath, string contents)
        {
            if (filePath.Exists() && FileManager.FromFileText(filePath.FullPath) == contents)
            {
                return;
            }

            System.IO.Directory.CreateDirectory(filePath.GetDirectoryContainingThis().FullPath);
            FlatRedBall.Glue.IO.FileWatchManager.IgnoreNextChangeOnFile(filePath.FullPath);
            FileManager.SaveText(contents, filePath.FullPath);
        }

        /// <summary>
        /// Builds the contents of <c>&lt;Name&gt;.Generated.cs</c>. Takes the namespace as a parameter
        /// rather than resolving it itself so this is pure - no IO, no GlueState - and directly unit
        /// testable against a plain <see cref="ScreenSave"/>/<see cref="EntitySave"/>.
        /// </summary>
        public static string GenerateGeneratedFileContents(GlueElement element, string elementNamespace)
        {
            string className = element.ClassName;
            string baseType = element is EntitySave
                ? "FlatRedBall2.Glue.GlueEntity"
                : "FlatRedBall2.Glue.GlueScreen";

            var typedObjects = element.NamedObjects
                .Where(nos => !string.IsNullOrEmpty(nos?.InstanceName))
                .Select(nos => (Nos: nos, Frb2TypeName: ResolveFrb2TypeName(nos)))
                .ToList();

            var sb = new StringBuilder();

            // The first line is the proof-of-authorship marker every reconciliation path in Glue
            // relies on to know it is safe to delete this file - see the glue-project-codegen skill.
            sb.AppendLine("//Code for " + element.Name);
            sb.AppendLine();
            sb.AppendLine("namespace " + elementNamespace);
            sb.AppendLine("{");
            sb.AppendLine($"    public partial class {className} : {baseType}");
            sb.AppendLine("    {");

            foreach (var (nos, frb2TypeName) in typedObjects)
            {
                if (frb2TypeName != null)
                {
                    sb.AppendLine($"        public {frb2TypeName} {nos.InstanceName} {{ get; private set; }}");
                }
            }

            sb.AppendLine();
            sb.AppendLine("        public override void CustomInitialize()");
            sb.AppendLine("        {");
            sb.AppendLine("            base.CustomInitialize();");

            foreach (var (nos, frb2TypeName) in typedObjects)
            {
                if (frb2TypeName != null)
                {
                    sb.AppendLine($"            {nos.InstanceName} = ({frb2TypeName})Objects[\"{nos.InstanceName}\"];");
                }
                else
                {
                    sb.AppendLine($"            // '{nos.InstanceName}' ({nos.SourceClassType}) has no typed accessor yet - use Objects[\"{nos.InstanceName}\"].");
                }
            }

            sb.AppendLine("            CustomInitializeAfterObjects();");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        partial void CustomInitializeAfterObjects();");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            return sb.ToString();
        }

        /// <summary>
        /// Resolves the FRB2 CLR type name for a NamedObjectSave, or null when this generator does
        /// not yet know how to type it safely. Public for direct unit testing.
        /// </summary>
        public static string ResolveFrb2TypeName(NamedObjectSave nos)
        {
            // FlatRedBall.Math.PositionedObjectList<T> has no FRB2 equivalent yet - see
            // FlatRedBall2/src/Glue/GlueTypeMap.cs's own comment on this.
            if (nos.IsList)
            {
                return null;
            }

            string sourceClassType = nos.SourceClassType;

            if (string.IsNullOrEmpty(sourceClassType))
            {
                return null;
            }

            // An element reference (a nested Screen/Entity instance, e.g. "Entities\Player") names
            // another Glue element rather than a CLR type - same backslash rule FRB2's own
            // GlueTypeName.IsElementReference uses. FRB2's GlueProject.CreateEntity always constructs
            // the shared GlueEntity type for these today, never a generated subclass, so that is the
            // most specific type this can honestly claim regardless of which entity is referenced.
            if (sourceClassType.Contains('\\'))
            {
                return "FlatRedBall2.Glue.GlueEntity";
            }

            return MappedTypesByGlueName.TryGetValue(sourceClassType, out string frb2TypeName)
                ? frb2TypeName
                : null;
        }

        private static void CreateCustomCodeFileIfMissing(GlueElement element, string elementNamespace)
        {
            FilePath customCodePath = GlueCommands.Self.FileCommands.GetCustomCodeFilePath(element);

            if (customCodePath.Exists())
            {
                return;
            }

            WriteCustomCode(element, elementNamespace);
        }

        /// <summary>
        /// Writes the custom <c>&lt;Name&gt;.cs</c> stub. Public, like FRB1's
        /// <c>IGenerateCodeCommands.GenerateElementCustomCode</c>, so it can be called directly to
        /// re-create a file the user chose to delete.
        /// </summary>
        public static void GenerateCustomCode(GlueElement element) =>
            WriteCustomCode(element, GlueCommands.Self.GenerateCodeCommands.GetNamespaceForElement(element));

        private static void WriteCustomCode(GlueElement element, string elementNamespace) =>
            WriteIfDiffers(
                GlueCommands.Self.FileCommands.GetCustomCodeFilePath(element),
                GenerateCustomCodeContents(element, elementNamespace));

        /// <summary>Pure content-building half of <see cref="GenerateCustomCode"/>, for direct unit testing.</summary>
        public static string GenerateCustomCodeContents(GlueElement element, string elementNamespace)
        {
            string className = element.ClassName;

            return
$@"namespace {elementNamespace}
{{
    partial class {className}
    {{
        partial void CustomInitializeAfterObjects()
        {{

        }}
    }}
}}
";
        }
    }
}
