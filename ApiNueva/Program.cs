using Microsoft.OpenApi.Models;
using UserManagementAPI.Models;

var builder = WebApplication.CreateBuilder(args);

// ===== Configuración de servicios =====
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "UserManagementAPI",
        Version = "v1",
        Description = "API para gestión de usuarios del departamento HR/IT",
        Contact = new OpenApiContact
        {
            Name = "Tu Nombre",
            Email = "tu.email@example.com"
        }
    });

    // Configuración para detectar endpoints en Minimal APIs
    c.TagActionsBy(api => new[] { api.RelativePath?.Split('/')[1] ?? "Default" });
    c.DocInclusionPredicate((name, api) => true);
});

// Base de datos en memoria (simulación)
builder.Services.AddSingleton<List<User>>(new List<User>
{
    new User { Id = 1, FirstName = "John", LastName = "Doe", Email = "john.doe@hr.com", Department = "HR" },
    new User { Id = 2, FirstName = "Jane", LastName = "Smith", Email = "jane.smith@it.com", Department = "IT" }
});

var app = builder.Build();

// ===== Configuración del pipeline =====
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "UserManagementAPI v1");
        c.RoutePrefix = "docs"; // Opcional: cambia la ruta de /swagger a /docs
    });
}

app.UseHttpsRedirection();

// ===== Endpoints CRUD =====
var usersGroup = app.MapGroup("/api/users").WithTags("Users");

// GET /api/users - Obtener todos los usuarios
usersGroup.MapGet("/", (List<User> users) => 
{
    return Results.Ok(users);
})
.WithName("GetAllUsers")
.Produces<List<User>>(StatusCodes.Status200OK)
.WithOpenApi();

// GET /api/users/{id} - Obtener usuario por ID
usersGroup.MapGet("/{id}", (int id, List<User> users) =>
{
    var user = users.FirstOrDefault(u => u.Id == id);
    return user is null ? Results.NotFound() : Results.Ok(user);
})
.WithName("GetUserById")
.Produces<User>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status404NotFound)
.WithOpenApi();

// POST /api/users - Crear nuevo usuario
usersGroup.MapPost("/", (User user, List<User> users) =>
{
    // Validación básica
    if (string.IsNullOrEmpty(user.Email))
    {
        return Results.BadRequest("El email es requerido");
    }

    if (users.Any(u => u.Email == user.Email))
    {
        return Results.Conflict("El email ya está registrado");
    }

    user.Id = users.Max(u => u.Id) + 1;
    users.Add(user);
    return Results.Created($"/api/users/{user.Id}", user);
})
.WithName("CreateUser")
.Produces<User>(StatusCodes.Status201Created)
.Produces(StatusCodes.Status400BadRequest)
.Produces(StatusCodes.Status409Conflict)
.WithOpenApi();

// PUT /api/users/{id} - Actualizar usuario existente
usersGroup.MapPut("/{id}", (int id, User updatedUser, List<User> users) =>
{
    var user = users.FirstOrDefault(u => u.Id == id);
    if (user is null)
    {
        return Results.NotFound();
    }

    user.FirstName = updatedUser.FirstName ?? user.FirstName;
    user.LastName = updatedUser.LastName ?? user.LastName;
    user.Email = updatedUser.Email ?? user.Email;
    user.Department = updatedUser.Department ?? user.Department;

    return Results.NoContent();
})
.WithName("UpdateUser")
.Produces(StatusCodes.Status204NoContent)
.Produces(StatusCodes.Status404NotFound)
.WithOpenApi();

// DELETE /api/users/{id} - Eliminar usuario
usersGroup.MapDelete("/{id}", (int id, List<User> users) =>
{
    var user = users.FirstOrDefault(u => u.Id == id);
    if (user is null)
    {
        return Results.NotFound();
    }

    users.Remove(user);
    return Results.NoContent();
})
.WithName("DeleteUser")
.Produces(StatusCodes.Status204NoContent)
.Produces(StatusCodes.Status404NotFound)
.WithOpenApi();

// Endpoint básico de prueba
app.MapGet("/", () => "UserManagementAPI está funcionando correctamente!")
    .ExcludeFromDescription(); // Oculta este endpoint en Swagger

app.Run();