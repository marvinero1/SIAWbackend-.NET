﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;

namespace SIAW.Models
{
    public partial class cocobranza
    {
        public int codigo { get; set; }
        public string id { get; set; }
        public int numeroid { get; set; }
        public string descripcion { get; set; }
        public DateTime fecha { get; set; }
        public DateTime fechaing { get; set; }
        public int vendedor { get; set; }
        public string cliente { get; set; }
        public decimal? monto { get; set; }
        public string moneda { get; set; }
        public decimal? montorest { get; set; }
        public byte estado { get; set; }
        public bool autorizada { get; set; }
        public int? tipopago { get; set; }
        public string codcuentab { get; set; }
        public string codbanco { get; set; }
        public string nrocheque { get; set; }
        public int? nropago { get; set; }
        public string codtalonario { get; set; }
        public int nrorecibo { get; set; }
        public bool mora { get; set; }
        public int? comprobante { get; set; }
        public string horareg { get; set; }
        public DateTime fechareg { get; set; }
        public string usuarioreg { get; set; }
        public bool reciboanulado { get; set; }
        public bool contabilizada { get; set; }
        public int? codalmacen { get; set; }
        public string idcuenta { get; set; }
        public bool? paga_comision { get; set; }
        public DateTime? fecha_conta { get; set; }
        public decimal? monto_cambio { get; set; }
        public decimal? tdc_cambio { get; set; }
        public decimal? monto_me { get; set; }
        public DateTime? fecha_anulacion { get; set; }
        public string iddeudor { get; set; }
        public int? nrocuotas_mora { get; set; }
        public int? codvendedor_comision { get; set; }
        public string if_nro_cuenta { get; set; }
        public string if_nro_documento { get; set; }
        public int? if_tipo_documento { get; set; }
        public DateTime? if_fecha_documento { get; set; }
        public string if_codbanco { get; set; }
        public bool? deposito_cliente { get; set; }
        public bool? deposito_cliente_aplicado { get; set; }
        public string iddeposito { get; set; }
        public int? numeroiddeposito { get; set; }
        public DateTime? fecha_deposito { get; set; }
        public string nit { get; set; }
        public bool? ajuste { get; set; }
    }
}