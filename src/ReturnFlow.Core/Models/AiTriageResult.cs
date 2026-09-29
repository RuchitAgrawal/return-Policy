using ReturnFlow.Core.Enums;

namespace ReturnFlow.Core.Models;

/// <summary>
/// Strongly-typed record representing the structured JSON output from the AI triage engine.
/// Both SemanticKernelTriageService and MockAiTriageService must produce this record.
/// </summary>
public record AiTriageResult(
    string DefectCategory,
    int SentimentScore,
    string FraudRiskLevel,
    string RecommendedAction,
    string WarehouseRoutingAction,
    string ReasoningSummary,
    string DraftedCustomerEmail
)
{
    /// <summary>Parses string enum values to typed enums, with safe fallbacks.</summary>
    public DefectCategory ParsedDefectCategory =>
        Enum.TryParse<DefectCategory>(DefectCategory, out var v) ? v : Enums.DefectCategory.WearAndTear;

    public FraudRiskLevel ParsedFraudRiskLevel =>
        Enum.TryParse<FraudRiskLevel>(FraudRiskLevel, out var v) ? v : Enums.FraudRiskLevel.Medium;

    public RecommendedAction ParsedRecommendedAction =>
        Enum.TryParse<RecommendedAction>(RecommendedAction, out var v) ? v : Enums.RecommendedAction.EscalateToManualInspect;

    public WarehouseRoutingAction ParsedWarehouseRoutingAction =>
        Enum.TryParse<WarehouseRoutingAction>(WarehouseRoutingAction, out var v) ? v : Enums.WarehouseRoutingAction.HoldInQuarantine;
}

