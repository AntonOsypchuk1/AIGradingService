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

builder.Services.AddScoped<ILlmClient, FakeLlmClient>();

builder.Services.AddScoped<EvaluationPipelineService>();
builder.Services.AddScoped<EvaluationMetricsService>();

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