using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall;
using FlatRedBall.Input;
using FlatRedBall.Instructions;
using FlatRedBall.AI.Pathfinding;
using FlatRedBall.Graphics.Animation;
using FlatRedBall.Graphics.Particle;
using FlatRedBall.Math.Geometry;
using Microsoft.Xna.Framework;

namespace LadderDemo.Entities;

public partial class Player
{
    AnimationController animationController;

    public IPressableInput RunInput { get; set; }

    public bool PressedUp => InputDevice.DefaultUpPressable.WasJustPressed;

    public AxisAlignedRectangle LastCollisionLadderRectange { get; set; }

    // GameScreen.cs's DoCollisionActivity nulls LastCollisionLadderRectange every frame before
    // re-running the ladder collision, so a fresh touch can reset it if the player is still on the
    // ladder. That's right while climbing through the shaft, but wrong the instant the player is
    // clamped flush at TopOfLadderY: standing ON TOP of the topmost ladder tile no longer vertically
    // overlaps it, so the collision doesn't re-fire and LastCollisionLadderRectange stays null even
    // though the player hasn't moved sideways. isOverLadder below reads from these cached bounds
    // instead, updated whenever a real ladder touch occurs but never cleared just because contact
    // paused - only stepping outside them (an actual horizontal move) counts as leaving the ladder.
    float? ladderColumnLeft;
    float? ladderColumnRight;

    public void SetIndex(int index)
    {
        switch (index)
        {
            case 0:
                SpriteInstance.AnimationChains = PlatformerAnimations;
                break;
            case 1:
                SpriteInstance.AnimationChains = P2animations;
                break;
            case 2:
                SpriteInstance.AnimationChains = P3animations;
                break;
            case 3:
                SpriteInstance.AnimationChains = P4animations;
                break;
        }
    }

    private void CustomInitialize()
    {

        animationController = new AnimationController(SpriteInstance);

        var idleLayer = new AnimationLayer();
        idleLayer.EveryFrameAction = () =>
        {
            return "CharacterIdle" + DirectionFacing;
        };
        animationController.Layers.Add(idleLayer);

        var lookUpLayer = new AnimationLayer();
        lookUpLayer.EveryFrameAction = () =>
        {
            if (VerticalInput.Value > 0)
            {
                return "CharacterLookUp" + DirectionFacing;
            }
            return null;
        };
        animationController.Layers.Add(lookUpLayer);

        var walkLayer = new AnimationLayer();
        walkLayer.EveryFrameAction = () =>
        {
            if (Velocity.X != 0)
            {
                return "CharacterWalk" + DirectionFacing;
            }
            return null;
        };
        animationController.Layers.Add(walkLayer);

        var runLayer = new AnimationLayer();
        runLayer.EveryFrameAction = () =>
        {
            if (XVelocity != 0 && RunInput.IsDown)
            {
                return "CharacterRun" + DirectionFacing;
            }
            return null;
        };
        animationController.Layers.Add(runLayer);

        var skidLayer = new AnimationLayer();
        skidLayer.EveryFrameAction = () =>
        {
            if (XVelocity != 0 && HorizontalInput.Value != 0 &&
                Math.Sign(XVelocity) != Math.Sign(HorizontalInput.Value) &&
                RunInput.IsDown)
            {
                return "CharacterSkid" + DirectionFacing;
            }
            return null;
        };
        animationController.Layers.Add(skidLayer);

        var duckLayer = new AnimationLayer();
        duckLayer.EveryFrameAction = () =>
        {
            if (VerticalInput.Value < 0) { return "CharacterDuck" + DirectionFacing; }
            return null;
        }; animationController.Layers.Add(duckLayer); var fallLayer = new AnimationLayer(); fallLayer.EveryFrameAction = () =>
        {
            if (IsOnGround == false)
            {
                return "CharacterFall" + DirectionFacing;
            }
            return null;
        };
        animationController.Layers.Add(fallLayer);

        var jumpLayer = new AnimationLayer();
        jumpLayer.EveryFrameAction = () =>
        {
            if (IsOnGround == false && YVelocity > 0)
            {
                return "CharacterJump" + DirectionFacing;
            }
            return null;
        };
        animationController.Layers.Add(jumpLayer);

        var runJump = new AnimationLayer();
        runJump.EveryFrameAction = () =>
        {
            if (IsOnGround == false && RunInput.IsDown)
            {
                return "CharacterRunJump" + DirectionFacing;
            }
            return null;
        };
        animationController.Layers.Add(runJump);

        var climb = new AnimationLayer();
        climb.EveryFrameAction = () =>
        {
            if (CurrentMovement.CanClimb)
            {
                if (YVelocity == 0)
                {
                    return "CharacterClimbRearIdle";
                }
                else
                {
                    return "CharacterClimbRear";
                }
            }
            return null;
        };
        animationController.Layers.Add(climb);
    }

    partial void CustomInitializePlatformerInput()
    {
        if (InputDevice is Keyboard asKeyboard)
        {
            RunInput = asKeyboard.GetKey(Microsoft.Xna.Framework.Input.Keys.LeftShift);
        }
        else if (InputDevice is Xbox360GamePad asGamepad)
        {
            RunInput = asGamepad.GetButton(Xbox360GamePad.Button.X);
        }
    }

    private void CustomActivity()
    {
        animationController.Activity();

        if (!CurrentMovement.CanClimb)
        {
            if (VerticalInput.Value < 0)
            {
                GroundMovement = PlatformerValuesStatic["Ducking"];
            }
            else if (RunInput.IsDown)
            {
                GroundMovement = PlatformerValuesStatic["Running"];
                AirMovement = PlatformerValuesStatic["RunningAir"];
            }
            else
            {
                GroundMovement = PlatformerValuesStatic["Ground"];
                AirMovement = PlatformerValuesStatic["Air"];
            }
        }
        if (LastCollisionLadderRectange != null)
        {
            ladderColumnLeft = LastCollisionLadderRectange.Left;
            ladderColumnRight = LastCollisionLadderRectange.Right;
        }

        // Even if we are colliding with it, we want to see if the player's "body" is over
        // the ladder. We can do this by checking the center.
        var isOverLadder = ladderColumnLeft != null && ladderColumnRight != null &&
            X < ladderColumnRight && X > ladderColumnLeft;

        if (InputDevice.DefaultUpPressable.WasJustPressed && LastCollisionLadderRectange != null)
        {
            GroundMovement = PlatformerValuesStatic["Climbing"];
            // snap the player's position to the center of the ladder
            X = LastCollisionLadderRectange.X;
            XVelocity = 0;
            if (IsOnGround == false)
            {
                // force the player on ground:
                CurrentMovementType = MovementType.Ground;
            }
        }

        if (isOverLadder == false && CurrentMovement.CanClimb)
        {
            // fall off the ladder...
            CurrentMovementType = MovementType.Air;
        }
    }

    // Generated code calls this automatically once climbing is clamped at TopOfLadderY and the player
    // isn't holding Up - Y is already positioned at the top of the ladder when this fires. Deliberately
    // left empty: swapping GroundMovement here used to enable gravity immediately, which only "works"
    // if the level has solid ground flush under the ladder's own column - Level1Map doesn't, so the
    // player fell straight through it. Matching FRB2's reference behavior, the player stays suspended
    // in the climbing values at the top until CustomActivity's isOverLadder check drops them into Air
    // movement when they actually step sideways off the ladder.
    partial void OnLadderTopReached()
    {
    }

    // Generated code calls this automatically once the player is grounded while climbing and isn't
    // holding Up - replaces the old hand-rolled "VerticalInput.Value < 0 && IsOnGround" poll.
    partial void OnLadderBottomReached()
    {
        GroundMovement = PlatformerValuesStatic["Ground"];
    }

    private void CustomDestroy()
    {
        
    }

    private static void CustomLoadStaticContent(string contentManagerName)
    {
        
    }
}