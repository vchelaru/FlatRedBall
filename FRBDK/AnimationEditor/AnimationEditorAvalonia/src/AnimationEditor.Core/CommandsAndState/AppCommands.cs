using AnimationEditor.Core.IO;
using AnimationEditor.Core.Rendering;
using FlatRedBall.Content.AnimationChain;
using FlatRedBall.Content.Math.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StringFunctions = FlatRedBall.Utilities.StringFunctions;

namespace AnimationEditor.Core.CommandsAndState
{
    /// <summary>
    /// Platform-independent command hub. UI-thread marshalling and dialog confirmations
    /// are handled by the registered delegates so this class stays free of Avalonia/WinForms.
    /// </summary>
    public class AppCommands : Singleton<AppCommands>
    {
        // Delegates wired up by the Avalonia app layer ----------------------------

        /// <summary>
        /// Run <paramref name="action"/> on the UI thread. Wired to
        /// <c>Dispatcher.UIThread.InvokeAsync</c> by the app layer.
        /// </summary>
        public Action<Action> DoOnUiThread { get; set; } = action => action();

        /// <summary>
        /// Show a yes/no confirmation dialog and return the user's answer.
        /// Wired by the app layer to an Avalonia MessageBox or equivalent.
        /// </summary>
        public Func<string, string, Task<bool>> ConfirmAsync { get; set; } =
            (message, title) => Task.FromResult(true);

        /// <summary>
        /// Request a full tree-view refresh. Raised instead of calling TreeViewManager directly.
        /// </summary>
        public event Action RefreshTreeViewRequested;

        /// <summary>
        /// Request a single chain's tree node to refresh.
        /// </summary>
        public event Action<AnimationChainSave> RefreshChainNodeRequested;

        /// <summary>
        /// Request a single frame's tree node to refresh.
        /// </summary>
        public event Action<AnimationFrameSave> RefreshFrameNodeRequested;

        /// <summary>
        /// Request the preview/animation-frame display to refresh.
        /// </summary>
        public event Action RefreshAnimationFrameDisplayRequested;

        /// <summary>
        /// Request the wireframe to refresh.
        /// </summary>
        public event Action RefreshWireframeRequested;

        /// <summary>
        /// File dialog abstraction. Wired to Avalonia's <c>StorageProvider</c> by the
        /// app layer. Defaults to <see cref="NullFileDialogService"/> (always cancels).
        /// </summary>
        public IFileDialogService FileDialogService { get; set; } = NullFileDialogService.Instance;

        /// <summary>
        /// Fired after <see cref="SaveCurrentAnimationChainListAsync"/> successfully saves a file.
        /// The argument is the full path of the saved file.
        /// </summary>
        public event Action<string>? SaveAsCompleted;

        // -------------------------------------------------------------------------

        public void LoadAnimationChain(string fileName)
        {
            var selectedTextureFilePath = string.Empty;
            ProjectManager.Self.LoadAnimationChain(fileName);
            RefreshTreeViewRequested?.Invoke();
            IoManager.Self.LoadAndApplyCompanionFileFor(fileName);
            RefreshWireframeRequested?.Invoke();
            RefreshAnimationFrameDisplayRequested?.Invoke();
        }

        public void RefreshTreeNode(AnimationChainSave animationChain) =>
            RefreshChainNodeRequested?.Invoke(animationChain);

        public void RefreshTreeNode(AnimationFrameSave animationFrame) =>
            RefreshFrameNodeRequested?.Invoke(animationFrame);

        public void RefreshAnimationFrameDisplay() =>
            RefreshAnimationFrameDisplayRequested?.Invoke();

        public void RefreshWireframe() =>
            RefreshWireframeRequested?.Invoke();

        public void SaveCurrentAnimationChainList(string fileName = null)
        {
            var target = fileName ?? ProjectManager.Self.FileName;
            if (!string.IsNullOrEmpty(target))
            {
                ProjectManager.Self.AnimationChainListSave?.Save(target);
            }
        }

        /// <summary>
        /// Show a Save-As file picker via <see cref="FileDialogService"/>, save the
        /// current animation chain list to the chosen path, update
        /// <see cref="ProjectManager.FileName"/>, and fire <see cref="SaveAsCompleted"/>.
        /// Does nothing if the user cancels the dialog.
        /// </summary>
        public async Task SaveCurrentAnimationChainListAsync()
        {
            var path = await FileDialogService.PickSaveFileAsync(
                "Save Animation Chain", "achx", "Animation Chain (*.achx)");

            if (string.IsNullOrEmpty(path)) return;

            SaveCurrentAnimationChainList(path);
            ProjectManager.Self.FileName = path;
            SaveAsCompleted?.Invoke(path);
        }

        public void DeleteAnimationChains(List<AnimationChainSave> animationChains)
        {
            var chainsCopy = animationChains.ToArray();
            foreach (var chain in chainsCopy)
            {
                ProjectManager.Self.AnimationChainListSave.AnimationChains.Remove(chain);
            }

            RefreshTreeViewRequested?.Invoke();
            RefreshAnimationFrameDisplayRequested?.Invoke();
            ApplicationEvents.Self.RaiseAnimationChainsChanged();
            RefreshWireframeRequested?.Invoke();
        }

        public void AddAxisAlignedRectangle(AnimationFrameSave frame)
        {
            var rectangleSave = new AxisAlignedRectangleSave
            {
                ScaleX = 8,
                ScaleY = 8,
                Name = StringFunctions.MakeStringUnique("AxisAlignedRectangleInstance",
                    GetSelectedFrameShapeNames())
            };

            MatchRectangleToFrame(rectangleSave, frame);
            frame.ShapeCollectionSave.AxisAlignedRectangleSaves.Add(rectangleSave);

            RefreshAnimationFrameDisplayRequested?.Invoke();
            RefreshFrameNodeRequested?.Invoke(frame);
            SelectedState.Self.SelectedRectangle = rectangleSave;
            SaveCurrentAnimationChainList();
        }

        public void AddCircle(AnimationFrameSave frame)
        {
            var circleSave = new CircleSave
            {
                Radius = 8,
                Name = StringFunctions.MakeStringUnique("CircleInstance",
                    GetSelectedFrameShapeNames())
            };

            MatchCircleToFrame(circleSave, frame);
            frame.ShapeCollectionSave.CircleSaves.Add(circleSave);

            RefreshAnimationFrameDisplayRequested?.Invoke();
            RefreshFrameNodeRequested?.Invoke(frame);
            SelectedState.Self.SelectedCircle = circleSave;
            SaveCurrentAnimationChainList();
        }

        public void MatchRectangleToFrame(AxisAlignedRectangleSave rectangle, AnimationFrameSave animationFrame)
        {
            // Texture width/height are not available at Core layer; the rendering layer should
            // override via AfterMatchRectangleToFrame if it wants pixel-accurate sizing.
            rectangle.X = animationFrame.RelativeX;
            rectangle.Y = animationFrame.RelativeY;
        }

        public void MatchCircleToFrame(CircleSave circle, AnimationFrameSave animationFrame)
        {
            circle.X = animationFrame.RelativeX;
            circle.Y = animationFrame.RelativeY;
        }

        public void DeleteCircle(CircleSave circle, AnimationFrameSave owner)
        {
            if (owner.ShapeCollectionSave.CircleSaves.Contains(circle))
            {
                owner.ShapeCollectionSave.CircleSaves.Remove(circle);
                RefreshFrameNodeRequested?.Invoke(owner);
                RefreshAnimationFrameDisplayRequested?.Invoke();
                ApplicationEvents.Self.RaiseAnimationChainsChanged();
            }
        }

        public void DeleteAxisAlignedRectangle(AxisAlignedRectangleSave rectangle, AnimationFrameSave owner)
        {
            if (owner.ShapeCollectionSave.AxisAlignedRectangleSaves.Contains(rectangle))
            {
                owner.ShapeCollectionSave.AxisAlignedRectangleSaves.Remove(rectangle);
                RefreshFrameNodeRequested?.Invoke(owner);
                RefreshAnimationFrameDisplayRequested?.Invoke();
                ApplicationEvents.Self.RaiseAnimationChainsChanged();
            }
        }

        public async Task AskToDeleteRectangles(List<AxisAlignedRectangleSave> rectangles)
        {
            var message = "Delete the following rectangle(s)?\n\n" +
                string.Join("\n", rectangles.Select(r => r.Name));

            if (await ConfirmAsync(message, "Delete?"))
            {
                foreach (var rectangle in rectangles.ToArray())
                {
                    var frame = ObjectFinder.Self.GetAnimationFrameContaining(rectangle);
                    if (frame != null) DeleteAxisAlignedRectangle(rectangle, frame);
                }
            }
        }

        public async Task AskToDeleteCircles(List<CircleSave> circles)
        {
            var message = "Delete the following circle(s)?\n\n" +
                string.Join("\n", circles.Select(c => c.Name));

            if (await ConfirmAsync(message, "Delete?"))
            {
                foreach (var circle in circles.ToArray())
                {
                    var frame = ObjectFinder.Self.GetAnimationFrameContaining(circle);
                    if (frame != null) DeleteCircle(circle, frame);
                }
            }
        }

        public async Task AskToDeleteAnimationChains(List<AnimationChainSave> animationChains)
        {
            var message = "Delete the following animation(s)?\n\n" +
                string.Join("\n", animationChains.Select(c => c.Name));

            if (await ConfirmAsync(message, "Delete?"))
            {
                DeleteAnimationChains(animationChains);
            }
        }

        public async Task AskToDeleteFrames(List<AnimationFrameSave> frames)
        {
            var message = $"Delete the following {frames.Count} frame(s)?\n\n" +
                string.Join("\n", frames.Select(f => $"Frame {f.TextureName}"));

            if (await ConfirmAsync(message, "Delete?"))
            {
                var framesToDelete = frames.ToArray();
                var chain = SelectedState.Self.SelectedChain;
                if (chain != null)
                {
                    foreach (var frame in framesToDelete)
                    {
                        chain.Frames.Remove(frame);
                    }
                }

                RefreshChainNodeRequested?.Invoke(chain);
                RefreshWireframeRequested?.Invoke();
                ApplicationEvents.Self.RaiseAnimationChainsChanged();
            }
        }

        private List<string> GetSelectedFrameShapeNames()
        {
            var frame = SelectedState.Self.SelectedFrame;
            if (frame?.ShapeCollectionSave == null) return new List<string>();

            return frame.ShapeCollectionSave.AxisAlignedRectangleSaves
                .Select(r => r.Name)
                .Concat(frame.ShapeCollectionSave.CircleSaves.Select(c => c.Name))
                .ToList();
        }

        // ── Chain / Frame operations ──────────────────────────────────────────

        public void AddAnimationChain()
        {
            var acls = ProjectManager.Self.AnimationChainListSave;
            if (acls is null) return;

            var newName = StringFunctions.MakeStringUnique(
                "NewAnimation",
                acls.AnimationChains.Select(c => c.Name).ToList());
            var chain = new AnimationChainSave { Name = newName };
            acls.AnimationChains.Add(chain);

            RefreshTreeViewRequested?.Invoke();
            SelectedState.Self.SelectedChain = chain;
            SaveCurrentAnimationChainList();
            ApplicationEvents.Self.RaiseAnimationChainsChanged();
        }

        /// <summary>
        /// Rename a chain.  Returns <c>false</c> (no-op) when another chain in the same ACLS
        /// already uses <paramref name="newName"/>; returns <c>true</c> on success.
        /// </summary>
        public bool RenameChain(AnimationChainSave chain, string newName)
        {
            if (chain.Name == newName) return true;

            var acls = ProjectManager.Self.AnimationChainListSave;
            if (acls is not null &&
                acls.AnimationChains.Any(c => !ReferenceEquals(c, chain) && c.Name == newName))
                return false;

            chain.Name = newName;
            RefreshChainNodeRequested?.Invoke(chain);
            SaveCurrentAnimationChainList();
            ApplicationEvents.Self.RaiseAnimationChainsChanged();
            return true;
        }

        /// <summary>
        /// Set the texture name (file path) of a frame — the in-tree rename action (TV08).
        /// Fires <see cref="ApplicationEvents.AnimationChainsChanged"/> and saves.
        /// </summary>
        public void RenameFrame(AnimationFrameSave frame, string newTextureName)
        {
            frame.TextureName = newTextureName;
            RefreshFrameNodeRequested?.Invoke(frame);
            SaveCurrentAnimationChainList();
            ApplicationEvents.Self.RaiseAnimationChainsChanged();
        }

        public void AddFrame(AnimationChainSave chain, string? textureName = null)
        {
            var frame = new AnimationFrameSave
            {
                TextureName  = textureName ?? string.Empty,
                LeftCoordinate   = 0f,
                RightCoordinate  = 1f,
                TopCoordinate    = 0f,
                BottomCoordinate = 1f,
                FrameLength      = 0.1f,
                ShapeCollectionSave = new FlatRedBall.Content.Math.Geometry.ShapeCollectionSave()
            };
            chain.Frames.Add(frame);
            RefreshChainNodeRequested?.Invoke(chain);
            SelectedState.Self.SelectedFrame = frame;
            SaveCurrentAnimationChainList();
            ApplicationEvents.Self.RaiseAnimationChainsChanged();
        }

        public void MoveChain(AnimationChainSave chain, int delta)
        {
            var chains = ProjectManager.Self.AnimationChainListSave?.AnimationChains;
            if (chains is null) return;
            int idx    = chains.IndexOf(chain);
            int newIdx = Math.Clamp(idx + delta, 0, chains.Count - 1);
            if (newIdx == idx) return;
            chains.RemoveAt(idx);
            chains.Insert(newIdx, chain);
            RefreshTreeViewRequested?.Invoke();
            SaveCurrentAnimationChainList();
            ApplicationEvents.Self.RaiseAnimationChainsChanged();
        }

        public void MoveChainToTop(AnimationChainSave chain)
        {
            var chains = ProjectManager.Self.AnimationChainListSave?.AnimationChains;
            if (chains is null) return;
            chains.Remove(chain);
            chains.Insert(0, chain);
            RefreshTreeViewRequested?.Invoke();
            SaveCurrentAnimationChainList();
            ApplicationEvents.Self.RaiseAnimationChainsChanged();
        }

        public void MoveChainToBottom(AnimationChainSave chain)
        {
            var chains = ProjectManager.Self.AnimationChainListSave?.AnimationChains;
            if (chains is null) return;
            chains.Remove(chain);
            chains.Add(chain);
            RefreshTreeViewRequested?.Invoke();
            SaveCurrentAnimationChainList();
            ApplicationEvents.Self.RaiseAnimationChainsChanged();
        }

        public void MoveFrame(AnimationFrameSave frame, AnimationChainSave chain, int delta)
        {
            int idx    = chain.Frames.IndexOf(frame);
            int newIdx = Math.Clamp(idx + delta, 0, chain.Frames.Count - 1);
            if (newIdx == idx) return;
            chain.Frames.RemoveAt(idx);
            chain.Frames.Insert(newIdx, frame);
            RefreshChainNodeRequested?.Invoke(chain);
            SaveCurrentAnimationChainList();
            ApplicationEvents.Self.RaiseAnimationChainsChanged();
        }

        public void MoveFrameToTop(AnimationFrameSave frame, AnimationChainSave chain)
        {
            chain.Frames.Remove(frame);
            chain.Frames.Insert(0, frame);
            RefreshChainNodeRequested?.Invoke(chain);
            SaveCurrentAnimationChainList();
            ApplicationEvents.Self.RaiseAnimationChainsChanged();
        }

        public void MoveFrameToBottom(AnimationFrameSave frame, AnimationChainSave chain)
        {
            chain.Frames.Remove(frame);
            chain.Frames.Add(frame);
            RefreshChainNodeRequested?.Invoke(chain);
            SaveCurrentAnimationChainList();
            ApplicationEvents.Self.RaiseAnimationChainsChanged();
        }

        public void FlipFrameHorizontally(AnimationFrameSave frame)
        {
            frame.FlipHorizontal = !frame.FlipHorizontal;
            SaveCurrentAnimationChainList();
            ApplicationEvents.Self.RaiseAnimationChainsChanged();
            RefreshWireframeRequested?.Invoke();
        }

        public void FlipFrameVertically(AnimationFrameSave frame)
        {
            frame.FlipVertical = !frame.FlipVertical;
            SaveCurrentAnimationChainList();
            ApplicationEvents.Self.RaiseAnimationChainsChanged();
            RefreshWireframeRequested?.Invoke();
        }

        public void FlipChainHorizontally(AnimationChainSave chain)
        {
            foreach (var frame in chain.Frames)
                frame.FlipHorizontal = !frame.FlipHorizontal;
            RefreshChainNodeRequested?.Invoke(chain);
            SaveCurrentAnimationChainList();
            ApplicationEvents.Self.RaiseAnimationChainsChanged();
            RefreshWireframeRequested?.Invoke();
        }

        public void FlipChainVertically(AnimationChainSave chain)
        {
            foreach (var frame in chain.Frames)
                frame.FlipVertical = !frame.FlipVertical;
            RefreshChainNodeRequested?.Invoke(chain);
            SaveCurrentAnimationChainList();
            ApplicationEvents.Self.RaiseAnimationChainsChanged();
            RefreshWireframeRequested?.Invoke();
        }

        public void InvertFrameOrder(AnimationChainSave chain)
        {
            chain.Frames.Reverse();
            RefreshChainNodeRequested?.Invoke(chain);
            SaveCurrentAnimationChainList();
            ApplicationEvents.Self.RaiseAnimationChainsChanged();
        }

        public void SetAllFrameLengths(AnimationChainSave chain, float frameLength)
        {
            foreach (var frame in chain.Frames)
                frame.FrameLength = frameLength;
            SaveCurrentAnimationChainList();
            ApplicationEvents.Self.RaiseAnimationChainsChanged();
        }

        public AnimationChainSave? DuplicateChain(
            AnimationChainSave source,
            bool   flipH    = false,
            bool   flipV    = false,
            string? newName = null)
        {
            var acls = ProjectManager.Self.AnimationChainListSave;
            if (acls is null) return null;

            var copyName = newName ?? StringFunctions.MakeStringUnique(
                source.Name + "Copy",
                acls.AnimationChains.Select(c => c.Name).ToList());

            var copy = new AnimationChainSave { Name = copyName };
            foreach (var frame in source.Frames)
            {
                var fCopy = new AnimationFrameSave
                {
                    TextureName      = frame.TextureName,
                    LeftCoordinate   = frame.LeftCoordinate,
                    RightCoordinate  = frame.RightCoordinate,
                    TopCoordinate    = frame.TopCoordinate,
                    BottomCoordinate = frame.BottomCoordinate,
                    FrameLength      = frame.FrameLength,
                    FlipHorizontal   = flipH ? !frame.FlipHorizontal : frame.FlipHorizontal,
                    FlipVertical     = flipV ? !frame.FlipVertical   : frame.FlipVertical,
                    RelativeX        = frame.RelativeX,
                    RelativeY        = frame.RelativeY,
                    ShapeCollectionSave = new FlatRedBall.Content.Math.Geometry.ShapeCollectionSave()
                };
                if (frame.ShapeCollectionSave != null)
                {
                    foreach (var r in frame.ShapeCollectionSave.AxisAlignedRectangleSaves)
                        fCopy.ShapeCollectionSave.AxisAlignedRectangleSaves.Add(
                            new AxisAlignedRectangleSave
                            { Name = r.Name, X = r.X, Y = r.Y, ScaleX = r.ScaleX, ScaleY = r.ScaleY });
                    foreach (var c in frame.ShapeCollectionSave.CircleSaves)
                        fCopy.ShapeCollectionSave.CircleSaves.Add(
                            new CircleSave
                            { Name = c.Name, X = c.X, Y = c.Y, Radius = c.Radius });
                }
                copy.Frames.Add(fCopy);
            }

            acls.AnimationChains.Add(copy);
            RefreshTreeViewRequested?.Invoke();
            SelectedState.Self.SelectedChain = copy;
            SaveCurrentAnimationChainList();
            ApplicationEvents.Self.RaiseAnimationChainsChanged();
            return copy;
        }

        public void SortAnimationsAlphabetically()
        {
            var acls = ProjectManager.Self.AnimationChainListSave;
            if (acls is null) return;
            var sorted = acls.AnimationChains.OrderBy(c => c.Name).ToList();
            acls.AnimationChains.Clear();
            foreach (var c in sorted)
                acls.AnimationChains.Add(c);
            RefreshTreeViewRequested?.Invoke();
            SaveCurrentAnimationChainList();
            ApplicationEvents.Self.RaiseAnimationChainsChanged();
        }

        // ── A16: Adjust Offsets ───────────────────────────────────────────────

        /// <summary>
        /// Justify (Bottom) — sets each frame's RelativeY so the bottom pixel edge
        /// aligns to y=0, divided by <paramref name="offsetMultiplier"/>.
        /// Requires the texture height for each frame; callers supply a resolver
        /// delegate so this method stays free of rendering code.
        /// </summary>
        /// <param name="chain">Chain whose frames will be adjusted.</param>
        /// <param name="getTextureHeight">
        ///     Returns the texture height in pixels for a given frame,
        ///     or <c>null</c> when no texture is loaded.
        /// </param>
        /// <param name="offsetMultiplier">Preview offset multiplier (PL12), default 1.0.</param>
        public void AdjustOffsetsJustifyBottom(
            AnimationChainSave chain,
            Func<AnimationFrameSave, float?> getTextureHeight,
            float offsetMultiplier = 1f)
        {
            foreach (var frame in chain.Frames)
            {
                var height = getTextureHeight(frame);
                if (height.HasValue)
                    AdjustOffsetCalculator.ApplyJustifyBottom([frame], height.Value, offsetMultiplier);
            }
            SaveCurrentAnimationChainList();
            ApplicationEvents.Self.RaiseAnimationChainsChanged();
            RefreshWireframeRequested?.Invoke();
        }

        /// <summary>
        /// Adjust All — shifts or overwrites RelativeX/Y of every frame in
        /// <paramref name="chain"/> by the given amounts.
        /// </summary>
        public void AdjustOffsetsAdjustAll(
            AnimationChainSave chain,
            float? deltaX,
            float? deltaY,
            bool relative)
        {
            AdjustOffsetCalculator.ApplyAdjustAll(chain.Frames, deltaX, deltaY, relative);
            SaveCurrentAnimationChainList();
            ApplicationEvents.Self.RaiseAnimationChainsChanged();
            RefreshWireframeRequested?.Invoke();
        }

        // ── A17: Scale Frame Times ────────────────────────────────────────────

        /// <summary>
        /// Scales all frame lengths so the total duration of <paramref name="chain"/>
        /// equals <paramref name="targetTotalDuration"/>, keeping ratios proportional.
        /// </summary>
        public void ScaleFrameTimesProportional(
            AnimationChainSave chain,
            float targetTotalDuration)
        {
            FrameTimeScaler.ApplyKeepProportional(chain.Frames, targetTotalDuration);
            SaveCurrentAnimationChainList();
            ApplicationEvents.Self.RaiseAnimationChainsChanged();
        }

        /// <summary>
        /// Sets every frame in <paramref name="chain"/> to the same duration:
        /// <paramref name="targetTotalDuration"/> / frame count.
        /// </summary>
        public void ScaleFrameTimesSetAllSame(
            AnimationChainSave chain,
            float targetTotalDuration)
        {
            FrameTimeScaler.ApplySetAllSame(chain.Frames, targetTotalDuration);
            SaveCurrentAnimationChainList();
            ApplicationEvents.Self.RaiseAnimationChainsChanged();
        }

        // ── F12: Add Multiple Frames ──────────────────────────────────────────

        /// <summary>
        /// Adds <paramref name="count"/> new frames to <paramref name="chain"/>,
        /// optionally auto-incrementing UV cells.
        /// Returns <c>true</c> if any frame's auto-incremented UV exceeded texture
        /// bounds (so the UI can warn the user).
        /// </summary>
        public bool AddMultipleFrames(
            AnimationChainSave chain,
            int count,
            bool incrementUV)
        {
            var lastFrame = chain.Frames.Count > 0 ? chain.Frames[^1] : null;
            var result    = BatchFrameBuilder.BuildBatch(lastFrame, count, incrementUV);

            foreach (var frame in result.Frames)
            {
                chain.Frames.Add(frame);
                SelectedState.Self.SelectedFrame = frame;
            }

            RefreshChainNodeRequested?.Invoke(chain);
            SaveCurrentAnimationChainList();
            ApplicationEvents.Self.RaiseAnimationChainsChanged();

            return result.ExceededTextureBounds;
        }

        // ── IO15: Adjust UV after texture resize ──────────────────────────────

        /// <summary>
        /// Adjusts UV coordinates of every frame in the current ACLS that references
        /// <paramref name="absoluteTextureFilePath"/> after it was resized from
        /// (<paramref name="oldWidth"/> × <paramref name="oldHeight"/>) to
        /// (<paramref name="newWidth"/> × <paramref name="newHeight"/>).
        /// Returns the list of frames that were modified.
        /// </summary>
        public List<AnimationFrameSave> AdjustUVAfterResize(
            string absoluteTextureFilePath,
            int oldWidth, int oldHeight,
            int newWidth, int newHeight)
        {
            var acls = ProjectManager.Self.AnimationChainListSave;
            if (acls is null) return [];

            var aclsDir = string.IsNullOrEmpty(ProjectManager.Self.FileName)
                ? string.Empty
                : System.IO.Path.GetDirectoryName(ProjectManager.Self.FileName) ?? string.Empty;

            var modified = TextureResizeAdjuster.AdjustAll(
                acls, aclsDir, absoluteTextureFilePath,
                oldWidth, oldHeight, newWidth, newHeight);

            if (modified.Count > 0)
            {
                SaveCurrentAnimationChainList();
                ApplicationEvents.Self.RaiseAnimationChainsChanged();
                RefreshWireframeRequested?.Invoke();
            }

            return modified;
        }

        // ── IO11: File > New ──────────────────────────────────────────────────

        /// <summary>
        /// Resets the editor to an empty, unsaved state:
        /// creates a fresh <see cref="AnimationChainListSave"/>, clears the file name,
        /// clears all selections, and notifies the UI.
        /// </summary>
        public void NewFile()
        {
            ProjectManager.Self.AnimationChainListSave = new AnimationChainListSave();
            ProjectManager.Self.FileName = string.Empty;
            SelectedState.Self.SelectedChain = null;
            SelectedState.Self.SelectedFrame = null;
            RefreshTreeViewRequested?.Invoke();
            ApplicationEvents.Self.RaiseAnimationChainsChanged();
        }

        // ── WF09: Create frame from magic-wand pixel bounds ───────────────────

        /// <summary>
        /// Creates a new <see cref="AnimationFrameSave"/> whose UV coordinates correspond
        /// to the pixel bounding box returned by the flood-fill (magic wand) tool, then
        /// appends it to <paramref name="chain"/> and selects it.
        /// </summary>
        /// <param name="chain">Chain to add the frame to.</param>
        /// <param name="textureName">Relative texture path (stored as-is in the frame).</param>
        /// <param name="minX">Left pixel bound of the region (inclusive).</param>
        /// <param name="minY">Top pixel bound of the region (inclusive).</param>
        /// <param name="maxX">Right pixel bound of the region (exclusive).</param>
        /// <param name="maxY">Bottom pixel bound of the region (exclusive).</param>
        /// <param name="bitmapWidth">Full width of the texture in pixels.</param>
        /// <param name="bitmapHeight">Full height of the texture in pixels.</param>
        public void AddFrameFromPixelBounds(
            AnimationChainSave chain,
            string textureName,
            int minX, int minY, int maxX, int maxY,
            int bitmapWidth, int bitmapHeight)
        {
            var frame = new AnimationFrameSave
            {
                TextureName         = textureName,
                LeftCoordinate      = minX / (float)bitmapWidth,
                RightCoordinate     = maxX / (float)bitmapWidth,
                TopCoordinate       = minY / (float)bitmapHeight,
                BottomCoordinate    = maxY / (float)bitmapHeight,
                FrameLength         = 0.1f,
                ShapeCollectionSave = new FlatRedBall.Content.Math.Geometry.ShapeCollectionSave()
            };

            chain.Frames.Add(frame);
            SelectedState.Self.SelectedFrame = frame;
            RefreshChainNodeRequested?.Invoke(chain);
            ApplicationEvents.Self.RaiseAnimationChainsChanged();
        }

        // ── Texture assignment (WF10b — write direction) ─────────────────────────

        /// <summary>
        /// Sets the texture name on <paramref name="frame"/> and fires change events so
        /// the tree view and preview panel refresh. This is the write-direction complement
        /// to <see cref="TextureListBuilder.GetAvailableTextures"/>.
        /// </summary>
        /// <param name="frame">The frame whose texture should change. No-ops when null.</param>
        /// <param name="textureName">Relative texture path to assign (may be null to clear).</param>
        public void SetFrameTextureName(AnimationFrameSave frame, string? textureName)
        {
            if (frame == null) return;
            frame.TextureName = textureName;
            RefreshFrameNodeRequested?.Invoke(frame);
            ApplicationEvents.Self.RaiseAnimationChainsChanged();
        }
    }
}
