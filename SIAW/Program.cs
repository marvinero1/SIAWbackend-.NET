using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.IdentityModel.Tokens;
//using SIAW.Data;
using siaw_DBContext.Data;
using System.Text;
using SIAW;
using System.Security.Claims;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using siaw_DBContext;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//builder.Services.AddDbContext<SIAW.Data.PSContext>(
//    options =>
//    {
//        options.UseSqlServer(builder.Configuration.GetConnectionString("PS_DB"));
//    });


builder.Services.AddCors(options =>
{
    options.AddPolicy("NuevaPolitica", app =>
    {
        app.AllowAnyOrigin()
        .AllowAnyMethod()
        .AllowAnyHeader();
    });
});

//builder.Services.AddScoped<ICustomDbContextFactory, CustomDbContextFactory>();
//builder.Services.AddHttpContextAccessor(); // Agregar esta línea

builder.Services.Configure<IISServerOptions>(options =>
{
    options.AllowSynchronousIO = true; // Esto puede ser necesario en algunas situaciones
    options.MaxRequestBodySize = null;
});


builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JWT:Key"])),
            ValidateIssuer = false,
            ValidateAudience = false
        };
    });

builder.Services.AddSingleton<UserConnectionManager>();


var app = builder.Build();


// Configure the HTTP request pipeline.
/*if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}*/



if (app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// CORS siuempre arriba de toda instancia app.

app.UseCors("NuevaPolitica");
app.UseAuthentication();


app.UseHttpsRedirection();



app.UseAuthorization();

app.MapControllers();





app.Run();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    try
    {
        // Obtener la fábrica de contextos personalizada
        var dbContextFactory = services.GetRequiredService<ICustomDbContextFactory>();

        // Usar la fábrica de contextos para crear un contexto y trabajar con él
        var dbContext = dbContextFactory.Create();

        // Realizar operaciones con el contexto
        // ...

        // Asegurarse de cerrar el contexto cuando ya no se necesita
        dbContext.Dispose();
    }
    catch (Exception ex)
    {
        // Manejar errores
        Console.WriteLine(ex.Message);
    }
}

public static class DbContextFactory
{
    public static DBContext Create(string connectionString)
    {
        var optionsBuilder = new DbContextOptionsBuilder<DBContext>();
        optionsBuilder.UseSqlServer(connectionString);

        return new DBContext(optionsBuilder.Options);
    }
}
/*
public static class DbContextFactory2
{
    public static DBContext Create(ClaimsPrincipal user)
    {
        var connectionStringClaim = user?.FindFirst("ConnectionString");
        var connectionString = connectionStringClaim?.Value;

        if (string.IsNullOrEmpty(connectionString))
        {
            throw new Exception("No se ha proporcionado una cadena de conexión válida.");
        }

        var optionsBuilder = new DbContextOptionsBuilder<DBContext>();
        optionsBuilder.UseSqlServer(connectionString);

        return new DBContext(optionsBuilder.Options);
    }
}
*/
