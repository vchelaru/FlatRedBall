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
using FlatRedBall.Gui;

namespace MultiplayerPlatformerDemo.Entities;

public partial class Player
{
    AnimationController animationController;

    public IPressableInput RunInput { get; set; }

    public bool PressedUp => InputDevice.DefaultUpPressable.WasJustPressed;

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
            if (Velocity.X != 0 && HorizontalInput.Value != 0)
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
        };
        animationController.Layers.Add(duckLayer);

        var fallLayer = new AnimationLayer();
        fallLayer.EveryFrameAction = () =>
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
        else
        {
            if (VerticalInput.Value < 0 && IsOnGround)
            {
                GroundMovement = PlatformerValuesStatic["Ground"];
            }
        }
    }

    private void CustomDestroy()
    {

    }

    private static void CustomLoadStaticContent(string contentManagerName)
    {

    }
}