using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using ParticleEditorControls.PropertyGrids;

namespace ParticleEditorControls.Managers
{
    public class PropertyGridManager : Singleton<PropertyGridManager>
    {
        #region Fields

        PropertyGrid mPropertyGrid;
        EmitterSavePropertyGridDisplayer mDisplayer;

        PropertyGrid mEmissionSettingsPropertyGrid;
        EmissionSettingsSaveDisplayer mEmissionSettingsDisplayer;

        #endregion

        public event EventHandler PropertyValueChanged;

        public void Initialize(PropertyGrid mainPropertyGrid, PropertyGrid emissionSettingsPropertyGrid)
        {
            mPropertyGrid = mainPropertyGrid;
            mEmissionSettingsPropertyGrid = emissionSettingsPropertyGrid;

            mPropertyGrid.PropertyValueChanged += new PropertyValueChangedEventHandler(HandlePropertyValueChangedInternal);
            mEmissionSettingsPropertyGrid.PropertyValueChanged += new PropertyValueChangedEventHandler(HandlePropertyValueChangedInternal);

            mDisplayer = new EmitterSavePropertyGridDisplayer();
            mDisplayer.RefreshOnTimer = false;
            mEmissionSettingsDisplayer = new EmissionSettingsSaveDisplayer();
            mEmissionSettingsDisplayer.RefreshOnTimer = false;
        }

        void HandlePropertyValueChangedInternal(object s, PropertyValueChangedEventArgs e)
        {
            if (PropertyValueChanged != null)
            {
                PropertyValueChanged(this, null);
            }
        }

        public void RefreshAll()
        {
            var emitterSave = ApplicationState.Self.SelectedEmitterSave;

            mDisplayer.Instance = emitterSave;
            mDisplayer.PropertyGrid = mPropertyGrid;

            if(emitterSave == null)
            {
                mEmissionSettingsDisplayer.Instance = null;
            }
            else
            {
                mEmissionSettingsDisplayer.Instance = emitterSave.EmissionSettings;
            }

            mEmissionSettingsDisplayer.PropertyGrid = mEmissionSettingsPropertyGrid;

        }
    }
}
