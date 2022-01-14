using arduino_ota_updater;
using idunno.Authentication.Basic;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

Log.Information("Starting up");

try
{
    Log.Information("Starting web host");
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((ctx, lc) => lc
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .ReadFrom.Configuration(ctx.Configuration));

    builder.Services.AddSingleton<IUserValidationService, UserValidationService>();
    builder.Services.AddAuthentication(BasicAuthenticationDefaults.AuthenticationScheme)
        .AddBasic(options =>
        {
            options.Realm = "arduino-ota-updater";
            options.Events = new BasicAuthenticationEvents
            {
                OnValidateCredentials = BasicAuth.ValidateCredentials
            };
        });
    builder.Services.AddAuthorization();

    var app = builder.Build();
    app.UseSerilogRequestLogging();

    if (app.Environment.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
    }
    app.UseSerilogRequestLogging();
    app.UseHttpsRedirection();
    app.UseAuthentication();
    app.UseAuthorization();

    var handler = new UpdateHandler();
    app.MapGet("/update", handler.Handle).RequireAuthorization();
    app.Run();
    return 0;
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host terminated unexpectedly");
    return 1;
}
finally
{
    Log.CloseAndFlush();
}
