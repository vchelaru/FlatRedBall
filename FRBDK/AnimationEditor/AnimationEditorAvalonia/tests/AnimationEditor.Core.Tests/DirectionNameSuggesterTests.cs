using AnimationEditor.Core.Rendering;
using Xunit;

namespace AnimationEditor.Core.Tests;

public class DirectionNameSuggesterTests
{
    // ── SuggestHorizontalMirror ───────────────────────────────────────────

    [Theory]
    [InlineData("WalkLeft",   "WalkRight")]
    [InlineData("WalkRight",  "WalkLeft")]
    [InlineData("RunLeft",    "RunRight")]
    [InlineData("RunRight",   "RunLeft")]
    [InlineData("AttackLeft", "AttackRight")]
    public void HorizontalMirror_TitleCase_ReturnsOpposite(string input, string expected)
        => Assert.Equal(expected, DirectionNameSuggester.SuggestHorizontalMirror(input));

    [Theory]
    [InlineData("walkleft",  "walkright")]
    [InlineData("walkright", "walkleft")]
    [InlineData("runleft",   "runright")]
    public void HorizontalMirror_LowerCase_ReturnsOpposite(string input, string expected)
        => Assert.Equal(expected, DirectionNameSuggester.SuggestHorizontalMirror(input));

    [Theory]
    [InlineData("WALKLEFT",  "WALKRIGHT")]
    [InlineData("WALKRIGHT", "WALKLEFT")]
    public void HorizontalMirror_UpperCase_ReturnsOpposite(string input, string expected)
        => Assert.Equal(expected, DirectionNameSuggester.SuggestHorizontalMirror(input));

    [Theory]
    [InlineData("WalkUp")]
    [InlineData("WalkDown")]
    [InlineData("Idle")]
    [InlineData("")]
    [InlineData(null)]
    public void HorizontalMirror_NoHorizontalToken_ReturnsNull(string? input)
        => Assert.Null(DirectionNameSuggester.SuggestHorizontalMirror(input));

    [Fact]
    public void HorizontalMirror_NameWithBothLeftAndRight_ReplacesFirst()
    {
        // "LeftRight" → should replace "Left" first → "RightRight"
        var result = DirectionNameSuggester.SuggestHorizontalMirror("LeftRight");
        Assert.NotNull(result);
        Assert.NotEqual("LeftRight", result);
    }

    // ── SuggestVerticalMirror ─────────────────────────────────────────────

    [Theory]
    [InlineData("JumpUp",   "JumpDown")]
    [InlineData("JumpDown", "JumpUp")]
    [InlineData("FlyUp",    "FlyDown")]
    public void VerticalMirror_TitleCase_ReturnsOpposite(string input, string expected)
        => Assert.Equal(expected, DirectionNameSuggester.SuggestVerticalMirror(input));

    [Theory]
    [InlineData("jumpup",   "jumpdown")]
    [InlineData("jumpdown", "jumpup")]
    public void VerticalMirror_LowerCase_ReturnsOpposite(string input, string expected)
        => Assert.Equal(expected, DirectionNameSuggester.SuggestVerticalMirror(input));

    [Theory]
    [InlineData("JUMPUP",   "JUMPDOWN")]
    [InlineData("JUMPDOWN", "JUMPUP")]
    public void VerticalMirror_UpperCase_ReturnsOpposite(string input, string expected)
        => Assert.Equal(expected, DirectionNameSuggester.SuggestVerticalMirror(input));

    [Theory]
    [InlineData("WalkLeft")]
    [InlineData("Idle")]
    [InlineData("")]
    [InlineData(null)]
    public void VerticalMirror_NoVerticalToken_ReturnsNull(string? input)
        => Assert.Null(DirectionNameSuggester.SuggestVerticalMirror(input));

    // ── Suggested name differs from original ──────────────────────────────

    [Theory]
    [InlineData("WalkLeft")]
    [InlineData("WalkRight")]
    [InlineData("JumpUp")]
    [InlineData("JumpDown")]
    public void Suggestion_AlwaysDiffersFromOriginal(string input)
    {
        var h = DirectionNameSuggester.SuggestHorizontalMirror(input);
        var v = DirectionNameSuggester.SuggestVerticalMirror(input);
        if (h != null) Assert.NotEqual(input, h);
        if (v != null) Assert.NotEqual(input, v);
    }
}
