using System;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Media;
using WpfDataUi;
using WpfDataUi.DataTypes;

namespace OfficialPlugins.Common.Controls
{
    /// <summary>
    /// Interaction logic for ColorDisplay.xaml
    /// </summary>
    public partial class ColorDisplay : UserControl, IDataUi
    {
        public ColorDisplay()
        {
            InitializeComponent();

            ColorPickerPanel.PropertyChanged += TestMe;
        }

        private void TestMe(object sender, PropertyChangedEventArgs e)
        {

        }

        InstanceMember mInstanceMember;
        Type mInstancePropertyType;

        public InstanceMember InstanceMember
        {
            get => mInstanceMember;
            set
            {
                bool instanceMemberChanged = mInstanceMember != value;
                if (mInstanceMember != null && instanceMemberChanged)
                {
                    mInstanceMember.PropertyChanged -= HandlePropertyChange;
                }
                mInstanceMember = value;
                if (mInstanceMember != null && instanceMemberChanged)
                {
                    mInstanceMember.PropertyChanged += HandlePropertyChange;
                }
                Refresh();
            }
        }
        public bool SuppressSettingProperty { get; set; }


        public void Refresh(bool forceRefreshEvenIfFocused = false)
        {
            SuppressSettingProperty = true;

            if (this.HasEnoughInformationToWork())
            {
                Type type = this.GetPropertyType();

                mInstancePropertyType = type;
            }

            object valueOnInstance;
            bool successfulGet = this.TryGetValueOnInstance(out valueOnInstance);

            if (successfulGet)
            {
                var wasSet = TrySetValueOnUi(valueOnInstance) == ApplyValueResult.Success;
            }

            this.Label.Text = InstanceMember.DisplayName;
            SuppressSettingProperty = false;
        }

        public ApplyValueResult TryGetValueOnUi(out object value)
        {
            var result = ApplyValueResult.UnknownError;
            value = null;
            if (!this.HasEnoughInformationToWork() || mInstancePropertyType == null)
            {
                result = ApplyValueResult.NotEnoughInformation;
            }
            else
            {
                if(ColorPickerPanel.SelectedColor == null)
                {
                    result = ApplyValueResult.NotSupported;
                }
                else if (mInstancePropertyType == typeof(Microsoft.Xna.Framework.Color))
                {
                    Microsoft.Xna.Framework.Color colorToReturn = new Microsoft.Xna.Framework.Color(
                        ColorPickerPanel.SelectedColor.Value.R,
                        ColorPickerPanel.SelectedColor.Value.G,
                        ColorPickerPanel.SelectedColor.Value.B,
                        ColorPickerPanel.SelectedColor.Value.A);

                    result = ApplyValueResult.Success;

                    value = colorToReturn;
                }
                else if (mInstancePropertyType == typeof(System.Drawing.Color))
                {
                    var toReturn = System.Drawing.Color.FromArgb(
                        ColorPickerPanel.SelectedColor.Value.A,
                        ColorPickerPanel.SelectedColor.Value.R,
                        ColorPickerPanel.SelectedColor.Value.G,
                        ColorPickerPanel.SelectedColor.Value.B
                        );

                    result = ApplyValueResult.Success;

                    value = toReturn;
                }
                else
                {
                    result = ApplyValueResult.NotSupported;
                }
            }

            return result;

        }

        public ApplyValueResult TrySetValueOnUi(object valueOnInstance)
        {
            if (valueOnInstance is Microsoft.Xna.Framework.Color color)
            {
                var windowsColor = new Color();
                windowsColor.A = color.A;
                windowsColor.R = color.R;
                windowsColor.G = color.G;
                windowsColor.B = color.B;

                this.ColorPickerPanel.SelectedColor = windowsColor;
                return ApplyValueResult.Success;
            }
            else if (valueOnInstance is System.Drawing.Color drawingColor)
            {
                var windowsColor = new Color();
                windowsColor.A = drawingColor.A;
                windowsColor.R = drawingColor.R;
                windowsColor.G = drawingColor.G;
                windowsColor.B = drawingColor.B;

                this.ColorPickerPanel.SelectedColor = windowsColor;
                return ApplyValueResult.Success;
            }
            else if(valueOnInstance is Color mediaColor)
            {
                this.ColorPickerPanel.SelectedColor = mediaColor;
                return ApplyValueResult.Success;
            }
            else
            {
                return ApplyValueResult.NotSupported;
            }
        }

        private void HandlePropertyChange(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(InstanceMember.Value))
            {
                this.Refresh();
            }
        }

        private void HandleColorPickerPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (SuppressSettingProperty)
            {
                return;
            }

            if (InstanceMember != null)
            {
                this.TrySetValueOnInstance();
            }
        }
    }
}
