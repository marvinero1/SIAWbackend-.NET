﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;

namespace SIAW.Models
{
    public partial class resuventas
    {
        public int codigo { get; set; }
        public int? anio { get; set; }
        public int? mes { get; set; }
        public decimal? ttl_bs { get; set; }
        public decimal? ttl_us { get; set; }
        public decimal? ttl_kg { get; set; }
        public DateTime fechareg { get; set; }
        public string horareg { get; set; }
        public string usuarioreg { get; set; }
    }
}