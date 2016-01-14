using FlatRedBall.Glue.CodeGeneration;
using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.SaveClasses;
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
