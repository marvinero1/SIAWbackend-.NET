﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;

namespace SIAW.Models
{
    public partial class cscostoitem
    {
        public int codigo { get; set; }
        public string coditem { get; set; }
        public decimal costo { get; set; }
        public string moneda { get; set; }
        public DateTime fecha { get; set; }
        public string idemb { get; set; }
        public int? numeroidemb { get; set; }
        public string horareg { get; set; }
        public DateTime fechareg { get; set; }
        public string usuarioreg { get; set; }
        public bool? importacion { get; set; }
        public bool? costear { get; set; }
        public decimal? cantidad { get; set; }
    }
}