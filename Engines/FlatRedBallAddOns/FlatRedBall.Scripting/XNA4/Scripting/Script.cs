

using System.Collections.Generic;
using System;

namespace FlatRedBall.Scripting
{
    public class Script : IScriptAction
    {
        private readonly List<IScriptAction> _actions = new List<IScriptAction>();

        public bool IsComplete()
        {
            foreach (var item in Actions)
            {
                if (!item.IsComplete())
                {
                    return false;
                }
            }
            return true;
        }

        public IScriptDecision Decision { get; set; }

        public List<IScriptAction> Actions
        {
            get { return _actions; }
        }

        public Script Parent { get; set; }

        public bool Execute()
        {
            if (Decision.ConditionsAreMet())
            {
                if (Decision is IDisposable)
                {
                    (Decision as IDisposable).Dispose();
                }

#if DEBUG && PC
                System.Console.WriteLine();
                System.Console.WriteLine(Decision.ToString());
#endif

                for (int i = 0; i < _actions.Count; i++)
                {
                    IScriptAction action = _actions[i];

                    action.Execute();
#if DEBUG && PC
                    System.Console.WriteLine(action.ToString());
#endif

                }


#if DEBUG && PC
                System.Console.WriteLine();
#endif

                return true;
            }

            return false;
        }

        public bool ContainsDecision(IScriptDecision decision)
        {
            if (decision == Decision)
            {
                return true;
            }
            else if (Decision is IDecisionList)
            {
                if (((IDecisionList)Decision).Contains(decision))
                {
                    return true;
                }
            }
            return false;
        }

        public override string ToString()
        {
#if DEBUG && (WINDOWS || PC)

            string decisionString = null;

            IScriptDecision decisionToUse = Decision;

            if (Decision is DecisionOrList)
            {
                bool isFirst = true;
                foreach (var decision in ((DecisionOrList)Decision).Decisions)
                {
                    if (isFirst)
                    {
                        isFirst = false;
                    }
                    else
                    {
                        decisionString += " || ";
                    }
                    decisionString += GetStringForScriptDecision(decision);

                }

            }
            else
            {
                decisionString = GetStringForScriptDecision(decisionToUse);

            }



            return decisionString + " (" + Actions.Count + ")";
#else
            return "Script with " + " (" + Actions.Count + ")";
#endif
        }



#if DEBUG && (WINDOWS || PC)
        private static string GetStringForScriptDecision(IScriptDecision decisionToUse)
        {
            string decisionString = "";

            if (decisionToUse is GeneralDecision)
            {
                decisionString = ((GeneralDecision)decisionToUse).Name;
            }
            // Handled in DecisionAndList.cs
            //else if (decisionToUse is DecisionAndList)
            //{
            //    decisionString = GetDecisionAndToString(decisionToUse);
            //}
            if (string.IsNullOrEmpty(decisionString))
            {
                Type type = decisionToUse.GetType();

                Type toStringDeclaringType = type.GetMethod("ToString").DeclaringType;


                if (toStringDeclaringType != typeof(object) && toStringDeclaringType != typeof(Script))
                {
                    decisionString = decisionToUse.ToString();
                }
                else
                {

                    decisionString = decisionToUse.GetType().Name;
                }
            }
            return decisionString;
        }
#endif

    }
}