﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;

namespace siaw_DBContext.Models
{
    public partial class veppreversion_log
    {
        public int codigo { get; set; }
        public string id { get; set; }
        public int numeroid { get; set; }
        public DateTime fecha { get; set; }
        public DateTime fecha_rev { get; set; }
        public int codvendedor { get; set; }
        public string codcliente { get; set; }
        public decimal? monto { get; set; }
        public string codmoneda { get; set; }
        public string id_revertida { get; set; }
        public string numeroid_revertida { get; set; }
        public DateTime? fechareg { get; set; }
        public string horareg { get; set; }
        public string usuarioreg { get; set; }
    }
}