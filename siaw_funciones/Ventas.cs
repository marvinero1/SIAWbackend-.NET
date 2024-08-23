using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using siaw_DBContext.Data;
using siaw_DBContext.Models;
using siaw_DBContext.Models_Extra;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Numerics;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Threading.Tasks;

namespace siaw_funciones
{
    public class Ventas
    {
        
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
        //Clase necesaria para el uso del DBContext del proyecto siaw_Context
        private readonly Depositos_Cliente depositos_cliente = new Depositos_Cliente();
        private Configuracion configuracion = new Configuracion();
        private Creditos creditos = new Creditos();
        private readonly Cliente cliente = new Cliente();
        private readonly TipoCambio tipocambio = new TipoCambio();
        private Funciones funciones = new Funciones();
        private readonly Nombres nombres = new Nombres();
        private readonly Empresa empresa = new Empresa();
        private readonly SIAT siat = new SIAT();
        private readonly Items items = new Items();
        //private readonly Anticipos_Vta_Contado anticipos_vta_contado = new Anticipos_Vta_Contado();

        //private readonly IDepositosCliente depositos_cliente;

        private const int CODDESEXTRA_PROMOCION = 10;
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
        public async Task<bool> Tarifa_EmpaqueCerrado(DBContext _context, int codtarifa)
        {
            try
            {
                bool resultado = false;
                //using (_context)
                //{
                var result = await _context.intarifa
                    .Where(v => v.codigo == codtarifa)
                    .Select(v => v.solo_empaque_cerrado)
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
        public async Task<bool> Tarifa_PermiteEmpaquesMixtos(DBContext _context, int codtarifa)
        {
            try
            {
                bool resultado = false;
                //using (_context)
                //{
                var result = await _context.intarifa
                    .Where(v => v.codigo == codtarifa)
                    .Select(v => v.permitir_empaques_mixtos)
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
        public async Task<string> MonedaPF(DBContext _context, int codproforma)
        {
            string resultado = "";

            //using (var _context = DbContextFactory.Create(userConnectionString))
            //{
            var result = await _context.veproforma
                .Where(v => v.codigo == codproforma)
                .Select(parametro => new
                {
                    parametro.codmoneda
                })
                .FirstOrDefaultAsync();
            if (result != null)
            {
                resultado = result.codmoneda;
            }

            // }
            return resultado;
        }
        public async Task<decimal> Total_Proforma(DBContext _context, int codproforma)
        {
            decimal resultado = 0;
            try
            {
                var result = await _context.veproforma
                .Where(v => v.codigo == codproforma)
                .Select(parametro => new
                {
                    parametro.total
                })
                .FirstOrDefaultAsync();
                if (result != null)
                {
                    resultado = result.total;
                }
            }
            catch (Exception)
            {
                return 0;
            }
            return resultado;
        }
        public async Task<decimal> SubTotal_Proforma(DBContext _context, int codproforma)
        {
            decimal resultado = 0;
            try
            {
                //using (var _context = DbContextFactory.Create(userConnectionString))
                //{
                var result = await _context.veproforma
                .Where(v => v.codigo == codproforma)
                .Select(parametro => new
                {
                    parametro.subtotal
                })
                .FirstOrDefaultAsync();
                if (result != null)
                {
                    resultado = result.subtotal;
                }

                // }
            }
            catch (Exception)
            {
                return 0;
            }
            return resultado;
        }
        public async Task<bool> Grabar_Descuento_Por_deposito_Pendiente(DBContext _context, int codproforma, string codempresa, string usuarioreg, List<vedesextraprof> vedesextraprof)
        {
            int coddesextra_depositos = await configuracion.emp_coddesextra_x_deposito(_context, codempresa);
            decimal PORCEN_DESCTO = await DescuentoExtra_Porcentaje(_context, coddesextra_depositos);
            bool resultado = true;
            bool ya_aplico = false;
            // busca los descuentos que son POR DEPOSITO
            var decuentoxDeplIST = vedesextraprof.Where(i => i.coddesextra == coddesextra_depositos).ToList();
            foreach (var reg in decuentoxDeplIST)
            {
                if (reg.codcobranza != 0)
                {
                    // verificar si la la cbza de credito alguna vez ya otorgo descto por deposito y esta registrado en cocobranza_deposito
                    if (await depositos_cliente.Cobranza_Credito_Se_Dio_Descto_Deposito(_context, (int)reg.codcobranza))
                    {
                        ya_aplico = true;
                    }
                }
                else if (reg.codcobranza_contado != 0)
                {
                    // verificar si la cbza contado CE alguna vez ya otorgo descto por deposito y esta registrado en cocobranza_deposito
                    if (await depositos_cliente.Cobranza_Contado_Ce_Se_Dio_Descto_Deposito(_context, (int)reg.codcobranza_contado))
                    {
                        ya_aplico = true;
                    }
                }
                else if (reg.codanticipo != 0)
                {
                    // verificar el anticipo aplicado a proforma alguna vez ya otorgo descto por deposito y esta registrado en cocobranza_deposito
                    if (await depositos_cliente.Anticipo_Contado_Aplicado_A_Proforma_Se_Dio_Descto_Deposito(_context, (int)reg.codanticipo))
                    {
                        ya_aplico = true;
                    }
                }

                // se registra solo si no aplico antes
                if (!ya_aplico)
                {
                    try
                    {
                        var dtcbza = new dtcbza();
                        if (reg.codcobranza != 0)
                        {
                            dtcbza = await _context.cocobranza
                                .Where(i => i.codigo == reg.codcobranza)
                                .Select(i => new dtcbza
                                {
                                    codigo = i.codigo,
                                    id = i.id,
                                    numeroid = i.numeroid,
                                    cliente = i.cliente,
                                    nit = i.nit
                                }).FirstOrDefaultAsync();
                        }
                        else if (reg.codcobranza_contado != 0)
                        {
                            dtcbza = await _context.cocobranza_contado
                                .Where(i => i.codigo == reg.codcobranza_contado)
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
                                .Where(i => i.codigo == reg.codanticipo)
                                .Select(i => new dtcbza
                                {
                                    codigo = i.codigo,
                                    id = i.id,
                                    numeroid = i.numeroid,
                                    cliente = i.codcliente,
                                    nit = i.nit
                                }).FirstOrDefaultAsync();
                        }

                        // si la cobranza no esta registrada en cocobranza_deposito se registra
                        if (await Cobranzas.Registrar_Descuento_Por_Deposito_de_Cbza(_context, reg.codcobranza ?? 0, dtcbza.cliente, dtcbza.cliente, dtcbza.nit, codproforma, codempresa, usuarioreg))
                        {
                            resultado = true;
                        }
                        else
                        {
                            resultado = false;
                            break;
                        }
                    }
                    catch (Exception)
                    {
                        resultado = false;
                    }
                }
            }
            return resultado;
        }

        public async Task<decimal> DescuentoExtra_Porcentaje(DBContext _context, int coddesextra)
        {
            var result = await _context.vedesextra
                    .Where(v => v.codigo == coddesextra)
                    .Select(parametro => parametro.porcentaje)
                    .FirstOrDefaultAsync();
            return result;
        }
        public async Task<List<string>> DescuentosPromocionAplicadosAsync(DBContext _context, List<vedesextraDatos>? tabladescuentos, int coddesextraDepositos)
        {
            List<string> resultado = new List<string>();
            //genera una lista con los descuentos extra que son 
            //de promocion(diferenciados por item)
            //pero no inclutye el descto por deposito
            foreach (var reg in tabladescuentos)
            {
                if (!reg.coddesextra.ToString().Equals(coddesextraDepositos))
                {
                    int codDesextra = Convert.ToInt32(reg.coddesextra);
                    if (await DescuentoExtra_Diferenciado_x_item(_context, codDesextra))
                    {
                        resultado.Add(codDesextra.ToString());
                    }
                }
            }

            return resultado;
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

        public async Task<double> preciodelistaitem(DBContext _context, int codtarifa, string coditem)
        {
            if (codtarifa == 0 || coditem== "")
            {
                return 0;
            }
            double precioFinal;

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

        public async Task<double> preciocliente(DBContext _context, string codcliente, int codalmacen, int codtarifa, string coditem, string desc_linea_seg_solicitud, string desc_linea, string opcion_nivel)
        {
            if (codtarifa == 0 || coditem == "" || codcliente.Trim() == "" || codalmacen == 0)
            {
                return 0;
            }
            double precioFinal;

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
        public async Task<double> preciocondescitem(DBContext _context, string codcliente, int codalmacen, int codtarifa, string coditem, int coddescuento, string desc_linea_seg_solicitud, string desc_linea, string opcion_nivel)
        {
            if (codtarifa == 0 || coditem == "")
            {
                return 0;
            }

            double precioFinal;

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
                if (DBNull.Value.Equals(reg.subtotal_descto_extra) || reg.subtotal_descto_extra == 0)
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

        public async Task<double> DescuentoExtra_Porcentaje_Item(DBContext _context, int coddesextra, string coditem)
        {
            var resultado = await _context.vedesextra_item.Where(i => i.coddesextra == coddesextra && i.coditem == coditem).Select(i => i.porcentaje).FirstOrDefaultAsync();
            return (double)(resultado ?? 0);
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
        public async Task<bool> Solicitud_Descuento_Ya_Utilizada(DBContext _context, string idsolicitud, string nroidsolicitud)
        {
            bool resultado = false;
            try
            {
                //using (_context)
                //// using (var _context = DbContextFactory.Create(userConnectionString))
                //{
                var yaUtilizada = await _context.veproforma
                    .AnyAsync(v => v.idsoldesctos == idsolicitud && v.nroidsoldesctos == int.Parse(nroidsolicitud) && v.anulada == false && v.desclinea_segun_solicitud == true);
                resultado = yaUtilizada;
                //}
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return false;
            }
            return resultado;
        }
        public async Task<string> ProformaConSolicitudDescuentoYaUtilizada(DBContext _context, string idsolicitud, string nroidsolicitud)
        {
            string resultado = "";
            try
            {
                //using (_context)
                ////using (var _context = DbContextFactory.Create(userConnectionString))
                //{
                var proforma = await _context.veproforma
                .Where(p => p.anulada == false && p.desclinea_segun_solicitud == true && p.idsoldesctos == idsolicitud && p.nroidsoldesctos == int.Parse(nroidsolicitud))
                .Select(p => new { p.id, p.numeroid })
                .FirstOrDefaultAsync();

                if (proforma != null)
                {
                    resultado = $"{proforma.id}-{proforma.numeroid}";
                }
                //}
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return "";
            }
            return resultado;
        }
        public async Task<DateTime> Descuento_Extra_Valido_Desde_Fecha(DBContext _context, int coddesextra)
        {
            DateTime resultado = new DateTime(1900, 1, 1);
            try
            {
                //using (_context)
                ////using (var _context = DbContextFactory.Create(userConnectionString))
                //{
                var result = await _context.vedesextra
                    .Where(v => v.codigo == coddesextra)
                    .Select(v => v.valido_desde)
                    .FirstOrDefaultAsync();

                if (result.HasValue)
                {
                    resultado = result.Value;
                }
                // }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");

            }
            return resultado;
        }
        public async Task<DateTime> Descuento_Extra_Valido_Hasta_Fecha(DBContext _context, int coddesextra)
        {
            DateTime resultado = new DateTime(1900, 1, 1);
            try
            {
                //using (_context)
                ////using (var _context = DbContextFactory.Create(userConnectionString))
                //{
                var result = await _context.vedesextra
                    .Where(v => v.codigo == coddesextra)
                    .Select(v => v.valido_hasta)
                    .FirstOrDefaultAsync();

                if (result.HasValue)
                {
                    resultado = result.Value;
                }
                //}
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");

            }
            return resultado;
        }
        public async Task<(List<tablarecargos> tablarecargos,double ttl_recargos_sobre_total_final)> Recargos_Sobre_Total_Final(DBContext _context, double total, string codmoneda, DateTime fecha, string codempresa, List<tablarecargos> tablarecargos)
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

            if (resultado.Count() > 0)
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
        public async Task<bool> Descuento_Extra_Es_Pronto_Pago(DBContext _context, int coddesextra)
        {
            try
            {
                bool resultado = false;
                //using (_context)
                //{
                var result = await _context.vedesextra
                    .Where(v => v.codigo == coddesextra)
                    .Select(v => v.prontopago)
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

        public async Task<string> VersionTarifa(DBContext _context, int codtarifa)
        {
            try
            {
                var resultado = await _context.intarifa.Where(i => i.codigo == codtarifa).Select(i => new { i.version, i.codigo }).FirstOrDefaultAsync();
                if (resultado != null)
                {
                    return resultado.version;
                }
                return "";
            }
            catch (Exception)
            {
                return "";
            }
        }

        public async Task<string> VersionTarifaActual(DBContext _context)
        {
            try
            {
                var codigo = await _context.intarifa.OrderBy(i => i.codigo).Select(i => new {i.codigo}).FirstOrDefaultAsync();
                if (codigo != null)
                {
                    var resultado = await VersionTarifa(_context, codigo.codigo);
                    return resultado;
                }
                return "";
            }
            catch (Exception)
            {
                return "";
            }
        }

        public async Task<(bool bandera, string mensaje, List<tabladescuentos> tabladescuentos)> AdicionarDescuentoPorDeposito(DBContext _context, double subtotal_proforma, string codmoneda_prof, List<tabladescuentos> tabladescuentos, List<dtdepositos_pendientes> dt_depositos_pendientes, List<tblcbza_deposito> tblcbza_deposito, int codproforma, string codcliente, string codempresa)
        {
            // codproforma debe ser 0 si no hay proforma.

            bool resultado = false;

            //verifica si el descuento por desposito esta habilitado
            if (!await configuracion.emp_hab_descto_x_deposito(_context, codempresa))
            {
                return (false,"", tabladescuentos);
            }

            int coddesextra = await configuracion.emp_coddesextra_x_deposito(_context, codempresa);
            if (coddesextra <= 0)
            {
                return (false, "La aplicacion del descuento por deposito esta habilitado, sin embargo no se ha definido cual es el descuento por deposito.", tabladescuentos);
            }

            // obtener el porcentaje limite del subtotal de la proforma(hasta el cual se puede aplicar descuento por deposito)
            // Dim porcen_limite_descto As Double = sia_funciones.Ventas.Instancia.Porcentaje_Limite_Descuento_Deposito(subtotal_proforma)
            // desde 28-09-2021 (se configura por cliente el descto maximo de deposito)
            double porcen_limite_descto = await Porcentaje_Limite_Descuento_Deposito_Cliente(_context, codcliente);
            if (porcen_limite_descto == 0)
            {
                return (false, "El porcentaje limite del subtotal de la proforma definido hasta el cual se puede aplicar el descuento por deposito es cero, por tanto no se puede aplicar el descuento por deposito, verifique esta situacion en el perfil de datos del cliente.", tabladescuentos);
            }

            // ++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
            // Aplicacion del descuento por deposito segun especificaciones de la politica
            // ++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
            // 1° primero borrar los descuentos por deposito que haya en la tabla para aplicarlos de nuevo si corresponde
            // para no duplicar
            tabladescuentos = tabladescuentos.Where(i => i.coddesextra != coddesextra).ToList();


            // ++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
            // 2° Buscar en la tabla de depositos pendientes de aplicacion y añadirlos a la tabla de descuentos
            // ++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
            double Porcen_Descto = (double)await DescuentoExtra_Porcentaje(_context, coddesextra);
            int nro = 0;
            foreach (var reg in dt_depositos_pendientes)
            {
                double monto_descto = 0;
                // verificar si ya esta asignado el descuento por deposito para la cbza indicada
                // solo se añade un descuento por deposito por cada cbza
                if (reg.codcobranza == null)
                {
                    reg.codcobranza = 0;
                }

                var verifica = await Verificar_Si_CodCbza_Ya_Esta(_context, tabladescuentos, reg.codcobranza ?? 0, reg.tipo_pago);
                // si el mensaje de verifica no es vacio se debe devolver mas adelante
                if (verifica.bandera)
                {
                    if (reg.tipo_pago == "es_cbza_credito")
                    {
                        // **************SI ES UN DESCTO DE CBZA CREDITO
                        foreach (var k in tabladescuentos)
                        {
                            if (k.codcobranza == reg.codcobranza)
                            {
                                // si es un deposito ya grabado en cocobranza
                                // como descuento por deposito con saldo pendiente de aplicacion
                                // entonces de toma la totalidad del monto
                                if (reg.tipo == 0)
                                {
                                    monto_descto = (double)reg.monto_dis;
                                }
                                else
                                {
                                    monto_descto = Porcen_Descto * 0.01 * (double)reg.monto_dis;
                                    monto_descto = Math.Round(monto_descto, 2);
                                }
                                k.total_dist += (double)reg.monto_dis;
                                k.total_desc += monto_descto;
                                k.montodoc += (decimal)monto_descto;
                                k.montorest += (double)k.total_desc - (double)k.montodoc;
                            }
                        }

                    }
                    else if (reg.tipo_pago == "es_cbza_contado")
                    {
                        // **************SI ES UN DESCTO DE CBZA CONTADO
                        foreach (var k in tabladescuentos)
                        {
                            if (k.codcobranza_contado == reg.codcobranza)
                            {
                                // si es un deposito ya grabado en cocobranza
                                // como descuento por deposito con saldo pendiente de aplicacion
                                // entonces de toma la totalidad del monto
                                if (reg.tipo == 0)
                                {
                                    monto_descto = (double)reg.monto_dis;
                                }
                                else
                                {
                                    monto_descto = Porcen_Descto * 0.01 * (double)reg.monto_dis;
                                    monto_descto = Math.Round(monto_descto, 2);
                                }
                                k.total_dist += (double)reg.monto_dis;
                                k.total_desc += monto_descto;
                                k.montodoc += (decimal)monto_descto;
                                k.montorest += (double)k.total_desc - (double)k.montodoc;
                            }
                        }
                    }
                    else if (reg.tipo_pago == "es_anticipo_contado")
                    {
                        // **************SI ANTICIPO APLICADO A PROFORMA
                        foreach (var k in tabladescuentos)
                        {
                            if (k.codanticipo == reg.codcobranza)
                            {
                                // si es un anticipo aplicado a proforma
                                // como descuento por deposito con saldo pendiente de aplicacion
                                // entonces de toma la totalidad del monto
                                if (reg.tipo == 0)
                                {
                                    monto_descto = (double)reg.monto_dis;
                                }
                                else
                                {
                                    monto_descto = Porcen_Descto * 0.01 * (double)reg.monto_dis;
                                    monto_descto = Math.Round(monto_descto, 2);
                                }
                                k.total_dist += (double)reg.monto_dis;
                                k.total_desc += monto_descto;
                                k.montodoc += (decimal)monto_descto;
                                k.montorest += (double)k.total_desc - (double)k.montodoc;
                            }
                        }
                    }
                }
                else
                {
                    tabladescuentos dr = new tabladescuentos();
                    dr.coddesextra = coddesextra;
                    dr.descrip = await nombres.nombredesextra(_context, coddesextra);
                    // tipo=0 si es un deposito ya grabado en cocobranza
                    // como descuento por deposito con saldo pendiente de aplicacion
                    // entonces de toma la totalidad del monto
                    if (reg.tipo_pago == "es_cbza_credito")
                    {
                        if (reg.tipo == 0)
                        {
                            // es saldo restante de descto, por eso el monto del descuento es el 100 
                            dr.porcen = 100;
                            monto_descto = (double)reg.monto_dis;
                            dr.total_desc = monto_descto;
                            dr.montodoc = (decimal)monto_descto;
                        }else if(reg.tipo == 1)
                        {
                            // es deposito nuevo
                            dr.porcen = (decimal)Porcen_Descto;
                            monto_descto = (double)dr.porcen * 0.01 * (double)reg.monto_dis;
                            monto_descto = Math.Round(monto_descto, 2);
                            dr.total_desc = monto_descto;
                            dr.montodoc = (decimal)monto_descto;
                        }
                        else
                        {
                            //es deposito nuevo
                            dr.porcen = (decimal)Porcen_Descto;
                            monto_descto = (double)dr.porcen * 0.01 * (double)reg.monto_dis;
                            monto_descto = Math.Round(monto_descto, 2);
                            dr.total_desc = monto_descto;
                            dr.montodoc = (decimal)monto_descto;
                        }
                        // si es deposito de cobranza al credito
                        dr.codcobranza = reg.codcobranza;
                        dr.codcobranza_contado = 0;
                        dr.codanticipo = 0;

                        dr.total_dist = (double)reg.monto_dis;
                        dr.montorest = dr.total_desc - (double)dr.montodoc;
                        // Desde 17-04-2023 ya no toma la moneda de moncbza sino de monpago
                        dr.codmoneda = reg.moncbza;
                        // dr("codmoneda") = reg("monpago")
                    }
                    else if(reg.tipo_pago == "es_cbza_contado")
                    {
                        if (reg.tipo == 0)
                        {
                            // es saldo restante de descto, por eso el monto del descuento es el 100 
                            dr.porcen = 100;
                            monto_descto = (double)reg.monto_dis;
                            dr.total_desc = monto_descto;
                            dr.montodoc = (decimal)monto_descto;
                        }
                        else if (reg.tipo == 1)
                        {
                            // es deposito nuevo
                            dr.porcen = (decimal)Porcen_Descto;
                            monto_descto = (double)dr.porcen * 0.01 * (double)reg.monto_dis;
                            monto_descto = Math.Round(monto_descto, 2);
                            dr.total_desc = monto_descto;
                            dr.montodoc = (decimal)monto_descto;
                        }
                        else
                        {
                            //es deposito nuevo
                            dr.porcen = (decimal)Porcen_Descto;
                            monto_descto = (double)dr.porcen * 0.01 * (double)reg.monto_dis;
                            monto_descto = Math.Round(monto_descto, 2);
                            dr.total_desc = monto_descto;
                            dr.montodoc = (decimal)monto_descto;
                        }
                        // si es cobranza contado
                        dr.codcobranza = 0;
                        dr.codcobranza_contado = reg.codcobranza;
                        dr.codanticipo = 0;

                        dr.total_dist = (double)reg.monto_dis;
                        dr.montorest = dr.total_desc - (double)dr.montodoc;
                        // Desde 17-04-2023 ya no toma la moneda de moncbza sino de monpago
                        dr.codmoneda = reg.moncbza;
                        // dr("codmoneda") = reg("monpago")
                    }
                    else if(reg.tipo_pago == "es_anticipo_contado")
                    {
                        if (reg.tipo == 0)
                        {
                            // es saldo restante de descto, por eso el monto del descuento es el 100 
                            dr.porcen = 100;
                            monto_descto = (double)reg.monto_dis;
                            dr.total_desc = monto_descto;
                            dr.montodoc = (decimal)monto_descto;
                        }
                        else if (reg.tipo == 1)
                        {
                            // es deposito nuevo
                            dr.porcen = (decimal)Porcen_Descto;
                            monto_descto = (double)dr.porcen * 0.01 * (double)reg.monto_dis;
                            monto_descto = Math.Round(monto_descto, 2);
                            dr.total_desc = monto_descto;
                            dr.montodoc = (decimal)monto_descto;
                        }
                        else
                        {
                            //es deposito nuevo
                            dr.porcen = (decimal)Porcen_Descto;
                            monto_descto = (double)dr.porcen * 0.01 * (double)reg.monto_dis;
                            monto_descto = Math.Round(monto_descto, 2);
                            dr.total_desc = monto_descto;
                            dr.montodoc = (decimal)monto_descto;
                        }
                        // si es cobranza contado
                        dr.codcobranza = 0;
                        dr.codcobranza_contado = 0;
                        dr.codanticipo = reg.codcobranza;

                        dr.total_dist = (double)reg.monto_dis;
                        dr.montorest = dr.total_desc - (double)dr.montodoc;
                        // Desde 17-04-2023 ya no toma la moneda de moncbza sino de monpago
                        dr.codmoneda = reg.moncbza;
                        // dr("codmoneda") = reg("monpago")
                    }

                    tabladescuentos.Add(dr);
                    nro += 1;
                }
            }

            // ++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
            // 3° verificar que el monto del descuento no supere el 10% o el limite definido del subtotal de la proforma
            // ++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
            // verificar si hay descuentos por depositos 
            // que estan demas, es decir que su monto de descto supera el 10%  del subtotal(el monto limite de descuento)

            double tot_descto_deposito = 0;
            double limite_descuento_deposito = subtotal_proforma * porcen_limite_descto * 0.01;
            limite_descuento_deposito = Math.Round(limite_descuento_deposito, 2);
            double diferencia = 0;

            List<tabladescuentos> tabladescuentos_aux = new List<tabladescuentos>();
            string monedae = await empresa.monedaext(_context, codempresa);
            DateTime fecha = await funciones.FechaDelServidor(_context);

            foreach (var reg in tabladescuentos)
            {
                if (reg.coddesextra == coddesextra)
                {
                    // si es descto por deposito, solo copiar los necesarios
                    if (codmoneda_prof == reg.codmoneda)
                    {
                        // si la moneda de la pf es igual a la moneda del descuento por deposito no convertir
                        if (tot_descto_deposito == 0)
                        {
                            if ((tot_descto_deposito + (double)reg.montodoc) >= limite_descuento_deposito)
                            {
                                // si el acumulado mas el nuevo descuento ya supera el 10% ya no se lo toma en cuenta
                                tabladescuentos_aux.Add(reg);
                                tot_descto_deposito += (double)reg.montodoc;
                                break;
                            }
                            else
                            {
                                // si no supera se lo toma en cuenta
                                tabladescuentos_aux.Add(reg);
                                tot_descto_deposito += (double)reg.montodoc;
                            }
                        }
                        else
                        {
                            if ((tot_descto_deposito + (double)reg.montodoc) >= limite_descuento_deposito)
                            {
                                // si el acumulado mas el nuevo descuento ya supera el 10% ya no se lo toma en cuenta
                                tabladescuentos_aux.Add(reg);
                                tot_descto_deposito += (double)reg.montodoc;
                                break;
                            }
                            else
                            {
                                // si no supera se lo toma en cuenta
                                tabladescuentos_aux.Add(reg);
                                tot_descto_deposito += (double)reg.montodoc;
                            }
                        }
                    }
                    else
                    {
                        // si la moneda de la pf esta en BS y el descuento en US, entonces convertir el descuento en BS
                        if ("BS" == codmoneda_prof && "US" == reg.codmoneda)
                        {
                            if (tot_descto_deposito == 0)
                            {
                                if ((tot_descto_deposito + (double)await tipocambio._conversion(_context,codmoneda_prof, reg.codmoneda, fecha,reg.montodoc) >= limite_descuento_deposito))
                                {
                                    // si el acumulado mas el nuevo descuento ya supera el 10% ya no se lo toma en cuenta
                                    tabladescuentos_aux.Add(reg);
                                    tot_descto_deposito += (double)await tipocambio._conversion(_context, codmoneda_prof, reg.codmoneda, fecha, reg.montodoc);
                                    break;
                                }
                                else
                                {
                                    // si no supera se lo toma en cuenta
                                    tabladescuentos_aux.Add(reg);
                                    tot_descto_deposito += (double)await tipocambio._conversion(_context, codmoneda_prof, reg.codmoneda, fecha, reg.montodoc);
                                }
                            }
                            else
                            {
                                if ((tot_descto_deposito + (double)await tipocambio._conversion(_context, codmoneda_prof, reg.codmoneda, fecha, reg.montodoc) >= limite_descuento_deposito))
                                {
                                    // si el acumulado mas el nuevo descuento ya supera el 10% ya no se lo toma en cuenta
                                    tabladescuentos_aux.Add(reg);
                                    tot_descto_deposito += (double)await tipocambio._conversion(_context, codmoneda_prof, reg.codmoneda, fecha, reg.montodoc);
                                    break;
                                }
                                else
                                {
                                    // si no supera se lo toma en cuenta
                                    tabladescuentos_aux.Add(reg);
                                    tot_descto_deposito += (double)await tipocambio._conversion(_context, codmoneda_prof, reg.codmoneda, fecha, reg.montodoc);
                                }
                            }
                        }
                        // si la moneda de la pf esta en US y el descuento en BS, entonces convertir el descuento en US
                        if ("US" == codmoneda_prof && "BS" == reg.codmoneda)
                        {
                            if (tot_descto_deposito == 0)
                            {
                                if ((tot_descto_deposito + (double)await tipocambio._conversion(_context,codmoneda_prof,reg.codmoneda,fecha,reg.montodoc)) >= limite_descuento_deposito)
                                {
                                    // si el acumulado mas el nuevo descuento ya supera el 10% ya no se lo toma en cuenta
                                    tabladescuentos_aux.Add(reg);
                                    tot_descto_deposito += (double)await tipocambio._conversion(_context, codmoneda_prof, reg.codmoneda, fecha, reg.montodoc);
                                    break;
                                }
                                else
                                {
                                    // si no supera se lo toma en cuenta
                                    tabladescuentos_aux.Add(reg);
                                    tot_descto_deposito += (double)await tipocambio._conversion(_context, codmoneda_prof, reg.codmoneda, fecha, reg.montodoc);
                                }
                            }
                            else
                            {
                                if ((tot_descto_deposito + (double)await tipocambio._conversion(_context, codmoneda_prof, reg.codmoneda, fecha, reg.montodoc)) >= limite_descuento_deposito)
                                {
                                    // si el acumulado mas el nuevo descuento ya supera el 10% ya no se lo toma en cuenta
                                    tabladescuentos_aux.Add(reg);
                                    tot_descto_deposito += (double)await tipocambio._conversion(_context, codmoneda_prof, reg.codmoneda, fecha, reg.montodoc);
                                    break;
                                }
                                else
                                {
                                    // si no supera se lo toma en cuenta
                                    tabladescuentos_aux.Add(reg);
                                    tot_descto_deposito += (double)await tipocambio._conversion(_context, codmoneda_prof, reg.codmoneda, fecha, reg.montodoc);
                                }
                            }
                        }
                    }
                }
                else
                {
                    tabladescuentos_aux.Add(reg);
                }
            }

            tabladescuentos.Clear();
            tabladescuentos = tabladescuentos_aux;



            // ++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
            // Totalizar el descuento por deposito
            // ++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
            tot_descto_deposito = 0;
            foreach (var reg in tabladescuentos)
            {
                if (reg.coddesextra == coddesextra)
                {
                    if (codmoneda_prof == reg.codmoneda)
                    {
                        tot_descto_deposito += (double)reg.montodoc;
                    }
                    else
                    {
                        // si la moneda de la pf esta en BS y el descuento en US, entonces convertir el descuento en BS
                        if ("BS" == codmoneda_prof && "US" == reg.codmoneda)
                        {
                            tot_descto_deposito += (double)await tipocambio._conversion(_context, codmoneda_prof, reg.codmoneda, fecha, reg.montodoc);
                        }
                        // si la moneda de la pf esta en US y el descuento en BS, entonces convertir el descuento en US
                        if ("US" == codmoneda_prof && "BS" == reg.codmoneda)
                        {
                            // convertir el descuento que esta en US a BS
                            tot_descto_deposito += (double)await tipocambio._conversion(_context, codmoneda_prof, reg.codmoneda, fecha, reg.montodoc);
                        }
                    }
                    // tot_descto_deposito += reg("montodoc")
                }
            }
            tot_descto_deposito = Math.Round(tot_descto_deposito, 2);

            // ++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
            // Realizar el ajuste del total del descuento para que no supere el 10%
            // el ajuste se realiza solo en la cbza que ocaciona que el descuento se exceda
            // ++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
            tblcbza_deposito.Clear();
            if (tot_descto_deposito > limite_descuento_deposito)
            {
                diferencia = tot_descto_deposito - limite_descuento_deposito;
                diferencia = Math.Round(diferencia, 2);
                tot_descto_deposito = 0;
                foreach (var reg in tabladescuentos)
                {
                    if (reg.coddesextra == coddesextra)
                    {
                        if (codmoneda_prof == reg.codmoneda)
                        {
                            if (tot_descto_deposito == 0)
                            {
                                if ((double)reg.montodoc > limite_descuento_deposito)
                                {
                                    // +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
                                    // Relizar el ajuste
                                    reg.montodoc -= (decimal)diferencia;
                                    reg.montorest = reg.total_desc - (double)reg.montodoc;
                                    tot_descto_deposito += (double)reg.montodoc;
                                    // +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

                                    /*
                                    ''//++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
                                    ''//añadir la cbza-deposito a la tabla cocobranza_deposito
                                    'Dim dr As DataRow
                                    'dr = tblcbza_deposito.NewRow
                                    'dr("codproforma") = codproforma
                                    'dr("codcobranza") = reg("codcobranza")
                                    'dr("montodist") = reg("total_dist")
                                    'dr("montodescto") = reg("montodoc")
                                    'dr("montorest") = reg("montodoc") - limite_descuento_deposito
                                    'tblcbza_deposito.Rows.Add(dr)
                                    ''//++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++ 
                                     */
                                }
                                else
                                {
                                    tot_descto_deposito += (double)reg.montodoc;
                                }
                            }
                            else
                            {
                                if ((tot_descto_deposito + (double)reg.montodoc) > limite_descuento_deposito)
                                {
                                    // ++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
                                    // Relizar el ajuste
                                    reg.montodoc -= (decimal)diferencia;
                                    reg.montorest = reg.total_desc - (double)reg.montodoc;
                                    tot_descto_deposito += (double)reg.montodoc;
                                    // +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

                                    /*
                                    ''//++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
                                    ''//añadir la cbza-deposito a la tabla cocobranza_deposito
                                    'Dim dr As DataRow
                                    'dr = tblcbza_deposito.NewRow
                                    'dr("codproforma") = codproforma
                                    'dr("codcobranza") = reg("codcobranza")
                                    'dr("montodist") = reg("total_dist")
                                    'dr("montodescto") = reg("montodoc")
                                    'dr("montorest") = reg("montodoc") - limite_descuento_deposito
                                    'tblcbza_deposito.Rows.Add(dr)
                                    ''//++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++ 
                                     */
                                    break;
                                }
                                else
                                {
                                    tot_descto_deposito += (double)reg.montodoc;
                                }
                            }
                        }
                        else
                        {
                            if (codmoneda_prof == "BS" && reg.codmoneda == "BS")
                            {
                                if (tot_descto_deposito == 0)
                                {
                                    if ((double)reg.montodoc > limite_descuento_deposito)
                                    {
                                        // +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
                                        // Relizar el ajuste
                                        reg.montodoc -= (decimal)diferencia;
                                        reg.montorest = reg.total_desc - (double)reg.montodoc;
                                        tot_descto_deposito += (double)reg.montodoc;
                                        // +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

                                        /*
                                        ''//++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
                                        ''//añadir la cbza-deposito a la tabla cocobranza_deposito
                                        'Dim dr As DataRow
                                        'dr = tblcbza_deposito.NewRow
                                        'dr("codproforma") = codproforma
                                        'dr("codcobranza") = reg("codcobranza")
                                        'dr("montodist") = reg("total_dist")
                                        'dr("montodescto") = reg("montodoc")
                                        'dr("montorest") = reg("montodoc") - limite_descuento_deposito
                                        'tblcbza_deposito.Rows.Add(dr)
                                        ''//++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++ 
                                         */
                                    }
                                    else
                                    {
                                        tot_descto_deposito += (double)reg.montodoc;
                                    }
                                }
                                else
                                {
                                    if ((tot_descto_deposito + (double)reg.montodoc) > limite_descuento_deposito)
                                    {
                                        // ++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
                                        // Relizar el ajuste
                                        reg.montodoc -= (decimal)diferencia;
                                        reg.montorest = reg.total_desc - (double)reg.montodoc;
                                        tot_descto_deposito += (double)reg.montodoc;
                                        // +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

                                        /*
                                        ''//++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
                                        ''//añadir la cbza-deposito a la tabla cocobranza_deposito
                                        'Dim dr As DataRow
                                        'dr = tblcbza_deposito.NewRow
                                        'dr("codproforma") = codproforma
                                        'dr("codcobranza") = reg("codcobranza")
                                        'dr("montodist") = reg("total_dist")
                                        'dr("montodescto") = reg("montodoc")
                                        'dr("montorest") = reg("montodoc") - limite_descuento_deposito
                                        'tblcbza_deposito.Rows.Add(dr)
                                        ''//++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++ 
                                         */
                                        break;
                                    }
                                    else
                                    {
                                        tot_descto_deposito += (double)reg.montodoc;
                                    }
                                }
                            }
                            else
                            {
                                if (tot_descto_deposito == 0)
                                {
                                    if ((double)await tipocambio._conversion(_context,codmoneda_prof,reg.codmoneda,fecha,reg.montodoc) > limite_descuento_deposito)
                                    {
                                        // +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
                                        // Relizar el ajuste
                                        // reg("montodoc") = sia_funciones.TipoCambio.Instancia.conversion(codmoneda_prof, reg("codmoneda"), fecha, reg("montodoc"))
                                        reg.montodoc -= await tipocambio._conversion(_context, reg.codmoneda, codmoneda_prof, fecha, (decimal)diferencia);
                                        // reg("montorest") = sia_funciones.TipoCambio.Instancia.conversion(codmoneda_prof, reg("codmoneda"), fecha, reg("total_desc")) - reg("montodoc")
                                        reg.montorest = reg.total_desc - (double)reg.montodoc;
                                        tot_descto_deposito += (double)await tipocambio._conversion(_context, codmoneda_prof, reg.codmoneda, fecha, reg.montodoc);
                                        // +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
                                        /*
                                         
                                        ''//++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
                                        ''//añadir la cbza-deposito a la tabla cocobranza_deposito
                                        'Dim dr As DataRow
                                        'dr = tblcbza_deposito.NewRow
                                        'dr("codproforma") = codproforma
                                        'dr("codcobranza") = reg("codcobranza")
                                        'dr("montodist") = reg("total_dist")
                                        'dr("montodescto") = reg("montodoc")
                                        'dr("montorest") = reg("montodoc") - limite_descuento_deposito
                                        'tblcbza_deposito.Rows.Add(dr)
                                        ''//++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
                                         
                                         */
                                    }
                                    else
                                    {
                                        tot_descto_deposito += (double)await tipocambio._conversion(_context, codmoneda_prof, reg.codmoneda, fecha, reg.montodoc);
                                    }
                                }
                                else
                                {
                                    if (tot_descto_deposito + (double)await tipocambio._conversion(_context, codmoneda_prof, reg.codmoneda, fecha, reg.montodoc) > limite_descuento_deposito)
                                    {
                                        // +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
                                        // Relizar el ajuste
                                        // reg("montodoc") = sia_funciones.TipoCambio.Instancia.conversion(codmoneda_prof, reg("codmoneda"), fecha, reg("montodoc"))
                                        reg.montodoc -= await tipocambio._conversion(_context, reg.codmoneda, codmoneda_prof, fecha, (decimal)diferencia);
                                        // reg("montorest") = sia_funciones.TipoCambio.Instancia.conversion(codmoneda_prof, reg("codmoneda"), fecha, reg("total_desc")) - reg("montodoc")
                                        reg.montorest = reg.total_desc - (double)reg.montodoc;
                                        tot_descto_deposito += (double)await tipocambio._conversion(_context, codmoneda_prof, reg.codmoneda, fecha, reg.montodoc);
                                        // +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
                                        /*
                                         
                                        ''//++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
                                        ''//añadir la cbza-deposito a la tabla cocobranza_deposito
                                        'Dim dr As DataRow
                                        'dr = tblcbza_deposito.NewRow
                                        'dr("codproforma") = codproforma
                                        'dr("codcobranza") = reg("codcobranza")
                                        'dr("montodist") = reg("total_dist")
                                        'dr("montodescto") = reg("montodoc")
                                        'dr("montorest") = reg("montodoc") - limite_descuento_deposito
                                        'tblcbza_deposito.Rows.Add(dr)
                                        ''//++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
                                         
                                         */
                                        break;
                                    }
                                    else
                                    {
                                        tot_descto_deposito += (double)await tipocambio._conversion(_context, codmoneda_prof, reg.codmoneda, fecha, reg.montodoc);
                                    }
                                }
                            }
                        }
                    }
                }


            }
            else
            {
                // ++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
                // verificar si se añadio en la tabla 
                // cocobranza_deposito las cbza que son de saldos pendientes
                // ++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
                List<int> lista_saldos = dt_depositos_pendientes.Where(i => i.tipo_pago == "es_cbza_credito").Select(i => i.codcobranza ?? 0).ToList();
                List<int> lista_saldos_contado = dt_depositos_pendientes.Where(i => i.tipo_pago == "es_cbza_contado").Select(i => i.codcobranza ?? 0).ToList();
                List<int> lista_saldos_anticipos_contado = dt_depositos_pendientes.Where(i => i.tipo_pago == "es_anticipo_contado").Select(i => i.codcobranza ?? 0).ToList();

                tblcbza_deposito.Clear();
                foreach (var reg in tabladescuentos)
                {
                    if (reg.codcobranza_contado == null)
                    {
                        reg.codcobranza_contado = 0;
                    }
                    if (reg.codcobranza == null)
                    {
                        reg.codcobranza = 0;
                    }
                    if (reg.codanticipo == null)
                    {
                        reg.codanticipo = 0;
                    }

                    if (reg.codcobranza != 0 && !lista_saldos.Contains(reg.codcobranza ?? 0))
                    {
                        tblcbza_deposito dr = new tblcbza_deposito();
                        dr.codproforma = codproforma;
                        dr.codcobranza = reg.codcobranza ?? 0;
                        dr.codcobranza_contado = 0;
                        dr.codanticipo = 0;
                        dr.montodist = reg.total_dist;
                        dr.montodescto = (double)reg.montodoc;
                        dr.montorest = (double)reg.montodoc;
                        // dr("codmoneda") = reg("moncbza")
                        tblcbza_deposito.Add(dr);
                    }
                    if (reg.codcobranza_contado != 0 && !lista_saldos_contado.Contains(reg.codcobranza_contado ?? 0))
                    {
                        tblcbza_deposito dr = new tblcbza_deposito();
                        dr.codproforma = codproforma;
                        dr.codcobranza = 0;
                        dr.codcobranza_contado = reg.codcobranza_contado ?? 0;
                        dr.codanticipo = 0;
                        dr.montodist = reg.total_dist;
                        dr.montodescto = (double)reg.montodoc;
                        dr.montorest = (double)reg.montodoc;
                        // dr("codmoneda") = reg("moncbza")
                        tblcbza_deposito.Add(dr);
                    }
                    if (reg.codanticipo != 0 && !lista_saldos_anticipos_contado.Contains(reg.codanticipo ?? 0))
                    {
                        tblcbza_deposito dr = new tblcbza_deposito();
                        dr.codproforma = codproforma;
                        dr.codcobranza = 0;
                        dr.codcobranza_contado = 0;
                        dr.codanticipo = reg.codanticipo ?? 0;
                        dr.montodist = reg.total_dist;
                        dr.montodescto = (double)reg.montodoc;
                        dr.montorest = (double)reg.montodoc;
                        // dr("codmoneda") = reg("moncbza")
                        tblcbza_deposito.Add(dr);
                    }
                }
            }

            // aqui verificar si la moneda de la tabla de los descuentos es igual a la moneda de la proforma sino es igual convertir a la moneda de la proforma
            foreach (var reg in tabladescuentos)
            {
                if (reg.coddesextra == coddesextra)
                {
                    if (reg.codmoneda != codmoneda_prof)
                    {
                        // si la moneda de la pf esta en BS y el descuento en US, entonces convertir el descuento en BS
                        if ("BS" == codmoneda_prof && "US" == reg.codmoneda)
                        {
                            reg.montodoc = (decimal)await siat.Redondeo_Decimales_SIA_2_decimales_SQL(_context,(double)await tipocambio._conversion(_context, codmoneda_prof, reg.codmoneda, fecha, reg.montodoc));
                            reg.total_dist = (double)await siat.Redondeo_Decimales_SIA_2_decimales_SQL(_context, (double)await tipocambio._conversion(_context, codmoneda_prof, reg.codmoneda, fecha, (decimal)reg.total_dist));
                            reg.total_desc = (double)await siat.Redondeo_Decimales_SIA_2_decimales_SQL(_context, (double)await tipocambio._conversion(_context, codmoneda_prof, reg.codmoneda, fecha, (decimal)reg.total_desc));
                            reg.codmoneda = "BS";
                        }
                        // si la moneda de la pf esta en US y el descuento en BS, entonces convertir el descuento en US
                        if ("US" == codmoneda_prof && "BS" == reg.codmoneda)
                        {
                            reg.montodoc = (decimal)await siat.Redondeo_Decimales_SIA_2_decimales_SQL(_context, (double)await tipocambio._conversion(_context, codmoneda_prof, reg.codmoneda, fecha, reg.montodoc));
                            reg.total_dist = (double)await siat.Redondeo_Decimales_SIA_2_decimales_SQL(_context, (double)await tipocambio._conversion(_context, codmoneda_prof, reg.codmoneda, fecha, (decimal)reg.total_dist));
                            reg.total_desc = (double)await siat.Redondeo_Decimales_SIA_2_decimales_SQL(_context, (double)await tipocambio._conversion(_context, codmoneda_prof, reg.codmoneda, fecha, (decimal)reg.total_desc));
                            reg.codmoneda = "US";
                        }
                    }
                }
            }
            if (nro>0)
            {
                resultado = true;
            }
            else
            {
                resultado = false;
            }
            return (resultado, "", tabladescuentos);
        }



        




        public async Task<(bool bandera, string mensaje)> Verificar_Si_CodCbza_Ya_Esta(DBContext _context, List<tabladescuentos> tabladescuentos, int cod_cbza, string tipo_pago)
        {
            /*
                '--tipo pago  es_cbza_credito
                '--tipo pago  es_cbza_contado
                '--tipo pago  es_anticipo
             */

            // si el valor de cod cbza credito s dfte de cero quiere decir que es cbza de credito
            if (tipo_pago == "es_cbza_credito")
            {
                //// busca si la cbza CREDITO ya esta
                var prueba = tabladescuentos.Where(i => i.codcobranza == cod_cbza).FirstOrDefault();
                if (prueba != null)
                {
                    return (true, "");
                }
            }
            else if(tipo_pago == "es_cbza_contado")
            {
                var prueba = tabladescuentos.Where(i => i.codcobranza_contado == cod_cbza).FirstOrDefault();
                if (prueba != null)
                {
                    return (true, "");
                }
            }
            else if(tipo_pago == "es_anticipo_contado")
            {
                var prueba = tabladescuentos.Where(i => i.codanticipo == cod_cbza).FirstOrDefault();
                if (prueba != null)
                {
                    return (true,"");
                }
            }
            else
            {
                // si no concide con ninguna mejor que devuelva true es decir que diga que la cocobranza ya esta
                return (true, "No se pudo verificar si la cobranza que esta enlazada a deposito ya esta en la tabla de descuentos del documento actual de venta!!!");
            }
            return (false, "");
        }

        public async Task<double> Porcentaje_Limite_Descuento_Deposito_Cliente(DBContext _context, string codcliente)
        {
            try
            {
                var dt = await _context.vecliente.Where(i => i.codigo == codcliente).FirstOrDefaultAsync();
                if (dt != null)
                {
                    return (double)(dt.porcentaje_limite_descto_deposito ?? 0);
                }
                return 0;
            }
            catch (Exception)
            {
                return 0;
            }
        }
        public async Task<List<veproforma1>> Actualizar_Peso_Detalle_Proforma(DBContext _context, List<veproforma1> veproforma1)
        {
            foreach (var reg in veproforma1)
            {
                var pesoItem = await _context.initem.Where(i => i.codigo == reg.coditem).Select(i => i.peso).FirstOrDefaultAsync() ?? 0;
                reg.peso = pesoItem * reg.cantidad;
            }
            return veproforma1;
        }
        public async Task<List<veremision1>> Remision_Cargar_Grupomer(DBContext _context, List<veremision1> veremision1)
        {
            foreach (var reg in veremision1)
            {
                reg.codgrupomer = await _context.inlinea
                    .Join(
                        _context.initem,
                        l => l.codigo,
                        i => i.codlinea,
                        (l, i) => new { l, i }
                    )
                    .Where(li => li.i.codigo == reg.coditem)
                    .Select(li => li.l.codgrupomer)
                    .FirstOrDefaultAsync();
            }
            return veremision1;
        }
        public async Task<List<veremision1>> Actualizar_Peso_Detalle_Remision(DBContext _context, List<veremision1> veremision1)
        {
            foreach (var reg in veremision1)
            {
                var pesoItem = await _context.initem.Where(i => i.codigo == reg.coditem).Select(i => i.peso).FirstOrDefaultAsync() ?? 0;
                reg.peso = pesoItem * reg.cantidad;
            }
            return veremision1;
        }
        public async Task<bool> Cliente_de_vendedor(DBContext _context, string codcliente, int codvendedor)
        {
            try
            {
                bool resultado = true;
                //using (_context)
                //{
                int count = _context.vecliente
                    .Where(vc => vc.codigo == codcliente && vc.codvendedor == codvendedor)
                    .Count();
                resultado = count > 0;
                return resultado;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return false;
            }
        }
        public async Task<string> Descuento_Extra_Tipo_Venta(DBContext _context, int coddesextra)
        {
            try
            {
                string resultado = "";
                //using (_context)
                //{
                var result = await _context.vedesextra
                    .Where(v => v.codigo == coddesextra)
                    .Select(v => v.tipo_venta)
                    .FirstOrDefaultAsync();
                if (result != null)
                {
                    resultado = result;
                }
                else { resultado = "SIN RESTRICCION"; }
                //}
                return resultado;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return "SIN RESTRICCION";
            }
        }
        public async Task<List<string>> AlinearItemsAsync(DBContext _context, List<string> listaItems)
        {
            List<string> resultado = new List<string>();

            try
            {
                string cadenaItems = string.Join(", ", listaItems.Select(item => $"'{item}'"));

                //using (_context)
                ////using (var _context = DbContextFactory.Create(userConnectionString))
                //{
                var query = from p1 in _context.initem
                            join p2 in _context.inlinea on p1.codlinea equals p2.codigo
                            where listaItems.Contains(p1.codigo)
                            select p1.codlinea;

                resultado = await query.Distinct().ToListAsync();
                // }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                // Manejar la excepción según tus necesidades.
                throw;
            }

            return resultado;
        }
        public async Task<List<string>> ListaPromocionesPorLineaAplicablesAsync(DBContext _context, string codLinea, int codTarifa, string codCliente, DateTime fechaProforma, double subttlDoc, string codMonedaDoc, string tipoVta)
        {
            List<string> resultado = new List<string>();

            try
            {
                //using (_context)
                ////using (var _context = DbContextFactory.Create(userConnectionString))
                //{
                var result = (from p1 in _context.vedesextra_item
                              join p2 in _context.initem on p1.coditem equals p2.codigo
                              join p3 in _context.inlinea on p2.codlinea equals p3.codigo
                              join p4 in _context.ingrupo on p3.codgrupo equals p4.codigo
                              join p5 in _context.vedesextra on p1.coddesextra equals p5.codigo
                              where p2.codlinea == codLinea &&
                                    p1.porcentaje > 0 &&
                                    p1.habilitado_para_desc == true &&
                                    _context.vedesextra
                                        .Where(v => v.habilitado == true && v.diferenciado_x_item == true &&
                                                    fechaProforma >= v.valido_desde && fechaProforma <= v.valido_hasta)
                                        .Select(v => v.codigo)
                                        .Contains(p1.coddesextra ?? -1) &&
                                    _context.vedesextra_tarifa
                                        .Where(t => t.codtarifa == codTarifa)
                                        .Select(t => t.coddesextra)
                                        .Contains(p1.coddesextra ?? -1) &&
                                    _context.vecliente_desextra
                                        .Where(c => c.codcliente == codCliente)
                                        .Select(c => c.coddesextra)
                                        .Contains(p1.coddesextra)
                              select new
                              {
                                  Codlinea = p2.codlinea,
                                  Descripcion = p3.descripcion,
                                  p1.coddesextra,
                                  p1.porcentaje,
                                  ValidaLineaCredito = p5.valida_linea_credito,
                                  MinContado = p5.min_contado,
                                  CodmonedaMinContado = p5.codmoneda_min_contado,
                                  MinCredito = p5.min_credito,
                                  CodmonedaMinCredito = p5.codmoneda_min_credito
                              }).Distinct().OrderBy(p1 => p1.coddesextra).ToList();

                resultado.Clear();
                foreach (var item in result)
                {
                    double montoMinDescto = 0;

                    if (item.ValidaLineaCredito == true)
                    {
                        //verificar si el cliente tiene linea de credito valida o si es cliente PERTEC
                        if (await creditos.Cliente_Tiene_Linea_De_Credito_Valida(_context, codCliente) == true || await cliente.EsClientePertec(_context, codCliente) == true)
                        {
                            if (tipoVta == "CONTADO")
                            {
                                montoMinDescto = (double)await tipocambio._conversion(_context, codMonedaDoc, item.CodmonedaMinContado, fechaProforma, item.MinContado ?? 0);
                            }
                            else
                             {
                                montoMinDescto = (double)await tipocambio._conversion(_context, codMonedaDoc, item.CodmonedaMinCredito, fechaProforma, item.MinCredito ?? 0);
                            }

                            if (subttlDoc >= montoMinDescto)
                            {
                                resultado.Add(item.coddesextra.ToString());
                            }
                        }
                    }
                    else
                    {
                        if (tipoVta == "CONTADO")
                        {
                            montoMinDescto = (double)await tipocambio._conversion(_context, codMonedaDoc, item.CodmonedaMinContado, fechaProforma, item.MinContado ?? 0);
                        }
                        else
                        {
                            montoMinDescto = (double)await tipocambio._conversion(_context, codMonedaDoc, item.CodmonedaMinCredito, fechaProforma, item.MinCredito ?? 0);
                        }

                        if (subttlDoc >= montoMinDescto)
                        {
                            resultado.Add(item.coddesextra.ToString());
                        }
                    }
                }
                //}
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                // Manejar la excepción según tus necesidades.
                throw;
            }

            return resultado;
        }

        public string MostrarMensajesPromocionesAplicar(DataTable dt_promo)
        {
            string cadena = "";

            if (dt_promo.Rows.Count > 0)
            {
                cadena = funciones.Rellenar("DESCUENTOS POR APLICAR y/o DESCUENTOS DE LINEA POR APLICAR", 118, " ", false) + "\n";
                cadena += "---------------------------------------------------------------------------------------------\n";
                cadena += funciones.Rellenar("COD    COD    DESCRIPCION           NIV  NIV  NIV POR   DESCTO    DESCTO          DESCTO POR", 96, " ", false) + "\n";
                cadena += funciones.Rellenar("GRUPO  LINEA  LINEA                 ACT  SUG  APLICAR   APLICADO  APLICABLE       APLICAR", 96, " ", false) + "\n";
                cadena += "---------------------------------------------------------------------------------------------\n\n";

                foreach (DataRow reg in dt_promo.Rows)
                {
                    cadena += funciones.Rellenar(reg["codgrupo"].ToString(), 4, " ", false);
                    cadena += "   " + funciones.Rellenar(reg["codlinea"].ToString(), 4, " ", false);
                    cadena += "   " + funciones.Rellenar(reg["desclinea"].ToString(), 20, " ", false);
                    cadena += "  " + funciones.Rellenar(reg["nivel"].ToString(), 3, " ", false);
                    cadena += "  " + funciones.Rellenar(reg["nivel_sugerido"].ToString(), 3, " ", false);
                    cadena += "  " + funciones.Rellenar(reg["nivel_por_aplicar"].ToString(), 8, " ", false);
                    cadena += "  " + funciones.Rellenar(reg["promo_aplicado"].ToString(), 15, " ", false);
                    cadena += "  " + funciones.Rellenar(reg["promo_aplicable"].ToString(), 15, " ", false);
                    cadena += "  " + funciones.Rellenar(reg["por_aplicar"].ToString(), 15, " ", false) + "\n";
                }
                cadena += "----------------------------------------------------------------------------------------------\n";
            }

            return cadena;
        }
        public async Task<int> Nro_Reversiones_Pendientes_de_Pago_Cliente(DBContext _context, string codcliente)
        {
            int resultado = 0;
            try
            {
                //using (_context)
                ////using (var _context = DbContextFactory.Create(userConnectionString))
                //{
                var res = _context.veremision
                     .Join(_context.coplancuotas,
                         p1 => p1.codigo,
                         p2 => p2.coddocumento,
                         (p1, p2) => new { p1, p2 })
                     .Where(x => !x.p1.anulada && x.p2.monto > x.p2.montopagado && x.p1.obs.Contains("rever") && x.p1.codcliente == codcliente)
                     .Select(x => new { x.p1.codcliente, x.p1.numeroid })
                     .Distinct()
                     .Count();
                if (res != null)
                {
                    resultado = (int)res;
                }
                //}
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"Error: {ex.Message}");
                return 0;
            }
            return resultado;
        }
        public async Task<int> Nro_Reversiones_Pendientes_Pago_Permitido(DBContext _context, string codempresa)
        {
            int resultado = 0;
            try
            {
                //using (_context)
                ////using (var _context = DbContextFactory.Create(userConnectionString))
                //{
                var res = await _context.adparametros
                .Where(v => v.codempresa == codempresa)
                .Select(v => v.nro_reversiones_pendientes)
                .FirstOrDefaultAsync();

                if (res != null)
                {
                    resultado = (int)res;
                }
                //}
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"Error: {ex.Message}");
                return 0;
            }
            return resultado;
        }
        public async Task<string> Notas_de_Reversion_Pendientes_de_Pago(DBContext _context, string codcliente)
        {
            //StringBuilder cadena = new StringBuilder();
            string cadena = "";
            try
            {
                //using (_context)
                ////using (var _context = DbContextFactory.Create(userConnectionString))
                //{
                var notasPendientes = (from p1 in _context.veremision
                                       join p2 in _context.coplancuotas on p1.codigo equals p2.coddocumento
                                       where !p1.anulada && p2.monto > p2.montopagado && p1.obs.Contains("rev") && p1.codcliente == codcliente
                                       group new { p1.codcliente, p1.id, p1.numeroid, p1.fecha, p2.monto, p2.montopagado } by new { p1.codcliente, p1.id, p1.numeroid, p1.fecha } into g
                                       select new
                                       {
                                           CodCliente = g.Key.codcliente,
                                           Id = g.Key.id,
                                           NumeroId = g.Key.numeroid,
                                           Fecha = g.Key.fecha,
                                           Saldo = g.Sum(x => x.monto - x.montopagado)
                                       }).ToList();

                if (notasPendientes.Any())
                {
                    cadena = "\r\n" + funciones.Rellenar("NOTAS DE REMISION-REVERSION PENDIENTES DE PAGO", 50, " ", false);
                    cadena = "\r\n" + "-----------------------------------------------------";
                    cadena = "\r\n" + funciones.Rellenar("COD      DOC                           ", 50, " ", false);
                    cadena = "\r\n" + funciones.Rellenar("CLIENTE  REMISION         FECHA       SALDO   ", 50, " ", false);
                    cadena = "\r\n" + "-----------------------------------------------------\n";

                    foreach (var nota in notasPendientes)
                    {
                        cadena = "\r\n" + funciones.Rellenar(nota.CodCliente, 7, " ", false);
                        cadena = "\r\n" + "  " + funciones.Rellenar(nota.Id + "-" + nota.NumeroId, 15, " ", false);
                        cadena = "\r\n" + "  " + funciones.Rellenar(nota.Fecha.ToString(), 10, " ", false);
                        cadena = "\r\n" + "  " + funciones.Rellenar(nota.Saldo.ToString(), 8, " ", false);
                    }
                    cadena = "\r\n" + "---------------------------------------------------";
                }
                //}
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"Error: {ex.Message}");
                return "";
            }
            return cadena;
        }
        public async Task<bool> CumpleEmpaqueCerrado(DBContext _context, string coditem, int codtarifa, int coddescuento, decimal cantidad, string codcliente)
        {
            try
            {
                bool resultado = false;
                if (cantidad > 0)
                {
                    bool es_cliente_final = await cliente.EsClienteFinal(_context, codcliente);
                    bool cliente_final_controla_empaque_cerrado = await cliente.Controla_empaque_cerrado(_context, codcliente);

                    decimal empaque_precio, empaque_precio_alternativo, empaque_descuento, empaque_mayor, mod_final;

                    // var cant_empaque_precio = _context.veempaque1
                    //.Join(_context.intarifa,
                    //    e => e.codempaque,
                    //    t => t.codempaque,
                    //    (e, t) => new { E = e, T = t })
                    //.Where(x => x.E.item == coditem && x.T.codigo == codtarifa)
                    //.Select(x => x.E.cantidad)
                    //.FirstOrDefault();

                    // if (cant_empaque_precio == null)
                    // {
                    //     empaque_precio = 0;
                    // }
                    // else { empaque_precio = (decimal)cant_empaque_precio; }

                    // var cant_empaque_descuento = _context.veempaque1
                    //.Join(_context.vedescuento,
                    //    e => e.codempaque,
                    //    t => t.codempaque,
                    //    (e, t) => new { E = e, T = t })
                    //.Where(x => x.E.item == coditem && x.T.codigo == coddescuento)
                    //.Select(x => x.E.cantidad)
                    //.FirstOrDefault();

                    //if (cant_empaque_descuento == null)
                    //{
                    //    empaque_descuento = 0;
                    //}
                    //else { empaque_descuento = (decimal)cant_empaque_descuento; }
                    empaque_precio = await EmpaquePrecio(_context, coditem, codtarifa);
                    empaque_descuento = await EmpaquePrecio(_context, coditem, coddescuento);


                    //si es cliente final y no controla empaque segun el archivo de clientes finales entonces no tiene empaque de precio
                    //asi se deifinio con JRA en fecha 29-06-2017
                    if (es_cliente_final == true && cliente_final_controla_empaque_cerrado == false)
                    {
                        empaque_precio = 0;
                    }

                    if (empaque_descuento > empaque_precio)
                    {
                        empaque_mayor = empaque_descuento;
                    }
                    else
                    {
                        empaque_mayor = empaque_precio;
                    }

                    resultado = CantidadCumpleEmpaque(_context, cantidad, empaque_descuento, empaque_precio, await Tarifa_PermiteEmpaquesMixtos(_context, codtarifa));

                    //Si el resultado es falso entonces tratar con el empaque alternativo y mezclando
                    //este empaque alternativo se refiere a empaques de caja cerrada, en la practica
                    if (!resultado)
                    {
                        // var cant_empaque_precio_alternativo = _context.veempaque1
                        //   .Join(_context.intarifa,
                        //       e => e.codempaque,
                        //       t => t.codempaque_alternativo,
                        //       (e, t) => new { E = e, T = t })
                        //   .Where(x => x.E.item == coditem && x.T.codigo == codtarifa)
                        //   .Select(x => x.E.cantidad)
                        //   .FirstOrDefault();

                        //if (cant_empaque_precio_alternativo == null)
                        //{
                        //    empaque_precio_alternativo = 0;
                        //}
                        //else { empaque_precio_alternativo = (decimal)cant_empaque_precio_alternativo; }
                        empaque_precio_alternativo = await EmpaqueAlternativo(_context, coditem, codtarifa);

                        mod_final = 0;
                        if (empaque_mayor > empaque_precio_alternativo)
                        {
                            if (empaque_mayor > 0)
                            {
                                mod_final = cantidad % empaque_mayor;
                                if (mod_final > 0)
                                {
                                    if (empaque_precio_alternativo > 0)
                                    {
                                        mod_final = mod_final % empaque_precio_alternativo;
                                    }
                                }
                            }
                            else if (empaque_precio_alternativo > 0)
                            {
                                mod_final = cantidad % empaque_precio_alternativo;
                            }
                        }
                        else if (empaque_precio_alternativo > 0)
                        {
                            mod_final = cantidad % empaque_precio_alternativo;
                            if (mod_final > 0)
                            {
                                if (empaque_mayor > 0)
                                {
                                    mod_final = mod_final % empaque_mayor;
                                }

                            }

                        }
                        else if (empaque_mayor > 0)
                        {
                            mod_final = cantidad % empaque_mayor;
                        }

                        if (mod_final == 0)
                        {
                            resultado = true;
                        }
                        else
                        {
                            resultado = false;
                        }
                    }
                    //Si no resulto ver si el empaque de precio o descuento tienen empaques alternativos
                    if (!resultado)
                    {
                        //los empaques alternativos deben ser cabales
                        DataTable tabla = new DataTable();
                        DataRow[] registro;
                        //DEL DESCUENTO
                        var query = from ve in _context.veempaque1
                                    where ve.item == coditem &&
                                          _context.veempaque_alternativo
                                            .Where(alternativo => _context.vedescuento
                                                .Any(descuento => descuento.codigo == coddescuento && descuento.codempaque == alternativo.codempaque))
                                            .Select(alternativo => alternativo.codempaque_alternativo)
                                            .Contains(ve.codempaque)
                                    select ve.cantidad;

                        var result = query.Distinct().ToList();
                        tabla = funciones.ToDataTable(result);

                        foreach (DataRow row in tabla.Rows)
                        {
                            if (cantidad % decimal.Parse(row[1].ToString()) == 0)
                            {
                                resultado = true;
                                break;
                            }
                        }
                        //DEL PRECIO
                        var query1 = from ve in _context.veempaque1
                                     where ve.item == coditem &&
                                           _context.veempaque_alternativo
                                             .Where(alternativo => _context.intarifa
                                                 .Any(descuento => descuento.codigo == codtarifa && descuento.codempaque == alternativo.codempaque))
                                             .Select(alternativo => alternativo.codempaque_alternativo)
                                             .Contains(ve.codempaque)
                                     select ve.cantidad;

                        var result1 = query1.Distinct().ToList();
                        tabla = funciones.ToDataTable(result1);

                        foreach (DataRow row in tabla.Rows)
                        {
                            if (cantidad % decimal.Parse(row[1].ToString()) == 0)
                            {
                                resultado = true;
                                break;
                            }
                        }
                    }
                }
                else
                {
                    resultado = true;

                }

                return resultado;
            }
            catch (Exception)
            {
                return false;
            }

        }
        public async Task<int> EmpaquePrecio(DBContext _context, string coditem, int codtarifa)
        {
            try
            {
                int resultado = 0;
                var cant_empaque_precio = await _context.veempaque1
                .Join(_context.intarifa,
                e => e.codempaque,
                t => t.codempaque,
                (e, t) => new { E = e, T = t })
                .Where(x => x.E.item == coditem && x.T.codigo == codtarifa)
                .Select(x => x.E.cantidad)
                .FirstOrDefaultAsync();

                if (cant_empaque_precio == null)
                {
                    resultado = 0;
                }
                else { resultado = (int)cant_empaque_precio; }
                return resultado;
            }
            catch (Exception)
            {
                return 0;
            }

        }
        public async Task<int> EmpaqueDescuento(DBContext _context, string coditem, int coddescuento)
        {
            try
            {
                int resultado = 0;
                var cant_empaque_descuento = await _context.veempaque1
                .Join(_context.vedescuento,
                e => e.codempaque,
                t => t.codempaque,
                (e, t) => new { E = e, T = t })
                .Where(x => x.E.item == coditem && x.T.codigo == coddescuento)
                .Select(x => x.E.cantidad)
                .FirstOrDefaultAsync();
                if (cant_empaque_descuento == null)
                {
                    resultado = 0;
                }
                else { resultado = (int)cant_empaque_descuento; }
                return resultado;
            }
            catch (Exception)
            {
                return 0;
            }

        }
        public async Task<int> EmpaqueAlternativo(DBContext _context, string coditem, int codtarifa)
        {
            try
            {
                int resultado = 0;
                var cant_empaque_precio_alternativo = await _context.veempaque1
                .Join(_context.intarifa,
                e => e.codempaque,
                t => t.codempaque_alternativo,
                (e, t) => new { E = e, T = t })
                .Where(x => x.E.item == coditem && x.T.codigo == codtarifa)
                .Select(x => x.E.cantidad)
                 .FirstOrDefaultAsync();

                if (cant_empaque_precio_alternativo == null)
                {
                    resultado = 0;
                }
                else { resultado = (int)cant_empaque_precio_alternativo; }

                return resultado;
            }
            catch (Exception)
            {
                return 0;
            }

        }
        public async Task<List<double>> Empaques_Alternativos_Lista(DBContext _context, int codtarifa, string coditem)
        {
            try
            {
                var query = await _context.intarifa
                    .Join(_context.veempaque_alternativo,
                        p1 => p1.codempaque,
                        p3 => p3.codempaque,
                        (p1, p3) => new { p1, p3 })
                    .Join(_context.veempaque1,
                        p => p.p3.codempaque_alternativo,
                        p4 => p4.codempaque,
                        (p, p4) => new { p, p4 })
                    .Where(x => x.p.p1.codigo == codtarifa && x.p4.item == coditem)
                    .Select(x => x.p4.cantidad)
                    .Distinct()
                    .ToListAsync();

                return query.Select(x => Convert.ToDouble(x)).ToList();
            }
            catch (Exception ex)
            {
                // Manejar la excepción según sea necesario
                return new List<double>();
            }

        }
        public bool CantidadCumpleEmpaque(DbContext _context, decimal cantidad, decimal empaque1, decimal empaque2, bool permite_mixtos)
        {
            bool resultado = false;
            decimal empaque_mayor = 0;
            decimal mod_final = 0;
            if ((empaque1 > empaque2))
            {
                empaque_mayor = empaque1;
                if ((empaque1 > 0))
                {
                    mod_final = (cantidad % empaque1);
                    if ((mod_final > 0))
                    {
                        if (permite_mixtos)
                        {
                            if ((empaque2 > 0))
                            {
                                mod_final = (mod_final % empaque2);
                            }
                        }
                    }
                }
                else
                {
                    if ((empaque2 > 0))
                    {
                        mod_final = (cantidad % empaque2);
                    }
                }
            }
            else
            {
                empaque_mayor = empaque2;
                if ((empaque2 > 0))
                {
                    mod_final = (cantidad % empaque2);
                    if ((mod_final > 0))
                    {
                        if (permite_mixtos)
                        {
                            if ((empaque1 > 0))
                            {
                                mod_final = (mod_final % empaque1);
                            }
                        }
                    }
                }
                else
                {
                    if ((empaque1 > 0))
                    {
                        mod_final = (cantidad % empaque1);
                    }
                }
            }
            if ((mod_final == 0))
            {
                resultado = true;
            }
            else
            {
                resultado = false;
            }
            return resultado;
        }

        public async Task<bool> TarifaValidaDescuento(DBContext _context, int codtarifa, int coddesextra)
        {
            bool resultado = false;
            try
            {

                int count = await _context.vedesextra_tarifa
                .CountAsync(t => t.coddesextra == coddesextra && t.codtarifa == codtarifa);

                if (count > 0)
                {
                    resultado = true;
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return false;
            }
            return resultado;
        }

        public async Task<bool> DescuentoEspecialValidoDescuento(DBContext _context, int coddescuento, int coddesextra)
        {
            bool resultado = false;
            try
            {

                int count = await _context.vedesextra_descuento
                .CountAsync(t => t.coddesextra == coddesextra && t.coddescuento == coddescuento);

                if (count > 0)
                {
                    resultado = true;
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return false;
            }
            return resultado;
        }

        public async Task<bool> DescuentoExtra_ItemValido(DBContext _context, string coditem, int coddesextra)
        {
            bool resultado = false;
            try
            {

                int count = await _context.vedesextra_item
                .CountAsync(t => t.coddesextra == coddesextra && t.coditem == coditem);

                if (count > 0)
                {
                    resultado = true;
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return false;
            }
            return resultado;
        }

        public async Task<(bool EsValido, string Mensaje)> Validar_NIT_Correcto(DBContext _context, string nit, string tipo_doc_id)
        {
            try
            {
                int max_largo = await Longitud_Max_NIT_Facturacion(_context);
                int min_largo = await Longitud_Min_NIT_Facturacion(_context);
                int largo = nit.Trim().Length;
                bool esValido = true;
                string cadena = "";

                // 1 CI - CEDULA DE IDENTIDAD
                // 2 CEX - CEDULA DE IDENTIDAD DE EXTRANJERO
                // 3 PAS - PASAPORTE
                // 4 OD - OTRO DOCUMENTO DE IDENTIDAD
                // 5 NIT - NUMERO DE IDENTIFICACION TRIBUTARIA

                if (largo <= 0)
                {
                    cadena = "Debe ingresar un NIT/CI u otro documento de identidad válido.";
                    esValido = false;
                    return (esValido, cadena);
                }

                if (largo == 1 && nit != "0")
                {
                    cadena = "Si el NIT/CI u otro documento de identidad es de un solo dígito, debe ser: 0.";
                    esValido = false;
                    return (esValido, cadena);
                }

                if (largo > 1)
                {
                    // Si es CI o NIT, validar así
                    if (tipo_doc_id == "1" || tipo_doc_id == "5")
                    {
                        if (!IsNumeric(nit))
                        {
                            cadena = "Debe ingresar un NIT/CI numérico.";
                            esValido = false;
                            return (esValido, cadena);
                        }

                        if (largo > max_largo)
                        {
                            cadena = "El NIT/CI debe ser un valor numérico de " + max_largo + " dígitos como máximo, verifique por favor.";
                            esValido = false;
                            return (esValido, cadena);
                        }

                        if (largo < max_largo && largo < min_largo)
                        {
                            cadena = "El NIT/CI debe ser un valor numérico de " + min_largo + " dígitos como mínimo y {max_largo} dígitos como máximo, verifique el NIT.";
                            esValido = false;
                            return (esValido, cadena);
                        }
                    }
                    else
                    {
                        // Aquí sería para pasaporte, carnet extranjero y otros
                        // 2 CEX - CEDULA DE IDENTIDAD DE EXTRANJERO
                        // 3 PAS - PASAPORTE
                        // 4 OD - OTRO DOCUMENTO DE IDENTIDAD
                        cadena = "";
                        esValido = true;
                        return (esValido, cadena);
                    }
                }

                // Todo está correcto
                return (esValido, cadena);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return (false, "Error en la validación");
            }
        }

        public async Task<int> Longitud_Max_NIT_Facturacion(DBContext _context)
        {
            int resultado = 13;
            try
            {
                var parametro = await _context.adparametros.Select(i => i.longitud_maxima_nit_facturacion).FirstOrDefaultAsync();
                if (parametro != null)
                {
                    resultado = (int)parametro.Value;
                }
            }
            catch (Exception)
            {
                // Manejar excepción aquí si es necesario
                throw;
            }
            return resultado;

        }

        public async Task<int> Longitud_Min_NIT_Facturacion(DBContext _context)
        {
            int resultado = 13;
            try
            {
                var parametro = await _context.adparametros.Select(i => i.longitud_minima_nit_facturacion).FirstOrDefaultAsync();
                if (parametro != null)
                {
                    resultado = (int)parametro.Value;
                }
            }
            catch (Exception)
            {
                // Manejar excepción aquí si es necesario
                throw;
            }
            return resultado;
        }

        public bool IsNumeric(string value)
        {
            double result;
            return double.TryParse(value, out result);
        }

        public async Task<double> PesoMinimoDescuentoExtra(DBContext _context, int coddesextra)
        {
            double minimo = 0;
            try
            {
                var result = await _context.vedesextra
                    .Where(v => v.codigo == coddesextra)
                    .Select(v => v.peso_minimo)
                    .FirstOrDefaultAsync();

                if (result.HasValue)
                {
                    minimo = (double)result.Value;
                }
            }
            catch (Exception ex)
            {
                minimo = 0;

            }
            return minimo;
        }

        public async Task<decimal> MontoMinimoContadoDescuentoExtra(DBContext _context, int coddesextra, string codmoneda, DateTime fecha)
        {
            decimal minimo = 0;
            string codmoneda_minimo = "";
            try
            {
                var result = await _context.vedesextra
                .Where(v => v.codigo == coddesextra)
                .Select(p => p.min_contado)
                .FirstOrDefaultAsync();
                if (result != null)
                {
                    minimo = result ?? 0;
                }

                var result1 = await _context.vedesextra
                .Where(v => v.codigo == coddesextra)
                .Select(p => p.codmoneda_min_contado)
                .FirstOrDefaultAsync();
                if (result1 != null)
                {
                    codmoneda_minimo = result1;
                }
            }
            catch (Exception)
            {
                minimo = 0;
                codmoneda_minimo = codmoneda;
            }
            if (minimo > 0)
            {
                minimo = await tipocambio._conversion(_context, codmoneda, codmoneda_minimo, fecha, minimo);
            }
            return minimo;
        }

        public async Task<decimal> MontoMinimoCreditoDescuentoExtra(DBContext _context, int coddesextra, string codmoneda, DateTime fecha)
        {
            decimal minimo = 0;
            string codmoneda_minimo = "";
            try
            {
                var result = await _context.vedesextra
                .Where(v => v.codigo == coddesextra)
                .Select(p => p.min_credito)
                .FirstOrDefaultAsync();
                if (result != null)
                {
                    minimo = result ?? 0;
                }

                var result1 = await _context.vedesextra
                .Where(v => v.codigo == coddesextra)
                .Select(p => p.codmoneda_min_credito)
                .FirstOrDefaultAsync();
                if (result1 != null)
                {
                    codmoneda_minimo = result1;
                }
            }
            catch (Exception)
            {
                minimo = 0;
                codmoneda_minimo = codmoneda;
            }
            if (minimo > 0)
            {
                minimo = await tipocambio._conversion(_context, codmoneda, codmoneda_minimo, fecha, minimo);
            }
            return minimo;
        }

        public async Task<double> Empaque(DBContext _context, int codempaque, string coditem)
        {
            double resultado = 0;
            try
            {
                //using (_context)
                ////using (var _context = DbContextFactory.Create(userConnectionString))
                //{
                var res = await _context.veempaque1
                .Where(v => v.codempaque == codempaque && v.item == coditem)
                .Select(v => v.cantidad)
                .FirstOrDefaultAsync();

                if (res != null)
                {
                    resultado = (double)res;
                }
                //}
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"Error: {ex.Message}");
                return 0;
            }
            return resultado;
        }


        public async Task<bool> Descuento_Especial_Habilitado(DBContext _context, int coddesc_especial)
        {
            try
            {
                var res = await _context.vedescuento
                .Where(v => v.codigo == coddesc_especial)
                .Select(v => new
                {
                    v.codigo,
                    habilitado = v.habilitado ?? false,
                    v.desde_fecha,
                    v.hasta_fecha
                })
                .FirstOrDefaultAsync();

                if (res != null)
                {
                    if (res.habilitado)
                    {
                        return true;
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<bool> Cumple_Empaque_De_DesctoEspecial(DBContext _context, string coditem, int codtarifa, int coddescuento, decimal cantidad, string codcliente)
        {
            bool resultado = false;
            try
            {

                if (cantidad > 0)
                {
                    bool es_cliente_final = await cliente.EsClienteFinal(_context, codcliente);
                    bool cliente_final_controla_empaque_cerrado = await cliente.Controla_empaque_cerrado(_context, codcliente);
                    bool cliente_final_permite_desc_caja_cerrado = await cliente.Permite_Descuento_caja_cerrada(_context, codcliente);
                    decimal empaque_precio_alternativo, empaque_descuento, empaque_mayor, mod_final;

                    //si es cliente final y no controla empaque segun el archivo de clientes finales entonces no tiene empaque de precio
                    //asi se deifinio con JRA en fecha 08-12-2022
                    if (es_cliente_final == true && cliente_final_controla_empaque_cerrado == false && cliente_final_permite_desc_caja_cerrado == true)
                    {
                        empaque_descuento = 0;
                        resultado = true;
                        return resultado;
                    }
                    //Desde 11-09-2023
                    //si es cliente NO es final pero si esta habilitado para el descuento de caja cerrada permitirlo
                    //asi se deifinio con JRA en fecha 11-09-2023
                    if (cliente_final_permite_desc_caja_cerrado == true)
                    {
                        empaque_descuento = 0;
                        resultado = true;
                        return resultado;
                    }
                    //Aqui preguntar a gerencia si un cliente  q no sea final y este habilitado para descuento caja cerrada puede ser o no
                    var cant_emp = _context.veempaque1
                        .Join(_context.vedescuento,
                          e => e.codempaque,
                          d => d.codempaque,
                          (e, d) => new { e, d })
                    .Where(x => x.e.item == coditem && x.d.codigo == coddescuento)
                    .Select(x => x.e.cantidad)
                    .FirstOrDefault();

                    if (cant_emp == null)
                    {
                        empaque_descuento = 0;
                    }
                    else { empaque_descuento = (decimal)cant_emp; }
                    empaque_mayor = empaque_descuento;
                    //verificar empaque cerrado del empaque mayor
                    resultado = CantidadCumpleEmpaque(_context, cantidad, empaque_descuento, empaque_descuento, await Tarifa_PermiteEmpaquesMixtos(_context, codtarifa));
                    // Dsd 06-12-2022
                    //  si el resultado es false que valide con el empaque dimediado como alternativo pero solo si el empaque alternativo NO ES el de DESCUENTO CAJA CERRADA(38)
                    // Es to por instruccion de CVA, Sra Marlen ya que al asignar el descuento de linea 302 a precio 2, hay item como ser el 03EAGE08 tiene los siguets empaques:
                    // 31=500 32=300 36=500 37=4000 38=4000
                    // el vendedor quiere venderle 500 pz para cumplir un empaque dimediado y en estos casos cuando son Dimediados se debe asignar el descuento de linea 302 ya que cumple empque dimediado
                    // para solucionar esto se verifica que si no cumple el empaque minimo de precio, valide con el empaque alternativo que del 32 es el 36, 42 es el 46 y 82 es el 86
                    // Si no resulto ver si el empaque de descuento tienen empaques alternativos en veempaque_alternativo_descuento creado 07-12-2022
                    if (!resultado)
                    {//los empaques alternativos deben ser cabales
                        DataTable tabla = new DataTable();
                        DataRow[] registro;
                        //DEL DESCUENTO
                        var codigosEmpaqueAlternativos = _context.veempaque_alternativo_descuento
                        .Where(va => _context.vedescuento
                        .Any(d => d.codempaque == va.codempaque_alternativo && d.codigo == coddescuento))
                        .Select(va => va.codempaque_alternativo)
                        .ToList();

                        var dt = new DataTable();
                        dt.Columns.Add("cantidad", typeof(int));

                        _context.veempaque1
                            .Where(e => e.item == coditem && codigosEmpaqueAlternativos.Contains(e.codempaque))
                            .Select(e => e.cantidad)
                            .ToList()
                            .ForEach(c => dt.Rows.Add(c));

                        foreach (DataRow row in dt.Rows)
                        {
                            if (cantidad % Convert.ToDecimal(row["cantidad"]) == 0)
                            {
                                resultado = true;
                                break;
                            }
                        }
                    }
                }
                else
                {
                    resultado = true;
                }

                return resultado;
            }
            catch (Exception)
            {
                return false;
            }

        }

        public async Task<int> Codigo_Empaque_Descuento_Especial(DBContext _context, int codigo)
        {
            int resultado = 0;
            try
            {
                var result = await _context.vedescuento
                .Where(v => v.codigo == codigo)
                .Select(p => p.codempaque)
                .FirstOrDefaultAsync();
                resultado = result;
            }
            catch (Exception)
            {
                resultado = 0;
            }
            return resultado;
        }

        public async Task<int> Codigo_Empaque_Precio(DBContext _context, int codigo)
        {
            int resultado = 0;
            try
            {
                var result = await _context.intarifa
                .Where(v => v.codigo == codigo)
                .Select(p => p.codempaque)
                .FirstOrDefaultAsync();
                resultado = result;
            }
            catch (Exception)
            {
                resultado = 0;
            }
            return resultado;
        }

        public async Task<int> Codigo_Descuento_Especial_Precio(DBContext _context, int codigo)
        {
            int resultado = 0;
            try
            {
                var result = await _context.vedescuento_tarifa
                    .Where(tarifa => tarifa.codtarifa == codigo && tarifa.coddescuento != 0 &&
                    _context.vedescuento.Any(descuento => descuento.codigo == tarifa.coddescuento && descuento.habilitado == true))
                    .Select(tarifa => tarifa.coddescuento)
                    .FirstOrDefaultAsync();

                resultado = result;
            }
            catch (Exception)
            {
                resultado = 0;
            }
            return resultado;
        }


        public async Task<string> Sugerir_Empaque_De_DesctoEspecial(DBContext _contex, string coditem, int codtarifa, int coddescuento, double cantidad, string codcliente, string codempresa)
        {
            double minimo = 0;

            string resultado = "0";
            int cantidad_final_mas = 0;
            int cantidad_final_menos = 0;
            if (cantidad > 0)
            {
                double porcentaje_empaque = 0;
                double empaque_descuento = 0;
                double cantidad_porcentaje_empaque = 0;
                double cuantos_empaques_mas = 0;
                double cuantos_empaques_menos = 0;
                int empaque_descuento_aux_mas = 0;
                int empaque_descuento_aux_menos = 0;
                int cantidad_aux_mas = 0;
                int cantidad_aux_menos = 0;

                empaque_descuento = await EmpaqueDescuento(_contex, coditem, coddescuento);
                //'verificar empaque cerrado del empaque mayor
                //'aqui calcular segun al 20 configurado en la tabla adparametros columna porcentaje_sugerencia_empaque 20%
                //'si la cantidad del detalle esta inferior al empaque cerrado en un 20% sugerir aumentar la cantidad
                //'si la cantidad del detalle esta superior al empaque cerrado en un 20% sugerir reducir la cantidad

                porcentaje_empaque = await configuracion.porcentaje_sugerencia_empaque(_contex, codempresa);
                cantidad_porcentaje_empaque = (empaque_descuento * porcentaje_empaque) / 100;

                empaque_descuento_aux_mas = Convert.ToInt32(cantidad + empaque_descuento);
                empaque_descuento_aux_menos = Convert.ToInt32(cantidad);
                if (empaque_descuento == 0)
                {
                    cantidad_final_mas = 0;
                    cantidad_final_menos = 0;
                }
                else
                {
                    cuantos_empaques_mas = empaque_descuento_aux_mas / empaque_descuento;
                    cuantos_empaques_mas = Math.Floor(cuantos_empaques_mas);
                    cantidad_aux_mas = Convert.ToInt32(cuantos_empaques_mas * empaque_descuento);
                    cantidad_final_mas = cantidad_aux_mas - Convert.ToInt32(cantidad);

                    cuantos_empaques_menos = empaque_descuento_aux_menos / empaque_descuento;
                    cuantos_empaques_menos = Math.Floor(cuantos_empaques_menos);
                    cantidad_aux_menos = Convert.ToInt32(cuantos_empaques_menos * empaque_descuento);
                    cantidad_final_menos = cantidad_aux_menos - Convert.ToInt32(cantidad);
                    cantidad_final_menos *= -1;

                    if ((cantidad_final_mas <= cantidad_porcentaje_empaque) || cantidad < empaque_descuento)
                    {// si la cantidad final es menor o igual a la cantidad del porcentaje de empaque debe aumentar la cantidad solicitada
                        if (cantidad_final_mas <= cantidad_porcentaje_empaque || cantidad_final_mas < empaque_descuento)
                        {
                            cantidad_final_mas = cantidad_final_mas;
                        }
                        else
                        {//si la cantidad final es mayor o igual a la cantidad del porcentaje de empaque debe reducir la cantidad solicitada
                            cantidad_final_mas = cantidad_final_mas;
                        }
                    }
                    else
                    {
                        cantidad_final_mas = cantidad_final_mas;
                    }
                    //menos
                    if ((cantidad_final_menos <= cantidad_porcentaje_empaque) && cantidad_final_mas == 0)
                    {//si la cantidad final es menor o igual a la cantidad del porcentaje de empaque debe aumentar la cantidad solicitada
                        if (cantidad_final_menos <= cantidad_porcentaje_empaque || cantidad_final_menos < empaque_descuento)
                        {
                            cantidad_final_menos *= -1;
                        }
                        else
                        {//si la cantidad final es mayor o igual a la cantidad del porcentaje de empaque debe reducir la cantidad solicitada
                            cantidad_final_menos = cantidad_final_menos;
                        }
                    }
                    else
                    {
                        cantidad_final_menos = cantidad_final_menos;
                    }
                }
            }
            else
            {
                cantidad_final_mas = 0;
                cantidad_final_menos = 0;
            }

            resultado = cantidad_final_mas.ToString() + "/" + cantidad_final_menos.ToString();

            return resultado;
        }

        public async Task<bool> aplicarstocksproforma(DBContext _context, int codigo, string codempresa)
        {
            bool resultado = true;
            if (await configuracion.emp_proforma_reserva(_context,codempresa))
            {
                int codalmacen = 0;
                var codAlmProf = await _context.veproforma.Where(i => i.codigo == codigo).FirstOrDefaultAsync();
                if (codAlmProf != null)
                {
                    codalmacen = codAlmProf.codalmacen;
                }
                else
                {
                    resultado = false;
                }
                List<veproforma1> tabla = new List<veproforma1>();
                if (resultado)
                {
                    tabla = await _context.veproforma1.Where(i=>i.codproforma==codigo).ToListAsync();
                    if (tabla.Count() == 0)
                    {
                        resultado = false;
                    }
                }
                if (resultado)
                {
                    //////////////
                    var proNull = await _context.instoactual.Where(i => i.proformas == null).ToListAsync();
                    if (proNull.Count > 0)
                    {
                        foreach (var reg in proNull)
                        {
                            reg.proformas = 0;
                            _context.Entry(proNull).State = EntityState.Modified;
                            await _context.SaveChangesAsync();
                        }
                    }
                    foreach (var reg in tabla)
                    {
                        if (await items.itemesconjunto(_context,reg.coditem))
                        {
                            var tablakit = await _context.inkit.Where(i=>i.codigo== reg.coditem)
                                .Select(i => new
                                {
                                    item = i.item,
                                    cantidad = i.cantidad
                                })
                                .ToListAsync();
                            foreach (var j in tablakit)
                            {
                                var instoactualUpdate = await _context.instoactual.Where(i => i.codalmacen == codalmacen && i.coditem == j.item).FirstOrDefaultAsync();
                                instoactualUpdate.proformas = instoactualUpdate.proformas + (reg.cantaut * j.cantidad);
                                _context.Entry(instoactualUpdate).State = EntityState.Modified;
                                await _context.SaveChangesAsync();
                            }
                        }
                        else
                        {
                            var instoactualUpdate = await _context.instoactual.Where(i => i.codalmacen == codalmacen && i.coditem == reg.coditem).FirstOrDefaultAsync();
                            instoactualUpdate.proformas = instoactualUpdate.proformas + reg.cantaut;
                            _context.Entry(instoactualUpdate).State = EntityState.Modified;
                            await _context.SaveChangesAsync();
                        }
                    }
                }
            }

            return true;
        }


        public async Task<bool> Existe_Proforma(DBContext _context, string id, int numeroid)
        {
            var resultado = await _context.veproforma.Where(i => i.id == id && i.numeroid == numeroid).CountAsync();
            if (resultado > 0)
            {
                return true;
            }
            return false;
        }

        public async Task<bool> proforma_aprobada(DBContext _context, int codproforma)
        {
            var resultado = await _context.veproforma.Where(i => i.codigo == codproforma).Select(i => i.aprobada).FirstOrDefaultAsync();
            return resultado;
        }

        public async Task<bool> proforma_para_aprobar(DBContext _context, int codproforma)
        {
            var resultado = await _context.veproforma.Where(i => i.codigo == codproforma).Select(i => i.paraaprobar).FirstOrDefaultAsync() ?? false;
            return resultado;
        }

        public async Task<string> Cliente_De_Proforma(DBContext _context, int codproforma)
        {
            string resultado = "";
            try
            {
                var resul = await _context.veproforma.Where(i => i.codigo == codproforma).Select(i => i.codcliente).FirstOrDefaultAsync();
                resultado = resul ?? "";

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return "";
            }
            return resultado;
        }

        public async Task<DateTime> Fecha_Autoriza_de_Proforma(DBContext _context, string id, int numeroid)
        {
            DateTime resultado = DateTime.Now;
            try
            {
                var resul = await _context.veproforma.Where(i => i.id == id && i.numeroid == numeroid).Select(i => i.fechaaut).FirstOrDefaultAsync();
                if (resul != null)
                {
                    resultado = (DateTime)resul;
                }
            }
            catch
            {
                resultado = DateTime.MinValue;
            }
            return resultado;
        }

        public async Task<List<int>> Proforma_Lista_Tipo_Precio(DBContext _context, int codproforma)
        {
            List<int> resultado = new List<int>();

            var codtarifas = await _context.veproforma1
                .Where(v => v.codproforma == codproforma)
                .Select(v => v.codtarifa)
                .Distinct().ToListAsync();

            foreach (var codtarifa in codtarifas)
            {
                if (!resultado.Contains(codtarifa))
                {
                    resultado.Add(codtarifa);
                }
            }
            return resultado;
        }

        public async Task<string> Tarifa_tipo(DBContext _context, int codtarifa)
        {
            string resultado = "";
            try
            {
                var resul = await _context.intarifa.Where(i => i.codigo == codtarifa).Select(i => i.tipo).FirstOrDefaultAsync();
                if (resul != null)
                {
                    resultado = resul;
                }
            }
            catch
            {
                resultado = "";
            }
            return resultado;
        }

        public async Task<bool> proforma_transferida(DBContext _context, int codproforma)
        {
            var resultado = await _context.veproforma.Where(i => i.codigo == codproforma).Select(i => i.transferida).FirstOrDefaultAsync();
            return resultado;
        }

        public async Task<string> Cliente_Referencia_Proforma_Etiqueta(DBContext _context, string id_proforma, int nroid_proforma)
        {
            var resultado = await _context.veproforma_etiqueta.Where(i => i.id_proforma == id_proforma && i.nroid_proforma == nroid_proforma)
                .Select(i => new
                {
                    i.codcliente_casual,
                    i.codcliente_real
                }).FirstOrDefaultAsync();
            if (resultado != null)
            {
                if (resultado.codcliente_real != null)
                {
                    return resultado.codcliente_real;
                }
                return resultado.codcliente_casual;
            }
            return "";
        }



        public async Task<string> Cliente_Referencia_Proforma_Etiqueta_Segun_CodProforma(DBContext _context, int codproforma)
        {

            //////////////////
            var resultado = await (from pf_etiqueta in _context.veproforma_etiqueta
                                   join proforma in _context.veproforma
                                   on new { IdProforma = pf_etiqueta.id_proforma, NroIdProforma = pf_etiqueta.nroid_proforma.ToString() }
                                   equals new { IdProforma = proforma.id, NroIdProforma = proforma.numeroid.ToString() }
                                   where proforma.codigo == codproforma
                                   select new
                                   {
                                       pf_etiqueta.codcliente_casual,
                                       pf_etiqueta.codcliente_real
                                   }).FirstOrDefaultAsync();

            if (resultado != null)
            {
                return resultado.codcliente_real ?? resultado.codcliente_casual ?? "";
            }

            return "";
        }

        public async Task<DateTime> Descuento_Linea_Fecha_Desde(DBContext _context, string nivel)
        {
            DateTime resultado = new DateTime(1900, 1, 1);
            try
            {
                //using (_context)
                ////using (var _context = DbContextFactory.Create(userConnectionString))
                //{
                var result = await _context.vedesitem_parametros
                    .Where(v => v.nivel == nivel)
                    .Select(v => v.desde_fecha)
                    .FirstOrDefaultAsync();

                if (result.HasValue)
                {
                    resultado = result.Value;
                }
                // }
            }
            catch (Exception ex)
            {

            }
            return resultado;
        }


        ///////////////////////////////// RESTRICCIONES

        public async Task<int> penalizado(DBContext _context, string codcliente, int codgrupo)
        {
            int resultado = await _context.vepenal.Where(i => i.codcliente == codcliente && i.codgrupo == codgrupo)
                .Select(i=>i.tiempo).FirstOrDefaultAsync() ?? 0;
            return resultado;
        }

        public async Task<List<dtDescxLineaCli>> Descuentosporlinea(DBContext _context, string codcliente)
        {
            var resultado = await _context.vedescliente
                .Where(d => d.cliente == codcliente)
                .Join(_context.initem, d => d.coditem, i => i.codigo, (d, i) => new { d, i })
                .Join(_context.inlinea, di => di.i.codlinea, l => l.codigo, (di, l) => new { di.d, di.i, l })
                .GroupBy(x => new { x.d.cliente, x.l.codgrupo })
                .Select(g => new dtDescxLineaCli
                {
                    cliente = g.Key.cliente,
                    codgrupo = g.Key.codgrupo ?? 0,
                    nivel = g.Max(d => d.d.nivel) ?? ""
                })
                .ToListAsync();
            return resultado;
        }

        public async Task<bool> Enlazar_Proforma_Nueva_Con_SolDesctos_Nivel(DBContext _context, int codproforma, string idsol_des, int nroidsol_des)
        {
            bool resultado = true;
            if (resultado)
            {
                // Encontrar el id de veproforma correspondiente
                var proforma = await _context.veproforma
                                            .Where(v => v.codigo == codproforma)
                                            .FirstOrDefaultAsync();

                if (proforma != null)
                {
                    // Encontrar el registro en vesoldsctos que debe ser actualizado
                    var vesoldsctos = await _context.vesoldsctos
                                                   .Where(v => v.id == idsol_des && v.numeroid == nroidsol_des)
                                                   .FirstOrDefaultAsync();

                    if (vesoldsctos != null)
                    {
                        // Actualizar el idproforma y numeroid_proforma
                        vesoldsctos.idproforma = proforma.id;
                        vesoldsctos.numeroid_proforma = proforma.numeroid;
                        // Guardar los cambios en la base de datos
                        await _context.SaveChangesAsync();
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
            }
            return resultado;
        }


        public async Task<(string id, int nroId)> Solicitud_Urgente_IdNroid_de_Proforma(DBContext _context, string idpf, int nroidpf)
        {
            try
            {
                var resultado = await _context.insolurgente
                .Where(i => i.anulada == false &&
                i.idproforma == idpf &&
                i.numeroidproforma == nroidpf)
                .Select(i => new
                {
                    i.id,
                    i.numeroid
                })
                .FirstOrDefaultAsync();
                if (resultado != null)
                {
                    return (resultado.id, resultado.numeroid);
                }
                return ("", 0);
            }
            catch (Exception)
            {
                return ("", 0);
            }
        }

        public async Task<int> numitemscaja(DBContext _context, int nrocaja)
        {
            var resultado = await _context.vehoja
                .Join(_context.vedosificacion,
                      h => h.codigo,
                      d => d.codhoja,
                      (h, d) => new { H = h, D = d })
                .Where(de => de.D.activa == true && de.D.nrocaja == nrocaja)
                .Select(de => de.H.numitems)
                .FirstOrDefaultAsync();

            if (resultado != null)
            {
                return resultado;
            }
            return 0;
        }

        public async Task<bool> Existe_Factura(DBContext _context, string id, int numeroid)
        {
            var resultado = await _context.vefactura.Where(i => i.id == id && i.numeroid == numeroid).CountAsync();
            if (resultado > 0)
            {
                return true;
            }
            return false;
        }
        public async Task<string> Cliente_De_Factura(DBContext _context, string id, int numeroid)
        {
            string resultado = "";
            try
            {
                var resul = await _context.vefactura.Where(i => i.id == id && i.numeroid == numeroid).Select(i => i.codcliente).FirstOrDefaultAsync();
                resultado = resultado ?? "";

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return "";
            }
            return resultado;
        }
        public async Task<DateTime> Fecha_de_Factura(DBContext _context, string id, int numeroid)
        {
            DateTime resultado = DateTime.MinValue;
            try
            {
                var resul = await _context.vefactura.Where(i => i.id == id && i.numeroid == numeroid).Select(i => i.fecha).FirstOrDefaultAsync();
                if (resul != null)
                {
                    resultado = (DateTime)resul;
                }
            }
            catch
            {
                resultado = DateTime.MinValue;
            }
            return resultado;
        }

        public async Task<bool> Descuento_Linea_Habilitado(DBContext _context, string nivel)
        {
            var resultado = await _context.vedesitem_parametros.Where(i => i.nivel == nivel).Select(i => i.habilitado).FirstOrDefaultAsync();
            return resultado ?? false;
        }

        public async Task<bool> Item_habilitado_para_DescExtra(DBContext _context, int coddesextra, string coditem)
        {
            var resultado = await _context.vedesextra_item.Where(i => i.coddesextra == coddesextra && i.coditem == coditem).Select(i => i.habilitado_para_desc).FirstOrDefaultAsync() ?? false;
            return resultado;
        }
        public async Task<bool> proforma_es_complementaria(DBContext _context, int codigo)
        {
            try
            {
                int resultado = await _context.veproforma.Where(i => i.codigo == codigo).Select(i => i.codcomplementaria).FirstOrDefaultAsync();
                if (resultado > 0)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }
        public async Task<bool> Precio_Permite_Descto_Especial(DBContext _context, int coddescto_especial, int codtarifa)
        {
            try
            {
                bool resultado = true;
                //using (_context)
                //{
                int count = _context.vedescuento_tarifa
                    .Where(vc => vc.codtarifa == codtarifa && vc.coddescuento == coddescto_especial)
                    .Count();
                resultado = count > 0;
                return resultado;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return false;
            }
        }
        public async Task<bool> TarifaValidaNivel(DBContext _context, int codtarifa, string nivel)
        {
            bool resultado = false;
            try
            {

                int count = await _context.vedesitem_tarifa
                .CountAsync(t => t.nivel == nivel && t.codtarifa == codtarifa);

                if (count > 0)
                {
                    resultado = true;
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return false;
            }
            return resultado;
        }
        public async Task<DateTime> Descuento_Linea_Fecha_Hasta(DBContext _context, string nivel)
        {
            DateTime resultado = new DateTime(1900, 1, 1);
            try
            {
                //using (_context)
                ////using (var _context = DbContextFactory.Create(userConnectionString))
                //{
                var result = await _context.vedesitem_parametros
                    .Where(v => v.nivel == nivel)
                    .Select(v => v.hasta_fecha)
                    .FirstOrDefaultAsync();

                if (result.HasValue)
                {
                    resultado = result.Value;
                }
                // }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            return resultado.Date;
        }
        public async Task<string> descuentosstr(DBContext _context, int codigo, string tipo, string tipo_descrip_desextra = "corta")
        {
            List<dtinfVtaReporte>? detalle = null;
            List<dtinfDescReporte>? dt_descuentos = null;

            if (tipo == "NR")
            {
                detalle = await _context.veremision1.Where(i => i.codremision == codigo)
                    .Select(i => new dtinfVtaReporte
                    {
                        cantidad = (double)i.cantidad,
                        preciolista = (double)i.preciolista,
                        preciodesc = (double)(i.preciodesc ?? 0),
                        total = (double)i.total,
                        precioneto = (double)i.precioneto,
                        coddescuento = i.coddescuento
                    }).ToListAsync();

                dt_descuentos = await _context.vedesextraremi
                    .Where(d => d.codremision == codigo)
                    .Join(
                        _context.vedesextra,
                        d => d.coddesextra,
                        v => v.codigo,
                        (d, v) => new dtinfDescReporte
                        {
                            descorta = v.descorta,
                            desc_descto_completa = v.descripcion,
                            montodoc = (double)d.montodoc
                        })
                    .OrderBy(x => x.descorta)
                    .ToListAsync();

            }
            else if(tipo == "PF")
            {
                detalle = await _context.veproforma1.Where(i => i.codproforma == codigo)
                    .Select(i => new dtinfVtaReporte
                    {
                        cantidad = (double)i.cantidad,
                        preciolista = (double)i.preciolista,
                        preciodesc = (double)(i.preciodesc ?? 0),
                        total = (double)i.total,
                        precioneto = (double)i.precioneto,
                        coddescuento = i.coddescuento
                    }).ToListAsync();

                dt_descuentos = await _context.vedesextraprof
                    .Where(d => d.codproforma == codigo)
                    .Join(
                        _context.vedesextra,
                        d => d.coddesextra,
                        v => v.codigo,
                        (d, v) => new dtinfDescReporte
                        {
                            descorta = v.descorta,
                            desc_descto_completa = v.descripcion,
                            montodoc = (double)d.montodoc
                        })
                    .OrderBy(x => x.descorta)
                    .ToListAsync();

            }
            else if(tipo == "CT")
            {
                detalle = await _context.vecotizacion1.Where(i => i.codcotizacion == codigo)
                    .Select(i => new dtinfVtaReporte
                    {
                        cantidad = (double)i.cantidad,
                        preciolista = (double)i.preciolista,
                        preciodesc = (double)(i.preciodesc ?? 0),
                        total = (double)i.total,
                        precioneto = (double)i.precioneto,
                        coddescuento = i.coddescuento
                    }).ToListAsync();

                dt_descuentos = await _context.vedesextracoti
                    .Where(d => d.codcotizacion == codigo)
                    .Join(
                        _context.vedesextra,
                        d => d.coddesextra,
                        v => v.codigo,
                        (d, v) => new dtinfDescReporte
                        {
                            descorta = v.descorta,
                            desc_descto_completa = v.descripcion,
                            montodoc = (double)d.montodoc
                        })
                    .OrderBy(x => x.descorta)
                    .ToListAsync();
            }

            if (detalle != null && detalle.Count()>0)
            {
                double tot = 0;
                double totlinea = 0;
                double subtotal = 0;
                // ##para sacar descuento por linea
                foreach (var reg in detalle)
                {
                    tot = tot + (reg.cantidad * reg.preciolista);
                    totlinea = totlinea + (reg.cantidad * reg.preciodesc);
                    subtotal = subtotal + reg.total;
                }
                double TTL_DESCTOS = tot - totlinea;

                //para otros descuentos desglozado
                string cadena = "0 ";
                List<int> descuentose = new List<int>();
                List<double> totales = new List<double>();

                // sacar tipos de descuento
                foreach (var reg in detalle)
                {
                    if (reg.coddescuento > 0)
                    {
                        if (!descuentose.Contains(reg.coddescuento))
                        {
                            descuentose.Add(reg.coddescuento);
                        }
                    }
                }

                // sacar datos de documento
                // sacartotales
                foreach (var reg1 in descuentose)
                {
                    double totales_aux = 0;
                    foreach (var reg in detalle)
                    {
                        if (reg.coddescuento == reg1)
                        {
                            totales_aux = totales_aux + (reg.cantidad * (reg.preciodesc - reg.precioneto));
                        }
                    }
                    totales.Add(totales_aux);
                }
                int i = 0;
                foreach (var reg1 in descuentose)
                {
                    cadena = cadena + await nombres.nombredescuento(_context, reg1) + "= " + totales[i].ToString("#,0.00", new System.Globalization.CultureInfo("en-US")) + " ";
                    TTL_DESCTOS += totales[i];
                    i++;

                }

                // fin para el desgloce
                // #Descuentos para todo el documentos separar por pronto pago y otros
                string cadena_todo = "";
                foreach (var reg in dt_descuentos)
                {
                    TTL_DESCTOS += reg.montodoc;
                    if (tipo_descrip_desextra == "corta")
                    {
                        cadena_todo = cadena_todo + (reg.descorta + "= " + reg.montodoc.ToString("#,0.00", new System.Globalization.CultureInfo("en-US"))) + " ";
                    }
                    else
                    {
                        cadena_todo = cadena_todo + (reg.desc_descto_completa + "= " + reg.montodoc.ToString("#,0.00", new System.Globalization.CultureInfo("en-US"))) + "| ";
                    }
                }
                // #fin

                // cadena = "PRECIO LISTA= " & CDbl(tot).ToString("###,##0.00") & "|  DESC DE LINEA= " & CDbl(tot - totlinea).ToString("###,##0.00") & "|  DESC ESPECIAL: " & cadena & "| DESC EXTRAS: " & cadena_todo & "|  TTL DESCTOS= " & TTL_DESCTOS.ToString("###,##0.00")
                cadena = "PRECIO LISTA= " + tot.ToString("#,0.00", new System.Globalization.CultureInfo("en-US")) + "| DESC DE PROMOCION= " + (tot - totlinea).ToString("#,0.00", new System.Globalization.CultureInfo("en-US")) + "|  DESC ESPECIAL: " + cadena + "| DESC EXTRAS: " + cadena_todo + "|  TTL DESCTOS= " + TTL_DESCTOS.ToString("#,0.00", new System.Globalization.CultureInfo("en-US"));

                return cadena;

            }
            else
            {
                return "";
            }
        }



        public async Task<string> telefonocliente_direccion(DBContext _context, string codcliente, string direccion)
        {
            string resultado = "---";
            try
            {

                resultado = await _context.vetienda
                .Where(v => v.codcliente == codcliente && v.direccion.Contains(direccion))
                .Select(v => v.telefono).FirstOrDefaultAsync() ?? "---";

            }
            catch (Exception ex)
            {
                resultado = await telefonocliente(_context, codcliente);
            }
            return resultado;
        }

        public async Task<string> telefonocliente(DBContext _context, string codcliente)
        {
            string resultado = "---";
            try
            {

                resultado = await _context.vetienda
                .Where(v => v.codcliente == codcliente && v.central==true)
                .Select(v => v.telefono).FirstOrDefaultAsync() ?? "---";

            }
            catch (Exception ex)
            {
                resultado = "---";
            }
            return resultado;
        }

        public async Task<string> ptoventacliente_direccion(DBContext _context, string codcliente, string direccion, int largo = 0)
        {
            string resultado = "";
            if (codcliente.Trim() == "")
            {
                resultado = "";
            }
            else
            {
                var tabla = await _context.vetienda
                    .Where(t => t.codcliente == codcliente)
                    .Join(_context.veptoventa,
                          t => t.codptoventa,
                          p => p.codigo,
                          (t, p) => new
                          {
                              t.codptoventa,
                              p.descripcion,
                              direccion1 = t.direccion + " (" + p.descripcion + " - " + p.codprovincia + ")"
                          })
                    .Where(temp => temp.direccion1.Contains(direccion))
                    .FirstOrDefaultAsync();
                if (tabla != null)
                {
                    resultado = tabla.codptoventa + " " + tabla.descripcion;
                }
                else
                {
                    resultado = await ptoventacliente(_context, codcliente, largo);
                }
            }

            if (largo > 0)
            {
                if (resultado.Length > largo)
                {
                    resultado = resultado.Substring(0, largo);
                }
            }
            return resultado;
        }

        public async Task<string> ptoventacliente(DBContext _context, string codcliente, int largo = 0)
        {
            string resultado = "";
            if (codcliente.Trim() == "")
            {
                resultado = "";
            }
            else
            {
                var tabla = await _context.vetienda
                    .Where(t => t.codcliente == codcliente
                    && t.central == true)
                    .Join(_context.veptoventa,
                          t => t.codptoventa,
                          p => p.codigo,
                          (t, p) => new
                          {
                              t.codptoventa,
                              p.descripcion,
                          })
                    .FirstOrDefaultAsync();
                if (tabla != null)
                {
                    resultado = tabla.codptoventa + " " + tabla.descripcion;
                }
                else
                {
                    resultado = "";
                }
            }

            if (largo > 0)
            {
                if (resultado.Length > largo)
                {
                    resultado = resultado.Substring(0, largo);
                }
            }
            return resultado;
        }

        public async Task<string> Proforma_Transporte(DBContext _context, int codproforma)
        {
            string resultado = "";
            try
            {
                var dt = await _context.veproforma.Where(i => i.codigo == codproforma)
                    .Select(i => new
                    {
                        i.tipoentrega,
                        i.transporte,
                        i.nombre_transporte
                    }).FirstOrDefaultAsync();
                if (dt != null)
                {
                    resultado = dt.tipoentrega + " - " + dt.transporte + " - " + dt.nombre_transporte;
                }
            }
            catch (Exception)
            {
                resultado = "";
            }
            return resultado;
        }

        public async Task<bool> proforma_ya_esta_etiqueta_impresa(DBContext _context, int codproforma)
        {
            try
            {
                bool resultado = await _context.veproforma.Where(i => i.codigo == codproforma).Select(i => i.etiqueta_impresa).FirstOrDefaultAsync() ?? false;
                return resultado;
            }
            catch (Exception)
            {
                return false;
            }
        }


        public async Task proforma_marcar_etiqueta_impresa(DBContext _context, int codproforma)
        {
            try
            {
                var resultado = await _context.veproforma.Where(i => i.codigo == codproforma).FirstOrDefaultAsync();
                if (resultado!=null)
                {
                    resultado.etiqueta_impresa = true;
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception)
            {

            }
        }

        public async Task<List<detalle_tuercas>> Detalle_tuercas_PF(DBContext _context, List<itemDataMatriz> tabladetalle)
        {
            List<detalle_tuercas> resultado = new List<detalle_tuercas>();
            var dt_inlinea_tuercas = await items.inlinea_tuercas(_context, true);
            foreach (var reg in tabladetalle)
            {
                string item_linea_pf = await items.itemlinea(_context, reg.coditem);
                if (await items.itemesconjunto(_context,reg.coditem) == true && (item_linea_pf != "05CT0" && item_linea_pf != "05CT1"))
                {
                    // es cjto
                    List<string> lista_items_cjto = await items.Partes_De_Conjunto(_context, reg.coditem);
                    foreach (var reg2 in lista_items_cjto)
                    {
                        // recorrer los items q forman el conjunto y verificar si uno de ellos es tuerca y añadir o sumar al resultado
                        string item_linea = await items.itemlinea(_context, reg2);
                        var registros_inlinea = dt_inlinea_tuercas.Where(i => i.codigo == item_linea).ToList();
                        if (registros_inlinea.Count() > 0)
                        {
                            var registros_item_tuercas = resultado.Where(i => i.coditem == reg2);
                            if (registros_item_tuercas.Count() > 0)
                            {
                                // si el item ya esta en resultado solo sumar al registro
                                foreach (var reg3 in registros_item_tuercas)
                                {
                                    reg3.cantidad = (decimal)reg.cantidad + reg3.cantidad;
                                }
                            }
                            else
                            {
                                // si el item NO esta en resultado añadir al resutlado
                                detalle_tuercas registro = new detalle_tuercas();
                                registro.coditem = reg2;
                                registro.descripcion = await items.itemdescripcion(_context, reg2);
                                registro.medida = await items.itemmedida(_context, reg2);
                                registro.udm = await items.itemudm(_context, reg2);
                                registro.cantidad = (decimal)reg.cantidad;
                                resultado.Add(registro);
                            }
                        }
                    }
                }
            }
            return resultado;
        }


        public async Task<bool> anular_proforma(DBContext _context, int codProforma)
        {
            try
            {
                var tabla = await _context.veproforma.Where(i => i.codcomplementaria == codProforma).FirstOrDefaultAsync();
                if (tabla != null)
                {
                    var tabla2 = await _context.veproforma.Where(i => i.codigo == codProforma).FirstOrDefaultAsync();
                    if (tabla2.codcomplementaria > 0)
                    {
                        tabla.codcomplementaria = tabla2.codcomplementaria;
                        _context.Entry(tabla).State = EntityState.Modified;
                        await _context.SaveChangesAsync();
                    }
                    else
                    {
                        tabla.codcomplementaria = 0;
                        _context.Entry(tabla).State = EntityState.Modified;
                        await _context.SaveChangesAsync();
                    }
                }
                var veproformaModif = await _context.veproforma.Where(i => i.codigo == codProforma).FirstOrDefaultAsync();
                veproformaModif.codcomplementaria = 0;
                veproformaModif.confirmada = false;
                veproformaModif.anulada = true;
                veproformaModif.aprobada = false;
                veproformaModif.paraaprobar = false;

                _context.Entry(veproformaModif).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }


        ///////////////////////////////////////////////////////////////////////////////
        //esta funcion verifica si un id de ventas descarga mercaderia o no
        ///////////////////////////////////////////////////////////////////////////////
        public async Task<bool> iddescarga(DBContext _context, string id)
        {
            try
            {
                bool resultado = await _context.venumeracion.Where(i => i.id == id).Select(i => i.descarga).FirstOrDefaultAsync();
                return resultado;
            }
            catch (Exception)
            {
                return true;
            }
        }




        public async Task<bool> revertirstocksproforma(DBContext _context, int codigo, string codempresa)
        {
            bool resultado = true;
            int codalmacen = new int();
            if (await configuracion.emp_proforma_reserva(_context,codempresa))
            {
                var tabla_aux = await _context.veproforma.Where(i => i.codigo == codigo).Select(i => new
                {
                    i.codalmacen
                }).FirstOrDefaultAsync();
                if (tabla_aux != null)
                {
                    codalmacen = tabla_aux.codalmacen;
                }
                else
                {
                    resultado = false;
                }
                var tabla = await _context.veproforma1.Where(i => i.codproforma == codigo).Select(i => new
                {
                    i.coditem,
                    i.cantaut
                }).ToListAsync();
                if (resultado)
                {
                    if (tabla.Count() == 0)
                    {
                        resultado = false; 
                    }
                }

                if (resultado)
                {
                    try
                    {
                        var nulos = await _context.instoactual.Where(i => i.proformas == null).ToListAsync();
                        if (nulos.Count() > 0)
                        {
                            foreach (var reg in nulos)
                            {
                                reg.proformas = 0;
                            }
                            await _context.SaveChangesAsync();
                        }
                    }
                    catch (Exception)
                    {
                        resultado = false;
                    }

                    foreach (var reg  in tabla)
                    {
                        if (await items.itemesconjunto(_context,reg.coditem))
                        {
                            var tablakit = await _context.inkit.Where(i => i.codigo == reg.coditem).Select(i => new
                            {
                                i.item,
                                i.cantidad
                            }).ToListAsync();
                            foreach (var item in tablakit)
                            {
                                try
                                {
                                    var itemInstroactual = await _context.instoactual.Where(i => i.codalmacen == codalmacen && i.coditem == item.item).FirstOrDefaultAsync();
                                    itemInstroactual.proformas = itemInstroactual.proformas - (reg.cantaut * item.cantidad);
                                    _context.Entry(itemInstroactual).State = EntityState.Modified;
                                    await _context.SaveChangesAsync();
                                }
                                catch (Exception)
                                {
                                    resultado = false;
                                    break;
                                }
                            }
                            if (!resultado)
                            {
                                break;
                            }
                        }
                        else
                        {
                            try
                            {
                                var itemInstroactual = await _context.instoactual.Where(i => i.codalmacen == codalmacen && i.coditem == reg.coditem).FirstOrDefaultAsync();
                                itemInstroactual.proformas = itemInstroactual.proformas - (reg.cantaut);
                                _context.Entry(itemInstroactual).State = EntityState.Modified;
                                await _context.SaveChangesAsync();
                            }
                            catch (Exception)
                            {
                                resultado = false;
                                break;
                            }
                        }
                    }
                }
                // actualizar a 0 si hay negativos
                try
                {
                    var negativos = await _context.instoactual.Where(i => i.proformas < 0).ToListAsync();
                    if (negativos.Count() > 0)
                    {
                        foreach (var reg in negativos)
                        {
                            reg.proformas = 0;
                        }
                        await _context.SaveChangesAsync();
                    }
                }
                catch (Exception)
                {
                    // throw;
                }
            }
            return resultado;
        }


        public async Task<bool> ProformaTieneNRAnulada(DBContext _context, int codproforma)
        {
            bool resultado = false;
            try
            {
                var c = await _context.veremision.Where(i => i.anulada == true && i.codproforma == codproforma).CountAsync();
                if (c > 0)
                {
                    resultado = true;
                }
            }
            catch (Exception)
            {
                resultado = false;
            }
            return resultado;
        }
        public async Task<DateTime> ProformaFechaMasAntiguaNR(DBContext _context, int codproforma)
        {
            DateTime resultado = new DateTime();
            try
            {
                resultado = await _context.veremision
                    .Where(v => v.codproforma == codproforma)
                    .OrderBy(v => v.fecha)
                    .Select(v => v.fecha)
                    .FirstOrDefaultAsync();
            }
            catch (Exception)
            {
                resultado = await funciones.FechaDelServidor(_context);
            }
            return resultado;
        }


        public async Task<double> TotalNRconNC_ND(DBContext _context, int codigo)
        {
            double resultado = 0;

            var dt_nota = await _context.veremision.Where(i => i.codigo == codigo).Select(i => new
            {
                i.id,
                i.numeroid,
                i.total,
                i.codmoneda,
                i.fecha
            }).FirstOrDefaultAsync();
            if (dt_nota != null)
            {
                resultado = (double)dt_nota.total;
                // #####notas de credito
                var dt_notas_credito = await _context.venotacredito
                    .Where(n => n.anticipo == false && n.anulada == false)
                    .Join(_context.vefactura,
                          n => n.codfactura,
                          f => f.codigo,
                          (n, f) => new { NotaCredito = n, Factura = f })
                    .Where(joined => joined.Factura.codremision == codigo)
                    .Select(joined => new
                    {
                        total = joined.NotaCredito.subtotal,
                        codmoneda = joined.NotaCredito.codmoneda,
                        fecha = joined.NotaCredito.fecha
                    })
                    .ToListAsync();
                foreach (var reg in dt_notas_credito)
                {
                    if (dt_nota.codmoneda == reg.codmoneda)
                    {
                        resultado = resultado - (double)(reg.total);
                    }
                    else
                    {
                        resultado = resultado - (double)(await tipocambio._conversion(_context, dt_nota.codmoneda, reg.codmoneda, dt_nota.fecha, reg.total));
                    }
                }

                // #####notas de debito
                var dt_notas_debito = await _context.vefactura
                    .Where(f => f.anulada == false && f.notadebito == true && f.idfc == dt_nota.id && f.numeroidfc == dt_nota.numeroid)
                    .Select(f => new
                    {
                        total = f.subtotal,
                        f.codmoneda,
                        f.fecha
                    })
                    .ToListAsync();
                foreach (var reg in dt_notas_debito)
                {
                    if (dt_nota.codmoneda == reg.codmoneda)
                    {
                        resultado = resultado + (double)reg.total;
                    }
                    else
                    {
                        resultado = resultado + (double)(await tipocambio._conversion(_context, dt_nota.codmoneda, reg.codmoneda, dt_nota.fecha, reg.total));
                    }
                }
            }
            if (resultado < 0)
            {
                resultado = 0;
            }
            return Math.Round(resultado, 2, MidpointRounding.AwayFromZero);
        }

        public async Task<(string id, int numeroId)> id_nroid_remision2(DBContext _context, int codremision)
        {
            try
            {
                var tbl = await _context.veremision
                    .Where(v => v.codigo == codremision)
                    .Select(v => new
                    {
                        v.id,
                        v.numeroid
                    })
                    .FirstOrDefaultAsync();
                if (tbl != null)
                {
                    return (tbl.id, tbl.numeroid);
                }
                return ("NSE", 0);
            }
            catch (Exception)
            {
                return ("NSE", 0);
            }
        }


        public async Task<string> Cliente_De_Remision(DBContext _context, int codigo)
        {
            string resultado = "";
            try
            {
                resultado = await _context.veremision
                    .Where(v => v.codigo == codigo)
                    .Select(v => v.codcliente)
                    .FirstOrDefaultAsync() ?? "";
            }
            catch (Exception)
            {
                resultado = "";
            }
            return resultado;
        }



        ///////////////////////////////////////////////////////////////////////////////
        // funcion que genera cuotas de pago segun un cliente, monto, codigo de docuemtno y tipo de documento
        ///////////////////////////////////////////////////////////////////////////////
        public async Task<bool> generarcuotaspago(DBContext _context, int codigodoc, int tipo, double sobremonto, double montod, string moneda, string codcliente, DateTime fechaini, bool es_reversion, string codempresa)
        {
            bool resultado = true;
            double montodoc = montod;
            string monedadoc = moneda;
            string clientedoc = await Cliente_De_Remision(_context,codigodoc);
            int nrocuotas = new int();
            double[,] cuotas = new double[3, 1];

            /// obtener la informacion
            // si el total es igual a cero entonces buscar el monto del docuemto
            if (montodoc == 0)
            {
                var tabla = await _context.veremision.Where(i => i.codigo == codigodoc).Select(i => new
                {
                    i.total,
                    i.codmoneda,
                    i.codcliente
                }).FirstOrDefaultAsync();
                if (tabla != null)
                {
                    montodoc = await TotalNRconNC_ND(_context, codigodoc);
                    if (monedadoc == "")
                    {
                        monedadoc = tabla.codmoneda;
                    }
                    if (clientedoc == "")
                    {
                        clientedoc = tabla.codcliente;
                    }
                }
            }


            // si algo paso
            if (montodoc == 0)
            {
                montodoc = montod;
                monedadoc = moneda;
            }

            // Desde 04 / 10 / 2023 aqui convertir el total de la nota a $us si la moneda de la nota esta en bolivianos para q con ese monto elija correctamente el plan
            // de pagos a generar sobremonto
            string monedabase = await Empresa.monedabase(_context, codempresa);
            string monedaus = await empresa.monedaext(_context,codempresa);
            if (monedadoc == monedabase)
            {
                // si la moneda de la NR es igual a la moneda base BS, convertir el monto en $us
                sobremonto = (double)await tipocambio._conversion(_context, monedaus, monedabase, DateTime.Today.Date, (decimal)sobremonto);
                sobremonto = Math.Round(sobremonto, 2);
            }
            ///
            if (montodoc == 0)
            {
                // su plan de pagos es igual a cero
            }
            else
            {
                if (await remision_es_PP(_context,codigodoc))
                {
                    nrocuotas = 1;
                    // --nro de cuota
                    cuotas[0, 0] = 1;
                    // --porcentaje
                    cuotas[1, 0] = 100;
                    // --dias
                     cuotas[2, 0] = await ProntoPago.DiasDescuentoProntoPagoNota(_context,codigodoc);
                }
                else
                {
                    List<planPagosCliente> planPagos = new List<planPagosCliente>();
                    if (es_reversion)
                    {
                        int codplanpago_reversion = await configuracion.emp_codplanpago_reversion(_context,codempresa);
                        planPagos = await _context.veplanpago
                            .Join(_context.veplanpago1,
                                  p => p.codigo,
                                  p1 => p1.codplanpago,
                                  (p, p1) => new { p, p1 })
                            .Where(joined => sobremonto >= (double)joined.p1.montodel
                                             && sobremonto <= (double)joined.p1.montoal
                                             && joined.p.codigo == codplanpago_reversion)
                            .Select(joined => new planPagosCliente
                            {
                                codigo = joined.p1.codigo,
                                nrocuotas = joined.p1.nrocuotas
                            }).ToListAsync();
                    }
                    else
                    {
                        int plan_especial = await PlanItemEspecial(_context,codigodoc);
                        if (plan_especial > 0)
                        {
                            // #####Si en el documento hay items especiales usar ese plan de pagos
                            planPagos = await _context.veplanpago
                            .Join(_context.veplanpago1,
                                  p => p.codigo,
                                  p1 => p1.codplanpago,
                                  (p, p1) => new { p, p1 })
                            .Where(joined => sobremonto >= (double)joined.p1.montodel
                                             && sobremonto <= (double)joined.p1.montoal
                                             && joined.p.codigo == plan_especial)
                            .Select(joined => new planPagosCliente
                            {
                                codigo = joined.p1.codigo,
                                nrocuotas = joined.p1.nrocuotas
                            }).ToListAsync();
                        }
                        else
                        {
                            // ####3Si no usar el plan de pagos del cliente
                            planPagos = await _context.vecliente
                                .Join(_context.veplanpago,
                                      c => c.codplanpago,
                                      p => p.codigo,
                                      (c, p) => new { c, p })
                                .Join(_context.veplanpago1,
                                      cp => cp.p.codigo,
                                      p1 => p1.codplanpago,
                                      (cp, p1) => new { cp.c, cp.p, p1 })
                                .Where(joined => sobremonto >= (double)joined.p1.montodel
                                                 && sobremonto <= (double)joined.p1.montoal
                                                 && joined.c.codigo == clientedoc)
                                .Select(joined => new planPagosCliente
                                {
                                    codigo = joined.p1.codigo,
                                    nrocuotas = joined.p1.nrocuotas
                                }).ToListAsync();
                        }
                    }
                    if (planPagos.Count() > 0)
                    {
                        // si hay plan de pagos adecuado ver si tiene especificacion de porcentajes por cuota
                        int codigo_pp = planPagos[0].codigo;
                        nrocuotas = planPagos[0].nrocuotas;

                        var veplanpago2List = await _context.veplanpago2.Where(i => i.coddetplanpago == codigo_pp).Select(i => new
                        {
                            i.nrocuota,
                            i.porcen,
                            i.diaspago
                        }).ToListAsync();
                        if (veplanpago2List.Count() > 0)
                        {
                            // hay especificacion de porcentajes por cuota
                            double[,] nuevaCuotas = new double[3, veplanpago2List.Count];

                            for (int i = 0; i < veplanpago2List.Count(); i++)
                            {
                                nuevaCuotas[0, i] = (double)veplanpago2List[i].nrocuota;
                                nuevaCuotas[1, i] = (double)veplanpago2List[i].porcen;
                                nuevaCuotas[2, i] = (double)veplanpago2List[i].diaspago;
                            }
                            // Asignamos la nueva matriz a 'cuotas'
                            cuotas = nuevaCuotas;
                        }
                        else
                        {
                            // no hay especificacion de porcentajes por cuota
                            ///// crear cuotas iguales
                            double acumulado_1 = 0;
                            double porcencuota = Math.Floor((100.0 / nrocuotas) * 100) / 100;
                            double primeracuota = 100 - (porcencuota * (nrocuotas - 1));

                            double[,] nuevaCuotas = new double[3, nrocuotas +1];

                            for (int i = 1; i <= nrocuotas; i++)  // todas menos la primera cuota
                            {
                                nuevaCuotas[0, i] = i + 1;
                                nuevaCuotas[1, i] = primeracuota;
                                nuevaCuotas[2, i] = 15;
                                acumulado_1 = acumulado_1 + primeracuota;
                            }

                            porcencuota = 100 - acumulado_1;
                            nuevaCuotas[0, 0] = 1;
                            nuevaCuotas[1, 0] = porcencuota;
                            nuevaCuotas[2, 0] = 15;
                            // Asignamos la nueva matriz a 'cuotas'
                            cuotas = nuevaCuotas;

                            /////fin de crear cuotas iguales
                        }
                    }
                    else
                    {
                        // no hay plan de pagos adecuado asi que una cuota 100% 0 dias
                        nrocuotas = 1;
                        cuotas[0, 0] = 1;
                        cuotas[1, 0] = 100;
                        cuotas[2, 0] = 0;
                    }
                }
                /////generar pagos
                // verificacion apra ver si el documento descarga mercaderia
                // borrar las anteriores cuotas
                try
                {
                    var coplancuotasAnt = await _context.coplancuotas.Where(i => i.coddocumento == codigodoc).ToListAsync();
                    if (coplancuotasAnt.Count() > 0)
                    {
                        _context.coplancuotas.RemoveRange(coplancuotasAnt);
                        await _context.SaveChangesAsync();
                    }
                }
                catch (Exception)
                {
                    resultado = false;
                }
                DateTime fecha = fechaini;
                double acumulado = 0;
                if (resultado)
                {
                    List<coplancuotas> NewListCoPlanCuo = new List<coplancuotas>();
                    for (int i = 0; i <= nrocuotas - 2; i++)
                    {
                        fecha = fecha.AddDays(cuotas[2, i]);
                        // acumulado = acumulado + System.Math.Round((montodoc / 100) * (cuotas(1, i)), 2)
                        acumulado = acumulado + Math.Floor((montodoc / 100) * (cuotas[1, i]));
                        coplancuotas newReg = new coplancuotas();

                        newReg.coddocumento = codigodoc;
                        newReg.codtipodoc = (byte)tipo;
                        newReg.cliente = clientedoc;
                        newReg.nrocuota = (short)cuotas[0, i];
                        newReg.monto = (decimal)Math.Floor((montodoc / 100) * (cuotas[1, i]));
                        newReg.montopagado = 0;
                        newReg.moneda = monedadoc;
                        newReg.vencimiento = fecha.Date;

                        NewListCoPlanCuo.Add(newReg);
                    }


                    fecha = fecha.AddDays(cuotas[2, nrocuotas - 1]);
                    // ultima cuota que complete todo
                    coplancuotas newReg_1 = new coplancuotas();

                    newReg_1.coddocumento = codigodoc;
                    newReg_1.codtipodoc = (byte)tipo;
                    newReg_1.cliente = clientedoc;
                    newReg_1.nrocuota = (short)cuotas[0, nrocuotas - 1];
                    newReg_1.monto = (decimal)(montodoc - acumulado);
                    newReg_1.montopagado = 0;
                    newReg_1.moneda = monedadoc;
                    newReg_1.vencimiento = fecha.Date;

                    NewListCoPlanCuo.Add(newReg_1);

                    try
                    {
                        _context.coplancuotas.AddRange(NewListCoPlanCuo);
                        await _context.SaveChangesAsync();
                    }
                    catch (Exception)
                    {
                        resultado = false;
                    }
                }
                ///fin de crear cuotas
            }
            ///////////////////////////// FALTAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA
            return resultado;
        }

        public async Task<int> PlanItemEspecial(DBContext _context, int cod_remision)
        {
            int resultado = 0;
            try
            {
                var dt = await _context.veremision1.Where(i => i.codremision == cod_remision).Select(i => new
                {
                    i.coditem,
                    i.codtarifa
                }).OrderBy(i => i.coditem)
                .ThenBy(i => i.codtarifa)
                .ToListAsync();
                foreach (var reg in dt)
                {
                    int plan = await EsItemEspecial(_context, reg.coditem, reg.codtarifa);
                    if (plan > 0)
                    {
                        resultado = plan;
                        break;
                    }
                }
            }
            catch (Exception)
            {
                resultado = 0;
            }
            return resultado;
        }
        public async Task<int> EsItemEspecial(DBContext _context, string coditem, int codtarifa)
        {
            var result = await _context.veespeciales.Where(i => i.coditem == coditem && i.codtarifa == codtarifa).Select(i => new
            {
                i.codplanpago
            }).OrderBy(i => i.codplanpago)
            .FirstOrDefaultAsync();
            if (result != null)
            {
                return result.codplanpago ?? 0;
            }
            return 0;

        }

        public async Task<string> aclaracion_direccion_direccion(DBContext _context, string codcliente, string direccion)
        {
            string resultado = "---";
            try
            {
                resultado = await _context.vetienda.Where(i => i.codcliente == codcliente && i.direccion.StartsWith(direccion)).Select(i => i.aclaracion_direccion).FirstOrDefaultAsync() ?? "";
            }
            catch (Exception)
            {
                resultado = await telefonocliente(_context,codcliente);
            }
            if (resultado.Trim() == "")
            {
                resultado = "---";
            }
            return resultado;

        }

        public async Task<bool> DireccionNotaRemisionEsCentral(DBContext _context, string id, int numeroid)
        {
            bool resultado = false;
            try
            {
                var dt = await _context.veremision.Where(i => i.id == id && i.numeroid == numeroid).Select(i => new
                {
                    i.codcliente,
                    i.direccion
                }).FirstOrDefaultAsync();
                if (dt != null)
                {
                    resultado = await _context.vetienda.Where(i => i.direccion.StartsWith(dt.direccion.Trim()) && i.codcliente == dt.codcliente).Select(i => i.central).FirstOrDefaultAsync();
                }
                else
                {
                    resultado = false;
                }
            }
            catch (Exception)
            {
                resultado = false;
            }
            return resultado;

        }

        public async Task<bool> Remision_Tiene_DescuentoExtra(DBContext _context, int codremision, int coddesextra)
        {
            bool resultado = false;
            try
            {
                int dt = await _context.vedesextraremi.Where(i => i.codremision == codremision && i.coddesextra == coddesextra).CountAsync();
                if (dt > 0)
                {
                    resultado = true;
                }
                else
                {
                    resultado = false;
                }
            }
            catch (Exception)
            {
                resultado = false;
            }
            return resultado;
        }

        public async Task<bool> EsRemisionEspecial(DBContext _context, int cod_remision)
        {
            bool resultado = false;
            try
            {
                var dt = await _context.veremision1.Where(i => i.codremision == cod_remision).Select(i => new
                {
                    i.coditem,
                    i.codtarifa
                }).Distinct().ToListAsync();
                foreach (var reg in dt)
                {
                    if (await EsItemEspecial(_context,reg.coditem, reg.codtarifa) != 0)
                    {
                        resultado = true;
                        break;
                    }
                }
            }
            catch (Exception)
            {
                resultado = false;
            }
            return resultado;
        }

        ///////////////////////////////////////////////////////////////////////////////
        // Esta funcion devuelve en una cadena el detalle de plan de pagos de un documento
        ///////////////////////////////////////////////////////////////////////////////
        public async Task<string> planpagosstr(DBContext _context, int codigo)
        {
            string resultado = "";
            var tabla = await _context.coplancuotas.Where(i=> i.coddocumento == codigo).ToListAsync();

            int dias_pp_cliente = 0;
            if (tabla.Count() > 0)
            {
                dias_pp_cliente = await cliente.dias_pronto_pago(_context, tabla[0].cliente);
            }

            var dt_modifica_dias = await _context.vedesextra_modifica_dias.ToListAsync();

            /*
             
            ''LARGO 100
            ''resultado = "  No  VENCTO      IMPORTE  No  VENCTO      IMPORTE  No  VENCTO      IMPORTE  No  VENCTO      IMPORTE"
            'If tabla.Rows.Count > 0 Then
            '    Dim i As Integer
            '    For i = 0 To tabla.Rows.Count - 1
            '        '  NN XX/XX/XXXX 99,999.99  NN XX/XX/XXXX 99,999.99  NN XX/XX/XXXX 99,999.99  NN XX/XX/XXXX 99,999.99
            '        resultado = resultado & "  " & CInt(tabla.Rows(i)("nrocuota")).ToString("00") & " " & CDate(tabla.Rows(i)("vencimiento")).ToString("dd/MM/yyyy") & " " & sia_funciones.Funciones.Instancia.rellenar(CDbl(tabla.Rows(i)("monto")).ToString("##,##0.00"), 9, " ")
            '    Next
            'End If
            'resumido
             
             */
            int dias_disminuye = 0;
            foreach (var reg in dt_modifica_dias)
            {
                // la nota tiene el descuento extra para el cual esta configurado se debe descotar los dias de vencimiento  de impresion???
                if (await Remision_Tiene_DescuentoExtra(_context,codigo, reg.coddesextra ?? 0))
                {
                    dias_disminuye += (reg.dias_disminuye ?? 0);
                    break;
                }
            }
            dias_disminuye *= -1;
            if (dias_disminuye == 0)
            {
                resultado = " [Nro VENCTO IMPORTE]: ";
            }
            else
            {
                // esto es para que en la impresion se pueda tener una señal que permite
                // al funcionario de Pertec dterminar que la fecha de vencimiento
                // esta disminuida
                resultado = " *[Nro VENCTO IMPORTE]: ";
            }
            if (tabla.Count() > 0)
            {
                DateTime fecha_vence = new DateTime();
                foreach (var reg in tabla)
                {
                    fecha_vence = reg.vencimiento.AddDays(dias_disminuye);
                    //   NN XX/XX/XXXX 99,999.99  NN XX/XX/XXXX 99,999.99  NN XX/XX/XXXX 99,999.99  NN XX/XX/XXXX 99,999.99
                    // resultado = resultado & " [" & CInt(tabla.Rows(i)("nrocuota")).ToString("00") & " " & CDate(tabla.Rows(i)("vencimiento")).ToString("dd/MM/yyyy") & "  " & CDbl(tabla.Rows(i)("monto")).ToString("##,##0.00") & "]  "
                    resultado = resultado + " [" + reg.nrocuota.ToString("00") + " " + fecha_vence.ToString("dd/MM/yyyy") + " " + reg.monto.ToString("##,##0.00") + "]  ";
                }
            }

            return resultado;
        }






        public async Task<string> remision_id_nro(DBContext _context, int codigo)
        {
            try
            {
                string resultado = await _context.veremision.Where(i => i.codigo == codigo).Select(i => i.id + "-" + i.numeroid).FirstOrDefaultAsync() ?? "";
                return resultado;
            }
            catch (Exception)
            {
                return "";
            }
        }


        public async Task<List<list_PFNR_comp_>> lista_PFNR_complementarias(DBContext _context, int codproforma)
        {
            bool bandera = true;
            //##################################'
            //# estructura de la lista         #
            //##################################'
            List<list_PFNR_comp_> lista = new List<list_PFNR_comp_>();

            //##################################'
            //#obtener el codigo de la nota actual
            //##################################'
            int codigo = codproforma;

            if (codigo > 0)
            {
                //##################################'
                //#ir al primer codigo de las proformas
                //##################################'
                bandera = true;
                while (bandera)
                {
                    var tabla = await _context.veproforma.Where(i => i.codcomplementaria == codigo).Select(i => new
                    {
                        i.codigo
                    }).ToListAsync();
                    if (tabla.Count() > 0)
                    {
                        codigo = tabla[0].codigo;
                    }
                    else
                    {
                        bandera = false;
                    }
                }

                //##################################'
                // #recorrer todos los codigos añadiendo a lista
                //##################################'
                bandera = true;
                while (bandera)
                {
                    var tabla = await _context.veproforma.Where(i => i.codigo == codigo).Select(i => new
                    {
                        i.codigo,
                        i.codcomplementaria
                    }).ToListAsync();
                    if (tabla.Count() > 0)
                    {
                        // añadir a la lista
                        list_PFNR_comp_ registro = new list_PFNR_comp_();
                        registro.codproforma = tabla[0].codigo;
                        registro.codremision = 0;
                        lista.Add(registro);
                        // si tiene complementaria copiar codigo, si no terminar el loop
                        if (tabla[0].codcomplementaria == null || tabla[0].codcomplementaria == 0)
                        {
                            bandera = false;
                        }
                        else
                        {
                            codigo = tabla[0].codcomplementaria;
                        }
                    }
                    else
                    {
                        bandera = false;
                    }
                }
                //'##################################'
                //#   poner las notas de remision  #
                //##################################'
                foreach (var reg in lista)
                {
                    var tabla = await _context.veremision.Where(i => i.anulada == false && i.codproforma == reg.codproforma).Select(i => new
                    {
                        i.codigo,
                        i.fecha
                    }).FirstOrDefaultAsync();
                    if (tabla != null)
                    {
                        reg.codremision = tabla.codigo;
                        reg.fecharemision = tabla.fecha;
                    }
                    else
                    {
                        reg.codremision = 0;
                        reg.fecharemision = await funciones.FechaDelServidor(_context);
                    }
                }
            }
            return lista;
        }



        public async Task<List<list_PFNR_comp_>> lista_PFNR_complementarias_noPP(DBContext _context, int codproforma)
        {
            bool bandera = true;
            //##################################'
            //# estructura de la lista         #
            //##################################'
            List<list_PFNR_comp_> lista = new List<list_PFNR_comp_> ();

            //##################################'
            //#obtener el codigo de la nota actual
            //##################################'
            int codigo = codproforma;
            if (codigo > 0)
            {
                //##################################'
                //#ir al primer codigo de las proformas
                //##################################'
                bandera = true;
                while (bandera)
                {
                    var tabla = await _context.veproforma.Where(i => i.codcomplementaria == codigo).Select(i => new
                    {
                        i.codigo
                    }).ToListAsync();
                    if (tabla.Count() > 0)
                    {
                        codigo = tabla[0].codigo;
                    }
                    else
                    {
                        bandera = false;
                    }
                }
                //##################################'
                // #recorrer todos los codigos añadiendo a lista
                //##################################'
                bandera = true;
                while (bandera)
                {
                    var tabla = await _context.veproforma.Where(i => i.codigo == codigo).Select(i => new
                    {
                        i.codigo,
                        i.codcomplementaria
                    }).ToListAsync();
                    if (tabla.Count() > 0)
                    {
                        // añadir a la lista
                        list_PFNR_comp_ registro = new list_PFNR_comp_();
                        registro.codproforma = tabla[0].codigo;
                        registro.codremision = 0;
                        lista.Add(registro);
                        // si tiene complementaria copiar codigo, si no terminar el loop
                        if (tabla[0].codcomplementaria == null || tabla[0].codcomplementaria == 0)
                        {
                            bandera = false;
                        }
                        else
                        {
                            codigo = tabla[0].codcomplementaria;
                        }
                    }
                    else
                    {
                        bandera = false;
                    }
                }
                //'##################################'
                //#   poner las notas de remision  #
                //##################################'
                foreach (var reg in lista)
                {
                    var tabla = await _context.veremision.Where(i => i.anulada == false && i.codproforma == reg.codproforma).Select(i => new
                    {
                        i.codigo,
                        i.fecha
                    }).FirstOrDefaultAsync();
                    if (tabla != null)
                    {
                        reg.codremision = tabla.codigo;
                        reg.fecharemision = tabla.fecha;
                    }
                    else
                    {
                        reg.codremision = 0;
                        reg.fecharemision = await funciones.FechaDelServidor(_context);
                    }
                }

                // ##################################'
                // #  Borrar todas las que son PP  #
                // ##################################'
                List<list_PFNR_comp_> filasAEliminar = new List<list_PFNR_comp_>();
                foreach (var reg in lista)
                {
                    if (await remision_es_PP(_context,reg.codremision))
                    {
                        filasAEliminar.Add(reg);
                    }
                }
                // Ahora eliminar los elementos marcados para eliminación
                foreach (var fila in filasAEliminar)
                {
                    lista.Remove(fila);
                }
            }
            return lista;
        }

        public async Task<double> MontoTotalComplementarias(DBContext _context, List<list_PFNR_comp_> lista)
        {
            double totalc = 0;
            foreach (var reg in lista)
            {
                if (reg.codremision == 0)
                {
                    // nada
                }
                else
                {
                    try
                    {
                        // totalnota = Convert.ToDouble(sia_DAL.Datos.Instancia.EjecutarComandoEscalar("select total from veremision where codigo=" + CStr(lista.Rows(i)("codremision"))))
                        double totalnota = await TotalNRconNC_ND(_context,reg.codremision);
                        totalc = totalc + totalnota;
                    }
                    catch (Exception)
                    {
                        // nada
                    }
                }
            }
            return totalc;
        }
        public async Task<DateTime> FechaComplementariaDeMayorMonto(DBContext _context, List<list_PFNR_comp_> lista)
        {
            double montomayor = 0;
            DateTime fecha = DateTime.Now.Date;
            foreach (var reg in lista)
            {
                if (reg.codremision == 0)
                {
                    // nada
                }
                else
                {
                    double monto = await RemisionMonto(_context, reg.codremision);
                    if (monto >= montomayor)
                    {
                        montomayor = monto;
                        fecha = reg.fecharemision.Date;
                    }
                }
            }
            return fecha;
        }
        public async Task<double> RemisionMonto(DBContext _context, int codigo)
        {
            double resultado = 0;
            try
            {
                resultado = (double)await _context.veremision.Where(i => i.codigo == codigo).Select(i => i.total).FirstOrDefaultAsync();
            }
            catch (Exception)
            {
                resultado = 0;
            }
            return resultado;
        }

        ///////////////////////////////////////////////////////////////////////////////
        /// Esta funcion revierte los pagos realizados de un documento a sus cobranzas//
        /// necesita el tipo de documento y su codigo, borra el plan de pagos         //
        ///////////////////////////////////////////////////////////////////////////////
        public async Task<bool> revertirpagos(DBContext _context, int codigo, int tipo)
        {
            bool resultado = true;
            // primero obtener las cuotas pagadas del documento
            var tabla = await _context.coplancuotas.Where(i => i.coddocumento == codigo && i.codtipodoc == tipo && i.montopagado > 0)
                .Select(i => new
                {
                    codigo
                }).ToListAsync();
            if (tabla.Count() > 0)
            {
                // de cada cuota pagada buscar sus pagos 
                foreach (var reg in tabla)
                {
                    var tablapagos = await _context.copagos.Where(i => i.codcuota == reg.codigo).Select(i => new
                    {
                        i.codigo,
                        i.codcobranza,
                        i.codcuota,
                        i.monto,
                        i.fecha,
                        i.tdc
                    }).ToListAsync();
                    // de cada pago revertir a la cuota y buscar su cobranza y revertir ahi tambien
                    // verificacion apra ver si el documento descarga mercaderia
                    foreach (var reg2 in tablapagos)
                    {
                        //////////////
                        // aumentar el monto en la cobranza
                        if (resultado)
                        {
                            try
                            {
                                var cobranza = await _context.cocobranza.FirstOrDefaultAsync(c => c.codigo == reg2.codcobranza);

                                if (cobranza != null)
                                {
                                    cobranza.montorest = (cobranza.montorest + reg2.monto) / reg2.tdc;
                                    cobranza.estado = 0;

                                    await _context.SaveChangesAsync();
                                }
                            }
                            catch (Exception)
                            {
                                resultado = false;
                            }
                        }
                        if (resultado)
                        {
                            try
                            {
                                var listCocobranza = await _context.cocobranza.Where(i => i.montorest > i.monto).ToListAsync();
                                if (listCocobranza.Count() > 0)
                                {
                                    foreach (var item in listCocobranza)
                                    {
                                        item.montorest = item.monto;
                                    }
                                    await _context.SaveChangesAsync();
                                }
                            }
                            catch (Exception)
                            {
                                resultado = false;
                            }
                        }
                        // borrar el anterior plan de cuotas plan de cuotas
                        if (resultado)
                        {
                            try
                            {
                                var coplancuotas = await _context.coplancuotas.FirstOrDefaultAsync(c => c.codigo == reg2.codcuota);

                                if (coplancuotas != null)
                                {
                                    coplancuotas.montopagado = coplancuotas.montopagado - reg2.monto;

                                    await _context.SaveChangesAsync();
                                }
                            }
                            catch (Exception)
                            {
                                resultado = false;
                            }
                        }
                        if (resultado)
                        {
                            try
                            {
                                var listCoPlanCuotas = await _context.coplancuotas.Where(i => i.montopagado < 0).ToListAsync();
                                if (listCoPlanCuotas.Count() > 0)
                                {
                                    foreach (var item in listCoPlanCuotas)
                                    {
                                        item.montopagado = 0;
                                    }
                                    await _context.SaveChangesAsync();
                                }
                            }
                            catch (Exception)
                            {
                                resultado = false;
                            }
                        }
                        // eliminar pago
                        if (resultado)
                        {
                            try
                            {
                                var copagosDelete = await _context.copagos.Where(i => i.codigo == reg2.codigo).ToListAsync();
                                if (copagosDelete.Count() > 0)
                                {
                                    _context.copagos.RemoveRange(copagosDelete);
                                    await _context.SaveChangesAsync();
                                }
                            }
                            catch (Exception)
                            {
                                resultado = false;
                            }
                        }
                    }
                    // borrar el plan de pagos anterior
                    if (resultado)
                    {
                        try
                        {
                            var coplancuotasDelete = await _context.coplancuotas.Where(i => i.coddocumento == codigo && i.codtipodoc == tipo).ToListAsync();
                            if (coplancuotasDelete.Count() > 0)
                            {
                                _context.coplancuotas.RemoveRange(coplancuotasDelete);
                                await _context.SaveChangesAsync();
                            }
                        }
                        catch (Exception)
                        {
                            resultado = false;
                        }
                    }
                }
            }
            else
            {
                // no habia nada pagado
                // borrar el plan de pagos anterior
                if (resultado)
                {
                    try
                    {
                        var coplancuotasDelete = await _context.coplancuotas.Where(i => i.coddocumento == codigo && i.codtipodoc == tipo).ToListAsync();
                        if (coplancuotasDelete.Count() > 0)
                        {
                            _context.coplancuotas.RemoveRange(coplancuotasDelete);
                            await _context.SaveChangesAsync();
                        }
                    }
                    catch (Exception)
                    {
                        resultado = false;
                    }
                }
            }

            return resultado;
        }


        public async Task<bool> ValidarTarifa(DBContext _context, string codcliente, string coditem, int codtarifa_b)
        {
            bool resultado = true;
            List<int> dt = new List<int>();
            try
            {
                if (await Controla_precios(_context, codcliente))
                {
                    if (await EstaControlado(_context, coditem, codtarifa_b))
                    {
                        dt = await ControlTarifa(_context, coditem, codtarifa_b);
                        foreach (var i in dt)
                        {
                            if (await BuscarCompra(_context, codcliente, coditem, i))
                            {
                                resultado = false;
                                break;
                            }
                        }
                    }
                    else
                    {
                        resultado = true;
                    }
                }
            }
            catch (Exception)
            {
                return false;
            }
            return resultado;
        }

        public async Task<bool> ValidarTarifa_Descuento(DBContext _context, int coddescuento, int codtarifa)
        {
            bool resultado = false;

            try
            {
                //using (_context)
                ////using (var _context = DbContextFactory.Create(userConnectionString))
                //{
                if (coddescuento > 0)
                {
                    var cont = await _context.vedescuento_tarifa
                             .CountAsync(t => t.coddescuento == coddescuento && t.codtarifa == codtarifa);
                    if (cont > 0)
                    {
                        resultado = true;
                    }
                }
                else
                {
                    resultado = true;
                }

                //}
            }
            catch (Exception)
            {
                return false;
            }
            return resultado;
        }

        public async Task<bool> UnidadSoloEnteros(DBContext _context, string codudemed)
        {
            bool resultado = false;

            try
            {
                var entera = await _context.inudemed
                            .Where(v => v.Codigo == codudemed)
                            .Select(v => new { v.entera })
                            .FirstOrDefaultAsync();
                if (entera != null)
                {
                    if (entera.entera.HasValue)
                    {
                        resultado = entera.entera.Value;
                    }
                    else
                    {
                        resultado = false;
                    }
                }
                else
                {
                    resultado = true;
                }
            }
            catch (Exception)
            {
                return true;
            }
            return resultado;
        }

        public async Task<bool> cliente_ventacontado(DBContext _context, string codcliente)
        {
            try
            {
                bool resultado = false;
                //using (_context)
                //{
                int count = _context.vecliente
                    .Where(vc => vc.codigo == codcliente && vc.tipoventa == 2 || vc.tipoventa == 0)
                    .Count();
                resultado = count > 0;
                return resultado;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> cliente_ventacredito(DBContext _context, string codcliente)
        {
            try
            {
                bool resultado = false;
                //using (_context)
                //{
                int count = _context.vecliente
                    .Where(vc => vc.codigo == codcliente && vc.tipoventa == 2 || vc.tipoventa == 1)
                    .Count();
                resultado = count > 0;
                return resultado;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> yahayproforma(DBContext _context, int codproforma)
        {
            try
            {
                var resultado = await _context.veremision.Where(i => i.codproforma == codproforma && i.anulada == false).Select(i => i.codigo).CountAsync();

                if (resultado > 0)
                {
                    return true;
                }
                else { return false; }
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<DataTable> lista_PFNR_complementarias_Table(DBContext _context, int codproforma)
        {
            DataTable lista = new DataTable();
            int codigo = 0;
            DataTable tabla = new DataTable();
            bool bandera = true;
            DataRow registro;
            int i = 0;

            //'##################################'
            //'# estructura de la lista         #
            //'##################################'
            lista.Columns.Add("codproforma", typeof(int));
            lista.Columns.Add("codremision", typeof(int));
            lista.Columns.Add("fecharemision", typeof(DateTime));
            codigo = Convert.ToInt32(codproforma);
            if (codigo > 0)
            {
                //##################################
                //#ir al primer codigo de las proformas
                //##################################
                tabla.Clear();
                tabla.Columns.Clear();
                bandera = true;
                while (bandera)
                {
                    tabla.Clear();
                    //llenar precios permitidos
                    var sql = await _context.veproforma
                                           .Where(pf => pf.codcomplementaria == codigo)
                                           .Select(pf => pf.codigo)
                                           .ToListAsync();
                    var result = sql.Distinct().ToList();
                    tabla = funciones.ToDataTable(result);

                    if (tabla.Rows.Count > 0)
                    {
                        codigo = Convert.ToInt32(tabla.Rows[0]["codigo"]);
                    }
                    else
                    {
                        bandera = false;
                    }
                }

                //##################################
                //#recorrer todos los codigos añadiendo a lista
                //##################################
                tabla.Clear();
                tabla.Columns.Clear();
                bandera = true;
                while (bandera)
                {
                    tabla.Clear();
                    var sql = await _context.veproforma
                                           .Where(pf1 => pf1.codigo == codigo)
                                           .Select(pf1 => new { pf1.codigo, pf1.codcomplementaria })
                                           .ToListAsync();
                    var result = sql.Distinct().ToList();
                    tabla = funciones.ToDataTable(result);

                    // añadir a la lista
                    registro = lista.NewRow();
                    registro["codproforma"] = Convert.ToInt32(tabla.Rows[0]["codigo"]);
                    registro["codremision"] = 0;
                    lista.Rows.Add(registro);
                    // si tiene complementaria copiar codigo, si no terminar el loop
                    if (tabla.Rows.Count > 0)
                    {
                        if (tabla.Rows[0]["codcomplementaria"] == DBNull.Value || Convert.ToInt32(tabla.Rows[0]["codcomplementaria"]) == 0)
                        {
                            bandera = false;
                        }
                        else
                        {
                            codigo = Convert.ToInt32(tabla.Rows[0]["codcomplementaria"]);
                        }
                    }
                }
                tabla.Reset();
                lista.AcceptChanges();

                //##################################
                //#   poner las notas de remision  #
                //##################################
                for (i = 0; i < lista.Rows.Count; i++)
                {
                    int codProforma = (int)lista.Rows[i]["codproforma"];
                    //tabla = sia_DAL.Datos.Instancia.ObtenerDataTable("select codigo,fecha from veremision where anulada=0 and codproforma=" + lista.Rows[i]["codproforma"].ToString());
                    var sql = await _context.veremision
                                          .Where(vr => vr.codproforma == codProforma)
                                          .Select(vr => new { vr.codigo, vr.fecha })
                                          .ToListAsync();
                    var result = sql.Distinct().ToList();
                    tabla = funciones.ToDataTable(result);

                    if (tabla.Rows.Count > 0)
                    {
                        lista.Rows[i]["codremision"] = Convert.ToInt32(tabla.Rows[0]["codigo"]);
                        lista.Rows[i]["fecharemision"] = Convert.ToDateTime(tabla.Rows[0]["fecha"]);
                    }
                    else
                    {
                        lista.Rows[i]["codremision"] = 0;
                        lista.Rows[i]["fecharemision"] = DateTime.Now.Date;
                    }
                }
                lista.AcceptChanges();
                tabla.Dispose();
            }
            return lista;
        }

        public async Task<decimal> Descuentos_Proforma(DBContext _context, int codproforma)
        {
            decimal resultado = 0;
            try
            {
                //using (var _context = DbContextFactory.Create(userConnectionString))
                //{
                var result = await _context.veproforma
                .Where(v => v.codigo == codproforma)
                .Select(parametro => new
                {
                    parametro.descuentos
                })
                .FirstOrDefaultAsync();
                if (result != null)
                {
                    resultado = result.descuentos;
                }

                // }
            }
            catch (Exception)
            {
                return 0;
            }
            return resultado;
        }

        public async Task<bool> Controla_precios(DBContext _context, string codcliente)
        {
            bool resultado = false;

            try
            {
                //using (_context)
                ////using (var _context = DbContextFactory.Create(userConnectionString))
                //{
                var controla_precios = await _context.vecliente
                                .Where(v => v.codigo == codcliente)
                                .Select(v => new { v.codigo, v.controla_precios })
                                .FirstOrDefaultAsync();

                if (controla_precios != null)
                {
                    if (controla_precios.controla_precios.HasValue)
                    {
                        resultado = controla_precios.controla_precios.Value;
                    }
                    else
                    {
                        resultado = false;
                    }
                }
                else
                {
                    resultado = true;
                }
                //}
            }
            catch (Exception)
            {
                return false;
            }
            return resultado;
        }

        public async Task<bool> EstaControlado(DBContext _context, string coditem, int codtarifa_b)
        {
            bool resultado = false;

            try
            {
                //using (_context)
                ////using (var _context = DbContextFactory.Create(userConnectionString))
                //{
                var controla = await _context.initem_controltarifa
                             .CountAsync(t => t.coditem == coditem && t.codtarifa_b == codtarifa_b);
                if (controla > 0)
                {
                    resultado = true;
                }
                else
                {
                    resultado = false;
                }
                //}
            }
            catch (Exception)
            {
                return false;
            }
            return resultado;
        }

        public async Task<List<int>> ControlTarifa(DBContext _context, string coditem, int codtarifa_b)
        {
            List<int> resultado = new List<int>();

            try
            {
                var detalle = await _context.initem_controltarifa
                                .Where(v => v.coditem == coditem && v.codtarifa_b == codtarifa_b)
                                .Select(v => new { v.codtarifa_a })
                                .ToListAsync();
                foreach (var codtarifab in detalle)
                {
                    if (!resultado.Contains(Convert.ToInt32(codtarifab)))
                    {
                        resultado.Add(Convert.ToInt32(codtarifab));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                // Manejar la excepción según tus necesidades.
                throw;
            }
            return resultado;
        }

        public async Task<bool> BuscarCompra(DBContext _context, string codcliente, string coditem, int codtarifa_a)
        {
            bool resultado = false;

            try
            {
                //using (_context)
                ////using (var _context = DbContextFactory.Create(userConnectionString))
                //{
                var cant = await _context.vefactura1
                .Join(_context.vefactura,
                e => e.codfactura,
                t => t.codigo,
                (e, t) => new { E = e, T = t })
                .Where(x => x.E.coditem == coditem && x.E.codtarifa == codtarifa_a && x.T.codcliente == codcliente)
                 .CountAsync();

                if (cant > 0)
                {
                    resultado = true;
                }
                else
                {
                    resultado = false;
                }
                //}
            }
            catch (Exception)
            {
                return false;
            }
            return resultado;
        }


        public async Task<int> Vendedor_de_Cliente_De_Proforma(DBContext _context, string id, int numeroid)
        {
            try
            {
                var resultado = await _context.veproforma
                    .Where(p => p.id == id && p.numeroid == numeroid)
                    .Join(_context.vecliente,
                          p => p.codcliente,
                          c => c.codigo,
                          (p, c) => new { c.codvendedor })
                    .FirstOrDefaultAsync();
                if (resultado != null)
                {
                    return resultado.codvendedor;
                }
                return 0;
            }
            catch (Exception)
            {
                return 0;
            }
        }

        public async Task<bool> RemisionEstaPagadaEnParte(DBContext _context, int codigo)
        {
            try
            {
                var pagado = await _context.coplancuotas
                    .Where(p => p.coddocumento == codigo)
                    .SumAsync(i => i.montopagado) ?? 0;
                if (pagado > 0)
                {
                    return true;
                }
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> Remision_Contado_Contra_Entrega_Esta_Pagada_2(DBContext _context, int codremision)
        {
            try
            {
                var tblpagos = await _context.copagos
                    .Where(p1 => p1.codremision == codremision)
                    .Join(
                        _context.cocobranza,
                        p1 => p1.codcobranza,
                        p2 => p2.codigo,
                        (p1, p2) => new { p1, p2 }
                    )
                    .Where(joined => joined.p2.reciboanulado == false)
                    .Select(joined => new
                    {
                        joined.p2.reciboanulado,
                        joined.p2.id,
                        joined.p2.numeroid,
                        joined.p2.fecha,
                        joined.p2.nit,
                        monto_cbza = joined.p2.monto,
                        monto_dist = joined.p1.monto
                    })
                    .ToListAsync();
                double pagado = 0;
                foreach (var reg in tblpagos)
                {
                    pagado = (double)reg.monto_dist;
                }
                ///////////////////////////////
                // si hay monto pagado
                ///////////////////////////////
                if (pagado > 0)
                {
                    return true;
                }
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////
        // devulve una cadena de facturas de una nota de remision id-nro, id-nro
        ///////////////////////////////////////////////////////////////////////////////
        public async Task<string> facturas_de_una_nr(DBContext _context, int codremision)
        {
            try
            {
                string resultado = "";
                var tabla = await _context.vefactura
                    .Where(p => p.codremision == codremision && p.anulada == false)
                    .Select(i => new
                    {
                        i.id,
                        i.numeroid
                    }).ToListAsync();
                if (tabla.Count() > 0)
                {
                    foreach (var reg in tabla)
                    {
                        resultado = resultado + reg.id + "-" + reg.numeroid + ", ";
                    }
                    resultado = resultado + ".";
                }
                return resultado;
            }
            catch (Exception)
            {
                return "";
            }
        }
        public async Task<bool> remision_es_reversion_pp(DBContext _context, string id, int nroid)
        {
            var tabla = await _context.veremision
                .Where(v => v.id == id && v.numeroid == nroid && v.obs.StartsWith("REVERSION"))
                .Select(v => v.codigo)
                .CountAsync();
            if (tabla > 0)
            {
                return true;
            }
            return false;
        }
        public async Task<int> Vendedor_de_Cliente_De_Remision(DBContext _context, string id, int numeroid)
        {
            int resultado = 0;
            try
            {
                var codVendedor = await _context.veremision
                    .Where(p => p.id == id && p.numeroid == numeroid)
                    .Join(_context.vecliente,
                          p => p.codcliente,
                          c => c.codigo,
                          (p, c) => new { c.codvendedor })
                    .FirstOrDefaultAsync();
                resultado = codVendedor.codvendedor;
            }
            catch (Exception)
            {
                resultado = 0;
            }
            return resultado;
        }

        public async Task<bool> ValidarMostrarDocumento(DBContext _context, TipoDocumento_Ventas tipo, int codigo_documento, string usuario)
        {
            bool resultado = true;
            var tarifas = new List<int>();
            switch (tipo)
            {
                case TipoDocumento_Ventas.Cotizacion:
                    tarifas = await _context.vecotizacion1
                      .Where(vc => vc.codcotizacion == codigo_documento)
                      .Select(vc => vc.codtarifa)
                      .Distinct()
                      .ToListAsync();
                    break;
                case TipoDocumento_Ventas.Proforma:
                    tarifas = await _context.veproforma1
                      .Where(vc => vc.codproforma == codigo_documento)
                      .Select(vc => vc.codtarifa)
                      .Distinct()
                      .ToListAsync();
                    break;
                case TipoDocumento_Ventas.Remision:
                    tarifas = await _context.veremision1
                      .Where(vc => vc.codremision == codigo_documento)
                      .Select(vc => vc.codtarifa)
                      .Distinct()
                      .ToListAsync();
                    break;
                case TipoDocumento_Ventas.Factura:
                    tarifas = await _context.vefactura1
                      .Where(vc => vc.codfactura == codigo_documento)
                      .Select(vc => vc.codtarifa)
                      .Distinct()
                      .ToListAsync();
                    break;
                default:
                    tarifas = null;
                    break;
            }
            if (tarifas != null)
            {
                foreach (var reg in tarifas)
                {
                    if (!await UsuarioTarifa_Permitido(_context,usuario,reg))
                    {
                        resultado = false;
                        break;
                    }
                }
            }
            else
            {
                resultado = false;
            }
            return resultado;
        }

        public async Task<bool> RemisionPonerFechaAnulacion(DBContext _context, int codremision)
        {
            bool resultado = new bool();
            try
            {
                // ver si tiene facturas
                var dt_facturas = await _context.vefactura.Where( i=> i.codremision == codremision).Select(i=> new { i.fecha_anulacion }).ToListAsync();
                if (dt_facturas.Count() > 0)
                {
                    // si tiene  poner la fecha de sus facturas
                    var maxFecha = dt_facturas.Max();
                    // DateTime fechaMax = (maxFecha.fecha_anulacion??DateTime.Now).Date;
                    var remision = _context.veremision.FirstOrDefault(r => r.codigo == codremision);

                    if (remision != null)
                    {
                        remision.fecha_anulacion = maxFecha.fecha_anulacion;
                        await _context.SaveChangesAsync();
                        resultado = true;
                    }
                    else
                    {
                        resultado = false;
                    }

                }
                else
                {
                    // si no tiene poner su misma fecha como fecha_anulacion
                    var remision = _context.veremision.FirstOrDefault(r => r.codigo == codremision);

                    if (remision != null)
                    {
                        remision.fecha_anulacion = remision.fecha;
                        await _context.SaveChangesAsync();
                        resultado = true;
                    }
                    else
                    {
                        resultado = false;
                    }
                }
            }
            catch (Exception)
            {
                resultado = false;
            }
            return resultado;
        }

        public async Task<bool> GenerarPlanDePagosRemision(DBContext _context, int codremision, string codempresa)
        {
            bool resultado = false;
            var dt_remision = await _context.veremision.FirstOrDefaultAsync(i => i.codigo == codremision);
            if (dt_remision != null)
            {
                if (dt_remision.tipopago == 0)
                {
                    // no es a credito
                }
                else
                {
                    await revertirpagos(_context, codremision, 4);
                    if (await ProformaTieneNRAnulada(_context,dt_remision.codproforma ?? 0))
                    {
                        // #########################################

                        // con fecha de ntoa anulada
                        DateTime fecha_antigua = await ProformaFechaMasAntiguaNR(_context, dt_remision.codproforma ?? 0);

                        // #si es PP generar con su monto y su fecha no importan las complementarias ni influye
                        if (await remision_es_PP(_context,codremision))
                        {
                            if (await generarcuotaspago(_context,codremision,4, await TotalNRconNC_ND(_context,codremision),await TotalNRconNC_ND(_context,codremision),dt_remision.codmoneda,dt_remision.codcliente,fecha_antigua,false,codempresa))
                            {
                                resultado = true;
                            }
                        }
                        else
                        {
                            // #si no es PP perocomo es complementaria hacer todo el el chenko
                            if (! await proforma_es_complementaria(_context,dt_remision.codproforma ?? 0))
                            {
                                if (await generarcuotaspago(_context, codremision, 4, await TotalNRconNC_ND(_context, codremision), await TotalNRconNC_ND(_context, codremision), dt_remision.codmoneda, dt_remision.codcliente, fecha_antigua, false, codempresa))
                                {
                                    resultado = true;
                                }
                            }
                            else
                            {
                                var lista = await lista_PFNR_complementarias_noPP(_context, dt_remision.codproforma ?? 0);
                                if (await generarcuotaspago(_context, codremision, 4, await MontoTotalComplementarias(_context, lista), await TotalNRconNC_ND(_context, codremision), dt_remision.codmoneda, dt_remision.codcliente, await FechaComplementariaDeMayorMonto(_context,lista), false, codempresa))
                                {
                                    resultado = true;
                                }

                                // para cada nota complementaria revertir pagos y generar sus cuotas
                                foreach (var reg in lista)
                                {
                                    if (reg.codremision == 0)
                                    {
                                        // nada
                                    }
                                    else
                                    {
                                        if (reg.codremision == codremision)
                                        {
                                            // la actual no hacer pues ya fue hecha
                                        }
                                        else
                                        {
                                            if (await revertirpagos(_context,reg.codremision,4))
                                            {
                                                await generarcuotaspago(_context, reg.codremision, 4, await MontoTotalComplementarias(_context, lista), await TotalNRconNC_ND(_context, reg.codremision), dt_remision.codmoneda, dt_remision.codcliente, await FechaComplementariaDeMayorMonto(_context, lista), false, codempresa);
                                            }
                                        }
                                    }
                                }

                            }
                        }
                    }
                    else
                    {
                        // ######################################### normal
                        // #si es PP generar con su monto y su fecha no importan las complementarias ni influye
                        if (await remision_es_PP(_context,codremision))
                        {
                            if (await generarcuotaspago(_context, codremision, 4, await TotalNRconNC_ND(_context, codremision), await TotalNRconNC_ND(_context, codremision), dt_remision.codmoneda, dt_remision.codcliente, dt_remision.fecha, false, codempresa))
                            {
                                resultado = true;
                            }
                            else
                            {
                                // #si no es PP perocomo es complementaria hacer todo el el chenko
                                if (! await proforma_es_complementaria(_context,dt_remision.codproforma ?? 0))
                                {
                                    if (await generarcuotaspago(_context, codremision, 4, await TotalNRconNC_ND(_context, codremision), await TotalNRconNC_ND(_context, codremision), dt_remision.codmoneda, dt_remision.codcliente, dt_remision.fecha, false, codempresa))
                                    {
                                        resultado = true;
                                    }
                                }
                                else
                                {
                                    var lista = await lista_PFNR_complementarias_noPP(_context, dt_remision.codproforma ?? 0);
                                    if (await generarcuotaspago(_context, codremision, 4, await MontoTotalComplementarias(_context, lista), await TotalNRconNC_ND(_context, codremision), dt_remision.codmoneda, dt_remision.codcliente, await FechaComplementariaDeMayorMonto(_context,lista), false, codempresa))
                                    {
                                        resultado = true;
                                    }
                                    // para cada nota complementaria revertir pagos y generar sus cuotas
                                    foreach (var reg in lista)
                                    {
                                        if (reg.codremision == 0)
                                        {
                                            // nada
                                        }
                                        else
                                        {
                                            if (reg.codremision == codremision)
                                            {
                                                // la actual no hacer pues ya fue hecha
                                            }
                                            else
                                            {
                                                if (await revertirpagos(_context, reg.codremision, 4))
                                                {
                                                    await generarcuotaspago(_context, reg.codremision, 4, await MontoTotalComplementarias(_context, lista), await TotalNRconNC_ND(_context, reg.codremision), dt_remision.codmoneda, dt_remision.codcliente, await FechaComplementariaDeMayorMonto(_context, lista), false, codempresa);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {

                        }
                    } // ###################
                }
            }
            return resultado;
        }


        public async Task<List<veremision_detalle>> DescuentoExtra_CalcularPorItem(DBContext _context, int coddesextra, List<veremision_detalle> detalle, string codcliente, string nit_cliente, double porcen_desc, bool obtener_desc_por_item)
        {
            // dt debe contener coditem y total
            double resultado = 0;
            try
            {
                // estas columnas se usan para realizar el calculo del descto diferenciado por item
                // y en cascada segun la politica de promocion 27 DESCUENTO LEALTAD
                foreach (var reg in detalle)
                {
                    // se obtiene el porccentaje por descuento extra segun el item y coddesextra
                    if (obtener_desc_por_item)
                    {
                        reg.porcentaje = await DescuentoExtra_Porcentaje_Item(_context, coddesextra, reg.coditem);
                    }
                    else
                    {
                        reg.porcentaje = porcen_desc;
                    }

                    // se modifica el porcentaje antes obtenido si corresponde
                    // segun politica gerencial emitida el 26-08-2015 para empresas competidoras
                    // si el nivel de descto es Z o X no se benefician de la promocion
                    if (await cliente.Es_Cliente_Competencia(_context, nit_cliente) == true)
                    {
                        if (await cliente.Cliente_Competencia_Controla_Descto_Nivel(_context,nit_cliente))
                        {
                            if (await Descuento_Extra_Valida_Nivel(_context,coddesextra))
                            {
                                if (reg.niveldesc == "Z" || reg.niveldesc == "z" || reg.niveldesc == "X" || reg.niveldesc == "x")
                                {
                                    reg.porcentaje = 0;
                                }
                            }
                        }
                    }
                    //////////////////////////////////////////////////////////////////////////
                    // aplicar los descuentos al precio individual de cada item
                    //////////////////////////////////////////////////////////////////////////
                    if (reg.precio_con_descto == 0)   // VERIFICAR CON LAS PRUEBAS YA QUE EN SIA PREGUNTA SI ES NULO, CAPAZ SALGA MAL OJOOOOOOOOOOOOOOOOOOO
                    {
                        reg.monto_descto = (double)reg.precioneto * 0.01 * reg.porcentaje;
                        reg.precio_con_descto = (double)reg.precioneto - reg.monto_descto;
                        reg.total_con_descto = (double)reg.cantidad * reg.precio_con_descto;
                    }
                    else
                    {
                        // si ya calculo del descuento una venz entra aqui para continuar con el precio con descuento ya calculado antes
                        // hacer copia del subtotal con descto extra
                        reg.subtotal_descto_extra = reg.total_con_descto;
                        reg.monto_descto = 0;
                        reg.monto_descto = reg.precio_con_descto * 0.01 * reg.porcentaje;
                        double precio_unit_con_descto = reg.precio_con_descto - reg.monto_descto;
                        reg.precio_con_descto = precio_unit_con_descto;
                        reg.total_con_descto = (double)reg.cantidad * reg.precio_con_descto;
                    }

                }

                return detalle;
            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task<int> almacen_de_remision(DBContext _context, int codRemision)
        {
            try
            {
                int resultado = await _context.veremision.Where(i => i.codigo == codRemision).Select(i => i.codalmacen).FirstOrDefaultAsync();
                return resultado;
            }
            catch (Exception)
            {
                return 0;
            }
        }
        public async Task<DateTime> RemisionFecha(DBContext _context, int codRemision)
        {
            try
            {
                DateTime resultado = await _context.veremision.Where(i => i.codigo == codRemision).Select(i => i.fecha).FirstOrDefaultAsync();
                return resultado.Date;
            }
            catch (Exception)
            {
                return DateTime.Now.Date;
            }
        }
        public async Task<int> NR_Tipo_Pago(DBContext _context, int codRemision)
        {
            // tipo_pago=0 CONTADO
            // tipo_pago=1 CREDITO
            try
            {
                int resultado = await _context.veremision.Where(i => i.codigo == codRemision).Select(i => i.tipopago).FirstOrDefaultAsync();
                return resultado;
            }
            catch (Exception)
            {
                return 0;
            }
        }
        public async Task<bool> Es_Venta_Contado_Contra_Entrega(DBContext _context, int codRemision)
        {
            // verifica cual es la proforma de la nota de remision y esta registrada con pago anticipado
            bool resultado = false;
            var dtremision = await _context.veremision.Where(i => i.codigo == codRemision && i.anulada == false).Select(i => new
            {
                i.id,
                i.numeroid,
                i.fecha,
                i.codcliente,
                i.tipopago,
                i.contra_entrega,
                i.estado_contra_entrega
            }).FirstOrDefaultAsync();
            if (dtremision != null)
            {
                if (dtremision.tipopago == 0 && dtremision.contra_entrega == true)
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
            return resultado;

        }

        public async Task<(bool resul, string msg)> Es_Venta_Contado_Con_Pago_Anticipo(DBContext _context, int codRemision)
        {
            bool resultado = false;
            string msg = "";
            var dt = await _context.veproforma
                .Where(v => v.anulada == false &&
                            v.pago_contado_anticipado == true &&
                            _context.veremision
                                .Where(e => e.codigo == codRemision &&
                                            e.anulada == false)
                                .Select(e => e.codproforma)
                                .Contains(v.codigo))
                .Select(v => new
                {
                    v.id,
                    v.numeroid,
                    v.fecha,
                    v.codcliente,
                    v.idanticipo,
                    v.numeroidanticipo,
                    v.monto_anticipo
                })
                .FirstOrDefaultAsync();

            var dtremision = await _context.veremision.Where(i => i.codigo == codRemision && i.anulada == false).Select(i => new
            {
                i.codigo,
                i.id,
                i.numeroid,
                i.fecha,
                i.codcliente,
                i.nomcliente,
                i.total,
                i.codmoneda
            }).FirstOrDefaultAsync();

            if (dt != null)
            {
                // SI es pago con anticipo o tiene la proforma asignada un anticipo
                // a continuacion se verifica si el anticipo existe y esta marcado para_venta_contado
                var dt1 = await _context.coanticipo.Where(i => i.id == dt.idanticipo && i.numeroid == dt.numeroidanticipo
                    && i.anulado == false && i.para_venta_contado == true).Select(i => new
                    {
                        i.id,
                        i.numeroid,
                        i.para_venta_contado,
                        i.anulado
                    }).FirstOrDefaultAsync();
                if (dt1 != null)
                {
                    // **********************************************************************************
                    // valida para la forma como se hacian las proformas contado hasta el 31-03-2016
                    // es decir en la proforma se indicaba el anticipo idanticipo-nroidanticipo monto anticipo
                    // ese decir una un anticipo para una sola proforma
                    // **********************************************************************************
                    resultado = true;
                }
                else
                {
                    // **********************************************************************************
                    // verifica si la proforma tiene distribuciones, verifica el total distribuido
                    // esta modalidad esta vigente desde 01-04-2016
                    // **********************************************************************************
                    double total_pagado_nr = await Anticipos_Vta_Contado.Total_Anticipos_Aplicados_A_Remision(_context, dtremision.codigo, dtremision.codmoneda);
                    total_pagado_nr = Math.Round(total_pagado_nr, 2);
                    if (total_pagado_nr >= (double)dtremision.total)
                    {
                        resultado = true;
                    }
                    else
                    {
                        // si el monto pagado a la proforma es menor se verifica que la diferencia no se mayor a 0.03
                        // lo mismo se valida al sacar la nota de remision
                        if (total_pagado_nr < (double)dtremision.total)
                        {
                            double DIFERENCIA = Math.Abs(total_pagado_nr - (double)dtremision.total);
                            // Desde 12/06/2024 el monto de 0.03 se cambio a 0.5 por instruccion de gereancia y sup nal oper para poder aplicar proformas con montos mayores o menores con una dif de 0.5 max
                            if (DIFERENCIA >= 0.0 && DIFERENCIA <= 0.5)
                            {
                                resultado = true;
                            }
                            else
                            {
                                msg = "El monto total de anticipo asignado no es suficiente para la venta !!!";
                                resultado = false;
                            }
                        }
                        else
                        {
                            resultado = false;
                        }
                    }
                }
            }
            else
            {
                resultado = false;
            }
            return (resultado, msg);

        }

        public async Task<DateTime> cufd_fechalimiteDate(DBContext _context, string cufd)
        {
            try
            {
                /*
                 'fecha = Convert.ToDateTime(sia_DAL.Datos.Instancia.EjecutarComandoEscalar("SELECT fechalimite FROM vedosificacion WHERE cufd='" & cufd & "' "))
                 'Dsd 15/12/2022 se modifico esta consulta para q solo busque segun el cufd y que este activa, ya que en ocasones podremos usar un CUFD para 2 dias debido a los cortes de los servicios
                 ' de impuestos que pueden existir, entonces tiene q buscar el CUFD activo
                */
                DateTime fecha = (await _context.vedosificacion.Where(i => i.cufd == cufd && i.activa == true).Select(i => i.fechalimite).FirstOrDefaultAsync() ?? DateTime.Now);
                return fecha.Date;
            }
            catch (Exception)
            {
                return DateTime.Now.Date;
            }
        }


        ///////////////////////////////////////////////////////////
        // esta funcion devuelve el tipo de factura de una dsificacion CUFD
        //////////////////////////////////////////////////////////
        public async Task<int> cufd_tipofactura(DBContext _context, string cufd)
        {
            // 1: manual
            // 2: registradora
            // 3: computarizada
            try
            {
                int resultado = await _context.vedosificacion.Where(i => i.cufd == cufd).Select(i => i.tipofac).FirstOrDefaultAsync();
                return resultado;
            }
            catch (Exception)
            {
                return 0;
            }
        }
        ///////////////////////////////////////////////////////////////////////////////
        // devulve si es que se debe dar la alerta de que se estan terminando la dosificacon
        ///////////////////////////////////////////////////////////////////////////////
        public async Task<string> alertadosificacion(DBContext _context, int nrocaja)
        {
            var tabla = await _context.vedosificacion.Where(i => i.activa == true && i.nrocaja == nrocaja).Select(i => new
            {
                i.nroactual,
                i.nroalerta
            }).FirstOrDefaultAsync();
            if (tabla != null)
            {
                if (tabla.nroactual == tabla.nroalerta)
                {
                    return "La Dosificacion asignada a esta caja ha alcanzado su nivel de alerta, por favor prepare una neuva dosificacion para reemplazo cuando se acabe la presente.";
                }
            }
            return "";
        }

        ///////////////////////////////////////////////////////////////////////////////
        // devulve el numero de factura que le corresponde a una nueva factura en una caja dada
        ///////////////////////////////////////////////////////////////////////////////
        public async Task<int> caja_numerofactura(DBContext _context, int nrocaja)
        {
            int resultado = 0;
            try
            {
                resultado = await _context.vedosificacion.Where(i => i.activa == true && i.nroactual == nrocaja).Select(i => i.nroactual).FirstOrDefaultAsync();
            }
            catch (Exception)
            {
                resultado = 0;
            }
            return (resultado + 1);
        }
        public static async Task<(string id, int numeroId)> id_nroid_factura_cuf(DBContext _context, int codfactura)
        {
            try
            {
                // 1ro. busca el doc de la factura en base al codfactura
                var tbl = await _context.vefactura.Where(i => i.codigo == codfactura).Select(i => new
                {
                    i.codigo,
                    i.id,
                    i.numeroid
                }).FirstOrDefaultAsync();
                if (tbl != null)
                {
                    return (tbl.id, tbl.numeroid);
                }
                else
                {
                    return ("", 0);
                }
            }
            catch (Exception)
            {
                return ("", 0);
            }
        }

        // IMPLEMENTADO EN FECHA: 18-03-2022
        /// SOLO SE USA DESDE FACTURAR NR, PARA EL NUEVO SISTEMA DE FACTURACION DEL SIAT
        /// esta funcion convierte una factura a un tipo de moneda dado
        public async Task<bool> Convertir_Moneda_Factura_NSF_SIAT(DBContext _context, int codfactura, string codmoneda, DateTime fecha, string codempresa)
        {
            bool resultado = true;
            int CODALMACEN = await empresa.AlmacenLocalEmpresa(_context, codempresa);
            int _codDocSector = await _context.adsiat_parametros_facturacion.Where(i => i.codalmacen == CODALMACEN).Select(i => i.tipo_doc_sector).FirstOrDefaultAsync() ?? -1;
            var tabla = await _context.vefactura.Where(i => i.codigo == codfactura).FirstOrDefaultAsync();
            if (tabla == null)
            {
                return false;
            }
            if (tabla.codmoneda == codmoneda)
            {
                // YA ESTA EN MISMA MONEDA
            }
            else
            {
                double tdc = (double)await tipocambio._tipocambio(_context, codmoneda, tabla.codmoneda, tabla.fecha);
                double tdc2 = (double)await tipocambio._tipocambio(_context, await Empresa.monedabase(_context, codempresa), codmoneda, tabla.fecha);

                // REALIZAR CONVERSION DE LA CABECERA
                tabla.codmoneda = codmoneda;
                tabla.tdc = (decimal)tdc2;
                tabla.subtotal = tabla.subtotal * (decimal)tdc;
                tabla.recargos = tabla.recargos * (decimal)tdc;
                tabla.descuentos = tabla.descuentos * (decimal)tdc;
                tabla.total = tabla.total * (decimal)tdc;
                tabla.iva = tabla.iva * (decimal)tdc;

                int guarda = await _context.SaveChangesAsync(); // Guarda los cambios en la base de datos

                if (guarda > 0)
                {
                    // actualiza el iva recalculando
                    // sia_DAL.Datos.Instancia.EjecutarComando("update vefactura set iva=((subtotal/100)*porceniva) where codigo=" & CStr(codfactura) & " ")

                    // ojo aqui se cambio en lugar que haga: total=subtotal+iva ahora hace: total=total+iva
                    tabla.total = tabla.total + (tabla.iva ?? 0);
                    await _context.SaveChangesAsync(); // Guarda los cambios en la base de datos

                    if (_codDocSector == 1)
                    {
                        // REALIZAR CONVERSION DEL DETALLE
                        // 1 : FACTURA COMPRA VENTA (2 DECIMALES)
                        var detalleFactData = await _context.vefactura1.Where(i => i.codfactura == codfactura).ToListAsync();
                        foreach (var reg in detalleFactData)
                        {
                            reg.precioneto = await siat.Redondeo_Decimales_SIA_2_decimales_SQL(_context, ((double)reg.precioneto * tdc));
                            reg.preciolista = await siat.Redondeo_Decimales_SIA_2_decimales_SQL(_context, ((double)reg.preciolista * tdc));
                            reg.preciodesc = await siat.Redondeo_Decimales_SIA_2_decimales_SQL(_context, ((double)(reg.preciodesc ?? 0) * tdc));
                            reg.total = await siat.Redondeo_Decimales_SIA_2_decimales_SQL(_context, ((double)reg.total * tdc));
                            reg.distdescuento = await siat.Redondeo_Decimales_SIA_2_decimales_SQL(_context, ((double)(reg.distdescuento ?? 0) * tdc));
                            reg.distrecargo = await siat.Redondeo_Decimales_SIA_2_decimales_SQL(_context, ((double)(reg.distrecargo ?? 0) * tdc));
                            reg.preciodist = await siat.Redondeo_Decimales_SIA_2_decimales_SQL(_context, ((double)(reg.preciodist ?? 0) * tdc));
                            reg.totaldist = await siat.Redondeo_Decimales_SIA_2_decimales_SQL(_context, ((double)(reg.totaldist ?? 0) * tdc));
                        }
                        // Finalmente guardamos los cambios en la base de datos
                        var guarda2 = await _context.SaveChangesAsync();
                        if (guarda2 > 0)
                        {
                            resultado = true;
                        }
                        else
                        {
                            resultado = false;
                        }

                        // lo que se convirtio se debe redondear a 2 decimales para luego recalcular todo
                        if (resultado)
                        {
                            // redondear los datos convertidos en la CABECERA
                            tabla.subtotal = await siat.Redondeo_Decimales_SIA_2_decimales_SQL(_context, ((double)tabla.subtotal));
                            tabla.recargos = await siat.Redondeo_Decimales_SIA_2_decimales_SQL(_context, ((double)tabla.recargos));
                            tabla.descuentos = await siat.Redondeo_Decimales_SIA_2_decimales_SQL(_context, ((double)tabla.descuentos));
                            tabla.total = await siat.Redondeo_Decimales_SIA_2_decimales_SQL(_context, ((double)tabla.total));
                            tabla.iva = await siat.Redondeo_Decimales_SIA_2_decimales_SQL(_context, ((double)(tabla.iva ?? 0)));
                            await _context.SaveChangesAsync(); // Guarda los cambios en la base de datos
                        }
                    }
                    else if(_codDocSector == 35)
                    {
                        // REALIZAR CONVERSION DEL DETALLE
                        // 35 : FACTURA COMPRA VENTA BONIFICACIONES(5 DECIMALES) 
                        var detalleFactData = await _context.vefactura1.Where(i => i.codfactura == codfactura).ToListAsync();
                        foreach (var reg in detalleFactData)
                        {
                            reg.precioneto = await siat.Redondeo_Decimales_SIA_5_decimales_SQL(_context, (reg.precioneto * (decimal)tdc));
                            reg.preciolista = await siat.Redondeo_Decimales_SIA_5_decimales_SQL(_context, (reg.preciolista * (decimal)tdc));
                            reg.preciodesc = await siat.Redondeo_Decimales_SIA_5_decimales_SQL(_context, ((reg.preciodesc ?? 0) * (decimal)tdc));
                            reg.total = await siat.Redondeo_Decimales_SIA_5_decimales_SQL(_context, (reg.total * (decimal)tdc));
                            reg.distdescuento = await siat.Redondeo_Decimales_SIA_5_decimales_SQL(_context, ((reg.distdescuento ?? 0) * (decimal)tdc));
                            reg.distrecargo = await siat.Redondeo_Decimales_SIA_5_decimales_SQL(_context, ((reg.distrecargo ?? 0) * (decimal)tdc));
                            reg.preciodist = await siat.Redondeo_Decimales_SIA_5_decimales_SQL(_context, ((reg.preciodist ?? 0) * (decimal)tdc));
                            reg.totaldist = await siat.Redondeo_Decimales_SIA_5_decimales_SQL(_context, ((reg.totaldist ?? 0) * (decimal)tdc));
                        }
                        // Finalmente guardamos los cambios en la base de datos
                        var guarda2 = await _context.SaveChangesAsync();
                        if (guarda2 > 0)
                        {
                            resultado = true;
                        }
                        else
                        {
                            resultado = false;
                        }

                        // lo que se convirtio se debe redondear a 2 decimales para luego recalcular todo
                        if (resultado)
                        {
                            // redondear los datos convertidos en la CABECERA
                            tabla.subtotal = await siat.Redondeo_Decimales_SIA_2_decimales_SQL(_context, ((double)tabla.subtotal));
                            tabla.recargos = await siat.Redondeo_Decimales_SIA_2_decimales_SQL(_context, ((double)tabla.recargos));
                            tabla.descuentos = await siat.Redondeo_Decimales_SIA_2_decimales_SQL(_context, ((double)tabla.descuentos));
                            tabla.total = await siat.Redondeo_Decimales_SIA_2_decimales_SQL(_context, ((double)tabla.total));
                            tabla.iva = await siat.Redondeo_Decimales_SIA_2_decimales_SQL(_context, ((double)(tabla.iva ?? 0)));
                            await _context.SaveChangesAsync(); // Guarda los cambios en la base de datos
                        }
                    }

                    // RECALCULAR LOS DATOS DE LA CABECERA
                    var detalleFactData_2 = await _context.vefactura1.Where(i => i.codfactura == codfactura).ToListAsync();
                    foreach (var reg in detalleFactData_2)
                    {
                        // total
                        reg.total = await siat.Redondeo_Decimales_SIA_5_decimales_SQL(_context, (reg.cantidad * reg.precioneto));
                        // totaldist
                        reg.totaldist = await siat.Redondeo_Decimales_SIA_5_decimales_SQL(_context, (reg.cantidad * (reg.preciodist ?? 0)));
                        // distdescuento
                        reg.distdescuento = await siat.Redondeo_Decimales_SIA_5_decimales_SQL(_context, (reg.total - (reg.totaldist ?? 0)));
                    }
                    var guarda3 = await _context.SaveChangesAsync();
                    if (guarda3 > 0)
                    {
                        resultado = true;
                    }
                    else
                    {
                        resultado = false;
                    }

                    // TOTALIZAR LA CABECERA EN BASE AL DETALLE
                    // subtotal
                    decimal subtotal_tot = await _context.vefactura1.Where(i => i.codfactura == codfactura).SumAsync(i => i.total);
                    tabla.subtotal = subtotal_tot;
                    //descuentos
                    decimal descuentos_tot = await _context.vefactura1.Where(i => i.codfactura == codfactura).SumAsync(i => i.distdescuento) ?? 0;
                    tabla.descuentos = descuentos_tot;
                    //total
                    tabla.total = subtotal_tot - descuentos_tot;

                    var guarda4 = await _context.SaveChangesAsync();
                    if (guarda4 > 0)
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


            }
            return resultado;


        }


        public async Task<decimal> Peso_Factura(DBContext _context, int codfactura)
        {
            decimal resultado = 0;
            try
            {
                resultado = (await _context.vefactura1
                    .Where(d => d.codfactura == codfactura)
                    .Join(
                        _context.initem,
                        d => d.coditem,
                        i => i.codigo,
                        (d, i) => new { d.cantidad, i.peso }
                    )
                    .SumAsync(x => x.cantidad * x.peso) ?? 0);
            }
            catch (Exception)
            {
                resultado = 0;
            }
            return resultado;
        }


        public async Task<bool> Actualizar_Peso_Detalle_Factura(DBContext _context, int codfactura)
        {
            try
            {
                var detalleFact = await _context.vefactura1.Where(i => i.codfactura == codfactura).ToListAsync();
                foreach (var reg in detalleFact)
                {
                    var pesoItem = await _context.initem.Where(i => i.codigo == reg.coditem).Select(i => i.peso).FirstOrDefaultAsync() ?? 0;
                    reg.peso = pesoItem * reg.cantidad;
                }
                await _context.SaveChangesAsync(); // Guarda los cambios en la base de datos
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }






        public async Task<List<ProformasWF>> Detalle_Proformas_Aprobadas_WF(string userConnectionString, string codempresa, string usuario)
        {
            List<ProformasWF> resultado = new List<ProformasWF>();
            try
            {
                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var sql = @"
                        WITH myproforma AS
                        (
                            SELECT aprobada, transferida, codigo codproforma, id AS codpf, numeroid, codcliente, nomcliente, codvendedor,
                                   fecha_inicial, hora_inicial, fecha, fechareg, horareg, fechaaut, horaaut, usuarioreg, total, peso
                            FROM veproforma sf 
                            WHERE sf.fecha >= '20240601' AND sf.id LIKE 'w%'
                        ),
                        Tiempo AS
                        (
                            SELECT p.*, 
                                   (SELECT COUNT(coditem) FROM veproforma1 WHERE codproforma = p.codproforma) AS nroitems,
                                   nr.id AS id_nr, nr.numeroid AS nroid_nr, nr.fechareg AS fechareg_nr, nr.horareg AS horareg_nr,
                                   CASE WHEN p.transferida = 1 THEN (DATEDIFF(day, p.fecha_inicial, nr.fechareg)) * 24 ELSE 0 END AS diasHrs_tomo,
                                   CASE WHEN p.transferida = 1 THEN DATEDIFF(HOUR, p.hora_inicial, nr.horareg) ELSE 0 END AS hrs_tomo,
                                   nr.tipopago, nr.contra_entrega, nr.estado_contra_entrega
                            FROM myproforma p
                            LEFT JOIN veremision nr ON nr.codproforma = p.codproforma
                        )
                        SELECT *, diasHrs_tomo + hrs_tomo AS ttl_hrs FROM Tiempo
                        WHERE transferida = 1
                        ORDER BY codvendedor, codpf, numeroid;
                    ";

                    // Ejecutar la consulta SQL
                    var connection = _context.Database.GetDbConnection();
                    await connection.OpenAsync();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = sql;
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var proforma = new ProformasWF
                                {
                                    Aprobada = reader.GetBoolean(reader.GetOrdinal("aprobada")),
                                    Transferida = reader.GetBoolean(reader.GetOrdinal("transferida")),
                                    Codproforma = reader.GetInt32(reader.GetOrdinal("codproforma")),
                                    Codpf = reader.GetString(reader.GetOrdinal("codpf")),
                                    Numeroid = reader.GetInt32(reader.GetOrdinal("numeroid")),
                                    Codcliente = reader.GetString(reader.GetOrdinal("codcliente")),
                                    Nomcliente = reader.GetString(reader.GetOrdinal("nomcliente")),
                                    Codvendedor = reader.GetInt32(reader.GetOrdinal("codvendedor")),
                                    Fecha_inicial = reader.GetDateTime(reader.GetOrdinal("fecha_inicial")),
                                    Hora_inicial = reader.GetString(reader.GetOrdinal("hora_inicial")),
                                    Fecha = reader.GetDateTime(reader.GetOrdinal("fecha")),
                                    Fechareg = reader.GetDateTime(reader.GetOrdinal("fechareg")),
                                    Horareg = reader.GetString(reader.GetOrdinal("horareg")),
                                    Fechaaut = reader.GetDateTime(reader.GetOrdinal("fechaaut")),
                                    Horaaut = reader.GetString(reader.GetOrdinal("horaaut")),
                                    Usuarioreg = reader.GetString(reader.GetOrdinal("usuarioreg")),
                                    Total = reader.GetDecimal(reader.GetOrdinal("total")),
                                    Peso = reader.GetDecimal(reader.GetOrdinal("peso")),
                                    Nroitems = reader.GetInt32(reader.GetOrdinal("nroitems")),
                                    Id_nr = reader.GetString(reader.GetOrdinal("id_nr")),
                                    Nroid_nr = reader.GetInt32(reader.GetOrdinal("nroid_nr")),
                                    Fechareg_nr = reader.GetDateTime(reader.GetOrdinal("fechareg_nr")),
                                    Horareg_nr = reader.GetString(reader.GetOrdinal("horareg_nr")),
                                    DiasHrs_tomo = reader.GetInt32(reader.GetOrdinal("diasHrs_tomo")),
                                    Hrs_tomo = reader.GetInt32(reader.GetOrdinal("hrs_tomo")),
                                    Tipopago = reader.GetByte(reader.GetOrdinal("tipopago")),
                                    Contra_entrega = reader.GetBoolean(reader.GetOrdinal("contra_entrega")),
                                    Estado_contra_entrega = reader.GetString(reader.GetOrdinal("estado_contra_entrega")),
                                    Ttl_hrs = reader.GetInt32(reader.GetOrdinal("ttl_hrs"))
                                };
                                resultado.Add(proforma);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return resultado;
        }


    }



    public class dtDescxLineaCli
    {
        public string cliente { get; set; }
        public int codgrupo { get; set; }
        public string nivel { get; set; }
    }

    public class dtcbza
    {
        public int codigo { get; set; }
        public string id { get; set; }
        public int numeroid { get; set; }
        public string cliente { get; set; }
        public string nit { get; set; }
    }

    public class dtinfVtaReporte
    {
        public double cantidad { get; set; }
        public double preciolista { get; set; }
        public double preciodesc { get; set; }
        public double total { get; set; }
        public double precioneto { get; set; }
        public int coddescuento { get; set; }
    }

    public class dtinfDescReporte
    {
        public string descorta { get; set; }
        public string desc_descto_completa { get; set; }
        public double montodoc { get; set; }
    }
    public class planPagosCliente
    {
        public int codigo { get; set; }
        public int nrocuotas { get; set; }
    }
    public class list_PFNR_comp_
    {
        public int codproforma { get; set; }
        public int codremision { get; set; }
        public DateTime fecharemision { get; set; }
    }
    public class detalle_tuercas
    {
        public string coditem { get; set; }
        public string descripcion { get; set; }
        public string medida { get; set; }
        public string udm { get; set; }
        public decimal cantidad { get; set; }
    }
    public class veremision_detalle
    {
        public int codremision { get; set; }
        public string coditem { get; set; }
        public decimal cantidad { get; set; }
        public string udm { get; set; }
        public decimal precioneto { get; set; }
        public decimal preciolista { get; set; }
        public string niveldesc { get; set; }
        public decimal preciodesc { get; set; }
        public int codtarifa { get; set; }
        public short coddescuento { get; set; }
        public decimal total { get; set; }
        public decimal porceniva { get; set; }
        public int codgrupomer { get; set; }
        public decimal peso { get; set; }
        public int codigo { get; set; }

        public double distdescuento { get; set; }
        public double distrecargo { get; set; }
        public double preciodist { get; set; }
        public double totaldist { get; set; }

        public double total_con_descto { get; set; } = 0;
        public double subtotal_descto_extra { get; set; } = 0;
        public double precio_con_descto { get; set; } = 0;

        public double porcentaje { get; set; } = 0;
        public double monto_descto { get; set; } = 0;
    }
    public enum TipoDocumento_Ventas
    {
        Cotizacion,
        Proforma,
        Remision,
        Factura
    }
}
