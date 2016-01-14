using FlatRedBall.Scripting;

namespace FlatRedBallScripting.Test
{
    public class TestScriptEngine : ScriptEngine, IIfTestScriptEngine, IDoTestScriptEngine
    {
        public void Test1()
        {
            AddDecision(new TestDecision());
        }

        public void DoTestAction()
        {
            AddAction(new TestAction());
        }
    }
}
