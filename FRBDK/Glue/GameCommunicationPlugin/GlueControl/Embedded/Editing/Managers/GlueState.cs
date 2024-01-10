using System;
using System.Collections.Generic;
using System.Text;
using GlueControl.Models;
using GlueControl.Dtos;
using System.Threading.Tasks;
using System.Linq;

namespace GlueControl.Managers
{
    internal class GlueState : GlueCommandsStateBase
    {
        public GlueElement CurrentElement
        {
            get => Editing.EditingManager.Self.CurrentGlueElement;
        }

        public NamedObjectSave CurrentNamedObjectSave
        {
            get => Editing.EditingManager.Self.CurrentNamedObjects.FirstOrDefault();

        }

        public int? SelectedSubIndex { get; set; }


        public static GlueState Self { get; }

        static GlueState() => Self = new GlueState();

        public async Task SetCurrentNamedObjectSave(NamedObjectSave namedObjectSave, GlueElement owner)
        {
            await SendPropertySetToGame(
                nameof(CurrentNamedObjectSave),
                NamedObjectSaveReference.From(namedObjectSave, owner));
        }

        private async Task SendPropertySetToGame(string caller, object value)
        {
            var dto = new GlueStateDto();
            dto.SetPropertyName = caller;
            dto.Parameters.Add(value);

            var objectResponse = await GlueControlManager.Self.SendToGlue(dto);
            // Do we do anything with this?
            //return objectResponse;
        }

        private async Task<object> SendMethodCallToGame(string caller, params object[] parameters)
        {
            return base.SendMethodCallToGame(new GlueStateDto(), caller, parameters);
        }
    }
}
