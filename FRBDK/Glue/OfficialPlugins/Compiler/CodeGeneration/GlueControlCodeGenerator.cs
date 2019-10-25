using FlatRedBall.Glue.Plugins.ExportedImplementations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OfficialPlugins.Compiler.CodeGeneration
{
    public class GlueControlCodeGenerator
    {


        public static string GetStringContents()
        {
            var toReturn = @"
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

// needs to generate the controlmanager init:
// GlueControlManager glueControlManager;
//glueControlManager = new GlueControlManager(8021);
//glueControlManager.Start();


namespace " + GlueState.Self.ProjectNamespace + @"
{
    public class GlueControlManager
    {
        bool isRunning;
        private TcpListener listener;

        public GlueControlManager(int port)
        {
            listener = new TcpListener(IPAddress.Any, port);
        }

        public void Start()
        {
            Thread serverThread = new Thread(new ThreadStart(Run));

            serverThread.Start();
        }

        private void Run()
        {
            isRunning = true;

            listener.Start();

            while (isRunning)
            {
                //Console.WriteLine($""Waiting for connection at { DateTime.Now}"");
                TcpClient client = listener.AcceptTcpClient();
                //Console.WriteLine($""Client connected at {DateTime.Now}"");
                HandleClient(client);

                client.Close();
            }

            isRunning = false;

            listener.Stop();
        }

        private void HandleClient(TcpClient client)
        {
            StreamReader reader = new StreamReader(client.GetStream());
            var stringBuilder = new StringBuilder();
            while (reader.Peek() != -1)
            {
                stringBuilder.AppendLine(reader.ReadLine());
            }

            ProcessMessage(stringBuilder.ToString()?.Trim());

            byte[] messageAsBytes = System.Text.ASCIIEncoding.UTF8.GetBytes(""true"");
            client.GetStream().Write(messageAsBytes, 0, messageAsBytes.Length);

        }

        private void ProcessMessage(string message)
        {
            var screen =
                FlatRedBall.Screens.ScreenManager.CurrentScreen;

            switch (message)
            {
                case ""RestartScreen"":
                    screen.RestartScreen(true);
                    break;
                case ""TogglePause"":

                    if (screen.IsPaused)
                    {
                        screen.UnpauseThisScreen();
                    }
                    else
                    {
                        screen.PauseThisScreen();
                    }

                    break;
        }
    }
}
}
";
            return toReturn;
        }
    }
}
