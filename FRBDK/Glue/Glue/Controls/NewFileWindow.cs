using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using FlatRedBall.Glue.Elements;
using Glue;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Utilities;
using FlatRedBall.Glue.Plugins.ExportedImplementations;

namespace FlatRedBall.Glue.Controls
{
	public partial class NewFileWindow : Form
    {
        #region Fields

        bool mIsNameDefault = true;
        DynamicUiHelper mDynamicUiHelper = new DynamicUiHelper();

        List<FileTypeOptions> mFileTypeOptions = new List<FileTypeOptions>();
        List<string> mNamesAlreadyUsed = new List<string>();


        #endregion

        #region Properties

        public List<string> NamesAlreadyUsed
        {
            get
            {
                return mNamesAlreadyUsed;
            }
        }

        public AssetTypeInfo ResultAssetTypeInfo
		{
			get { return mOptionsComboBox.SelectedItem as AssetTypeInfo; }
		}

        public object SelectedItem
        {
            get
            {
                return mOptionsComboBox.SelectedItem;
            }
            set
            {
                mOptionsComboBox.SelectedItem = value;
            }
        }

        public string ResultName
		{
			get { return textBox1.Text; }
            set
            {
                textBox1.Text = value;
            }
		}

        public string ComboBoxMessage
        {
            get { return ComboBoxLabel.Text; }
            set { ComboBoxLabel.Text = value; }
        }

        #endregion


        public NewFileWindow()
		{
			InitializeComponent();

			StartPosition = FormStartPosition.Manual;
            Create2D3DUI();
            CreateSpreadsheetOptions();

            foreach (var option in mFileTypeOptions)
            {
                CreateUiForOptions(option);
            }

            Location = new Point(MainGlueWindow.MousePosition.X - Width/2, MainGlueWindow.MousePosition.Y - Height/2);
        }

        private void Create2D3DUI()
        {
            FileTypeOptions m2D3DFileTypeOptions = new FileTypeOptions();
            m2D3DFileTypeOptions.ObjectType.Add("Scene");
            m2D3DFileTypeOptions.Options.Add("2D");
            m2D3DFileTypeOptions.Options.Add("3D");
            m2D3DFileTypeOptions.UiType = UiType.RadioButton;

            mFileTypeOptions.Add(m2D3DFileTypeOptions);
        }

        private void CreateSpreadsheetOptions()
        {
            FileTypeOptions m2D3DFileTypeOptions = new FileTypeOptions();
            m2D3DFileTypeOptions.ObjectType.Add("Spreadsheet");
            m2D3DFileTypeOptions.Options.Add("Dictionary");
            m2D3DFileTypeOptions.Options.Add("List");
            m2D3DFileTypeOptions.UiType = UiType.RadioButton;

            mFileTypeOptions.Add(m2D3DFileTypeOptions);
        }

        private void CreateUiForOptions(FileTypeOptions options)
        {
            options.UiId = mDynamicUiHelper.AddUi(this.DynamicUiPanel, options.Options, options.UiType);
            mDynamicUiHelper.Hide(options.UiId);
        }

		public void AddOption(object option)
		{
			mOptionsComboBox.Items.Add(option);
		}

        private void OptionsComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {

            if (mOptionsComboBox.SelectedItem != null && mOptionsComboBox.SelectedItem is AssetTypeInfo)
            {
                AssetTypeInfo ati = mOptionsComboBox.SelectedItem as AssetTypeInfo;

                string fileType = GetObjectTypeFromAti(ati);

                
                foreach (var option in mFileTypeOptions)
                {
                    if (option.ObjectType.Contains(fileType))
                    {
                        mDynamicUiHelper.Show(option.UiId);
                    }
                    else
                    {
                        mDynamicUiHelper.Hide(option.UiId);
                    }
                }
                if (mIsNameDefault)
                {
                    // We want to make sure we don't
                    // suggest a name that is already
                    // being used.
                    //textBox1.Text = fileType + "File";
                    textBox1.Text = StringFunctions.MakeStringUnique(fileType + "File", mNamesAlreadyUsed, 2);
                    
                    while (GlueCommands.Self.GluxCommands.GetReferencedFileSaveFromFile(textBox1.Text + "." + ResultAssetTypeInfo.Extension) != null)
                    {
                        textBox1.Text = FlatRedBall.Utilities.StringFunctions.IncrementNumberAtEnd(textBox1.Text);
                    }
                }
            }
        }

        public string GetObjectTypeFromAti(AssetTypeInfo ati)
        {
            if (ati == null)
            {
                return null;
            }
            else
            {
                string fileType = ati.FriendlyName;
                if (fileType.Contains("("))
                {
                    fileType = fileType.Substring(0, fileType.IndexOf('('));
                }

                fileType = fileType.Replace(" ", "");
                return fileType;
            }
        }

        
        public int AddTextBox(string label)
        {
            flowLayoutPanel1.SuspendLayout();
            flowLayoutPanel1.Controls.Remove(OkCancelPanel);

            List<string> labels = new List<string>();
            
            labels.Add(label);
            int labelId = mDynamicUiHelper.AddUi(flowLayoutPanel1, labels, UiType.Label);

            Label labelControl = mDynamicUiHelper.GetControl(labelId) as Label;
            labelControl.Margin = new System.Windows.Forms.Padding(3, 0, 3, 0);
            labelControl.Height = 16;

            List<string> values = new List<string>();
            values.Add("");
            int returnValue = mDynamicUiHelper.AddUi(flowLayoutPanel1, values, UiType.TextBox);

            TextBox textBox = mDynamicUiHelper.GetControl(returnValue) as TextBox;

            textBox.Margin = new Padding(3, 0, 3, 3);
            flowLayoutPanel1.Controls.Add(OkCancelPanel);
            flowLayoutPanel1.ResumeLayout();
            flowLayoutPanel1.PerformLayout();
            

            return returnValue;
        }

        internal string GetValueFromId(int valueId)
        {
            return mDynamicUiHelper.GetValue(valueId);
        }

        private void mOkWindow_Click(object sender, EventArgs e)
        {

        }
    }

    #region FileTypeOptions

    class FileTypeOptions
    {
        public List<string> ObjectType = new List<string>();
        public List<string> Options = new List<string>();
        public UiType UiType;
        public int UiId;
    }

    #endregion
}


