﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;

namespace SIAW.Models
{
    public partial class vedevolucion
    {
        public int codigo { get; set; }
        public string id { get; set; }
        public int numeroid { get; set; }
        public int? codfactura { get; set; }
        public int? codremision { get; set; }
        public int codalmacen { get; set; }
        public string codcliente { get; set; }
        public string nomcliente { get; set; }
        public string nit { get; set; }
        public int codvendedor { get; set; }
        public string codmoneda { get; set; }
        public DateTime fecha { get; set; }
        public decimal tdc { get; set; }
        public decimal subtotal { get; set; }
        public decimal descuentos { get; set; }
        public decimal recargos { get; set; }
        public decimal total { get; set; }
        public bool anulada { get; set; }
        public string transporte { get; set; }
        public string fletepor { get; set; }
        public string direccion { get; set; }
        public bool descarga { get; set; }
        public bool contabilizado { get; set; }
        public string horareg { get; set; }
        public DateTime fechareg { get; set; }
        public string usuarioreg { get; set; }
    }
}