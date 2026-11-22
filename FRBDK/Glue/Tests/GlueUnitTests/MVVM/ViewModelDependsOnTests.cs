using System.Collections.Generic;
using FlatRedBall.Glue.MVVM;
using Shouldly;
using Xunit;

namespace GlueUnitTests.MVVM;

/// <summary>
/// ViewModel builds its DependsOn map once per type and shares it between instances, so each instance
/// must still notify its dependent properties, exactly once per change.
/// </summary>
public class ViewModelDependsOnTests
{
    class TestViewModel : ViewModel
    {
        public int Count
        {
            get => Get<int>();
            set => Set(value);
        }

        [DependsOn(nameof(Count))]
        public string CountText => Count.ToString();
    }

    static List<string> RecordNotifications(ViewModel viewModel)
    {
        var names = new List<string>();
        viewModel.PropertyChanged += (_, e) => names.Add(e.PropertyName);
        return names;
    }

    [Fact]
    public void SettingAProperty_NotifiesItsDependentsOnce_OnEveryInstance()
    {
        var first = new TestViewModel();
        var second = new TestViewModel();
        var firstNames = RecordNotifications(first);
        var secondNames = RecordNotifications(second);

        first.Count = 1;
        second.Count = 2;

        firstNames.ShouldBe(new[] { nameof(TestViewModel.Count), nameof(TestViewModel.CountText) });
        secondNames.ShouldBe(new[] { nameof(TestViewModel.Count), nameof(TestViewModel.CountText) });
    }
}
