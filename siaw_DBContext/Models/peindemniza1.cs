﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;

namespace siaw_DBContext.Models
{
    public partial class peindemniza1
    {
        public int codplanilla { get; set; }
        public int codpersona { get; set; }
        public string cargo { get; set; }
        public int? codalmacen { get; set; }
        public DateTime? fechaing { get; set; }
        public bool hay_recesion { get; set; }
        public DateTime? fecha_recesion { get; set; }
        public bool hay_liquidacion { get; set; }
        public DateTime? fecha_liquidacion { get; set; }
        public decimal? prom_cotizable { get; set; }
        public int? meses_trabajo { get; set; }
        public string dias_trabajo { get; set; }
        public decimal? duodecimas { get; set; }
        public decimal? total_indemniza { get; set; }
        public decimal? saldo { get; set; }
        public decimal? totliquidado { get; set; }
        public int? meses_liquidado { get; set; }
        public int? dias_liquidado { get; set; }
        public decimal? totfiniquito { get; set; }
        public decimal? meses_finiquito { get; set; }
        public decimal? dias_finiquito { get; set; }
    }
}