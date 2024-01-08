using System.Text.Json.Serialization;
using System.Text.Json;
using git_lfs_synchronizer.Services;

var builder = WebApplication.CreateBuilder(args);

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

builder.Services.AddSingleton<LfsService>();
builder.Services.AddHostedService<ClientService>();

var app = builder.Build();

app.MapControllers();

app.Run();
