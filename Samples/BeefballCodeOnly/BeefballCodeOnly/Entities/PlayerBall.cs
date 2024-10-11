using FlatRedBall;
using FlatRedBall.Input;
using FlatRedBall.Math.Geometry;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeefballCodeOnly.Entities;

class PlayerBall : PositionedObject, ICollidable
{
    Circle Circle;

    Circle CooldownCircle;

    public ShapeCollection Collision { get; private set; } = new ();

    public HashSet<string> ItemsCollidedAgainst { get; set; } = new();
    public HashSet<string> LastFrameItemsCollidedAgainst { get; set; } = new();
    public HashSet<object> ObjectsCollidedAgainst { get; set; } = new();
    public HashSet<object> LastFrameObjectsCollidedAgainst { get; set; } = new();

    public I2DInput MovementInput { get; set; }
    public IPressableInput BoostInput { get; set; }

    float MovementSpeed = 340;
    float DashSpeed = 600;
    float DashFrequency = 2;

    private double lastTimeDashed = -1000;

    public Color Color
    {
        get => Circle.Color;
        set => Circle.Color = value;
    }

    public PlayerBall()
    {
        Drag = 1;

        Circle = new Circle();
        Circle.Radius = 16;
        Circle.AttachTo(this, false);
        ShapeManager.AddCircle(Circle);
        Collision.Circles.Add(Circle);

        CooldownCircle = new Circle();
        CooldownCircle.Radius = 16;
        CooldownCircle.AttachTo(this, false);
        ShapeManager.AddCircle(CooldownCircle);


        SpriteManager.AddPositionedObject(this);
    }

    public void Activity()
    {
        MovementActivity();

        DashActivity();

        CooldownVisualizationActivity();
    }


    private void MovementActivity()
    {
        if (MovementInput != null)
        {
            this.XAcceleration = MovementInput.X * MovementSpeed;
            this.YAcceleration = MovementInput.Y * MovementSpeed;
        }
    }

    bool IsBoostReady => TimeManager.CurrentScreenSecondsSince(lastTimeDashed) > DashFrequency;

    private void DashActivity()
    {
        float magnitude = MovementInput?.Magnitude ?? 0;

        bool shouldBoost = BoostInput?.WasJustPressed == true &&
            IsBoostReady &&
            magnitude > 0;

        if (shouldBoost)
        {
            lastTimeDashed = TimeManager.CurrentScreenTime;

            // dividing by magnitude tells us what X and Y would
            // be if the user were holding the input all the way in
            // the current direction.
            float normalizedX = MovementInput.X / magnitude;
            float normalizedY = MovementInput.Y / magnitude;

            XVelocity = normalizedX * DashSpeed;
            YVelocity = normalizedY * DashSpeed;
        }
    }


    private void CooldownVisualizationActivity()
    {
        if(IsBoostReady)
        {
            CooldownCircle.Visible = false;
        }
        else
        {
            CooldownCircle.Visible = true;

            float ratio = (float)(TimeManager.CurrentScreenSecondsSince(lastTimeDashed) / DashFrequency);
            CooldownCircle.Radius = Circle.Radius * ratio;
        }
    }

    public void Destroy()
    {
        ShapeManager.Remove(Circle);
        ShapeManager.Remove(CooldownCircle);

        Collision.Circles.Remove(Circle);
        SpriteManager.RemovePositionedObject(this);
    }
}
