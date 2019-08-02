using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.Managers;
using Gum.DataTypes;
using GumPlugin.DataGeneration;
using GumPlugin.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GumPlugin.CodeGeneration
{
    public class GueRuntimeTypeAssociationGenerator : Singleton<GueRuntimeTypeAssociationGenerator>
    {



        public string GetRuntimeRegistrationPartialClassContents(bool registerFormsAssociations)
        {
            CodeBlockBase codeBlock = new CodeBlockBase(null);

            ICodeBlock currentBlock = codeBlock.Namespace("FlatRedBall.Gum");

            currentBlock = currentBlock.Class("public ", "GumIdbExtensions", "");

            currentBlock = currentBlock.Function("public static void", "RegisterTypes", "");
            {
                AddAssignmentFunctionContents(currentBlock);

                if(registerFormsAssociations)
                {
                    currentBlock._();
                    AddFormsAssociations(currentBlock);
                }

            }


            return codeBlock.ToString();
        }
        void AddAssignmentFunctionContents(ICodeBlock codeBlock)
        {
            foreach (var element in AppState.Self.AllLoadedElements)
            {
                if (GueDerivingClassCodeGenerator.Self.ShouldGenerateRuntimeFor(element))
                {
                    AddRegisterCode(codeBlock, element);
                }
            }
        }

        class AssociationFulfillment
        {
            public ElementSave Element { get; set; }
            public bool IsCompletelyFulfilled { get; set; }
            public string ControlType { get; set; }

            public override string ToString()
            {
                return $"{ControlType} by {Element}";
            }
        }

        private void AddFormsAssociations(ICodeBlock currentBlock)
        {
            List<AssociationFulfillment> associationFulfillments = new List<AssociationFulfillment>();

            var loadedElements = AppState.Self.AllLoadedElements.ToList();

            foreach (var element in loadedElements)
            {
                var elementAsComponent = element as ComponentSave;

                if(elementAsComponent != null)
                {
                    foreach(var behavior in elementAsComponent.Behaviors)
                    {
                        var formsControlInfo = FormsControlInfo.AllControls
                            .First(item => item.BehaviorName == behavior.BehaviorName);

                        string controlType = formsControlInfo.ControlName;

                        AssociationFulfillment matchingFulfillment = null;

                        if (controlType != null)
                        {
                            matchingFulfillment = associationFulfillments.FirstOrDefault(item => item.ControlType == controlType);
                        }

                        // Here we try to get the "most fulfilled" version of an object to set it as the default.
                        // For example, Button text is optional, and two Gum objects may have the Button behavior.
                        // If one of them has text properties then we should favor that over the one that doens't.
                        // Of coruse, the user can still change the defaults at runtime or manually create the visual
                        // for a form if they don't want the default, but this will hopefully give the "best fit"
                        // default.
                        if(matchingFulfillment == null || matchingFulfillment.IsCompletelyFulfilled == false)
                        {
                            bool isCompleteFulfillment = GetIfIsCompleteFulfillment(element, controlType);

                            if(matchingFulfillment == null)
                            {
                                var newFulfillment = new AssociationFulfillment();
                                newFulfillment.Element = element;
                                newFulfillment.IsCompletelyFulfilled = isCompleteFulfillment;
                                newFulfillment.ControlType = controlType;

                                associationFulfillments.Add(newFulfillment);
                            }
                            else if(isCompleteFulfillment)
                            {
                                matchingFulfillment.Element = element;
                                matchingFulfillment.IsCompletelyFulfilled = isCompleteFulfillment;
                                matchingFulfillment.ControlType = controlType;
                            }
                            
                        }
                    }
                }
                
            }

            // Here we add controls that don't have explicit visual definitions (yet)
            var userControlFulfillment = associationFulfillments.FirstOrDefault(item => item.ControlType == "UserControl");

            // StackPanel doesn't have any visuals, it's invisible so it will just be a regular GraphicalUiElement.
            //if(userControlFulfillment != null)
            //{
            //    associationFulfillments.Add(new AssociationFulfillment
            //    {
            //        Element = userControlFulfillment.Element,
            //        IsCompletelyFulfilled = true,
            //        ControlType = "StackPanel"
            //    });
            //}

            foreach(var fulfillment in associationFulfillments)
            {
                var qualifiedControlType = "FlatRedBall.Forms.Controls." + fulfillment.ControlType;

                var gumRuntimeType = 
                    GueDerivingClassCodeGenerator.Self.GetQualifiedRuntimeTypeFor(fulfillment.Element);

                var line =
                    $"FlatRedBall.Forms.Controls.FrameworkElement.DefaultFormsComponents[typeof({qualifiedControlType})] = typeof({gumRuntimeType});";

                currentBlock.Line(line);
            }
        }

        private bool GetIfIsCompleteFulfillment(ElementSave element, string controlType)
        {
            switch(controlType)
            {
                // some controls are automatically completely fulfilled:
                case "ComboBox":
                case "ListBox":
                case "PasswordBox":
                case "ScrollBar":
                case "ScrollViewer":
                case "Slider":
                case "TextBox":
                case "UserControl":
                case "TreeViewItem":
                case "TreeView":
                    return true;
                    // These require a Text object
                case "Button": 
                case "CheckBox":
                case "ListBoxItem":
                case "RadioButton":
                case "ToggleButton":
                    return element.Instances.Any(item => item.Name == "TextInstance" && item.BaseType == "Text");

                default:
                    throw new NotImplementedException($"Need to handle {controlType}");
            }

        }
        
        private void GenerateAssociation(string controlType, string gumRuntimeType)
        {

        }

        private static void AddRegisterCode(ICodeBlock codeBlock, Gum.DataTypes.ElementSave element)
        {
            string elementNameString = element.Name.Replace("\\", "\\\\") ;

            codeBlock.Line(
                "GumRuntime.ElementSaveExtensions.RegisterGueInstantiationType(\"" +
                elementNameString +
                "\", typeof(" +
                GueDerivingClassCodeGenerator.Self.GetQualifiedRuntimeTypeFor(element) +
                "));");
        }
    }
}
