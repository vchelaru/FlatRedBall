using FlatRedBall.Glue.CodeGeneration;
using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Parsing;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Graphics.Animation;
using FlatRedBall.IO;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TopDownPlugin.Models;

namespace TopDownPlugin.CodeGenerators;

public class EntityTopDownAnimationCodeGenerator : ElementComponentCodeGenerator
{
    // Feb 23, 2024
    // See EntityPlatformerAnimationCodeGenerator
    // for information about caching, performance concerns,
    // and possible future perofrmance-related changes.
    Models.AllTopDownAnimationValues GetAnimationValuesFor(GlueElement element)
    {
        var asEntitySave = element as EntitySave;

        var shouldGenerate = asEntitySave != null &&
            (TopDownPlugin.Controllers.MainController.IsTopDown(asEntitySave) || Controllers.MainController.Self.GetIfInheritsFromTopDown(asEntitySave)) &&
            GlueState.Self.CurrentGlueProject.FileVersion >= (int)GlueProjectSave.GluxVersions.ITopDownEntity;

        FilePath topDownAnimationJson = null;

        if (shouldGenerate)
        {
            topDownAnimationJson = Controllers.AnimationController.TopDownAnimationsFileLocationFor(asEntitySave);

            shouldGenerate = topDownAnimationJson.Exists();
        }

        if (shouldGenerate)
        {
            var fileContents = System.IO.File.ReadAllText(topDownAnimationJson.FullPath);
            var deserialized = JsonConvert.DeserializeObject<AllTopDownAnimationValues>(fileContents);

            return deserialized;
        }
        return null;
    }

    string TopDownAnimationControllerClassName =>
        $"global::{GlueState.Self.ProjectNamespace}.TopDown.TopDownAnimationController";

    public override ICodeBlock GenerateFields(ICodeBlock codeBlock, IElement element)
    {
        var animationValues = GetAnimationValuesFor(element as GlueElement);

        if (animationValues != null)
        {
            var asEntitySave = element as EntitySave;

            var isTopDownThroughInheritance =
                !TopDownPlugin.Controllers.MainController.IsTopDown(asEntitySave) && Controllers.MainController.Self.GetIfInheritsFromTopDown(asEntitySave);

            if (!isTopDownThroughInheritance)
            {
                codeBlock.Line($"protected {TopDownAnimationControllerClassName} TopDownAnimationController;");
            }
        }
        return codeBlock;
    }

    public override ICodeBlock GenerateInitialize(ICodeBlock codeBlock, IElement element)
    {
        return codeBlock;
    }

    public override ICodeBlock GeneratePostInitialize(ICodeBlock codeBlock, IElement element)
    {

        var animationValues = GetAnimationValuesFor(element as GlueElement);

        if (animationValues != null)
        {
            // only do this if it's the base class that directly inherits from it, not if it is top down
            // through inheritance:

            var asEntitySave = element as EntitySave;

            var isTopDownThroughInheritance =
                !TopDownPlugin.Controllers.MainController.IsTopDown(asEntitySave) && Controllers.MainController.Self.GetIfInheritsFromTopDown(asEntitySave);

            // April 9, 2025
            // Until today, there
            // was no support for setting
            // animations on an entity which
            // inherited from a top-down entity.
            // I started working on it but realized
            // that the problem is more complicated than
            // I originally planned. The animation UI should
            // behave the same as top dow movement variables -
            // it should show values from base, and let you overwrite
            // them. This is more work than simply modifying the code generation.
            // This is somewhat low priority because the common pattern is (currently)
            // to define all animations on the base, and then let the derived use the same
            // animation assigning logic, but with a different .achx that looks different. So
            // that's why I'm putting the entire block in a !isTopDownThroughInheritance.

            if (!isTopDownThroughInheritance)
            {
                codeBlock.Line($"TopDownAnimationController = new {TopDownAnimationControllerClassName}(this);");

                // This currently assumes not recursive, so it relies on SetByDerived exposing the sprite
                var firstSprite = element.AllNamedObjects.FirstOrDefault(item => item.GetAssetTypeInfo() == AvailableAssetTypes.CommonAtis.Sprite);

                if (firstSprite != null)
                {
                    codeBlock.Line($"TopDownAnimationController.AnimatedObject = {firstSprite.FieldName};");
                }

                foreach (var entry in animationValues.Values)
                {
                    codeBlock = codeBlock.Block();
                    {
                        string animationSpeedAssignment = $"global::{GlueState.Self.ProjectNamespace}.TopDown.AnimationSpeedAssignment.{entry.AnimationSpeedAssignment}";


                        codeBlock.Line("var configuration = new TopDown.TopDownAnimationConfiguration");
                        codeBlock.Line("{");
                        var variableAssignmentBlock = codeBlock.CodeBlockIndented();
                        variableAssignmentBlock.Line($"AnimationName={CodeParser.ConvertValueToCodeString(entry.AnimationName)},");
                        variableAssignmentBlock.Line($"IsDirectionFacingAppended={CodeParser.ConvertValueToCodeString(entry.IsDirectionFacingAppended)},");
                        variableAssignmentBlock.Line($"MinVelocityAbsolute={CodeParser.ConvertValueToCodeString(entry.MinVelocityAbsolute)},");
                        variableAssignmentBlock.Line($"MaxVelocityAbsolute={CodeParser.ConvertValueToCodeString(entry.MaxVelocityAbsolute)} ,");
                        variableAssignmentBlock.Line($"MinMovementInputAbsolute={CodeParser.ConvertValueToCodeString(entry.MinMovementInputAbsolute)} ,");
                        variableAssignmentBlock.Line($"MaxMovementInputAbsolute={CodeParser.ConvertValueToCodeString(entry.MaxMovementInputAbsolute)} ,");
                        variableAssignmentBlock.Line($"AbsoluteVelocityAnimationSpeedMultiplier={CodeParser.ConvertValueToCodeString(entry.AbsoluteVelocityAnimationSpeedMultiplier)} ,");
                        variableAssignmentBlock.Line($"MaxSpeedRatioMultiplier={CodeParser.ConvertValueToCodeString(entry.MaxSpeedRatioMultiplier)} ,");
                        if (entry.MovementName != "<NULL>")
                        {
                            // If it's "<NULL>" that's an option in the CSV parser. Let's keep using it, and just omit any line if it's null which will just use the default fallback of null for strings
                            variableAssignmentBlock.Line($"MovementName={CodeParser.ConvertValueToCodeString(entry.MovementName)} ,");
                        }

                        variableAssignmentBlock.Line($"AnimationSpeedAssignment={animationSpeedAssignment}");


                        codeBlock.Line("};");

                        codeBlock.Line("TopDownAnimationController.AddLayer(configuration);");

                        if (!string.IsNullOrWhiteSpace(entry.CustomCondition))
                        {
                            codeBlock.Line($"configuration.AdditionalPredicate += () => {entry.CustomCondition};");
                        }
                    }
                    codeBlock = codeBlock.End();
                }
            }
        }

        return codeBlock;
    }

    public override ICodeBlock GenerateActivity(ICodeBlock codeBlock, IElement element)
    {
        var animationValues = GetAnimationValuesFor(element as GlueElement);

        if (animationValues != null)
        {
            var asEntitySave = element as EntitySave;

            var isTopDownThroughInheritance =
                !TopDownPlugin.Controllers.MainController.IsTopDown(asEntitySave) && Controllers.MainController.Self.GetIfInheritsFromTopDown(asEntitySave);
            if (!isTopDownThroughInheritance)
            {
                codeBlock.Line("TopDownAnimationController.Activity();");
            }
        }

        return codeBlock;
    }

}
