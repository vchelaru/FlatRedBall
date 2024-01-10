using FlatRedBall.Forms.MVVM;

namespace ChickenClicker.ViewModels
{
    internal class GameScreenViewModel : ViewModel
    {
        public int Chickens
        {
            get => Get<int>();
            set => Set(value);
        }

        [DependsOn(nameof(Chickens))]
        public string ChickensLabel => $"Chickens: {Chickens}";

        public double CurrentChickenCost
        {
            get => Get<double>();
            set => Set(value);
        }

        [DependsOn(nameof(CurrentChickenCost))]
        public string CurrentChickenCostLabel => $"Cost: {CurrentChickenCost}";

        public int ChickenEggs
        {
            get => Get<int>();
            set => Set(value);
        }

        [DependsOn(nameof(ChickenEggs))]
        public string ChickenEggsLabel => $"Chicken Eggs: {ChickenEggs}";

        public int Ostriches
        {
            get => Get<int>();
            set => Set(value);
        }

        [DependsOn(nameof(Ostriches))]
        public string OstrichesLabel => $"Ostriches: {Ostriches}";

        public double CurrentOstrichCost
        {
            get => Get<double>();
            set => Set(value);
        }

        [DependsOn(nameof(CurrentOstrichCost))]
        public string CurrentOstrichCostLabel => $"Cost: {CurrentOstrichCost}";

        public int OstrichEggs
        {
            get => Get<int>();
            set => Set(value);
        }

        [DependsOn(nameof(OstrichEggs))]
        public string OstrichEggsLabel => $"Ostrich Eggs: {OstrichEggs}";

        public int Dollars
        {
            get => Get<int>();
            set => Set(value);
        }

        [DependsOn(nameof(Dollars))]
        public string DollarsLabel => $"Dollars: {Dollars}";

        public bool OstrichesUnlocked
        {
            get => Get<bool>();
            set => Set(value);
        }
    }
}
