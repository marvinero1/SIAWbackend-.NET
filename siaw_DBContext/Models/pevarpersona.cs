﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;

namespace siaw_DBContext.Models
{
    public partial class pevarpersona
    {
        public int codigo { get; set; }
        public int codplanilla { get; set; }
        public int codpersona { get; set; }
        public int codvariable { get; set; }
        public decimal cantidad { get; set; }
        public string codmoneda { get; set; }
        public decimal? tdc { get; set; }
        public int? ano { get; set; }
        public int? mes { get; set; }
        public string horareg { get; set; }
        public DateTime fechareg { get; set; }
        public string usuarioreg { get; set; }
        public int? codpersona_aux { get; set; }
    }
}