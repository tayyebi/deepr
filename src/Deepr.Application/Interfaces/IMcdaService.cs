using Deepr.Application.DTOs;

namespace Deepr.Application.Interfaces;

/// <summary>
/// Standalone MCDA (Multi-Criteria Decision Analysis) service.
/// All methods operate purely on the provided decision matrix — no AI, sessions, or database required.
/// </summary>
public interface IMcdaService
{
    /// <summary>
    /// Analytic Hierarchy Process (AHP).
    /// Normalises the decision matrix by column and computes AHP priority scores.
    /// Recommended input scale: 1–9 (Saaty).
    /// </summary>
    McdaResult RunAhp(McdaInput input);

    /// <summary>
    /// ELECTRE outranking method.
    /// Builds concordance and discordance matrices; ranks alternatives by net outranking score.
    /// Input scale: 0–10.
    /// </summary>
    McdaResult RunElectre(McdaInput input);

    /// <summary>
    /// TOPSIS (Technique for Order of Preference by Similarity to Ideal Solution).
    /// Ranks alternatives by their closeness coefficient C* to the positive ideal solution.
    /// Supports benefit and cost criteria via <see cref="McdaCriterion.IsBenefit"/>.
    /// Input scale: 0–10.
    /// </summary>
    McdaResult RunTopsis(McdaInput input);

    /// <summary>
    /// PROMETHEE II.
    /// Applies a linear preference function and ranks alternatives by net outranking flow φ = φ+ − φ−.
    /// Input scale: 0–10.
    /// </summary>
    McdaResult RunPromethee(McdaInput input);

    /// <summary>
    /// Grey Relational Analysis (GRA).
    /// Normalises scores against an ideal reference sequence and ranks alternatives by
    /// weighted grey relational grade (GRG). Distinguishing coefficient ζ = 0.5.
    /// Input scale: 0–10.
    /// </summary>
    McdaResult RunGreyTheory(McdaInput input);
}
