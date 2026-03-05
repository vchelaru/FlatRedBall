using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Plugins.ExportedInterfaces;
using FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces;
using FlatRedBall.Glue.SaveClasses;
using Moq;
using OfficialPlugins.TreeViewPlugin.ViewModels;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;

namespace GlueUnitTests.TreeViewPlugin;

/// <summary>
/// Tests for CollectSearchResults in MainTreeViewViewModel.Search.cs.
/// [WpfFact] is required because NodeViewModel uses WPF types (BitmapImage, etc.)
/// in its constructors.
/// </summary>
public class SearchResultsTests
{
    readonly MainTreeViewViewModel _vm;

    public SearchResultsTests()
    {
        // NamedObjectsRootNodeViewModel.GetIcon accesses AvailableAssetTypes.CommonAtis.Layer,
        // which is null until the editor's full startup runs. Initialize it so tests don't throw.
        if (AvailableAssetTypes.CommonAtis == null)
        {
            typeof(AvailableAssetTypes)
                .GetProperty(nameof(AvailableAssetTypes.CommonAtis))!
                .SetValue(null, new CommonAtis(), BindingFlags.NonPublic | BindingFlags.Static, null, null, null);
        }

        _vm = new MainTreeViewViewModel(Mock.Of<IGlueState>(), Mock.Of<IGlueCommands>());
    }

    static EntitySave MakeEntity(string strippedName) =>
        new EntitySave { Name = $"Entities\\{strippedName}" };

    static ScreenSave MakeScreen(string strippedName) =>
        new ScreenSave { Name = $"Screens\\{strippedName}" };

    List<NodeViewModel> Search(
        IEnumerable<EntitySave>? entities = null,
        IEnumerable<ScreenSave>? screens = null,
        IEnumerable<ReferencedFileSave>? files = null,
        string? searchText = null,
        string prefixText = "") =>
        _vm.CollectSearchResults(
            entities ?? Enumerable.Empty<EntitySave>(),
            screens ?? Enumerable.Empty<ScreenSave>(),
            files ?? Enumerable.Empty<ReferencedFileSave>(),
            searchText,
            prefixText);

    // 1: entity name matching
    [WpfFact]
    public void EntityNameMatch_ReturnsEntityNode()
    {
        var entity = MakeEntity("Player");

        var results = Search(entities: new[] { entity }, searchText: "play", prefixText: "e");

        results.Count.ShouldBe(1);
        results[0].Tag.ShouldBeOfType<EntitySave>();
    }

    // 2: screen name matching
    [WpfFact]
    public void ScreenNameMatch_ReturnsScreenNode()
    {
        var screen = MakeScreen("GameScreen");

        var results = Search(screens: new[] { screen }, searchText: "Game", prefixText: "s");

        results.Count.ShouldBe(1);
        results[0].Tag.ShouldBeOfType<ScreenSave>();
    }

    // 3: no match returns empty
    [WpfFact]
    public void NoMatch_ReturnsEmptyList()
    {
        var entity = MakeEntity("Player");
        var screen = MakeScreen("GameScreen");

        var results = Search(
            entities: new[] { entity },
            screens: new[] { screen },
            searchText: "zzzzz",
            prefixText: "");

        results.ShouldBeEmpty();
    }

    // 4: prefix "e" filters to entities only
    [WpfFact]
    public void PrefixE_FiltersToEntitiesOnly()
    {
        var entity = MakeEntity("PlayerEntity");
        var screen = MakeScreen("PlayerScreen");

        var results = Search(
            entities: new[] { entity },
            screens: new[] { screen },
            searchText: "player",
            prefixText: "e");

        results.ShouldNotBeEmpty();
        results.ShouldAllBe(n => n.Tag is EntitySave);
        results.Any(n => n.Tag is ScreenSave).ShouldBeFalse();
    }

    // 5: prefix "s" filters to screens only
    [WpfFact]
    public void PrefixS_FiltersToScreensOnly()
    {
        var entity = MakeEntity("PlayerEntity");
        var screen = MakeScreen("PlayerScreen");

        var results = Search(
            entities: new[] { entity },
            screens: new[] { screen },
            searchText: "player",
            prefixText: "s");

        results.ShouldNotBeEmpty();
        results.ShouldAllBe(n => n.Tag is ScreenSave);
        results.Any(n => n.Tag is EntitySave).ShouldBeFalse();
    }

    // 6: prefix "o" returns named objects
    [WpfFact]
    public void PrefixO_ReturnsNamedObjects()
    {
        var nos = new NamedObjectSave { InstanceName = "SpriteInstance" };
        var entity = MakeEntity("Player");
        entity.NamedObjects.Add(nos);

        var results = Search(
            entities: new[] { entity },
            searchText: "sprite",
            prefixText: "o");

        results.ShouldNotBeEmpty();
        results.ShouldAllBe(n => n.Tag is NamedObjectSave);
    }

    // 7: prefix "v" returns custom variables
    [WpfFact]
    public void PrefixV_ReturnsVariables()
    {
        var variable = new CustomVariable { Name = "Speed" };
        var entity = MakeEntity("Player");
        entity.CustomVariables.Add(variable);

        var results = Search(
            entities: new[] { entity },
            searchText: "speed",
            prefixText: "v");

        results.ShouldNotBeEmpty();
        results.ShouldAllBe(n => n.Tag is CustomVariable);
    }

    // 8: no prefix includes named objects from entities
    [WpfFact]
    public void NoPrefixIncludesNamedObjects()
    {
        var nos = new NamedObjectSave { InstanceName = "SpriteInstance" };
        var entity = MakeEntity("Player");
        entity.NamedObjects.Add(nos);

        // search for "sprite" with no prefix — should find NOS, not the entity itself
        var results = Search(
            entities: new[] { entity },
            searchText: "sprite",
            prefixText: "");

        results.Any(n => n.Tag is NamedObjectSave).ShouldBeTrue();
    }

    // 9: results sorted by weight descending (exact match before prefix match)
    [WpfFact]
    public void ResultsSortedByWeightDescending()
    {
        // "Player" is an exact match for "Player"; "PlayerEnemy" is a prefix match
        var exact = MakeEntity("Player");
        var prefix = MakeEntity("PlayerEnemy");

        var results = Search(
            entities: new[] { prefix, exact }, // pass prefix first to ensure sort is applied
            searchText: "Player",
            prefixText: "e");

        results.Count.ShouldBe(2);
        // Exact match (higher weight) should come first
        results[0].Tag.ShouldBeSameAs(exact);
        results[1].Tag.ShouldBeSameAs(prefix);
    }

    // 10: same weight sorted alphabetically
    [WpfFact]
    public void SameWeightSortedAlphabetically()
    {
        // Both are case-insensitive prefix matches for "player" — same weight
        var zebra = MakeEntity("PlayerZebra");
        var alpha = MakeEntity("PlayerAlpha");

        var results = Search(
            entities: new[] { zebra, alpha },
            searchText: "player",
            prefixText: "e");

        results.Count.ShouldBe(2);
        // Alpha comes before Zebra alphabetically
        results[0].Tag.ShouldBeSameAs(alpha);
        results[1].Tag.ShouldBeSameAs(zebra);
    }

    // 11: empty search returns empty list
    [WpfFact]
    public void EmptySearch_ReturnsEmptyList()
    {
        var entity = MakeEntity("Player");

        var results = Search(
            entities: new[] { entity },
            searchText: "",
            prefixText: "");

        results.ShouldBeEmpty();
    }

    // 12: DefinedByBase NOS ranks below non-derived NOS
    [WpfFact]
    public void DefinedByBase_NosHasLowerWeight()
    {
        var baseNos = new NamedObjectSave { InstanceName = "Sprite", DefinedByBase = true };
        var ownNos = new NamedObjectSave { InstanceName = "Sprite", DefinedByBase = false };

        var entity = MakeEntity("Player");
        entity.NamedObjects.Add(baseNos);
        entity.NamedObjects.Add(ownNos);

        var results = Search(
            entities: new[] { entity },
            searchText: "Sprite",
            prefixText: "o");

        results.Count.ShouldBe(2);
        // Non-derived (higher weight) should come first
        var first = (NamedObjectSave)results[0].Tag!;
        var second = (NamedObjectSave)results[1].Tag!;
        first.DefinedByBase.ShouldBeFalse();
        second.DefinedByBase.ShouldBeTrue();
    }

    // 13–17: typing "f ", "e ", "s ", "o ", "v " (prefix only, no search term) should
    // return all items of that type. Currently broken because GetMatchWeight returns 0
    // for an empty search term even when a prefix filter is active.

    [WpfFact]
    public void PrefixF_EmptySearch_ReturnsAllFiles()
    {
        var rfs = new ReferencedFileSave { Name = "Content/MyFile.png" };

        var results = Search(files: new[] { rfs }, searchText: "", prefixText: "f");

        results.ShouldNotBeEmpty();
        results.ShouldAllBe(n => n.Tag is ReferencedFileSave);
    }

    [WpfFact]
    public void PrefixE_EmptySearch_ReturnsAllEntities()
    {
        var entity = MakeEntity("Player");

        var results = Search(entities: new[] { entity }, searchText: "", prefixText: "e");

        results.ShouldNotBeEmpty();
        results.ShouldAllBe(n => n.Tag is EntitySave);
    }

    [WpfFact]
    public void PrefixS_EmptySearch_ReturnsAllScreens()
    {
        var screen = MakeScreen("GameScreen");

        var results = Search(screens: new[] { screen }, searchText: "", prefixText: "s");

        results.ShouldNotBeEmpty();
        results.ShouldAllBe(n => n.Tag is ScreenSave);
    }

    [WpfFact]
    public void PrefixO_EmptySearch_ReturnsAllNamedObjects()
    {
        var nos = new NamedObjectSave { InstanceName = "SpriteInstance" };
        var entity = MakeEntity("Player");
        entity.NamedObjects.Add(nos);

        var results = Search(entities: new[] { entity }, searchText: "", prefixText: "o");

        results.ShouldNotBeEmpty();
        results.ShouldAllBe(n => n.Tag is NamedObjectSave);
    }

    [WpfFact]
    public void PrefixV_EmptySearch_ReturnsAllVariables()
    {
        var variable = new CustomVariable { Name = "Speed" };
        var entity = MakeEntity("Player");
        entity.CustomVariables.Add(variable);

        var results = Search(entities: new[] { entity }, searchText: "", prefixText: "v");

        results.ShouldNotBeEmpty();
        results.ShouldAllBe(n => n.Tag is CustomVariable);
    }
}
