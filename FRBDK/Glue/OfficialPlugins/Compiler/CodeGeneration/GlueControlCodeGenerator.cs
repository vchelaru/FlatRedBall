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
                try
                {
                    TcpClient client = listener.AcceptTcpClient();
                    HandleClient(client);

                    client.Close();
                }
                catch(System.Exception e)
                {
                    if(isRunning)
                    {
                        throw e;
                    }
                }
            }

            isRunning = false;

            listener.Stop();
        }

        public void Kill()
        {
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

            var response = ProcessMessage(stringBuilder.ToString()?.Trim());
            if(response == null)
            {
                response = ""true"";
            }
            byte[] messageAsBytes = System.Text.ASCIIEncoding.UTF8.GetBytes(response);
            client.GetStream().Write(messageAsBytes, 0, messageAsBytes.Length);

        }

        private string ProcessMessage(string message)
        {
            var screen =
                FlatRedBall.Screens.ScreenManager.CurrentScreen;
            bool handledImmediately = false;
            switch(message)
            {
                case ""GetCurrentScreen"":
                    handledImmediately = true;
                    return screen.GetType().FullName;
            }

            if(!handledImmediately)
            {
                FlatRedBall.Instructions.InstructionManager.AddSafe(() =>
                {
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
                });
            }

            return ""true"";
        }
    }
}
";
            return toReturn;
        }
    }
}
