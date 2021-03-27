using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WpfDataUi;

namespace GlueFormsCore.Controls
{
    public class StyledGroupBox : GroupBox
    {
        public StyledGroupBox()
        {
            this.Resources = MainPanelControl.ResourceDictionary;

            //this.Style = "GroupBoxStyle";

            var style = this.TryFindResource("GroupBoxStyle") as Style;
            if (style != null)
            {
                this.Style = style;
            }

            //this.BorderBrush = new SolidColorBrush(Colors.Red);
            //this.BorderThickness = new Thickness(6);
            //this.Foreground = new SolidColorBrush(Colors.Green);







            /*
             * 
             *         
        <BorderGapMaskConverter x:Key="BorderGapMaskConverter"/>
        <Style x:Key="StyledGroupBoxStyle1" TargetType="{x:Type glue:StyledGroupBox}">
            <Setter Property="BorderBrush" Value="#D5DFE5"/>
            <Setter Property="BorderThickness" Value="1"/>
            <Setter Property="Template">
                <Setter.Value>
                    <ControlTemplate TargetType="{x:Type glue:StyledGroupBox}">
                        <Grid SnapsToDevicePixels="true">
                            <Grid.ColumnDefinitions>
                                <ColumnDefinition Width="6"/>
                                <ColumnDefinition Width="Auto"/>
                                <ColumnDefinition Width="*"/>
                                <ColumnDefinition Width="6"/>
                            </Grid.ColumnDefinitions>
                            <Grid.RowDefinitions>
                                <RowDefinition Height="Auto"/>
                                <RowDefinition Height="Auto"/>
                                <RowDefinition Height="*"/>
                                <RowDefinition Height="6"/>
                            </Grid.RowDefinitions>
                            <Border Background="{TemplateBinding Background}" BorderThickness="{TemplateBinding BorderThickness}" BorderBrush="Transparent" CornerRadius="4" Grid.ColumnSpan="4" Grid.Column="0" Grid.RowSpan="3" Grid.Row="1"/>
                            <Border BorderThickness="{TemplateBinding BorderThickness}" BorderBrush="White" CornerRadius="4" Grid.ColumnSpan="4" Grid.RowSpan="3" Grid.Row="1">
                                <Border.OpacityMask>
                                    <MultiBinding Converter="{StaticResource BorderGapMaskConverter}" ConverterParameter="7">
                                        <Binding ElementName="Header" Path="ActualWidth"/>
                                        <Binding Path="ActualWidth" RelativeSource="{RelativeSource Self}"/>
                                        <Binding Path="ActualHeight" RelativeSource="{RelativeSource Self}"/>
                                    </MultiBinding>
                                </Border.OpacityMask>
                                <Border BorderThickness="{TemplateBinding BorderThickness}" BorderBrush="{TemplateBinding BorderBrush}" CornerRadius="3">
                                    <Border BorderThickness="{TemplateBinding BorderThickness}" BorderBrush="White" CornerRadius="2"/>
                                </Border>
                            </Border>
                            <Border x:Name="Header" Grid.Column="1" Padding="3,1,3,0" Grid.RowSpan="2" Grid.Row="0">
                                <ContentPresenter ContentSource="Header" RecognizesAccessKey="True" SnapsToDevicePixels="{TemplateBinding SnapsToDevicePixels}"/>
                            </Border>
                            <ContentPresenter Grid.ColumnSpan="2" Grid.Column="1" Margin="{TemplateBinding Padding}" Grid.Row="2" SnapsToDevicePixels="{TemplateBinding SnapsToDevicePixels}"/>
                        </Grid>
                    </ControlTemplate>
                </Setter.Value>
            </Setter>
        </Style>
             * 
             * 
             */





        }
    }
}
