using System;
using System.Windows.Forms;
using GlueCsvEditor.Data;
using GlueCsvEditor.Controllers;

namespace GlueCsvEditor.Controls
{
    public partial class EditorMain : UserControl
    {
        private CsvData _csvData;
        private GridView _gridView;
        private CachedTypes _cachedTypes;

        UndoController mUndoController;

        public EditorMain()
        {
            InitializeComponent();

            _cachedTypes = new CachedTypes(CachedTypesReadyHandler);
            mUndoController = new UndoController();

            _gridView = new GridView(_cachedTypes, mUndoController);
            Controls.Add(_gridView);
        }

        public void LoadCsv(string csvPath, char delimiter)
        {
            _csvData = new CsvData(csvPath, _cachedTypes, delimiter);
            mUndoController.CsvData = _csvData;

            _gridView.CsvData = _csvData;
        }

        public void NotifyOfCsvUpdate()
        {
            if (_gridView.IgnoreNextFileChange)
            {
                _gridView.IgnoreNextFileChange = false;
            }
            else
            {
                // The change may have occurred because the file was removed. Don't want
                // to reload if it doesn't exist...
                if(System.IO.File.Exists( _csvData.CsvPath))
                {
                    ReloadCsv();
                }
            }
        }

        public void SaveEditorSettings()
        {
            _gridView.SaveEditorSettings();
        }

        private void EditorMain_Load(object sender, EventArgs e)
        {
            Dock = DockStyle.Fill;


            // Victor Chelaru November 22, 2012
            // I don't think we need this because
            // it causes a double-load of the CSV.
            // This makes the CSV plugin a little slower
            // and makes debugging more difficult.
            //ReloadCsv();
        }

        private void ReloadCsv()
        {
            lock (this)
            {
                _csvData.Reload();
                _gridView.ReloadCsvDisplay();
                
            }

        }

        private void CachedTypesReadyHandler()
        {
            // Don't let this crash Glue:
            if (_gridView != null)
            {
                _gridView.CachedTypesReady();
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            // ctrl z, ctrl + z, ctrl, z, ctrlz, undo
            switch (keyData)
            {
                case Keys.Control | Keys.Z:
                    int row;
                    int column;
                    mUndoController.PerformUndo(out row, out column);
                    if (row > -1 && column > -1)
                    {
                        this._gridView.RefreshCell(row, column);
                        this._gridView.UpdateCellDisplays(true);
                        _gridView.SelectCell(row, column);
                    }
                    return true;
                    //break;
                case Keys.Control | Keys.F:
                    _gridView.FocusSearchTextBox();

                    return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
    }
}
