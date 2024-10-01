using FlatRedBall.Glue.CodeGeneration;
using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using System;
using System.Collections.Generic;
using System.Text;
using TopDownPlugin.Logic;

namespace EntityInputMovementPlugin.CodeGenerators
{
    class EntityCodeGenerator : ElementComponentCodeGenerator
    {
        public override void AddInheritedTypesToList(List<string> listToAddTo, GlueElement element)
        {
            base.AddInheritedTypesToList(listToAddTo, element);

            var entity = element as EntitySave;
            var isPlatformer = FlatRedBall.PlatformerPlugin.Generators.EntityCodeGenerator.GetIfIsPlatformer(element);

            if(entity != null && isPlatformer && GlueState.Self.CurrentGlueProject.FileVersion >= (int)GlueProjectSave.GluxVersions.IPlatformer)
            {
                listToAddTo.Add("IPlatformer");
            }
        }

        public override ICodeBlock GenerateAdditionalMethods(ICodeBlock codeBlock, GlueElement element)
        {
            var entity = element as EntitySave;
            /////////////Early Out////////////////
            if(entity == null)
            {
                return codeBlock;
            }
            ////////////End Early Out/////////////

            var isPlatformer = FlatRedBall.PlatformerPlugin.Generators.EntityCodeGenerator.GetIfIsPlatformer(element);
            var inheritsFromPlatformer = FlatRedBall.PlatformerPlugin.Generators.EntityCodeGenerator.GetIfInheritsFromPlatformer(element);
            var isTopDown = TopDownEntityPropertyLogic.GetIfIsTopDown(element);
            var inheritsFromTopDown = TopDownPlugin.Controllers.MainController.Self.GetIfInheritsFromTopDown(entity);

            string GetInitializeCall(string movementTypeSpecificCall)
            {
                var inputDevice = (ViewModels.InputDevice) entity.Properties.GetValue<int>(nameof(ViewModels.MainViewModel.InputDevice));

                if(inputDevice == ViewModels.InputDevice.GamepadWithKeyboardFallback)
                {
                    return @$"
                if (FlatRedBall.Input.InputManager.Xbox360GamePads[0].IsConnected)
                {{
                    {movementTypeSpecificCall}(FlatRedBall.Input.InputManager.Xbox360GamePads[0]);
                }}
                else
                {{
                    {movementTypeSpecificCall}(FlatRedBall.Input.InputManager.Keyboard);
                }}
    ";
                }
                else if(inputDevice == ViewModels.InputDevice.ZeroInputDevice)
                {
                    return $"{movementTypeSpecificCall}(new FlatRedBall.Input.ZeroInputDevice());";
                }
                else
                {
                    return null;
                }
            }

            if (isPlatformer && !inheritsFromPlatformer)
            {
                codeBlock.Line(
@"
        /// <summary>
        /// Sets the HorizontalInput and JumpInput instances to either the keyboard or 
        /// Xbox360GamePad index 0. This can be overridden by base classes to default
        /// to different input devices.
        /// </summary>
        protected virtual void InitializeInput()
        {");
                codeBlock.Line(GetInitializeCall("InitializePlatformerInput"));
                codeBlock.Line(@"
        }
");
            }
            else if(isTopDown && !inheritsFromTopDown)
            {
                codeBlock.Line(
@"
        /// <summary>
        /// Sets the MovementInput to either the keyboard or 
        /// Xbox360GamePad index 0. This can be overridden by base classes to default
        /// to different input devices.
        /// </summary>
        protected virtual void InitializeInput()
        {");
                codeBlock.Line(GetInitializeCall("InitializeTopDownInput"));
                codeBlock.Line("InputEnabled = true;");
                codeBlock.Line(@"

        }");
            }

            return codeBlock;

        }
    }
}
