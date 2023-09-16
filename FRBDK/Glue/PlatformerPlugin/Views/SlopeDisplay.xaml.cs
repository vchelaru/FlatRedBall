using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace FlatRedBall.PlatformerPlugin.Views
{
    /// <summary>
    /// Interaction logic for SlopeDisplay.xaml
    /// </summary>
    public partial class SlopeDisplay : UserControl, INotifyPropertyChanged
    {

        public static readonly DependencyProperty AngleProperty =
            DependencyProperty.Register("Angle", typeof(decimal), typeof(SlopeDisplay),
                new FrameworkPropertyMetadata(0m, HandleAnglePropertyChanged)
                {
                    DefaultUpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                    BindsTwoWayByDefault = true
                }
                );

        private static void HandleAnglePropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            SlopeDisplay slopeDisplay = (SlopeDisplay)dependencyObject;


            slopeDisplay.NotifyPropertyChange(nameof(Angle));
            slopeDisplay.NotifyPropertyChange(nameof(DiagramAngle));
            slopeDisplay.DegreeTextBox.Text = slopeDisplay.Angle.ToString();
        }

        public decimal Angle
        {
            get { return (decimal)GetValue(AngleProperty); }
            set { SetValue(AngleProperty, value); }
        }

        bool isInverted;
        public bool IsInverted
        {
            get { return isInverted; }
            set
            {
                if (value != isInverted)
                {
                    isInverted = value;
                    NotifyPropertyChange(nameof(IsInverted));
                    NotifyPropertyChange(nameof(DiagramAngle));
                }
            }
        }


        // needed for binding to the canvas which is inverted:
        public decimal DiagramAngle
        {
            get
            {
                // Diagram drawing in WPF uses positive angle for clockwise movement, which is
                // mathematically inverted. Therefore, if IsInverted is true, we just return the angle
                // as is.
                if(IsInverted)
                {
                    return Angle;
                }
                else
                {
                    return -Angle;
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;


        public SlopeDisplay()
        {
            InitializeComponent();


            this.DegreeTextBox.Text = "0";


            this.Diagram.DataContext = this;
        }

        void NotifyPropertyChange(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }


        private void TextBox_PreviewKeyDown_1(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                // don't handle it, let the text box display logic handle it too:
                //e.Handled = true;
                ApplyTextBoxText();
                //mTextAtStartOfEditing = TextBox.Text;

            }
        }

        private void ApplyTextBoxText()
        {
            decimal value;
            if (decimal.TryParse(this.DegreeTextBox.Text, out value))
            {
                Angle = value;
            }
        }

        private void TextBox_LostFocus_1(object sender, RoutedEventArgs e)
        {
            ApplyTextBoxText();
        }

    }
}
