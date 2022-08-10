using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall;

using MathFunctions = FlatRedBall.Math.MathFunctions;

using FlatRedBall.Math.Geometry;
using FlatRedBall.Graphics;
using FlatRedBall.Instructions.Pause;
using FlatRedBall.Instructions.Interpolation;
using System.Reflection;

using FlatRedBall.Math;
using System.Collections.ObjectModel;
using Microsoft.Xna.Framework;
using System.Threading.Tasks;
using System.Threading;

namespace FlatRedBall.Instructions
{
    #region InterpolationType Enum
    public enum InterpolatorType
    {
        Linear,
        ClosestRotation
    }
    #endregion

    public static class InstructionManager
    {
        #region Fields

        static object syncLock = new object();

        static Queue<Instruction> instructionQueue = new Queue<Instruction>();

        static bool mIsExecutingInstructions = true;

        static InstructionList mInstructions = new InstructionList();
        //static ListBuffer<Instruction> mInstructionBuffer;

        static InstructionList mUnpauseInstructions = new InstructionList();

        static Dictionary<Type, IInterpolator> mInterpolators = new Dictionary<Type, IInterpolator>();

        static Dictionary<Type, IInterpolator> mRotationInterpolators = new Dictionary<Type, IInterpolator>();

        static List<VelocityValueRelationship> mVelocityValueRelationships = new List<VelocityValueRelationship>();
        static List<AnimationValueRelationship> mAnimationValueRelationships = new List<AnimationValueRelationship>();
        static List<AbsoluteRelativeValueRelationship> mAbsoluteRelativeValueRelationships = new List<AbsoluteRelativeValueRelationship>();


        static List<string> mRotationMembers = new List<string>();

        #endregion

        #region Properties

        /// <summary>
        /// Holds instructions which will be executed by the InstructionManager
        /// in its Update method (called automatically by FlatRedBallServices).
        /// This list is sorted by time.
        /// </summary>
        /// <remarks>
        /// Instructions for managed PositionedObjects like Sprites and Text objects
        /// should be added to the object's internal InstructionList.  This prevents instructions
        /// from referencing removed objects and helps with debugging.  This list should only be used
        /// on un-managed objects or for instructions which do not associate with a particular object.
        /// </remarks>        
        public static InstructionList Instructions
        {
            get { return mInstructions; }
        }

        public static bool IsEnginePaused
        {
            get { return mUnpauseInstructions.Count != 0; }
        }

        #region XML Docs
        /// <summary>
        /// Whether the (automatically called) Update method executes instructions.  Default true.
        /// </summary>
        #endregion
        public static bool IsExecutingInstructions
        {
            get { return mIsExecutingInstructions; }
            set { mIsExecutingInstructions = value; }
        }

        public static int UnpauseInstructionCount => mUnpauseInstructions.Count;

        public static ReadOnlyCollection<VelocityValueRelationship> VelocityValueRelationships
        {
            get;
            private set;
        }
        public static ReadOnlyCollection<AbsoluteRelativeValueRelationship> AbsoluteRelativeValueRelationships
        {
            get;
            private set;
        }

        #endregion

        #region Methods

        #region Constructor/Initialize

        // This would normally be private, but we're making
        // it public because Glue doesn't do the regular engine
        // setup, but it still needs the information that is created
        // in this Class' Initialize calls.
        public static void Initialize()
        {
            //mInstructionBuffer = new ListBuffer<Instruction>(mInstructions);

            CreateInterpolators();

            CreateValueRelationships();

            CreateRotationMembers();
        }
        #endregion

        #region Public Methods

        public static void AddSafe(Instruction instruction)
        {
            lock (syncLock)
            {
                instructionQueue.Enqueue(instruction);
            }
        }

        /// <summary>
        /// Creates a new DelegateInstruction using the argument Action, and adds it to be executed
        /// on the next frame on the primary thread.
        /// </summary>
        /// <param name="action">The action to execute.</param>
        public static void AddSafe(Action action)
        {
            lock (syncLock)
            {
                instructionQueue.Enqueue(new DelegateInstruction(action));
            }
        }

        /// <summary>
        /// Performs the argument action on the main thread and awaits its execution. 
        /// </summary>
        /// <param name="action">The action to execute</param>
        /// <returns>A task which can be awaited.</returns>
        public static async Task DoOnMainThreadAsync(Action action)
        {
            var semaphor = new SemaphoreSlim(1);
            semaphor.Wait();

            AddSafe(() =>
            {
                action();
                semaphor.Release();
            });

            await semaphor.WaitAsync();

        }

        /// <summary>
        /// Adds the argument instruction to the InstructionManager, to be executed when its time is reached.
        /// </summary>
        /// <param name="instruction">The instruction to remove</param>
        public static void Add(Instruction instruction)
        {
            mInstructions.Add(instruction);
        }

        /// <summary>
        /// Attempts to execute instructions held by the argument instructable according to the TimeManager.CurrentTime.
        /// Executed instructions will either be removed or cycled if the CycleTime is greater than 0.
        /// </summary>
        /// <param name="instructable">The instructable to execute instructions on.</param>
        public static void ExecuteInstructionsOnConsideringTime(IInstructable instructable)
        {
            ExecuteInstructionsOnConsideringTime(instructable, TimeManager.CurrentTime);
        }


        /// <summary>
        /// Attempts to execute instructions held by the argument instruct
        /// able according to the currentTime value.
        /// Executed instructions will either be removed or cycled if the CycleTime is greater than 0.
        /// </summary>
        /// <param name="instructable">The instructable to execute instructions on.</param>
        /// <param name="currentTime">The time to compare to instructions in the instructable instance.</param>
        public static void ExecuteInstructionsOnConsideringTime(IInstructable instructable, double currentTime)
        {
            Instructions.Instruction instruction;

#if DEBUG
            if(instructable.Instructions == null)
            {
                throw new InvalidOperationException(
                    $"The instructable of type {instructable.GetType()} has null instructions. These should be instantiated before attempting to execute instructions");
            }
#endif

            while (instructable.Instructions.Count > 0 &&
                instructable.Instructions[0].TimeToExecute <= currentTime)
            {
                instruction = instructable.Instructions[0];

                instruction.Execute();

                // The instruction may have cleared the InstructionList, so we need to test if it did.
                if (instructable.Instructions.Count < 1)
                    continue;

                if (instruction.CycleTime == 0)
                    instructable.Instructions.Remove(instruction);
                else
                {
                    instruction.TimeToExecute += instruction.CycleTime;
                    instructable.Instructions.InsertionSortAscendingTimeToExecute();
                }
            }
        }

        public static Type GetTypeForMember(Type type, string member)
        {
            PropertyInfo propertyInfo = type.GetProperty(member);

            if (propertyInfo != null)
            {
                return propertyInfo.PropertyType;
            }

            FieldInfo fieldInfo = type.GetField(member);

            if (fieldInfo != null)
            {
                return fieldInfo.FieldType;
            }

            return null;

        }

        public static AnimationValueRelationship GetAnimationValueRelationship(string frameMemberName)
        {
            for (int i = 0; i < mAnimationValueRelationships.Count; i++)
            {
                if (mAnimationValueRelationships[i].Frame == frameMemberName)
                {
                    return mAnimationValueRelationships[i];
                }
            }
            return null;
        }

        public static string GetStateForVelocity(string velocity)
        {
            for (int i = 0; i < mVelocityValueRelationships.Count; i++)
            {
                if (mVelocityValueRelationships[i].Velocity == velocity)
                    return mVelocityValueRelationships[i].State;
            }

            return null;
        }

        public static string GetRelativeForAbsolute(string absolute)
        {
            for (int i = 0; i < mAbsoluteRelativeValueRelationships.Count; i++)
            {
                if (mAbsoluteRelativeValueRelationships[i].AbsoluteValue == absolute)
                {
                    return mAbsoluteRelativeValueRelationships[i].RelativeValue;
                }
            }

            return null;
        }

        public static string GetVelocityForState(string state)
        {
            for (int i = 0; i < mVelocityValueRelationships.Count; i++)
            {
                if (mVelocityValueRelationships[i].State == state)
                    return mVelocityValueRelationships[i].Velocity;
            }

            return null;
        }

        public static bool IsObjectReferencedByInstructions(object objectToReference)
        {
            for (int i = 0; i < mInstructions.Count; i++)
            {
                if (mInstructions[i].Target == objectToReference)
                {
                    return true;
                }
            }
            return false;
        }


        public static bool IsRotationMember(string rotationMember)
        {
            return mRotationMembers.Contains(rotationMember);
        }

        #region Move 

        public static void MoveThrough<T>(FlatRedBall.PositionedObject positionedObject, IList<T> list, float velocity) where T : IPositionable
        {
            double time = TimeManager.CurrentTime;

            float lastX = positionedObject.X;
            float lastY = positionedObject.Y;
            float lastZ = positionedObject.Z;

            float distanceX = 0;
            float distanceY = 0;
            float distanceZ = 0;

            double totalDistance = 0;

            Vector3 newVelocity = new Vector3();

            foreach (IPositionable positionable in list)
            {
                distanceX = positionable.X - lastX;
                distanceY = positionable.Y - lastY;
                distanceZ = positionable.Z - lastZ;

                totalDistance = (float)System.Math.Sqrt(
                    distanceX * distanceX + distanceY * distanceY + distanceZ * distanceZ);

                newVelocity.X = distanceX;
                newVelocity.Y = distanceY;
                newVelocity.Z = distanceZ;

                newVelocity.Normalize();
                newVelocity *= velocity;

                positionedObject.Instructions.Add(new Instruction<FlatRedBall.PositionedObject, Vector3>(
                    positionedObject, "Velocity", newVelocity, time));


                lastX = positionable.X;
                lastY = positionable.Y;
                lastZ = positionable.Z;

                time += totalDistance / velocity;

            }

            positionedObject.Instructions.Add(new Instruction<FlatRedBall.PositionedObject, Vector3>(
                positionedObject, "Velocity", new Vector3(), time));



        }


        public static void MoveToAccurate(FlatRedBall.PositionedObject positionedObject, float x, float y, float z, double secondsToTake)
        {
            if (secondsToTake != 0.0f)
            {
                positionedObject.XVelocity = (x - positionedObject.X) / (float)secondsToTake;
                positionedObject.YVelocity = (y - positionedObject.Y) / (float)secondsToTake;
                positionedObject.ZVelocity = (z - positionedObject.Z) / (float)secondsToTake;

                double timeToExecute = TimeManager.CurrentTime + secondsToTake;

                positionedObject.Instructions.Add(
                    new Instruction<FlatRedBall.PositionedObject, float>(positionedObject, "XVelocity", 0, timeToExecute));
                positionedObject.Instructions.Add(
                    new Instruction<FlatRedBall.PositionedObject, float>(positionedObject, "YVelocity", 0, timeToExecute));
                positionedObject.Instructions.Add(
                    new Instruction<FlatRedBall.PositionedObject, float>(positionedObject, "ZVelocity", 0, timeToExecute));

                positionedObject.Instructions.Add(
                    new Instruction<FlatRedBall.PositionedObject, float>(positionedObject, "X", x, timeToExecute));
                positionedObject.Instructions.Add(
                    new Instruction<FlatRedBall.PositionedObject, float>(positionedObject, "Y", y, timeToExecute));
                positionedObject.Instructions.Add(
                    new Instruction<FlatRedBall.PositionedObject, float>(positionedObject, "Z", z, timeToExecute));
            }
            else
            {
                positionedObject.X = x;
                positionedObject.Y = y;
                positionedObject.Z = z;
            }
        }

        public static void MoveTo(FlatRedBall.PositionedObject positionedObject, float x, float y, float z, double secondsToTake)
        {
            if (secondsToTake != 0.0f)
            {
                positionedObject.XVelocity = (x - positionedObject.X) / (float)secondsToTake;
                positionedObject.YVelocity = (y - positionedObject.Y) / (float)secondsToTake;
                positionedObject.ZVelocity = (z - positionedObject.Z) / (float)secondsToTake;

                double timeToExecute = TimeManager.CurrentTime + secondsToTake;

                positionedObject.Instructions.Add(
                    new Instruction<FlatRedBall.PositionedObject, float>(positionedObject, "XVelocity", 0, timeToExecute));
                positionedObject.Instructions.Add(
                    new Instruction<FlatRedBall.PositionedObject, float>(positionedObject, "YVelocity", 0, timeToExecute));
                positionedObject.Instructions.Add(
                    new Instruction<FlatRedBall.PositionedObject, float>(positionedObject, "ZVelocity", 0, timeToExecute));
            }
            else
            {
                positionedObject.X = x;
                positionedObject.Y = y;
                positionedObject.Z = z;
            }
        }

        #endregion

        #region Pause Methods

        internal static PositionedObjectList<FlatRedBall.PositionedObject> PositionedObjectsIgnoringPausing = 
            new PositionedObjectList<FlatRedBall.PositionedObject>();

        public static List<object> ObjectsIgnoringPausing { get; private set; } = new List<object>();

        public static void IgnorePausingFor(FlatRedBall.PositionedObject positionedObject)
        {
            // This function needs to tolerate
            // the same object being added multiple
            // times.  The reason is that an Entity in
            // Glue may set one of its objects to be ignored
            // in pausing, but then the object itself may also
            // be set to be ignored.
            if (!PositionedObjectsIgnoringPausing.Contains(positionedObject))
            {
                PositionedObjectsIgnoringPausing.Add(positionedObject);
            }
        }

        public static void IgnorePausingFor<T>(IList<T> list) where T : FlatRedBall.PositionedObject
        {
            for (int i = 0; i < list.Count; i++)
            {
                IgnorePausingFor(list[i]);
            }
        }


        public static void IgnorePausingFor(Scene scene)
        {

            IgnorePausingFor(scene.SpriteFrames);
            IgnorePausingFor(scene.Sprites);
            IgnorePausingFor(scene.Texts);
        }

        public static void IgnorePausingFor(ShapeCollection shapeCollection)
        {
            IgnorePausingFor(shapeCollection.AxisAlignedCubes);
            IgnorePausingFor(shapeCollection.AxisAlignedRectangles);
            IgnorePausingFor(shapeCollection.Capsule2Ds);
            IgnorePausingFor(shapeCollection.Circles);
            IgnorePausingFor(shapeCollection.Lines);
            IgnorePausingFor(shapeCollection.Polygons);
            IgnorePausingFor(shapeCollection.Spheres);
        }



        static void IgnorePausingFor<T>(PositionedObjectList<T> list) where T : FlatRedBall.PositionedObject
        {
            int count = list.Count;
            for (int i = 0; i < count; i++)
            {
                IgnorePausingFor(list[i]);
            }
        }

        public static void PauseEngine()
        {
            PauseEngine(true);
        }

        public static void PauseEngine(bool storeUnpauseInstructions)
        {
            if (mUnpauseInstructions.Count != 0)
            {
                throw new System.InvalidOperationException(
                    "Can't execute pause since there are already instructions in the Unpause InstructionList." +
                    " Are you causing PauseEngine twice in a row?" +
                    " Each PauseEngine method must be followed by an UnpauseEngine before PauseEngine can be called again.");

            }
            // When the engine pauses, each manager stops 
            // all activity and fills the unpauseInstructions
            // with instructions that are executed to restart activity.

            // Turn off sorting so we don't sort over and over and over...
            // Looks like we never need to turn this back on.  
            mUnpauseInstructions.SortOnAdd = false;

            SpriteManager.Pause(mUnpauseInstructions);

            ShapeManager.Pause(mUnpauseInstructions);

            TextManager.Pause(mUnpauseInstructions);

            InstructionListUnpause unpauseInstruction = new InstructionListUnpause(mInstructions);
            mInstructions.Clear();

            // the minority of instructions should be targeting objects that can't be paused, so loop through
            // and pull them back out
            for(int i = unpauseInstruction.TemporaryInstructions.Count - 1; i > -1; i--)
            {
                var instruction = unpauseInstruction.TemporaryInstructions[i];

                if(ObjectsIgnoringPausing.Contains( instruction.Target))
                {
                    unpauseInstruction.TemporaryInstructions.RemoveAt(i);
                    // add it back!
                    mInstructions.Add(instruction);
                }
            }


            mUnpauseInstructions.Add(unpauseInstruction);

            if (!storeUnpauseInstructions)
            {
                mUnpauseInstructions.Clear();
            }

            // ... now do one sort at the end to make sure all is sorted properly
            // Actually, no, don't sort, it's not necessary!  We're going to execute
            // everything anyway.
            //mUnpauseInstructions.Sort((a, b) => a.TimeToExecute.CompareTo(b));
        }

        public static void FindAndExecuteUnpauseInstruction( object target )
        {
            for( int iCurInstruction = mUnpauseInstructions.Count - 1; iCurInstruction > -1; iCurInstruction-- )
            {                
                if( mUnpauseInstructions[ iCurInstruction ].Target == target )
                {
                    mUnpauseInstructions[ iCurInstruction ].Execute();
                    mUnpauseInstructions.RemoveAt( iCurInstruction );
                    break;
                }
            }
        }

        public static void UnpauseEngine()
        {
            foreach (Instruction instruction in mUnpauseInstructions)
            {
                instruction.Execute();
            }

            mUnpauseInstructions.Clear();

        }

        #endregion

        /// <summary>
        /// Removes the argument instruction from the internal list. A removed instruction will not
        /// automatically be executed.
        /// </summary>
        /// <param name="instruction">The instruction to remove.</param>
        public static void Remove(Instruction instruction)
        {
            mInstructions.Remove(instruction);
            
        }


        #region XML Docs
        /// <summary>
        /// Sets a member on an uncasted object.  If the type of objectToSetOn is known, use
        /// LateBinder for performance and safety reasons.
        /// </summary>
        /// <param name="objectToSetOn">The object whose field or property should be set.</param>
        /// <param name="memberName">The name of the field or property to set.</param>
        /// <param name="valueToSet">The value of the field or property to set.</param>
        #endregion
        public static void UncastedSetMember(object objectToSetOn, string memberName, object valueToSet)
        {
            if (objectToSetOn == null)
            {
                throw new ArgumentNullException("Argument " + objectToSetOn + " cannot be null");
            }

            Type typeOfObject = objectToSetOn.GetType();

            IEnumerable<PropertyInfo> properties = typeOfObject.GetProperties();

            foreach (PropertyInfo propertyInfo in properties)
            {
                if (propertyInfo.Name == memberName)
                {
                    propertyInfo.SetValue(objectToSetOn, valueToSet, null);
                    return; // end here since it's been set - there's no reason to keep going on to fields
                }
            }

            var fields = typeOfObject.GetFields();

            foreach (FieldInfo field in fields)
            {
                if (field.Name == memberName)
                {
                    field.SetValue(objectToSetOn, valueToSet);
                }
            }

        }

        #endregion

        #region Internal

        static internal IInterpolator GetInterpolator(Type type)
        {
            return GetInterpolator(type, InterpolatorType.Linear);
        }

        static internal IInterpolator GetInterpolator(Type type, string memberName)
        {
            InterpolatorType interpolatorType = InterpolatorType.Linear;

            if (mRotationMembers.Contains(memberName))
                interpolatorType = InterpolatorType.ClosestRotation;

            return GetInterpolator(type, interpolatorType);
        }

        static internal IInterpolator GetInterpolator(Type type, InterpolatorType interpolatorType)
        {
            Dictionary<Type, IInterpolator> interpolatorDictionary = null;

            switch (interpolatorType)
            {
                case InterpolatorType.Linear:
                    interpolatorDictionary = mInterpolators;
                    break;
                case InterpolatorType.ClosestRotation:
                    interpolatorDictionary = mRotationInterpolators;
                    break;
            }


            if (interpolatorDictionary.ContainsKey(type))
            {
                return interpolatorDictionary[type];
            }
            else
            {
                throw new InvalidOperationException("There is no interpolator registered for type " + type.ToString());
            }
        }

        static internal bool HasInterpolatorForType(Type type)
        {
            return mInterpolators.ContainsKey(type);
        }

        /// <summary>
        /// Performs every-frame updates which include moving queued instructions to the main instruction list and
        /// executing instructions according to their TimeToExecute.
        /// </summary>
        internal static void Update()
        {
            //Flush();

            var currentTime = TimeManager.CurrentTime;

            Instructions.Instruction instruction;

            lock (syncLock)
            {
                while (instructionQueue.Count > 0)
                {
                    Add(instructionQueue.Dequeue());
                }
            }

            while (mIsExecutingInstructions &&
                mInstructions.Count > 0 &&
                mInstructions[0].TimeToExecute <= currentTime)
            {
                instruction = mInstructions[0];

                // Nov 2, 2019
                // An instruction 
                // may pause the game, 
                // which would then take 
                // this instruction and put
                // it on the to-execute list 
                // of instructions. But since 
                // it's already executing we don't
                // want that to happen, so going to 
                // remove it before executing. This is
                // different from how the engine used to
                // work prior to this date, so hopefully this
                // doesn't introduce any weird behaviors

                if (instruction.CycleTime == 0)
                    mInstructions.Remove(instruction);


                instruction.Execute();

                // The instruction may have cleared the InstructionList, so we need to test if it did.
                if (mInstructions.Count < 1)
                    continue;

                if (instruction.CycleTime != 0)
                {
                    instruction.TimeToExecute += instruction.CycleTime;
                    mInstructions.InsertionSortAscendingTimeToExecute();
                }
            }

            // The ScreenManager doesn't have any engine-initiated
            // activity, it's all initiated by custom code.  However,
            // instructions are supposed to execute before any custom code
            // runs.  Therefore, we're going to have these be handled here:
            if (Screens.ScreenManager.CurrentScreen != null)
            {
                ExecuteInstructionsOnConsideringTime(Screens.ScreenManager.CurrentScreen);
            }

        }

        #endregion
        
        #region Private Methods

        private static void CreateInterpolators()
        {
            mInterpolators.Add(typeof(float), new FloatInterpolator());
            mInterpolators.Add(typeof(double), new DoubleInterpolator());
            mInterpolators.Add(typeof(long), new LongInterpolator());
            mInterpolators.Add(typeof(int), new IntInterpolator());

            mRotationInterpolators.Add(typeof(float), new FloatAngleInterpolator());
        }

        private static void CreateRotationMembers()
        {
            mRotationMembers.Add("RotationX");
            mRotationMembers.Add("RotationY");
            mRotationMembers.Add("RotationZ");

            mRotationMembers.Add("RelativeRotationX");
            mRotationMembers.Add("RelativeRotationY");
            mRotationMembers.Add("RelativeRotationZ");

        }

        private static void CreateValueRelationships()
        {
            #region Create the Value Relationships

            mVelocityValueRelationships.Add(new VelocityValueRelationship("X", "XVelocity", "XAcceleration"));
            mVelocityValueRelationships.Add(new VelocityValueRelationship("Y", "YVelocity", "YAcceleration"));
            mVelocityValueRelationships.Add(new VelocityValueRelationship("Z", "ZVelocity", "ZAcceleration"));

            mVelocityValueRelationships.Add(new VelocityValueRelationship("RelativeX", "RelativeXVelocity", "RelativeXAcceleration"));
            mVelocityValueRelationships.Add(new VelocityValueRelationship("RelativeY", "RelativeYVelocity", "RelativeYAcceleration"));
            mVelocityValueRelationships.Add(new VelocityValueRelationship("RelativeZ", "RelativeZVelocity", "RelativeZAcceleration"));

            mVelocityValueRelationships.Add(new VelocityValueRelationship("ScaleX", "ScaleXVelocity", null));
            mVelocityValueRelationships.Add(new VelocityValueRelationship("ScaleY", "ScaleYVelocity", null));
            mVelocityValueRelationships.Add(new VelocityValueRelationship("ScaleZ", "ScaleZVelocity", null));

            mVelocityValueRelationships.Add(new VelocityValueRelationship("Radius", "RadiusVelocity", null));

            mVelocityValueRelationships.Add(new VelocityValueRelationship("Alpha", "AlphaRate", null));
            mVelocityValueRelationships.Add(new VelocityValueRelationship("Red", "RedRate", null));
            mVelocityValueRelationships.Add(new VelocityValueRelationship("Green", "GreenRate", null));
            mVelocityValueRelationships.Add(new VelocityValueRelationship("Blue", "BlueRate", null));

            mVelocityValueRelationships.Add(new VelocityValueRelationship("RotationX", "RotationXVelocity", null));
            mVelocityValueRelationships.Add(new VelocityValueRelationship("RotationY", "RotationYVelocity", null));
            mVelocityValueRelationships.Add(new VelocityValueRelationship("RotationZ", "RotationZVelocity", null));

            mVelocityValueRelationships.Add(new VelocityValueRelationship("RelativeRotationX", "RelativeRotationXVelocity", null));
            mVelocityValueRelationships.Add(new VelocityValueRelationship("RelativeRotationY", "RelativeRotationYVelocity", null));
            mVelocityValueRelationships.Add(new VelocityValueRelationship("RelativeRotationZ", "RelativeRotationZVelocity", null));

            
            mVelocityValueRelationships.Add(new VelocityValueRelationship("LeftDestination", "LeftDestinationVelocity", null));
            mVelocityValueRelationships.Add(new VelocityValueRelationship("RightDestination", "RightDestinationVelocity", null));
            mVelocityValueRelationships.Add(new VelocityValueRelationship("TopDestination", "TopDestinationVelocity", null));
            mVelocityValueRelationships.Add(new VelocityValueRelationship("BottomDestination", "BottomDestinationVelocity", null));

            mVelocityValueRelationships.Add(new VelocityValueRelationship("Scale", "ScaleVelocity", null));
            mVelocityValueRelationships.Add(new VelocityValueRelationship("Spacing", "SpacingVelocity", null));

            #endregion

            #region Create the Animation Relationships

            mAnimationValueRelationships.Add(new AnimationValueRelationship("CurrentFrameIndex", "AnimationSpeed", "CurrentChain", "Count"));

            #endregion

            #region Create the Absolute/Relative Relationships

            mAbsoluteRelativeValueRelationships.Add(new AbsoluteRelativeValueRelationship("X", "RelativeX"));
            mAbsoluteRelativeValueRelationships.Add(new AbsoluteRelativeValueRelationship("Y", "RelativeY"));
            mAbsoluteRelativeValueRelationships.Add(new AbsoluteRelativeValueRelationship("Z", "RelativeZ"));

            mAbsoluteRelativeValueRelationships.Add(new AbsoluteRelativeValueRelationship("XVelocity", "RelativeXVelocity"));
            mAbsoluteRelativeValueRelationships.Add(new AbsoluteRelativeValueRelationship("YVelocity", "RelativeYVelocity"));
            mAbsoluteRelativeValueRelationships.Add(new AbsoluteRelativeValueRelationship("ZVelocity", "RelativeZVelocity"));

            mAbsoluteRelativeValueRelationships.Add(new AbsoluteRelativeValueRelationship("RotationX", "RelativeRotationX"));
            mAbsoluteRelativeValueRelationships.Add(new AbsoluteRelativeValueRelationship("RotationY", "RelativeRotationY"));
            mAbsoluteRelativeValueRelationships.Add(new AbsoluteRelativeValueRelationship("RotationZ", "RelativeRotationZ"));

            mAbsoluteRelativeValueRelationships.Add(new AbsoluteRelativeValueRelationship("RotationXVelocity", "RelativeRotationXVelocity"));
            mAbsoluteRelativeValueRelationships.Add(new AbsoluteRelativeValueRelationship("RotationYVelocity", "RelativeRotationYVelocity"));
            mAbsoluteRelativeValueRelationships.Add(new AbsoluteRelativeValueRelationship("RotationZVelocity", "RelativeRotationZVelocity"));

            mAbsoluteRelativeValueRelationships.Add(new AbsoluteRelativeValueRelationship("Top", "RelativeTop"));
            mAbsoluteRelativeValueRelationships.Add(new AbsoluteRelativeValueRelationship("Bottom", "RelativeBottom"));
            mAbsoluteRelativeValueRelationships.Add(new AbsoluteRelativeValueRelationship("Left", "RelativeLeft"));
            mAbsoluteRelativeValueRelationships.Add(new AbsoluteRelativeValueRelationship("Right", "RelativeRight"));

            #endregion

#if !SILVERLIGHT
            VelocityValueRelationships = new ReadOnlyCollection<VelocityValueRelationship>(mVelocityValueRelationships);
            AbsoluteRelativeValueRelationships = new ReadOnlyCollection<AbsoluteRelativeValueRelationship>(mAbsoluteRelativeValueRelationships);
#endif
        }

        //private static void Flush()
        //{
        //    mInstructionBuffer.Flush();
        //}

        #endregion

        #endregion
    }
}
