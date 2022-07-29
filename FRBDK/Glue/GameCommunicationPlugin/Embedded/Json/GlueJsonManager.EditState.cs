using System;
using System.Collections.Generic;
using System.Text;
using GlueControl.Dtos;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Threading.Tasks;

namespace GlueCommunication.Json
{
    internal partial class GlueJsonManager
    {
        public void UpdateEditState(SelectObjectDto dto)
        {
            var editStateMgr = GetEditState();
            var editStateJson = editStateMgr.GetCurrentUIJson();

            editStateJson["SelectionDTO"] = JObject.Parse(JsonConvert.SerializeObject(dto));

            var patch = editStateMgr.ApplyUIUpdate(editStateJson);

            if (patch != null)
            {
                Debug.Print($"Changes for Edit State");
                Debug.Print(patch.ToString());

                Task.Run(async () =>
                {
                    try
                    {
                        var returnValue = await SendPacketWithResponse(new GameConnectionManager.Packet
                        {
                            PacketType = PacketType_JsonUpdate,
                            Payload = JsonConvert.SerializeObject(new
                            {
                                Type = "EditState",
                                Patch = patch.ToString()
                            })
                        });
                    }
                    finally
                    {
                        editStateMgr.UpdateJson(patch);
                    }
                });

            }
        }
    }
}