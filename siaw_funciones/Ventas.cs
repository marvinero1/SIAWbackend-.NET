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


        public async Task<(bool, string)> AdicionarDescuentoPorDeposito(DBContext _context, double subtotal_proforma, string codmoneda_prof, List<tabladescuentos> tabladescuentos, List<dtdepositos_pendientes> dt_depositos_pendientes, List<tblcbza_deposito> tblcbza_deposito, int codproforma, string codcliente, string codempresa)
        {
            // codproforma debe ser 0 si no hay proforma.

            bool resultado = false;

            //verifica si el descuento por desposito esta habilitado
            if (!await configuracion.emp_hab_descto_x_deposito(_context, codempresa))
            {
                return (false,"");
            }

            int coddesextra = await configuracion.emp_coddesextra_x_deposito(_context, codempresa);
            if (coddesextra <= 0)
            {
                return (false, "La aplicacion del descuento por deposito esta habilitado, sin embargo no se ha definido cual es el descuento por deposito.");
            }

            // obtener el porcentaje limite del subtotal de la proforma(hasta el cual se puede aplicar descuento por deposito)
            // Dim porcen_limite_descto As Double = sia_funciones.Ventas.Instancia.Porcentaje_Limite_Descuento_Deposito(subtotal_proforma)
            // desde 28-09-2021 (se configura por cliente el descto maximo de deposito)
            double porcen_limite_descto = await Porcentaje_Limite_Descuento_Deposito_Cliente(_context, codcliente);
            if (porcen_limite_descto == 0)
            {
                return (false, "El porcentaje limite del subtotal de la proforma definido hasta el cual se puede aplicar el descuento por deposito es cero, por tanto no se puede aplicar el descuento por deposito, verifique esta situacion en el perfil de datos del cliente.");
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
                            reg.montodoc = (decimal)await siat.Redondeo_Decimales_SIA_2_decimales_SQL(_context,(float)await tipocambio._conversion(_context, codmoneda_prof, reg.codmoneda, fecha, reg.montodoc));
                            reg.total_dist = (double)await siat.Redondeo_Decimales_SIA_2_decimales_SQL(_context, (float)await tipocambio._conversion(_context, codmoneda_prof, reg.codmoneda, fecha, (decimal)reg.total_dist));
                            reg.total_desc = (double)await siat.Redondeo_Decimales_SIA_2_decimales_SQL(_context, (float)await tipocambio._conversion(_context, codmoneda_prof, reg.codmoneda, fecha, (decimal)reg.total_desc));
                            reg.codmoneda = "BS";
                        }
                        // si la moneda de la pf esta en US y el descuento en BS, entonces convertir el descuento en US
                        if ("US" == codmoneda_prof && "BS" == reg.codmoneda)
                        {
                            reg.montodoc = (decimal)await siat.Redondeo_Decimales_SIA_2_decimales_SQL(_context, (float)await tipocambio._conversion(_context, codmoneda_prof, reg.codmoneda, fecha, reg.montodoc));
                            reg.total_dist = (double)await siat.Redondeo_Decimales_SIA_2_decimales_SQL(_context, (float)await tipocambio._conversion(_context, codmoneda_prof, reg.codmoneda, fecha, (decimal)reg.total_dist));
                            reg.total_desc = (double)await siat.Redondeo_Decimales_SIA_2_decimales_SQL(_context, (float)await tipocambio._conversion(_context, codmoneda_prof, reg.codmoneda, fecha, (decimal)reg.total_desc));
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


            return (resultado, "");
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
                else if ((empaque2 > 0))
                {
                    mod_final = (cantidad % empaque2);
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
                else if ((empaque1 > 0))
                {
                    mod_final = (cantidad % empaque1);
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
                var parametro = await _context.adparametros.FirstOrDefaultAsync();
                if (parametro != null)
                {
                    resultado = (int)parametro.longitud_maxima_nit_facturacion;
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
                var parametro = await _context.adparametros.FirstOrDefaultAsync();
                if (parametro != null)
                {
                    resultado = (int)parametro.longitud_minima_nit_facturacion;
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
