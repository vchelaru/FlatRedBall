using Glue;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using FlatRedBall.Glue.Tasks;

namespace FlatRedBall.Glue.Managers
{

    public class TaskManager : Singleton<TaskManager>
    {





        int asyncTasks;

        #region Fields

        List<GlueTask> mSyncedActions = new List<GlueTask>();
        static object mSyncLockObject = new object();


        List<GlueTask> mActiveAsyncTasks = new List<GlueTask>();

        #endregion

        public event Action TaskAddedOrRemoved;

        #region Properties

        public int SyncTaskTasks
        {
            get
            {
                return mSyncedActions.Count;
            }
        }

        public bool AreAllAsyncTasksDone
        {
            get
            {
                lock (mActiveAsyncTasks)
                {
                    return mActiveAsyncTasks.Count == 0;
                }
            }
        }

        public int TaskCount
        {
            get
            {
                lock (mActiveAsyncTasks)
                {
                    return mActiveAsyncTasks.Count + asyncTasks + mSyncedActions.Count;
                }
            }
        }

        public string CurrentTask
        {
            get
            {
                string toReturn = "";


                if (mActiveAsyncTasks.Count != 0)
                {
                    // This could update while we're looping. We don't want to throw errors, don't want to lock anything, 
                    // so just handle it with a try catch:
                    try
                    {
                        foreach(var item in mActiveAsyncTasks)
                        {
                            toReturn += item.DisplayInfo + "\n";
                        }
                    }
                    catch
                    {
                        // do nothing
                    }

                }

                if (mSyncedActions.Count != 0)
                {
                    try
                    {
                        toReturn += mSyncedActions[0].DisplayInfo;
                    }
                    catch
                    {
                        // do nothing
                    }
                }


                return toReturn;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Adds a task which can execute simultaneously with other tasks
        /// </summary>
        /// <param name="action">The action to perform.</param>
        /// <param name="details">The details of the task, to be displayed in the tasks window.</param>
        public void AddAsyncTask(Action action, string details)
        {
            asyncTasks++;

            ThreadPool.QueueUserWorkItem(
                (arg)=>ExecuteActionSync(action, details));
        }

        void CallTaskAddedOrRemoved()
        {
            if(TaskAddedOrRemoved != null)
            {
                TaskAddedOrRemoved();
            }
        }

        void ExecuteActionSync(Action action, string details)
        {
            var glueTask = new GlueTask
            {
                Action = action,
                DisplayInfo = details
            };

            lock (mActiveAsyncTasks)
            {
                mActiveAsyncTasks.Add(glueTask);
            }

            CallTaskAddedOrRemoved();
            ((Action)action)();

            lock (mActiveAsyncTasks)
            {
                mActiveAsyncTasks.Remove(glueTask);
            }
            asyncTasks--;

            CallTaskAddedOrRemoved();

            

        }


        internal void WaitForAllTasksFinished()
        {
            while (!AreAllAsyncTasksDone)
            {
                System.Threading.Thread.Sleep(50);
            }
        }


        /// <summary>
        /// Adds an action to be executed, guaranteeing that no other actions will be executed at the same time as this.
        /// Actions added will be executed in the order they were added (fifo).
        /// </summary>
        /// <param name="action">The action to execute.</param>
        /// <param name="displayInfo">The details of the task, to de bisplayed in the tasks window.</param>
        public void AddSync(Action action, string displayInfo)
        {
            bool shouldProcess = false;
            lock (mSyncLockObject)
            {
                var glueTask = new GlueTask();
                glueTask.Action = action;
                glueTask.DisplayInfo = displayInfo;
                mSyncedActions.Add(glueTask);
                shouldProcess = mSyncedActions.Count == 1;
            }
            CallTaskAddedOrRemoved();
            if (shouldProcess)
            {
                ProcessNextSync();
            }
        }

        private void ProcessNextSync()
        {
            Action toProcess = null;
            lock (mSyncLockObject)
            {
                if (mSyncedActions.Count > 0)
                {
                    toProcess = mSyncedActions[0].Action;
                }
            }

            if (toProcess != null)
            {
                ThreadPool.QueueUserWorkItem(delegate
                {
                    toProcess();

                    lock (mSyncLockObject)
                    {

                        if (mSyncedActions.Count > 0)
                        {
                            mSyncedActions.RemoveAt(0);
                        }
                    }

                    CallTaskAddedOrRemoved();
                    if (mSyncedActions.Count > 0)
                    {
                        ProcessNextSync();
                    }

                });
                CallTaskAddedOrRemoved();
            }
        }

        public void OnUiThread(Action action)
        {
            MainGlueWindow.Self.Invoke(action);
        }

        #endregion
    }
}
