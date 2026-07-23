namespace Npc;

/// <summary>
/// Abstracts user-facing messages raised during project creation so the core creation logic can run
/// headless (e.g. in tests). Production uses <see cref="WpfProjectCreationNotifier"/>.
/// </summary>
public interface IProjectCreationNotifier
{
    void ShowMessage(string message);
}

public class WpfProjectCreationNotifier : IProjectCreationNotifier
{
    public void ShowMessage(string message) => System.Windows.MessageBox.Show(message);
}
