﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;

namespace SIAW.Models
{
    public partial class venumeracion
    {
        public string id { get; set; }
        public string descripcion { get; set; }
        public int nroactual { get; set; }
        public byte tipodoc { get; set; }
        public bool habilitado { get; set; }
        public bool descarga { get; set; }
        public string horareg { get; set; }
        public DateTime fechareg { get; set; }
        public string usuarioreg { get; set; }
        public string codunidad { get; set; }
        public bool reversion { get; set; }
        public string tipo { get; set; }
    }
}