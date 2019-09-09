using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RacingPlugin.Models
{
    class RacingEntityValues
    {
        public string Name;


        public float BrakeStoppingAcceleration = 130;
        public float ReverseMaxSpeed = 60;
        public float ReverseAcceleration = 60;
        public float EffectiveMaxSpeed = 280;
        public float ForwardAcceleration = 50;
        public float NoGasSlowdownRate = 30;
        public float FastSlowDown = 30;

        public float MaxTurnRate = 1;
        public float TimeToMaxTurn = .8f;
        public float SteeringWheelToNormalTime = .175f;
        public float Stability = 350;
        public float NoGasExtraStability = 100;
        public float NoTurnExtraStability = 100;
        public float MinSpeedForMaxTurnRate = 30;
    }
}
