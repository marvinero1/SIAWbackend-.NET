﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;

namespace SIAW.Models
{
    public partial class vecompetencia_control
    {
        public int codigo { get; set; }
        public string descripcion { get; set; }
        public bool? requiere_autorizacion { get; set; }
        public bool? permite_descto_linea { get; set; }
        public bool? permite_descto_volumen { get; set; }
        public bool? permite_descto_proveedor { get; set; }
        public bool? permite_descto_extra { get; set; }
        public bool? permite_descto_promocion { get; set; }
        public bool? controla_descto_nivel { get; set; }
        public string horareg { get; set; }
        public DateTime? fechareg { get; set; }
        public string usuarioreg { get; set; }
    }
}