using System;
namespace FlatRedBall.Scripting
{
    public class AfterThatDecision : IScriptDecision 
    {


        public void AddDecision(IScriptDecision decision)
        {
            throw new System.NotImplementedException();
        }

        public Script ScriptToFollow
        {
            get;
            set;
        }

        public IDecisionList Parent
        {
            get
            {
                throw new System.NotImplementedException();
            }
            set
            {
                throw new System.NotImplementedException();
            }
        }

        public bool Contains(IScriptDecision decision)
        {
            return false;
        }

        public bool ConditionsAreMet()
        {
            if (ScriptToFollow == null)
            {
                throw new Exception("ScriptToFollow needs to be set first");
            }
            else
            {
                bool toReturn = true;
                foreach (var action in ScriptToFollow.Actions)
                {
                    if (!action.IsComplete())
                    {
                        toReturn = false;
                        break;
                    }
                }
                return toReturn;
            }
        }
    }
}
