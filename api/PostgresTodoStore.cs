using Npgsql;

namespace TodoApi;

// Speichert die To-dos in PostgreSQL. Die Tabelle wird automatisch angelegt.
public class PostgresTodoStore : ITodoStore
{
    private const string CreateTableSql = """
        CREATE TABLE IF NOT EXISTS todos (
            id    SERIAL  PRIMARY KEY,
            title TEXT    NOT NULL,
            done  BOOLEAN NOT NULL DEFAULT FALSE
        )
        """;

    private readonly NpgsqlDataSource dataSource;
    private bool tableReady;

    public PostgresTodoStore(string connectionString)
    {
        dataSource = NpgsqlDataSource.Create(connectionString);
    }

    public string Name => "postgres";

    // Wird beim Start aufgerufen. Ist die DB dann noch nicht bereit,
    // holt der erste Zugriff das Anlegen nach.
    public async Task EnsureTableAsync()
    {
        if (tableReady)
        {
            return;
        }

        await using var command = dataSource.CreateCommand(CreateTableSql);
        await command.ExecuteNonQueryAsync();
        tableReady = true;
    }

    public async Task<IReadOnlyList<Todo>> GetAllAsync()
    {
        await EnsureTableAsync();
        await using var command = dataSource.CreateCommand("SELECT id, title, done FROM todos ORDER BY id");
        return await ReadTodosAsync(command);
    }

    public async Task<Todo> AddAsync(string title)
    {
        await EnsureTableAsync();
        await using var command = dataSource.CreateCommand("INSERT INTO todos (title) VALUES ($1) RETURNING id, title, done");
        command.Parameters.AddWithValue(title);
        return (await ReadTodosAsync(command))[0];
    }

    public async Task<Todo?> ToggleAsync(int id)
    {
        await EnsureTableAsync();
        await using var command = dataSource.CreateCommand("UPDATE todos SET done = NOT done WHERE id = $1 RETURNING id, title, done");
        command.Parameters.AddWithValue(id);
        return (await ReadTodosAsync(command)).FirstOrDefault();
    }

    public async Task<bool> DeleteAsync(int id)
    {
        await EnsureTableAsync();
        await using var command = dataSource.CreateCommand("DELETE FROM todos WHERE id = $1");
        command.Parameters.AddWithValue(id);
        return await command.ExecuteNonQueryAsync() > 0;
    }

    public async Task<bool> IsHealthyAsync()
    {
        try
        {
            await using var command = dataSource.CreateCommand("SELECT 1");
            await command.ExecuteScalarAsync();
            return true;
        }
        catch (NpgsqlException)
        {
            return false;
        }
    }

    private static async Task<List<Todo>> ReadTodosAsync(NpgsqlCommand command)
    {
        var result = new List<Todo>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result.Add(new Todo(reader.GetInt32(0), reader.GetString(1), reader.GetBoolean(2)));
        }

        return result;
    }
}
