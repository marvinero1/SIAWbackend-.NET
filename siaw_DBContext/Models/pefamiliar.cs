﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;

namespace siaw_DBContext.Models
{
    public partial class pefamiliar
    {
        public int codigo { get; set; }
        public int? codpersona { get; set; }
        public string apellido { get; set; }
        public string nombre { get; set; }
        public string apcasado { get; set; }
        public string cuil { get; set; }
        public bool trabaja { get; set; }
        public DateTime? fechanac { get; set; }
        public int? parentesco { get; set; }
        public string nrodoc { get; set; }
        public string tipodoc { get; set; }
        public bool sexo { get; set; }
        public DateTime? fechaingreso { get; set; }
        public byte estadocivil { get; set; }
        public byte? salud { get; set; }
        public string nivel { get; set; }
        public bool mostrar { get; set; }
        public string observacion { get; set; }
        public string horareg { get; set; }
        public DateTime? fechareg { get; set; }
        public string usuarioreg { get; set; }
    }
}