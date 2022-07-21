using GlueControl.Dtos;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace GlueControl.Managers
{
    public enum TaskExecutionPreference : ulong
    {
        Asap = 0,
        Fifo = 1 * (ulong.MaxValue / 3),
        AddOrMoveToEnd = 2 * (ulong.MaxValue / 3),
    }

    internal class GlueCommandsStateBase
    {
        protected async Task<object> SendMethodCallToGame(FacadeCommandBase dto, string caller = null, params object[] parameters)
        {
            dto.Method = caller;
            foreach (var parameter in parameters)
            {
                dto.Parameters.Add(parameter);
            }

            var objectResponse = await GlueControlManager.Self.SendToGlue(dto);
            return objectResponse;
        }
    }
}
