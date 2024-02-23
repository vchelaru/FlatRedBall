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
            TopDownPlugin.Controllers.MainController.IsTopDown(asEntitySave) &&
            GlueState.Self.CurrentGlueProject.FileVersion >= (int)GlueProjectSave.GluxVersions.ITopDownEntity;

        FilePath topDownAnimationJson = null;

        if(shouldGenerate)
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
        $"{GlueState.Self.ProjectNamespace}.TopDown.TopDownAnimationController";

    public override ICodeBlock GenerateFields(ICodeBlock codeBlock, IElement element)
    {
        var animationValues = GetAnimationValuesFor(element as GlueElement);

        if (animationValues != null)
        {
            codeBlock.Line($"{TopDownAnimationControllerClassName} TopDownAnimationController;");
        }
        return codeBlock;
    }

    public override ICodeBlock GenerateInitialize(ICodeBlock codeBlock, IElement element)
    {

        var animationValues = GetAnimationValuesFor(element as GlueElement);

        if (animationValues != null)
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

        return codeBlock;
    }

    public override ICodeBlock GenerateActivity(ICodeBlock codeBlock, IElement element)
    {
        var animationValues = GetAnimationValuesFor(element as GlueElement);

        if (animationValues != null)
        {
            codeBlock.Line("TopDownAnimationController.Activity();");
        }

        return codeBlock;
    }

}
