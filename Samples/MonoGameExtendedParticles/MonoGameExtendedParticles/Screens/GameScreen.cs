using System.Collections.Generic;
using FlatRedBall;
using FlatRedBall.Graphics;
using FlatRedBall.Input;
using FlatRedBall.Math.Geometry;
using FlatRedBall.Screens;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended.Particles;
using MonoGame.Extended.Particles.Data;
using MonoGame.Extended.Particles.Modifiers;
using MonoGame.Extended.Particles.Modifiers.Interpolators;
using MonoGame.Extended.Particles.Profiles;

// FlatRedBall, MonoGame, and MonoGame.Extended all define types called Sprite and Mouse,
// so those namespaces are aliased rather than imported wholesale.
using Texture2DRegion = MonoGame.Extended.Graphics.Texture2DRegion;
using FrbMouse = FlatRedBall.Input.Mouse;

namespace MonoGameExtendedParticles.Screens
{
    /// <summary>
    /// Shows a MonoGame.Extended ParticleEffect drawn through an IDrawableBatch alongside
    /// ordinary FlatRedBall Sprites. Moving and zooming the camera is the point of the
    /// sample - the particles must stay locked to the FlatRedBall world while it happens.
    /// </summary>
    class GameScreen : Screen
    {
        ParticleEffectDrawableBatch fountainBatch;
        ParticleEffectDrawableBatch burstBatch;
        Texture2D particleTexture;
        Sprite referenceSprite;
        AxisAlignedRectangle originMarker;

        public GameScreen() : base(nameof(GameScreen)) { }

        public override void Initialize(bool addToManagers)
        {
            particleTexture = ParticleTexture.CreateSoftCircle(FlatRedBallServices.GraphicsDevice, 32);

            CreateOriginMarker();
            CreateFountain();
            CreateBurst();

            base.Initialize(addToManagers);
        }

        /// <summary>
        /// Ordinary FlatRedBall objects at the origin. If the drawable batch's transform is
        /// right, the fountain erupts from exactly this spot no matter where the camera is
        /// or how far it is zoomed - which is the thing that is easy to get wrong and easy
        /// to check by eye.
        /// </summary>
        void CreateOriginMarker()
        {
            referenceSprite = SpriteManager.AddSprite(particleTexture);
            referenceSprite.TextureScale = 1;
            referenceSprite.Color = Color.CornflowerBlue;

            // The additive fountain washes the Sprite out, so an outlined rectangle gives a
            // reference that stays readable through the particles.
            originMarker = ShapeManager.AddAxisAlignedRectangle();
            originMarker.Width = 100;
            originMarker.Height = 100;
            originMarker.Color = Color.CornflowerBlue;
        }

        void CreateFountain()
        {
            var effect = new ParticleEffect("Fountain")
            {
                // AutoTrigger emits continuously, so nothing needs to call Trigger.
                AutoTrigger = true,
                AutoTriggerFrequency = 0.02f,
                Emitters = new List<ParticleEmitter>
                {
                    new ParticleEmitter(initialCapacity: 2000)
                    {
                        LifeSpan = 2f,
                        TextureRegion = new Texture2DRegion(particleTexture),
                        // Vector2(0, -1) is up on screen, because the simulation runs in
                        // MonoGame.Extended's Y-down space rather than FlatRedBall's.
                        Profile = Profile.Spray(new Vector2(0, -1), 0.5f),
                        Parameters = new ParticleReleaseParameters
                        {
                            Speed = new ParticleFloatParameter(150f, 320f),
                            Quantity = new ParticleInt32Parameter(4, 8),
                            Scale = new ParticleVector2Parameter(new Vector2(0.4f), new Vector2(0.9f)),
                            // Color is HSL: hue in degrees, then saturation and lightness.
                            Color = new ParticleColorParameter(new Vector3(15f, 1f, 0.6f), new Vector3(45f, 1f, 0.6f)),
                        },
                        Modifiers =
                        {
                            // Gravity pointing at +Y pulls particles down the screen, again
                            // because the simulation is in Y-down space.
                            new LinearGravityModifier { Direction = Vector2.UnitY, Strength = 260f },
                            new AgeModifier
                            {
                                Interpolators =
                                {
                                    new ScaleInterpolator
                                    {
                                        StartValue = new Vector2(1f),
                                        EndValue = new Vector2(0.1f)
                                    },
                                    new OpacityInterpolator { StartValue = 1f, EndValue = 0f }
                                }
                            }
                        }
                    }
                }
            };

            fountainBatch = new ParticleEffectDrawableBatch(effect);
            SpriteManager.AddDrawableBatch(fountainBatch);
        }

        void CreateBurst()
        {
            var effect = new ParticleEffect("Burst")
            {
                // ParticleEffect enables AutoTrigger in its constructor, so an effect that is
                // only meant to fire on demand has to switch it off or it also emits once a
                // second on its own.
                AutoTrigger = false,
                Emitters = new List<ParticleEmitter>
                {
                    new ParticleEmitter(initialCapacity: 4000)
                    {
                        LifeSpan = 1.2f,
                        TextureRegion = new Texture2DRegion(particleTexture),
                        Profile = Profile.Circle(12f, CircleRadiation.Out),
                        Parameters = new ParticleReleaseParameters
                        {
                            Speed = new ParticleFloatParameter(120f, 400f),
                            Quantity = new ParticleInt32Parameter(180, 260),
                            Scale = new ParticleVector2Parameter(new Vector2(0.3f), new Vector2(0.7f)),
                            Color = new ParticleColorParameter(new Vector3(180f, 0.9f, 0.65f), new Vector3(220f, 0.9f, 0.65f)),
                        },
                        Modifiers =
                        {
                            new DragModifier { DragCoefficient = 0.6f, Density = 0.5f },
                            new AgeModifier
                            {
                                Interpolators =
                                {
                                    new OpacityInterpolator { StartValue = 1f, EndValue = 0f }
                                }
                            }
                        }
                    }
                }
            };

            burstBatch = new ParticleEffectDrawableBatch(effect);
            SpriteManager.AddDrawableBatch(burstBatch);
        }

        public override void Activity(bool firstTimeCalled)
        {
            MoveCamera();
            TriggerBurstOnClick();

            base.Activity(firstTimeCalled);
        }

        /// <summary>
        /// Arrow keys pan and Z/X zoom. Both exist so the transform in
        /// ParticleEffectDrawableBatch can be checked by eye against the origin marker.
        /// </summary>
        void MoveCamera()
        {
            const float panSpeed = 200f;
            var keyboard = InputManager.Keyboard;

            if (keyboard.KeyDown(Keys.Left)) Camera.Main.X -= panSpeed * TimeManager.SecondDifference;
            if (keyboard.KeyDown(Keys.Right)) Camera.Main.X += panSpeed * TimeManager.SecondDifference;
            if (keyboard.KeyDown(Keys.Down)) Camera.Main.Y -= panSpeed * TimeManager.SecondDifference;
            if (keyboard.KeyDown(Keys.Up)) Camera.Main.Y += panSpeed * TimeManager.SecondDifference;

            if (keyboard.KeyDown(Keys.Z)) Camera.Main.OrthogonalHeight *= 1 + TimeManager.SecondDifference;
            if (keyboard.KeyDown(Keys.X)) Camera.Main.OrthogonalHeight *= 1 - TimeManager.SecondDifference;

            // FlatRedBall does not derive one from the other, so width has to follow height
            // to keep the aspect ratio square.
            Camera.Main.OrthogonalWidth =
                Camera.Main.OrthogonalHeight * Camera.Main.DestinationRectangle.Width / Camera.Main.DestinationRectangle.Height;
        }

        void TriggerBurstOnClick()
        {
            if (InputManager.Mouse.ButtonPushed(FrbMouse.MouseButtons.LeftButton))
            {
                // WorldXAt/WorldYAt give FlatRedBall world coordinates; the drawable batch
                // converts them into particle space.
                burstBatch.Trigger(
                    InputManager.Mouse.WorldXAt(0),
                    InputManager.Mouse.WorldYAt(0));
            }
        }

        public override void Destroy()
        {
            // RemoveDrawableBatch calls the batch's Destroy method.
            SpriteManager.RemoveDrawableBatch(fountainBatch);
            SpriteManager.RemoveDrawableBatch(burstBatch);
            SpriteManager.RemoveSprite(referenceSprite);
            ShapeManager.Remove(originMarker);
            particleTexture.Dispose();

            base.Destroy();
        }
    }
}
