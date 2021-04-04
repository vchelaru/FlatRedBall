
using System;

namespace FlatRedBall.Scripting
{
    public interface IScreenScriptIf : FlatRedBall.Scripting.IIfScriptEngine
    {
        void TimeHasPassed(double time);
        void Check(Func<bool> conditionFunc);
        void Check<T1>(Func<T1, bool> conditionFunc, T1 arg1);
        void Check<T1, T2>(Func<T1, T2, bool> conditionFunc, T1 arg1, T2 arg2);
        void Check<T1, T2, T3>(Func<T1, T2, T3, bool> conditionFunc, T1 arg1, T2 arg2, T3 arg3);
        void Check<T1, T2, T3, T4>(Func<T1, T2, T3, T4, bool> conditionFunc, T1 arg1, T2 arg2, T3 arg3, T4 arg4);
        void Check<T1, T2, T3, T4, T5>(Func<T1, T2, T3, T4, T5, bool> conditionFunc, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5);
        void Check<T1, T2, T3, T4, T5, T6>(Func<T1, T2, T3, T4, T5, T6, bool> conditionFunc, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6);
        void Check<T1, T2, T3, T4, T5, T6, T7>(Func<T1, T2, T3, T4, T5, T6, T7, bool> conditionFunc, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7);

    }

    public interface IScreenScriptDo : FlatRedBall.Scripting.IDoScriptEngine
    {
        GeneralAction Call(Action action);
        GeneralAction Call<T>(Action<T> action, T arg1);
        GeneralAction Call<T1, T2>(Action<T1, T2> action, T1 arg1, T2 arg2);
        GeneralAction Call<T1, T2, T3>(Action<T1, T2, T3> action, T1 arg1, T2 arg2, T3 arg3);
        GeneralAction Call<T1, T2, T3, T4>(Action<T1, T2, T3, T4> action, 
            T1 arg1, T2 arg2, T3 arg3, T4 arg4);
        GeneralAction Call<T1, T2, T3, T4, T5>(Action<T1, T2, T3, T4, T5> action,
            T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5);
        GeneralAction Call<T1, T2, T3, T4, T5, T6>(Action<T1, T2, T3, T4, T5, T6> action,
            T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6);
        GeneralAction Call<T1, T2, T3, T4, T5, T6, T7>(Action<T1, T2, T3, T4, T5, T6, T7> action,
            T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7);
        void Wait(double secondsToWait);
    }


    public class ScreenScript<T> : FlatRedBall.Scripting.ScriptEngine, IScreenScriptIf, IScreenScriptDo
        where T : FlatRedBall.Screens.Screen
    {
        protected T Screen
        {
            get;
            private set;
        }

        public ScreenScript(T screen)
        {
            this.Screen = screen;
        }

        #region If Implementations

        public void True()
        {
            CreateGeneralDecision(() => true);
        }

        public void TimeHasPassed(double time)
        {
            CreateGeneralDecision(() => Screen.PauseAdjustedCurrentTime > time);
        }

        public void Check(Func<bool> conditionFunc)
        {
            CreateGeneralDecision(conditionFunc);
        }


        public void Check<T1>(Func<T1, bool> conditionFunc, T1 arg1)
        {
            CreateGeneralDecision(() => conditionFunc(arg1));
        }

        public void Check<T1, T2>(Func<T1, T2, bool> conditionFunc, T1 arg1, T2 arg2)
        {
            CreateGeneralDecision(() => conditionFunc(arg1, arg2));
        }

        public void Check<T1, T2, T3>(Func<T1, T2, T3, bool> conditionFunc, T1 arg1, T2 arg2, T3 arg3)
        {
            CreateGeneralDecision(() => conditionFunc(arg1, arg2, arg3));
        }

        public void Check<T1, T2, T3, T4>(Func<T1, T2, T3, T4, bool> conditionFunc, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            CreateGeneralDecision(() => conditionFunc(arg1, arg2, arg3, arg4));
        }

        public void Check<T1, T2, T3, T4, T5>(Func<T1, T2, T3, T4, T5, bool> conditionFunc, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
        {
            CreateGeneralDecision(() => conditionFunc(arg1, arg2, arg3, arg4, arg5));
        }

        public void Check<T1, T2, T3, T4, T5, T6>(Func<T1, T2, T3, T4, T5, T6, bool> conditionFunc, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
        {
            CreateGeneralDecision(() => conditionFunc(arg1, arg2, arg3, arg4, arg5, arg6));
        }

        public void Check<T1, T2, T3, T4, T5, T6, T7>(Func<T1, T2, T3, T4, T5, T6, T7, bool> conditionFunc, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
        {
            CreateGeneralDecision(() => conditionFunc(arg1, arg2, arg3, arg4, arg5, arg6, arg7));
        }


        #endregion

        #region Do Implementations

        public GeneralAction Call(Action action)
        {
            return CreateGeneralAction(action);
        }

        public GeneralAction Call<T1>(Action<T1> action, T1 arg1)
        {
            return CreateGeneralAction(() => action(arg1));
        }

        public GeneralAction Call<T1, T2>(Action<T1, T2> action, T1 arg1, T2 arg2)
        {
            return CreateGeneralAction(() => action(arg1, arg2));
        }

        public GeneralAction Call<T1, T2, T3>(Action<T1, T2, T3> action, T1 arg1, T2 arg2, T3 arg3)
        {
            return CreateGeneralAction(() => action(arg1, arg2, arg3));
        }

        public GeneralAction Call<T1, T2, T3, T4>(Action<T1, T2, T3, T4> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            return CreateGeneralAction(() => action(arg1, arg2, arg3, arg4));
        }

        public GeneralAction Call<T1, T2, T3, T4, T5>(Action<T1, T2, T3, T4, T5> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
        {
            return CreateGeneralAction(() => action(arg1, arg2, arg3, arg4, arg5));
        }

        public GeneralAction Call<T1, T2, T3, T4, T5, T6>(Action<T1, T2, T3, T4, T5, T6> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
        {
            return CreateGeneralAction(() => action(arg1, arg2, arg3, arg4, arg5, arg6));
        }

        public GeneralAction Call<T1, T2, T3, T4, T5, T6, T7>(Action<T1, T2, T3, T4, T5, T6, T7> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
        {
            return CreateGeneralAction(() => action(arg1, arg2, arg3, arg4, arg5, arg6, arg7));
        }

        public void Wait(double secondsToWait)
        {
            var action = new WaitAction();
            action.TimeToWaitInSeconds = secondsToWait;
            AddAction(action);
        }



        #endregion
    }
}
