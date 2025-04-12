﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace BibliotecaAPI.Controllers;

[ApiController]
[Route("api/configuraciones")]
public class ConfiguracionesController : Controller
{
    private readonly IConfiguration configuration;
    private readonly ProcesamientoPago procesamientoPagos;
    private readonly IConfigurationSection seccion_01;
    private readonly IConfigurationSection seccion_02;
    private readonly PersonaOpciones _opcionesPersona;

    public ConfiguracionesController(IConfiguration configuration, IOptionsSnapshot<PersonaOpciones> opcionesPersona, ProcesamientoPago procesamientoPagos)
    {
        this.configuration = configuration;
        this.procesamientoPagos = procesamientoPagos;
        seccion_01 = configuration.GetSection("seccion_1");
        seccion_02 = configuration.GetSection("seccion_2");
        _opcionesPersona = opcionesPersona.Value;
    }

    [HttpGet("options-monitor")]
    public ActionResult GetTarifas()
    {
        return Ok(procesamientoPagos.ObtenerTarifas());
    }


    [HttpGet("seccion_1_opciones")]
    public ActionResult GetSeccion1Opciones()
    {
        return Ok(_opcionesPersona);
    }


    [HttpGet("proveedores")]
    public ActionResult GetProveedor()
    {
        var valor = configuration.GetValue<string>("quien_soy");
        return Ok(new { valor });
    }

    [HttpGet("obtenertodos")]
    public ActionResult GetObtenerTodos()
    {
        var hijos = seccion_02.GetChildren().Select(x => $"{x.Key}: {x.Value}");
        return Ok(new { hijos });
    }

    [HttpGet("seccion_01")]
    public ActionResult GetSeccion01()
    {
        var nombre = seccion_01.GetValue<string>("nombre");
        var edad = seccion_01.GetValue<string>("edad");

        return Ok(new { nombre, edad });
    }

    [HttpGet("seccion_02")]
    public ActionResult GetSeccion02()
    {
        var nombre = seccion_02.GetValue<string>("nombre");
        var edad = seccion_02.GetValue<string>("edad");

        return Ok(new { nombre, edad });
    }

    [HttpGet]
    public ActionResult<string> Get()
    {
        var opcion1 = configuration["apellidos"];
        var opcion2 = configuration.GetValue<string>("apellidos")!;
        return opcion2;
    }

    [HttpGet("secciones")]
    public ActionResult<string> GetSeccion()
    {
        var opcion1 = configuration["ConnectionStrings:DefaultConnection"];
        var opcion2 = configuration.GetValue<string>("ConnectionStrings:DefaultConnection");
        var seccion = configuration.GetSection("ConnectionStrings");
        var opcion3 = seccion["DefaultConnection"];
        return opcion3!;
    }
}
