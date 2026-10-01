using ReturnFlow.Core.Interfaces;
using ReturnFlow.Core.Services;

namespace ReturnFlow.Web.Extensions;

public static class AiServiceExtensions
{
    public static IServiceCollection AddAiTriage(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpClient();

        var openAiKey = configuration["AI:OpenAiApiKey"]
            ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY");

        if (!string.IsNullOrWhiteSpace(openAiKey))
        {
            services.AddScoped<IAiTriageService, SemanticKernelTriageService>();
        }
        else
        {
            services.AddScoped<IAiTriageService, MockAiTriageService>();
        }

        return services;
    }
}
