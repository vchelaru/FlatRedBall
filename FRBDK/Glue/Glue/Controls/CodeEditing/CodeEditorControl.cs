using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Reflection;
using FlatRedBall.Utilities;
using FlatRedBall.Glue.CodeGeneration;
using FlatRedBall.IO;
using FlatRedBall.Glue.Events;
using System.IO;
using FlatRedBall.Glue.Parsing;
using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Plugins.ExportedImplementations.CommandInterfaces;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.Editor;

namespace FlatRedBall.Glue.Controls
{
    public partial class CodeEditorControl : UserControl
    {
        #region Fields

        public bool mIsCodeValid = false;
        
        DateTime mLastSourceChangeTime;
        bool mShouldSave = false;
        Timer mTimer;

        string mLastSavedText;
        StringBuilder stringBuilder = new StringBuilder();

        AutoCompleteForm autoCompleteForm = new AutoCompleteForm();

        List<string> originalList;

        AutoCompleteManager mAutoCompleteManager;

        #endregion

        #region Properties

        public string TopText
        {
            get { return TopLabel.Text; }
            set { TopLabel.Text = value; }
        }

        public string BottomText
        {
            get { return BottomLabel.Text; }
            set { BottomLabel.Text = value; }
        }

        public override bool Focused
        {
            get
            {
                return base.Focused || this.syntaxBoxControl1.Focused;
            }
        }
        #endregion

        #region Event Methods

        void OnTimerTick(object sender, EventArgs e)
        {
            const float timeSinceLastEditToWaitForSave = 1;
            if (mShouldSave && (DateTime.Now - mLastSourceChangeTime).TotalSeconds > timeSinceLastEditToWaitForSave)
            {
                SaveText();
                mShouldSave = false;
            }
        }

        #endregion

        #region Constructor

        public CodeEditorControl()
        {
            InitializeComponent();
            mAutoCompleteManager = new AutoCompleteManager();
            mAutoCompleteManager.Initialize(syntaxBoxControl1);

            syntaxDocument1.SyntaxFile = @"Content\SyntaxHighlighting\C#.syn";
            mTimer = new Timer {Interval = 2000};

            // This means the timer will check every 2 seconds to see if the user has typed anything that should be saved.
            mTimer.Tick += OnTimerTick;
            mTimer.Start();

            TopText = "";
            BottomText = "";
        }

        #endregion
        
        public void UpdateDisplayToCurrentObject()
        {
            string fullFileName = "<Unable to get file name>";
            try
            {
                EventResponseSave eventResponseSave = EditorLogic.CurrentEventResponseSave;
                IElement element = EditorLogic.CurrentElement;

                fullFileName = eventResponseSave.GetSharedCodeFullFileName();

                string contents = FileManager.FromFileText(fullFileName);

                CSharpParser parser = new CSharpParser();
                SyntaxTree syntaxTree = parser.Parse(contents);
                mIsCodeValid = syntaxTree.Errors.Count == 0;

                if (mIsCodeValid)
                {
                    ParsedMethod parsedMethod = eventResponseSave.GetParsedMethodFromAssociatedFile();

                    string textToAssign = null;
                    if (parsedMethod == null)
                    {
                        textToAssign = eventResponseSave.GetEventContents();

                    }
                    else
                    {
                        StringBuilderDocument document = new StringBuilderDocument(contents);

                        bool wasFound;

                        textToAssign = GetMethodContentsFor(syntaxTree, document, eventResponseSave.EventName, out wasFound);
                        if (wasFound)
                        {
                            textToAssign = textToAssign.Replace("\n", "\r\n");
                        }
                        else
                        {
                            mIsCodeValid = false;
                            textToAssign = "Could not find the method, or encountered a parse error.";
                        }
                    }

                    textToAssign = RemoveWhiteSpaceForCodeWindow(textToAssign);
                    this.syntaxBoxControl1.Document.Text = textToAssign;
                    mLastSavedText = this.syntaxBoxControl1.Document.Text;
                }
                else
                {
                    this.syntaxBoxControl1.Document.Text = "This code file is not a complete code file:\n" +
                        fullFileName +
                        "\nGlue is unable to parse it.  Please correct the problems in Visual Studio";
                }
            }
            catch (Exception e)
            {
                mIsCodeValid = false;
                this.syntaxBoxControl1.Document.Text = "Error parsing file:\n" +
                    fullFileName +
                    "\nMore details:\n\n" + e.ToString();
            }
        }

        private string GetMethodContentsFor(SyntaxTree syntaxTree, StringBuilderDocument document, string methodName, out bool wasFound)
        {
            ICSharpCode.NRefactory.CSharp.NamespaceDeclaration namespaceDecl = null;
            TypeDeclaration classDecl = null;
            MethodDeclaration methodDecl = null;
            BlockStatement blockStatement = null;
            string toReturn = null;

            ICSharpCode.NRefactory.TextLocation? start = null;
            ICSharpCode.NRefactory.TextLocation? end = null;
            

            foreach (var child in syntaxTree.Children)
            {
                if (child is ICSharpCode.NRefactory.CSharp.NamespaceDeclaration)
                {
                    namespaceDecl = child as NamespaceDeclaration;
                    break;
                }
            }

            if (namespaceDecl != null)
            {
                foreach (var child in namespaceDecl.Children)
                {
                    if (child is TypeDeclaration)
                    {
                        classDecl = child as TypeDeclaration;
                        break;
                    }
                }
            }
            if (classDecl != null)
            {
                foreach (var child in classDecl.Children)
                {
                    if (child is MethodDeclaration && (child as MethodDeclaration).Name == "On" + methodName)
                    {
                        methodDecl = child as MethodDeclaration;
                        break;
                    }
                }
            }

            if (methodDecl != null)
            {
                foreach (var child in methodDecl.Children)
                {
                    if (child is BlockStatement)
                    {
                        blockStatement = child as BlockStatement;
                        break;
                    }
                }
            }

            if (blockStatement != null)
            {
                foreach (var child in blockStatement.Children)
                {
                    if ((child is CSharpTokenNode) == false)
                    {
                        if (start == null)
                        {
                            start = child.StartLocation;
                        }
                        end = child.EndLocation;
                    }
                }

            }

            if (start.HasValue)
            {
                int offset = document.GetOffset(start.Value);
                int length = document.GetOffset(end.Value) - offset;
                wasFound = true;
                return document.GetText(offset, length);
            }
            else if (blockStatement != null)
            {
                // we found a pure empty method
                wasFound = true;
                return "";
            }
            else
            {
                wasFound = false;
                return null;
            }
        }

        public static string RemoveWhiteSpaceForCodeWindow(string textToAssign)
        {
            if (!string.IsNullOrEmpty(textToAssign))
            {
                textToAssign = textToAssign.Replace("\r\r", "\r");
                textToAssign = textToAssign.Replace("\n\t\t", "\n");
                textToAssign = textToAssign.Replace("\r\n\t", "\r\n");
                textToAssign = textToAssign.Replace("\r\n\t\t", "\r\n");
                textToAssign = textToAssign.Replace("\r\n            ", "\r\n");
                textToAssign = textToAssign.Replace("\n            ", "\n");
                if (textToAssign.StartsWith("            "))
                {
                    textToAssign = textToAssign.Substring(12);
                }
            }
            return textToAssign;
        }

        /// <summary>
        /// Determines if a code file is valid based off of the number of opening and closing
        /// brackets it has.  This method counts { and }, but doesn't include comments or contsts like
        /// "{0}".
        /// </summary>
        /// <param name="fileName">The file name to open - this should be the .cs file for C# files.</param>
        /// <returns>Whether the file is valid.</returns>
        public static bool DetermineIfCodeFileIsValid(string fileName)
        {
            string contents = FileManager.FromFileText(fileName);

            contents = ParsedClass.RemoveComments(contents);


            int numberOfOpenBrackets = ParsedClass.NumberOfValid('{', contents);
            int numberOfClosedBrackets = ParsedClass.NumberOfValid('}', contents);

            return numberOfOpenBrackets == numberOfClosedBrackets;
        }


        private void SaveText()
        {
            if (EditorLogic.CurrentEventResponseSave != null && mIsCodeValid)
            {
                IElement currentElement = EditorLogic.CurrentElement;
                EventResponseSave currentEvent = EditorLogic.CurrentEventResponseSave;

                
                if (this.syntaxBoxControl1.Document.Text != mLastSavedText)
                {
                    if (HasMatchingBrackets(this.syntaxBoxControl1.Document.Text))
                    {
                        mLastSavedText = this.syntaxBoxControl1.Document.Text;

                        EventCodeGenerator.InjectTextForEventAndSaveCustomFile(currentElement, EditorLogic.CurrentEventResponseSave, mLastSavedText);
                        PluginManager.ReceiveOutput("Saved " + EditorLogic.CurrentEventResponseSave);
                        GlueCommands.Self.GenerateCodeCommands.GenerateCurrentElementCode();
                        GluxCommands.Self.SaveGlux();

                    }
                    else
                    {
                        PluginManager.ReceiveError("Mismatch of } and { in event " + EditorLogic.CurrentEventResponseSave);
                    }
                }
            }
        }

        public static bool HasMatchingBrackets(string text)
        {
            string contentsWithoutComments = ParsedClass.RemoveComments(text);

            int numberOfOpening = contentsWithoutComments.CountOf('{');
            int numberOfClosing = contentsWithoutComments.CountOf('}');

            return numberOfOpening == numberOfClosing;
        }





        private void syntaxBoxControl1_TextChanged(object sender, EventArgs e)
        {


            mShouldSave = true;

        }

        private void syntaxBoxControl1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                syntaxBoxControl1.AutoListVisible = false;
            }
            else if (e.KeyCode == Keys.Back)
            {

                int index = syntaxBoxControl1.Caret.Position.X;

                if (index > 0)
                {

                    char characterDeleting = syntaxBoxControl1.Caret.CurrentRow.Text[index-1];

                    if (characterDeleting == '.')
                    {
                        syntaxBoxControl1.AutoListVisible = false;
                    }
                }
            }
        }




        private void syntaxBoxControl1_KeyPress(object sender, KeyPressEventArgs e)
        {

        }

        

        string lastFilteredWord = "";
        private void syntaxBoxControl1_KeyUp(object sender, KeyEventArgs e)
        {
            if (ShouldAutoCompleteShowForKeyTyped(e))
            {

                originalList = mAutoCompleteManager.GetAutoCompleteValues();


                if (originalList.Count != 0)
                {

                    syntaxBoxControl1.AutoListClear();
                    syntaxBoxControl1.AutoListBeginLoad();
                    foreach (string s in originalList)
                    {
                        syntaxBoxControl1.AutoListAdd(s, -1);
                    }
                    syntaxBoxControl1.AutoListEndLoad();
                    syntaxBoxControl1.AutoListVisible = true;

                    syntaxBoxControl1.AutoListPosition = syntaxBoxControl1.Caret.Position;

                    if (originalList.Count != 0)
                    {
                        syntaxBoxControl1.AutoListSelectedText = originalList[0];
                    }
                }

            }
            else if (syntaxBoxControl1.AutoListVisible)
            {
                if (syntaxBoxControl1.Caret.CurrentWord != null)
                {
                    string word = syntaxBoxControl1.Caret.CurrentWord.Text;
                    if (word != "." && lastFilteredWord != word)
                    {
                        lastFilteredWord = word;
                        List<string> filtered = new List<string>();
                        foreach (string s in originalList)
                        {
                            if (s.Contains(word))
                            {
                                filtered.Add(s);
                            }
                        }

                        syntaxBoxControl1.AutoListClear();
                        syntaxBoxControl1.AutoListBeginLoad();


                        foreach (string s in filtered)
                        {
                            syntaxBoxControl1.AutoListAdd(s, -1);
                        }
                        syntaxBoxControl1.AutoListEndLoad();

                        syntaxBoxControl1.AutoListAutoSelect = true;

                        if (filtered.Count != 0)
                        {
                            // First we want to sse if we have any entries
                            // that start with our current word.  If not, then
                            // we'll just pick the first item

                            // Default to entry 0
                            int entryToShow = 0;
                            for (int i = 0; i < filtered.Count; i++)
                            {
                                if (filtered[i].StartsWith(lastFilteredWord))
                                {
                                    entryToShow = i;
                                    break;
                                }
                            }

                            syntaxBoxControl1.AutoListSelectedText = filtered[entryToShow];
                        }
                    }
                }

            }
        }

        private static bool ShouldAutoCompleteShowForKeyTyped(KeyEventArgs e)
        {
            return e.KeyCode == Keys.OemPeriod || e.KeyCode == Keys.Space;
        }

        private void CodeEditorControl_VisibleChanged(object sender, EventArgs e)
        {
            if (this.Visible == false)
            {
                syntaxBoxControl1.AutoListVisible = false;
            }
        }

        internal void HideAutoComplete()
        {
            syntaxBoxControl1.AutoListVisible = false;

        }
    }
}
