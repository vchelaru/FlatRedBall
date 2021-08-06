using GlueCsvEditor.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace GlueCsvEditor.Controllers
{
    public class SearchController
    {
        CsvData mCsvData;
        DataGridView mDataGridView;
        TextBox txtSearch;

        private const int EM_SETCUEBANNER = 0x1501;
        private const int EM_GETCUEBANNER = 0x1502;

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern Int32 SendMessage(IntPtr hWnd, int msg,
        int wParam, [MarshalAs(UnmanagedType.LPWStr)] string lParam);



        public CsvData CsvData
        {
            get { return mCsvData; }
            set { mCsvData = value; }
        }

        public void Initialize(DataGridView dataGridView, TextBox searchTextBox)
        {
            mDataGridView = dataGridView;
            txtSearch = searchTextBox;


            SetCueText(txtSearch, "Search... (CTRL + F)");
        }

        public void GoToNextSearchMatch(int currentRowIndex, int currentColumnIndex, bool reverse = false)
        {
            if (string.IsNullOrWhiteSpace(txtSearch.Text))
                return;

            var cell = mCsvData.FindNextValue(txtSearch.Text, currentRowIndex, currentColumnIndex, true, reverse);
            if (cell == null)
                return;

            mDataGridView.CurrentCell = mDataGridView[cell.ColumnIndex, cell.RowIndex];
        }

        static void SetCueText(Control control, string text)
        {
            SendMessage(control.Handle, EM_SETCUEBANNER, 0, text);
        }
    }
}
