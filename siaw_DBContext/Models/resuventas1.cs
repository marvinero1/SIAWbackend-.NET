﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;

namespace siaw_DBContext.Models
{
    public partial class resuventas1
    {
        public int codliquidacion { get; set; }
        public int codigo { get; set; }
        public int? codremision { get; set; }
        public byte tipopago { get; set; }
        public string codcliente { get; set; }
        public DateTime fecha { get; set; }
        public int? mes { get; set; }
        public int? anio { get; set; }
        public string hora { get; set; }
        public string minuto { get; set; }
        public decimal ttl_bs { get; set; }
        public decimal? ttl_us { get; set; }
        public decimal? ttl_peso { get; set; }
    }
}