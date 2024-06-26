﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;

namespace siaw_DBContext.Models
{
    public partial class veproforma
    {
        public int codigo { get; set; }
        public string id { get; set; }
        public int numeroid { get; set; }
        public int codalmacen { get; set; }
        public string codcliente { get; set; }
        public string nomcliente { get; set; }
        public string nit { get; set; }
        public int codvendedor { get; set; }
        public string codmoneda { get; set; }
        public DateTime fecha { get; set; }
        public decimal tdc { get; set; }
        public byte tipopago { get; set; }
        public decimal subtotal { get; set; }
        public decimal descuentos { get; set; }
        public decimal recargos { get; set; }
        public decimal total { get; set; }
        public bool anulada { get; set; }
        public string transporte { get; set; }
        public string fletepor { get; set; }
        public string direccion { get; set; }
        public bool aprobada { get; set; }
        public bool? paraaprobar { get; set; }
        public bool transferida { get; set; }
        public DateTime? fechaaut { get; set; }
        public int codcomplementaria { get; set; }
        public string obs { get; set; }
        public string obs2 { get; set; }
        public string horareg { get; set; }
        public DateTime fechareg { get; set; }
        public string usuarioreg { get; set; }
        public string preparacion { get; set; }
        public decimal? iva { get; set; }
        public string horaaut { get; set; }
        public string tipoentrega { get; set; }
        public decimal? porceniva { get; set; }
        public string odc { get; set; }
        public decimal? peso { get; set; }
        public bool? impresa { get; set; }
        public bool? etiqueta_impresa { get; set; }
        public DateTime? fecha_inicial { get; set; }
        public string hora_inicial { get; set; }
        public string usuarioaut { get; set; }
        public bool? contra_entrega { get; set; }
        public string idanticipo { get; set; }
        public int? numeroidanticipo { get; set; }
        public decimal? monto_anticipo { get; set; }
        public bool? pago_contado_anticipado { get; set; }
        public string codcliente_real { get; set; }
        public bool? venta_cliente_oficina { get; set; }
        public string estado_contra_entrega { get; set; }
        public string nombre_transporte { get; set; }
        public bool? desclinea_segun_solicitud { get; set; }
        public string idsoldesctos { get; set; }
        public int? nroidsoldesctos { get; set; }
        public string hora { get; set; }
        public string ubicacion { get; set; }
        public bool? es_sol_urgente { get; set; }
        public string niveles_descuento { get; set; }
        public string idpf_complemento { get; set; }
        public int? nroidpf_complemento { get; set; }
        public string complemento_ci { get; set; }
        public int? tipo_venta { get; set; }
        public int? tipo_docid { get; set; }
        public string email { get; set; }
        public int? tipo_complementopf { get; set; }
        public bool? confirmada { get; set; }
        public DateTime? fecha_confirmada { get; set; }
        public string hora_confirmada { get; set; }
        public string latitud_entrega { get; set; }
        public string longitud_entrega { get; set; }
    }
}