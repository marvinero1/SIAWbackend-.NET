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
using static siaw_funciones.Validar_Vta;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.CodeAnalysis.Differencing;
using static System.Net.Mime.MediaTypeNames;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;

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

        public veproformaController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }

        [HttpGet]
        [Route("getAlmacenUser/{userConn}/{usuario}")]
        public async Task<ActionResult<inconcepto>> getAlmacenUser(string userConn, string usuario)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var codalmacen = await _context.adusparametros
                        .Where(item => item.usuario == usuario)
                        .Select(item => item.codalmacen)
                        .FirstOrDefaultAsync();

                    if (codalmacen == null)
                    {
                        return NotFound(new { resp = "No se encontro un registro con este código (cod almacen)" });
                    }

                    return Ok(codalmacen);
                }

            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
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
                        .OrderBy (t => t.codigoclasificador)
                        .Select(t => new
                        {
                            t.codigoclasificador,
                            t.descripcion
                        })
                        .ToListAsync();
                    return Ok(result);
                }
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
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
                    return BadRequest( new { resp = "No se pudo obtener la cadena de conexión"});
                }

                var instoactual = await empaque_func.GetSaldosActual(ad_conexion_vpnResult, codalmacen, coditem);
                if (instoactual == null)
                {
                    return NotFound( new { resp = "No se encontraron registros con los datos proporcionados." });
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
                    return NotFound( new { resp = "No se encontraron registros con los datos proporcionados." });
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
                List< sldosItemCompleto > listaSaldos = new List< sldosItemCompleto>();
                sldosItemCompleto saldoItemTotal = new sldosItemCompleto();
                sldosItemCompleto var8 = new sldosItemCompleto();
                saldoItemTotal.descripcion = "Total Saldo";

                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);
                var conexion = userConnectionString;
                bool eskit = await empaque_func.GetEsKit(conexion, coditem);  // verifica si el item es kit o no
                bool obtener_cantidades_aprobadas_de_proformas = await empaque_func.IfGetCantidadAprobadasProformas(userConnectionString, codempresa); // si se obtender las cantidades reservadas de las proformas o no



                // Falta validacion para saber si traera datos de manera local o por vpn
                // Obtener el contexto de base de datos correspondiente al usuario
                bool usar_bd_opcional = await saldos.get_usar_bd_opcional(userConnectionString, usuario);
                if (usar_bd_opcional)
                {
                    conexion = empaque_func.Getad_conexion_vpnFromDatabase(userConnectionString, agencia);
                    if (conexion == null)
                    {
                        return BadRequest( new { resp = "No se pudo obtener la cadena de conexión"});
                    }
                }

                

                // obtiene saldos de agencia del item seleccionado
                instoactual instoactual = await getEmpaquesItemSelect(conexion, coditem, codalmacen, eskit);

                if (eskit)
                {
                    //saldosDetalleItem.txtReservaProf = "(-) PROFORMAS APROBADAS ITEM(" + instoactual.coditem + ") DEL CJTO(" + coditem + ")";
                    sldosItemCompleto saldosDetalleItem = new sldosItemCompleto();
                    saldosDetalleItem.descripcion = "(+) SALDO ACTUAL ITEM (" + instoactual.coditem + ") DEL CJTO(" + coditem + ")";
                    saldosDetalleItem.valor = (float)instoactual.cantidad;
                    listaSaldos.Add(saldosDetalleItem);
                }
                else
                {
                    //saldosDetalleItem.txtReservaProf = "(-) PROFORMAS APROBADAS: " + instoactual.coditem;
                    sldosItemCompleto saldosDetalleItem = new sldosItemCompleto();
                    saldosDetalleItem.descripcion = "(+)SALDO ACTUAL ITEM: " + instoactual.coditem;
                    saldosDetalleItem.valor = (float)instoactual.cantidad;
                    listaSaldos.Add(saldosDetalleItem);
                }
                saldoItemTotal.valor = (float)instoactual.cantidad;


                // obtiene reservas en proforma
                List<saldosObj> saldosReservProformas = await getReservasProf(conexion, coditem, codalmacen, obtener_cantidades_aprobadas_de_proformas, eskit); 

                
                string codigoBuscado = instoactual.coditem;

                var reservaProf = saldosReservProformas.FirstOrDefault(obj => obj.coditem == codigoBuscado);
                
                instoactual.coditem = coditem;
                if (eskit)
                {
                    sldosItemCompleto saldosDetalleItem = new sldosItemCompleto();
                    saldosDetalleItem.descripcion = "(-) PROFORMAS APROBADAS ITEM(" + instoactual.coditem + ") DEL CJTO(" + coditem + ")";
                    saldosDetalleItem.valor = (float)reservaProf.TotalP * -1;
                    listaSaldos.Add(saldosDetalleItem);
                }
                else
                {
                    sldosItemCompleto saldosDetalleItem = new sldosItemCompleto();
                    saldosDetalleItem.descripcion = "(-) PROFORMAS APROBADAS: " + instoactual.coditem;
                    saldosDetalleItem.valor = (float)reservaProf.TotalP * -1;
                    listaSaldos.Add(saldosDetalleItem);
                }

                saldoItemTotal.valor -= (float)reservaProf.TotalP;  // reduce saldo total
                var8.valor = saldoItemTotal.valor;






                // pivote variable para agregar a la lista
                sldosItemCompleto var1 = new sldosItemCompleto();

                // obtiene items si no son kit, sus reservas para armar conjuntos.
                float CANTIDAD_RESERVADA = await getReservasCjtos(userConnectionString, coditem, codalmacen, codempresa, eskit, (float)instoactual.cantidad, (float)reservaProf.TotalP);
                var1.descripcion = "(-) SALDO RESERVADO PARA ARMAR CJTOS";
                var1.valor = CANTIDAD_RESERVADA * -1;
                listaSaldos.Add(var1);
                saldoItemTotal.valor -= CANTIDAD_RESERVADA;  // reduce saldo total

                // obtiene el saldo minimo que debe mantenerse en agencia
                sldosItemCompleto var2 = new sldosItemCompleto();
                float Saldo_Minimo_Item = await empaque_func.getSaldoMinimo(userConnectionString, coditem);
                var2.descripcion = "(-) SALDO MINIMO DEL ITEM";
                var2.valor = Saldo_Minimo_Item * -1;
                listaSaldos.Add(var2);
                saldoItemTotal.valor -= Saldo_Minimo_Item;  // reduce saldo total

                // obtiene reserva NM ingreso para sol-Urgente
                bool validar_ingresos_solurgentes = await empaque_func.getValidaIngreSolurgente(userConnectionString, codempresa);
                
                float total_reservado = 0;
                float total_para_esta = 0;
                float total_proforma = 0;
                if (validar_ingresos_solurgentes)
                {
                    //  RESTAR LAS CANTIDADES DE INGRESO POR NOTAS DE MOVIMIENTO URGENTES
                    // de facturas que aun no estan aprobadas
                    string resp_total_reservado = await getSldIngresoReservNotaUrgent(userConnectionString, coditem, codalmacen);
                    total_reservado = float.Parse(resp_total_reservado);
                    

                    //AUMENTAR CANTIDAD PARA ESTA PROFORMA DE INGRESO POR NOTAS DE MOVIMIENTO URGENTES
                    total_para_esta = await getSldReservNotaUrgentUnaProf(userConnectionString, coditem, codalmacen, "''", 0);
                    

                    //AUMENTAR LA CANTIDAD DE LA PROFORMA DE ESTA NOTA QUE PUEDE ESTAR COMO RESERVADA.
                    total_proforma = await getSldReservProf(userConnectionString, coditem, codalmacen, "''", 0);
                    
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


                // PARA LA SEGUNDA PESTAÑA (SALDO VARIABLE)
                // Verifica si el usuario no tiene acceso devuelve la lista.
                bool ver_detalle_saldo_variable = await empaque_func.ve_detalle_saldo_variable(userConnectionString, usuario);
                if (!ver_detalle_saldo_variable)
                {
                    return Ok(listaSaldos);
                }
                // si el usuario tiene acceso se llenan los datos de la segunda tabla:
                inreserva_area inreserva_Area = await empaque_func.get_inreserva_area(userConnectionString, coditem, codalmacen);
                if (inreserva_Area == null)
                {
                    return Ok(listaSaldos);
                }
                // promedio de venta
                sldosItemCompleto var6 = new sldosItemCompleto();
                var6.descripcion = "Promedio de Vta.";
                var6.valor = (float)inreserva_Area.promvta;
                listaSaldos.Add(var6);

                // stock minimo
                sldosItemCompleto var7 = new sldosItemCompleto();
                var7.descripcion = "Stock Mínimo.";
                var7.valor = (float)inreserva_Area.smin;
                listaSaldos.Add(var7);

                // saldo actual (Saldo Seg Kardex - Cant Prof Ap)
                var8.descripcion = "Saldo Actual (Saldo Seg Kardex - Cant Prof Ap)";
                listaSaldos.Add(var8);

                // % Vta Permitido: 0.53% pero solo se toma: 50%
                sldosItemCompleto var9 = new sldosItemCompleto();
                var9.descripcion = "% Vta Permitido: 0.53% pero solo se toma: 50%";
                var9.valor = (float)inreserva_Area.porcenvta;
                listaSaldos.Add(var9);

                // Reserva para Vta en Cjto
                sldosItemCompleto var10 = new sldosItemCompleto();
                var10.descripcion = "Reserva para Vta en Cjto";
                var10.valor = CANTIDAD_RESERVADA;
                listaSaldos.Add(var10);

                // Saldo para Vta sueltos
                sldosItemCompleto var11 = new sldosItemCompleto();
                var11.descripcion = "Saldo para Vta sueltos";
                var11.valor = saldoItemTotal.valor;
                listaSaldos.Add(var11);


                return Ok(listaSaldos);

            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }


        private async Task<float> getSldReservProf(string userConnectionString, string coditem, int codalmacen, string idProf, int nroIdProf)
        {
            // verifica si es almacen o tienda
            float respuestaValor = 0;

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





        private async Task<string> getSldIngresoReservNotaUrgent(string userConnectionString, string coditem, int codalmacen)
        {
            // verifica si es almacen o tienda
            string respuestaValor = "";

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

                    respuestaValor = respuesta.Value.ToString();
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

                    respuestaValor = respuesta.Value.ToString();
                }
            }
            return respuestaValor;
        }



        private async Task<float> getSldReservNotaUrgentUnaProf(string userConnectionString, string coditem, int codalmacen, string idProf, int nroIdProf)
        {
            // verifica si es almacen o tienda
            float total_para_esta = 0;

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



        private async Task<float> getReservasCjtos(string userConnectionString, string coditem, int codalmacen, string codempresa, bool eskit, float _saldoActual, float reservaProf)
        {
            List<inctrlstock> itemsinReserva = null;
            float CANTIDAD_RESERVADA = 0;
            
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
                    float cubrir_item = (float)(itemRef.cantidad * (item.porcentaje / 100));
                    //cubrir_item = Math.Floor(cubrir_item);
                    CANTIDAD_RESERVADA += cubrir_item;
                }
                */
                var cantidadReservadaTasks = itemsinReserva.Select(async item =>
                {
                    instoactual itemRef = await empaque_func.GetSaldosActual(userConnectionString, codalmacen, item.coditemcontrol);
                    return (float)(itemRef.cantidad * (item.porcentaje / 100));
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
                    float cubrir_item = (float)reserva2[0].cantidad;
                    CANTIDAD_RESERVADA += cubrir_item;
                }
                if (CANTIDAD_RESERVADA < 0)
                {
                    CANTIDAD_RESERVADA = 0;
                }

                float resul = 0;
                float reserva_para_cjto = 0;
                float CANTIDAD_RESERVADA_DINAMICA = 0;

                inreserva_area reserva = await empaque_func.Obtener_Cantidad_Segun_SaldoActual_PromVta_SMin_PorcenVta(userConnectionString, coditem, codalmacen);


                if (reserva != null)
                {
                    if ((float)reserva.porcenvta > 0.5)
                    {
                        reserva.porcenvta = (decimal)0.5;
                    }

                    if (_saldoActual >= (double)reserva.smin)
                    {
                        resul = (float)(reserva.porcenvta * reserva.promvta);
                        resul = (float)Math.Round(resul, 2);
                        reserva_para_cjto = _saldoActual - resul - reservaProf;
                    }
                    else
                    {
                        reserva_para_cjto = _saldoActual;
                    }


                    CANTIDAD_RESERVADA_DINAMICA = reserva_para_cjto;
                    if (CANTIDAD_RESERVADA_DINAMICA > 0)
                    {
                        CANTIDAD_RESERVADA = (float)CANTIDAD_RESERVADA_DINAMICA;
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


        private async Task<instoactual> getEmpaquesItemSelect (string conexion, string coditem, int codalmacen, bool eskit)
        {
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
                if (codarea_empaque==-1)
                {
                    return BadRequest( new { resp = "No se pudo obtener el codigo de área" });
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
                        return NotFound(new { resp = "No se encontraron registros con los datos proporcionados." });
                    }
                    return Ok(empaques);
                }
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
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
                float cantMin = await empaque_func.getEmpaqueMinimo(userConnectionString, coditem, codintarifa, codvedescuento);
                float pesoMin = await empaque_func.getPesoItem(userConnectionString, coditem);
                float porcenMaxVnta = await empaque_func.getPorcentMaxVenta(userConnectionString, coditem, codalmacen);

                return Ok(new
                {
                    cantMin = cantMin,
                    pesoMin = pesoMin * cantMin,
                    porcenMaxVnta = "Max Vta: "+ porcenMaxVnta + "% del saldo"
                });
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
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
                List <int> listaAlmacenes = new List<int>();
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
                        return NotFound( new { resp = "No se encontro un registro con este código" });
                    }
                    /*listaAlmacenes.Add((int)codAlmacenes.codalmsald1);
                    listaAlmacenes.Add((int)codAlmacenes.codalmsald2);
                    listaAlmacenes.Add((int)codAlmacenes.codalmsald3);
                    listaAlmacenes.Add((int)codAlmacenes.codalmsald4);
                    listaAlmacenes.Add((int)codAlmacenes.codalmsald5);*/
                    return Ok(codAlmacenes);
                }

            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
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
            catch (Exception)
            {
                return Problem("Error en el servidor");
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
                return BadRequest(new { resp = "Datos no validos verifique por favor!!!" });
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
                    catch (Exception)
                    {
                        dbContexTransaction.Rollback();
                        return Problem("Error en el servidor");
                        throw;
                    }
                }
            }
        }

        [HttpPost]
        [Route("validarProforma/{userConn}/{cadena_controles}/{entidad}/{opcion_validar}")]
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
        public async Task<ActionResult<List<Controles>>> ValidarProforma(string userConn, string cadena_controles, string entidad, string opcion_validar, RequestValidacion RequestValidacion)
        {
            try
            {
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);
                DatosDocVta datosDocVta = new DatosDocVta();
                List<itemDataMatriz> itemDataMatriz = new List<itemDataMatriz>();
                List<vedesextraDatos> vedesextraDatos = new List<vedesextraDatos>();
                List<vedetalleEtiqueta> vedetalleEtiqueta = new List<vedetalleEtiqueta>();
                List<vedetalleanticipoProforma> vedetalleanticipoProforma = new List<vedetalleanticipoProforma>();
                List<verecargosDatos> verecargosDatos = new List<verecargosDatos>();

                datosDocVta = RequestValidacion.datosDocVta;
                itemDataMatriz = RequestValidacion.detalleItemsProf;
                vedesextraDatos = RequestValidacion.detalleDescuentos;
                vedetalleEtiqueta = RequestValidacion.detalleEtiqueta;
                vedetalleanticipoProforma = RequestValidacion.detalleAnticipos;
                verecargosDatos = RequestValidacion.detalleRecargos;


                var resultado = await validar_Vta.DocumentoValido(userConnectionString, cadena_controles, entidad, opcion_validar, datosDocVta, itemDataMatriz, vedesextraDatos, vedetalleEtiqueta, vedetalleanticipoProforma, verecargosDatos);
                if (resultado != null)
                {
                    ///
                    string jsonResult = JsonConvert.SerializeObject(resultado);

                    return Ok(jsonResult);
                }
                else { return BadRequest(new { resp = "No se pudo validar el documento." }); }
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
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
                        return Ok(new {resp = "Se ha actualizado exitosamente el email del cliente en sus Datos y en datos de la Tienda del Cliente." });

                    }
                    catch (Exception)
                    {
                        dbContexTransaction.Rollback();
                        return Problem("Error en el servidor");
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
                    var result = await saldos.infoitem(userConnectionString, coditem,true, codempresa, usuario);
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
                catch (Exception)
                {
                    return Problem("Error en el servidor");
                    throw;
                }
            }
                
        }


        [HttpGet]
        [Route("getItemMatriz_Anadir/{userConn}/{coditem}/{tarifa}/{descuento}/{cantidad_pedida}/{cantidad}/{codcliente}/{opcion_nivel}/{codalmacen}/{desc_linea_seg_solicitud}/{codmoneda}/{fecha}")]
        public async Task<ActionResult<itemDataMatriz>> getItemMatriz_Anadir(string userConn, string coditem, int tarifa, int descuento, decimal cantidad_pedida, decimal cantidad, string codcliente, string opcion_nivel, int codalmacen, string desc_linea_seg_solicitud, string codmoneda, DateTime fecha)
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
                        .FirstOrDefaultAsync();
                    //convertir a la moneda el precio item
                    var monedabase = await ventas.monedabasetarifa(_context, tarifa);
                    precioItem = await tipocambio.conversion(userConnectionString, codmoneda, monedabase, fecha, (decimal)precioItem);
                    precioItem = await cliente.Redondear_5_Decimales(userConnectionString, (decimal)precioItem);
                    //porcentaje de mercaderia
                    decimal porcen_merca = 0;
                    if (codalmacen > 0)
                    {
                        var controla_stok_seguridad = await empresa.ControlarStockSeguridad(userConnectionString, "PE");
                        if (controla_stok_seguridad == true)
                        {
                            //List<sldosItemCompleto> sld_ctrlstock_para_vtas = await saldos.SaldoItem_CrtlStock_Para_Ventas(userConnectionString, "311", codalmacen, coditem, "PE", "dpd3");
                            var sld_ctrlstock_para_vtas = await saldos.SaldoItem_CrtlStock_Para_Ventas(userConnectionString, "311", codalmacen, coditem, "PE", "dpd3");
                            porcen_merca = cantidad * 100 / sld_ctrlstock_para_vtas;
                        }
                        else { porcen_merca = 0; }
                    }
                    else { porcen_merca = 0; }


                    //descuento de nivel del cliente
                    var niveldesc = await cliente.niveldesccliente(_context, codcliente, coditem, tarifa, opcion_nivel, false);

                    //porcentaje de descuento de nivel del cliente
                    var porcentajedesc = await cliente.porcendesccliente(_context, codcliente, coditem, tarifa, opcion_nivel, false);

                    //preciodesc 
                    var preciodesc = await cliente.Preciodesc(userConnectionString, codcliente, codalmacen, tarifa, coditem, desc_linea_seg_solicitud, niveldesc, opcion_nivel);
                    preciodesc = await tipocambio.conversion(userConnectionString, codmoneda, monedabase, fecha, (decimal)preciodesc);
                    preciodesc = await cliente.Redondear_5_Decimales(userConnectionString, preciodesc);
                    //precioneto 
                    var precioneto = await cliente.Preciocondescitem(userConnectionString, codcliente, codalmacen, tarifa, coditem, descuento, desc_linea_seg_solicitud, niveldesc, opcion_nivel);
                    precioneto = await tipocambio.conversion(userConnectionString, codmoneda, monedabase, fecha, (decimal)precioneto);
                    precioneto = await cliente.Redondear_5_Decimales(userConnectionString, precioneto);
                    //total
                    var total = cantidad * precioneto;
                    total = await cliente.Redondear_5_Decimales(userConnectionString, total);

                    var item = await _context.initem
                        .Where(i => i.codigo == coditem)
                        .Select(i => new itemDataMatriz
                        {
                            coditem = i.codigo,
                            descripcion = i.descripcion,
                            medida = i.medida,
                            ud = i.unidad,
                            porceniva = (float)i.iva,
                            cantidad_pedida = (float)cantidad_pedida,
                            cantidad = (float)cantidad,
                            porcen_mercaderia = (float)porcen_merca,
                            codtarifa = tarifa,
                            coddescuento = descuento,
                            preciolista = (float)precioItem,
                            niveldesc = niveldesc,
                            porcendesc = (float)porcentajedesc,
                            preciodesc = (float)preciodesc,
                            precioneto = (float)precioneto,
                            total = (float)total

                        })
                        .FirstOrDefaultAsync();

                    if (item == null)
                    {
                        return NotFound( new { resp = "No se encontro un registro con este código" });
                    }

                    return Ok(item);
                }

            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }

        [Authorize]
        [HttpPost]
        [Route("guardarProforma/{userConn}/{idProf}/{codempresa}")]
        public async Task<object> guardarProforma(string userConn, string idProf, string codempresa,SaveProformaCompleta datosProforma)
        {
            veproforma veproforma = datosProforma.veproforma;
            List<veproforma1> veproforma1 = datosProforma.veproforma1;
            /*
            List<veproforma_valida> veproforma_valida = datosProforma.veproforma_valida;
            List<veproforma_anticipo> veproforma_anticipo = datosProforma.veproforma_anticipo;
            List<vedesextraprof> vedesextraprof = datosProforma.vedesextraprof;
            List<verecargoprof> verecargoprof = datosProforma.verecargoprof;
            List<veproforma_iva> veproforma_iva = datosProforma.veproforma_iva;

            */


            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);


            // ###############################
            // ACTUALIZAR DATOS DE CODIGO PRINCIPAL SI ES APLICABLE
            await cliente.ActualizarParametrosDePrincipal(userConnectionString, veproforma.codcliente);
            // ###############################

            if (veproforma1.Count()<=0)
            {
                return BadRequest(new { resp = "No hay ningun item en su documento!!!" });
            }



            // ###############################  FALTA

            // RECALCULARPRECIOS(True, True);




            




            
            

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                using (var dbContexTransaction = _context.Database.BeginTransaction())
                {
                    try
                    {



                        string result = await Grabar_Documento(_context, idProf, codempresa, datosProforma);
                        if (result != "ok")
                        {
                            dbContexTransaction.Rollback();
                            return BadRequest(new { resp = result });
                        }


                        /*
                         
                        //Grabar Etiqueta
                        If dt_etiqueta.Rows.Count > 0 Then
                            Try
                                If IsDBNull(dt_etiqueta.Rows(0)("celular")) Then
                                    dt_etiqueta.Rows(0)("celular") = "---"
                                End If
                                sia_DAL.Datos.Instancia.EjecutarComando("delete from veetiqueta_proforma where id='" & id.Text & "' and numeroid=" & numeroid.Text)
                                sia_DAL.Datos.Instancia.EjecutarComando("insert into veetiqueta_proforma(id,numeroid,codcliente,linea1,linea2,representante,telefono,ciudad,celular,latitud_entrega,longitud_entrega) values('" & id.Text & "'," & numeroid.Text & ",'" & codcliente.Text & "','" & CStr(dt_etiqueta.Rows(0)("linea1")) & "','" & CStr(dt_etiqueta.Rows(0)("linea2")) & "','" & CStr(dt_etiqueta.Rows(0)("representante")) & "','" & CStr(dt_etiqueta.Rows(0)("telefono")) & "','" & CStr(dt_etiqueta.Rows(0)("ciudad")) & "','" & CStr(dt_etiqueta.Rows(0)("celular")) & "','" & CStr(dt_etiqueta.Rows(0)("latitud_entrega")) & "','" & CStr(dt_etiqueta.Rows(0)("longitud_entrega")) & "' )")
                            Catch ex As Exception

                            End Try
                        End If

                        //ACTUALIZAR PESO
                        sia_DAL.Datos.Instancia.EjecutarComando("update veproforma set peso=" & CStr(sia_funciones.Ventas.Instancia.Peso_Proforma(CInt(codigo.Text))) & " where codigo=" & codigo.Text & "")
                        sia_funciones.Ventas.Instancia.Actualizar_Peso_Detalle_Proforma(CInt(codigo.Text))


                        //enlazar sol desctos con proforma
                        If desclinea_segun_solicitud.Checked = True And idsoldesctos.Text.Trim.Length > 0 And nroidsoldesctos.Text.Trim.Length > 0 Then
                            If Not sia_funciones.Ventas.Instancia.Enlazar_Proforma_Nueva_Con_SolDesctos_Nivel(codigo.Text, idsoldesctos.Text, nroidsoldesctos.Text) Then
                                MessageBox.Show("No se pudo realizar el enlace de esta proforma con la solicitud de descuentos de nivel, verifique el enlace en la solicitu de descuentos!!!", "ErroR de Enlace", MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1)
                            End If
                        End If


                        //grabar la etiqueta dsd 16-05-2022        
                        //solo si es cliente casual, y el cliente referencia o real es un no casual
                        //If sia_funciones.Cliente.Instancia.Es_Cliente_Casual(codcliente.Text) = True And sia_funciones.Cliente.Instancia.Es_Cliente_Casual(codcliente_real) = False Then

                        //Desde 10-10-2022 se definira si una venta es casual o no si el codigo de cliente y el codigo de cliente real son diferentes entonces es una venta casual
                        If Not codcliente.Text = txtcodcliente_real.Text Then
                            If Not Me.Grabar_Proforma_Etiqueta(codigo.Text) Then
                                MessageBox.Show("No se pudo grabar la etiqueta Cliente Casual/Referencia de la proforma!!!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1)
                            End If
                        End If


                        If MessageBox.Show("Se grabo la Proforma " & id.Text & "-" & numeroid.Text & " con Exito. Desea Exportar el documento? ", "Exportar", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) = Windows.Forms.DialogResult.Yes Then
                            Me.exportar_Click()
                        End If

                        //validar lo que se validaba en la ventana de aprobar proforma
                        Dim mi_idpf As String = sia_funciones.Ventas.Instancia.proforma_id(_CODPROFORMA)
                        Dim mi_nroidpf As String = sia_funciones.Ventas.Instancia.proforma_numeroid(_CODPROFORMA)

                        //validar lo que se validaba en la ventana de aprobar proforma
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

                        //Borrar del detalle los Ceros en cantidad
                        For i As Integer = 0 To tabladetalle.Rows.Count - 1
                            If i < tabladetalle.Rows.Count Then
                                If tabladetalle.Rows(i)("cantidad") = 0 Then
                                    tabladetalle.Rows(i).Delete()
                                    i = i - 1
                                End If
                            End If
                        Next

                        //mostrar dialogo de imprimir
                        Preguntar_Tipo_Impresion()

                        limpiardoc()
                        mostrardatos("0")
                        leerparametros()
                        ponerpordefecto()
                        id.Focus()
                         
                         
                         */


                        dbContexTransaction.Commit();
                        return Ok(new { resp = "Se Grabo la Proforma de manera Exitosa" });
                    }
                    catch (Exception)
                    {
                        dbContexTransaction.Rollback();
                        return Problem("Error en el servidor");
                        throw;
                    }
                }
            }
        }



        private async Task<string> Grabar_Documento(DBContext _context, string idProf, string codempresa, SaveProformaCompleta datosProforma)
        {
            veproforma veproforma = datosProforma.veproforma;
            List<veproforma1> veproforma1 = datosProforma.veproforma1;
            List<veproforma_valida> veproforma_valida = datosProforma.veproforma_valida;
            List<veproforma_anticipo> veproforma_anticipo = datosProforma.veproforma_anticipo;
            List<vedesextraprof> vedesextraprof = datosProforma.vedesextraprof;
            List<verecargoprof> verecargoprof = datosProforma.verecargoprof;
            List<veproforma_iva> veproforma_iva = datosProforma.veproforma_iva;

            ////////////////////   GRABAR DOCUMENTO

            int _ag = await empresa.AlmacenLocalEmpresa(_context, codempresa);
            // verificar si valido el documento, si es tienda no es necesario que valide primero
            if (!await almacen.Es_Tienda(_context, _ag))
            {
                if (veproforma_valida.Count() < 1)
                {
                    return "Antes de grabar el documento debe previamente validar el mismo!!!";
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
                return "Error al obtener los datos de numero de proforma";
            }

            // valida si existe ya la proforma
            if (await datos_proforma.existeProforma(_context, idProf, idnroactual))
            {
                return "Ese numero de documento, ya existe, por favor consulte con el administrador del sistema.";
            }

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
            
            // guarda detalle (veproforma1)
            // actualizar codigoproforma para agregar
            veproforma1 = veproforma1.Select(p => { p.codproforma = codProforma; return p; }).ToList();

            _context.veproforma1.AddRange(veproforma1);
            await _context.SaveChangesAsync();

            // actualiza numero id
            var numeracion = _context.venumeracion.FirstOrDefault(n => n.id == idProf);
            numeracion.nroactual += 1;
            await _context.SaveChangesAsync(); // Guarda los cambios en la base de datos

            //======================================================================================
            // grabar detalle de validacion
            //======================================================================================

            veproforma_valida = veproforma_valida.Select(p => { p.codproforma = codProforma; return p; }).ToList();
            _context.veproforma_valida.AddRange(veproforma_valida);
            await _context.SaveChangesAsync();

            //======================================================================================
            //grabar anticipos aplicados
            //======================================================================================

            var anticiposprevios = await _context.veproforma_anticipo.Where(i => i.codproforma == codProforma).ToListAsync();
            if (anticiposprevios.Count() > 0)
            {
                _context.veproforma_anticipo.RemoveRange(anticiposprevios);
                await _context.SaveChangesAsync();
            }
            veproforma_anticipo = veproforma_anticipo.Select(p => { p.codproforma = codProforma; return p; }).ToList();
            _context.veproforma_anticipo.AddRange(veproforma_anticipo);
            await _context.SaveChangesAsync();


            // grabar descto por deposito si hay descuentos

            if (vedesextraprof.Count() > 0)
            {
                await grabardesextra(_context, codProforma, vedesextraprof);
            }

            // grabar recargo si hay recargos
            if (verecargoprof.Count > 0)
            {
                await grabarrecargo(_context, codProforma, verecargoprof);
            }

            // grabar iva

            await grabariva(_context, codProforma, veproforma_iva);


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


            return "ok";


        }


        private async Task grabardesextra(DBContext _context, int codProf, List<vedesextraprof> vedesextraprof)
        {
            var descExtraAnt = await _context.vedesextraprof.Where(i => i.codproforma == codProf).ToListAsync();
            if (descExtraAnt.Count()>0)
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
            _context.veproforma_iva.RemoveRange(veproforma_iva);
            await _context.SaveChangesAsync();
        }


        private async Task<string> RECALCULARPRECIOS(DBContext _context, string usuario, string codmoneda, DateTime fecha, int codtarifadefect, int codalmacen, string idsoldesctos, int nroidsoldesctos, string codcliente_real, bool reaplicar_desc_deposito, bool preguntar_si_aplicare_desc_deposito, bool desclinea_segun_solicitud, string cmbniveles_descuento, string codempresa, List<itemDataMatriz> tabla_detalle, List<verecargoprof> tablarecargos)
        {
            //aplicar desctos primavera 2022
            //Aplicar_Descto_Primavera2022(False)

            ////////////////////////////////////////////////////////////
            //verificar los precios permitidos al usuario
            string cadena_precios_no_autorizados_al_us = await validar_Vta.Validar_Precios_Permitidos_Usuario(_context, usuario, tabla_detalle);
            if (cadena_precios_no_autorizados_al_us.Trim().Length > 0)
            {
                return "El documento tiene items a precio(s): " + cadena_precios_no_autorizados_al_us + " los cuales no estan asignados al usuario " + usuario + " verifique esta situacion!!!";
            }
            ////////////////////////////////////////////////////////////



            ////////////////////////////////////////////////////////////
            //validar si es con descuentos de linea de una solicitu previamente creada
            
            if (!desclinea_segun_solicitud)
            {
                //ojo aqui revisar
                if (await ventas.Tarifa_Permite_Desctos_Linea(_context, codtarifadefect))
                {

                }
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
                if (! await ventas.Existe_Solicitud_Descuento_Nivel(_context, idsoldesctos, nroidsoldesctos))
                {
                    return "Ha elegido utilizar la solicitud de descuentos de nivel: " + idsoldesctos + "-" + nroidsoldesctos + " para aplicar descuentos de linea, pero la solicitud indicada no existe!!!";
                }
                if (codcliente_real != await ventas.Cliente_Solicitud_Descuento_Nivel(_context,idsoldesctos,nroidsoldesctos))
                {
                    return "La solicitud de descuentos de nivel: " + idsoldesctos + "-" + nroidsoldesctos + " a la que hace referencia no pertenece al mismo cliente de esta proforma!!!";
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
            foreach (var reg in tabla_detalle)
            {
                reg.cantidad = (float)Math.Round(reg.cantidad, 2, MidpointRounding.AwayFromZero);
                //***************************************************
                ////aqui se verifica si los decuentos de nivel se calculan con solicitud directo o lo que esta cargado en sus desctos del cliente
                if (desclinea_segun_solicitud)
                {
                    if (idsoldesctos.Trim().Length == 0 || nroidsoldesctos.ToString().Length == 0)
                    {
                        //si la proforma es con descuentos de nivel de una solicitud entonces se recupera de la solicitud los descuentos
                        reg.niveldesc = "X";
                        reg.porcendesc = 0;
                    }
                    else
                    {
                        //si la proforma es con descuentos de nivel de una solicitud entonces se recupera de la solicitud los descuentos
                        reg.niveldesc = await cliente.niveldesccliente_segun_solicitud(_context, idsoldesctos, nroidsoldesctos, codcliente_real, reg.coditem, reg.codtarifa);
                        reg.porcendesc = (float)await cliente.porcen_desccliente_segun_solicitud(_context, idsoldesctos, nroidsoldesctos, codcliente_real, reg.coditem, reg.codtarifa);
                    }
                }
                else
                {
                    reg.niveldesc = await cliente.niveldesccliente(_context,codcliente_real,reg.coditem,reg.codtarifa, cmbniveles_descuento, false);
                    reg.porcendesc = (float)await cliente.porcendesccliente(_context, codcliente_real, reg.coditem, reg.codtarifa, cmbniveles_descuento, false);

                }

                //***************************************************

                /*
                 If codalmacen.Text.Trim = "" Then
                    tabladetalle.Rows(i)("porcen_mercaderia") = 0.0
                Else
                    Try

                        '///////////////////////////////////////////////////////////////////////
                        '//verificar si el item esta en los que no reservan saldos
                        '///////////////////////////////////////////////////////////////////////

                        'tabladetalle.Rows(i)("porcen_mercaderia") = 0.0

                        'Dim FACTOR_RESERVA_SALDO As Integer = 0
                        'If sia_funciones.Inventario.Instancia.Item_No_Reserva_Saldo(CStr(tabladetalle.Rows(i)("coditem"))) Then
                        '    FACTOR_RESERVA_SALDO = 0
                        'Else
                        '    FACTOR_RESERVA_SALDO = 1
                        'End If

                        'If Me.Usar_Bd_Opcional Then
                        '    If sia_funciones.Empresa.Instancia.ControlarStockSeguridad(sia_compartidos.temporales.Instancia.codempresa) Then
                        '        tabladetalle.Rows(i)("porcen_mercaderia") = FACTOR_RESERVA_SALDO * ((CDbl(tabladetalle.Rows(i)("cantidad")) * 100) / sia_funciones.Saldos.Instancia.SaldoItem_CrtlStock_Para_Ventas(CStr(tabladetalle.Rows(i)("coditem")), sia_funciones.Configuracion.Instancia.usr_codalmacen_saldo_1(sia_compartidos.temporales.Instancia.usuario), True, "", 0, True))
                        '    Else
                        '        tabladetalle.Rows(i)("porcen_mercaderia") = FACTOR_RESERVA_SALDO * ((CDbl(tabladetalle.Rows(i)("cantidad")) * 100) / sia_funciones.Saldos.Instancia.SaldoItem_CrtlStock_Para_Ventas(CStr(tabladetalle.Rows(i)("coditem")), sia_funciones.Configuracion.Instancia.usr_codalmacen_saldo_1(sia_compartidos.temporales.Instancia.usuario), False, "", 0, True))
                        '    End If
                        'Else
                        '    If sia_funciones.Empresa.Instancia.ControlarStockSeguridad(sia_compartidos.temporales.Instancia.codempresa) Then
                        '        tabladetalle.Rows(i)("porcen_mercaderia") = FACTOR_RESERVA_SALDO * ((CDbl(tabladetalle.Rows(i)("cantidad")) * 100) / sia_funciones.Saldos.Instancia.SaldoItem_CrtlStock_Para_Ventas(CStr(tabladetalle.Rows(i)("coditem")), CInt(codalmacen.Text), True, "", 0, True))
                        '    Else
                        '        tabladetalle.Rows(i)("porcen_mercaderia") = FACTOR_RESERVA_SALDO * ((CDbl(tabladetalle.Rows(i)("cantidad")) * 100) / sia_funciones.Saldos.Instancia.SaldoItem_CrtlStock_para_ventas(CStr(tabladetalle.Rows(i)("coditem")), CInt(codalmacen.Text), False,"",0,true))
                        '    End If
                        'End If

                    Catch ex As Exception
                        tabladetalle.Rows(i)("porcen_mercaderia") = 0.0
                    End Try
                End If
                 */

                //CALCULO DEL PRECIO FINAL CON DESCUENTO DE LINEA
                if (reg.codtarifa > 0)
                {
                    string monBase = await ventas.monedabasetarifa(_context, reg.codtarifa);

                    reg.preciolista = (float)await tipocambio._conversion(_context, codmoneda, monBase, fecha, (decimal)await ventas.preciodelistaitem(_context, reg.codtarifa, reg.coditem));
                    // redondear a 5 decimales
                    var precio_lista = reg.preciolista;
                    precio_lista = (float)await siat.Redondeo_Decimales_SIA_5_decimales_SQL(precio_lista);
                    reg.preciolista = precio_lista;

                    // si hay descuento
                    reg.preciodesc = tipocambio._conversion(_context, codmoneda, monBase, fecha, await ventas.preciocliente(_context, codcliente_real, codalmacen, reg.codtarifa, reg.coditem, (desclinea_segun_solicitud == true) ? "SI" : "NO", reg.niveldesc, cmbniveles_descuento));
                    reg.precioneto = tipocambio._conversion(_context, codmoneda, monBase, fecha, await ventas.preciocondescitem(_context, codcliente_real, codalmacen, reg.codtarifa, reg.coditem, reg.coddescuento, (desclinea_segun_solicitud == true) ? "SI" : "NO", reg.niveldesc, cmbniveles_descuento));

                    // redondear a 5 decimales
                    var preciodesc = reg.preciodesc;
                    preciodesc = (float)await siat.Redondeo_Decimales_SIA_5_decimales_SQL(preciodesc);
                    reg.preciodesc = preciodesc;

                    // redondear a 5 decimales
                    var precioneto = reg.precioneto;
                    precioneto = (float)await siat.Redondeo_Decimales_SIA_5_decimales_SQL(precioneto);
                    reg.precioneto = precioneto;

                    if (reg.cantidad > 0)
                    {
                        reg.total = reg.cantidad * reg.precioneto;
                        // redondear a 5 decimales
                        var total_item = reg.total;
                        total_item = (float)await siat.Redondeo_Decimales_SIA_5_decimales_SQL(total_item);
                        reg.total = total_item;
                    }
                    else
                    {
                        reg.total = 0;
                    }

                }
                else
                {
                    reg.preciolista = 0;
                    reg.preciodesc = 0;
                    reg.precioneto = 0;
                    reg.total = 0;
                }

            }
            var result = await versubtotal(_context,tabla_detalle);
            float subtotal = result.st;
            float peso = result.peso;
            if (reaplicar_desc_deposito)
            {
                // Revisar_Aplicar_Descto_Deposito(preguntar_si_aplicare_desc_deposito);
            }

            var recargo = await verrecargos(_context,codempresa, codmoneda, fecha, subtotal, tablarecargos);
            //verdesextra();
            //vertotal();
            //QUITAR
            return "";

        }


        private async Task<(float st, float peso)> versubtotal(DBContext _context, List<itemDataMatriz> tabla_detalle)
        {
            // filtro de codigos de items
            tabla_detalle = tabla_detalle.Where(item => item.coditem != null && item.coditem.Length >= 8).ToList();
            // calculo subtotal
            float peso = 0;
            float st = 0;

            foreach (var reg in tabla_detalle)
            {
                st = st + reg.total;
                peso = (float)(peso + (await items.itempeso(_context, reg.coditem)) * reg.cantidad);
            }

            // desde 08/01/2023 redondear el resultado a dos decimales con el SQLServer
            // REVISAR SI HAY OTRO MODO NO DA CON LINQ.
            st = (float)await siat.Redondeo_Decimales_SIA_5_decimales_SQL(st);
            return (st, peso);
        }

        private async Task<float> verrecargos(DBContext _context, string codempresa, string codmoneda,DateTime fecha, float subtotal, List<verecargoprof> tablarecargos)
        {
            int codrecargo_pedido_urg_provincia = await configuracion.emp_codrecargo_pedido_urgente_provincia(_context, codempresa);
            //TOTALIZAR LOS RECARGOS QUE NO SON POR PEDIDO URG PROVINCIAS (los que se aplican al total final)
            float total = 0;
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
                    total = total + (float) reg.montodoc;
                }
            }
            return total;

        }
        private async Task<float> verdesextra(DBContext _context, string codempresa, string nit, List<vedesextraprof> tabladescuentos, List<itemDataMatriz> detalleProf)
        {
            int coddesextra_depositos = await configuracion.emp_coddesextra_x_deposito(_context, codempresa);
            tabladescuentos = await ventas.Ordenar_Descuentos_Extra(_context, tabladescuentos);
            //calcular el monto  de descuento segun el porcentaje
            ////////////////////////////////////////////////////////////////////////////////
            //primero calcular los montos de los que se aplican en el detalle o son
            //diferenciados por item
            ////////////////////////////////////////////////////////////////////////////////
            foreach (var reg in tabladescuentos)
            {
                //verifica si el descuento es diferenciado por item
                if ( await ventas.DescuentoExtra_Diferenciado_x_item(_context,reg.coddesextra))
                {
                    var resp = await ventas.DescuentoExtra_CalcularMonto(_context, reg.coddesextra, detalleProf, "", nit);
                    double monto_desc = resp.resultado;
                    detalleProf = resp.dt;

                    //si hay complemento, verificar cual es el complemento


                }
            }
            // QUITAR
            return 0;

        }

    }
}
