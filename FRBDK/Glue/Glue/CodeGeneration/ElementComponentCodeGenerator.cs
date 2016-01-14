using System.Text;
using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.Plugins.Interfaces;
using System.Collections.Generic;

namespace FlatRedBall.Glue.CodeGeneration
{


    public abstract class ElementComponentCodeGenerator
    {
        public virtual CodeLocation CodeLocation
        {
            get
            {
                return Plugins.Interfaces.CodeLocation.StandardGenerated;
            }   
        }

        // We don't generate using statements.  Why?
        // Because multiple plugins may generate using
        // statements for types that would conflict.  Therefore
        // plugins should just use the full type nme for things that
        // aren't by default added in the generate code template.

        public virtual void AddInheritedTypesToList(List<string> listToAddTo, IElement element)
        {

        }
        public virtual ICodeBlock GenerateFields(ICodeBlock codeBlock, IElement element)
        {
            return codeBlock;

        }
        public virtual ICodeBlock GenerateInitialize(ICodeBlock codeBlock, IElement element)
        {
            return codeBlock;

        }
        public virtual ICodeBlock GenerateInitializeLate(ICodeBlock codeBlock, IElement element)
        {
            return codeBlock;
        }
        public virtual ICodeBlock GeneratePostInitialize(ICodeBlock codeBlock, IElement element)
        {
            return codeBlock;
        }
        public virtual ICodeBlock GenerateAddToManagers(ICodeBlock codeBlock, IElement element)
        {
            return codeBlock;

        }
        public virtual ICodeBlock GenerateDestroy(ICodeBlock codeBlock, IElement element)
        {
            return codeBlock;

        }
        public virtual ICodeBlock GenerateActivity(ICodeBlock codeBlock, IElement element)
        {
            return codeBlock;

        }
        public virtual ICodeBlock GenerateAdditionalMethods(ICodeBlock codeBlock, IElement element)
        {
            return codeBlock;

        }
        public virtual ICodeBlock GenerateLoadStaticContent(ICodeBlock codeBlock, IElement element)
        {
            return codeBlock;

        }

        public virtual ICodeBlock GenerateUnloadStaticContent(ICodeBlock codeBlock, IElement element)
        {
            return codeBlock;

        }

        public virtual void GenerateAdditionalClasses(ICodeBlock codeBlock, IElement element)
        {


        }

        public virtual void GenerateRemoveFromManagers(ICodeBlock codeBlock, IElement element)
        {

        }

        public virtual bool HandlesVariable(CustomVariable variable, IElement element)
        {
            return false;
        }

    }
}
