﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;

namespace siaw_DBContext.Models
{
    public partial class pecargo_log1
    {
        public int codcargo_log { get; set; }
        public int? codcargo { get; set; }
        public string descripcion { get; set; }
        public decimal? salariodesglozado { get; set; }
        public string monedasaldes { get; set; }
        public decimal salariobase { get; set; }
        public string moneda { get; set; }
        public decimal comision { get; set; }
        public string monedacom { get; set; }
        public bool calculahrsextras { get; set; }
        public bool rotativo { get; set; }
        public byte portiendaarea { get; set; }
        public byte portiendanac { get; set; }
        public byte poralmarea { get; set; }
        public byte poralmnac { get; set; }
        public byte poralmacen { get; set; }
        public string horareg { get; set; }
        public DateTime? fechareg { get; set; }
        public string usuarioreg { get; set; }
        public string desccorta { get; set; }
        public decimal? porhrsextras { get; set; }
    }
}