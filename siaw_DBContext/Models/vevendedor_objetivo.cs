﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;

namespace siaw_DBContext.Models
{
    public partial class vevendedor_objetivo
    {
        public int? codvendedor { get; set; }
        public decimal? pesoest_rendi { get; set; }
        public DateTime? desde { get; set; }
        public DateTime? hasta { get; set; }
        public bool? graficar { get; set; }
        public bool? analizar_rendimiento { get; set; }
        public DateTime? fechareg { get; set; }
        public string horareg { get; set; }
        public string usuarioreg { get; set; }
        public decimal? pesomin_rendi { get; set; }
        public decimal? porcemin_rendi { get; set; }
    }
}