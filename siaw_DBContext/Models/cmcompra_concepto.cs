﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;

namespace siaw_DBContext.Models
{
    public partial class cmcompra_concepto
    {
        public int codigo { get; set; }
        public int codcompra { get; set; }
        public string codconcepto { get; set; }
        public decimal importe { get; set; }
        public string codauxiliar { get; set; }
        public decimal? importe_ice { get; set; }
        public decimal? importe_nogravado { get; set; }
        public decimal? arg_impneto { get; set; }
        public decimal? arg_nogravado { get; set; }
        public decimal? arg_siniva { get; set; }
        public decimal? arg_porceniva { get; set; }
        public decimal? arg_iva { get; set; }
        public decimal? arg_exento { get; set; }
        public decimal? arg_bonificacion { get; set; }
        public decimal? tasas { get; set; }
        public decimal? impotro_no_credito_fiscal { get; set; }
        public decimal? impexcento { get; set; }
        public decimal? impgravado_tasa_cero { get; set; }
        public decimal? impdescuento { get; set; }
    }
}