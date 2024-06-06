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
    .WriteTo.Console() // Want to log within console and the file location underneath.
                       // How often a file should be created, which is daily (day) (Code below)
    .WriteTo.File("logs/cityinfo.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog(); // Ensures it is using Serilog to log information.

// Add services to the container.
builder.Services.AddControllers(options =>
{
    options.ReturnHttpNotAcceptable = true; // Lets the user know that the requested format is not acceptable
})  .AddNewtonsoftJson() // Replaced json formatters with json.net
    .AddXmlDataContractSerializerFormatters(); // Allows XML format to be returned if requested

builder.Services.AddProblemDetails(); // Ensures the user gets a user-friendly message.
/*builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = ctx =>
    {
        ctx.ProblemDetails.Extensions.Add("additionalInfo", "Additional Info Example");
        ctx.ProblemDetails.Extensions.Add("server", Environment.MachineName);
    };
});*/

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<FileExtensionContentTypeProvider>();

#if DEBUG
builder.Services.AddTransient<IMailService, LocalMailService>(); // Enables the local mailing service

#else
builder.Services.AddTransient<IMailService, CloudMailService>(); // Enabled the cloud mailing service

#endif
builder.Services.AddSingleton<CitiesDataStore>(); // Registers this for dependancy injection

builder.Services.AddDbContext<CityInfoContext>(dbContextOptions =>
    dbContextOptions.UseSqlite(builder.Configuration["ConnectionStrings:CityInfoDBConnectionString"])); // Registers this for dependancy injection

builder.Services.AddScoped<ICityInfoRepository, CityInfoRepository>(); // Registers the patterned repository

builder.Services.AddSingleton(new MapperConfiguration(m =>
{
    m.AddProfile(new CityProfile());
    m.AddProfile(new PointOfInterestProfile()); // Each profile class needs to be added like these two here.
}).CreateMapper()); // Different way to add Automapper compared to the below statement.

// builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies()); // Adds automapper.

builder.Services.AddAuthentication("Bearer").AddJwtBearer(options =>
{ // Register the services related to Bearer-token authentication
    options.TokenValidationParameters = new()
    {
        ValidateIssuer = true, 
        ValidateAudience = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Authentication:Issuer"], // Only tokens created by this API are valid
        ValidAudience = builder.Configuration["Authentication:Audience"], // Checks if the token is meant for this API
        IssuerSigningKey = new SymmetricSecurityKey(
               Convert.FromBase64String(builder.Configuration["Authentication:SecretForKey"]))
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("MustBeFromAntwerp", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireClaim("city", "Antwerp"); // Can do this with a team for example: "team", "Support"
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment()) // Is in Prod
{
    app.UseExceptionHandler();
}

if (app.Environment.IsDevelopment()) // Is in Development
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication(); // Checks whether the request is authenticated before going to the next part of Middleware

app.UseAuthorization();

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
});

app.Run();
