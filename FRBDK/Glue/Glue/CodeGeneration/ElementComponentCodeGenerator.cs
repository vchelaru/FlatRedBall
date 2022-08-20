using System.Text;
using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.Plugins.Interfaces;
using System.Collections.Generic;
using FlatRedBall.Glue.Events;

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
        public virtual ICodeBlock GenerateConstructor(ICodeBlock codeBlock, IElement element)
        {
            return codeBlock;

        }

        #region Initialize

        public string InitializeCategory { get; set; }
        public string InitializeAfterCategory { get; set; }

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

        #endregion

        #region AddToManagers

        public virtual ICodeBlock GenerateAddToManagers(ICodeBlock codeBlock, IElement element)
        {
            return codeBlock;

        }

        public virtual void GenerateAddToManagersBottomUp(ICodeBlock codeBlock, IElement element)
        {

        }

        #endregion

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

        #region Pause/Unpause

        public virtual void GeneratePauseThisScreen(ICodeBlock codeBlock, IElement element)
        {

        }

        public virtual void GenerateUnpauseThisScreen(ICodeBlock codeBlock, IElement element)
        {

        }

        #endregion

        /// <summary>
        /// Adds code to the contents of the entity's UpdateDependencies call. If no code generators
        /// add code to the UpdateDependencies block then Glue will not generate an UpdateDependencies call.
        /// </summary>
        /// <remarks>
        /// UpdateDependences has historically been implemented in custom code on Glue entities. This is slowly
        /// changing with the introduction of the entity performance plugin which can generate code to
        /// update attachments. But we don't want to break all projects that have custom code UpdateDependencies
        /// </remarks>
        /// <param name="codeBlock">The code block for the UpdateDependencies method</param>
        /// <param name="element">The element that is currently being generated.</param>
        public virtual void GenerateUpdateDependencies(ICodeBlock codeBlock, IElement element)
        {

        }

        public virtual bool HandlesVariable(CustomVariable variable, IElement element)
        {
            return false;
        }

        public virtual void GenerateEvent(ICodeBlock codeBlock, GlueElement element, EventResponseSave ers)
        {

        }

    }
}
