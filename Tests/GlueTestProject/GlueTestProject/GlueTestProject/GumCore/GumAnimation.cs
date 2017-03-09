using FlatRedBall;
using FlatRedBall.Instructions;
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

        Func<IEnumerable<Instruction>> getInstructionsFunc;

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

        public GumAnimation(float length, Func<IEnumerable<Instruction>> getInstructionsFunc)
        {
            this.getInstructionsFunc = getInstructionsFunc;
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

        public void Play(object whatStartedPlayingThis = null)
        {
            WhatStartedPlayingThis = whatStartedPlayingThis;

            foreach (var namedEvent in namedEvents)
            {
                if (namedActions.ContainsKey(namedEvent.Name))
                {
                    var action = namedActions[namedEvent.Name];

                    var instruction = new DelegateInstruction(action);
                    instruction.TimeToExecute = TimeManager.CurrentTime + namedEvent.Time;
                    instruction.Target = this;
                    InstructionManager.Instructions.Add(instruction);
                }
            }

            foreach (var instruction in getInstructionsFunc())
            {
                instruction.Target = this;
                InstructionManager.Instructions.Add(instruction);
            }

            {
                Action endReachedAction = () => EndReached?.Invoke();
                var endInstruction = new DelegateInstruction(endReachedAction);
                endInstruction.TimeToExecute = TimeManager.CurrentTime + this.Length;
                endInstruction.Target = this;
                InstructionManager.Instructions.Add(endInstruction);
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
