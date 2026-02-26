namespace Deepr.Web.Models;

public class IssueTemplate
{
    public string Id { get; init; } = string.Empty;
    public string Icon { get; init; } = "📋";
    public string Title { get; init; } = string.Empty;
    public string ContextVector { get; init; } = string.Empty;
    public string Category { get; init; } = "General";
    public string Description { get; init; } = string.Empty;
}

public static class IssueTemplates
{
    public static readonly IReadOnlyList<IssueTemplate> All = new List<IssueTemplate>
    {
        new()
        {
            Id = "blank",
            Icon = "✏️",
            Title = "",
            ContextVector = "",
            Category = "Blank",
            Description = "Start with an empty issue and define everything yourself."
        },
        new()
        {
            Id = "microservices-vs-monolith",
            Icon = "🏗️",
            Title = "Should we adopt microservices architecture?",
            ContextVector = "Our monolithic application is growing in complexity and is becoming harder to scale and maintain. We need to decide whether to refactor into microservices or continue with our current monolith. Key considerations include team size, deployment complexity, data consistency, and operational overhead.",
            Category = "Technology",
            Description = "Evaluate the trade-offs of microservices vs monolith for a growing system."
        },
        new()
        {
            Id = "cloud-migration",
            Icon = "☁️",
            Title = "Which cloud provider should we migrate to?",
            ContextVector = "We are planning to migrate our on-premises infrastructure to the cloud. We need to select among AWS, Azure, and GCP. Criteria include cost, existing team expertise, compliance requirements, managed service offerings, and vendor lock-in risk.",
            Category = "Technology",
            Description = "Compare cloud providers for an infrastructure migration."
        },
        new()
        {
            Id = "tech-stack-selection",
            Icon = "⚙️",
            Title = "What technology stack should we use for our new product?",
            ContextVector = "We are starting development of a new product and need to choose our technology stack. Considerations include developer availability, performance requirements, time-to-market, long-term maintainability, ecosystem maturity, and community support.",
            Category = "Technology",
            Description = "Select the right technology stack for a greenfield project."
        },
        new()
        {
            Id = "devops-tooling",
            Icon = "🔧",
            Title = "Which CI/CD and DevOps toolchain should we standardise on?",
            ContextVector = "Our engineering teams are using different CI/CD pipelines and DevOps tools, leading to inconsistency and duplicated effort. We need to standardise on a common toolchain covering source control, CI, CD, monitoring, and incident management.",
            Category = "Technology",
            Description = "Standardise CI/CD and DevOps tools across engineering teams."
        },
        new()
        {
            Id = "remote-work-policy",
            Icon = "🏠",
            Title = "Should we adopt a remote-first or hybrid work policy?",
            ContextVector = "Post-pandemic, we need to formalise our work policy. Options range from fully remote to full office return. We must consider employee satisfaction, productivity, collaboration quality, real estate costs, talent acquisition, and team culture.",
            Category = "HR & Organisation",
            Description = "Decide between remote-first, hybrid, and in-office work arrangements."
        },
        new()
        {
            Id = "compensation-framework",
            Icon = "💰",
            Title = "How should we redesign our compensation and benefits framework?",
            ContextVector = "Our current compensation structure is outdated and not competitive in the current market. We need to redesign salary bands, equity options, bonuses, and benefits. Constraints include budget limits, pay equity requirements, retention goals, and market benchmarks.",
            Category = "HR & Organisation",
            Description = "Redesign a competitive and equitable compensation framework."
        },
        new()
        {
            Id = "team-restructuring",
            Icon = "🏢",
            Title = "How should we restructure our engineering teams?",
            ContextVector = "Our organisation has grown rapidly and our team structure is creating communication bottlenecks and unclear ownership. We are considering moving from functional teams to cross-functional product squads. We need to evaluate impact on delivery speed, accountability, and culture.",
            Category = "HR & Organisation",
            Description = "Evaluate team topology changes to improve delivery and ownership."
        },
        new()
        {
            Id = "performance-review",
            Icon = "📊",
            Title = "What performance review process should we implement?",
            ContextVector = "Our annual performance review cycle is seen as burdensome and not effective. We are evaluating alternatives including continuous feedback, OKRs, 360-degree reviews, and manager-only assessments. We need a process that drives growth, is fair, and does not create excessive overhead.",
            Category = "HR & Organisation",
            Description = "Design a fair and effective performance review process."
        },
        new()
        {
            Id = "feature-prioritisation",
            Icon = "📋",
            Title = "Which product features should we prioritise for the next quarter?",
            ContextVector = "We have a backlog of requested features and improvements but limited engineering capacity. We need to select and prioritise work for the next quarter based on customer impact, strategic alignment, technical dependencies, and effort estimates.",
            Category = "Product & Business",
            Description = "Prioritise product features against capacity constraints and strategic goals."
        },
        new()
        {
            Id = "go-to-market",
            Icon = "🚀",
            Title = "What go-to-market strategy should we use for the new product?",
            ContextVector = "We are preparing to launch a new product and need to define our go-to-market strategy. Key decisions involve target segments, pricing positioning, sales channels, marketing mix, launch timing, and success metrics.",
            Category = "Product & Business",
            Description = "Define the launch strategy for a new product or major feature."
        },
        new()
        {
            Id = "pricing-model",
            Icon = "🏷️",
            Title = "Should we change our pricing model?",
            ContextVector = "Our current pricing model is not effectively capturing value or maximising revenue. We are considering moving from per-seat to usage-based pricing, introducing a freemium tier, or restructuring our enterprise tier. We need to evaluate impact on revenue, churn, and customer acquisition.",
            Category = "Product & Business",
            Description = "Evaluate a shift in pricing model to improve growth and retention."
        },
        new()
        {
            Id = "partnership",
            Icon = "🤝",
            Title = "Should we enter into a strategic partnership with [Company X]?",
            ContextVector = "We have received a partnership proposal that would involve co-marketing, technology integration, and potential revenue sharing. We need to assess strategic fit, financial impact, resource requirements, risk factors, and how this partnership aligns with our long-term goals.",
            Category = "Product & Business",
            Description = "Evaluate a proposed strategic partnership opportunity."
        },
        new()
        {
            Id = "vendor-selection",
            Icon = "🛒",
            Title = "Which vendor should we select for [service/product]?",
            ContextVector = "We need to procure a new vendor for a critical service or product. We have shortlisted several candidates and need to evaluate them against criteria including price, quality, reliability, support responsiveness, contract terms, and alignment with our procurement standards.",
            Category = "Procurement",
            Description = "Select the best vendor through structured multi-criteria evaluation."
        },
        new()
        {
            Id = "budget-allocation",
            Icon = "💼",
            Title = "How should we allocate next year's budget across departments?",
            ContextVector = "Annual budget planning is underway. We need to allocate limited funds across engineering, marketing, sales, operations, and R&D. Criteria include strategic priority, expected ROI, current performance, headcount needs, and market conditions.",
            Category = "Finance",
            Description = "Distribute budget allocation across departments for the coming year."
        },
        new()
        {
            Id = "risk-assessment",
            Icon = "⚠️",
            Title = "Should we proceed with [project/initiative] given the identified risks?",
            ContextVector = "A proposed initiative carries several identified risks including financial exposure, regulatory uncertainty, technical complexity, and market timing. We need to assess whether the potential benefits outweigh the risks, and what mitigation strategies are available.",
            Category = "Risk",
            Description = "Assess risks vs. rewards before committing to a major initiative."
        },
        new()
        {
            Id = "investment-decision",
            Icon = "📈",
            Title = "Should we invest in [acquisition/expansion/new market]?",
            ContextVector = "We are evaluating a significant investment opportunity. It could be an acquisition, geographic expansion, or entry into a new market. We need to assess financial returns, strategic fit, execution complexity, integration risk, and alternative uses of capital.",
            Category = "Finance",
            Description = "Evaluate a major strategic investment or expansion opportunity."
        },
        new()
        {
            Id = "esg-initiative",
            Icon = "🌱",
            Title = "Which ESG and sustainability initiatives should we prioritise?",
            ContextVector = "Stakeholders are increasingly focused on our environmental, social, and governance performance. We need to decide which initiatives to prioritise, including carbon reduction, supply chain ethics, diversity programmes, and community investment, within our sustainability budget.",
            Category = "Sustainability",
            Description = "Prioritise ESG and sustainability programmes against impact and feasibility."
        },
        new()
        {
            Id = "data-privacy-policy",
            Icon = "🔒",
            Title = "How should we update our data privacy and retention policies?",
            ContextVector = "Upcoming regulatory changes and growing customer concerns about data privacy require us to update our data retention and privacy policies. We need to balance compliance obligations, user trust, data utility for analytics, and implementation cost.",
            Category = "Compliance",
            Description = "Revise data privacy and retention policies to meet regulations and user trust."
        },
        new()
        {
            Id = "office-space",
            Icon = "🏬",
            Title = "Should we renew, downsize, or relocate our office space?",
            ContextVector = "Our office lease is expiring and we need to make a decision about our physical footprint. Options include renewing in place, downsizing to reflect hybrid work patterns, or relocating to a more cost-effective or strategic location. Considerations include cost, employee commute, collaboration needs, and brand perception.",
            Category = "Operations",
            Description = "Decide office space strategy given changing work patterns and costs."
        },
        new()
        {
            Id = "open-source-strategy",
            Icon = "🌐",
            Title = "Should we open-source our core platform?",
            ContextVector = "We are considering open-sourcing part or all of our core platform. This could accelerate adoption, attract contributors, and strengthen our brand. However, it also involves IP considerations, competitive risk, governance requirements, and ongoing maintenance obligations.",
            Category = "Technology",
            Description = "Evaluate the strategic, legal, and technical trade-offs of open-sourcing your platform."
        },
        new()
        {
            Id = "ai-adoption",
            Icon = "🤖",
            Title = "How should we adopt AI tools in our engineering and operations workflows?",
            ContextVector = "AI-assisted tooling (code generation, testing, documentation, customer support) has matured significantly. We need a policy and adoption roadmap that balances productivity gains, quality assurance, security and IP risk, staff upskilling, and ethical use guidelines.",
            Category = "Technology",
            Description = "Create a structured plan for adopting AI tools responsibly across teams."
        }
    };

    public static IssueTemplate? FindById(string id) =>
        All.FirstOrDefault(t => t.Id == id);
}
