﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;

namespace siaw_DBContext.Models
{
    public partial class pememo
    {
        public int codigo { get; set; }
        public int codper { get; set; }
        public DateTime fecha { get; set; }
        public string motivo { get; set; }
        public string memo { get; set; }
        public string horareg { get; set; }
        public DateTime fechareg { get; set; }
        public string usuarioreg { get; set; }
        public int? codresponsable { get; set; }
        public byte[] memo_imagen { get; set; }
    }
}