using System.Collections.Generic;
using FlatRedBall.Glue.SaveClasses;

namespace GlueUnitTests.PropertyGrid;

/// <summary>
/// A small in-memory project with the files and variables the property parity tests show: files of several kinds in a
/// screen, an entity and Global Content, and variables that are new, tunnel, convert their type, or come from a base.
/// </summary>
public static class EditorPropertyFixtures
{
    public sealed record FileCase(string Key, ReferencedFileSave File);

    public sealed record VariableCase(string Key, CustomVariable Variable, GlueElement Element);

    public sealed class Fixture
    {
        public GlueProjectSave Project { get; } = new();
        public List<FileCase> Files { get; } = new();
        public List<VariableCase> Variables { get; } = new();
    }

    public static Fixture Create()
    {
        var fixture = new Fixture();
        var project = fixture.Project;

        var screen = new ScreenSave { Name = "Screens\\GameScreen" };
        var enemy = new EntitySave { Name = "Entities\\Enemy" };
        var boss = new EntitySave { Name = "Entities\\Boss", BaseEntity = "Entities\\Enemy" };
        project.Screens.Add(screen);
        project.Entities.Add(enemy);
        project.Entities.Add(boss);

        enemy.NamedObjects.Add(new NamedObjectSave
        {
            InstanceName = "SpriteInstance",
            SourceType = SourceType.FlatRedBallType,
            SourceClassType = "FlatRedBall.Sprite",
        });

        ReferencedFileSave AddFile(string key, List<ReferencedFileSave> files, ReferencedFileSave rfs)
        {
            files.Add(rfs);
            fixture.Files.Add(new FileCase(key, rfs));
            return rfs;
        }

        AddFile("entity png", enemy.ReferencedFiles, new ReferencedFileSave { Name = "Entities/Enemy/Texture.png" });
        AddFile("screen png", screen.ReferencedFiles, new ReferencedFileSave { Name = "Screens/GameScreen/Background.png" });
        AddFile("screen png not loaded at runtime", screen.ReferencedFiles,
            new ReferencedFileSave { Name = "Screens/GameScreen/Unused.png", LoadedAtRuntime = false });
        AddFile("global csv", project.GlobalFiles, new ReferencedFileSave { Name = "GlobalContent/EnemyInfo.csv", CreatesDictionary = true });
        AddFile("entity csv", enemy.ReferencedFiles, new ReferencedFileSave { Name = "Entities/Enemy/Waves.csv" });
        AddFile("global txt as csv", project.GlobalFiles, new ReferencedFileSave { Name = "GlobalContent/Notes.txt", TreatAsCsv = true });
        AddFile("global txt", project.GlobalFiles, new ReferencedFileSave { Name = "GlobalContent/Readme.txt" });
        AddFile("screen achx", screen.ReferencedFiles, new ReferencedFileSave { Name = "Screens/GameScreen/Animations.achx" });
        AddFile("entity png from wildcard", enemy.ReferencedFiles,
            new ReferencedFileSave { Name = "Entities/Enemy/Wildcard.png", IsCreatedByWildcard = true });

        void AddVariable(string key, GlueElement element, CustomVariable variable)
        {
            element.CustomVariables.Add(variable);
            fixture.Variables.Add(new VariableCase(key, variable, element));
        }

        AddVariable("new float", enemy, new CustomVariable { Name = "Speed", Type = "float", DefaultValue = 3f, SetByDerived = true });
        AddVariable("tunneling", enemy, new CustomVariable
        {
            Name = "SpriteX", Type = "float", SourceObject = "SpriteInstance", SourceObjectProperty = "X",
        });
        AddVariable("type converter", enemy, new CustomVariable
        {
            Name = "Score", Type = "int", OverridingPropertyType = "string", TypeConverter = "Comma Separating",
        });
        AddVariable("defined by base", boss, new CustomVariable { Name = "Speed", Type = "float", DefinedByBase = true });
        AddVariable("screen string", screen, new CustomVariable { Name = "LevelName", Type = "string", DefaultValue = "Level1" });

        return fixture;
    }
}
