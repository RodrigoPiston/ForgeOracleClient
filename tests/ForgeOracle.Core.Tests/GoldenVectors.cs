using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ForgeOracle.Core.Tests
{
    /// <summary>
    /// Vectors produced by tools/golden/generate-vectors.mjs from the Forge Oracle reference implementation.
    /// </summary>
    public sealed class GoldenVectors
    {
        private static readonly Lazy<GoldenVectors> Instance = new Lazy<GoldenVectors>(Load);

        public static GoldenVectors Current => Instance.Value;

        [JsonPropertyName("rules")] public RulesVector Rules { get; set; } = new RulesVector();

        [JsonPropertyName("hashes")] public List<HashVector> Hashes { get; set; } = new List<HashVector>();

        [JsonPropertyName("attempts")] public List<AttemptVector> Attempts { get; set; } = new List<AttemptVector>();

        [JsonPropertyName("chains")] public List<ChainVector> Chains { get; set; } = new List<ChainVector>();

        [JsonPropertyName("boundaries")]
        public List<BoundaryVector> Boundaries { get; set; } = new List<BoundaryVector>();

        [JsonPropertyName("wrapping")] public List<AttemptVector> Wrapping { get; set; } = new List<AttemptVector>();

        private static GoldenVectors Load()
        {
            string path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "golden-vectors.json");
            return JsonSerializer.Deserialize<GoldenVectors>(File.ReadAllText(path))
                ?? throw new InvalidDataException("golden-vectors.json is empty");
        }
    }

    public sealed class RulesVector
    {
        [JsonPropertyName("success")] public float Success { get; set; }

        [JsonPropertyName("break")] public float Break { get; set; }
    }

    public sealed class HashVector
    {
        [JsonPropertyName("text")] public string Text { get; set; } = string.Empty;

        [JsonPropertyName("hash")] public int Hash { get; set; }

        public override string ToString() => Text;
    }

    public class DrawVector
    {
        [JsonPropertyName("seed")] public int Seed { get; set; }

        [JsonPropertyName("rawBits23")] public uint RawBits23 { get; set; }

        [JsonPropertyName("roll")] public double Roll { get; set; }

        [JsonPropertyName("nextSeed")] public int NextSeed { get; set; }
    }

    public sealed class AttemptVector : DrawVector
    {
        [JsonPropertyName("character")] public string Character { get; set; } = string.Empty;

        [JsonPropertyName("prefab")] public string Prefab { get; set; } = string.Empty;

        [JsonPropertyName("currentLevel")] public int CurrentLevel { get; set; }

        [JsonPropertyName("targetLevel")] public int TargetLevel { get; set; }

        [JsonPropertyName("key")] public int Key { get; set; }

        [JsonPropertyName("storedSeed")] public int? StoredSeed { get; set; }

        [JsonPropertyName("referenceClass")] public string ReferenceClass { get; set; } = string.Empty;

        [JsonPropertyName("expectedOutcome")] public string ExpectedOutcome { get; set; } = string.Empty;

        [JsonPropertyName("precisionSensitive")] public bool PrecisionSensitive { get; set; }

        public override string ToString() => $"{Character}/{Prefab}/{CurrentLevel}/{StoredSeed}";
    }

    public sealed class ChainVector
    {
        [JsonPropertyName("start")] public int Start { get; set; }

        [JsonPropertyName("steps")] public List<DrawVector> Steps { get; set; } = new List<DrawVector>();

        public override string ToString() => Start.ToString();
    }

    public sealed class BoundaryVector : DrawVector
    {
        [JsonPropertyName("expected")] public string Expected { get; set; } = string.Empty;

        [JsonPropertyName("sensitive")] public bool Sensitive { get; set; }

        public override string ToString() => RawBits23.ToString();
    }
}
