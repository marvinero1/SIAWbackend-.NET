﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;

namespace siaw_DBContext.Models
{
    public partial class pepaguinaldos
    {
        public int codigo { get; set; }
        public int gestion { get; set; }
        public string codempresa { get; set; }
        public byte mesdesde { get; set; }
        public int anodesde { get; set; }
        public byte meshasta { get; set; }
        public int anohasta { get; set; }
        public bool cerrado { get; set; }
        public DateTime? tdcafecha { get; set; }
        public string horareg { get; set; }
        public DateTime fechareg { get; set; }
        public string usuarioreg { get; set; }
        public string obs { get; set; }
        public bool? segundo_aguinaldo { get; set; }
    }
}