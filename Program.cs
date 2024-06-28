using AutoMapper;
using CityInfo.API;
using CityInfo.API.DbContexts;
using CityInfo.API.Profiles;
using CityInfo.API.Services;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    // This allows real-time logging to the console, useful for monitoring during development.
    .WriteTo.File("logs/cityinfo.txt", rollingInterval: RollingInterval.Day)
    // Log output to a file, with a new file created daily.
    .CreateLogger(); // Create the logger instance with the configured settings.

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog(); // Use Serilog as the logging provider for the application.

// Add services to the dependency injection container.
builder.Services.AddControllers(options =>
{
    options.ReturnHttpNotAcceptable = true; // Return a 406 Not Acceptable response if the request format is unsupported.
})
.AddNewtonsoftJson() // Use Newtonsoft.Json for JSON serialization, which offers more flexibility and features compared to the default JSON serializer.
.AddXmlDataContractSerializerFormatters(); // Add support for XML serialization and deserialization.

builder.Services.AddProblemDetails(); // Add support for returning problem details in HTTP responses.
/*builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = ctx =>
    {
        ctx.ProblemDetails.Extensions.Add("additionalInfo", "Additional Info Example");
        ctx.ProblemDetails.Extensions.Add("server", Environment.MachineName);
    };
});*/
// Optional customization for ProblemDetails to include additional context like server name or custom information.

builder.Services.AddEndpointsApiExplorer(); // For endpoint discovery.
builder.Services.AddSwaggerGen(); // Add Swagger for generating API documentation and providing an interactive API interface.
builder.Services.AddSingleton<FileExtensionContentTypeProvider>(); // Register a singleton service to map file extensions to MIME types.

#if DEBUG
builder.Services.AddTransient<IMailService, LocalMailService>(); // Use LocalMailService for sending emails in a development environment.
#else
builder.Services.AddTransient<IMailService, CloudMailService>(); // Use CloudMailService for sending emails in a production environment.
#endif

builder.Services.AddSingleton<CitiesDataStore>(); // Register CitiesDataStore as a singleton for dependency injection.
// Singleton ensures there is only one instance of CitiesDataStore throughout the application lifecycle.

builder.Services.AddDbContext<CityInfoContext>(dbContextOptions =>
    dbContextOptions.UseSqlite(builder.Configuration["ConnectionStrings:CityInfoDBConnectionString"]));
// Register CityInfoContext for dependency injection with SQLite database configuration.
// The connection string is retrieved from the apps config settings.

builder.Services.AddScoped<ICityInfoRepository, CityInfoRepository>(); // Register CityInfoRepository with scoped lifetime.
// Scoped lifetime ensures a new instance of the repository is created for each HTTP request.

builder.Services.AddSingleton(new MapperConfiguration(m =>
{
    m.AddProfile(new CityProfile());
    m.AddProfile(new PointOfInterestProfile());
    // Register AutoMapper profiles for mapping between domain models and DTOs.
}).CreateMapper());

// builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
// Alternative way to register AutoMapper by scanning all assemblies for profiles.

builder.Services.AddAuthentication("Bearer").AddJwtBearer(options =>
{
    // Configure JWT Bearer token authentication.
    options.TokenValidationParameters = new()
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Authentication:Issuer"], // The expected issuer of the token.
        ValidAudience = builder.Configuration["Authentication:Audience"], // The expected audience of the token.
        IssuerSigningKey = new SymmetricSecurityKey(
            Convert.FromBase64String(builder.Configuration["Authentication:SecretForKey"]))
        // The secret key used to sign the token, converted from a Base64 string.
    };
});
// Add JWT Bearer authentication, which validates the token and ensures it is issued by a trusted source.

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("MustBeFromAntwerp", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireClaim("city", "Antwerp"); // Require that the user's token contains a claim with the key "city" and value "Antwerp".
    });
});

var app = builder.Build(); // Build the application pipeline.

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler(); // Use a global exception handler for non-development environments.
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger(); // Enable Swagger for API documentation in the development environment.
    app.UseSwaggerUI(); // Enable the Swagger UI for interactive API exploration.
}

app.UseHttpsRedirection(); // Redirect HTTP requests to HTTPS.

app.UseRouting(); // Enable routing for the application.

app.UseAuthentication(); // Enable authentication middleware to check for authenticated requests.

app.UseAuthorization(); // Enable authorization middleware to check for authorization policies.

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers(); // Map attribute-routed controllers to endpoints.
    // Ensures that the endpoints configured by the controllers are available to handle requests.
});

app.Run();
