﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;

namespace SIAW.Models
{
    public partial class repestado_anticipos_fc
    {
        public string tipo { get; set; }
        public string desde { get; set; }
        public int? anio { get; set; }
        public int? mes { get; set; }
        public int codreporte { get; set; }
        public string codalmacen { get; set; }
        public string codvendedor { get; set; }
        public string codcliente { get; set; }
        public string nomcliente { get; set; }
        public decimal? saldo_anterior { get; set; }
        public decimal? anticipo { get; set; }
        public decimal? devolucion { get; set; }
        public decimal? reversion { get; set; }
        public decimal? saldo_periodo { get; set; }
        public decimal? saldo_final { get; set; }
        public string tipo_saldo { get; set; }
    }
}