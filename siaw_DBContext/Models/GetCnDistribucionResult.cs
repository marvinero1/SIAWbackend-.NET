﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace siaw_DBContext.Models
{
    public partial class GetCnDistribucionResult
    {
        public string codcuenta { get; set; }
        public int? centrocosto_origen { get; set; }
        public int? centrocosto_destino { get; set; }
        [Column("porcentaje", TypeName = "decimal(18,2)")]
        public decimal? porcentaje { get; set; }
        public int? mes { get; set; }
    }
}
