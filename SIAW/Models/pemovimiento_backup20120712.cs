﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;

namespace SIAW.Models
{
    public partial class pemovimiento_backup20120712
    {
        public int codigo { get; set; }
        public int codpersona { get; set; }
        public int tipo { get; set; }
        public DateTime? fechainicio { get; set; }
        public string razon { get; set; }
        public int? codalm { get; set; }
        public int codcargo { get; set; }
        public string observacion { get; set; }
        public decimal porcentaje { get; set; }
        public string horareg { get; set; }
        public DateTime fechareg { get; set; }
        public string usuarioreg { get; set; }
        public decimal? salariobase { get; set; }
        public string monsalbase { get; set; }
        public decimal? salariodesglozado { get; set; }
        public string monedasaldes { get; set; }
        public decimal? comisionest { get; set; }
        public string moncomest { get; set; }
        public decimal? salarioact { get; set; }
        public string monsact { get; set; }
        public decimal? comisionact { get; set; }
        public string moncact { get; set; }
        public decimal? portiendaarea { get; set; }
        public decimal? portiendanac { get; set; }
        public decimal? poralmarea { get; set; }
        public decimal? poralmnac { get; set; }
        public decimal? poralmacen { get; set; }
        public bool aprueba { get; set; }
        public int? codtipocontra { get; set; }
        public bool estado { get; set; }
        public bool hayfechafin { get; set; }
        public DateTime? fechafin { get; set; }
        public int? tipofin { get; set; }
        public string motivofin { get; set; }
        public decimal? porhrsextras { get; set; }
        public bool? dsdmediodia { get; set; }
        public int? codcontrato { get; set; }
        public bool? comision_fija { get; set; }
        public int? codtramo_comision { get; set; }
        public int? codrango_comision { get; set; }
        public bool? comision_segun_rango { get; set; }
        public bool? calcula_hrs_extras { get; set; }
    }
}