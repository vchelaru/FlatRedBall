using System.Collections.Generic;
using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.Elements;
using FlatRedBall.IO;
using FlatRedBall.Glue.Controls;
using System;
using FlatRedBall.Glue.Plugins;

namespace FlatRedBall.Glue.CodeGeneration
{
    public class IVisibleCodeGenerator : ElementComponentCodeGenerator
    {

        static List<NamedObjectSave> mPreservedVisibleNoses = new List<NamedObjectSave>();
        public static Dictionary<string, string> ReusableEntireFileRfses
        {
            get;
            set;
        }

        public override void AddInheritedTypesToList(List<string> listToAddTo, GlueElement element)
        {
            if (element is EntitySave && ((EntitySave)element).ImplementsIVisible)
            {
                listToAddTo.Add("FlatRedBall.Graphics.IVisible");
            }
        }


        public override ICodeBlock GenerateFields(ICodeBlock codeBlock, GlueElement element)
        {
            EntitySave entitySave = element as EntitySave;

            if (entitySave != null && entitySave.ImplementsIVisible)
            {
                #region Comments on the history of events
                // CustomVariable visibleCustomVariable = entitySave.GetCustomVariable("Visible");
                // September 22, 2011 (Victor Chelaru)
                // We used to only create events if the
                // user set them in Glue.  We're changing
                // the approach for events now to always have
                // them available in code, but then have them be
                // exposed in Glue.  This enables us to centralize
                // event code generation instead of spreading it out
                // through every module that may create events.
                //bool createsVisibleEvent =
                //    visibleCustomVariable != null && visibleCustomVariable.CreatesEvent;

                //if (createsVisibleEvent)
                //{
                #endregion
                bool isNew = entitySave.GetInheritsFromIVisible();

                EventCodeGenerator.GenerateEventsForVariable(codeBlock, "Visible", "bool", isNew);

                GenerateVisibleProperty(codeBlock, element, entitySave);

                GenerateIgnoresParentVisibility(codeBlock, entitySave);

                GenerateAbsoluteVisible(codeBlock, entitySave);

                GenerateIVisibleParent(codeBlock, entitySave);
            }

            return codeBlock;
        }

        private void GenerateIVisibleParent(ICodeBlock codeBlock, EntitySave entitySave)
        {
            if (!entitySave.GetInheritsFromIVisible())
            {
                var prop = codeBlock.Property("FlatRedBall.Graphics.IVisible.Parent", Override: false, Type: "FlatRedBall.Graphics.IVisible");
                var get = prop.Get();
                get.If("this.Parent != null && this.Parent is FlatRedBall.Graphics.IVisible")
                    .Line("return this.Parent as FlatRedBall.Graphics.IVisible;").End()
                    .Else()
                    .Line("return null;");
            }
        }

        private void GenerateAbsoluteVisible(ICodeBlock codeBlock, EntitySave entitySave)
        {
            if (!entitySave.GetInheritsFromIVisible())
            {
                var prop = codeBlock.Property("AbsoluteVisible", Public: true, Override: false, Type: "bool");
                prop.Get().Line("return Visible && (Parent == null || IgnoresParentVisibility || Parent is FlatRedBall.Graphics.IVisible == false || (Parent as FlatRedBall.Graphics.IVisible).AbsoluteVisible);");
            }

        }

        private void GenerateIgnoresParentVisibility(ICodeBlock codeBlock, EntitySave entitySave)
        {
            if (!entitySave.GetInheritsFromIVisible())
            {
                codeBlock.AutoProperty("public bool", "IgnoresParentVisibility");
            }
        }

        private static void GenerateVisibleProperty(ICodeBlock codeBlock, GlueElement element, EntitySave entitySave)
        {
            bool inheritsFromIVisible = entitySave.GetInheritsFromIVisible();

            #region Get whether we use virtual or override

            if (!inheritsFromIVisible)
            {
                codeBlock.Line("protected bool mVisible = true;");
            }

            #endregion

            var prop = codeBlock.Property("Visible", Public: true, Override: entitySave.GetInheritsFromIVisible(), Virtual: !entitySave.GetInheritsFromIVisible(), Type: "bool");

            if (inheritsFromIVisible)
            {
                prop.Get()
                    .Line("return base.Visible;");
            }
            else
            {
                prop.Get()
                    .Line("return mVisible;");
            }
            var set = prop.Set();

            #region History on the before set code

            // See comment above about why we no longer
            // check to see if it creates an event.
            //if (createsVisibleEvent)
            //{
            // Keep hasChanged around just in case we want to make a Changed event.  It won't be used by the Set event
            // Update November 27, 2011
            // This is just polluting code.  Let's remove it for now
            //set.Line("bool hasChanged = value != mVisible;");
            #endregion

            EventCodeGenerator.GenerateEventRaisingCode(set, BeforeOrAfter.Before, "Visible", entitySave);

            if (entitySave.GetInheritsFromIVisible())
            {
                set.Line("base.Visible = value;");
            }
            else
            {
                set.Line("mVisible = value;");
            }

            // May 6, 2012
            // We used to manually
            // set the Visible of all
            // children, but now we no
            // longer do that because there
            // is a concept of relative visibility.
            // WriteVisibleSetForNamedObjectSaves(set, entitySave);
            
            EventCodeGenerator.GenerateEventRaisingCode(set, BeforeOrAfter.After, "Visible", entitySave);
        }

        private static NamedObjectSave GetEntireFileNos(EntitySave entitySave, string sourceFileName)
        {
            foreach (NamedObjectSave nos in entitySave.NamedObjects)
            {
                if (nos.IsEntireFile && nos.SourceType == SourceType.File &&
                    nos.SourceFile == sourceFileName)
                {
                    return nos;
                }
            }
            return null;
        }


        public override void GenerateAdditionalClasses(ICodeBlock codeBlock, GlueElement element)
        {
            var currentBlock = codeBlock;

            string nameWithoutPath = FileManager.RemovePath(element.Name);


            if (element is EntitySave && (element as EntitySave).ImplementsIVisible)
            {
                currentBlock = currentBlock
                    .Class("public static", nameWithoutPath + "ExtensionMethods", "");
                currentBlock
                    .Function("public static void", "SetVisible", "this FlatRedBall.Math.PositionedObjectList<" + nameWithoutPath + "> list, bool value")
                        .Line("int count = list.Count;")
                            .For("int i = 0; i < count; i++")
                                .Line("list[i].Visible = value;")
                            .End()
                        .End()
                    .End();
            }
        }



    }
}
