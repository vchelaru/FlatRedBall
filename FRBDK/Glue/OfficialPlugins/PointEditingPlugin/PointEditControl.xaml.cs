using Microsoft.Xna.Framework;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace OfficialPluginsCore.PointEditingPlugin
{
    /// <summary>
    /// Interaction logic for PointEditControl.xaml
    /// </summary>
    public partial class PointEditControl : UserControl
    {
        List<Vector2> mData;

        public List<Vector2> Data
        {
            get
            {
                return mData;
            }
            set
            {
                mData = value;

                UpdateToData();
            }
        }

        Vector2 SelectedVector2
        {
            get
            {
                if (ListBox.SelectedItem != null)
                {
                    return (Vector2)ListBox.SelectedItem;
                }
                else
                {
                    return Vector2.Zero;
                }
            }
        }

        public event EventHandler DataChanged;


        public PointEditControl()
        {
            InitializeComponent();
        }

        private void UpdateToData()
        {
            int index = ListBox.SelectedIndex;

            ListBox.Items.Clear();

            if (mData != null)
            {
                foreach (var item in mData)
                {
                    ListBox.Items.Add(item);
                }
            }

            if (index > -1 && index < ListBox.Items.Count)
            {
                ListBox.SelectedIndex = index;
            }
        }

        private void CallDataChanged()
        {
            if (DataChanged != null)
            {
                DataChanged(this, null);
            }
        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ListBox.SelectedItem != null)
            {
                if (!XTextBox.IsFocused)
                {
                    XTextBox.Text = SelectedVector2.X.ToString();
                }
                if (!YTextBox.IsFocused)
                {
                    YTextBox.Text = SelectedVector2.Y.ToString();
                }
            }
            else
            {
                if (!XTextBox.IsFocused)
                {
                    XTextBox.Text = null;
                }
                if (!YTextBox.IsFocused)
                {
                    YTextBox.Text = null;
                }
            }
        }

        private void AddButtonClicked(object sender, RoutedEventArgs e)
        {
            if (Data != null)
            {
                Data.Add(new Vector2());

                UpdateToData();

                ListBox.SelectedIndex = ListBox.Items.Count - 1;

                CallDataChanged();
            }
        }

        private void RemoveButtonClicked(object sender, RoutedEventArgs e)
        {
            if (this.Data != null && ListBox.SelectedItem != null)
            {
                int indexToRemove = ListBox.SelectedIndex;

                this.Data.RemoveAt(indexToRemove);

                UpdateToData();

                CallDataChanged();

            }
        }

        private void XTextBox_TextChanged_1(object sender, TextChangedEventArgs e)
        {
            if (ListBox.SelectedItem != null && float.TryParse(XTextBox.Text, out float outValue))
            {
                int index = this.ListBox.SelectedIndex;

                if (index != -1)
                {
                    Vector2 vector = SelectedVector2;
                    if (outValue != vector.X)
                    {
                        vector.X = outValue;

                        Data[index] = vector;
                        UpdateToData();

                        CallDataChanged();

                    }
                }
            }
        }

        private void YTextBox_TextChanged_1(object sender, TextChangedEventArgs e)
        {
            if (ListBox.SelectedItem != null && float.TryParse(YTextBox.Text, out float outValue))
            {
                int index = this.ListBox.SelectedIndex;

                if (index != -1)
                {
                    Vector2 vector = SelectedVector2;
                    if (outValue != vector.Y)
                    {
                        vector.Y = outValue;

                        Data[index] = vector;
                        UpdateToData();

                        CallDataChanged();

                    }
                }
            }
        }
    }
}
