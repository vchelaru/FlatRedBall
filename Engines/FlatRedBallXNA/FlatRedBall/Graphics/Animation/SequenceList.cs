using System;
using System.Collections.Generic;
using FlatRedBall.Content.AnimationChain;
using FlatRedBall.IO;

namespace FlatRedBall.Graphics.Animation
{
    /// <summary>
    /// A loaded .seqj: the file's data plus the animation chains its sources resolve to. This is the runtime form of a
    /// <see cref="SequenceListSave"/>, like <see cref="AnimationChainList"/> is for an .achx. It belongs to no entity, so
    /// load it once and give it to a <see cref="SequencePlayer"/> on each entity that plays it.
    /// </summary>
    public class SequenceList
    {
        private readonly Func<string, AnimationChainList> mSourceResolver;
        private readonly Dictionary<string, AnimationChainList> mSources =
            new Dictionary<string, AnimationChainList>(StringComparer.OrdinalIgnoreCase);

        /// <summary>The file's data.</summary>
        public SequenceListSave Save { get; }

        /// <param name="save">The parsed .seqj.</param>
        /// <param name="sourceResolver">Returns the chains of a source as written in the .seqj's source list, or null when
        /// it can't be loaded. Called once per source.</param>
        public SequenceList(SequenceListSave save, Func<string, AnimationChainList> sourceResolver)
        {
            Save = save ?? throw new ArgumentNullException(nameof(save));
            mSourceResolver = sourceResolver ?? throw new ArgumentNullException(nameof(sourceResolver));
        }

        /// <summary>
        /// Loads a .seqj from disk. Each source is loaded, on first use, from the .achx/.achj next to it (paths in the file
        /// are relative to the .seqj); a source that can't be loaded is treated as missing.
        /// </summary>
        public static SequenceList FromFile(string seqjFileName, string contentManagerName)
        {
            string absolute = FileManager.IsRelative(seqjFileName) ? FileManager.MakeAbsolute(seqjFileName) : seqjFileName;
            string directory = FileManager.GetDirectory(absolute);

            return new SequenceList(SequenceListSave.FromFile(absolute), source =>
                AnimationChainListSave.FromFile(directory + source)
                    .ToAnimationChainList(contentManagerName, throwError: false));
        }

        /// <summary>The sequence with this name, or null.</summary>
        public SequenceSave Find(string sequenceName)
        {
            return Save.Sequences.Find(item => item.Name == sequenceName);
        }

        /// <summary>The chains of this source, or null when it is missing or could not be loaded.</summary>
        public AnimationChainList GetSource(string source)
        {
            if (!mSources.TryGetValue(source, out AnimationChainList list))
            {
                try
                {
                    list = mSourceResolver(source);
                }
                catch (Exception)
                {
                    // A source that can't be loaded is a missing source, not a crash.
                    list = null;
                }
                mSources[source] = list;
            }
            return list;
        }

        /// <summary>The chain a reference points at, or null when its source or chain is missing.</summary>
        public AnimationChain FindChain(SequenceChainReferenceSave reference)
        {
            AnimationChainList list = GetSource(reference.Source);
            if (list == null)
            {
                return null;
            }
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].Name == reference.Chain)
                {
                    return list[i];
                }
            }
            return null;
        }
    }
}
