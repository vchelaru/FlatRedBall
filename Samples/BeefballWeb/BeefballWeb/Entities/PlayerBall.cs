using FlatRedBall.Input;

namespace BeefballWeb.Entities
{
    public partial class PlayerBall
    {
        public I2DInput MovementInput { get; set; }
        public IPressableInput BoostInput { get; set; }

        // Set a large negative number so that dashing can happen immediately
        private double lastTimeDashed = -1000;

        /// <summary>
        /// Initialization logic which is executed only one time for this Entity (unless the Entity is pooled).
        /// This method is called when the Entity is added to managers. Entities which are instantiated but not
        /// added to managers will not have this method called.
        /// </summary>
        private void CustomInitialize()
        {

        }

        private void CustomActivity()
        {
            MovementActivity();
            DashActivity();
        }

        private void MovementActivity()
        {
            if (MovementInput != null)
            {
                XAcceleration = MovementInput.X * MovementSpeed;
                YAcceleration = MovementInput.Y * MovementSpeed;
            }
        }

        private void DashActivity()
        {
            float magnitude = MovementInput.Magnitude;

            bool shouldBoost = BoostInput.WasJustPressed &&
                FlatRedBall.Screens.ScreenManager.CurrentScreen.PauseAdjustedSecondsSince(lastTimeDashed) > DashFrequency &&
                magnitude > 0;

            if (shouldBoost)
            {
                lastTimeDashed =
                    FlatRedBall.Screens.ScreenManager.CurrentScreen.PauseAdjustedCurrentTime;

                // dividing by magnitude tells us what X and Y would
                // be if the user were holding the input all the way in
                // the current direction.
                float normalizedX = MovementInput.X / magnitude;
                float normalizedY = MovementInput.Y / magnitude;

                XVelocity = normalizedX * DashSpeed;
                YVelocity = normalizedY * DashSpeed;

                CurrentDashCategoryState = DashCategory.Tired;
                InterpolateToState(DashCategory.Rested, DashFrequency);
            }
        }

        private void CustomDestroy()
        {

        }

        private static void CustomLoadStaticContent(string contentManagerName)
        {

        }
    }
}
