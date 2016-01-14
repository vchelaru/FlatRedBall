namespace FlatRedBallScripting.Test
{
    public class TestScript
    {
        public void Test()
        {
            var scriptEngine = new TestScriptEngine();
            IIfTestScriptEngine IF = scriptEngine;
            IDoTestScriptEngine DO = scriptEngine;

            //Basic
            
            IF.Test1();
            DO.Begin();
            DO.DoTestAction();
            DO.End();

            //Advanced
            IF.Test1();
            DO.Begin();

            IF.OrGroup();
            DO.DoTestAction();
            IF.EndGroup();

            IF.AndGroup();
            IF.Test1();
            IF.Test1();
            IF.EndGroup();

            DO.Begin();
            DO.DoTestAction();
            DO.End();

            DO.End();
        }
    }
}
