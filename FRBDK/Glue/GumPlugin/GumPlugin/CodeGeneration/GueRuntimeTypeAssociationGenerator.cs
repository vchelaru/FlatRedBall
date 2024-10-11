using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.Managers;
using Gum.DataTypes;
using GumPlugin.DataGeneration;
using GumPlugin.Managers;
using GumPlugin.CodeGeneration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms.Design.Behavior;

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
            // This is the list of fulfillments that we will fill below
            List<AssociationFulfillment> associationFulfillments = new List<AssociationFulfillment>();

            var loadedElements = AppState.Self.AllLoadedElements.ToList();

            // First let's loop through the behaviors and see if any behavior has a default implementation
            foreach(var behavior in AppState.Self.GumProjectSave.Behaviors)
            {
                if(!string.IsNullOrEmpty( behavior.DefaultImplementation))
                {
                    var foundElement = loadedElements.FirstOrDefault(item => item.Name == behavior.DefaultImplementation);
                    var formsControlInfo = FormsControlInfo.AllControls
                        .FirstOrDefault(item => !string.IsNullOrEmpty(item.BehaviorName) && item.BehaviorName == behavior.Name);
                    if(foundElement != null && formsControlInfo != null)
                    {
                        var newFulfillment = new AssociationFulfillment();
                        newFulfillment.Element = foundElement;
                        // Technically this may not be completely fulfilled, but if it's a default implementation we don't care, we give it highest priority
                        newFulfillment.IsCompletelyFulfilled = true;
                        newFulfillment.ControlType = formsControlInfo.ControlName;

                        associationFulfillments.Add(newFulfillment);
                    }
                }
            }


            // Loop through all elements to see if they...
            foreach (var element in loadedElements)
            {
                // ...have behaviors...
                foreach(var behavior in element.Behaviors)
                {
                    // ...which implement control.
                    var formsControlInfo = FormsControlInfo.AllControls
                        .FirstOrDefault(item => !string.IsNullOrEmpty(item.BehaviorName) && item.BehaviorName == behavior.BehaviorName);

                    if(formsControlInfo != null)
                    {
                        string controlType = formsControlInfo.ControlName;

                        AssociationFulfillment matchingFulfillment = null;

                        if (controlType != null)
                        {
                            // Is there already a matching control? We need to know so we can compare if this element fulfills the control better
                            matchingFulfillment = associationFulfillments.FirstOrDefault(item => item.ControlType == controlType);
                        }

                        // Here we try to get the "most fulfilled" version of an object to set it as the default.
                        // For example, Button text is optional, and two Gum objects may have the Button behavior.
                        // If one of them has text properties then we should favor that over the one that doens't.
                        // Of course, the user can still change the defaults at runtime or manually create the visual
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

            foreach(var component in AppState.Self.GumProjectSave.Components)
            {
                // Is this component a forms control, but not a default forms control?
                // update October 17, 2023
                // Why do we exclude if it's 
                // a default forms control? Those
                // need the association too. I really
                // don't understand how this code was working
                // before, but this is causing problems, so I'm
                // going to fix it:
                // Update November 27, 2023 Vic
                // I now understand why this code was written this
                // way. For standard forms objects like Button, the 
                // fulfillments will be based on the element behaviors 
                // above. If GetIfShouldGenerate is false, that means we
                // aren't going to generate code for this type of forms component
                // because it's part of the standard FlatRedBall.Forms library. If
                // the object has Forms, but it is not a standard, then we will handle 
                // the fulfillment here:
                var isCustomComponent =
                    FormsClassCodeGenerator.Self.GetIfShouldGenerate(component);
                if (isCustomComponent)
                {
                    var shouldGenerate = FormsClassCodeGenerator.Self.GetIfShouldGenerate(component);

                    if (FormsClassCodeGenerator.Self.GetIfShouldGenerate(component) || 
                        (component.Behaviors != null && GueDerivingClassCodeGenerator.GetFormsControlTypeFrom(component.Behaviors) != null))
                        {
                        // associate them:
                        var newFulfillment = new AssociationFulfillment();
                        newFulfillment.Element = component;
                        newFulfillment.IsCompletelyFulfilled = true;

                        string standardFormsType = null;
                        if(component.Behaviors != null)
                        {
                            standardFormsType = GueDerivingClassCodeGenerator.GetFormsControlTypeFrom(component.Behaviors);
                        }

                        newFulfillment.ControlType = standardFormsType ?? FormsClassCodeGenerator.Self.GetFullRuntimeNamespaceFor(component) + 
                            "." + FormsClassCodeGenerator.Self.GetUnqualifiedRuntimeTypeFor(component);

                        associationFulfillments.Add(newFulfillment);
                    }

                }
            }

            foreach(var fulfillment in associationFulfillments)
            {
                string qualifiedControlType;

                if(fulfillment.ControlType.Contains("."))
                {
                    qualifiedControlType = fulfillment.ControlType;
                }
                else
                {
                    qualifiedControlType = "FlatRedBall.Forms.Controls." + fulfillment.ControlType;

                }

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
                case "FlatRedBall.Forms.Controls.Games.OnScreenKeyboard":
                case "FlatRedBall.Forms.Controls.Games.PlayerJoinView":
                case "FlatRedBall.Forms.Controls.Games.PlayerJoinViewItem":
                case "FlatRedBall.Forms.Controls.Games.SettingsView":
                case "FlatRedBall.Forms.Controls.Games.InputDeviceSelector":
                case "FlatRedBall.Forms.Controls.Games.InputDeviceSelectionItem":
                    return true;
                    // These require a Text object
                case "Button": 
                case "CheckBox":
                case "Label":
                case "ListBoxItem":
                case "RadioButton":
                case "ToggleButton":
                case "Toast":
                case "FlatRedBall.Forms.Controls.Popups.Toast":
                case "FlatRedBall.Forms.Controls.Games.DialogBox":
                    return element.Instances.Any(item => item.Name == "TextInstance" && item.BaseType == "Text");

                default:
                    throw new NotImplementedException($"Need to handle {controlType} in {nameof(GetIfIsCompleteFulfillment)}");
            }

        }
        
        private void GenerateAssociation(string controlType, string gumRuntimeType)
        {

        }

        private static void AddRegisterCode(ICodeBlock codeBlock, Gum.DataTypes.ElementSave element)
        {
            string elementNameString = element.Name.Replace("\\", "\\\\") ;

            var qualifiedName = GueDerivingClassCodeGenerator.Self.GetQualifiedRuntimeTypeFor(element);

            codeBlock.Line(
                "GumRuntime.ElementSaveExtensions.RegisterGueInstantiationType(\"" +
                elementNameString +
                "\", typeof(" +
                qualifiedName +
                "));");

            var needsGeneric = element is StandardElementSave && element.Name == "Container";

            if(needsGeneric)
            {
                codeBlock.Line(
                    "GumRuntime.ElementSaveExtensions.RegisterGueInstantiationType(\"" +
                    elementNameString + "<T>" +
                    "\", typeof(" +
                    qualifiedName + "<>" +
                    "));");
            }

        }
    }
}
