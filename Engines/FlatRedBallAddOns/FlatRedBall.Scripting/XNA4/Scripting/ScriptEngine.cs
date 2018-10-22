using System;
using System.Collections.Generic;

#if DEBUG
using System.Diagnostics;
#endif

namespace FlatRedBall.Scripting
{
    public class ScriptEngine : IIfScriptEngine, IDoScriptEngine
    {
        #region Fields

        private bool _inActionRegion = false;
        private Modes _mode;
        private bool _inDo;

        public List<Script> Scripts = new List<Script>();

        protected Script CurrentScript;
        protected IDecisionList CurrentDecision;

        protected Dictionary<string, object> mScriptVariables = new Dictionary<string,object>();

        #endregion

        #region Properties


        public ExecutionModes ExecutionMode { get; set; }

        public bool IsAnyScriptLeft
        {
            get { return Scripts.Count != 0; }
        }


        #endregion

        #region Enums

        public enum Modes
        {
            Simple,
            Expanded
        }

        public enum ExecutionModes
        {
            Linear,
            Normal
        }

        #endregion

        public ScriptEngine()
        {
            _inDo = false;
            _mode = Modes.Simple;
            ExecutionMode = ExecutionModes.Normal;
            CurrentScript = new Script();
            CurrentDecision = new DecisionOrList();
        }

        public ScriptEngine(Modes mode) : this()
        {
            _mode = mode;
        }

        public void Activity()
        {
            //Need to end do section when in simple mode
            EndActiveIf();

            switch (ExecutionMode)
            {
                case ExecutionModes.Linear:

                    for (int i = 0; i < Scripts.Count; i++)
                    {
                        if (!Scripts[i].Execute())
                        {
                            break;
                        }

                        Scripts.RemoveAt(i);
                        i--;
                    }

                    break;
                case ExecutionModes.Normal:

                    for (int i = 0; i < Scripts.Count; i++)
                    {
                        // Removal may occur here
                        Script script = Scripts[i];

                        if (script.Execute())
                        {
                            // This may have removed scripts
                            // so let's get the index again:
                            int index = Scripts.IndexOf(script);
                            Scripts.RemoveAt(index);
                            i = index - 1;
                        }
                    }

                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        public void EndActiveIf()
        {
            if (_mode == Modes.Simple && _inDo)
            {
                ((IDoScriptEngine)this).End();
                _inDo = false;
            }
            else
            {
                // We didn't have any Dos set to this decision, but the decision
                // could have had If's. If so, we want to clear them out so that they
                // don't stick around and "or" with the next decisions unexpectedly
                CurrentDecision = new DecisionOrList();
            }
        }

        public virtual void Initialize()
        {

        }

        protected void RemoveScriptByDecision(IScriptDecision decision)
        {
            for (int i = 0; i < Scripts.Count; i++)
            {
                Script script = Scripts[i];

                if (script.ContainsDecision(decision))
                {
                    Scripts.RemoveAt(i);
                    break;
                }
            }
        }

        #region IIfScriptEngine

        public IScriptDecision AddDecision(IScriptDecision decision)
        {
            //Need to end do section when in simple mode
            EndActiveIf();
            
            if (_inActionRegion)
            {
                var oldScript = CurrentScript;

                CurrentScript = new Script {Parent = oldScript};
                oldScript.Actions.Add(CurrentScript);
            }

            CurrentDecision.AddDecision(decision);
            return decision;
        }

        void IIfScriptEngine.AndGroup()
        {
            var oldDecision = CurrentDecision;

            CurrentDecision = new DecisionAndList { Parent = oldDecision };

            oldDecision.AddDecision(CurrentDecision);
        }

        void IIfScriptEngine.OrGroup()
        {
            var oldDecision = CurrentDecision;

            CurrentDecision = new DecisionOrList { Parent = oldDecision };

            oldDecision.AddDecision(CurrentDecision);
        }

        void IIfScriptEngine.EndGroup()
        {
            if (CurrentDecision.Parent != null)
                CurrentDecision = CurrentDecision.Parent;
        }

        void IIfScriptEngine.True()
        {
            CreateGeneralDecision(() => true);
        }

        public GeneralDecision CreateGeneralDecision(Func<bool> func)
        {
            return CreateGeneralDecision(func, null);

        }
        public GeneralDecision CreateGeneralDecision(Func<bool> func, string displayText)
        {
            GeneralDecision generalDecision = new GeneralDecision();

            generalDecision.ConditionFunc = func;

#if DEBUG && (PC || WINDOWS)
            if (string.IsNullOrEmpty(displayText))
            {
                StackTrace stackTrace = new StackTrace();
                StackFrame[] stackFrames = stackTrace.GetFrames();


                generalDecision.Name = stackFrames[2].GetMethod().Name;
            }
            else
            {
                generalDecision.Name = displayText;
            }
#endif

            AddDecision(generalDecision);
            return generalDecision;
        }

        public void IsScriptVariableEqualTo(string name, object value)
        {
            Func<bool> func = () =>
            {
                return mScriptVariables.ContainsKey(name) && mScriptVariables[name] == value;
            };

            CreateGeneralDecision(func);
        }

        public void AfterThat()
        {
            var actions = CurrentScript.Actions;

            AfterThatDecision decision = new AfterThatDecision();
            decision.ScriptToFollow = CurrentScript;

            AddDecision(decision);
        }

        #endregion

        #region IDoScriptEngine


        public GeneralAction CreateGeneralAction(Action action)
        {
            return CreateGeneralAction(action, null);

        }

        public GeneralAction CreateGeneralAction(Action action, string name)
        {
            GeneralAction generalAction = new GeneralAction();

            generalAction.Name = name;
           
            
#if DEBUG && (WINDOWS || PC)
            if (string.IsNullOrEmpty(name))
            {
                StackTrace stackTrace = new StackTrace();
                StackFrame[] stackFrames = stackTrace.GetFrames();

                string nameFromStackTrace = stackFrames[1].GetMethod().Name;
                if(nameFromStackTrace == stackFrames[0].GetMethod().Name)
                {
                    nameFromStackTrace = stackFrames[2].GetMethod().Name;
                }
                generalAction.Name = nameFromStackTrace;
            }
#endif
            
            generalAction.ActionToPerform = action;
            AddAction(generalAction);
            return generalAction;
        }

        public void AddAction(IScriptAction action)
        {
            //Need to begin do section when in simple mode
            if (_mode == Modes.Simple && !_inDo)
            {
                ((IDoScriptEngine)this).Begin();
                _inDo = true;
            }

            if(_inActionRegion || _mode == Modes.Simple)
                CurrentScript.Actions.Add(action);
            else 
                throw new Exception("Can not add action here.");
        }

        void IDoScriptEngine.Begin()
        {
            while (CurrentDecision.Parent != null)
                CurrentDecision = CurrentDecision.Parent;

            CurrentScript.Decision = CurrentDecision;

            CurrentDecision = new DecisionOrList();
        }

        public void SetScriptVariable(string name, object value)
        {
            Action action = () =>
            {
                mScriptVariables[name] = value;
            };

            CreateGeneralAction(action);
        }

        protected void SetCurrentScript(Script script)
        {
            if (!Scripts.Contains(CurrentScript))
            {
                Scripts.Add(CurrentScript);
            }
            _inActionRegion = false;

            CurrentScript = script;

        }

        void IDoScriptEngine.End()
        {
            if (!_inActionRegion && _mode != Modes.Simple)
                throw new Exception("Can not call end here.");

            if (CurrentScript.Parent != null)
            {
                CurrentScript = CurrentScript.Parent;
            }
            else
            {
                if (!Scripts.Contains(CurrentScript))
                {
                    Scripts.Add(CurrentScript);
                }
                CurrentScript = new Script();
                _inActionRegion = false;
            }
        }

        #endregion
    }
}
