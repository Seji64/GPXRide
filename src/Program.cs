using Append.Blazor.WebShare;
using BlazorDownloadFile;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using GPXRide;
using PatrickJahr.Blazor.FileHandling;
using PatrickJahr.Blazor.PwaUpdate;
using Serilog;
using Serilog.Core;
using Serilog.Events;

WebAssemblyHostBuilder builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

LoggingLevelSwitch levelSwitch = new()
{
    MinimumLevel = LogEventLevel.Debug
};

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.ControlledBy(levelSwitch)
    .WriteTo.BrowserConsole()
    .CreateLogger();

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddMudServices();
builder.Services.AddWebShare();
builder.Services.AddBlazorDownloadFile();
builder.Services.AddFileHandlingService();
builder.Services.AddUpdateService();

await builder.Build().RunAsync();

