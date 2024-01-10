using FlatRedBall.Glue.Plugins.ExportedImplementations;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace FlatRedBall.Glue.Controls
{
    /// <summary>
    /// Interaction logic for MultiButtonMessageBoxWpf.xaml
    /// </summary>
    public partial class MultiButtonMessageBoxWpf : System.Windows.Window
    {
        #region Fields/Properties

        List<Button> buttons = new List<Button>();

        public object ClickedResult
        {
            get;
            private set;
        }

        public string MessageText
        {
            set
            {
                LabelInstance.Text = value;
            }
        }

        #endregion

        public MultiButtonMessageBoxWpf()
        {
            InitializeComponent();

            this.KeyDown += HandleKeyDown;
            this.Loaded += (_,__) => GlueCommands.Self.DialogCommands.MoveToCursor(this);
        }

        private void HandleKeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Escape)
            {
                this.Close();
            }
        }

        public void AddButton(string text, object result)
        {
            Button button = new Button();
            button.Click += HandleButtonClickInternal;
            button.TabIndex = buttons.Count;
            //button.Content = text;

            TextBlock myTextBlock = new TextBlock();
            // Set the TextWrapping property to Wrap
            myTextBlock.TextWrapping = TextWrapping.Wrap;
            // Set the text of the TextBlock
            myTextBlock.Text = text;

            // Set the content of the Button to the TextBlock
            button.Content = myTextBlock;

            button.Tag  = result;

            button.Margin = new Thickness(8,4,8,4);

            // Make this 50 so that it is bigger and can have 2 lines of text. 
            // Eventually this could be automatic.
            button.Height = 50;

            buttons.Add(button);

            this.MainPanel.Children.Add(button);
        }

        private void HandleButtonClickInternal(object sender, RoutedEventArgs e)
        {
            ClickedResult = ((Button)sender).Tag;

            this.DialogResult = true;
        }

    }
}
