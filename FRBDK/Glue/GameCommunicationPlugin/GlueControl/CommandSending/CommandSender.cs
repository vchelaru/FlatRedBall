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

namespace GameCommunicationPlugin.GlueControl.CommandSending
{
    public class CommandSender
    {
        #region Fields/Properties

        public Action<string> PrintOutput { get; set; }
        public Func<string, Task<string>> SendPacket { get; set; }
        SemaphoreSlim sendCommandSemaphore = new SemaphoreSlim(1, 1);

        public GlueViewSettingsViewModel GlueViewSettingsViewModel { get; set; }
        public CompilerViewModel CompilerViewModel { get; set; }
        public bool IsConnected { get; internal set; }

        #endregion

        #region General Send
        public async Task<ToolsUtilities.GeneralResponse<string>> Send(object dto, bool isImportant = true)
        {
            var dtoTypeName = dto.GetType().Name;

            var serialized = JsonConvert.SerializeObject(dto);

            return await SendCommand($"{dtoTypeName}:{serialized}", isImportant);
        }

        public async Task<ToolsUtilities.GeneralResponse<T>> Send<T>(object dto, bool isImportant = true)
        {

            var sendResponse = await Send(dto, isImportant);
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

        private async Task<ToolsUtilities.GeneralResponse<string>> SendCommand(string text, bool isImportant = true)
        {
            var shouldPrint = isImportant && text?.StartsWith("SelectObjectDto:") == false;

            if(isImportant && CompilerViewModel.IsPrintEditorToGameCheckboxChecked)
            {

                if(CompilerViewModel.IsShowParametersChecked && CompilerViewModel.CommandParameterCheckboxVisibility == System.Windows.Visibility.Visible)
                {
                    PrintOutput(text);
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
                    await sendCommandSemaphore.WaitAsync();
                }

                return await SendCommandNoSemaphore(text, isImportant, shouldPrint, shouldRetry:true);
            }
            finally
            {
                sendCommandSemaphore.Release();
            }

        }

        private async Task<ToolsUtilities.GeneralResponse<string>> SendCommandNoSemaphore(string text, bool isImportant, bool shouldPrint, bool shouldRetry )
        {
            var returnValue = await SendPacket(text);

            if (returnValue == null)
            {
                return new ToolsUtilities.GeneralResponse<string>
                {
                    Succeeded = false,
                    Message = "No Handler Found",
                    Data = null
                };
            }

            var response = JsonConvert.DeserializeObject<ToolsUtilities.GeneralResponse<string>>(returnValue);

            if(!response.Succeeded && shouldRetry)
            {
                return await SendCommandNoSemaphore(text, isImportant, shouldPrint, false);
            }

            return response;
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
