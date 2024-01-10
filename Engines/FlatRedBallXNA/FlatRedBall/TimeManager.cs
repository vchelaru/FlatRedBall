using System.Collections.Generic;

using Microsoft.Xna.Framework;
using System.Text;

using System.Threading;
using FlatRedBall.IO;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;

namespace FlatRedBall
{
    #region Enums
    /// <summary>
    /// Represents the unit of time measurement.  This can be used in files that store timing information.
    /// </summary>
    public enum TimeMeasurementUnit
    {
        Undefined,
        Millisecond,
        Second
    }
    #endregion

    #region Struct

    public struct TimedSection
    {
        public string Name;
        public double Time;

        public override string ToString()
        {
            return Name + ": " + Time;
        }
    }

    struct TimedTasks
    {
        public double Time;
        public TaskCompletionSource<object> TaskCompletionSource;
    }

    struct PredicateTask
    {
        public Func<bool> Predicate;
        public TaskCompletionSource<object> TaskCompletionSource;
    }

    struct FrameTask
    {
        public int FrameIndex;
        public TaskCompletionSource<object> TaskCompletionSource;
    }

    #endregion
    
    /// <summary>
    /// Class providing timing information for the current frame, absolute time since the game has started running, and for the current screen.
    /// </summary>
    public static class TimeManager
    {
        #region Classes

        struct VoidTaskResult { }



        #endregion

        #region Fields

        static float mSecondDifference;
        static float mLastSecondDifference;
        static float mSecondDifferenceSquaredDividedByTwo;

        /// <summary>
        /// The amount of time in seconds since the game started running. 
        /// This value is updated once-per-frame so it will 
        /// always be the same value until the next frame is called.
        /// This value does not consider pausing. To consider pausing, see CurrentScreenTime.
        /// </summary>
        /// <remarks>
        /// This value can be used to uniquely identify a frame.
        /// </remarks>
        public static double CurrentTime;
        public static int CurrentFrame;

        static double mLastCurrentTime;

        static double mTimeFactor = 1.0f;

        static double mCurrentTimeForTimedSections;

        static System.Diagnostics.Stopwatch stopWatch;

        static List<double> sections = new List<double>();
        static List<string> sectionLabels = new List<string>();

        static List<double> lastSections = new List<double>();
        static List<string> lastSectionLabels = new List<string>();

        static Dictionary<string, double> mPersistentSections = new Dictionary<string, double>();
        static double mLastPersistentTime;

        static Dictionary<string, double> mSumSections = new Dictionary<string, double>();
        static Dictionary<string, int> mSumSectionHitCount = new Dictionary<string, int>();
        static double mLastSumTime;

        static StringBuilder stringBuilder;

        static bool mTimeSectionsEnabled = true;

        static bool mIsPersistentTiming = false;

        static GameTime mLastUpdateGameTime;

		static TimeMeasurementUnit mTimedSectionReportngUnit = TimeMeasurementUnit.Millisecond;

		static float mMaxFrameTime = 0.5f;

        static readonly List<TimedTasks> screenTimeDelayedTasks = new List<TimedTasks>();
        static readonly List<PredicateTask> predicateTasks = new List<PredicateTask>();
        static readonly List<FrameTask> frameTasks = new List<FrameTask>();

        #endregion

        #region Properties

        public static double LastCurrentTime
        {
            get { return mLastCurrentTime; }
        }

        /// <summary>
        /// The number of seconds (usually a fraction of a second) since
        /// the last frame.  This value can be used for time-based movement.
        /// This value is changed once per frame, and will remain constant within each frame, assuming a consant TimeFactor.
        /// Changing the TimeFactor adjusts this value.
        /// </summary>
        public static float SecondDifference
        {
            get { return mSecondDifference; }
        }

        public static float LastSecondDifference
        {
            get { return mLastSecondDifference; }
        }

        public static float SecondDifferenceSquaredDividedByTwo
        {
            get { return mSecondDifferenceSquaredDividedByTwo; }
        }

        public static bool TimeSectionsEnabled
        {
            get { return mTimeSectionsEnabled; }
            set { mTimeSectionsEnabled = value; }
        }

        /// <summary>
        /// A multiplier for how fast time runs.  This is 1 by default.  Setting
        /// this value to 2 will make everything run twice as fast. Increasing this value
        /// effectively increases the SecondDifference value, so custom code which is time-based
        /// will behave properly when TimeFactor is adjusted.
        /// </summary>
        public static double TimeFactor
        {
            get { return mTimeFactor; }
            set { mTimeFactor = value; }
        }

        public static GameTime LastUpdateGameTime
        {
            get { return mLastUpdateGameTime; }
        }

		public static TimeMeasurementUnit TimedSectionReportingUnit
        {
            get { return mTimedSectionReportngUnit; }
            set { mTimedSectionReportngUnit = value; }
        }

		public static float MaxFrameTime
		{
			get { return mMaxFrameTime; }
			set { mMaxFrameTime = value; }
		}

        /// <summary>
        /// Returns the amount of time since the current screen started. This value does not 
        /// advance when the screen is paused.
        /// </summary>
        /// <remarks>
        /// This value is the same as 
        /// Screens.ScreenManager.CurrentScreen.PauseAdjustedCurrentTime
        /// </remarks>
        public static double CurrentScreenTime => Screens.ScreenManager.CurrentScreen.PauseAdjustedCurrentTime;

        public static Dictionary<string, double> SumSectionDictionary
        {
            get { return mSumSections; }
        }

        [Obsolete("Use CurrentSystemTime as that name is more consistent and this will eventually be removed.")]
        public static double SystemCurrentTime
        {
            get 
            { 
                return stopWatch.Elapsed.TotalSeconds; 
            }

        }

        public static double CurrentSystemTime => stopWatch.Elapsed.TotalSeconds; 


        public static int TimedSectionCount
        {
            get { return sections.Count; }
        }

        public static bool SetNextFrameTimeTo0
        {
            get; set;
        }



        #endregion

        #region Methods

        public static void CreateXmlSumTimeSectionReport(string fileName)
        {
            List<TimedSection> tempList = GetTimedSectionList();

            FileManager.XmlSerialize<List<TimedSection>>(tempList, fileName);
        }

        public static List<TimedSection> GetTimedSectionList()
        {
            List<TimedSection> tempList = new List<TimedSection>(
                mSumSections.Count);

            foreach (KeyValuePair<string, double> kvp in mSumSections)
            {
                TimedSection timedSection = new TimedSection()
                {
                    Name = kvp.Key,
                    Time = kvp.Value
                };

                tempList.Add(timedSection);
            }
            return tempList;
        }

        /// <summary>
        /// Allows the game component to perform any initialization it needs to before starting
        /// to run.  This is where it can query for any required services and load content.
        /// </summary>
        public static void Initialize()
        {
            stringBuilder = new StringBuilder(200);

            InitializeStopwatch();
        }

        public static void InitializeStopwatch()
        {
            if(stopWatch == null)
            {
                // This may be initialized outside of FRB if the user is trying to time pre-FRB calls
                stopWatch = new System.Diagnostics.Stopwatch();
                stopWatch.Start();
            }
        }

        #region TimeSection code

        public static string GetPersistentTimedSections()
        {

            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            foreach (KeyValuePair<string, double> kvp in mPersistentSections)
            {
                sb.Append(kvp.Key).Append(": ").AppendLine(kvp.Value.ToString());
            }

            mIsPersistentTiming = false;

            return sb.ToString();
        }


        public static string GetSumTimedSections()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            foreach (KeyValuePair<string, double> kvp in mSumSections)
            {
				if (TimedSectionReportingUnit == TimeMeasurementUnit.Millisecond)
				{
					sb.Append(kvp.Key).Append(": ").AppendLine((kvp.Value * 1000.0f).ToString("f2"));
				}
				else
				{
					sb.Append(kvp.Key).Append(": ").AppendLine(kvp.Value.ToString());
				}
            }

            return sb.ToString();
        }


        public static string GetTimedSections(bool showTotal)
        {
            stringBuilder.Remove(0, stringBuilder.Length);

            int largestIndex = -1;
            double longestTime = -1;

            for (int i = 0; i < lastSections.Count; i++)
            {
                if (lastSections[i] > longestTime)
                {
                    longestTime = lastSections[i];
                    largestIndex = i;
                }
            }

            for (int i = 0; i < lastSections.Count; i++)
            {
                if (i == largestIndex)
                {
					if (lastSectionLabels[i] != "")
					{
						if (TimedSectionReportingUnit == TimeMeasurementUnit.Millisecond)
						{
							stringBuilder.Append("-!-" + lastSectionLabels[i]).Append(": ").Append(lastSections[i].ToString("f2")).Append("\n");
						}
						else
						{
							stringBuilder.Append("-!-" + lastSectionLabels[i]).Append(": ").Append(lastSections[i].ToString()).Append("\n");
						}
					}
					else
					{
						if (TimedSectionReportingUnit == TimeMeasurementUnit.Millisecond)
						{
							stringBuilder.Append("-!-" + lastSections[i].ToString("f2")).Append("\n");
						}
						else
						{
							stringBuilder.Append("-!-" + lastSections[i].ToString()).Append("\n");
						}
					}
                }
                else
                {
					if (lastSectionLabels[i] != "")
					{
						if (TimedSectionReportingUnit == TimeMeasurementUnit.Millisecond)
						{
							stringBuilder.Append(lastSectionLabels[i]).Append(": ").Append(lastSections[i].ToString("f2")).Append("\n");
						}
						else
						{
							stringBuilder.Append(lastSectionLabels[i]).Append(": ").Append(lastSections[i].ToString()).Append("\n");
						}
					}
					else
					{
						if (TimedSectionReportingUnit == TimeMeasurementUnit.Millisecond)
						{
							stringBuilder.Append(lastSections[i].ToString("f2")).Append("\n");
						}
						else
						{
							stringBuilder.Append(lastSections[i].ToString()).Append("\n");
						}
					}
                }
            }

            if (showTotal)
			{
				if (TimedSectionReportingUnit == TimeMeasurementUnit.Millisecond)
				{
					stringBuilder.Append("Total Timed: " + ((TimeManager.CurrentSystemTime - TimeManager.mCurrentTimeForTimedSections) * 1000.0f).ToString("f2"));
				}
				else
				{
					stringBuilder.Append("Total Timed: " + (TimeManager.CurrentSystemTime - TimeManager.mCurrentTimeForTimedSections));
				}
			}

            return stringBuilder.ToString();

        }


        public static void PersistentTimeSection(string label)
        {
            if (mIsPersistentTiming)
            {
                double currentTime = CurrentSystemTime;
                if (mPersistentSections.ContainsKey(label))
                {
					if (TimedSectionReportingUnit == TimeMeasurementUnit.Millisecond)
					{
						mPersistentSections[label] = ((currentTime - mLastPersistentTime) * 1000.0f);
					}
					else
					{
						mPersistentSections[label] = currentTime - mLastPersistentTime;
					}
                }
                else
                {
					if (TimedSectionReportingUnit == TimeMeasurementUnit.Millisecond)
					{
						mPersistentSections.Add(label, (currentTime - mLastPersistentTime) * 1000.0f);
					}
					else
					{
						mPersistentSections.Add(label, currentTime - mLastPersistentTime);
					}
                }

                mLastPersistentTime = currentTime;
            }
        }



        public static void StartPersistentTiming()
        {
            mPersistentSections.Clear();

            mIsPersistentTiming = true;

            mLastPersistentTime = CurrentSystemTime;
        }

        /// <summary>
        /// Begins Sum Timing
        /// </summary>
        /// <remarks>
        /// <code>
        /// 
        /// StartSumTiming();
        /// 
        /// foreach(Sprite sprite in someSpriteArray)
        /// {
        ///     SumTimeRefresh();
        ///     PerformSomeFunction(sprite);
        ///     SumTimeSection("PerformSomeFunction time:");
        /// 
        /// 
        ///     SumTimeRefresh();
        ///     PerformSomeOtherFunction(sprite);
        ///     SumTimeSection("PerformSomeOtherFunction time:);
        /// 
        /// }
        /// </code>
        ///
        /// </remarks>
        public static void StartSumTiming()
        {
            mSumSections.Clear();
            mSumSectionHitCount.Clear();

            mLastSumTime = CurrentSystemTime;
        }


        public static void SumTimeSection(string label)
        {
            double currentTime = CurrentSystemTime;
            if (mSumSections.ContainsKey(label))
            {
                mSumSections[label] += currentTime - mLastSumTime;
                //mSumSectionHitCount[label]++;
            }
            else
            {
                mSumSections.Add(label, currentTime - mLastSumTime);
                //mSumSectionHitCount.Add(label, 1);
            }
            mLastSumTime = currentTime;
        }


        public static void SumTimeRefresh()
        {
            mLastSumTime = CurrentSystemTime;
        }

        /// <summary>
        /// Stores an unnamed timed section.
        /// </summary>
        /// <remarks>
        /// A timed section is the amount of time (in seconds) since the last time either Update
        /// or TimeSection has been called.  The sections are reset every time Update is called.
        /// The sections can be retrieved through the GetTimedSections method.
        /// <seealso cref="FRB.TimeManager.GetTimedSection"/>
        /// </remarks>
        public static void TimeSection()
        {
            TimeSection("");
        }


        /// <summary>
        /// Stores an named timed section.
        /// </summary>
        /// <remarks>
        /// A timed section is the amount of time (in seconds) since the last time either Update
        /// or TimeSection has been called.  The sections are reset every time Update is called.
        /// The sections can be retrieved through the GetTimedSections method.
        /// <seealso cref="FRB.TimeManager.GetTimedSection"/>
        /// </remarks>
        /// <param name="label">The label for the timed section.</param>
        public static void TimeSection(string label)
        {
            if (mTimeSectionsEnabled)
            {
                Monitor.Enter(sections);

                double f = (CurrentSystemTime - mCurrentTimeForTimedSections);
                if (TimedSectionReportingUnit == TimeMeasurementUnit.Millisecond)
                {
                    f *= 1000.0f;
                }

                for (int i = sections.Count - 1; i > -1; i--)
                    f -= sections[i];


                sections.Add(f);
                sectionLabels.Add(label);

                Monitor.Exit(sections);
            }
        }

        #endregion

        /// <summary>
        /// Returns the number of seconds which have passed since the argument value in game time.
        /// This value continues to increment when the screen is paused, and does not reset when switching screens.
        /// Usually game logic should use CurrentScreenSecondsSince.
        /// </summary>
        /// <remarks>
        /// This value will only change once per frame, so it can be called multiple times per frame and the same
        /// value will be returned, assuming the same parameter is passed.
        /// </remarks>
        /// <param name="absoluteTime">The amount of time since the start of the game.</param>
        /// <returns>The number of seconds which have passed in absolute time since the start of the game.</returns>
        public static double SecondsSince(double absoluteTime)
        {
            return CurrentTime - absoluteTime;
        }

        /// <summary>
        /// Returns the number of seconds that have passed since the argument value. The
        /// return value will not increase when the screen is paused, so it can be used to 
        /// determine how much game time has passed for event which should occur on a timer.
        /// </summary>
        /// <param name="time">The time value, probably obtained earlier by calling CurrentScreenTime</param>
        /// <returns>The number of unpaused seconds that have passed since the argument time.</returns>
        public static double CurrentScreenSecondsSince(double time)
        {
            return Screens.ScreenManager.CurrentScreen.PauseAdjustedSecondsSince(time);
        }

        public static Task Delay(TimeSpan timeSpan)
        {
            return DelaySeconds(timeSpan.TotalSeconds);
        }

        public static Task DelaySeconds(double seconds)
        {
            if(seconds <= 0)
            {
                return Task.CompletedTask;
            }
            var time = CurrentScreenTime + seconds;
            var taskSource = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

            var index = screenTimeDelayedTasks.Count;
            for(int i = 0; i < screenTimeDelayedTasks.Count; i++)
            {
                if (screenTimeDelayedTasks[i].Time > time)
                {
                    index = i;
                    break;
                }
            }

            screenTimeDelayedTasks.Insert(index, new TimedTasks { Time = time, TaskCompletionSource = taskSource});

            return taskSource.Task;
        }

        public static Task DelayUntil(Func<bool> predicate)
        {
            if(predicate())
            {
                return Task.CompletedTask;
            }
            var taskSource = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            predicateTasks.Add(new PredicateTask { Predicate = predicate, TaskCompletionSource = taskSource });
            return taskSource.Task;
        }

        public static Task DelayFrames(int frameCount)
        {
            if(frameCount <= 0)
            {
                return Task.CompletedTask;
            }
            var taskSource = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            var index = frameTasks.Count;
            var absoluteFrame = TimeManager.CurrentFrame + frameCount;
            for (int i = 0; i < frameTasks.Count; i++)
            {
                if (frameTasks[i].FrameIndex > absoluteFrame)
                {
                    index = i;
                    break;
                }
            }
            frameTasks.Insert(index, new FrameTask { FrameIndex = absoluteFrame, TaskCompletionSource = taskSource });
            return taskSource.Task;

        }

        static bool isFirstUpdate = false;
        /// <summary>
        /// Performs every-frame logic to update timing values such as CurrentTime and SecondDifference.  If this method is not called, CurrentTime will not advance.
        /// </summary>
        /// <param name="time">The GameTime value provided by the MonoGame Game class.</param>
        public static void Update(GameTime time)
        {
            mLastUpdateGameTime = time;

            lastSections.Clear();
            lastSectionLabels.Clear();

            for (int i = sections.Count - 1; i > -1; i--)
            {

                lastSections.Insert(0, sections[i]);
                lastSectionLabels.Insert(0, sectionLabels[i]);
            }

            sections.Clear();
            sectionLabels.Clear();

            mLastSecondDifference = mSecondDifference;
            mLastCurrentTime = CurrentTime;

            const bool useSystemCurrentTime = false;

            double elapsedTime;

            if (useSystemCurrentTime)
            {
                double systemCurrentTime = CurrentSystemTime;
                elapsedTime = systemCurrentTime - mLastCurrentTime;
                mLastCurrentTime = systemCurrentTime;
                //stop big frame times
                if (elapsedTime > MaxFrameTime)
                {
                    elapsedTime = MaxFrameTime;
                }
            }
            else
            {
                /*
                mSecondDifference = (float)(currentSystemTime - mCurrentTime);
                mCurrentTime = currentSystemTime;
                */

                if (SetNextFrameTimeTo0)
                {
                    elapsedTime = 0;
                    SetNextFrameTimeTo0 = false;
                }
                else
                {
                    elapsedTime = time.ElapsedGameTime.TotalSeconds * mTimeFactor;
                }

                //stop big frame times
                if (elapsedTime > MaxFrameTime)
                {
                    elapsedTime = MaxFrameTime;
                }
            }

            mSecondDifference = (float)(elapsedTime);
            CurrentTime += elapsedTime;

            double currentSystemTime = CurrentSystemTime + mSecondDifference;

            mSecondDifferenceSquaredDividedByTwo = (mSecondDifference * mSecondDifference) / 2.0f;
            mCurrentTimeForTimedSections = currentSystemTime;

            if (isFirstUpdate)
            {
                isFirstUpdate = false;
            }
            else
            {
                CurrentFrame++;
            }
        }

        internal static void DoTaskLogic()
        {

            // Check if any delayed tasks should be completed
            while (screenTimeDelayedTasks.Count > 0)
            {
                var first = screenTimeDelayedTasks[0];
                if (first.Time <= CurrentScreenTime)
                {
                    screenTimeDelayedTasks.RemoveAt(0);
                    first.TaskCompletionSource.SetResult(null);
                }
                else
                {
                    // The earliest task is not ready to be completed, so we can stop checking
                    break;
                }
            }

            // Check if any predicate tasks should be completed
            // do a reverse loop, run the predicate, and remove them and set their result to null if the predicate is true
            for(int i = predicateTasks.Count - 1; i > -1; i--)
            {
                var predicateTask = predicateTasks[i];
                if(predicateTask.Predicate())
                {
                    predicateTasks.RemoveAt(i);
                    predicateTask.TaskCompletionSource.SetResult(null);
                }
            }

            while(frameTasks.Count > 0)
            {
                var first = frameTasks[0];
                if(first.FrameIndex <= CurrentFrame)
                {
                    frameTasks.RemoveAt(0);
                    first.TaskCompletionSource.SetResult(null);
                }
                else
                {
                    break;
                }
            }

        }

        internal static void ClearTasks()
        {
            foreach (var timedTasks in screenTimeDelayedTasks.ToList())
            {
                timedTasks.TaskCompletionSource.SetCanceled();
            }
            screenTimeDelayedTasks.Clear();

            foreach(var predicateTask in predicateTasks.ToList())
            {
                predicateTask.TaskCompletionSource.SetCanceled();
            }   
            predicateTasks.Clear();

            foreach(var frameTask in frameTasks.ToList())
            {
                frameTask.TaskCompletionSource.SetCanceled();
            }
            frameTasks.Clear();
        }

        #endregion
    }
}


