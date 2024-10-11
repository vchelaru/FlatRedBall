using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Tasks;
using FlatRedBall.IO;
using Nito.AsyncEx;

namespace FlatRedBall.Glue.Managers
{
    #region Enums

    public enum TaskExecutionPreference : ulong
    {
        Asap = 0,
        Fifo = 1 * (ulong.MaxValue/3),
        AddOrMoveToEnd = 2 * (ulong.MaxValue/3),
    }

    public enum TaskEvent
    {
        Created,
        Queued,
        Started,
        StartedImmediate,
        Removed,
        MovedToEnd
    }

    #endregion

    public class TaskManager : Singleton<TaskManager>
    {
        #region Fields
        int asyncTasks;

        List<GlueTaskBase> mActiveParallelTasks = new List<GlueTaskBase>();

        const int maxTasksInHistory = 121;
        List<string> taskHistory = new List<string>();

        public int? SyncTaskThreadId { get; private set; }

        // This implementation of BlockingCollection with ConcurrentPriorityQueue is explained here:
        // https://stackoverflow.com/questions/7502615/element-order-in-blockingcollection
        // As of March 15, it's the last answer

        readonly BlockingCollection<KeyValuePair<ulong, GlueTaskBase>> taskQueue = new BlockingCollection<KeyValuePair<ulong, GlueTaskBase>>(new ConcurrentPriorityQueue<ulong, GlueTaskBase>());

        ConcurrentDictionary<string, GlueTaskBase> addOrMoveToEndTaskQueueTaskIds = new ();

        const string RestartTaskDisplay = "Restarting due to Glue or file change";
        public bool HasRestartTask => taskQueue.Any(item => item.Value.DisplayInfo == RestartTaskDisplay);







        #endregion

        #region Properties

        GlueTaskBase CurrentlyRunningTask;

        public bool AreAllAsyncTasksDone => 
            TaskCountAccurate == 0

            ;

        /// <summary>
        /// Returns the task count, including cancelled tasks.
        /// </summary>
        public int TaskCount
        {
            get
            {
                lock (mActiveParallelTasks)
                {
                    var toReturn = mActiveParallelTasks.Count + asyncTasks +
                        //taskQueue.Where(item => item.Value.IsCancelled == false).Count();
                        // This could be much faster with systems that have a lot of tasks
                        taskQueueCount;
                    if (CurrentlyRunningTask != null)
                    {
                        toReturn++;
                    }
                    return toReturn;
                }
            }
        }

        /// <summary>
        /// Returns the number of tasks by actually counting them rather than relying on the taskQueueCount which could be incorrect
        /// </summary>
        public int TaskCountAccurate
        {
            get
            {
                lock (mActiveParallelTasks)
                {
                    var toReturn = mActiveParallelTasks.Count + asyncTasks +
                        taskQueue.Where(item => item.Value.IsCancelled == false).Count();
                    // This could be much faster with systems that have a lot of tasks

                    if (CurrentlyRunningTask != null)
                    {
                        toReturn++;
                    }
                    if(toReturn == 0)
                    {
                        taskQueueCount = 0;
                    }
                    return toReturn;
                }
            }
        }

        public string CurrentTaskDescription
        {
            get
            {
                string toReturn = "";

                if(IsTaskProcessingEnabled == false)
                {
                    toReturn += "Task processing disabled, next task when re-enabled:\n";
                }

                if (mActiveParallelTasks.Count != 0)
                {
                    // This could update while we're looping. We don't want to throw errors, don't want to lock anything, 
                    // so just handle it with a try catch:
                    try
                    {
                        foreach(var item in mActiveParallelTasks)
                        {
                            toReturn += item.DisplayInfo + "\n";
                        }
                    }
                    catch
                    {
                        // do nothing
                    }

                }

                if (taskQueue.Count != 0)
                {
                    try
                    {
                        toReturn += taskQueue.FirstOrDefault().Value?.DisplayInfo;
                    }
                    catch
                    {
                        // do nothing
                    }
                }

                var currentlyRunning = CurrentlyRunningTask;
                if(currentlyRunning != null)
                {
                    return currentlyRunning.DisplayInfo;
                }

                return toReturn;
            }
        }

        public string AllTasksDescription
        {
            get
            {
                StringBuilder toReturn = new StringBuilder();

                if (IsTaskProcessingEnabled == false)
                {
                    toReturn.AppendLine("Task processing disabled, next task when re-enabled:");
                }

                var currentlyRunning = CurrentlyRunningTask;
                if (currentlyRunning != null)
                {
                    toReturn.AppendLine(GetDetails(currentlyRunning));
                }

                var taskQueueArray = taskQueue.ToArray();

                foreach (var item in taskQueueArray)
                {
                    if(item.Value.IsCancelled == false && item.Value != currentlyRunning)
                    {
                        toReturn.AppendLine(GetDetails(item.Value));
                    }
                }

                string GetDetails(GlueTaskBase taskBase) => $"{taskBase?.DisplayInfo} ({taskBase?.TaskExecutionPreference})";
                return toReturn.ToString();
            }
        }

        public string NextTasksDescription
        {
            get
            {
                string toReturn = "";

                if (IsTaskProcessingEnabled == false)
                {
                    toReturn += "Task processing disabled, next task when re-enabled:\n";
                }


                var currentlyRunning = CurrentlyRunningTask;
                if (currentlyRunning != null)
                {
                    toReturn += GetDetails(currentlyRunning);
                }

                var tasksToPrint = taskQueue
                    .Where(item => item.Value.IsCancelled == false && item.Value != currentlyRunning)
                    .Take(10)
                    .ToArray();
                foreach(var item in tasksToPrint)
                {
                    toReturn += GetDetails(item.Value);
                }
                string GetDetails(GlueTaskBase taskBase) => $"{taskBase?.DisplayInfo} ({taskBase?.TaskExecutionPreference})\n";
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
                //if(turnedOn)
                //{
                //    ProcessNextSync();

                //}
            }
        }

        #endregion

        #region Events

        public event Action<TaskEvent, GlueTaskBase> TaskAddedOrRemoved;

        #endregion

        public TaskManager()
        {
            new Thread(StartDoTaskManagerLoop)
            {
                IsBackground = true
            }.Start();
        }

        void StartDoTaskManagerLoop()
        {
            var shouldForceOnSameThread = true;

            if(shouldForceOnSameThread)
            {
                AsyncContext.Run(DoTaskManagerLoop);
            }
            else
            {
                DoTaskManagerLoop();
            }
        }

        const int TaskManagerLoopDelayMs = 50;

        async void DoTaskManagerLoop()
        {
            SyncTaskThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;

            while (true)
            {
                // `taskQueue` is a blocking collection, which means if we call `GetConsumingEnumerable()` it will 
                // sit here and block the thread while waiting for an item to be added to the collection. However, 
                // the intention of the task manager is that all tasks that are executed are stickied to the 
                // same thread, and thus all code executed by a task is executed by this same thread. This is done
                // via the `AsyncContext.Run()` used to invoke the current method.
                //
                // Due to the desire for this to be single threaded, if a task's implementation calls a fire and
                // forget async method via calling an `async void` method, then once the first `await` call is hit,
                // the task will no longer executed, as the Task continuation will be waiting to be marshalled to
                // the current thread, but that thread is "active" waiting for the blocking collection to return.
                //
                // To fix this, instead of looping over the blocking collection forever, we instead grab each task
                // out of the queue and execute it, and once we have no items left in the queue we use a 
                // `Task.Delay()` to free the thread up for handling any pending continuation tasks that are waiting.
                //
                // Related to https://github.com/vchelaru/FlatRedBall/issues/1412.
                if (taskQueue.TryTake(out var item))
                {
                    try
                    {
                        if (item.Value.IsCancelled == false)
                        {
                            taskQueueCount--;
                            if (isTaskProcessingEnabled)
                            {
                                // Remove before adding - we want to err on the side of running things more
                                // rather than less, and removing it earlier will increase the chances of
                                // it running again.
                                if(item.Value.TaskExecutionPreference == TaskExecutionPreference.AddOrMoveToEnd)
                                {
                                    addOrMoveToEndTaskQueueTaskIds.Remove(item.Value.EffectiveId, out _);
                                }
                                await RunTask(item.Value, markAsCurrent: true);

                                if (System.Threading.Thread.CurrentThread.ManagedThreadId != SyncTaskThreadId)
                                {
                                    string message = "TaskManager.RunTask should be running on the SyncTaskThreadId. " +
                                                     "This may cause issues. Task: " + item.Value.DisplayInfo;
                                    GlueCommands.Self.PrintError(message);
                                    int m = 3;
                                }

                            }

                            else
                            {
                                AddInternal(item.Value);
                                System.Threading.Thread.Sleep(50);

                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        GlueCommands.Self.PrintError(ex.ToString());
                    }
                }
                else
                {
                    await Task.Delay(TaskManagerLoopDelayMs);
                }
            }
        }

        private async Task RunTask(GlueTaskBase task, bool markAsCurrent)
        {
            if(task.IsCancelled == false)
            {
                if(markAsCurrent) CurrentlyRunningTask = task;
                TaskAddedOrRemoved?.Invoke(TaskEvent.Started, task);
                task.TimeStarted = DateTime.Now;
                try
                {
                    await task.Do_Action_Internal();
                }
                finally
                {
                    task.TimeEnded = DateTime.Now;
                    // Set it to null before raising the event so that the TaskCount uses a null object.
                    if (markAsCurrent) CurrentlyRunningTask = null;
                    TaskAddedOrRemoved?.Invoke(TaskEvent.Removed, task);
                }
            }

        }

        public void Dispose()
        {
            taskQueue.CompleteAdding();
            taskQueue.Dispose();
        }

        #region Add Tasks
        /// <summary>
        /// Adds a task which can execute simultaneously with other tasks
        /// </summary>
        /// <param name="action">The action to perform.</param>
        /// <param name="details">The details of the task, to be displayed in the tasks window.</param>
        [Obsolete]
        public void AddParallelTask(Action action, string details)
        {

            ThreadPool.QueueUserWorkItem(
                (arg)=>ExecuteParallelAction(action, details));
        }

        /// <summary>
        /// Adds an action to be executed, guaranteeing that no other actions will be executed at the same time as this.
        /// Actions added will be executed in the order they were added (fifo).
        /// </summary>
        [Obsolete("Use Add, which allows specifying the priority")]
        public void AddSync(Action action, string displayInfo) => Add(action, displayInfo);

        /// <summary>
        /// Force adds a task to the queue, even if already in a task. To optionally add if not in a task, see AddAsync.
        /// </summary>
        public GlueTask Add(Action action, string displayInfo, TaskExecutionPreference executionPreference = TaskExecutionPreference.Fifo, bool doOnUiThread = false, string customId = null)
        {
            var glueTask = new GlueTask();
            glueTask.Action = action;
            glueTask.DoOnUiThread = doOnUiThread;
            glueTask.TaskExecutionPreference = executionPreference;
            glueTask.DisplayInfo = displayInfo;
            glueTask.CustomId = customId;

            AddInternal(glueTask);
            return glueTask;
        }

        public GlueTask<T> Add<T>(Func<T> func, string displayInfo, TaskExecutionPreference executionPreference = TaskExecutionPreference.Fifo, bool doOnUiThread = false, string customId = null)
        {
            var glueTask = new GlueTask<T>();
            glueTask.Func = func;
            glueTask.DoOnUiThread = doOnUiThread;
            glueTask.TaskExecutionPreference = executionPreference;
            glueTask.DisplayInfo = displayInfo;
            glueTask.CustomId=customId;
            AddInternal(glueTask);
            return glueTask;
        }

        /// <summary>
        /// Force adds a task to the queue, even if already in a task
        /// </summary>
        public GlueAsyncTask Add(Func<Task> func, string displayInfo, TaskExecutionPreference executionPreference = TaskExecutionPreference.Fifo, bool doOnUiThread = false, string customId = null)
        {
            var glueTask = new GlueAsyncTask();
            glueTask.Func = func;
            glueTask.DoOnUiThread = doOnUiThread;
            glueTask.TaskExecutionPreference = executionPreference;
            glueTask.DisplayInfo = displayInfo;
            glueTask.CustomId = customId;
            AddInternal(glueTask);
            return glueTask;
        }

        /// <summary>
        /// Adds an action to the TaskManager to be executed according to the argument TaskExecutionPreference. If the 
        /// callstack is already part of a task, then the action is executed immediately. The returned task will complete
        /// when the argument Action has completed.
        /// </summary>
        /// <param name="action">The action to execute.</param>
        /// <param name="displayInfo">The display info to show in the task manager tab</param>
        /// <param name="executionPreference">When to execute the action.</param>
        /// <param name="doOnUiThread">Whether the action must be performed on the UI thread.</param>
        /// <returns>A task which will complete once the action has finished executing.</returns>
        public Task AddAsync(Action action, string displayInfo, TaskExecutionPreference executionPreference = TaskExecutionPreference.Fifo, bool doOnUiThread = false, string customId = null)
        {
            var glueTask = AddOrRunIfTasked(action, displayInfo, executionPreference, doOnUiThread, customId);
            return WaitForTaskToFinish(glueTask);
        }

        public async Task<T> AddAsync<T>(Func<T> func, string displayInfo, TaskExecutionPreference executionPreference = TaskExecutionPreference.Fifo, bool doOnUiThread = false)
        {
            var glueTask = AddOrRunIfTasked(func, displayInfo, executionPreference, doOnUiThread);
            return await WaitForTaskToFinish(glueTask);
        }

        /// <summary>
        /// Adds a function which returns a Task. If the callstack is already part of a task, 
        /// then the action is executed immediately.The returned task will be completed when the internal operation is executed and completes.  
        /// </summary>
        /// <param name="func">The function to add to the internal queue.</param>
        /// <param name="displayInfo">The information to display to the user.</param>
        /// <param name="executionPreference">The execution preference for the task.</param>
        /// <param name="doOnUiThread">Whether the task must be done on UI thread. If the task directly accesses UI, then this should be true.</param>
        /// <returns></returns>
        public async Task AddAsync(Func<Task> func, string displayInfo, TaskExecutionPreference executionPreference = TaskExecutionPreference.Fifo, bool doOnUiThread = false)
        {
            var glueTask = await AddOrRunIfTasked(func, displayInfo, executionPreference, doOnUiThread);
            await WaitForTaskToFinish(glueTask);
        }


        public GlueTask AddOrRunIfTasked(Action action, string displayInfo, TaskExecutionPreference executionPreference = TaskExecutionPreference.Fifo, bool doOnUiThread = false, string customId = null)
        {
            if (IsInTask() && 
                // If the user is moving the tasks to the end, then we will push it at the end always
                executionPreference != TaskExecutionPreference.AddOrMoveToEnd)
            {

                // we're in a task:
                var task = new GlueTask()
                {
                    DisplayInfo = displayInfo,
                    Action = action,
                    TaskExecutionPreference = executionPreference,
                    DoOnUiThread = doOnUiThread,
                    CustomId = customId
                };

                TaskAddedOrRemoved?.Invoke(TaskEvent.StartedImmediate, task);
                RunTask(task, markAsCurrent:false).Wait();
                TaskAddedOrRemoved?.Invoke(TaskEvent.Removed, task);

                return task;
            }
            else
            {
                return TaskManager.Self.Add(action, displayInfo, executionPreference, doOnUiThread);
            }
        }

        public async Task<GlueAsyncTask> AddOrRunIfTasked(Func<Task> func, string displayInfo, TaskExecutionPreference executionPreference = TaskExecutionPreference.Fifo, bool doOnUiThread = false)
        {
            if (IsInTask() &&
                // If the user is moving the tasks to the end, then we will push it at the end always
                executionPreference != TaskExecutionPreference.AddOrMoveToEnd)
            {
                // we're in a task:
                var task = new GlueAsyncTask()
                {
                    DisplayInfo = displayInfo,
                    Func = func,
                    TaskExecutionPreference = executionPreference,
                    DoOnUiThread = doOnUiThread
                };

                TaskAddedOrRemoved?.Invoke(TaskEvent.StartedImmediate, task);
                await RunTask(task, markAsCurrent: false);
                TaskAddedOrRemoved?.Invoke(TaskEvent.Removed, task);

                return task;
            }
            else
            {
                return TaskManager.Self.Add(func, displayInfo, executionPreference, doOnUiThread);
            }
        }

        public GlueTask<T> AddOrRunIfTasked<T>(Func<T> func, string displayInfo, TaskExecutionPreference executionPreference = TaskExecutionPreference.Fifo, bool doOnUiThread = false)
        {
            if (IsInTask() &&
                // If the user is moving the tasks to the end, then we will push it at the end always
                executionPreference != TaskExecutionPreference.AddOrMoveToEnd)
            {
                // we're in a task:
                var task = new GlueTask<T>()
                {
                    DisplayInfo = displayInfo,
                    Func = func,
                    TaskExecutionPreference = executionPreference,
                    DoOnUiThread = doOnUiThread,
                };

                RunTask(task, markAsCurrent: false).Wait();

                return task;
            }
            else
            {
                return TaskManager.Self.Add(func, displayInfo, executionPreference, doOnUiThread);
            }
        }

        //Dictionary<string, int> addCalls = new Dictionary<string, int>();
        ulong taskoffset = 0;
        int taskQueueCount = 0;
        private void AddInternal(GlueTaskBase glueTask)
        {
            var priorityValue = (ulong)glueTask.TaskExecutionPreference;
            priorityValue += taskoffset;
            taskoffset++;

            var wasMoved = false;
            if(glueTask.TaskExecutionPreference == TaskExecutionPreference.AddOrMoveToEnd)
            {
                var effectiveId = glueTask.EffectiveId;

                var isPresent = addOrMoveToEndTaskQueueTaskIds.ContainsKey(effectiveId);
                if(isPresent)
                {
                    addOrMoveToEndTaskQueueTaskIds.TryGetValue(effectiveId, out var existing);

                    if (existing != null)
                    {
                        existing.IsCancelled = true;
                        wasMoved = true;

                        taskQueueCount--;
                    }
                }


            }

            if(wasMoved == false)
            {
                TaskAddedOrRemoved?.Invoke(TaskEvent.Queued, glueTask);
            }
            else
            {
                TaskAddedOrRemoved?.Invoke(TaskEvent.MovedToEnd, glueTask);
            }

            taskQueue.Add(new KeyValuePair<ulong, GlueTaskBase>(priorityValue, glueTask));

            if(glueTask.TaskExecutionPreference == TaskExecutionPreference.AddOrMoveToEnd)
            {
                addOrMoveToEndTaskQueueTaskIds[glueTask.EffectiveId] = glueTask;
            }

            taskQueueCount++;

        }


        #endregion

        #region Wait Tasks

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

        bool DoesTaskNeedToFinish(GlueTaskBase glueTask)
        {
            if(glueTask.IsCancelled)
            {
                return false;
            }
            else
            {
                return
                    glueTask.TimeStarted == null ||
                    taskQueue.Any(item => item.Value == glueTask) ||
                    mActiveParallelTasks.Contains(glueTask) ||
                    CurrentlyRunningTask == glueTask;
            }

        }

        public async Task WaitForTaskToFinish(GlueTaskBase glueTask)
        {
            if(glueTask == null)
            {
                return;
            }
            else
            {
                bool IsTaskDone()
                {
                    lock(mActiveParallelTasks)
                    {
                        if(DoesTaskNeedToFinish(glueTask))
                        {
                            return false;
                        }

                        return true;
                    }
                }

                while(!IsTaskDone())
                {
                    const int waitDelay = 30;
                    await Task.Delay(waitDelay);
                }
            }
        }

        public async Task<T> WaitForTaskToFinish<T>(GlueTask<T> glueTask)
        {
            if (glueTask == null)
            {
                return default(T);
            }
            else
            {
                bool IsTaskDone()
                {
                    lock (mActiveParallelTasks)
                    {
                        if (DoesTaskNeedToFinish(glueTask))
                        {
                            return false;
                        }

                        return true;
                    }
                }

                while (!IsTaskDone())
                {
                    await Task.Delay(150);
                }
                return (T)glueTask.Result;
            }
        }

        public async Task WaitForTaskToFinish(string taskDescription)
        {
            GlueTaskBase task = null;
            if(addOrMoveToEndTaskQueueTaskIds.ContainsKey(taskDescription))
            {
                addOrMoveToEndTaskQueueTaskIds.TryGetValue(taskDescription, out task);
            }

            if(task == null)
            {
                var taskInQueue = taskQueue.FirstOrDefault(item => item.Value.DisplayInfo == taskDescription);
                task = taskInQueue.Value;

            }
            
            if(task == null)
            {
                task = mActiveParallelTasks.FirstOrDefault(item => item.DisplayInfo == taskDescription);
            }

            if(task != null)
            {
                await WaitForTaskToFinish(task);
            }
        }

        #endregion

        #region Etc Methods


        void ExecuteParallelAction(Action action, string details)
        {
            var glueTask = new GlueTask
            {
                Action = action,
                DisplayInfo = details
            };

            lock (mActiveParallelTasks)
            {
                mActiveParallelTasks.Add(glueTask);
            }

            TaskAddedOrRemoved?.Invoke(TaskEvent.Queued, glueTask);

            ((Action)action)();

            lock (mActiveParallelTasks)
            {
                mActiveParallelTasks.Remove(glueTask);
            }
            asyncTasks--;

            // not sure why but this can go into the negative...
            asyncTasks = System.Math.Max(asyncTasks, 0);

            TaskAddedOrRemoved?.Invoke(TaskEvent.Removed, glueTask);
        }


        public void RecordTaskHistory(string taskDisplayInfo)
        {
            var projectName = GlueState.Self.CurrentMainProject?.FullFileName;
            
            var taskDetail = $"{DateTime.Now.ToString("hh:mm:ss tt")} {projectName} {taskDisplayInfo}";
            taskHistory.Add(taskDetail);


            while (taskHistory.Count > maxTasksInHistory)
            {
                taskHistory.RemoveAt(0);
            }
        }

        public void OnUiThread(Func<Task> action)
        {
            if(IsOnUiThread)
            {
                action.Invoke().Wait();
            }
            else
            {
                global::Glue.MainGlueWindow.Self.Invoke(() => action.Invoke().Wait());
            }
        }

        public void OnUiThread(Action action)
        {
            if (IsOnUiThread)
            {
                action();
            }
            else
            {
                global::Glue.MainGlueWindow.Self.Invoke(action);
            }
        }

        public void BeginOnUiThread(Action action)
        {
            if (IsOnUiThread)
            {
                action();
            }
            else
            {
                global::Glue.MainGlueWindow.Self.BeginInvoke(action);
            }
        }

        public bool IsOnUiThread => System.Threading.Thread.CurrentThread.ManagedThreadId == global::Glue.MainGlueWindow.UiThreadId;

        public bool IsInTask()
        {
            var currentThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
            if (currentThreadId == TaskManager.Self.SyncTaskThreadId)
            {
                return true;
            }

            // It's possible we may not be in a task based on threads, but we are 
            // still in the task based on callstack because we're running on the UI thread.
            // So we need to do the expensive operation of checking the callstack.
            var stackTrace = new System.Diagnostics.StackTrace();
            //var stackFrame = new System.Diagnostics.StackFrame();

            List<string> frameTexts = new List<string>();
            //for (int i = stackTrace.FrameCount - 1; i > -1; i--)
            //{
            //    var frame = stackTrace.GetFrame(i);
            //    var frameText = frame.ToString();

            //    frameTexts.Add(frameText);
            //}

            /* For debugging:
             * 

            foreach(var frameText in frameTexts)
            { 
                if (frameText.StartsWith("RunOnUiThreadTasked ") || 
                    // Vic says - not sure why but sometimes thread IDs change when in an async function.
                    // So I thought I could check if the thread is the main task thread, but this won't work
                    // because command receiving from the game runs on a separate thread, so that would behave
                    // as if it's tasked, even though it's not
                    // so we check this:
                    frameText.StartsWith(nameof(GlueTask.Do_Action_Internal) + " "))
                {
                    return true;
                }
            }
            */
            for (int i = stackTrace.FrameCount - 1; i > -1; i--)
            {
                var frame = stackTrace.GetFrame(i);
                var frameText = frame.ToString();

                var isTasked = frameText.StartsWith("RunOnUiThreadTasked");

                if (isTasked)
                {
                    return true;
                }
            }

            return false;
        }

        public void WarnIfNotInTask()
        {
            if(!IsInTask())
            {
                var stackTrace = Environment.StackTrace;

            }
        }

        #endregion
    }

}
