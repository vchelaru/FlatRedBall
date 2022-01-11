using FlatRedBall.Glue.Plugins.ExportedImplementations;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using OfficialPlugins.Compiler.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OfficialPlugins.Compiler.CommandSending
{
    public static class CommandSender
    {
        #region Fields/Properties

        static Stream TcpClientStream;
        public static bool IsConnected => TcpClientStream != null;

        public static Action<string> PrintOutput { get; set; }
        static SemaphoreSlim sendCommandSemaphore = new SemaphoreSlim(1, 1);

        public static GlueViewSettingsViewModel GlueViewSettingsViewModel { get; set; }
        public static CompilerViewModel CompilerViewModel { get; set; }

        #endregion

        public static async Task<ToolsUtilities.GeneralResponse<string>> Send(object dto, bool isImportant = true)
        {
            var dtoTypeName = dto.GetType().Name;

            var serialized = JsonConvert.SerializeObject(dto);

            return await SendCommand($"{dtoTypeName}:{serialized}", isImportant);
        }

        public static async Task<ToolsUtilities.GeneralResponse<T>> Send<T>(object dto, bool isImportant = true)
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

        private static async Task<ToolsUtilities.GeneralResponse<string>> SendCommand(string text, bool isImportant = true)
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

        private static async Task<ToolsUtilities.GeneralResponse<string>> SendCommandNoSemaphore(string text, bool isImportant, bool shouldPrint, bool shouldRetry )
        {
            ToolsUtilities.GeneralResponse<string> toReturn = new ToolsUtilities.GeneralResponse<string>();
            toReturn.Succeeded = true;

            var connectResponse = await ConnectIfNecessary(GlueViewSettingsViewModel.PortNumber, shouldPrint);

            if (!connectResponse.Succeeded)
            {
                toReturn.SetFrom(connectResponse);
            }

            if (toReturn.Succeeded && TcpClientStream != null)
            {
                string stringFromClient = null;
                try
                {
                    await WriteMessageToStream(TcpClientStream, text);

                    stringFromClient = await ReadFromClient(TcpClientStream);
                    toReturn.Data = stringFromClient;
                }
                catch (IOException ioexception)
                {
                    // this is expected, happens if the game is closed
                    TcpClientStream = null;
                    toReturn.Succeeded = false;
                    toReturn.Message = $"IOException trying to write to client:\n{ioexception}";

                    if (shouldRetry)
                    {
                        // retry, but only once
                        toReturn = await SendCommandNoSemaphore(text, isImportant, shouldPrint, shouldRetry: false);
                    }
                }
                catch (Exception e)
                {
                    var message = $"Exception on get stream/write/read:\n{e}";
                    if (shouldPrint) PrintOutput(message);
                    // do nothing...
                    TcpClientStream = null;
                    toReturn.Succeeded = false;
                    toReturn.Message = message;
                }
            }

            return toReturn;
        }

        private static async Task<ToolsUtilities.GeneralResponse> ConnectIfNecessary(int port, bool shouldPrintTimeout)
        {
            if(TcpClientStream == null)
            {

                TcpClient client = new TcpClient();

                // this takes ~2 seconds, according to this:
                // https://github.com/dotnet/runtime/issues/31085

                var connectTask = client.ConnectAsync("127.0.0.1", port);
                // 1000 seemed to timeout - not super frequently but sometimes
                // Increasing to 2000 
                const int timeoutDuration = 2000;
                var timeoutTask = Task.Delay(timeoutDuration);

                var completedTask = await Task.WhenAny(timeoutTask, connectTask);
                if (completedTask == timeoutTask)
                {
                    if (shouldPrintTimeout) PrintOutput("Timed out waiting for connection");
                    client.Dispose();
                    TcpClientStream = null;
                    var response = ToolsUtilities.GeneralResponse.UnsuccessfulResponse;
                    response.Message = "Timed out waiting for connection";
                    return response;
                }
                else
                {
                    TcpClientStream = client.GetStream();

                    if (TcpClientStream != null)
                    {
                        return ToolsUtilities.GeneralResponse.SuccessfulResponse;
                    }
                    else
                    {
                        return ToolsUtilities.GeneralResponse.UnsuccessfulWith("Tried to connect, did not time out, but still was unable to get a TcpClientStream");

                    }
                }
            }
            else
            {
                return ToolsUtilities.GeneralResponse.SuccessfulResponse;
            }
        }

        private static async Task WriteMessageToStream(Stream clientStream, string message)
        {
            byte[] messageAsBytes = System.Text.ASCIIEncoding.UTF8.GetBytes(message);

            if (messageAsBytes.Length > 0)
            {
                var lengthAsBytes =
                    BitConverter.GetBytes(messageAsBytes.Length);
                await clientStream.WriteAsync(lengthAsBytes, 0, lengthAsBytes.Length);
                await clientStream.WriteAsync(messageAsBytes, 0, messageAsBytes.Length);

            }
        }


        private static async Task<string> ReadFromClient(Stream stm)
        {
            //// Read response from server.
            //var readTask = stm.ReadAsync(buffer, 0, buffer.Length);

            byte[] intBytes = await GetByteArrayFromStream(stm, 4, new byte[4]);
            var length = BitConverter.ToInt32(intBytes, 0);

            if(length > 1_000_000)
            {
                var firstThousand = await GetByteArrayFromStream(stm, 1000);
                var response = Encoding.UTF8.GetString(firstThousand, 0, firstThousand.Length);

                int m = 3;
            }
            if(length > 0)
            {
                byte[] byteArray = await GetByteArrayFromStream(stm, length);
                var response = Encoding.UTF8.GetString(byteArray, 0, length);
                return response;
            }
            else
            {
                return string.Empty;
            }
            //Console.ReadLine();
        }

        static byte[] defaultBuffer = new byte[8192];

        private static async Task<byte[]> GetByteArrayFromStream(Stream stm, int totalLength, byte[] buffer = null)
        {
            byte[] toReturn = null;

            using (var memoryStream = new MemoryStream())
            {
                buffer = buffer ?? defaultBuffer;
                int bytesRead;
                int bytesLeft = totalLength;
                while (bytesLeft > 0 && (bytesRead = await stm.ReadAsync(buffer, 0, Math.Min(buffer.Length, bytesLeft))) > 0)
                {
                    memoryStream.Write(buffer, 0, bytesRead);
                    bytesLeft -= bytesRead;
                }
                toReturn = memoryStream.ToArray();
            }
            return toReturn;

            //if (buffer == null && totalLength > defaultBuffer.Length)
            //{
            //    defaultBuffer = new byte[totalLength];
            //}
            //buffer = buffer ?? defaultBuffer;
            //var amountRead = await stm.ReadAsync(buffer, 0, totalLength);
            //if(amountRead != totalLength)
            //{
            //    int m = 3;
            //}
            //return buffer;
        }

        /// <summary>
        /// Returns the qualified class name like "GameNamespace.Screens.MyScreen"
        /// </summary>
        /// <param name="portNumber">Game's port number</param>
        /// <returns>The screen name using screen name</returns>
        internal static async Task<string> GetScreenName()
        {
            string screenName = null;

            try
            {
                var response = await CommandSending.CommandSender.SendCommand("GetCurrentScreen");
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

        public static async Task<FlatRedBall.Glue.SaveClasses.ScreenSave> GetCurrentInGameScreen()
        {
            var screenName = await CommandSender.GetScreenName();

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

        internal static async Task<Vector3> GetCameraPosition(int portNumber)
        {
            string cameraPositionAsString = null;

            try
            {
                var sendResponse = await CommandSending.CommandSender.Send(new Dtos.GetCameraPosition());
                cameraPositionAsString = sendResponse.Succeeded ? sendResponse.Data : String.Empty;
            }
            catch (SocketException)
            {
                // do nothing, may not have been able to communicate, just output
                //control.PrintOutput("Could not get the game's screen, restarting game from startup screen");
            }

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
    }
}
