using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using GlueView.Facades;
using GlueView.Scripting;

namespace GlueViewOfficialPlugins.Scripting
{
    public partial class ScriptingControl : UserControl
    {
        public event EventHandler ButtonClick;
        ScriptParsingPlugin mScriptParsingPlugin;

        List<FullScriptingReplForm> mScriptingForms = new List<FullScriptingReplForm>();

        public bool Enabled
        {
            get
            {
                return EnabledCheckBox.Checked;
            }
        }

        public ScriptingControl(ScriptParsingPlugin scriptParsingPlugin)
        {
            mScriptParsingPlugin = scriptParsingPlugin;
            InitializeComponent();
        }

        internal void AddText(string text)
        {
            this.OutputTextBox.Invoke((MethodInvoker)(() =>
            {
                this.OutputTextBox.Text += text + "\n";
            }));
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (ButtonClick != null)
            {
                ButtonClick(this, null);
            }
        }

        private void ReplTextBox_KeyPress(object sender, KeyPressEventArgs e)
        {

        }

        private void ReplTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                OutputTextBox.Text = "";
                string whatToProcess = ReplTextBox.Text;
                e.Handled = true;
                e.SuppressKeyPress = true;
                mScriptParsingPlugin.ApplyLinesInternal(
                    new string[] { whatToProcess },
                    0,
                    1,
                    GlueViewState.Self.CurrentElement,
                    new CodeContext(GlueViewState.Self.CurrentElementRuntime));

                // We don't want to lose focus - that just
                // makes the output box gain focus.
                //ReplTextBox.Enabled = false;
                //ReplTextBox.Enabled = true;
                ReplTextBox.Text = "";
            }
        }

        private void EnabledCheckBox_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void EditInWindowButton_Click(object sender, EventArgs e)
        {
            ShowNewFullScriptingForm();
        }

        public void ShowNewFullScriptingForm()
        {
            FullScriptingReplForm fullForm = new FullScriptingReplForm(mScriptParsingPlugin);
            fullForm.Show();
            mScriptingForms.Add(fullForm);
            fullForm.FormClosing += RemoveFromList;
        }

        private void RemoveFromList(object sender, FormClosingEventArgs e)
        {
            mScriptingForms.Remove(sender as FullScriptingReplForm);
        }

        public void FrameBasedUpdate()
        {
            foreach (FullScriptingReplForm form in mScriptingForms)
            {
                form.PerformUpdateScript();
            }
        }
    }
}
