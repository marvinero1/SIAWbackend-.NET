﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;

namespace siaw_DBContext.Models
{
    public partial class vehcredito
    {
        public int codigo { get; set; }
        public string codcliente { get; set; }
        public string codtipocredito { get; set; }
        public decimal credant { get; set; }
        public string monedaant { get; set; }
        public decimal credito { get; set; }
        public string moneda { get; set; }
        public DateTime fecha { get; set; }
        public DateTime fechavenc { get; set; }
        public string usuario { get; set; }
        public string autoriza { get; set; }
        public bool? revertido { get; set; }
        public int? codtipogarantia { get; set; }
        public string obs_garantia { get; set; }
        public string horareg { get; set; }
        public bool? hay_fecha_vence_garantia { get; set; }
        public DateTime? fecha_vence_garantia { get; set; }
        public DateTime? fecha_emision_garantia { get; set; }
        public string motivo_revierte { get; set; }
        public DateTime? fecha_revierte { get; set; }
        public string hr_revierte { get; set; }
        public string usr_revierte { get; set; }
        public DateTime? fecha_hr_reg { get; set; }
    }
}