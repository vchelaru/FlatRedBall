namespace Beefball.Screens
{
    public partial class GameScreen
    {
        void OnPuckVsGoalCollided(Entities.Puck puck, Entities.Goal goal)
        {
            if (goal == RightGoal)
            {
                player1Score++;
                ReactToNewScore();
            }
            else if (goal == LeftGoal)
            {
                player2Score++;
                ReactToNewScore();
            }
        }

        private void ReactToNewScore()
        {
            PlayerBall1.X = -180;
            PlayerBall1.Y = 0;
            PlayerBall1.Velocity = Microsoft.Xna.Framework.Vector3.Zero;
            PlayerBall1.Acceleration = Microsoft.Xna.Framework.Vector3.Zero;

            PlayerBall2.X = 180;
            PlayerBall2.Y = 0;
            PlayerBall2.Velocity = Microsoft.Xna.Framework.Vector3.Zero;
            PlayerBall2.Acceleration = Microsoft.Xna.Framework.Vector3.Zero;

            Puck1.X = 0;
            Puck1.Y = 0;
            Puck1.Velocity = Microsoft.Xna.Framework.Vector3.Zero;

            ScoreHud1.Score1 = player1Score;
            ScoreHud1.Score2 = player2Score;
        }
    }
}
