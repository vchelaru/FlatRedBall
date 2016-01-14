using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace FlatRedBall.Glue.FormHelpers
{
    public enum UiType
    {
        Button,
        TextBox,
        RadioButton,
        Label
    }

    public class DynamicUiHelper
    {
        int nextId = 0;

        Dictionary<int, Control> mControlDictionary = new Dictionary<int, Control>();

        public int AddUi(FlowLayoutPanel flowLayoutPanel, List<string> values, UiType uiType)
        {
            FlowLayoutPanel layoutToAddTo = flowLayoutPanel;

            if (values.Count > 1)
            {
                FlowLayoutPanel subPanel = new FlowLayoutPanel();
                subPanel.AutoSize = true;
                subPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
                layoutToAddTo.Controls.Add(subPanel);
                layoutToAddTo = subPanel;
            }

            for(int i = 0; i < values.Count; i++)
            {
                string value = values[i];

                switch (uiType)
                {
                    case UiType.Button:
                        Button button = new Button();
                        button.Text = value;
                        layoutToAddTo.Controls.Add(button);

                        break;

                    case UiType.RadioButton:
                        RadioButton radioButton = new RadioButton();
                        radioButton.Text = value;
                        layoutToAddTo.Controls.Add(radioButton);
                        if (i == 0)
                        {
                            radioButton.Checked = true;
                        }
                        break;
                    case UiType.TextBox:
                        TextBox textBox = new TextBox();
                        textBox.Text = value;
                        textBox.Width = layoutToAddTo.Width - 4;
                        layoutToAddTo.Controls.Add(textBox);
                        

                        break;
                    case UiType.Label:
                        Label label = new Label();
                        label.Text = value;
                        label.Width = layoutToAddTo.Width - 4;
                        layoutToAddTo.Controls.Add(label);
                        

                        break;
                }

 
            }

            if (values.Count == 1)
            {
                mControlDictionary.Add(nextId, flowLayoutPanel.Controls[flowLayoutPanel.Controls.Count - 1]);
            }
            else
            {
                mControlDictionary.Add(nextId, layoutToAddTo);
            }

            nextId++;
            return nextId - 1;

        }

        public void Hide(int id)
        {
            mControlDictionary[id].Visible = false;
        }

        public void Show(int id)
        {
            mControlDictionary[id].Visible = true;
        }

        public Control GetControl(int id)
        {
            return mControlDictionary[id];
        }

        public string GetValue(int id)
        {
            Control controlAtId = mControlDictionary[id];

            if (controlAtId is FlowLayoutPanel)
            {
                if (controlAtId.Controls[0] is RadioButton)
                {
                    foreach (Control control in controlAtId.Controls)
                    {
                        if(((RadioButton)control).Checked)
                        {
                            return ((RadioButton)control).Text;
                        }
                    }
                }
                else if (controlAtId.Controls[0] is TextBox)
                {
                    return ((TextBox)controlAtId.Controls[0]).Text;
                }
                return "";

            }
            else if (controlAtId is TextBox)
            {
                return ((TextBox)controlAtId).Text;
            }
            else
            {

                throw new NotImplementedException();
            }
        }

    }
}
