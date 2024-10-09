using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NuGet.Configuration;
//using SIAW.Data;
//using SIAW.Models;
//using SIAW.Models_Extra;
using siaw_DBContext.Data;
using siaw_DBContext.Models;
using siaw_DBContext.Models_Extra;
using System.Data;
using System.Drawing;
using System.Security.Policy;
using System.Text;
using System.Web.Http.Results;
using siaw_funciones;
using LibSIAVB;
using static siaw_funciones.Validar_Vta;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.CodeAnalysis.Differencing;
using static System.Net.Mime.MediaTypeNames;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Humanizer;
using System.Net;
using System.Drawing.Drawing2D;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Linq;
using System.Security.Cryptography;
using System.Xml;
using NuGet.Packaging;
using System.Xml.Linq;
using Humanizer;
using System.Globalization;
using ICSharpCode.SharpZipLib.Core;
using Polly;
using siaw_ws_siat;

namespace SIAW.Controllers.ventas.transaccion
{
    [Route("api/venta/transac/[controller]")]
    [ApiController]
    public class veproformaController : ControllerBase
    {

        private readonly UserConnectionManager _userConnectionManager;
        private readonly siaw_funciones.empaquesFunciones empaque_func = new siaw_funciones.empaquesFunciones();
        private readonly siaw_funciones.datosProforma datos_proforma = new siaw_funciones.datosProforma();
        private readonly siaw_funciones.ClienteCasual clienteCasual = new siaw_funciones.ClienteCasual();
        private readonly siaw_funciones.Cliente cliente = new siaw_funciones.Cliente();
        private readonly siaw_funciones.Empresa empresa = new siaw_funciones.Empresa();
        private readonly siaw_funciones.Saldos saldos = new siaw_funciones.Saldos();
        private readonly siaw_funciones.TipoCambio tipocambio = new siaw_funciones.TipoCambio();
        private readonly siaw_funciones.Ventas ventas = new siaw_funciones.Ventas();
        //private readonly siaw_funciones.IVentas ventas;
        private readonly siaw_funciones.Items items = new siaw_funciones.Items();
        private readonly siaw_funciones.Validar_Vta validar_Vta = new siaw_funciones.Validar_Vta();
        private readonly siaw_funciones.Almacen almacen = new siaw_funciones.Almacen();
        private readonly siaw_funciones.SIAT siat = new siaw_funciones.SIAT();
        private readonly siaw_funciones.Configuracion configuracion = new siaw_funciones.Configuracion();
        private readonly siaw_funciones.Creditos creditos = new siaw_funciones.Creditos();
        private readonly siaw_funciones.Cobranzas cobranzas = new siaw_funciones.Cobranzas();
        private readonly siaw_funciones.Nombres nombres = new siaw_funciones.Nombres();
        private readonly siaw_funciones.Seguridad seguridad = new siaw_funciones.Seguridad();

        private readonly siaw_funciones.Funciones funciones = new Funciones();
        private readonly siaw_funciones.ziputil ziputil = new ziputil();
        private readonly func_encriptado encripVB = new func_encriptado();
        private readonly Anticipos_Vta_Contado anticipos_vta_contado = new Anticipos_Vta_Contado();
        private readonly Log log = new Log();
        private readonly Depositos_Cliente depositos_cliente = new Depositos_Cliente();
        private readonly string _controllerName = "veproformaController";


        private readonly Funciones_SIAT funciones_SIAT = new Funciones_SIAT();
        private readonly ServFacturas serv_Facturas = new ServFacturas();

        public veproformaController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }


        // GET: api/adsiat_tipodocidentidad
        [HttpGet]
        [Route("getTipoDocIdent/{userConn}")]
        public async Task<ActionResult<IEnumerable<adsiat_tipodocidentidad>>> Getaadsiat_tipodocidentidad(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.adsiat_tipodocidentidad == null)
                    {
                        return BadRequest(new { resp = "Entidad adtipocambio es null." });
                    }
                    var result = await _context.adsiat_tipodocidentidad
                        .OrderBy(t => t.codigoclasificador)
                        .Select(t => new
                        {
                            t.codigoclasificador,
                            t.descripcion
                        })
                        .ToListAsync();
                    return Ok(result);
                }
            }
            catch (Exception ex)
            {
                return Problem("Error en el servidor al obtener tipo Doc Identidad: " + ex.Message);
                throw;
            }
        }





        /// <summary>
        /// Obtiene saldos de un item de una agencia por VPN
        /// </summary>
        /// <param name="userConn"></param>
        /// <param name="agencia"></param>
        /// <param name="codalmacen"></param>
        /// <param name="coditem"></param>
        /// <returns></returns>
        // GET: api/ad_conexion_vpn/5
        [HttpGet]
        [Route("getsladoVpn/{userConn}/{agencia}/{codalmacen}/{coditem}")]
        public async Task<ActionResult> Getsaldos_vpn(string userConn, string agencia, int codalmacen, string coditem)
        {
            try
            {
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);
                var ad_conexion_vpnResult = empaque_func.Getad_conexion_vpnFromDatabase(userConnectionString, agencia);
                if (ad_conexion_vpnResult == null)
                {
                    return BadRequest(new { resp = "No se pudo obtener la cadena de conexión" });
                }

                var instoactual = await empaque_func.GetSaldosActual(ad_conexion_vpnResult, codalmacen, coditem);
                if (instoactual == null)
                {
                    return NotFound(new { resp = "No se encontraron registros con los datos proporcionados." });
                }
                return Ok(instoactual);
                //return Ok(ad_conexion_vpnResult);
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }



        /// <summary>
        /// Obtiene saldos de un item de una agencia de manera local
        /// </summary>
        /// <param name="userConn"></param>
        /// <param name="codalmacen"></param>
        /// <param name="coditem"></param>
        /// <returns></returns>
        // GET: api/ad_conexion_vpn/5
        [HttpGet]
        [Route("getsladoLocal/{userConn}/{codalmacen}/{coditem}")]
        public async Task<ActionResult<instoactual>> Getsaldos_local(string userConn, int codalmacen, string coditem)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                var instoactual = await empaque_func.GetSaldosActual(userConnectionString, codalmacen, coditem);
                if (instoactual == null)
                {
                    return NotFound(new { resp = "No se encontraron registros con los datos pr<porcionados." });
                }
                return Ok(instoactual);
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }







        /// <summary>
        /// Obtiene saldos de manera completa
        /// </summary>
        /// <param name="userConn"></param>
        /// <returns></returns>
        // GET: api/ad_conexion_vpn/5
        [HttpPost]
        [Route("getsaldoDetalleSP/{userConn}")]
        public async Task<ActionResult<List<sldosItemCompleto>>> getsaldoDetalleSP (string userConn, RequestDataSaldosSP RequestDataSaldosSP)
        {
            string agencia = RequestDataSaldosSP.agencia;
            int codalmacen = RequestDataSaldosSP.codalmacen;
            string coditem = RequestDataSaldosSP.coditem;
            string codempresa = RequestDataSaldosSP.codempresa;
            string usuario = RequestDataSaldosSP.usuario;
            string idProforma = RequestDataSaldosSP.idProforma;
            int nroIdProforma = RequestDataSaldosSP.nroIdProforma;

            try
            {
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                // Falta validacion para saber si traera datos de manera local o por vpn
                // Obtener el contexto de base de datos correspondiente a la empresa
                string titulo = "";
                bool usar_bd_opcional = await saldos.Obtener_Saldos_Otras_Agencias_Localmente(userConnectionString, codempresa);
                if (!usar_bd_opcional)
                {
                    userConnectionString = empaque_func.Getad_conexion_vpnFromDatabase(userConnectionString, agencia);
                    titulo = "Los Saldos (Para Ventas) del Item Se obtienen por medio de VPN";
                    if (userConnectionString == null)
                    {
                        return BadRequest(new { resp = "No se pudo obtener la cadena de conexión" });
                    }
                }
                else
                {
                    titulo = "Los Saldos (Para Ventas) del Item Se obtienen Localmente";
                }


                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    bool obtener_saldos_otras_ags_localmente = await saldos.Obtener_Saldos_Otras_Agencias_Localmente_context(_context, codempresa); // si se obtener las cantidades reservadas de las proformas o no
                    bool obtener_cantidades_aprobadas_de_proformas = await saldos.Obtener_Cantidades_Aprobadas_De_Proformas(_context, codempresa); // si se obtener las cantidades reservadas de las proformas o no
                    var esTienda = await almacen.Es_Tienda(_context, codalmacen);
                    bool ctrlSeguridad = true;
                    bool include_saldos_a_cubrir = true;
                    int AlmacenLocalEmpresa = await empresa.AlmacenLocalEmpresa_context(_context, codempresa);

                    var resultados = await saldos.SaldoItem_Crtlstock_Tabla_Para_Ventas_Sam(_context, coditem, codalmacen, esTienda, ctrlSeguridad, idProforma, nroIdProforma, include_saldos_a_cubrir, codempresa, usuario, obtener_saldos_otras_ags_localmente, obtener_cantidades_aprobadas_de_proformas, AlmacenLocalEmpresa);
                    var detalleSaldos = resultados.detalleSaldos;
                    if (detalleSaldos == null)
                    {
                        return BadRequest(new { resp = "No se pudo obtener los saldos del item seleccionado" });
                    }
                    decimal cantidad_ag_local_incluye_cubrir = detalleSaldos.Sum(i => i.cantidad_ag_local_incluye_cubrir);
                    cantidad_ag_local_incluye_cubrir = cantidad_ag_local_incluye_cubrir < 0 ? 0 : cantidad_ag_local_incluye_cubrir;
                    return Ok(new
                    {
                        titulo,
                        detalleSaldo = resultados.detalleSaldos,
                        saldoVariable = resultados.detalleSaldosVariables,
                        totalSaldo = cantidad_ag_local_incluye_cubrir
                    });
                }

            }
            catch (Exception ex)
            {
                return Problem("Error en el servidor al obtener empaques: " + ex.Message);
                throw;
            }
        }




        /// <summary>
        /// Obtiene saldos de manera completa
        /// </summary>
        /// <param name="userConn"></param>
        /// <param name="codalmacen"></param>
        /// <param name="coditem"></param>
        /// <returns></returns>
        // GET: api/ad_conexion_vpn/5
        [HttpGet]
        [Route("getsaldosCompleto/{userConn}/{agencia}/{codalmacen}/{coditem}/{codempresa}/{usuario}")]
        public async Task<ActionResult<List<sldosItemCompleto>>> getsaldosCompleto(string userConn, string agencia, int codalmacen, string coditem, string codempresa, string usuario)
        {
            try
            {
                List<sldosItemCompleto> listaSaldos = new List<sldosItemCompleto>();
                sldosItemCompleto saldoItemTotal = new sldosItemCompleto();
                sldosItemCompleto var8 = new sldosItemCompleto();
                saldoItemTotal.descripcion = "Total Saldo";
                List<object> resultados = new List<object>();


                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);
                var conexion = userConnectionString;
                bool eskit = await empaque_func.GetEsKit(conexion, coditem);  // verifica si el item es kit o no
                bool obtener_cantidades_aprobadas_de_proformas = await empaque_func.IfGetCantidadAprobadasProformas(userConnectionString, codempresa); // si se obtender las cantidades reservadas de las proformas o no



                // Falta validacion para saber si traera datos de manera local o por vpn
                // Obtener el contexto de base de datos correspondiente a la empresa
                bool usar_bd_opcional = await saldos.Obtener_Saldos_Otras_Agencias_Localmente(userConnectionString, codempresa);
                if (!usar_bd_opcional)
                {
                    conexion = empaque_func.Getad_conexion_vpnFromDatabase(userConnectionString, agencia);
                    resultados.Add(new { resp = "Los Saldos (Para Ventas) del Item Se obtienen por medio de VPN" });
                    if (conexion == null)
                    {
                        return BadRequest(new { resp = "No se pudo obtener la cadena de conexión" });
                    }
                }
                else
                {
                    resultados.Add(new { resp = "Los Saldos (Para Ventas) del Item Se obtienen Localmente" });
                }



                // obtiene saldos de agencia del item seleccionado
                instoactual instoactual = await getEmpaquesItemSelect(conexion, coditem, codalmacen, eskit);

                if (eskit)
                {
                    //saldosDetalleItem.txtReservaProf = "(-) PROFORMAS APROBADAS ITEM(" + instoactual.coditem + ") DEL CJTO(" + coditem + ")";
                    sldosItemCompleto saldosDetalleItem = new sldosItemCompleto();
                    saldosDetalleItem.descripcion = "(+) SALDO ACTUAL ITEM (" + instoactual.coditem + ") DEL CJTO(" + coditem + ")";
                    saldosDetalleItem.valor = (double)instoactual.cantidad;
                    listaSaldos.Add(saldosDetalleItem);
                }
                else
                {
                    //saldosDetalleItem.txtReservaProf = "(-) PROFORMAS APROBADAS: " + instoactual.coditem;
                    sldosItemCompleto saldosDetalleItem = new sldosItemCompleto();
                    saldosDetalleItem.descripcion = "(+)SALDO ACTUAL ITEM: " + instoactual.coditem;
                    saldosDetalleItem.valor = (double)instoactual.cantidad;
                    listaSaldos.Add(saldosDetalleItem);
                }
                saldoItemTotal.valor = (double)instoactual.cantidad;


                // obtiene reservas en proforma
                List<saldosObj> saldosReservProformas = await getReservasProf(conexion, coditem, codalmacen, obtener_cantidades_aprobadas_de_proformas, eskit);


                string codigoBuscado = instoactual.coditem;

                var reservaProf = saldosReservProformas.FirstOrDefault(obj => obj.coditem == codigoBuscado);

                instoactual.coditem = coditem;
                if (eskit)
                {
                    sldosItemCompleto saldosDetalleItem = new sldosItemCompleto();
                    saldosDetalleItem.descripcion = "(-) PROFORMAS APROBADAS ITEM(" + instoactual.coditem + ") DEL CJTO(" + coditem + ")";
                    saldosDetalleItem.valor = (double)reservaProf.TotalP * -1;
                    listaSaldos.Add(saldosDetalleItem);
                }
                else
                {
                    sldosItemCompleto saldosDetalleItem = new sldosItemCompleto();
                    saldosDetalleItem.descripcion = "(-) PROFORMAS APROBADAS: " + instoactual.coditem;
                    saldosDetalleItem.valor = (double)reservaProf.TotalP * -1;
                    listaSaldos.Add(saldosDetalleItem);
                }

                saldoItemTotal.valor -= (double)reservaProf.TotalP;  // reduce saldo total
                var8.valor = saldoItemTotal.valor;






                // pivote variable para agregar a la lista
                sldosItemCompleto var1 = new sldosItemCompleto();
                double CANTIDAD_RESERVADA = 0;
                /*
                if (eskit)
                {
                    //CANTIDAD_RESERVADA = await getReservasCjtos(userConnectionString, coditem, codalmacen, codempresa, eskit, (double)instoactual.cantidad, (double)reservaProf.TotalP);
                    CANTIDAD_RESERVADA = await getReservasCjtos(userConnectionString, coditem, codalmacen, codempresa, eskit, (double)saldoItemTotal.valor, (double)reservaProf.TotalP);

                }
                else
                {
                    CANTIDAD_RESERVADA = await getReservasCjtos(userConnectionString, coditem, codalmacen, codempresa, eskit, (double)saldoItemTotal.valor, (double)reservaProf.TotalP);
                    //CANTIDAD_RESERVADA = await getReservasCjtos(userConnectionString, coditem, codalmacen, codempresa, eskit, (double)instoactual.cantidad, (double)reservaProf.TotalP);
                }
                */
                // obtiene items si no son kit, sus reservas para armar conjuntos.
                // double CANTIDAD_RESERVADA = await getReservasCjtos(userConnectionString, coditem, codalmacen, codempresa, eskit, (double)instoactual.cantidad, (double)reservaProf.TotalP);
                // CANTIDAD_RESERVADA = await getReservasCjtos(userConnectionString, coditem, codalmacen, codempresa, eskit, (double)saldoItemTotal.valor, (double)reservaProf.TotalP);
                CANTIDAD_RESERVADA = await getReservasCjtos(userConnectionString, codigoBuscado, codalmacen, codempresa, eskit, (double)saldoItemTotal.valor, (double)reservaProf.TotalP);
                if (CANTIDAD_RESERVADA < 0)
                {
                    CANTIDAD_RESERVADA = 0;
                }

                var1.descripcion = "(-) SALDO RESERVADO PARA ARMAR CJTOS";
                var1.valor = CANTIDAD_RESERVADA * -1;
                listaSaldos.Add(var1);
                saldoItemTotal.valor -= CANTIDAD_RESERVADA;  // reduce saldo total

                // obtiene el saldo minimo que debe mantenerse en agencia
                sldosItemCompleto var2 = new sldosItemCompleto();
                // double Saldo_Minimo_Item = await empaque_func.getSaldoMinimo(userConnectionString, coditem);
                double Saldo_Minimo_Item = await empaque_func.getSaldoMinimo(userConnectionString, codigoBuscado);

                var2.descripcion = "(-) SALDO MINIMO DEL ITEM";
                var2.valor = Saldo_Minimo_Item * -1;
                listaSaldos.Add(var2);
                saldoItemTotal.valor -= Saldo_Minimo_Item;  // reduce saldo total

                // obtiene reserva NM ingreso para sol-Urgente
                bool validar_ingresos_solurgentes = await empaque_func.getValidaIngreSolurgente(userConnectionString, codempresa);

                double total_reservado = 0;
                double total_para_esta = 0;
                double total_proforma = 0;
                if (validar_ingresos_solurgentes)
                {
                    //  RESTAR LAS CANTIDADES DE INGRESO POR NOTAS DE MOVIMIENTO URGENTES
                    // de facturas que aun no estan aprobadas
                    // string resp_total_reservado = await getSldIngresoReservNotaUrgent(userConnectionString, coditem, codalmacen);
                    total_reservado = await getSldIngresoReservNotaUrgent(userConnectionString, codigoBuscado, codalmacen);
                    // total_reservado = double.Parse(resp_total_reservado);


                    //AUMENTAR CANTIDAD PARA ESTA PROFORMA DE INGRESO POR NOTAS DE MOVIMIENTO URGENTES
                    // total_para_esta = await getSldReservNotaUrgentUnaProf(userConnectionString, coditem, codalmacen, "''", 0);
                    total_para_esta = await getSldReservNotaUrgentUnaProf(userConnectionString, codigoBuscado, codalmacen, "''", 0);


                    //AUMENTAR LA CANTIDAD DE LA PROFORMA DE ESTA NOTA QUE PUEDE ESTAR COMO RESERVADA.
                    // total_proforma = await getSldReservProf(userConnectionString, coditem, codalmacen, "''", 0);
                    total_proforma = await getSldReservProf(userConnectionString, codigoBuscado, codalmacen, "''", 0);

                }

                sldosItemCompleto var3 = new sldosItemCompleto();
                var3.descripcion = "(-) RESERVA NM INGRESO PARA SOL-URGENTE";
                var3.valor = total_reservado * -1;
                listaSaldos.Add(var3);
                saldoItemTotal.valor -= total_reservado;  // reduce saldo total

                sldosItemCompleto var4 = new sldosItemCompleto();
                var4.descripcion = "(+) INGRESOS SOLICITUDES URGENTE DE PROFORMA : -0";
                var4.valor = total_para_esta;
                listaSaldos.Add(var4);
                saldoItemTotal.valor += total_para_esta;  // reduce saldo total

                sldosItemCompleto var5 = new sldosItemCompleto();
                var5.descripcion = "(+) CANTIDAD RESERVADA PROFORMA APROBADA: -0";
                var5.valor = total_proforma;
                listaSaldos.Add(var5);
                saldoItemTotal.valor += total_proforma;  // reduce saldo total


                listaSaldos.Add(saldoItemTotal);

                // devolver resultados finales
                /*return Ok(new
                {
                    saldoActual = instoactual,
                    reservaProf = reservaProf,
                    reservaCjtos = CANTIDAD_RESERVADA,
                    sldMinItem = Saldo_Minimo_Item,
                    reservaNMIngreso = total_reservado,
                    ingresoSolProfUrg = total_para_esta,
                    cantResProfAprob = total_proforma

                });*/


                resultados.Add(listaSaldos);
                // PARA LA SEGUNDA PESTAÑA (SALDO VARIABLE)
                // Verifica si el usuario no tiene acceso devuelve la lista.
                bool ver_detalle_saldo_variable = await empaque_func.ve_detalle_saldo_variable(userConnectionString, usuario);
                if (!ver_detalle_saldo_variable)
                {
                    return Ok(resultados);
                }

                List<sldosItemCompleto> saldoItemTotalPestaña2 = new List<sldosItemCompleto>();
                // si el usuario tiene acceso se llenan los datos de la segunda tabla:
                inreserva_area inreserva_Area = await empaque_func.get_inreserva_area(userConnectionString, coditem, codalmacen);
                if (inreserva_Area == null)
                {
                    return Ok(resultados);
                }
                // promedio de venta
                sldosItemCompleto var6 = new sldosItemCompleto();
                var6.descripcion = "Promedio de Vta.";
                var6.valor = (double)inreserva_Area.promvta;
                saldoItemTotalPestaña2.Add(var6);

                // stock minimo
                sldosItemCompleto var7 = new sldosItemCompleto();
                var7.descripcion = "Stock Mínimo.";
                var7.valor = (double)inreserva_Area.smin;
                saldoItemTotalPestaña2.Add(var7);

                // saldo actual (Saldo Seg Kardex - Cant Prof Ap)
                var8.descripcion = "Saldo Actual (Saldo Seg Kardex - Cant Prof Ap)";
                saldoItemTotalPestaña2.Add(var8);

                // % Vta Permitido: 0.53% pero solo se toma: 50%
                sldosItemCompleto var9 = new sldosItemCompleto();
                var9.descripcion = "% Vta Permitido: 0.53% pero solo se toma: 50%";
                var9.valor = (double)inreserva_Area.porcenvta;
                saldoItemTotalPestaña2.Add(var9);

                // Reserva para Vta en Cjto
                sldosItemCompleto var10 = new sldosItemCompleto();
                var10.descripcion = "Reserva para Vta en Cjto";
                var10.valor = CANTIDAD_RESERVADA;
                saldoItemTotalPestaña2.Add(var10);

                // Saldo para Vta sueltos
                sldosItemCompleto var11 = new sldosItemCompleto();
                var11.descripcion = "Saldo para Vta sueltos";
                var11.valor = saldoItemTotal.valor;
                saldoItemTotalPestaña2.Add(var11);


                resultados.Add(saldoItemTotalPestaña2);
                return Ok(resultados);

            }
            catch (Exception ex)
            {
                return Problem("Error en el servidor al obtener los saldos detallados: " + ex.Message);
                throw;
            }
        }


        private async Task<double> getSldReservProf(string userConnectionString, string coditem, int codalmacen, string idProf, int nroIdProf)
        {
            // verifica si es almacen o tienda
            double respuestaValor = 0;

            bool esAlmacen = await empaque_func.esAlmacen(userConnectionString, codalmacen);

            if (esAlmacen)
            {
                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var respuesta = new SqlParameter("@respuesta", SqlDbType.Decimal)
                    {
                        Direction = ParameterDirection.Output,
                        Precision = 18,
                        Scale = 2
                    };

                    await _context.Database.ExecuteSqlRawAsync(
                        "EXEC SP001_Cantidad_Reservada_En_Proforma_Almacenes @coditem, @codalmacen, @id_prof, @nroid_prof, @respuesta OUTPUT",
                        new SqlParameter("@coditem", SqlDbType.NVarChar) { Value = coditem },
                        new SqlParameter("@codalmacen", SqlDbType.Int) { Value = codalmacen },
                        new SqlParameter("@id_prof", SqlDbType.NVarChar) { Value = idProf }, // Reemplaza idProf con el valor correcto
                        new SqlParameter("@nroid_prof", SqlDbType.Int) { Value = nroIdProf }, // Reemplaza nroIdProf con el valor correcto
                        respuesta
                    );

                    respuestaValor = Convert.ToSingle(respuesta.Value);
                }
            }
            else
            {
                using (var _context = DbContextFactory.Create(userConnectionString))
                {

                    var respuesta = new SqlParameter("@respuesta", SqlDbType.Decimal)
                    {
                        Direction = ParameterDirection.Output,
                        Precision = 18,
                        Scale = 2
                    };

                    await _context.Database.ExecuteSqlRawAsync(
                        "EXEC SP001_Cantidad_Reservada_En_Proforma_Tiendas @coditem, @codalmacen, @id_prof, @nroid_prof, @respuesta OUTPUT",
                        new SqlParameter("@coditem", SqlDbType.NVarChar) { Value = coditem },
                        new SqlParameter("@codalmacen", SqlDbType.Int) { Value = codalmacen },
                        new SqlParameter("@id_prof", SqlDbType.NVarChar) { Value = idProf }, // Reemplaza idProf con el valor correcto
                        new SqlParameter("@nroid_prof", SqlDbType.Int) { Value = nroIdProf }, // Reemplaza nroIdProf con el valor correcto
                        respuesta
                    );

                    respuestaValor = Convert.ToSingle(respuesta.Value);
                }
            }
            return respuestaValor;
        }





        private async Task<double> getSldIngresoReservNotaUrgent(string userConnectionString, string coditem, int codalmacen)
        {
            // verifica si es almacen o tienda
            double respuestaValor = 0;

            bool esAlmacen = await empaque_func.esAlmacen(userConnectionString, codalmacen);

            if (esAlmacen)
            {
                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var respuesta = new SqlParameter("@respuesta", SqlDbType.VarChar, 20)
                    {
                        Direction = ParameterDirection.Output
                    };

                    await _context.Database.ExecuteSqlRawAsync(
                        "EXEC SP001_SaldoReservadoNotasUrgentes_Almacenes @coditem, @codalmacen, @respuesta OUTPUT",
                        new SqlParameter("@coditem", SqlDbType.NVarChar) { Value = coditem },
                        new SqlParameter("@codalmacen", SqlDbType.Int) { Value = codalmacen },
                        respuesta
                    );

                    // respuestaValor = Convert.ToSingle(respuesta.Value);
                    if (double.TryParse(respuesta.Value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out double parsedValue))
                    {
                        respuestaValor = parsedValue;
                    }

                }
            }
            else
            {
                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var respuesta = new SqlParameter("@respuesta", SqlDbType.VarChar, 20)
                    {
                        Direction = ParameterDirection.Output
                    };

                    await _context.Database.ExecuteSqlRawAsync(
                        "EXEC SP001_SaldoReservadoNotasUrgentes_Tiendas @coditem, @codalmacen, @respuesta OUTPUT",
                        new SqlParameter("@coditem", SqlDbType.NVarChar) { Value = coditem },
                        new SqlParameter("@codalmacen", SqlDbType.Int) { Value = codalmacen },
                        respuesta
                    );
                    if (double.TryParse(respuesta.Value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out double parsedValue))
                    {
                        respuestaValor = parsedValue;
                    }
                    // respuestaValor = Convert.ToSingle(respuesta.Value);
                }
            }
            return respuestaValor;
        }



        private async Task<double> getSldReservNotaUrgentUnaProf(string userConnectionString, string coditem, int codalmacen, string idProf, int nroIdProf)
        {
            // verifica si es almacen o tienda
            double total_para_esta = 0;

            bool esAlmacen = await empaque_func.esAlmacen(userConnectionString, codalmacen);

            if (esAlmacen)
            {
                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var respuesta = new SqlParameter("@respuesta", SqlDbType.Decimal)
                    {
                        Direction = ParameterDirection.Output,
                        Precision = 18,
                        Scale = 2
                    };

                    var resultado = await _context.Database
                        .ExecuteSqlRawAsync("EXEC SP001_SaldoReservadoNotasUrgente_Una_proforma_Almacenes @coditem, @codalmacen, @id_prof, @nroid_prof, @respuesta OUTPUT",
                            new SqlParameter("@coditem", SqlDbType.NVarChar) { Value = coditem },
                            new SqlParameter("@codalmacen", SqlDbType.Int) { Value = codalmacen },
                            new SqlParameter("@id_prof", SqlDbType.NVarChar) { Value = idProf },
                            new SqlParameter("@nroid_prof", SqlDbType.Int) { Value = nroIdProf },
                            respuesta);

                    total_para_esta = Convert.ToSingle(respuesta.Value);
                }
            }
            else
            {
                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var respuesta = new SqlParameter("@respuesta", SqlDbType.Decimal)
                    {
                        Direction = ParameterDirection.Output,
                        Precision = 18,
                        Scale = 2
                    };

                    var resultado = await _context.Database
                        .ExecuteSqlRawAsync("EXEC SP001_SaldoReservadoNotasUrgente_Una_proforma_Tiendas @coditem, @codalmacen, @id_prof, @nroid_prof, @respuesta OUTPUT",
                            new SqlParameter("@coditem", SqlDbType.NVarChar) { Value = coditem },
                            new SqlParameter("@codalmacen", SqlDbType.Int) { Value = codalmacen },
                            new SqlParameter("@id_prof", SqlDbType.NVarChar) { Value = idProf },
                            new SqlParameter("@nroid_prof", SqlDbType.Int) { Value = nroIdProf },
                            respuesta);

                    total_para_esta = Convert.ToSingle(respuesta.Value);
                }
            }
            return total_para_esta;
        }



        private async Task<double> getReservasCjtos(string userConnectionString, string coditem, int codalmacen, string codempresa, bool eskit, double _saldoActual, double reservaProf)
        {
            List<inctrlstock> itemsinReserva = null;
            double CANTIDAD_RESERVADA = 0;

            /*if (!eskit)  // si no es kit debe verificar si el item es utilizado para armar conjuntos
            {
                
            }*/

            // pregunta si incluye saldos a cubrir y si debe restringir venta suelta
            // incluye saldos a cubrir esta por defecto en true en la funcion revisar bien
            // restringir venta suelta eso varia dependiendo de items verificar mas
            // if (include_saldos_a_cubrir && RESTRINGIR_VENTA_SUELTA){}
            // lo siguiente debe estar dentro de esto

            bool Reserva_Tuercas_En_Porcentaje = await empaque_func.reserv_tuer_porcen(userConnectionString, codempresa);

            if (Reserva_Tuercas_En_Porcentaje)
            {
                itemsinReserva = await empaque_func.ReservaItemsinKit1(userConnectionString, coditem);
                /*
                foreach (var item in itemsinReserva)
                {
                    instoactual itemRef = await empaque_func.GetSaldosActual(userConnectionString, codalmacen, item.coditemcontrol);
                    double cubrir_item = (double)(itemRef.cantidad * (item.porcentaje / 100));
                    //cubrir_item = Math.Floor(cubrir_item);
                    CANTIDAD_RESERVADA += cubrir_item;
                }
                */
                var cantidadReservadaTasks = itemsinReserva.Select(async item =>
                {
                    instoactual itemRef = await empaque_func.GetSaldosActual(userConnectionString, codalmacen, item.coditemcontrol);
                    return (double)(itemRef.cantidad * (item.porcentaje / 100));
                });

                var cantidadReservadaArray = await Task.WhenAll(cantidadReservadaTasks);
                CANTIDAD_RESERVADA = cantidadReservadaArray.Sum();

                if (CANTIDAD_RESERVADA < 0)
                {
                    CANTIDAD_RESERVADA = 0;
                }
            }
            else
            {
                List<inreserva> reserva2 = await empaque_func.ReservaItemsinKit2(userConnectionString, coditem, codalmacen);
                if (reserva2.Count > 0)
                {
                    double cubrir_item = (double)reserva2[0].cantidad;
                    cubrir_item = Math.Truncate(cubrir_item);
                    CANTIDAD_RESERVADA += cubrir_item;
                }
                else
                {
                    double cubrir_item = 0;
                    CANTIDAD_RESERVADA += cubrir_item;
                }
                if (CANTIDAD_RESERVADA < 0)
                {
                    CANTIDAD_RESERVADA = 0;
                }

                double resul = 0;
                double reserva_para_cjto = 0;
                double CANTIDAD_RESERVADA_DINAMICA = 0;

                inreserva_area reserva = await empaque_func.Obtener_Cantidad_Segun_SaldoActual_PromVta_SMin_PorcenVta(userConnectionString, coditem, codalmacen);


                if (reserva != null)
                {
                    if ((double)reserva.porcenvta > 0.5)
                    {
                        reserva.porcenvta = (decimal)0.5;
                    }

                    if (_saldoActual >= (double)reserva.smin)
                    {
                        resul = (double)(reserva.porcenvta * reserva.promvta);
                        resul = (double)Math.Round(resul, 2);
                        // reserva_para_cjto = _saldoActual - resul - reservaProf;
                        reserva_para_cjto = _saldoActual - resul;

                        //reserva.saldo = _saldoActual;
                        //reserva.saldo_para_vta_sueltos
                    }
                    else
                    {
                        reserva_para_cjto = _saldoActual;
                    }


                    CANTIDAD_RESERVADA_DINAMICA = reserva_para_cjto;
                    if (CANTIDAD_RESERVADA_DINAMICA > 0)
                    {
                        CANTIDAD_RESERVADA = (double)CANTIDAD_RESERVADA_DINAMICA;
                    }
                }
            }
            return CANTIDAD_RESERVADA;
        }




        private async Task<List<saldosObj>> getReservasProf(string conexion, string coditem, int codalmacen, bool obtener_cantidades_aprobadas_de_proformas, bool eskit)
        {
            List<saldosObj> saldosReservProformas;
            if (obtener_cantidades_aprobadas_de_proformas)
            {
                saldosReservProformas = await empaque_func.GetSaldosReservaProforma(conexion, codalmacen, coditem, eskit);
            }
            else
            {
                saldosReservProformas = await empaque_func.GetSaldosReservaProformaFromInstoactual(conexion, codalmacen, coditem, eskit);
            }
            return saldosReservProformas;
        }


        private async Task<instoactual> getEmpaquesItemSelect(string conexion, string coditem, int codalmacen, bool eskit)
        {
            // ***************************///////////////////************************
            // ***************************///////////////////************************
            // Desde 13/08/2024 en los items que son ganchos J validar el saldo segun el gancho J suelto y segun la tabla inkit_saldo_base

            using (var _context = DbContextFactory.Create(conexion))
            {
                var dt_ganchos = await _context.inkit_saldo_base.Where(i => i.codigo == coditem).OrderBy(i => i.codigo).FirstOrDefaultAsync();
                if (dt_ganchos != null)
                {
                    coditem = dt_ganchos.item;       // hacemos que solo traiga el saldo actual del gancho J si esta registrado aca
                    eskit = false;
                }
            }


            // ***************************///////////////////************************
            // ***************************///////////////////************************

            instoactual instoactual = null;

            if (!eskit)  // como no es kit obtiene los datos de stock directamente
            {
                //verificar si el item tiene saldos para ese almacen
                instoactual = await empaque_func.GetSaldosActual(conexion, codalmacen, coditem);
            }
            else // como es kit se debe buscar sus piezas sin importar la cantidad de estas que tenga
            {
                List<inkit> kitItems = await empaque_func.GetItemsKit(conexion, coditem);  // se tiene la lista de piezas
                /*
                foreach (inkit kit in kitItems) // se recorre la lista de piezas para consultar sus saldos disponibles de cada una (SE DEBE BASAR EL STOCK EN BASE AL MENOR NUMERO)
                {
                    var pivot = await empaque_func.GetSaldosActual(conexion, codalmacen, kit.item);
                    var cantDisp = pivot.cantidad / kit.cantidad;
                    pivot.cantidad = cantDisp;
                    if (instoactual == null)
                    {
                        instoactual = pivot;
                    }
                    else
                    {
                        if (instoactual.cantidad > cantDisp)
                        {
                            instoactual = pivot;
                        }
                    }
                }
                */
                using (var _context = DbContextFactory.Create(conexion))
                {
                    var menorSaldos = await _context.inkit
                    .Join(_context.instoactual,
                        kit => kit.item,
                        insto => insto.coditem,
                        (kit, insto) => new { kit, insto })
                    .Where(joined => joined.kit.codigo == coditem && joined.insto.codalmacen == codalmacen)
                    .Select(joined => new instoactual
                    {
                        codalmacen = codalmacen,
                        coditem = joined.kit.item,
                        cantidad = joined.insto.cantidad / joined.kit.cantidad
                    })
                    .OrderBy(result => result.cantidad)
                    .FirstOrDefaultAsync();
                    return menorSaldos;
                }

                //instoactual.coditem = coditem;
            }

            return instoactual;
        }






        /// <summary>
        /// Obtiene empaques dependiendo al area y codigo de item (debe recibir)
        /// </summary>
        /// <param name="userConn"></param>
        /// <param name="item"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("getempaques/{userConn}/{item}")]
        public async Task<ActionResult<IEnumerable<adusparametros>>> Getempaques_item_area(string userConn, string item)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);
                int codarea_empaque = Getcod_area_empaqueFromadparametros(userConnectionString);
                if (codarea_empaque == -1)
                {
                    return BadRequest(new { resp = "No se pudo obtener el codigo de área" });
                }
                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var empaques = await _context.veempaque
                        .Join(_context.veempaque1,
                              c => c.codigo,
                              d => d.codempaque,
                              (c, d) => new { C = c, D = d })
                        .Where(cd => cd.C.codarea_empaque == codarea_empaque && cd.D.item == item)
                        .OrderBy(cd => cd.C.codigo)
                        .Select(cd => new
                        {
                            Codigo = cd.C.codigo,
                            Descripcion = cd.C.corta,
                            Cantidad = cd.D.cantidad
                        })
                        .ToListAsync();

                    if (empaques.Count() == 0)
                    {
                        return NotFound(new { resp = 801 });
                    }
                    return Ok(empaques);
                }
            }
            catch (Exception ex)
            {
                return Problem("Error en el servidor al obtener empaques: " + ex.Message);
                throw;
            }
        }



        [HttpGet]
        [Route("getPreciosItem/{userConn}/{item}/{codalmacen}/{codmoneda}")]
        public async Task<ActionResult<IEnumerable<adusparametros>>> getPreciosItem(string userConn, string item, int codalmacen, string codmoneda)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);
                
                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    int area = await almacen.AreaAlmacen(_context, codalmacen);
                    string precios_por_defecto = "";
                    switch (area)
                    {
                        case 300:
                            precios_por_defecto = "1,2,3";
                            break;
                        case 400:
                            precios_por_defecto = "4,5,6";
                            break;
                        case 800:
                            precios_por_defecto = "7,8,9";
                            break;
                        default:
                            precios_por_defecto = "0";
                            break;
                    }
                    var dt = await _context.intarifa
                                    .Join(_context.intarifa1,
                                          t => t.codigo,
                                          t1 => t1.codtarifa,
                                          (t, t1) => new { t, t1 })
                                    .Where(joined => joined.t1.item == item)
                                    .OrderBy(joined => joined.t.codigo)
                                    .Select(joined => new
                                    {
                                        codigo = joined.t.codigo,
                                        descripcion = joined.t.descripcion,
                                        precio = joined.t1.precio
                                    })
                                    .ToListAsync();

                    var codigos = precios_por_defecto.Split(',').ToList();

                    var filteredResult = dt
                        .Where(joined => codigos.Contains(joined.codigo.ToString()))
                        .ToList();

                    // luego convertir los precios del detalle segun el tipo de cambio seleccionado en la proforma
                    string cadena = "";
                    foreach (var reg in filteredResult)
                    {
                        double precio_lista = (double)await tipocambio._conversion(_context, codmoneda, await ventas.monedabasetarifa(_context, reg.codigo), DateTime.Today.Date, reg.precio ?? 0);
                        precio_lista = (double)await siat.Redondeo_Decimales_SIA_5_decimales_SQL(_context, (decimal)precio_lista);
                        cadena = cadena + "(" + reg.codigo + ")" + reg.descripcion + "-" + precio_lista + " , ";
                    }

                    return Ok(new
                    {
                        precios = cadena
                    });
                }
            }
            catch (Exception ex)
            {
                return Problem("Error en el servidor al obtener precios de items: " + ex.Message);
                throw;
            }
        }


        protected int Getcod_area_empaqueFromadparametros(string userConnectionString)
        {
            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (_context.adparametros == null)
                {
                    return -1;
                }
                var codarea_empaque = _context.adparametros
                    .Select(a => new
                    {
                        codarea_empaque = a.codarea_empaque
                    })
                    .FirstOrDefault();
                if (codarea_empaque == null)
                {
                    return -1;
                }
                int codArea = (int)codarea_empaque.codarea_empaque;

                return codArea;
            }
        }


        [HttpGet]
        [Route("getMinimosItem/{userConn}/{coditem}/{codintarifa}/{codvedescuento}/{codalmacen}")]
        public async Task<object> getMinimosItem(string userConn, string coditem, int codintarifa, int codvedescuento, int codalmacen)
        {
            try
            {
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);
                double cantMin = await empaque_func.getEmpaqueMinimo(userConnectionString, coditem, codintarifa, codvedescuento);
                double pesoMin = await empaque_func.getPesoItem(userConnectionString, coditem);
                double porcenMaxVnta = await empaque_func.getPorcentMaxVenta(userConnectionString, coditem, codalmacen);

                return Ok(new
                {
                    cantMin = cantMin,
                    pesoMin = pesoMin * cantMin,
                    porcenMaxVnta = "Max Vta: " + porcenMaxVnta + "% del saldo"
                });
            }
            catch (Exception ex)
            {
                return Problem("Error en el servidor : " + ex.Message);
                throw;
            }
        }


        [HttpGet]
        [Route("getCodAlmSlds/{userConn}/{usuario}")]
        public async Task<ActionResult<adusparametros>> getCodAlmSlds(string userConn, string usuario)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);
                List<int> listaAlmacenes = new List<int>();
                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var codAlmacenes = await _context.adusparametros
                        .Where(i => i.usuario == usuario)
                        .Select(i => new
                        {
                            codalmsald1 = i.codalmsald1,
                            codalmsald2 = i.codalmsald2,
                            codalmsald3 = i.codalmsald3,
                            codalmsald4 = i.codalmsald4,
                            codalmsald5 = i.codalmsald5
                        })
                        .ToListAsync();

                    if (codAlmacenes.Count() == 0)
                    {
                        return NotFound(new { resp = "No se encontro un registro con este código" });
                    }
                    /*listaAlmacenes.Add((int)codAlmacenes.codalmsald1);
                    listaAlmacenes.Add((int)codAlmacenes.codalmsald2);
                    listaAlmacenes.Add((int)codAlmacenes.codalmsald3);
                    listaAlmacenes.Add((int)codAlmacenes.codalmsald4);
                    listaAlmacenes.Add((int)codAlmacenes.codalmsald5);*/
                    return Ok(codAlmacenes);
                }

            }
            catch (Exception ex)
            {
                return Problem("Error en el servidor : " + ex.Message);
                throw;
            }
        }
        //Para obtener si un item esta habilitado para ventas o no
        [HttpGet]
        [Route("getItemParaVta/{userConn}/{coditem}")]
        public async Task<bool> getItemParaVta(string userConn, int codalmacen, string coditem)
        {
            try
            {
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);
                var habilitado = await items.itemventa(userConnectionString, coditem);
                return habilitado;
                //if (habilitado == false) { return false; } else{ return true; }

            }
            catch (Exception)
            {
                return false;
            }
        }


        [HttpGet]
        [Route("getNumActProd/{userConn}/{id}")]
        public async Task<ActionResult<inconcepto>> getNumActProd(string userConn, string id)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);
                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    int nroactual = await datos_proforma.getNumActProd(_context, id);

                    if (nroactual == null)
                    {
                        return NotFound(new { resp = "No se encontro un registro con este código" });
                    }
                    return Ok(nroactual);
                }
            }
            catch (Exception ex)
            {
                return Problem("Error en el servidor : " + ex.Message);
                throw;
            }
        }


        [HttpPost]
        [Route("crearCliente/{userConn}")]
        public async Task<object> crearCliente(string userConn, clienteCasual cliCasual)
        {
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            string datosValidos = await clienteCasual.validar_crear_cliente(userConnectionString, cliCasual.codSN, cliCasual.nit_cliente_casual, cliCasual.tipo_doc_cliente_casual);
            if (datosValidos != "Ok")
            {
                return BadRequest(new { resp = datosValidos });
            }
            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                using (var dbContexTransaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        bool crear_cli_casu = await clienteCasual.Crear_Cliente_Casual(_context, cliCasual);
                        if (!crear_cli_casu)
                        {
                            return BadRequest(new { resp = "Error al crear el cliente" });
                        }
                        dbContexTransaction.Commit();
                        return Ok(new { resp = "Cliente creado exitosamente" });

                    }
                    catch (Exception ex)
                    {
                        dbContexTransaction.Rollback();
                        return Problem("Error en el servidor al crear cliente : " + ex.Message);
                        throw;
                    }
                }
            }
        }

        [HttpPost]
        //[Route("validarProforma/{userConn}/{cadena_controles}/{entidad}/{opcion_validar}")]
        [Route("validarProforma/{userConn}/{cadena_controles}/{entidad}/{opcion_validar}/{codempresa}/{usuario}")]
        //Task<ActionResult<itemDataMatriz>>
        //Task<object> ValidarProforma
        //para opcion_validar
        //grabar
        //grabar_aprobar
        //para entidad
        //proforma
        //remision no se usa
        //factura
        //para cadena_controles
        // vacio si no va controlar controles en especifico
        // cadena con el siguiente formato 00001+00002+00003 con los controles en especifico que se quiere controlar
        public async Task<ActionResult<List<Controles>>> ValidarProforma(string userConn, string cadena_controles, string entidad, string opcion_validar, string codempresa, string usuario, RequestValidacion RequestValidacion)
        {
            try
            {
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);
                DatosDocVta datosDocVta = new DatosDocVta();
                List<itemDataMatriz> itemDataMatriz = new List<itemDataMatriz>();
                List<vedesextraDatos>? vedesextraDatos = new List<vedesextraDatos>();
                List<vedetalleEtiqueta> vedetalleEtiqueta = new List<vedetalleEtiqueta>();
                List<vedetalleanticipoProforma>? vedetalleanticipoProforma = new List<vedetalleanticipoProforma>();
                List<verecargosDatos>? verecargosDatos = new List<verecargosDatos>();
                List<Controles>? controles_recibidos = new List<Controles>();

                datosDocVta = RequestValidacion.datosDocVta;
                itemDataMatriz = RequestValidacion.detalleItemsProf;
                vedesextraDatos = RequestValidacion.detalleDescuentos;
                vedetalleEtiqueta = RequestValidacion.detalleEtiqueta;
                vedetalleanticipoProforma = RequestValidacion.detalleAnticipos;
                verecargosDatos = RequestValidacion.detalleRecargos;
                controles_recibidos = RequestValidacion.detalleControles;

                var resultado = await validar_Vta.DocumentoValido(userConnectionString, cadena_controles, entidad, opcion_validar, datosDocVta, itemDataMatriz, vedesextraDatos, vedetalleEtiqueta, vedetalleanticipoProforma, verecargosDatos, controles_recibidos, codempresa, usuario);
                resultado = resultado.Select(p => { p.CodServicio = p.CodServicio == "" ? "0" : p.CodServicio; return p; }).ToList();
                if (resultado != null)
                {
                    ///
                    string jsonResult = JsonConvert.SerializeObject(resultado);

                    return Ok(jsonResult);
                }
                else { return BadRequest(new { resp = "No se pudo validar el documento." }); }
            }
            catch (Exception ex)
            {
                return Problem("Error en el servidor al validar Proforma: " + ex.Message);
                throw;
            }

        }



        [HttpPut]
        [Route("actualizarCorreoCliente/{userConn}")]
        public async Task<object> actualizarCorreoCliente(string userConn, updateEmailClient data)
        {
            string codcliente = data.codcliente;
            string email = data.email;

            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            if (await clienteCasual.EsClienteSinNombre(userConnectionString, codcliente))
            {
                return BadRequest(new { resp = "No se puede actualizar el correo de un codigo SIN NOMBRE!!!" });
            }

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                using (var dbContexTransaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        /////////      ACTUALIZAR DATOS DE CLIENTE Y DE TIENDA
                        bool actualizaEmailClient = await clienteCasual.actualizarEmailCliente(_context, codcliente, email);
                        if (!actualizaEmailClient)
                        {
                            return BadRequest(new { resp = "No se pudo actualizar el email del cliente" });
                        }

                        bool actualizaEmailTienda = await clienteCasual.actualizarEmailClienteTienda(_context, codcliente, email);
                        if (!actualizaEmailTienda)
                        {
                            return BadRequest(new { resp = "No se pudo actualizar el email del cliente (tienda)" });
                            throw new Exception();
                        }

                        dbContexTransaction.Commit();
                        return Ok(new { resp = "Se ha actualizado exitosamente el email del cliente en sus Datos y en datos de la Tienda del Cliente." });

                    }
                    catch (Exception ex)
                    {
                        dbContexTransaction.Rollback();
                        return Problem("Error en el servidor al actualizar el correo del cliente: " + ex.Message);
                        throw;
                    }
                }
            }
        }


        [HttpGet]
        [Route("getSaldosItemF9/{userConn}/{coditem}/{codempresa}/{usuario}")]
        public async Task<object> getSaldosItemF9(string userConn, string coditem, string codempresa, string usuario)
        {
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                try
                {
                    var result = await saldos.infoitem(userConnectionString, coditem, true, codempresa, usuario);
                    bool eskit = await empaque_func.GetEsKit(userConnectionString, coditem);  // verifica si el item es kit o no 

                    List<object> respuestas = new List<object>();
                    respuestas.Add(result);
                    if (eskit)
                    {
                        List<inkit> kitItems = await empaque_func.GetItemsKit(userConnectionString, coditem);  // se tiene la lista de piezas
                        foreach (var kit in kitItems)
                        {
                            var res = await saldos.infoitem(userConnectionString, kit.item, true, codempresa, usuario);
                            respuestas.Add(res);
                        }
                    }

                    return Ok(respuestas);
                }
                catch (Exception ex)
                {
                    return Problem("Error en el servidor : " + ex.Message);
                    throw;
                }
            }

        }


        [HttpGet]
        [Route("getItemMatriz_Anadir/{userConn}/{codempresa}/{usuario}/{coditem}/{tarifa}/{descuento}/{cantidad_pedida}/{cantidad}/{codcliente}/{opcion_nivel}/{codalmacen}/{desc_linea_seg_solicitud}/{codmoneda}/{fecha}")]
        public async Task<ActionResult<itemDataMatriz>> getItemMatriz_Anadir(string userConn, string codempresa, string usuario, string coditem, int tarifa, int descuento, decimal cantidad_pedida, decimal cantidad, string codcliente, string opcion_nivel, int codalmacen, string desc_linea_seg_solicitud, string codmoneda, DateTime fecha)
        {
            try
            {
                //string nivel = "X";
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);


                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    //precio unitario del item
                    var precioItem = await _context.intarifa1
                        .Where(i => i.codtarifa == tarifa && i.item == coditem)
                        .Select(i => i.precio)
                        .FirstOrDefaultAsync() ?? 0;
                    //convertir a la moneda el precio item
                    var monedabase = await ventas.monedabasetarifa(_context, tarifa);
                    precioItem = await tipocambio.conversion(userConnectionString, codmoneda, monedabase, fecha, (decimal)precioItem);
                    precioItem = await cliente.Redondear_5_Decimales(_context, (decimal)precioItem);
                    //porcentaje de mercaderia
                    decimal porcen_merca = 0;
                    if (codalmacen > 0)
                    {
                        var controla_stok_seguridad = await empresa.ControlarStockSeguridad(userConnectionString, codempresa);
                        if (controla_stok_seguridad == true)
                        {
                            //List<sldosItemCompleto> sld_ctrlstock_para_vtas = await saldos.SaldoItem_CrtlStock_Para_Ventas(userConnectionString, "311", codalmacen, coditem, "PE", "dpd3");
                            var sld_ctrlstock_para_vtas = await saldos.SaldoItem_CrtlStock_Para_Ventas(userConnectionString, "", codalmacen, coditem, codempresa, usuario);
                            if (sld_ctrlstock_para_vtas > 0)
                            {
                                porcen_merca = cantidad * 100 / sld_ctrlstock_para_vtas;
                            }
                            else { porcen_merca = 0; }
                        }
                        else { porcen_merca = 0; }
                    }
                    else { porcen_merca = 0; }

                    // descuento asignar asutomaticamente dependiendo de cantidad
                    var _descuento_precio = await ventas.Codigo_Descuento_Especial_Precio(_context, tarifa);
                    // pregunta si la cantidad ingresada cumple o no el empaque para descuento
                    if (await ventas.Cumple_Empaque_De_DesctoEspecial(_context,coditem,tarifa,_descuento_precio,cantidad,codcliente))
                    {
                        // si cumple
                        descuento = _descuento_precio;
                    }
                    else
                    {
                        descuento = 0;
                    }

                    //descuento de nivel del cliente
                    var niveldesc = await cliente.niveldesccliente(_context, codcliente, coditem, tarifa, opcion_nivel, false);

                    //porcentaje de descuento de nivel del cliente
                    var porcentajedesc = await cliente.porcendesccliente(_context, codcliente, coditem, tarifa, opcion_nivel, false);

                    //preciodesc 
                    var preciodesc = await cliente.Preciodesc(_context, codcliente, codalmacen, tarifa, coditem, desc_linea_seg_solicitud, niveldesc, opcion_nivel);
                    preciodesc = await tipocambio.conversion(userConnectionString, codmoneda, monedabase, fecha, (decimal)preciodesc);
                    preciodesc = await cliente.Redondear_5_Decimales(_context, preciodesc);
                    //precioneto 
                    var precioneto = await cliente.Preciocondescitem(_context, codcliente, codalmacen, tarifa, coditem, descuento, desc_linea_seg_solicitud, niveldesc, opcion_nivel);
                    precioneto = await tipocambio.conversion(userConnectionString, codmoneda, monedabase, fecha, (decimal)precioneto);
                    precioneto = await cliente.Redondear_5_Decimales(_context, precioneto);
                    //total
                    var total = cantidad * precioneto;
                    total = await cliente.Redondear_5_Decimales(_context, total);

                    var item = await _context.initem
                        .Where(i => i.codigo == coditem)
                        .Select(i => new itemDataMatriz
                        {
                            coditem = i.codigo,
                            descripcion = i.descripcion,
                            medida = i.medida,
                            udm = i.unidad,
                            porceniva = (double)i.iva,
                            cantidad_pedida = (double)cantidad_pedida,
                            cantidad = (double)cantidad,
                            porcen_mercaderia = (double)porcen_merca,
                            codtarifa = tarifa,
                            coddescuento = descuento,
                            preciolista = (double)precioItem,
                            niveldesc = niveldesc,
                            porcendesc = (double)porcentajedesc,
                            preciodesc = (double)preciodesc,
                            precioneto = (double)precioneto,
                            total = (double)total

                        })
                        .FirstOrDefaultAsync();

                    if (item == null)
                    {
                        return NotFound(new { resp = "No se encontro un registro con este código" });
                    }

                    return Ok(item);
                }

            }
            catch (Exception ex)
            {
                return Problem("Error en el servidor al añadir items por Matriz Uno a uno: " + ex.Message);
                throw;
            }
        }

        [HttpPost]
        [Route("getItemMatriz_AnadirbyGroup/{userConn}/{codempresa}/{usuario}")]
        public async Task<ActionResult<itemDataMatriz>> getItemMatriz_AnadirbyGroup(string userConn, string codempresa, string usuario, List<cargadofromMatriz> data)
        {
            try
            {
                if (data.Count() < 1)
                {
                    return BadRequest(new { resp = "No se esta recibiendo ningun dato, verifique esta situación." });
                }

                //string nivel = "X";
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);


                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    /*
                    if (addbyEmpqMin)
                    {
                        // si es agregar por empaques minimos, debe validar tambien que el descuento corresponda al precio.
                        // como se agrega en conjunto, en teoria todos tienen mismo precio y mismo descuento.
                        var tarifa = data[0].tarifa;
                        var descuento = data[0].descuento;
                        var comprueba = await _context.vedescuento_tarifa.Where(i => i.codtarifa == tarifa && i.coddescuento==descuento).FirstOrDefaultAsync();
                        if (comprueba==null)
                        {
                            return BadRequest(new { resp = "El descuento seleccionado no corresponde a la tarifa aplicada, revise los datos." });
                        }
                        int empaque = await _context.vedescuento.Where(i => i.codigo == descuento).Select(i => i.codempaque).FirstOrDefaultAsync();
                        foreach (var reg in data)
                        {
                            reg.cantidad = await _context.veempaque1.Where(i => i.codempaque==empaque && i.item == reg.coditem).Select(i => i.cantidad).FirstOrDefaultAsync() ?? 0;
                            reg.cantidad_pedida = reg.cantidad;
                        }
                    }*/

                    var resultado = await calculoPreciosMatriz(_context, codempresa, usuario, userConnectionString, data, true);

                    if (resultado == null)
                    {
                        return BadRequest(new { resp = "No se encontro informacion con los datos proporcionados." });
                    }
                    return Ok(resultado);
                }
            }
            catch (Exception ex)
            {
                return Problem("Error en el servidor al añadir items por matriz en grupo: " + ex.Message);
                throw;
            }
        }


        [HttpGet]
        [Route("getCantITemsbyEmp/{userConn}/{d_tipo}/{tarifa}/{coditem}/{cantEmpaques}")]
        public async Task<ActionResult<int>> getCantITemsbyEmp(string userConn, string d_tipo, int tarifa, string coditem, int cantEmpaques)
        {
            try
            {
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);
                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    int _descuento_precio = await ventas.Codigo_Descuento_Especial_Precio(_context, tarifa);
                    int _empaque_precio = await ventas.Codigo_Empaque_Precio(_context, tarifa);
                    int _empaque_descuento = await ventas.Codigo_Empaque_Descuento_Especial(_context, _descuento_precio);
                    double empaque = 0;
                    if (d_tipo == "Precio")
                    {
                        // si es por precio
                        empaque = await ventas.Empaque(_context, _empaque_precio, coditem);
                    }
                    else
                    {
                        // si es por descuento
                        empaque = await ventas.Empaque(_context, _empaque_descuento, coditem);
                    }
                    if (empaque == 0)
                    {
                        empaque = 1;
                    }
                    return Ok(new { total = empaque * cantEmpaques });
                }
            }
            catch (Exception ex)
            {
                return Problem("Error en el servidor : " + ex.Message);
                throw;
            }
        }


        [HttpPost]
        [Route("getCantITemsbyEmpinGroup/{userConn}/{d_tipo}/{cantEmpaques}")]
        public async Task<ActionResult<cargadofromMatriz>> getCantITemsbyEmpinGroup(string userConn, string d_tipo, int cantEmpaques, List<cargadofromMatriz> data)
        {
            try
            {
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);
                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    // como se agrega en conjunto por empaque, en teoria todos tienen mismo precio y mismo descuento.
                    var tarifa = data[0].tarifa;

                    int _descuento_precio = await ventas.Codigo_Descuento_Especial_Precio(_context, tarifa);
                    int _empaque_precio = await ventas.Codigo_Empaque_Precio(_context, tarifa);
                    int _empaque_descuento = await ventas.Codigo_Empaque_Descuento_Especial(_context, _descuento_precio);
                    
                    foreach (var item in data)
                    {
                        double empaque = 0;
                        if (d_tipo == "Precio")
                        {
                            // si es por precio
                            empaque = await ventas.Empaque(_context, _empaque_precio, item.coditem);
                        }
                        else
                        {
                            // si es por descuento
                            empaque = await ventas.Empaque(_context, _empaque_descuento, item.coditem);
                        }
                        if (empaque == 0)
                        {
                            empaque = 1;
                        }
                        item.cantidad = (decimal)empaque * cantEmpaques;
                        item.cantidad_pedida = (decimal)empaque * cantEmpaques;
                        item.descripcion = await _context.initem.Where(i => i.codigo == item.coditem).Select(i => i.descripcion).FirstOrDefaultAsync() ?? "";
                        item.medida = await _context.initem.Where(i => i.codigo == item.coditem).Select(i => i.medida).FirstOrDefaultAsync() ?? "";
                    }
                    
                    return Ok(data);
                }
            }
            catch (Exception ex)
            {
                return Problem("Error en el servidor : " + ex.Message);
                throw;
            }
        }


        [HttpPost]
        [Route("getCantfromEmpaque/{userConn}")]
        public async Task<ActionResult<cargadofromMatriz>> getCantfromEmpaque(string userConn, List<cargadofromMatriz> data)
        {
            try
            {
                if (data.Count() < 1)
                {
                    return BadRequest(new { resp = "No se esta recibiendo ningun dato, verifique esta situación." });
                }

                //string nivel = "X";
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    // como se agrega en conjunto por empaque, en teoria todos tienen mismo precio y mismo descuento.
                    var tarifa = data[0].tarifa;
                    var descuento = data[0].descuento;
                    if (descuento != 0)
                    {
                        var comprueba = await _context.vedescuento_tarifa.Where(i => i.codtarifa == tarifa && i.coddescuento == descuento).FirstOrDefaultAsync();
                        if (comprueba == null)
                        {
                            return BadRequest(new { resp = "El descuento seleccionado no corresponde a la tarifa aplicada, revise los datos." });
                        }

                    }
                    int empaqueDesc = await _context.vedescuento.Where(i => i.codigo == descuento).Select(i => i.codempaque).FirstOrDefaultAsync();
                    int empaquePrecio = await _context.intarifa.Where(i => i.codigo == tarifa).Select(i => i.codempaque).FirstOrDefaultAsync();


                    foreach (var reg in data)
                    {
                        var cantDesc = await _context.veempaque1.Where(i => i.codempaque == empaqueDesc && i.item == reg.coditem).Select(i => i.cantidad).FirstOrDefaultAsync() ?? 0;
                        var cantPrecio = await _context.veempaque1.Where(i => i.codempaque == empaquePrecio && i.item == reg.coditem).Select(i => i.cantidad).FirstOrDefaultAsync() ?? 0;
                        if (cantDesc > cantPrecio)
                        {
                            reg.cantidad = cantDesc;
                            reg.cantidad_pedida = cantDesc;
                        }
                        else
                        {
                            reg.cantidad = cantPrecio;
                            reg.cantidad_pedida = cantPrecio;
                        }

                        reg.descripcion = await _context.initem.Where(i => i.codigo == reg.coditem).Select(i => i.descripcion).FirstOrDefaultAsync() ?? "";
                        reg.medida = await _context.initem.Where(i => i.codigo == reg.coditem).Select(i => i.medida).FirstOrDefaultAsync() ?? "";
                    }
                    return Ok(data);
                }
            }
            catch (Exception ex)
            {
                return Problem("Error en el servidor : " + ex.Message);
                throw;
            }
        }




        private async Task<List<itemDataMatriz>> calculoPreciosMatriz(DBContext _context, string codEmpresa, string usuario, string userConnectionString, List<cargadofromMatriz> data, bool calcular_porcentaje)
        {
            List<itemDataMatriz> resultado = new List<itemDataMatriz>();
            string monedabase = "";
            int _descuento_precio = 0;
            //porcentaje de mercaderia
            decimal porcen_merca = 0;
            var controla_stok_seguridad = await empresa.ControlarStockSeguridad(userConnectionString, codEmpresa);
            foreach (var reg in data)
            {
                //precio unitario del item
                var precioItem = await _context.intarifa1
                    .Where(i => i.codtarifa == reg.tarifa && i.item == reg.coditem)
                    .Select(i => i.precio)
                    .FirstOrDefaultAsync() ?? 0;
                //convertir a la moneda el precio item
                monedabase = await ventas.monedabasetarifa(_context, reg.tarifa);
                precioItem = await tipocambio._conversion(_context, reg.codmoneda, monedabase, reg.fecha, (decimal)precioItem);
                precioItem = await cliente.Redondear_5_Decimales(_context, (decimal)precioItem);
                porcen_merca = reg.porcen_mercaderia;
                if (calcular_porcentaje == true)
                {
                    if (reg.codalmacen > 0)
                    {
                        if (controla_stok_seguridad == true)
                        {
                            //List<sldosItemCompleto> sld_ctrlstock_para_vtas = await saldos.SaldoItem_CrtlStock_Para_Ventas(userConnectionString, "311", codalmacen, coditem, "PE", "dpd3");
                            var sld_ctrlstock_para_vtas = await saldos.SaldoItem_CrtlStock_Para_Ventas(userConnectionString, "", reg.codalmacen, reg.coditem, codEmpresa, usuario);
                            if (sld_ctrlstock_para_vtas > 0)
                            {
                                porcen_merca = reg.cantidad * 100 / sld_ctrlstock_para_vtas;
                            }
                            else { porcen_merca = 0; }
                        }
                        else { porcen_merca = 0; }
                    }
                    else
                    {
                        porcen_merca = 0;
                    }
                }


                // descuento asignar asutomaticamente dependiendo de cantidad
                _descuento_precio = await ventas.Codigo_Descuento_Especial_Precio(_context, reg.tarifa);
                // pregunta si la cantidad ingresada cumple o no el empaque para descuento
                if (await ventas.Cumple_Empaque_De_DesctoEspecial(_context, reg.coditem, reg.tarifa, _descuento_precio, reg.cantidad, reg.codcliente))
                {
                    // si cumple
                    reg.descuento = _descuento_precio;
                }
                else
                {
                    reg.descuento = 0;
                }

                //descuento de nivel del cliente
                var niveldesc = await cliente.niveldesccliente(_context, reg.codcliente, reg.coditem, reg.tarifa, reg.opcion_nivel, false);

                //porcentaje de descuento de nivel del cliente
                var porcentajedesc = await cliente.porcendesccliente(_context, reg.codcliente, reg.coditem, reg.tarifa, reg.opcion_nivel, false);

                //preciodesc 
                var preciodesc = await cliente.Preciodesc(_context, reg.codcliente, reg.codalmacen, reg.tarifa, reg.coditem, reg.desc_linea_seg_solicitud, niveldesc, reg.opcion_nivel);
                preciodesc = await tipocambio.conversion(userConnectionString, reg.codmoneda, monedabase, reg.fecha, (decimal)preciodesc);

                preciodesc = await cliente.Redondear_5_Decimales(_context, preciodesc);
                //precioneto 
                var precioneto = await cliente.Preciocondescitem(_context, reg.codcliente, reg.codalmacen, reg.tarifa, reg.coditem, reg.descuento, reg.desc_linea_seg_solicitud, niveldesc, reg.opcion_nivel);
                precioneto = await tipocambio.conversion(userConnectionString, reg.codmoneda, monedabase, reg.fecha, (decimal)precioneto);
                precioneto = await cliente.Redondear_5_Decimales(_context, precioneto);
                //total
                var total = reg.cantidad * precioneto;
                total = await cliente.Redondear_5_Decimales(_context, total);

                var item = await _context.initem
                    .Where(i => i.codigo == reg.coditem)
                    .Select(i => new itemDataMatriz
                    {
                        coditem = i.codigo,
                        descripcion = i.descripcion,
                        medida = i.medida,
                        udm = i.unidad,
                        porceniva = (double)i.iva,
                        empaque = reg.empaque,
                        cantidad_pedida = (double)reg.cantidad_pedida,
                        cantidad = (double)reg.cantidad,
                        porcen_mercaderia = (double)Math.Round(porcen_merca, 2),
                        codtarifa = reg.tarifa,
                        coddescuento = reg.descuento,
                        preciolista = (double)precioItem,
                        niveldesc = niveldesc,
                        porcendesc = (double)porcentajedesc,
                        preciodesc = (double)preciodesc,
                        precioneto = (double)precioneto,
                        total = (double)total,
                        cumpleMin = reg.cumpleMin,
                        nroitem = reg.nroitem ?? 0
                    })
                    .FirstOrDefaultAsync();

                if (item != null)
                {
                    resultado.Add(item);
                }
            }
            if (resultado.Count() < 1)
            {
                return null;
            }
            resultado = resultado.OrderBy(i => i.nroitem).ThenByDescending(i => i.coddescuento).ToList();
            return resultado;
        }

        [HttpPost]
        [Route("aplicarDescuentoCliente/{userConn}")]
        public async Task<ActionResult<object>> aplicarDescuentoCliente(string userConn, RequestAddDescNCliente RequestAddDescNCliente)
        {
            string cmbtipo_desc_nivel = RequestAddDescNCliente.cmbtipo_desc_nivel;
            DateTime fechaProf = RequestAddDescNCliente.fechaProf.Date;
            int codtarifa_main = RequestAddDescNCliente.codtarifa_main;
            string codcliente = RequestAddDescNCliente.codcliente;
            string codcliente_real = RequestAddDescNCliente.codcliente_real;
            string codclientedescripcion = RequestAddDescNCliente.codclientedescripcion;

            string[] datos_desc = new string[2];
            datos_desc = cmbtipo_desc_nivel.Split(new char[] { ':' });
            if (datos_desc[0] == null)
            {
                return BadRequest(new { resp = "No se selecciono el tipo de descuento, verifique esta situación" });
            }
            if (datos_desc[0].Trim() == "")
            {
                return BadRequest(new { resp = "No se selecciono el tipo de descuento, verifique esta situación" });
            }
            try
            {
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);
                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    string NIVEL_ELEGIDO = datos_desc[0].Trim();
                    if (await ventas.Descuento_Linea_Habilitado(_context, NIVEL_ELEGIDO) == false)
                    {
                        return BadRequest(new { resp = "El Descuento: " + NIVEL_ELEGIDO + "-" + datos_desc[1] + " esta deshabilitado!!!" });
                    }

                    // Dsd 25-11-2022 se implemento el control de verificar la fecha de inicio y fin de validez de un vedesitem en vedesitem_parametros
                    DateTime fechaServ = (await funciones.FechaDelServidor(_context)).Date;
                    if (fechaServ < await ventas.Descuento_Linea_Fecha_Desde(_context,NIVEL_ELEGIDO))
                    {
                        return BadRequest(new { resp = "El Descuento: " + NIVEL_ELEGIDO + "-" + datos_desc[1] + " no puede ser aplicado, la proforma no debe ser anterior a la fecha inicial de la promocion!!!" });
                    }
                    if (fechaServ > await ventas.Descuento_Linea_Fecha_Hasta(_context,NIVEL_ELEGIDO))
                    {
                        return BadRequest(new { resp = "El Descuento: " + NIVEL_ELEGIDO + "-" + datos_desc[1] + " no puede ser aplicado, la proforma no debe ser despues a la fecha final de la promocion!!!" });
                    }
                    if (fechaProf.Date < await ventas.Descuento_Linea_Fecha_Desde(_context,NIVEL_ELEGIDO))
                    {
                        return BadRequest(new { resp = "El Descuento: " + NIVEL_ELEGIDO + "-" + datos_desc[1] + " no puede ser aplicado, la fecha de la proforma no debe ser anterior a la fecha inicial de la promocion!!!" });
                    }

                    // Desde 11-03-2024 Controlar si el precio de la proforma es valido para el descuento de nivel
                    if (! await ventas.TarifaValidaNivel(_context,codtarifa_main,NIVEL_ELEGIDO))
                    {
                        return BadRequest(new { resp = "El Descuent de Nivel: " + NIVEL_ELEGIDO + "-" + datos_desc[1] + " no puede ser aplicado, por el tipo de precio de la proforma!!!" });
                    }

                    if (codcliente.Trim().Length == 0)
                    {
                        return BadRequest(new { resp = "Debe ingresar el codigo de cliente!!!" });
                    }
                    if (await cliente.ExisteCliente(_context,codcliente) == false)
                    {
                        return BadRequest(new { resp = "El cliente no existe en la base de datos!!!" });
                    }
                    if (await cliente.EsClienteSinNombre(_context,codcliente))
                    {
                        return BadRequest(new { resp = "No se puede aplicar descuentos de nivel a clientes sin nombre!!!" });
                    }

                    // 1º quitar los desctos siempre
                    bool resultado = true;
                    if (resultado)
                    {
                        var desclienteElim = await _context.vedescliente.Where(i => i.cliente == codcliente && i.nivel == NIVEL_ELEGIDO).ToListAsync();
                        if (desclienteElim.Count() != 0)
                        {
                            _context.vedescliente.RemoveRange(desclienteElim);
                            await _context.SaveChangesAsync();
                        }
                        var desclienteRealElim = await _context.vedescliente.Where(i => i.cliente == codcliente_real && i.nivel == NIVEL_ELEGIDO).ToListAsync();
                        if (desclienteRealElim.Count() != 0)
                        {
                            _context.vedescliente.RemoveRange(desclienteRealElim);
                            await _context.SaveChangesAsync();
                        }
                    }

                    /*
                     
                    ''//el descto solo se puede aplicar entre el 19 al 24 de sept 2022
                    'If sia_DAL.Datos.Instancia.FechaDelServidor.Date < mifecha_desde Then
                    '    If mostrar_mensajes = True Then
                    '        MessageBox.Show("El descuento promocion primavera 2022, solo se puede aplicar desde al 19-09-2022 al 24-09-2022!!!", "Alerta!!!", MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1)
                    '    End If
                    '    Exit Sub
                    'End If


                    ''//el descto solo se puede aplicar entre el 19 al 24 de sept 2022
                    'If sia_DAL.Datos.Instancia.FechaDelServidor.Date > mifecha_hasta Then
                    '    If mostrar_mensajes = True Then
                    '        MessageBox.Show("El descuento promocion primavera 2022, solo se puede aplicar desde al 19-09-2022 al 24-09-2022!!!", "Alerta!!!", MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1)
                    '    End If
                    '    Exit Sub
                    'End If
                     
                     */

                    //2ª asignar los descuentos a partir del cliente pivote
                    try
                    {
                        var add_vedescliente = await _context.vedescliente.Where(i => i.cliente == "DESNIV" && i.nivel == NIVEL_ELEGIDO).Select(i => new vedescliente
                        {
                            cliente = codcliente,
                            coditem = i.coditem,
                            nivel = i.nivel,
                            estado = i.estado,
                            nivel_anterior = i.nivel_anterior,
                            nivel_actual_copia = i.nivel_actual_copia

                        }).OrderBy(i => i.coditem).ToListAsync();

                        _context.vedescliente.AddRange(add_vedescliente);
                        await _context.SaveChangesAsync();
                        resultado = true;
                    }
                    catch (Exception)
                    {
                        resultado = false;
                    }


                    // si la proforma es con codcliente referencia distinto
                    try
                    {
                        if (resultado)
                        {
                            if (codcliente_real != codcliente)
                            {
                                var add_vedesclienteReal = await _context.vedescliente.Where(i => i.cliente == "DESNIV" && i.nivel == NIVEL_ELEGIDO).Select(i => new vedescliente
                                {
                                    cliente = codcliente_real,
                                    coditem = i.coditem,
                                    nivel = i.nivel,
                                    estado = i.estado,
                                    nivel_anterior = i.nivel_anterior,
                                    nivel_actual_copia = i.nivel_actual_copia

                                }).OrderBy(i => i.coditem).ToListAsync();

                                _context.vedescliente.AddRange(add_vedesclienteReal);
                                await _context.SaveChangesAsync();
                                resultado = true;
                            }
                        }
                    }
                    catch (Exception)
                    {
                        resultado = false;
                    }
                    if (resultado)
                    {
                        return Ok(new { resp = "Los descuentos promocion se han asignado exitosamente al cliente: " + codcliente + " - " + codclientedescripcion + " !!!" });
                    }
                    return BadRequest(new { resp = "Ocurrio un error al asignar los descuentos promocion!!!" });
                }
            }
            catch (Exception ex)
            {
                return Problem($"Error en el servidor: {ex.Message}");
                throw;
            }
        }

        //[Authorize]
        [HttpPost]
        [QueueFilter(1)] // Limitar a 1 solicitud concurrente
        [Route("guardarProforma/{userConn}/{idProf}/{codempresa}/{paraAprobar}/{codcliente_real}")]
        public async Task<object> guardarProforma(string userConn, string idProf, string codempresa, bool paraAprobar, string codcliente_real, SaveProformaCompleta datosProforma)
        {
            bool check_desclinea_segun_solicitud = false;  // de momento no se utiliza, si se llegara a utilizar, se debe pedir por ruta
            veproforma veproforma = datosProforma.veproforma;
            List<veproforma1> veproforma1 = datosProforma.veproforma1;
            /*
            List<veproforma_valida> veproforma_valida = datosProforma.veproforma_valida;
            List<veproforma_anticipo> veproforma_anticipo = datosProforma.veproforma_anticipo;
            List<vedesextraprof> vedesextraprof = datosProforma.vedesextraprof;
            List<verecargoprof> verecargoprof = datosProforma.verecargoprof;
            List<veproforma_iva> veproforma_iva = datosProforma.veproforma_iva;

            */

            if (veproforma.tdc == null)
            {
                return BadRequest(new { resp = "No se esta recibiendo el tipo de cambio verifique esta situación." });
            }

            if (veproforma.tdc == 0)
            {
                return BadRequest(new { resp = "El tipo de cambio esta como 0 verifique esta situación." });
            }

            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            List<string> msgAlerts = new List<string>();


            using (var _context = DbContextFactory.Create(userConnectionString))
            {

                // ###############################
                // ACTUALIZAR DATOS DE CODIGO PRINCIPAL SI ES APLICABLE
                await cliente.ActualizarParametrosDePrincipal(_context, veproforma.codcliente);
                // ###############################
                datosProforma.veproforma.paraaprobar = paraAprobar;

                if (veproforma1.Count() <= 0)
                {
                    return BadRequest(new { resp = "No hay ningun item en su documento!!!" });
                }



                // ###############################  SE PUEDE LLAMAR DESDE FRONT END PARA LUEGO IR DIRECTO AL GRABADO ???????

                // RECALCULARPRECIOS(True, True);


                using (var dbContexTransaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        if (datosProforma.veproforma.idsoldesctos == null)
                        {
                            datosProforma.veproforma.idsoldesctos = "";
                        }
                        if (datosProforma.veproforma.estado_contra_entrega == null)
                        {
                            datosProforma.veproforma.estado_contra_entrega = "";
                        }
                        if (datosProforma.veproforma.contra_entrega == null)
                        {
                            datosProforma.veproforma.contra_entrega = false;
                        }
                        if (datosProforma.veproforma.tipo_complementopf >= 0 && datosProforma.veproforma.tipo_complementopf <= 1)
                        {
                            datosProforma.veproforma.tipo_complementopf = datosProforma.veproforma.tipo_complementopf + 1;
                        }

                        if (datosProforma.veproforma.tipo_complementopf == null)
                        {
                            datosProforma.veproforma.tipo_complementopf = 0;
                        }
                        if (datosProforma.veproforma.tipo_complementopf >= 3)
                        {
                            datosProforma.veproforma.tipo_complementopf = 0;
                        }
                        if (datosProforma.veproforma.pago_contado_anticipado == null)
                        {
                            datosProforma.veproforma.pago_contado_anticipado = false;
                        }
                        if (datosProforma.veproforma.obs == null)
                        {
                            datosProforma.veproforma.obs = "---";
                        }
                        if (datosProforma.veproforma.obs2 == null)
                        {
                            datosProforma.veproforma.obs2 = "";
                        }
                        if (datosProforma.veproforma.odc == null)
                        {
                            datosProforma.veproforma.odc = "";
                        }
                        if (datosProforma.veproforma.porceniva == null)
                        {
                            datosProforma.veproforma.porceniva = 0;
                        }

                        datosProforma.veproforma.fechareg = DateTime.Today.Date;
                        datosProforma.veproforma.fechaaut = new DateTime(1900, 1, 1);     // PUEDE VARIAR SI ES PARA APROBAR

                        datosProforma.veproforma.horareg = DateTime.Now.ToString("HH:mm");
                        datosProforma.veproforma.horaaut = "00:00";                       // PUEDE VARIAR SI ES PARA APROBAR
                        
                        if (veproforma.confirmada == true)
                        {
                            datosProforma.veproforma.fecha_confirmada = DateTime.Today.Date;
                            datosProforma.veproforma.hora_confirmada = DateTime.Now.ToString("HH:mm");
                        }
                        else
                        {
                            datosProforma.veproforma.fecha_confirmada = new DateTime(1900, 1, 1);
                            datosProforma.veproforma.hora_confirmada = "00:00";
                        }
                        /*
                        if (paraAprobar)
                        {
                            datosProforma.veproforma.fechaaut = DateTime.Today.Date;
                            datosProforma.veproforma.horaaut = DateTime.Now.ToString("HH:mm");
                        }*/

                        // ESTA VALIDACION ES MOMENTANEA, DESPUES SE DEBE COLOCAR SU PROPIA RUTA PARA VALIDAR, YA QUE PEDIRA CLAVE.
                        var validacion_inicial = await Validar_Datos_Cabecera(_context, codempresa, codcliente_real, veproforma);
                        if (!validacion_inicial.bandera)
                        {
                            return BadRequest(new { resp = validacion_inicial.msg });
                        }

                        var result = await Grabar_Documento(_context, idProf, codempresa, datosProforma);
                        if (result.resp != "ok")
                        {
                            dbContexTransaction.Rollback();
                            return BadRequest(new { resp = result.resp });
                        }
                        await log.RegistrarEvento(_context, veproforma.usuarioreg, Log.Entidades.SW_Proforma, result.codprof.ToString(), idProf, result.numeroId.ToString(), this._controllerName, "Grabar", Log.TipoLog.Creacion);

                        //Grabar Etiqueta
                        if (datosProforma.veetiqueta_proforma != null)
                        { 
                            veetiqueta_proforma dt_etiqueta = datosProforma.veetiqueta_proforma;

                            if (dt_etiqueta.celular == null)
                            {
                                dt_etiqueta.celular = "---";
                            }
                            dt_etiqueta.numeroid = result.numeroId;
                            var etiqueta = await _context.veetiqueta_proforma.Where(i => i.id == dt_etiqueta.id && i.numeroid == dt_etiqueta.numeroid).FirstOrDefaultAsync();
                            if (etiqueta != null)
                            {
                                _context.veetiqueta_proforma.Remove(etiqueta);
                                await _context.SaveChangesAsync();
                            }
                            _context.veetiqueta_proforma.Add(dt_etiqueta);
                            await _context.SaveChangesAsync();
                        }

                        ////ACTUALIZAR PESO
                        ///ya se guarda el documento con el peso calculado.



                        /*
                         //enlazar sol desctos con proforma   FALTA 
                        If desclinea_segun_solicitud.Checked = True And idsoldesctos.Text.Trim.Length > 0 And nroidsoldesctos.Text.Trim.Length > 0 Then
                            If Not sia_funciones.Ventas.Instancia.Enlazar_Proforma_Nueva_Con_SolDesctos_Nivel(codigo.Text, idsoldesctos.Text, nroidsoldesctos.Text) Then
                                MessageBox.Show("No se pudo realizar el enlace de esta proforma con la solicitud de descuentos de nivel, verifique el enlace en la solicitu de descuentos!!!", "ErroR de Enlace", MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1)
                            End If
                        End If
                         */



                        // devolver mensajes pero como alerta Extra
                        string msgAler1 = "";
                        // enlazar sol desctos con proforma
                        if (check_desclinea_segun_solicitud == true && veproforma.idsoldesctos.Trim().Length > 0 && veproforma.nroidsoldesctos > 0)
                        {
                            if (!await ventas.Enlazar_Proforma_Nueva_Con_SolDesctos_Nivel(_context, result.codprof, veproforma.idsoldesctos, veproforma.nroidsoldesctos ?? 0))
                            {
                                msgAler1 = "Se grabo la Proforma, pero No se pudo realizar el enlace de esta proforma con la solicitud de descuentos de nivel, verifique el enlace en la solicitu de descuentos!!!";
                                msgAlerts.Add(msgAler1);
                            }
                        }
                       
                        // grabar la etiqueta dsd 16-05-2022        
                        // solo si es cliente casual, y el cliente referencia o real es un no casual
                        //If sia_funciones.Cliente.Instancia.Es_Cliente_Casual(codcliente.Text) = True And sia_funciones.Cliente.Instancia.Es_Cliente_Casual(codcliente_real) = False Then

                        // Desde 10-10-2022 se definira si una venta es casual o no si el codigo de cliente y el codigo de cliente real son diferentes entonces es una venta casual
                        string msgAlert2 = "";
                        if (veproforma.codcliente != codcliente_real)
                        {
                            if (!await Grabar_Proforma_Etiqueta(_context, idProf, result.numeroId,check_desclinea_segun_solicitud, codcliente_real, veproforma))
                            {
                                msgAlert2 = "Se grabo la Proforma, pero No se pudo grabar la etiqueta Cliente Casual/Referencia de la proforma!!!";
                                msgAlerts.Add(msgAlert2);
                            }
                        }

                        if (paraAprobar)
                        {


                            // *****************O J O *************************************************************************************************************
                            // IMPLEMENTADO EN FECHA 26-04-2018 LLAMA A LA FUNNCION QUE VALIDA LO QUE SE VALIDA DESDE LA VENTANA DE APROBACION DE PROFORMAS
                            // *****************O J O *************************************************************************************************************


                            string mensajeAprobacion = "";
                            var resultValApro = await Validar_Aprobar_Proforma(_context, veproforma.id, result.numeroId, result.codprof, codempresa, datosProforma.tabladescuentos, datosProforma.DVTA, datosProforma.tablarecargos);

                            msgAlerts.AddRange(resultValApro.msgsAlert);


                            if (resultValApro.resp)
                            {
                                // verifica antes si la proforma esta grabar para aprobar
                                if (await ventas.proforma_para_aprobar(_context, result.codprof))
                                {
                                    // **aprobar la proforma
                                    var profforAprobar = await _context.veproforma.Where(i => i.codigo == result.codprof).FirstOrDefaultAsync();
                                    profforAprobar.aprobada = true;
                                    profforAprobar.fechaaut = DateTime.Today.Date;
                                    profforAprobar.horaaut = datos_proforma.getHoraActual();
                                    profforAprobar.usuarioaut = veproforma.usuarioreg;
                                    _context.Entry(profforAprobar).State = EntityState.Modified;
                                    await _context.SaveChangesAsync();

                                    // %%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
                                    // realizar la reserva de la mercaderia
                                    // Desde 15/11/2023 registrar en el log si por alguna razon no actualiza en instoactual correctamente al disminuir el saldo de cantidad y la reserva en proforma
                                    if (await ventas.aplicarstocksproforma(_context, result.codprof, codempresa) == false)
                                    {
                                        await log.RegistrarEvento(_context, veproforma.usuarioreg, Log.Entidades.SW_Proforma, result.codprof.ToString(), veproforma.id, result.numeroId.ToString(), this._controllerName, "No actualizo stock al sumar cantidad de reserva en PF.", Log.TipoLog.Creacion);
                                    }
                                    // %%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%

                                    mensajeAprobacion = "La proforma fue grabada para aprobar y tambien aprobada.";
                                    // Desde 23/11/2023 guardar el log de grabado aqui
                                    await log.RegistrarEvento(_context, veproforma.usuarioreg, Log.Entidades.SW_Proforma, result.codprof.ToString(), veproforma.id, result.numeroId.ToString(), this._controllerName, "Grabar Para Aprobar", Log.TipoLog.Creacion);

                                }
                                else
                                {
                                    mensajeAprobacion = "La proforma no se grabo para aprobar por lo cual no se puede aprobar.";
                                }
                            }
                            else
                            {
                                mensajeAprobacion = "La proforma solo se grabo para aprobar, pero no se pudo aprobar porque no cumple con las condiciones de aprobacion!!! Revise la proforma en la ventana de modificacion de proformas.";
                                var desaprobarProforma = await _context.veproforma.Where(i => i.codigo == result.codprof).FirstOrDefaultAsync();
                                desaprobarProforma.aprobada = false;
                                desaprobarProforma.fechaaut = new DateTime(1900,1,1);
                                desaprobarProforma.horaaut = "00:00";
                                desaprobarProforma.usuarioaut = "";
                                _context.Entry(desaprobarProforma).State = EntityState.Modified;
                                await _context.SaveChangesAsync();
                            }
                            msgAlerts.Add(mensajeAprobacion);

                        }
                        /*
                         
                         '//validar lo que se validaba en la ventana de aprobar proforma
                            Dim mi_idpf As String = sia_funciones.Ventas.Instancia.proforma_id(_CODPROFORMA)
                            Dim mi_nroidpf As String = sia_funciones.Ventas.Instancia.proforma_numeroid(_CODPROFORMA)

                            '//validar lo que se validaba en la ventana de aprobar proforma
                            Dim dt As New DataTable
                            Dim qry As String = ""
                            Dim coddesextra As String = sia_funciones.Configuracion.Instancia.emp_coddesextra_x_deposito(sia_compartidos.temporales.Instancia.codempresa)

                            qry = "select * from vedesextraprof where  coddesextra='" & coddesextra & "' and codproforma=(select codigo from veproforma where id='" & mi_idpf & "' and numeroid='" & mi_nroidpf & "')"
                            dt.Clear()
                            dt = sia_DAL.Datos.Instancia.ObtenerDataTable(qry)
                            '//verificar si la proforma tiene descto por deposito
                            If dt.Rows.Count > 0 Then
                                If Not Me.Validar_Desctos_Por_Depositos_Solo_Al_Grabar(id.Text, numeroid.Text) Then
                                    If sia_funciones.Ventas.Instancia.Eliminar_Descuento_Deposito_De_Proforma(id.Text, numeroid.Text, Me.Name) Then
                                        MessageBox.Show("Se verifico que la proforma fue grabada con montos de descuentos por deposito incorrectos, por lo que se procedio a eliminar los descuentos por deposito de la proforma; " & mi_idpf & "-" & mi_nroidpf, "Alerta", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1)
                                    End If
                                End If
                            End If

                         */


                        dbContexTransaction.Commit();
                        return Ok(new { resp = "Se Grabo la Proforma de manera Exitosa", codProf = result.codprof, alerts = msgAlerts });
                    }
                    catch (Exception ex)
                    {
                        dbContexTransaction.Rollback();
                        return Problem($"Error en el servidor al guardar Proforma: {ex.Message}");
                        throw;
                    }
                }
            }
        }


        
        private async Task<(bool resp, List<string> msgsAlert)> Validar_Aprobar_Proforma(DBContext _context, string id_pf, int nroid_pf, int cod_proforma,string codempresa, List<vedesextraDatos> tabladescuentos, DatosDocVta DVTA, List<verecargosDatos> tablarecargos)
        {
            bool resultado = true;
            List<string> msgsAlert = new List<string>();
            var dt_pf = await _context.veproforma.Where(i => i.codigo == cod_proforma).FirstOrDefaultAsync();

            ////////////////////////////////////////////////////////////////////////////////
            // validar el monto de desctos por deposito aplicado
            var respdepCoCred = await depositos_cliente.Validar_Desctos_x_Deposito_Otorgados_De_Cobranzas_Credito(_context, id_pf, nroid_pf, codempresa);
            if (!respdepCoCred.result)
            {
                resultado = false;
                if (respdepCoCred.msgAlert != "")
                {
                    msgsAlert.Add(respdepCoCred.msgAlert);
                }
            }

            var respdepCoCont = await depositos_cliente.Validar_Desctos_x_Deposito_Otorgados_De_Cbzas_Contado_CE(_context, id_pf, nroid_pf, codempresa);
            if (!respdepCoCont.result)
            {
                resultado = false;
                if (respdepCoCont.msgAlert != "")
                {
                    msgsAlert.Add(respdepCoCont.msgAlert);
                }
            }

            var respdepAntProfCont = await depositos_cliente.Validar_Desctos_x_Deposito_Otorgados_De_Anticipos_Que_Pagaron_Proformas_Contado(_context, id_pf, nroid_pf, codempresa);
            if (!respdepAntProfCont.result)
            {
                resultado = false;
                if (respdepAntProfCont.msgAlert != "")
                {
                    msgsAlert.Add(respdepAntProfCont.msgAlert);
                }
            }

            if (resultado == false)
            {
                msgsAlert.Add("No se puede aprobar la proforma, porque tiene descuentos por deposito en montos no validos!!!");
                return (false, msgsAlert);
            }
            ////////////////////////////////////////////////////////////////////////////////


            //======================================================================================
            /////////////////VALIDAR DESCTOS POR DEPOSITO APLICADOS
            //======================================================================================

            var validDescDepApli = await Validar_Descuentos_Por_Deposito_Excedente(_context,codempresa,tabladescuentos,DVTA);
            if (!validDescDepApli.result)
            {
                resultado = false;
                msgsAlert.Add(validDescDepApli.msgAlert);
                msgsAlert.Add("La proforma no puede ser aprobada, porque tiene descuentos por deposito en montos no validos!!!");
                return (false, msgsAlert);
            }
            //======================================================================================
            ///////////////VALIDAR RECARGOS POR DEPOSITO APLICADOS
            //======================================================================================
            var validRecargoDepExcedente = await Validar_Recargos_Por_Deposito_Excedente(_context, codempresa, tablarecargos, DVTA);
            if (!validRecargoDepExcedente.result)
            {
                resultado = false;
                msgsAlert.Add(validDescDepApli.msgAlert);
                msgsAlert.Add("La proforma no puede ser aprobada, porque tiene recargos por descuentos por deposito excedentes en montos no validos!!!");
                return (false, msgsAlert);
            }

            //////////////////////////////////////////////

            // mostrar mensaje de credito disponible
            /*
            
            If IsDBNull(reg("contra_entrega")) Then
                qry = "update veproforma set contra_entrega=0 where id='" & id_pf & "' and numeroid='" & nroid_pf & "'"
                sia_DAL.Datos.Instancia.EjecutarComando(qry)
            End If

             */

            // tipo pago CONTADO
            if (DVTA.tipo_vta == "CONTADO")
            {
                ////////////////////////////////////////////////////////////////////////////////////////////
                // se añadio en fecha 15-3-2016
                // es venta al contado y no necesita validar el credito
                // TODA VENTA AL CONTADO DEBE TENER ASIGNADO ID-NROID DE ANTICIPO SI ES PAGO ANTICIPADO
                // SI SE HABILITO LA OPCION DE PAGO ANTICIPADO
                ////////////////////////////////////////////////////////////////////////////////////////////
                var dt_anticipos = await anticipos_vta_contado.Anticipos_Aplicados_a_Proforma(_context, id_pf, nroid_pf);
                if (dt_anticipos.Count > 0)
                {
                    ResultadoValidacion objres = new ResultadoValidacion();
                    objres = await anticipos_vta_contado.Validar_Anticipo_Asignado_2(_context, true, DVTA, dt_anticipos, codempresa);
                    if (objres.resultado)
                    {
                        // Desde 15/01/2024 se cambio esta funcion porque no estaba validando correctamente la transformacion de moneda de los anticipos a aplicarse ya se en $us o BS
                        // If sia_funciones.Anticipos_Vta_Contado.Instancia.Validar_Anticipo_Asignado(True, dt_anticipos, reg("codcliente"), reg("nomcliente"), reg("total")) = True Then
                        goto finalizar_ok;
                    }
                    else
                    {
                        if (dt_anticipos != null)
                        {
                            return (false, msgsAlert);
                        }
                    }
                }
                goto finalizar_ok;

            }

        finalizar_ok:
            return (true, msgsAlert);

        }




        private async Task<(bool result, string msgAlert)> Validar_Descuentos_Por_Deposito_Excedente(DBContext _context, string codempresa, List<vedesextraDatos> tabladescuentos, DatosDocVta DVTA)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            bool resultado = true;
            string msgAlert = "";
           
            objres = await validar_Vta.Validar_Descuento_Por_Deposito(_context, DVTA, tabladescuentos, codempresa);

            if (objres.resultado == false)
            {
                msgAlert = objres.observacion + " " + objres.obsdetalle + "Alerta Descuentos Por Deposito!!!";
                resultado = false;
            }
            return (resultado,msgAlert);
        }
        private async Task<(bool result, string msgAlert)> Validar_Recargos_Por_Deposito_Excedente(DBContext _context, string codempresa, List<verecargosDatos> tablarecargos, DatosDocVta DVTA)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            bool resultado = true;
            string msgAlert = "";
            
            objres = await validar_Vta.Validar_Recargo_Aplicado_Por_Desc_Deposito_Excedente(_context, DVTA, tablarecargos, codempresa);

            if (objres.resultado == false)
            {
                msgAlert = objres.observacion + " " + objres.obsdetalle + "Alerta!!!";
                resultado = false;
            }
            
            return (resultado, msgAlert);
        }
        /*
        private async Task<bool> Llenar_Datos_Del_Documento(DBContext _context, int cod_proforma, string codempresa, List<itemDataMatriz> tabladetalle, veproforma veproforma)
        {
            var datos_id = await _context.adsiat_tipodocidentidad.Select(i => new
            {
                i.codigoclasificador,
                i.descripcion
            }).OrderBy(i => i.codigoclasificador).ToListAsync();
            List<string> List_tipo_vta = new List<string>();
            List_tipo_vta.Add("CONTADO");
            List_tipo_vta.Add("CREDITO");

            DatosDocVta DVTA = new DatosDocVta();
            DVTA.coddocumento = cod_proforma;
            DVTA.estado_doc_vta = "NUEVO";
            DVTA.id = veproforma.id;
            DVTA.numeroid = veproforma.numeroid.ToString();
            DVTA.fechadoc = veproforma.fecha;
            DVTA.codcliente = veproforma.codcliente;
            DVTA.nombcliente = veproforma.nomcliente;
            DVTA.nitfactura = veproforma.nit;
            DVTA.tipo_doc_id = datos_id[veproforma.tipo_docid ?? 0].descripcion;

            DVTA.codcliente_real = veproforma.codcliente_real;
            DVTA.nomcliente_real = "";
            DVTA.codmoneda = veproforma.codmoneda;
            DVTA.codtarifadefecto = await validar_Vta.Precio_Unico_Del_Documento(tabladetalle);
            DVTA.subtotaldoc = (double)veproforma.subtotal;

            DVTA.totdesctos_extras = (double)veproforma.descuentos;
            DVTA.totrecargos = (double)veproforma.recargos;
            DVTA.totaldoc = (double)veproforma.total;
            DVTA.tipo_vta = List_tipo_vta[veproforma.tipopago];
            DVTA.codalmacen = veproforma.codalmacen.ToString();

            DVTA.codvendedor = veproforma.codvendedor;
            DVTA.preciovta = veproforma.

        }


        */























        private async Task<(bool bandera, string msg)> Validar_Datos_Cabecera(DBContext _context, string codempresa, string codcliente_real, veproforma veproforma)
        {
            // VALIDACIONES PARA EVITAR NULOS
            if (veproforma.id == null) { return (false, "No se esta recibiendo el ID del documento, Consulte con el Administrador del sistema."); }
            if (veproforma.numeroid == null) { return (false, "No se esta recibiendo el número de ID del documento, Consulte con el Administrador del sistema."); }
            if (veproforma.codalmacen == null) { return (false, "No se esta recibiendo el codigo de Almacen, Consulte con el Administrador del sistema."); }
            if (veproforma.codvendedor == null) { return (false, "No se esta recibiendo el código de vendedor, Consulte con el Administrador del sistema."); }
            if (veproforma.preparacion == null) { return (false, "No se esta recibiendo el tipo de preparación, Consulte con el Administrador del sistema."); }
            if (veproforma.tipopago == null) { return (false, "No se esta recibiendo el tipo de pago, Consulte con el Administrador del sistema."); }
            if (veproforma.contra_entrega == null) { return (false, "No se esta recibiendo si la venta es contra entrega o no, Consulte con el Administrador del sistema."); }
            if (veproforma.estado_contra_entrega == null) { return (false, "No se esta recibiendo el estado contra entrega, Consulte con el Administrador del sistema."); }
            if (veproforma.id == null) { return (false, "No se esta recibiendo el ID del documento, Consulte con el Administrador del sistema."); }
            if (veproforma.codcliente == null) { return (false, "No se esta recibiendo el codigo de cliente, Consulte con el Administrador del sistema."); }
            if (veproforma.nomcliente == null) { return (false, "No se esta recibiendo el nombre del cliente, Consulte con el Administrador del sistema."); }
            if (veproforma.tipo_docid == null) { return (false, "No se esta recibiendo el tipo de documento, Consulte con el Administrador del sistema."); }
            if (veproforma.nit == null) { return (false, "No se esta recibiendo el NIT/CI del cliente, Consulte con el Administrador del sistema."); }
            if (veproforma.email == null) { return (false, "No se esta recibiendo el Email del cliente, Consulte con el Administrador del sistema."); }
            if (veproforma.codmoneda == null) { return (false, "No se esta recibiendo el codigo de moneda, Consulte con el Administrador del sistema."); }

            if (veproforma.pago_contado_anticipado == null) { return (false, "No se esta recibiendo si el pago de realiza de forma anticipada o No, Consulte con el Administrador del sistema."); }
            if (veproforma.idpf_complemento == null) { return (false, "No se esta recibiendo el ID de proforma complemento, Consulte con el Administrador del sistema."); }
            if (veproforma.nroidpf_complemento == null) { return (false, "No se esta recibiendo el Número ID de proforma complemento, Consulte con el Administrador del sistema."); }
            if (veproforma.tipo_complementopf == null) { return (false, "No se esta recibiendo el tipo de proforma complemento, Consulte con el Administrador del sistema."); }
            if (veproforma.niveles_descuento == null) { return (false, "No se esta recibiendo el nivel de descuento actual del cliente, Consulte con el Administrador del sistema."); }



            // POR AHORA VALIDACIONES QUE REQUIERAN CONSULTA A BASE DE DATOS.
            string id = veproforma.id;
            int codalmacen = veproforma.codalmacen;
            int codvendedor = veproforma.codvendedor;
            string codcliente = veproforma.codcliente;
            string nomcliente = veproforma.nomcliente;
            string nit = veproforma.nit;
            string codmoneda = veproforma.codmoneda;
            //string codcliente_real = veproforma.codcliente_real;

            veproforma.direccion = (veproforma.direccion == "") ? "---" : veproforma.direccion;
            veproforma.obs = (veproforma.obs == "") ? "---" : veproforma.obs;

            string direccion = veproforma.direccion;
            string obs = veproforma.obs;

            int tipo_doc_id = veproforma.tipo_docid ?? -2;

            // verificar si se puede realizar ventas a codigos SN
            if (await cliente.EsClienteSinNombre(_context, codcliente))
            {
                if (nit.Trim().Length > 0)
                {
                    if (int.TryParse(nit, out int result))
                    {
                        if (result > 0)
                        {
                            // ya no se podra hacer ventas a codigo sin nombre dsd mayo 2022, se debe asignar codigo al cliente
                            if (!await configuracion.emp_permitir_facturas_sin_nombre(_context, codempresa))
                            {
                                return (false, "No se puede realizar facturas a codigos SIN NOMBRE y con NIT diferente de Cero, si va a facturar a un NIT/CI dfte. de cero debe crear al cliente!!!.");
                            }
                        }
                        // si se puede facturar a codigo SIN NOMBRE CON NIT CERO
                    }
                    // igual devuelve valido si el nit no es numerico, mas abajo se validara si es correcto
                }
                // igual devuelve valido si NO INGRESO EL nit, mas abajo se validara si es correcto
            }

            if (!await cliente.clientehabilitado(_context, codcliente_real))
            {
                return (false, "Ese Cliente: " + codcliente_real + " no esta habilitado.");
            }
            // Desde 14 / 08 / 2023 validar que el cliente casual este habilitado
            if (!await cliente.clientehabilitado(_context, codcliente))
            {
                return (false, "Ese Cliente: " + codcliente + " no esta habilitado.");
            }

            tipo_doc_id = tipo_doc_id + 1;
            var respNITValido = await ventas.Validar_NIT_Correcto(_context, nit, tipo_doc_id.ToString());
            if (!respNITValido.EsValido)
            {
                return (false, "Verifique que el NIT tenga el formato correcto!!! " + respNITValido.Mensaje);
            }
            if (veproforma.tipopago==1 && veproforma.contra_entrega == true) // 0 = CONTADO, 1 = CREDITO
            {
                return (false, "LA PROFORMA NO PUEDE SER TIPO CREDITO Y CONTADO CONTRA ENTREGA, VERIFIQUE ESTA SITUACIÓN.");
            }
            if (veproforma.tipopago == 0 && veproforma.pago_contado_anticipado==true && veproforma.contra_entrega == true) // 0 = CONTADO, 1 = CREDITO
            {
                return (false, "LA PROFORMA NO PUEDE SER TIPO CONTADO CONTRA-ENTREGA CON ANTICIPOS, VERIFIQUE ESTA SITUACIÓN.");
            }
            if (veproforma.tipopago == 1 && veproforma.pago_contado_anticipado == true) // 0 = CONTADO, 1 = CREDITO
            {
                return (false, "LA PROFORMA NO PUEDE SER TIPO CREDITO Y TENER ANTICIPOS, VERIFIQUE ESTA SITUACIÓN.");
            }


            if (veproforma.contra_entrega == true)
            {
                if (veproforma.estado_contra_entrega == "")
                {
                    return (false, "Debe especificar el estado de pago del pedido si este es: CONTRA ENTREGA ");
                }
            }
            //verifica si el usuario definio como se entregara el pedido
            if (veproforma.tipoentrega != "RECOGE CLIENTE" && veproforma.tipoentrega != "ENTREGAR")
            {
                return (false, "Debe definir si el pedido: Recogera el Cliente o si Pertec Realizara la Entrega");
            }
            //verificar si elegio enlazar con proforma complemento hayan los datos
            if (veproforma.tipo_complementopf > 0)
            {
                if (veproforma.idpf_complemento.Trim().Length == 0)
                {
                    return (false, "Ha elegido complementar la proforma pero no indico el Id de la proforma con la cual desdea complementar!!!");
                }
                if (veproforma.nroidpf_complemento.ToString().Trim().Length == 0)
                {
                    return (false, "Ha elegido complementar la proforma pero no indico el NroId de la proforma con la cual desdea complementar!!!");
                }
            }
            //validar email
            if (veproforma.email.Trim().Length == 0)
            {
                return (false, "Si no especifica una direccion de email valida, no se podra enviar la factura en formato digital.");
            }
            //validar tipo de preparacion "CAJA CERRADA RECOJE CLIENTE/RECOJE CLIENTE"
            //Dsd 29 - 11 - 2022 se corrigio la palabra RECOJE por RECOGE para que se igual al campo tipoentrega.text
            if (veproforma.preparacion == "CAJA CERRADA RECOGE CLIENTE")
            {
                if (veproforma.tipoentrega != "RECOGE CLIENTE")
                {
                    return (false, "El tipo de preparacion de la proforma es: CAJA CERRADA RECOGE CLIENTE, por tanto el tipo de entrega debe ser: RECOGE CLIENTE. Verifique esta situacion!!!");
                }
            }

            //validar el NIT en el SIN
            /*
            If resultado Then
                If Not Validar_NIT_En_El_SIN() Then
                    cmbtipo_docid.Focus()
                    resultado = False
                End If
            End If
             */
            return (true, "Error al guardar todos los datos.");
        }



        private async Task<bool> Grabar_Proforma_Etiqueta(DBContext _context, string idProf, int nroidpf, bool desclinea_segun_solicitud, string codcliente_real, veproforma dtpf)
        {
            try
            {
                veproforma_etiqueta datospfe = new veproforma_etiqueta();
                // obtener datos de la etiqueta
                var dt_etiqueta = await _context.veetiqueta_proforma.Where(i => i.id == idProf && i.numeroid == nroidpf).FirstOrDefaultAsync();
                // obtener datos de proforma
                /*
                var dtpf = await _context.veproforma.Where(i => i.id == idProf && i.numeroid == nroidpf)
                    .Select(i => new
                    {
                        i.codigo,
                        i.id,
                        i.numeroid,
                        i.fecha,
                        i.codcliente,
                        i.direccion,
                        i.latitud_entrega,
                        i.longitud_entrega,
                        i.codalmacen
                    })
                    .FirstOrDefaultAsync();
                */
                datospfe.id_proforma = idProf;
                datospfe.nroid_proforma = nroidpf;
                datospfe.codalmacen = dtpf.codalmacen;
                datospfe.codcliente_casual = dtpf.codcliente;
                if (desclinea_segun_solicitud == true && dtpf.idsoldesctos.Trim().Length > 0 && (dtpf.nroidsoldesctos > 0 || dtpf.nroidsoldesctos != null))
                {
                    datospfe.codcliente_real = await ventas.Cliente_Referencia_Solicitud_Descuentos(_context, dtpf.idsoldesctos, dtpf.nroidsoldesctos ?? 0);
                }
                else
                {
                    datospfe.codcliente_real = codcliente_real;
                }
                datospfe.fecha = dtpf.fecha;
                datospfe.direccion = dtpf.direccion;
                if (dt_etiqueta != null)
                {
                    datospfe.ciudad = dt_etiqueta.ciudad;
                }
                else
                {
                    datospfe.ciudad = "";
                }

                datospfe.latitud_entrega = dtpf.latitud_entrega;
                datospfe.longitud_entrega = dtpf.longitud_entrega;
                datospfe.horareg = dtpf.horareg;
                datospfe.fechareg = dtpf.fechareg;
                datospfe.usuarioreg = dtpf.usuarioreg;

                // insertar proforma_etiqueta (datospfe)
                var profEtiqueta = await _context.veproforma_etiqueta.Where(i => i.id_proforma == datospfe.id_proforma && i.nroid_proforma == datospfe.nroid_proforma).FirstOrDefaultAsync();
                if (profEtiqueta != null)
                {
                    _context.veproforma_etiqueta.Remove(profEtiqueta);
                    await _context.SaveChangesAsync();
                }
                _context.veproforma_etiqueta.Add(datospfe);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        private async Task<(string resp, int codprof, int numeroId)> Grabar_Documento(DBContext _context, string idProf, string codempresa, SaveProformaCompleta datosProforma)
        {
            veproforma veproforma = datosProforma.veproforma;
            List<veproforma1> veproforma1 = datosProforma.veproforma1;
            var veproforma_valida = datosProforma.veproforma_valida;
            var dt_anticipo_pf = datosProforma.dt_anticipo_pf;
            var vedesextraprof = datosProforma.vedesextraprof;
            var verecargoprof = datosProforma.verecargoprof;
            var veproforma_iva = datosProforma.veproforma_iva;

            ////////////////////   GRABAR DOCUMENTO

            int _ag = await empresa.AlmacenLocalEmpresa(_context, codempresa);
            // verificar si valido el documento, si es tienda no es necesario que valide primero
            if (!await almacen.Es_Tienda(_context, _ag))
            {
                if (veproforma_valida.Count() < 1 || veproforma_valida == null)
                {
                    return ("Antes de grabar el documento debe previamente validar el mismo!!!", 0, 0);
                }
            }

            /*
            ///////////////////////////////////////////////   FALTA VALIDACIONES


            If Not Validar_Detalle() Then
                Return False
            End If

            If Not Validar_Datos() Then
                Return False
            End If

            //************************************************
            //control implementado en fecha: 09-10-2020

            If Not Me.Validar_Saldos_Negativos(False) Then
                If MessageBox.Show("La proforma genera saldos negativos, esta seguro de grabar la proforma???", "Validar Negativos", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1) = Windows.Forms.DialogResult.No Then
                    Return False
                End If
            End If






            */
            //************************************************

            //obtenemos numero actual de proforma de nuevo
            int idnroactual = await datos_proforma.getNumActProd(_context, idProf);

            if (idnroactual == 0)
            {
                return ("Error al obtener los datos de numero de proforma", 0, 0);
            }

            // valida si existe ya la proforma
            if (await datos_proforma.existeProforma(_context, idProf, idnroactual))
            {
                return ("Ese numero de documento, ya existe, por favor consulte con el administrador del sistema.", 0, 0);
            }
            veproforma.numeroid = idnroactual;

            //fin de obtener id actual

            // obtener hora y fecha actual si es que la proforma no se importo
            if (veproforma.hora_inicial == "")
            {
                veproforma.fecha_inicial = DateTime.Parse(datos_proforma.getFechaActual());
                veproforma.hora_inicial = datos_proforma.getHoraActual();
            }


            // accion de guardar

            // guarda cabecera (veproforma)
            _context.veproforma.Add(veproforma);
            await _context.SaveChangesAsync();

            var codProforma = veproforma.codigo;

            // actualiza numero id
            var numeracion = _context.venumeracion.FirstOrDefault(n => n.id == idProf);
            numeracion.nroactual += 1;
            await _context.SaveChangesAsync(); // Guarda los cambios en la base de datos


            int validaCantProf = await _context.veproforma.Where(i => i.id == veproforma.id && i.numeroid == veproforma.numeroid).CountAsync();
            if (validaCantProf > 1)
            {
                return ("Se detecto más de un número del mismo documento, por favor consulte con el administrador del sistema.", 0, 0);
            }


            // guarda detalle (veproforma1)
            // actualizar codigoproforma para agregar
            veproforma1 = veproforma1.Select(p => { p.codproforma = codProforma; return p; }).ToList();
            // colocar obs como vacio no nulo
            veproforma1 = veproforma1.Select(o => { o.obs = ""; return o; }).ToList();
            // actualizar peso del detalle.
            veproforma1 = await ventas.Actualizar_Peso_Detalle_Proforma(_context, veproforma1);

            _context.veproforma1.AddRange(veproforma1);
            await _context.SaveChangesAsync();





            //======================================================================================
            // grabar detalle de validacion
            //======================================================================================

            veproforma_valida = veproforma_valida.Select(p => { p.codproforma = codProforma; return p; }).ToList();
            _context.veproforma_valida.AddRange(veproforma_valida);
            await _context.SaveChangesAsync();

            //======================================================================================
            //grabar anticipos aplicados
            //======================================================================================
            try
            {
                var anticiposprevios = await _context.veproforma_anticipo.Where(i => i.codproforma == codProforma).ToListAsync();
                if (anticiposprevios.Count() > 0)
                {
                    _context.veproforma_anticipo.RemoveRange(anticiposprevios);
                    await _context.SaveChangesAsync();
                }
                if (dt_anticipo_pf != null)
                {
                    if (dt_anticipo_pf.Count() > 0)
                    {

                        var newData = dt_anticipo_pf
                            .Select(i => new veproforma_anticipo
                            {
                                codproforma = codProforma,
                                codanticipo = i.codanticipo,
                                monto = (decimal?)i.monto,
                                tdc = (decimal?)i.tdc,

                                fechareg = DateTime.Parse(datos_proforma.getFechaActual()),
                                usuarioreg = veproforma.usuarioreg,
                                horareg = datos_proforma.getHoraActual()
                            }).ToList();
                        _context.veproforma_anticipo.AddRange(newData);
                        await _context.SaveChangesAsync();

                    }
                }
                
            }
            catch (Exception)
            {

                throw;
            }

            //======================================================================================
            //grabar diferencias de anticipos aplicados
            //======================================================================================
            try
            {
                var diferencias_previos = await _context.veproforma_anticipo_diferencias.Where(i => i.codproforma == codProforma).ToListAsync();
                if (diferencias_previos.Count() > 0)
                {
                    _context.veproforma_anticipo_diferencias.RemoveRange(diferencias_previos);
                    await _context.SaveChangesAsync();
                }
                //obtener si hay diferencia enntre el total de aplicado de anticipo contra el total de la proforma
                decimal ttl_anticipos_aplicados = 0;
                decimal ttl_pf = 0;
                decimal diferencia_ant_pf = 0;
                bool anticipo_mayor = true;

                if (dt_anticipo_pf != null)
                {
                    if (dt_anticipo_pf.Count() > 0)
                    {
                        foreach (var ant in dt_anticipo_pf)
                        {
                            ttl_anticipos_aplicados += Math.Round(Convert.ToDecimal(ant.monto), 2);
                        }
                        ttl_pf = Math.Round(veproforma.total, 2);
                        diferencia_ant_pf = Math.Round(ttl_anticipos_aplicados - ttl_pf, 2);
                        if (ttl_anticipos_aplicados != ttl_pf)
                        {
                            anticipo_mayor = ttl_anticipos_aplicados > ttl_pf;

                            var newData = new veproforma_anticipo_diferencias
                            {
                                codproforma = codProforma,
                                monto = diferencia_ant_pf,
                                tdc = 1,
                                fechareg = DateTime.Parse(datos_proforma.getFechaActual()),
                                usuarioreg = veproforma.usuarioreg,
                                horareg = datos_proforma.getHoraActual(),
                                anticipo_aplicado_mayor = anticipo_mayor
                            };
                            await _context.veproforma_anticipo_diferencias.AddAsync(newData);
                            await _context.SaveChangesAsync();
                        }
                    }
                }
                
            }
            catch (Exception)
            {

                throw;
            }
            // grabar descto por deposito si hay descuentos
            if (vedesextraprof != null)
            {
                if (vedesextraprof.Count() > 0)
                {
                    await grabardesextra(_context, codProforma, vedesextraprof);
                }
            }

            if (verecargoprof != null)
            {
                // grabar recargo si hay recargos
                if (verecargoprof.Count > 0)
                {
                    await grabarrecargo(_context, codProforma, verecargoprof);
                }
            }

            if (veproforma_iva != null)
            {
                // grabar iva
                if (veproforma_iva.Count > 0)
                {
                    await grabariva(_context, codProforma, veproforma_iva);
                }
            }
            

            bool resultado = new bool();
            // grabar descto por deposito
            if (await ventas.Grabar_Descuento_Por_deposito_Pendiente(_context, codProforma, codempresa, veproforma.usuarioreg, vedesextraprof))
            {
                resultado = true;
            }
            else
            {
                resultado = false;
            }

            // ======================================================================================
            // actualizar saldo restante de anticipos aplicados
            // ======================================================================================
            if (resultado)
            {
                if (dt_anticipo_pf != null)
                {
                    foreach (var reg in dt_anticipo_pf)
                    {
                        if (!await anticipos_vta_contado.ActualizarMontoRestAnticipo(_context, reg.id_anticipo, reg.nroid_anticipo, reg.codproforma ?? 0, reg.codanticipo ?? 0, 0, codempresa))
                        //    if (!await anticipos_vta_contado.ActualizarMontoRestAnticipo(_context, reg.id_anticipo, reg.nroid_anticipo, reg.codproforma ?? 0, reg.codanticipo ?? 0, reg.monto, codempresa))
                        {
                            resultado = false;
                        }
                    }
                }
            }


            /*

            //grabar descto por deposito

            If resultado Then
                If sia_funciones.Ventas.Instancia.Grabar_Descuento_Por_deposito_Pendiente(codigo.Text, tabladescuentos, sia_compartidos.temporales.Instancia.codempresa, sia_compartidos.temporales.Instancia.usuario) Then
                    resultado = True
                Else
                    resultado = False
                End If
            End If


            //======================================================================================
            //actualizar saldo restante de anticipos aplicados
            //======================================================================================
            If resultado Then
                If resultado Then
                    For i = 0 To dt_anticipo_pf.Rows.Count - 1
                        'añadir detalle al documento
                        If Not sia_funciones.Anticipos_Vta_Contado.Instancia.ActualizarMontoRestAnticipo(dt_anticipo_pf.Rows(i)("id_anticipo"), dt_anticipo_pf.Rows(i)("nroid_anticipo"), dt_anticipo_pf.Rows(i)("codproforma"), dt_anticipo_pf.Rows(i)("codanticipo"), dt_anticipo_pf.Rows(i)("monto")) Then
                            resultado = False
                        End If
                    Next
                End If
            End If

             */


            return ("ok", codProforma, veproforma.numeroid);


        }


        private async Task grabardesextra(DBContext _context, int codProf, List<vedesextraprof> vedesextraprof)
        {
            var descExtraAnt = await _context.vedesextraprof.Where(i => i.codproforma == codProf).ToListAsync();
            if (descExtraAnt.Count() > 0)
            {
                _context.vedesextraprof.RemoveRange(descExtraAnt);
                await _context.SaveChangesAsync();
            }
            vedesextraprof = vedesextraprof.Select(p => { p.codproforma = codProf; return p; }).ToList();
            _context.vedesextraprof.AddRange(vedesextraprof);
            await _context.SaveChangesAsync();
        }


        private async Task grabarrecargo(DBContext _context, int codProf, List<verecargoprof> verecargoprof)
        {
            var recargosAnt = await _context.verecargoprof.Where(i => i.codproforma == codProf).ToListAsync();
            if (recargosAnt.Count() > 0)
            {
                _context.verecargoprof.RemoveRange(recargosAnt);
                await _context.SaveChangesAsync();
            }
            verecargoprof = verecargoprof.Select(p => { p.codproforma = codProf; return p; }).ToList();
            _context.verecargoprof.AddRange(verecargoprof);
            await _context.SaveChangesAsync();
        }

        private async Task grabariva(DBContext _context, int codProf, List<veproforma_iva> veproforma_iva)
        {
            var ivaAnt = await _context.veproforma_iva.Where(i => i.codproforma == codProf).ToListAsync();
            if (ivaAnt.Count() > 0)
            {
                _context.veproforma_iva.RemoveRange(ivaAnt);
                await _context.SaveChangesAsync();
            }
            veproforma_iva = veproforma_iva.Select(p => { p.codproforma = codProf; return p; }).ToList();
            _context.veproforma_iva.AddRange(veproforma_iva);
            await _context.SaveChangesAsync();
        }


        //[Authorize]
        [HttpPost]
        [QueueFilter(1)] // Limitar a 1 solicitud concurrente
        [Route("totabilizarProf/{userConn}/{usuario}/{codempresa}/{desclinea_segun_solicitud}/{cmbtipo_complementopf}/{opcion_nivel}/{codcliente_real}")]
        public async Task<object> totabilizarProf(string userConn, string usuario, string codempresa, bool desclinea_segun_solicitud, int cmbtipo_complementopf, string opcion_nivel, string codcliente_real, TotabilizarProformaCompleta datosProforma)
        {
            veproforma veproforma = datosProforma.veproforma;
            List<veproforma1_2> veproforma1_2 = datosProforma.veproforma1_2;
            List<veproforma_valida> veproforma_valida = datosProforma.veproforma_valida;
            var veproforma_anticipo = datosProforma.veproforma_anticipo;
            var vedesextraprof = datosProforma.vedesextraprof;
            var verecargoprof = datosProforma.verecargoprof;
            var veproforma_iva = datosProforma.veproforma_iva;

            if (veproforma.tdc == null)
            {
                return BadRequest(new { resp = "No se esta recibiendo el tipo de cambio verifique esta situación." });
            }

            if (veproforma.tdc == 0)
            {
                return BadRequest(new { resp = "El tipo de cambio esta como 0 verifique esta situación." });
            }


            if (veproforma1_2.Count() < 1)
            {
                return BadRequest(new { resp = "No se esta recibiendo ningun dato, verifique esta situación." });
            }

            var data = veproforma1_2.Select(i => new cargadofromMatriz
            {
                coditem = i.coditem,
                tarifa = i.codtarifa,
                descuento = i.coddescuento,
                empaque = i.empaque,
                cantidad_pedida = i.cantidad_pedida ?? 0,
                cantidad = i.cantidad,
                // codcliente = veproforma.codcliente
                codcliente = codcliente_real,
                opcion_nivel = opcion_nivel,
                codalmacen = veproforma.codalmacen,
                desc_linea_seg_solicitud = desclinea_segun_solicitud ? "SI" : "NO",  //(SI o NO)
                codmoneda = veproforma.codmoneda,
                fecha = veproforma.fecha,
                nroitem = i.nroitem,
                porcen_mercaderia = i.porcen_mercaderia
            }).ToList();

            var tabla_detalle = veproforma1_2.Select(i => new itemDataMatriz
            {
                coditem = i.coditem,
                descripcion = "",
                medida = "",
                udm = i.udm,
                porceniva = (double)i.porceniva,
                empaque = i.empaque,
                cantidad_pedida = (double)i.cantidad_pedida,
                cantidad = (double)i.cantidad,
                porcen_mercaderia = Convert.ToDouble(i.porcen_mercaderia),
                codtarifa = i.codtarifa,
                coddescuento = i.coddescuento,
                preciolista = (double)i.preciolista,
                niveldesc = i.niveldesc,
                porcendesc = 0,
                preciodesc = (double)i.preciodesc,
                precioneto = (double)i.precioneto,
                total = (double)i.total
            }).ToList();
            //string nivel = "X";
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);


            using (var _context = DbContextFactory.Create(userConnectionString))
            {

                //aplicar desctos primavera 2022
                //Aplicar_Descto_Primavera2022(False)

                ////////////////////////////////////////////////////////////
                //verificar los precios permitidos al usuario
                string cadena_precios_no_autorizados_al_us = await validar_Vta.Validar_Precios_Permitidos_Usuario(_context, usuario, tabla_detalle);
                if (cadena_precios_no_autorizados_al_us.Trim().Length > 0)
                {
                    return BadRequest(new { resp = "El documento tiene items a precio(s): " + cadena_precios_no_autorizados_al_us + " los cuales no estan asignados al usuario " + veproforma.usuarioreg + " verifique esta situacion!!!" });
                }
                ////////////////////////////////////////////////////////////



                ////////////////////////////////////////////////////////////
                //validar si es con descuentos de linea de una solicitu previamente creada

                if (!desclinea_segun_solicitud)
                {
                    //ojo aqui revisar
                    /*
                    if (await ventas.Tarifa_Permite_Desctos_Linea(_context, codtarifadefect))
                    {

                    }
                    */
                    /*
                     If codtarifadefect.Text.Trim.Length > 0 Then
                        If sia_funciones.Ventas.Instancia.Tarifa_Permite_Desctos_Linea(codtarifadefect.Text) Then
                            If cmbniveles_descuento.SelectedIndex < 0 Then
                                MessageBox.Show("Debe definir si los descuentos de nivel a utilizar seran los actuales o los anteriores!!", "Validar", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1)
                                cmbniveles_descuento.Focus()
                                TabControl1.SelectTab(2)
                                Exit Sub
                            End If
                        End If
                    End If
                     */
                }

                //verificar si la solicitud de descuentos de linea existe
                if (desclinea_segun_solicitud)
                {
                    if (!await ventas.Existe_Solicitud_Descuento_Nivel(_context, veproforma.idsoldesctos, veproforma.nroidsoldesctos ?? 0))
                    {
                        return BadRequest(new { resp = "Ha elegido utilizar la solicitud de descuentos de nivel: " + veproforma.idsoldesctos + "-" + veproforma.nroidsoldesctos + " para aplicar descuentos de linea, pero la solicitud indicada no existe!!!" });
                    }
                    if (codcliente_real != await ventas.Cliente_Solicitud_Descuento_Nivel(_context, veproforma.idsoldesctos, veproforma.nroidsoldesctos ?? 0))
                    {
                        return BadRequest(new { resp = "La solicitud de descuentos de nivel: " + veproforma.idsoldesctos + "-" + veproforma.nroidsoldesctos + " a la que hace referencia no pertenece al mismo cliente de esta proforma!!!" });
                    }
                }

                /*
                 codmoneda.Text = Trim(codmoneda.Text)

                If Trim(codmoneda.Text) = "" Then
                    codmoneda.Text = sia_funciones.Empresa.Instancia.monedabase(sia_compartidos.temporales.Instancia.codempresa)
                    tdc.Text = "1"
                Else
                    tdc.Text = sia_funciones.TipoCambio.Instancia.tipocambio(sia_funciones.Empresa.Instancia.monedabase(sia_compartidos.temporales.Instancia.codempresa), codmoneda.Text, fecha.Value.Date)
                End If
                 */




                var resultado = await calculoPreciosMatriz(_context, codempresa, usuario, userConnectionString, data, false);
                if (resultado == null)
                {
                    return BadRequest(new { resp = "No se encontro informacion con los datos proporcionados." });
                }

                var totales = await RECALCULARPRECIOS(_context, false, codempresa, cmbtipo_complementopf, codcliente_real, resultado, verecargoprof, veproforma, vedesextraprof);


                return Ok(new
                {
                    totales = totales,
                    detalleProf = resultado
                });
            }
        }

        //[Authorize]
        [HttpPost]
        [Route("recarcularRecargos/{userConn}/{codempresa}/{descuentos}/{codcliente_real}")]
        public async Task<object> recarcularRecargos(string userConn, string codempresa, double descuentos, string codcliente_real, RequestRecarlculaRecargoDescuentos RequestRecargos)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                List<itemDataMatriz> tabla_detalle = RequestRecargos.detalleItemsProf;
                veproforma veproforma = RequestRecargos.veproforma;
                List<tablarecargos> tablarecargos = RequestRecargos.tablarecargos;

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var result = await versubtotal(_context, tabla_detalle);
                    double subtotal = result.st;
                    double peso = result.peso;
                    var respRecargo = await verrecargos(_context, codempresa, veproforma.codmoneda, veproforma.fecha, subtotal, tablarecargos);
                    double recargo = respRecargo.total;
                    tablarecargos = respRecargo.tablarecargos;

                    var total = await vertotal(_context, subtotal, recargo, descuentos, codcliente_real, veproforma.codmoneda, codempresa, veproforma.fecha, tabla_detalle, tablarecargos);
                    return new
                    {
                        subtotal = subtotal,
                        peso = peso,
                        recargo = recargo,
                        total = total.TotalGen,
                        tablaRecargos = tablarecargos
                    };
                }
            }
            catch (Exception ex)
            {
                return Problem("Error en el servidor al recalcular recargos: " + ex.Message);
                throw;
            }
        }

        //[Authorize]
        [HttpPost]
        [Route("recarcularDescuentos/{userConn}/{codempresa}/{recargos}/{cmbtipo_complementopf}/{codcliente_real}")]
        public async Task<object> recarcularDescuentos(string userConn, string codempresa, double recargos, int cmbtipo_complementopf, string codcliente_real, RequestRecarlculaRecargoDescuentos RequestDescuentos)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                List<itemDataMatriz> tabla_detalle = RequestDescuentos.detalleItemsProf;
                veproforma veproforma = RequestDescuentos.veproforma;
                List<tabladescuentos>? tabladescuentos = RequestDescuentos.tabladescuentos;

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (tabladescuentos != null)
                    {
                        var result = await versubtotal(_context, tabla_detalle);
                        double subtotal = result.st;
                        double peso = result.peso;
                        var respDescuento = await verdesextra(_context, codempresa, veproforma.nit, veproforma.codmoneda, cmbtipo_complementopf, veproforma.idpf_complemento, veproforma.nroidpf_complemento ?? 0, subtotal, veproforma.fecha, tabladescuentos, tabla_detalle);

                        double descuento = respDescuento.respdescuentos;
                        tabladescuentos = respDescuento.tabladescuentos;

                        var total = await vertotal(_context, subtotal, recargos, descuento, codcliente_real, veproforma.codmoneda, codempresa, veproforma.fecha, tabla_detalle, RequestDescuentos.tablarecargos);
                        return new
                        {
                            subtotal = subtotal,
                            peso = peso,
                            descuento = descuento,
                            total = total.TotalGen,
                            tablaDescuentos = tabladescuentos
                        };
                    }
                    return BadRequest(new { resp = "No se seleccionaron descuentos extras" });
                }
            }
            catch (Exception ex)
            {
                return Problem("Error en el servidor al recalcular descuentos: " + ex.Message);
                throw;
            }
        }
        [HttpPost]
        [Route("getDescripDescExtra/{userConn}")]
        public async Task<object> getDescripDescExtra(string userConn, List<tabladescuentos>? tabladescuentos)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);
                if (tabladescuentos == null)
                {
                    return StatusCode(203, new { resp = "La proforma no tiene descuentos Extra asignados.", tieneDesc = false });
                }
                if (tabladescuentos.Count() == 0)
                {
                    return StatusCode(203, new { resp = "La proforma no tiene descuentos Extra asignados.", tieneDesc = false });
                }
                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    foreach (var reg in tabladescuentos)
                    {
                        reg.descrip = await nombres.nombredesextra(_context, reg.coddesextra);
                    }
                    return Ok(new
                    {
                        tabladescuentos,
                        tieneDesc = true
                    });
                }

            }
            catch (Exception ex)
            {
                return Problem("Error en el servidor : " + ex.Message);
                throw;
            }
        }

        private async Task<object> RECALCULARPRECIOS(DBContext _context, bool reaplicar_desc_deposito, string codempresa, int cmbtipo_complementopf, string codcliente_real, List<itemDataMatriz> tabla_detalle, List<tablarecargos> tablarecargos, veproforma veproforma, List<tabladescuentos> vedesextraprof)
        {
            var tabladescuentos = vedesextraprof.Select(i => new tabladescuentos
            {
                codproforma = i.codproforma,
                coddesextra = i.coddesextra,
                porcen = i.porcen,
                montodoc = i.montodoc,
                codcobranza = i.codcobranza,
                codcobranza_contado = i.codcobranza_contado,
                codanticipo = i.codanticipo,
                id = i.id,
                codmoneda = veproforma.codmoneda
            }).ToList();

            var result = await versubtotal(_context, tabla_detalle);
            double subtotal = result.st;
            double peso = result.peso;
            if (reaplicar_desc_deposito)
            {
                // Revisar_Aplicar_Descto_Deposito(preguntar_si_aplicare_desc_deposito);
            }

            var respRecargo = await verrecargos(_context, codempresa, veproforma.codmoneda, veproforma.fecha, subtotal, tablarecargos);
            double recargo = respRecargo.total;

            var respDescuento = await verdesextra(_context, codempresa, veproforma.nit, veproforma.codmoneda, cmbtipo_complementopf, veproforma.idpf_complemento, veproforma.nroidpf_complemento ?? 0, subtotal, veproforma.fecha, tabladescuentos, tabla_detalle);
            double descuento = respDescuento.respdescuentos;

            var resultados = await vertotal(_context, subtotal, recargo, descuento, codcliente_real, veproforma.codmoneda, codempresa, veproforma.fecha, tabla_detalle, tablarecargos);
            //QUITAR
            return new
            {
                subtotal = await siat.Redondeo_Decimales_SIA_2_decimales_SQL(_context,subtotal),
                peso = await siat.Redondeo_Decimales_SIA_2_decimales_SQL(_context, peso),
                recargo = await siat.Redondeo_Decimales_SIA_2_decimales_SQL(_context, recargo),
                descuento = await siat.Redondeo_Decimales_SIA_2_decimales_SQL(_context, descuento),
                iva = await siat.Redondeo_Decimales_SIA_2_decimales_SQL(_context, resultados.totalIva),
                total = await siat.Redondeo_Decimales_SIA_2_decimales_SQL(_context, resultados.TotalGen),
                tablaIva = resultados.tablaiva,

                tablaRecargos = respRecargo.tablarecargos,
                tablaDescuentos = respDescuento.tabladescuentos
            };

        }


        private async Task<double> Revisar_Aplicar_Descto_Deposito(DBContext _context, bool preguntar_si_aplicar_descto_deposito, string codcliente, string txtcodcliente_real, string codempresa, List<tabladescuentos> tabladescuentos)
        {
            //////////*****ojo****///////////////////
            //segun la politica de ventas vigente desde el 01-08-2022
            //solo se aplican desctos por deposito a ventas que son codcliete y codclienteref
            //el mimsmo, es decir ya no se aplica si un cliente quiere comprar a nombre de otro(casual)
            if (codcliente == txtcodcliente_real)
            {
                //PRIMERO VERIFICAR SI SE APLICA DESCTO POR DEPOSITO
                if (await configuracion.emp_hab_descto_x_deposito(_context, codempresa))
                {
                    //verificacion si le corresponde descuento por deposito
                    if (!await Se_Aplico_Descuento_Deposito(_context, codempresa, tabladescuentos))
                    {
                        if (preguntar_si_aplicar_descto_deposito)
                        {
                            /*
                             If MessageBox.Show("Desea verificar y aplicar descuentos por deposito si el cliente tiene pendiente algun descuento pendiente por este concepto?", "Validar", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) = Windows.Forms.DialogResult.Yes Then
                                Aplicar_Descuento_Por_Deposito(False, True)
                             End If
                             */
                        }
                    }
                }
            }
            // QUITAR ESTO:
            return 0;

        }

        private async Task<double> Aplicar_Descuento_Por_Deposito(DBContext _context, string codempresa, bool alertar, bool preguntar_aplicar, string codcliente, string txtcodcliente_real, string nit_cliente, List<tabladescuentos> tabladescuentos)
        {
            //primero borrar el descuento por deposito
            tabladescuentos = await borrar_descuento_por_deposito(_context, codempresa, tabladescuentos);
            //si el cliente referencia no es el mismo al cliente al cual saldra el pedido
            //entonces no se busca desctos por deposito
            //de acuerdo a la nueva politica de desctos, vigente desde el 01-08-2022

            if (codcliente != txtcodcliente_real)
            {
                return 0;
            }
            //clientes casualas no deben tener descto por deposito seg/poliita desde el 01-08-2022
            if (await cliente.Es_Cliente_Casual(_context, codcliente))
            {
                return 0;
            }
            //verificar si es cliente competencia
            if (await cliente.EsClienteCompetencia(_context, nit_cliente))
            {
                return 0;
            }

            //verificar que los desctos esten habilitados para el precio principal de la proforma
            int coddesextra_depositos = await configuracion.emp_coddesextra_x_deposito(_context, codempresa);

            return 0;


        }

        // ELIMINAR DESCUENTO POR DEPOSITO
        private async Task<List<tabladescuentos>> borrar_descuento_por_deposito(DBContext _context, string codempresa, List<tabladescuentos> tabladescuentos)
        {
            int coddesextra_depositos = await configuracion.emp_coddesextra_x_deposito(_context, codempresa);
            var result = tabladescuentos.Where(i => i.coddesextra != coddesextra_depositos).ToList();
            return result;
        }

        private async Task<bool> Se_Aplico_Descuento_Deposito(DBContext _context, string codempresa, List<tabladescuentos> tabladescuentos)
        {
            int coddesextra_depositos = await configuracion.emp_coddesextra_x_deposito(_context, codempresa);
            var result = tabladescuentos.Where(i => i.coddesextra == coddesextra_depositos).FirstOrDefault();
            if (result != null)
            {
                return true;
            }
            return false;
        }


        private async Task<(double st, double peso)> versubtotal(DBContext _context, List<itemDataMatriz> tabla_detalle)
        {
            // filtro de codigos de items
            tabla_detalle = tabla_detalle.Where(item => item.coditem != null && item.coditem.Length >= 8).ToList();
            // calculo subtotal
            double peso = 0;
            double st = 0;

            foreach (var reg in tabla_detalle)
            {
                st = st + reg.total;
                peso = (double)(peso + (await items.itempeso(_context, reg.coditem)) * reg.cantidad);
            }

            // desde 08/01/2023 redondear el resultado a dos decimales con el SQLServer
            // REVISAR SI HAY OTRO MODO NO DA CON LINQ.
            st = (double)await siat.Redondeo_Decimales_SIA_2_decimales_SQL(_context, st);
            return (st, peso);
        }

        private async Task<(double total, List<tablarecargos> tablarecargos)> verrecargos(DBContext _context, string codempresa, string codmoneda, DateTime fecha, double subtotal, List<tablarecargos> tablarecargos)
        {
            int codrecargo_pedido_urg_provincia = await configuracion.emp_codrecargo_pedido_urgente_provincia(_context, codempresa);
            //TOTALIZAR LOS RECARGOS QUE NO SON POR PEDIDO URG PROVINCIAS (los que se aplican al total final)
            double total = 0;
            foreach (var reg in tablarecargos)
            {
                string tipo = await ventas.Tipo_Recargo(_context, reg.codrecargo);
                if (reg.codrecargo != codrecargo_pedido_urg_provincia)
                {
                    if (tipo == "MONTO")
                    {
                        //si el recargo se aplica directo en MONTO
                        reg.montodoc = await tipocambio._conversion(_context, codmoneda, reg.moneda, fecha, reg.monto);
                    }
                    else
                    {
                        //si el recargo se aplica directo en %
                        reg.montodoc = (decimal)subtotal / 100 * reg.porcen;
                    }
                    reg.montodoc = Math.Round(reg.montodoc, 2);
                    total += (double)reg.montodoc;
                }
            }
            return (total, tablarecargos);

        }
        private async Task<(double respdescuentos, List<tabladescuentos> tabladescuentos)> verdesextra(DBContext _context, string codempresa, string nit, string codmoneda, int cmbtipo_complementopf, string idpf_complemento, int nroidpf_complemento, double subtotal, DateTime fecha, List<tabladescuentos> tabladescuentos, List<itemDataMatriz> detalleProf)
        {
            int coddesextra_depositos = await configuracion.emp_coddesextra_x_deposito(_context, codempresa);
            tabladescuentos = await ventas.Ordenar_Descuentos_Extra(_context, tabladescuentos);
            double monto_desc_pf_complementaria = 0;
            //calcular el monto  de descuento segun el porcentaje
            ////////////////////////////////////////////////////////////////////////////////
            //primero calcular los montos de los que se aplican en el detalle o son
            //diferenciados por item
            ////////////////////////////////////////////////////////////////////////////////
            foreach (var reg in tabladescuentos)
            {
                //verifica si el descuento es diferenciado por item
                if (await ventas.DescuentoExtra_Diferenciado_x_item(_context, reg.coddesextra))
                {
                    var resp = await ventas.DescuentoExtra_CalcularMonto(_context, reg.coddesextra, detalleProf, "", nit);
                    double monto_desc = resp.resultado;
                    detalleProf = resp.dt;

                    //si hay complemento, verificar cual es el complemento
                    if (cmbtipo_complementopf == 1 && idpf_complemento.Trim().Length > 0 && nroidpf_complemento > 0)
                    {
                        int codproforma_complementaria = await ventas.codproforma(_context, idpf_complemento, nroidpf_complemento);
                        //verificar si la proforma ya tiene el mismo descto extra, solo SI NO TIENE, se debe calcular de esa cuanto seria el descto
                        //implemantado en fecha:31-08-2022
                        if (!await ventas.Proforma_Tiene_DescuentoExtra(_context, codproforma_complementaria, reg.coddesextra))
                        {
                            List<itemDataMatriz> dtproforma1 = await _context.veproforma1
                                .Where(i => i.codproforma == codproforma_complementaria)
                                .OrderBy(i => i.coditem)
                                .Select(i => new itemDataMatriz
                                {
                                    coditem = i.coditem,
                                    //descripcion = i.descripcion,
                                    
                                    //medida = i.medida,
                                    udm = i.udm,
                                    porceniva = (double)i.porceniva,
                                    cantidad_pedida = (double)i.cantidad_pedida,
                                    cantidad = (double)i.cantidad,
                                    //porcen_mercaderia = i.porcen_mercaderia,
                                    codtarifa = i.codtarifa,
                                    coddescuento = i.coddescuento,
                                    preciolista = (double)i.preciolista,
                                    niveldesc = i.niveldesc,
                                    //porcendesc = i.porcendesc,
                                    //preciodesc = i.preciodesc,
                                    precioneto = (double)i.precioneto,
                                    total = (double)i.total,
                                    //cumple = i.cumple,
                                    nroitem = i.nroitem ?? 0,
                                })
                                .ToListAsync();
                            var resul = await ventas.DescuentoExtra_CalcularMonto(_context, reg.coddesextra, dtproforma1, "", nit);
                            monto_desc_pf_complementaria = resul.resultado;
                        }
                        else
                        {
                            monto_desc_pf_complementaria = 0;
                        }
                        
                    }
                    //sumar el monto de la proforma complementaria
                    reg.montodoc = await siat.Redondeo_Decimales_SIA_2_decimales_SQL(_context, (double)(monto_desc + monto_desc_pf_complementaria));
                }
            }

            ////////////////////////////////////////////////////////////////////////////////
            //los que se aplican en el SUBTOTAL
            ////////////////////////////////////////////////////////////////////////////////
            foreach (var reg in tabladescuentos)
            {
                if (!await ventas.DescuentoExtra_Diferenciado_x_item(_context, reg.coddesextra))
                {
                    if (reg.aplicacion == "SUBTOTAL")
                    {
                        if (coddesextra_depositos == reg.coddesextra)
                        {
                            //el monto por descuento de deposito ya esta calculado
                            //pero se debe verificar si este monto de este descuento esta en la misma moneda que la proforma

                            if (reg.codmoneda != codmoneda)
                            {
                                double monto_cambio = (double)await tipocambio._conversion(_context, codmoneda, reg.codmoneda, fecha, reg.montodoc);
                                reg.montodoc = await siat.Redondeo_Decimales_SIA_2_decimales_SQL(_context, (double)monto_cambio);
                                reg.codmoneda = codmoneda;
                            }
                        }
                        else
                        {
                            //este descuento se aplica sobre el subtotal de la venta
                            reg.montodoc = await siat.Redondeo_Decimales_SIA_2_decimales_SQL(_context, (double)((subtotal / 100) * (double)reg.porcen));
                        }
                    }
                }
            }

            //totalizar los descuentos que se aplicar al subtotal
            double total_desctos1 = 0;
            foreach (var reg in tabladescuentos)
            {
                if (reg.aplicacion == "SUBTOTAL")
                {
                    total_desctos1 += (double)reg.montodoc;
                }
            }
            //desde 08 / 01 / 2023 redondear el resultado a dos decimales con el SQLServer
            total_desctos1 = (double)await siat.Redondeo_Decimales_SIA_2_decimales_SQL(_context, (double)total_desctos1);
            // retornar total_desctos1

            ////////////////////////////////////////////////////////////////////////////////
            //los que se aplican en el TOTAL
            ////////////////////////////////////////////////////////////////////////////////

            double total_preliminar = subtotal - total_desctos1;
            foreach (var reg in tabladescuentos)
            {
                if (!await ventas.DescuentoExtra_Diferenciado_x_item(_context, reg.coddesextra))
                {
                    if (reg.aplicacion == "TOTAL")
                    {
                        if (coddesextra_depositos == reg.coddesextra)
                        {
                            //el descuento se aplica sobre el monto del deposito
                            //ya esta calculado
                        }
                        else
                        {
                            //este descuento se aplica sobre el subtotal de la venta
                            reg.montodoc = await siat.Redondeo_Decimales_SIA_2_decimales_SQL(_context, (double)((total_preliminar / 100) * (double)reg.porcen));
                        }
                    }
                }
            }
            double total_desctos2 = 0;
            foreach (var reg in tabladescuentos)
            {
                if (reg.aplicacion == "TOTAL")
                {
                    total_desctos2 += (double)reg.montodoc;
                }
            }
            //desde 08 / 01 / 2023 redondear el resultado a dos decimales con el SQLServer
            total_desctos2 = (double)await siat.Redondeo_Decimales_SIA_2_decimales_SQL(_context, (double)total_desctos2);

            double respdescuentos = (double)await siat.Redondeo_Decimales_SIA_2_decimales_SQL(_context, (double)(total_desctos1 + total_desctos2));

            return (respdescuentos, tabladescuentos);

        }

        private async Task<(double totalIva, double TotalGen, List<veproforma_iva> tablaiva)> vertotal(DBContext _context, double subtotal, double recargos, double descuentos, string codcliente_real, string codmoneda, string codempresa, DateTime fecha, List<itemDataMatriz> tabladetalle, List<tablarecargos> tablarecargos)
        {
            double suma = subtotal + recargos - descuentos;
            double totalIva = 0;
            if (suma < 0)
            {
                suma = 0;
            }
            List<veproforma_iva> tablaiva = new List<veproforma_iva>();
            if (await cliente.DiscriminaIVA(_context, codcliente_real))
            {
                // Calculo de ivas
                tablaiva = await CalcularTablaIVA(subtotal, recargos, descuentos, tabladetalle);
                //fin calculo ivas
                totalIva = await veriva(tablaiva);
                suma = suma + totalIva;
            }
            //obtener los recargos que se aplican al final
            var respues = await ventas.Recargos_Sobre_Total_Final(_context, suma, codmoneda, fecha, codempresa, tablarecargos);
            double ttl_recargos_finales = respues.ttl_recargos_sobre_total_final;

            suma = suma + ttl_recargos_finales;
            return (totalIva, suma, tablaiva);
        }

        private async Task<List<veproforma_iva>> CalcularTablaIVA(double subtotal, double recargos, double descuentos, List<itemDataMatriz> tabladetalle)
        {
            List<clsDobleDoble> lista = new List<clsDobleDoble>();

            foreach (var reg in tabladetalle)
            {
                bool encontro = false;
                foreach (var item in lista)
                {
                    if (item.dobleA == reg.porceniva)
                    {
                        encontro = true;
                        item.dobleB = item.dobleB + reg.total;
                        break;
                    }
                }
                if (!encontro)
                {
                    clsDobleDoble newReg = new clsDobleDoble();
                    newReg.dobleA = reg.porceniva;
                    newReg.dobleB = reg.total;
                    lista.Add(newReg);
                }
            }
            // pasar a tabla
            var tablaiva = lista.Select(i => new veproforma_iva
            {
                codproforma = 0,
                porceniva = (decimal)i.dobleA,
                total = (decimal)i.dobleB,
                porcenbr = 0,
                br = 0,
                iva = 0
            }).ToList();

            //calcular porcentaje de br
            double porcenbr = 0;
            try
            {
                if (subtotal > 0)
                {
                    porcenbr = ((recargos - descuentos) * 100) / subtotal;
                }
            }
            catch (Exception)
            {
                porcenbr = 0;
            }
            //calcular en la tabla
            foreach (var reg in tablaiva)
            {
                reg.porcenbr = (decimal)porcenbr;
                reg.br = (reg.total / 100) * (decimal)porcenbr;
                reg.iva = ((reg.total + reg.br) / 100) * reg.porceniva;
            }
            return tablaiva;
        }

        private async Task<double> veriva(List<veproforma_iva> tablaiva)
        {
            var total = tablaiva.Sum(i => i.iva) ?? 0;
            return (double)total;
        }


        //[Authorize]
        [HttpGet]
        [Route("valCredDispCli/{userConn}/{codcliente_real}/{usuario}/{codempresa}/{codmoneda}/{totalProf}/{fecha}")]
        public async Task<object> valCredDispCli(string userConn, string codcliente_real, string usuario, string codempresa, string codmoneda, double totalProf, DateTime fecha)
        {
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);


            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                var moneda_cliente = await cliente.monedacliente(_context, codcliente_real, usuario, codempresa);
                var resultados = await Validar_Credito_Disponible(_context, codcliente_real, usuario, codempresa, codmoneda, totalProf, fecha);
                return Ok(new
                {
                    monto_credito_disponible = resultados.monto_credito_disponible,
                    moneda_cliente = moneda_cliente,
                    msgAlertActualiza = resultados.msgAlertActualiza,
                    resultado_func = resultados.resultado_func,
                    data = resultados.data
                });
            }
        }


        [Authorize]
        [HttpPost]
        [Route("aplicarCredTempAutoCli/{userConn}/{codcliente_real}/{usuario}/{codempresa}/{codmoneda}/{monto_proforma}/{moneda_cliente}/{monto_credito_disponible}/{idprof}/{numeroidprof}")]
        public async Task<object> aplicarCredTempAutoCli(string userConn, string codcliente_real, string usuario, string codempresa, string codmoneda, double monto_proforma, string moneda_cliente, double monto_credito_disponible, string idprof, int numeroidprof)
        {
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);


            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                string docProf = idprof + "-" + numeroidprof;
                var resultados = await creditos.Añadir_Credito_Temporal_Automatico_Nal(_context, monto_credito_disponible, moneda_cliente, codcliente_real, usuario, codempresa, docProf, monto_proforma, codmoneda);
                string msgConfir = "";
                if (resultados.resp)
                {
                    msgConfir = "El credito temporal se asigno exitosamente, verifique el credito.";
                }
                else
                {
                    msgConfir = "No se pudo asignar el credito temporal, verifique las condiciones del cliente.";
                }
                
                return Ok(new
                {
                    msgConfir,
                    resultados.resp,
                    resultados.msgAlertOpcional,
                    resultados.msgInfo
                });
            }
        }


        private async Task<(bool resultado_func, object? data, string msgAlertActualiza, double monto_credito_disponible)> Validar_Credito_Disponible(DBContext _context, string codcliente_real, string usuario, string codempresa, string codmoneda, double totalProf, DateTime fecha)
        {
            string moneda_cliente = await cliente.monedacliente(_context, codcliente_real, usuario, codempresa);

            bool resultado = false;

            double monto_proforma = totalProf;

            string monedae = await empresa.monedaext(_context, codempresa);
            string monedabase = await Empresa.monedabase(_context, codempresa);
            if (codmoneda == monedae)
            {
                var res = await creditos.ValidarCreditoDisponible_en_Bs(_context, true, codcliente_real, true, totalProf, codempresa, usuario, monedae, codmoneda);
                return (res.resultado_func, res.data, res.msgAlertActualiza, res.monto_credito_disponible);
            }
            else
            {
                //Desde 17-04-2023
                //convierte el monto de la proforma a la moneda del cliente y con el monto convertido valida
                var res = await creditos.ValidarCreditoDisponible_en_Bs(_context, true, codcliente_real, true, (double)await tipocambio._conversion(_context, monedabase, codmoneda, fecha, (decimal)totalProf), codempresa, usuario, monedae, codmoneda);
                return (res.resultado_func, res.data, res.msgAlertActualiza, res.monto_credito_disponible);
            }
        }



        [HttpGet]
        [Route("transfDatosCotizacion/{userConn}/{idCotizacion}/{nroidCotizacion}/{codempresa}")]
        public async Task<object> transfDatosCotizacion(string userConn, string idCotizacion, int nroidCotizacion, string codempresa)
        {
            try
            {
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    // obtener cabecera.
                    var cabecera = await _context.vecotizacion
                        .Where(i => i.id == idCotizacion && i.numeroid == nroidCotizacion)
                        .FirstOrDefaultAsync();

                    if (cabecera == null)
                    {
                        return BadRequest(new { resp = "No se encontró una cotizacion con los datos proporcionados, revise los datos" });
                    }

                    // obtener detalles.
                    var codCotizacion = cabecera.codigo;
                    var detalle = await _context.vecotizacion1
                        .Where(i => i.codcotizacion == codCotizacion)
                        .Join(_context.initem,
                        c => c.coditem,
                        i => i.codigo,
                        (c, i) => new { c, i })
                        .Select(i => new itemDataMatriz
                        {
                            /*
                            i.c.codcotizacion,
                            i.c.coditem,
                            descripcion = i.i.descripcion,
                            medida = i.i.medida,
                            i.c.cantidad,
                            i.c.udm,
                            i.c.precioneto,
                            i.c.preciodesc,
                            i.c.niveldesc,
                            i.c.preciolista,
                            i.c.codtarifa,
                            i.c.coddescuento,
                            i.c.total,
                            i.c.porceniva,
                            i.c.peso,
                            */
                            //codproforma = i.p.codproforma,
                            coditem = i.c.coditem,
                            descripcion = i.i.descripcion,
                            medida = i.i.medida,
                            cantidad = (double)i.c.cantidad,
                            udm = i.c.udm,
                            precioneto = (double)i.c.precioneto,
                            preciodesc = (double)(i.c.preciodesc ?? 0),
                            niveldesc = i.c.niveldesc,
                            preciolista = (double)i.c.preciolista,
                            codtarifa = i.c.codtarifa,
                            coddescuento = i.c.coddescuento,
                            total = (double)i.c.total,
                            // cantaut = i.p.cantaut,
                            // totalaut = i.p.totalaut,
                            // obs = i.p.obs,
                            porceniva = (double)(i.c.porceniva ?? 0),
                            cantidad_pedida = (double)i.c.cantidad,
                            // peso = i.p.peso,
                            nroitem = 0,
                            // id = i.p.id,
                            porcen_mercaderia = 0,
                            porcendesc = 0
                        })
                        .ToListAsync();
                    // obtener cod descuentos x deposito
                    var codDesextraxDeposito = await configuracion.emp_coddesextra_x_deposito(_context, codempresa);
                    var codDesextraxDepositoContado = await configuracion.emp_coddesextra_x_deposito_contado(_context, codempresa);
                    // obtener descuentos
                    var descuentosExtra = await _context.vedesextracoti
                        .Where(i => i.codcotizacion == codCotizacion && i.coddesextra != codDesextraxDeposito && i.coddesextra != codDesextraxDepositoContado)
                        .Join(_context.vedesextra,
                        p => p.coddesextra,
                        e => e.codigo,
                        (p, e) => new { p, e })
                        .Select(i => new
                        {
                            i.p.codcotizacion,
                            i.p.coddesextra,
                            descripcion = i.e.descripcion,
                            i.p.porcen,
                            i.p.montodoc
                        })
                        .ToListAsync();
                    return Ok(new
                    {
                        cabecera = cabecera,
                        detalle = detalle,
                        descuentos = descuentosExtra
                    });
                }
            }
            catch (Exception ex)
            {
                return Problem("Error en el servidor al transferir desde cotizacion : " + ex.Message);
                throw;
            }
        }

        [HttpGet]
        [Route("transfDatosProforma/{userConn}/{idProforma}/{nroidProforma}/{codempresa}")]
        public async Task<object> transfDatosProforma(string userConn, string idProforma, int nroidProforma, string codempresa)
        {
            try
            {
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    // obtener cabecera.
                    var cabecera = await _context.veproforma
                        .Where(i => i.id == idProforma && i.numeroid == nroidProforma)
                        .FirstOrDefaultAsync();

                    if (cabecera == null)
                    {
                        return BadRequest(new { resp = "No se encontró una proforma con los datos proporcionados, revise los datos" });
                    }

                    // obtener detalles.
                    var codProforma = cabecera.codigo;
                    var detalle = await _context.veproforma1
                        .Where(i => i.codproforma == codProforma)
                        .Join(_context.initem,
                        p => p.coditem,
                        i => i.codigo,
                        (p, i) => new { p, i })
                        .Select(i => new itemDataMatriz
                        {
                            //codproforma = i.p.codproforma,
                            coditem = i.p.coditem,
                            descripcion = i.i.descripcion,
                            medida = i.i.medida,
                            cantidad = (double)i.p.cantidad,
                            udm = i.p.udm,
                            precioneto = (double)i.p.precioneto,
                            preciodesc = (double)(i.p.preciodesc ?? 0),
                            niveldesc = i.p.niveldesc,
                            preciolista = (double)i.p.preciolista,
                            codtarifa = i.p.codtarifa,
                            coddescuento = i.p.coddescuento,
                            total = (double)i.p.total,
                            // cantaut = i.p.cantaut,
                            // totalaut = i.p.totalaut,
                            // obs = i.p.obs,
                            porceniva = (double)(i.p.porceniva ?? 0),
                            cantidad_pedida = (double)(i.p.cantidad_pedida ?? 0),
                            // peso = i.p.peso,
                            nroitem = i.p.nroitem ?? 0,
                            // id = i.p.id,
                            porcen_mercaderia = 0,
                            porcendesc = 0
                        })
                        .ToListAsync();
                    // obtener cod descuentos x deposito
                    var codDesextraxDeposito = await configuracion.emp_coddesextra_x_deposito(_context, codempresa);
                    var codDesextraxDepositoContado = await configuracion.emp_coddesextra_x_deposito_contado(_context, codempresa);
                    // obtener descuentos
                    var descuentosExtra = await _context.vedesextraprof
                        .Join(_context.vedesextra,
                        p => p.coddesextra,
                        e => e.codigo,
                        (p, e) => new { p, e })
                        .Where(i => i.p.codproforma == codProforma && i.p.coddesextra != codDesextraxDeposito && i.p.coddesextra != codDesextraxDepositoContado)
                        .Select(i => new
                        {
                            i.p.codproforma,
                            i.p.coddesextra,
                            descripcion = i.e.descripcion,
                            i.p.porcen,
                            i.p.montodoc,
                            i.p.codcobranza,
                            i.p.codcobranza_contado,
                            i.p.codanticipo,
                            i.p.id
                        })
                        .ToListAsync();
                    return Ok(new
                    {
                        cabecera = cabecera,
                        detalle = detalle,
                        descuentos = descuentosExtra
                    });
                }
            }
            catch (Exception ex)
            {
                return Problem("Error en el servidor al transferir desde proforma: " + ex.Message);
                throw;
            }
        }


        [HttpPost]
        [Route("getDataEtiqueta/{userConn}")]
        public async Task<object> getDataEtiqueta(string userConn, RequestDataEtiqueta RequestDataEtiqueta)
        {
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);


            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                //verificar si hay descuentos segun solicitud, pero puede que sea segun solicitud pero no con cliente referencia
                string codcliente_ref = RequestDataEtiqueta.codcliente_real;
                if (RequestDataEtiqueta.desclinea_segun_solicitud == true && RequestDataEtiqueta.idsoldesctos.Trim().Length > 0 && RequestDataEtiqueta.nroidsoldesctos > 0)
                {
                    codcliente_ref = await ventas.Cliente_Referencia_Solicitud_Descuentos(_context, RequestDataEtiqueta.idsoldesctos, RequestDataEtiqueta.nroidsoldesctos);
                    if (codcliente_ref.Trim().Length == 0)
                    {
                        codcliente_ref = RequestDataEtiqueta.codcliente;
                    }
                }
                // falta esto detalle codcliente_ref

                if (await cliente.EsClienteSinNombre(_context, RequestDataEtiqueta.codcliente_real))
                {
                    return Ok(new
                    {
                        codigo = 0,
                        id = RequestDataEtiqueta.id,
                        numeroid = RequestDataEtiqueta.numeroid,
                        codcliente = RequestDataEtiqueta.codcliente,
                        linea1 = RequestDataEtiqueta.nomcliente,
                        linea2 = "",
                        representante = "direccion",
                        telefono = "telefono",
                        celular = "celular",
                        ciudad = "ciudad",
                        latitud_entrega = "0",
                        longitud_entrega = "0"
                    });
                }

                var telefonosTelf1 = await _context.vetienda
                    .Where(v => v.codcliente == RequestDataEtiqueta.codcliente_real)
                    .Select(v => new { codcliente = v.codcliente, tipo = "Telf1", telf = v.telefono, nomtelf = v.nomb_telf1 })
                    .ToListAsync();

                var telefonosTelf2 = await _context.vetienda
                    .Where(v => v.codcliente == RequestDataEtiqueta.codcliente_real)
                    .Select(v => new { codcliente = v.codcliente, tipo = "Telf2", telf = v.telefono_2, nomtelf = v.nomb_telf2 })
                    .ToListAsync();

                var telefonosCel1 = await _context.vetienda
                    .Where(v => v.codcliente == RequestDataEtiqueta.codcliente_real)
                    .Select(v => new { codcliente = v.codcliente, tipo = "Cel1", telf = v.celular, nomtelf = v.nomb_cel1 })
                    .ToListAsync();

                var telefonosCel2 = await _context.vetienda
                    .Where(v => v.codcliente == RequestDataEtiqueta.codcliente_real)
                    .Select(v => new { codcliente = v.codcliente, tipo = "Cel2", telf = v.celular_2, nomtelf = v.nomb_cel2 })
                    .ToListAsync();

                var telefonosCel3 = await _context.vetienda
                    .Where(v => v.codcliente == RequestDataEtiqueta.codcliente_real)
                    .Select(v => new { codcliente = v.codcliente, tipo = "Cel3", telf = v.celular_3, nomtelf = v.nomb_cel3 })
                    .ToListAsync();

                var telefonosCelWhats = await _context.vetienda
                    .Where(v => v.codcliente == RequestDataEtiqueta.codcliente_real)
                    .Select(v => new { codcliente = v.codcliente, tipo = "CelWhats", telf = v.celular_whatsapp, nomtelf = v.nomb_whatsapp })
                    .ToListAsync();

                var clienteTelefonos = telefonosTelf1
                    .Concat(telefonosTelf2)
                    .Concat(telefonosCel1)
                    .Concat(telefonosCel2)
                    .Concat(telefonosCel3)
                    .Concat(telefonosCelWhats);


                var dirCliente = await cliente.direccioncliente(_context, codcliente_ref);
                var coordenadasCliente = await cliente.latitud_longitud_cliente(_context, codcliente_ref);
                return Ok(new
                {
                    codigo = 0,
                    id = RequestDataEtiqueta.id,
                    numeroid = RequestDataEtiqueta.numeroid,
                    codcliente = codcliente_ref,
                    linea1 = await cliente.Razonsocial(_context, codcliente_ref),
                    linea2 = "",
                    representante = dirCliente + " (" + await cliente.PuntoDeVentaCliente_Segun_Direccion(_context, codcliente_ref, dirCliente) + ")",
                    telefono = await cliente.TelefonoPrincipal(_context, codcliente_ref),
                    celular = await cliente.CelularPrincipal(_context, codcliente_ref),
                    ciudad = await cliente.UbicacionCliente(_context, codcliente_ref),
                    latitud_entrega = coordenadasCliente.latitud,
                    longitud_entrega = coordenadasCliente.longitud,
                    telefonos = clienteTelefonos
                });
            }
        }


        //[Authorize]
        [HttpPost]
        [Route("versubTotal/{userConn}/{codempresa}/{usuario}")]
        public async Task<object> versubTotal(string userConn, string codempresa, string usuario, List<cargadofromMatriz> data)
        {
            if (data.Count() < 1)
            {
                return BadRequest(new { resp = "No se esta recibiendo ningun dato, verifique esta situación." });
            }
            /*
            var data = veproforma1.Select(i => new cargadofromMatriz
            {
                coditem = i.coditem,
                tarifa = i.codtarifa,
                descuento = i.coddescuento,
                cantidad_pedida = i.cantidad_pedida ?? 0,
                cantidad = i.cantidad,
                codcliente = codcliente,
                opcion_nivel = i.niveldesc,
                codalmacen = codalmacen,
                desc_linea_seg_solicitud = desclinea_segun_solicitud ? "SI" : "NO",  //(SI o NO)
                codmoneda = codmoneda,
                fecha = fecha
            }).ToList();
            */
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);


            using (var _context = DbContextFactory.Create(userConnectionString))
            {

                var resultado = await calculoPreciosMatriz(_context, codempresa, usuario, userConnectionString, data, false);
                if (resultado == null)
                {
                    return BadRequest(new { resp = "No se encontro informacion con los datos proporcionados." });
                }


                double tot = resultado.Sum(i => (i.cantidad * i.preciolista));
                double totlinea = resultado.Sum(i => (i.cantidad * i.preciodesc));
                double subtotal = resultado.Sum(i => i.total);

                // para el desgloce
                // sacar precios
                var descuentos = resultado
                    .GroupBy(obj => obj.coddescuento)
                    .Select(grp => grp.First())
                    .Select(i => i.coddescuento)
                    .ToList();



                // sacartotales
                List<double> totales = new List<double>();

                for (int i = 0; i < descuentos.Count(); i++)
                {
                    double total = resultado
                        .Where(row => row.coddescuento == descuentos[i])
                    .Sum(row => row.cantidad * (row.preciodesc - row.precioneto));

                    totales.Add(total);
                }

                var desglose = totales.Select((i, index) => new
                {
                    total = i,
                    descuento = descuentos[index]
                }).ToList();

                return Ok(new
                {
                    resul = resultado,
                    a = tot,
                    b = tot - totlinea,
                    c = totlinea,
                    d = totlinea - subtotal,
                    e = subtotal,
                    desgloce = desglose
                });
            }
        }



        [HttpGet]
        [Route("getSugerenciaTarfromDesc/{userConn}/{coddescuento}/{usuario}")]
        public async Task<object> getSugerenciaTarfromDesc(string userConn, int coddescuento, string usuario)
        {
            try
            {
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var tarifaSugerencia = await _context.vedescuento_tarifa
                        .Join(_context.adusuario_tarifa,
                        vt => vt.codtarifa,
                        at => at.codtarifa,
                        (vt, at) => new { vt, at }
                        )
                        .Where(i => i.vt.coddescuento == coddescuento && i.at.usuario == usuario)
                        .Select(i => i.vt.codtarifa)
                        .FirstOrDefaultAsync();

                    return Ok(new
                    {
                        codTarifa = tarifaSugerencia
                    });
                }
            }
            catch (Exception ex)
            {
                return Problem("Error en el servidor : " + ex.Message);
                throw;
            }
        }


        [HttpPost]
        [Route("getTarifaPrincipal/{userConn}")]
        public async Task<object> getTarifaPrincipal(string userConn, getTarifaPrincipal_Rodrigo data)
        {
            try
            {
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var tarifa = await validar_Vta.Tarifa_Monto_Min_Mayor_Rodrigo(_context, await validar_Vta.Lista_Precios_En_El_Documento(data.tabladetalle), data.DVTA);

                    return Ok(new
                    {
                        codTarifa = tarifa
                    });
                }
            }
            catch (Exception ex)
            {
                return Problem("Error en el servidor al obtener tarifa principal: " + ex.Message);
                throw;
            }
        }


        // GET: api/vedesextra/5
        [HttpGet]
        [Route("validaAddRecargo/{userConn}/{codrecargo}/{codempresa}")]
        public async Task<ActionResult<object>> validaAddRecargo(string userConn, int codrecargo, string codempresa)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    // verificacion aplicacion de RECARGOS por descuento excedente deposito bancario
                    // primero verifica si la empresa esta habilitada para aplicar recargos por descto excedente de deposito aplicado
                    int codrecargo_deposito = await configuracion.emp_codrecargo_x_deposito(_context, codempresa);
                    // si esta intentando asignar RECARGO POR DEPOSITO
                    if (codrecargo == codrecargo_deposito)
                    {
                        return BadRequest(new { resp = "La aplicacion manual de recargos por excedente de descuentos por deposito no esta habilitada!!!" });
                        /*
                        'If Not sia_funciones.Configuracion.Instancia.emp_aplica_recargos_por_descto_deposito_excedente(sia_compartidos.temporales.Instancia.codempresa) Then
                        '    MessageBox.Show("La aplicacion de recargos por excedente de deascuentos por deposito no esta habilitada!!!", "Validacion", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
                        '    resultado = False
                        'End If
                         */
                    }
                    return Ok(true);

                }
            }
            catch (Exception ex)
            {
                return Problem("Error en el servidor al validar Recargos para agregar: " + ex.Message);
                throw;
            }
        }

        // GET: api/vedesextra/5
        [HttpPost]
        [Route("validaAddDescExtraProf/{userConn}/{coddesextra}/{codigodescripcion}/{codcliente}/{codcliente_real}/{codempresa}/{tipopago}/{contra_entrega}")]
        public async Task<ActionResult<object>> validaAddDescExtraProf(string userConn, int coddesextra, string codigodescripcion, string codcliente, string codcliente_real, string codempresa, string tipopago, bool contra_entrega, List<vedesextraprof> vedesextraprof)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);
                string descuentoCredito = "";
                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    // Verificar si el descuento esta habilitado
                    var habilitado = await ventas.Descuento_Extra_Habilitado(_context, coddesextra);
                    if (!habilitado)
                    {
                        return StatusCode(203, new { resp = "El descuento: " + coddesextra + " esta deshabilitado, por favor verifique esta situacion!!!", status = false });
                    }
                    // verificar si hay descuentos excluyentes
                    var verificaResp = await hay_descuentos_excluyentes(_context, coddesextra, vedesextraprof);
                    if (verificaResp.val == false)
                    {
                        return StatusCode(203, new { resp = verificaResp.msg, status = false });
                    }
                    // validar si el descuento que esta intentando añadir valida 
                    // si el cliente deberia tener linea de credito valida
                    if (await ventas.Descuento_Extra_Valida_Linea_Credito(_context, coddesextra))
                    {
                        // implementado en fecha 27-01-2020
                        // si es cliente pertec aunque no tenga credito si se le puede orotegar el descuento
                        if (await cliente.EsClientePertec(_context, codcliente_real) == false)
                        {
                            // validar que el cliente tenga linea de credito, vigente no revertida
                            if (await creditos.Cliente_Tiene_Linea_De_Credito_Valida(_context, codcliente_real) == false)
                            {
                                // sia_funciones.Cliente.Instancia.cliente
                                // si el cliente no tiene linea de credito valida se puede dar el descuento con clave
                                // si se llena el mensaje enviar en respuesta json
                                descuentoCredito = "El descuento: " + coddesextra + " - " + codigodescripcion + " no puede ser añadido porque el cliente: " + codcliente_real + " no tiene linea de credito valida o vigente, sin embargo se añadira pero al momento de grabar y aprobar se requerira clave!!!";
                            }
                        }
                    }


                    ////////////////////////////////////////////////////////////////////////////////////////
                    // verificar que el descto extra este asignado al CLIENTE REAL
                    // implementado en fecha: 30-11-2021
                    if (await cliente.Cliente_Tiene_Descto_Extra_Asignado(_context, coddesextra, codcliente_real) == false)
                    {
                        return StatusCode(203, new { resp = "El cliente: " + codcliente_real + " no tiene asignado el descuento: " + coddesextra + ", verificque esta situacion!!!", status = false });
                    }

                    ////////////////////////////////////////////////////////////////////////////////////////
                    // verificar que el descto extra este asignado CLIENTE
                    // implementado en fecha: 30-11-2021
                    if (codcliente != codcliente_real)
                    {
                        if (await cliente.Cliente_Tiene_Descto_Extra_Asignado(_context, coddesextra, codcliente) == false)
                        {
                            return StatusCode(203, new { resp = "El cliente: " + codcliente + " no tiene asignado el descuento: " + coddesextra + ", verificque esta situacion!!!", status = false });
                        }
                    }

                    ////////////////////////////////////////////////////////////////////////////////////////
                    // verificar si es el descuento por deposito
                    // si es impedir que se añada manualmente
                    int cod_desextra = await configuracion.emp_coddesextra_x_deposito(_context, codempresa);
                    if (cod_desextra != 0)
                    {
                        if (coddesextra == cod_desextra)
                        {
                            return StatusCode(203, new { resp = "El descuento por desposito: " + coddesextra + " no puede ser añadido manualmente!!!", status = false });
                        }
                    }

                    // verificar si es el descuento por deposito contado 
                    // si es impedir que se añada si
                    // 1.- antes no hay un enlace con un anticipo contado
                    // 2.- que el anticipo este enlazado a un deposito de cliente
                    // 3.- que la proforma solo este marcada como contado y no asi contado - contra entrega
                    int cod_desextra_contado = await configuracion.emp_coddesextra_x_deposito_contado(_context, codempresa);
                    if (cod_desextra != 0)
                    {
                        if (coddesextra == cod_desextra_contado)
                        {
                            // 1.- antes no hay un enlace con un anticipo contado
                            // If dt_anticipo_pf.Rows.Count = 0 Then
                            // MessageBox.Show("El descuento por desposito CONTADO: " & codigo.Text & " debe tener asignado un anticipo!!!", "Validar Descuento", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1)
                            // resultado = False
                            // End If
                            // 2.- que la proforma no sea contra entrega
                            // Dim id_nroid_deposito As String()
                            // id_nroid_deposito = sia_funciones.Depositos_Cliente.Instancia.IdNroid_Deposito_Asignado_Anticipo(dt_anticipo_pf.Rows(0)("id_anticipo"), dt_anticipo_pf.Rows(0)("nroid_anticipo"))
                            if (contra_entrega && tipopago == "CONTADO")
                            {
                                return StatusCode(203, new { resp = "La proforma es de tipo pago CONTADO - CONTRA ENTREGA lo cual no esta permitido para este descuento!!!", status = false });
                            }
                            if (tipopago == "CREDITO")
                            {
                                return StatusCode(203, new { resp = "La proforma es de tipo pago CREDITO lo cual no esta permitido para este descuento!!!", status = false });
                            }
                            /*
                             ''//3.- que el anticipo este enlazado a un deposito de cliente
                            'If resultado Then
                            '    Dim id_nroid_deposito As String()
                            '    id_nroid_deposito = sia_funciones.Depositos_Cliente.Instancia.IdNroid_Deposito_Asignado_Anticipo(dt_anticipo_pf.Rows(0)("id_anticipo"), dt_anticipo_pf.Rows(0)("nroid_anticipo"))
                            '    If id_nroid_deposito(0) = "NSE" Then
                            '        MessageBox.Show("El anticipo: " & dt_anticipo_pf.Rows(0)("id_anticipo") & "-" & dt_anticipo_pf.Rows(0)("nroid_anticipo") & " asignado a la proforma no esta enlazado a algun deposito de cliente!!!", "Validar Descuento", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1)
                            '        resultado = False
                            '    End If
                            'End If

                            '//3.- que la proforma solo este marcada como contado y no asi contado - contra entrega
                            'If dt_anticipo_pf.Rows.Count = 0 Then
                            '    MessageBox.Show("El descuento por desposito CONTADO: " & codigo.Text & " debe tener asignado un anticipo!!!", "Validar Descuento", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1)
                            '    resultado = False
                            'End If
                             */
                        }
                    }
                    return Ok(new { resp = descuentoCredito, status = true });
                }

            }
            catch (Exception ex)
            {
                return Problem("Error en el servidor al validar desc. Extra para agregar en PF : " + ex.Message);
                throw;
            }
        }



        private async Task<(bool val, string msg)> hay_descuentos_excluyentes(DBContext _context, int coddesextra, List<vedesextraprof> vedesextraprof)
        {
            foreach (var reg in vedesextraprof)
            {
                var valida = await _context.vedesextra_excluyentes
                    .Where(i => (i.coddesextra1 == coddesextra && i.coddesextra2 == reg.coddesextra) || (i.coddesextra1 == reg.coddesextra && i.coddesextra2 == coddesextra))
                    .FirstOrDefaultAsync();
                if (valida != null)
                {
                    return (false, "El descuento: " + coddesextra + " y el descuento: " + reg.coddesextra + " no pueden ser aplicados de manera simultanea en una misma proforma.");
                }
            }
            return (true, "");
        }

        // GET: api/getUbicacionCliente/5
        [HttpPost]
        [Route("getUbicacionCliente/{userConn}")]
        public async Task<ActionResult<object>> getUbicacionCliente(string userConn, ubicacionCliente ubicacionCliente)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);
            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                var codPtoVenta = await cliente.Codigo_PuntoDeVentaCliente_Segun_Direccion(_context, ubicacionCliente.codcliente, ubicacionCliente.dircliente);
                var ubicacion = await cliente.Ubicacion_PtoVenta(_context, codPtoVenta);
                return Ok(new { ubi = ubicacion });
            }
        }

        // GET: api/post_quitar_descuento_por_deposito/5
        [HttpPost]
        [Route("reqstQuitarDescDeposito/{userConn}/{codempresa}")]
        public async Task<ActionResult<object>> reqstQuitarDescDeposito(string userConn, string codempresa, List<tabladescuentos> tabladescuentos)
        {
            try
            {
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);
                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    tabladescuentos = await borrar_descuento_por_deposito(_context, codempresa, tabladescuentos);
                    return Ok(new { tabladescuentos });
                }
            }
            catch (Exception ex)
            {
                return Problem("Error en el servidor al quitar desc. por deposito PF : " + ex.Message);
                throw;
            }
        }

        // GET: api/aplicar_descuento_por_deposito/5
        [HttpPost]
        [Route("aplicar_descuento_por_deposito/{userConn}/{codcliente}/{codcliente_real}/{nit}/{codempresa}/{subtotal}/{codmoneda}/{codproforma}")]
        public async Task<ActionResult<object>> aplicar_descuento_por_deposito(string userConn, string codcliente, string codcliente_real, string nit, string codempresa, double subtotal, string codmoneda, int codproforma, objetoDescDepositos objetoDescDepositos)
        {
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);
            
            try
            {
                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    // primero borrar el descuento por deposito
                    List<tabladescuentos> tabladescuentos = objetoDescDepositos.tabladescuentos;
                    tabladescuentos = await borrar_descuento_por_deposito(_context, codempresa, tabladescuentos);


                    // si el cliente referencia no es el mismo al cliente al cual saldra el pedido
                    // entonces no se busca desctos por deposito
                    // de acuerdo a la nueva politica de desctos, vigente desde el 01-08-2022
                    if (codcliente != codcliente_real)
                    {
                        return StatusCode(203, new { respOculto = "Cliente referencia no es el mismo que el cliente del pedido." });
                    }
                    getTarifaPrincipal_Rodrigo data = objetoDescDepositos.getTarifaPrincipal;


                    // clientes casualas no deben tener descto por deposito seg/poliita desde el 01-08-2022
                    if (await cliente.Es_Cliente_Casual(_context, codcliente))
                    {
                        return StatusCode(203, new { respOculto = "Cliente es casual, no puede tener descuento por depósito." });
                    }
                    // verificar si es cliente competencia
                    if (await cliente.EsClienteCompetencia(_context, nit))
                    {
                        return StatusCode(203, new { respOculto = "Cliente es cliente competencia, no puede tener descuento por depósito." });
                    }
                    // verificar que los desctos esten habilitados para el precio principal de la proforma
                    var coddesextra_deposito = await configuracion.emp_coddesextra_x_deposito(_context, codempresa);
                    var tarifa_main = await validar_Vta.Tarifa_Monto_Min_Mayor_Rodrigo(_context, await validar_Vta.Lista_Precios_En_El_Documento(data.tabladetalle), data.DVTA);
                    if (await ventas.Descuento_Extra_Habilitado_Para_Precio(_context, coddesextra_deposito, tarifa_main) == false)
                    {
                        return StatusCode(203, new { respOculto = "El descuento no esta habilitado para el precio principal de la proforma." });
                    }

                    // la aplicacion del descuento por deposito no esta permitido para 
                    // proformas con codigo de cliente sin nombre

                    // +++++SE QUITO LA RESTRICCION DE NO PERMITIR APLICAR DESCTOS POR DEPOSITOS A VENTAS CONTADO SIN NOMBRE EN FECHA 15-05-2019

                    // If Not sia_funciones.Cliente.Instancia.EsClienteSinNombre(codcliente.Text, False) Then
                    // verificacion si le corresponde descuento por deposito
                    DateTime Depositos_Desde_Fecha = await configuracion.Depositos_Nuevos_Desde_Fecha(_context);
                    bool buscar_por_nit = false;
                    if (await cliente.EsClienteSinNombre(_context, codcliente))
                    {
                        buscar_por_nit = true;
                    }

                    /////////////////////////////////////////////////////////////////////////////////////////////
                    // DEPOSITOS PENDIENTE DE CBZAS CREDITO
                    var dt_depositos_pendientes = await cobranzas.Depositos_Cobranzas_Credito_Cliente_Sin_Aplicar(_context, "cliente", "", codcliente, nit, codcliente_real, buscar_por_nit, "APLICAR_DESCTO", codproforma, "Proforma_Nueva", codempresa, false, Depositos_Desde_Fecha, true);

                    foreach (var reg in dt_depositos_pendientes)
                    {
                        reg.tipo_pago = "es_cbza_credito";
                        if (reg.tipo == 0)
                        {
                            // la moneda de pago igual a dela cobrzna
                            reg.monpago = reg.moncbza;
                        }
                        else
                        {
                            reg.monpago = await cobranzas.Moneda_De_Pago_de_una_Cobranza2(_context, reg.codcobranza, reg.codremision);
                        }
                    }
                    var dt_credito_depositos_pendientes = await cobranzas.Totalizar_Cobranzas_Depositos_Pendientes(_context, dt_depositos_pendientes);

                    /////////////////////////////////////////////////////////////////////////////////////////////


                    /*

                    '/////////////////////////////////////////////////////////////////////////////////////////////
                    '//DEPOSITOS PENDIENTE DE CBZAS CONTADO
                    'dt_depositos_pendientes.Clear()
                    'dt_depositos_pendientes = sia_funciones.Cobranzas.Instancia.Depositos_Cobranzas_Contado_Cliente_Sin_Aplicar(codcliente_real, codigo.Text, "Proforma_Nueva", sia_compartidos.temporales.Instancia.codempresa)
                    'If Not dt_depositos_pendientes.Columns.Contains("tipo_pago") Then dt_depositos_pendientes.Columns.Add("tipo_pago", System.Type.GetType("System.String"))
                    'For y As Integer = 0 To dt_depositos_pendientes.Rows.Count - 1
                    '    dt_depositos_pendientes.Rows(y)("tipo_pago") = "es_cbza_contado"
                    'Next

                    'dt_contado_depositos_pendientes.Clear()
                    'dt_contado_depositos_pendientes = sia_funciones.Cobranzas.Instancia.Totalizar_Cobranzas_Depositos_Pendientes(dt_depositos_pendientes)
                    '/////////////////////////////////////////////////////////////////////////////////////////////



                    '/////////////////////////////////////////////////////////////////////////////////////////////
                    '//DEPOSITOS PENDIENTES DE ANTICIPOS PARA VENTAS CONTADO APLICADOS EN PROFORMAS DIRECTAMENTE
                    'dt_depositos_pendientes.Clear()
                    'dt_depositos_pendientes = sia_funciones.Cobranzas.Instancia.Depositos_Anticipos_Contado_Cliente_Sin_Aplicar(codcliente_real, 0, "Proforma_Nueva", sia_compartidos.temporales.Instancia.codempresa)
                    'If Not dt_depositos_pendientes.Columns.Contains("tipo_pago") Then dt_depositos_pendientes.Columns.Add("tipo_pago", System.Type.GetType("System.String"))
                    'For y As Integer = 0 To dt_depositos_pendientes.Rows.Count - 1
                    '    dt_depositos_pendientes.Rows(y)("tipo_pago") = "es_anticipo_contado"
                    'Next
                    'dt_anticipos_depositos_pendientes.Clear()
                    'dt_anticipos_depositos_pendientes = sia_funciones.Cobranzas.Instancia.Totalizar_Cobranzas_Depositos_Pendientes(dt_depositos_pendientes)
                    '/////////////////////////////////////////////////////////////////////////////////////////////

                     */


                    // esta instruccion es para copiar la estructura de una de las tablas a la tabla final de resultado: dt_depositos_pendientes

                    /*
                     If dt_credito_depositos_pendientes.Rows.Count > 0 Then
                        dt_depositos_pendientes = dt_credito_depositos_pendientes.Copy
                        dt_depositos_pendientes.Clear()
                    ElseIf dt_contado_depositos_pendientes.Rows.Count > 0 Then
                        dt_depositos_pendientes = dt_contado_depositos_pendientes.Copy
                        dt_depositos_pendientes.Clear()
                    ElseIf dt_anticipos_depositos_pendientes.Rows.Count > 0 Then
                        dt_depositos_pendientes = dt_anticipos_depositos_pendientes.Copy
                        dt_depositos_pendientes.Clear()
                    End If
                     */
                    // ***********************************************************************************************************
                    // ********************************UNIR EN UNA SOLA TABLA***************************************************
                    // ***********************************************************************************************************
                    // copiar los depositos pendientes de cbzas credito
                    /*For h As Integer = 0 To dt_credito_depositos_pendientes.Rows.Count - 1
                        dt_depositos_pendientes.ImportRow(dt_credito_depositos_pendientes.Rows(h))
                    Next
                    */



                    // DE MOMENTO SE PUEDE UTILIZAR SOLO ESTO: 
                    ////////////////////////////////////////// dt_credito_depositos_pendientes //////////////////////////////////////////////////////



                    // copiar los depositos pendientes de cbzas contado
                    /*For h As Integer = 0 To dt_contado_depositos_pendientes.Rows.Count - 1
                        dt_depositos_pendientes.ImportRow(dt_contado_depositos_pendientes.Rows(h))
                    Next
                    */
                    // copiar los depositos de anticipos contado aplicados a proforma
                    /*For h As Integer = 0 To dt_anticipos_depositos_pendientes.Rows.Count - 1
                        dt_depositos_pendientes.ImportRow(dt_anticipos_depositos_pendientes.Rows(h))
                    Next
                    */
                    /*
                    //***********************************************************************************************************
                    'Desde 17-04-2023 Se debe añadir un nuevo campo monpago para determinar con ese tipo de moneda el descueto por deposito
                    'ese campo monpago es la moneda con q se realizo el pago y dio el descuento por deposito respectivo
                    'If Not dt_depositos_pendientes.Columns.Contains("monpago") Then
                    '    'Desde 17-04-2023
                    '    dt_depositos_pendientes.Columns.Add("monpago", System.Type.GetType("System.String"))
                    'End If
                    'Dim j As Integer = 0
                    'Dim reg_1 As DataRow
                    'For j = 0 To dt_depositos_pendientes.Rows.Count - 1
                    '    reg_1 = dt_depositos_pendientes.Rows(j)
                    '    reg_1("monpago") = sia_funciones.Cobranzas.Instancia.Moneda_De_Pago_de_una_Cobranza2(reg_1("codcobranza"), reg_1("monto_dis"))
                    'Next
                     */
                    string message = "";
                    if (dt_credito_depositos_pendientes.Count() > 0)
                    {
                        // message = "El cliente tiene descuentos por deposito pendientes de aplicacion, desea aplicar el descuento a esta proforma?";
                        message = "El cliente tiene descuentos por deposito pendientes de aplicacion, se aplicaran a esta proforma";
                    }

                    var seAplicoDsctoPorDeposito = await ventas.AdicionarDescuentoPorDeposito(_context, subtotal, codmoneda, tabladescuentos, dt_credito_depositos_pendientes, objetoDescDepositos.tblcbza_deposito, codproforma, codcliente_real, codempresa);

                    if (seAplicoDsctoPorDeposito.bandera)
                    {
                        // devolver mensaje, data de descuentos y mensaje grande de Descuentos por deposito llamado desde una clase
                        var cadena_msg = await cobranzas.mostrar_mensajes_depositos_aplicar(_context, codempresa, dt_depositos_pendientes, seAplicoDsctoPorDeposito.tabladescuentos);
                        return Ok(new
                        {
                            tabladescuentos = seAplicoDsctoPorDeposito.tabladescuentos,
                            msgDesctApli = seAplicoDsctoPorDeposito.mensaje,
                            msgVentCob = cadena_msg,
                            megAlert = message
                        });
                    }
                    return Ok(new
                    {
                        tabladescuentos = tabladescuentos,
                        msgDesctApli = seAplicoDsctoPorDeposito.mensaje,
                        msgVentCob = "",
                        megAlert = "No se encontraron descuentos por deposito pendientes de aplicación."
                    });
                }
            }
            catch (Exception ex)
            {
                return Problem("Error en el servidor al aplicar descuento por Deposito: " + ex.Message);
                throw;
            }
        }


        // GET: api/get_entrega_pedido/5
        [HttpPost]
        [Route("get_entrega_pedido/{userConn}/{codempresa}")]
        public async Task<ActionResult<object>> get_entrega_pedido(string userConn,string codempresa, RequestEntregaPedido RequestEntregaPedido)
        {
            try
            {
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);
                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var objres = await validar_Vta.Validar_Monto_Minimo_Para_Entrega_Pedido(_context, RequestEntregaPedido.datosDocVta, RequestEntregaPedido.detalleItemsProf, codempresa);
                    if (objres.resultado == false)
                    {
                        return Ok(new { mensaje = "RECOGE CLIENTE" });
                    }
                    // tipoentrega.Text = "ENTREGAR"
                    if (RequestEntregaPedido.preparacion == "CAJA CERRADA RECOGE CLIENTE")
                    {
                        return Ok(new { mensaje = "RECOGE CLIENTE" });
                    }
                    return Ok(new { mensaje = "ENTREGAR" });
                }
            }
            catch (Exception ex)
            {
                return Problem("Error en el servidor : " + ex.Message);
                throw;
            }
        }





        //private async Task<List<veproforma_iva>> CalcularTablaIVA(double subtotal, double recargos, double descuentos, List<itemDataMatriz> tabladetalle)
        [HttpPost]
        [Route("empaquesCerradosVerifica/{userConn}/{codcliente}")]
        public async Task<ActionResult<object>> empaquesCerradosVerifica(string userConn, string codcliente, List<itemDataMatriz> tabladetalle)
        {
            try
            {
                tabladetalle.ForEach(item => item.cumpleEmp = true);
                tabladetalle.ForEach(item => item.cumpleMin = true);
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);
                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (tabladetalle.Count() > 0)
                    {
                        bool cumple = true;
                        foreach (var reg in tabladetalle)
                        {
                            if (await ventas.Tarifa_EmpaqueCerrado(_context,reg.codtarifa))
                            {
                                if (await ventas.CumpleEmpaqueCerrado(_context,reg.coditem, reg.codtarifa, reg.coddescuento, (decimal)reg.cantidad, codcliente))
                                {
                                    reg.cumpleEmp = true;
                                }
                                else
                                {
                                    reg.cumpleEmp = false;
                                    cumple = false;
                                }
                            }
                        }
                        if (cumple)
                        {
                            return Ok(new { reg = "Todos los items del documento cumplen con empaques cerrados.", cumple = cumple, tabladetalle = tabladetalle });
                        }
                        return Ok(new { reg = "Hay algunos items que no cumplen los empaques cerrados.", cumple = cumple, tabladetalle = tabladetalle });
                    }
                    return BadRequest(new { reg = "Debe seleccionar items para poder validar los empaques cerrados."});
                }
            }
            catch (Exception ex)
            {
                return Problem("Error en el servidor : " + ex.Message);
                throw;
            }
        }


        //private async Task<List<veproforma_iva>> CalcularTablaIVA(double subtotal, double recargos, double descuentos, List<itemDataMatriz> tabladetalle)
        [HttpPost]
        [Route("empaquesMinimosVerifica/{userConn}/{codcliente}/{codalmacen}")]
        public async Task<ActionResult<object>> empaquesMinimosVerifica(string userConn, string codcliente, int codalmacen, List<itemDataMatriz> tabladetalle)
        {
            try
            {
                tabladetalle.ForEach(item => item.cumpleEmp = true);
                tabladetalle.ForEach(item => item.cumpleMin = true);
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);
                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (tabladetalle.Count() > 0)
                    {
                        var valida = await validar_Vta.Validar_Resaltar_Empaques_Minimos_Segun_Lista_Precios(_context, tabladetalle, codalmacen, codcliente);
                        if (valida.cumple)
                        {
                            return Ok(new { reg = "Todo el documento cumple con los empaques minimos de Precio y descuento.", cumple = valida.cumple, tabladetalle = valida.tabladetalle });
                        }
                        return Ok(new { reg = "Hay algunos items que no cumplen el empaque minimo de Precio y descuento.", cumple = valida.cumple, tabladetalle = valida.tabladetalle });
                    }
                    return BadRequest(new { reg = "Debe seleccionar items para poder validar los empaques cerrados." });
                }
            }
            catch (Exception ex)
            {
                return Problem("Error en el servidor : " + ex.Message);
                throw;
            }
        }

        // boton dividir por empaques 
        [HttpPost]
        [Route("aplicar_dividir_items/{userConn}/{codempresa}/{usuario}/{codcliente}/{opcion_nivel}/{codalmacen}/{desc_linea_seg_solicitud}/{codmoneda}/{fecha}")]
        public async Task<ActionResult<List<itemDataMatriz>>> aplicar_dividir_items(string userConn, string codempresa, string usuario, string codcliente, string opcion_nivel, int codalmacen, string desc_linea_seg_solicitud, string codmoneda, DateTime fecha,  List<itemDataMatriz> tabladetalle)
        {
            try
            {
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);
                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    List<itemDataMatriz> dt = new List<itemDataMatriz>();
                    int codempaque_permite_item_repetido = await configuracion.codempaque_permite_item_repetido(_context,codempresa);
                    // obtener los tipos de precio
                    int j = 0;
                    foreach (var reg in tabladetalle)
                    {
                        var qry_cantidad = await _context.veempaque1
                            .Join(_context.vedescuento,
                                e => e.codempaque,
                                d => d.codempaque,
                                (e, d) => new { e, d })
                            .Where(ed => ed.e.item == reg.coditem &&
                                         ed.d.codempaque == codempaque_permite_item_repetido &&
                                         _context.vedescuento_tarifa
                                             .Where(dt => dt.codtarifa == reg.codtarifa)
                                             .Select(dt => dt.coddescuento)
                                             .Contains(ed.d.codigo))
                            .Select(ed => ed.e.cantidad)
                            .FirstOrDefaultAsync();

                        double cantidad = reg.cantidad;
                        double empaque_descuento = funciones.LimpiarDoble(qry_cantidad);
                        bool cumple = ventas.CantidadCumpleEmpaque(_context, (decimal)cantidad, (decimal)empaque_descuento, (decimal)empaque_descuento, await ventas.Tarifa_PermiteEmpaquesMixtos(_context, reg.codtarifa));
                        if (!cumple)
                        {
                            // verificar si la cantidad es mayor a un empaque caja cerrada
                            if (cantidad > empaque_descuento)
                            {
                                // verificar el multiplo del empaque del item y dividir las cantidades del item en 2 items repetidos, donde en el primer item se pondra la cantidad de empaques cerrados
                                // segun la operacion cantidad mod empaque cerrado = numero de empaques, para luego ese resultado sera la cantidad de empaques, para posterior multiplicar esa cantidad
                                // por un empaque de caja cerrada, y en el segundo item se pondra el resultado de esta operacione menos la cantidad solcitiada por el usuario
                                int cant_empaques = (int)(cantidad / empaque_descuento);
                                double cant_item_empaque = cant_empaques * empaque_descuento;
                                double cant_item_sin_empaque = cantidad - cant_item_empaque;
                                // en el detalle modificar la cantidad del item por la variable cant_item_empaque y luego añadir a un datable el item en cuestion con la cantidad = cant_item_sin_empaque
                                reg.cantidad = (double)cant_item_empaque;
                                itemDataMatriz dt_aux = new itemDataMatriz();
                                // copiar datos importantes
                                dt_aux.coditem = reg.coditem;
                                dt_aux.codtarifa = reg.codtarifa;
                                dt_aux.coddescuento = reg.coddescuento;
                                dt_aux.cantidad = (double)cant_item_sin_empaque;
                                dt_aux.cantidad_pedida = (double)cant_item_sin_empaque;
                                dt_aux.nroitem = reg.nroitem;
                                dt.Add(dt_aux);
                                
                            }
                            
                        }
                    }
                    string msgAlert = "";
                    if (dt.Count() > 0)
                    {
                        msgAlert = "Se tienen items q se dividiran en dos items repetidos para optar por el descuento caja cerrada.";
                    }
                    var data = dt
                        .Select(i => new cargadofromMatriz
                        {
                            coditem = i.coditem,
                            tarifa = i.codtarifa,
                            descuento = i.coddescuento,
                            cantidad_pedida = (decimal)i.cantidad_pedida,
                            cantidad = (decimal)i.cantidad,
                            codcliente = codcliente,
                            opcion_nivel = opcion_nivel,
                            codalmacen = codalmacen,
                            desc_linea_seg_solicitud = desc_linea_seg_solicitud,
                            codmoneda = codmoneda,
                            fecha = fecha,
                            nroitem = i.nroitem
                        }).ToList();

                    var tablaDetalleExtra = await calculoPreciosMatriz(_context, codempresa, usuario, userConnectionString, data,false);

                    if (tablaDetalleExtra == null)
                    {
                        return StatusCode(203,new { resp = "Los items ya cumplen con empaques, no se dividiran." });
                    }
                    tabladetalle = tabladetalle.Concat(tablaDetalleExtra).OrderBy(i=>i.coditem).ThenByDescending(i=> i.coddescuento).ToList();

                    return Ok(new
                    {
                        alertMsg = msgAlert,
                        tabladetalle = tabladetalle
                    });

                }
            }
            catch (Exception ex)
            {
                return Problem("Error en el servidor al dividir items : " + ex.Message);
                throw;
            }
        }


        // boton dividir por empaques 
        [HttpPost]
        [Route("aplicar_desc_esp_seg_precio/{userConn}")]
        public async Task<ActionResult<List<itemDataMatriz>>> aplicar_desc_esp_seg_precio(string userConn, List<itemDataMatriz> tabladetalle)
        {
            // obtener los tipos de precio
            try
            {
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);
                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    List<int> lista_desc_especial = new List<int>();
                    List<string> datos_precios = new List<string>();
                    foreach (var reg in tabladetalle)
                    {
                        int coddescto_esp = 0;
                        var dt = await _context.vedescuento_tarifa
                            .Where(tarifa => tarifa.codtarifa == reg.codtarifa &&
                                             tarifa.coddescuento != 0 &&
                                             _context.vedescuento
                                                    .Where(descuento => descuento.habilitado == true)
                                                    .Select(descuento => descuento.codigo)
                                                    .Contains(tarifa.coddescuento))
                            .FirstOrDefaultAsync();
                        if (dt != null)
                        {
                            coddescto_esp = dt.coddescuento;
                            if (!lista_desc_especial.Contains(dt.coddescuento))
                            {
                                lista_desc_especial.Add(dt.coddescuento);
                            }
                        }
                        else
                        {
                            // si no hay descto asignado al precio aw asigna 0 osea sin descto
                            coddescto_esp = 0;
                        }
                        string cadena = reg.codtarifa.ToString() + "-" + coddescto_esp.ToString();
                        if (!datos_precios.Contains(cadena))
                        {
                            datos_precios.Add(cadena);
                        }
                    }

                    string[] datos_des = new string[2];
                    foreach (var reg in tabladetalle)
                    {
                        reg.coddescuento = 0;

                        for (int j = 0; j < datos_precios.Count; j++)
                        {
                            datos_des = datos_precios[j].Split(new char[] { '-' });
                            if (datos_des[0] == reg.codtarifa.ToString())
                            {
                                reg.coddescuento = int.Parse(datos_des[1]);
                            }
                        }
                    }

                    // verificar los desctos especiales que no estan validos
                    string cadena_desEsp_validos = "";
                    foreach (var reg in lista_desc_especial)
                    {
                        if (!await ventas.Descuento_Especial_Habilitado(_context,reg))
                        {
                            cadena_desEsp_validos = cadena_desEsp_validos + "El descuento especial: " + reg + "-" + await nombres.nombre_descuento_especial(_context, reg) + " esta deshabilitado!!! \r\n";
                        }
                    }
                    if (cadena_desEsp_validos.Trim().Length > 0)
                    {
                        return Ok(new
                        {
                            msgTitulo = "Se tiene observaciones en la aplicacion de los descuentos especiales aplicados: ",
                            msgDetalle = cadena_desEsp_validos,
                            tabladetalle = tabladetalle
                        });
                    }
                    return Ok(new
                    {
                        msgTitulo = "",
                        msgDetalle = cadena_desEsp_validos,
                        tabladetalle = tabladetalle
                    });
                }
            }
            catch (Exception ex)
            {
                return Problem("Error en el servidor al aplicar descuento especial segun precio: " + ex.Message);
                throw;
            }
        }

        // boton dividir por empaques 
        [HttpPost]
        [Route("valEmpDescEsp/{userConn}")]
        public async Task<ActionResult<List<object>>> valEmpDescEsp(string userConn, requestRecargaDetalle requestRecargaDetalle)
        {
            try
            {
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);
                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    string codempresa = requestRecargaDetalle.codempresa;
                    string usuario = requestRecargaDetalle.usuario;
                    int codalmacen = requestRecargaDetalle.codalmacen;
                    string codcliente_real = requestRecargaDetalle.codcliente_real;
                    string codcliente = requestRecargaDetalle.codcliente;
                    string opcion_nivel = requestRecargaDetalle.opcion_nivel;
                    string desc_linea_seg_solicitud = requestRecargaDetalle.desc_linea_seg_solicitud;
                    string codmoneda = requestRecargaDetalle.codmoneda;
                    DateTime fecha = requestRecargaDetalle.fecha;
                    List<itemDataMatriz> tabladetalle = requestRecargaDetalle.tabladetalle;

                    var result = await validar_empaques_caja_cerrada(_context, userConnectionString, codempresa, usuario, codalmacen, codcliente_real, codcliente, opcion_nivel, desc_linea_seg_solicitud, codmoneda, fecha, tabladetalle);
                    if (result.val)
                    {
                        return Ok(new
                        {
                            cumple = result.val,
                            msg = "Todo el documento cumple con los empaques minimos o multimplos del descuento aplicado.",
                            tabladetalle = result.tabladetalle
                        });
                    }
                    return Ok(new
                    {
                        cumple = result.val,
                        msg = "Hay algunos items que no cumplen el empaque minimo o multiplos del descuento que se aplico, por lo cual se  quito el descuento de estos items.",
                        tabladetalle = result.tabladetalle
                    });
                }
            }
            catch (Exception ex)
            {
                return Problem("Error en el servidor al validar descuento especial por empaque: " + ex.Message);
                throw;
            }
        }

        private async Task<(bool val, List<itemDataMatriz> tabladetalle)> validar_empaques_caja_cerrada(DBContext _context, string userConnectionString, string codempresa, string usuario, int codalmacen, string codcliente_real, string codcliente, string opcion_nivel, string desc_linea_seg_solicitud, string codmoneda, DateTime fecha, List<itemDataMatriz> tabladetalle)
        {
            if (tabladetalle.Count() > 0)
            {
                bool quitar_descuento = true;
                var resultados = await validar_Vta.Validar_Resaltar_Empaques_Caja_Cerrada_DesctoEspecial(_context, tabladetalle, codalmacen, quitar_descuento, codcliente_real);
                var data = resultados.tabladetalle
                        .Select(i => new cargadofromMatriz
                        {
                            coditem = i.coditem,
                            tarifa = i.codtarifa,
                            descuento = i.coddescuento,
                            cantidad_pedida = (decimal)i.cantidad_pedida,
                            cantidad = (decimal)i.cantidad,
                            codcliente = codcliente,
                            opcion_nivel = opcion_nivel,
                            codalmacen = codalmacen,
                            desc_linea_seg_solicitud = desc_linea_seg_solicitud,
                            codmoneda = codmoneda,
                            fecha = fecha,
                            cumpleMin = i.cumpleMin
                        }).ToList();

                var tablaDetalleNew = await calculoPreciosMatriz(_context, codempresa, usuario, userConnectionString, data, false);
                return (resultados.result, tablaDetalleNew);
            }
            return (false, tabladetalle);
        }


        [HttpPost]
        [Route("sugerirCantDescEsp/{userConn}/{coddescuentodefect}/{codalmacen}/{codempresa}")]
        public async Task<ActionResult<object>> sugerirCantDescEsp(string userConn, int coddescuentodefect, int codalmacen, string codempresa, List<itemDataMatriz> tabladetalle)
        {
            try
            {
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);
                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var result = await validar_sugerir_cantidades_empaques_caja_cerrada(_context, coddescuentodefect, codalmacen, codempresa, tabladetalle);
                    if (result.tabla_sugerencia == null)
                    {
                        return StatusCode(203, new { resp = result.val, status = false });
                        /*
                        return BadRequest(new
                        {
                            msgDetalle = result.val,
                        });*/
                    }
                    return Ok(new
                    {
                        msgDetalle = result.val,
                        tabla_sugerencia = result.tabla_sugerencia,
                        tabladetalle = result.tabladetalle
                    });
                }
            }
            catch (Exception ex)
            {
                return Problem("Error en el servidor : " + ex.Message);
                throw;
            }
        }


        private async Task<(string val, List<itemDataSugerencia>? tabla_sugerencia, List<itemDataMatriz> tabladetalle)> validar_sugerir_cantidades_empaques_caja_cerrada(DBContext _context, int coddescuentodefect, int codalmacen, string codempresa, List<itemDataMatriz> tabladetalle)
        {
            if (coddescuentodefect==0)
            {
                return ("Debe seleccionar el codigo de descuento valido.", null, tabladetalle);
            }
            if (tabladetalle.Count() > 0)
            {
                var resultados = await validar_Vta.Sugerir_Cantidades_Empaques_Caja_Cerrada_DesctoEspecial(_context, tabladetalle, codalmacen, coddescuentodefect, codempresa);
                var dt_items_sugerencia = resultados.tabla_sugerencia;
                tabladetalle = resultados.tabladetalle;


                if (dt_items_sugerencia.Count() > 0)
                {
                    return ("Hay algunos items que no cumplen el empaque minimo o multiplos del descuento que se aplico, por lo cual se  quito el descuento de estos items.", dt_items_sugerencia, tabladetalle);
                }
                return ("Todo el documento cumple con los empaques minimos o multimplos del descuento aplicado.", dt_items_sugerencia, tabladetalle);

            }
            return ("No se tiene items en la tabla detalle, verifique esta stuación.", null,tabladetalle);
        }

        // boton de ultimas proformas
        [HttpGet]
        [Route("ultimasProformas/{userConn}/{codcliente_real}/{codcliente}/{usuario}")]
        public async Task<ActionResult<List<object>>> ultimasProformas(string userConn, string codcliente_real, string codcliente, string usuario)
        {
            try
            {
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);
                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (codcliente_real.Trim() == "")
                    {
                        return BadRequest(new { resp = "No se ingreso codigo de cliente Real." });
                    }
                    if (await seguridad.autorizado_vendedores(_context, usuario, await cliente.Vendedor_de_cliente(_context,codcliente), await cliente.Vendedor_de_cliente(_context, codcliente)))
                    {
                        ////////////////////////////////////////////////////////////////////
                        // PROFORMAS NO APAPROBADAS NO ANULADAS
                        ////////////////////////////////////////////////////////////////////
                        //solo tomar  las ultimas 10 proformas

                        var dt_last_pf = await _context.veproforma
                            .Where(p => p.anulada == false && p.codcliente_real == codcliente_real)
                            .OrderByDescending(p => p.fecha)
                            .Select(p => new {
                                p.codigo,
                                p.id,
                                p.numeroid,
                                p.codcliente,
                                p.codcliente_real,
                                p.nomcliente,
                                p.nit,
                                p.fecha,
                                p.total,
                                p.codmoneda,
                                p.aprobada,
                                p.transferida,
                                p.usuarioreg,
                                p.fechareg,
                                p.horareg
                            })
                            .Take(10)
                            .ToListAsync();
                        return Ok(dt_last_pf);
                    }
                    return BadRequest(new { resp = "No autorizado para ver esta información." });
                }

            }
            catch (Exception ex)
            {
                return Problem("Error en el servidor al obtener ultimas proformas: " + ex.Message);
                throw;
            }

        }

        [HttpGet]
        [Route("cargarPFVtaDias/{userConn}/{coditem}/{codempresa}/{codcliente_real}/{diascontrol}")]
        public async Task<ActionResult<List<object>>> Cargar_PF_Vta_Dias(string userConn, string coditem, string codempresa, string codcliente_real, int diascontrol)
        {
            try
            {
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);
                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    DateTime fecha_serv = DateTime.Today;
                    string valida_nr_pf = await configuracion.Valida_Maxvta_NR_PF(_context, codempresa);
                    if (coditem.Trim() == "")
                    {
                        return BadRequest(new { resp = "Debe seleccionar un item." });
                    }
                    /*
                    if (valida_nr_pf == "PF")
                    {
                        var dt_items_vta_dias = await _context.veproforma
                            .Join(_context.veproforma1,
                                c => c.codigo,
                                d => d.codproforma,
                                (c, d) => new { c, d })
                            .Where(joined => joined.c.anulada == false && joined.c.codcliente_real == codcliente_real &&
                                joined.c.aprobada == true && joined.c.fecha >= fecha_serv.AddDays(-diascontrol) && joined.c.fecha <= fecha_serv &&
                                joined.d.coditem == coditem)
                            .Select(joined => new
                            {
                                joined.c.codigo,
                                joined.c.id,
                                joined.c.numeroid,
                                joined.c.codcliente,
                                joined.c.codcliente_real,
                                joined.c.nomcliente,

                                joined.c.nit,
                                joined.c.fecha,
                                joined.c.total,
                                joined.d.coditem,
                                joined.d.cantidad,
                                joined.c.codmoneda,

                                joined.c.aprobada,
                                joined.c.transferida
                            })
                            .OrderBy(result => result.fecha)
                            .ToListAsync();
                    }
                    else */
                    if(valida_nr_pf == "NR")
                    {
                        var dt_items_vta_dias = await _context.veremision
                            .Join(_context.veremision1,
                                c => c.codigo,
                                d => d.codremision,
                                (c, d) => new { c, d })
                            .Where(joined => joined.c.anulada == false && joined.c.codcliente_real == codcliente_real &&
                                joined.c.transferida == true && joined.c.fecha >= fecha_serv.AddDays(-diascontrol) && joined.c.fecha <= fecha_serv &&
                                joined.d.coditem == coditem)
                            .Select(joined => new
                            {
                                joined.c.codigo,
                                joined.c.id,
                                joined.c.numeroid,
                                joined.c.codcliente,
                                joined.c.codcliente_real,
                                joined.c.nomcliente,

                                joined.c.nit,
                                joined.c.fecha,
                                joined.c.total,
                                joined.d.coditem,
                                joined.d.cantidad,
                                joined.c.codmoneda,

                                aprobada = joined.c.transferida,
                                joined.c.transferida
                            })
                            .OrderBy(result => result.fecha)
                            .ToListAsync();
                        return Ok(dt_items_vta_dias);
                    }
                    else
                    {
                        var dt_items_vta_dias = await _context.veproforma
                            .Join(_context.veproforma1,
                                c => c.codigo,
                                d => d.codproforma,
                                (c, d) => new { c, d })
                            .Where(joined => joined.c.anulada == false && joined.c.codcliente_real == codcliente_real &&
                                joined.c.aprobada == true && joined.c.fecha >= fecha_serv.AddDays(-diascontrol) && joined.c.fecha <= fecha_serv &&
                                joined.d.coditem == coditem)
                            .Select(joined => new
                            {
                                joined.c.codigo,
                                joined.c.id,
                                joined.c.numeroid,
                                joined.c.codcliente,
                                joined.c.codcliente_real,
                                joined.c.nomcliente,

                                joined.c.nit,
                                joined.c.fecha,
                                joined.c.total,
                                joined.d.coditem,
                                joined.d.cantidad,
                                joined.c.codmoneda,

                                joined.c.aprobada,
                                joined.c.transferida
                            })
                            .OrderBy(result => result.fecha)
                            .ToListAsync();
                        return Ok(dt_items_vta_dias);
                    }
                }
            }
            catch (Exception ex)
            {
                return Problem("Error en el servidor : " + ex.Message);
                throw;
            }
        }

        [HttpGet]
        [Route("getDiasControl/{userConn}/{codempresa}")]
        public async Task<ActionResult<List<object>>> getDiasControl(string userConn, string codempresa)
        {
            try
            {
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);
                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var diascontrol = await configuracion.Dias_Proforma_Vta_Item_Cliente(_context, codempresa);
                    return Ok(new { diascontrol  = diascontrol });
                }

            }
            catch (Exception ex)
            {
                return Problem("Error en el servidor : " + ex.Message);
                throw;
            }
        }
        

        [HttpPost]
        [Route("recuperarPfComplemento/{userConn}/{idpf_complemento}/{nroidpf_complemento}/{cmbtipo_complementopf}/{codempresa}")]
        public async Task<ActionResult<List<object>>> recuperar_pfcomplemento(string userConn, string idpf_complemento, int nroidpf_complemento, int cmbtipo_complementopf, string codempresa, RequestEntregaPedido data)
        {
            try
            {
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);
                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    ResultadoValidacion objres = new ResultadoValidacion();
                    // if cmbtipo_complementopf.SelectedIndex = 0
                    if (cmbtipo_complementopf == 0)
                    {
                        // validar complementar DIMEDIDADO CON MAYORISTA
                        objres = await validar_Vta.Validar_Enlace_Proforma_Mayorista_Dimediado(_context, data.detalleItemsProf, data.datosDocVta, codempresa);
                        if (objres.resultado == false)
                        {
                            return StatusCode(203, new { resp = "No se puede realizar el enlace!!!", value = false, detalle = objres.obsdetalle });
                            /*
                             resultado = False
                             chkcomplementarpf.Checked = False
                             */
                        }
                        else
                        {
                            int codproforma = await ventas.codproforma(_context, idpf_complemento, nroidpf_complemento);
                            var subtotal_pfcomplemento = await ventas.SubTotal_Proforma(_context, codproforma);
                            var total_pfcomplemento = await ventas.Total_Proforma(_context, codproforma);
                            var moneda_total_pfcomplemento = await ventas.MonedaPF(_context,codproforma);
                            var fechaaut_pfcomplemento = await ventas.Fecha_Autoriza_de_Proforma(_context, idpf_complemento, nroidpf_complemento);
                            return Ok(new
                            {
                                subtotal_pfcomplemento = subtotal_pfcomplemento,
                                total_pfcomplemento = total_pfcomplemento,
                                moneda_total_pfcomplemento = moneda_total_pfcomplemento,
                                fechaaut_pfcomplemento = fechaaut_pfcomplemento
                            });

                        }
                    }else if(cmbtipo_complementopf == 1)
                    {
                        // VALIDAR CUMPLIR MONTO MIN PARA DESCTO POR IMPORTE
                        objres = await validar_Vta.Validar_Complementar_Proforma_Para_Descto_Extra(_context,data.detalleItemsProf, data.datosDocVta, codempresa);
                        if (objres.resultado == false)
                        {
                            return StatusCode(203, new { resp = "No se puede realizar el enlace!!!", value = false, detalle = objres.obsdetalle });

                            //return BadRequest(new { resp = "No se puede realizar el enlace!!!", value = false, detalle = objres.obsdetalle });
                            /*
                             resultado = False
                             chkcomplementarpf.Checked = False
                             */
                        }
                        else
                        {
                            int codproforma = await ventas.codproforma(_context, idpf_complemento, nroidpf_complemento);
                            var subtotal_pfcomplemento = await ventas.SubTotal_Proforma(_context, codproforma);
                            var total_pfcomplemento = await ventas.Total_Proforma(_context, codproforma);
                            var moneda_total_pfcomplemento = await ventas.MonedaPF(_context, codproforma);
                            var fechaaut_pfcomplemento = await ventas.Fecha_Autoriza_de_Proforma(_context, idpf_complemento, nroidpf_complemento);
                            return Ok(new
                            {
                                subtotal_pfcomplemento = subtotal_pfcomplemento,
                                total_pfcomplemento = total_pfcomplemento,
                                moneda_total_pfcomplemento = moneda_total_pfcomplemento,
                                fechaaut_pfcomplemento = fechaaut_pfcomplemento
                            });
                        }
                    }
                    else
                    {
                        return Ok(new
                        {
                            subtotal_pfcomplemento = "",
                            total_pfcomplemento = "",
                            moneda_total_pfcomplemento = "",
                            fechaaut_pfcomplemento = ""
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                return Problem("Error en el servidor al recuperar proforma complemento: " + ex.Message);
                throw;
            }
        }



        ////////////////////////////////////////////  IMPORTAR DOCUMENTO PROFORMA


        [HttpPost]
        [Route("importProf")]
        public async Task<IActionResult> importProf([FromForm] IFormFile file)
        {
            
            // Guardar el archivo en una ubicación temporal
            string currentDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string outputDirectory = Path.Combine(currentDirectory, "OutputFiles");

            Directory.CreateDirectory(outputDirectory); // Crear el directorio si no existe


            if (file == null || file.Length == 0)
            {
                return BadRequest("No se cargo el archivo correctamente.");
            }
            string filePath = "";

            string _targetDirectory = "";
            try
            {
                _targetDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "OutputFiles");
                // Combina el directorio de destino con el nombre del archivo
                filePath = Path.Combine(_targetDirectory, file.FileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }
            }
            catch (Exception)
            {
                throw;
            }
            ziputil zUtil = new ziputil();

            string primerArchivo = zUtil.ObtenerPrimerArchivoEnZip(filePath);
            ///descomprimir
            try
            {
                await zUtil.DescomprimirArchivo(_targetDirectory, filePath, primerArchivo);
                string xmlDecript = await encripVB.DecryptData(Path.Combine(_targetDirectory, primerArchivo));
                //await funciones.DecryptData(Path.Combine(_targetDirectory, primerArchivo), Path.Combine(_targetDirectory, "profor.xml"), key, IV2);
                return Ok(new { respXml = xmlDecript });
            }
            catch (Exception ex)
            {
                return Problem("Error en el servidor al importar proforma: " + ex.Message);
                throw;
            }

        }




        [HttpPost]
        [Route("importProfinJson")]
        public async Task<IActionResult> importProfinJson([FromForm] IFormFile file)
        {

            // Guardar el archivo en una ubicación temporal
            string currentDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string outputDirectory = Path.Combine(currentDirectory, "OutputFiles");

            Directory.CreateDirectory(outputDirectory); // Crear el directorio si no existe


            if (file == null || file.Length == 0)
            {
                return BadRequest("No se cargo el archivo correctamente.");
            }
            string filePath = "";

            string _targetDirectory = "";
            try
            {
                _targetDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "OutputFiles");
                // Combina el directorio de destino con el nombre del archivo
                filePath = Path.Combine(_targetDirectory, file.FileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }
            }
            catch (Exception)
            {
                throw;
            }
            ziputil zUtil = new ziputil();

            string primerArchivo = zUtil.ObtenerPrimerArchivoEnZip(filePath);
            ///descomprimir
            try
            {
                await zUtil.DescomprimirArchivo(_targetDirectory, filePath, primerArchivo);
                string xmlDecript = await encripVB.DecryptData(Path.Combine(_targetDirectory, primerArchivo));
                //await funciones.DecryptData(Path.Combine(_targetDirectory, primerArchivo), Path.Combine(_targetDirectory, "profor.xml"), key, IV2);

                DataSet dataSet = new DataSet();

                using (StringReader stringReader = new StringReader(xmlDecript))
                {
                    dataSet.ReadXml(stringReader);
                }

                Console.WriteLine("XML convertido a DataSet exitosamente.");

                // Suponiendo que tienes un DataSet llamado dataSet y quieres convertirlo a un diccionario de tablas:
                Dictionary<string, DataTable> datosConvertidos = dataSet.ToDictionary();

                // Accede a una tabla específica por su nombre
                DataTable cabeceraTabla = datosConvertidos["cabecera"];
                DataTable detalleTabla = datosConvertidos["detalle"];
                DataTable recargoTabla = datosConvertidos["recargo"];
                DataTable descuentoTabla = datosConvertidos["descuento"];
                DataTable ivaTabla = datosConvertidos["iva"];
                DataTable etiquetaTabla = datosConvertidos["etiqueta"];
                DataTable clienteTabla = datosConvertidos["cliente"];
                DataTable vedesclienteTabla = datosConvertidos["vedescliente"];

                List<Dictionary<string, object>> cabeceraList = DataTableToListConverter.ConvertToList(cabeceraTabla);
                List<Dictionary<string, object>> detalleList = DataTableToListConverter.ConvertToList(detalleTabla);
                List<Dictionary<string, object>> recargoList = DataTableToListConverter.ConvertToList(recargoTabla);
                List<Dictionary<string, object>> descuentoList = DataTableToListConverter.ConvertToList(descuentoTabla);
                List<Dictionary<string, object>> ivaList = DataTableToListConverter.ConvertToList(ivaTabla);
                List<Dictionary<string, object>> etiquetaList = DataTableToListConverter.ConvertToList(etiquetaTabla);
                List<Dictionary<string, object>> clienteList = DataTableToListConverter.ConvertToList(clienteTabla);
                List<Dictionary<string, object>> vedesclienteList = DataTableToListConverter.ConvertToList(vedesclienteTabla);
                return Ok(new { 
                    cabeceraList,
                    detalleList,
                    recargoList,
                    descuentoList,
                    ivaList,
                    etiquetaList,
                    clienteList,
                    vedesclienteList
                });
            }
            catch (Exception ex)
            {
                return Problem("Error en el servidor al importar proforma en JSON: " + ex.Message);
                throw;
            }
            finally
            {
                System.IO.File.Delete(filePath);
            }

        }

        ////////////////////////////////////////////  EXPORTAR DOCUMENTO PROFORMA



        [HttpGet]
        [Route("exportProforma/{userConn}/{codProforma}/{codcliente}")]
        public async Task<IActionResult> exportProforma(string userConn, int codProforma, string codcliente)
        {
            try
            {
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);
                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var result = await cargariedataset(_context, codProforma, codcliente);
                    if (result.resp)
                    {
                        string stringDataXml = ConvertDataSetToXml(result.iedataset);
                        string id = result.id;
                        int numeroid = result.numeroid;

                        var resp_dataEncriptada = await exportar_encriptado(stringDataXml, id, numeroid);
                        if (resp_dataEncriptada.resp)
                        {
                            string zipFilePath = resp_dataEncriptada.filePath; 

                            if (System.IO.File.Exists(zipFilePath))
                            {
                                byte[] fileBytes = System.IO.File.ReadAllBytes(zipFilePath);
                                string fileName = Path.GetFileName(zipFilePath);
                                try
                                {
                                    // Devuelve el archivo ZIP para descargar
                                    return File(fileBytes, "application/zip", fileName);
                                }
                                catch (Exception)
                                {
                                    return Problem("Error en el servidor");
                                    throw;
                                }
                                finally
                                {
                                    System.IO.File.Delete(zipFilePath);
                                }
                                
                            }
                            else
                            {
                                return NotFound("El archivo ZIP no se encontró.");
                            }
                        }
                        //return Ok(stringDataXml);
                    }
                    return Ok(result.resp);
                }
            }
            catch (Exception ex)
            {
                return Problem("Error en el servidor al exportar proforma ZIP: " + ex.Message);
                throw;
            }
        }

        private string ConvertDataSetToXml(DataSet iedataset)
        {
            using (StringWriter sw = new StringWriter())
            {
                iedataset.WriteXml(sw, XmlWriteMode.WriteSchema);
                return sw.ToString();
            }
        }

        private async Task<(bool resp, DataSet iedataset, string id, int numeroid)> cargariedataset(DBContext _context, int codProforma, string codcliente)
        {
            DataSet iedataset = new DataSet();

            try
            {
                iedataset.Clear();
                iedataset.Reset();

                // cargar cabecera
                var dataProforma = await _context.veproforma.Where(i => i.codigo == codProforma).ToListAsync();
                if (dataProforma.Any())
                {
                    DataTable cabeceraTable = dataProforma.ToDataTable();
                    cabeceraTable.TableName = "cabecera";
                    iedataset.Tables.Add(cabeceraTable);
                    iedataset.Tables["cabecera"].Columns.Add("documento", typeof(string));
                    iedataset.Tables["cabecera"].Rows[0]["documento"] = "PROFORMA";

                }
                string id = dataProforma[0].id;
                int numeroid = dataProforma[0].numeroid;
                /*
                // Añadir campo identificador
                iedataset.Tables["cabecera"].Columns.Add("documento", typeof(string));
                iedataset.Tables["cabecera"].Rows[0]["documento"] = "PROFORMA";

                */

                // Cargar detalle usando LINQ y Entity Framework
                var dataDetalle = await _context.veproforma1
                    .Where(p => p.codproforma == codProforma)
                    .Join(_context.initem,
                          p => p.coditem,
                          i => i.codigo,
                          (p, i) => new
                          {
                              p.codproforma,
                              p.coditem,
                              i.descripcion,
                              i.medida,
                              p.niveldesc,
                              p.preciodesc,
                              p.cantidad_pedida,
                              p.cantidad,
                              p.udm,
                              p.porceniva,
                              p.codtarifa,
                              p.coddescuento,
                              p.preciolista,
                              p.precioneto,
                              p.total,
                              cumple = 1,
                              p.cantaut,
                              p.totalaut,
                              porcendesc = 0.00,
                              porcen_mercaderia = 0.000,
                              p.peso,
                              p.nroitem,
                              empaque = 0
                          })
                    .OrderBy(p => p.coditem)
                    .ToListAsync();
                /*
                if (dataDetalle.Any())
                {
                    DataTable detalleTable = dataDetalle.ToDataTable();   // convertir a dataTable
                    detalleTable.TableName = "detalle";
                    iedataset.Tables.Add(detalleTable);
                }*/
                DataTable detalleTable = dataDetalle.ToDataTable();   // convertir a dataTable
                detalleTable.TableName = "detalle";
                iedataset.Tables.Add(detalleTable);

                // cargar recargos
                var dataRecargos = await _context.verecargoprof
                    .Where(i => i.codproforma == codProforma)
                    .ToListAsync();
                /*
                if (dataRecargos.Any())
                {
                    DataTable recargoTable = dataRecargos.ToDataTable();
                    recargoTable.TableName = "recargo";
                    iedataset.Tables.Add(recargoTable);
                }*/
                DataTable recargoTable = dataRecargos.ToDataTable();
                recargoTable.TableName = "recargo";
                iedataset.Tables.Add(recargoTable);

                // cargar descuentos
                var dataDescuentos = await _context.vedesextraprof
                    .Where(i => i.codproforma == codProforma)
                    .ToListAsync();
                /*
                if (dataDescuentos.Any())
                {
                    DataTable descuentoTable = dataDescuentos.ToDataTable();
                    descuentoTable.TableName = "descuento";
                    iedataset.Tables.Add(descuentoTable);
                }*/
                DataTable descuentoTable = dataDescuentos.ToDataTable();
                descuentoTable.TableName = "descuento";
                iedataset.Tables.Add(descuentoTable);


                // cargar iva
                var dataIva = await _context.veproforma_iva
                    .Where(i => i.codproforma == codProforma)
                    .ToListAsync();
                /*
                if (dataIva.Any())
                {
                    DataTable ivaTable = dataIva.ToDataTable();
                    ivaTable.TableName = "iva";
                    iedataset.Tables.Add(ivaTable);
                }*/
                DataTable ivaTable = dataIva.ToDataTable();
                ivaTable.TableName = "iva";
                iedataset.Tables.Add(ivaTable);

                // cargar etiqueta
                var dataEtiqueta = await _context.veetiqueta_proforma
                    .Where(i => i.id == id && i.numeroid == numeroid)
                    .ToListAsync();
                /*
                if (dataEtiqueta.Any())
                {
                    DataTable etiquetaTable = dataEtiqueta.ToDataTable();
                    etiquetaTable.TableName = "etiqueta";
                    iedataset.Tables.Add(etiquetaTable);
                }*/
                DataTable etiquetaTable = dataEtiqueta.ToDataTable();
                etiquetaTable.TableName = "etiqueta";
                iedataset.Tables.Add(etiquetaTable);

                // cargar datos del cliente por si acaso
                var dataCliente = await _context.vecliente
                    .Where(i => i.codigo == codcliente)
                    .ToListAsync();
                /*
                if (dataCliente.Any())
                {
                    DataTable clienteTable = dataCliente.ToDataTable();
                    clienteTable.TableName = "cliente";
                    iedataset.Tables.Add(clienteTable);
                }*/
                DataTable clienteTable = dataCliente.ToDataTable();
                clienteTable.TableName = "cliente";
                iedataset.Tables.Add(clienteTable);


                // cargar datos de descuentos del cliente
                var dataDescuentosCliente = await ventas.Descuentosporlinea(_context, codcliente);
                /*
                if (dataDescuentosCliente.Any())
                {
                    DataTable vedesclienteTable = dataDescuentosCliente.ToDataTable();
                    vedesclienteTable.TableName = "vedescliente";
                    iedataset.Tables.Add(vedesclienteTable);
                }*/
                DataTable vedesclienteTable = dataDescuentosCliente.ToDataTable();
                vedesclienteTable.TableName = "vedescliente";
                iedataset.Tables.Add(vedesclienteTable);
                return (true, iedataset, id, numeroid);
            }
            catch (Exception)
            {
                return (false, iedataset, "", 0);
            }
        }



        private async Task<(bool resp, string filePath)> exportar_encriptado(string xmlText, string id, int numeroid)
        {
            ziputil zUtil = new ziputil();
            try
            {
                string currentDirectory = AppDomain.CurrentDomain.BaseDirectory;
                string outputDirectory = Path.Combine(currentDirectory, "OutputFiles");

                Directory.CreateDirectory(outputDirectory); // Crear el directorio si no existe
                string outName = Path.Combine(outputDirectory, id + "-" + numeroid + ".pro");

                string[] archivo = new string[1];
                archivo[0] = outName;

                //await funciones.EncryptData(xmlText, outName, key, IV2);
                await encripVB.EncryptData(xmlText, outName);

                await zUtil.Comprimir(archivo, outName.Substring(0, outName.Length - 4) + ".zip", false);

                
                return (true, outName.Substring(0, outName.Length - 4) + ".zip");
            }
            catch (Exception)
            {
                return (false, "");
            }
        }


        [HttpPost]
        [Route("getDataPDF/{userConn}")]
        public async Task<IActionResult> getDataPDF(string userConn, RequestGetDataPDF RequestGetDataPDF)
        {
            int codProforma = RequestGetDataPDF.codProforma;
            string codcliente = RequestGetDataPDF.codcliente;
            string codcliente_real = RequestGetDataPDF.codcliente_real;
            string codempresa = RequestGetDataPDF.codempresa;
            string cmbestado_contra_entrega = RequestGetDataPDF.cmbestado_contra_entrega;
            bool paraAprobar = RequestGetDataPDF.paraAprobar;
            try
            {
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);
                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    // OBTENER DATOS DE PROFORMA CUERPO
                    var dtveproforma1 = await _context.veproforma1
                        .Join(_context.initem,
                              p1 => p1.coditem,
                              p2 => p2.codigo,
                              (p1, p2) => new { p1, p2 })
                        .Where(x => x.p1.codproforma == codProforma && x.p1.cantidad > 0)
                        .OrderBy(x => x.p1.coditem)
                        .Select(x => new
                        {
                            coditem = x.p1.coditem,
                            descripcion = x.p2.descripcion,
                            medida = x.p2.medida,
                            udm = x.p1.udm,
                            cantidad = x.p1.cantidad,
                            codtarifa = x.p1.codtarifa,
                            precioneto = Math.Round(x.p1.precioneto,5),
                            total = x.p1.total
                        }).ToListAsync();


                    string nomEmpresa = await nombres.nombreempresa(_context, codempresa);
                    string nit = await empresa.NITempresa(_context, codempresa);
                    bool proforma_es_complementaria = await ventas.proforma_es_complementaria(_context, codProforma);
                    bool esClienteFinal = await cliente.EsClienteFinal(_context, codcliente_real);
                    string dsctosdescrip = await ventas.descuentosstr(_context, codProforma, "PF", "Descripcion Completa");


                    var docveprofCab = await _context.veproforma
                        .Where(i => i.codigo == codProforma)
                        .Select(i => new veproforma1_Report
                        {
                            // Report Header
                            empresa = nomEmpresa,
                            hora_impresion = DateTime.Now.ToString("h:mm tt"),   // hora de generacion de informacion de PDF
                            fecha_impresion = DateTime.Now.ToString("d/M/yyyy"), // fecha de generacion de informacion de PDF
                            rnit = nit,
                            rnota_remision = "",                                // DE MOMENTO VACIO NO ES NECESARIO
                            inicial = i.hora_inicial + " " + (i.fecha_inicial ?? DateTime.Today).ToString("d/M/yyyy"),

                            // Page Header
                            titulo = (proforma_es_complementaria ? "PROFORMA " + i.id + "-" + i.numeroid + " COMPLEMENTARIA " : "PROFORMA " + i.id + "-" + i.numeroid + " ") + (i.aprobada ? "APROBADA" : "NO-APROBADA"),
                            tipopago = i.tipopago == 0 ? "CONTADO" : "CREDITO",
                            codalmacen = i.codalmacen.ToString(),
                            rcodcliente = i.codcliente,                 // verificar y cambiar luego si es cliente sin nombre.
                            rcliente = i.nomcliente,                    // verificar y cambiar luego si es cliente sin nombre.
                            rnombre_comercial = i.nomcliente,           // verificar y cambiar a nombre comercial luego.
                            rcodvendedor = i.codvendedor.ToString(),
                            rtdc = i.tdc.ToString(),
                            rmonedabase = i.codmoneda,
                            rfecha = i.fecha.ToString("d/M/yyyy") + " " + i.hora,
                            rdireccion = i.direccion,                   // Esto debe cambiar si el cliente es casual.
                            rtelefono = "---",                          // Esto debe cambiar dependiendo si es sin nombre, etc
                            rpreparacion = i.preparacion + " " + (esClienteFinal ? "CLIENTE FINAL" : ""),
                            rptoventa = i.direccion,                    // Esto debe cambiar dependiendo si es sin nombre, etc

                            // Report Footer
                            rpesototal = Math.Round((i.peso ?? 0),2).ToString(),
                            rsubtotal = i.subtotal.ToString(),
                            rrecargos = i.recargos.ToString(),
                            rdescuentos = i.descuentos.ToString(),
                            riva = (i.iva ?? 0).ToString(),
                            rtotalimp = i.total.ToString(),
                            rtotalliteral = "SON: " + funciones.ConvertDecimalToWords(i.total),    // AÑADE LUEGO LA DESCRIPCION DE MONEDA
                            rdsctosdescrip = dsctosdescrip,
                            rtransporte = "",                       // ACA FALTA REVISAR Y UNA VALIDACION CON PRTOF COMPLEMENTARIA
                            rfletepor = i.fletepor,
                            robs = i.obs,                           // ACA ARMAR DE ACUERDO A SI ES A CREDITO O NO Y DEMAS
                            rfacturacion = i.nomcliente + " - " + i.nit + " - " + i.complemento_ci,
                            rpagocontadoanticipado = "",            // VERIFICAR CON ANTICIPOS DE PROFORMA
                            ridanticipo = "",                       // VERFICICAR CON ANTICIPOS DE PROFORMA
                            rimprimir_etiqueta_cliente = "",        // VERIFICAR SI ES CLIENTE SIN NOMBRE

                            crfecha_hr_inicial = (i.fecha_inicial ?? DateTime.Today).ToShortDateString() + " " + i.hora_inicial,
                            crfecha_hr_autoriza = i.aprobada == true ? ((i.fechaaut ?? DateTime.Today).ToShortDateString() + " " + i.horaaut) : " ",

                        }).FirstOrDefaultAsync();



                    // FINALIZAR CON EL LLENADO TENIENDO EN CUENTA LAS VALIDACIONES

                    

                    bool es_casual = false;
                    if (codcliente != codcliente_real)
                    {
                        es_casual = true;
                    }

                    var dataAux = await _context.veproforma.Where(i => i.codigo == codProforma)
                        .Select(i => new
                        {
                            i.id,
                            i.numeroid,
                            i.nomcliente,
                            i.direccion,
                            i.codalmacen,
                            i.codmoneda,
                            i.tipoentrega,
                            i.tipopago,
                            i.contra_entrega,
                            i.obs,
                            i.idpf_complemento,
                            i.nroidpf_complemento,
                            i.transporte,
                            i.nombre_transporte
                        }).FirstOrDefaultAsync();
                    docveprofCab.rtotalliteral = docveprofCab.rtotalliteral + " " + await nombres.nombremoneda(_context, dataAux.codmoneda);

                    // SI ES EL CLIENTE SIN NOMBRE MODIFICAR ALGUNOS DATOS DE IMPRESION
                    if (await cliente.EsClienteSinNombre(_context,codcliente))
                    {
                        if (codcliente == codcliente_real)
                        {
                            docveprofCab.rcliente = "--";
                            docveprofCab.rcodcliente = "CLIENTE:";
                            docveprofCab.rnombre_comercial = dataAux.nomcliente;

                            var dt_etiqueta_aux = await _context.veetiqueta_proforma.Where(i => i.id == dataAux.id && i.numeroid == dataAux.numeroid)
                                .Select(i => new
                                {
                                    i.codigo,
                                    i.linea1,
                                    i.linea2,
                                    i.representante,
                                    i.telefono,
                                    i.ciudad,
                                    i.celular,
                                }).FirstOrDefaultAsync();
                            if (dt_etiqueta_aux != null)
                            {
                                docveprofCab.rptoventa = dt_etiqueta_aux.ciudad;
                                docveprofCab.rtelefono = dt_etiqueta_aux.telefono + " - " + dt_etiqueta_aux.celular;
                            }
                            else
                            {
                                docveprofCab.rptoventa = dataAux.direccion;
                                docveprofCab.rtelefono = "";
                            }
                        }
                        else
                        {
                            docveprofCab.rcliente = "--";
                            docveprofCab.rcodcliente = "CLIENTE:";
                            docveprofCab.rnombre_comercial = dataAux.nomcliente;
                            docveprofCab.rtelefono = await ventas.telefonocliente_direccion(_context, codcliente_real, dataAux.direccion);
                            docveprofCab.rptoventa = await ventas.ptoventacliente_direccion(_context, codcliente_real, dataAux.direccion);
                        }
                        docveprofCab.rimprimir_etiqueta_cliente = "***IMPRIMIR ETIQUETA***";
                    }
                    else
                    {
                        docveprofCab.rcliente = await nombres.nombrecliente(_context, codcliente);
                        docveprofCab.rcodcliente = codcliente;
                        docveprofCab.rnombre_comercial = await cliente.NombreComercial(_context, codcliente.Trim());

                        if (es_casual)
                        {
                            docveprofCab.rtelefono = await ventas.telefonocliente_direccion(_context, codcliente_real, dataAux.direccion);
                            docveprofCab.rptoventa = await ventas.ptoventacliente_direccion(_context, codcliente_real, dataAux.direccion);
                        }
                        else
                        {
                            docveprofCab.rtelefono = await ventas.telefonocliente_direccion(_context, codcliente, dataAux.direccion);
                            docveprofCab.rptoventa = await ventas.ptoventacliente_direccion(_context, codcliente, dataAux.direccion);
                        }
                    }


                    if (es_casual)
                    {
                        // si el cliente es casual, poner la direccion del cliente casual y no del cliente referencia por instruccion Gerencia dsd 05-07-2022
                        /*
                         
                        'rdireccion.Text = Chr(34) & direccion.Text & Chr(34)
                        'rdireccion.Text = Chr(34) & sia_funciones.Cliente.Instancia.direccioncliente(codcliente.Text, Me.Usar_Bd_Opcional)
                        'rdireccion.Text &= " (" & sia_funciones.Cliente.Instancia.PuntoDeVentaCliente_Segun_Direccion(codcliente.Text, rdireccion.Text) & ")" & Chr(34)
            
                         */

                        // Desde 10-10-2022 si la venta es casual la direccion se pondra la del almacen
                        docveprofCab.rdireccion = await almacen.direccionalmacen(_context, dataAux.codalmacen);
                        // definir con que punto de venta se creara el cliente
                        int codpto_vta = 0;
                        var dt1 = await _context.inalmacen
                            .Where(p1 => p1.codigo == dataAux.codalmacen)
                            .Join(_context.adarea,
                                  p1 => p1.codarea,
                                  p2 => p2.codigo,
                                  (p1, p2) => new
                                  {
                                      codarea = p2.codigo,
                                      p2.descripcion,
                                  })
                            .FirstOrDefaultAsync();
                        if (dt1 != null)
                        {
                            if (dt1.codarea == 300)
                            {
                                codpto_vta = 300;
                            }
                            else if (dt1.codarea == 400)
                            {
                                codpto_vta = 400;
                            }
                            else
                            {
                                codpto_vta = 800;
                            }
                        }
                        docveprofCab.rdireccion = docveprofCab.rdireccion + " (" + await cliente.PuntoDeVenta_Casual(_context, codpto_vta) + ")";
                    }
                    else
                    {
                        docveprofCab.rdireccion = dataAux.direccion;
                    }



                    // ###########################################################################################
                    // verificar si la proforma esta cancelada con anticipo
                    // ###########################################################################################
                    string cadena_anticipos = "";
                    bool Pagado_Con_Anticipo = false;
                    string docanticipo = "";

                    var tblanticipos = await _context.veproforma_anticipo.Where(i => i.codproforma == codProforma).ToListAsync();
                    if (tblanticipos.Count > 0)
                    {
                        Pagado_Con_Anticipo = true;
                        foreach (var reg in tblanticipos)
                        {
                            docanticipo = await cobranzas.IdNroid_Anticipo(_context, reg.codanticipo ?? 0);
                            cadena_anticipos = cadena_anticipos + "(" + docanticipo + ")";
                        }
                        docveprofCab.ridanticipo = cadena_anticipos;
                        // docveprofCab.rnumeroidanticipo = "";
                        docveprofCab.rpagocontadoanticipado = "LA PROFORMA SE PAGO CON ANTICIPO: ";
                    }
                    else
                    {
                        Pagado_Con_Anticipo = false;
                        docveprofCab.ridanticipo = "";
                        // docveprofCab.rnumeroidanticipo = "";
                        docveprofCab.rpagocontadoanticipado = "";
                    }
                    // ###########################################################################################

                    ////////////////////////////////////////////////////
                    // definicion del campo de observaciones
                    ////////////////////////////////////////////////////

                    string observacion = dataAux.tipoentrega;
                    if (dataAux.tipopago == 0)
                    {
                        //%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
                        //%%   ES CONTADO
                        //%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
                        if (dataAux.contra_entrega ?? false)
                        {
                            observacion = observacion + "-" + "VENTA CONTADO - CONTRA ENTREGA" + cmbestado_contra_entrega;
                        }
                        else
                        {
                            if (Pagado_Con_Anticipo)
                            {
                                observacion = observacion + "-" + "VENTA CONTADO - YA FUE CANCELADO CON ANTIPO: " + cadena_anticipos;
                            }
                            else
                            {
                                observacion = observacion + "-" + "VENTA CONTADO - NO CANCELADO" + cmbestado_contra_entrega;
                            }
                        }
                    }
                    else
                    {
                        //%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
                        //%%   ES CREDITO
                        //%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
                        if (dataAux.contra_entrega ?? false)
                        {
                            observacion = observacion + "-" + "VENTA CREDITO - CONTRA ENTREGA " + cmbestado_contra_entrega; 
                        }
                        else
                        {
                            observacion = observacion + "-" + "VENTA ENTREGA CREDITO";
                        }
                    }

                    var dt_etiqueta = await _context.veetiqueta_proforma.Where(i => i.id == dataAux.id && i.numeroid == dataAux.numeroid)
                        .Select(i => new
                        {
                            codigo = i.codigo,
                            linea1 = i.linea1,
                            linea2 = i.linea2,
                            representante = i.representante,
                            telefono = i.telefono,
                            ciudad = i.ciudad,
                            celular = i.celular,
                            nom_factura = docveprofCab.rfacturacion
                        })
                        .FirstOrDefaultAsync();
                    if (dt_etiqueta != null)
                    {
                        if (dt_etiqueta.linea2.Trim() == "" || dt_etiqueta.linea2.Trim() == "-")
                        {
                            if (es_casual)
                            {
                                // si es casual no mostrar la parte de etiqueta
                                observacion = observacion + " - " + dataAux.obs;
                            }
                            else
                            {
                                observacion = observacion + " - " + dataAux.obs + " Etiqueta: " + dt_etiqueta.linea1;
                            }
                        }
                        else
                        {
                            if (es_casual)
                            {
                                // si es casual no mostrar la parte de etiqueta
                                observacion = observacion + " - " + dataAux.obs;
                            }
                            else
                            {
                                observacion = observacion + " - " + dataAux.obs + " Etiqueta: " + dt_etiqueta.linea2;
                            }
                        }
                    }
                    else
                    {
                        observacion = observacion + " - " + dataAux.obs;
                    }
                    docveprofCab.robs = observacion;



                    // si la proforma es dimediado y tiene complemento con una de precios mayorista
                    // verificar si tiene complemento dimediado
                    // implementado en fecha: 15-07-2021
                    string _tipoentrega = "";
                    string _complemento_dimediado = "";
                    if (dataAux.idpf_complemento.Trim().Length > 0 && dataAux.nroidpf_complemento > 0)
                    {
                        _complemento_dimediado = "Complemento Dim: " + dataAux.idpf_complemento + "-" + dataAux.nroidpf_complemento;
                        _tipoentrega = await ventas.Proforma_Transporte(_context, codProforma);
                    }
                    else
                    {
                        _complemento_dimediado = "";
                        _tipoentrega = dataAux.transporte;
                    }

                    docveprofCab.rtransporte = _tipoentrega + " Nomb. Transporte: " + dataAux.nombre_transporte + " " + _complemento_dimediado;
                    /*
                    if (!es_casual)
                    {
                        dt_etiqueta = null;
                    }
                    */

                    List<vetuercas> ds_tuercas_lista = new List<vetuercas>();
                    if (paraAprobar) // si es para aprobar la proforma, solo ahi se genera el detalle de etiqueta tuercas
                    {
                        List<itemDataMatriz> tabladetalle = dtveproforma1.Select(i => new itemDataMatriz
                        {
                            coditem = i.coditem,
                            cantidad = (double)i.cantidad
                        }).ToList();
                        var tabladetalle_tuercas = await ventas.Detalle_tuercas_PF(_context, tabladetalle);
                        if (tabladetalle_tuercas.Count() > 0)
                        {
                            foreach (var reg in tabladetalle_tuercas)
                            {
                                vetuercas drow = new vetuercas();
                                drow.coditem = reg.coditem;
                                drow.descripcion = reg.descripcion;
                                drow.medida = reg.medida;
                                drow.udm = reg.udm;
                                drow.cantidad = reg.cantidad;
                                ds_tuercas_lista.Add(drow);
                            }
                        }
                    }
                    return Ok(new
                    {
                        docveprofCab = docveprofCab,
                        dtveproforma1 = dtveproforma1,
                        dt_etiqueta = dt_etiqueta,
                        ds_tuercas_lista = ds_tuercas_lista
                    });
                }

            }
            catch (Exception ex)
            {
                return Problem("Error en el servidor al obtener los datos para PDF: " + ex.Message);
                throw;
            }
        }

        /*
        private static string ConvertDecimalToWords(decimal number)
        {
            int integerPart = (int)Math.Truncate(number);
            int decimalPart = (int)((number - integerPart) * 100);

            string integerPartInWords = integerPart.ToWords(new CultureInfo("es")).ToUpper();
            string decimalPartInWords = $"{decimalPart}/100";

            return $"{integerPartInWords} {decimalPartInWords}";
        }

        */
   
        [HttpGet]
        [Route("getConsultEtiquetasImpresas/{userConn}/{codProforma}")]
        public async Task<IActionResult> getConsultEtiquetasImpresas(string userConn, int codProforma)
        {
            try
            {
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);
                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    bool control = await ventas.proforma_ya_esta_etiqueta_impresa(_context, codProforma);
                    return Ok(new
                    {
                        resultado = control
                    });
                }

            }
            catch (Exception ex)
            {
                return Problem("Error en el servidor : " + ex.Message);
                throw;
            }
        }
        [HttpGet]
        [Route("getEtiquetasProformaPDF/{userConn}/{codProforma}/{codempresa}")]
        public async Task<IActionResult> getEtiquetasProformaPDF(string userConn, int codProforma, string codempresa)
        {
            try
            {
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);
                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var docInfProforma = await _context.veproforma.Where(i => i.codigo == codProforma)
                        .Select(i => new
                        {
                            i.id,
                            i.numeroid
                        }).FirstOrDefaultAsync();

                    if (docInfProforma!=null)
                    {
                        var etiquetas = await _context.veproforma
                            .Where(p1 => p1.id == docInfProforma.id && p1.numeroid == docInfProforma.numeroid)
                            .Join(_context.veproforma1,
                                  p1 => p1.codigo,
                                  p2 => p2.codproforma,
                                  (p1, p2) => new { p1, p2 })
                            .Where(joined => joined.p2.cantidad > 0)
                            .Join(_context.initem,
                                  joined => joined.p2.coditem,
                                  p3 => p3.codigo,
                                  (joined, p3) => new
                                  {
                                      coditem = joined.p2.coditem,
                                      descripcion = p3.descripcion,
                                      medida = p3.medida,
                                      udm = p3.unidad,
                                      cantidad = joined.p2.cantidad
                                  })
                            .ToListAsync();

                        string empresaNom = await nombres.nombreempresa(_context, codempresa);
                        string titulo = " ETIQUETAS PROFORMA " + docInfProforma.id + "-" + docInfProforma.numeroid;
                        string nit = await empresa.NITempresa(_context, codempresa);

                        await ventas.proforma_marcar_etiqueta_impresa(_context, codProforma);

                        return Ok(new
                        {
                            // cabecera
                            rempresa = empresaNom,
                            rtitulo = titulo,
                            rnit = nit,
                            hora_impresion = DateTime.Now.ToString("h:mm tt"),   // hora de generacion de informacion de PDF
                            fecha_impresion = DateTime.Now.ToString("dd/MM/yyyy"), // fecha de generacion de informacion de PDF

                            // etiquetas
                            etiquetas = etiquetas
                        });
                    }
                    return BadRequest(new { resp = "No se encontraron datos con ese codigo de proforma" });

                }

            }
            catch (Exception ex)
            {
                return Problem("Error en el servidor : " + ex.Message);
                throw;
            }
        }


        [HttpPost]
        [Route("DetallePFAprobadasWF/{userConn}/{codempresa}/{usuario}")]
        public async Task<ActionResult<List<ProformasWF>>> DetallePFAprobadasWF(string userConn, string codempresa, string usuario)
        {
            try
            {
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);
                List<ProformasWF>? proformas_aprobadas = new List<ProformasWF>();
                var resultado = await ventas.Detalle_Proformas_Aprobadas_WF(userConnectionString, codempresa, usuario);
                if (resultado != null)
                {
                    ///
                    string jsonResult = JsonConvert.SerializeObject(resultado);

                    return Ok(jsonResult);
                }
                else { return BadRequest(new { resp = "No se pudo generar el detalle de proformas aprobadas grabadas en el SIAW." }); }
            }
            catch (Exception ex)
            {
                return Problem("Error en el servidor: " + ex.ToString());
                throw;
            }
        }

        [HttpGet]
        [Route("fechaHoraServidor/{userConn}")]
        public async Task<ActionResult<List<ProformasWF>>> fechaHoraServidor(string userConn)
        {
            try
            {
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);
                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    DateTime fechaServidor = await funciones.FechaDelServidor(_context);
                    string horaServidor = datos_proforma.getHoraActual();
                    return Ok(new
                    {
                        fechaServidor,
                        horaServidor
                    });
                }
            }
            catch (Exception ex)
            {
                return Problem("Error en el servidor: " + ex.ToString());
                throw;
            }

        }

        [HttpGet]
        [Route("verColEmpbyUser/{userConn}/{usuario}")]
        public async Task<ActionResult<List<ProformasWF>>> verColEmpbyUser(string userConn, string usuario)
        {
            try
            {
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);
                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    bool usrVeEmp = await configuracion.usr_ver_columna_empaques(_context,usuario);
                    return Ok(new
                    {
                        veEmpaques = usrVeEmp
                    });
                }
            }
            catch (Exception ex)
            {
                return Problem("Error en el servidor: " + ex.ToString());
                throw;
            }

        }

        [HttpGet]
        [Route("getRedondeo2decimales/{userConn}/{numero}")]
        public async Task<IActionResult> getRedondeo2decimales(string userConn, double numero)
        {
            try
            {
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);
                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    decimal numRedondeado = await siat.Redondeo_Decimales_SIA_2_decimales_SQL(_context, numero);
                    return Ok(new
                    {
                        resultado = numRedondeado
                    });
                }

            }
            catch (Exception ex)
            {
                return Problem("Error en el servidor : " + ex.Message);
                throw;
            }
        }
        [HttpGet]
        [Route("getRedondeo5decimales/{userConn}/{numero}")]
        public async Task<IActionResult> getRedondeo5decimales(string userConn, decimal numero)
        {
            try
            {
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);
                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    decimal numRedondeado = await siat.Redondeo_Decimales_SIA_5_decimales_SQL(_context, numero);
                    return Ok(new
                    {
                        resultado = numRedondeado
                    });
                }

            }
            catch (Exception ex)
            {
                return Problem("Error en el servidor : " + ex.Message);
                throw;
            }
        }

        [HttpGet]
        [Route("getTipoDescNivel/{userConn}")]
        public async Task<IActionResult> getTipoDescNivel(string userConn)
        {
            try
            {
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);
                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var lista = await ventas.Obtener_Tipos_Descuento_Nivel(_context);
                    return Ok(lista);
                }

            }
            catch (Exception ex)
            {
                return Problem("Error en el servidor : " + ex.Message);
                throw;
            }
        }

        [HttpGet]
        [Route("validarNITenSIN/{userConn}/{codempresa}/{usuario}/{codalmacen}/{nit_a_verificar}/{tipo_doc}")]
        public async Task<IActionResult> validarNITenSIN(string userConn, string codempresa, string usuario, int codalmacen, string nit_a_verificar, int tipo_doc)
        {
            if (tipo_doc != 5)
            {
                return StatusCode(203, new
                {
                    resp = "El tipo de documento no es del tipo NIT"
                });
            }
            if (nit_a_verificar.Trim().Length == 0)
            {
                return StatusCode(203, new
                {
                    resp = "El NIT ingresado se encuentra vacio, verifique esta situación"
                });
            }
            try
            {
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);
                using (var _context = DbContextFactory.Create(userConnectionString))
                {

                    var serviOnline = await _context.adsiat_parametros_facturacion.Where(i => i.codalmacen == codalmacen).Select(i => new
                    {
                        i.servicio_internet_activo,
                        i.servicio_sin_activo
                    }).FirstOrDefaultAsync();

                    bool adsiat_internet_activo = false;
                    bool adsiat_sin_activo = false;
                    if (serviOnline != null)
                    {
                        adsiat_internet_activo = serviOnline.servicio_internet_activo ?? false;
                        adsiat_sin_activo = serviOnline.servicio_sin_activo ?? false;
                    }
                    if (adsiat_internet_activo && await funciones.Verificar_Conexion_Internet() == true)
                    {
                        if (adsiat_sin_activo && await serv_Facturas.VerificarComunicacion(_context, codalmacen))   
                        {
                            string miNIT = await empresa.NITempresa(_context, codempresa);
                            string nit_es_valido = await funciones_SIAT.Verificar_NIT_SIN_2024(_context, codalmacen, long.Parse(miNIT), long.Parse(nit_a_verificar), usuario);
                            int status = new int();
                            switch (nit_es_valido)
                            {
                                case "VALIDO":
                                    status = 1;
                                    break;
                                case "ERROR":
                                    status = 2;
                                    break;
                                case "OTRO":
                                    status = 3;
                                    break;
                                case "INVALIDO":
                                    status = 4;
                                    break;
                                default:
                                    status = 0; // Si ningún caso coincide, puedes definir un valor predeterminado
                                    break;
                            }
                            return Ok(new
                            {
                                nit_es_valido,
                                status
                            });
                        }
                        else
                        {
                            return StatusCode(203, new
                            {
                                resp = "No se tiene comunicación con Impuestos o se deshabilitó la comunicacion con Impuestos, verifique esta situación."
                            });
                        }
                    }
                    else
                    {
                        return StatusCode(203, new
                        {
                            resp = "No se tiene comunicación con Internet o se deshabilitó la comunicacion con Internet, verifique esta situación."
                        });
                    }  
                }
            }
            catch (Exception ex)
            {
                return Problem("Error en el servidor : " + ex.Message);
                throw;
            }
        }


    }


    public static class ListToDataTableConverter
    {
        public static DataTable ToDataTable<T>(this IList<T> data)
        {
            PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(typeof(T));
            DataTable table = new DataTable();

            foreach (PropertyDescriptor prop in properties)
            {
                table.Columns.Add(prop.Name, Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType);
            }

            foreach (T item in data)
            {
                DataRow row = table.NewRow();
                foreach (PropertyDescriptor prop in properties)
                {
                    row[prop.Name] = prop.GetValue(item) ?? DBNull.Value;
                }
                table.Rows.Add(row);
            }

            return table;
        }
    }

    public static class DataSetToObjectConverter
    {
        public static Dictionary<string, DataTable> ToDictionary(this DataSet dataSet)
        {
            Dictionary<string, DataTable> result = new Dictionary<string, DataTable>();

            foreach (DataTable table in dataSet.Tables)
            {
                result.Add(table.TableName, table);
            }

            return result;
        }
    }

    public static class DataTableToListConverter
    {
        public static List<Dictionary<string, object>> ConvertToList(DataTable dataTable)
        {
            List<Dictionary<string, object>> list = new List<Dictionary<string, object>>();

            foreach (DataRow row in dataTable.Rows)
            {
                Dictionary<string, object> dict = new Dictionary<string, object>();
                foreach (DataColumn col in dataTable.Columns)
                {
                    dict[col.ColumnName] = row[col];
                }
                list.Add(dict);
            }

            return list;
        }
    }

    public class requestRecargaDetalle
    {
        public string codempresa { get; set; }
        public string usuario { get; set; }
        public int codalmacen { get; set; }
        public string codcliente_real { get; set; }
        public string codcliente { get; set; }
        public string opcion_nivel { get; set; }
        public string desc_linea_seg_solicitud { get; set; }
        public string codmoneda { get; set; }
        public DateTime fecha { get; set; }
        public List<itemDataMatriz> tabladetalle { get; set; }
    }


    public class cargadofromMatriz
    {
        public string coditem { get; set; }
        public int tarifa { get; set; }
        public int descuento { get; set; }
        public int ? empaque { get; set; }
        public decimal cantidad_pedida { get; set; }
        public decimal cantidad { get; set; }
        public string codcliente { get; set; }
        public string opcion_nivel { get; set; }
        public int codalmacen { get; set; }
        public string desc_linea_seg_solicitud { get; set; }
        public string codmoneda { get; set; }
        public DateTime fecha { get; set; }
        public bool cumpleMin { get; set; } = true;
        public string ? descripcion { get; set; }
        public string ? medida { get; set; }
        public int orden_pedido { get; set; }
        public int ? nroitem { get; set; }
        public decimal porcen_mercaderia { get; set; }
    }
    public class getTarifaPrincipal_Rodrigo
    {
        public List<itemDataMatriz> tabladetalle { get; set; }
        public veproforma DVTA { get; set; }
        //public DatosDocVta DVTA { get; set; }
    }

    public class objetoDescDepositos
    {
        public getTarifaPrincipal_Rodrigo getTarifaPrincipal { get; set; }
        public List<tabladescuentos> tabladescuentos { get; set; }
        public List<tblcbza_deposito> tblcbza_deposito { get; set; }
    }

    public class ubicacionCliente
    {
        public string codcliente { get; set; }
        public string dircliente { get; set; }
    }
    public class RequestRecarlculaRecargoDescuentos
    {
        public veproforma veproforma { get; set; }
        public List<itemDataMatriz> detalleItemsProf { get; set; }
        public List<tablarecargos> tablarecargos { get; set; }
        public List<tabladescuentos> ? tabladescuentos { get; set; }

    }
    public class RequestEntregaPedido
    {
        public DatosDocVta datosDocVta { get; set; }
        public List<itemDataMatriz> detalleItemsProf { get; set; }
        public string preparacion { get; set; }

    }
    public class RequestDataEtiqueta
    {
        public string codcliente_real { get; set; }
        public string id { get; set; }
        public int numeroid { get; set; }
        public string codcliente { get; set; }
        public string nomcliente { get; set; }
        public bool desclinea_segun_solicitud { get; set; }
        public string idsoldesctos { get; set; }
        public int nroidsoldesctos { get; set; }
    }


    public class RequestDataSaldosSP
    {
        public string agencia { get; set; }
        public int codalmacen { get; set; }
        public string coditem { get; set; }
        public string codempresa { get; set; }
        public string usuario { get; set; }
        public string idProforma { get; set; }
        public int nroIdProforma { get; set; }
    }

    public class RequestGetDataPDF
    {
        public int codProforma { get; set; }
        public string codcliente { get; set; }
        public string codcliente_real { get; set; }
        public string codempresa { get; set; }
        public string cmbestado_contra_entrega { get; set; }
        public bool paraAprobar {  get; set; }
    }

    public class RequestAddDescNCliente
    {
        public string cmbtipo_desc_nivel {  get; set; }
        public DateTime fechaProf { get; set; }
        public int codtarifa_main {  get; set; }
        public string codcliente { get; set; }
        public string codcliente_real { get; set; }
        public string codclientedescripcion { get; set; }
    }
}
