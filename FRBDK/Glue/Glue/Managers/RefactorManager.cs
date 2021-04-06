using FlatRedBall.Glue.Managers;
using System;
using System.Collections.Generic;
using System.Text;

namespace GlueFormsCore.Managers
{
    public class RefactorManager : Singleton<RefactorManager>
    {
        public void RenameClassInCode(string oldClassName, string newClassName, ref string contents)
        {
            contents = contents.Replace("partial class " + oldClassName,
                "partial class " + newClassName);
        }
    }
}
