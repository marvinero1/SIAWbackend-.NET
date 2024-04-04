using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using siaw_DBContext.Data;
using siaw_DBContext.Models;
using siaw_DBContext.Models_Extra;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace siaw_funciones
{
    public class Ventas
    {
        //Clase necesaria para el uso del DBContext del proyecto siaw_Context
        private readonly Depositos_Cliente depositos_cliente = new Depositos_Cliente();
        //private readonly IDepositosCliente depositos_cliente;
        private readonly Cliente cliente = new Cliente();
        private readonly TipoCambio tipocambio = new TipoCambio();
        private const int CODDESEXTRA_PROMOCION = 10;
        /*
        public Ventas(IDepositosCliente depositosCliente)
        {
            depositos_cliente = depositosCliente;
        }*/
        public static class DbContextFactory
        {
            public static DBContext Create(string connectionString)
            {
                var optionsBuilder = new DbContextOptionsBuilder<DBContext>();
                optionsBuilder.UseSqlServer(connectionString);

                return new DBContext(optionsBuilder.Options);
            }
        }
        private Configuracion configuracion = new Configuracion();
        public async Task<string> monedabasetarifa(DBContext _context, int codtarifa)
        {
            string resultado = "";

            var result = await _context.intarifa
                    .Where(v => v.codigo == codtarifa)
                    .Select(parametro => new
                    {
                        parametro.monedabase
                    })
                    .FirstOrDefaultAsync();
            if (result != null)
            {
                resultado = result.monedabase;
            }
            return resultado;
        }


        public async Task<bool> Grabar_Descuento_Por_deposito_Pendiente(DBContext _context, int codproforma, string codempresa, string usuarioreg, List<vedesextraprof> vedesextraprof)
        {
            int coddesextra_depositos = await configuracion.emp_coddesextra_x_deposito(_context, codempresa);
            decimal PORCEN_DESCTO = await DescuentoExtra_Porcentaje(_context, coddesextra_depositos);

            bool ya_aplico = false;

            var decuentoxDep = vedesextraprof.Where(i => i.coddesextra == coddesextra_depositos).FirstOrDefault();
            if (decuentoxDep != null)
            {
                if (decuentoxDep.codcobranza != 0)
                {
                    if (await depositos_cliente.Cobranza_Credito_Se_Dio_Descto_Deposito(_context, (int)decuentoxDep.codcobranza))
                    {
                        ya_aplico = true;
                    }
                }
                else if (decuentoxDep.codcobranza_contado != 0)
                {
                    if (await depositos_cliente.Cobranza_Contado_Ce_Se_Dio_Descto_Deposito(_context, (int)decuentoxDep.codcobranza_contado))
                    {
                        ya_aplico = true;
                    }
                }
                else if (decuentoxDep.codanticipo != 0)
                {
                    if (await depositos_cliente.Anticipo_Contado_Aplicado_A_Proforma_Se_Dio_Descto_Deposito(_context, (int)decuentoxDep.codanticipo))
                    {
                        ya_aplico = true;
                    }
                }

                // se registra solo si no aplico antes
                if (!ya_aplico)
                {
                    var dtcbza = new dtcbza();
                    if (decuentoxDep.codcobranza != 0)
                    {
                        dtcbza = await _context.cocobranza
                            .Where(i => i.codigo == decuentoxDep.codcobranza)
                            .Select(i => new dtcbza
                            {
                                codigo = i.codigo,
                                id = i.id,
                                numeroid = i.numeroid,
                                cliente = i.cliente,
                                nit = i.nit
                            }).FirstOrDefaultAsync();
                    }
                    else if (decuentoxDep.codcobranza_contado != 0)
                    {
                        dtcbza = await _context.cocobranza_contado
                            .Where(i => i.codigo == decuentoxDep.codcobranza_contado)
                            .Select(i => new dtcbza
                            {
                                codigo = i.codigo,
                                id = i.id,
                                numeroid = i.numeroid,
                                cliente = i.cliente,
                                nit = i.nit
                            }).FirstOrDefaultAsync();
                    }
                    else
                    {
                        dtcbza = await _context.coanticipo
                            .Where(i => i.codigo == decuentoxDep.codanticipo)
                            .Select(i => new dtcbza
                            {
                                codigo = i.codigo,
                                id = i.id,
                                numeroid = i.numeroid,
                                cliente = i.codcliente,
                                nit = i.nit
                            }).FirstOrDefaultAsync();
                    }
                }
            }

            return true;
        }

        public async Task<decimal> DescuentoExtra_Porcentaje(DBContext _context, int coddesextra)
        {
            var result = await _context.vedesextra
                    .Where(v => v.codigo == coddesextra)
                    .Select(parametro => parametro.porcentaje)
                    .FirstOrDefaultAsync();
            return result;
        }


        public async Task<int> Disminuir_Fecha_Vence(DBContext _context, int codremision, string codcliente)
        {
            int nro_dias = 0;
            var dt1 = await _context.vedesextraremi
                    .Where(v => v.codremision == codremision)
                    .ToListAsync();
            foreach (var reg1 in dt1)
            {
                var dt2 = await _context.vecliente_desextra
                    .Where(v => v.codcliente == codcliente && v.coddesextra == reg1.coddesextra).FirstOrDefaultAsync();
                if (dt2 != null)
                {
                    nro_dias += await Dias_Disminuir_Vencimiento(_context, reg1.coddesextra, dt2.dias ?? -1);
                }
            }

            int resultado = nro_dias * -1;


            return resultado;
        }

        public async Task<int> Dias_Disminuir_Vencimiento(DBContext _context, int coddesxextra, int dias)
        {
            var dt = await _context.vedesextra_modifica_dias
                    .Where(v => v.coddesextra == coddesxextra && v.dias == dias)
                    .FirstOrDefaultAsync();
            if (dt != null && dt.dias_disminuye!= null)
            {
                return (int)dt.dias_disminuye;
            }
            return 0;
        }
        public async Task<bool> Pedido_Esta_Despachado(DBContext _context, int codproforma)
        {
            bool resultado = false; 
            string id_proforma = await proforma_id(_context, codproforma);
            int nroid_proforma = await proforma_numeroid(_context, codproforma);

            var dt = await _context.vedespacho
                .Where(i => i.id == id_proforma && i.nroid == nroid_proforma && i.estado== "DESPACHADO")
                .Select(i => new
                {
                    i.id,
                    i.nroid,
                    i.estado,
                    i.fdespachado,
                    i.hdespachado
                }).FirstOrDefaultAsync();
            if (dt != null)
            {
                if (dt.estado != null)
                {
                    if (dt.estado == "DESPACHADO")
                    {
                        resultado = true;
                    }
                    else
                    {
                        resultado = false;
                    }
                }
                else
                {
                    resultado = false;
                }
            }else { resultado = false; }

            return resultado;
        }



        public async Task<string> proforma_id(DBContext _context, int codproforma)
        {
            var resultado = await _context.veproforma.Where(i=>i.codigo == codproforma).Select(i=> i.id).FirstOrDefaultAsync();
            return resultado ?? "";
        }
        public async Task<int> proforma_numeroid(DBContext _context, int codproforma)
        {
            var resultado = await _context.veproforma.Where(i => i.codigo == codproforma).Select(i => i.numeroid).FirstOrDefaultAsync();
            return resultado;
        }

        public async Task<int> codproforma_de_remision(DBContext _context, int codremision)
        {
            var resultado = await _context.veremision.Where(i => i.codigo == codremision).Select(i => i.codproforma).FirstOrDefaultAsync();
            return resultado ?? 0;
        }
        public static async Task<bool> Existe_Proforma1(DBContext _context, int codproforma)
        {
            var resultado = await _context.veproforma.Where(i => i.codigo == codproforma).CountAsync();
            if (resultado > 0)
            {
                return true;
            }
            return false;
        }
        public static async Task<bool> proforma_anulada(DBContext _context, int codproforma)
        {
            var resultado = await _context.veproforma.Where(i => i.codigo == codproforma).Select(i => i.anulada).FirstOrDefaultAsync();
            return resultado;
        }

        public static async Task<bool> Existe_NotaRemision1(DBContext _context, int codremision)
        {
            var resultado = await _context.veremision.Where(i => i.codigo == codremision).CountAsync();
            if (resultado > 0)
            {
                return true;
            }
            return false;
        }
        public static async Task<bool> remision_anulada(DBContext _context, int codremision)
        {
            var resultado = await _context.veremision.Where(i => i.codigo == codremision).Select(i => i.anulada).FirstOrDefaultAsync();
            return resultado;
        }

        public async Task<DateTime> Obtener_Fecha_Despachado_Pedido(DBContext _context, int codproforma)
        {
            DateTime resultado = new DateTime(1900, 1, 1);
            string id_proforma = await proforma_id(_context, codproforma);
            int nroid_proforma = await proforma_numeroid(_context, codproforma);
            string estado_final_pf = await Estado_Final_Proformas(_context);

            var dt = await _context.vedespacho
                .Where(i => i.id == id_proforma && i.nroid == nroid_proforma && i.estado == estado_final_pf)
                .Select(i => new
                {
                    i.id,
                    i.nroid,
                    i.estado,
                    i.fdespacho,
                    i.hdespacho
                }).FirstOrDefaultAsync();
            if (dt != null)
            {
                if (dt.fdespacho != null)
                {
                    resultado = new DateTime(
                        ((DateTime)dt.fdespacho).Year,
                        ((DateTime)dt.fdespacho).Month,
                        ((DateTime)dt.fdespacho).Day
                        );
                }
            }

            return resultado;
        }

        public async Task<string> Estado_Final_Proformas(DBContext _context)
        {
            var resultado = await _context.adparametros.Select(i => i.estado_final_proformas).FirstOrDefaultAsync();
            return resultado ?? "DESPACHADO";
        }


        public async Task<bool> UsuarioTarifa_Permitido(DBContext _context, string usuario, int codtarifa)
        {
            var resultado = await _context.adusuario_tarifa.Where(i => i.usuario == usuario && i.codtarifa == codtarifa).FirstOrDefaultAsync();
            if (resultado != null)
            {
                return true;
            }
            return false;
        }

        public async Task<bool> Tarifa_Permite_Desctos_Linea(DBContext _context, int codtarifa)
        {
            var resultado = await _context.intarifa.Where(i => i.codigo == codtarifa).Select(i => i.desitem).FirstOrDefaultAsync();
            return resultado;
        }
        public async Task<bool> Existe_Solicitud_Descuento_Nivel(DBContext _context, string idsolicitud, int nroidsolicitud)
        {
            var resultado = await _context.vesoldsctos.Where(i => i.id == idsolicitud && i.numeroid == nroidsolicitud).FirstOrDefaultAsync();
            if (resultado != null)
            {
                return true;
            }
            return false;
        }
        public async Task<string> Cliente_Solicitud_Descuento_Nivel(DBContext _context, string idsolicitud, int nroidsolicitud)
        {
            var resultado = await _context.vesoldsctos.Where(i => i.id == idsolicitud && i.numeroid == nroidsolicitud).Select(i => i.codcliente).FirstOrDefaultAsync();
            if (resultado != null)
            {
                return resultado;
            }
            return "";
        }

        public async Task<float> preciodelistaitem(DBContext _context, int codtarifa, string coditem)
        {
            if (codtarifa == 0 || coditem== "")
            {
                return 0;
            }
            float precioFinal;

            var tarifaParam = new SqlParameter("@tarifa", SqlDbType.Int) { Value = codtarifa };
            var itemParam = new SqlParameter("@item", SqlDbType.NVarChar, 8) { Value = coditem };
            var precioFinalParam = new SqlParameter("@preciofinal", SqlDbType.Float) { Direction = ParameterDirection.Output };

            await _context.Database
                .ExecuteSqlRawAsync("EXECUTE preciolista @tarifa, @item, @preciofinal OUTPUT",
                    tarifaParam,
                    itemParam,
                    precioFinalParam);

            precioFinal = Convert.ToSingle(precioFinalParam.Value);

            return precioFinal;
        }

        public async Task<float> preciocliente(DBContext _context, string codcliente, int codalmacen, int codtarifa, string coditem, string desc_linea_seg_solicitud, string desc_linea, string opcion_nivel)
        {
            if (codtarifa == 0 || coditem == "" || codcliente.Trim() == "" || codalmacen == 0)
            {
                return 0;
            }
            float precioFinal;

            var clienteParam = new SqlParameter("@cliente", SqlDbType.NVarChar, 10) { Value = codcliente };
            var almacenParam = new SqlParameter("@almacen", SqlDbType.Int) { Value = codalmacen };
            var tarifaParam = new SqlParameter("@tarifa", SqlDbType.Int) { Value = codtarifa };
            var itemParam = new SqlParameter("@item", SqlDbType.NVarChar, 8) { Value = coditem };
            var descSegSolicitudParam = new SqlParameter("@nivel_desc_segun_solicitud", SqlDbType.NVarChar, 2) { Value = desc_linea_seg_solicitud };
            var descLineaParam = new SqlParameter("@nivel_desc_solicitud", SqlDbType.NVarChar, 2) { Value = desc_linea };
            var opcionNivelParam = new SqlParameter("@opcion_nivel_desctos", SqlDbType.NVarChar, 10) { Value = opcion_nivel };
            var precioFinalParam = new SqlParameter("@preciofinal", SqlDbType.Float) { Direction = ParameterDirection.Output };

            await _context.Database.ExecuteSqlRawAsync(
                "EXECUTE preciocliente @cliente, @almacen, @tarifa, @item, @nivel_desc_segun_solicitud, @nivel_desc_solicitud, @opcion_nivel_desctos, @preciofinal OUTPUT",
                clienteParam, almacenParam, tarifaParam, itemParam, descSegSolicitudParam, descLineaParam, opcionNivelParam, precioFinalParam);

            precioFinal = Convert.ToSingle(precioFinalParam.Value);

            return precioFinal;
        }
        public async Task<float> preciocondescitem(DBContext _context, string codcliente, int codalmacen, int codtarifa, string coditem, int coddescuento, string desc_linea_seg_solicitud, string desc_linea, string opcion_nivel)
        {
            if (codtarifa == 0 || coditem == "")
            {
                return 0;
            }

            float precioFinal;

            var clienteParam = new SqlParameter("@cliente", SqlDbType.NVarChar, 10) { Value = codcliente };
            var almacenParam = new SqlParameter("@almacen", SqlDbType.Int) { Value = codalmacen };
            var tarifaParam = new SqlParameter("@tarifa", SqlDbType.Int) { Value = codtarifa };
            var itemParam = new SqlParameter("@item", SqlDbType.NVarChar, 8) { Value = coditem };
            var descuentoParam = new SqlParameter("@descuento", SqlDbType.Int) { Value = coddescuento };
            var descSegSolicitudParam = new SqlParameter("@nivel_desc_segun_solicitud", SqlDbType.NVarChar, 2) { Value = desc_linea_seg_solicitud };
            var descLineaParam = new SqlParameter("@nivel_desc_solicitud", SqlDbType.NVarChar, 2) { Value = desc_linea };
            var opcionNivelParam = new SqlParameter("@opcion_nivel_desctos", SqlDbType.NVarChar, 10) { Value = opcion_nivel };
            var precioFinalParam = new SqlParameter("@preciofinal", SqlDbType.Float) { Direction = ParameterDirection.Output };

            await _context.Database.ExecuteSqlRawAsync(
                "EXECUTE preciocondesc @cliente, @almacen, @tarifa, @item, @descuento, @nivel_desc_segun_solicitud, @nivel_desc_solicitud, @opcion_nivel_desctos, @preciofinal OUTPUT",
                clienteParam, almacenParam, tarifaParam, itemParam, descuentoParam, descSegSolicitudParam, descLineaParam, opcionNivelParam, precioFinalParam);

            precioFinal = Convert.ToSingle(precioFinalParam.Value);

            return precioFinal;
        }

        public async Task<string> Tipo_Recargo(DBContext _context, int codrecargo)
        {
            var resultado = await _context.verecargo
                .Where(i => i.codigo == codrecargo)
                .FirstOrDefaultAsync();
            if (resultado != null)
            {
                if (resultado.montopor == true)   // hasta el 14/03/2024 esto era false
                {
                    return "MONTO";
                }
                return "PORCENTAJE";
            }
            return "";
        }

        public async Task<List<tabladescuentos>> Ordenar_Descuentos_Extra(DBContext _context, List<tabladescuentos> tabladescuentos)
        {
            List < tabladescuentos > dt_subtotal = new List<tabladescuentos> ();
            List<tabladescuentos> dt_total = new List<tabladescuentos>();
            foreach (var reg in tabladescuentos)
            {
                var aplicacion = await Donde_Aplica_Descuento_Extra(_context, reg.coddesextra);
                reg.aplicacion = aplicacion;
                if (aplicacion == "SUBTOTAL")
                {
                    dt_subtotal.Add(reg);
                }
                else if (aplicacion == "TOTAL")
                {
                    dt_total.Add(reg);
                }
            }
            tabladescuentos.Clear();
            tabladescuentos = dt_subtotal.Union(dt_total).ToList();
            return tabladescuentos;
        }

        public async Task<string> Donde_Aplica_Descuento_Extra(DBContext _context, int coddesextra)
        {
            var resultado = await _context.vedesextra
                .Where(i => i.codigo == coddesextra)
                .FirstOrDefaultAsync();
            if (resultado != null)
            {
                return resultado.aplicacion;
            }
            return "";
        }
        public async Task<bool> DescuentoExtra_Diferenciado_x_item(DBContext _context, int coddesextra)
        {
            var resultado = await _context.vedesextra.Where(i => i.codigo == coddesextra).Select(i => i.diferenciado_x_item).FirstOrDefaultAsync();
            return resultado??false;
        }


        public async Task<(double resultado, List<itemDataMatriz> dt)> DescuentoExtra_CalcularMonto(DBContext _context, int coddesextra, List<itemDataMatriz> dt, string codcliente, string nit_cliente)
        {
            double resultado = 0;
            foreach (var reg in dt)
            {
                //se obtiene el porccentaje por descuento extra segun el item y coddesextra
                reg.porcentaje = await DescuentoExtra_Porcentaje_Item(_context, coddesextra, reg.coditem);

                //se modifica el porcentaje antes obtenido si corresponde
                //segun politica gerencial emitida el 26-08-2015 para empresas competidoras
                //si el nivel de descto es Z o X no se benefician de la promocion
                if (await cliente.EsClienteCompetencia(_context, nit_cliente))
                {
                    if (await cliente.Cliente_Competencia_Controla_Descto_Nivel(_context, nit_cliente))
                    {
                        if (await Descuento_Extra_Valida_Nivel(_context, coddesextra))
                        {
                            if (reg.niveldesc == "Z" || reg.niveldesc == "z" || reg.niveldesc == "X" || reg.niveldesc == "x")
                            {
                                reg.porcentaje = 0;
                            }
                        }
                    }
                }
                if (DBNull.Value.Equals(reg.subtotal_descto_extra))
                {
                    reg.monto_descto = (reg.total / 100) * reg.porcentaje;
                    reg.subtotal_descto_extra = reg.total - reg.monto_descto;
                }
                else
                {
                    reg.monto_descto = (reg.subtotal_descto_extra / 100) * reg.porcentaje;
                    reg.subtotal_descto_extra = reg.subtotal_descto_extra - reg.monto_descto;
                }
                resultado = resultado + reg.monto_descto;
            }
            resultado = Math.Round(resultado, 2, MidpointRounding.AwayFromZero);
            return (resultado, dt);
        }

        public async Task<float> DescuentoExtra_Porcentaje_Item(DBContext _context, int coddesextra, string coditem)
        {
            var resultado = await _context.vedesextra_item.Where(i => i.coddesextra == coddesextra && i.coditem == coditem).Select(i => i.porcentaje).FirstOrDefaultAsync();
            return (float)(resultado ?? 0);
        }

        public async Task<bool> Descuento_Extra_Valida_Nivel(DBContext _context, int coddesextra)
        {
            var resultado = await _context.vedesextra.Where(i => i.codigo == coddesextra).Select(i => i.valida_descuento_linea).FirstOrDefaultAsync();
            if (resultado != null)
            {
                return (bool)resultado;
            }
            return false;
        }

        public async Task<int> codproforma(DBContext _context, string id_proforma, int numeroid_proforma)
        {
            var resultado = await _context.veproforma.Where(i => i.id == id_proforma && i.numeroid == numeroid_proforma).Select(i => i.codigo).FirstOrDefaultAsync();
            return resultado;
        }
        public async Task<bool> Proforma_Tiene_DescuentoExtra(DBContext _context, int codproforma, int coddesextra)
        {
            var resultado = await _context.vedesextraprof.Where(i => i.codproforma == codproforma && i.coddesextra == coddesextra).FirstOrDefaultAsync();
            if (resultado != null)
            {
                return true;
            }
            return false;
        }

        public async Task<(List<verecargoprof> tablarecargos,double ttl_recargos_sobre_total_final)> Recargos_Sobre_Total_Final(DBContext _context, double total, string codmoneda, DateTime fecha, string codempresa, List<verecargoprof> tablarecargos)
        {
            int codrecargo_pedido_urg_provincia = await configuracion.emp_codrecargo_pedido_urgente_provincia(_context, codempresa);
            double ttl_recargos_sobre_total_final = 0;

            //de momento a fecha: 26-10-2021 el unico recargo que va al final es del 8 - RECARGO PEDIDO URG PROVINCIAS
            foreach (var reg in tablarecargos)
            {
                if (reg.codrecargo == codrecargo_pedido_urg_provincia)
                {
                    if (reg.porcen > 0)
                    {
                        reg.montodoc = (decimal)(total / 100) * reg.porcen;
                    }
                    else
                    {
                        reg.montodoc = await tipocambio._conversion(_context, codmoneda, reg.moneda, fecha, reg.monto);
                    }
                    reg.montodoc = Math.Round(reg.montodoc,2);
                    ttl_recargos_sobre_total_final += (double)reg.montodoc;
                }
            }
            return (tablarecargos, ttl_recargos_sobre_total_final);
        }

        public async Task<bool> remision_es_PP(DBContext _context, int codigo)
        {
            var resultado = await _context.vedesextraremi
                .Join(_context.vedesextra,
                      d => d.coddesextra,
                      e => e.codigo,
                      (d, e) => new { D = d, E = e })
                .Where(de => de.D.codremision == codigo && de.E.prontopago == true)
                .Select(de => de.D.codremision)
                .ToListAsync();

            if (resultado != null)
            {
                return true;
            }
            return false;
        }

        public async Task<double> promocion_monto(DBContext _context, string codcliente, DateTime fecha)
        {
            var resultado = await _context.vepromocion_A
                .Where(i => i.codcliente == codcliente && i.coddesextra == CODDESEXTRA_PROMOCION && i.mes == fecha.Month && i.anio == fecha.Year)
                .Select(i => i.monto)
                .FirstOrDefaultAsync() ?? 0;
            return (double)resultado;
        }
        public async Task<double> promocion_usado(DBContext _context, string codcliente, DateTime fecha)
        {
            DateTime startDate = new DateTime(fecha.Year, fecha.Month, 1);
            DateTime endDate = new DateTime(fecha.Year, fecha.Month, DateTime.DaysInMonth(fecha.Year, fecha.Month));

            var sumMontodoc = _context.veproforma
                    .Join(_context.vedesextraprof,
                          c => c.codigo,
                          d => d.codproforma,
                          (c, d) => new { Veproforma = c, Vedesextraprof = d })
                    .Where(joinResult =>
                           joinResult.Veproforma.codcliente == codcliente &&
                           joinResult.Veproforma.fecha >= startDate && joinResult.Veproforma.fecha <= endDate &&
                           joinResult.Vedesextraprof.coddesextra == CODDESEXTRA_PROMOCION &&
                           joinResult.Veproforma.anulada == false && joinResult.Veproforma.aprobada == true)
                    .Sum(joinResult => joinResult.Vedesextraprof.montodoc);

            return (double)sumMontodoc;
        }

        public async Task<string> Cliente_Referencia_Solicitud_Descuentos(DBContext _context, string idsol_des, int nroidsol_des)
        {
            var resultado = await _context.vesoldsctos
                .Where(i => i.id == idsol_des && i.numeroid == nroidsol_des)
                .Select(i => new
                {
                    i.codcliente,
                    i.codcliente_referencia
                })
                .FirstOrDefaultAsync();
            if (resultado == null)
            {
                return "";
            }
            if (resultado.codcliente_referencia == null)
            {
                return resultado.codcliente;
            }
            return resultado.codcliente_referencia;
        }
        public async Task<bool> Descuento_Extra_Habilitado(DBContext _context, int coddesextra)
        {
            var resultado = await _context.vedesextra
                .Where(i => i.codigo == coddesextra)
                .FirstOrDefaultAsync();
            if (resultado != null)
            {
                return resultado.habilitado??false;
            }
            return false;
        }

        public async Task<bool> Descuento_Extra_Valida_Linea_Credito(DBContext _context, int coddesextra)
        {
            try
            {
                bool resultado = false;
                //using (_context)
                //{
                var result = await _context.vedesextra
                    .Where(v => v.codigo == coddesextra)
                    .Select(v => v.valida_linea_credito)
                    .FirstOrDefaultAsync();
                if (result != null)
                {
                    resultado = (bool)result;
                }
                else { resultado = false; }
                //}
                return resultado;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> Descuento_Extra_Habilitado_Para_Precio(DBContext _context, int coddesextra, int codtarifa)
        {
            var resultado = await _context.vedesextra_tarifa.Where(i => i.coddesextra == coddesextra && i.codtarifa == codtarifa).FirstOrDefaultAsync();
            if (resultado != null)
            {
                return true;
            }
            return false;
        }
    }


    public class dtcbza
    {
        public int codigo { get; set; }
        public string id { get; set; }
        public int numeroid { get; set; }
        public string cliente { get; set; }
        public string nit { get; set; }
    }
}
