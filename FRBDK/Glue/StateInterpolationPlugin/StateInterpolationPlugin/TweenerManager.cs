using FlatRedBall;
using FlatRedBall.Glue.StateInterpolation;
using FlatRedBall.Instructions;
using FlatRedBall.Instructions.Reflection;
using FlatRedBall.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StateInterpolationPlugin
{
    public class TweenerManager : IManager
    {
        List<Tweener> mTweeners;

        static TweenerManager mSelf;


        public static TweenerManager Self
        {
            get
            {
                if (mSelf == null)
                {
                    mSelf = new TweenerManager();
                }
                return mSelf;
            }
        }

        public TweenerManager()
        {
            mTweeners = new List<Tweener>();

            FlatRedBallServices.AddManager(this);
        }

        public void Add(Tweener tweener)
        {
            mTweeners.Add(tweener);
            // We could use a function in the
            // tweener to prevent allocating a function.
            tweener.Ended += () => mTweeners.Remove(tweener);
        }

        public void StopAllTweenersOwnedBy(object owner)
        {
            for (int i = mTweeners.Count - 1; i > -1; i--)
            {
                if (mTweeners[i].Owner == owner)
                {
                    mTweeners[i].Stop();
                    mTweeners.RemoveAt(i);
                }
            }
        }

        public void Update()
        {
            // Forward-loop it so that if there are two tweeners modifying the same object, the one added last will have priority.
            for (int i = 0; i < mTweeners.Count; i++)
            {
                var tweener = mTweeners[i];

                // The user can manually stop tweeners if they don't
                // want them to go anymore.  If so, let's kill them here:
                if (tweener.Running == false)
                {
                    mTweeners.RemoveAt(i);
                    i--;
                }
                else
                {
                    tweener.Update(TimeManager.SecondDifference);

                    if (i >= mTweeners.Count || mTweeners[i] != tweener)
                    {
                        i--;
                    }
                }
            }
        }

        public void UpdateDependencies()
        {

        }

        public bool IsObjectReferencedByTweeners(object obj)
        {
            foreach (var tweener in mTweeners)
            {
                if (tweener.Owner == obj)
                {
                    return true;
                }
            }
            return false;
        }
    }

    public static class PositionedObjectTweenerExtensionMethods
    {
        public static TweenerHolder Tween(this PositionedObject positionedObject, string memberToSet)
        {
            TweenerHolder toReturn = new TweenerHolder();
            toReturn.Caller = positionedObject;
            toReturn.Tween(memberToSet);
            return toReturn;
        }

        public static TweenerHolder Tween(this PositionedObject positionedObject, string property, float to,
            float during, InterpolationType interpolation, Easing easing)
        {
            TweenerHolder toReturn = new TweenerHolder();
            toReturn.Caller = positionedObject;
            toReturn.Tween(property, to, during, interpolation, easing);
            return toReturn;
        }
    }

    public struct TweenerHolder : ITweenerTween, ITweenerTo, ITweenerDuring, ITweenerUsing
    {
        internal object Caller;
        internal string MemberToSet;
        internal float ValueToSet;
        internal double TimeToTake;

        public Tweener Tween(string member, float value, float time, InterpolationType interpolation, Easing easing)
        {
            MemberToSet = member;

            object currentValueAsObject =
                LateBinder.GetValueStatic(Caller, member);

            if (currentValueAsObject is float)
            {
                float currentValue = (float)currentValueAsObject;
                Tweener tweener = new Tweener(currentValue, value, time,
                    interpolation, easing);

                tweener.PositionChanged = HandlePositionSet;

                tweener.Owner = Caller;

                TweenerManager.Self.Add(tweener);
                tweener.Start();
                return tweener;
            }
            else
            {
                throw new NotImplementedException();
            }
        }
        
        [Obsolete("Use Tween(string, float, float, InterpolationType, Easing) instead.")]
        public ITweenerTo Tween(string member)
        {
            this.MemberToSet = member;
            return this;
        }

        [Obsolete("Use Tween(string, float, float, InterpolationType, Easing) instead.")]
        public ITweenerDuring To(float value)
        {
            this.ValueToSet = value;
            return this;
        }

        [Obsolete("Use Tween(string, float, float, InterpolationType, Easing) instead.")]
        public ITweenerUsing During(double time)
        {
            TimeToTake = time;
            return this;
        }

        [Obsolete("Use Tween(string, float, float, InterpolationType, Easing) instead.")]
        public Tweener Using(InterpolationType interpolation, Easing easing)
        {
            object currentValueAsObject =
                LateBinder.GetValueStatic(Caller, MemberToSet);

            if (currentValueAsObject is float)
            {
                float currentValue = (float)currentValueAsObject;
                Tweener tweener = new Tweener(currentValue, ValueToSet, (float)TimeToTake,
                    interpolation, easing);

                tweener.PositionChanged = HandlePositionSet;

                tweener.Owner = Caller;

                TweenerManager.Self.Add(tweener);
                tweener.Start();
                return tweener;
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        private void HandlePositionSet(float newPosition)
        {
            LateBinder.SetValueStatic(Caller, MemberToSet, newPosition);
        }
    }

    public interface ITweenerTween
    {
        ITweenerTo Tween(string member);
    }

    public interface ITweenerTo
    {
        ITweenerDuring To(float value);
    }

    public interface ITweenerDuring
    {
        ITweenerUsing During(double time);
    }

    public interface ITweenerUsing
    {
        Tweener Using(InterpolationType interpolation, Easing easing);
    }


}
