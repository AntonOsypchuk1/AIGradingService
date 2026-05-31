using AIGradingService.Api.Services;
using AIGradingService.Api.Services.Baselines;
using AIGradingService.Api.Services.Llm;
using AIGradingService.Api.Services.Pipeline;
using AIGradingService.Api.Services.StaticAnalysis;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<DatasetService>();
builder.Services.AddScoped<PythonExecutionService>();
builder.Services.AddScoped<PythonStaticAnalysisService>();

builder.Services.AddScoped<RuleBasedGradingService>();

builder.Services.AddScoped<IEvaluationBaseline, EqualWeightRuleBasedBaseline>();
builder.Services.AddScoped<IEvaluationBaseline, WeightedRuleBasedBaseline>();
builder.Services.AddScoped<IEvaluationBaseline, StaticAnalysisRuleBasedBaseline>();
builder.Services.AddScoped<IEvaluationBaseline, HybridRuleBasedBaseline>();
builder.Services.AddScoped<IEvaluationBaseline, LlmOnlyBaseline>();
builder.Services.AddScoped<IEvaluationBaseline, TestAwareLlmBaseline>();

builder.Services.AddScoped<EvaluationPipelineService>();
builder.Services.AddScoped<EvaluationMetricsService>();

builder.Services.Configure<OpenRouterOptions>(
    builder.Configuration.GetSection("OpenRouter"));

var llmProvider = builder.Configuration["Llm:Provider"];

if (string.Equals(llmProvider, "OpenRouter", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddHttpClient<ILlmClient, OpenRouterLlmClient>();
}
else
{
    builder.Services.AddScoped<ILlmClient, FakeLlmClient>();
}

var app = builder.Build();

var datasetService = app.Services.GetRequiredService<DatasetService>();
datasetService.Load();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.Run();