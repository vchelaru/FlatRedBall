using System;

namespace FlatRedBall.Glue.StateInterpolation
{
    public delegate float TweeningFunction(float timeElapsed, float start, float change, float duration);

    public class Tweener
    {
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
            return String.Format("{0}.{1}. Tween {2} -> {3} in {4}s. Elapsed {5:##0.##}s",
                tweeningFunction.Method.DeclaringType.Name,
                tweeningFunction.Method.Name,
                from, 
                from + change, 
                duration, 
                elapsed);
        }
        #endregion
    }
}
