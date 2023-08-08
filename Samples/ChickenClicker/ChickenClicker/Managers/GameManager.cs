using System;

namespace ChickenClicker.Managers
{
    internal class GameManager
    {
        public bool OstrichesUnlocked => (Chickens >= 10);

        public int Dollars { get; private set; }
        public int Chickens { get; private set; }
        public int Ostriches { get; private set; }
        public int ChickenEggs { get; private set; }
        public int OstrichEggs { get; private set; }

        public GameManager()
        {
            Chickens = 1;
        }

        public void Update(float frameTimePassedInSeconds)
        {
            _accumulatedSeconds += frameTimePassedInSeconds;
            if (_accumulatedSeconds > 1)
            {
                _accumulatedSeconds -= 1;
                SecondTick();
            }
        }

        public void SellEggs()
        {
            Dollars += ChickenEggs;
            ChickenEggs = 0;

            Dollars += OstrichEggs * 20;
            OstrichEggs = 0;
        }

        public void BuyChicken()
        {
            var wholeCost = (int)Math.Ceiling(CalculateCurrentChickenCost());
            if (Dollars >= wholeCost)
            {
                Dollars -= wholeCost;
                Chickens++;
            }
        }

        public void BuyOstrich()
        {
            var wholeCost = (int)Math.Ceiling(CalculateCurrentOstrichCost());
            if (Dollars >= wholeCost)
            {
                Dollars -= wholeCost;
                Ostriches++;
            }
        }

        internal double CalculateCurrentChickenCost()
        {
            var baseCost = 10;
            var multiplier = 1.1;

            return baseCost * (Math.Pow(multiplier, Chickens));
        }

        internal double CalculateCurrentOstrichCost()
        {
            var baseCost = 100;
            var multiplier = 1.1;

            return baseCost * (Math.Pow(multiplier, Ostriches));
        }

        private void SecondTick()
        {
            ChickenEggs += Chickens;

            _accumulatedOstrichSeconds++;
            if (_accumulatedOstrichSeconds >= 10)
            {
                _accumulatedOstrichSeconds -= 10;
                OstrichEggs += Ostriches;
            }
        }

        private float _accumulatedSeconds;
        private int _accumulatedOstrichSeconds;
    }
}
