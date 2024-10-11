using BeefballCodeOnly.Entities;
using FlatRedBall.Input;
using FlatRedBall.Math;
using FlatRedBall.Math.Collision;
using FlatRedBall.Math.Geometry;
using FlatRedBall.Screens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeefballCodeOnly.Screens;

class GameScreen : Screen
{
    PositionedObjectList<PlayerBall> PlayerBallList = new();
    PositionedObjectList<Puck> PuckList = new();
    PositionedObjectList<Goal> GoalList = new();

    Goal LeftGoal;
    Goal RightGoal;

    PlayerBall PlayerBall1;
    PlayerBall PlayerBall2;

    Puck Puck1;

    ScoreHud ScoreHud;

    int player1Score = 0;
    int player2Score = 0;

    ShapeCollection Walls = new();

    public override void Initialize(bool addToManagers)
    {
        PlayerBall1 = new PlayerBall();
        PlayerBall1.X = -180;
        PlayerBallList.Add(PlayerBall1);

        PlayerBall2 = new PlayerBall();
        PlayerBall2.X = 180;
        PlayerBall2.Color = Microsoft.Xna.Framework.Color.Cyan;
        PlayerBallList.Add(PlayerBall2);

        Puck1 = new Puck();
        PuckList.Add(Puck1);

        LeftGoal = new Goal();
        LeftGoal.X = 410;
        GoalList.Add(LeftGoal);

        RightGoal = new Goal();
        RightGoal.X = -410;
        GoalList.Add(RightGoal);

        ScoreHud = new ScoreHud();
        ScoreHud.Score1 = player1Score;
        ScoreHud.Score2 = player2Score;

        AssignInput(PlayerBall1, PlayerBall2);
        CreateWalls();
        CreateCollisionRelationships();
    }

    private void CreateWalls()
    {
        var rectangle = ShapeManager.AddAxisAlignedRectangle();
        rectangle.Width = 800;
        rectangle.Height = 30;
        rectangle.Y = 300;
        Walls.AxisAlignedRectangles.Add(rectangle);

        rectangle = ShapeManager.AddAxisAlignedRectangle();
        rectangle.Width = 800;
        rectangle.Height = 30;
        rectangle.Y = -300;
        Walls.AxisAlignedRectangles.Add(rectangle);

        rectangle = ShapeManager.AddAxisAlignedRectangle();
        rectangle.Width = 30;
        rectangle.Height = 200;
        rectangle.X = -400;
        rectangle.Y = 200;
        Walls.AxisAlignedRectangles.Add(rectangle);

        rectangle = ShapeManager.AddAxisAlignedRectangle();
        rectangle.Width = 30;
        rectangle.Height = 200;
        rectangle.X = 400;
        rectangle.Y = 200;
        Walls.AxisAlignedRectangles.Add(rectangle);

        rectangle = ShapeManager.AddAxisAlignedRectangle();
        rectangle.Width = 30;
        rectangle.Height = 200;
        rectangle.X = -400;
        rectangle.Y = -200;
        Walls.AxisAlignedRectangles.Add(rectangle);

        rectangle = ShapeManager.AddAxisAlignedRectangle();
        rectangle.Width = 30;
        rectangle.Height = 200;
        rectangle.X = 400;
        rectangle.Y = -200;
        Walls.AxisAlignedRectangles.Add(rectangle);
    }

    private void CreateCollisionRelationships()
    {
        var playerVsWalls = CollisionManager.Self.CreateRelationship(PlayerBallList, Walls);
        playerVsWalls.SetBounceCollision(0, 1, 1);

        var puckVsWalls = CollisionManager.Self.CreateRelationship(PuckList, Walls);
        puckVsWalls.SetBounceCollision(0, 1, 1);

        var playerVsPuck = CollisionManager.Self.CreateRelationship(PlayerBallList, PuckList);
        playerVsPuck.SetBounceCollision(1, .3f, 1);

        var playerVsPlayer = CollisionManager.Self.CreateRelationship(PlayerBallList, PlayerBallList);
        playerVsPlayer.SetBounceCollision(1, 1, 1);

        var playerVsGoal = CollisionManager.Self.CreateRelationship(PlayerBallList, GoalList);
        playerVsGoal.SetBounceCollision(0, 1, 1);

        var puckVsGoal = CollisionManager.Self.CreateRelationship(PuckList, GoalList);
        puckVsGoal.CollisionOccurred += (puck, goal) =>
        {
            if (goal == LeftGoal)
            {
                player2Score++;
                ReactToNewScore();
            }
            else if (goal == RightGoal)
            {
                player1Score++;
                ReactToNewScore();
            }
        };
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

        ScoreHud.Score1 = player1Score;
        ScoreHud.Score2 = player2Score;
    }

    private static void AssignInput(PlayerBall PlayerBall1, PlayerBall PlayerBall2)
    {
        if (InputManager.Xbox360GamePads[0].IsConnected)
        {
            PlayerBall1.MovementInput =
                InputManager.Xbox360GamePads[0].LeftStick;
            PlayerBall1.BoostInput =
                InputManager.Xbox360GamePads[0].GetButton(Xbox360GamePad.Button.A);
        }
        else
        {
            PlayerBall1.MovementInput =
                InputManager.Keyboard.Get2DInput(Microsoft.Xna.Framework.Input.Keys.A,
                Microsoft.Xna.Framework.Input.Keys.D,
                Microsoft.Xna.Framework.Input.Keys.W,
                Microsoft.Xna.Framework.Input.Keys.S);
            PlayerBall1.BoostInput =
                InputManager.Keyboard.GetKey(Microsoft.Xna.Framework.Input.Keys.B);
        }

        if (InputManager.Xbox360GamePads[1].IsConnected)
        {
            PlayerBall2.MovementInput =
                InputManager.Xbox360GamePads[1].LeftStick;
            PlayerBall2.BoostInput =
                InputManager.Xbox360GamePads[1].GetButton(Xbox360GamePad.Button.A);
        }
        else
        {
            PlayerBall2.MovementInput =
                InputManager.Keyboard.Get2DInput(Microsoft.Xna.Framework.Input.Keys.Left,
                Microsoft.Xna.Framework.Input.Keys.Right,
                Microsoft.Xna.Framework.Input.Keys.Up,
                Microsoft.Xna.Framework.Input.Keys.Down);
            PlayerBall2.BoostInput =
                InputManager.Keyboard.GetKey(Microsoft.Xna.Framework.Input.Keys.RightShift);
        }
    }

    public override void Activity(bool firstTimeCalled)
    {

        foreach(var playerBall in PlayerBallList)
        {
            playerBall.Activity();
        }

        base.Activity(firstTimeCalled);
    }

    public override void Destroy()
    {
        while(PlayerBallList.Count > 0)
        {
            PlayerBallList[0].Destroy();
        }

        while (PuckList.Count > 0)
        {
            PuckList[0].Destroy();
        }

        while (GoalList.Count > 0)
        {
            GoalList[0].Destroy();
        }

        ScoreHud.Destroy();

        base.Destroy();
    }

}
