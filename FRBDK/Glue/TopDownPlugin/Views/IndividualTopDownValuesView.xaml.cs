using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TopDownPlugin.ViewModels;
using WpfDataUi.DataTypes;

namespace TopDownPlugin.Views
{
    /// <summary>
    /// Interaction logic for IndividualTopDownValuesView.xaml
    /// </summary>
    public partial class IndividualTopDownValuesView : UserControl
    {
        public event RoutedEventHandler XClick;

        TopDownValuesViewModel ViewModel => DataContext as TopDownValuesViewModel;

        public IndividualTopDownValuesView()
        {
            InitializeComponent();

            this.DataContextChanged += HandleDataContextChanged;
        }

        private void HandleXClick(object sender, RoutedEventArgs e)
        {
            XClick?.Invoke(this, null);
        }

        private void HandleDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            DataGrid.Categories.Clear();
            if(ViewModel != null)
            {
                var category = new MemberCategory() ;
                foreach(var kvp in ViewModel.AdditionalProperties)
                {
                    var instanceMember = new InstanceMember();

                    instanceMember.Name = kvp.Key;
                    instanceMember.CustomSetPropertyEvent += (owner, args) =>
                    {
                        var value = args.Value;
                        ViewModel.AdditionalProperties[kvp.Key].Value = value;
                        ViewModel.NotifyAdditionalPropertiesChanged();

                    };
                    instanceMember.CustomGetEvent += (owner) =>
                    {
                        return ViewModel.AdditionalProperties[kvp.Key].Value;
                    };
                    instanceMember.CustomGetTypeEvent += (owner) =>
                    {
                        return ViewModel.AdditionalProperties[kvp.Key].Type;
                    };
                    // don't set the initial value, this will happen by using the getter above

                    category.Members.Add(instanceMember);

                }
                DataGrid.Categories.Add(category);
            }
        }

        private void TextBox_KeyEnterUpdate(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                TextBox tBox = (TextBox)sender;
                DependencyProperty prop = TextBox.TextProperty;

                BindingExpression binding = BindingOperations.GetBindingExpression(tBox, prop);
                if (binding != null) { binding.UpdateSource(); }
            }
        }
    }
}
