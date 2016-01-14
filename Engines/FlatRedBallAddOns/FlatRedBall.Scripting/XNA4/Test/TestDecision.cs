using FlatRedBall.Scripting;

namespace FlatRedBallScripting.Test
{
    public class TestDecision : IScriptDecision
    {
        public bool ConditionsAreMet()
        {
            return true;
        }
    }
}
