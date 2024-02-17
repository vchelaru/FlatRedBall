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
    #region DialogPageTask

    public class DialogPageTask
    {
        public string Page { get; set; }
        public Func<Task>  Task { get; set; }

        public static implicit operator DialogPageTask(string page) => 
            new DialogPageTask { Page = page };

        public static implicit operator DialogPageTask(Func<Task> task) =>
            new DialogPageTask { Task = task };

    }

    #endregion

    public class DialogBox : FrameworkElement, IInputReceiver
    {
        #region Fields/Properties

        GraphicalUiElement textComponent;
        GraphicalUiElement continueIndicatorInstance;
        RenderingLibrary.Graphics.Text coreTextObject;

        public static double LastTimeDismissed { get; private set; }

        List<DialogPageTask> Pages = new List<DialogPageTask>();

        public int PagesRemaining => Pages.Count;

        static global::Gum.DataTypes.Variables.StateSave NoTextShownState;

        string currentPageText;

        Tweener showLetterTweener;

        public event FocusUpdateDelegate FocusUpdate;

        public List<Keys> IgnoredKeys => throw new NotImplementedException();

        public bool TakingInput { get; set; } = true;

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

        /// <summary>
        /// The number of letters to show per second when printing out in "typewriter style". 
        /// If null, 0, or negative, then the text is shown immediately.
        /// </summary>
        public int? LettersPerSecond { get; set; } = 20;

        public bool TypeNextPageImmediatelyOnCancelPush { get; set; } = true;

        #endregion

        #region Events

        /// <summary>
        /// Raised when the dialog box finishes showing all pages.
        /// </summary>
        public event EventHandler FinishedShowing;

        /// <summary>
        /// Raised whenever a page finishes typing out, either automatically or in response to input.
        /// </summary>
        public event EventHandler FinishedTypingPage;

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

        #region Show Methods
        /// <summary>
        /// Shows the dialog box (adds it to managers and sets IsVisible to true) and begins showing the text.
        /// </summary>
        /// <param name="text">The text to print out, either immediately or letter-by-letter according to LettersPerSecond.</param>
        /// <param name="frbLayer">The FlatRedBall Layer to add the DialogBox to. If null, the dialog box will not be layered. This will attempt to use a Gum layer matching the FRB layer. This will automatically work if the Layer has been added through the FlatRedBall Editor.</param>
        public void Show(string text, FlatRedBall.Graphics.Layer frbLayer = null)
        {
            base.Show(frbLayer);
            showNextPageOnDismissedPage = true;
            ShowInternal(text, forceImmediatePrint:false);
        }

        public void Show(IEnumerable<string> pages, FlatRedBall.Graphics.Layer frbLayer = null)
        {
            base.Show(frbLayer);

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

        /// <summary>
        /// Shows the dialog box (adds it to managers and sets IsVisible to true) and begins showing the text.
        /// </summary>
        /// <param name="text">The text to print out, either immediately or letter-by-letter according to LettersPerSecond.</param>
        /// <returns>A task which completes when the text has been displayed and the DialogBox has been dismissed.</returns>
        public Task ShowAsync(string text)
        {
            var pages = ConvertToPages(text);
            return ShowAsync(pages);
        }


        public async Task ShowAsync(IEnumerable<string> pages, FlatRedBall.Graphics.Layer frbLayer = null)
        {
            base.Show(frbLayer);

            showNextPageOnDismissedPage = false;
            if (pages.Any())
            {
                foreach (var page in pages)
                {
                    this.Pages.Add(page);
                }
                await StartShowAllPagesLoop();
            }
        }

        public async Task ShowAsync(IEnumerable<DialogPageTask> pageTasks, FlatRedBall.Graphics.Layer frbLayer = null)
        {
            base.Show(frbLayer);

            showNextPageOnDismissedPage = false;
            if (pageTasks.Any())
            {
                this.Pages.AddRange(pageTasks);
                await StartShowAllPagesLoop();
            }
        }

        // September 28, 2023
        // Vic asks - why do we 
        // have ShowDialog? Is it
        // to match the ShowDialog 
        // from the base FrameworkElement?
        // If so, what's the poinnt because
        // this doesn't override the base parameters.
        // It requires pages. 
        [Obsolete("Use ShowAsync")]
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

        // See comment above about why this is obsolete.
        [Obsolete("Use ShowAsync")]
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

        public void ShowNextPage(bool forceImmediatePrint = false)
        {
            var page = Pages.FirstOrDefault();

            if(page != null)
            {
                ShowInternal(page.Page, forceImmediatePrint);
                Pages.RemoveAt(0);
            }
        }

        bool wasLastAdvancePressPrintImmediate = false;
        private async Task StartShowAllPagesLoop()
        {
            var page = Pages.FirstOrDefault();
            wasLastAdvancePressPrintImmediate = false;

            while (page != null)
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
                    // Update August 9, 2023 - no, don't always 
                    // focus it. The user may have intentionally 
                    // unfocused:
                    //this.IsFocused = true;

                    var semaphoreSlim = new SemaphoreSlim(1);

                    void ReleaseSemaphor(object sender, EventArgs args) => 
                        semaphoreSlim.Release();

                    this.PageAdvanced += ReleaseSemaphor;

                    semaphoreSlim.Wait();
                    ShowInternal(page.Page, forceImmediatePrint: wasLastAdvancePressPrintImmediate);

                    await semaphoreSlim.WaitAsync();
                    semaphoreSlim.Dispose();
                    this.PageAdvanced -= ReleaseSemaphor;

                }
                page = Pages.FirstOrDefault();
            }
        }

        private void ShowInternal(string text, bool forceImmediatePrint)
        {
            IsVisible = true;

            currentPageText = text;

            showLetterTweener?.Stop();
#if DEBUG
            ReportMissingTextInstance();
#endif
            // go through the component instead of the core text object to force a layout refresh if necessary
            textComponent.SetProperty("Text", text);


            var tags = BbCodeParser.Parse(text, CustomSetPropertyOnRenderable.Tags);
            var strippedLength = BbCodeParser.RemoveTags(text, tags).Length;

            var shouldPrintCharacterByCharacter = LettersPerSecond > 0 && !forceImmediatePrint;
            if(shouldPrintCharacterByCharacter)
            {
                coreTextObject.MaxLettersToShow = 0;
                var allTextShownState = new global::Gum.DataTypes.Variables.StateSave();


                allTextShownState.Variables.Add(new global::Gum.DataTypes.Variables.VariableSave
                {
                    Name = "TextInstance.MaxLettersToShow",
                    Value = strippedLength,
                    SetsValue = true
                });

                var duration = strippedLength / (float)LettersPerSecond;

                showLetterTweener = this.Visual.InterpolateTo(NoTextShownState, allTextShownState, duration, InterpolationType.Linear, Easing.Out);

                if (continueIndicatorInstance != null)
                {
                    continueIndicatorInstance.Visible = false;
                }
                showLetterTweener.Ended += () =>
                {
                    if (TakingInput && continueIndicatorInstance != null)
                    {
                        continueIndicatorInstance.Visible = true;
                    }
                    FinishedTypingPage?.Invoke(this, null);
                };
            }
            else
            {
                coreTextObject.MaxLettersToShow = strippedLength;

                if (TakingInput && continueIndicatorInstance != null)
                {
                    continueIndicatorInstance.Visible = true;
                }

                if (continueIndicatorInstance != null)
                {
                    continueIndicatorInstance.Visible = true;
                }
                FinishedTypingPage?.Invoke(this, null);
            }
        }


        private string[] ConvertToPages(string text)
        {

            var limitsLines = 
                this.coreTextObject.MaxNumberOfLines != null || 
                this.textComponent.HeightUnits != global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;

            if(!limitsLines)
            {
                // it can show any number of lines, it's up to the user to handle spillover
                // by limiting the page length or by expanding the dialog box.
                return new string[] { text };
            }
            else
            {
                var foundTags = BbCodeParser.Parse(text, CustomSetPropertyOnRenderable.Tags);

                var withRemovedTags = BbCodeParser.RemoveTags(text, foundTags);

                var unlimitedLines = new List<string>();
                var oldVerticalMode = this.coreTextObject.TextOverflowVerticalMode;
                this.coreTextObject.TextOverflowVerticalMode = RenderingLibrary.Graphics.TextOverflowVerticalMode.SpillOver;
                coreTextObject.RawText = withRemovedTags;
                coreTextObject.UpdateLines(unlimitedLines);

                this.coreTextObject.TextOverflowVerticalMode = oldVerticalMode;
                this.textComponent.SetProperty("Text", withRemovedTags);

                var limitedLines = coreTextObject.WrappedText;

                if (unlimitedLines.Count == limitedLines.Count)
                {
                    // no need to break it up
                    return new string[] { text };
                }
                else
                {
                    var pages = new List<string>();

                    var absoluteLineNumber = 0;

                    var currentPage = new StringBuilder();
                    currentPage.Clear();

                    StringBuilder stringBuilder = new StringBuilder();
                    int strippedTextCount = 0;
                    while(absoluteLineNumber < unlimitedLines.Count)
                    {
                        stringBuilder.Clear();

                        for(int i = 0; i < limitedLines.Count && absoluteLineNumber < unlimitedLines.Count; i++)
                        {
                            var toAppend = unlimitedLines[absoluteLineNumber];
                            var sizeBeforeTags = toAppend.Length;
                            if(foundTags.Count > 0)
                            {
                                toAppend = BbCodeParser.AddTags(toAppend, foundTags, strippedTextCount);
                            }
                            strippedTextCount += sizeBeforeTags;
                            stringBuilder.Append(toAppend);
                            absoluteLineNumber++;
                        }
                        pages.Add(stringBuilder.ToString());
                    }

                    return pages.ToArray();
                }
            }


        }

        #endregion

        #region Event Handler Methods

        private void HandleClick(IWindow window)
        {
            if(AdvancePageInputPredicate == null)
            {
                ReactToConfirmInput();
            }
        }

        /// <summary>
        /// This makes the next page auto-show when pushing input on an already-typed out page.
        /// This should be true if doing a normal Show call, but false if in an async call since
        /// the async call will internally loop through all pages.
        /// </summary>
        bool showNextPageOnDismissedPage = true;
        private void ReactToConfirmInput()
        {
            ReactToInputForAdvancing(forceImmediatePrint: false);
        }

        private void ReactToCancelInput()
        {
            wasLastAdvancePressPrintImmediate = TypeNextPageImmediatelyOnCancelPush;
            ReactToInputForAdvancing(forceImmediatePrint: true);
        }

        private void ReactToInputForAdvancing(bool forceImmediatePrint)
        {
            ////////////////////Early Out/////////////////////
            if (!TakingInput)
            {
                return;
            }
            //////////////////End Early Out///////////////////
            //var hasMoreToType = coreTextObject.MaxLettersToShow < currentPageText?.Length;

            // Use the raw text since that has stripped out the tags
            var hasMoreToType = coreTextObject.MaxLettersToShow < coreTextObject.RawText.Length;
            if (hasMoreToType)
            {
                showLetterTweener?.Stop();

                if (continueIndicatorInstance != null && TakingInput)
                {
                    continueIndicatorInstance.Visible = true;
                }

                coreTextObject.MaxLettersToShow = currentPageText.Length;

                FinishedTypingPage?.Invoke(this, null);
            }
            else if (Pages.Count > 0)
            {
                if (showNextPageOnDismissedPage)
                {
                    ShowNextPage(forceImmediatePrint);
                }

                PageAdvanced?.Invoke(this, null);
            }
            else
            {
                Dismiss();
            }
        }

        public void Dismiss()
        {
            this.IsVisible = false;
            LastTimeDismissed = TimeManager.CurrentTime;
            PageAdvanced?.Invoke(this, null);
            FinishedShowing?.Invoke(this, null);
            IsFocused = false;
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
                    ReactToConfirmInput();
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
                        ReactToConfirmInput();
                    }

                    if(inputDevice.DefaultCancelInput.WasJustPressed)
                    {
                        ReactToCancelInput();
                    }
                }

                var genericGamepads = GuiManager.GenericGamePadsForUiControl;
                for(int i = 0; i < genericGamepads.Count; i++)
                {
                    var gamepad = gamepads[i];

                    var inputDevice = gamepad as IInputDevice;

                    if (inputDevice.DefaultConfirmInput.WasJustPressed)
                    {
                        ReactToConfirmInput();
                    }

                    if(inputDevice.DefaultCancelInput.WasJustPressed)
                    {
                        ReactToCancelInput();
                    }
                }
                var keyboardAsInputDevice = InputManager.Keyboard as IInputDevice;

                if(keyboardAsInputDevice.DefaultPrimaryActionInput.WasJustPressed)
                {
                    ReactToConfirmInput();
                }
                if(keyboardAsInputDevice.DefaultCancelInput.WasJustPressed)
                {
                    ReactToCancelInput();
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
