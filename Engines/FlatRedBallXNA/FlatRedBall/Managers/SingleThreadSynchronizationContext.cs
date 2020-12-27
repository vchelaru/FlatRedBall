using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace FlatRedBall.Managers
{
    internal class SingleThreadSynchronizationContext : SynchronizationContext, IManager
    {
        private readonly Queue<Action> _messagesToProcess = new Queue<Action>();
        private readonly object _syncHandle = new object();

        public SingleThreadSynchronizationContext()
        {
            SynchronizationContext.SetSynchronizationContext(this);
        }

        public override void Send(SendOrPostCallback codeToRun, object state)
        {
            throw new NotImplementedException();
        }

        public override void Post(SendOrPostCallback codeToRun, object state)
        {
            lock (_syncHandle)
            {
                _messagesToProcess.Enqueue(() => codeToRun(state));
                SignalContinue();
            }
        }


        private void SignalContinue()
        {
            Monitor.Pulse(_syncHandle);
        }

        public void Update()
        {
            while (true)
            {
                Action nextToRun = null;

                lock (_syncHandle)
                {
                    if (_messagesToProcess.Count > 0)
                    {
                        nextToRun = _messagesToProcess.Dequeue();
                    }
                }

                if (nextToRun == null)
                {
                    break;
                }
                else
                {
                    nextToRun();
                }
            }
        }

        public void UpdateDependencies()
        {

        }
    }
}
