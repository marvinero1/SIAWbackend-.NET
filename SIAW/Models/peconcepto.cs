﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;

namespace SIAW.Models
{
    public partial class peconcepto
    {
        public int codigo { get; set; }
        public bool habilitado { get; set; }
        public int? tipo { get; set; }
        public string descripcion { get; set; }
        public string grupo { get; set; }
        public string formula { get; set; }
        public bool? automatico { get; set; }
        public bool? imprime_recibo { get; set; }
        public bool? imprime_cero { get; set; }
        public bool afectaacreditacion { get; set; }
        public bool afectaganancia { get; set; }
        public DateTime? fechareg { get; set; }
        public string horareg { get; set; }
        public string usuarioreg { get; set; }
        public decimal? valor { get; set; }
        public bool? multiplicador { get; set; }
        public bool? incluye_presentismo { get; set; }
    }
}