﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;

namespace siaw_DBContext.Models
{
    public partial class crregistro
    {
        public string usuario { get; set; }
        public DateTime? fechaini { get; set; }
        public string horaini { get; set; }
        public DateTime? fechafin { get; set; }
        public string horafin { get; set; }
        public bool exportar { get; set; }
        public int? codalmacen { get; set; }
        public string obs { get; set; }
        public int? errores { get; set; }
        public DateTime? desdef { get; set; }
        public DateTime? hastaf { get; set; }
        public string servidor { get; set; }
        public string codempresa { get; set; }
    }
}