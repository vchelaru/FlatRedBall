using System;
using FlatRedBall.Glue.CodeGeneration;
using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Parsing;
using FlatRedBall.Glue.SaveClasses;
using GlueUnitTests.Tasks;
using GlueUnitTests.TestSupport;
using Shouldly;

namespace GlueUnitTests.CodeGeneration;

/// <summary>
/// An entity whose base is an FRB type (for example after marking a Circle IsContainer and choosing to set
/// the container's type) is itself the instance, so ATI callbacks that generate code for it receive no
/// NamedObjectSave. GitHub issue #2482.
/// </summary>
// Replaces ObjectFinder.Self.GlueProject, so it runs in the sequential collection.
[Collection(nameof(TaskManagerSequentialCollection))]
public class EntityInheritingFrbTypeCodeGenerationTests : IDisposable
{
    private readonly GlueProjectSave _originalGlueProject;

    public EntityInheritingFrbTypeCodeGenerationTests()
    {
        GlueTestBootstrap.EnsureInitialized();
        GlueTestBootstrap.EnsureCirclePluginRegisteredWithPluginManager();
        GlueTestBootstrap.EnsureAxisAlignedRectanglePluginRegisteredWithPluginManager();
        GlueTestBootstrap.EnsureCameraControllingEntityPluginRegisteredWithPluginManager();
        _originalGlueProject = ObjectFinder.Self.GlueProject;
    }

    public void Dispose()
    {
        ObjectFinder.Self.GlueProject = _originalGlueProject;
    }

    private static EntitySave NewEntityInheriting(string qualifiedType, string containerName, bool? visible = null)
    {
        var project = new GlueProjectSave { FileVersion = GlueProjectSave.LatestVersion };
        ObjectFinder.Self.GlueProject = project;

        var entity = new EntitySave { Name = "Entities\\PlayerBall", BaseEntity = qualifiedType };
        project.Entities.Add(entity);

        if (containerName != null)
        {
            var container = new NamedObjectSave
            {
                InstanceName = containerName,
                SourceType = SourceType.FlatRedBallType,
                SourceClassType = qualifiedType,
                IsContainer = true
            };
            if (visible != null)
            {
                container.SetVariable("Visible", visible.Value);
            }
            entity.NamedObjects.Add(container);
        }
        return entity;
    }

    private static string GenerateAddToManagers(EntitySave entity)
    {
        var document = new CodeDocument();
        CodeWriter.GenerateAddToManagers(entity, document);
        return document.ToString();
    }

    [Theory]
    [InlineData("FlatRedBall.Math.Geometry.Circle", "CircleInstance")]
    [InlineData("FlatRedBall.Math.Geometry.Circle", null)]
    [InlineData("FlatRedBall.Math.Geometry.AxisAlignedRectangle", "AxisAlignedRectangleInstance")]
    [InlineData("FlatRedBall.Math.Geometry.AxisAlignedRectangle", null)]
    public void AddToManagers_ShapeBase_AddsThisToLayer(string qualifiedType, string containerName)
    {
        var entity = NewEntityInheriting(qualifiedType, containerName);

        var code = GenerateAddToManagers(entity);

        code.ShouldContain("this.Visible = true;", Case.Sensitive);
        code.ShouldContain("FlatRedBall.Math.Geometry.ShapeManager.AddToLayer(this, layerToAddTo);", Case.Sensitive);
    }

    [Fact]
    public void AddToManagers_ShapeBaseWithContainerInvisible_DoesNotForceVisible()
    {
        var entity = NewEntityInheriting("FlatRedBall.Math.Geometry.Circle", "CircleInstance", visible: false);

        var code = GenerateAddToManagers(entity);

        code.ShouldNotContain("Visible = true", Case.Sensitive);
        code.ShouldContain("FlatRedBall.Math.Geometry.ShapeManager.AddToLayer(this, layerToAddTo);", Case.Sensitive);
    }

    [Fact]
    public void AddToManagers_CameraControllingEntityBase_AddsThis()
    {
        var entity = NewEntityInheriting("FlatRedBall.Entities.CameraControllingEntity", null);

        var code = GenerateAddToManagers(entity);

        code.ShouldContain("FlatRedBall.SpriteManager.AddPositionedObject(this); this.Activity();", Case.Sensitive);
    }

    [Theory]
    [InlineData("Map", "this.Map = TiledMapInstance;")]
    [InlineData("Targets", "this.Targets.Clear(); this.Targets.Add(TiledMapInstance);")]
    public void AssignCustomVariables_CameraControllingEntityBase_AssignsOnThis(string variableName, string expectedLine)
    {
        var entity = NewEntityInheriting("FlatRedBall.Entities.CameraControllingEntity", null);
        entity.NamedObjects.Add(new NamedObjectSave
        {
            InstanceName = "TiledMapInstance",
            SourceType = SourceType.FlatRedBallType,
            SourceClassType = "FlatRedBall.Sprite"
        });
        entity.CustomVariables.Add(new CustomVariable
        {
            Name = variableName,
            Type = "string",
            DefaultValue = "TiledMapInstance"
        });

        var document = new CodeDocument();
        CodeWriter.GenerateAssignCustomVariables(document, entity);

        document.ToString().ShouldContain(expectedLine, Case.Sensitive);
    }
}
