﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;

namespace siaw_DBContext.Models
{
    public partial class pepersona_cargo
    {
        public int? codmovto { get; set; }
        public int? codalm { get; set; }
        public int? codpersona { get; set; }
        public string nombre { get; set; }
        public decimal? codvendedor { get; set; }
        public int codcargo { get; set; }
        public string desccargo { get; set; }
        public decimal porcentaje { get; set; }
        public bool aprueba { get; set; }
        public bool? comision_fija { get; set; }
        public decimal comision_estandar { get; set; }
        public decimal salario_base { get; set; }
        public decimal comision_rendimiento { get; set; }
        public bool? comision_segun_rango { get; set; }
        public int? codrango_comision { get; set; }
        public string desc_rango { get; set; }
        public bool? comision_segun_tramo { get; set; }
        public int? codtramo_comision { get; set; }
        public string desc_tramo { get; set; }
        public int? codarea1 { get; set; }
        public decimal? porcen_tienda1 { get; set; }
        public decimal? porcen_externo1 { get; set; }
        public decimal? total_area1 { get; set; }
        public int? codarea2 { get; set; }
        public decimal? porcen_tienda2 { get; set; }
        public decimal? porcen_externo2 { get; set; }
        public decimal? total_area2 { get; set; }
        public int? codarea3 { get; set; }
        public decimal? porcen_tienda3 { get; set; }
        public decimal? porcen_externo3 { get; set; }
        public decimal? total_area3 { get; set; }
        public decimal? porcen_tienda4 { get; set; }
        public decimal? porcen_externo4 { get; set; }
        public decimal? total_area4 { get; set; }
    }
}