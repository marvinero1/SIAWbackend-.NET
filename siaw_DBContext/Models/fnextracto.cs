﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;

namespace siaw_DBContext.Models
{
    public partial class fnextracto
    {
        public int codigo { get; set; }
        public string id { get; set; }
        public int numeroid { get; set; }
        public DateTime fecha { get; set; }
        public string codcuentab { get; set; }
        public string nrocuentab { get; set; }
        public string ciudad { get; set; }
        public string nomcliente { get; set; }
        public string cicliente { get; set; }
        public decimal monto { get; set; }
        public string codmoneda { get; set; }
        public decimal? monto_debito { get; set; }
        public string codmoneda_debito { get; set; }
        public string nrodeposito { get; set; }
        public bool? pendiente { get; set; }
        public string id_enlace { get; set; }
        public int? numeroid_enlace { get; set; }
        public string idticket { get; set; }
        public int? numeroidticket { get; set; }
        public string usuarioreg { get; set; }
        public string horareg { get; set; }
        public DateTime? fechareg { get; set; }
    }
}