using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Graphics.Particle;
using System.Xml.Serialization;
using FlatRedBall.Content.Scene;

namespace FlatRedBall.Content.Particle
{
    #region XML Docs
    /// <summary>
    /// Base class for EmitterSave and EmitterSaveContent.
    /// </summary>
    #endregion
    [XmlInclude(typeof(EmitterSave))]
    public class EmitterSaveBase
    {

        #region Fields

        public float X;
        public float Y;
        public float Z;

        public float ScaleX;
        public float ScaleY;
        public float ScaleZ;

        public float RotationX;
        public float RotationY; 
        public float RotationZ;

        public string AreaEmissionType;

        // This used to default to null, but I think
        // we want it to always have a valid instance.
        // But if something screws up in XML deserialization,
        // we'll have to figure out a different solution.
        public EmissionSettingsSave EmissionSettings = new EmissionSettingsSave();

        public bool BoundedEmission;
        public Polygon.PolygonSave EmissionBoundary;

        public string ParentSpriteName;
        public string Name;

        // Justin Johnson, 04/2015 - retiring particle blueprints,
        // this now should just create a fake object to
        // hold the texture value
        // public T ParticleBlueprint = new T();
        [Obsolete]
        public ParticleBlueprintSave ParticleBlueprint;

        public string RemovalEvent;

        public float SecondFrequency;
        public int NumberPerEmission = 1;
        public bool TimedEmission;

        public float RelativeX;
        public float RelativeY;
        public float RelativeZ;


        public bool ParentVelocityChangesEmissionVelocity;
		public bool RotationChangesParticleRotation;
		public bool RotationChangesParticleAcceleration;

        //
        //[XmlIgnore]
		//public FrbInstructionArray particleInstructionArray;
        //
        
        public bool AssetsRelativeToFile;

        public float SecondsLasting;

        /// <summary>
        /// Stores the source .emix file name.  This 
        /// is set if an EmitterSaveList is loaded from file.
        /// </summary>
        [XmlIgnore]
        public string FileName;

        #endregion

        #region Methods

        #region Constructor

        public EmitterSaveBase()
        { 
        }
        #endregion

        #region Public Methods

        public void InvertHandedness()
        {
            
            Z = -Z;

            RelativeZ = -RelativeZ;

            EmissionSettings.InvertHandedness();
        }

        #endregion

        #endregion

    }
}
