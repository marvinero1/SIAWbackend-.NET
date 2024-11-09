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
using SIAW.Controllers;
using siaw_funciones;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();



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

// Agregar el middleware de conversión a mayúsculas
builder.Services.AddScoped<UppercaseMiddleware>();


// agregar servicios interfaces
//builder.Services.AddScoped<IDepositosCliente, Depositos_Cliente>();
//builder.Services.AddScoped<IVentas, Ventas>();
//builder.Services.AddScoped<IValidarVta, Validar_Vta>();

// OBTENER CLAVE ENCRIPTADA

var encryptedConnectionString = builder.Configuration["ConnectionStrings:pp"];
Console.WriteLine(encryptedConnectionString);

var encriptado = EncryptionHelper.EncryptString(encryptedConnectionString);
Console.WriteLine(encriptado);

var decryptedConnectionString = EncryptionHelper.DecryptString(encriptado);
Console.WriteLine(decryptedConnectionString);


var app = builder.Build();



if (app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// CORS siuempre arriba de toda instancia app.

app.UseCors("NuevaPolitica");
app.UseAuthentication();

// Agregar el middleware de cola de espera con un máximo de 1 solicitud concurrente
// app.UseRequestQueue(1);

app.UseMiddleware<UppercaseMiddleware>(); // Agregar aquí el middleware

// Registro del middleware en `Program.cs`
app.UseMiddleware<CustomExceptionMiddleware>();


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





//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// 07/11/2024 SE DETECTO UN ERROR GRACIAS A LOS LOGS DE NOTAS DE REMISION, ES EL SIGUIENTE:
// An exception has been raised that is likely due to a transient failure. Consider enabling transient error resiliency by adding 'EnableRetryOnFailure' to the 'UseSqlServer' call.
// EXISTE PROBLEMAS TRANSITORIOS CON EL SQL, PARA REINTENTAR SE AJUSTARA LA FABRICA DE CONTEXTOS CON EL SIGUIENTE METODO:
// ESTO A FIN DE QUE SE REINTENTE AUTOMATICAMENTE LOS REGISTROS.
//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
 
/*
public static class DbContextFactory
{
    public static DBContext Create(string connectionString)
    {
        var optionsBuilder = new DbContextOptionsBuilder<DBContext>();
        optionsBuilder.UseSqlServer(connectionString, sqlServerOptions =>
        {
            sqlServerOptions.EnableRetryOnFailure(
                maxRetryCount: 3,                  // Número máximo de reintentos
                maxRetryDelay: TimeSpan.FromSeconds(5),  // Tiempo entre reintentos
                errorNumbersToAdd: null            // Códigos de error opcionales
            );
        });

        return new DBContext(optionsBuilder.Options);
    }
}
*/

 
public static class DbContextFactory
{
    public static DBContext Create(string connectionString)
    {
        var optionsBuilder = new DbContextOptionsBuilder<DBContext>();
        optionsBuilder.UseSqlServer(connectionString);

        return new DBContext(optionsBuilder.Options);
    }
}

 







