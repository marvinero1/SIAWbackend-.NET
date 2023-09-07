using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NuGet.Configuration;
using SIAW.Data;
using SIAW.Models;
using SIAW.Models_Extra;
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
        [Route("GetsaldosPrueba/{userConn}/{agencia}/{codalmacen}/{coditem}/{codempresa}")]
        public async Task<ActionResult<instoactual>> Getsaldos(string userConn, string agencia, int codalmacen, string coditem, string codempresa)
        {
            try
            {
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
                // obtiene reservas en proforma
                List<saldosObj> saldosReservProformas = await getReservasProf(conexion, coditem, codalmacen, obtener_cantidades_aprobadas_de_proformas, eskit); 

                
                string codigoBuscado = instoactual.coditem;

                var reservaProf = saldosReservProformas.FirstOrDefault(obj => obj.coditem == codigoBuscado);
                instoactual.coditem = coditem;


                // obtiene items si no son kit, sus reservas para armar conjuntos.
                float CANTIDAD_RESERVADA = await getReservasCjtos(userConnectionString, coditem, codalmacen, codempresa, eskit, (float)instoactual.cantidad, (float)reservaProf.TotalP);

                // obtiene el saldo minimo que debe mantenerse en agencia

                float Saldo_Minimo_Item = await empaque_func.getSaldoMinimo(userConnectionString, coditem);


                // obtiene reserva NM ingreso para sol-Urgente
                bool validar_ingresos_solurgentes = await empaque_func.getValidaIngreSolurgente(userConnectionString, codempresa);
                
                float total_reservado = 0;  

                if (validar_ingresos_solurgentes)
                {
                    //  RESTAR LAS CANTIDADES DE INGRESO POR NOTAS DE MOVIMIENTO URGENTES
                    // de facturas que aun no estan aprobadas

                    // verifica si es almacen o tienda
                    bool esAlmacen = await empaque_func.esAlmacen(userConnectionString, codalmacen);
                    if (esAlmacen)
                    {

                    }

                }


                // devolver resultados finales
                return Ok(new
                {
                    saldoActual = instoactual,
                    reservaProf = reservaProf,
                    reservaCjtos = CANTIDAD_RESERVADA,
                    sldMinItem = Saldo_Minimo_Item
                });



            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }


        private async Task<float> getReservasCjtos(string userConnectionString, string coditem, int codalmacen, string codempresa, bool eskit, float _saldoActual, float reservaProf)
        {
            List<inctrlstock> itemsinReserva = null;
            float CANTIDAD_RESERVADA = 0;
            
            if (!eskit)  // si no es kit debe verificar si el item es utilizado para armar conjuntos
            {
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
        [Route("actualizarCorreoCliente/{userConn}/{codcliente}/{email}")]
        public async Task<object> actualizarCorreoCliente(string userConn, string codcliente, string email)
        {

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
                        return Ok("Se ha actualizado exitosamente el email del cliente en sus Datos y en datos de la Tienda del Cliente.");

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
    }
}
