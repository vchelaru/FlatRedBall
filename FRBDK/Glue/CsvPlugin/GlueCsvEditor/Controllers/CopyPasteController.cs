using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace GlueCsvEditor.Controllers
{
    public class CopyPasteController
    {
        DataGridView mDataGridView;


        List<CellValue> mCopiedCells = new List<CellValue>();


        public void Initialize(DataGridView dataGridView)
        {
            mDataGridView = dataGridView;
        }

        public void HandleCopy()
        {
            int minimumRowIndex = int.MaxValue;
            int minimumColumnIndex = int.MaxValue;

            mCopiedCells.Clear();

            foreach (DataGridViewCell cell in mDataGridView.SelectedCells)
            {
                CellValue cellValue = new CellValue();
                cellValue.Row = cell.RowIndex;
                cellValue.Column = cell.ColumnIndex;
                cellValue.Value = (string)cell.Value;

                mCopiedCells.Add(cellValue);

                minimumRowIndex = System.Math.Min(cell.RowIndex, minimumRowIndex);
                minimumColumnIndex = System.Math.Min(cell.ColumnIndex, minimumColumnIndex);
            }

            // Make all cell values be relative to the furthest top/left:
            for (int i = 0; i < mCopiedCells.Count; i++)
            {
                mCopiedCells[i].Row -= minimumRowIndex;
                mCopiedCells[i].Column -= minimumColumnIndex;
            }
        }

        public void HandleCut()
        {
            mCopiedCells.Clear();

            if (mDataGridView.CurrentCell != null)
            {

                // If this fails, we will just ignore
                bool succeeded = false;
                try
                {
                    Clipboard.SetDataObject(
                        mDataGridView.CurrentCell.Value.ToString(), //text to store in clipboard
                        true,        //do keep after our app exits
                        5,           //retry 5 times
                        200);        //200ms delay between retries
                    succeeded = true;
                }
                catch
                {
                    succeeded = false;
                }
                if (succeeded)
                {
                    mDataGridView.CurrentCell.Value = string.Empty;
                }
            }
        }

        public void HandlePaste(int columnIndex)
        {
            if (mCopiedCells.Count == 0)
            {

                var data = Clipboard.GetData(DataFormats.Text).ToString();
                var cells = data.Split('\t');
                if (mDataGridView.CurrentRow != null)
                    for (var i = 0; i < cells.Length; i++)
                        mDataGridView[columnIndex + i, mDataGridView.CurrentRow.Index].Value = cells[i];
            }
            else
            {
                int minimumRowIndex = int.MaxValue;
                int minimumColumnIndex = int.MaxValue;
                if (mDataGridView.SelectedCells.Count != 0)
                {
                    foreach (DataGridViewCell cell in mDataGridView.SelectedCells)
                    {
                        minimumRowIndex = System.Math.Min(cell.RowIndex, minimumRowIndex);
                        minimumColumnIndex = System.Math.Min(cell.ColumnIndex, minimumColumnIndex);
                    }

                    // start at the top-left, set the values

                    foreach (var cell in mCopiedCells)
                    {
                        int destinationRow = cell.Row + minimumRowIndex;
                        int destinationColumn = cell.Column + minimumColumnIndex;
                        string value = cell.Value;

                        if (destinationRow < mDataGridView.RowCount &&
                            destinationColumn < mDataGridView.ColumnCount)
                        {
                            mDataGridView[destinationColumn, destinationRow].Value = value;
                        }
                    }

                }
            }
        }
    }
}
