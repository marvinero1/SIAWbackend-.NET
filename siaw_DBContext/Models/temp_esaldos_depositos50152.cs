﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;

namespace siaw_DBContext.Models
{
    public partial class temp_esaldos_depositos50152
    {
        public bool? es_ajuste { get; set; }
        public string pendiente { get; set; }
        public string tipo_cbza { get; set; }
        public int? tipo { get; set; }
        public int? codigo { get; set; }
        public int? codalmacen { get; set; }
        public int? codvendedor { get; set; }
        public string codcliente { get; set; }
        public string razonsocial { get; set; }
        public string codcliente_deposito { get; set; }
        public string nit { get; set; }
        public string nomcliente_nit { get; set; }
        public string iddeposito { get; set; }
        public int? nroiddeposito { get; set; }
        public int? codcobranza { get; set; }
        public string idcbza { get; set; }
        public int? nroidcbza { get; set; }
        public DateTime? fechacbza { get; set; }
        public decimal? monto_cbza { get; set; }
        public decimal? monto_dist_cbza { get; set; }
        public decimal? monto_dist_cbza_valido { get; set; }
        public string codproforma { get; set; }
        public int? codprofreal { get; set; }
        public string idprof { get; set; }
        public int? nroidprof { get; set; }
        public DateTime? fechaprof { get; set; }
        public decimal? montodist { get; set; }
        public decimal? montodescto { get; set; }
        public decimal? montorest { get; set; }
        public string moncbza { get; set; }
        public decimal? deposito_aplicado { get; set; }
        public string codmoneda_pf { get; set; }
        public decimal? tdc { get; set; }
        public bool? verificado { get; set; }
    }
}