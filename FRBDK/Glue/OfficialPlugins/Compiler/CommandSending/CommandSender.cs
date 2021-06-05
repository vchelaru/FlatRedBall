using FlatRedBall.Glue.Plugins.ExportedImplementations;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace OfficialPlugins.Compiler.CommandSending
{

    public static class CommandSender
    {
        public static async Task<string> SendCommand(string text, int port)
        {

            TcpClient client = new TcpClient();

            // this takes ~2 seconds, according to this:
            // https://github.com/dotnet/runtime/issues/31085

            var isConnected = false;

            await Task.Run(() =>
            {
                try
                {
                    client.Connect("127.0.0.1", port);
                    isConnected = true;
                }
                catch(Exception)
                {
                    // throw away - no need to tell the user it failed
                }
            });

            if(isConnected)
            {
                // Stream string to server
                Stream stm = client.GetStream();
                //ASCIIEncoding asen = new ASCIIEncoding();

                if(!text.EndsWith("\n"))
                {
                    text += "\n"; 
                }

                //byte[] ba = asen.GetBytes(input);
                byte[] messageAsBytes = System.Text.ASCIIEncoding.UTF8.GetBytes(text);
                stm.Write(messageAsBytes, 0, messageAsBytes.Length);


                // give the server time to finish what it's doing:
                //await Task.Delay((int)(1 * 60));
                var read = await ReadFromClient(client, client.GetStream());
                return read;
            }
            else
            {
                return null;
            }

        }

        private static async Task<string> ReadFromClient(TcpClient client, Stream stm)
        {

            //// Read response from server.
            byte[] buffer = new byte[1024];
            //var readTask = stm.ReadAsync(buffer, 0, buffer.Length);
            var bytesRead = await stm.ReadAsync(buffer, 0, buffer.Length);

            var response = Encoding.ASCII.GetString(buffer, 0, bytesRead);

            client.Close();

            return response;
            //Console.ReadLine();
        }

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
    }
}
