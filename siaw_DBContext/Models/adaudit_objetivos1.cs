﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;

namespace siaw_DBContext.Models
{
    public partial class adaudit_objetivos1
    {
        public int? codauditoria { get; set; }
        public bool? evaluar { get; set; }
        public string codcliente { get; set; }
        public string tipo { get; set; }
        public string codgrupo_linea { get; set; }
        public string nivel_actual_leido { get; set; }
        public string nivel_actual { get; set; }
        public decimal? prom_cumplir { get; set; }
        public decimal? prom_subir { get; set; }
        public string nivel_subir { get; set; }
        public DateTime? fecha_inicio { get; set; }
        public DateTime? fecha_fin { get; set; }
        public string clasificacion { get; set; }
        public string clasificacion_abreviada { get; set; }
        public decimal? total_kg_actual { get; set; }
        public decimal? total_kg_mes { get; set; }
        public int? meses_promedia { get; set; }
        public int? frecuencia { get; set; }
        public decimal? resto { get; set; }
        public decimal? promedio_actual { get; set; }
        public decimal? porcentaje_logrado { get; set; }
        public string nivel_final { get; set; }
        public string obs { get; set; }
        public int? operacion { get; set; }
    }
}