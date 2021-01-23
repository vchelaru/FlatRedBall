using FlatRedBall;
using FlatRedBall.Instructions;
using StateInterpolationPlugin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlatRedBall.Gum.Animation
{
    public class GumAnimation
    {
        #region Internal Classes

        class NamedEvent
        {
            public string Name { get; set; }
            public float Time { get; set; }
        }

        #endregion

        float animationSpeed = 1;
        /// <summary>
        /// The speed multiplier used to play the animation. A value greater than 1 will make the animation
        /// play faster than normal. For example, a value of 2 will make the animation play two times as fast.
        /// This must be greater than 0
        /// </summary>
        public float AnimationSpeed
        {
            get => animationSpeed;
            set
            {
                if(value != animationSpeed)
                {
                    var oldSpeed = animationSpeed;
                    animationSpeed = value;

                    if(IsPlaying())
                    {
                        var multiplier = oldSpeed / value;

                        float elapsedAdvance = 0;

                        foreach(var tweener in TweenerManager.Self.Tweeners)
                        {
                            if(tweener.Owner == this)
                            {
                                // If we increase the duration, (like from 2->4) then we  need to
                                // also increase our elapsed, or else it will look like we "rewind" the animation
                                var elapsedRatio = tweener.elapsed / tweener.Duration;
                                tweener.Duration *= multiplier;
                                tweener.elapsed = elapsedRatio * tweener.Duration;
                            }
                        }

                        foreach (var instruction in InstructionManager.Instructions)
                        {
                            if (instruction.Target == this)
                            {
                                var timeLeft = instruction.TimeToExecute - TimeManager.CurrentTime;
                                instruction.TimeToExecute = TimeManager.CurrentTime + timeLeft * multiplier;
                            }
                        }
                        InstructionManager.Instructions.InsertionSortAscendingTimeToExecute();

                    }
                }
            }
        }

        public float PlayingTimeLeft
        {
            get
            {
                Instruction lastInstruction = null;
                foreach(var instruction in InstructionManager.Instructions)
                {
                    if(instruction.Target == this)
                    {
                        lastInstruction = instruction;
                    }
                }

                if(lastInstruction == null)
                {
                    return 0;
                }
                else
                {
                    return (float)(lastInstruction.TimeToExecute - TimeManager.CurrentTime);
                }
            }
        }

        public List<GumAnimation> SubAnimations
        {
            get;
            private set;
        }

        /// <summary>
        /// List of named events, used when the animation is played.
        /// </summary>
        /// <remarks>
        /// This is not a Dictionary becuase we want to allow the same event name
        /// to appear multiple times. For example, a user might create a "Bounce" event
        /// where a sound effect plays. An animation may bounce multiple times, so the same
        /// event might be added in multiple spots.
        /// </remarks>
        List<NamedEvent> namedEvents = new List<NamedEvent>();

        Dictionary<string, Action> namedActions = new Dictionary<string, Action>();

        public event Action EndReached;

        Func<object, IEnumerable<Instruction>> getInstructionsFunc;
        Func<float, object, IEnumerable<Instruction>> getTimedInstructionsFunc;

        public object WhatStartedPlayingThis
        {
            get;
            private set;
        }

        public float Length
        {
            get;
            private set;
        }

        public GumAnimation(float length, Func<object, IEnumerable<Instruction>> getInstructionsFunc)
        {
            this.getInstructionsFunc = getInstructionsFunc;
            this.Length = length;
            SubAnimations = new List<GumAnimation>();
        }

        public GumAnimation(float length, Func<float, object, IEnumerable<Instruction>> getTimedInstructionsFunc)
        {
            this.getTimedInstructionsFunc = getTimedInstructionsFunc;
            this.Length = length;
            SubAnimations = new List<GumAnimation>();
        }

        public void Stop()
        {
            InstructionManager.Instructions.RemoveAll(item => item.Target == this);
            StateInterpolationPlugin.TweenerManager.Self.StopAllTweenersOwnedBy(this);

            foreach (var anim in SubAnimations)
            {
                anim.StopIfStartedBy(this);
            }
        }

        public void StopIfStartedBy(object objectThatMayHaveStartedPlaying)
        {
            if (WhatStartedPlayingThis == objectThatMayHaveStartedPlaying)
            {
                Stop();
            }
        }

        public void AddEvent(string name, float time)
        {
            namedEvents.Add(new NamedEvent { Name = name, Time = time });
        }

        public void AddAction(string name, Action action)
        {
#if DEBUG
            if(namedEvents.Any(item=>item.Name == name) == false)
            {
                throw new ArgumentException("Could not find any registered events with the name " + name);
            }
#endif
            namedActions.Add(name, action);
        }

        public bool TryAddAction(string name, Action action)
        {
            var hasNamedEvent = namedEvents.Any(item => item.Name == name);

            if(hasNamedEvent)
            {
                namedActions.Add(name, action);
            }
            return hasNamedEvent;
        }


        public void SetInitialState()
        {
            global::FlatRedBall.Instructions.Instruction lastInstruction = null;

            if(getInstructionsFunc != null)
            {
                foreach(var instruction in getInstructionsFunc(this))
                {
                    if(lastInstruction == null || lastInstruction.TimeToExecute == instruction.TimeToExecute)
                    {
                        instruction.ExecuteOn(this);
                        lastInstruction = instruction;
                    }
                }
            }
            else if(getTimedInstructionsFunc != null)
            {
                foreach(var instruction in getTimedInstructionsFunc(AnimationSpeed, this))
                {
                    if (lastInstruction == null || lastInstruction.TimeToExecute == instruction.TimeToExecute)
                    {
                        instruction.ExecuteOn(this);
                        lastInstruction = instruction;
                    }
                }
            }
        }

        public void Play(object whatStartedPlayingThis = null)
        {
            /////////////Early Out//////////////
            if(IsPlaying())
            {
                return;
            }
            //////////End Early Out/////////////
            WhatStartedPlayingThis = whatStartedPlayingThis;

            foreach (var namedEvent in namedEvents)
            {
                if (namedActions.ContainsKey(namedEvent.Name))
                {
                    var action = namedActions[namedEvent.Name];


                    if(namedEvent.Time == 0)
                    {
                        action.Invoke();
                    }
                    else
                    {
                        var instruction = new DelegateInstruction(action);
                        instruction.Target = this;
                        instruction.TimeToExecute = TimeManager.CurrentTime + namedEvent.Time/AnimationSpeed;
                        InstructionManager.Instructions.Add(instruction);
                    }
                }
            }

            if(getTimedInstructionsFunc != null)
            {
                foreach (var instruction in getTimedInstructionsFunc(AnimationSpeed, this))
                {
                    if (instruction.TimeToExecute == TimeManager.CurrentTime)
                    {
                        instruction.ExecuteOn(this);
                    }
                    else
                    {
                        InstructionManager.Instructions.Add(instruction);
                    }
                }
            }
            else if(getInstructionsFunc != null)
            {
                foreach (var instruction in getInstructionsFunc(this))
                {
                    if(instruction.TimeToExecute == TimeManager.CurrentTime)
                    {
                        instruction.ExecuteOn(this);
                    }
                    else
                    {
                        InstructionManager.Instructions.Add(instruction);
                    }
                }
            }

            {
                if(this.Length == 0)
                {
                    EndReached?.Invoke();
                }
                else
                {
                    Action endReachedAction = () => EndReached?.Invoke();
                    var endInstruction = new DelegateInstruction(endReachedAction);
                    endInstruction.TimeToExecute = TimeManager.CurrentTime + this.Length / AnimationSpeed;
                    endInstruction.Target = this;
                    InstructionManager.Instructions.Add(endInstruction);
                }
            }

        }

        public bool IsPlaying()
        {
            return InstructionManager.Instructions.Any(item => item.Target == this);
        }

        public void PlayAfter(float delay, object whatStartedPlayigThis = null)
        {
            DelegateInstruction instruction = new DelegateInstruction(() => Play(whatStartedPlayigThis));
            instruction.TimeToExecute = TimeManager.CurrentTime + delay;
            InstructionManager.Instructions.Add(instruction);
        }
    }
}
