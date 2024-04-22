using FlatRedBall.Content.Scene;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using GameCommunicationPlugin.GlueControl.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CompilerLibrary.ViewModels;
using ToolsUtilities;

namespace GameCommunicationPlugin.GlueControl.CommandSending
{
    public enum SendImportance
    {
        IfNotBusy,
        Normal,
        RetryOnFailure
    }

    public class CommandSender
    {
        #region Fields/Properties

        public Action<string> PrintOutput { get; set; }
        public Func<string, bool, Task<GeneralResponse<string>>> SendPacket { get; set; }
        SemaphoreSlim sendCommandSemaphore = new SemaphoreSlim(1, 1);

        public GlueViewSettingsViewModel GlueViewSettingsViewModel { get; set; }
        public CompilerViewModel CompilerViewModel { get; set; }

        bool isConnected;
        public bool IsConnected 
        { 
            get => isConnected;
            internal set
            {
                isConnected = value;

                if (CompilerViewModel != null)
                {
                    CompilerViewModel.IsConnectedToGame = value;
                }
            }
        }

        public static CommandSender Self { get; private set; }


        #endregion

        static CommandSender()
        {
            Self = new CommandSender();
        }
        private CommandSender() { }
        #region General Send


        public async Task<ToolsUtilities.GeneralResponse<string>> Send(object dto, SendImportance importance = SendImportance.Normal, bool waitForResponse = true)
        {
            var dtoTypeName = dto.GetType().Name;

            var serialized = JsonConvert.SerializeObject(dto);

            return await SendCommand($"{dtoTypeName}:{serialized}", importance, waitForResponse:waitForResponse);
        }

        public async Task<ToolsUtilities.GeneralResponse<T>> Send<T>(object dto, SendImportance importance = SendImportance.Normal)
        {

            var sendResponse = await Send(dto, importance);
            var responseString = sendResponse.Succeeded ? sendResponse.Data : String.Empty;

            ToolsUtilities.GeneralResponse<T> toReturn = new ToolsUtilities.GeneralResponse<T>();

            if(sendResponse.Succeeded == false)
            {
                toReturn.SetFrom(sendResponse);
            }
            else
            {
                try
                {
                    var deserialized = JsonConvert.DeserializeObject<T>(responseString);
                    toReturn.Succeeded = true;
                    toReturn.Data = deserialized;
                }
                catch(Exception e)
                {
                    toReturn.Succeeded = false;
                    toReturn.Message = $"Failed with exception:\n{e}";
                    toReturn.Data = default(T);
                }

            }
            return toReturn;
        }

        string lastStartedSend;
        string lastFinishedSend;
        private async Task<ToolsUtilities.GeneralResponse<string>> SendCommand(string text, SendImportance importance = SendImportance.Normal, bool waitForResponse = true)
        {
            // commands cannot be sent when receiving commands or we get a deadlock:
            var stackTrace = new System.Diagnostics.StackTrace();

            bool hasHandleDto = false;
            bool hasSendResponseBack = false;
            foreach (var frame in stackTrace.GetFrames())
            {
                var methodName = frame.GetMethod().Name;
                if (methodName == "HandleDto")
                {
                    hasHandleDto = true;
                }
                else if (methodName == "SendResponseBackToGame")
                {
                    hasSendResponseBack = true;
                }
            }

            if (hasHandleDto && !hasSendResponseBack)
            {
                return ToolsUtilities.GeneralResponse<string>.UnsuccessfulWith("Cannot send commands while receiving commands");
            }


            var isImportant = importance != SendImportance.IfNotBusy;
            var shouldPrint = isImportant && text?.StartsWith("SelectObjectDto:") == false;

            if(isImportant && CompilerViewModel.IsPrintEditorToGameCheckboxChecked)
            {

                if(CompilerViewModel.IsShowParametersChecked && CompilerViewModel.CommandParameterCheckboxVisibility == System.Windows.Visibility.Visible)
                {
                    PrintOutput(text);
                    GlueCommands.Self.PrintOutput(text);
                    PrintOutput("------------------------------------------");
                }
                else
                {
                    string prefix = text;
                    if(text.Contains(":"))
                    {
                        var indexOfColon = text.IndexOf(":");
                        prefix = text.Substring(0, indexOfColon);
                    }
                    PrintOutput(prefix);
                    GlueCommands.Self.PrintOutput(prefix);
                }
            }

            var isSemaphoreAvailable = sendCommandSemaphore.Wait(0);

            if (!isImportant && !isSemaphoreAvailable)
            {
                return ToolsUtilities.GeneralResponse<string>.UnsuccessfulWith("Didn't try because it wasn't important and this was already busy.");
            }
            try
            {
                if (!isSemaphoreAvailable)
                {
                    
                    var textPrefix = text?.Contains(":")==true ? text.Substring(0, text.IndexOf(":")) : text;
                    var lastPrefix = lastStartedSend?.Contains(":")==true ? lastStartedSend.Substring(0, lastStartedSend.IndexOf(":")) : lastStartedSend;
                    GlueCommands.Self.PrintOutput($"Waiting to send {textPrefix}\nWaiting on {lastPrefix}");
                    await sendCommandSemaphore.WaitAsync();
                    GlueCommands.Self.PrintOutput($"--Done with {textPrefix}");
                }

                lastStartedSend = text;

                int triesLeft = importance  == SendImportance.RetryOnFailure ? 5 : 1;
                GeneralResponse<string> result = GeneralResponse<string>.UnsuccessfulResponse;

                while(result.Succeeded == false && triesLeft > 0)
                {
                    triesLeft--;
                    result = await SendCommandNoSemaphore(text, isImportant, shouldPrint, waitForResponse);
                }

                lastFinishedSend = text;

                return result;
            }
            finally
            {
                sendCommandSemaphore.Release();
            }

        }

        private async Task<ToolsUtilities.GeneralResponse<string>> SendCommandNoSemaphore(string text, bool isImportant, bool shouldPrint, bool waitForResponse )
        {
            var returnValue = await SendPacket(text, waitForResponse);

            if (returnValue == null)
            {
                return new ToolsUtilities.GeneralResponse<string>
                {
                    Succeeded = false,
                    Message = "No Handler Found",
                    Data = null
                };
            }


            return returnValue;
        }


        #endregion

        /// <summary>
        /// Returns the qualified class name like "GameNamespace.Screens.MyScreen"
        /// </summary>
        /// <param name="portNumber">Game's port number</param>
        /// <returns>The screen name using screen name</returns>
        internal async Task<string> GetScreenName()
        {
            string screenName = null;

            try
            {
                var response = await SendCommand("GetCurrentScreen");
                if(response.Succeeded)
                {
                    screenName = response.Data;
                }
            }
            catch (SocketException)
            {

            }
            return screenName;
        }

        public async Task<FlatRedBall.Glue.SaveClasses.ScreenSave> GetCurrentInGameScreen()
        {
            var screenName = await GetScreenName();

            if (!string.IsNullOrEmpty(screenName) && screenName.Contains(".Screens."))
            {
                // remove prefix:
                var screensDotStart = screenName.IndexOf("Screens.");
                screenName = screenName.Substring(screensDotStart).Replace(".", "\\");
                var screen = FlatRedBall.Glue.Elements.ObjectFinder.Self.GetScreenSave(screenName);
                return screen;
            }
            else
            {
                return null;
            }
        }

        internal async Task<Vector3> GetCameraPosition()
        {
            var sendResponse = await Send(new Dtos.GetCameraPosition());
            var cameraPositionAsString = sendResponse.Succeeded ? sendResponse.Data : String.Empty;

            if(string.IsNullOrEmpty(cameraPositionAsString))
            {
                return Vector3.Zero;
            }
            else
            {
                var response = JsonConvert.DeserializeObject<Dtos.GetCameraPositionResponse>(cameraPositionAsString);
                return new Vector3(response.X, response.Y, response.Z);
            }
        }

        internal async Task<CameraSave> GetCameraSave()
        {
            var sendResponse = await Send(new Dtos.GetCameraSave());
            var cameraSaveAsString = sendResponse.Succeeded ? sendResponse.Data : String.Empty;

            if (string.IsNullOrEmpty(cameraSaveAsString))
            {
                return null;
            }
            else
            {
                var cameraSave = JsonConvert.DeserializeObject<CameraSave>(cameraSaveAsString);
                return cameraSave;
            }
        }
    }
}
