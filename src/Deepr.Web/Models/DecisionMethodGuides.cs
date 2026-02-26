namespace Deepr.Web.Models;

public class MethodGuide
{
    public int MethodId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Emoji { get; init; } = "⚙️";
    public string Category { get; init; } = string.Empty;
    public string Tagline { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string BestFor { get; init; } = string.Empty;
    public string NotSuitableFor { get; init; } = string.Empty;
    public int Rounds { get; init; }
    public string RoundsSummary { get; init; } = string.Empty;
    public string[] Tips { get; init; } = Array.Empty<string>();
    public string[] Pros { get; init; } = Array.Empty<string>();
    public string[] Cons { get; init; } = Array.Empty<string>();
}

public static class DecisionMethodGuides
{
    public static readonly IReadOnlyList<MethodGuide> All = new List<MethodGuide>
    {
        new()
        {
            MethodId = 0,
            Name = "Delphi",
            Emoji = "🔄",
            Category = "Group-Based",
            Tagline = "Anonymous expert rounds with structured iteration",
            Description = "The Delphi method collects expert opinions anonymously across multiple rounds. After each round, a summary is shared and experts revise their opinions. This process converges on a consensus without the social pressure of face-to-face discussion. Developed at RAND Corporation in the 1950s.",
            BestFor = "Long-term forecasting, policy decisions, technology roadmaps, situations where independent expertise is critical and groupthink must be avoided.",
            NotSuitableFor = "Urgent decisions, situations requiring real-time discussion, or where participant relationships and dialogue are important.",
            Rounds = 3,
            RoundsSummary = "Round 1: Individual expert responses → Round 2: Revised responses after seeing summary → Round 3: Final convergence",
            Tips = new[] {
                "Ensure anonymity — agents won't be influenced by seniority or authority",
                "Provide clear statistical feedback between rounds (median, spread)",
                "Allow agents to revise their estimates based on group feedback",
                "3 rounds typically achieve stable convergence"
            },
            Pros = new[] { "Eliminates groupthink and authority bias", "Produces well-reasoned consensus", "Works across distributed expert panels" },
            Cons = new[] { "Time-consuming (multiple rounds)", "Can oversimplify complex disagreements", "Quality depends on expert panel quality" }
        },
        new()
        {
            MethodId = 1,
            Name = "NGT — Nominal Group Technique",
            Emoji = "📝",
            Category = "Group-Based",
            Tagline = "Structured idea generation followed by silent ranking",
            Description = "NGT is a structured meeting method that combines individual silent idea generation with group discussion and anonymous ranking. It equalises participation, preventing dominant personalities from hijacking the decision. Developed by Delbecq and Van de Ven in 1971.",
            BestFor = "Idea prioritisation, root cause analysis, generating diverse perspectives on complex problems, situations where some participants may be reluctant to speak up.",
            NotSuitableFor = "Situations requiring deep analytical deliberation, or where relationship-building through dialogue is the goal.",
            Rounds = 4,
            RoundsSummary = "Round 1: Silent idea generation → Round 2: Round-robin idea sharing → Round 3: Clarification discussion → Round 4: Private ranking vote",
            Tips = new[] {
                "Keep Round 1 strictly silent — no discussion until sharing phase",
                "In Round 2, each person shares one idea at a time — no interruptions",
                "Discussion in Round 3 is for clarification only, not persuasion",
                "Anonymous ranking in Round 4 prevents conformity bias"
            },
            Pros = new[] { "Equal voice for all participants", "Generates many diverse ideas quickly", "Structured ranking produces clear priorities" },
            Cons = new[] { "Less natural dialogue than open discussion", "Can feel rigid", "May not work well for highly complex analytical problems" }
        },
        new()
        {
            MethodId = 2,
            Name = "Brainstorming",
            Emoji = "💡",
            Category = "Group-Based",
            Tagline = "Free-form idea generation — quantity over quality",
            Description = "Classic brainstorming defers judgment to generate the maximum number of ideas in a short time. All ideas are accepted without criticism. The goal is creative divergence, not evaluation. Coined by Alex Osborn in 'Applied Imagination' (1953).",
            BestFor = "Early-stage exploration, innovation challenges, breaking out of conventional thinking, warm-up exercises before deeper analysis.",
            NotSuitableFor = "Final decisions requiring structured evaluation, highly technical decisions, or situations with clear correct answers.",
            Rounds = 1,
            RoundsSummary = "Round 1: Open idea generation — no criticism, all ideas welcome",
            Tips = new[] {
                "Defer all judgment — 'yes, and...' not 'but...'",
                "Encourage wild ideas — they often spark practical ones",
                "Build on others' ideas (piggybacking)",
                "Go for quantity first — editing comes later"
            },
            Pros = new[] { "Fast, energising, low barrier to entry", "Produces large volume of ideas", "Creative and non-threatening" },
            Cons = new[] { "Ideas may lack depth", "Dominant voices can still emerge", "Production blocking can reduce individual output" }
        },
        new()
        {
            MethodId = 4,
            Name = "Consensus Building",
            Emoji = "🤝",
            Category = "Group-Based",
            Tagline = "Everyone can genuinely accept the decision",
            Description = "Consensus building seeks a decision that all participants can live with, even if not everyone's first preference. Unlike unanimous consent, consensus doesn't require everyone to love the decision — just that no one is opposed enough to block it. Used widely in governance and collaborative organisations.",
            BestFor = "Decisions that require long-term buy-in from all stakeholders, community governance, team norms and working agreements, low-urgency strategic decisions.",
            NotSuitableFor = "Urgent decisions, large groups (>10), situations where positions are entrenched and compromise is unlikely.",
            Rounds = 2,
            RoundsSummary = "Round 1: Share perspectives and identify concerns → Round 2: Indicate agreement level and surface remaining objections",
            Tips = new[] {
                "Distinguish 'I prefer X' from 'I cannot accept Y' — both are important",
                "Focus on interests (underlying needs), not positions",
                "Document any conditions attached to agreement",
                "A 'stand-aside' (I won't block but disagree) is still consensus"
            },
            Pros = new[] { "High buy-in and implementation commitment", "Surfaces all concerns before deciding", "Strengthens team relationships" },
            Cons = new[] { "Slow for large groups", "Risk of false consensus under social pressure", "May result in watered-down decisions" }
        },
        new()
        {
            MethodId = 5,
            Name = "ADKAR",
            Emoji = "🔄",
            Category = "Change Management",
            Tagline = "Structured change management: Awareness → Desire → Knowledge → Ability → Reinforcement",
            Description = "ADKAR (Prosci model) frames decisions as change management problems. It asks: do people know change is needed (Awareness)? Do they want it (Desire)? Do they know how (Knowledge)? Can they do it (Ability)? Will it stick (Reinforcement)? Best for decisions that involve significant organisational or behavioural change.",
            BestFor = "Technology adoption decisions, policy changes requiring behavioural change, organisational restructuring, culture transformation initiatives.",
            NotSuitableFor = "Pure analytical decisions with no change management component, or rapid-fire tactical choices.",
            Rounds = 5,
            RoundsSummary = "Round 1: Awareness — why change is needed → Round 2: Desire — motivations and resistance → Round 3: Knowledge — training and capability needs → Round 4: Ability — implementation planning → Round 5: Reinforcement — sustaining the change",
            Tips = new[] {
                "Each round maps to a specific change barrier — don't skip rounds",
                "Desire is often the hardest — understand the 'what's in it for me'",
                "Knowledge ≠ Ability — knowing what to do and being able to do it are different",
                "Reinforcement without follow-through causes change rollback"
            },
            Pros = new[] { "Structured framework for change-heavy decisions", "Surfaces resistance early", "Ensures implementation readiness" },
            Cons = new[] { "5 rounds can be lengthy", "Less suited to pure strategy decisions", "Assumes agents understand change management" }
        },
        new()
        {
            MethodId = 6,
            Name = "Weighted Deliberation",
            Emoji = "⚖️",
            Category = "Analytical / Structured",
            Tagline = "Structured discussion + expert vote + weighted scoring matrix",
            Description = "Weighted Deliberation combines structured expert discussion, a voting round, and a weighted multi-criteria matrix to produce a quantified recommendation. It is the most comprehensive of the discussion-based methods and produces a numerical ranking of options alongside qualitative deliberation.",
            BestFor = "High-stakes decisions with multiple competing options, decisions requiring a documented and defensible rationale, situations where criteria weighting matters.",
            NotSuitableFor = "Quick decisions, exploratory decisions where options aren't yet defined, or very early-stage problems.",
            Rounds = 4,
            RoundsSummary = "Round 1: Moderator framing → Round 2: Expert discussion → Round 3: Expert deliberation → Round 4: Voting + weighted matrix",
            Tips = new[] {
                "Define criteria and weights BEFORE discussion to avoid post-hoc rationalisation",
                "Moderator role is critical — frames scope, prevents premature convergence",
                "Combine with SWOT or PESTLE as analytical tool for best results",
                "The matrix output is a guide, not a mandate — expert judgment still matters"
            },
            Pros = new[] { "Rigorous and defensible output", "Combines qualitative and quantitative analysis", "Produces ranked options with scores" },
            Cons = new[] { "Requires well-defined criteria upfront", "More complex to set up", "4 rounds takes time" }
        },
        new()
        {
            MethodId = 7,
            Name = "AHP — Analytic Hierarchy Process",
            Emoji = "📊",
            Category = "MCDA",
            Tagline = "Pairwise comparison of criteria and options using Saaty 1–9 scale",
            Description = "AHP (Thomas Saaty, 1970s) decomposes a decision into a hierarchy of criteria and alternatives, then uses pairwise comparisons (1=equal, 9=absolutely more important) to derive priority weights. It produces a mathematically consistent ranking with consistency ratio checks.",
            BestFor = "Complex multi-criteria decisions where criteria importance must be explicitly compared, procurement, strategic investment, infrastructure decisions.",
            NotSuitableFor = "Simple binary decisions, situations where criteria weights are already agreed, or very large option sets (>7 becomes unwieldy).",
            Rounds = 4,
            RoundsSummary = "AHP feeds the MCDA Analysis module — run 4 deliberation rounds then use the MCDA Calculator",
            Tips = new[] {
                "Keep pairwise comparisons consistent — AHP calculates a consistency ratio",
                "Limit to ≤7 criteria for cognitive manageability",
                "Use the 1–9 Saaty scale: 1=equal, 3=moderate, 5=strong, 7=very strong, 9=absolute",
                "After running rounds, use the MCDA Calculator tab to compute the final ranking"
            },
            Pros = new[] { "Mathematically rigorous ranking", "Handles both quantitative and qualitative criteria", "Consistency ratio detects logical errors" },
            Cons = new[] { "Can be cognitively demanding with many criteria", "Requires all pairwise comparisons (n²)", "Less intuitive than simple scoring" }
        },
        new()
        {
            MethodId = 8,
            Name = "ELECTRE — Outranking Method",
            Emoji = "📊",
            Category = "MCDA",
            Tagline = "Concordance/discordance outranking for multi-criteria decisions",
            Description = "ELECTRE (Élimination et Choix Traduisant la Réalité) is a family of outranking methods that compare options pairwise on concordance (how many criteria favour A over B) and discordance (how strongly B outperforms A on any criterion). It handles incomparability and strong veto thresholds.",
            BestFor = "Decisions where some criteria are non-compensable (a very bad score on one criterion cannot be offset by good scores elsewhere), environmental and infrastructure decisions.",
            NotSuitableFor = "Situations where a single composite score is required, or where stakeholders need a simple and easily explainable ranking.",
            Rounds = 4,
            RoundsSummary = "ELECTRE feeds the MCDA Analysis module — run 4 deliberation rounds then use the MCDA Calculator",
            Tips = new[] {
                "Define veto thresholds — the maximum discordance before an option is eliminated",
                "Use with the MCDA Calculator after completing deliberation rounds",
                "Best for 4–8 options and 4–8 criteria",
                "Results show partial rankings — some options may be 'incomparable'"
            },
            Pros = new[] { "Handles incomparability explicitly", "Strong veto prevents bad options winning on aggregate", "Used in major European infrastructure decisions" },
            Cons = new[] { "Complex to explain to non-technical stakeholders", "Requires careful threshold calibration", "Can produce partial or ambiguous rankings" }
        },
        new()
        {
            MethodId = 9,
            Name = "TOPSIS",
            Emoji = "📊",
            Category = "MCDA",
            Tagline = "Rank options by closeness to ideal and distance from anti-ideal",
            Description = "TOPSIS (Technique for Order of Preference by Similarity to Ideal Solution) ranks options by computing their distance from the ideal solution (best on all criteria) and the anti-ideal solution (worst on all criteria). The best option is closest to ideal AND furthest from anti-ideal.",
            BestFor = "Supplier selection, vendor evaluation, site selection, technology choice — any decision with multiple numeric criteria and clear ideal values.",
            NotSuitableFor = "Qualitative-only criteria, decisions with fewer than 3 options, or where criteria are not numeric.",
            Rounds = 4,
            RoundsSummary = "TOPSIS feeds the MCDA Analysis module — run 4 deliberation rounds then use the MCDA Calculator",
            Tips = new[] {
                "Normalise criteria units before scoring (TOPSIS handles this automatically)",
                "Define whether each criterion is 'benefit' (higher is better) or 'cost' (lower is better)",
                "Scores should be on a consistent 0–10 scale",
                "Combine with expert deliberation rounds for best results"
            },
            Pros = new[] { "Simple geometric interpretation", "Handles both benefit and cost criteria", "Produces a clear ranking with numeric scores" },
            Cons = new[] { "Sensitive to range of scores (rank reversal can occur)", "Assumes linear preference", "Requires numeric scores for all criteria" }
        },
        new()
        {
            MethodId = 10,
            Name = "PROMETHEE II",
            Emoji = "📊",
            Category = "MCDA",
            Tagline = "Net outranking flow for complete preference ranking",
            Description = "PROMETHEE II (Preference Ranking Organisation Method for Enrichment Evaluations) computes a net outranking flow for each option by comparing it pairwise against all other options across all criteria. A positive net flow means the option outperforms more than it is outperformed.",
            BestFor = "Strategic decisions with heterogeneous criteria (mix of quantitative and qualitative), energy and environmental policy decisions, logistics optimisation.",
            NotSuitableFor = "Very large option sets (>15 becomes slow), or situations where stakeholders prefer a simpler method.",
            Rounds = 4,
            RoundsSummary = "PROMETHEE II feeds the MCDA Analysis module — run 4 deliberation rounds then use the MCDA Calculator",
            Tips = new[] {
                "Choose the right preference function per criterion (V-shape, Gaussian, linear)",
                "Positive net flow = overall preferred option; negative = overall dominated",
                "Can produce partial ranking (PROMETHEE I) or complete ranking (PROMETHEE II)",
                "Use GAIA plane visualisation (if available) to see clustering of options and criteria"
            },
            Pros = new[] { "Handles qualitative and quantitative criteria", "No normalisation required", "Produces both partial and complete rankings" },
            Cons = new[] { "Preference function selection requires expertise", "Less intuitive than TOPSIS for non-specialists", "Computationally intensive for large datasets" }
        },
        new()
        {
            MethodId = 11,
            Name = "Grey Theory — Grey Relational Analysis",
            Emoji = "📊",
            Category = "MCDA",
            Tagline = "Decision-making under uncertainty with incomplete information",
            Description = "Grey Theory (Deng Julong, 1982) is designed for decisions made with limited, incomplete, or uncertain data. Grey Relational Analysis (GRA) measures how closely each option's performance profile matches the ideal reference sequence, producing a grey relational grade.",
            BestFor = "Decisions with limited historical data, small sample sizes, or significant data uncertainty — common in emerging markets, new technology evaluation, and early-stage projects.",
            NotSuitableFor = "Situations with abundant, high-quality data (where classical statistics are preferable), or where stakeholders require intuitively explainable rankings.",
            Rounds = 4,
            RoundsSummary = "Grey Theory feeds the MCDA Analysis module — run 4 deliberation rounds then use the MCDA Calculator",
            Tips = new[] {
                "Grey theory is specifically designed for 'small data, poor information' situations",
                "The distinguishing coefficient (ζ) controls resolution — typically set to 0.5",
                "Ideal reference sequence = best value for each criterion",
                "Grey relational grade closer to 1.0 = better option"
            },
            Pros = new[] { "Handles data uncertainty explicitly", "Effective with small sample sizes", "Established in manufacturing and supply chain decisions" },
            Cons = new[] { "Less known to Western practitioners", "Distinguishing coefficient choice affects results", "Harder to communicate to non-technical stakeholders" }
        },
        new()
        {
            MethodId = 12,
            Name = "Majority Voting",
            Emoji = "🗳️",
            Category = "Group-Based",
            Tagline = "Open discussion followed by one-person-one-vote",
            Description = "Majority Voting is the most familiar democratic decision method. After an open discussion where options are debated, each participant casts a single vote. The option with more than 50% of votes wins. If no majority is reached, the plurality winner is selected (most votes, not necessarily >50%).",
            BestFor = "Clear binary or multi-option decisions, democratic team decisions, low-to-medium stakes choices where speed and fairness matter.",
            NotSuitableFor = "Highly technical decisions requiring expertise, situations where the minority view needs to be heard, or where the decision requires ongoing buy-in from all parties.",
            Rounds = 2,
            RoundsSummary = "Round 1: Open discussion and proposal → Round 2: Anonymous vote — VOTE: [option]",
            Tips = new[] {
                "Ensure the discussion phase is time-limited to prevent one voice dominating",
                "Make voting anonymous to prevent conformity bias",
                "Define the winner criterion upfront: majority (>50%) or plurality (most votes)",
                "Consider a runoff if no option reaches majority on first vote"
            },
            Pros = new[] { "Fast, familiar, and easy to implement", "Perceived as fair", "Works with any group size" },
            Cons = new[] { "Minority view may be systematically ignored", "Majority can be wrong", "Encourages adversarial rather than collaborative framing" }
        },
        new()
        {
            MethodId = 13,
            Name = "Six Thinking Hats",
            Emoji = "🎩",
            Category = "Analytical / Structured",
            Tagline = "Parallel thinking — explore all 6 perspectives systematically",
            Description = "Six Thinking Hats (Edward de Bono, 1985) is a structured parallel thinking framework. Instead of everyone debating simultaneously from different angles, all participants think from the same perspective at the same time. Each 'hat' represents a distinct thinking mode: facts (white), emotion (red), caution (black), optimism (yellow), creativity (green), process (blue).",
            BestFor = "Decisions where balanced exploration of multiple perspectives is needed, creative problem-solving, overcoming cognitive bias, reducing conflict in decision meetings.",
            NotSuitableFor = "Rapid decisions, purely quantitative analysis, or situations requiring deep domain expertise across all rounds.",
            Rounds = 6,
            RoundsSummary = "Round 1: 🎩 White (Facts) → Round 2: ❤️ Red (Emotions) → Round 3: ⚫ Black (Caution) → Round 4: 💛 Yellow (Optimism) → Round 5: 💚 Green (Creativity) → Round 6: 🔵 Blue (Process/Summary)",
            Tips = new[] {
                "Enforce hat discipline — agents must think ONLY from the current hat's perspective",
                "Black hat prevents premature optimism; Yellow hat prevents excessive pessimism",
                "Green hat is deliberately uncritical — wild ideas are valuable",
                "Blue hat (final round) synthesises ALL previous rounds into a recommendation"
            },
            Pros = new[] { "Reduces adversarial debate", "Ensures all perspectives are explored", "Creative and structured simultaneously" },
            Cons = new[] { "Six rounds can be time-consuming", "Requires facilitation discipline", "Less effective if agents default to their 'natural' thinking style" }
        },
        new()
        {
            MethodId = 14,
            Name = "Cost–Benefit Analysis",
            Emoji = "📉",
            Category = "Analytical / Structured",
            Tagline = "Quantify and compare all costs and benefits before deciding",
            Description = "Cost–Benefit Analysis (CBA) is a systematic approach to estimate the economic pros and cons of a decision. All costs and benefits are identified, quantified (where possible), and compared to determine net value. CBA is the standard framework for public policy and major investment decisions.",
            BestFor = "Investment decisions, project go/no-go decisions, policy evaluation, infrastructure choices, technology purchases — any decision where financial and non-financial trade-offs must be compared.",
            NotSuitableFor = "Exploratory decisions where options aren't yet defined, decisions where costs/benefits resist quantification, or time-critical operational choices.",
            Rounds = 3,
            RoundsSummary = "Round 1: Cost identification and estimation → Round 2: Benefit identification and estimation → Round 3: Synthesis — net value + PROCEED / DO NOT PROCEED / CONDITIONS",
            Tips = new[] {
                "Include ALL costs: direct, indirect, opportunity costs, and externalities",
                "Include ALL benefits: financial, strategic, operational, and reputational",
                "Use NPV (Net Present Value) for multi-year analyses to account for time value of money",
                "Sensitivity analysis: how does the conclusion change if key assumptions are wrong?"
            },
            Pros = new[] { "Rigorous, defensible, and widely understood", "Forces explicit quantification of fuzzy benefits", "Produces a clear PROCEED/NOT PROCEED recommendation" },
            Cons = new[] { "Can undervalue non-quantifiable factors", "Garbage in, garbage out — estimates must be honest", "May create false precision with uncertain data" }
        },
        new()
        {
            MethodId = 15,
            Name = "RAPID",
            Emoji = "⚡",
            Category = "Analytical / Structured",
            Tagline = "Role-based decision clarity: Recommend, Agree, Perform, Input, Decide",
            Description = "RAPID (Bain & Company) is a framework for clarifying who does what in decision-making. Each letter maps to a role: Recommend (proposes the decision), Agree (must sign off), Perform (implements), Input (provides data and advice), Decide (makes the final call). It eliminates confusion about accountability and prevents decision-making gridlock.",
            BestFor = "Decisions suffering from unclear accountability, cross-functional decisions involving multiple stakeholders, decisions requiring explicit sign-off authority.",
            NotSuitableFor = "Simple team decisions where everyone has equal standing, urgent crisis decisions where role assignment is impractical.",
            Rounds = 4,
            RoundsSummary = "Round 1: R — Recommend (present proposal) → Round 2: I — Input (expert data) → Round 3: A — Agree (surface objections) → Round 4: D — Decide (final authority decides)",
            Tips = new[] {
                "Assign RAPID roles before the session — don't let them emerge organically",
                "Only one person holds D (Decide) — ambiguity here causes deadlock",
                "A (Agree) has veto power — if they block, the decision cannot proceed until resolved",
                "P (Perform) should be involved early to flag implementation constraints"
            },
            Pros = new[] { "Eliminates accountability confusion", "Prevents decision gridlock", "Scalable across complex multi-stakeholder environments" },
            Cons = new[] { "Requires clear upfront role assignment", "Can feel bureaucratic for small teams", "Not designed for emergent/adaptive decisions" }
        },
        new()
        {
            MethodId = 16,
            Name = "OODA Loop",
            Emoji = "🔁",
            Category = "Analytical / Structured",
            Tagline = "Observe → Orient → Decide → Act — iterative rapid decision-making",
            Description = "The OODA Loop (Col. John Boyd, US Air Force) is a decision-making cycle for fast-moving, competitive situations. The loop emphasises speed of decision and the ability to disrupt an opponent's (or market's) decision cycle by acting faster. The key is that 'Orient' shapes how we observe — mental models, experience, and culture all filter perception.",
            BestFor = "Competitive strategy, crisis response, cybersecurity incident management, M&A competitive bids, market entry decisions with time pressure.",
            NotSuitableFor = "Deliberative policy decisions requiring extensive stakeholder consultation, decisions where speed is less important than thoroughness.",
            Rounds = 4,
            RoundsSummary = "Round 1: Observe (gather raw facts) → Round 2: Orient (sense-making & mental model challenge) → Round 3: Decide (select course of action) → Round 4: Act (implementation plan + feedback loop)",
            Tips = new[] {
                "Orient is the most critical phase — it's where biases are challenged",
                "Speed matters: a fast 70% solution often beats a slow perfect one",
                "The loop repeats — Act feeds new Observations for the next cycle",
                "Challenge mental models explicitly in Orient: 'What assumptions are we making?'"
            },
            Pros = new[] { "Fast and adaptive to changing conditions", "Explicitly surfaces mental model biases", "Well-tested in competitive military and business contexts" },
            Cons = new[] { "Less rigorous than analytical methods for complex multi-criteria decisions", "Can bias toward speed over thoroughness", "Requires honest self-assessment of mental models" }
        }
    };

    private static readonly Dictionary<int, MethodGuide> _byId = All.ToDictionary(m => m.MethodId);

    public static MethodGuide? FindById(int id) =>
        _byId.TryGetValue(id, out var guide) ? guide : null;
}
