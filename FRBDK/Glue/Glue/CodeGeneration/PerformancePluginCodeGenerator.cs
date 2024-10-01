using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.CodeGeneration;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.Elements;
using FlatRedBall.IO;

namespace FlatRedBall.Glue.Plugins.Performance
{
    internal class PerformancePluginCodeGenerator
    {
        public static IElement SaveObject
        {
            get;
            set;
        }

        public static ICodeBlock CodeBlock
        {
            get;
            set;
        }

        static string mLastName;

        public static bool IsEnabled
        {
            get;
            set;
        }

        static PerformancePluginCodeGenerator()
        {
            IsEnabled = false;
        }

        public static void GenerateFields(ICodeBlock codeBlock, GlueElement element)
        {
            if (ObjectFinder.Self.GlueProject.PerformanceSettingsSave.RecordInitializeSegments &&
                element is ScreenSave)
            {
                codeBlock.Line("FlatRedBall.Performance.Measurement.Section mSection;");
            }

        }

        public static void GenerateStartTimingInitialize(IElement saveObject, ICodeBlock codeBlock)
        {
            if (ObjectFinder.Self.GlueProject.PerformanceSettingsSave.RecordInitializeSegments)
            {
                string appended = "";

                StartMeasurement(saveObject, codeBlock, appended, false);

            }
        }

        public static void GenerateEndTimingInitialize(IElement saveObject, ICodeBlock codeBlock)
        {
            if (ObjectFinder.Self.GlueProject.PerformanceSettingsSave.RecordInitializeSegments)
            {
                string appended = "";

                EndMeasurement(saveObject, codeBlock, appended, false);
            }
        }

        public static void GenerateStart(string appendName)
        {
            mLastName = appendName;
            GenerateStart(SaveObject, CodeBlock, appendName);
        }

        public static void GenerateStart(IElement saveObject, ICodeBlock codeBlock, string appendName)
        {
            if (ObjectFinder.Self.GlueProject.PerformanceSettingsSave.RecordInitializeSegments)
            {
                StartMeasurement(saveObject, codeBlock, appendName, true);
            }
        }

        public static void GenerateEnd()
        {
            GenerateEnd(SaveObject, CodeBlock, mLastName);
        }

        public static void GenerateEnd(IElement saveObject, ICodeBlock codeBlock, string appendName)
        {
            if (ObjectFinder.Self.GlueProject.PerformanceSettingsSave.RecordInitializeSegments)
            {
                EndMeasurement(saveObject, codeBlock, appendName, true);
            }
        }

        private static void StartMeasurement(IElement saveObject, ICodeBlock codeBlock, string appended, bool forceInstance)
        {
            if (saveObject is EntitySave || forceInstance)
            {
                codeBlock.Line("FlatRedBall.Performance.Measurement.Section section" + appended.Replace(" ", "_").Replace(".", "_") + ";");
            }
            string sectionInstance = GetSectionInstance(saveObject, forceInstance) + appended.Replace(" ", "_").Replace(".", "_");

            string name;

            if (saveObject != null)
            {
                name = FileManager.RemovePath(saveObject.Name);
            }
            else
            {
                name = "GlobalContent";
            }
            codeBlock.Line(sectionInstance + " = FlatRedBall.Performance.Measurement.Section.GetAndStartContextAndTime(\"" + name + appended + "\");");
        }
        
        private static void EndMeasurement(IElement saveObject, ICodeBlock codeBlock, string appended, bool forceInstance)
        {
            string sectionInstance = GetSectionInstance(saveObject, forceInstance);

            codeBlock.Line(sectionInstance + appended.Replace(" ", "_").Replace(".", "_") + ".EndTimeAndContext();");
        }

        static string GetSectionInstance(IElement saveObject, bool forceInstance)
        {
            if (saveObject is ScreenSave && !forceInstance)
            {
                return "mSection";
            }
            else
            {
                return "section";
            }
        }
    }
}
