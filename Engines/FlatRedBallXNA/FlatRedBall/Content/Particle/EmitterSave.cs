using System;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using FlatRedBall.Graphics.Particle;
using FlatRedBall.Content.Scene;
using RemovalEventType = FlatRedBall.Graphics.Particle.Emitter.RemovalEventType;
using Microsoft.Xna.Framework;

namespace FlatRedBall.Content.Particle
{
    /// <summary>
    /// Justin Johnson, 04/2015
    /// This only exists to keep existing emix files functional
    /// because texture path is stored in the sprite
    /// blueprint object. This should eventually 
    /// be retired.
    /// </summary>
    [Obsolete]
#if ANDROID
    [Android.Runtime.Preserve (AllMembers = true)]
#endif
    public class ParticleBlueprintSave
    {
        public string Texture { get; set; }
    }

	public class EmitterSave : EmitterSaveBase
    {
        
        #region Methods

        public EmitterSave() : base() 
        {

        }

        [Obsolete]
        public static EmitterSave FromEmitter(Emitter emitterToCreateFrom)
		{
            EmitterSave emitterToReturn = new EmitterSave();

			emitterToReturn.X = (float)emitterToCreateFrom.Position.X;
			emitterToReturn.Y = (float)emitterToCreateFrom.Position.Y;
			emitterToReturn.Z = (float)emitterToCreateFrom.Position.Z;

			emitterToReturn.ScaleX = emitterToCreateFrom.ScaleX;
			emitterToReturn.ScaleY = emitterToCreateFrom.ScaleY;
			emitterToReturn.ScaleZ = emitterToCreateFrom.ScaleZ;

            emitterToReturn.RotationX = emitterToCreateFrom.RotationX;
            emitterToReturn.RotationY = emitterToCreateFrom.RotationY;
            emitterToReturn.RotationZ = emitterToCreateFrom.RotationZ;

			emitterToReturn.AreaEmissionType = FlatRedBall.Graphics.GraphicalEnumerations.TranslateAreaEmissionType(
                emitterToCreateFrom.AreaEmission);

            emitterToReturn.RemovalEvent =
                FlatRedBall.Graphics.Particle.Emitter.TranslateRemovalEvent( emitterToCreateFrom.RemovalEvent);

            if (emitterToCreateFrom.Parent != null)
            {
                emitterToReturn.ParentSpriteName = emitterToCreateFrom.Parent.Name;
            }

			emitterToReturn.Name = emitterToCreateFrom.Name;

            // Justin Johnson, 04/2015 - retiring particle blueprints
            //emitterToReturn.ParticleBlueprint = SpriteSave.FromSprite(emitterToCreateFrom.ParticleBlueprint);

            // I don't think that we need to manually set the texture - the SprietSave should do it on its own
            //particleBlueprint.spriteTexture = FRB.File.FileManager.MakeRelativeToPath(
            //    particleBlueprint.spriteTexture, relativeTo);

            emitterToReturn.BoundedEmission = emitterToCreateFrom.BoundedEmission;
            emitterToReturn.EmissionBoundary = Polygon.PolygonSave.FromPolygon(emitterToCreateFrom.EmissionBoundary);

            emitterToReturn.EmissionSettings = EmissionSettingsSave.FromEmissionSettings(emitterToCreateFrom.EmissionSettings);

            emitterToReturn.SecondFrequency = emitterToCreateFrom.SecondFrequency;
            emitterToReturn.NumberPerEmission = emitterToCreateFrom.NumberPerEmission;
            emitterToReturn.TimedEmission = emitterToCreateFrom.TimedEmission;

            emitterToReturn.RemovalEvent = 
                Emitter.TranslateRemovalEvent(emitterToCreateFrom.RemovalEvent);
            emitterToReturn.SecondsLasting = emitterToCreateFrom.SecondsLasting;

            emitterToReturn.RelativeX = emitterToCreateFrom.RelativePosition.X;
            emitterToReturn.RelativeY = emitterToCreateFrom.RelativePosition.Y;
            emitterToReturn.RelativeZ = emitterToCreateFrom.RelativePosition.Z;

            emitterToReturn.ParentVelocityChangesEmissionVelocity = emitterToCreateFrom.ParentVelocityChangesEmissionVelocity;
			emitterToReturn.RotationChangesParticleRotation = emitterToCreateFrom.RotationChangesParticleRotation;
			emitterToReturn.RotationChangesParticleAcceleration = emitterToCreateFrom.RotationChangesParticleAcceleration;

            // TODO:  Need to add support for saving off instructions.

			//particleInstructionArray = emitterToCreateFrom.ParticleBlueprint.instructionArray.Clone();

			// each instruction will reference the blueprint Sprite.  When serialized, the referenced Sprite
			// will be attempted to be saved.  This will throw an error (and also would take up a lot of memory if succeeded)
			//foreach(FrbInstruction frbi in particleInstructionArray)
			//	frbi.referenceObject = null;

            return emitterToReturn;
        }

        [Obsolete]
        public Emitter ToEmitter(string contentManagerName)
        {
            Emitter emitter = new Emitter();
            emitter.Position = new Vector3(X, Y, Z);

            emitter.ScaleX = ScaleX;
            emitter.ScaleY = ScaleY;
            emitter.ScaleZ = ScaleZ;

            emitter.RotationX = RotationX;
            emitter.RotationY = RotationY; 
            emitter.RotationZ = RotationZ;


            emitter.AreaEmission =
                FlatRedBall.Graphics.GraphicalEnumerations.TranslateAreaEmissionType(
                    AreaEmissionType);

            emitter.RemovalEvent = 
                FlatRedBall.Graphics.Particle.Emitter.TranslateRemovalEvent( RemovalEvent );

            //TODO: Attachments
            emitter.Name = Name;

            // Justin Johnson, 04/2015 - retiring particle blueprints
            //emitter.ParticleBlueprint = ParticleBlueprint.ToSprite(contentManagerName);

            emitter.SecondFrequency = SecondFrequency;
            emitter.NumberPerEmission = NumberPerEmission;
            emitter.TimedEmission = TimedEmission;

            emitter.RelativePosition =
                new Vector3(RelativeX, RelativeY, RelativeZ);

            emitter.ParentVelocityChangesEmissionVelocity =
                ParentVelocityChangesEmissionVelocity;
			emitter.RotationChangesParticleRotation = 
				RotationChangesParticleRotation;
			emitter.RotationChangesParticleAcceleration =
				RotationChangesParticleAcceleration;

            emitter.SecondsLasting = SecondsLasting;

            // This handles older emix files that stored texture in a particle blueprint.
            // To keep particles working we copy the old texture value into the
            // emission settings object. But only if the emission settings
            // doesn't already have a value.
            if (ParticleBlueprint != null &&
                !string.IsNullOrEmpty(ParticleBlueprint.Texture) && 
                string.IsNullOrEmpty(EmissionSettings.Texture))
            {
                EmissionSettings.Texture = ParticleBlueprint.Texture;
            }

            // Now that we've copied the values we can inflate with a hopefully-valid
            // texture string if one existed in either place.
            emitter.EmissionSettings = EmissionSettings.ToEmissionSettings(contentManagerName);

            


            emitter.BoundedEmission = BoundedEmission;
            if (EmissionBoundary != null)
                emitter.EmissionBoundary = EmissionBoundary.ToPolygon();
            return emitter;

        }

        public float GetEquilibriumParticleCount()
        {
            float spriteLastingTime = 0;

            #if FRB_MDX
            float divisionValue = 1;
#else
            float divisionValue = 255;
#endif

            if (this.RemovalEvent == RemovalEventType.Alpha0.ToString() || RemovalEvent == "Fadeout")
            {
                float startingAlpha =(255 - EmissionSettings.Fade) / divisionValue;
                
                float alphaRate = -EmissionSettings.FadeRate / divisionValue;

                spriteLastingTime = -startingAlpha / alphaRate;
            }
            else if (this.RemovalEvent == RemovalEventType.Timed.ToString())
            {
                spriteLastingTime = this.SecondsLasting;
            }

            // Now that we know how long a Sprite lasts, see how many Sprites can be emitted during that time
            float numberOfSprites = spriteLastingTime/this.SecondFrequency * this.NumberPerEmission;

            return numberOfSprites;
        }


        #endregion
        

    }
}
