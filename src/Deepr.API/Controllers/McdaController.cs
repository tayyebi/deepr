using Deepr.Application.DTOs;
using Deepr.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Deepr.API.Controllers;

/// <summary>
/// Standalone MCDA (Multi-Criteria Decision Analysis) endpoints.
/// All methods operate purely on the provided decision matrix —
/// no AI, no sessions, and no database are required.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class McdaController : ControllerBase
{
    private readonly IMcdaService _mcda;

    public McdaController(IMcdaService mcda) => _mcda = mcda;

    /// <summary>
    /// Lists the available MCDA methods.
    /// </summary>
    [HttpGet("methods")]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status200OK)]
    public IActionResult GetMethods()
        => Ok(new[] { "ahp", "electre", "topsis", "promethee", "grey-theory" });

    /// <summary>
    /// Runs the Analytic Hierarchy Process (AHP).
    /// Provide scores on the 1–9 Saaty scale for best results.
    /// Returns alternatives ranked by AHP priority score.
    /// </summary>
    [HttpPost("ahp")]
    [ProducesResponseType(typeof(McdaResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<McdaResult> RunAhp([FromBody] McdaInput input)
    {
        try { return Ok(_mcda.RunAhp(input)); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
    }

    /// <summary>
    /// Runs the ELECTRE outranking method.
    /// Builds concordance/discordance matrices; ranks alternatives by net outranking score.
    /// Provide scores on a 0–10 scale.
    /// </summary>
    [HttpPost("electre")]
    [ProducesResponseType(typeof(McdaResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<McdaResult> RunElectre([FromBody] McdaInput input)
    {
        try { return Ok(_mcda.RunElectre(input)); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
    }

    /// <summary>
    /// Runs TOPSIS (Technique for Order of Preference by Similarity to Ideal Solution).
    /// Ranks alternatives by their closeness coefficient C*.
    /// Mark cost criteria with <c>isBenefit: false</c>. Provide scores on a 0–10 scale.
    /// </summary>
    [HttpPost("topsis")]
    [ProducesResponseType(typeof(McdaResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<McdaResult> RunTopsis([FromBody] McdaInput input)
    {
        try { return Ok(_mcda.RunTopsis(input)); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
    }

    /// <summary>
    /// Runs PROMETHEE II.
    /// Uses a linear preference function; ranks alternatives by net outranking flow φ = φ+ − φ−.
    /// Provide scores on a 0–10 scale.
    /// </summary>
    [HttpPost("promethee")]
    [ProducesResponseType(typeof(McdaResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<McdaResult> RunPromethee([FromBody] McdaInput input)
    {
        try { return Ok(_mcda.RunPromethee(input)); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
    }

    /// <summary>
    /// Runs Grey Relational Analysis (GRA / Grey Theory).
    /// Ranks alternatives by weighted grey relational grade (GRG) using ζ = 0.5.
    /// Provide scores on a 0–10 scale.
    /// </summary>
    [HttpPost("grey-theory")]
    [ProducesResponseType(typeof(McdaResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<McdaResult> RunGreyTheory([FromBody] McdaInput input)
    {
        try { return Ok(_mcda.RunGreyTheory(input)); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
    }

    /// <summary>
    /// Convenience endpoint: runs any MCDA method by name.
    /// Valid method names: <c>ahp</c>, <c>electre</c>, <c>topsis</c>, <c>promethee</c>, <c>grey-theory</c>.
    /// </summary>
    [HttpPost("run/{method}")]
    [ProducesResponseType(typeof(McdaResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<McdaResult> Run(string method, [FromBody] McdaInput input)
    {
        try
        {
            McdaResult result = method.ToLowerInvariant() switch
            {
                "ahp"         => _mcda.RunAhp(input),
                "electre"     => _mcda.RunElectre(input),
                "topsis"      => _mcda.RunTopsis(input),
                "promethee"   => _mcda.RunPromethee(input),
                "grey-theory" => _mcda.RunGreyTheory(input),
                _             => throw new ArgumentException($"Unknown MCDA method '{method}'. Valid: ahp, electre, topsis, promethee, grey-theory.")
            };
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
