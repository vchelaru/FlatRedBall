using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Tasks;
using FlatRedBall.IO;

namespace FlatRedBall.Glue.Managers
{
    #region Enums

    public enum TaskExecutionPreference
    {
        Fifo,
        Asap,
        AddOrMoveToEnd
    }

    public enum TaskEvent
    {
        Created,
        Queued,
        Started,
        Removed
    }

    #endregion

    public class TaskManager : Singleton<TaskManager>
    {
        #region Fields
        int asyncTasks;

        /// <summary>
        /// Contains all synced actions including the currently-executing action. The current task will
        /// remain in this list until it has finished executing.
        /// </summary>
        List<GlueTask> mSyncedActions = new List<GlueTask>();
        public GlueTask[] SyncedActions
        {
            get => mSyncedActions.ToArray();
        }
        static object mSyncLockObject = new object();


        List<GlueTask> mActiveAsyncTasks = new List<GlueTask>();

        const int maxTasksInHistory = 121;
        List<string> taskHistory = new List<string>();

        public int? SyncTaskThreadId { get; private set; }

        #endregion

        public event Action<TaskEvent, GlueTask> TaskAddedOrRemoved;

        #region Properties

        public int SyncTaskTasks
        {
            get
            {
                return mSyncedActions.Count;
            }
        }

        public bool AreAllAsyncTasksDone => TaskCount == 0;

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
                        toReturn += mSyncedActions.FirstOrDefault()?.DisplayInfo;
                    }
                    catch
                    {
                        // do nothing
                    }
                }


                return toReturn;
            }
        }

        bool isTaskProcessingEnabled = true;
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
        [Obsolete]
        public void AddAsyncTask(Action action, string details)
        {

            ThreadPool.QueueUserWorkItem(
                (arg)=>ExecuteActionSync(action, details));
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

            TaskAddedOrRemoved?.Invoke(TaskEvent.Queued, glueTask);

            ((Action)action)();

            lock (mActiveAsyncTasks)
            {
                mActiveAsyncTasks.Remove(glueTask);
            }
            asyncTasks--;

            // not sure why but this can go into the negative...
            asyncTasks = System.Math.Max(asyncTasks, 0);

            TaskAddedOrRemoved?.Invoke(TaskEvent.Removed, glueTask);
        }


        public async Task<bool> WaitForAllTasksFinished()
        {
            var didWait = false;
            while (!AreAllAsyncTasksDone)
            {
                didWait = true;
                await Task.Delay(200);
            }
            return didWait;
        }

        public async Task WaitForTaskToFinish(GlueTask glueTask)
        {
            if(glueTask == null)
            {
                return;
            }
            else
            {
                bool IsTaskDone()
                {
                    lock(mActiveAsyncTasks)
                    {
                        if(mSyncedActions.Contains(glueTask) || mActiveAsyncTasks.Contains(glueTask))
                        {
                            return false;
                        }

                        return true;
                    }
                }

                while(!IsTaskDone())
                {
                    await Task.Delay(150);
                }
            }
        }

        [Obsolete("Use Add, which allows specifying the priority")]
        /// <summary>
        /// Adds an action to be executed, guaranteeing that no other actions will be executed at the same time as this.
        /// Actions added will be executed in the order they were added (fifo).
        /// </summary>
        public void AddSync(Action action, string displayInfo) => Add(action, displayInfo);

        public GlueTask Add(Action action, string displayInfo, TaskExecutionPreference executionPreference = TaskExecutionPreference.Fifo, bool doOnUiThread = false)
        {
            var glueTask = new GlueTask();
            glueTask.Action = action;
            glueTask.DoOnUiThread = doOnUiThread;
            glueTask.TaskExecutionPreference = executionPreference;
            AddInternal(displayInfo, glueTask);
            return glueTask;
        }

        private void AddInternal(string displayInfo, GlueTask glueTask)
        {
            glueTask.DisplayInfo = displayInfo;

            bool shouldProcess = false;

            bool createdNew = true;

            lock (mSyncLockObject)
            {
                if (glueTask.TaskExecutionPreference == TaskExecutionPreference.Asap)
                {
                    if (mSyncedActions.Count > 0)
                    {
                        // don't insert at 0, finish the current task, but insert at 1:
                        var wasAdded = false;
                        for(int i = 1; i < mSyncedActions.Count; i++)
                        {
                            if(mSyncedActions[i].TaskExecutionPreference != TaskExecutionPreference.Asap)
                            {
                                mSyncedActions.Insert(i, glueTask);
                                wasAdded = true;
                                break;
                            }
                        }

                        if(!wasAdded)
                        {
                            mSyncedActions.Add(glueTask);
                        }
                    }
                    else
                    {
                        mSyncedActions.Add(glueTask);
                    }
                }
                else if (glueTask.TaskExecutionPreference == TaskExecutionPreference.AddOrMoveToEnd)
                {
                    // There's a few possible situations:
                    // 1. This task is not present in the list at all. 
                    //    - In this case, add it at the end
                    // 2. This task is present in the list and is not currently executing.
                    //    - In this case, remove the item from the list, and re-add it at the end
                    // 3. This task is present in the list, but it is the first entry, which means it's probably currently running
                    //    a. If this is the only item in the list, then do nothing since it's already executing
                    //    b. If it's the first item in the list, but there are other actions that will execute...
                    //       i. If there are no other entries in the list that match this, add this at the end
                    //       ii. If there are other entries in the list (besides the first) remove those, add at the end
                    GlueTask existingAction = mSyncedActions.FirstOrDefault(item =>
                        item.DisplayInfo == displayInfo);

                    GlueTask actionToRemove = null;

                    var shouldAddAtEnd = false;
                    if (existingAction == null)
                    {
                        // This is #1 from above
                        actionToRemove = null;
                        shouldAddAtEnd = true;
                    }
                    else
                    {
                        if (existingAction != mSyncedActions[0])
                        {
                            // This is #2
                            actionToRemove = existingAction;
                            shouldAddAtEnd = true;
                        }
                        else if (mSyncedActions.Count == 1)
                        {
                            // this is #3a
                            actionToRemove = null;
                            shouldAddAtEnd = false;
                        }
                        else
                        {
                            // this is #3b

                            // see if it's (a) or (b) from above
                            var existingActionLaterInList = mSyncedActions.Skip(1).FirstOrDefault(item =>
                                item.DisplayInfo == displayInfo);
                            if (existingActionLaterInList == null)
                            {
                                // #3b i
                                shouldAddAtEnd = true;
                                actionToRemove = null;
                            }
                            else
                            {
                                // #3b ii
                                shouldAddAtEnd = true;
                                actionToRemove = existingActionLaterInList;
                            }
                        }
                    }

                    if (actionToRemove != null)
                    {
                        mSyncedActions.Remove(actionToRemove);
                    }
                    if (shouldAddAtEnd)
                    {
                        mSyncedActions.Add(glueTask);
                    }

                    createdNew = (shouldAddAtEnd && actionToRemove == null);

                }
                else
                {
                    mSyncedActions.Add(glueTask);
                }
                shouldProcess = createdNew && mSyncedActions.Count == 1 && IsTaskProcessingEnabled;
            }
            // process will take care of reporting it
            if (createdNew)
            {
                TaskAddedOrRemoved?.Invoke(TaskEvent.Created, glueTask);
            }

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
                string taskDisplayInfo = glueTask.DisplayInfo;
                RecordTaskHistory(taskDisplayInfo);
                ThreadPool.QueueUserWorkItem(delegate
                {
                    SyncTaskThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
                    // This can be uncommented to get information about the task history
                    // to try to improve performance
                    //this.taskHistory.Add(glueTask?.DisplayInfo);
                    glueTask.TimeStarted = DateTime.Now;
                    TaskAddedOrRemoved?.Invoke(TaskEvent.Started, glueTask);
                    if (glueTask.DoOnUiThread)
                    {
                        global::Glue.MainGlueWindow.Self.Invoke(toProcess);
                    }
                    else
                    {
                        toProcess();

                    }
                    SyncTaskThreadId = null;

                    glueTask.TimeEnded = DateTime.Now;
                    bool shouldProcess = false;

                    lock (mSyncLockObject)
                    {
                        // The task may have already been removed
                        if (mSyncedActions.Contains(glueTask))
                        {
                            mSyncedActions.Remove(glueTask);
                        }

                        shouldProcess = mSyncedActions.Count > 0 && IsTaskProcessingEnabled;
                    }
                    TaskAddedOrRemoved?.Invoke(TaskEvent.Removed, glueTask);


                    if (shouldProcess)
                    {
                        ProcessNextSync();
                    }

                });
                TaskAddedOrRemoved?.Invoke(TaskEvent.Queued, glueTask);

            }
        }

        public void RecordTaskHistory(string taskDisplayInfo)
        {
            var projectName = GlueState.Self.CurrentMainProject?.FullFileName;
            if (string.IsNullOrEmpty(projectName))
            {
                projectName = "<NO PROJECT>";
            }
            else
            {
                projectName = FileManager.RemovePath(FileManager.RemoveExtension(projectName));
            }
            var taskDetail = $"{DateTime.Now.ToString("hh:mm:ss tt")} {projectName} {taskDisplayInfo}";
            taskHistory.Add(taskDetail);


            while (taskHistory.Count > maxTasksInHistory)
            {
                taskHistory.RemoveAt(0);
            }
        }

        public void OnUiThread(Func<Task> action)
        {
            global::Glue.MainGlueWindow.Self.Invoke(() => action.Invoke().Wait());
        }

        public void OnUiThread(Action action)
        {
            global::Glue.MainGlueWindow.Self.Invoke(action);
        }

        public bool IsInTask() => TaskManager.Self.SyncTaskThreadId != null && System.Threading.Thread.CurrentThread.ManagedThreadId == TaskManager.Self.SyncTaskThreadId;

        public GlueTask AddOrRunIfTasked(Action action, string displayInfo, TaskExecutionPreference executionPreference = TaskExecutionPreference.Fifo)
        {
            if (IsInTask())
            {
                // we're in a task:
                action();
                return null;
            }
            else
            {
                return TaskManager.Self.Add(action, displayInfo, executionPreference);
            }
        }

        #endregion
    }
}
