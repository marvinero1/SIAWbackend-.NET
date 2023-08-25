﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;

namespace SIAW.Models
{
    public partial class acactivo_backup
    {
        public int codigo { get; set; }
        public string nrocomprobante { get; set; }
        public string descripcion { get; set; }
        public string id { get; set; }
        public int? numeroid { get; set; }
        public decimal? pordep { get; set; }
        public decimal? vidautil { get; set; }
        public DateTime fechaad { get; set; }
        public decimal costoad { get; set; }
        public decimal? tipocambioad { get; set; }
        public string monedaad { get; set; }
        public string numfactura { get; set; }
        public DateTime fechaact { get; set; }
        public decimal? costoact { get; set; }
        public decimal? tipocambioact { get; set; }
        public string monedaact { get; set; }
        public string monedaajuste { get; set; }
        public decimal? depreacumulada { get; set; }
        public int almacen { get; set; }
        public int persona { get; set; }
        public int seguro { get; set; }
        public int centrocosto { get; set; }
        public string caracteristica1 { get; set; }
        public string caracteristica2 { get; set; }
        public string caracteristica3 { get; set; }
        public bool baja { get; set; }
        public string horareg { get; set; }
        public DateTime fechareg { get; set; }
        public string usuarioreg { get; set; }
        public DateTime? fecha_baja { get; set; }
        public string codacrazon_baja { get; set; }
        public string codmonedaajuste { get; set; }
        public string ubicacion { get; set; }
        public string costo_mercado { get; set; }
        public string costo_moneda { get; set; }
    }
}