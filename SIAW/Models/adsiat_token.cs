﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;

namespace SIAW.Models
{
    public partial class adsiat_token
    {
        public int codigo { get; set; }
        public string codsistema { get; set; }
        public int? codambiente { get; set; }
        public string version_sia { get; set; }
        public string token { get; set; }
        public DateTime? valido_desde { get; set; }
        public DateTime? valido_hasta { get; set; }
        public DateTime? fechareg { get; set; }
        public string horareg { get; set; }
        public string usuarioreg { get; set; }
        public bool? activo { get; set; }
    }
}