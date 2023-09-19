using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NuGet.Configuration;
using SIAW.Data;
using SIAW.Models;
using SIAW.Models_Extra;
using System.Data;
using System.Drawing;
using System.Security.Policy;
using System.Text;
using System.Web.Http.Results;

namespace SIAW.Controllers.ventas.transaccion
{
    [Route("api/venta/transac/[controller]")]
    [ApiController]
    public class veproformaController : ControllerBase
    {

        private readonly UserConnectionManager _userConnectionManager;
        private empaquesFunciones empaque_func = new empaquesFunciones();
        private ClienteCasual clienteCasual = new ClienteCasual();

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
                    var nroactual = await _context.venumeracion
                        .Where(item => item.id == id)
                        .Select(item => item.nroactual)
                        .FirstOrDefaultAsync();

                    if (nroactual == null)
                    {
                        return NotFound("No se encontro un registro con este código");
                    }

                    return Ok(nroactual + 1);
                }

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
        [Route("getItemMatriz/{userConn}/{coditem}/{tarifa}/{descuento}/{cantidad}")]
        public async Task<ActionResult<itemDataMatriz>> getItemMatriz(string userConn, string coditem, int tarifa, int descuento, float cantidad)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var precioItem = await _context.intarifa1
                        .Where(i => i.codtarifa == tarifa && i.item == coditem)
                        .Select(i => i.precio)
                        .FirstOrDefaultAsync();


                    var item = await _context.initem
                        .Where(i => i.codigo == coditem)
                        .Select(i => new itemDataMatriz
                        {
                            coditem = i.codigo,
                            descripcion = i.descripcion,
                            medida = i.medida,
                            ud = i.unidad,
                            porcenIV = 0,
                            pedido = cantidad,
                            cantidad = cantidad,
                            porcentSld = 0,
                            tp = tarifa,
                            de = descuento,
                            pul = (float)precioItem,
                            ni = "X",
                            porcen = 0,
                            pd = 0,
                            pu = 0,
                            total = 0

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


        /*

        /// <summary>
        /// Obtiene saldos de un item de una agencia de manera local
        /// </summary>
        /// <param name="userConn"></param>
        /// <param name="codalmacen"></param>
        /// <param name="coditem"></param>
        /// <returns></returns>
        // GET: api/ad_conexion_vpn/5
        [HttpGet]
        [Route("getsladovarfinal/{userConn}/{codalmacen}/{usuario}")]
        public async Task<ActionResult<instoactual>> detallar_saldo_variable_final(string userConn, string usuario)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                bool ver_detalle_saldo_variable = await empaque_func.ve_detalle_saldo_variable(userConnectionString, usuario);
                if (!ver_detalle_saldo_variable)
                {
                    return Ok("No tiene permiso para visualizar esta información.");
                }
                


            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }

        */
    }
}
