using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using FlatRedBall;
using FlatRedBall.Glue;
using System.Runtime.Remoting;
using System.Threading;
using FlatRedBall.Glue.Plugins;

namespace Glue
{
	static class Program
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main()
		{
            // Add proper exception handling so we can handle plugin exceptions:
            CreateExceptionHandlingEvents();

			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
            bool succeededToObtainMutex;

            // Only open one instance of Glue
            System.Threading.Mutex appMutex = new System.Threading.Mutex(true, Application.ProductName, out succeededToObtainMutex);
            // Creates a Mutex, if it works, then we can go on and start Glue, if not...
            if (succeededToObtainMutex)
            {
                try
                {
                    Application.Run(new MainGlueWindow());
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.ToString());
                }
                finally
                {
                    appMutex.ReleaseMutex();

                }
            }
            else
            {
                //...We come here, and tell the user they already have Glue running.
                string msg = String.Format("The Program \"{0}\" is already running", Application.ProductName);
                MessageBox.Show(msg, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
         }

        private static void CreateExceptionHandlingEvents()
        {
            // Add the event handler for handling UI thread exceptions to the event.
            Application.ThreadException += new ThreadExceptionEventHandler(UIThreadException);

            // Set the unhandled exception mode to force all Windows Forms errors to go through
            // our handler.
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

            // Add the event handler for handling non-UI thread exceptions to the event. 
            AppDomain.CurrentDomain.UnhandledException +=
                new UnhandledExceptionEventHandler(UnhandledException);
        }

        private static void UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            HandleExceptionsUnified(e.ExceptionObject);
        }

        private static void HandleExceptionsUnified(object objectToPrint)
        {
            bool wasPluginError = PluginManager.TryHandleException(objectToPrint as Exception);
            if (!wasPluginError)
            {
                MessageBox.Show("Error: " + objectToPrint);
            }
        }

        private static void UIThreadException(object sender, ThreadExceptionEventArgs e)
        {
            HandleExceptionsUnified(e.Exception);
        } 
			

    }
}	

