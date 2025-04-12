using BibliotecaAPI;
using BibliotecaAPI.Datos;
using BibliotecaAPI.Servicios;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json.Serialization;

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

            builder.Services.AddControllers().AddNewtonsoftJson();

            builder.Services.AddDbContext<ApplicationDbContext>(opciones => opciones.UseSqlServer("name=DefaultConnection"));

            builder.Services.AddIdentityCore<IdentityUser>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            builder.Services.AddScoped<UserManager<IdentityUser>>();
            builder.Services.AddScoped<SignInManager<IdentityUser>>();
            builder.Services.AddTransient<IServiciosUsuarios, ServiciosUsuarios>();

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

            var app = builder.Build();

            // área de middlewares

            app.MapControllers();

            app.Run();
        }
    }
}
