namespace FlatRedBall.Math
{

    /// <summary>
    /// Provides an interface for 3D position, velocity, and acceleration.
    /// </summary>
    /// <remarks>
    /// For an interface which provides only and no velocity and acceleration, see
    /// FlatRedBall.Math.IStaticPositionable.
    /// <seealso cref="FlatRedBall.Math.IStaticPositionable"/>
    /// </remarks>
    public interface IPositionable : IStaticPositionable
    {
        
        /// <summary>
        /// Gets and sets the absolute X Velocity.  Measured in units per second.
        /// </summary>
        float XVelocity { get; set;}

        
        /// <summary>
        /// Gets and sets the absolute Y Velocity.  Measured in units per second.
        /// </summary>
        float YVelocity { get; set;}

        
        /// <summary>
        /// Gets and sets the absolute Z Velocity.  Measured in units per second.
        /// </summary>
        float ZVelocity { get; set;}


        
        /// <summary>
        /// Gets and sets the absolute X Acceleration.  Measured in units per second.
        /// </summary>
        float XAcceleration { get; set;}

        
        /// <summary>
        /// Gets and sets the absolute Y Acceleration.  Measured in units per second.
        /// </summary>
        float YAcceleration { get; set;}

        
        /// <summary>
        /// Gets and sets the absolute Z Acceleration.  Measured in units per second.
        /// </summary>
        float ZAcceleration { get; set;}
    }
}
