using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using FlatRedBall.Localization;
using FlatRedBall.Glue;
using GlueView.Facades;

namespace GlueView.Forms
{
    public partial class LocalizationControl : UserControl
    {
        static LocalizationControl mSelf;





        public static LocalizationControl Self
        {
            get { return mSelf; }
        }

        public LocalizationControl()
        {
            mSelf = this;
            InitializeComponent();
        }


        public void PopulateFromLocalizationManager()
        {
            bool differs = GetWhetherLanguagesHaveChanged();

            if (differs)
            {
                this.comboBox1.Items.Clear();
                for (int i = 0; i < LocalizationManager.Languages.Count; i++)
                {
                    comboBox1.Items.Add(LocalizationManager.Languages[i]);
                }

                if (LocalizationManager.Languages.Count != 0)
                {
                    comboBox1.Text = LocalizationManager.Languages[0];
                }
                else
                {
                    comboBox1.Text = "";
                }
            }
        }

        private bool GetWhetherLanguagesHaveChanged()
        {
            bool differs = false;

            if (LocalizationManager.Languages.Count != comboBox1.Items.Count)
            {
                differs = true;
            }
            else
            {
                for (int i = 0; i < LocalizationManager.Languages.Count; i++)
                {
                    if (LocalizationManager.Languages[i] != (string)comboBox1.Items[i])
                    {
                        differs = true;
                    }
                }
            }
            return differs;
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            string language = (string)comboBox1.Text;

            int index = LocalizationManager.Languages.IndexOf(language);

            LocalizationManager.CurrentLanguage = index;

            // All of the text is going to change, so we need to reload the current element
            GlueViewCommands.Self.ElementCommands.ReloadCurrentElement();
        }
    }
}
