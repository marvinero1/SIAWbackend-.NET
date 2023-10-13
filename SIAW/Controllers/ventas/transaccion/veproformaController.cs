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

namespace SIAW.Controllers.ventas.transaccion
{
    [Route("api/venta/transac/[controller]")]
    [ApiController]
    public class veproformaController : ControllerBase
    {

        private readonly UserConnectionManager _userConnectionManager;
        private siaw_funciones.empaquesFunciones empaque_func = new siaw_funciones.empaquesFunciones();
        private siaw_funciones.datosProforma datos_proforma = new siaw_funciones.datosProforma();
        private siaw_funciones.ClienteCasual clienteCasual = new siaw_funciones.ClienteCasual();
        private siaw_funciones.Cliente cliente = new siaw_funciones.Cliente();
        private siaw_funciones.Empresa empresa = new siaw_funciones.Empresa();
        private siaw_funciones.Saldos saldos = new siaw_funciones.Saldos();
        private siaw_funciones.TipoCambio tipocambio = new siaw_funciones.TipoCambio();
        private siaw_funciones.Ventas ventas = new siaw_funciones.Ventas();
        private siaw_funciones.Items items = new siaw_funciones.Items();

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
                        return NotFound("No se encontro un registro con este código (cod almacen)");
                    }

                    return Ok(codalmacen);
                }

            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }



        // GET: api/adsiat_tipodocidentidad
        [HttpGet]
        [Route("catalogoNumProf/{userConn}/{codUsuario}")]
        public async Task<ActionResult<IEnumerable<venumeracion>>> catalogoNumProf(string userConn, string codUsuario)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var resultado = await _context.venumeracion
                        .Where(v => v.tipodoc == 2 && v.habilitado == true &&
                                    _context.adusuario_idproforma
                                        .Where(a => a.usuario == codUsuario)
                                        .Select(a => a.idproforma)
                                        .Contains(v.id))
                        .OrderBy(v => v.id)
                        .Select(v => new
                        {
                            codigo = v.id,
                            descripcion = v.descripcion
                        })
                        .ToListAsync();
                    return Ok(resultado);
                }
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
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
                        return Problem("Entidad adtipocambio es null.");
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
                return BadRequest("Error en el servidor");
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
                    return Problem("No se pudo obtener la cadena de conexión");
                }

                var instoactual = await empaque_func.GetSaldosActual(ad_conexion_vpnResult, codalmacen, coditem);
                if (instoactual == null)
                {
                    return NotFound("No existe un registro con esos datos");
                }
                return Ok(instoactual);
                //return Ok(ad_conexion_vpnResult);
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
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
                    return NotFound("No existe un registro con esos datos");
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
                bool bandera = false;
                bool item_reserva_para_conjunto = false;  // ayuda a verificar si el item no kit es utilizado para armar conjuntos.



                // Falta validacion para saber si traera datos de manera local o por vpn
                if (bandera)
                {
                    conexion = empaque_func.Getad_conexion_vpnFromDatabase(userConnectionString, agencia);
                    if (conexion == null)
                    {
                        return Problem("No se pudo obtener la cadena de conexión");
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
                saldoItemTotal.valor -= total_para_esta;  // reduce saldo total

                sldosItemCompleto var5 = new sldosItemCompleto();
                var5.descripcion = "(+) CANTIDAD RESERVADA PROFORMA APROBADA: -0";
                var5.valor = total_proforma;
                listaSaldos.Add(var5);
                saldoItemTotal.valor -= total_proforma;  // reduce saldo total


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
                foreach (var item in itemsinReserva)
                {
                    instoactual itemRef = await empaque_func.GetSaldosActual(userConnectionString, codalmacen, item.coditemcontrol);
                    float cubrir_item = (float)(itemRef.cantidad * (item.porcentaje / 100));
                    //cubrir_item = Math.Floor(cubrir_item);
                    CANTIDAD_RESERVADA += cubrir_item;
                }
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
                    return Problem("No se pudo obtener el codigo de área");
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
                        return Problem("No se encontraron datos.");
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
                        return NotFound("No se encontro un registro con este código");
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
                return BadRequest("Error en el servidor");
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
                int nroactual = await datos_proforma.getNumActProd(userConnectionString, id);

                if (nroactual == null)
                {
                    return NotFound("No se encontro un registro con este código");
                }
                return Ok(nroactual);
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
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
                return BadRequest("Datos no validos verifique por favor!!!");
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
                            return BadRequest(new { message = "Error al crear el cliente" });
                        }
                        dbContexTransaction.Commit();
                        return Ok(new { message = "Cliente creado exitosamente" });

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

        [HttpPut]
        [Route("actualizarCorreoCliente/{userConn}")]
        public async Task<object> actualizarCorreoCliente(string userConn, updateEmailClient data)
        {
            string codcliente = data.codcliente;
            string email = data.email;

            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            if (await clienteCasual.EsClienteSinNombre(userConnectionString, codcliente))
            {
                return "No se puede actualizar el correo de un codigo SIN NOMBRE!!!";
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
                            return BadRequest("No se pudo actualizar el email del cliente");
                        }

                        bool actualizaEmailTienda = await clienteCasual.actualizarEmailClienteTienda(_context, codcliente, email);
                        if (!actualizaEmailTienda)
                        {
                            return BadRequest("No se pudo actualizar el email del cliente (tienda)");
                            throw new Exception();
                        }

                        dbContexTransaction.Commit();
                        return Ok(new {message = "Se ha actualizado exitosamente el email del cliente en sus Datos y en datos de la Tienda del Cliente." });

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
                    var monedabase = await ventas.monedabasetarifa(userConnectionString, tarifa);
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
                    var niveldesc = await cliente.niveldesccliente(userConnectionString, codcliente, coditem, tarifa, opcion_nivel, false);

                    //porcentaje de descuento de nivel del cliente
                    var porcentajedesc = await cliente.porcendesccliente(userConnectionString, codcliente, coditem, tarifa, opcion_nivel, false);

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
                            porcenIV = (float)i.iva,
                            pedido = (float)cantidad_pedida,
                            cantidad = (float)cantidad,
                            porcentSld = (float)porcen_merca,
                            tp = tarifa,
                            de = descuento,
                            pul = (float)precioItem,
                            ni = niveldesc,
                            porcen = (float)porcentajedesc,
                            pd = (float)preciodesc,
                            pu = (float)precioneto,
                            total = (float)total

                        })
                        .FirstOrDefaultAsync();

                    if (item == null)
                    {
                        return NotFound("No se encontro un registro con este código");
                    }

                    return Ok(item);
                }

            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }



        // GET: api/catalogo
        [HttpGet]
        [Route("catalogoVetiposoldsctos/{userConn}")]
        public async Task<ActionResult<IEnumerable<vetiposoldsctos>>> catalogoVetiposoldsctos(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                //var _context = _userConnectionManager.GetUserConnection(userId);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var result = _context.vetiposoldsctos
                    .Select(i => new
                    {
                        codigo = i.id,
                        descripcion = i.descripcion
                    })
                    .ToListAsync();

                    return Ok(result);
                }

            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
                throw;
            }
        }

        // GET: api/catalogo
        [HttpGet]
        [Route("catalogoVenumeracionProf/{userConn}")]
        public async Task<ActionResult<IEnumerable<vetiposoldsctos>>> catalogoVenumeracionProf(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                //var _context = _userConnectionManager.GetUserConnection(userId);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var result = _context.venumeracion
                    .Where(e => e.tipodoc == 2 && e.habilitado == true)
                    .OrderBy(e => e.id)
                    .Select(e => new
                    {
                        codigo = e.id,
                        descripcion = e.descripcion
                    })
                    .ToListAsync();

                    return Ok(result);
                }

            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
                throw;
            }
        }



        [HttpPost]
        [Route("guardarProforma/{userConn}/{idProf}/{hora_inicial}/{fecha_inicial}")]
        public async Task<object> guardarProforma(string userConn, string idProf, string hora_inicial, DateTime fecha_inicial, SaveProformaCompleta datosProforma)
        {
            veproforma veproforma = datosProforma.veproforma;
            List<veproforma1> veproforma1 = datosProforma.veproforma1;
            List<veproforma_valida> veproforma_valida = datosProforma.veproforma_valida;
            veproforma_anticipo veproforma_anticipo = datosProforma.veproforma_anticipo;
            List<vedesextraprof> vedesextraprof = datosProforma.vedesextraprof;
            List<verecargoprof> verecargoprof = datosProforma.verecargoprof;
            veproforma_iva veproforma_iva = datosProforma.veproforma_iva;




            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            /*
            string datosValidos = await clienteCasual.validar_crear_cliente(userConnectionString, cliCasual.codSN, cliCasual.nit_cliente_casual, cliCasual.tipo_doc_cliente_casual);
            if (datosValidos != "Ok")
            {
                return BadRequest("Datos no validos verifique por favor!!!");
            }
            */

            // validar detalle()
            // validar datos()

            //obtenemos numero actual de proforma de nuevo
            int idnroactual = await datos_proforma.getNumActProd(userConnectionString, idProf);

            if (idnroactual == 0)
            {
                return BadRequest("Error al obtener los datos de numero de proforma");
            }
            // valida si existe ya la proforma
            if (await datos_proforma.existeProforma(userConnectionString, idProf, idnroactual))
            {
                return BadRequest("Ese numero de documento, ya existe, por favor consulte con el administrador del sistema.");
            }

            // obtener hora y fecha actual si es que la proforma no se importo
            if (veproforma.hora_inicial == "")
            {
                veproforma.fecha_inicial = DateTime.Parse(datos_proforma.getFechaActual());
                veproforma.hora_inicial = datos_proforma.getHoraActual();
            }
            // obtener ultimo codigo de proforma y aumentar + 1
            int codigoProf = await datos_proforma.ultimoCodProf(userConnectionString) + 1;
            

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                using (var dbContexTransaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        if (veproforma1.Count < 1)
                        {
                            return BadRequest("No se tiene detalle de la proforma");
                        }
                        // guarda cabecera (veproforma)
                        _context.veproforma.Add(veproforma);
                        await _context.SaveChangesAsync();


                        // guarda detalle (veproforma1)
                        _context.veproforma1.AddRange(veproforma1);
                        await _context.SaveChangesAsync();

                        // actualiza numero id
                        var numeracion = _context.venumeracion.FirstOrDefault(n => n.id == idProf);
                        numeracion.nroactual += 1;
                        await _context.SaveChangesAsync(); // Guarda los cambios en la base de datos

                        // guarda el detalle de validacion
                        _context.veproforma_valida.AddRange(veproforma_valida);
                        await _context.SaveChangesAsync();

                        // grabar anticipos aplicados
                        _context.veproforma_anticipo.Add(veproforma_anticipo);
                        await _context.SaveChangesAsync();

                        // grabar descto por deposito si hay descuentos
                        Requestgrabardesextra requestgrabardesextra = new Requestgrabardesextra();
                        requestgrabardesextra.context = _context;
                        requestgrabardesextra.vedesextraprofList = vedesextraprof;
                        if (vedesextraprof.Count > 0)
                        {
                            await grabardesextra(codigoProf, requestgrabardesextra);
                        }

                        // grabar recargo si hay recargos
                        Requestgrabarrecargo requestgrabarrecargo = new Requestgrabarrecargo();
                        requestgrabarrecargo.context = _context;
                        requestgrabarrecargo.verecargoprof = verecargoprof;
                        if (verecargoprof.Count > 0)
                        {
                            await grabarrecargo(codigoProf, requestgrabarrecargo);
                        }
                        

                        return Ok("Se Grabo la Proforma de manera Exitosa");
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

        private async Task grabardesextra(int codProf, Requestgrabardesextra requestgrabardesextra)
        {
            DBContext _context = requestgrabardesextra.context;
            List<vedesextraprof> vedesextraprof = requestgrabardesextra.vedesextraprofList;
            foreach (var item in vedesextraprof)
            {
                item.codproforma = codProf;
                if (item.codcobranza == null)
                {
                    item.codproforma = 0;
                }
                if (item.codcobranza_contado == null)
                {
                    item.codcobranza_contado = 0;
                }
                if (item.codanticipo == null)
                {
                    item.codanticipo = 0;
                }
            }
            _context.vedesextraprof.AddRange(vedesextraprof);
            await _context.SaveChangesAsync();
        }


        private async Task grabarrecargo(int codProf, Requestgrabarrecargo requestgrabarrecargo)
        {
            List<verecargoprof> verecargoprof = requestgrabarrecargo.verecargoprof;
            DBContext _context = requestgrabarrecargo.context;
            foreach (var item in verecargoprof)
            {
                item.codproforma = codProf;
            }
            _context.verecargoprof.AddRange(verecargoprof);
            await _context.SaveChangesAsync();
        }

        private async Task grabariva(int codProf, Requestgrabariva requestgrabariva)
        {
            veproforma_iva veproforma_iva = requestgrabariva.veproforma_iva;
            DBContext _context = requestgrabariva.context;
            veproforma_iva.codproforma = codProf;
            _context.veproforma_iva.Add(veproforma_iva);
            await _context.SaveChangesAsync();
        }

        // POST: api/acaseguradora
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        //[Authorize]
        [HttpGet]
        [Route("getdetalleSldsAgs/{userConn}/{codItem}/{codempresa}/{usuario}")]
        public async Task<ActionResult<acaseguradora>> getdetalleSldsAgs(string userConn, string codItem, string codempresa, string usuario)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                var infoitem = await saldos.infoitem(userConnectionString, codItem, codempresa, usuario);

                return Ok(infoitem);
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }


    }
}
