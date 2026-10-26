namespace TodoApi;

public record Todo(int Id, string Title, bool Done);

public interface ITodoStore
{
    string Name { get; }

    Task<IReadOnlyList<Todo>> GetAllAsync();

    Task<Todo> AddAsync(string title);

    Task<Todo?> ToggleAsync(int id);

    Task<bool> DeleteAsync(int id);

    Task<bool> IsHealthyAsync();
}

// Speichert die To-dos im Arbeitsspeicher. Nach einem Neustart ist alles weg.
public class InMemoryTodoStore : ITodoStore
{
    private readonly List<Todo> todos = [];
    private readonly Lock gate = new();
    private int nextId = 1;

    public string Name => "memory";

    public Task<IReadOnlyList<Todo>> GetAllAsync()
    {
        lock (gate)
        {
            return Task.FromResult<IReadOnlyList<Todo>>(todos.ToList());
        }
    }

    public Task<Todo> AddAsync(string title)
    {
        lock (gate)
        {
            var todo = new Todo(nextId++, title, false);
            todos.Add(todo);
            return Task.FromResult(todo);
        }
    }

    public Task<Todo?> ToggleAsync(int id)
    {
        lock (gate)
        {
            var index = todos.FindIndex(t => t.Id == id);
            if (index < 0)
            {
                return Task.FromResult<Todo?>(null);
            }

            todos[index] = todos[index] with { Done = !todos[index].Done };
            return Task.FromResult<Todo?>(todos[index]);
        }
    }

    public Task<bool> DeleteAsync(int id)
    {
        lock (gate)
        {
            return Task.FromResult(todos.RemoveAll(t => t.Id == id) > 0);
        }
    }

    public Task<bool> IsHealthyAsync()
    {
        return Task.FromResult(true);
    }
}
