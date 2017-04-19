using System;

namespace FlatRedBall.Glue.StateInterpolation
{
    /// <summary>
    /// Delegate used for all tweening functions.
    /// </summary>
    /// <param name="timeElapsed">How much time has passed since the start of the tween.  If this is called over a period of time then this value will increase.</param>
    /// <param name="startingValue">The starting value value for tweening.  For example this may be the X position of an object.</param>
    /// <param name="endingValue">The value when the tween is finished.  For example this may be the ending X position of an object.</param>
    /// <param name="durationInSeconds">How long (in seconds) that the tween will last.</param>
    /// <returns>The resulting value.</returns>
    public delegate float TweeningFunction(float timeElapsed, float startingValue, float endingValue, float durationInSeconds);

    #region Enums

    public enum InterpolationType
    {
        Back,
        Bounce,
        Circular,
        Cubic,
        Elastic,
        Exponential,
        Instant,
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

    #endregion

    public class Tweener
    {
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

        public float Duration
        {
            get;
            set;
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

        protected TweeningFunction _tweeningFunction;
        protected TweeningFunction tweeningFunction
        {
            get
            {
                return _tweeningFunction;
            }
        }


        public object Owner { get; set; }
        
        #endregion

        public delegate void PositionChangedHandler(float newPosition);

        // Not making this an event because I want users to be able to 
        // = it instead of only +=
        public PositionChangedHandler PositionChanged;


        public event Action Ended;


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
                case InterpolationType.Instant:
                    switch (easing)
                    {
                        case Easing.In:
                            return Instant.EaseIn;
                        case Easing.Out:
                            return Instant.EaseOut;
                        case Easing.InOut:
                            return Instant.EaseInOut;
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


        public Tweener(float from, float to, float duration, InterpolationType type, Easing easing)
        {
            Start(from, to, duration, GetInterpolationFunction(type, easing));
            // Start sets Running to true, so let's set it to false
            Running = false;

        }


        #region Methods
        public void Update(float timePassed)
        {
            if (!Running || (elapsed == Duration))
            {
                Running = false;
                return;
            }

            elapsed += timePassed;
            elapsed = System.Math.Min(elapsed, Duration);

            Position = tweeningFunction(elapsed, from, change, Duration);

            if (PositionChanged != null)
            {
                PositionChanged(Position);
            }

            if (elapsed >= Duration)
            {
                elapsed = Duration;
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
            Duration = duration;
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
#if WINDOWS_8 || UWP
            return String.Format("Tween {0} -> {1} in {2}s. Elapsed {3:##0.##}s",
                from, 
                from + change, 
                Duration, 
                elapsed);
#else
            return String.Format("{0}.{1}. Tween {2} -> {3} in {4}s. Elapsed {5:##0.##}s",
                tweeningFunction.Method.DeclaringType.Name,
                tweeningFunction.Method.Name,
                from, 
                from + change, 
                Duration, 
                elapsed);
#endif
        }
        #endregion
    }
}
