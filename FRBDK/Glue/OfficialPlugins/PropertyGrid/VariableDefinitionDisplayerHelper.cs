using FlatRedBall.Glue.Elements;
using System;
using System.Collections.Generic;
using WpfDataUi.Controls;

namespace OfficialPlugins.PropertyGrid
{
    internal static class VariableDefinitionDisplayerHelper
    {
        /// <summary>
        /// Forwards a VariableDefinition's MinValue/MaxValue onto the property grid's numeric textbox
        /// when only one bound is set (e.g. a floor with no ceiling). Both-bounds-set is handled
        /// separately by forcing a SliderDisplay, so this only fires for the asymmetric case, and only
        /// when nothing earlier already picked a non-default displayer.
        /// </summary>
        internal static void ApplyAsymmetricMinMax(VariableDefinition variableDefinition, Type currentPreferredDisplayer, Dictionary<string, object> propertiesToSetOnDisplayer)
        {
            if (currentPreferredDisplayer != null)
            {
                return;
            }

            if (variableDefinition?.MinValue == null && variableDefinition?.MaxValue == null)
            {
                return;
            }

            if (variableDefinition.MinValue != null)
            {
                propertiesToSetOnDisplayer[nameof(TextBoxDisplay.MinValue)] = variableDefinition.MinValue.Value;
            }
            if (variableDefinition.MaxValue != null)
            {
                propertiesToSetOnDisplayer[nameof(TextBoxDisplay.MaxValue)] = variableDefinition.MaxValue.Value;
            }
        }
    }
}
