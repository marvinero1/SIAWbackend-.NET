﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;

namespace SIAW.Models
{
    public partial class cncuenta
    {
        public string codigo { get; set; }
        public string descripcion { get; set; }
        public int nivel { get; set; }
        public bool imputable { get; set; }
        public string moneda { get; set; }
        public byte tipo { get; set; }
        public byte clase { get; set; }
        public int codplan { get; set; }
        public string codcuentapadre { get; set; }
        public string horareg { get; set; }
        public DateTime fechareg { get; set; }
        public string usuarioreg { get; set; }
        public bool? creaaux { get; set; }
        public bool? ajuste_ajustar { get; set; }
        public int? ajuste_modo { get; set; }
        public string ajuste_codmoneda { get; set; }
        public string ajuste_codcuenta { get; set; }
        public bool? ajustar_moneda { get; set; }
        public string ajuste_codcuenta_aplicacion { get; set; }
        public bool? ajuste_saldoinicial { get; set; }
    }
}