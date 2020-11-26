using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.Utilities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Controls;

namespace FlatRedBall.Glue.Controls
{
    public class AddScreenWindow : CustomizableTextInputWindow
    {
        public IReadOnlyCollection<UserControl> UserControlChildren
        {
            get
            {
                var listToReturn = new List<UserControl>();

                var uiElements = AboveTextBoxStackPanel.Children.Where(item => item is UserControl);
                foreach (var element in uiElements)
                {
                    listToReturn.Add(element as UserControl);
                }

                uiElements = BelowTextBoxStackPanel.Children.Where(item => item is UserControl);
                foreach (var element in uiElements)
                {
                    listToReturn.Add(element as UserControl);
                }


                return listToReturn;
            }
        }

        public AddScreenWindow() : base()
        {
            Width = 500;
        }
    }
}
