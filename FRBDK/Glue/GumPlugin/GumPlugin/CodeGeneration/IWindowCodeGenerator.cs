using FlatRedBall.Glue.CodeGeneration;
using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Gui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GumPlugin.CodeGeneration
{
    class IWindowCodeGenerator : FlatRedBall.Glue.CodeGeneration.ElementComponentCodeGenerator
    {
        public string GetWrapperNameFor(NamedObjectSave namedObject)
        {
            return "m" + namedObject.InstanceName + "IWindow";
        }

        public override ICodeBlock GenerateFields(ICodeBlock codeBlock, FlatRedBall.Glue.SaveClasses.IElement element)
        {
            //foreach (var namedObject in element.AllNamedObjects.Where(item => IsGue(item) && 
            //    NamedObjectSaveCodeGenerator.GetFieldCodeGenerationType(item) == CodeGenerationType.Full))
            //{
            //    codeBlock.Line("FlatRedBall.Gum.GueIWindowWrapper " + GetWrapperNameFor(namedObject) + ";");
            //}

            // Feb 21, 2024
            // Some codegen is handled by IWindowTemplate.txt
            // That's a yucky system because it doesn't (easily) 
            // allow for conditional generation, and it breaks from
            // how normal codegen is done. So I'll add these props here.
            // IWindow is out of favor anyway due to Gum so...this may not
            // be touched much in the future:
            var isNewEnough = GlueState.Self.IsReferencingFrbSource;
            if(element is EntitySave entitySave && entitySave.ImplementsIWindow && isNewEnough)
            {
                codeBlock.Line("public void CallRemovedAsPushedWindow() => RemovedAsPushedWindow?.Invoke(this);");
                codeBlock.Line("public event WindowEvent RemovedAsPushedWindow;");
    }

            return codeBlock;
        }



        public override ICodeBlock GenerateInitialize(ICodeBlock codeBlock, FlatRedBall.Glue.SaveClasses.IElement element)
        {
            //foreach(var namedObject in element.AllNamedObjects.Where(item=> IsGue(item) &&
            //    NamedObjectSaveCodeGenerator.GetInitializeCodeGenerationType(item, element) == CodeGenerationType.Full))
            //{
            //    string name = GetWrapperNameFor(namedObject);
            //    codeBlock.Line( name + " = new FlatRedBall.Gum.GueIWindowWrapper();");
            //    //mNarrowButtonIWindow.GraphicalUiElement = NarrowButton;
            //    codeBlock.Line(name + ".GraphicalUiElement = " + namedObject.InstanceName + ";");

            //}


            return codeBlock;
        }

        public override ICodeBlock GenerateAddToManagers(ICodeBlock codeBlock, FlatRedBall.Glue.SaveClasses.IElement element)
        {
            //foreach (var namedObject in element.AllNamedObjects.Where(item => IsGue(item)))
            //{
            //    //FlatRedBall.Gui.GuiManager.AddWindow(mNarrowButtonIWindow);
            //    codeBlock.Line("FlatRedBall.Gui.GuiManager.AddWindow(" + GetWrapperNameFor(namedObject) + ");");
            //}
            return base.GenerateAddToManagers(codeBlock, element);
        }

        public override void GenerateRemoveFromManagers(ICodeBlock codeBlock, FlatRedBall.Glue.SaveClasses.IElement element)
        {
            base.GenerateRemoveFromManagers(codeBlock, element);
        }

        public override ICodeBlock GenerateDestroy(ICodeBlock codeBlock, FlatRedBall.Glue.SaveClasses.IElement element)
        {
            return base.GenerateDestroy(codeBlock, element);
        }
    }
}
