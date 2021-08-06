namespace GlueCsvEditor.Settings
{
    class EditorLayoutSettings
    {
        public int[] ColumnWidths { get; set; }
        public int HeaderColumnWidth { get; set; }
        public int LastSelectedRowIndex { get; set; }
        public int LastSelectedColumnIndex { get; set; }
        public int PropertyGridSplitterLocation { get; set; }

        public EditorLayoutSettings()
        {
            PropertyGridSplitterLocation = 100;
        }
    }
}
