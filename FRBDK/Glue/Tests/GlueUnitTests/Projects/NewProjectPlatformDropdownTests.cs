using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using Npc;
using Shouldly;
using Xunit;

namespace GlueUnitTests.Projects;

// The platform dropdown renders a row's engine version as a group header and its OS/framework detail as
// a muted second line, so the row itself can stay short. Neither of those survives into the closed
// ComboBox, which draws the selected item with no group header and only one line of room - the
// ItemTemplate has a trigger that swaps in the qualified name for that case. It is invisible in the
// XAML's happy path and silently degrades to a short, version-less name if it stops matching, so it
// gets driven through real WPF layout rather than trusted. See GitHub issue #2127.
public class NewProjectPlatformDropdownTests
{
    [StaFact]
    public void ClosedPlatformDropdown_ShouldShowTheEngineVersionWithTheProjectType()
    {
        var selectionBoxText = RenderSelectionBoxText();

        selectionBoxText.ShouldContain(
            $"{PlatformProjectInfo.Frb1Category} - Desktop - MonoGame",
            $"The closed dropdown shows '{string.Join("' / '", selectionBoxText)}', which does not say " +
            "which engine version the selected template builds.");
    }

    // The muted detail line is what lets the row be short; drawing it in the closed box too would make
    // the single-line ComboBox two lines tall.
    [StaFact]
    public void ClosedPlatformDropdown_ShouldNotShowTheDetailLine()
    {
        RenderSelectionBoxText().ShouldNotContain(text => text.Contains(".NET 9"));
    }

    [StaFact]
    public void PlatformDropdown_ShouldGroupByEngineVersion()
    {
        var window = new MainWindow();

        var groupNames = window.ViewModel.GroupedProjects.Groups
            .OfType<CollectionViewGroup>()
            .Select(group => (string)group.Name)
            .ToList();

        groupNames.ShouldBe(new[]
        {
            PlatformProjectInfo.Frb1Category,
            PlatformProjectInfo.Frb2Category,
            PlatformProjectInfo.OtherCategory,
        });
    }

    /// <summary>
    /// Every line of text the closed platform ComboBox actually draws. The window has to be shown for
    /// WPF to build a visual tree at all - Measure/Arrange alone leave it empty, which reads as a
    /// passing "the detail line is absent" while proving nothing - so it is shown far off-screen and
    /// closed again rather than flashed onto whoever is running the suite.
    /// </summary>
    static List<string> RenderSelectionBoxText()
    {
        var window = new MainWindow
        {
            WindowStartupLocation = WindowStartupLocation.Manual,
            Left = -32000,
            Top = -32000,
            ShowInTaskbar = false,
        };

        try
        {
            window.Show();
            window.UpdateLayout();

            // IsVisible, not just Text: a collapsed TextBlock keeps whatever it was bound to, so reading
            // every TextBlock's text would report the detail line as drawn when it is hidden.
            var text = TextBlocksIn(window.PlatformComboBox)
                .Where(item => item.IsVisible)
                .Select(item => item.Text)
                .Where(item => !string.IsNullOrEmpty(item))
                .ToList();

            text.ShouldNotBeEmpty("The closed dropdown drew no text at all, so nothing below this is " +
                "actually asserting anything about what it shows.");

            return text;
        }
        finally
        {
            window.Close();
        }
    }

    static IEnumerable<TextBlock> TextBlocksIn(DependencyObject root)
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);

            if (child is TextBlock textBlock)
            {
                yield return textBlock;
            }

            foreach (var descendant in TextBlocksIn(child))
            {
                yield return descendant;
            }
        }
    }
}
