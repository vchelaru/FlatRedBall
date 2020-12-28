using FlatRedBall.Glue.StateInterpolation;
using FlatRedBall.Gui;
using FlatRedBall.Input;
using Gum.Wireframe;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlatRedBall.Forms.Controls.Games
{
    public class DialogBox : FrameworkElement, IInputReceiver
    {
        #region Fields/Properties

        GraphicalUiElement textComponent;
        GraphicalUiElement continueIndicatorInstance;
        RenderingLibrary.Graphics.Text coreTextObject;

        List<string> Pages = new List<string>();

        static global::Gum.DataTypes.Variables.StateSave NoTextShownState;

        string currentPageText;

        Tweener showLetterTweener;

        public event FocusUpdateDelegate FocusUpdate;

        public List<Keys> IgnoredKeys => throw new NotImplementedException();

        public bool TakingInput => throw new NotImplementedException();

        public IInputReceiver NextInTabSequence { get; set; }

        #endregion

        #region Initialize

        static DialogBox()
        {
            NoTextShownState = new global::Gum.DataTypes.Variables.StateSave();
            NoTextShownState.Variables.Add(new global::Gum.DataTypes.Variables.VariableSave
            {
                Name = "TextInstance.MaxLettersToShow",
                Value = 0,
                SetsValue = true
            });
        }

        public DialogBox() : base() { }

        public DialogBox(GraphicalUiElement visual) : base(visual) { }

        protected override void ReactToVisualChanged()
        {
            textComponent = base.Visual.GetGraphicalUiElementByName("TextInstance");

            // it's okay if this is null
            continueIndicatorInstance = base.Visual.GetGraphicalUiElementByName("ContinueIndicatorInstance");

            coreTextObject = textComponent?.RenderableComponent as RenderingLibrary.Graphics.Text;

            Visual.Click += HandleClick;

            base.ReactToVisualChanged();
        }

        #endregion

        public void Show(string text)
        {
            ShowInternal(text);
        }

        public void Show(IEnumerable<string> pages)
        {
            if(pages.Any())
            {
                this.Pages.AddRange(pages);

                ShowNextPage();
            }
        }

        private void ShowNextPage()
        {
            var page = Pages.FirstOrDefault();

            if(page != null)
            {
                ShowInternal(page);
                Pages.RemoveAt(0);
            }
        }

        private void ShowInternal(string text)
        {
            IsVisible = true;

            currentPageText = text;

            showLetterTweener?.Stop();
#if DEBUG
            ReportMissingTextInstance();
#endif
            // go through the component instead of the core text object to force a layout refresh if necessary
            textComponent.SetProperty("Text", text);

            coreTextObject.MaxLettersToShow = 0;

            var allTextShownState = new global::Gum.DataTypes.Variables.StateSave();
            allTextShownState.Variables.Add(new global::Gum.DataTypes.Variables.VariableSave
            {
                Name = "TextInstance.MaxLettersToShow",
                Value = text.Length,
                SetsValue = true
            });

            const float lettersPerSecond = 20;
            var duration = text.Length / lettersPerSecond;

            showLetterTweener = this.Visual.InterpolateTo(NoTextShownState, allTextShownState, duration, InterpolationType.Linear, Easing.Out);

            if (continueIndicatorInstance != null)
            {
                continueIndicatorInstance.Visible = false;
                showLetterTweener.Ended += () =>
                {
                    continueIndicatorInstance.Visible = true;
                };
            }
        }

        #region Event Handler Methods

        private void HandleClick(IWindow window)
        {
            ReactToInput();
        }

        private void ReactToInput()
        {
            var hasMoreToType = coreTextObject.MaxLettersToShow < currentPageText?.Length;
            if (hasMoreToType)
            {
                showLetterTweener?.Stop();

                if (continueIndicatorInstance != null)
                {
                    continueIndicatorInstance.Visible = true;
                }

                coreTextObject.MaxLettersToShow = currentPageText.Length;
            }
            else if(Pages.Count > 0)
            {
                ShowNextPage();
            }
            else
            {
                this.IsVisible = false;
            }
        }

        #endregion

        #region Utilities

#if DEBUG
        private void ReportMissingTextInstance()
        {
            if (textComponent == null)
            {
                throw new Exception(
                    $"This button was created with a Gum component ({Visual?.ElementSave}) " +
                    "that does not have an instance called 'text'. A 'text' instance must be added to modify the button's Text property.");
            }
        }
#endif

        #endregion

        #region IInputReceiver Methods

        public void OnFocusUpdate()
        {
            var gamepads = GuiManager.GamePadsForUiControl;
            for (int i = 0; i < gamepads.Count; i++)
            {
                var gamepad = gamepads[i];

                var inputDevice = gamepad as IInputDevice;

                if(inputDevice.DefaultPrimaryActionInput.WasJustPressed)
                {
                    ReactToInput();
                }
            }
        }

        public void OnGainFocus()
        {
        }

        public void LoseFocus()
        {
        }

        public void ReceiveInput()
        {
        }

        public void HandleKeyDown(Keys key, bool isShiftDown, bool isAltDown, bool isCtrlDown)
        {

        }

        public void HandleCharEntered(char character)
        {

        }

        #endregion
    }
}
