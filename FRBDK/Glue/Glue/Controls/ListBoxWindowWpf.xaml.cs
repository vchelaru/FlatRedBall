using FlatRedBall.Glue.Plugins.ExportedImplementations;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace FlatRedBall.Glue.Controls
{
    /// <summary>
    /// Interaction logic for ListBoxWindowWpf.xaml
    /// </summary>
    public partial class ListBoxWindowWpf : Window
    {
        List<Button> mButtons = new List<Button>();

        public object ClickedOption { get; private set; }

        public object SelectedListBoxItem => ListBoxInstance.SelectedItem;

        public string Message
        {
            get => DisplayTextLabel.Text;
            set => DisplayTextLabel.Text = value;
        }

        public ListBoxWindowWpf()
        {
            InitializeComponent();

            AddButton("OK", System.Windows.Forms.DialogResult.OK);

            GlueCommands.Self.DialogCommands.MoveToCursor(this);
        }

        public void AddItem(object objectToAdd)
        {
            ListBoxInstance.Items.Add(objectToAdd);
        }

        public void ClearButtons()
        {
            foreach (Button button in mButtons)
            {
                this.ButtonStackPanel.Children.Remove(button);
            }

            mButtons.Clear();

        }

        public void AddControl(UIElement element)
        {
            this.AdditionalControlStackPanel.Children.Add(element);
        }

        public void AddButton(string message, object result)
        {
            Button button = new Button();

            button.Content = message;
            button.Tag = result;
            button.Click += (not, used) =>
            {
                ClickedOption = result;
                this.DialogResult = true;
            };
            this.ButtonStackPanel.Children.Add(button);
            mButtons.Add(button);
        }
    }
}
