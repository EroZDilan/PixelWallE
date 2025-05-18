using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Configuración mínima
builder.Services.AddRazorPages(options =>
{
    // Desactivar Antiforgery temporalmente para aislar el problema
    options.Conventions.ConfigureFilter(new IgnoreAntiforgeryTokenAttribute());
});

// Agregar CORS para permitir peticiones AJAX si es necesario
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalhost", policy =>
    {
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// Configuración de entorno
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

// Configuración básica
app.UseHttpsRedirection();
app.UseStaticFiles(); // Esto servirá los archivos desde wwwroot
app.UseRouting();

// Habilitar CORS si es necesario
// app.UseCors("AllowLocalhost");

app.MapRazorPages();

app.Run();