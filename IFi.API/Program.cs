using IFi.API;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
string accessKey = builder.Configuration["access_key"];
builder.Services.AddHttpClient(Constants.MarketStackClientName, client => client.BaseAddress = new Uri("https://api.marketstack.com/v1/"))
    .ConfigurePrimaryHttpMessageHandler(() => new HttpMessageHandler_QueryStringParams(new() { { "access_key", accessKey } }));

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

//app.UseAuthorization();

app.MapControllers();

app.Run();
