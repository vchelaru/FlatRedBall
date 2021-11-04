using FlatRedBall.Glue.Plugins.ExportedImplementations;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
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

        public static Action<string> PrintOutput { get; set; }
        static SemaphoreSlim sendCommandSemaphore = new SemaphoreSlim(1);

        public static int PortNumber { get; set; }

        #endregion

        public static async Task<string> Send(object dto)
        {
            var dtoTypeName = dto.GetType().Name;

            var serialized = JsonConvert.SerializeObject(dto);

            return await SendCommand($"{dtoTypeName}:{serialized}");
        }

        public static async Task<T> Send<T>(object dto)
        {
            var responseString = await Send(dto);

            try
            {
                return JsonConvert.DeserializeObject<T>(responseString);
            }
            catch
            {
                // no biggie
                return default(T);
            }
        }

        public static async Task<string> SendCommand(string text, bool isImportant = true)
        {
            var shouldPrint = isImportant && text?.StartsWith("SelectObjectDto:") == false;

            try
            {
                if(isImportant)
                {
                    DateTime startWait = DateTime.Now;
                    await sendCommandSemaphore.WaitAsync();
                    var waitEndTime = DateTime.Now;
                }
                else
                {
                    if(sendCommandSemaphore.Wait(0) == false)
                    {
                        return null;
                    }
                }

                await ConnectIfNecessary(PortNumber, shouldPrint);

                if (TcpClientStream != null)
                {
                    string read = null;
                    try
                    {
                        WriteMessageToStream(TcpClientStream, text);
                        const int millisecondsToLetGameRespond = 60;
                        await Task.Delay(millisecondsToLetGameRespond);
                        read = await ReadFromClient(TcpClientStream);
                    }
                    catch(Exception e)
                    {
                        if (shouldPrint) PrintOutput($"Exception on get stream/write/read:\n{e}");
                        // do nothing...
                        TcpClientStream = null;
                    }
                    return read;
                }
                else
                {
                    return null;
                }
            }
            finally
            {
                sendCommandSemaphore.Release();
            }

        }

        private static async Task ConnectIfNecessary(int port, bool shouldPrintTimeout)
        {
            if(TcpClientStream == null)
            {

                TcpClient client = new TcpClient();

                // this takes ~2 seconds, according to this:
                // https://github.com/dotnet/runtime/issues/31085

                var isConnected = false;

                const int timeoutDuration = 1000;
                var timeoutTask = Task.Delay(timeoutDuration);
                var connectTask = client.ConnectAsync("127.0.0.1", port);

                var completedTask = await Task.WhenAny(timeoutTask, connectTask);
                if (completedTask == timeoutTask)
                {
                    if (shouldPrintTimeout) PrintOutput("Timed out waiting for connection");
                    client.Dispose();
                    isConnected = false;
                    TcpClientStream = null;
                }
                else
                {
                    isConnected = true;
                    TcpClientStream = client.GetStream();
                }
            }
        }

        private static void WriteMessageToStream(Stream clientStream, string message)
        {
            byte[] messageAsBytes = System.Text.ASCIIEncoding.UTF8.GetBytes(message);

            var length = messageAsBytes.Length;
            var lengthAsBytes =
                BitConverter.GetBytes(length);
            clientStream.Write(lengthAsBytes, 0, 4);
            if (messageAsBytes.Length > 0)
            {
                clientStream.Write(messageAsBytes, 0, messageAsBytes.Length);

            }
        }


        const int bufferSize = 2048;
        static byte[] buffer = new byte[bufferSize];
        private static async Task<string> ReadFromClient(Stream stm)
        {
            //// Read response from server.
            //var readTask = stm.ReadAsync(buffer, 0, buffer.Length);

            byte[] intBytes = await GetByteArrayFromStream(stm, 4);
            var length = BitConverter.ToInt32(intBytes, 0);

            if(length > 0)
            {
                byte[] byteArray = await GetByteArrayFromStream(stm, length);
                var response = Encoding.UTF8.GetString(byteArray, 0, (int)byteArray.Length);


                return response;
            }
            else
            {
                return string.Empty;
            }
            //Console.ReadLine();
        }


        static TimeSpan readFromClientTimeout = TimeSpan.FromSeconds(4);

        private static async Task<byte[]> GetByteArrayFromStream(Stream stm, int totalLength)
        {
            using var memoryStream = new MemoryStream();
            int totalBytesRead = 0;

            var timeStarted = DateTime.Now;
            int lastReadNumberOfBytes = 0;

            int bytesLeftToRead = totalLength;

            bool hasMoreToRead = totalBytesRead < totalLength;
            do
            {
                lastReadNumberOfBytes = await stm.ReadAsync(buffer, 0, Math.Min(bytesLeftToRead, buffer.Length));
                memoryStream.Write(buffer, 0, lastReadNumberOfBytes);
                totalBytesRead += lastReadNumberOfBytes;

                bytesLeftToRead -= lastReadNumberOfBytes;
                hasMoreToRead = bytesLeftToRead > 0;
            } while (hasMoreToRead && lastReadNumberOfBytes != 0);

            var byteArray =
                memoryStream.ToArray();
            return byteArray;
        }

        /// <summary>
        /// Returns the qualified class name like "GameNamespace.Screens.MyScreen"
        /// </summary>
        /// <param name="portNumber">Game's port number</param>
        /// <returns>The screen name using screen name</returns>
        internal static async Task<string> GetScreenName(int portNumber)
        {
            string screenName = null;

            try
            {
                screenName = await CommandSending.CommandSender.SendCommand("GetCurrentScreen");
            }
            catch (SocketException)
            {

            }
            return screenName;
        }

        internal static async Task<Vector3> GetCameraPosition(int portNumber)
        {
            string cameraPositionAsString = null;

            try
            {
                cameraPositionAsString = await CommandSending.CommandSender.Send(new Dtos.GetCameraPosition());
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
