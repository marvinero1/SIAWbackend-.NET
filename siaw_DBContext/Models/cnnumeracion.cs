﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;

namespace siaw_DBContext.Models
{
    public partial class cnnumeracion
    {
        public string id { get; set; }
        public string descripcion { get; set; }
        public int nroactual { get; set; }
        public bool ajuste { get; set; }
        public DateTime? desde { get; set; }
        public DateTime? hasta { get; set; }
        public string horareg { get; set; }
        public DateTime fechareg { get; set; }
        public string usuarioreg { get; set; }
    }
}