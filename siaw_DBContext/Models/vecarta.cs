﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;

namespace siaw_DBContext.Models
{
    public partial class vecarta
    {
        public int codigo { get; set; }
        public string descripcion { get; set; }
        public string ciudad { get; set; }
        public string referencia { get; set; }
        public string tenor { get; set; }
        public string pie { get; set; }
        public string firma { get; set; }
        public string reffirma { get; set; }
        public bool conforme { get; set; }
        public string textoconforme { get; set; }
        public DateTime? fecha_carta { get; set; }
        public DateTime? fechareg { get; set; }
        public string horareg { get; set; }
        public string usuarioreg { get; set; }
    }
}