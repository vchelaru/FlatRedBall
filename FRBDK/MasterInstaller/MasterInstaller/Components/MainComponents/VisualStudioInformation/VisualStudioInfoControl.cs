using MasterInstaller.Components.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;

namespace MasterInstaller.Components.MainComponents.VisualStudioInformation
{
    public class VisualStudioInfoControl : BasePage
    {
        public VisualStudioInfoControl() : base()
        {
            Title = "Visual Studio Required";

            StackPanel stackPanel = new StackPanel();
            LeftPanel.Children.Add(stackPanel);

            string message = "FlatRedBall requires Visual Studio 2015. " +
                "Visual Studio 2015 (Community Edition) can be downloaded " +
                "for free by clicking the button below.";

            var textBlock = new TextBlock();
            textBlock.Text = message;
            textBlock.FontSize = 16;
            textBlock.LineHeight = 24;
            textBlock.TextWrapping = System.Windows.TextWrapping.Wrap;
            stackPanel.Children.Add(textBlock);

            var button = new Button();
            button.Content = "Download Visual Studio 2015";
            button.Height = 42;
            button.Click += DownloadButtonClicked;
            button.Margin = new Thickness(14);
            stackPanel.Children.Add(button);


            message = "If you already have " +
                "Visual Studio 2015, click Next.";

            textBlock = new TextBlock();
            textBlock.Text = message;
            textBlock.FontSize = 16;
            textBlock.LineHeight = 24;
            textBlock.TextWrapping = System.Windows.TextWrapping.Wrap;
            stackPanel.Children.Add(textBlock);
        }

        private void DownloadButtonClicked(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://www.visualstudio.com/en-us/products/visual-studio-community-vs");
        }
    }
}
