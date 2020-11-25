using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall.Math.Geometry;

namespace FlatRedBall.Entities
{
    public interface IDamageArea : ICollidable
    {
        event Action Destroyed;
        double SecondsBetweenDamage { get; set; }
        object DamageDealer { get; }
        int TeamIndex { get; }
    }
}
