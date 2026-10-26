// Alle Aufrufe gehen relativ an /api. nginx leitet sie an den Container "api" weiter.

const list = document.querySelector("#todos");
const empty = document.querySelector("#empty");
const error = document.querySelector("#error");
const form = document.querySelector("#new-todo");
const title = document.querySelector("#title");

async function api(path, options) {
  const response = await fetch(`/api${path}`, options);
  if (!response.ok) {
    throw new Error(messageFor(response.status));
  }
  return response.status === 204 ? null : response.json();
}

function messageFor(status) {
  if (status === 503) {
    return "Datenbank nicht erreichbar.";
  }
  if (status === 502 || status === 504) {
    return "API nicht erreichbar.";
  }
  return `Fehler von der API (HTTP ${status}).`;
}

function showError(err) {
  error.textContent = err ? err.message : "";
  error.hidden = !err;
}

function render(todos) {
  list.replaceChildren();
  for (const todo of todos) {
    const item = document.createElement("li");
    item.classList.toggle("done", todo.done);

    const checkbox = document.createElement("input");
    checkbox.type = "checkbox";
    checkbox.checked = todo.done;
    checkbox.addEventListener("change", () => run(() => api(`/todos/${todo.id}`, { method: "PUT" })));

    const text = document.createElement("span");
    text.textContent = todo.title;

    const remove = document.createElement("button");
    remove.textContent = "Löschen";
    remove.addEventListener("click", () => run(() => api(`/todos/${todo.id}`, { method: "DELETE" })));

    item.append(checkbox, text, remove);
    list.append(item);
  }
  empty.hidden = todos.length > 0;
}

// Aktion ausführen und danach die Liste neu laden
async function run(action) {
  try {
    if (action) {
      await action();
    }
    render(await api("/todos"));
    showError(null);
  } catch (err) {
    showError(err);
  }
}

async function loadVersion() {
  const badge = document.querySelector("#version");
  try {
    const info = await api("/version");
    badge.textContent = `API ${info.version} · ${info.storage}`;
    badge.title = `Host: ${info.host}`;
  } catch {
    badge.textContent = "API: –";
  }
}

form.addEventListener("submit", (event) => {
  event.preventDefault();
  const value = title.value.trim();
  if (!value) {
    return;
  }
  run(async () => {
    await api("/todos", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ title: value }),
    });
    title.value = "";
  });
});

loadVersion();
run();
