﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;

namespace siaw_DBContext.Models
{
    public partial class veventasalm
    {
        public int codigo { get; set; }
        public int mes { get; set; }
        public int ano { get; set; }
        public DateTime? desde { get; set; }
        public DateTime? hasta { get; set; }
        public decimal? totalestandar { get; set; }
        public decimal? totalminimo { get; set; }
        public decimal? totalcontado { get; set; }
        public decimal? totalcbza { get; set; }
        public decimal? totaldist { get; set; }
        public decimal? totalanticiporev { get; set; }
        public decimal? totalanticipo { get; set; }
        public decimal? totaling { get; set; }
        public decimal? totaldocespecial { get; set; }
        public decimal? totaldesctos { get; set; }
        public decimal? alcanzado { get; set; }
        public int? factor { get; set; }
        public int? compensa { get; set; }
        public string codmoneda { get; set; }
        public string horareg { get; set; }
        public string fechareg { get; set; }
        public string usuarioreg { get; set; }
        public decimal? pesomin { get; set; }
        public decimal? pesoest { get; set; }
        public decimal? ventaspeso { get; set; }
        public decimal? totalpagosenmora { get; set; }
        public bool? inc_vtas_almacen { get; set; }
    }
}