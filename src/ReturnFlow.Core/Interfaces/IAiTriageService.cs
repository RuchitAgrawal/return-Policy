using ReturnFlow.Core.Models;

namespace ReturnFlow.Core.Interfaces;

/// <summary>
/// Abstraction for the AI triage engine. 
/// Implementations: MockAiTriageService (no API key) and SemanticKernelTriageService (live OpenAI).
/// </summary>
public interface IAiTriageService
{
    /// <summary>
    /// Analyze a customer complaint alongside SQL order context and produce a structured triage decision.
    /// </summary>
    /// <param name="complaintText">Raw unstructured customer complaint text.</param>
    /// <param name="context">Structured SQL context injected into the prompt to ground the LLM.</param>
    /// <returns>A strongly-typed <see cref="AiTriageResult"/> record.</returns>
    Task<AiTriageResult> AnalyzeAsync(string complaintText, AiTriageContext context);

    /// <summary>
    /// Returns true if this implementation is using a live LLM (vs. a deterministic mock).
    /// Used by the UI to display the AI mode badge.
    /// </summary>
    bool IsLiveMode { get; }
}

/// <summary>
/// SQL order context passed to the AI engine to ground its decision in database facts.
/// </summary>
public record AiTriageContext(
    string OrderNumber,
    string SalesChannel,
    string ProductName,
    string Sku,
    decimal UnitPrice,
    DateTime OrderDate,
    DateTime DeliveredDate,
    int DaysSinceDelivery,
    bool IsWithinReturnWindow,
    bool IsWithinWarrantyWindow,
    int WarrantyDays,
    string ConditionReportedByCustomer,
    string RequestedResolution
);

