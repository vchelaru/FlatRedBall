using FlatRedBall.Glue.CodeGeneration;
using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.Parsing;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace PlatformerPluginCore.CodeGenerators
{
    public class EntityPlatformerAnimationCodeGenerator : ElementComponentCodeGenerator
    {
        // July 17, 2022
        // Vic says - Unlike other data which is embedded in teh glue files, this sits in its
        // own file. This means that we don't have a cached place for it. We could...but that would
        // be more coding overhead and possible bugs. This should be really fast, since most games will
        // not have very many of these, and these deserialize fast.
        // We'll review performance if in the future some game uses a ton of these files and codegen slows
        // down too much, but I think that's unlikely.
        SaveClasses.AllPlatformerAnimationValues GetAnimationValuesFor(IElement element)
        {
            EntitySave asEntitySave = element as EntitySave;
            var shouldGenerate = asEntitySave != null &&
                FlatRedBall.PlatformerPlugin.Controllers.MainController.IsPlatformer(asEntitySave);

            FilePath platformerAnimationJson = null;

            if(shouldGenerate)
            {
                platformerAnimationJson = Controllers.AnimationController.PlatformerAnimationsFileLocationFor(asEntitySave);

                shouldGenerate = platformerAnimationJson.Exists();
            }

            if(shouldGenerate)
            {
                var fileContents = System.IO.File.ReadAllText(platformerAnimationJson.FullPath);
                var deserialized = JsonConvert.DeserializeObject<SaveClasses.AllPlatformerAnimationValues>(fileContents);

                return deserialized;
            }
            return null;
        }

        string PlatformerAnimationControllerClassName =>
            $"{GlueState.Self.ProjectNamespace}.Entities.PlatformerAnimationController";

        public override ICodeBlock GenerateFields(ICodeBlock codeBlock, IElement element)
        {
            var animationValues = GetAnimationValuesFor(element);

            if(animationValues != null)
            {
                codeBlock.Line($"{PlatformerAnimationControllerClassName} PlatformerAnimationController;");
            }
            return codeBlock;
        }

        
        public override ICodeBlock GenerateInitialize(ICodeBlock codeBlock, IElement element)
        {

            var animationValues = GetAnimationValuesFor(element);

            if(animationValues != null)
            {
                codeBlock.Line($"PlatformerAnimationController = new {PlatformerAnimationControllerClassName}(this);");
                // todo - get the sprite instance name:
                codeBlock.Line("PlatformerAnimationController.AnimatedObject = SpriteInstance;");

                foreach (var entry in animationValues.Values)
                {

                    string animationSpeedAssignment = $"{GlueState.Self.ProjectNamespace}.Entities.AnimationSpeedAssignment.{entry.AnimationSpeedAssignment}";

                    codeBlock.Line("PlatformerAnimationController.AddLayer(new PlatformerAnimationConfiguration");
                    codeBlock.Line("{");
                    var variableAssignmentBlock = codeBlock.CodeBlockIndented();
                    variableAssignmentBlock.Line($"AnimationName={CodeParser.ConvertValueToCodeString(entry.AnimationName)},");
                    variableAssignmentBlock.Line($"HasLeftAndRight={CodeParser.ConvertValueToCodeString(entry.HasLeftAndRight)},");
                    variableAssignmentBlock.Line($"MinXVelocityAbsolute={CodeParser.ConvertValueToCodeString(entry.MinXVelocityAbsolute)},");
                    variableAssignmentBlock.Line($"MaxXVelocityAbsolute={CodeParser.ConvertValueToCodeString(entry.MaxXVelocityAbsolute)} ,");
                    variableAssignmentBlock.Line($"MinYVelocity={CodeParser.ConvertValueToCodeString(entry.MinYVelocity)} ,");
                    variableAssignmentBlock.Line($"MaxYVelocity={CodeParser.ConvertValueToCodeString(entry.MaxYVelocity)} ,");
                    variableAssignmentBlock.Line($"AbsoluteXVelocityAnimationSpeedMultiplier={CodeParser.ConvertValueToCodeString(entry.AbsoluteXVelocityAnimationSpeedMultiplier)} ,");
                    variableAssignmentBlock.Line($"AbsoluteYVelocityAnimationSpeedMultiplier={CodeParser.ConvertValueToCodeString(entry.AbsoluteYVelocityAnimationSpeedMultiplier)} ,");
                    variableAssignmentBlock.Line($"OnGroundRequirement={CodeParser.ConvertValueToCodeString(entry.OnGroundRequirement)} ,");
                    variableAssignmentBlock.Line($"MovementName={CodeParser.ConvertValueToCodeString(entry.MovementName)} ,");
                    variableAssignmentBlock.Line($"AnimationSpeedAssignment={animationSpeedAssignment}");
                    codeBlock.Line("});");
                }
            }

            return codeBlock;
        }

        public override ICodeBlock GenerateActivity(ICodeBlock codeBlock, IElement element)
        {
            var animationValues = GetAnimationValuesFor(element);

            if(animationValues != null)
            {
                codeBlock.Line("PlatformerAnimationController.Activity();");
            }

            return codeBlock;
        }
    }
}
