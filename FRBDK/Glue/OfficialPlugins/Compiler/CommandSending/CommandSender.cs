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
        public static Action<string> PrintOutput { get; set; }
        static SemaphoreSlim sendCommandSemaphore = new SemaphoreSlim(1);
        public static async Task<string> Send(object dto, int port)
        {
            var dtoTypeName = dto.GetType().Name;

            var serialized = JsonConvert.SerializeObject(dto);

            return await SendCommand($"{dtoTypeName}:{serialized}", port);
        }

        static string LastCommand;
        // Maybe we should have a bool here on whether to wait. if false, exit if the sendCommandSemaphore returns false
        public static async Task<string> SendCommand(string text, int port, bool isImportant = true)
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

                LastCommand = text;

                TcpClient client = new TcpClient();

                // this takes ~2 seconds, according to this:
                // https://github.com/dotnet/runtime/issues/31085

                var isConnected = false;

                //await Task.Run(() =>
                //{
                //    try
                //    {
                //        client.Connect("127.0.0.1", port);
                //        isConnected = true;
                //    }
                //    catch(Exception)
                //    {
                //        // throw away - no need to tell the user it failed
                //    }
                //});
                const int timeoutDuration = 1000;
                var timeoutTask = Task.Delay(timeoutDuration);
                var connectTask = client.ConnectAsync("127.0.0.1", port);

                var completedTask = await Task.WhenAny(timeoutTask, connectTask);
                if (completedTask == timeoutTask)
                {
                    if (shouldPrint) PrintOutput("Timed out waiting for connection");
                    client.Dispose();
                    isConnected = false;
                }
                else
                {
                    isConnected = true;
                }





                if (isConnected)
                {
                    string read = null;
                    try
                    {
                        var steamStart = DateTime.Now;
                        // Stream string to server
                        DateTime startWrite;
                        if (!text.EndsWith("\n"))
                        {
                            text += "\n";
                        }
                        using (Stream stm = client.GetStream())
                        {

                            byte[] messageAsBytes = System.Text.ASCIIEncoding.UTF8.GetBytes(text);
                            stm.Write(messageAsBytes, 0, messageAsBytes.Length);
                            // give the server time to finish what it's doing:
                            const int millisecondsToLetGameRespond = 60;
                            await Task.Delay(millisecondsToLetGameRespond);
                            read = await ReadFromClient(client, stm);
                        }


                    }
                    catch(Exception e)
                    {
                        if (shouldPrint) PrintOutput($"Exception on get stream/write/read:\n{e}");
                        // do nothing...
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

        const int bufferSize = 1024;
        static byte[] buffer = new byte[bufferSize];
        private static async Task<string> ReadFromClient(TcpClient client, Stream stm)
        {
            //// Read response from server.
            //var readTask = stm.ReadAsync(buffer, 0, buffer.Length);

            byte[] intBytes = await GetByteArrayFromStream(stm, 4);
            var length = BitConverter.ToInt32(intBytes, 0);

            if(length > 0)
            {
                byte[] byteArray = await GetByteArrayFromStream(stm, length);
                var response = Encoding.ASCII.GetString(byteArray, 0, (int)byteArray.Length);

                client.Close();

                return response;
            }
            else
            {
                return string.Empty;
            }
            //Console.ReadLine();
        }

        private static async Task<byte[]> GetByteArrayFromStream(Stream stm, int length)
        {
            using var memoryStream = new MemoryStream();
            int totalBytesRead = 0;
            TimeSpan timeout = TimeSpan.FromSeconds(3);
            var timeStarted = DateTime.Now;
            int bytesRead = 0;
            do
            {
                bytesRead = await stm.ReadAsync(buffer, 0, Math.Min(length, buffer.Length));
                memoryStream.Write(buffer, 0, bytesRead);
                totalBytesRead += bytesRead;

            } while (totalBytesRead < length && DateTime.Now - timeStarted < timeout );

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
                screenName = await CommandSending.CommandSender.SendCommand("GetCurrentScreen", portNumber);
            }
            catch (SocketException)
            {
                // do nothing, may not have been able to communicate, just output
                //control.PrintOutput("Could not get the game's screen, restarting game from startup screen");
            }
            return screenName;
        }

        internal static async Task<Vector3> GetCameraPosition(int portNumber)
        {
            string cameraPositionAsString = null;

            try
            {
                cameraPositionAsString = await CommandSending.CommandSender.Send(new Dtos.GetCameraPosition(), portNumber);
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
