﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;

namespace siaw_DBContext.Models
{
    public partial class cralmacen
    {
        public int? codalmacen { get; set; }
        public bool? exportar { get; set; }
        public bool? importar { get; set; }
        public bool? conectar_vpn { get; set; }
        public string nomb_servidor { get; set; }
        public string nomb_bdd { get; set; }
        public string nomb_usr { get; set; }
        public string clave_usr { get; set; }
        public int? orden_exporta { get; set; }
    }
}