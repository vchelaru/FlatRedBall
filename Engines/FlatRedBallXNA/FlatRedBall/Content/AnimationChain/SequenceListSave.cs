using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace FlatRedBall.Content.AnimationChain
{
    /// <summary>
    /// A <c>.seqj</c> file: named sequences, each playing animation chains from one or more .achx/.achj files on
    /// several sprites (tracks) at once. JSON only. The format is owned by the FlatRedBall2 AnimationEditor, which is
    /// also the writer; this class only reads. Properties this version does not know are ignored.
    /// </summary>
    public class SequenceListSave
    {
        /// <summary>The newest format version this code understands.</summary>
        public const int CurrentVersion = 1;

        /// <summary>The file's format version; 1 when the file has none.</summary>
        public int Version { get; set; } = CurrentVersion;

        /// <summary>True when the file declares a higher version than this code knows, so some of it may be ignored.</summary>
        public bool IsFromNewerVersion => Version > CurrentVersion;

        /// <summary>
        /// The .achx/.achj files the sequences use, relative to the .seqj with forward slashes. Shared by every sequence.
        /// </summary>
        public List<string> Sources { get; } = new List<string>();

        /// <summary>The sequences in the file, each played by name.</summary>
        public List<SequenceSave> Sequences { get; } = new List<SequenceSave>();

        /// <summary>Loads a .seqj from disk.</summary>
        /// <exception cref="JsonException">The file is not valid JSON, or a field has the wrong kind; the message names the property.</exception>
        public static SequenceListSave FromFile(string fileName)
        {
            using (Stream stream = File.OpenRead(fileName))
            {
                return Read(() => JsonNode.Parse(stream));
            }
        }

        /// <summary>Parses .seqj JSON. A leading byte-order mark is allowed.</summary>
        /// <exception cref="JsonException">The text is not valid JSON, or a field has the wrong kind; the message names the property.</exception>
        public static SequenceListSave FromJsonString(string json)
        {
            // JsonNode.Parse(string) rejects a byte-order mark, unlike the stream overload.
            return Read(() => JsonNode.Parse(json.TrimStart('﻿')));
        }

        // A repeated property name surfaces as ArgumentException when the parsed object is first read, so this wraps
        // the whole read.
        private static SequenceListSave Read(Func<JsonNode> parse)
        {
            try
            {
                if (!(parse() is JsonObject root))
                {
                    throw new JsonException("A .seqj must be a JSON object.");
                }

                SequenceListSave result = new SequenceListSave();

                if (root["version"] != null)
                {
                    result.Version = Int(root, "version", 1);
                    if (result.Version < 1)
                    {
                        throw new JsonException("The \"version\" property must be a whole number of at least 1.");
                    }
                }

                foreach (JsonNode source in Array(root, "sources"))
                {
                    if (!(source is JsonValue value) || !value.TryGetValue(out string text))
                    {
                        throw new JsonException("Every entry of the \"sources\" property must be a string.");
                    }
                    result.Sources.Add(text);
                }

                foreach (JsonObject sequenceObject in Objects(root, "sequences"))
                {
                    SequenceSave sequence = new SequenceSave
                    {
                        Name = String(sequenceObject, "name"),
                        Loop = Bool(sequenceObject, "loop", false),
                    };

                    foreach (JsonObject trackObject in Objects(sequenceObject, "tracks"))
                    {
                        SequenceTrackSave track = new SequenceTrackSave
                        {
                            Name = String(trackObject, "name"),
                            X = Float(trackObject, "x", 0),
                            Y = Float(trackObject, "y", 0),
                            FlipHorizontal = Bool(trackObject, "flipHorizontal", false),
                            Visible = Bool(trackObject, "visible", true),
                        };

                        foreach (JsonObject chainObject in Objects(trackObject, "chains"))
                        {
                            track.Chains.Add(new SequenceChainReferenceSave
                            {
                                Source = String(chainObject, "source"),
                                Chain = String(chainObject, "chain"),
                                Start = Double(chainObject, "start", 0),
                                Loops = Float(chainObject, "loops", 1),
                            });
                        }

                        sequence.Tracks.Add(track);
                    }

                    result.Sequences.Add(sequence);
                }

                return result;
            }
            catch (ArgumentException exception)
            {
                throw new JsonException("The file repeats a property name: " + exception.Message, exception);
            }
        }

        private static JsonArray Array(JsonObject parent, string name)
        {
            JsonNode node = parent[name];
            if (node == null)
            {
                return new JsonArray();
            }
            if (node is JsonArray array)
            {
                return array;
            }
            throw new JsonException("The \"" + name + "\" property must be an array.");
        }

        private static IEnumerable<JsonObject> Objects(JsonObject parent, string name)
        {
            foreach (JsonNode node in Array(parent, name))
            {
                if (!(node is JsonObject item))
                {
                    throw new JsonException("Every entry of the \"" + name + "\" property must be an object.");
                }
                yield return item;
            }
        }

        private static string String(JsonObject parent, string name)
        {
            JsonNode node = parent[name];
            if (node == null)
            {
                return string.Empty;
            }
            if (node is JsonValue value && value.TryGetValue(out string text))
            {
                return text;
            }
            throw new JsonException("The \"" + name + "\" property must be a string.");
        }

        private static bool Bool(JsonObject parent, string name, bool fallback)
        {
            JsonNode node = parent[name];
            if (node == null)
            {
                return fallback;
            }
            if (node is JsonValue value && value.TryGetValue(out bool result))
            {
                return result;
            }
            throw new JsonException("The \"" + name + "\" property must be true or false.");
        }

        private static int Int(JsonObject parent, string name, int fallback)
        {
            JsonNode node = parent[name];
            if (node == null)
            {
                return fallback;
            }
            if (node is JsonValue value && value.TryGetValue(out int result))
            {
                return result;
            }
            throw new JsonException("The \"" + name + "\" property must be a whole number.");
        }

        private static float Float(JsonObject parent, string name, float fallback)
        {
            JsonNode node = parent[name];
            if (node == null)
            {
                return fallback;
            }
            if (node is JsonValue value && value.TryGetValue(out float result))
            {
                return result;
            }
            throw new JsonException("The \"" + name + "\" property must be a number.");
        }

        private static double Double(JsonObject parent, string name, double fallback)
        {
            JsonNode node = parent[name];
            if (node == null)
            {
                return fallback;
            }
            if (node is JsonValue value && value.TryGetValue(out double result))
            {
                return result;
            }
            throw new JsonException("The \"" + name + "\" property must be a number.");
        }
    }

    /// <summary>A named timeline in a .seqj, played by name. It has no stored length: it ends when its last chain stops playing.</summary>
    public class SequenceSave
    {
        /// <summary>The name game code plays the sequence by.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Whether playback restarts at 0 once the sequence ends.</summary>
        public bool Loop { get; set; }

        /// <summary>The sprites, one per track.</summary>
        public List<SequenceTrackSave> Tracks { get; } = new List<SequenceTrackSave>();
    }

    /// <summary>
    /// One sprite's lane in a sequence. Its offset, flip and draw order hold for the whole sequence. Outside every chain
    /// on the track, the sprite is hidden.
    /// </summary>
    public class SequenceTrackSave
    {
        /// <summary>Display name of the sprite.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Offset from the entity origin.</summary>
        public float X { get; set; }

        /// <summary>Offset from the entity origin, Y+ up like a frame's own offsets.</summary>
        public float Y { get; set; }

        /// <summary>Mirrors the sprite and its frame offsets across the track's X.</summary>
        public bool FlipHorizontal { get; set; }

        /// <summary>Initial visibility of the track. A track that is not visible shows nothing.</summary>
        public bool Visible { get; set; } = true;

        /// <summary>Chain references in any order. Playback orders them by start; list order breaks ties.</summary>
        public List<SequenceChainReferenceSave> Chains { get; } = new List<SequenceChainReferenceSave>();
    }

    /// <summary>One placement of an animation chain on a track. The next chain on the track can cut it short.</summary>
    public class SequenceChainReferenceSave
    {
        /// <summary>The .achx/.achj holding the chain, as written in <see cref="SequenceListSave.Sources"/>.</summary>
        public string Source { get; set; } = string.Empty;

        /// <summary>The chain's name inside <see cref="Source"/>.</summary>
        public string Chain { get; set; } = string.Empty;

        /// <summary>Seconds from the sequence start.</summary>
        public double Start { get; set; }

        /// <summary>
        /// The block's length in playback cycles (a ping-pong cycle is twice the chain). A chain with Loop on repeats across
        /// the length; one with Loop off plays one cycle, then holds its last frame until the length ends.
        /// </summary>
        public float Loops { get; set; } = 1;
    }
}
