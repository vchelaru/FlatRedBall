using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using FlatRedBall.IO;
using GlueView.Facades;
using GlueViewOfficialPlugins.Scripting;

namespace GlueView.Scripting
{
    public partial class FullScriptingReplForm : Form
    {
        ScriptParsingPlugin mScriptParsingPlugin;

        bool mIsUpdateTextDirty = false;
        private const int EM_SETCUEBANNER = 0x1501;
        private const int EM_GETCUEBANNER = 0x1502;

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern Int32 SendMessage(IntPtr hWnd, int msg,
        int wParam, [MarshalAs(UnmanagedType.LPWStr)] string lParam);

        CodeContext mClassLevelCodeContext = new CodeContext(null);

        public FullScriptingReplForm(ScriptParsingPlugin scriptParsingPlugin)
        {
            InitializeComponent();
            mScriptParsingPlugin = scriptParsingPlugin;

            ClassScopeTextBox.AllowDrop = true;
            ClassScopeTextBox.DragDrop += HandleDragDrop;
            ClassScopeTextBox.SelectAll();
            int numberOfTabs = 30;
            int[] tabs = new int[numberOfTabs];
            for (int i = 0; i < tabs.Length; i++)
            {
                tabs[i] = 14 * i;
            }
            ClassScopeTextBox.SelectionTabs = tabs;

            InitializeTextBox.AllowDrop = true;
            InitializeTextBox.DragDrop += HandleDragDrop;
            InitializeTextBox.SelectionTabs = tabs;

            UpdateTextBox.AllowDrop = true;
            UpdateTextBox.DragDrop += HandleDragDrop;
            UpdateTextBox.SelectionTabs = tabs;
            // does not work on rich text boxes
            //SetCueText(InitializeTextBox, "<Add Intialize Code Here>");
            //SetCueText(UpdateTextBox, "<Add Update Code Here>");
        }

        void HandleDragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("FileDrop"))
            {
                foreach (var fileName in (string[])e.Data.GetData("FileDrop"))
                {
                    // only allow one file
                    LoadScript(fileName);
                    break;
                }
            }
        }

        private void Apply_Click(object sender, EventArgs e)
        {
            mIsUpdateTextDirty = true;

            if (ResetOnApplyCheckBox.Checked)
            {
                ResetClick(null, null);
            }

            mClassLevelCodeContext = new CodeContext(null);

            try
            {
                ApplyRichTextBoxScript(ClassScopeTextBox);
            }
            catch
            {
                MessageBox.Show("Error parsing Class Scope script");
            }

            try
            {
                mClassLevelCodeContext.AddVariableStack();
                ApplyRichTextBoxScript(InitializeTextBox);

                mClassLevelCodeContext.RemoveVariableStack();
            }
            catch
            {
                MessageBox.Show("Error parsing Initialize script");
            }
        }

        private void ResetClick(object sender, EventArgs e)
        {
            GlueViewCommands.Self.ElementCommands.ReloadCurrentElement();
        }

        private void ScriptTextBox_TextChanged(object sender, EventArgs e)
        {
            // What did this code even do?
            //int start = InitializeTextBox.SelectionStart;
            //int count = InitializeTextBox.SelectionLength;

            //string text = InitializeTextBox.Text;
            //InitializeTextBox.Clear();
            //InitializeTextBox.Text = text;
            //InitializeTextBox.SelectionStart = start;
            //InitializeTextBox.SelectionLength = count;
        }


        static void SetCueText(Control control, string text)
        {
            SendMessage(control.Handle, EM_SETCUEBANNER, 0, text);
        }

        private void ClassScopeTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F5)
            {
                Apply_Click(null, null);
            }
            if (e.KeyCode == Keys.Tab)
            {
                e.Handled = true;
            }
            if (e.Control && e.KeyCode == Keys.Down)
            {
                this.InitializeTextBox.Focus();
            }
        }

        private void InitializeTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F5)
            {
                Apply_Click(null, null);
            }
            if (e.KeyCode == Keys.Tab)
            {
                e.Handled = true;
            }
            if (e.Control && e.KeyCode == Keys.Up)
            {
                this.ClassScopeTextBox.Focus();
            }
            if (e.Control && e.KeyCode == Keys.Down)
            {
                this.UpdateTextBox.Focus();
            }
        }
        private void UpdateTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F5)
            {
                Apply_Click(null, null);

            }
            if (e.KeyCode == Keys.Tab)
            {
                e.Handled = true;
            }
            if (e.Control && e.KeyCode == Keys.Up)
            {
                this.InitializeTextBox.Focus();
            }
        }

        internal void PerformUpdateScript()
        {

            try
            {
                mClassLevelCodeContext.AddVariableStack();

                var richTextBox = UpdateTextBox;

                ApplyRichTextBoxScript(richTextBox, mIsUpdateTextDirty);

                mIsUpdateTextDirty = false;
            }
            catch (Exception e)
            {
                MessageBox.Show("Error on update");
            }
            finally
            {
                mClassLevelCodeContext.RemoveVariableStack();
            }
        }

        private void ApplyRichTextBoxScript(RichTextBox richTextBox, bool refreshSelection = true)
        {
            if (richTextBox.Lines.Length != 0)
            {
                bool succeeded = mScriptParsingPlugin.ApplyLinesInternal(
                        richTextBox.Lines,
                        0,
                        richTextBox.Lines.Length,
                        GlueViewState.Self.CurrentElement,
                        mClassLevelCodeContext, null);

                //if (succeeded && richTextBox.BackColor != Color.White)
                //{
                //    richTextBox.BackColor = Color.White;
                //}
                //else if (!succeeded && richTextBox.BackColor != Color.Pink)
                //{
                //    richTextBox.BackColor = Color.Pink;
                //}
                if (refreshSelection)
                {
                    richTextBox.Invoke((MethodInvoker)(() =>
                    {
                        int oldSelection = richTextBox.SelectionStart;

                        if (!succeeded)
                        {
                            int errorLine = mScriptParsingPlugin.LastErrorLine;

                            if (errorLine != -1)
                            {
                                MarkLineAsRed(richTextBox, errorLine);
                            }
                        }
                        else
                        {

                            richTextBox.Select(0, richTextBox.Text.Length);


                            richTextBox.SelectionBackColor = System.Drawing.Color.White;
                            richTextBox.SelectionColor = System.Drawing.Color.Black;
                            //richTextBox.SelectionColor = Color.Red;

                        }
                        richTextBox.Select(oldSelection, 0);
                    }));
                }

            }
            else
            {
                //if (richTextBox.BackColor != Color.White)
                //{
                //    richTextBox.BackColor = Color.White;
                //}
            }
        }

        private void MarkLineAsRed(RichTextBox richTextBox, int errorLine)
        {
            int index = 0;
            errorLine = Math.Min(errorLine, richTextBox.Lines.Length - 1);

            for (int i = 0; i < errorLine; i++)
            {
                index += richTextBox.Lines[i].Length + 1;
            }

            richTextBox.Select(index, richTextBox.Lines[errorLine].Length);


            richTextBox.SelectionBackColor = System.Drawing.Color.Red;
            richTextBox.SelectionColor = System.Drawing.Color.Black;
            //richTextBox.SelectionColor = Color.Red;
            richTextBox.Select(0, 0);
        }

        private void loadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            var result = ofd.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK)
            {
                string fileToLoad = ofd.FileName;

                LoadScript(fileToLoad);
            }
        }

        private void LoadScript(string fileToLoad)
        {
            ScriptSave scriptSave = FileManager.XmlDeserialize<ScriptSave>(fileToLoad);

            this.SetFromScriptSave(scriptSave);

            Apply_Click(null, null);
        }

        private void SetFromScriptSave(ScriptSave scriptSave)
        {
            this.ClassScopeTextBox.Text = scriptSave.ClassScope;
            this.InitializeTextBox.Text = scriptSave.Initialize;
            this.UpdateTextBox.Text = scriptSave.Update;
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            var result = sfd.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK)
            {
                string fileToSave = sfd.FileName;

                ScriptSave scriptSave = this.CreateScriptSave();

                FileManager.XmlSerialize(scriptSave, fileToSave);
            }
        }

        private ScriptSave CreateScriptSave()
        {
            ScriptSave toReturn = new ScriptSave();
            toReturn.ClassScope = this.ClassScopeTextBox.Text;
            toReturn.Initialize = this.InitializeTextBox.Text;
            toReturn.Update = this.UpdateTextBox.Text;
            return toReturn;
        }

        private void UpdateTextBox_TextChanged(object sender, EventArgs e)
        {
            mIsUpdateTextDirty = true;
        }

        
    }
}
