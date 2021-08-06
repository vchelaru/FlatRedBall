using GlueCsvEditor.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GlueCsvEditor.Controllers
{

    public class CellValue
    {
        public int Row { get; set; }
        public int Column { get; set; }
        public string Value { get; set; }


        public virtual void ApplyTo(CsvData csvData)
        {
            if (Row < csvData.RowCount && Column < csvData.ColumnCount)
            {
                csvData.SetValue(Row, Column, Value);
            }
        }

        public override string ToString()
        {
            return string.Format("({0}, {1}) = {2}", Row, Column, Value);
        }

    }

    public class UndoController
    {
        #region Fields

        CsvData mCsvData;

        CellValue mValueBeforeChange;

        List<CellValue> mUndoStack = new List<CellValue>();

        /// <summary>
        /// Whether the UndoController will record values when the BeforeValueChange event is raised.
        /// </summary>
        bool mIsRecordingUndos = true;

        #endregion

        #region Properties

        public bool IsRecordingUndos
        {
            get { return mIsRecordingUndos; }
            set { mIsRecordingUndos = value; }
        }

        public CsvData CsvData
        {
            get
            {
                return mCsvData;
            }
            set
            {
                if (value != mCsvData)
                {
                    // Remove old events
                    if (mCsvData != null)
                    {
                        mCsvData.BeforeValueChange -= HandleBeforeValueChanged;
                        mCsvData.AfterValueChange -= HandleAfterValueChanged;
                    }
                    mCsvData = value;

                    // add new events
                    if (mCsvData != null)
                    {
                        mCsvData.BeforeValueChange += HandleBeforeValueChanged;
                        mCsvData.AfterValueChange += HandleAfterValueChanged;
                    }
                }
            }


        }

        #endregion

        public UndoController()
        {
        }

        public void PerformUndo(out int row, out int column)
        {
            row = -1;
            column = -1;
            if (mUndoStack.Count != 0)
            {
                mIsRecordingUndos = false;

                row = mUndoStack.Last().Row;
                column = mUndoStack.Last().Column;
                mUndoStack.Last().ApplyTo(mCsvData);
                mUndoStack.RemoveAt(mUndoStack.Count - 1);

                mIsRecordingUndos = true;
            }
        }

        private void HandleAfterValueChanged(int row, int column)
        {
            // I don't think we need to do anything here...
        }

        private void HandleBeforeValueChanged(int row, int column)
        {
            if (mIsRecordingUndos)
            {
                mValueBeforeChange = new CellValue();
                mValueBeforeChange.Row = row;
                mValueBeforeChange.Column = column;
                mValueBeforeChange.Value = mCsvData.GetValue(row, column);

                mUndoStack.Add(mValueBeforeChange);
            }
        }


        // todo more here

    }
}
