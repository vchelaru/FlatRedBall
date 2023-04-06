using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.IO;
using Gum.DataTypes;
using GumPlugin.Managers;
using StateAnimationPlugin.SaveClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.SaveClasses;
using GumPluginCore.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;

namespace GumPlugin.CodeGeneration
{
    #region Classes

    public class StateCodeGeneratorContext
    {
        public ElementSave Element { get; set; }
    }

    #endregion


    public partial class StateCodeGenerator
    {
        #region Enums
        enum AbsoluteOrRelative
        {
            Absolute,
            Relative
        }
        #endregion

        private void GenerateAnimationEnumerables(ElementSave elementSave, ICodeBlock currentBlock)
        {
            currentBlock.Line("#region State Animations");

            StateCodeGeneratorContext context = new StateCodeGeneratorContext();
            context.Element = elementSave;

            ElementAnimationsSave animations = AnimationLogic.GetAnimationsFor(elementSave);

            if(animations != null)
            {
                foreach(var animation in animations.Animations)
                {
                    GenerateGetEnumerableFor(context, currentBlock, animation, AbsoluteOrRelative.Absolute);
                    GenerateGetEnumerableFor(context, currentBlock, animation, AbsoluteOrRelative.Relative);

                    GenerateAnimationMember(context, currentBlock, animation, AbsoluteOrRelative.Absolute);
                    GenerateAnimationMember(context, currentBlock, animation, AbsoluteOrRelative.Relative);
                }
            }

            currentBlock.Line("#endregion");
        }



        private void GenerateAnimationMember(StateCodeGeneratorContext context, ICodeBlock currentBlock, AnimationSave animation, AbsoluteOrRelative absoluteOrRelative)
        {
            string propertyName = animation.PropertyNameInCode();
            if (absoluteOrRelative == AbsoluteOrRelative.Relative)
            {
                propertyName += "Relative";
            }
            string referencedInstructionProperty = propertyName + "Instructions";
            // Force the property to be upper-case, since the field is lower-case:



            // We want to generate something like:
            //private FlatRedBall.Gum.Animation.GumAnimation uncategorizedAnimation;
            //public FlatRedBall.Gum.Animation.GumAnimation UncategorizedAnimation
            //{
            //    get
            //    {
            //        if (uncategorizedAnimation == null)
            //        {
            //            uncategorizedAnimation = new FlatRedBall.Gum.Animation.GumAnimation(1, () => UncategorizedAnimationInstructions);
            //            uncategorizedAnimation.AddEvent("Event1", 3.0f);
            //        }
            //        return uncategorizedAnimation;
            //    }
            //}

            var firstCharacterLower = propertyName.Substring(0, 1).ToLowerInvariant();
            var fieldName = firstCharacterLower + propertyName.Substring(1);
            

            currentBlock.Line($"private FlatRedBall.Gum.Animation.GumAnimation {fieldName};");

            currentBlock = currentBlock.Property("public FlatRedBall.Gum.Animation.GumAnimation", propertyName).Get();



            float length = GetAnimationLength(context.Element, animation);

            string lengthAsString = ToFloatString(length);

            var ifBlock = currentBlock.If($"{fieldName} == null");
            {
                ifBlock.Line(
                    $"{fieldName} = new FlatRedBall.Gum.Animation.GumAnimation({lengthAsString}, {referencedInstructionProperty});");

                foreach(var namedEvent in animation.Events)
                {
                    string timeAsString = ToFloatString(namedEvent.Time);
                    ifBlock.Line(
                        $"{fieldName}.AddEvent(\"{namedEvent.Name}\", {timeAsString});");
                }
                foreach(var subAnimation in animation.Animations)
                {
                    var isMissingInstance = false;
                    if(string.IsNullOrEmpty(subAnimation.SourceObject) == false)
                    {
                        isMissingInstance = context.Element.GetInstance(subAnimation.SourceObject) == null;
                    }

                    if(isMissingInstance)
                    {
                        ifBlock.Line($"//Missing object {subAnimation.SourceObject}");
                    }
                    else
                    {
                        ifBlock.Line($"{fieldName}.SubAnimations.Add({subAnimation.PropertyNameInCode()});");
                    }
                }
            }

            currentBlock.Line($"return {fieldName};");
        }

        private float GetAnimationLength(ElementSave element, AnimationSave animation)
        {
            float max = 0;

            if (animation.States.Count != 0)
            {
                max = animation.States.Max(item => item.Time);
            }
            if(animation.Events.Count != 0)
            {
                max = System.Math.Max(
                    max,
                    animation.Events.Max(item => item.Time));
            }
            foreach (var item in animation.Animations)
            {
                AnimationSave subAnimation = null;
                ElementSave subElement = null;

                if(!string.IsNullOrEmpty( item.SourceObject))
                {
                    var instance = element.GetInstance(item.SourceObject);

                    // This may refer to an instance that was deleted at some point:
                    if (instance != null)
                    {
                        subElement = Gum.Managers.ObjectFinder.Self.GetElementSave(instance);
                    }

                    if (subElement != null)
                    {
                        subAnimation = AnimationLogic.GetAnimationsFor(subElement).Animations.FirstOrDefault(candidate => candidate.Name == item.RootName);
                    }
                }
                else
                {
                    subElement = element;
                    subAnimation = AnimationLogic.GetAnimationsFor(element).Animations.FirstOrDefault(candidate => candidate.Name == item.RootName);
                }

                if (subElement != null && subAnimation != null)
                {
                    max = Math.Max(max, item.Time + GetAnimationLength(subElement, subAnimation));
                }
            }

            return max;
        }

        private void GenerateGetEnumerableFor(StateCodeGeneratorContext context, ICodeBlock currentBlock, AnimationSave animation, AbsoluteOrRelative absoluteOrRelative)
        {
            string animationType = "VariableState";

            string animationName = animation.PropertyNameInCode();

            if(absoluteOrRelative == AbsoluteOrRelative.Relative)
            {
                animationName += "Relative";
            }

            string propertyName = animationName + "Instructions";

            // Instructions used to be public - the user would grab them and add them to the InstructionManager,
            // but now everything is encased in an Animation object which handles stopping itself and provides a simple
            // Play method.

            const string signature = "private System.Collections.Generic.IEnumerable<FlatRedBall.Instructions.Instruction>";

            if (animation.States.Count == 0 && animation.Animations.Count == 0)
            {
                currentBlock = currentBlock.Function(signature, propertyName, "object target");

                currentBlock.Line("yield break;");

            }
            else if(absoluteOrRelative == AbsoluteOrRelative.Relative && animation.States.Count < 2 && animation.Animations.Count == 0)
            {

                currentBlock = currentBlock.Function(signature, propertyName, "object target");

                currentBlock.Line("yield break;");
            }
            else
            {
                if (animation.States.Count != 0)
                {
                    var firstState = context.Element.AllStates.FirstOrDefault(item => item.Name == animation.States.First().StateName);

                    var category = context.Element.Categories.FirstOrDefault(item => item.States.Contains(firstState));

                    if (category != null)
                    {
                        animationType = category.Name;
                    }
                }

                currentBlock = currentBlock.Function(signature, propertyName, "object target");

                GenerateOrderedStateAndSubAnimationCode(context, currentBlock, animation, animationType, absoluteOrRelative);

                if(animation.Loops)
                {
                    currentBlock = currentBlock.Block();

                    currentBlock.Line("var toReturn = new FlatRedBall.Instructions.DelegateInstruction(  " + 
                        "() => FlatRedBall.Instructions.InstructionManager.Instructions.AddRange(this." + propertyName + $"(target)));");
                    string executionTime = "0.0f";

                    if(animation.States.Count != 0)
                    {
                        executionTime = ToFloatString( animation.States.Last().Time);
                    }

                    if(HasAnimationSpeed)
                    {
                        currentBlock.Line("toReturn.TimeToExecute = FlatRedBall.TimeManager.CurrentTime + " + executionTime + $"/{animationName}.AnimationSpeed;");
                    }
                    else
                    {
                        currentBlock.Line("toReturn.TimeToExecute = FlatRedBall.TimeManager.CurrentTime + " + executionTime + $";");
                    }
                    currentBlock.Line("toReturn.Target = target;");

                    currentBlock.Line("yield return toReturn;");
                    currentBlock = currentBlock.End();

                }
            }
            
        }

        static bool HasAnimationSpeed => GlueState.Self.CurrentGlueProject?.FileVersion >= (int)GlueProjectSave.GluxVersions.GumGueHasGetAnimation;

        private void GenerateOrderedStateAndSubAnimationCode(StateCodeGeneratorContext context, ICodeBlock currentBlock, AnimationSave animation, string animationType, AbsoluteOrRelative absoluteOrRelative)
        {
            List<AnimatedStateSave> remainingStates = new List<AnimatedStateSave>();
            remainingStates.AddRange(animation.States);

            List<AnimationReferenceSave> remainingSubAnimations = new List<AnimationReferenceSave>();
            remainingSubAnimations.AddRange(animation.Animations);

            double nextStateTime;
            double nextAnimationTime;


            AnimatedStateSave previousState = null;
            AnimatedStateSave currentState = null;

            while (remainingStates.Count > 0 || remainingSubAnimations.Count > 0)
            {
                if (remainingStates.Count > 0)
                {
                    nextStateTime = remainingStates[0].Time;
                }
                else
                {
                    nextStateTime = double.PositiveInfinity;
                }

                if (remainingSubAnimations.Count > 0)
                {
                    nextAnimationTime = remainingSubAnimations[0].Time;
                }
                else
                {
                    nextAnimationTime = double.PositiveInfinity;
                }

                if (nextAnimationTime < nextStateTime)
                {
                    CreateInstructionForSubAnimation(currentBlock, remainingSubAnimations[0], absoluteOrRelative, animation, context);

                    remainingSubAnimations.RemoveAt(0);
                }
                else
                {
                    currentState = remainingStates[0];
                    CreateInstructionForInterpolation(context, currentBlock, animationType, previousState, 
                        currentState, absoluteOrRelative, animation.PropertyNameInCode());
                    previousState = currentState;

                    remainingStates.RemoveAt(0);
                }
            }
        }

        private static void CreateInstructionForSubAnimation(ICodeBlock currentBlock, AnimationReferenceSave animationReferenceSave, AbsoluteOrRelative absoluteOrRelative, AnimationSave parentAnimation, StateCodeGeneratorContext context)
        {
            currentBlock = currentBlock.Block();

            //var instruction = new FlatRedBall.Instructions.DelegateInstruction(() =>
            //FlatRedBall.Instructions.InstructionManager.Instructions.AddRange(ClickableBushInstance.GrowAnimation));
            //instruction.TimeToExecute = FlatRedBall.TimeManager.CurrentTime + asdf;
            //yield return instruction;

            var isReferencingMissingInstance = !string.IsNullOrEmpty(animationReferenceSave.SourceObject) &&
                context.Element.GetInstance(animationReferenceSave.SourceObject) == null;

            ////////////////Early Out///////////////
            if(isReferencingMissingInstance)
            {
                currentBlock.Line($"// This animation references a missing instance named {animationReferenceSave.SourceObject}");
                return;
            }
            /////////////End Early Out/////////////


            string animationName = animationReferenceSave.PropertyNameInCode();
                //animationReferenceSave. FlatRedBall.IO.FileManager.RemovePath(animationReferenceSave.Name) + "Animation";
            if(absoluteOrRelative == AbsoluteOrRelative.Relative)
            {
                animationName += "Relative";
            }

            currentBlock.Line($"var instruction = new FlatRedBall.Instructions.DelegateInstruction(()=>{animationName}.Play({parentAnimation.PropertyNameInCode()}));");
            if (HasAnimationSpeed)
            {
                currentBlock.Line("instruction.TimeToExecute = FlatRedBall.TimeManager.CurrentTime + " + ToFloatString(animationReferenceSave.Time) + $"/{animationName}.AnimationSpeed;");
            }
            else
            {
                currentBlock.Line("instruction.TimeToExecute = FlatRedBall.TimeManager.CurrentTime + " + ToFloatString(animationReferenceSave.Time) + $";");
            }


            currentBlock.Line("yield return instruction;");
            currentBlock = currentBlock.End();
        }

        private void CreateInstructionForInterpolation(StateCodeGeneratorContext context, ICodeBlock currentBlock, string animationType, AnimatedStateSave previousState, AnimatedStateSave currentState, AbsoluteOrRelative absoluteOrRelative, string animationName)
        {

            currentBlock = currentBlock.Block();

            if (absoluteOrRelative == AbsoluteOrRelative.Absolute)
            {

                CreateInstructionForInterpolationAbsolute(context, currentBlock, animationType, previousState, currentState, animationName);
            }
            else
            {
                CreateInstructionForInterpolationRelative(context,currentBlock,  previousState, currentState, animationName);
            }
            currentBlock = currentBlock.End();
        }

        private void CreateInstructionForInterpolationRelative(StateCodeGeneratorContext context, ICodeBlock currentBlock, AnimatedStateSave previousState, AnimatedStateSave currentState, string animationName)
        {
            if(previousState != null)
            {
                currentBlock.Line("var toReturn = new FlatRedBall.Instructions.DelegateInstruction(() =>");
                {
                    currentBlock = currentBlock.Block();

                    // Is the start clone necessary?
                    currentBlock.Line("var relativeStart = ElementSave.AllStates.FirstOrDefault(item => item.Name == \"" + previousState.StateName + "\").Clone();");
                    currentBlock.Line("var relativeEnd = ElementSave.AllStates.FirstOrDefault(item => item.Name == \"" + currentState.StateName + "\").Clone();");
                    currentBlock.Line("Gum.DataTypes.Variables.StateSaveExtensionMethods.SubtractFromThis(relativeEnd, relativeStart);");
                    currentBlock.Line("var difference = relativeEnd;");

                    string categoryName = "VariableState";
                    var category = context.Element.Categories.FirstOrDefault(item => item.States.Any(stateCandidate => stateCandidate.Name == currentState.StateName));

                    string enumValue = currentState.StateName;

                    if(currentState.StateName.Contains('/'))
                    {
                        var split = currentState.StateName.Split('/');

                        category = context.Element.Categories.FirstOrDefault(item => item.Name == split[0]);
                        enumValue = split[1];
                    }

                    enumValue = SaveObjectExtensionMethods.GetStateMemberNameInCode(enumValue);

                    if(category != null)
                    {
                        categoryName = category.Name;
                    }
                    currentBlock.Line("Gum.DataTypes.Variables.StateSave first = GetCurrentValuesOnState(" + categoryName + "." + enumValue + ");");

                    currentBlock.Line("Gum.DataTypes.Variables.StateSave second = first.Clone();");
                    currentBlock.Line("Gum.DataTypes.Variables.StateSaveExtensionMethods.AddIntoThis(second, difference);");


                    string interpolationTime;
                    if(HasAnimationSpeed)
                    {
                        interpolationTime = ToFloatString(currentState.Time - previousState.Time) + $"/{animationName}.AnimationSpeed";
                    }
                    else
                    {
                        interpolationTime = ToFloatString(currentState.Time - previousState.Time) + $"";
                    }

                    string easing = "FlatRedBall.Glue.StateInterpolation.Easing." + previousState.Easing;
                    string interpolationType = "FlatRedBall.Glue.StateInterpolation.InterpolationType." + previousState.InterpolationType;


                    currentBlock.Line(
                        string.Format("FlatRedBall.Glue.StateInterpolation.Tweener tweener = new FlatRedBall.Glue.StateInterpolation.Tweener(from: 0, to: 1, duration: {0}, type: {1}, easing: {2});",
                        interpolationTime,
                        interpolationType,
                        easing));

                    currentBlock.Line("tweener.Owner = this;");

                    currentBlock.Line("tweener.PositionChanged = newPosition => this.InterpolateBetween(first, second, newPosition);");
                    currentBlock.Line("tweener.Start();");
                    currentBlock.Line("StateInterpolationPlugin.TweenerManager.Self.Add(tweener);");


                    currentBlock = currentBlock.End();
                }
                currentBlock.Line(");");
                string previousStateTime = ToFloatString(previousState.Time);


                if (HasAnimationSpeed)
                {
                    currentBlock.Line("toReturn.TimeToExecute = FlatRedBall.TimeManager.CurrentTime + " + previousStateTime + $"/{animationName}.AnimationSpeed;");
                }
                else
                {
                    currentBlock.Line("toReturn.TimeToExecute = FlatRedBall.TimeManager.CurrentTime + " + previousStateTime + $";");
                }
                currentBlock.Line("toReturn.Target = target;");

                currentBlock.Line("yield return toReturn;");
                
            }
        }

        private void CreateInstructionForInterpolationAbsolute(StateCodeGeneratorContext context, ICodeBlock currentBlock, string animationType, 
            AnimatedStateSave previousState, AnimatedStateSave currentState, string animationName)
        {

            if (previousState == null)
            {
                string variableStateName = null;

                variableStateName = "CurrentVariableState";

                if (currentState.StateName.Contains("/"))
                {
                    var split = currentState.StateName.Split('/');
                    animationType = split[0];
                }

                if (animationType != "VariableState")
                {
                    variableStateName = "Current" + animationType + "State";
                }

                // todo:  Change this on categories
                //System.Action action = () => this.CurrentState = fromState;

                string enumValue = currentState.StateName;
                if(enumValue.Contains("/"))
                {
                    enumValue = enumValue.Split('/')[1];
                }

                enumValue = SaveObjectExtensionMethods.GetStateMemberNameInCode(enumValue);

                currentBlock.Line("var toReturn = new FlatRedBall.Instructions.DelegateInstruction( ()=> this." + variableStateName + " = " +
                    animationType + "." + enumValue + ");");
                currentBlock.Line("toReturn.TimeToExecute = FlatRedBall.TimeManager.CurrentTime;");
                currentBlock.Line("toReturn.Target = target;");

            }
            else
            {
                var previousCategory = context.Element.Categories.FirstOrDefault(item => item.States.Any(stateCandiate => stateCandiate.Name == previousState.StateName));
                var currentCategory = context.Element.Categories.FirstOrDefault(item => item.States.Any(stateCandiate => stateCandiate.Name == currentState.StateName));

                // Now that we interpolateTo a single state, we don't
                // need to pass in the StateSave object:
                //bool differentCategories = previousCategory != currentCategory;

                // November 3, 2015
                // Gum uses a "cumulative 
                // state" system, so that each 
                // keyframe in an animation will 
                // tween with variables both before 
                // and after.
                // The code as of the time of the writing 
                // of this comment does InterpolateBetween
                // two states, which does not consider the state
                // of the object and only tweens variables common
                // to the two states. This makes the runtime behave
                // different than Glue, and it also makes the runtime
                // behave in confusing ways as authors don't often think
                // about the individual variables that may be set in a state.
                // We can solve this by instead doing Interpolate to between the
                // current state of the instance and the state that we are interpolating
                // to. Going to accomplish this by getting rid of the "from" state:
                // Update November 22, 2020
                // While it's true that animations can interpolate any state to any state,
                // it's more common to place all animations in a single category. In this case,
                // we can use a from->to interpolation which is much faster at runtime than doing
                // a <current state>->to.

                string fromState = null;
                string toState = null;

                string previousEnumValue = previousState?.StateName;
                if(previousState.StateName.Contains("/") == true)
                {
                    previousCategory = context.Element.Categories.FirstOrDefault(item => item.Name == previousState.StateName.Split('/')[0]);
                    previousEnumValue = previousState.StateName.Split('/')[1];
                }
                string enumValue = currentState.StateName;
                if(currentState.StateName.Contains("/"))
                {
                    currentCategory = context.Element.Categories.FirstOrDefault(item => item.Name == currentState.StateName.Split('/')[0]);
                    enumValue = currentState.StateName.Split('/')[1];
                }


                //if (differentCategories)
                //{
                //fromState = "this.ElementSave.AllStates.FirstOrDefault(item => item.Name == \"" + previousState.StateName + "\")";
                //toState = "this.ElementSave.AllStates.FirstOrDefault(item => item.Name == \"" + currentState.StateName + "\")";
                //}
                //else
                //{
                //fromState = animationType + "." + previousState.StateName;

                enumValue = SaveObjectExtensionMethods.GetStateMemberNameInCode(enumValue);
                previousEnumValue = SaveObjectExtensionMethods.GetStateMemberNameInCode(previousEnumValue);

                if (currentCategory == null)
                {
                    toState = "VariableState." + enumValue;

                    if(previousCategory == currentCategory)
                    {
                        fromState = "VariableState." + previousEnumValue;
                    }
                }
                else
                {
                    toState = currentCategory.Name + "." + enumValue;
                    if (previousCategory == currentCategory)
                    {
                        fromState = currentCategory.Name + "." + previousEnumValue;
                    }
                }
                //}

                string previousStateTime = ToFloatString(previousState.Time);

                string interpolationTime = null;

                if (HasAnimationSpeed)
                {
                    interpolationTime = ToFloatString(currentState.Time - previousState.Time) + $"/{animationName}.AnimationSpeed";
                }
                else
                {
                    interpolationTime = ToFloatString(currentState.Time - previousState.Time);
                }

                string easing = "FlatRedBall.Glue.StateInterpolation.Easing." + previousState.Easing;
                string interpolationType = "FlatRedBall.Glue.StateInterpolation.InterpolationType." + previousState.InterpolationType;

                string line;

                if(previousCategory == currentCategory)
                {
                    line = "var toReturn = new FlatRedBall.Instructions.DelegateInstruction(  () => this.InterpolateTo(" +
                        string.Format($"{fromState}, {toState}, {interpolationTime}, {interpolationType}, {easing}, {animationName}));");
                }
                else
                {
                    line = "var toReturn = new FlatRedBall.Instructions.DelegateInstruction(  () => this.InterpolateTo(" +
                        string.Format("{0}, {1}, {2}, {3}, {4}));", toState, interpolationTime, interpolationType, easing, animationName);
                }

                currentBlock.Line(line);
                currentBlock.Line("toReturn.Target = target;");

                if (HasAnimationSpeed)
                {
                    currentBlock.Line("toReturn.TimeToExecute = FlatRedBall.TimeManager.CurrentTime + " + previousStateTime + $"/{animationName}.AnimationSpeed;");
                }
                else
                {
                    currentBlock.Line("toReturn.TimeToExecute = FlatRedBall.TimeManager.CurrentTime + " + previousStateTime + $";");
                }


            }
            currentBlock.Line("yield return toReturn;");
            //System.Action action = () => this.InterpolateTo(fromState, toState, timeToTake, interpolationType, easing);
        }

        private void GenerateStopAnimations(ElementSave elementSave, ICodeBlock currentBlock)
        {
            bool hasBase = !string.IsNullOrEmpty(elementSave.BaseType);

            if(hasBase)
            {
                var elementBase = Gum.Managers.ObjectFinder.Self.GetElementSave(elementSave.BaseType);

                if(elementBase is StandardElementSave)
                {
                    hasBase = false;
                }
            }


            currentBlock = currentBlock.Function("public override void", "StopAnimations", "");

            currentBlock.Line("base.StopAnimations();");

            foreach (var item in elementSave.Instances)
            {
                var itemBase = Gum.Managers.ObjectFinder.Self.GetElementSave(item);
                // Base objects don't have animations (for now)
                if (itemBase is StandardElementSave == false)
                {
                    currentBlock.Line(item.MemberNameInCode() + ".StopAnimations();");
                }
            }
            ElementAnimationsSave animations = AnimationLogic.GetAnimationsFor(elementSave);
            if (animations != null)
            {
                foreach (var animation in animations.Animations)
                {
                    currentBlock.Line($"{animation.Name}Animation.Stop();");
                }
            }
        }

        private void GenerateGetAnimations(ElementSave elementSave, ICodeBlock currentBlock)
        {
            //////////////Early Out/////////////////////////
            if(GlueState.Self.CurrentGlueProject.FileVersion < (int)GlueProjectSave.GluxVersions.GumGueHasGetAnimation)
            {
                // The method doesn't exist to override
                return;
            }

            currentBlock = currentBlock.Function("public override FlatRedBall.Gum.Animation.GumAnimation", "GetAnimation", "string animationName");


            ElementAnimationsSave animations = AnimationLogic.GetAnimationsFor(elementSave);
            if (animations?.Animations.Count > 0)
            {
                var switchBlock = currentBlock.Switch("animationName");
                foreach (var animation in animations.Animations)
                {
                    var caseBlock = switchBlock.CaseNoBreak($"\"{animation.Name}Animation\"");
                    caseBlock.Line($"return {animation.Name}Animation;");
                }
            }
            currentBlock.Line("return base.GetAnimation(animationName);");
        }

        private static string ToFloatString(float value)
        {
            string toReturn = value.ToString(System.Globalization.CultureInfo.InvariantCulture);

            if(toReturn.Contains('.'))
            {
                toReturn += "f";
            }

            return toReturn;
        }

    }
}
