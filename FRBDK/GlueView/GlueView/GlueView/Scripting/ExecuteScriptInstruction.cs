using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Instructions;
using GlueView.Facades;
using FlatRedBall;

namespace GlueView.Scripting
{
    public class ExecuteScriptInstruction : Instruction
    {
        CodeContext mCodeContext;
        string mScript;

        public override object Target
        {
            get
            {
                return null;
            }
            set { throw new InvalidOperationException(); }
        }

        public ExecuteScriptInstruction(CodeContext context, string script)
        {
            mCodeContext = context;
            mScript = script;
        }

        public override void Execute()
        {
            GlueViewCommands.Self.ScriptingCommands.ApplyScript(mScript, mCodeContext);
        }

        public override void ExecuteOn(object target)
        {
            throw new NotImplementedException();
        }

        // The After function exists here so that this will automatically get called when the "After" script is evalutaed
        // In other words, this makes the script parsing for Call.After much easier
        public void After(double time)
        {
            this.TimeToExecute = TimeManager.CurrentTime + time;
            (mCodeContext.ContainerInstance as PositionedObject).Instructions.Add(this);

        }
    }
}
