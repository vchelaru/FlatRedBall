using System;
using System.Collections.Generic;

#if WINDOWS_PHONE
using Microsoft.Phone.Shell;
#endif

namespace FlatRedBall.IO
{
    /// <summary>Note: this class will box value types, use only to store system state to allow recovery, 
    /// not in-game state while in a loop.</summary>
    public abstract class StateManager
    {
        /// <summary>This event is raised when the game is being resumed after being deactivated.</summary>
        public event Action Activating;

        /// <summary>This event is raised when state should be saved.</summary>
        public event Action Deactivating;

        /// <summary>Will be true of the Activating event was raised.</summary>
        public bool WasActivated { get; set; }

        /// <summary>Casts the key value to "T".</summary>
        /// <returns>default(T) if the value is not found in state manager.</returns>
        public abstract T Get<T>(string key);

        /// <returns>Returns the key value, null if not found</returns>
        public abstract object this[string key] { get; set; }

        /// <summary>should be after events are subscribed to.</summary>
        public virtual void Initialize() { }

        /// <summary>Raises the Activating event.</summary>
        protected void OnActivating()
        {
            Action e = Activating;
            if (e != null)
            {
                WasActivated = true;
                e();
            }
        }

        /// <summary>Raises the Deactivating event.</summary>
        protected void OnDeactivating()
        {
            Action e = Deactivating;
            if (e != null)
            {
                e();
            }
        }

        #region Singleton Members

        private static StateManager instance;
        private static object syncLock = new object();

        /// <summary>Singleton instance</summary>
        public static StateManager Current
        {
            get
            {
                VerifyInstance();

                return instance;
            }
        }

        private static void VerifyInstance()
        {
            if (instance == null)
            {
                // double lock check ensures thread safety for this singleton instance
                lock (syncLock)
                {
                    if (instance == null)
                    {
#if WINDOWS_PHONE
                        instance = new PhoneStateManager();
#else
                        instance = new DefaultStateManager();
#endif
                    }
                }
            }
        }

        #endregion

        #region Private Implementation Classes

#if WINDOWS_PHONE

        private class PhoneStateManager : DefaultStateManager
        {
            public PhoneStateManager()
            {
                PhoneApplicationService.Current.Activated += (o, e) => this.OnActivating();
                PhoneApplicationService.Current.Deactivated += (o, e) => this.OnDeactivating();

            }

            public override void Initialize()
            {
                if (PhoneApplicationService.Current.StartupMode == StartupMode.Activate)
                {
                    this.OnActivating();
                }
            }
            protected override IDictionary<string,object> InitializeStore()
            {
                return PhoneApplicationService.Current.State;
            }

        }

#endif

        /// <summary>Simple implementation ... keeps state in memory, does not save anywhere.
        /// Also, Activated and Deactivated events will never raise.</summary>
        /// <remarks>This implementation is for cross platform support on non-windows phone platforms</remarks>
        private class DefaultStateManager : StateManager
        {
            private IDictionary<string, object> store;

            public DefaultStateManager()
            {
                this.store = this.InitializeStore();
            }

            protected virtual IDictionary<string, object> InitializeStore()
            {
                return new Dictionary<string, object>();
            }

            public override T Get<T>(string key)
            {
                object val = store[key];
                if (val == null) return default(T);
                return (T)val;
            }

            public override object this[string key]
            {
                get
                {
                    object val;
                    store.TryGetValue(key, out val);
                    return val;
                }
                set
                {
                    if (store.ContainsKey(key))
                    {
                        store[key] = value;
                    }
                    else
                    {
                        store.Add(key, value);
                    }
                }
            }
        }

        #endregion

    }
}
