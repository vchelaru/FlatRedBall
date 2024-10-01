using FlatRedBall.Glue.CodeGeneration.CodeBuilder;

namespace FlatRedBall.Glue.CodeGeneration
{
    public class ErrorCheckingCodeGenerator : ElementComponentCodeGenerator
    {
        public override ICodeBlock GenerateFields(ICodeBlock codeBlock, SaveClasses.GlueElement element)
        {
            codeBlock.Line("#if DEBUG");
            codeBlock.Line("public static bool HasBeenLoadedWithGlobalContentManager { get; private set; }= false;");
            codeBlock.Line("#endif");

            return codeBlock;
        }

        public override ICodeBlock GenerateInitialize(ICodeBlock codeBlock, SaveClasses.GlueElement element)
        {
            return codeBlock;

        }

        public override ICodeBlock GenerateAddToManagers(ICodeBlock codeBlock, SaveClasses.GlueElement element)
        {
            return codeBlock;

        }

        public override ICodeBlock GenerateActivity(ICodeBlock codeBlock, SaveClasses.GlueElement element)
        {
            return codeBlock;
        }

        public override ICodeBlock GenerateAdditionalMethods(ICodeBlock codeBlock, SaveClasses.GlueElement element)
        {
            return codeBlock;
        }

        public override ICodeBlock GenerateLoadStaticContent(ICodeBlock codeBlock, SaveClasses.GlueElement element)
        {
            #region For debugging record if we're using a global content manager

            codeBlock.Line("#if DEBUG");
            
            codeBlock.If("contentManagerName == FlatRedBall.FlatRedBallServices.GlobalContentManager")
                .Line("HasBeenLoadedWithGlobalContentManager = true;");
            
            codeBlock.ElseIf("HasBeenLoadedWithGlobalContentManager")
                .Line("throw new System.Exception( \"" + element.ClassName + " has been loaded with a Global content manager, then loaded with a non-global.  This can lead to a lot of bugs\");");

            codeBlock.Line("#endif");
            
            #endregion

            return codeBlock;
        }
    }
}
