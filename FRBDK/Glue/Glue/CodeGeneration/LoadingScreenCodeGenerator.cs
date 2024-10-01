using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.SaveClasses;

using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.IO;

namespace FlatRedBall.Glue.CodeGeneration
{
    public class LoadingScreenCodeGenerator : ElementComponentCodeGenerator
    {
        bool IsLoadingScreen(GlueElement element)
        {
            ScreenSave throwaway;
            return IsLoadingScreen(element, out throwaway);
        }

        bool IsLoadingScreen(GlueElement element, out ScreenSave screenSave)
        {
            screenSave = null;

            if (element is ScreenSave)
            {
                ScreenSave asScreenSave = element as ScreenSave;

                return asScreenSave.IsLoadingScreen;
            }
            return false;
        }

        public override ICodeBlock GenerateFields(ICodeBlock codeBlock, GlueElement element)
        {
            if (IsLoadingScreen(element))
            {
                codeBlock.Line("double mSavedTargetElapedTime;");

                codeBlock.Line("private static System.Action<FlatRedBall.Screens.Screen> nextCallback;");
            }
            return codeBlock;
        }
        public override CodeBuilder.ICodeBlock GenerateInitialize(CodeBuilder.ICodeBlock codeBlock, SaveClasses.GlueElement element)
        {
            if(IsLoadingScreen(element))
            {
                codeBlock.Line("mSavedTargetElapedTime = FlatRedBallServices.Game.TargetElapsedTime.TotalSeconds;");
                codeBlock.Line("FlatRedBall.FlatRedBallServices.Game.TargetElapsedTime = TimeSpan.FromSeconds(.1);");

            }
            return codeBlock;
        }

        public override ICodeBlock GenerateActivity(ICodeBlock codeBlock, GlueElement element)
        {
            if(IsLoadingScreen(element))
            {
                codeBlock.Line("AsyncActivity();");
            }
            return codeBlock;
        }

        public override ICodeBlock GenerateAdditionalMethods(ICodeBlock codeBlock, GlueElement element)
        {
            if(IsLoadingScreen(element))
            {
                codeBlock
                    .Line("static string mNextScreenToLoad;")
                    .Property("public static string", "NextScreenToLoad")
                        .Get()
                            .Line("return mNextScreenToLoad;")
                        .End()
                        .Set()
                            .Line("mNextScreenToLoad = value;")
                        .End()
                    .End();


                string screenName =
                    FileManager.RemovePath(element.Name);

                
                
                codeBlock
                    .Function("public static void", "TransitionToScreen", "System.Type screenType, System.Action<FlatRedBall.Screens.Screen> screenCreatedCallback = null")
                        .Line("TransitionToScreen(screenType.FullName, screenCreatedCallback);")
                    .End();


                codeBlock
                    .Function("public static void", "TransitionToScreen", "string screenName, System.Action<FlatRedBall.Screens.Screen> screenCreatedCallback = null")
                        .Line("FlatRedBall.Screens.Screen currentScreen = FlatRedBall.Screens.ScreenManager.CurrentScreen;")
                        .Line("currentScreen.IsActivityFinished = true;")
                        .Line("currentScreen.NextScreen = typeof(" + screenName + ").FullName;")
                        .Line("mNextScreenToLoad = screenName;")
                        .Line("nextCallback = screenCreatedCallback;")
                    .End();


                codeBlock
                    .Function("void", "AsyncActivity", "")
                        .Switch("AsyncLoadingState")
                            .Case("FlatRedBall.Screens.AsyncLoadingState.NotStarted")
                                .If("!string.IsNullOrEmpty(mNextScreenToLoad)")
                                    .Line("#if REQUIRES_PRIMARY_THREAD_LOADING")
                                    .If("HasDrawBeenCalled")
                                        .Line("MoveToScreen(mNextScreenToLoad);")
                                    .End()
                                    .Line("#else")
                                    .Line("StartAsyncLoad(mNextScreenToLoad);")
                                    .Line("#endif")
                                .End()
                            .End()
                            .Case("FlatRedBall.Screens.AsyncLoadingState.LoadingScreen")
                            .End()
                            .Case("FlatRedBall.Screens.AsyncLoadingState.Done")
                    // The loading screen can be used to rehydrate.  
                                .Line("FlatRedBall.Screens.ScreenManager.ShouldActivateScreen = false;")
                                
                                .Line("FlatRedBall.Screens.ScreenManager.MoveToScreen(mNextScreenToLoad, nextCallback);")
                            .End()
                        .End()
                    .End();
            }

            return codeBlock;
        }

        public override ICodeBlock GenerateDestroy(ICodeBlock codeBlock, GlueElement element)
        {
            if (IsLoadingScreen(element))
            {
                codeBlock.Line("FlatRedBall.FlatRedBallServices.Game.TargetElapsedTime = TimeSpan.FromSeconds(mSavedTargetElapedTime);");
            }
            return codeBlock;
        }
    }
}
