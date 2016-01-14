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

namespace GumPlugin.CodeGeneration
{
    public partial class StateCodeGenerator
    {
        
        enum AbsoluteOrRelative
        {
            Absolute,
            Relative
        }

        ElementSave mElementSaveContext;

        private void GenerateAnimateForCategory(ICodeBlock currentBlock, string categoryName, List<Gum.DataTypes.Variables.StateSave> states)
        {
            string propertyToAssign;
            if (categoryName == "VariableState")
            {
                propertyToAssign = "this.CurrentVariableState";
            }
            else
            {
                propertyToAssign = "this.Current" + categoryName + "State";
            }
            currentBlock = currentBlock.Function("public void", "Animate",
                "System.Collections.Generic.IEnumerable<FlatRedBall.Gum.Keyframe<" + categoryName + ">> keyframes");
            {
                currentBlock.Line("bool isFirst = true;");
                currentBlock.Line("FlatRedBall.Gum.Keyframe<" + categoryName + "> lastKeyframe = null;");

                var foreachBlock = currentBlock.ForEach("var frame in keyframes");
                {
                    var ifBlock = foreachBlock.If("isFirst");
                    {
                        ifBlock.Line("isFirst = false;");
                        ifBlock.Line(propertyToAssign + " = frame.State;");

                    }
                    var elseBlock = ifBlock.End().Else();
                    {
                        elseBlock.Line("float timeToTake = frame.Time - lastKeyframe.Time;");
                        elseBlock.Line("var fromState = lastKeyframe.State;");
                        elseBlock.Line("var toState = frame.State;");
                        elseBlock.Line("var interpolationType = lastKeyframe.InterpolationType;");
                        elseBlock.Line("var easing = lastKeyframe.Easing;");

                        elseBlock.Line(
                            "System.Action action = () => this.InterpolateTo(fromState, toState, timeToTake, interpolationType, easing, {});");

                        elseBlock.Line(
                            "FlatRedBall.Instructions.DelegateInstruction instruction = new FlatRedBall.Instructions.DelegateInstruction(action);");
                        elseBlock.Line("instruction.Target = this;");
                        elseBlock.Line("instruction.TimeToExecute = FlatRedBall.TimeManager.CurrentTime + lastKeyframe.Time;");

                        elseBlock.Line("FlatRedBall.Instructions.InstructionManager.Instructions.Add(instruction);");
                    }

                    foreachBlock.Line("lastKeyframe = frame;");
                }
            }
        }

        private void GenerateAnimationEnumerables(ElementSave elementSave, ICodeBlock currentBlock)
        {
            currentBlock.Line("#region State Animations");


            ElementAnimationsSave animations = GetAnimationsFor(elementSave);

            if(animations != null)
            {
                foreach(var animation in animations.Animations)
                {
                    GenerateEnumerableFor(elementSave, currentBlock, animation, AbsoluteOrRelative.Absolute);
                    GenerateEnumerableFor(elementSave, currentBlock, animation, AbsoluteOrRelative.Relative);

                    GenerateAnimationMember(elementSave, currentBlock, animation, AbsoluteOrRelative.Absolute);
                    GenerateAnimationMember(elementSave, currentBlock, animation, AbsoluteOrRelative.Relative);
                }
            }

            currentBlock.Line("#endregion");
        }

        private static ElementAnimationsSave GetAnimationsFor(ElementSave elementSave)
        {
            string gumFolder = FileManager.GetDirectory(AppState.Self.GumProjectSave.FullFileName);

            string fullAnimationName = null;
            fullAnimationName = gumFolder + elementSave.Subfolder + "/" + elementSave.Name + "Animations.ganx";

            ElementAnimationsSave animations = null;

            if (!string.IsNullOrEmpty(fullAnimationName) && System.IO.File.Exists(fullAnimationName))
            {
                animations = FileManager.XmlDeserialize<ElementAnimationsSave>(fullAnimationName);
            }
            return animations;
        }

        private void GenerateAnimationMember(ElementSave elementSave, ICodeBlock currentBlock, AnimationSave animation, AbsoluteOrRelative absoluteOrRelative)
        {
            string referencedInstructionProperty = animation.Name + "AnimationInstructions"; 
            string propertyName = animation.Name + "Animation";
            if (absoluteOrRelative == AbsoluteOrRelative.Relative)
            {
                propertyName += "Relative";
                referencedInstructionProperty += "Relative";
            }

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



            float length = GetAnimationLength(elementSave, animation);

            string lengthAsString = ToFloatString(length);

            var ifBlock = currentBlock.If($"{fieldName} == null");
            {
                ifBlock.Line(
                    $"{fieldName} = new FlatRedBall.Gum.Animation.GumAnimation({lengthAsString}, () => {referencedInstructionProperty});");

                foreach(var namedEvent in animation.Events)
                {
                    string timeAsString = ToFloatString(namedEvent.Time);
                    ifBlock.Line(
                        $"{fieldName}.AddEvent(\"{namedEvent.Name}\", {timeAsString});");
                }
                foreach(var subAnimation in animation.Animations)
                {
                    if(string.IsNullOrEmpty(subAnimation.SourceObject) == false)
                    {
                        ifBlock.Line($"{fieldName}.SubAnimations.Add({subAnimation.Name}Animation);");
                    }
                }
            }

            currentBlock.Line($"return {fieldName};");
        }

        private float GetAnimationLength(ElementSave elementSave, AnimationSave animation)
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
                    var instance = elementSave.GetInstance(item.SourceObject);

                    // This may refer to an instance that was deleted at some point:
                    if (instance != null)
                    {
                        subElement = Gum.Managers.ObjectFinder.Self.GetElementSave(instance);
                    }

                    if (subElement != null)
                    {
                        subAnimation = GetAnimationsFor(subElement).Animations.FirstOrDefault(candidate => candidate.Name == item.RootName);
                    }
                }
                else
                {
                    subElement = elementSave;
                    subAnimation = GetAnimationsFor(elementSave).Animations.FirstOrDefault(candidate => candidate.Name == item.RootName);
                }

                if (subElement != null && subAnimation != null)
                {
                    max = Math.Max(max, item.Time + GetAnimationLength(subElement, subAnimation));
                }
            }

            return max;
        }

        private void GenerateEnumerableFor(ElementSave elementSave, ICodeBlock currentBlock, AnimationSave animation, AbsoluteOrRelative absoluteOrRelative)
        {
            mElementSaveContext = elementSave;

            string animationType = "VariableState";

            string animationName = animation.Name + "Animation";

            string propertyName = animation.Name + "AnimationInstructions";
            if(absoluteOrRelative == AbsoluteOrRelative.Relative)
            {
                propertyName += "Relative";
                animationName += "Relative";
            }

            // Instructions used to be public - the user would grab them and add them to the InstructionManager,
            // but now everything is encased in an Animation object which handles stopping itself and provides a simple
            // Play method.
            if (animation.States.Count == 0 && animation.Animations.Count == 0)
            {
                currentBlock = currentBlock.Property("private System.Collections.Generic.IEnumerable<FlatRedBall.Instructions.Instruction>", propertyName).Get();

                currentBlock.Line("yield break;");

            }
            if(absoluteOrRelative == AbsoluteOrRelative.Relative && animation.States.Count < 2 && animation.Animations.Count == 0)
            {

                currentBlock = currentBlock.Property("private System.Collections.Generic.IEnumerable<FlatRedBall.Instructions.Instruction>", propertyName).Get();

                currentBlock.Line("yield break;");
            }
            else
            {
                if (animation.States.Count != 0)
                {
                    var firstState = elementSave.AllStates.FirstOrDefault(item => item.Name == animation.States.First().StateName);

                    var category = elementSave.Categories.FirstOrDefault(item => item.States.Contains(firstState));

                    if (category != null)
                    {
                        animationType = category.Name;
                    }
                }

                currentBlock = currentBlock.Property("private System.Collections.Generic.IEnumerable<FlatRedBall.Instructions.Instruction>", propertyName).Get();

                GenerateOrderedStateAndSubAnimationCode(currentBlock, animation, animationType, absoluteOrRelative);

                if(animation.Loops)
                {
                    currentBlock = currentBlock.Block();

                    currentBlock.Line("var toReturn = new FlatRedBall.Instructions.DelegateInstruction(  " + 
                        "() => FlatRedBall.Instructions.InstructionManager.Instructions.AddRange(this." + propertyName + "));");
                    string executionTime = "0.0f";

                    if(animation.States.Count != 0)
                    {
                        executionTime = ToFloatString( animation.States.Last().Time);
                    }

                    currentBlock.Line("toReturn.TimeToExecute = FlatRedBall.TimeManager.CurrentTime + " + executionTime + ";");

                    currentBlock.Line("yield return toReturn;");
                    currentBlock = currentBlock.End();

                }
            }
            
        }

        private void GenerateOrderedStateAndSubAnimationCode(ICodeBlock currentBlock, AnimationSave animation, string animationType, AbsoluteOrRelative absoluteOrRelative)
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
                    CreateInstructionForSubAnimation(currentBlock, remainingSubAnimations[0], absoluteOrRelative, animation);

                    remainingSubAnimations.RemoveAt(0);
                }
                else
                {
                    currentState = remainingStates[0];
                    CreateInstructionForInterpolation(currentBlock, animationType, previousState, currentState, absoluteOrRelative, animation.Name + "Animation");
                    previousState = currentState;

                    remainingStates.RemoveAt(0);
                }
            }
        }

        private static void CreateInstructionForSubAnimation(ICodeBlock currentBlock, AnimationReferenceSave animationReferenceSave, AbsoluteOrRelative absoluteOrRelative, AnimationSave parentAnimation)
        {
            currentBlock = currentBlock.Block();

            //var instruction = new FlatRedBall.Instructions.DelegateInstruction(() =>
                //FlatRedBall.Instructions.InstructionManager.Instructions.AddRange(ClickableBushInstance.GrowAnimation));
            //instruction.TimeToExecute = FlatRedBall.TimeManager.CurrentTime + asdf;
            //yield return instruction;

            string animationName = FlatRedBall.IO.FileManager.RemovePath(animationReferenceSave.Name) + "Animation";
            if(absoluteOrRelative == AbsoluteOrRelative.Relative)
            {
                animationName += "Relative";
            }

            currentBlock.Line($"var instruction = new FlatRedBall.Instructions.DelegateInstruction(()=>{animationName}.Play({parentAnimation.Name}Animation));");
            currentBlock.Line("instruction.TimeToExecute = FlatRedBall.TimeManager.CurrentTime + " + ToFloatString(animationReferenceSave.Time) + ";");


            currentBlock.Line("yield return instruction;");
            currentBlock = currentBlock.End();
        }

        private void CreateInstructionForInterpolation(ICodeBlock currentBlock, string animationType, AnimatedStateSave previousState, AnimatedStateSave currentState, AbsoluteOrRelative absoluteOrRelative, string animationName)
        {

            currentBlock = currentBlock.Block();

            if (absoluteOrRelative == AbsoluteOrRelative.Absolute)
            {

                CreateInstructionForInterpolationAbsolute(currentBlock, animationType, previousState, currentState, animationName);
            }
            else
            {
                CreateInstructionForInterpolationRelative(currentBlock,  previousState, currentState);
            }
            currentBlock = currentBlock.End();
        }

        private void CreateInstructionForInterpolationRelative(ICodeBlock currentBlock, AnimatedStateSave previousState, AnimatedStateSave currentState)
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
                    var category = mElementSaveContext.Categories.FirstOrDefault(item => item.States.Any(stateCandidate => stateCandidate.Name == currentState.StateName));
                    if(category != null)
                    {
                        categoryName = category.Name;
                    }
                    currentBlock.Line("Gum.DataTypes.Variables.StateSave first = GetCurrentValuesOnState(" + categoryName + "." + currentState.StateName + ");");

                    currentBlock.Line("Gum.DataTypes.Variables.StateSave second = first.Clone();");
                    currentBlock.Line("Gum.DataTypes.Variables.StateSaveExtensionMethods.AddIntoThis(second, difference);");


                    string interpolationTime = ToFloatString(currentState.Time - previousState.Time);

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

                currentBlock.Line("toReturn.TimeToExecute = FlatRedBall.TimeManager.CurrentTime + " + previousStateTime + ";");
                currentBlock.Line("yield return toReturn;");






            }
        }

        private void CreateInstructionForInterpolationAbsolute(ICodeBlock currentBlock, string animationType, AnimatedStateSave previousState, AnimatedStateSave currentState, string animationName)
        {

            if (previousState == null)
            {
                string variableStateName = "CurrentVariableState";

                if (animationType != "VariableState")
                {
                    variableStateName = "Current" + animationType + "State";
                }

                // todo:  Change this on categories
                //System.Action action = () => this.CurrentState = fromState;
                currentBlock.Line("var toReturn = new FlatRedBall.Instructions.DelegateInstruction( ()=> this." + variableStateName + " = " +
                    animationType + "." + currentState.StateName + ");");
                currentBlock.Line("toReturn.TimeToExecute = FlatRedBall.TimeManager.CurrentTime;");

            }
            else
            {
                var previousCategory = mElementSaveContext.Categories.FirstOrDefault(item => item.States.Any(stateCandiate => stateCandiate.Name == previousState.StateName));
                var currentCategory = mElementSaveContext.Categories.FirstOrDefault(item => item.States.Any(stateCandiate => stateCandiate.Name == currentState.StateName));

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
                //string fromState = null;

                string toState = null;

                //if (differentCategories)
                //{
                //fromState = "this.ElementSave.AllStates.FirstOrDefault(item => item.Name == \"" + previousState.StateName + "\")";
                //toState = "this.ElementSave.AllStates.FirstOrDefault(item => item.Name == \"" + currentState.StateName + "\")";
                //}
                //else
                //{
                //fromState = animationType + "." + previousState.StateName;
                if (currentCategory == null)
                {
                    toState = "VariableState." + currentState.StateName;

                }
                else
                {
                    toState = currentCategory.Name + "." + currentState.StateName;
                }
                //}

                string previousStateTime = ToFloatString(previousState.Time);

                string interpolationTime = ToFloatString(currentState.Time - previousState.Time);

                string easing = "FlatRedBall.Glue.StateInterpolation.Easing." + previousState.Easing;
                string interpolationType = "FlatRedBall.Glue.StateInterpolation.InterpolationType." + previousState.InterpolationType;

                currentBlock.Line("var toReturn = new FlatRedBall.Instructions.DelegateInstruction(  () => this.InterpolateTo(" +
                    string.Format("{0}, {1}, {2}, {3}, {4}));", toState, interpolationTime, interpolationType, easing, animationName));
                currentBlock.Line("toReturn.TimeToExecute = FlatRedBall.TimeManager.CurrentTime + " + previousStateTime + ";");

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
            ElementAnimationsSave animations = GetAnimationsFor(elementSave);
            if (animations != null)
            {
                foreach (var animation in animations.Animations)
                {
                    currentBlock.Line($"{animation.Name}Animation.Stop();");
                }
            }
        }

        private static string ToFloatString(float value)
        {
            string toReturn = value.ToString();

            if(toReturn.Contains('.'))
            {
                toReturn += "f";
            }

            return toReturn;
        }

    }
}
