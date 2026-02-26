using System.Text.Json.Serialization;

namespace Deepr.Application.DTOs;

/// <summary>
/// A single evaluation criterion for a standalone MCDA computation.
/// </summary>
public class McdaCriterion
{
    /// <summary>Criterion name (e.g. "Feasibility").</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Relative weight. Weights are automatically normalised to sum to 1.</summary>
    public double Weight { get; set; } = 1.0;

    /// <summary>
    /// For TOPSIS only: <c>true</c> (default) if a higher score is better (benefit criterion);
    /// <c>false</c> if a lower score is better (cost criterion).
    /// </summary>
    public bool IsBenefit { get; set; } = true;

    [JsonConstructor]
    public McdaCriterion() { }
    public McdaCriterion(string name, double weight, bool isBenefit = true)
    {
        Name = name;
        Weight = weight;
        IsBenefit = isBenefit;
    }
}

/// <summary>
/// Input payload for a standalone MCDA computation.
/// </summary>
public class McdaInput
{
    /// <summary>List of alternative names to evaluate.</summary>
    public List<string> Options { get; set; } = new();

    /// <summary>Criteria definitions including names, weights and (for TOPSIS) benefit/cost type.</summary>
    public List<McdaCriterion> Criteria { get; set; } = new();

    /// <summary>
    /// Decision matrix: <c>scores[option][criterion] = value</c>.
    /// Values should be on a consistent numeric scale (e.g. 0–10).
    /// For AHP, values on the 1–9 Saaty scale are recommended.
    /// Missing entries (option/criterion combinations not present in this dictionary)
    /// default to <c>5.0</c> (mid-point of the 0–10 scale).
    /// </summary>
    public Dictionary<string, Dictionary<string, double>> Scores { get; set; } = new();
}

/// <summary>
/// Result of a standalone MCDA computation.
/// </summary>
public class McdaResult
{
    /// <summary>Name of the MCDA method applied.</summary>
    public string Method { get; set; } = string.Empty;

    /// <summary>Alternatives ordered from most to least preferred.</summary>
    public List<string> Ranking { get; set; } = new();

    /// <summary>Method-specific numeric score per alternative (higher = better for all methods).</summary>
    public Dictionary<string, double> Scores { get; set; } = new();

    /// <summary>Human-readable Markdown summary table.</summary>
    public string Summary { get; set; } = string.Empty;

    /// <summary>Additional method-specific intermediate data (matrices, flows, etc.).</summary>
    public Dictionary<string, object> Details { get; set; } = new();
}
