﻿using BibliotecaAPI.NewFolder;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace BibliotecaAPI.Entidades;

public class Autor
{
    public int Id { get; set; }
    [Required(ErrorMessage = "El campo nombre es requerido")]
    [StringLength(150, ErrorMessage = "El campo {0} debe tener {1} caracteres o menos")]
    [PrimeraLetraMayuscula]
    public required string Nombres { get; set; }
    [Required(ErrorMessage = "El campo nombre es requerido")]
    [StringLength(20, ErrorMessage = "El campo {0} debe tener {1} caracteres o menos")]
    [PrimeraLetraMayuscula]
    public required string Apellidos { get; set; }
    public string? identificacion { get; set; }
    [Unicode(false)]
    public string? Foto { get; set; }
    public List<AutorLibro> Libros { get; set; } = [];
}
