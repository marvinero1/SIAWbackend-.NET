﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;

namespace siaw_DBContext.Models
{
    public partial class adusuario
    {
        public string login { get; set; }
        public string password { get; set; }
        public short persona { get; set; }
        public DateTime vencimiento { get; set; }
        public bool activo { get; set; }
        public string codrol { get; set; }
        public string horareg { get; set; }
        public DateTime fechareg { get; set; }
        public string usuarioreg { get; set; }
        public string correo { get; set; }
        public string password_siaw { get; set; }
        public DateTime fechareg_siaw { get; set; }
        public string passwordcorreo { get; set; }
        public string celcorporativo { get; set; }
    }
}