using OfficialPlugins.TreeViewPlugin.Logic;
using Shouldly;

namespace GlueUnitTests.TreeViewPlugin;

public class SearchMatcherTests
{
    [Fact]
    public void NullOrEmptySearchTerm_ReturnsZero()
    {
        SearchMatcher.GetMatchWeight("Sprite", null).ShouldBe(0);
        SearchMatcher.GetMatchWeight("Sprite", "").ShouldBe(0);
    }

    [Fact]
    public void ExactCaseSensitiveMatch_ReturnsOne()
    {
        SearchMatcher.GetMatchWeight("Sprite", "Sprite").ShouldBe(1.0);
    }

    [Fact]
    public void CaseSensitivePrefixMatch_ReturnsPointEight()
    {
        SearchMatcher.GetMatchWeight("Sprite", "Spri").ShouldBe(0.8);
    }

    [Fact]
    public void ExactCaseInsensitiveMatch_ReturnsPointSeven()
    {
        // "sprite" != "Sprite" (case-sensitive) but matches case-insensitively
        SearchMatcher.GetMatchWeight("Sprite", "sprite").ShouldBe(0.7);
    }

    [Fact]
    public void CamelCaseAbbreviation_ReturnsPointSixFive()
    {
        // MGE matches Machine Gun Enemy via uppercase letters M, G, E
        SearchMatcher.GetMatchWeight("MachineGunEnemy", "MGE").ShouldBe(0.65);
        // Partial camel prefix also matches
        SearchMatcher.GetMatchWeight("MachineGunEnemy", "MG").ShouldBe(0.65);
    }

    [Fact]
    public void CaseInsensitivePrefixMatch_ReturnsPointSix()
    {
        // "spri" lowercased matches "sprite" lowercased prefix but not exact
        SearchMatcher.GetMatchWeight("Sprite", "spri").ShouldBe(0.6);
    }

    [Fact]
    public void CaseSensitiveContains_ReturnsPointFive()
    {
        // "rit" is contained in "Sprite" case-sensitively
        SearchMatcher.GetMatchWeight("Sprite", "rit").ShouldBe(0.5);
    }

    [Fact]
    public void CaseInsensitiveContains_ReturnsPointFour()
    {
        // "magetod" is not found literally but "agetod" is a subset of "DamageToDeal" lowercased
        SearchMatcher.GetMatchWeight("DamageToDeal", "agetod").ShouldBe(0.4);
    }

    [Fact]
    public void NoMatch_ReturnsZero()
    {
        SearchMatcher.GetMatchWeight("Sprite", "xyz").ShouldBe(0);
    }

    [Fact]
    public void DefinedByBase_SlightlyReducesWeight()
    {
        var normal = SearchMatcher.GetMatchWeight("Sprite", "Sprite", isDefinedByBase: false);
        var derived = SearchMatcher.GetMatchWeight("Sprite", "Sprite", isDefinedByBase: true);
        derived.ShouldBeLessThan(normal);
        // Reduction is small so derived still sorts close to its base definition
        (normal - derived).ShouldBeLessThan(0.01);
    }

    [Fact]
    public void ExactMatchRanksHigherThanPrefix()
    {
        var exact = SearchMatcher.GetMatchWeight("Sprite", "Sprite");
        var prefix = SearchMatcher.GetMatchWeight("SpriteList", "Sprite");
        exact.ShouldBeGreaterThan(prefix);
    }

    [Fact]
    public void CaseSensitivePrefixRanksHigherThanCaseInsensitivePrefix()
    {
        var caseSensitive = SearchMatcher.GetMatchWeight("Sprite", "Spri");   // 0.8
        var caseInsensitive = SearchMatcher.GetMatchWeight("Sprite", "spri"); // 0.6
        caseSensitive.ShouldBeGreaterThan(caseInsensitive);
    }

    [Fact]
    public void CamelCaseMatchIsCaseSensitiveForSearchTerm()
    {
        // "mge" (lowercase) should NOT match via camel case (camel case check is case-sensitive)
        var weight = SearchMatcher.GetMatchWeight("MachineGunEnemy", "mge");
        weight.ShouldBeLessThan(0.65);
    }
}
