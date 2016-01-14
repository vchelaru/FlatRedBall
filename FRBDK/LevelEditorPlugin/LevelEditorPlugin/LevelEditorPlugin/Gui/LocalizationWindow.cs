using FlatRedBall.Gui;
using FlatRedBall.Localization;

namespace LevelEditor.Gui
{
    public sealed class LocalizationWindow : Window
    {
        readonly ComboBox _currentLanguageComboBox;
        void OnLanguageSelection(Window callingWindow)
        {
            var index = (int)_currentLanguageComboBox.SelectedObject;

            LocalizationManager.CurrentLanguage = index;
        }

        public LocalizationWindow(Cursor cursor)
            : base(cursor)
        {
            ScaleX = 10;
            ScaleY = 2.0f;
            HasMoveBar = true;
            HasCloseButton = true;

            _currentLanguageComboBox = new ComboBox(cursor);
            AddWindow(_currentLanguageComboBox);
            _currentLanguageComboBox.ScaleX = 9.5f;

            _currentLanguageComboBox.ItemClick += OnLanguageSelection;
        }

        public void PopulateFromLocalizationManager()
        {
            _currentLanguageComboBox.Clear();
            for(int i = 0; i < LocalizationManager.Languages.Count; i++)
            {
                _currentLanguageComboBox.AddItem(LocalizationManager.Languages[i], i);
            }

            if (LocalizationManager.Languages.Count != 0)
            {
                _currentLanguageComboBox.Text = LocalizationManager.Languages[0];
            }
        }
    }
}
