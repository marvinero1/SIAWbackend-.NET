﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;

namespace SIAW.Models
{
    public partial class vedespacho
    {
        public int codigo { get; set; }
        public string id { get; set; }
        public int nroid { get; set; }
        public DateTime frecibido { get; set; }
        public string hrecibido { get; set; }
        public decimal hojas { get; set; }
        public string estado { get; set; }
        public int? preparapor { get; set; }
        public decimal? peso { get; set; }
        public decimal? bolsas { get; set; }
        public decimal? cajas { get; set; }
        public decimal? amarres { get; set; }
        public decimal? bultos { get; set; }
        public int? resdespacho { get; set; }
        public DateTime? fterminado { get; set; }
        public string hterminado { get; set; }
        public string guia { get; set; }
        public string nombtrans { get; set; }
        public string tipotrans { get; set; }
        public DateTime? fdespacho { get; set; }
        public string hdespacho { get; set; }
        public string tarribo { get; set; }
        public string obs { get; set; }
        public string horareg { get; set; }
        public DateTime fechareg { get; set; }
        public string usuarioreg { get; set; }
        public int? nroitems { get; set; }
        public DateTime? fpreparacion { get; set; }
        public string hpreparacion { get; set; }
        public DateTime? fdespachado { get; set; }
        public string hdespachado { get; set; }
        public DateTime? fadespachar { get; set; }
        public string hadespachar { get; set; }
        public string celchofer { get; set; }
        public string nroplaca { get; set; }
        public decimal? monto_flete { get; set; }
        public string nombchofer { get; set; }
        public DateTime? fecha { get; set; }
        public string preparacion { get; set; }
        public string tipoentrega { get; set; }
        public string codcliente { get; set; }
        public string nomcliente { get; set; }
        public decimal? total { get; set; }
        public string codmoneda { get; set; }
        public int? codalmacen { get; set; }
        public int? codvendedor { get; set; }
        public int? sobres { get; set; }
        public string hdespacho_fin { get; set; }
        public decimal? kilometraje { get; set; }
        public bool? flete_por_pagar { get; set; }
    }
}