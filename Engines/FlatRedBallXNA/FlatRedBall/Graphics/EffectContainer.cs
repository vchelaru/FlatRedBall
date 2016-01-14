using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework.Graphics;

#if SILVERLIGHT
using Effect = System.Windows.Media.Effects.Effect;
#endif

namespace FlatRedBall.Graphics
{
    #region XML Docs
    /// <summary>
    /// A container for an effect that maintains variable values
    /// </summary>
    #endregion
    public class EffectContainer
    {
        #region Fields

        #region XML Docs
        /// <summary>
        /// The effect managed by this effect container
        /// </summary>
        #endregion
        internal Effect Effect;

        #endregion

        #region Properties

        public Effect GetEffect { get { return Effect; } }

        #endregion

        #region Methods

        #region Constructor

        #region XML Docs
        /// <summary>
        /// Creates an effect container
        /// </summary>
        /// <param name="effectPath">The path to the effect file to use in this container</param>
        #endregion
        public EffectContainer(String effectPath)
        {
            Effect = FlatRedBallServices.Load<Effect>(effectPath);
        }

        #region XML Docs
        /// <summary>
        /// Creates an effect container
        /// </summary>
        /// <param name="effectPath">The path to the effect file to use in this container</param>
        /// <param name="contentManagerName">The name of the content manager to use to load this effect</param>
        #endregion
        public EffectContainer(String effectPath, String contentManagerName)
        {
            Effect = FlatRedBallServices.Load<Effect>(effectPath, contentManagerName);
        }

        #region XML Docs
        /// <summary>
        /// Creates an effect container
        /// </summary>
        /// <param name="effect">The effect to use in this container</param>
        #endregion
        public EffectContainer(Effect effect)
        {
            Effect = effect;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Sets all the parameters in addition to the ones in the effect itself.
        /// </summary>
        public virtual void SetParameterValues()
        {
        }

        #endregion

        #region Protected Methods

        #region XML Docs
        /// <summary>
        /// Overridable method to set parameter values before drawing
        /// </summary>
        #endregion


        #endregion

        #endregion
    }
}
