using System.Collections.Generic;
using FlatRedBall.Glue.SaveClasses;

namespace GlueUnitTests.PropertyGrid;

/// <summary>
/// A small in-memory project with the objects, screens and entities the element property parity tests show: shapes,
/// an entity instance, a list, a Layer and a file-sourced object, base and derived screens and entities, and an
/// ICollidable entity.
/// </summary>
public static class ElementPropertyFixtures
{
    public sealed record ObjectCase(string Key, NamedObjectSave Object, GlueElement Element);

    public sealed record ElementCase(string Key, GlueElement Element);

    public sealed class Fixture
    {
        public GlueProjectSave Project { get; } = new() { FileVersion = GlueProjectSave.LatestVersion };
        public List<ObjectCase> Objects { get; } = new();
        public List<ElementCase> Elements { get; } = new();
    }

    public static Fixture Create()
    {
        var fixture = new Fixture();
        var project = fixture.Project;

        var gameScreen = new ScreenSave { Name = "Screens\\GameScreen" };
        var level1 = new ScreenSave { Name = "Screens\\Level1", BaseScreen = "Screens\\GameScreen", NextScreen = "Screens\\GameScreen" };
        var enemy = new EntitySave { Name = "Entities\\Enemy" };
        var boss = new EntitySave { Name = "Entities\\Boss", BaseEntity = "Entities\\Enemy" };
        var player = new EntitySave { Name = "Entities\\Player", ImplementsICollidable = true, CreatedByOtherEntities = true };
        project.Screens.Add(gameScreen);
        project.Screens.Add(level1);
        project.Entities.Add(enemy);
        project.Entities.Add(boss);
        project.Entities.Add(player);

        enemy.States.Add(new StateSave { Name = "Angry" });
        enemy.States.Add(new StateSave { Name = "Calm" });

        gameScreen.ReferencedFiles.Add(new ReferencedFileSave { Name = "Screens/GameScreen/Background.png" });

        void AddObject(string key, GlueElement element, NamedObjectSave nos)
        {
            element.NamedObjects.Add(nos);
            fixture.Objects.Add(new ObjectCase(key, nos, element));
        }

        AddObject("screen layer", gameScreen, new NamedObjectSave
        {
            InstanceName = "HudLayer", SourceType = SourceType.FlatRedBallType, SourceClassType = "FlatRedBall.Graphics.Layer",
            IndependentOfCamera = true, Is2D = true,
        });
        AddObject("screen sprite", gameScreen, new NamedObjectSave
        {
            InstanceName = "SpriteInstance", SourceType = SourceType.FlatRedBallType, SourceClassType = "FlatRedBall.Sprite", LayerOn = "HudLayer",
        });
        AddObject("screen entity instance", gameScreen, new NamedObjectSave
        {
            InstanceName = "EnemyInstance", SourceType = SourceType.Entity, SourceClassType = "Entities\\Enemy",
        });
        AddObject("screen list", gameScreen, new NamedObjectSave
        {
            InstanceName = "EnemyList", SourceType = SourceType.FlatRedBallType, SourceClassType = "PositionedObjectList<T>",
            SourceClassGenericType = "Entities\\Enemy",
        });
        AddObject("screen shape collection", gameScreen, new NamedObjectSave
        {
            InstanceName = "Walls", SourceType = SourceType.FlatRedBallType, SourceClassType = "FlatRedBall.Math.Geometry.ShapeCollection",
        });
        AddObject("screen file object", gameScreen, new NamedObjectSave
        {
            InstanceName = "BackgroundTexture", SourceType = SourceType.File, SourceFile = "Screens/GameScreen/Background.png",
            SourceName = "Entire File (Texture2D)",
        });
        AddObject("entity circle", player, new NamedObjectSave
        {
            InstanceName = "CircleInstance", SourceType = SourceType.FlatRedBallType, SourceClassType = "FlatRedBall.Math.Geometry.Circle",
            IncludeInICollidable = true,
        });
        AddObject("entity sprite", enemy, new NamedObjectSave
        {
            InstanceName = "EnemySprite", SourceType = SourceType.FlatRedBallType, SourceClassType = "FlatRedBall.Sprite", ExposedInDerived = true,
        });
        AddObject("derived entity object from base", boss, new NamedObjectSave
        {
            InstanceName = "EnemySprite", SourceType = SourceType.FlatRedBallType, SourceClassType = "FlatRedBall.Sprite",
            DefinedByBase = true, InstantiatedByBase = true,
        });

        fixture.Elements.Add(new ElementCase("screen", gameScreen));
        fixture.Elements.Add(new ElementCase("derived screen", level1));
        fixture.Elements.Add(new ElementCase("entity", enemy));
        fixture.Elements.Add(new ElementCase("derived entity", boss));
        fixture.Elements.Add(new ElementCase("icollidable entity", player));

        return fixture;
    }
}
