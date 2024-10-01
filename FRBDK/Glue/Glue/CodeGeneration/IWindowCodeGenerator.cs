using System.Collections.Generic;
using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Gui;
using System.Linq;

namespace FlatRedBall.Glue.CodeGeneration
{
    public class IWindowCodeGenerator : ElementComponentCodeGenerator
    {
        public override ICodeBlock GenerateInitialize(ICodeBlock codeBlock, GlueElement element)
        {
            if (element is EntitySave && ((EntitySave)element).ImplementsIWindow &&
                ((EntitySave)element).GetInheritsFromIWindow() == false)
            {
                codeBlock.Line("this.Click += CallLosePush;");
                codeBlock.Line("this.RollOff += CallLosePush;");

            }
            return codeBlock;
        }

        public override void AddInheritedTypesToList(List<string> listToAddTo, GlueElement element)
        {
            if (element is EntitySave)
            {
                EntitySave entitySave = element as EntitySave;

                if (entitySave.ImplementsIWindow)
                {
                    listToAddTo.Add("FlatRedBall.Gui.IWindow");
                }
                if (entitySave.ImplementsIClickable)
                {
                    listToAddTo.Add("FlatRedBall.Gui.IClickable");
                }
            }
        }
        public override ICodeBlock GenerateAdditionalMethods(ICodeBlock codeBlock, GlueElement element)
        {
            EntitySave entitySave = element as EntitySave;

            if (entitySave == null || (!entitySave.ImplementsIClickable && !entitySave.ImplementsIWindow))
            {
                return codeBlock;
            }


            if (entitySave.ImplementsIWindow)
            {
                bool inheritsFromIWindow = entitySave.GetInheritsFromIWindow();

                // Add all the code that never changes if this is the base IWindow (doesn't have a parent IWindow)
                if (!inheritsFromIWindow)
                {
                    GenerateEnabledVariable(codeBlock, element);      
                }
            }

            IWindowCodeGenerator.WriteCodeForHasCursorOver(
                entitySave, codeBlock, entitySave.GetInheritsFromIWindowOrIClickable());

            var isVirtual = string.IsNullOrEmpty(entitySave.BaseEntity) || entitySave.GetInheritsFromIWindowOrIClickable() == false;

            codeBlock
                .Function("WasClickedThisFrame", "FlatRedBall.Gui.Cursor cursor", Public: true, Virtual: isVirtual, Override: !isVirtual, Type: "bool")
                .Line("return cursor.PrimaryClick && HasCursorOver(cursor);")
                .End();

            return codeBlock;
        }

        private static void GenerateEnabledVariable(ICodeBlock codeBlock, GlueElement element)
        {
            CustomVariable exposedEnabledVariable = element.CustomVariables.FirstOrDefault(item => item.Name == "Enabled" && item.Type == "bool");
            bool isEnableVariableExposed = exposedEnabledVariable != null;

            bool hasEvents = exposedEnabledVariable != null && exposedEnabledVariable.CreatesEvent && element.Events.Any(item => item.SourceVariable == exposedEnabledVariable.Name);

            if (hasEvents)
            {
                EventCodeGenerator.GenerateEventsForVariable(codeBlock, exposedEnabledVariable.Name, exposedEnabledVariable.Type);
            }

            string prefix;
            string propertyName;
            if (isEnableVariableExposed)
            {
                prefix = "public bool";
                propertyName = "Enabled";
            }
            else
            {
                prefix = "bool";
                propertyName = "FlatRedBall.Gui.IWindow.Enabled";
            }

            codeBlock.Line(Resources.Resource1.IWindowTemplate);

            var property = codeBlock.Property(prefix, propertyName);

            property.Get().Line("return mEnabled;");

            var setBlock = property.Set();
            if (hasEvents)
            {
                EventCodeGenerator.GenerateEventRaisingCode(setBlock, BeforeOrAfter.Before, exposedEnabledVariable.Name, element);
            }

            var setIf = setBlock.If("mEnabled != value");
            setIf.Line("mEnabled = value;");
            setIf.Line("EnabledChange?.Invoke(this);");
            if (hasEvents)
            {
                EventCodeGenerator.GenerateEventRaisingCode(setIf, BeforeOrAfter.After, exposedEnabledVariable.Name, element);
            }
        }

        private static void WriteCodeForHasCursorOver(EntitySave entitySave, ICodeBlock codeBlock, bool doesParentHaveCursorOverMethod)
        {
            ICodeBlock func;




            #region Write the method header

            if (doesParentHaveCursorOverMethod)
            {
                func = codeBlock.Function("public override bool", "HasCursorOver", "FlatRedBall.Gui.Cursor cursor");

                func.If("base.HasCursorOver(cursor)")
                    .Line("return true;");
            }
            else
            {
                func = codeBlock.Function("public virtual bool", "HasCursorOver", "FlatRedBall.Gui.Cursor cursor");
            }

            #endregion

            // Even if this has a base Entity, it should check to see
            // if it is paused, invisible, or on an invisible layer.  
            // The reason is if we rely on the base to
            // tell us that, the base will return false if it's paused but
            // we'll still end up checking the clickable objects in this.

            #region Make sure the Entity isn't paused

            func.If("mIsPaused")
                .Line("return false;");

            #endregion

            #region If IVisible, then check to make sure the object is visible
            if (entitySave.ImplementsIVisible)
            {
                func.If("!AbsoluteVisible")
                    .Line("return false;");
            }
            #endregion

            #region Check to make sure the layer this is on is visible

            func.If("LayerProvidedByContainer != null && LayerProvidedByContainer.Visible == false")
                .Line("return false;");

            #endregion

            #region Make sure cursor is on the layer

            func.If("!cursor.IsOn(LayerProvidedByContainer)")
                .Line("return false;");

            #endregion

            AddCodeForIsOn(func, entitySave.NamedObjects);

            func.Line("return false;");
        }

        public static void AddCodeForIsOn(ICodeBlock codeBlock, List<NamedObjectSave> namedObjects)
        {
            foreach (NamedObjectSave nos in namedObjects)
            {

                if (!nos.IsDisabled && nos.IncludeInIClickable)
                {
                    AddCodeForIsOn(codeBlock, nos);
                }
            }
        }

        private static void AddCodeForIsOn(ICodeBlock codeBlock, NamedObjectSave nos)
        {
            string condition = null;


            AssetTypeInfo ati = nos.GetAssetTypeInfo();
            if (ati != null && ati.HasCursorIsOn)
            {
                bool shouldConsiderVisible =
                    ati.QualifiedRuntimeTypeName.QualifiedType == "FlatRedBall.Sprite" ||
                    ati.QualifiedRuntimeTypeName.QualifiedType == "FlatRedBall.ManagedSpriteGroups.SpriteFrame" ||
                    ati.QualifiedRuntimeTypeName.QualifiedType == "FlatRedBall.Graphics.Text";

                bool shouldConsiderAlpha = 
                    ati.QualifiedRuntimeTypeName.QualifiedType == "FlatRedBall.Sprite" ||
                    ati.QualifiedRuntimeTypeName.QualifiedType == "FlatRedBall.ManagedSpriteGroups.SpriteFrame" ||
                    ati.QualifiedRuntimeTypeName.QualifiedType == "FlatRedBall.Graphics.Text";

                string whatToFormat;

                if (shouldConsiderVisible)
                {
                    whatToFormat = "{0}.AbsoluteVisible && cursor.IsOn3D({0}, LayerProvidedByContainer)";

                }
                else if (ati.QualifiedRuntimeTypeName.QualifiedType == "FlatRedBall.Scene")
                {
                    whatToFormat = "cursor.IsOn3D({0}, LayerProvidedByContainer, false)";
                }
                else
                {
                    whatToFormat = "cursor.IsOn3D({0}, LayerProvidedByContainer)";
                }

                if (shouldConsiderAlpha)
                {
                    whatToFormat = "{0}.Alpha != 0 && " + whatToFormat;
                }


                condition = string.Format(whatToFormat, nos.InstanceName);
            }
            else if (nos.SourceType == SourceType.Entity)
            {
                EntitySave entitySave = ObjectFinder.Self.GetEntitySave(nos.SourceClassType);
                if (entitySave != null)
                {
                    // This happens if:
                    // The user has an Entity which is IWindow
                    // The user adds a new object
                    // The user sets the object to Entity - this will cause a code regeneration and this will be null;
                    if (entitySave.ImplementsIWindow || entitySave.ImplementsIClickable)
                    {
                        condition = string.Format("{0}.HasCursorOver(cursor)", nos.InstanceName);
                    }
                }
            }
            if (condition != null)
            {
                codeBlock.If(condition)
                         .Line("return true;");
            }
        }

        public override bool HandlesVariable(CustomVariable variable, GlueElement container)
        {
            return variable.Name == "Enabled" && container is EntitySave && ((EntitySave)container).GetImplementsIWindowRecursively();
        }

        internal static void TryGenerateAddToManagers(ICodeBlock codeBlock, EntitySave saveObject)
        {
            if (saveObject.ImplementsIWindow && !saveObject.GetInheritsFromIWindow())
            {
                codeBlock.Line("FlatRedBall.Gui.GuiManager.AddWindow(this);");
            }
        }
    }
}
