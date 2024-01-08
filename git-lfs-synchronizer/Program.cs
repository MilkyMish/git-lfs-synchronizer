using System.Text.Json.Serialization;
using System.Text.Json;
using git_lfs_synchronizer.Services;
using git_lfs_synchronizer.Configuration;
using git_lfs_synchronizer.Heplers;

var configurationFile = $"etc{Path.DirectorySeparatorChar}config.json";

if (!JsonUtils.CheckJsonFile(configurationFile))
{
    throw new Exception($"Incorrect configuration file: {configurationFile}");
}

var builder = WebApplication.CreateBuilder(args);

var config = new MainConfiguration();
builder.Configuration.AddJsonFile(configurationFile, true, true);
builder.Configuration.Bind(config);
builder.Services.AddSingleton(config);

builder.Services.AddControllers().AddJsonOptions(o =>
{
    o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
});

var serializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
{
    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    Converters =
    {
        new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
    }
};

builder.Services.AddSingleton(serializerOptions);
builder.Services.AddSingleton<LfsService>();
builder.Services.AddHostedService<ClientService>();


builder.Services.AddHttpClient("GitLfsSynchronizerClient", client =>
{
    var uriBuilder = new UriBuilder(config.Repos.First().Url)
    {
        Scheme = Uri.UriSchemeHttp
    };

    client.BaseAddress = uriBuilder.Uri;
});

builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

app.MapControllers();

app.Run();
