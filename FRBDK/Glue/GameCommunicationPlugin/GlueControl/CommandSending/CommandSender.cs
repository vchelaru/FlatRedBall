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
using Newtonsoft.Json.Linq;

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

        GameJsonCommunicationPlugin.Common.GameConnectionManager connectionManager;
        /// <summary>
        /// The transport this sender writes to. Defaults to the process-wide instance the plugin creates;
        /// assignable so a test can point one sender at its own loopback connection instead of racing the
        /// singleton.
        /// </summary>
        internal GameJsonCommunicationPlugin.Common.GameConnectionManager ConnectionManager
        {
            get => connectionManager ?? GameJsonCommunicationPlugin.Common.GameConnectionManager.Self;
            set => connectionManager = value;
        }


        #endregion

        static CommandSender()
        {
            Self = new CommandSender();
        }
        internal CommandSender() { }

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
            // An empty reply is the game's "handled it, nothing to send back", which is a fine answer for a
            // fire-and-forget command but not for a caller that asked for typed data. Deserializing it
            // yields null without throwing, so without this the caller gets Succeeded = true, Data = null
            // and has to invent its own meaning for that.
            else if(string.IsNullOrWhiteSpace(responseString))
            {
                toReturn.Succeeded = false;
                toReturn.Message = $"The game did not send back a {typeof(T).Name}, so the command was not carried out";
                toReturn.Data = default(T);
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
            /////////////////////////////////Early Out/////////////////////////////////////
            if(GlueState.Self.CurrentGlueProject == null)
            {
                return ToolsUtilities.GeneralResponse<string>.UnsuccessfulWith("No project loaded");
            }
            ///////////////////////////////End Early Out///////////////////////////////////


            // commands cannot be sent when receiving commands or we get a deadlock:
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
                    //GlueCommands.Self.PrintOutput($"Waiting to send {textPrefix}\nWaiting on {lastPrefix}");
                    await sendCommandSemaphore.WaitAsync();
                    //GlueCommands.Self.PrintOutput($"--Done with {textPrefix}");
                }

                lastStartedSend = text;

                GeneralResponse<string> result = GeneralResponse<string>.UnsuccessfulResponse;

                if (importance == SendImportance.RetryOnFailure)
                {
                    // A single attempt races the game's own startup (see GameReadinessRetryPolicy) -
                    // the game can report itself as not connected, or not yet ready to dispatch the
                    // command, for up to a second or so after its window first appears. Retrying
                    // without waiting between attempts loses that race every time, since the condition
                    // that makes the next attempt succeed hasn't had time to change.
                    await GameReadinessRetryPolicy.TryRepeatedlyAsync(async () =>
                    {
                        result = await SendCommandNoSemaphore(text, isImportant, shouldPrint, waitForResponse);
                        return result.Succeeded;
                    });
                }
                else
                {
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
            var returnValue = await SendPacketInternal(text, waitForResponse);

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

        public async Task<GeneralResponse<string>> SendPacketInternal(string value, bool waitForResponse)
        {
            var whatToSend = new GameJsonCommunicationPlugin.Common.GameConnectionManager.Packet
            {
                PacketType = "OldDTO",
                Payload = value
            };

            if(waitForResponse)
            {
                var toReturn = new global::ToolsUtilities.GeneralResponse<string>();
                try
                {
                    var gameConnectionManager = ConnectionManager;
                    if(gameConnectionManager != null)
                    {
                        var response = await gameConnectionManager.SendItemWithResponse(whatToSend);

                        toReturn.SetFrom(response);
                        toReturn.Data = response?.Data;    
                    }
                }
                catch(Exception e)
                {
                    toReturn.Succeeded = false;
                    toReturn.Message = $"Failed to send packet: {e}";
                }

                return toReturn;
            }
            else
            {
                await ConnectionManager.SendItem(whatToSend);
                // I guess we return success?
                return new global::ToolsUtilities.GeneralResponse<string>() { Succeeded = true };
            }

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
