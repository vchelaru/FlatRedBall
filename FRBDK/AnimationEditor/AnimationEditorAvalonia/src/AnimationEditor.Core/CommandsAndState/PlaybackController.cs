using FlatRedBall.Content.AnimationChain;
using System;

namespace AnimationEditor.Core.CommandsAndState;

/// <summary>
/// Pure playback state machine: tracks animation time, advances the current frame
/// index, and fires <see cref="FrameIndexChanged"/> when the frame changes.
/// <para>
/// Has no timer or threading dependency — callers tick it by calling
/// <see cref="Advance"/> from whatever timer they control. This makes the
/// controller fully unit-testable without a running event loop.
/// </para>
/// </summary>
public class PlaybackController
{
    private double _animTime;
    private int _currentFrameIndex;
    private AnimationChainSave? _chain;

    // ── Public state ──────────────────────────────────────────────────────────

    /// <summary>Current frame index into the active chain.</summary>
    public int CurrentFrameIndex => _currentFrameIndex;

    /// <summary>Accumulated playback time in seconds (wraps at chain total time).</summary>
    public double AnimTime => _animTime;

    /// <summary>
    /// When <c>false</c>, <see cref="Advance"/> is a no-op so the animation is paused.
    /// Defaults to <c>true</c>.
    /// </summary>
    public bool IsPlaying { get; private set; } = true;

    /// <summary>
    /// Multiplier applied to the delta inside <see cref="Advance"/>.
    /// 1.0 = normal speed, 2.0 = double, 0.5 = half.
    /// </summary>
    public double SpeedMultiplier { get; set; } = 1.0;

    // ── Events ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Fired whenever the current frame index changes.
    /// The argument is the new frame index.
    /// </summary>
    public event Action<int>? FrameIndexChanged;

    // ── Commands ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Set the animation chain to play. Resets time and frame index to zero.
    /// Pass <c>null</c> to stop playback.
    /// </summary>
    public void SetChain(AnimationChainSave? chain)
    {
        _chain = chain;
        Reset();
    }

    /// <summary>Resume playback (undoes <see cref="Pause"/>).</summary>
    public void Play() => IsPlaying = true;

    /// <summary>Pause playback at the current frame without resetting time or frame index.</summary>
    public void Pause() => IsPlaying = false;

    /// <summary>Reset time and frame index to zero without changing the active chain.</summary>
    public void Reset()
    {
        _animTime = 0;
        _currentFrameIndex = 0;
        FrameIndexChanged?.Invoke(0);
    }

    /// <summary>
    /// Advance the animation by <paramref name="deltaSeconds"/>.
    /// Call this from a timer tick; the method is a no-op when no chain is set
    /// or the chain has only one frame.
    /// </summary>
    public void Advance(double deltaSeconds)
    {
        if (!IsPlaying) return;

        var chain = _chain;
        if (chain is null || chain.Frames.Count <= 1) return;

        _animTime += deltaSeconds * SpeedMultiplier;

        double totalTime = 0;
        foreach (var f in chain.Frames)
            totalTime += f.FrameLength > 0 ? f.FrameLength : 0.1;
        if (totalTime <= 0) return;

        _animTime %= totalTime;

        double t = 0;
        int newIdx = chain.Frames.Count - 1;
        for (int i = 0; i < chain.Frames.Count; i++)
        {
            double fl = chain.Frames[i].FrameLength > 0 ? chain.Frames[i].FrameLength : 0.1;
            if (_animTime < t + fl) { newIdx = i; break; }
            t += fl;
        }

        if (newIdx != _currentFrameIndex)
        {
            _currentFrameIndex = newIdx;
            FrameIndexChanged?.Invoke(newIdx);
        }
    }
}
