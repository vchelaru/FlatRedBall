using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using GlueCsvEditor.Data;
using System.Reflection;
using System.Threading;
using FlatRedBall.Glue.Parsing;
using GlueCsvEditor.Settings;
using FormsTimer = System.Windows.Forms.Timer;
using GlueCsvEditor.Controllers;
using FlatRedBall.Glue.Plugins;
using GlueCsvEditor.Styling;
using FlatRedBall.IO.Csv;
using FlatRedBall.Glue.Plugins.ExportedImplementations;

namespace GlueCsvEditor.Controls
{
    public enum LayoutState
    {
        OnlyDataGrid,
        ShowPropertyGrid,
        ShowMultiLineText,
        ExpandedShowNothing
    }

    public partial class GridView : UserControl
    {
        #region Member Variables

        private readonly List<Keys> _downArrowKeys;
        private readonly CachedTypes _cachedTypes;
        private EditorLayoutSettings _editorLayoutSettings;
        private readonly object _filterKnownTypesLock = new object();
        private int _lastColumnIndex = -1;
        private int _currentColumnIndex;
        private int _currentRowIndex;
        private CsvData _data;

        private bool _stringColumnSelected;
        private LayoutState _currentLayoutStateButPleaseUseThePropertyInstead;
        private readonly FormsTimer _scrollTimer;

        private UndoController mUndoController;
        private SearchController mSearchController;
        private CopyPasteController mCopyPasteController;

        #endregion

        #region Properties

        LayoutState CurrentLayoutState
        {
            get
            {
                return _currentLayoutStateButPleaseUseThePropertyInstead;
            }
            set
            {
                var oldCurrentState =
                    _currentLayoutStateButPleaseUseThePropertyInstead;


                _currentLayoutStateButPleaseUseThePropertyInstead = value;

                if (_currentLayoutStateButPleaseUseThePropertyInstead == LayoutState.OnlyDataGrid)
                {
                    // Hide the top
                    LeftSideSplitContainer.Panel1MinSize = 0;
                    LeftSideSplitContainer.IsSplitterFixed = true;
                    // This seems to crash on some devices. Not sure why:
                    try
                    {
                        LeftSideSplitContainer.SplitterDistance = 0;
                    }
                    catch { }
                }
                else
                {
                    // Show the top
                    LeftSideSplitContainer.Panel1MinSize = 30;
                    LeftSideSplitContainer.IsSplitterFixed = false;
                    if (oldCurrentState == LayoutState.OnlyDataGrid)
                    {
                        try
                        {
                            LeftSideSplitContainer.SplitterDistance = _editorLayoutSettings.PropertyGridSplitterLocation;

                        }
                        catch { }
                    }
                }


                // Show the right controls
                RefreshAlternativeEditControlVisibility();

                // Refresh the display
                switch (_currentLayoutStateButPleaseUseThePropertyInstead)
                {
                    case LayoutState.ShowMultiLineText:
                        RefreshMultiLineDisplay();
                        break;
                    case LayoutState.ShowPropertyGrid:
                        break;

                }
            }
        }

        private int DataLoadingCount { get; set; }

        private bool DataLoading
        {
            get
            {
                return DataLoadingCount != 0;
            }
        }

        public bool IgnoreNextFileChange { get; set; }

        public bool IgnoreChangesToRightSideUi { get; set; }

        public CsvData CsvData
        {
            get
            {
                return _data;
            }
            set
            {
                _data = value;
                _editorLayoutSettings = SettingsManager.LoadEditorSettings(_data);

                ReloadCsvDisplay();

                mSearchController.CsvData = _data;
            }
        }

        #endregion

        #region Public Methods

        public GridView(CachedTypes cachedTypes, UndoController undoController)
        {
            mUndoController = undoController;
            DataLoadingCount = 0;
            _cachedTypes = cachedTypes;
            _downArrowKeys = new List<Keys>();

            InitializeComponent();
            _scrollTimer = new System.Windows.Forms.Timer {Interval = 50};
            _scrollTimer.Tick += ScrollTimer_Tick;

            mSearchController = new SearchController();
            mSearchController.Initialize(dgrEditor, txtSearch);

            mCopyPasteController = new CopyPasteController();
            mCopyPasteController.Initialize(dgrEditor);

            dgrEditor.DefaultCellStyle.SelectionBackColor = System.Drawing.Color.Blue;

            RefreshContextMenu(0, 0);
        }

        public void ReloadCsvDisplay()
        {
            IgnoreChangesToRightSideUi = true;
            // Clear the right editor side
            txtHeaderName.Text = string.Empty;
            txtHeaderType.Text = string.Empty;
            chkIsList.Checked = false;
            chkIsList.Checked = false;
            IgnoreChangesToRightSideUi = false;

            DataLoadingCount++;

            // Reset the current column count so we are sure the CellEnter event
            //  so we can guarantee that the cell displays are updated
            _currentColumnIndex = -1;
            _currentRowIndex = -1;
            
            SuspendLayout();

            var headers = RefreshDisplayTo(_data);

            // Auto-focus on the first cell
            if (headers.Count > 0 && dgrEditor.Rows.Count > 0)
            {
                txtHeaderName.Enabled = true;
                txtHeaderType.Enabled = true;
                chkIsList.Enabled = true;
                chkIsRequired.Enabled = true;

                dgrEditor.CurrentCell = dgrEditor[0, 0];
            }
            else
            {
                // Since we have no headers, disable all the right side controls
                txtHeaderName.Enabled = false;
                txtHeaderType.Enabled = false;
                chkIsList.Enabled = false;
                chkIsRequired.Enabled = false;
            }

            ApplyLayoutSettings();

            DataLoadingCount--;
        }

        private List<string> RefreshDisplayTo(CsvData dataToLoad)
        {
            // Add the CSV headers to the datagrid
            var headers = dataToLoad.GetHeaderText();

            dgrEditor.CurrentCell = null;
            while (dgrEditor.Columns.Count != 0)
            {
                dgrEditor.Columns.RemoveAt(dgrEditor.Columns.Count - 1);
            }

            foreach (var header in headers)
                dgrEditor.Columns.Add(header, header);

            // Add the records
            dgrEditor.RowCount = dataToLoad.GetRecordCount();

            RefreshRowHeaders();
            ResumeLayout();
            return headers;
        }

        public void CachedTypesReady()
        {
            // Make sure this call is done in the control's thread
            GlueCommands.Self.DoOnUiThread(() =>
            {
                cmbCelldata.Enabled = true;
                btnShowComplexProperties.Enabled = true;

                // Load all the known types
                //_knownTypes = _cachedTypes.KnownTypes;
                FilterKnownTypes();
                UpdateCellDisplays(true);
            });
        }

        public void SaveEditorSettings()
        {
            // Save column widths
            var widths = dgrEditor.Columns
                                  .Cast<DataGridViewColumn>()
                                  .Select(x => x.Width)
                                  .ToArray();

            if (_editorLayoutSettings == null)
            {
                _editorLayoutSettings = new EditorLayoutSettings();
            }

            _editorLayoutSettings.ColumnWidths = widths;
            _editorLayoutSettings.PropertyGridSplitterLocation = LeftSideSplitContainer.SplitterDistance;


            if (dgrEditor.CurrentCell != null)
            {

                // Save selected sell
                _editorLayoutSettings.LastSelectedColumnIndex = dgrEditor.CurrentCell.ColumnIndex;
                _editorLayoutSettings.LastSelectedRowIndex = dgrEditor.CurrentCell.RowIndex;
                _editorLayoutSettings.HeaderColumnWidth = dgrEditor.RowHeadersWidth;

            }
            SettingsManager.SaveEditorSettings(_data, _editorLayoutSettings);
        }

        public void UpdateCellDisplays(bool forceUpdate = false)
        {
            DataLoadingCount++;
            if (_currentColumnIndex != -1)
            {
                // Update the selected header information
                var header = _data.GetHeaderDetails(_currentColumnIndex);

                // Only update the header details if we changed columns
                if (_lastColumnIndex != _currentColumnIndex || forceUpdate)
                {
                    txtHeaderName.Text = header.Name;
                    txtHeaderType.Text = header.Type;
                    chkIsList.Checked = header.IsList;
                    chkIsRequired.Checked = header.IsRequired;
                    FilterKnownTypes();
                }

                // Setup the combobox
                var value = _data.GetValue(_currentRowIndex, _currentColumnIndex);
                cmbCelldata.Text = value;

                _stringColumnSelected = (header.Type == "string");

                bool isComplexType = IsComplexType(header.Type);

                btnShowComplexProperties.Enabled = isComplexType || _stringColumnSelected;

                if (!_stringColumnSelected &&
                    isComplexType == false)
                {
                    if (CurrentLayoutState != LayoutState.OnlyDataGrid)
                    {
                        CurrentLayoutState = LayoutState.ExpandedShowNothing;
                    }
                }
                else
                {
                    if (CurrentLayoutState != LayoutState.OnlyDataGrid)
                    {
                        if (_stringColumnSelected)
                        {
                            CurrentLayoutState = LayoutState.ShowMultiLineText;
                        }
                        else
                        {
                            CurrentLayoutState = LayoutState.ShowPropertyGrid;
                        }
                    }

                    if (!_stringColumnSelected)
                    {
                        UpdatePropertiesDisplay(header.Type, value);
                    }
                }
            }
            DataLoadingCount--;
        }

        public void RefreshCell(int row, int column)
        {
            bool oldIsRecording = mUndoController.IsRecordingUndos;
            mUndoController.IsRecordingUndos = false;

            dgrEditor[column, row].Value = (string)_data.GetValue(row, column);

            mUndoController.IsRecordingUndos = oldIsRecording;


        }

        #endregion

        #region Form Events

        private void GridView_Load(object sender, EventArgs e)
        {
            Dock = DockStyle.Fill;
            SetDoubleBuffered(true);

            // Disable any controls that rely on the cached types
            btnShowComplexProperties.Enabled = false;
            cmbCelldata.Enabled = false;

            CurrentLayoutState = LayoutState.OnlyDataGrid;
        }

        private void txtHeaderName_TextChanged(object sender, EventArgs e)
        {
            if (IgnoreChangesToRightSideUi)
            {
                return;
            }

            UpdateColumnDetails();
        }

        private void txtHeaderType_TextChanged(object sender, EventArgs e)
        {
            if (DataLoading || IgnoreChangesToRightSideUi)
                return;

            UpdateColumnDetails();
            SetupCellKnownValuesComboBox();
            FilterKnownTypes();
            UpdateCellDisplays();

        }

        private void chkIsRequired_CheckedChanged(object sender, EventArgs e)
        {
            if (IgnoreChangesToRightSideUi)
            {
                return;
            }

            UpdateColumnDetails();
        }

        private void chkIsList_CheckedChanged(object sender, EventArgs e)
        {
            if (IgnoreChangesToRightSideUi)
            {
                return;
            }

            UpdateColumnDetails();
        }

        private void dgrEditor_CellValuePushed(object sender, DataGridViewCellValueEventArgs e)
        {
            DataLoadingCount++;
            
            _data.TrySetValue(e.RowIndex, e.ColumnIndex, e.Value as string);

            cmbCelldata.Text = e.Value as string;
            SaveCsv();

            // If this was an update to a value in the first column, update the header cell text
            if (e.ColumnIndex == 0)
                dgrEditor.Rows[e.RowIndex].HeaderCell.Value = e.Value as string;

            // Since this is set in BeginEdit we need to end it here

            // Update the display
            UpdateCellDisplays(true);

            // This refreshes the row so it shows white/blue as appropriate
            RefreshRowHeaders();

            DataLoadingCount--;
        }

        private void dgrEditor_CellEnter(object sender, DataGridViewCellEventArgs e)
        {
            _lastColumnIndex = _currentColumnIndex;
            _currentColumnIndex = e.ColumnIndex;
            _currentRowIndex = e.RowIndex;

            // If no arrow keys are currently being pressed, update the cell displays
            if (_downArrowKeys.Count == 0)
                UpdateCellDisplays();
        }

        private void dgrEditor_CellBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
        {
        }

        private void dgrEditor_ColumnAdded(object sender, DataGridViewColumnEventArgs e)
        {
            if (DataLoading)
                return;

            _data.AddColumn(e.Column.Index);
            SaveCsv();

            // If the new column is the first column, blank out all the header cell values
            if (e.Column.Index == 0)
                foreach (DataGridViewRow row in dgrEditor.Rows)
                    row.HeaderCell.Value = string.Empty;

            // Update the header text for the new column
            // Update the column header
            var header = _data.GetHeaderText()[e.Column.Index];
            dgrEditor.Columns[e.Column.Index].HeaderText = header;
        }

        private void dgrEditor_ColumnRemoved(object sender, DataGridViewColumnEventArgs e)
        {
            if (DataLoading)
                return;

            _data.RemoveColumn(e.Column.Index);
            SaveCsv();

            // If the first column was removed, update the header cell values
            if (e.Column.Index == 0)
                if (_data.GetHeaderText().Count > 0 && dgrEditor.ColumnCount > 0)
                    for (int x = 0; x < _data.GetRecordCount(); x++)
                        dgrEditor.Rows[x].HeaderCell.Value = _data.GetValue(x, 0);
        }

        private void dgrEditor_CellValueNeeded(object sender, DataGridViewCellValueEventArgs e)
        {
            string value;
            _data.TryGetValue(e.RowIndex, e.ColumnIndex, out value);
            e.Value = value;
        }

        private void dgrEditor_KeyDown(object sender, KeyEventArgs e)
        {
            var arrowKeys = new[] { Keys.Down, Keys.Up, Keys.Left, Keys.Right };

            if (e.KeyCode == Keys.V && e.Control)
            {
                mCopyPasteController.HandlePaste(_currentColumnIndex);
                SaveCsv();
                e.Handled = true;
            }

            else if (e.KeyCode == Keys.X && e.Control)
            {
                mCopyPasteController.HandleCut();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.C && e.Control)
            {
                mCopyPasteController.HandleCopy();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.F3)
            {


                mSearchController.GoToNextSearchMatch(_currentRowIndex, _currentColumnIndex, e.Shift);

                e.Handled = true;
            }

            else if (e.KeyCode == Keys.Delete)
            {
                // Clear the value of all selected cells
                var cells = dgrEditor.SelectedCells;
                for (int x = 0; x < cells.Count; x++)
                {
                    cells[x].Value = string.Empty;
                }

                e.Handled = true;
            }
            else if(e.KeyCode == Keys.Enter)
            {
                if(e.Control && e.Shift)
                {
                    AddRowAtIndex(_currentRowIndex);
                }
                else if (e.Control)
                {
                    AddRowAtIndex(_currentRowIndex + 1);
                }
            }
            // If an arrow key is pushed, note it down
            else if (arrowKeys.Contains(e.KeyCode))
            {
                if (!_downArrowKeys.Contains(e.KeyCode))
                    _downArrowKeys.Add(e.KeyCode);
            }
        }

        private void dgrEditor_KeyUp(object sender, KeyEventArgs e)
        {
            var arrowKeys = new[] { Keys.Down, Keys.Up, Keys.Left, Keys.Right };
            if (arrowKeys.Contains(e.KeyCode))
            {
                if (_downArrowKeys.Contains(e.KeyCode))
                    _downArrowKeys.Remove(e.KeyCode);

                if (_downArrowKeys.Count == 0)
                    UpdateCellDisplays();
            }
        }

        private void dgrEditor_Leave(object sender, EventArgs e)
        {
            dgrEditor.DefaultCellStyle.SelectionBackColor = System.Drawing.Color.Orange;
            // Clear out all the known pressed keys
            _downArrowKeys.Clear();
        }

        private void AddRowAtIndex(int index)
        {
            if (DataLoading)
                return;

            _data.AddRow(index);
            SaveCsv();

            dgrEditor.RowCount = _data.GetRecordCount();
            RefreshRowHeaders();
            UpdateCellDisplays(true);
            dgrEditor.Invalidate();

        }

        private void btnAddRow_Click(object sender, EventArgs e)
        {
            AddRowAtIndex(_currentRowIndex + 1);
        }

        private void btnPrependRow_Click(object sender, EventArgs e)
        {
            AddRowAtIndex(_currentRowIndex);
        }

        private void btnDeleteRow_Click(object sender, EventArgs e)
        {
            var multipleRowsSelected = false;
            int? lastRow = null;
            for (int x = 0; x < dgrEditor.SelectedCells.Count; x++)
            {
                var curRowIndex = dgrEditor.SelectedCells[x].RowIndex;
                if (lastRow != null && lastRow != curRowIndex)
                {
                    multipleRowsSelected = true;
                    break;
                }

                lastRow = curRowIndex;
            }

            if (multipleRowsSelected)
                DeleteMultipleRows();
            else
                DeleteSingleRow();
        }

        private void btnAddColumn_Click(object sender, EventArgs e)
        {
            AddColumnAtIndex(_currentColumnIndex);
        }

        private void AddColumnAtIndex(int index)
        {
            if (DataLoading)
                return;

            _data.AddColumn(index);
            SaveCsv();

            ReloadCsvDisplay();
        }

        private void btnRemove_Click(object sender, EventArgs e)
        {
            int columnIndex = _currentColumnIndex;

            DeleteSingleColumn(columnIndex);
        }

        private void DeleteSingleColumn(int columnIndex)
        {
            string message = string.Format("Are you sure you want to remove the '{0}' column?", _data.GetHeaderText()[columnIndex]);
            var result = MessageBox.Show(message, "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation);
            if (result != DialogResult.Yes)
                return;

            // Delete the current column
            dgrEditor.Columns.RemoveAt(columnIndex);
        }

        private void txtSearch_TextChanged(object sender, EventArgs e)
        {
            


            if (string.IsNullOrWhiteSpace(txtSearch.Text))
                return;

            var cell = _data.FindNextValue(txtSearch.Text, _currentRowIndex, _currentColumnIndex);
            if (cell == null)
                return;

            dgrEditor.CurrentCell = dgrEditor[cell.ColumnIndex, cell.RowIndex];
        }

        private void txtSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Down)
            {
                mSearchController.GoToNextSearchMatch(_currentRowIndex, _currentColumnIndex);

                e.Handled = true;
            }

            else if (e.KeyCode == Keys.Up)
            {
                mSearchController.GoToNextSearchMatch(_currentRowIndex, _currentColumnIndex, true);

                e.Handled = true;
            }

            else if (e.KeyCode == Keys.F3)
            {
                mSearchController.GoToNextSearchMatch(_currentRowIndex, _currentColumnIndex, e.Shift);

                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Escape)
            {
                // Lose focus, go down to the spreadsheet portion
                dgrEditor.Focus();
                txtSearch.Text = null;
            }
        }

        private void btnFindNext_Click(object sender, EventArgs e)
        {
            mSearchController.GoToNextSearchMatch(_currentRowIndex, _currentColumnIndex);

        }

        private void cmbCelldata_TextChanged(object sender, EventArgs e)
        {
            if (DataLoading)
                return;

            _data.SetValue(_currentRowIndex, _currentColumnIndex, cmbCelldata.Text);
            SaveCsv();

            // Update the value in the datagrid
            dgrEditor.InvalidateCell(_currentColumnIndex, _currentRowIndex);
        }

        private void cmbCelldata_DropDown(object sender, EventArgs e)
        {
            SetupCellKnownValuesComboBox();
        }

        //private void lstFilteredTypes_MouseDoubleClick(object sender, MouseEventArgs e)
        //{
        //    if (lstFilteredTypes.SelectedIndex != -1)
        //    {
        //        var rect = lstFilteredTypes.GetItemRectangle(lstFilteredTypes.SelectedIndex);
        //        if (rect.Contains(e.Location))
        //        {
        //            txtHeaderType.Text = lstFilteredTypes.SelectedValue as string;
        //        }
        //    }
        //}

        private void btnShowComplexProperties_Click(object sender, EventArgs e)
        {

            if (CurrentLayoutState != LayoutState.OnlyDataGrid)
            {
                CurrentLayoutState = LayoutState.OnlyDataGrid;
            }
            else
            {
                // If we currently have a string column selected, don't show the property grid
                //   instead show the textbox for text editing
                if (_stringColumnSelected)
                {
                    CurrentLayoutState = LayoutState.ShowMultiLineText;
                }
                else
                {
                    CurrentLayoutState = LayoutState.ShowPropertyGrid;
                    
                }
            }
        }

        private void txtMultilineEditor_Leave(object sender, EventArgs e)
        {
            txtMultilineEditor.Visible = false;
        }

        private void txtMultilineEditor_TextChanged(object sender, EventArgs e)
        {
            if (DataLoading)
                return;

            _data.SetValue(_currentRowIndex, _currentColumnIndex, txtMultilineEditor.Text);
            SaveCsv();
        }

        private void cmbCelldata_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                if (_currentRowIndex < (dgrEditor.RowCount - 1))
                    dgrEditor.CurrentCell = dgrEditor[_currentColumnIndex, _currentRowIndex + 1];
            }
        }

        public void SelectCell(int row, int column, bool scrollTo = true)
        {
            try
            {
                dgrEditor.CurrentCell =
                    dgrEditor[column, row];

                if (scrollTo)
                {
                    dgrEditor.FirstDisplayedScrollingColumnIndex = column;
                }
            }
            catch (InvalidOperationException)
            {
                // Most likely this is caused by the window being too small and not enough room
                // to set the first displayed column.  Nothing to be done so just ignore the error
            }
        }

        private void ScrollTimer_Tick(object sender, EventArgs e)
        {
            _scrollTimer.Stop();
            if (_editorLayoutSettings.LastSelectedRowIndex != -1 &&
                _editorLayoutSettings.LastSelectedColumnIndex != -1)
            {
                SelectCell(_editorLayoutSettings.LastSelectedRowIndex, _editorLayoutSettings.LastSelectedColumnIndex);
            }
        }

        #endregion

        #region Internal Methods

        private bool IsComplexType(string typeAsString)
        {
            Type type = TypeManager.GetTypeFromString(typeAsString);

            if (type == null || type.IsPrimitive == false || type.IsEnum == false)
            {
                return true;
            }
            else
            {
                return false;
            }

        }

        private void SaveCsv()
        {
            IgnoreNextFileChange = true;
            
            var response =_data.SaveCsv();
            
            if(response.Succeeded)
            {
                SaveEditorSettings();
            }
            else
            {
                PluginManager.ReceiveOutput(response.Message);
                // Justin requested that 
                // we should show a MessageBox
                // so that the user knows they may
                // lose data.
                MessageBox.Show(response.Message);

            }
        }

        private void RefreshRowHeaders()
        {
            // Add the first value of each record to the row header text
            if (_data.GetHeaderText().Count > 0)
            {
                for (int x = 0; x < _data.GetRecordCount(); x++)
                {
                    var row = dgrEditor.Rows[x];

                    string value = _data.GetValue(x, 0);


                    row.HeaderCell.Value = value;

                    EditorObjects.IoC.Container.Get<ColoringLogic>().SetRowColor(row, value);

                }
            }
        }

        private void UpdateColumnDetails()
        {
            if (DataLoading)
                return;

            if (_currentColumnIndex != -1)
            {
                _data.SetHeader(_currentColumnIndex, txtHeaderName.Text, txtHeaderType.Text, chkIsRequired.Checked, chkIsList.Checked);
                SaveCsv();

                // Update the column header
                var header = _data.GetHeaderText()[_currentColumnIndex];
                dgrEditor.Columns[_currentColumnIndex].HeaderText = header;
            }
        }

        private void SetupCellKnownValuesComboBox()
        {
            cmbCelldata.Items.Clear();
            if (_currentColumnIndex != -1)
            {
                var knownValues = _data.GetKnownValues(_currentColumnIndex);
                foreach (string value in knownValues.Where(x => !string.IsNullOrWhiteSpace(x)))
                    cmbCelldata.Items.Add(value);
            }
        }

        private void FilterKnownTypes()
        {
            // This can't be in the ThreadPool because we can't access winforms stuff from other threads
            string trimmed = txtHeaderType.Text.Trim();
            ThreadPool.QueueUserWorkItem(stateObject =>
            {
                lock(_filterKnownTypesLock)
                {


                }
            });
        }

        bool IsExcluded(ComplexTypeProperty property)
        {
            return property.Attributes.Any(attribute =>
                        attribute.ToLowerInvariant().Contains("excludefromdesigner"));
        }

        private void UpdatePropertiesDisplay(string defaultRowType, string cellValue)
        {
            if (string.IsNullOrWhiteSpace(defaultRowType))
                return;

            ComplexCsvTypeDetails complexType = GetComplexType(defaultRowType, cellValue);


            // Get property information for the type

            var types = GetTypesIncludingInheritance(cellValue, complexType, defaultRowType);

            List<ComplexTypeProperty> complexTypeProperties = new List<ComplexTypeProperty>();

            foreach (var type in types)
            {
                complexTypeProperties.AddRange(_data.GetKnownProperties(type));
            }

            for(int i = complexTypeProperties.Count - 1; i > -1; i--)
            {
                var candidateForRemoval = complexTypeProperties[i];

                bool isExcluded = IsExcluded(candidateForRemoval);

                if(isExcluded)
                {
                    // skip it for now, we'll remove this later
                }
                else if(complexTypeProperties.Any(item=>item.Name == candidateForRemoval.Name && item != candidateForRemoval) )
                {
                    complexTypeProperties.RemoveAt(i);
                }
            }

            complexTypeProperties.RemoveAll(item => IsExcluded(item));


            btnShowComplexProperties.Visible = true;

            if (!complexTypeProperties.Any() && complexType == null)
            {
                btnShowComplexProperties.Visible = false;
                return;
            }


            // Go through all the properties and add any "known ones" that weren't part of the parsed set
            foreach (var prop in complexTypeProperties)
            {
                var tp = complexType.Properties.FirstOrDefault(x => x.Name.Equals(prop.Name, StringComparison.OrdinalIgnoreCase));

                if (tp == null)
                {
                    complexType.Properties.Add(new ComplexTypeProperty { Name = prop.Name, Type = prop.Type });
                }
                // Otherwise if the property exists then update the type
                else
                {
                    tp.Type = prop.Type;
                }
            }

            // Setup pgrid displayer
            // Note: While the displayer variable is never used, this cannot be deleted.
            //   The propertygrid property assignment triggers the property grid to show the complex properties
            //   correctly.  Without this code, the property grid will be empty
#pragma warning disable 168
            var displayer = new ComplexTypePropertyGridDisplayer(_data)
            {
                ComplexTypeUpdatedHandler = ComplexTypeUpdated,
                Instance = complexType,
                PropertyGrid = pgrPropertyEditor
            };
#pragma warning restore 168
        }

        private List<string> GetTypesIncludingInheritance(string cellValue, ComplexCsvTypeDetails complexType, string defaultType)
        {

            string mostDerivedType = defaultType;

            List<string> cellTypeIncludingInheritance = new List<string>();

            // todo - need to get the parsed class, consider inheritance, and recursively find the inheritance chain

            cellTypeIncludingInheritance.Add(defaultType);

            if (cellValue != null && cellValue.Contains("new"))
            {
                if (!string.IsNullOrEmpty(complexType?.Namespace))
                {
                    mostDerivedType = complexType.Namespace + "." + complexType.TypeName;
                }
                else
                {
                    mostDerivedType = complexType?.TypeName;
                }
            }

            FillTypeListRecursively(mostDerivedType, cellTypeIncludingInheritance);
            
            return cellTypeIncludingInheritance.Distinct().ToList();
        }

        private void FillTypeListRecursively(string className, List<string> cellTypeIncludingInheritance)
        {
            ParsedClass parsedClass = this._cachedTypes.ProjectClasses.FirstOrDefault(item=>item.Name == className);

            cellTypeIncludingInheritance.Add(className);

            if(parsedClass?.ParentParsedClasses != null)
            {
                foreach(var parentClass in parsedClass.ParentParsedClasses.Where(item=>item.IsInterface == false))
                {
                    FillTypeListRecursively(parentClass.Name, cellTypeIncludingInheritance);
                }
            }
        }

        private static ComplexCsvTypeDetails GetComplexType(string type, string value)
        {
            var complexType = ComplexCsvTypeDetails.ParseValue(value);

            var ns = string.Empty;
            if (type.Contains(".") && type.Last() != '.')
            {
                ns = type.Remove(type.LastIndexOf('.'));
                type = type.Substring(type.LastIndexOf('.') + 1);
            }
            // If the complex type coudln't be parsed from the current value, create one manually
            if (complexType == null)
            {
                complexType = new ComplexCsvTypeDetails { Namespace = ns, TypeName = type };
            }

            complexType.DefaultType = type;
            return complexType;
        }

        private void ComplexTypeUpdated(string complexTypeString)
        {
            dgrEditor[_currentColumnIndex, _currentRowIndex].Value = complexTypeString;
        }

        private void RefreshAlternativeEditControlVisibility()
        {
            switch (_currentLayoutStateButPleaseUseThePropertyInstead)
            {
                case LayoutState.ShowMultiLineText:
                    pgrPropertyEditor.Visible = false;

                    txtMultilineEditor.Visible = true;
                    break;
                case LayoutState.ShowPropertyGrid:
                    pgrPropertyEditor.Visible = true;
                    txtMultilineEditor.Visible = false;

                    break;
                case LayoutState.ExpandedShowNothing:
                    pgrPropertyEditor.Visible = false;
                    txtMultilineEditor.Visible = false;

                    break;
            }
        }

        private void RefreshMultiLineDisplay()
        {
            DataLoadingCount++;
            txtMultilineEditor.Text = _data.GetValue(_currentRowIndex, _currentColumnIndex);
            DataLoadingCount--;
        }

        private void SetDoubleBuffered(bool setting)
        {
            Type dgvType = dgrEditor.GetType();
            var pi = dgvType.GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic);
            pi.SetValue(dgrEditor, setting, null);
        }

        private void DeleteSingleRow()
        {
            int rowIndex = _currentRowIndex;

            DeleteSingleRow(rowIndex);
        }

        private void DeleteSingleRow(int rowIndex)
        {
            string message;
            // Find some identifying information for the row to present in
            //   a confirmation box
            var headers = Enumerable.Range(1, _data.GetHeaderText().Count).Select(x => _data.GetHeaderDetails(x - 1)).ToList();
            var values = Enumerable.Range(1, headers.Count).Select(x => _data.GetValue(rowIndex, x - 1)).ToList();

            // First check if all the values are empty
            if (values.All(string.IsNullOrWhiteSpace))
            {
                message = string.Format("Delete row #{0} (empty row)?", rowIndex);
            }
            else if (headers.Any(x => x.IsRequired))
            {
                var requiredHeader = headers.FirstOrDefault(x => x.IsRequired);
                message = string.Format("Delete row #{0} ({1})?", rowIndex, values[headers.IndexOf(requiredHeader)]);
            }
            else
            {
                // Get the first non-empty value
                var val = values.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));
                message = string.Format("Delete row #{0} ({1})?", rowIndex, val);
            }

            var result = MessageBox.Show(message, "Confirm Row Deletion", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation);
            if (result == DialogResult.Yes)
            {
                _data.RemoveRow(rowIndex);
                SaveCsv();

                dgrEditor.RowCount = _data.GetRecordCount();
                dgrEditor.Invalidate();
                RefreshRowHeaders();
            }
        }

        private void DeleteMultipleRows()
        {
            var message = new StringBuilder("Delete rows ");

            // Get all selected rows
            var rowIndexes = dgrEditor.SelectedCells
                                      .Cast<DataGridViewCell>()
                                      .Select(x => x.RowIndex)
                                      .Distinct()
                                      .OrderBy(x => x)
                                      .ToArray();

            // Check if the indexes are consecutive
            var isConsecutive = true;
            var lastIndex = (int?) null;
            foreach (var rowIndex in rowIndexes )
            {
                if (lastIndex != null && lastIndex + 1 != rowIndex)
                {
                    isConsecutive = false;
                    break;
                }

                lastIndex = rowIndex;
            }

            if (isConsecutive)
            {
                message.Append(rowIndexes[0])
                       .Append("-")
                       .Append(rowIndexes[rowIndexes.Length - 1] + 1);
            }
            else
            {
                for (int x = 0; x < rowIndexes.Length; x++)
                {
                    if (x > 0)
                        message.Append(", ");

                    if (x == rowIndexes.Length - 1)
                        message.Append("and ");

                    message.Append(rowIndexes[x] + 1);
                }
            }

            var result = MessageBox.Show(message.ToString(), "Confirm Row Deletion", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation);
            if (result == DialogResult.Yes)
            {
                // Required to prevent crashes if the user deletes the last rows
                //   If the selection is not cleared, CellEnter will be called
                //   On the incorrect row/column out of bounds of the CSV and crash
                dgrEditor.ClearSelection();
                dgrEditor.CurrentCell = null;

                // Delete the rows in descending order
                for (int x = rowIndexes.Length - 1; x >= 0; x-- )
                    _data.RemoveRow(rowIndexes[x]);

                SaveCsv();

                var recordCount = _data.GetRecordCount();
                if (_currentRowIndex >= recordCount)
                    _currentRowIndex = recordCount - 1;

                dgrEditor.RowCount = recordCount;
                dgrEditor.Invalidate();
                RefreshRowHeaders();

                dgrEditor.CurrentCell = dgrEditor[_currentColumnIndex, _currentRowIndex];
                UpdateCellDisplays();
            }
        }

        private void ApplyLayoutSettings()
        {
            if (_editorLayoutSettings == null)
                return;

            // Set the column widths
            if (_editorLayoutSettings.ColumnWidths != null)
            {
                for (int x = 0; x < dgrEditor.Columns.Count; x++)
                {
                    if (x >= _editorLayoutSettings.ColumnWidths.Length)
                        break;

                    dgrEditor.Columns[x].Width = _editorLayoutSettings.ColumnWidths[x];
                }
            }

            if (_editorLayoutSettings.HeaderColumnWidth > 0)
                dgrEditor.RowHeadersWidth = _editorLayoutSettings.HeaderColumnWidth;

            // Select the previously selected cell
            if (_editorLayoutSettings.LastSelectedColumnIndex >= dgrEditor.Columns.Count)
                _editorLayoutSettings.LastSelectedColumnIndex = dgrEditor.Columns.Count - 1;

            if (_editorLayoutSettings.LastSelectedRowIndex >= dgrEditor.Rows.Count)
                _editorLayoutSettings.LastSelectedRowIndex = dgrEditor.Rows.Count - 1;

            _scrollTimer.Start();
        }

        #endregion

        internal void FocusSearchTextBox()
        {
            this.txtSearch.Focus();
        }

        private void dgrEditor_Enter(object sender, EventArgs e)
        {
            dgrEditor.DefaultCellStyle.SelectionBackColor = System.Drawing.Color.Blue;

        }

        private void dgrEditor_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Right)
            {
                var result = dgrEditor.HitTest(e.X, e.Y);

                RefreshContextMenu(result.RowIndex, result.ColumnIndex);

                if (result.RowIndex == -1)
                {
                    // selecting the entire row
                }
                else if (result.ColumnIndex == -1)
                {
                    // selecting the entire column
                }
                else
                {
                    SelectCell(result.RowIndex, result.ColumnIndex, scrollTo: false);
                }
            }
        }

        private void RefreshContextMenu(int row, int column)
        {
            DataGridContextMenuStrip.Items.Clear();

            bool showAddColumn = column != -1;
            bool showAddRow = row != -1;

            var items = DataGridContextMenuStrip.Items;


            if (showAddRow)
            {
                ToolStripMenuItem rowAddItem = items.Add("Add Row") as ToolStripMenuItem;

                rowAddItem.DropDownItems.Add("Before (CTRL + SHIFT + Enter)", null, (a, b) =>
                    {
                        AddRowAtIndex(row);

                    });
                rowAddItem.DropDownItems.Add("After (CTRL + Enter)", null, (a, b) =>
                    {
                        AddRowAtIndex(row + 1);
                    });
            }

            if (showAddColumn)
            {
                ToolStripMenuItem columnAddItem = items.Add("Add Column") as ToolStripMenuItem;

                columnAddItem.DropDownItems.Add("Before", null, (a, b) => 
                    {
                        AddColumnAtIndex(column);
                    });
                columnAddItem.DropDownItems.Add("After", null, (a, b) =>
                    {
                        AddColumnAtIndex(column + 1);
                
                    });
            }

            items.Add("-");

            if (showAddRow)
            {
                items.Add("Delete Row", null, (a, b) =>
                    {
                        DeleteSingleRow(row);
                    });
            }
            if (showAddColumn)
            {
                items.Add("Delete Column", null, (a, b) =>
                    {
                        DeleteSingleColumn(column);
                    });
            }

            //DataGridContextMenuStrip.Items.Add("Hello", null, (a, b) => { });
        }
    }
}
