using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlatRedBall.Forms.MVVM;
using Shouldly;

namespace EngineUnitTests.Gum.Wireframe;
public class GraphicalUiElementTests
{
    // Since FRB has its own binding implementation, it has its own tests.

    [Fact]
    public void DependsOn_WithTwoPrameters_ShouldBindProperly()
    {
        DerivedGue element = new();
        OuterViewModel viewModel = new ();

        element.BindingContext = viewModel;
        element.SetBinding(nameof(element.IntProperty), nameof(viewModel.InnerIntProperty));

        viewModel.InnerViewModel.IntProperty = 4321;

        element.IntProperty.ShouldBe(4321);
    }


    class DerivedGue : global::Gum.Wireframe.GraphicalUiElement
    {
        public int IntProperty { get; set; } 
    }


    class TestViewModel : ViewModel
    {
        public int IntProperty
        {
            get => Get<int>();
            set => Set(value);
        }
    }

    class OuterViewModel : ViewModel
    {
        public TestViewModel InnerViewModel
        {
            get => Get<TestViewModel>();
            set => Set(value);
        }

        [DependsOn(nameof(InnerViewModel), nameof(InnerViewModel.IntProperty))]
        public int InnerIntProperty => InnerViewModel.IntProperty;

        public OuterViewModel()
        {
            InnerViewModel = new TestViewModel();
        }
    }


}
