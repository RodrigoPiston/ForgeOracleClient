namespace ForgeOracle.Core.Abstractions
{
    /// <summary>Something in the running game that affects whether a prediction can be trusted.</summary>
    public sealed class CompatibilityIssue
    {
        public CompatibilityIssue(string code, IssueSeverity severity, string source, string detail)
        {
            Code = code ?? string.Empty;
            Severity = severity;
            Source = source ?? string.Empty;
            Detail = detail ?? string.Empty;
        }

        /// <summary>Stable machine-readable code, for example <c>foreign-roll-patch</c>.</summary>
        public string Code { get; }

        public IssueSeverity Severity { get; }

        /// <summary>The mod or component responsible, for example a BepInEx GUID or Harmony id.</summary>
        public string Source { get; }

        /// <summary>English diagnostic detail for logs and the status command.</summary>
        public string Detail { get; }

        public override string ToString() => $"[{Severity}] {Code} ({Source}): {Detail}";
    }
}
