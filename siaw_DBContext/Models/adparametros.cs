﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;

namespace siaw_DBContext.Models
{
    public partial class adparametros
    {
        public string codempresa { get; set; }
        public decimal? iva { get; set; }
        public decimal? minnacional { get; set; }
        public string monminnal { get; set; }
        public decimal? rciva { get; set; }
        public bool? negativos { get; set; }
        public string acmonedaajuste { get; set; }
        public string codcuentaajustes { get; set; }
        public string codcuentaajustesmn { get; set; }
        public string codctautilidad { get; set; }
        public string codctaperdida { get; set; }
        public decimal? iue { get; set; }
        public string frase { get; set; }
        public decimal? afp { get; set; }
        public decimal? riesgocomun { get; set; }
        public decimal? comisionafp { get; set; }
        public decimal? aportevoluntario { get; set; }
        public decimal? riesgoprof { get; set; }
        public decimal? pnsv { get; set; }
        public decimal? minnoimponible { get; set; }
        public string monminnoimpo { get; set; }
        public decimal? regcomple { get; set; }
        public string monregcomple { get; set; }
        public decimal? diascomerciales { get; set; }
        public decimal? horasmes { get; set; }
        public decimal? horasdia { get; set; }
        public byte diascomplementarias { get; set; }
        public decimal? cuatrosabados { get; set; }
        public decimal? cincosabados { get; set; }
        public int? prommesessueldo { get; set; }
        public int? prommesescomision { get; set; }
        public int? diasproceso { get; set; }
        public string monedae { get; set; }
        public string ctadebfiscal { get; set; }
        public string ctacredfiscal { get; set; }
        public bool? clientevendedor { get; set; }
        public string codmonedafact { get; set; }
        public int? cotippago { get; set; }
        public decimal? it { get; set; }
        public string referencia { get; set; }
        public string tenor { get; set; }
        public string pie { get; set; }
        public string firma { get; set; }
        public string reffirma { get; set; }
        public string ciudad { get; set; }
        public int maxcomplementarias { get; set; }
        public bool? stock_seguridad { get; set; }
        public int? nronombrenit { get; set; }
        public int? maxurgentes { get; set; }
        public int? maxitemurgentes { get; set; }
        public int? codtarifaajuste { get; set; }
        public bool? rev_automaticas { get; set; }
        public int? diasextrapp { get; set; }
        public int? dias_igualacion { get; set; }
        public int? maxdiaspp { get; set; }
        public bool? exportar { get; set; }
        public bool? importar { get; set; }
        public string condicion { get; set; }
        public string nrobruto { get; set; }
        public bool? esargentina { get; set; }
        public string ctsc1_descripcion { get; set; }
        public string ctsc2_descripcion { get; set; }
        public int? dias_cierre_01 { get; set; }
        public int? dias_cierre_02 { get; set; }
        public int? dias_cierre_03 { get; set; }
        public int? dias_cierre_04 { get; set; }
        public int? dias_cierre_05 { get; set; }
        public int? dias_cierre_06 { get; set; }
        public int? dias_cierre_07 { get; set; }
        public int? dias_cierre_08 { get; set; }
        public int? dias_cierre_09 { get; set; }
        public int? dias_cierre_10 { get; set; }
        public int? dias_cierre_11 { get; set; }
        public int? dias_cierre_12 { get; set; }
        public bool? proforma_reserva { get; set; }
        public string resp_lc_ci { get; set; }
        public string resp_lv_ci { get; set; }
        public string resp_lc_nombre { get; set; }
        public string resp_lv_nombre { get; set; }
        public string ciudad_reportes_conta { get; set; }
        public int? duracion_habilitado { get; set; }
        public bool? preg_cont_compras { get; set; }
        public bool? preg_cont_pagos { get; set; }
        public bool? preg_cont_depositos { get; set; }
        public bool? preg_cont_retiros { get; set; }
        public bool? preg_cont_transferencias { get; set; }
        public bool? preg_cont_movfondos { get; set; }
        public bool? preg_cont_cheques { get; set; }
        public bool? preg_cont_ventascontado { get; set; }
        public bool? preg_cont_ventascredito { get; set; }
        public bool? preg_cont_cobranza { get; set; }
        public bool? preg_cont_bajaaf { get; set; }
        public bool? preg_cont_trasladoaf { get; set; }
        public bool? preg_cont_notamov { get; set; }
        public bool? conforme { get; set; }
        public string textoconforme { get; set; }
        public DateTime? fecha_carta { get; set; }
        public int? estandardiaspp { get; set; }
        public int? maxcomplementarias_sindesc { get; set; }
        public int? dias_alerta_dosificacion { get; set; }
        public int? codplanpago_reversion { get; set; }
        public int? fact_lineas_arriba { get; set; }
        public bool? obliga_remito { get; set; }
        public decimal? porcencomventas { get; set; }
        public decimal? porcencomcbzas { get; set; }
        public decimal? mingarantizado { get; set; }
        public bool? comnoviajantes { get; set; }
        public byte? hoja_reportes { get; set; }
        public string codmoneda_costeo { get; set; }
        public string ctabancofactura { get; set; }
        public int? nrodec_movtos { get; set; }
        public bool? actualiza_personal { get; set; }
        public int? crm_ta_diasmora { get; set; }
        public int? crm_ta_diassinventa { get; set; }
        public bool? forzar_etiqueta { get; set; }
        public bool? permitir_items_repetidos { get; set; }
        public int? crm_ta_diassinvisita { get; set; }
        public int? crm_ta_diasdespacho { get; set; }
        public int? crm_ta_diasdespacho_horalimite { get; set; }
        public bool? crm_ta_mora { get; set; }
        public bool? crm_ta_sinventa { get; set; }
        public bool? crm_ta_sinvisita { get; set; }
        public bool? crm_ta_pp { get; set; }
        public bool? crm_ta_despacho { get; set; }
        public bool? crm_ta_cumpleanios { get; set; }
        public bool? valida_salariomin { get; set; }
        public bool? crm_ta_vencimiento { get; set; }
        public bool? crm_ta_vencimiento_dias { get; set; }
        public decimal? porcentaje_afp { get; set; }
        public decimal? porcentaje_cns { get; set; }
        public decimal? porcentaje_fonvis { get; set; }
        public decimal? porcentaje_infocal { get; set; }
        public bool? actualiza_vendedor { get; set; }
        public bool? permitir_compras_credito { get; set; }
        public bool? permitir_distribuir_compras_cc { get; set; }
        public bool? exporta_vendedor { get; set; }
        public bool? exporta_cliente { get; set; }
        public bool? actualiza_cliente { get; set; }
        public decimal? topemonto_asn1 { get; set; }
        public decimal? porcentaje_asn1 { get; set; }
        public decimal? topemonto_asn2 { get; set; }
        public decimal? porcentaje_asn2 { get; set; }
        public decimal? topemonto_asn3 { get; set; }
        public decimal? porcentaje_asn3 { get; set; }
        public string servidor_opcional { get; set; }
        public string bd_opcional { get; set; }
        public bool? cierres_diarios { get; set; }
        public decimal? monto_rnd100011 { get; set; }
        public bool? factura_imprime_labels { get; set; }
        public bool? crm_ta_ctrlpedidos { get; set; }
        public bool? incluir_vendedores { get; set; }
        public int? dias_pendientes { get; set; }
        public string controla_tiempo_uso { get; set; }
        public int? precio_lista_precios { get; set; }
        public DateTime? fecha_inicio_rutas { get; set; }
        public int? numeracion_clientes_desde { get; set; }
        public int? numeracion_clientes_hasta { get; set; }
        public bool? cambiar_credito_morosos { get; set; }
        public int? dias_sin_compra { get; set; }
        public bool? genera_hoja_de_rutas { get; set; }
        public bool? valida_cobertura { get; set; }
        public bool? reimprimir_factura_original { get; set; }
        public string carpeta_backup_facturas { get; set; }
        public int? dias_mora_limite { get; set; }
        public decimal? monto_maximo_facturas_sin_nombre { get; set; }
        public bool? asignar_cliente_para_descuentos_linea_en_proformas_sn { get; set; }
        public bool? modificar_id_proformas { get; set; }
        public decimal? distancia_mts_cobertura { get; set; }
        public bool? bono_rendimiento_pymes_segun_evaluacion { get; set; }
        public bool? hab_descto_x_deposito { get; set; }
        public int? coddesextra_x_deposito { get; set; }
        public decimal? monto_maximo_mora { get; set; }
        public string codmoneda_monto_max_mora { get; set; }
        public int? dias_plazo_tareas_automaticas { get; set; }
        public int? dias_plazo_casos { get; set; }
        public int? crm_ta_dias_antes_de_vencer { get; set; }
        public bool? revertir_creditos_caducos { get; set; }
        public int? maxurgentes_dia { get; set; }
        public int? dias_previos_alerta_vence_credito { get; set; }
        public bool? añadir_creditos_temporales_automaticos { get; set; }
        public string estado_final_proformas { get; set; }
        public bool? realizar_ingreso_embarques { get; set; }
        public bool? habilitar_venta_clientes_en_oficina { get; set; }
        public bool? validar_informacion_actualizada { get; set; }
        public int? frecuencia_dias_actualiza_informacion { get; set; }
        public decimal? monto_min_vta_cliente_oficina { get; set; }
        public string moneda_monto_min_vta_cliente_oficina { get; set; }
        public int? maximo_items_conteo_vta_cliente_oficina { get; set; }
        public int? codempaque_venta_cliente_oficina { get; set; }
        public int? cantidad_empaques_venta_cliente_oficina { get; set; }
        public int? longitud_maxima_nit_facturacion { get; set; }
        public bool? revertir_niveles_primera_vta { get; set; }
        public int? dias_previos_revertir_creditos { get; set; }
        public int? longitud_minima_nit_facturacion { get; set; }
        public int? nro_reversiones_pendientes { get; set; }
        public bool? actualizar_obs_asistencia { get; set; }
        public int? dias_previos_proformas_aprobadas { get; set; }
        public bool? permitir_añadir_clientes_hoja_ruta { get; set; }
        public decimal? monto_limite_mn_extracto { get; set; }
        public decimal? monto_limite_me_extracto { get; set; }
        public string servidor_main_office { get; set; }
        public string bd_main_office { get; set; }
        public bool? reservar_tuerca_en_porcentaje { get; set; }
        public string tipo_arbol_evaluacion { get; set; }
        public bool? sueldos_incluir_comision_seg_obj_logrado { get; set; }
        public bool? calcular_hrs_extras_semana { get; set; }
        public bool? aplicar_porcentaje_sobre_comision_vtas { get; set; }
        public int? dias_mora_limite_revertir_credito { get; set; }
        public decimal? monto_maximo_mora_revertir_credito { get; set; }
        public string codmoneda_monto_max_mora_revertir_credito { get; set; }
        public bool? obtener_saldos_otras_ags_localmente { get; set; }
        public bool? validar_ingresos_solurgentes { get; set; }
        public bool? grabar_proforma_solurgente_en_destino { get; set; }
        public int? dias_previos_proforma_revertir_credito { get; set; }
        public bool? permite_actualizar_saldos_ags { get; set; }
        public bool? resudiarioof_incluye_vtascontado_ce_sinpago { get; set; }
        public bool? validar_version_sia { get; set; }
        public DateTime? vtascontado_ce_sinpago_desde { get; set; }
        public int? nro_items_urgentes_empaque_cerrado { get; set; }
        public bool? aplicar_recargo_descdeposito_excedente { get; set; }
        public int? codrecargo_descdeposito_excendente { get; set; }
        public bool? aplicar_ajuste_descdeposito { get; set; }
        public DateTime? nuevos_depositos_desde { get; set; }
        public string valida_max_vta_nr_pf { get; set; }
        public bool? obtener_cufd_vpn { get; set; }
        public bool? codalmacen_cufd_vpn { get; set; }
        public DateTime? fecha_inicio_nsf { get; set; }
        public bool? dosificacion_automatica { get; set; }
        public bool? distribuir_desc_extra_en_factura { get; set; }
        public bool? enviar_mail_factura { get; set; }
        public string mail_recepcion_facturas { get; set; }
        public string pwd_mail_recepcion_facturas { get; set; }
        public bool? revision_estado_facturas_sin_nal { get; set; }
        public bool? contabilizar_segun_vendedor_operacion { get; set; }
        public DateTime? fecha_cambio_pesos_aran { get; set; }
        public string dircertif_produccion { get; set; }
        public string pwd_certif_produccion { get; set; }
        public string dircertif_pruebas { get; set; }
        public string pwd_certif_pruebas { get; set; }
        public int? codarea_empaque { get; set; }
        public bool? obtener_cantidades_aprobadas_de_proformas { get; set; }
        public bool? crm_ta_proximos_vencimientos { get; set; }
        public int? codrecargo_pedido_urgente_provincia { get; set; }
        public int? coddesextra_x_deposito_contado { get; set; }
        public bool? permitir_desc_x_depo_casual_referencial { get; set; }
        public int? dias_proforma_vta_item_cliente { get; set; }
        public bool? permitir_facturas_sn { get; set; }
    }
}