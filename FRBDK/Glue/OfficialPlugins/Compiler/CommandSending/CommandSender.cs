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
        static SemaphoreSlim sendCommandSemaphore = new SemaphoreSlim(1, 1);

        public static int PortNumber { get; set; }

        #endregion

        public static async Task<string> Send(object dto, bool isImportant = true)
        {
            var dtoTypeName = dto.GetType().Name;

            var serialized = JsonConvert.SerializeObject(dto);

            return await SendCommand($"{dtoTypeName}:{serialized}", isImportant);
        }

        public static async Task<T> Send<T>(object dto, bool isImportant = true)
        {
            var responseString = await Send(dto, isImportant);

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

            var isInSemaphore = sendCommandSemaphore.Wait(0);

            if (!isImportant && !isInSemaphore)
            {
                return null;
            }
            if(!isInSemaphore)
            {
                await sendCommandSemaphore.WaitAsync();
            }

            try
            {
                await ConnectIfNecessary(PortNumber, shouldPrint);

                if (TcpClientStream != null)
                {
                    string stringFromClient = null;
                    try
                    {
                        await WriteMessageToStream(TcpClientStream, text);

                        stringFromClient = await ReadFromClient(TcpClientStream);
                    }
                    catch(IOException)
                    {
                        // this is expected, happens if the game is closed
                        TcpClientStream = null;
                    }
                    catch(Exception e)
                    {
                        if (shouldPrint) PrintOutput($"Exception on get stream/write/read:\n{e}");
                        // do nothing...
                        TcpClientStream = null;
                    }
                    return stringFromClient;
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

                const int timeoutDuration = 1000;
                var timeoutTask = Task.Delay(timeoutDuration);
                var connectTask = client.ConnectAsync("127.0.0.1", port);

                var completedTask = await Task.WhenAny(timeoutTask, connectTask);
                if (completedTask == timeoutTask)
                {
                    if (shouldPrintTimeout) PrintOutput("Timed out waiting for connection");
                    client.Dispose();
                    TcpClientStream = null;
                }
                else
                {
                    TcpClientStream = client.GetStream();
                }
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
