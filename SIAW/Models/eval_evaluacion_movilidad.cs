﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;

namespace SIAW.Models
{
    public partial class eval_evaluacion_movilidad
    {
        public int codigo { get; set; }
        public byte? nivel { get; set; }
        public int anio { get; set; }
        public int mes { get; set; }
        public int codpersona_evaluador { get; set; }
        public int codpersona_evaluado { get; set; }
        public decimal? porcentaje { get; set; }
        public DateTime? fechareg { get; set; }
        public string horareg { get; set; }
        public string usuarioreg { get; set; }
    }
}