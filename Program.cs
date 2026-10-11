using System.Text.Json.Serialization;
using PatientVitalsPrediction.Simple;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<RiskPredictionService>();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

var app = builder.Build();

// Force the model to train immediately on startup (instead of on first request)
// so the first person to use the page doesn't wait for training.
_ = app.Services.GetRequiredService<RiskPredictionService>();

app.UseDefaultFiles();   // serves wwwroot/index.html at "/"
app.UseStaticFiles();

app.MapPost("/api/predict", (PatientInputFile input, RiskPredictionService svc) =>
{
    try
    {
        var result = svc.RunFullPrediction(input);
        return Results.Ok(result);
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.Run();
