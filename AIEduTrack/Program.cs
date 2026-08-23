using AIEduTrack.Data;
using AIEduTrack.Services;
using AIEduTrack.Services.Agents;
using AIEduTrack.Services.LLM;
using AIEduTrack.Services.Report;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Регистрация обычного HTTP клиента
builder.Services.AddHttpClient();

// Регистрация специального HTTP клиента для GigaChat (Игнорирует ошибки сертификатов)
builder.Services.AddHttpClient("GigaChatClient")
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
    });

// Регистрация наших сервисов
builder.Services.AddScoped<ILLMFactory, AIEduTrack.Services.LLM.LLMFactory>();
builder.Services.AddScoped<ILlmSettingsService, AIEduTrack.Services.LLM.LlmSettingsService>();

builder.Services.AddScoped<TrajectoryOrchestrator>();
builder.Services.AddScoped<IContextAnalyzerAgent, ContextAnalyzerAgent>();
builder.Services.AddScoped<ITrajectoryCuratorAgent, TrajectoryCuratorAgent>();
builder.Services.AddScoped<IValidatorAgent, ValidatorAgent>();
builder.Services.AddScoped<IExplainerAgent, ExplainerAgent>();
builder.Services.AddScoped<IBenchmarkService, BenchmarkService>();
builder.Services.AddSingleton<IDataRepository, ExcelDataRepository>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
