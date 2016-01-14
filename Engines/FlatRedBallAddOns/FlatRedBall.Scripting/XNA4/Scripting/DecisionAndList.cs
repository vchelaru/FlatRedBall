using System;
using System.Collections.Generic;

namespace FlatRedBall.Scripting
{
    public class DecisionAndList : IDecisionList, IDisposable
    {
        private readonly List<IScriptDecision> _decisions = new List<IScriptDecision>();

        public void AddDecision(IScriptDecision decision)
        {
            _decisions.Add(decision);
        }

        public IDecisionList Parent { get; set; }

        public bool ConditionsAreMet()
        {
            for (var i = 0; i < _decisions.Count; i++)
            {
                if (!_decisions[i].ConditionsAreMet())
                {
                    return false;
                }
            }

            return true;
        }

        public bool Contains(IScriptDecision decision)
        {
            return _decisions.Contains(decision);
        }
        public override string ToString()
        {
            if (_decisions.Count > 1)
            {
                string toReturn = "";
                for (int i = 0; i < _decisions.Count; i++)
                {
                    toReturn += _decisions[i].ToString();

                    if (i != _decisions.Count - 1)
                    {
                        toReturn += " --AND-- ";
                    }
                }
                return toReturn;
            }
            else
            {
                return "And list with " + _decisions.Count + " decisions";
            }
        }

        void IDisposable.Dispose()
        {
            for (int i = 0; i < _decisions.Count; i++)
            {
                if (_decisions[i] is IDisposable)
                {
                    (_decisions[i] as IDisposable).Dispose();
                }
            }
        }
    }
}