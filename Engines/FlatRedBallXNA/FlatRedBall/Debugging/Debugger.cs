using System;
 
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Audio;
using FlatRedBall.Graphics;
using FlatRedBall.Math;
using FlatRedBall.Math.Collision;
using FlatRedBall.Math.Geometry;
using Microsoft.Xna.Framework.Media;

namespace FlatRedBall.Debugging
{
    #region CountedCategory class
    class CountedCategory
    {
        public string String;
        public int Count;
        public int Invisible;

        public override string ToString()
        {
            return String;
        }
    }
    #endregion

    public static class Debugger
    {
        #region Enums

        public enum Corner
        {
            TopLeft,
            TopRight,
            BottomLeft,
            BottomRight
        }

        #endregion

        #region Fields

        static Text mText;
        static Layer mLayer;
        static string MemoryInformation;
        static double LastCalculationTime;
        public static Corner TextCorner = Corner.TopLeft;
        public static float TextRed = 1;
        public static float TextGreen = 1;
        public static float TextBlue = 1;

        public static int NumberOfLinesInCommandLine = 5;

        static List<string> mCommandLineOutput = new List<string>();

#if DEBUG
        static RollingAverage mAllocationAverage = new RollingAverage(4);
        static long mLastMemoryUse = -1;
#endif

        public static StringBuilder LogStringBuilder = new StringBuilder();

        static double mMemoryUpdateFrequency = .25;
        #endregion

        #region Properties

        public static double MemoryUpdateFrequency
        {
            get { return mMemoryUpdateFrequency; }
            set { mMemoryUpdateFrequency = value; }
        }

        public static AutomaticDebuggingBehavior AutomaticDebuggingBehavior
        {
            get;
            set;
        }

        #endregion


        #region Constructor
        static Debugger()
        {
            AutomaticDebuggingBehavior = new AutomaticDebuggingBehavior();
        }
        #endregion

        #region Methods

        static void CreateTextIfNecessary()
        {
            if (mText == null)
            {
                mText = TextManager.AddText("");
                mText.Name = "Debugger rendering text";
                mLayer = SpriteManager.TopLayer;
                TextManager.AddToLayer(mText, mLayer);
                mText.AttachTo(SpriteManager.Camera, false);
                mText.VerticalAlignment = VerticalAlignment.Top;

                mText.AdjustPositionForPixelPerfectDrawing = true;
            }
        }

        public static void DestroyText()
        {
            if (mText != null)
            {
                SpriteManager.RemoveLayer(mLayer);
                TextManager.RemoveText(mText);

                mLayer = null;
                mText = null;

            }
        }

        public static void ClearCommandLine()
        {
            mCommandLineOutput.Clear();
        }

        public static void CommandLineWrite(string stringToWrite)
        {
            mCommandLineOutput.Add(stringToWrite);

            if (mCommandLineOutput.Count > NumberOfLinesInCommandLine)
            {
                mCommandLineOutput.RemoveAt(0);
            }

        }

        public static void CommandLineWrite(object objectToWrite)
        {
            if (objectToWrite != null)
            {
                CommandLineWrite(objectToWrite.ToString());
            }
            else
            {
                CommandLineWrite("<null>");
            }
        }

        public static void Write(string stringToWrite)
        {
            CreateTextIfNecessary();

            mText.DisplayText = stringToWrite;
			
			//position the text each frame in case of camera changes

            AdjustTextPosition();

            mText.Red = TextRed;
            mText.Green = TextGreen;
            mText.Blue = TextBlue;
            
			mText.SetPixelPerfectScale(mLayer);
        }

        public static void Write(object objectToWrite)
        {
            if (objectToWrite == null)
            {
                Write("<NULL>");
            }
            else
            {
                Write(objectToWrite.ToString());
            }
        }


        public static void WriteMemoryInformation()
        {
            if (TimeManager.SecondsSince(LastCalculationTime) > mMemoryUpdateFrequency)
            {
                MemoryInformation = ForceGetMemoryInformation();
            }
            Write(MemoryInformation);
        }

        public static void WriteSongInformation(Song song)
        {
            var format = song.Duration.TotalMinutes > 60
                ? @"hh\:mm\:ss"
                : @"mm\:ss";

#if MONOGAME && !UWP && !__IOS__
            var currentTime = song.Position.ToString(format);
#else
            var currentTime = song == AudioManager.CurrentSong ?  Microsoft.Xna.Framework.Media.MediaPlayer.PlayPosition.ToString(format) : new TimeSpan(0).ToString(format);
#endif
            var totalDuration = song.Duration.ToString(format);
            Write($"{song.Name} {currentTime} / {totalDuration}");
        }

        public static string ForceGetMemoryInformation()
        {

            string memoryInformation;

            long currentUsage;

                currentUsage = GC.GetTotalMemory(false);
                memoryInformation = "Total Memory: " + currentUsage.ToString("N0");

#if DEBUG
            if (mLastMemoryUse >= 0)
            {
                long difference = currentUsage - mLastMemoryUse;

                if (difference >= 0)
                {
                    mAllocationAverage.AddValue((float)(difference / mMemoryUpdateFrequency));
                }
            }
            memoryInformation += "\nAverage Growth per second: " +
                mAllocationAverage.Average.ToString("N0");
#endif

            LastCalculationTime = TimeManager.CurrentTime;
#if DEBUG
            mLastMemoryUse = currentUsage;
#endif


            return memoryInformation;
        }       

        public static void WriteAutomaticallyUpdatedObjectInformation()
        {
            string result = GetAutomaticallyUpdatedObjectInformation();

            Write(result);
        }

        public static string GetAutomaticallyUpdatedObjectInformation()
        {
            const string indentString = " * ";

            StringBuilder stringBuilder = new StringBuilder();
            int total = 0;
            // SpriteManager
            stringBuilder.AppendLine(SpriteManager.ManagedPositionedObjects.Count + " PositionedObjects");

            var entityCount = SpriteManager.ManagedPositionedObjects.Where(item => item.GetType().FullName.Contains(".Entities")).Count();
            stringBuilder.AppendLine($"{indentString} {entityCount} Entities");

            total += SpriteManager.ManagedPositionedObjects.Count;

            var totalSpriteCount = SpriteManager.AutomaticallyUpdatedSprites.Count;
            stringBuilder.AppendLine(totalSpriteCount + " Sprites");

            var spriteNonParticleEntityCount = SpriteManager.AutomaticallyUpdatedSprites.Where(item => item.GetType().FullName.Contains(".Entities")).Count();
            stringBuilder.AppendLine(indentString + spriteNonParticleEntityCount + " Entity Sprites");
            stringBuilder.AppendLine(indentString + SpriteManager.ParticleCount + " Particles");
            var normalSpriteCount = SpriteManager.AutomaticallyUpdatedSprites.Count - spriteNonParticleEntityCount - SpriteManager.ParticleCount;
            stringBuilder.AppendLine(indentString + (normalSpriteCount) + " Normal Sprites");
            total += SpriteManager.AutomaticallyUpdatedSprites.Count;

            stringBuilder.AppendLine(SpriteManager.SpriteFrames.Count + " SpriteFrames");
            total += SpriteManager.SpriteFrames.Count;

            stringBuilder.AppendLine(SpriteManager.Cameras.Count + " Cameras");
            total += SpriteManager.Cameras.Count;

            stringBuilder.AppendLine(SpriteManager.Emitters.Count + " Emitters");
            total += SpriteManager.Emitters.Count;


            // ShapeManager
            stringBuilder.AppendLine(ShapeManager.AutomaticallyUpdatedShapes.Count + " Shapes");
            total += ShapeManager.AutomaticallyUpdatedShapes.Count;
            // TextManager
            stringBuilder.AppendLine(TextManager.AutomaticallyUpdatedTexts.Count + " Texts");
            total += TextManager.AutomaticallyUpdatedTexts.Count;

            stringBuilder.AppendLine("---------------");
            stringBuilder.AppendLine(total + " Total");

            string result = stringBuilder.ToString();
            return result;
        }

        public static string GetAutomaticallyUpdatedEntityInformation()
        {
            Dictionary<Type, int> countDictionary = new Dictionary<Type, int>();

            var entities = SpriteManager.ManagedPositionedObjects.Where(item => item.GetType().FullName.Contains(".Entities"));

            foreach(var entity in entities)
            {
                var type = entity.GetType();
                if (countDictionary.ContainsKey(type))
                {
                    countDictionary[type]++;
                }
                else
                {
                    countDictionary[type] = 1;
                }
            }

            StringBuilder builder = new StringBuilder();
            foreach(var kvp in countDictionary.OrderByDescending(item =>item.Value))
            {
                builder.AppendLine($"{kvp.Value} {kvp.Key.Name}");
            }

            return builder.ToString();
        }

        public static void WriteAutomaticallyUpdatedSpriteFrameBreakdown()
        {
            string result = 
                GetAutomaticallyUpdatedBreakdownFromList<FlatRedBall.ManagedSpriteGroups.SpriteFrame>(SpriteManager.SpriteFrames);
            Write(result);
        }

        public static void WriteAutomaticallyUpdatedShapeBreakdown()
        {
            var result =
                GetShapeBreakdown();

            Write(result);
        }

        public static void WriteAutomaticallyUpdatedSpriteBreakdown()
        {
            string result = GetAutomaticallyUpdatedSpriteBreakdown();
            Write(result);
        }

        public static string GetAutomaticallyUpdatedSpriteBreakdown()
        {
            return GetAutomaticallyUpdatedBreakdownFromList<Sprite>(SpriteManager.AutomaticallyUpdatedSprites);
        }

        public static string GetAutomaticallyUpdatedBreakdownFromList<T>(IEnumerable<T> list) where T : PositionedObject
        {
            Dictionary<string, CountedCategory> typeDictionary = new Dictionary<string, CountedCategory>();

            bool isIVisible = false;

            if (list.Count() != 0)
            {
                isIVisible = list.First() is IVisible;
            }

            foreach(var atI in list)
            {
                string typeName = "Unparented";

                if (typeof(T) != atI.GetType())
                {
                    typeName = atI.GetType().Name;
                }
                else if (atI.Parent != null)
                {
                    typeName = atI.Parent.GetType().Name;
                }

                if (typeDictionary.ContainsKey(typeName) == false)
                {
                    var toAdd = new CountedCategory();
                    toAdd.String = typeName;

                    typeDictionary.Add(typeName, toAdd);

                }

                typeDictionary[typeName].Count++;

                if (isIVisible && !((IVisible)atI).AbsoluteVisible)
                {
                    typeDictionary[typeName].Invisible++;
                }
            }


            string toReturn = "Total: " + list.Count() + " " + typeof(T).Name + "s\n" +
                GetFilledStringBuilderWithNumberedTypes(typeDictionary).ToString();

            if (list.Count() == 0)
            {
                toReturn = "No automatically updated " + typeof(T).Name + "s";
            }

            return toReturn;

        
        }

        static StringBuilder stringBuilder = new StringBuilder();
        public static string GetFullPerformanceInformation()
        {
            if(Renderer.RecordRenderBreaks == false)
            {
                Renderer.RecordRenderBreaks = true;
            }
            stringBuilder.Clear();

            var objectCount = GetAutomaticallyUpdatedObjectInformation();
            stringBuilder.AppendLine(objectCount);
            stringBuilder.AppendLine();

            stringBuilder.Append(GetInstructionInformation());

            stringBuilder.AppendLine();
            stringBuilder.AppendLine($"Render breaks: {Renderer.LastFrameRenderBreakList.Count}");

            foreach(var renderBreak in Renderer.LastFrameRenderBreakList)
            {
                stringBuilder.AppendLine(renderBreak.ToString());
            }
            stringBuilder.AppendLine();
            var collisionInformation = GetCollisionInformation();
            stringBuilder.AppendLine(collisionInformation);

            stringBuilder.AppendLine();

            if (TimeManager.SecondsSince(LastCalculationTime) > mMemoryUpdateFrequency)
            {
                MemoryInformation = ForceGetMemoryInformation();
            }

            stringBuilder.Append(MemoryInformation);


            return stringBuilder.ToString();
        }

        private static string GetCollisionInformation()
        {
            var collisionManager = Math.Collision.CollisionManager.Self;
            int numberOfCollisions = 0;
            int? maxCollisions = null;
            CollisionRelationship collisionRelationshipWithMost = null;
            foreach (var relationship in collisionManager.Relationships)
            {
                numberOfCollisions += relationship.DeepCollisionsThisFrame;

                if(relationship.DeepCollisionsThisFrame > maxCollisions || maxCollisions == null)
                {
                    maxCollisions = numberOfCollisions;
                    collisionRelationshipWithMost = relationship;
                }
            }

            var collisionsThisFrame =
            $"Deep collisions: {Math.Collision.CollisionManager.Self.DeepCollisionsThisFrame}";
            if(collisionRelationshipWithMost != null)
            {
                collisionsThisFrame += 
                    $"\nHighest Relationship: {collisionRelationshipWithMost.Name} with {collisionRelationshipWithMost.DeepCollisionsThisFrame}";
            }
            return collisionsThisFrame;
        }

        public static void WritePositionedObjectBreakdown()
        {
            string result = GetPositionedObjectBreakdown();
            Write(result);
        }

        public static string GetPositionedObjectBreakdown()
        {
            Dictionary<string, CountedCategory> typeDictionary = new Dictionary<string, CountedCategory>();

            int count = SpriteManager.ManagedPositionedObjects.Count;

            for (int i = 0; i < count; i++)
            {
                var atI = SpriteManager.ManagedPositionedObjects[i];

                Type type = atI.GetType();

                if (typeDictionary.ContainsKey(type.Name) == false)
                {
                    var countedCategory = new CountedCategory();
                    countedCategory.String = type.Name;
                    typeDictionary.Add(type.Name, countedCategory);
                }

                typeDictionary[type.Name].Count++;
            }

            StringBuilder stringBuilder = GetFilledStringBuilderWithNumberedTypes(typeDictionary);

            string toReturn = "Total: " + count + "\n" + stringBuilder.ToString();

            if (count == 0)
            {
                toReturn = "No automatically updated PositionedObjects";
            }

            return toReturn;

        }

        public static string GetShapeBreakdown()
        {
            Dictionary<string, CountedCategory> typeDictionary = new Dictionary<string, CountedCategory>();

            foreach(PositionedObject shape in ShapeManager.AutomaticallyUpdatedShapes)
            {
                var parent = shape.Parent;

                string parentType = "<null>";
                if(parent != null)
                {
                    parentType = parent.GetType().Name;
                }

                if (typeDictionary.ContainsKey(parentType) == false)
                {
                    var countedCategory = new CountedCategory();
                    countedCategory.String = parentType;
                    typeDictionary.Add(parentType, countedCategory);
                }

                typeDictionary[parentType].Count++;

            }

            StringBuilder stringBuilder = GetFilledStringBuilderWithNumberedTypes(typeDictionary);

            var count = ShapeManager.AutomaticallyUpdatedShapes.Count;

            string toReturn = "Total: " + count + "\n" + stringBuilder.ToString();

            if (count == 0)
            {
                toReturn = "No automatically updated Shapes";
            }

            return toReturn;
        }

        public static string GetInstructionInformation()
        {
            return $"Instruction Count: {Instructions.InstructionManager.Instructions.Count}\n" +
                $"Unpause Instructions Count: {Instructions.InstructionManager.UnpauseInstructionCount}";
        }

        private static StringBuilder GetFilledStringBuilderWithNumberedTypes(Dictionary<string, CountedCategory> typeDictionary)
        {
            List<CountedCategory> listOfItems = new List<CountedCategory>();
            foreach (var kvp in typeDictionary)
            {
                listOfItems.Add(kvp.Value);
            }

            listOfItems.Sort((first, second) => second.Count.CompareTo(first.Count));

            StringBuilder stringBuilder = new StringBuilder();

            foreach (var item in listOfItems)
            {
                if (item.Invisible != 0)
                {
                    string whatToAdd = item.Count + " (" + item.Invisible + " invisible)" + " " + item.String;
                    stringBuilder.AppendLine(whatToAdd);

                }
                else
                {
                    stringBuilder.AppendLine(item.Count + " " + item.String);
                }
            }
            return stringBuilder;
        }

        public static void Log(string message)
        {
            LogStringBuilder.AppendLine(message);
        }

        public static void Update()
        {
            if (mText != null)
            {
                mText.DisplayText = "";



                mText.Red = TextRed;
                mText.Green = TextGreen;
                mText.Blue = TextBlue;


                if (mCommandLineOutput.Count != 0)
                {

                    for (int i = 0; i < mCommandLineOutput.Count; i++)
                    {
                        mText.DisplayText += mCommandLineOutput[i] + '\n';
                    }
                }
            }
            else if(mCommandLineOutput.Count != 0)
            {
                Write("Command Line...");

            }
        }

        internal static void UpdateDependencies()
        {
            if (mText != null)
            {
                AdjustTextPosition();
                mText.UpdateDependencies(TimeManager.CurrentTime);
                mText.SetPixelPerfectScale(mLayer);
                bool orthogonal = true;
                if (mLayer.LayerCameraSettings != null)
                {
                    orthogonal = mLayer.LayerCameraSettings.Orthogonal;
                }
                else
                {
                    orthogonal = Camera.Main.Orthogonal;
                }

                mText.AdjustPositionForPixelPerfectDrawing = orthogonal;
            }
        }

        private static void AdjustTextPosition()
        {
            switch(TextCorner)
            {
                case Corner.TopLeft:
                    mText.RelativeX = -SpriteManager.Camera.RelativeXEdgeAt(SpriteManager.Camera.Z -40) * .95f;// -15;
                    mText.RelativeY = SpriteManager.Camera.RelativeYEdgeAt(SpriteManager.Camera.Z - 40) * .95f;
                    mText.HorizontalAlignment = HorizontalAlignment.Left;
                    break;
                case Corner.TopRight:
                    mText.RelativeX = SpriteManager.Camera.RelativeXEdgeAt(SpriteManager.Camera.Z -40) * .95f;// -15;
                    mText.RelativeY = SpriteManager.Camera.RelativeYEdgeAt(SpriteManager.Camera.Z - 40) * .95f;
                    mText.HorizontalAlignment = HorizontalAlignment.Right;
                    break;
                case Corner.BottomLeft:
                    mText.RelativeX = -SpriteManager.Camera.RelativeXEdgeAt(SpriteManager.Camera.Z -40) * .95f;// -15;
                    mText.RelativeY = -SpriteManager.Camera.RelativeYEdgeAt(SpriteManager.Camera.Z - 40) * .95f + 
                        mText.NumberOfLines * mText.NewLineDistance;
                    mText.HorizontalAlignment = HorizontalAlignment.Left;

                    break;
                case Corner.BottomRight:
                    mText.RelativeX = SpriteManager.Camera.RelativeXEdgeAt(SpriteManager.Camera.Z - 40) * .95f;// -15;
                    mText.RelativeY = -SpriteManager.Camera.RelativeYEdgeAt(SpriteManager.Camera.Z - 40) * .95f +
                        NumberOfLinesInCommandLine * mText.NewLineDistance;
                    mText.HorizontalAlignment = HorizontalAlignment.Right;

                    break;

            }



            mText.RelativeZ = -40;

            if (float.IsNaN(mText.RelativeX) || float.IsPositiveInfinity(mText.RelativeX) || float.IsNegativeInfinity(mText.RelativeX))
            {
                mText.RelativeX = 0;
            }

            if (float.IsNaN(mText.RelativeY) || float.IsPositiveInfinity(mText.RelativeY) || float.IsNegativeInfinity(mText.RelativeY))
            {
                mText.RelativeY = 0;
            }

        }

        #endregion
    }
}
