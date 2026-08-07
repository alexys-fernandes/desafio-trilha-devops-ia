using HabitApp.Application.Mappers;
using HabitApp.Infrastructure.Data.Context;
using HabitApp.Infrastructure.IOC;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var envValues = LoadEnvironmentVariables(builder.Environment.ContentRootPath);
builder.Configuration.AddInMemoryCollection(envValues);
builder.Configuration.AddEnvironmentVariables();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<SqliteContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("SqliteConnection")));

builder.Services.AddAutoMapper(cfg => cfg.AddProfile<AutoMapperProfiles>());

ConfigurationIOC.ConfigureServices(builder.Services);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.WithOrigins(
                "http://localhost:4200",
                "http://localhost:4210",
                "https://localhost:4200")
              .SetIsOriginAllowed(origin => true)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddHttpsRedirection(options =>
    {
        options.RedirectStatusCode = StatusCodes.Status307TemporaryRedirect;
        options.HttpsPort = 7010;
    });
}

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors("AllowAngular");

app.UseAuthorization();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<SqliteContext>();
    await SqliteSchemaMigrator.MigrateAsync(context);
}

app.Run();

static Dictionary<string, string?> LoadEnvironmentVariables(string contentRootPath)
{
    var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
    var searchPaths = new List<string> { contentRootPath };

    var current = new DirectoryInfo(contentRootPath);
    while (current?.Parent is not null)
    {
        current = current.Parent;
        searchPaths.Add(current.FullName);
    }

    foreach (var directory in searchPaths.Distinct(StringComparer.OrdinalIgnoreCase))
    {
        foreach (var fileName in new[] { ".env", ".env.example" })
        {
            var fullPath = Path.Combine(directory, fileName);
            if (!File.Exists(fullPath))
            {
                continue;
            }

            foreach (var rawLine in File.ReadAllLines(fullPath))
            {
                var line = rawLine.Trim();
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#", StringComparison.Ordinal))
                {
                    continue;
                }

                var separatorIndex = line.IndexOf('=');
                if (separatorIndex < 0)
                {
                    continue;
                }

                var key = line[..separatorIndex].Trim();
                var value = line[(separatorIndex + 1)..].Trim();

                if ((value.StartsWith('"') && value.EndsWith('"')) || (value.StartsWith('\'') && value.EndsWith('\'')))
                {
                    value = value[1..^1];
                }

                values[key] = value;
                Environment.SetEnvironmentVariable(key, value);
            }
        }
    }

    return values;
}
