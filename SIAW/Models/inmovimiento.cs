﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;

namespace SIAW.Models
{
    public partial class inmovimiento
    {
        public int codigo { get; set; }
        public string id { get; set; }
        public int codconcepto { get; set; }
        public int numeroid { get; set; }
        public DateTime fecha { get; set; }
        public int codalmacen { get; set; }
        public int? codalmorigen { get; set; }
        public int? codalmdestino { get; set; }
        public string obs { get; set; }
        public short factor { get; set; }
        public string horareg { get; set; }
        public DateTime fechareg { get; set; }
        public string usuarioreg { get; set; }
        public string fid { get; set; }
        public int? fnumeroid { get; set; }
        public bool? anulada { get; set; }
        public int? codvendedor { get; set; }
        public bool? contabilizada { get; set; }
        public int? comprobante { get; set; }
        public decimal? peso { get; set; }
        public string idproforma { get; set; }
        public int? numeroidproforma { get; set; }
        public DateTime? fecha_inicial { get; set; }
        public int? codpersona { get; set; }
        public string codcliente { get; set; }
        public string idproforma_sol { get; set; }
        public int? numeroidproforma_sol { get; set; }
    }
}