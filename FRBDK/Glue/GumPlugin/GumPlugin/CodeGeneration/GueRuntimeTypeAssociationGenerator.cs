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
        }

        private void AddFormsAssociations(ICodeBlock currentBlock)
        {
            List<AssociationFulfillment> assocationFulfillments = new List<AssociationFulfillment>();

            var loadedElements = AppState.Self.AllLoadedElements.ToList();

            foreach (var element in loadedElements)
            {
                var elementAsComponent = element as ComponentSave;

                if(elementAsComponent != null)
                {
                    foreach(var behavior in elementAsComponent.Behaviors)
                    {
                        string controlType = GetControlTypeFromBehavior(behavior);

                        AssociationFulfillment matchingFulfillment = null;

                        if (controlType != null)
                        {
                            matchingFulfillment = assocationFulfillments.FirstOrDefault(item => item.ControlType == controlType);
                        }

                        if(matchingFulfillment == null || matchingFulfillment.IsCompletelyFulfilled == false)
                        {
                            bool isCompleteFulfillment = GetIfIsCompleteFulfillment(element, controlType);

                            if(matchingFulfillment == null)
                            {
                                var newFulfillment = new AssociationFulfillment();
                                newFulfillment.Element = element;
                                newFulfillment.IsCompletelyFulfilled = isCompleteFulfillment;
                                newFulfillment.ControlType = controlType;

                                assocationFulfillments.Add(newFulfillment);
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

            foreach(var fulfillment in assocationFulfillments)
            {
                var qualifiedControlType = "FlatRedBall.Forms.Controls." + fulfillment.ControlType;

                string unqualifiedName = FlatRedBall.IO.FileManager.RemovePath(fulfillment.Element.Name);
                var gumRuntimeType =
                    GueDerivingClassCodeGenerator.GueRuntimeNamespace + "." + unqualifiedName + "Runtime";

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

        private static string GetControlTypeFromBehavior(Gum.DataTypes.Behaviors.ElementBehaviorReference behavior)
        {
            string controlType = null;
            switch (behavior.BehaviorName)
            {
                case BehaviorGenerator.ButtonBehaviorName: controlType = "Button"; break;
                case BehaviorGenerator.CheckBoxBehaviorName: controlType = "CheckBox"; break;
                case BehaviorGenerator.ComboBoxBehaviorName: controlType = "ComboBox"; break;
                case BehaviorGenerator.ListBoxItemBehaviorName: controlType = "ListBoxItem"; break;
                case BehaviorGenerator.ListBoxBehaviorName: controlType = "ListBox"; break;
                case BehaviorGenerator.RadioButtonBehaviorName: controlType = "RadioButton"; break;
                case BehaviorGenerator.ScrollBarBehaviorName: controlType = "ScrollBar"; break;
                case BehaviorGenerator.ScrollViewerBehaviorName: controlType = "ScrollViewer"; break;
                case BehaviorGenerator.SliderBehaviorName: controlType = "Slider"; break;
                case BehaviorGenerator.TextBoxBehaviorName: controlType = "TextBox"; break;
                case BehaviorGenerator.ToggleBehaviorName: controlType = "ToggleButton"; break;
                case BehaviorGenerator.TreeViewBehaviorName: controlType = "TreeView"; break;
                case BehaviorGenerator.TreeViewItemBehaviorName: controlType = "TreeViewItem"; break;
                case BehaviorGenerator.UserControlBehaviorName: controlType = "UserControl"; break;
            }

            return controlType;
        }

        private void GenerateAssociation(string controlType, string gumRuntimeType)
        {

        }

        private static void AddRegisterCode(ICodeBlock codeBlock, Gum.DataTypes.ElementSave element)
        {
            // don't remove the path:
            string unqualifiedName = FlatRedBall.IO.FileManager.RemovePath(  element.Name );
            string elementNameString = element.Name.Replace("\\", "\\\\") ;

            codeBlock.Line(
                "GumRuntime.ElementSaveExtensions.RegisterGueInstantiationType(\"" +
                elementNameString +
                "\", typeof(" +
                GueDerivingClassCodeGenerator.GueRuntimeNamespace + "." +
                unqualifiedName + "Runtime" +
                "));");
        }
    }
}
