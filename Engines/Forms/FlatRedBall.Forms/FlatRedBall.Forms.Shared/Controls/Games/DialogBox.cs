using FlatRedBall.Glue.StateInterpolation;
using FlatRedBall.Gui;
using FlatRedBall.Input;
using Gum.Wireframe;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FlatRedBall.Forms.Controls.Games
{
    public class DialogPageTask
    {
        public string Page { get; set; }
        public Func<Task>  Task { get; set; }

        public static implicit operator DialogPageTask(string page) => 
            new DialogPageTask { Page = page };

        public static implicit operator DialogPageTask(Func<Task> task) =>
            new DialogPageTask { Task = task };

    }

    public class DialogBox : FrameworkElement, IInputReceiver
    {
        #region Fields/Properties

        GraphicalUiElement textComponent;
        GraphicalUiElement continueIndicatorInstance;
        RenderingLibrary.Graphics.Text coreTextObject;

        public static double LastTimeDismissed { get; private set; }

        List<DialogPageTask> Pages = new List<DialogPageTask>();

        static global::Gum.DataTypes.Variables.StateSave NoTextShownState;

        string currentPageText;

        Tweener showLetterTweener;

        public event FocusUpdateDelegate FocusUpdate;

        public List<Keys> IgnoredKeys => throw new NotImplementedException();

        public bool TakingInput => throw new NotImplementedException();

        public IInputReceiver NextInTabSequence { get; set; }

        public override bool IsFocused
        {
            get => base.IsFocused;
            set
            {
                base.IsFocused = value;
                UpdateToIsFocused();
            }
        }

        #endregion

        #region Events

        public event EventHandler FinishedShowing;

        public event EventHandler PageAdvanced;

        /// <summary>
        /// If not null, this predicate is used to determine if input
        /// has been pressed to advance the input. If null, the default 
        /// page-advancing logic will be performed.
        /// </summary>
        public Func<bool> AdvancePageInputPredicate;

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
            base.Show();
            showNextPageOnDismissedPage = true;
            ShowInternal(text);
        }

        public void Show(IEnumerable<string> pages)
        {
            base.Show();

            showNextPageOnDismissedPage = true;
            if (pages.Any())
            {
                foreach(var page in pages)
                {
                    this.Pages.Add(page);
                }

                ShowNextPage();
            }
        }

        public async Task ShowAsync(IEnumerable<string> pages, FlatRedBall.Graphics.Layer frbLayer = null)
        {
            base.Show();

            showNextPageOnDismissedPage = false;
            if (pages.Any())
            {
                foreach (var page in pages)
                {
                    this.Pages.Add(page);
                }
                await ShowNextPageAsync();
            }
        }

        public async Task ShowAsync(IEnumerable<DialogPageTask> pageTasks, FlatRedBall.Graphics.Layer frbLayer = null)
        {
            base.Show(frbLayer);

            showNextPageOnDismissedPage = false;
            if (pageTasks.Any())
            {
                this.Pages.AddRange(pageTasks);
                await ShowNextPageAsync();
            }
        }

        public async Task<bool?> ShowDialog(IEnumerable<string> pageTasks, FlatRedBall.Graphics.Layer frbLayer = null)
        {
#if DEBUG
            if (Visual == null)
            {
                throw new InvalidOperationException("Visual must be set before calling Show");
            }
#endif
            await ShowAsync(pageTasks, frbLayer);

            this.Close();

            return null;
        }

        public async Task<bool?> ShowDialog(IEnumerable<DialogPageTask> pageTasks, FlatRedBall.Graphics.Layer frbLayer = null)
        {
#if DEBUG
            if (Visual == null)
            {
                throw new InvalidOperationException("Visual must be set before calling Show");
            }
#endif
            var semaphoreSlim = new SemaphoreSlim(1);

            void HandleRemovedFromManagers(object sender, EventArgs args) => semaphoreSlim.Release();
            Visual.RemovedFromGuiManager += HandleRemovedFromManagers;

            semaphoreSlim.Wait();
            await ShowAsync(pageTasks, frbLayer);
            await semaphoreSlim.WaitAsync();

            Visual.RemovedFromGuiManager -= HandleRemovedFromManagers;
            // for now, return null, todo add dialog results

            semaphoreSlim.Dispose();

            return null;
        }

        private void ShowNextPage()
        {
            var page = Pages.FirstOrDefault();

            if(page != null)
            {
                ShowInternal(page.Page);
                Pages.RemoveAt(0);
            }
        }

        private async Task ShowNextPageAsync()
        {
            var page = Pages.FirstOrDefault();

            while(page != null)
            {
                // remove it before calling ShowInternal so that the dialog box hides if there are no pages
                Pages.RemoveAt(0);
                if(page.Task != null)
                {
                    this.IsVisible = false;
                    this.IsFocused = false;
                    await page.Task();

                    // special case if ending on a dialog:
                    if(Pages.Count == 0)
                    {
                        LastTimeDismissed = TimeManager.CurrentTime;
                        PageAdvanced?.Invoke(this, null);
                        FinishedShowing?.Invoke(this, null);
                    }
                }
                else
                {
                    this.IsVisible = true;
                    // todo - do we want to always focus it?
                    this.IsFocused = true;

                    var semaphoreSlim = new SemaphoreSlim(1);

                    void ReleaseSemaphor(object sender, EventArgs args) => 
                        semaphoreSlim.Release();

                    this.PageAdvanced += ReleaseSemaphor;

                    semaphoreSlim.Wait();
                    ShowInternal(page.Page);

                    await semaphoreSlim.WaitAsync();
                    semaphoreSlim.Dispose();
                    this.PageAdvanced -= ReleaseSemaphor;

                }
                page = Pages.FirstOrDefault();
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

        /// <summary>
        /// This makes the next page auto-show when pushing input on an already-typed out page.
        /// This should be true if doing a normal Show call, but false if in an async call since
        /// the async call will internally loop through all pages.
        /// </summary>
        bool showNextPageOnDismissedPage = true;
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
                if(showNextPageOnDismissedPage)
                {
                    ShowNextPage();
                }

                PageAdvanced?.Invoke(this, null);
            }
            else
            {
                this.IsVisible = false;
                LastTimeDismissed = TimeManager.CurrentTime;
                PageAdvanced?.Invoke(this, null);
                FinishedShowing?.Invoke(this, null);
                IsFocused = false;
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
            if(AdvancePageInputPredicate != null)
            {
                if(AdvancePageInputPredicate())
                {
                    ReactToInput();
                }
            }
            else
            {
                var gamepads = GuiManager.GamePadsForUiControl;
                for (int i = 0; i < gamepads.Count; i++)
                {
                    var gamepad = gamepads[i];

                    var inputDevice = gamepad as IInputDevice;

                    if(inputDevice.DefaultConfirmInput.WasJustPressed)
                    {
                        ReactToInput();
                    }
                }

                var genericGamepads = GuiManager.GenericGamePadsForUiControl;
                for(int i = 0; i < genericGamepads.Count; i++)
                {
                    var gamepad = gamepads[i];

                    var inputDevice = gamepad as IInputDevice;

                    if (inputDevice.DefaultConfirmInput.WasJustPressed)
                    {
                        ReactToInput();
                    }
                }
            }

            var keyboardAsInputDevice = InputManager.Keyboard as IInputDevice;

            if(keyboardAsInputDevice.DefaultPrimaryActionInput.WasJustPressed)
            {
                ReactToInput();
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

        #region UpdateTo Methods

        private void UpdateToIsFocused()
        {
            UpdateState();

            if (isFocused)
            {
                if (FlatRedBall.Input.InputManager.InputReceiver != this)
                {
                    FlatRedBall.Input.InputManager.InputReceiver = this;
                }
            }

            else if (!isFocused)
            {
                if (FlatRedBall.Input.InputManager.InputReceiver == this)
                {
                    FlatRedBall.Input.InputManager.InputReceiver = null;
                }

                // Vic says - why do we need to deselect when it loses focus? It could stay selected
                //SelectionLength = 0;
            }
        }

        #endregion
    }
}
