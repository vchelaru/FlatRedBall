using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

using FlatRedBall;
using FlatRedBall.Input;
using FlatRedBall.Instructions;
using FlatRedBall.AI.Pathfinding;
using FlatRedBall.Graphics.Animation;
using FlatRedBall.Graphics.Particle;
using FlatRedBall.Math.Geometry;
using FlatRedBall.Localization;
using GlueTestProject.Forms.Controls;
using FlatRedBall.Forms.Controls;
using GlueTestProject.TestFramework;
using FlatRedBall.Forms.MVVM;
using FlatRedBall.Screens;
using System.Net.NetworkInformation;
using GlueTestProject.GumRuntimes;

namespace GlueTestProject.Screens;

#region View Models

class TestViewModel : ViewModel
{
    public bool IsChecked
    {
        get => Get<bool>();
        set => Set(value);
    }
}

class GumPageViewModel : ViewModel
{
    public bool IsFirstChecked
    {
        get => Get<bool>();
        set => Set(value);
    }

    public bool IsSecondChecked
    {
        get => Get<bool>();
        set => Set(value);
    }

    public bool IsThirdChecked
    {
        get => Get<bool>();
        set => Set(value);
    }

    public bool IsFourthChecked
    {
        get => Get<bool>();
        set => Set(value);
    }

    public bool IsFifthChecked
    {
        get => Get<bool>();
        set => Set(value);
    }

    public decimal DecimalValueForTextBox { get => Get<decimal>(); set => Set(value); }
    public double DoubleValueForTextBox { get => Get<double>(); set => Set(value); }
    public float FloatValueForTextBox { get => Get<float>(); set => Set(value); }
    public int IntValueForTextBox { get => Get<int>(); set => Set(value); }
    public byte ByteValueForTextBox { get => Get<byte>(); set => Set(value); }
}

public class MethodCallingViewModel : ViewModel
{
    public event Action TestEvent;

    public void RaiseTestEvent() => TestEvent?.Invoke();
}

#endregion

public partial class FormsScreen
{
    CustomUserControl control;

    void CustomInitialize()
    {
        DialogBox_ShouldHaveMultiplePages_WhenEnteringLongText();
        Forms_ShouldRemoveInternalBinding_WhenBindingContextChanges();

        Binding_ShouldCascadeToChildren_WhenChildrenAreAddedToParent();

        Binding_ShouldRaiseEvents_WhenBindingToEvents();

        DerivedControls_ShouldHaveVisualCreated_WhenInstantiated();

        DialogBox_ShouldWrapTextProperly_WhenShowingMultiplePages();

        ListBox_ShouldRaiseSelectectionChanged_WhenSelectedObjectIsSet();
        ListBox_ShouldShowItem_WhenSelectingByIndex();

        RadioButton_SettingIsChecked_ShouldUncheckOtherRadioButtons();

        PreFilledListBox_ShouldHaveListBoxItems_WhenAddedInGum();

        TextBox_ShouldBindCorrectly_WhenBoundToNumericValues();

    }

    private void Binding_ShouldRaiseEvents_WhenBindingToEvents()
    {
        var instance = new EventBindingComponentRuntime();
        instance.SetBinding(
            nameof(instance.BoundEventHandler),
            nameof(MethodCallingViewModel.TestEvent));
        var vm = new MethodCallingViewModel();
        instance.BindingContext = vm;

        instance.TimesEventRaised.ShouldBe(0);
        vm.RaiseTestEvent();
        instance.TimesEventRaised.ShouldBe(1);

        // now test it with indirect binding context


        var container = new ContainerRuntime();
        container.BindingContext = vm;
        var childInstance = new EventBindingComponentRuntime();
        childInstance.SetBinding(
            nameof(instance.BoundEventHandler),
            nameof(MethodCallingViewModel.TestEvent));
        container.Children.Add(childInstance);

        childInstance.TimesEventRaised.ShouldBe(0);
        vm.RaiseTestEvent();
        childInstance.TimesEventRaised.ShouldBe(1);
        container.Children.Remove(childInstance);
        vm.RaiseTestEvent();
        childInstance.TimesEventRaised.ShouldBe(1);


    }

    private void Binding_ShouldCascadeToChildren_WhenChildrenAreAddedToParent()
    {
        var stackPanel = new StackPanel();

        var viewModel = new ViewModel();
        var buttonBefore = new Button();
        stackPanel.AddChild(buttonBefore);
        stackPanel.BindingContext = viewModel;

        buttonBefore.BindingContext.ShouldBe(viewModel);

        var button = new Button();
        button.BindingContext.ShouldBe(null);

        stackPanel.AddChild(button);
        button.BindingContext.ShouldBe(viewModel);
    }

    private void PreFilledListBox_ShouldHaveListBoxItems_WhenAddedInGum()
    {
        Forms.PreFilledListBox.ListBoxItems.Count.ShouldBeGreaterThan(0, "because items were added in Gum, so we should have those here too");

        Forms.PreFilledListBox.ListBoxItems[0].IsSelected = true;
        Forms.PreFilledListBox.ListBoxItems[1].IsSelected = true;

        Forms.PreFilledListBox.ListBoxItems[0].IsSelected.ShouldBe(false, "because selecting a different item should deselect this");
    }

    private void ListBox_ShouldShowItem_WhenSelectingByIndex()
    {
        var listBox = new ListBox();

        listBox.Width = 100;
        listBox.Height = 100;

        for (int i = 0; i < 100; i++)
        {
            // intentionally add the same item multiple times
            listBox.Items.Add(0);
        }

        listBox.VerticalScrollBarValue.ShouldBe(0);

        listBox.ScrollIndexIntoView(50);
        listBox.VerticalScrollBarValue.ShouldNotBe(0, "because scrolling to the 50th item should change the vertical scroll value");
    }

    private void DerivedControls_ShouldHaveVisualCreated_WhenInstantiated()
    {
        // Test if derived controls automatically get visuals from their base if the derived doesn't exist...
        control = new CustomUserControl();
        control.Visual.AddToManagers();
    }

    private void DialogBox_ShouldHaveMultiplePages_WhenEnteringLongText()
    {
        var dialogBox = Forms.DialogBoxInstance;

        var dialogBoxString = string.Empty;
        for (int i = 0; i < 30; i++)
        {
            dialogBoxString += "This is a long string.\n";
        }

        var textInstance = dialogBox.Visual.GetGraphicalUiElementByName("TextInstance");
        textInstance.HeightUnits.ShouldNotBe(Gum.DataTypes.DimensionUnitType.RelativeToChildren);
        textInstance.TextOverflowVerticalMode.ShouldBe(RenderingLibrary.Graphics.TextOverflowVerticalMode.TruncateLine);

        dialogBox.Show(dialogBoxString);
        dialogBox.PagesRemaining.ShouldNotBe(0, "because this text should be long enough to require multiple pages");

        dialogBox.Dismiss();

    }

    private void DialogBox_ShouldWrapTextProperly_WhenShowingMultiplePages()
    {
        var dialogBox = Forms.DialogBoxInstance;

        var styledString =
            "This is [Color=Orange]some really[/Color] long[Color=Pink] text[/Color]. " +
            "[Color=Purple]We[/Color] want to show long text so that it line wraps[Color=Cyan] " +
            "and[/Color] so that it has [Color=Green]enough[/Color] text to fill an " +
            "[Color=Yellow]entire page[/Color]. The DialogBox control should automatically " +
            "detect if the text is too long for a single page and it should break it up into " +
            "multiple pages.You can advance this dialog by clicking on it with the " +
            "[Color=Blue]mouse[/Color] or by pressing the [Color=Gold]space bar[/Color] " +
            "on the keyboard.";
        dialogBox.LettersPerSecond = null;

        var gumObject = dialogBox.Visual;
        var gue = gumObject.GetGraphicalUiElementByName("TextInstance");
        gue.TextOverflowVerticalMode = RenderingLibrary.Graphics.TextOverflowVerticalMode.TruncateLine;
        dialogBox.Show(styledString);

        // As of Feb 22, 2024 this is an old .glux so it doesn't codegen the height limit:

        var textRenderable = gue.Component as RenderingLibrary.Graphics.Text;

        textRenderable.WrappedText.Count.ShouldBe(5);

        var areAnyBlank = textRenderable.WrappedText.Any(item => string.IsNullOrEmpty(item));
        areAnyBlank.ShouldNotBe(true, "because paging should not result in any blank lines");
    }

    private void Forms_ShouldRemoveInternalBinding_WhenBindingContextChanges()
    {
        int timesCalled = 0;
        var vm = new TestViewModel();
        vm.PropertyChanged += (not, used) =>
        {
            timesCalled++;
        };

        timesCalled.ShouldBe(0);

        // Stack it a few deep to make sure all works okay
        var stack = new StackPanel();
        var innerStack = new StackPanel();
        var checkBox = new CheckBox();
        checkBox.SetBinding(nameof(checkBox.IsChecked), nameof(TestViewModel.IsChecked));
        stack.AddChild(innerStack);
        innerStack.AddChild(checkBox);
        stack.Visual.AddToManagers();

        stack.BindingContext = vm;

        timesCalled.ShouldBe(0);

        checkBox.IsChecked = true;

        timesCalled.ShouldBe(1);

        stack.Visual.RemoveFromManagers();

        stack.Visual.BindingContext.ShouldBe(null);

        timesCalled.ShouldBe(1);

        checkBox.IsChecked = false;

        timesCalled.ShouldBe(1);

        stack.Visual.AddToManagers();
        stack.Visual.BindingContext = vm;

        checkBox.IsChecked = !checkBox.IsChecked;

        timesCalled.ShouldBe(2);

        stack.Visual.RemoveFromManagers();

    }

    private static void ListBox_ShouldRaiseSelectectionChanged_WhenSelectedObjectIsSet()
    {
        var listBox = new ListBox();

        var listBoxItem = new ListBoxItem();
        listBox.Items.Add(listBoxItem);

        listBox.Items.Add(1);
        listBox.Items.Add(2);

        object selectedItem = null;

        listBox.SelectionChanged += (not, used) =>
        {
            selectedItem = listBox.SelectedObject;
        };

        listBox.SelectedObject = listBoxItem;
        listBox.SelectedObject.ShouldNotBe(null);
        selectedItem.ShouldBe(listBoxItem, "because the SelectionChanged should be raised");

        listBox.SelectedObject = null;
        selectedItem.ShouldBe(null, "because the SelectionChanged should be raised");
    }

    private void RadioButton_SettingIsChecked_ShouldUncheckOtherRadioButtons()
    {
        var stackPanel = new StackPanel();

        var radioButton1 = new RadioButton();
        stackPanel.AddChild(radioButton1);

        var radioButton2 = new RadioButton();
        stackPanel.AddChild(radioButton2);

        radioButton1.IsChecked = true;
        radioButton2.IsChecked.ShouldBe(false);

        radioButton2.IsChecked = true;
        radioButton1.IsChecked.ShouldBe(false, "because checking the 2nd should uncheck the first");

        radioButton1.Visual.RemoveFromManagers();
        radioButton2.Visual.RemoveFromManagers();
    }

    private void TextBox_ShouldBindCorrectly_WhenBoundToNumericValues()
    {
        var viewModel = new GumPageViewModel();
        GumScreen.BindingContext = viewModel;
        var textBox = Forms.TextBoxBoundToNumericValues;

        textBox.SetBinding(nameof(textBox.Text), nameof(GumPageViewModel.DecimalValueForTextBox));
        viewModel.DecimalValueForTextBox = 2m;
        textBox.Text.ShouldBe("2");
        textBox.Text = "3";
        viewModel.DecimalValueForTextBox.ShouldBe(3m);

        textBox.SetBinding(nameof(textBox.Text), nameof(GumPageViewModel.DoubleValueForTextBox));
        viewModel.DoubleValueForTextBox = 10;
        textBox.Text.ShouldBe("10");
        textBox.Text = "11";
        viewModel.DoubleValueForTextBox.ShouldBe(11);

        textBox.SetBinding(nameof(textBox.Text), nameof(GumPageViewModel.FloatValueForTextBox));
        viewModel.FloatValueForTextBox = 12f;
        textBox.Text.ShouldBe("12");
        textBox.Text = "13";
        viewModel.FloatValueForTextBox.ShouldBe(13f);

        textBox.SetBinding(nameof(textBox.Text), nameof(GumPageViewModel.IntValueForTextBox));
        viewModel.IntValueForTextBox = 14;
        textBox.Text.ShouldBe("14");
        textBox.Text = "15";
        viewModel.IntValueForTextBox.ShouldBe(15);

        textBox.SetBinding(nameof(textBox.Text), nameof(GumPageViewModel.ByteValueForTextBox));
        viewModel.ByteValueForTextBox = 16;
        textBox.Text.ShouldBe("16");
        textBox.Text = "17";
        viewModel.ByteValueForTextBox.ShouldBe((byte)17);

    }


    void CustomActivity(bool firstTimeCalled)
    {
        IsActivityFinished = true;

    }

    void CustomDestroy()
    {
        control.Visual.RemoveFromManagers();

    }

    static void CustomLoadStaticContent(string contentManagerName)
    {


    }

}
