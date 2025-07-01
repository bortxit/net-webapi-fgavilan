using BibliotecaAPI.Datos;
using BibliotecaAPI.Entidades;
using BibliotecaAPI.Servicios;
using BibliotecaAPI.Swagger;
using BibliotecaAPI.Utilidades;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Text;

namespace BibliotecaAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            var diccionarioConfiguraciones = new Dictionary<string, string>
            {
                { "quien_soy", "un diccionario en memoria" }
            };

            builder.Configuration.AddInMemoryCollection(diccionarioConfiguraciones!);

            //área de servicios

            //builder.Services.AddOutputCache(opciones =>
            //{
            //    opciones.DefaultExpirationTimeSpan = TimeSpan.FromSeconds(60);
            //});

            builder.Services.AddStackExchangeRedisOutputCache(opciones =>
            {
                opciones.Configuration = builder.Configuration.GetConnectionString("redis");
            });

            var origenesPermitidos = builder.Configuration.GetSection("origenesPermitidos").Get<string[]>()!;

            builder.Services.AddCors(opciones =>
            {
                opciones.AddDefaultPolicy(opcionesCORS =>
                {
                    opcionesCORS.WithOrigins(origenesPermitidos).AllowAnyMethod().AllowAnyHeader()
                    .WithExposedHeaders("cantidad-total-registros");
                });
            });

            builder.Services.AddOptions<PersonaOpciones>()
                .Bind(builder.Configuration.GetSection(PersonaOpciones.Seccion))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            builder.Services.AddOptions<TarifaOpciones>()
                .Bind(builder.Configuration.GetSection(TarifaOpciones.Seccion))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            builder.Services.AddSingleton<ProcesamientoPago>();

            builder.Services.AddAutoMapper(typeof(Program));

            builder.Services.AddControllers(opciones =>
            {
                opciones.Filters.Add<FiltroTiempoEjecucion>();
            }).AddNewtonsoftJson();

            builder.Services.AddDbContext<ApplicationDbContext>(opciones => opciones.UseSqlServer("name=DefaultConnection"));

            builder.Services.AddIdentityCore<Usuario>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            builder.Services.AddScoped<UserManager<Usuario>>();
            builder.Services.AddScoped<SignInManager<Usuario>>();
            builder.Services.AddTransient<IServiciosUsuarios, ServiciosUsuarios>();
            //builder.Services.AddTransient<IAlmacenadorArchivos, AlmacenadorArchivosAzure>();
            builder.Services.AddTransient<IAlmacenadorArchivos, AlmacenadorArchivosLocal>();
            builder.Services.AddScoped<MiFiltroDeAccion>();
            builder.Services.AddScoped<FiltroValidacionLibro>();
            builder.Services.AddScoped<BibliotecaAPI.Servicios.V1.IServicioAutores, BibliotecaAPI.Servicios.V1.ServicioAutores>();

            builder.Services.AddHttpContextAccessor();

            builder.Services.AddAuthentication().AddJwtBearer(opciones =>
            {
                opciones.MapInboundClaims = false;
                opciones.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["llavejwt"]!)),
                    ClockSkew = TimeSpan.Zero
                };
            });

            builder.Services.AddAuthorization(opciones =>
            {
                opciones.AddPolicy("esadmin", politica => politica.RequireClaim("esadmin"));
            });

            builder.Services.AddSwaggerGen(opciones =>
            {
                opciones.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
                {
                    Version = "v1",
                    Title = "Biblioteca API",
                    Description = "Este es un web api para trabajar con datos de autores y libros",
                    Contact = new Microsoft.OpenApi.Models.OpenApiContact
                    {
                        Email = "felipe@hotmail.com",
                        Name = "Felipe Gavilán",
                        Url = new Uri("https://gavilan.blog")
                    },
                    License = new Microsoft.OpenApi.Models.OpenApiLicense
                    {
                        Name = "MIT",
                        Url = new Uri("https://opensource.org/license/mit/")
                    }
                });

                opciones.SwaggerDoc("v2", new Microsoft.OpenApi.Models.OpenApiInfo
                {
                    Version = "v2",
                    Title = "Biblioteca API",
                    Description = "Este es un web api para trabajar con datos de autores y libros",
                    Contact = new Microsoft.OpenApi.Models.OpenApiContact
                    {
                        Email = "felipe@hotmail.com",
                        Name = "Felipe Gavilán",
                        Url = new Uri("https://gavilan.blog")
                    },
                    License = new Microsoft.OpenApi.Models.OpenApiLicense
                    {
                        Name = "MIT",
                        Url = new Uri("https://opensource.org/license/mit/")
                    }
                });

                //Filtro que agrupa por namespace V1 y V2
                opciones.DocInclusionPredicate((documentName, apiDescription) =>
                {
                    if (!apiDescription.TryGetMethodInfo(out var methodInfo))
                        return false;

                    var ns = methodInfo.DeclaringType?.Namespace ?? string.Empty;

                    return ns.Contains($".V{documentName}", StringComparison.OrdinalIgnoreCase);
                });

                opciones.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header
                });

                opciones.OperationFilter<FiltroAutorizacion>();

                //opciones.AddSecurityRequirement(new OpenApiSecurityRequirement
                //{
                //    {
                //        new OpenApiSecurityScheme
                //        {
                //            Reference = new OpenApiReference
                //            {
                //                Type = ReferenceType.SecurityScheme,
                //                Id = "Bearer"
                //            }
                //        },
                //        new string[]{ }
                //    }
                //});
            });

            var app = builder.Build();

            // área de middlewares

            app.UseExceptionHandler(exceptionHandlerApp => exceptionHandlerApp.Run(async context =>
            {
                var exceptionHandlerFeature = context.Features.Get<IExceptionHandlerFeature>();
                var excepcion = exceptionHandlerFeature?.Error!;

                var error = new Error()
                {
                    MensajeDeError = excepcion.Message,
                    StackTrace = excepcion.StackTrace,
                    Fecha = DateTime.UtcNow
                };

                var dbContext = context.RequestServices.GetRequiredService<ApplicationDbContext>();
                dbContext.Add(error);
                await dbContext.SaveChangesAsync();
                await Results.InternalServerError(new
                {
                    tipo = "error",
                    mensaje = "Ha ocurrido un error inesperado",
                    estatus = 500
                }).ExecuteAsync(context);
            }));
            app.UseSwagger();
            app.UseSwaggerUI(opciones =>
            {
                opciones.SwaggerEndpoint("/swagger/v1/swagger.json", "Biblioteca API V1");
                opciones.SwaggerEndpoint("/swagger/v2/swagger.json", "Biblioteca API V2");
            });

            app.UseStaticFiles();

            app.UseCors();

            app.UseOutputCache();

            app.MapControllers();

            app.Run();
        }
    }
}
