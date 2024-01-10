using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace FlatRedBall.Utilities
{
    public interface IPlayable
    {
        void Play();
        Task PlayAsync();
    }
}
