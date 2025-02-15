using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall.Localization;
using MonoGameGum.Localization;

namespace GumCoreShared.FlatRedBall.Embedded;

public class LocalizationManagerWrapper : ILocalizationService
{
    public string Translate(string stringId)
    {
        return LocalizationManager.Translate(stringId);
    }


}
