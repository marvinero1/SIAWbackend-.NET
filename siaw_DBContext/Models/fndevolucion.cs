﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;

namespace siaw_DBContext.Models
{
    public partial class fndevolucion
    {
        public int codigo { get; set; }
        public string id { get; set; }
        public int numeroid { get; set; }
        public string detalle { get; set; }
        public DateTime fecha { get; set; }
        public string coddeudor { get; set; }
        public decimal? monto { get; set; }
        public string moneda { get; set; }
        public int? tipopago { get; set; }
        public string codcuentab { get; set; }
        public int? comprobante { get; set; }
        public string horareg { get; set; }
        public DateTime fechareg { get; set; }
        public string usuarioreg { get; set; }
        public int? codalmacen { get; set; }
        public bool? anulada { get; set; }
        public string idchequera { get; set; }
        public int? nrocheque { get; set; }
        public string idcuenta { get; set; }
        public bool? contabilizada { get; set; }
        public bool? porrendir { get; set; }
        public bool? porcobrar { get; set; }
        public bool? prestamo { get; set; }
        public bool? anticipo { get; set; }
        public int? codentrega { get; set; }
        public bool? flete { get; set; }
    }
}