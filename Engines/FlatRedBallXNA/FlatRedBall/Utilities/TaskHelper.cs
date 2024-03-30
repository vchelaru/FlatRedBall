using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace FlatRedBall.Utilities
{
    public static class TaskHelper
    {
        public static async Task InSequence(params Func<Task>[] tasks)
        {
            foreach(var task in tasks)
            {
                await task();
            }
        }

        public static async Task InSequence(params Task[] tasks)
        {
            foreach(var task in tasks)
            {
                await task;
            }
        }
    }
}
