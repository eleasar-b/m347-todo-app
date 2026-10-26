using Npgsql;
using TodoApi;

var builder = WebApplication.CreateBuilder(args);

// Port 8080, ausser er wird von aussen gesetzt (ASPNETCORE_HTTP_PORTS)
builder.WebHost.UseSetting("http_ports", builder.Configuration["HTTP_PORTS"] ?? "8080");

// Version aus der ENV APP_VERSION, z. B. per Build-Arg im Dockerfile gesetzt
var version = builder.Configuration["APP_VERSION"] ?? "dev";

// Mit ENV ConnectionStrings__Todo: PostgreSQL, sonst In-Memory
var connectionString = builder.Configuration.GetConnectionString("Todo");
ITodoStore store = string.IsNullOrWhiteSpace(connectionString)
    ? new InMemoryTodoStore()
    : new PostgresTodoStore(connectionString);

var app = builder.Build();
app.Logger.LogInformation("To-do-API {Version}, Speicher: {Storage}", version, store.Name);

if (store is PostgresTodoStore postgres)
{
    try
    {
        await postgres.EnsureTableAsync();
    }
    catch (NpgsqlException ex)
    {
        app.Logger.LogWarning("Datenbank beim Start nicht erreichbar: {Message}", ex.Message);
    }
}

// Datenbankfehler werden zu 503, die API stürzt nicht ab
app.Use(async (context, next) =>
{
    try
    {
        await next(context);
    }
    catch (NpgsqlException ex)
    {
        app.Logger.LogWarning("Datenbank nicht erreichbar: {Message}", ex.Message);
        await Results.Problem(title: "Datenbank nicht erreichbar", statusCode: 503).ExecuteAsync(context);
    }
});

app.MapGet("/health", async () =>
    await store.IsHealthyAsync()
        ? Results.Ok(new { status = "ok" })
        : Results.Problem(title: "Datenbank nicht erreichbar", statusCode: 503));

app.MapGet("/api/version", () => new { version, storage = store.Name, host = Environment.MachineName });

var todos = app.MapGroup("/api/todos");

todos.MapGet("", () => store.GetAllAsync());

todos.MapPost("", async (NewTodo input) =>
{
    if (string.IsNullOrWhiteSpace(input.Title))
    {
        return Results.Problem(title: "Titel fehlt", statusCode: 400);
    }

    var todo = await store.AddAsync(input.Title.Trim());
    return Results.Created($"/api/todos/{todo.Id}", todo);
});

todos.MapPut("/{id:int}", async (int id) =>
    await store.ToggleAsync(id) is Todo todo ? Results.Ok(todo) : Results.NotFound());

todos.MapDelete("/{id:int}", async (int id) =>
    await store.DeleteAsync(id) ? Results.NoContent() : Results.NotFound());

app.Run();

record NewTodo(string? Title);
