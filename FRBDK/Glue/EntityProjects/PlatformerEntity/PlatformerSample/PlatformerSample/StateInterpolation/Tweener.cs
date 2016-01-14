using System;

namespace FlatRedBall.Glue.StateInterpolation
{
    public delegate float TweeningFunction(float timeElapsed, float start, float change, float duration);

    public enum InterpolationType
    {
        Back,
        Bounce,
        Circular,
        Cubic,
        Elastic,
        Exponential,
        Linear,
        Quadratic,
        Quartic,
        Quintic,
        Sinusoidal
    }


    public enum Easing
    {
        In,
        Out,
        InOut
    }

    public class Tweener
    {

        public static TweeningFunction GetInterpolationFunction(InterpolationType type, Easing easing)
        {
            switch (type)
            {
                case InterpolationType.Back:
                    switch (easing)
                    {
                        case Easing.In:
                            return Back.EaseIn;
                        case Easing.Out:
                            return Back.EaseOut;
                        case Easing.InOut:
                            return Back.EaseInOut;
                        default:
                            throw new Exception();
                    }
                case InterpolationType.Bounce:
                    switch (easing)
                    {
                        case Easing.In:
                            return Bounce.EaseIn;
                        case Easing.Out:
                            return Bounce.EaseOut;
                        case Easing.InOut:
                            return Bounce.EaseInOut;
                        default:
                            throw new Exception();
                    }
                case InterpolationType.Circular:
                    switch (easing)
                    {
                        case Easing.In:
                            return Circular.EaseIn;
                        case Easing.Out:
                            return Circular.EaseOut;
                        case Easing.InOut:
                            return Circular.EaseInOut;
                        default:
                            throw new Exception();
                    }
                case InterpolationType.Cubic:
                    switch (easing)
                    {
                        case Easing.In:
                            return Cubic.EaseIn;
                        case Easing.Out:
                            return Cubic.EaseOut;
                        case Easing.InOut:
                            return Cubic.EaseInOut;
                        default:
                            throw new Exception();
                    }
                case InterpolationType.Elastic:
                    switch (easing)
                    {
                        case Easing.In:
                            return Elastic.EaseIn;
                        case Easing.Out:
                            return Elastic.EaseOut;
                        case Easing.InOut:
                            return Elastic.EaseInOut;
                        default:
                            throw new Exception();
                    }
                case InterpolationType.Exponential:
                    switch (easing)
                    {
                        case Easing.In:
                            return Exponential.EaseIn;
                        case Easing.Out:
                            return Exponential.EaseOut;
                        case Easing.InOut:
                            return Exponential.EaseInOut;
                        default:
                            throw new Exception();
                    }
                case InterpolationType.Linear:
                    switch (easing)
                    {
                        case Easing.In:
                            return Linear.EaseIn;
                        case Easing.Out:
                            return Linear.EaseOut;
                        case Easing.InOut:
                            return Linear.EaseInOut;
                        default:
                            throw new Exception();
                    }
                case InterpolationType.Quadratic:
                    switch (easing)
                    {
                        case Easing.In:
                            return Quadratic.EaseIn;
                        case Easing.Out:
                            return Quadratic.EaseOut;
                        case Easing.InOut:
                            return Quadratic.EaseInOut;
                        default:
                            throw new Exception();
                    }
                case InterpolationType.Quartic:
                    switch (easing)
                    {
                        case Easing.In:
                            return Quartic.EaseIn;
                        case Easing.Out:
                            return Quartic.EaseOut;
                        case Easing.InOut:
                            return Quartic.EaseInOut;
                        default:
                            throw new Exception();
                    }
                case InterpolationType.Quintic:
                    switch (easing)
                    {
                        case Easing.In:
                            return Quintic.EaseIn;
                        case Easing.Out:
                            return Quintic.EaseOut;
                        case Easing.InOut:
                            return Quintic.EaseInOut;
                        default:
                            throw new Exception();
                    }
                case InterpolationType.Sinusoidal:
                    switch (easing)
                    {
                        case Easing.In:
                            return Sinusoidal.EaseIn;
                        case Easing.Out:
                            return Sinusoidal.EaseOut;
                        case Easing.InOut:
                            return Sinusoidal.EaseInOut;
                        default:
                            throw new Exception();
                    }
                default:
                    throw new Exception();

            }

        }


        // Added so that we can reuse these
        public Tweener()
        {

        }

        public Tweener(float from, float to, float duration, TweeningFunction tweeningFunction)
        {
            Start(from, to, duration, tweeningFunction);
            // Start sets Running to true, so let's set it to false
            Running = false;
        }

        public Tweener(float from, float to, TimeSpan duration, TweeningFunction tweeningFunction)
            : this(from, to, (float)duration.TotalSeconds, tweeningFunction)
        {

        }

        #region Properties
        private float _position;
        public float Position
        {
            get
            {
                return _position;
            }
            protected set
            {
                _position = value;
            }
        }

        private float _from;
        protected float from
        {
            get
            {
                return _from;
            }
            set
            {
                _from = value;
            }
        }

        private float _change;
        protected float change
        {
            get
            {
                return _change;
            }
            set
            {
                _change = value;
            }
        }

        private float _duration;
        protected float duration
        {
            get
            {
                return _duration;
            }
        }

        private float _elapsed = 0.0f;
        protected float elapsed
        {
            get
            {
                return _elapsed;
            }
            set
            {
                _elapsed = value;
            }
        }

        private bool _running = true;
        public bool Running
        {
            get { return _running; }
            protected set { _running = value; }
        }

        private TweeningFunction _tweeningFunction;
        protected TweeningFunction tweeningFunction
        {
            get
            {
                return _tweeningFunction;
            }
        }

        public delegate void PositionChangedHandler(float newPosition);

        // Not making this an event because I want users to be able to 
        // = it instead of only +=
        public PositionChangedHandler PositionChanged;

        public delegate void EndHandler();
        public event EndHandler Ended;
        #endregion

        #region Methods
        public void Update(float time)
        {
            if (!Running || (elapsed == duration))
            {
                Running = false;
                return;
            }
            Position = tweeningFunction(elapsed, from, change, duration);
            elapsed += time;

            if (PositionChanged != null)
            {
                PositionChanged(Position);
            }

            if (elapsed >= duration)
            {
                elapsed = duration;
                Position = from + change;
                OnEnd();
            }
        }

        protected void OnEnd()
        {
            if (Ended != null)
            {
                Ended();
            }
        }

        public void Start()
        {
            Running = true;
        }

        public void Start(float from, float to, float duration, TweeningFunction tweeningFunction)
        {
            _from = from;
            _position = from;
            _change = to - from;
            _tweeningFunction = tweeningFunction;
            _duration = duration;
            _elapsed = 0;

            Running = true;

        }

        public void Stop()
        {
            Running = false;
        }

        public void Reset()
        {
            elapsed = 0.0f;
            from = Position;
        }

        public void Reset(float to)
        {
            change = to - Position;
            Reset();
        }

        public void Reverse()
        {
            elapsed = 0.0f;
            change = -change + (from + change - Position);
            from = Position;
        }

        public override string ToString()
        {
#if WINDOWS_8
            return String.Format("Tween {0} -> {1} in {2}s. Elapsed {3:##0.##}s",
                from, 
                from + change, 
                duration, 
                elapsed);
#else
            return String.Format("{0}.{1}. Tween {2} -> {3} in {4}s. Elapsed {5:##0.##}s",
                tweeningFunction.Method.DeclaringType.Name,
                tweeningFunction.Method.Name,
                from, 
                from + change, 
                duration, 
                elapsed);
#endif
        }
        #endregion
    }
}
