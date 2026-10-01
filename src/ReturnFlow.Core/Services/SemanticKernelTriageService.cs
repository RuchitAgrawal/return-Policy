using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ReturnFlow.Core.Interfaces;
using ReturnFlow.Core.Models;

namespace ReturnFlow.Core.Services;

/// <summary>
/// Live AI triage implementation using OpenAI / Semantic Kernel compatible endpoints.
/// Enforces strict structured JSON output to prevent hallucinations.
/// Falls back gracefully to MockAiTriageService if parsing fails or credits expire.
/// </summary>
public class SemanticKernelTriageService : IAiTriageService
{
    private readonly string? _apiKey;
    private readonly string _modelId;
    private readonly MockAiTriageService _fallbackService;
    private readonly ILogger<SemanticKernelTriageService> _logger;
    private readonly HttpClient _httpClient;

    public bool IsLiveMode => !string.IsNullOrWhiteSpace(_apiKey);

    public SemanticKernelTriageService(
        IConfiguration configuration,
        ILogger<SemanticKernelTriageService> logger,
        HttpClient httpClient)
    {
        _logger = logger;
        _httpClient = httpClient;
        _apiKey = configuration["AI:OpenAiApiKey"]
            ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        _modelId = configuration["AI:ModelId"] ?? "gpt-4o-mini";
        _fallbackService = new MockAiTriageService();
    }

    public async Task<AiTriageResult> AnalyzeAsync(string complaintText, AiTriageContext context)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            _logger.LogInformation("No OpenAI API key detected. Delegating triage to deterministic MockAiTriageService.");
            return await _fallbackService.AnalyzeAsync(complaintText, context);
        }

        try
        {
            var systemPrompt = BuildSystemPrompt(context);
            var userPrompt = $"Customer Complaint:\n\"{complaintText}\"";

            var requestBody = new
            {
                model = _modelId,
                response_format = new { type = "json_object" },
                messages = new object[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userPrompt }
                },
                temperature = 0.2
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);
            request.Content = new StringContent(JsonSerializer.Serialize(requestBody), System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var errorText = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("OpenAI API call returned status {StatusCode}: {Error}. Falling back to mock.", response.StatusCode, errorText);
                return await _fallbackService.AnalyzeAsync(complaintText, context);
            }

            var jsonResponse = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(jsonResponse);
            var contentString = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            if (string.IsNullOrWhiteSpace(contentString))
            {
                _logger.LogWarning("Empty response from AI service. Falling back to mock.");
                return await _fallbackService.AnalyzeAsync(complaintText, context);
            }

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            var parsed = JsonSerializer.Deserialize<AiTriageResult>(contentString, options);
            if (parsed != null && !string.IsNullOrWhiteSpace(parsed.DefectCategory))
            {
                _logger.LogInformation("Live AI Triage succeeded. Category: {Category}, Risk: {Risk}", parsed.DefectCategory, parsed.FraudRiskLevel);
                return parsed;
            }

            _logger.LogWarning("Deserialized AI response was null or invalid. Falling back to mock.");
            return await _fallbackService.AnalyzeAsync(complaintText, context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in live AI triage. Invoking deterministic fallback.");
            return await _fallbackService.AnalyzeAsync(complaintText, context);
        }
    }

    private static string BuildSystemPrompt(AiTriageContext ctx)
    {
        return $@"You are ReturnFlow AI, an enterprise Supply Chain & Reverse Logistics Auditor.
Your responsibility is to analyze unstructured customer return complaints, cross-reference them with SQL database facts, and produce a structured JSON triage decision.

### RELATIONAL DATABASE CONTEXT (Ground Truth)
- Order Number: {ctx.OrderNumber}
- Sales Channel: {ctx.SalesChannel}
- Product: {ctx.ProductName} (SKU: {ctx.Sku})
- Unit Price: INR {ctx.UnitPrice:N2}
- Order Date: {ctx.OrderDate:yyyy-MM-dd}
- Delivered Date: {ctx.DeliveredDate:yyyy-MM-dd}
- Days Since Delivery: {ctx.DaysSinceDelivery} days
- Standard 30-Day Return Window Active: {ctx.IsWithinReturnWindow}
- Hardware Warranty ({ctx.WarrantyDays} days) Active: {ctx.IsWithinWarrantyWindow}
- Customer-Reported Condition: {ctx.ConditionReportedByCustomer}
- Customer-Requested Resolution: {ctx.RequestedResolution}

### POLICY RULES
1. If Days Since Delivery > 30, general buyer remorse returns are strictly OUT OF POLICY. Only valid hardware defects, shipping damage, or wrong items are eligible under warranty.
2. If condition is 'OpenedUnused' within 30 days, recommend 'ApproveWithRestockingFee' (15% standard, 20% for B2B Wholesale).
3. If customer reports accidental user damage (e.g. dropping down stairs, physical impact) or makes threats of negative reviews/chargebacks, classify FraudRiskLevel as 'High' and RecommendedAction as 'EscalateToManualInspect' or 'RejectOutOfPolicy', with WarehouseRoutingAction as 'HoldInQuarantine'.
4. For legitimate defects within warranty, recommend 'ApproveReplacement' or 'ApproveFullRefund' with WarehouseRoutingAction 'SendToRefurbish'.

### REQUIRED JSON OUTPUT FORMAT
Respond ONLY with a valid JSON object matching this exact schema:
{{
  ""DefectCategory"": ""HardwareDefect"" | ""ShippingDamage"" | ""WrongItemSent"" | ""BuyerRemorse"" | ""WearAndTear"" | ""SuspectedAbuse"",
  ""SentimentScore"": <integer 1 to 10 where 10 is maximum anger/escalation>,
  ""FraudRiskLevel"": ""Low"" | ""Medium"" | ""High"",
  ""RecommendedAction"": ""ApproveFullRefund"" | ""ApproveReplacement"" | ""ApproveWithRestockingFee"" | ""RejectOutOfPolicy"" | ""EscalateToManualInspect"",
  ""WarehouseRoutingAction"": ""RestockToSellable"" | ""SendToRefurbish"" | ""ScrapDefect"" | ""HoldInQuarantine"",
  ""ReasoningSummary"": ""<Clear 2-3 sentence policy audit grounding reason in days since delivery and warranty>"",
  ""DraftedCustomerEmail"": ""<Professional, courteous customer-facing email explaining the decision and next steps>""
}}";
    }
}
