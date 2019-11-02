using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FlatRedBall.Glue.Tasks;

namespace FlatRedBall.Glue.Managers
{

    public enum TaskExecutionPreference
    {
        Fifo,
        Asap,
        //AddIfNotYetAdded,
        AddOrMoveToEnd
    }

    public class TaskManager : Singleton<TaskManager>
    {

        #region Fields
        int asyncTasks;

        List<GlueTask> mSyncedActions = new List<GlueTask>();
        static object mSyncLockObject = new object();


        List<GlueTask> mActiveAsyncTasks = new List<GlueTask>();

        List<string> taskHistory = new List<string>();

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

                if(IsTaskProcessingEnabled == false)
                {
                    toReturn += "Task processing disabled, next task when re-enabled:\n";
                }

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

        bool isTaskProcessingEnabled;
        /// <summary>
        /// Whether to process tasks - if this is false, then tasks will not be processed.
        /// </summary>
        public bool IsTaskProcessingEnabled
        {
            get { return isTaskProcessingEnabled; }
            set
            {
                bool turnedOn = value == true && isTaskProcessingEnabled == false;
                isTaskProcessingEnabled = value;
                if(turnedOn)
                {
                    ProcessNextSync();

                }
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

            // not sure why but this can go into the negative...
            asyncTasks = System.Math.Max(asyncTasks, 0);

            CallTaskAddedOrRemoved();

            

        }


        public async Task WaitForAllTasksFinished()
        {
            while (!AreAllAsyncTasksDone)
            {
                await Task.Delay(500);
            }
        }


        
        /// <summary>
        /// Adds an action to be executed, guaranteeing that no other actions will be executed at the same time as this.
        /// Actions added will be executed in the order they were added (fifo).
        /// </summary>
        public void AddSync(Action action, string displayInfo)
        {
            AddSync(action, displayInfo, isHighPriority: false);
        }

        /// <summary>
        /// Adds an action to be executed, guaranteeing that no other actions will be executed at the same time as this.
        /// Actions added will be executed in the order they were added (fifo).
        /// </summary>
        /// <param name="action">The action to execute.</param>
        /// <param name="displayInfo">The details of the task, to de bisplayed in the tasks window.</param>
        /// <param name="isHighPriority">Whether to attempt to run the action immediately - useful for UI tasks</param>
        public void AddSync(Action action, string displayInfo, bool isHighPriority)
        {
            TaskExecutionPreference executionPreference = TaskExecutionPreference.Fifo;

            if(isHighPriority)
            {
                executionPreference = TaskExecutionPreference.Asap;
            }

            Add(action, displayInfo, executionPreference);
        }

        public void Add(Action action, string displayInfo, TaskExecutionPreference executionPreference = TaskExecutionPreference.Fifo)
        {
            var glueTask = new GlueTask();
            glueTask.Action = action;
            glueTask.DisplayInfo = displayInfo;

            bool shouldProcess = false;

            lock (mSyncLockObject)
            {
                if (executionPreference == TaskExecutionPreference.Asap)
                {
                    if (mSyncedActions.Count > 0)
                    {
                        // don't insert at 0, finish the current task, but insert at 1:
                        mSyncedActions.Insert(1, glueTask);
                    }
                    else
                    {
                        mSyncedActions.Add(glueTask);
                    }
                }
                else if(executionPreference == TaskExecutionPreference.AddOrMoveToEnd)
                {
                    var existingAction = mSyncedActions.FirstOrDefault(item =>
                        item.DisplayInfo == displayInfo);

                    if(existingAction != null)
                    {
                        // just move it to the end
                        mSyncedActions.Remove(existingAction);
                        mSyncedActions.Add(glueTask);
                    }
                    else
                    {
                        // doesn't exist, so add it normally:
                        mSyncedActions.Add(glueTask);
                    }
                }
                else
                {
                    mSyncedActions.Add(glueTask);
                }
                shouldProcess = mSyncedActions.Count == 1 && IsTaskProcessingEnabled;
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

            GlueTask glueTask = null;

            lock (mSyncLockObject)
            {
                if (mSyncedActions.Count > 0)
                {
                    glueTask = mSyncedActions[0];
                    toProcess = glueTask.Action;
                }
            }

            if (toProcess != null)
            {
                ThreadPool.QueueUserWorkItem(delegate
                {
                    // This can be uncommented to get information about the task history
                    // to try to improve performance
                    //this.taskHistory.Add(glueTask?.DisplayInfo);

                    toProcess();

                    bool shouldProcess = false;

                    lock (mSyncLockObject)
                    {
                        // The task may have already been removed
                        if (mSyncedActions.Count > 0 && glueTask == mSyncedActions[0])
                        {
                            mSyncedActions.RemoveAt(0);
                        }

                        shouldProcess = mSyncedActions.Count > 0 && IsTaskProcessingEnabled;
                    }
                    CallTaskAddedOrRemoved();

                    if (shouldProcess)
                    {
                        ProcessNextSync();
                    }

                });
                CallTaskAddedOrRemoved();
            }
        }

        public void OnUiThread(Action action)
        {
#if GLUE
            global::Glue.MainGlueWindow.Self.Invoke(action);
#else
            action();
#endif
        }

#endregion
    }
}
