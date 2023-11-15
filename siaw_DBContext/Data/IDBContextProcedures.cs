﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using siaw_DBContext.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace siaw_DBContext.Data
{
    public partial interface IDBContextProcedures
    {
        Task<int> Actualizar_CodCliente_Real_FacturasAsync(int? anio, int? mes, OutputParameter<decimal?> resultado, OutputParameter<int> returnValue = null, CancellationToken cancellationToken = default);
        Task<int> arregla_credito_disponibleAsync(OutputParameter<int> returnValue = null, CancellationToken cancellationToken = default);
        Task<int> asignar_nro_facturaAsync(int? codfactura, int? codalmacen, byte? nrocaja, string id, OutputParameter<int> returnValue = null, CancellationToken cancellationToken = default);
        Task<int> asignar_nro_notacreditoAsync(int? codnotacredito, int? codalmacen, byte? nrocaja, string id, OutputParameter<int> returnValue = null, CancellationToken cancellationToken = default);
        Task<List<conectadosResult>> conectadosAsync(string BD, OutputParameter<int> returnValue = null, CancellationToken cancellationToken = default);
        Task<int> descrippiezaAsync(string codigo, OutputParameter<string> pieza, OutputParameter<int> returnValue = null, CancellationToken cancellationToken = default);
        Task<int> dm_actualizar_venta_mensual_facturas_parametrosAsync(int? mes_actual, int? anio_actual, OutputParameter<int> returnValue = null, CancellationToken cancellationToken = default);
        Task<List<GetCnDistribucionResult>> GetCnDistribucionAsync(string p_codcuenta, int? p_centrocosto, int? p_mes, int? p_anio, OutputParameter<int> returnValue = null, CancellationToken cancellationToken = default);
        Task<List<Liquidar_Ventas_DMResult>> Liquidar_Ventas_DMAsync(int? anio, int? mes, OutputParameter<decimal?> resultado, OutputParameter<int> returnValue = null, CancellationToken cancellationToken = default);
        Task<List<Liquidar_Ventas_DM_CasualesResult>> Liquidar_Ventas_DM_CasualesAsync(int? anio, int? mes, OutputParameter<decimal?> resultado, OutputParameter<int> returnValue = null, CancellationToken cancellationToken = default);
        Task<List<Liquidar_Ventas_DM_Casuales_peso_documentoResult>> Liquidar_Ventas_DM_Casuales_peso_documentoAsync(int? anio, int? mes, OutputParameter<decimal?> resultado, OutputParameter<int> returnValue = null, CancellationToken cancellationToken = default);
        Task<List<NroItemsProfResult>> NroItemsProfAsync(string id, int? nroid, OutputParameter<int> returnValue = null, CancellationToken cancellationToken = default);
        Task<List<PesoProfResult>> PesoProfAsync(string id, int? nroid, OutputParameter<int> returnValue = null, CancellationToken cancellationToken = default);
        Task<int> precioclienteAsync(string cliente, int? almacen, int? tarifa, string item, string nivel_desc_segun_solicitud, string nivel_desc_solicitud, string opcion_nivel_desctos, OutputParameter<double?> preciofinal, OutputParameter<int> returnValue = null, CancellationToken cancellationToken = default);
        Task<int> preciocliente_seg_nivelAsync(string cliente, int? almacen, int? tarifa, string item, string nivel, OutputParameter<double?> preciofinal, OutputParameter<int> returnValue = null, CancellationToken cancellationToken = default);
        Task<int> preciocondescAsync(string cliente, int? almacen, int? tarifa, string item, int? descuento, string nivel_desc_segun_solicitud, string nivel_desc_solicitud, string opcion_nivel_desctos, OutputParameter<double?> preciofinal, OutputParameter<int> returnValue = null, CancellationToken cancellationToken = default);
        Task<int> preciocondesc_seg_nivelAsync(string cliente, int? almacen, int? tarifa, string item, int? descuento, string nivel, OutputParameter<double?> preciofinal, OutputParameter<int> returnValue = null, CancellationToken cancellationToken = default);
        Task<int> preciolistaAsync(int? tarifa, string item, OutputParameter<double?> preciofinal, OutputParameter<int> returnValue = null, CancellationToken cancellationToken = default);
        Task<int> Redondeo_Decimales_SIA_0_decimales_SQLAsync(decimal? minumero, OutputParameter<decimal?> resultado, OutputParameter<int> returnValue = null, CancellationToken cancellationToken = default);
        Task<int> Redondeo_Decimales_SIA_5_decimales_SQLAsync(decimal? minumero, OutputParameter<decimal?> resultado, OutputParameter<int> returnValue = null, CancellationToken cancellationToken = default);
        Task<int> reserva_proformasAsync(string codigo, int? codalmacen, OutputParameter<double?> cantidad, OutputParameter<int> returnValue = null, CancellationToken cancellationToken = default);
        Task<List<SIA00001_ConversionMonedaResult>> SIA00001_ConversionMonedaAsync(DateTime? p_fecha, string p_monDesde, string p_monHasta, decimal? p_monto, OutputParameter<decimal?> v_resultado, OutputParameter<int> returnValue = null, CancellationToken cancellationToken = default);
        Task<List<SIA00002_TipoCambioResult>> SIA00002_TipoCambioAsync(string p_monBase, string p_moneda, DateTime? p_fecha, OutputParameter<decimal?> v_resultado, OutputParameter<int> returnValue = null, CancellationToken cancellationToken = default);
        Task<List<sp_deslgozarcjtos349Result>> sp_deslgozarcjtos349Async(OutputParameter<int> returnValue = null, CancellationToken cancellationToken = default);
        Task<List<sp_deslgozarcjtos353Result>> sp_deslgozarcjtos353Async(OutputParameter<int> returnValue = null, CancellationToken cancellationToken = default);
        Task<List<sp_deslgozarcjtos400Result>> sp_deslgozarcjtos400Async(OutputParameter<int> returnValue = null, CancellationToken cancellationToken = default);
        Task<List<sp_deslgozarcjtos416Result>> sp_deslgozarcjtos416Async(OutputParameter<int> returnValue = null, CancellationToken cancellationToken = default);
        Task<List<sp_deslgozarcjtos744Result>> sp_deslgozarcjtos744Async(OutputParameter<int> returnValue = null, CancellationToken cancellationToken = default);
        Task<List<sp_deslgozarcjtos964Result>> sp_deslgozarcjtos964Async(OutputParameter<int> returnValue = null, CancellationToken cancellationToken = default);
        Task<int> sp_eliminar_personas_duplicadas_puntualAsync(OutputParameter<int> returnValue = null, CancellationToken cancellationToken = default);
        Task<int> SP001_Cantidad_Reservada_En_Proforma_AlmacenesAsync(string coditem, int? codalmacen, string id_prof, int? nroid_prof, OutputParameter<decimal?> respuesta, OutputParameter<int> returnValue = null, CancellationToken cancellationToken = default);
        Task<int> SP001_Cantidad_Reservada_En_Proforma_TiendasAsync(string coditem, int? codalmacen, string id_prof, int? nroid_prof, OutputParameter<decimal?> respuesta, OutputParameter<int> returnValue = null, CancellationToken cancellationToken = default);
        Task<int> SP001_SaldoReservadoNotasUrgente_Una_proforma_AlmacenesAsync(string coditem, int? codalmacen, string id_prof, int? nroid_prof, OutputParameter<decimal?> respuesta, OutputParameter<int> returnValue = null, CancellationToken cancellationToken = default);
        Task<int> SP001_SaldoReservadoNotasUrgente_Una_proforma_TiendasAsync(string coditem, int? codalmacen, string id_prof, int? nroid_prof, OutputParameter<decimal?> respuesta, OutputParameter<int> returnValue = null, CancellationToken cancellationToken = default);
        Task<int> SP001_SaldoReservadoNotasUrgentesAsync(string coditem, int? codalmacen, OutputParameter<string> respuesta, OutputParameter<int> returnValue = null, CancellationToken cancellationToken = default);
        Task<int> SP001_SaldoReservadoNotasUrgentes_AlmacenesAsync(string coditem, int? codalmacen, OutputParameter<string> respuesta, OutputParameter<int> returnValue = null, CancellationToken cancellationToken = default);
        Task<int> SP001_SaldoReservadoNotasUrgentes_TiendasAsync(string coditem, int? codalmacen, OutputParameter<string> respuesta, OutputParameter<int> returnValue = null, CancellationToken cancellationToken = default);
        Task<int> stock_para_tiendasAsync(string codigo, int? codalmacen, OutputParameter<double?> cantidad, OutputParameter<int> returnValue = null, CancellationToken cancellationToken = default);
        Task<int> TalonarioPorDefectoAsync(int? codvendedor, OutputParameter<string> codtalonario, OutputParameter<int> returnValue = null, CancellationToken cancellationToken = default);
        Task<int> unircostosAsync(DateTime? fecha, OutputParameter<int> returnValue = null, CancellationToken cancellationToken = default);
        Task<List<val_cambio_creditoResult>> val_cambio_creditoAsync(string codcliente, decimal? credito, OutputParameter<int?> resultado, OutputParameter<int> returnValue = null, CancellationToken cancellationToken = default);
        Task<int> val_contraentregaAsync(int? contra_entrega, int? coddescuento, OutputParameter<int?> resultado, OutputParameter<int> returnValue = null, CancellationToken cancellationToken = default);
    }
}
