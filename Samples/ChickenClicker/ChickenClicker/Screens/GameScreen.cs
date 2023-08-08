using ChickenClicker.Managers;
using ChickenClicker.ViewModels;
using FlatRedBall;
using FlatRedBall.Forms.MVVM;

namespace ChickenClicker.Screens
{
    public partial class GameScreen
    {
        void CustomInitialize()
        {
            _gameManager = new GameManager();
            SetupMvvmBindings();
            SetupButtonClickEvents();
            SetupContent();
        }

        private void SetupMvvmBindings()
        {
            ViewModel = new GameScreenViewModel();
            Forms.BindingContext = ViewModel;

            Forms.DollarsCountLabel.SetBinding(
                nameof(Forms.DollarsCountLabel.Text),
                nameof(GameScreenViewModel.DollarsLabel));

            Forms.ChickenCountLabel.SetBinding(
                nameof(Forms.ChickenCountLabel.Text),
                nameof(GameScreenViewModel.ChickensLabel));

            Forms.ChickenEggCountLabel.SetBinding(
                nameof(Forms.ChickenEggCountLabel.Text),
                nameof(GameScreenViewModel.ChickenEggsLabel));

            Forms.ChickenCostLabel.SetBinding(
                nameof(Forms.ChickenCostLabel.Text),
                nameof(GameScreenViewModel.CurrentChickenCostLabel));

            Forms.OstrichCountLabel.SetBinding(
                nameof(Forms.OstrichCountLabel.Text),
                nameof(GameScreenViewModel.OstrichesLabel));

            Forms.OstrichEggCountLabel.SetBinding(
                nameof(Forms.OstrichEggCountLabel.Text),
                nameof(GameScreenViewModel.OstrichEggsLabel));

            Forms.OstrichCostLabel.SetBinding(
                nameof(Forms.OstrichCostLabel.Text),
                nameof(GameScreenViewModel.CurrentOstrichCostLabel));

            Forms.BuyOstrichButton.SetBinding(
                nameof(Forms.BuyOstrichButton.IsVisible),
                nameof(GameScreenViewModel.OstrichesUnlocked));

            Forms.OstrichCountLabel.SetBinding(
                nameof(Forms.OstrichCountLabel.IsVisible),
                nameof(GameScreenViewModel.OstrichesUnlocked));

            Forms.OstrichEggCountLabel.SetBinding(
                nameof(Forms.OstrichEggCountLabel.IsVisible),
                nameof(GameScreenViewModel.OstrichesUnlocked));

            Forms.OstrichCostLabel.SetBinding(
                nameof(Forms.OstrichCostLabel.IsVisible),
                nameof(GameScreenViewModel.OstrichesUnlocked));
        }

        private void SetupContent()
        {
            Forms.OstrichCountLabel.IsVisible = false;
            Forms.OstrichEggCountLabel.IsVisible = false;
            Forms.BuyOstrichButton.IsVisible = false;
            Forms.OstrichCostLabel.IsVisible = false;
        }

        void CustomActivity(bool firstTimeCalled)
        {
            _gameManager.Update(TimeManager.SecondDifference);
            UpdateProperties();
        }

        private void SetupButtonClickEvents()
        {
            Forms.SellEggsButton.Click += (not, used) => _gameManager.SellEggs();
            Forms.BuyChickenButton.Click += (not, used) => _gameManager.BuyChicken();
            Forms.BuyOstrichButton.Click += (not, used) => _gameManager.BuyOstrich();
        }

        private void UpdateProperties()
        {
            ViewModel.Chickens = _gameManager.Chickens;
            ViewModel.ChickenEggs = _gameManager.ChickenEggs;

            ViewModel.Ostriches = _gameManager.Ostriches;
            ViewModel.OstrichEggs = _gameManager.OstrichEggs;

            ViewModel.Dollars = _gameManager.Dollars;

            ViewModel.CurrentChickenCost = _gameManager.CalculateCurrentChickenCost();
            ViewModel.CurrentOstrichCost = _gameManager.CalculateCurrentOstrichCost();

            ViewModel.OstrichesUnlocked = _gameManager.OstrichesUnlocked;
        }

        void CustomDestroy()
        {

        }

        static void CustomLoadStaticContent(string contentManagerName)
        { 
        
        }

        private GameScreenViewModel ViewModel;
        private GameManager _gameManager;
    }
}
