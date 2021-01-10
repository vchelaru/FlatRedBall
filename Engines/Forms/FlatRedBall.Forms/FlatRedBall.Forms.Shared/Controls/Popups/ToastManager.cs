using FlatRedBall.Managers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FlatRedBall.Forms.Controls.Popups
{
    public static class ToastManager 
    {
        static BlockingCollection<string> toastMessages = new BlockingCollection<string>();

        static bool hasBeenStarted;
        static void Start()
        {
            if(!hasBeenStarted)
            {
                hasBeenStarted = true;
                var thread = new Thread(new ThreadStart(DoLoop));
                thread.Start();
            }
        }

        public static void Show(string message)
        {
            if(!hasBeenStarted)
            {
                if(FlatRedBallServices.IsThreadPrimary())
                {
                    Start();
                }
                else
                {
                    Instructions.InstructionManager.AddSafe(Start);
                }
            }
            toastMessages.Add(message);
        }

        private static async void DoLoop()
        {
            Toast toast = null;

            foreach (var message in toastMessages.GetConsumingEnumerable(CancellationToken.None))
            {
                if(toast == null)
                {
                    toast = new FlatRedBall.Forms.Controls.Popups.Toast();
                }

                toast.Text = message;
                toast.Show();
                await Task.Delay(2000);
                toast.Close();
                // so there's a small gap between toasts
                await Task.Delay(100);
            }
        }
    }
}
