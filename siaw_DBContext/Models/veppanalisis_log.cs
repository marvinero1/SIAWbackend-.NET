﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;

namespace siaw_DBContext.Models
{
    public partial class veppanalisis_log
    {
        public int codigo { get; set; }
        public int codproforma { get; set; }
        public int codremision { get; set; }
        public string id { get; set; }
        public int? numeroid { get; set; }
        public DateTime? rev_desde { get; set; }
        public DateTime? rev_hasta { get; set; }
        public string obs { get; set; }
        public string estado { get; set; }
        public DateTime? fechareg { get; set; }
        public string horareg { get; set; }
        public string usuarioreg { get; set; }
    }
}