namespace OfficialPlugins.TreeViewPlugin.Logic;

internal static class SearchMatcher
{
    /// <summary>
    /// Returns a match weight in [0, 1] where 0 means no match.
    /// Higher weights sort first in search results.
    /// </summary>
    public static double GetMatchWeight(string itemName, string? searchTerm, bool isDefinedByBase = false)
    {
        if (string.IsNullOrEmpty(searchTerm)) return 0;

        var searchToLower = searchTerm.ToLowerInvariant();
        var itemNameToLower = itemName.ToLowerInvariant();

        var weight = 0.0;
        if (itemName == searchTerm)
        {
            // Search: Sprite
            // Actual: Sprite
            weight = 1.0;
        }
        else if (itemName.StartsWith(searchTerm))
        {
            // Search: Spri
            // Actual: Sprite
            weight = 0.8;
        }
        else if (itemNameToLower == searchToLower)
        {
            // Search: sprite
            // Actual: Sprite
            weight = 0.7;
        }
        else if (CamelCaseMatchUpper(itemName, searchTerm))
        {
            // Search: MGE
            // Actual: MachineGunEnemy
            weight = 0.65;
        }
        else if (itemNameToLower.StartsWith(searchToLower))
        {
            // Search: spri
            // Actual: Sprite
            weight = 0.6;
        }
        else if (itemName.Contains(searchTerm))
        {
            // Search: rit
            // Actual: Sprite
            weight = 0.5;
        }
        else if (itemNameToLower.Contains(searchToLower))
        {
            // Search: magetod
            // Actual: DamageToDeal
            weight = 0.4;
        }

        // if defined by base, weigh slightly less so it sorts after the original definition
        if (isDefinedByBase)
        {
            weight -= 0.001;
        }

        return weight;
    }

    internal static bool CamelCaseMatchUpper(string itemName, string searchTerm)
    {
        string upperCaseLetters = string.Empty;
        for (int i = 0; i < itemName.Length; i++)
        {
            if (char.IsUpper(itemName[i]))
            {
                upperCaseLetters += itemName[i];
            }
        }
        return upperCaseLetters.StartsWith(searchTerm);
    }
}
