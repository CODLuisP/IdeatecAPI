using System.IO.Compression;
using Microsoft.AspNetCore.ResponseCompression;
using IdeatecAPI.Infrastructure;
using Microsoft.OpenApi.Models; // ← AGREGAR
using MySqlConnector;
using QuestPDF.Infrastructure;

QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(opts =>
    {
        opts.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
    
// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// MODIFICAR AddSwaggerGen para incluir JWT
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "IdeatecAPI", 
        Version = "v1",
        Description = "API de Facturación Electrónica IDEATEC"
    });

    // Configuración de seguridad JWT para Swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Ingrese 'Bearer' seguido de un espacio y luego su token JWT.\n\nEjemplo: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Agregar Infrastructure (incluye JWT, Repositorios, Servicios)
builder.Services.AddInfrastructure(builder.Configuration);

// Robot de reintentos SUNAT en background
builder.Services.AddHostedService<IdeatecAPI.API.BackgroundServices.PendienteRetryWorker>();

// CORS para cualquier origen (Frontend)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.SetIsOriginAllowed(origin => true) // Permite cualquier origen dinámicamente
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Compresion de respuestas: el JSON del catalogo de productos viajaba sin
// comprimir (~1 MB con 1000 productos). Gzip/Brotli lo reducen alrededor del
// 90%, que es la diferencia entre tardar segundos y tardar un instante.
//
// GZIP VA PRIMERO A PROPOSITO. El proveedor que se registra antes gana la
// negociacion con el cliente, y Brotli aqui sale peor: MVC serializa el JSON
// por chunks y hace flush seguido, y Brotli emite un bloque nuevo en cada
// flush perdiendo el contexto de compresion. Medido contra
// /api/productos/18 (1137 KB sin comprimir): brotli 280 KB, gzip 93 KB.
// Los navegadores piden "gzip, deflate, br", asi que con Brotli primero
// estaban recibiendo el triple de bytes.
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<GzipCompressionProvider>();
    options.Providers.Add<BrotliCompressionProvider>();
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        new[] { "application/json", "application/json; charset=utf-8" });
});

// El thread pool arranca con pocos hilos y, cuando la app pasa un rato sin
// trafico, los suelta. Al llegar el primer request tiene que crear hilos otra
// vez y los inyecta de a uno o dos por segundo: por eso la primera peticion
// tras un reposo costaba ~1.5 s mientras las siguientes iban en ~350 ms.
//
// Esta caja lanza 3 lecturas del catalogo en paralelo y ademas el UnitOfWork
// abre su conexion de forma sincrona (bloquea un hilo), asi que necesita varios
// hilos disponibles de golpe. Con un minimo alto ya estan listos y no hay espera.
System.Threading.ThreadPool.SetMinThreads(workerThreads: 64, completionPortThreads: 64);

var app = builder.Build();


// Mantiene el pool de conexiones VIVO, no solo lo calienta al arrancar.
//
// Antes esto corria una sola vez. El problema: tras unos minutos sin trafico el
// pool descarta las conexiones ociosas y el servidor de DigitalOcean tambien
// cierra las suyas, asi que el primer request despues de un rato pagaba el
// saludo TCP+TLS completo — ~1.2 s contra los ~380 ms de una peticion normal.
// Justo el peor momento: el cajero abriendo la caja por la manana.
//
// Se abren varias en paralelo a proposito: el catalogo se trae en 3 lotes (ver
// ProductoRepository.LotesCatalogo) y ADEMAS el UnitOfWork abre la suya, asi que
// un request del catalogo necesita 4 conexiones, no 3.
//
// Calentar solo 3 fue un error: dejaba siempre una cuarta sin preparar, y tras
// un reposo esa se abria de cero contra DigitalOcean. Eso metia un pico de ~1.5 s
// en la primera peticion que antes de repartir en lotes no existia.
const int ConexionesCalientes = 5;   // 3 lotes + la del UnitOfWork + 1 de holgura
const int SegundosEntreLatidos = 60;

_ = Task.Run(async () =>
{
    await Task.Delay(500); // Esperar que la app termine de inicializar
    var connStrings = new[]
    {
        builder.Configuration.GetConnectionString("ProductionConnection"),
        builder.Configuration.GetConnectionString("BetaConnection")
    };

    while (true)
    {
        foreach (var cs in connStrings)
        {
            if (string.IsNullOrEmpty(cs)) continue;
            try
            {
                // Se abren todas a la vez y recien despues se cierran: si se
                // hicieran una por una, el pool reciclaria siempre la misma y
                // las otras dos seguirian muertas.
                var latidos = Enumerable.Range(0, ConexionesCalientes).Select(async _ =>
                {
                    var conn = new MySqlConnection(cs);
                    await conn.OpenAsync();
                    await using var cmd = new MySqlCommand("SELECT 1", conn);
                    await cmd.ExecuteScalarAsync();
                    return conn;
                }).ToArray();

                var abiertas = await Task.WhenAll(latidos);
                foreach (var conn in abiertas) await conn.DisposeAsync();
            }
            catch { /* Si la DB no responde, se reintenta en el proximo latido */ }
        }

        await Task.Delay(TimeSpan.FromSeconds(SegundosEntreLatidos));
    }
});

// [TEMPORAL - MEDICION] Va por ENCIMA de la compresion para que su cronometro
// incluya serializar el JSON, comprimirlo y escribirlo al socket. El controller
// deja en HttpContext.Items["perf"] lo que tardo la consulta y el mapeo, asi
// que restando se sabe cuanto se va en cada etapa:
//
//   total - (consulta + mapeo) = serializar + comprimir + escribir
//
// Quitar este bloque cuando se cierre el analisis.
app.Use(async (context, next) =>
{
    var cron = System.Diagnostics.Stopwatch.StartNew();
    await next();
    if (context.Items.TryGetValue("perf", out var detalle))
    {
        app.Logger.LogInformation(
            "[PERF] {Ruta} {Detalle} TOTAL={Total:F0}ms",
            context.Request.Path, detalle, cron.Elapsed.TotalMilliseconds);
    }
});

// Primer middleware del pipeline: para comprimir una respuesta hay que estar
// por encima de quien la escribe.
app.UseResponseCompression();

    app.UseSwagger();
    app.UseSwaggerUI();
 
app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
