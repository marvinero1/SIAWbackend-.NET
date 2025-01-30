using LibSIAVB;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Polly.Retry;
using SIAW.Controllers.ventas.modificacion;
using siaw_DBContext.Data;
using siaw_DBContext.Models;
using siaw_DBContext.Models_Extra;
using siaw_funciones;
using System.Data;
using System.Web.Http.Results;
using static siaw_funciones.Validar_Vta;
using static System.Net.Mime.MediaTypeNames;

namespace SIAW.Controllers.ventas.transaccion
{
    [Route("api/venta/transac/[controller]")]
    [ApiController]
    public class veremisionController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        private readonly siaw_funciones.Ventas ventas = new siaw_funciones.Ventas();
        private readonly siaw_funciones.Cliente cliente = new siaw_funciones.Cliente();
        private readonly Anticipos_Vta_Contado anticipos_vta_contado = new Anticipos_Vta_Contado();
        private readonly siaw_funciones.Nombres nombres = new siaw_funciones.Nombres();
        private readonly siaw_funciones.TipoCambio tipocambio = new siaw_funciones.TipoCambio();
        private readonly siaw_funciones.Configuracion configuracion = new siaw_funciones.Configuracion();
        private readonly siaw_funciones.Saldos saldos = new siaw_funciones.Saldos();
        private readonly siaw_funciones.Creditos creditos = new siaw_funciones.Creditos();
        private readonly siaw_funciones.Empresa empresa = new siaw_funciones.Empresa();
        private readonly siaw_funciones.Almacen almacen = new siaw_funciones.Almacen();
        private readonly siaw_funciones.Funciones funciones = new Funciones();
        private readonly siaw_funciones.Cobranzas cobranzas = new siaw_funciones.Cobranzas();
        private readonly siaw_funciones.datosProforma datos_proforma = new siaw_funciones.datosProforma();
        private readonly siaw_funciones.Items items = new siaw_funciones.Items();
        private readonly siaw_funciones.Validar_Vta validar_Vta = new siaw_funciones.Validar_Vta();
        private readonly siaw_funciones.Seguridad seguridad = new siaw_funciones.Seguridad();
        private readonly siaw_funciones.SIAT siat = new siaw_funciones.SIAT();

        private readonly Documento documento = new Documento();
        private readonly Depositos_Cliente depositos_Cliente = new Depositos_Cliente();
        private readonly Inventario inventario = new Inventario();
        private readonly Restricciones restricciones = new Restricciones();
        private readonly HardCoded hardCoded = new HardCoded();

        private readonly Log log = new Log();
        private readonly string _controllerName = "veremisionController";

        // Definir la política de reintento como una propiedad global
        private readonly AsyncRetryPolicy _retryPolicy;



        public veremisionController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
            // Inicializar el nombre del controlador en el constructor
        }
        [HttpGet]
        [Route("getParametrosIniciales/{userConn}/{usuario}/{codempresa}")]
        public async Task<object> getParametrosIniciales(string userConn, string usuario, string codempresa)
        {
            try
            {
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    // obtener ID
                    string id = await configuracion.usr_idremision(_context,usuario);
                    // obtener nroID
                    int numeroid = new int();
                    if (id == "")
                    {
                        numeroid = 0;
                    }
                    else
                    {
                        numeroid = await documento.ventasnumeroid(_context, id) + 1;
                    }

                    //obtener cod vendedor
                    int codvendedor = await configuracion.usr_codvendedor(_context,usuario);
                    // obtener codmoneda
                    var codmoneda = await configuracion.usr_codmoneda(_context,usuario);
                    // obtener tdc
                    var tdc = await tipocambio._tipocambio(_context, await Empresa.monedabase(_context,codempresa),codmoneda,DateTime.Now.Date);
                    // obtener almacen actual
                    var codalmacen = await configuracion.usr_codalmacen(_context,usuario);
                    // obtener codigo tarifa defecto
                    var codtarifadefect = await configuracion.usr_codtarifa(_context,usuario);
                    // obtener codigo decuento defecto
                    var coddescuentodefect = await configuracion.usr_coddescuento(_context, usuario);

                    return Ok(new
                    {
                        id,
                        numeroid,
                        codvendedor,
                        codmoneda,
                        tdc,
                        codalmacen,
                        codtarifadefect,
                        coddescuentodefect
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return Problem("Error en el servidor : " + ex.Message);
                throw;
            }
        }

        [HttpGet]
        [Route("validaTranferencia/{userConn}/{idProforma}/{nroidProforma}")]
        public async Task<object> validaTranferencia(string userConn, string idProforma, int nroidProforma)
        {
            if (idProforma.Trim().Length == 0)
            {
                return BadRequest(new { resp = "El ID de la proforma no puede estar vacio. " });
            }
            if (nroidProforma == 0)
            {
                return BadRequest(new { resp = "El número ID de la proforma no puede ser 0. " });
            }
            try
            {
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var codProforma = await _context.veproforma.Where(i=> i.id== idProforma && i.numeroid == nroidProforma && i.aprobada == true)
                        .Select(i=> new
                        {
                            i.codigo
                        }).FirstOrDefaultAsync();
                    if (codProforma == null)
                    {
                        return BadRequest(new { resp = "La proforma que intenta buscar no se encuentra aprobada. No se puede Transferir." });
                    }
                    if(await yahayproforma(_context, codProforma.codigo))
                    {
                        return BadRequest(new { resp = "Esta proforma ya fue transferida a otra Nota de Remision. No se puede Transferir." });
                    }
                    return Ok(new
                    {
                        resp = "Transfiriendo Proforma",
                        codProforma = codProforma.codigo
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return Problem("Error en el servidor al validar transferencia de proforma en NR : " + ex.Message);
                throw;
            }
        }

        private async Task<bool> yahayproforma(DBContext _context, int codproforma)
        {
            bool yahayproforma = false;
            // ver si no fue transferida a una nota de remision
            var transferida = await _context.veremision.Where(i=>i.anulada==false && i.codproforma==codproforma)
                .Select(i => new
                {
                    i.codigo
                }).FirstOrDefaultAsync();
            int profTransferida = await _context.veproforma.Where(i => i.codigo == codproforma && i.transferida == true).CountAsync();


            if (transferida != null || profTransferida>0)
            {
                yahayproforma = true;
            }
            return yahayproforma;

        }


        [HttpGet]
        [Route("transferirProforma/{userConn}/{idProforma}/{nroidProforma}/{codproforma}")]
        public async Task<object> transferirProforma(string userConn, string idProforma, int nroidProforma, int codproforma)
        {
            if (idProforma.Trim().Length == 0)
            {
                return BadRequest(new { resp = "El ID de la proforma no puede estar vacio. " });
            }
            if (nroidProforma == 0)
            {
                return BadRequest(new { resp = "El número ID de la proforma no puede ser 0. " });
            }
            if (codproforma == 0)
            {
                return BadRequest(new { resp = "El número código de la proforma no puede ser 0. " });
            }
            try
            {
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    //verificar si la proforma esta vinculada a una solicitud urgente
                    var doc_solurgente = await ventas.Solicitud_Urgente_IdNroid_de_Proforma(_context, idProforma, nroidProforma);
                    string msgAlert = "";
                    string txtid_solurgente = "";
                    int txtnroid_solurgente = 0;
                    if (doc_solurgente.id != "")
                    {
                        msgAlert = "La proforma es una solicitud urgente!!!";
                        txtid_solurgente = doc_solurgente.id;
                        txtnroid_solurgente = doc_solurgente.nroId;
                    }

                    // transferirdoc(trans.codigo_elegido, trans.tipo_documento)
                    var data = await transferirdatosproforma(_context, codproforma, idProforma, nroidProforma);
                    if (data.msg != "")
                    {
                        return BadRequest(new { resp = data.msg });
                    }
                    return Ok(new
                    {
                        msgAlert = msgAlert,
                        txtid_solurgente,
                        txtnroid_solurgente,

                        data.datos
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return Problem("Error en el servidor al transferir Proforma en NR : " + ex.Message);
                throw;
            }
        }







        private async Task<(object? datos, string msg)> transferirdatosproforma(DBContext _context, int codproforma, string idProf, int nroIdProf)
        {
            // obtener cabecera.
            var cabecera = await _context.veproforma
                .Where(i => i.codigo == codproforma)
                .FirstOrDefaultAsync();
            if (cabecera == null)
            {
                return (null,"No se encontraron datos con este código de proforma, consulte con el administrador.");
            }
            // validacion si coinciden los datos que se reciben con los de la base de datos
            if (cabecera.id != idProf.ToUpper() || cabecera.numeroid != nroIdProf)
            {
                return (null, "El id o número id recibidos no corresponden al de la proforma con el código proporcionado, consulte con el administrador.");
            }

            string _codcliente_real = cabecera.codcliente_real;

            
            if (cabecera.tipo_complementopf == 0)
            {
                cabecera.tipo_complementopf = 3;
            }
            if (cabecera.tipo_complementopf == 1 || cabecera.tipo_complementopf == 2)
            {
                cabecera.tipo_complementopf = cabecera.tipo_complementopf - 1;
            }


            // obtener razon social de cliente
            var codclientedescripcion = await cliente.Razonsocial(_context, cabecera.codcliente);


            // _codcliente_real = tabla.Rows(0)("codcliente_real")
            if (await cliente.Es_Cliente_Casual(_context,cabecera.codcliente))
            {
                _codcliente_real = await ventas.Cliente_Referencia_Proforma_Etiqueta(_context, cabecera.id,cabecera.numeroid);
            }

            // obtener tipo cliente
            var tipo_cliente = await cliente.Tipo_Cliente(_context, cabecera.codcliente_real);

            // obtener cliente habilitado
            string lblhabilitado = "";
            if (await cliente.clientehabilitado(_context,cabecera.codcliente_real))
            {
                lblhabilitado = "SI";
            }
            else
            {
                lblhabilitado = "NO";
            }


            // establecer ubicacion
            if (cabecera.ubicacion == null || cabecera.ubicacion == "")
            {
                cabecera.ubicacion = "NSE";
            }

            //////////////////////////////////////////////////////////////////////////////


            // obtener recargos
            var recargos = await _context.verecargoprof.Where(i => i.codproforma == codproforma).ToListAsync();

            // obtener descuentos
            var descuentosExtra = await _context.vedesextraprof
                .Join(_context.vedesextra,
                p => p.coddesextra,
                e => e.codigo,
                (p, e) => new { p, e })
                .Where(i => i.p.codproforma == codproforma)
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

            // obtener iva
            var iva = await _context.veproforma_iva.Where(i => i.codproforma == codproforma).ToListAsync();




            // obtener detalles.
            var codProforma = cabecera.codigo;
            var detalle = await _context.veproforma1
                .Where(i => i.codproforma == codProforma)
                .Join(_context.initem,
                p => p.coditem,
                i => i.codigo,
                (p, i) => new { p, i })
                .Select(i => new itemDataMatriz   // ajustando el detalle al modelo de matriz que se utiliza
                {
                    coditem = i.p.coditem,
                    descripcion = i.i.descripcion,
                    medida = i.i.medida,
                    udm = i.p.udm,
                    porceniva = (double)(i.p.porceniva ?? 0),
                    niveldesc = i.p.niveldesc,
                    cantidad = (double)i.p.cantidad,
                    codtarifa = i.p.codtarifa,
                    coddescuento = i.p.coddescuento,
                    precioneto = (double)i.p.precioneto,
                    preciodesc = (double)(i.p.preciodesc ?? 0),
                    preciolista = (double)i.p.preciolista,
                    total = (double)i.p.total,
                })
                .OrderBy(i => i.coditem)
                .ToListAsync();

            // Obtener anticipos si los hay
            var anticiposProf = await _context.veproforma_anticipo
                    .Where(va => va.codproforma == cabecera.codigo)
                    .Join(
                        _context.coanticipo,
                        va => va.codanticipo,    // Llave foránea en veproforma_anticipo
                        co => co.codigo,         // Llave primaria en coanticipo
                        (va, co) => new vedetalleanticipoProforma
                        {
                            codproforma = cabecera.codigo,
                            codanticipo = va.codigo,
                            docanticipo = co.id + "-" + co.numeroid,
                            id_anticipo = co.id,
                            nroid_anticipo = co.numeroid,
                            monto = (double)(va.monto ?? 0),
                            tdc = (double)(va.tdc ?? 0),
                            codmoneda = cabecera.codmoneda,
                            fechareg = (DateTime)va.fechareg,
                            usuarioreg = va.usuarioreg,
                            horareg = va.horareg,
                            codvendedor = cabecera.codvendedor.ToString()

                        }
                    )
                    .ToListAsync();

            return (new
            {
                codcliente_real = _codcliente_real,
                codclientedescripcion,
                tipo_cliente,
                lblhabilitado,

                cabecera = cabecera,
                detalle = detalle,
                descuentos = descuentosExtra,
                recargos = recargos,
                iva = iva,
                detalleAnticipos = anticiposProf
            },"");
        }





        //[Authorize]
        [HttpPost]
        [QueueFilter(1)] // Limitar a 1 solicitud concurrente
        [Route("grabarNotaRemision/{userConn}/{id}/{usuario}/{desclinea_segun_solicitud}/{codProforma}/{id_pf}/{nroid_pf}/{codempresa}/{id_solurg}/{nroid_solurg}/{sin_validar}/{sin_validar_empaques}/{sin_validar_negativos}/{sin_validar_monto_min_desc}/{sin_validar_monto_total}/{sin_validar_doc_ant_inv}")]
        public async Task<object> grabarNotaRemision(string userConn, string id, string usuario, bool desclinea_segun_solicitud, int codProforma, string id_pf, int nroid_pf, string codempresa, string id_solurg, int nroid_solurg, bool sin_validar, bool sin_validar_empaques, bool sin_validar_negativos, bool sin_validar_monto_min_desc, bool sin_validar_monto_total, bool sin_validar_doc_ant_inv, SaveNRemisionCompleta datosRemision)
        {
            bool resultado = false;
            // borrar los items con cantidad cero


            if (datosRemision.veremision == null) { return BadRequest(new { resp = "Se esta enviando como Nulo la cabecera de NR, consulte con el administrador." }); }
            if (datosRemision.veremision1 == null) { return BadRequest(new { resp = "Se esta enviando como Nulo el detalle de NR, consulte con el administrador." }); }
            if (datosRemision.vedesextraremi == null) { return BadRequest(new { resp = "Se esta enviando como Nulo el detalle de descuentos de NR, consulte con el administrador." }); }
            if (datosRemision.verecargoremi == null) { return BadRequest(new { resp = "Se esta enviando como Nulo el detalle de recargos de NR, consulte con el administrador." }); }
            if (datosRemision.veremision_iva == null) { return BadRequest(new { resp = "Se esta enviando como Nulo el detalle de IVA de NR, consulte con el administrador." }); }

            datosRemision.veremision1.RemoveAll(i => i.cantidad <= 0);
            if (datosRemision.veremision1.Count() <= 0)
            {
                return BadRequest(new { resp = "No hay ningun item en su documento." });
            }
            veremision veremision = datosRemision.veremision;
            // 
            // VALIDACIONES PARA EVITAR NULOS
            if (veremision.id == null) { return BadRequest(new { resp = "No se esta recibiendo el ID del documento, Consulte con el Administrador del sistema." }); }
            if (veremision.numeroid == null) { return BadRequest(new { resp = "No se esta recibiendo el número de ID del documento, Consulte con el Administrador del sistema." }); }
            if (veremision.codalmacen == null) { return BadRequest(new { resp = "No se esta recibiendo el codigo de Almacen, Consulte con el Administrador del sistema." }); }
            if (veremision.codvendedor == null) { return BadRequest(new { resp = "No se esta recibiendo el código de vendedor, Consulte con el Administrador del sistema." }); }
            if (veremision.preparacion == null) { return BadRequest(new { resp = "No se esta recibiendo el tipo de preparación, Consulte con el Administrador del sistema." }); }
            if (veremision.tipopago == null) { return BadRequest(new { resp = "No se esta recibiendo el tipo de pago, Consulte con el Administrador del sistema." }); }
            if (veremision.contra_entrega == null) { return BadRequest(new { resp = "No se esta recibiendo si la venta es contra entrega o no, Consulte con el Administrador del sistema." }); }
            if (veremision.estado_contra_entrega == null) { return BadRequest(new { resp = "No se esta recibiendo el estado contra entrega, Consulte con el Administrador del sistema." }); }
            if (veremision.codcliente == null) { return BadRequest(new { resp = "No se esta recibiendo el codigo de cliente, Consulte con el Administrador del sistema." }); }
            if (veremision.nomcliente == null) { return BadRequest(new { resp = "No se esta recibiendo el nombre del cliente, Consulte con el Administrador del sistema." }); }
            if (veremision.tipo_docid == null) { return BadRequest(new { resp = "No se esta recibiendo el tipo de documento, Consulte con el Administrador del sistema." }); }
            if (veremision.nit == null) { return BadRequest(new { resp = "No se esta recibiendo el NIT/CI del cliente, Consulte con el Administrador del sistema." }); }
            if (veremision.email == null) { return BadRequest(new { resp = "No se esta recibiendo el Email del cliente, Consulte con el Administrador del sistema." }); }
            if (veremision.codmoneda == null) { return BadRequest(new { resp = "No se esta recibiendo el codigo de moneda, Consulte con el Administrador del sistema." }); }
            if (veremision.obs == null || veremision.obs.Trim() == "") { datosRemision.veremision.obs = "---"; }



            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);
            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                try
                {
                    /*
                        recalcularprecios()
                        Me.versubtotal()
                        Me.verdesextra()
                        Me.verrecargos()
                        Me.vertotal()
                        */
                    /////**************************VALIDAR DATOS PARA GRABAR UNA NOTA DE REMISION
                    
                    var validacion = await Validar_Grabar_Nota_Remision(_context, sin_validar, sin_validar_empaques, sin_validar_negativos, sin_validar_monto_min_desc, sin_validar_monto_total, sin_validar_doc_ant_inv, id_pf, nroid_pf, codProforma, codempresa, usuario, datosRemision);
                    if (validacion.resp == false)
                    {
                        //return BadRequest(new { resp = validacion.resp, validacion.msgAlert, validacion.dtnegativos_result, validacion.codigo_control });
                        //return BadRequest(new { resp = validacion.resp, validacion.msgsAlert, validacion.dtnegativos_result, validacion.codigo_control });
                        return StatusCode(203, new { resp = validacion.resp, validacion.msgsAlert, validacion.dtnegativos_result, validacion.codigo_control });
                    }
                    /////**************************
                }
                catch (Exception ex)
                {
                    return Problem($"Error en el servidor al Validar NR: {ex.Message}");
                }

                int codNRemision = 0;
                int numeroId = 0;
                bool mostrarModificarPlanCuotas = false;
                List<planPago_object>? plandeCuotas = new List<planPago_object>();
                using (var dbContexTransaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        // El front debe llamar a las funciones de totalizar antes de mandar a grabar
                        var doc_grabado = await Grabar_Documento(_context, id, usuario, desclinea_segun_solicitud, codProforma, codempresa, datosRemision);
                        // si doc_grabado.mostrarModificarPlanCuotas  == true    Marvin debe desplegar ventana para modificar plan de cuotas, es ventana nueva aparte
                        if (doc_grabado.resp != "ok")
                        {
                            await dbContexTransaction.RollbackAsync();
                            return BadRequest(new { resp = doc_grabado.resp, msgAlert = "No se pudo Grabar la Nota de Remision con Exito." });
                        }
                        // SI ESTA TODO OK QUE LO GUARDE EN VARIABLES LO QUE RECIBE PARA USAR MAS ADELANTE
                        codNRemision = doc_grabado.codNRemision;
                        numeroId = doc_grabado.numeroId;
                        mostrarModificarPlanCuotas = doc_grabado.mostrarModificarPlanCuotas;
                        await dbContexTransaction.CommitAsync();
                    }
                    catch (Exception ex)
                    {
                        await dbContexTransaction.RollbackAsync();
                        return Problem($"Error en el servidor al grabar NR: {ex.Message}");
                        throw;
                    }
                }


                try
                {
                    // verificacion para ver si el documento descarga mercaderia
                    bool descarga = await ventas.iddescarga(_context, id);
                    // actualizar stock actual si es que descarga mercaderia
                    if (descarga)
                    {
                        bool actualizaNR = new bool();
                        // Desde 15/11/2023 registrar en el log si por alguna razon no actualiza en instoactual correctamente al disminuir el saldo de cantidad y la reserva en proforma
                        try
                        {
                            actualizaNR = await saldos.Veremision_ActualizarSaldo(_context, codNRemision, Saldos.ModoActualizacion.Crear);
                        }
                        catch (Exception ex)
                        {
                            // return ("Error al Actualizar stock de NR desde Nota de Remision, por favor consulte con el administrador del sistema: " + ex.Message, 0, 0, false, null);
                            Console.WriteLine(ex.ToString() );
                        }
                        if (actualizaNR == false)
                        {
                            await log.RegistrarEvento(_context, usuario, Log.Entidades.SW_Nota_Remision, codNRemision.ToString(), veremision.id, numeroId.ToString(), this._controllerName, "No actualizo stock al restar cantidad en NR.", Log.TipoLog.Creacion);
                        }
                        else
                        {
                            bool resultActPF = new bool();
                            string msgActPF = "";
                            try
                            {
                                var actualizaProfSaldo = await ventas.revertirstocksproforma(_context, codProforma, codempresa);
                                resultActPF = actualizaProfSaldo.resultado;
                                msgActPF = actualizaProfSaldo.msg;
                            }
                            catch (Exception ex)
                            {
                                // return ("Error al Actualizar stock de PF desde Nota de Remision, por favor consulte con el administrador del sistema: " + ex.Message, 0, 0, false, null);
                                Console.WriteLine(ex.ToString());
                            }
                            if (resultActPF == false)
                            {
                                await log.RegistrarEvento(_context, usuario, Log.Entidades.SW_Nota_Remision, codNRemision.ToString(), veremision.id, numeroId.ToString(), this._controllerName, msgActPF, Log.TipoLog.Creacion);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // return ("Error al Actualizar stock de NR desde Nota de Remision, por favor consulte con el administrador del sistema: " + ex.Message, 0, 0, false, null);
                    Console.WriteLine(ex.ToString());
                }
                try
                {
                    await log.RegistrarEvento(_context, usuario, Log.Entidades.SW_Nota_Remision, codNRemision.ToString(), datosRemision.veremision.id, numeroId.ToString(), this._controllerName, "Grabar", Log.TipoLog.Creacion);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error al guardar Log de veremision" + ex.Message);
                }
                try
                {
                    resultado = true;
                    // devolver
                    string msgAlertGrabado = "Se grabo la Nota de Remision " + datosRemision.veremision.id + "-" + numeroId + " con Exito.";

                    //Actualizar Credito
                    //sia_funciones.Creditos.Instancia.Actualizar_Credito_2020(codcliente.Text, sia_compartidos.temporales.Instancia.usuario, sia_compartidos.temporales.Instancia.codempresa, True, Me.Usar_Bd_Opcional)

                    // Actualizar Credito
                    await creditos.Actualizar_Credito_2023(_context, datosRemision.veremision.codcliente, usuario, codempresa, true);

                    // enlazar a la solicitud urgente
                    string msgSolUrg = "";
                    if (id_solurg.Trim() == "0")
                    {
                        id_solurg = "";
                    }
                    if (id_solurg.Trim() != "" && nroid_solurg > 0)  // si no es sol urgente, Marvin debe mandar id en vacio "" y nroid en 0
                    {
                        try
                        {
                            var insolUrg = await _context.insolurgente.Where(i => i.id == id_solurg && i.numeroid == nroid_solurg).FirstOrDefaultAsync();
                            insolUrg.fid = datosRemision.veremision.id;
                            insolUrg.fnumeroid = numeroId;
                            await _context.SaveChangesAsync();
                            msgSolUrg = "La nota de remision fue enlazada con la solicitud urgente: " + id_solurg + "-" + nroid_solurg;
                        }
                        catch (Exception)
                        {
                            msgSolUrg = "La nota de remision no pudo ser enlazada a la solicitud Urgente, contacte con el administrador de sistemas.";
                            //throw;
                        }
                    }

                    return Ok(new
                    {
                        resp = msgAlertGrabado,
                        codNotRemision = codNRemision,
                        nroIdRemision = numeroId,
                        mostrarVentanaModifPlanCuotas = mostrarModificarPlanCuotas,
                        planCuotas = plandeCuotas,
                        msgSolUrg = msgSolUrg
                    });
                }
                catch (Exception)
                {
                    return Ok(new
                    {
                        resp = "Se grabo la Nota de Remision pero hay un problema en la actualizacion de creditos o enlace con solicitud urgente, consulte con el Administrador del sistema.",
                        codNotRemision = codNRemision,
                        nroIdRemision = numeroId,
                        mostrarVentanaModifPlanCuotas = mostrarModificarPlanCuotas,
                        planCuotas = plandeCuotas,
                        msgSolUrg = ""
                    });
                }
            }

        }



        private async Task<(bool resp, List<string> msgsAlert, List<Dtnegativos> dtnegativos_result, int codigo_control)> Validar_Grabar_Nota_Remision(DBContext _context, bool sin_validar, bool sin_validar_empaques, bool sin_validar_negativos, bool sin_validar_monto_min_desc, bool sin_validar_monto_total, bool sin_validar_doc_ant_inv, string id_pf, int nroid_pf, int cod_proforma, string codempresa, string usuario, SaveNRemisionCompleta datosRemision)
        {
            bool resultado = true;
            List<string> msgsAlert = new List<string>();
            //var dt_pf = await _context.veproforma.Where(i => i.codigo == cod_proforma).FirstOrDefaultAsync();
            string codcliente_real = "";
            string codmoneda_pf = "";
            //
            veremision veremision = datosRemision.veremision;
            List<veremision1> veremision1 = datosRemision.veremision1;
            var vedesextraremi = datosRemision.vedesextraremi;
            var verecargoremi = datosRemision.verecargoremi;
            var veremision_iva = datosRemision.veremision_iva;
            var veremision_chequerechazado = datosRemision.veremision_chequerechazado;


            //CONVERTIR
            //aqui convertir veremision a DatosDocVta
            DatosDocVta DVTA = await ConvertirVeremisionADatosDocVta(veremision);
            //aqui convertir List<vedesextraremi> a List<vedesextraDatos>
            List<vedesextraDatos> tabladescuentos = await ConvertirVedesextraRemiADatos(vedesextraremi);
            //aqui convertir List<verecargoremi> a List<verecargosDatos>
            List<verecargosDatos> tablarecargos = await ConvertirListaVerecargoremiAListaVerecargosDatos(verecargoremi);
            //aqui convertir List<veremision1> a List<itemDataMatriz>
            List<itemDataMatriz> tabladetalle = await ConvertirListaVeremision1AListaItemDataMatriz(veremision1);

            codcliente_real = await ventas.Cliente_Referencia_Proforma_Etiqueta(_context, id_pf, nroid_pf);
            if (codcliente_real == "")
            {
                codcliente_real = DVTA.codcliente;
            }
            codmoneda_pf = await ventas.MonedaPF(_context, cod_proforma);

            if (id_pf != null && nroid_pf != 0)
            {
                //valida los descuentos por depositos de credito
                var respdepCoCred = await depositos_Cliente.Validar_Desctos_x_Deposito_Otorgados_De_Cobranzas_Credito(_context, id_pf, nroid_pf, codempresa);
                if (!respdepCoCred.result)
                {
                    resultado = false;
                    msgsAlert.Add("No se puede emitir la nota de remision de esta proforma, debido a que tiene descuentos por depositos de cobranzas credito en montos no validos!!!");
                    //if (respdepCoCred.msgAlert != "")
                    //{
                    //    msgsAlert.Add(respdepCoCred.msgAlert);
                    //}
                }
                //valida los descuentos por depositos de contado
                var respdepCoCont = await depositos_Cliente.Validar_Desctos_x_Deposito_Otorgados_De_Cbzas_Contado_CE(_context, id_pf, nroid_pf, codempresa);
                if (!respdepCoCont.result)
                {
                    resultado = false;
                    msgsAlert.Add("No se puede emitir la nota de remision de esta proforma, debido a que tiene descuentos por deposito de cobranzas contado en montos no validos!!!");
                    //if (respdepCoCont.msgAlert != "")
                    //{
                    //    msgsAlert.Add(respdepCoCont.msgAlert);
                    //}
                }
                //valida los descuentos por depositos de contado
                var respdepAntProfCont = await depositos_Cliente.Validar_Desctos_x_Deposito_Otorgados_De_Anticipos_Que_Pagaron_Proformas_Contado(_context, id_pf, nroid_pf, codempresa);
                if (!respdepAntProfCont.result)
                {
                    resultado = false;
                    msgsAlert.Add("No se puede emitir la nota de remision de esta proforma, debido a que tiene descuentos por deposito de anticipo contado en montos no validos!!!");
                    //if (respdepAntProfCont.msgAlert != "")
                    //{
                    //    msgsAlert.Add(respdepAntProfCont.msgAlert);
                    //}
                }

                if (resultado == false)
                {
                    msgsAlert.Add("No se puede Grabar la Nota de Remision, porque tiene descuentos por deposito en montos no validos!!!");
                    return (false, msgsAlert, dtnegativos, 0);
                }
                //======================================================================================
                /////////////////VALIDAR DESCTOS POR DEPOSITO APLICADOS
                //======================================================================================

                var validDescDepApli = await Validar_Descuentos_Por_Deposito_Excedente(_context, codempresa, tabladescuentos, DVTA);
                if (!validDescDepApli.result)
                {
                    resultado = false;
                    msgsAlert.Add(validDescDepApli.msgAlert);
                    msgsAlert.Add("No se puede emitir la nota de remision de la proforma: " + id_pf + "-" + nroid_pf + " debido a que tiene descuentos por deposito en montos no validos!!!");
                    return (false, msgsAlert, dtnegativos, 0);
                }
                //======================================================================================
                ///////////////VALIDAR RECARGOS POR DEPOSITO APLICADOS
                //======================================================================================
                var validRecargoDepExcedente = await Validar_Recargos_Por_Deposito_Excedente(_context, codempresa, tablarecargos, DVTA);
                if (!validRecargoDepExcedente.result)
                {
                    resultado = false;
                    msgsAlert.Add(validDescDepApli.msgAlert);
                    msgsAlert.Add("No se puede emitir la nota de remision de la proforma: " + id_pf + "-" + nroid_pf + " debido a que tiene recargos por descuentos por deposito excedentes en montos no validos!!!");
                    return (false, msgsAlert, dtnegativos, 0);
                }
            }
            ////////////////////////////////////////////////////////////////////////////////
            /////VALIDAR DETALLE DE ITEMS
            ///////////////////////////////////////////////////////////////////////////////////
            var validacion_detalle = await Validar_Detalle(_context, sin_validar, sin_validar_empaques, sin_validar_negativos, codempresa, usuario, codcliente_real, DVTA, tabladetalle, tabladescuentos, id_pf, nroid_pf);
            if (!validacion_detalle.bandera)
            {
                resultado = false;
                msgsAlert.Add("No se puede emitir la nota de remision de la proforma: " + id_pf + "-" + nroid_pf + " debido a que no cumple con los datos del detalle!!!");
                msgsAlert.Add(validacion_detalle.msg);
                return (false, msgsAlert, dtnegativos, validacion_detalle.codigo_control);
            }

            ////////////////////////////////////////////////////////////////////////////////
            /////VALIDAR CABECERA
            ///////////////////////////////////////////////////////////////////////////////////
            var validacion_cabecera = await Validar_Datos_Cabecera_Remision(_context, sin_validar, sin_validar_monto_min_desc, sin_validar_monto_total, sin_validar_doc_ant_inv, codempresa, usuario, cod_proforma, id_pf, nroid_pf, codmoneda_pf, DVTA.codcliente_real, DVTA, tabladetalle, tabladescuentos, veremision.email, veremision.tdc);
            if (!validacion_cabecera.bandera)
            {
                resultado = false;
                msgsAlert.Add("No se puede emitir la nota de remision de la proforma: " + id_pf + "-" + nroid_pf + " debido a que no cumple con los datos de la cabecera!!!");
                msgsAlert.Add(validacion_cabecera.msg);
                return (false, msgsAlert, dtnegativos, validacion_cabecera.codigo_control);
            }
            //////////////////////////////////////////////
            
            // validar que el subtotal sea igual a la suma de todos los totales de los items, ademas de utilizar el redondeo de 2 decimales.

            var subtotalSuma = veremision1.Sum(i => i.total);
            subtotalSuma = await siat.Redondeo_Decimales_SIA_2_decimales_SQL(_context, (double)subtotalSuma);
            if (subtotalSuma != veremision.subtotal)
            {
                msgsAlert.Add("La suma del total de items, no coinciden con el subtotal de la nota de remision, consulte con el administrador.");
                return (false, msgsAlert, dtnegativos, 0);
            }

        // mostrar mensaje de credito disponible
        /*

        If IsDBNull(reg("contra_entrega")) Then
            qry = "update veproforma set contra_entrega=0 where id='" & id_pf & "' and numeroid='" & nroid_pf & "'"
            sia_DAL.Datos.Instancia.EjecutarComando(qry)
        End If

         */

        // tipo pago CONTADO
        //if (DVTA.tipo_vta == "CONTADO")
        //{
        //    ////////////////////////////////////////////////////////////////////////////////////////////
        //    // se añadio en fecha 15-3-2016
        //    // es venta al contado y no necesita validar el credito
        //    // TODA VENTA AL CONTADO DEBE TENER ASIGNADO ID-NROID DE ANTICIPO SI ES PAGO ANTICIPADO
        //    // SI SE HABILITO LA OPCION DE PAGO ANTICIPADO
        //    ////////////////////////////////////////////////////////////////////////////////////////////
        //    var dt_anticipos = await anticipos_Vta_Contado.Anticipos_Aplicados_a_Proforma(_context, id_pf, nroid_pf);
        //    if (dt_anticipos.Count > 0)
        //    {
        //        ResultadoValidacion objres = new ResultadoValidacion();
        //        objres = await anticipos_Vta_Contado.Validar_Anticipo_Asignado_2(_context, true, DVTA, dt_anticipos, codempresa);
        //        if (objres.resultado)
        //        {
        //            // Desde 15/01/2024 se cambio esta funcion porque no estaba validando correctamente la transformacion de moneda de los anticipos a aplicarse ya se en $us o BS
        //            // If sia_funciones.Anticipos_Vta_Contado.Instancia.Validar_Anticipo_Asignado(True, dt_anticipos, reg("codcliente"), reg("nomcliente"), reg("total")) = True Then
        //            goto finalizar_ok;
        //        }
        //        else
        //        {
        //            if (dt_anticipos != null)
        //            {
        //                return (false, msgsAlert, dtnegativos, 0);
        //            }
        //        }
        //    }
        //    goto finalizar_ok;

        //}

        finalizar_ok:
            return (true, msgsAlert, dtnegativos, 0);

        }


        public List<Dtnegativos> dtnegativos = new List<Dtnegativos>();


        private async Task<(bool bandera, string msg, int codigo_control, List<Dtnegativos> dtnegativos_result)> Validar_Detalle(DBContext _context, bool sin_validar, bool sin_validar_empaques, bool sin_validar_negativos, string codempresa, string usuario, string codcliente_real, DatosDocVta DVTA, List<itemDataMatriz> tabladetalle, List<vedesextraDatos> tabladescuentos, string id_pf, int numeroid_pf)
        {
            bool resultado = true;
            int i = 0;
            ResultadoValidacion objres = new ResultadoValidacion();

            foreach (var row in tabladetalle)
            {

                if (Convert.IsDBNull(row.coddescuento))
                {
                    row.coddescuento = 0;
                }
                if (string.IsNullOrEmpty(Convert.ToString(row.coddescuento)))
                {
                    row.coddescuento = 0;
                }
                if (Convert.IsDBNull(row.coditem))
                {
                    resultado = false;
                    return (false, "No eligio El Item en la Linea " + (i + 1) + " .", 0, dtnegativos);
                }
                else if (Convert.ToString(row.coditem).Length < 1)
                {
                    resultado = false;
                    return (false, "No eligio El Item en la Linea " + (i + 1) + " .", 0, dtnegativos);
                }
                else if (!await items.itemventa_context(_context, Convert.ToString(row.coditem)))
                {
                    resultado = false;
                    return (false, "El Item en la Linea " + (i + 1) + " " + Convert.ToString(row.coditem) + " No esta a la venta.", 0, dtnegativos);
                }
                else if (Convert.IsDBNull(row.udm))
                {
                    resultado = false;
                    return (false, "No puso la Unidad de Medida en la Linea " + (i + 1) + " " + Convert.ToString(row.coditem) + " .", 0, dtnegativos);
                }
                else if (Convert.ToString(row.udm).Length < 1)
                {
                    resultado = false;
                    return (false, "No puso la Unidad de Medida en la Linea " + (i + 1) + " " + Convert.ToString(row.coditem) + ".", 0, dtnegativos);
                }
                else if (Convert.IsDBNull(row.cantidad))
                {
                    resultado = false;
                    return (false, "No puso la cantidad en la Linea " + (i + 1) + " " + Convert.ToString(row.coditem) + " .", 0, dtnegativos);
                }
                else if (Convert.ToString(row.cantidad).Length < 1)
                {
                    resultado = false;
                    return (false, "No puso la cantidad en la Linea " + (i + 1) + " " + Convert.ToString(row.coditem) + " .", 0, dtnegativos);
                }
                else if (Convert.ToDouble(row.cantidad) <= 0)
                {
                    resultado = false;
                    return (false, "No puso la cantidad en la Linea " + (i + 1) + " " + Convert.ToString(row.coditem) + " .", 0, dtnegativos);
                }
                else if (Convert.IsDBNull(row.preciodesc))
                {
                    resultado = false;
                    return (false, "No puso el Precio Desc en la Linea " + (i + 1) + " Item : " + Convert.ToString(row.coditem) + " .", 0, dtnegativos);
                }
                else if (Convert.ToString(row.preciodesc).Length < 1)
                {
                    resultado = false;
                    return (false, "No puso el Precio Desc en la Linea " + (i + 1) + " Item : " + Convert.ToString(row.coditem) + " .", 0, dtnegativos);
                }
                else if (Convert.ToDouble(row.preciodesc) <= 0)
                {
                    resultado = false;
                    return (false, "No puso el Precio Desc en la Linea " + (i + 1) + " Item : " + Convert.ToString(row.coditem) + " .", 0, dtnegativos);
                }
                //Desde 06/12/2024
                else if (Convert.IsDBNull(row.preciolista))
                {
                    resultado = false;
                    return (false, "No puso el Precio en la Linea " + (i + 1) + " Item : " + Convert.ToString(row.coditem) + " .", 0, dtnegativos);
                }
                else if (Convert.ToString(row.preciolista).Length < 1)
                {
                    resultado = false;
                    return (false, "No puso el Precio en la Linea " + (i + 1) + " Item : " + Convert.ToString(row.coditem) + " .", 0, dtnegativos);
                }
                else if (Convert.ToDouble(row.preciolista) <= 0)
                {
                    resultado = false;
                    return (false, "No puso el Precio en la Linea " + (i + 1) + " Item : " + Convert.ToString(row.coditem) + " .", 0, dtnegativos);
                }
                //
                else if (Convert.IsDBNull(row.precioneto))
                {
                    resultado = false;
                    return (false, "No puso el Precio Neto en la Linea " + (i + 1) + " Item : " + Convert.ToString(row.coditem) + " .", 0, dtnegativos);
                }
                else if (Convert.ToString(row.precioneto).Length < 1)
                {
                    resultado = false;
                    return (false, "No puso el Precio Neto en la Linea " + (i + 1) + " Item : " + Convert.ToString(row.coditem) + " .", 0, dtnegativos);
                }
                else if (Convert.ToDouble(row.precioneto) <= 0)
                {
                    resultado = false;
                    return (false, "No puso el Precio Neto en la Linea " + (i + 1) + " Item : " + Convert.ToString(row.coditem) + " .", 0, dtnegativos);
                }
                //
                else if (Convert.IsDBNull(row.total))
                {
                    resultado = false;
                    return (false, "No puso el Total en la Linea " + (i + 1) + " Item : " + Convert.ToString(row.coditem) + " .", 0, dtnegativos);
                }
                else if (Convert.ToString(row.total).Length < 1)
                {
                    resultado = false;
                    return (false, "No puso el Total en la Linea " + (i + 1) + " Item : " + Convert.ToString(row.coditem) + " .", 0, dtnegativos);
                }
                else if (Convert.ToDouble(row.total) <= 0)
                {
                    resultado = false;
                    return (false, "No puso el Total en la Linea " + (i + 1) + " Item : " + Convert.ToString(row.coditem) + " .", 0, dtnegativos);
                }
                else if(Convert.ToInt32(row.codtarifa) < 0)
                {
                    resultado = false;
                    return (false, "Codigo de tarifa negativo en la Linea " + (i + 1) + " Item : " + Convert.ToString(row.coditem) + " .", 0, dtnegativos);
                }
                else if (Convert.ToInt32(row.coddescuento) < 0)
                {
                    resultado = false;
                    return (false, "Codigo de descuento negativo en la Linea " + (i + 1) + " Item : " + Convert.ToString(row.coditem) + " .", 0, dtnegativos);
                }
                //

                else if (!await ventas.ValidarTarifa(_context, codcliente_real, Convert.ToString(row.coditem), Convert.ToInt32(row.codtarifa)))
                {
                    resultado = false;
                    return (false, "El Item en la Linea " + (i + 1) + " no se puede vender a ese precio para este cliente.", 0, dtnegativos);
                }
                else if (!await ventas.ValidarTarifa_Descuento(_context, row.coddescuento, Convert.ToInt32(row.codtarifa)))
                {
                    resultado = false;
                    return (false, "En la Linea " + (i + 1) + " no se puede vender a ese tipo de precio con ese descuento especial.", 0, dtnegativos);
                }
                i = i + 1;
            }

            await Llenar_Datos_Del_Documento(_context, codempresa, DVTA, tabladetalle);

            // ###verificar que la unidad de medida sea entero o decimal
            if (resultado)
            {
                foreach (var row in tabladetalle)
                {
                    if (await ventas.UnidadSoloEnteros(_context, Convert.ToString(row.udm)))
                    {
                        // verificar que la cantidad sea entero
                        if (Convert.ToDouble(row.cantidad) != Math.Floor(Convert.ToDouble(row.cantidad)))
                        {
                            resultado = false;
                            return (false, "La cantidad en la Linea " + (i + 1) + " " + Convert.ToString(row.coditem) + " no puede tener decimales .", 0, dtnegativos);
                        }
                    }
                }
            }
            if (!sin_validar)
            {
                if (!sin_validar_empaques)
                {
                    if (resultado)
                    {
                        if (await cliente.Controla_empaque_cerrado(_context, codcliente_real))
                        {
                            // Validar empaques Cerrados
                            foreach (var row in tabladetalle)
                            {
                                if (await ventas.Tarifa_EmpaqueCerrado(_context, Convert.ToInt32(row.codtarifa)))
                                {
                                    if (!await ventas.CumpleEmpaqueCerrado(_context, Convert.ToString(row.coditem), Convert.ToInt32(row.codtarifa), Convert.ToInt32(row.coddescuento), Convert.ToDecimal(row.cantidad), DVTA.codcliente))
                                    {
                                        resultado = false;
                                        return (false, "El item " + Convert.ToString(row.coditem) + " no cumple con empaques cerrados.", 25, dtnegativos);
                                    }
                                }
                            }
                            //if (!resultado)
                            //{
                            //    // Permiso especial pedir contrasenia
                            //    var control = new sia_compartidos.prgcontrasena("25", id.Text + "-" + numeroid.Text + " " + codcliente.Text + "-" + nomcliente.Text, id.Text, numeroid.Text);
                            //    control.ShowDialog();
                            //    if (control.control == "si")
                            //    {
                            //        // pedir la razon
                            //        var prgpedirnumero = new sia_compartidos.prgpedircadena(100);
                            //        prgpedirnumero.ShowDialog();
                            //        if (prgpedirnumero.eligio)
                            //        {
                            //            sia_funciones.Seguridad.Instancia.grabar_log_permisos("VENTA SIN EMPAQUES CERRADOS", prgpedirnumero.valor_str, id.Text + "-" + numeroid.Text + ": " + codcliente.Text + "-" + nomcliente.Text + " Total:" + subtotal.Text, sia_compartidos.temporales.Instancia.usuario);
                            //            resultado = true;
                            //        }
                            //        prgpedirnumero.Dispose();
                            //    }
                            //    control.Dispose();
                            //}
                        }
                    }
                }
            }


            // SOLO VALIDAR NEGATIVOS
            // if (!sin_validar)
            if (!sin_validar_negativos)
            {
                if (resultado)
                {
                    if (!await inventario.PermitirNegativos(_context, codempresa))
                    {
                        var msgs = new List<string>();
                        var negs = new List<string>();

                        var validar_negativos = await Validar_Saldos_Negativos_Doc(_context, codempresa, usuario, codcliente_real, DVTA, tabladetalle, id_pf, numeroid_pf);

                        if (!validar_negativos.bandera)
                        {
                            resultado = false;
                            return (false, "Este documento generara saldos negativos!!!", 3, dtnegativos);

                            //var control = new sia_compartidos.prgcontrasena("3", id.Text + "-" + numeroid.Text + " NEGATIVOS " + codcliente.Text + "-" + nomcliente.Text, id.Text, numeroid.Text);
                            //control.ShowDialog();
                            //if (control.control == "si")
                            //{
                            //    sia_funciones.Restricciones.Instancia.GrabarNegativos("NR", id.Text, numeroid.Text, fecha.Value.Date, codcliente.Text, codalmacen.Text, ListaNegativos);
                            //    resultado = true;
                            //}
                            //control.Dispose();
                        }
                    }
                    // ///preguntar por contraseña de autorizacion
                    //if (!resultado)
                    //{
                    //    var control = new sia_compartidos.prgcontrasena("3", id.Text + "-" + numeroid.Text + "  NEGATIVOS " + codcliente.Text + "-" + nomcliente.Text, id.Text, numeroid.Text);
                    //    control.ShowDialog();
                    //    if (control.control == "si")
                    //    {
                    //        sia_funciones.Restricciones.Instancia.GrabarNegativos("NR", id.Text, numeroid.Text, fecha.Value.Date, codcliente.Text, codalmacen.Text, ListaNegativos);
                    //        resultado = true;
                    //    }
                    //    control.Dispose();
                    //}
                }
            }


            // //validar no mezclar desctos especiales
            //sia_funciones.Validar_Vta.ResultadoValidacion objres;
            //if (resultado)
            //{
            //    validar_Vta.InicializarResultado(objres);
            //    objres = await validar_Vta.Validar_No_Mezclar_Descuentos_Especiales(_context, tabladetalle, codempresa);

            //    if (!objres.resultado)
            //    {
            //        if (objres.accion == Validar_Vta.Acciones_Validar.Confirmar_SiNo)
            //        {
            //            if (MessageBox.Show(objres.observacion + "\n" + objres.obsdetalle + "¿Desea Continuar?", "Validar", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1) == DialogResult.No)
            //            {
            //                resultado = false;
            //            }
            //            else
            //            {
            //                resultado = true;
            //            }
            //        }
            //        else
            //        {
            //            resultado = false;
            //        }
            //    }
            //}

            // //validar nro minimo de venta de items G2 pag-41
            if (resultado)
            {
                validar_Vta.InicializarResultado(objres);
                objres = await validar_Vta.Validar_Venta_Minima_Nro_Items_Grado2_KG(_context, tabladetalle, codempresa);
                if (!objres.resultado)
                {
                    resultado = false;
                    return (false, objres.observacion + Environment.NewLine + objres.obsdetalle, 0, dtnegativos);
                }
            }

            // //validar descto PP venta de items G2 pag-41
            if (resultado)
            {
                validar_Vta.InicializarResultado(objres);
                objres = await validar_Vta.Validar_Descto_PP_Items_Grado2_KG(_context, tabladetalle, tabladescuentos, codempresa);
                if (!objres.resultado)
                {
                    resultado = false;
                    return (false, objres.observacion + Environment.NewLine + objres.obsdetalle, 0, dtnegativos);
                }
            }

            // VALIDACION TARIFAS PERMITIDAS POR USUARIO
            if (resultado)
            {
                foreach (var row in tabladetalle)
                {
                    if (!await ventas.UsuarioTarifa_Permitido(_context, usuario, Convert.ToInt32(row.codtarifa)))
                    {
                        resultado = false;
                        return (false, "Este usuario no esta habilitado para ver ese tipo de Precio", 0, dtnegativos);
                    }
                }
            }

            return (true, "OK", 0, dtnegativos);
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
            return (resultado, msgAlert);
        }
        private async Task<(bool bandera, string msg, int codigo_control)> Validar_Datos_Cabecera_Remision(DBContext _context, bool sin_validar, bool sin_validar_MontoMin_desc, bool sin_validar_monto_total, bool sin_validar_doc_ant_inv, string codempresa, string usuario, int cod_proforma, string id_proforma, int nroid_proforma, string codmoneda_pf, string codcliente_real, DatosDocVta DVTA, List<itemDataMatriz> tabladetalle, List<vedesextraDatos> tabladescuentos, string email, decimal tdc)
        {
            bool resultado = true;
            DataTable lista_complementaria = new DataTable();

            if (cod_proforma == 0)
            {
                resultado = false;
                return (false, "Toda Nota de Remision debe ser transferida desde una Proforma.", 0);
            }
            if (DVTA.codcliente_real.Length == 0)
            {
                resultado = false;
                return (false, "El codigo de cliente real esta vacio, consulte con el administrador de sistemas.", 0);
            }

            if (!sin_validar)
            {
                if (DVTA.tipo_vta == "1")
                {
                    if (await cliente.EsContraEntrega(_context, DVTA.codcliente))
                    {
                        //como es contraentrega no controla limite de credito
                    }
                    else
                    {
                        if (DVTA.contra_entrega == "NO")
                        {
                            decimal cd = 0;
                            string moneda_credito = "";
                            moneda_credito = await creditos.Credito_Fijo_Asignado_Vigente_Moneda(_context, codcliente_real);
                            cd = await creditos.CreditoDisponibleParaVentaNuevaRemision(_context, DVTA.codcliente, DVTA.codmoneda, cod_proforma, usuario, codempresa, moneda_credito);
                            if (Convert.ToDecimal(DVTA.totaldoc) > cd)
                            {
                                resultado = false;
                                return (false, "Este Documento sobrepasa el credito disponible del cliente: " + DVTA.codcliente + " el credito disponible es de: " + Math.Round(cd, 2).ToString() + moneda_credito + ". ", 0);
                            }
                        }
                    }
                }
            }

            if (resultado == true)
            {
                if (DVTA.id == "")
                {
                    resultado = false;
                    return (false, "No puede dejar la casilla de Id de la Nota de Remision en blanco.", 0);
                }
                else if (DVTA.codalmacen == "")
                {
                    resultado = false;
                    return (false, "No puede dejar la casilla de Almacen en blanco.", 0);
                }
                else if (DVTA.codvendedor == "")
                {
                    resultado = false;
                    return (false, "No puede dejar la casilla de Vendedor en blanco.", 0);
                }
                else if (DVTA.codcliente == "")
                {
                    resultado = false;
                    return (false, "No puede dejar la casilla del Codigo de Cliente en blanco.", 0);
                }
                else if (DVTA.codcliente_real == "")
                {
                    resultado = false;
                    return (false, "No puede dejar la casilla del Codigo de Cliente Real en blanco.", 0);
                }
                else if (!await cliente.clientehabilitado(_context, DVTA.codcliente))
                {
                    resultado = false;
                    return (false, "Ese Cliente no esta habilitado.", 0);
                }
                else if (!await cliente.clientehabilitado(_context, DVTA.codcliente_real))
                {
                    resultado = false;
                    return (false, "Ese Cliente Real no esta habilitado.", 0);
                }
                else if (DVTA.nombcliente == "")
                {
                    resultado = false;
                    return (false, "No puede dejar la casilla de Nombre del Cliente en blanco.", 0);
                }
                else if (DVTA.nitfactura == "")
                {
                    resultado = false;
                    return (false, "No puede dejar la casilla de N.I.T. del cliente en blanco.", 0);
                }
                
                if (DVTA.direccion == "")
                {
                    DVTA.direccion = "---";
                }
                //verificar que haya elegido el tipo de doc de identidad
                if (resultado)
                {
                    if (Convert.ToInt32(DVTA.tipo_doc_id) < 0)
                    {
                        resultado = false;
                        return (false, "Debe identificar el tipo de documento de identificación del cliente.", 0);
                    }
                }
                if (resultado)
                {
                    var (EsValido, Mensaje) = await ventas.Validar_NIT_Correcto(_context, DVTA.nitfactura, DVTA.tipo_doc_id );
                    if (!EsValido)
                    {
                        resultado = false;
                        return (false, "Verifique que el NIT tenga el formato correcto!!! " + Mensaje, 0);
                    }
                }
                if (email.Trim().Length == 0)
                {
                    resultado = false;
                    return (false, "No se puede dejar la casilla del email en blanco.", 0);
                }
                if (DVTA.preparacion == null || DVTA.preparacion.Trim().Length == 0)
                {
                    resultado = false;
                    return (false, "No se puede dejar la casilla de preparación en blanco.", 0);
                }
                if (DVTA.codmoneda == null || DVTA.codmoneda.Trim().Length == 0)
                {
                    resultado = false;
                    return (false, "No se puede dejar la casilla del código de moneda en blanco.", 0);
                }
                if (DVTA.estado_contra_entrega == null)
                {
                    resultado = false;
                    return (false, "No se puede dejar la casilla de Estado en nulo.", 0);
                }
                if (tdc == 0)
                {
                    resultado = false;
                    return (false, "No se puede dejar la casilla de tipo de Cambio en 0.", 0);
                }
                if (DVTA.subtotaldoc == null)
                {
                    resultado = false;
                    return (false, "No se puede dejar la casilla del sub total en blanco.", 0);
                }
                if (DVTA.subtotaldoc <= 0)
                {
                    resultado = false;
                    return (false, "No se puede dejar la casilla del sub total en 0.", 0);
                }
                if (DVTA.totrecargos == null)
                {
                    resultado = false;
                    return (false, "No se puede dejar la casilla del total total de recargos en blanco.", 0);
                }
                if (DVTA.totdesctos_extras == null)
                {
                    resultado = false;
                    return (false, "No se puede dejar la casilla del total de descuentos en blanco.", 0);
                }
                if (DVTA.totaldoc == null)
                {
                    resultado = false;
                    return (false, "No se puede dejar la casilla del total en blanco.", 0);
                }
                if (DVTA.totaldoc <= 0)
                {
                    resultado = false;
                    return (false, "No se puede dejar la casilla del total en 0.", 0);
                }
            }

            // validar si es una venta al contado con pago anticipado exista los anticipos
            if (DVTA.tipo_vta == "1" && DVTA.contra_entrega == "SI")
            {
                resultado = false;
                return (false, "No esta permitido realizar ventas CREDITO - CONTRA ENTREGA, verifique esta situación.", 0);
            }
            if (DVTA.tipo_vta == "1" && DVTA.estado_contra_entrega.Trim().Length > 0)
            {
                resultado = false;
                return (false, "Una venta a CREDITO con ESTADO CONTRA ENTREGA no es posible, verifique esta situación", 0);
            }
            if (DVTA.tipo_vta == "0" && DVTA.contra_entrega == "SI" && DVTA.estado_contra_entrega.Trim().Length == 0)
            {
                resultado = false;
                return (false, "Una venta al CONTADO-CONTRA ENTREGA sin ESTADO CONTRA ENTREGA NO ES POSIBLE, verifique esta situación", 0);
            }


            if (!sin_validar)
            {
                if (DVTA.tipo_vta == "0")
                {
                    if (!await ventas.cliente_ventacontado(_context, DVTA.codcliente))
                    {
                        resultado = false;
                        return (false, "No se le puede vender al contado al Cliente: " + DVTA.codcliente, 0);
                    }
                }
                else
                {
                    if (!await ventas.cliente_ventacredito(_context, DVTA.codcliente))
                    {
                        resultado = false;
                        return (false, "No se le puede vender al credito al Cliente: " + DVTA.codcliente, 0);
                    }
                }
            }
            if (await seguridad.periodo_fechaabierta_context(_context, DVTA.fechadoc, 3))
            { }
            else
            {
                resultado = false;
                return (false, "No puede crear documentos para ese periodo de fechas.", 0);
            }
            
            //ninguna venta al credito con NIT CERO
            //segun instruccion JRA 23-07-2015
            if (resultado)
            {
                if (DVTA.tipo_vta == "1")
                {
                    //VENTA CREDITO
                    var (EsValido, Mensaje) = await ventas.Validar_NIT_Correcto(_context, DVTA.nitfactura, DVTA.tipo_doc_id + 1);
                    if (EsValido)
                    {
                        if (Convert.ToDouble(DVTA.nitfactura) == 0)
                        {
                            resultado = false;
                            return (false, "No se puede realizar una venta al credito con N.I.T. o Nro de C.I. igual a cero." + Mensaje, 0);
                        }
                    }
                    else
                    {
                        resultado = false;
                        return (false, "Para realizar una venta al credito, debe indicar un N.I.T. o Nro de C.I. Valido. " + Mensaje, 0);
                    }
                }
            }
            if (resultado)
            {
                if (await ventas.yahayproforma(_context, cod_proforma))
                {
                    resultado = false;
                    return (false, "La proforma ya fue transferida a otro documento mientras usted realizaba este documento.", 0);
                }
            }

            if (!sin_validar)
            {
                if (resultado)
                {
                    if (await ventas.proforma_es_complementaria(_context, cod_proforma))
                    {
                        //sacar precios de complementarias
                        lista_complementaria = await ventas.lista_PFNR_complementarias_Table(_context, cod_proforma);
                        var (EsValidoPrecio, mensaje) = await preciosvalidos(_context, "0", DVTA.codcliente, tabladetalle);
                        if (EsValidoPrecio == false)
                        //if (await preciosvalidos(_context, "0", DVTA.codcliente, tabladetalle) == false)
                        {
                            resultado = false;
                            //return (false, "El documento contiene Items a precios no permitidos para este cliente.");
                            return (false, mensaje, 0);
                        }

                        if (resultado)
                        {
                            if (await cliente.Controla_Monto_Minimo(_context, codcliente_real))
                            {
                                if (await montosminimosvalidoscomp(_context, await cliente.EsClienteNuevo(_context, codcliente_real), DVTA.tipo_vta, DVTA.contra_entrega, DVTA.codmoneda, DVTA.fechadoc, tabladetalle, lista_complementaria) == false)
                                {
                                    resultado = false;
                                    return (false, "El Documento no cumple los montos minimos de las listas de precios elegidas.", 0);
                                }
                            }
                        }
                        if (resultado)
                        {
                            if (await cliente.Controla_Monto_Minimo(_context, codcliente_real))
                            {
                                if (await Validar_Monto_Minimo_Para_Aplicar_Descuentos_Especiales_Complementarios(_context, DVTA.codmoneda, DVTA.fechadoc, tabladetalle, lista_complementaria) == false)
                                {
                                    resultado = false;
                                    return (false, "El Documento no cumple los montos minimos de los descuentos elegidos.", 0);
                                }
                            }
                        }
                        if (resultado)
                        {
                            if (await cliente.Controla_empaque_minimo(_context, codcliente_real))
                            {
                                if (await empaquesdoc(_context, Convert.ToInt32(DVTA.codalmacen), codcliente_real, tabladetalle) == false)
                                {
                                    resultado = false;
                                    return (false, "El Documento no cumple los empaques minimos de algunos items.", 0);
                                }
                            }
                        }
                    }
                    else
                    {
                        var (EsValidoPrecio, mensaje) = await preciosvalidos(_context, "0", DVTA.codcliente, tabladetalle);
                        if (EsValidoPrecio == false)
                        //if (await preciosvalidos(_context, "0", DVTA.codcliente, tabladetalle) == false)
                        {
                            resultado = false;
                            //return (false, "El documento contiene Items a precios no permitidos para este cliente.");
                            return (false, mensaje, 0);
                        }

                        if (resultado)
                        {
                            if (await cliente.Controla_Monto_Minimo(_context, codcliente_real))
                            {
                                if (await Validar_Monto_Minimos_Segun_Lista_Precio(_context, await cliente.EsClienteNuevo(_context, codcliente_real), DVTA.tipo_vta, DVTA.contra_entrega, DVTA.codmoneda, DVTA.fechadoc, tabladetalle, lista_complementaria) == false)
                                {
                                    resultado = false;
                                    return (false, "El Documento no cumple los montos minimos de las listas de precios elegidas.", 0);
                                }
                            }
                        }
                        if (resultado)
                        {
                            if (await cliente.Controla_Monto_Minimo(_context, codcliente_real))
                            {
                                if (await Validar_Monto_Minimo_Para_Aplicar_Descuentos_Especiales(_context, DVTA.codmoneda, DVTA.fechadoc, tabladetalle, lista_complementaria) == false)
                                {
                                    resultado = false;
                                    return (false, "El Documento no cumple el monto minimo(SubTotal) requerido para aplicar el descuento especial proveedor o volumen.", 0);
                                }
                            }
                        }
                        if (resultado)
                        {
                            if (await cliente.Controla_empaque_minimo(_context, codcliente_real))
                            {
                                if (await empaquesdoc(_context, Convert.ToInt32(DVTA.codalmacen), codcliente_real, tabladetalle) == false)
                                {
                                    resultado = false;
                                    return (false, "El Documento no cumple los empaques minimos de algunos items.", 0);
                                }
                            }
                        }
                    }
                    //Validar montos minimos de descuentos a todo el documento
                    if (resultado)
                    {
                        if (!sin_validar_MontoMin_desc)
                        {
                            var EsValidoMontosMinimosDesc = await Validar_Montos_Minimos_Para_Desctos_Extras(_context, codcliente_real, DVTA.tipo_vta, Convert.ToDecimal(DVTA.subtotaldoc), DVTA.codmoneda, DVTA.fechadoc, tabladescuentos);
                            if (EsValidoMontosMinimosDesc.EsValido == false)
                            //if (await preciosvalidos(_context, "0", DVTA.codcliente, tabladetalle) == false)
                            {
                                resultado = false;
                                //return (false, "El documento contiene Items a precios no permitidos para este cliente.");
                                return (false, EsValidoMontosMinimosDesc.msg, EsValidoMontosMinimosDesc.codigo_control);
                            }
                        }


                    }
                    //Validar Peso minimo de los descuentos
                    if (resultado)
                    {
                        if (await cliente.Controla_Monto_Minimo(_context, codcliente_real))
                        {
                            //calcular Peso
                            double peso_doc = 0;
                            foreach (var i in tabladetalle)
                            {
                                peso_doc = peso_doc + (i.cantidad * await items.itempeso(_context, i.coditem));
                            }
                            foreach (var j in tabladescuentos)
                            {
                                if (peso_doc < await ventas.PesoMinimoDescuentoExtra(_context, j.coddesextra))
                                {
                                    resultado = false;
                                    //return (false, "El documento contiene Items a precios no permitidos para este cliente.");
                                    return (false, "El descuento " + j.descripcion + " no cumple el peso minimo para su aplicacion.", 0);
                                }
                            }

                        }
                    }
                }

            }
            if (!sin_validar)
            {
                if (resultado)
                {
                    var EsValidoValidarDesc = await ValidarDescuentosHabilitado(_context, codcliente_real, tabladescuentos);
                    if (EsValidoValidarDesc.EsValido == false)
                    {
                        resultado = false;
                        return (false, EsValidoValidarDesc.msg, 0);
                    }
                }
            }
            else
            {
                if (resultado)
                {
                    var EsValidoValidarDesc = await ValidarDescuentosValidos(_context, codempresa, codcliente_real, DVTA, tabladetalle, tabladescuentos);
                    if (EsValidoValidarDesc.EsValido == false)
                    {
                        resultado = false;
                        return (false, EsValidoValidarDesc.msg, 0);
                    }
                }
            }
            //Validar si recoge el cliente o se le entrega
            if (resultado)
            {
                var EsValidoValidarDesc = await MontosMinimosEntregaValidos(_context, DVTA.tipo_vta, DVTA.contra_entrega, DVTA.codmoneda, DVTA.fechadoc, tabladetalle);
                if (EsValidoValidarDesc == true)
                {
                    DVTA.tipoentrega = "ENTREGAR";
                }
                else
                {
                    DVTA.tipoentrega = "RECOGE CLIENTE";
                }
            }
            if (!sin_validar)
            {
                if (resultado)
                {
                    var EsValidoDescRepetidos = await DescuentosRepetidos(_context, codempresa, tabladescuentos);
                    if (EsValidoDescRepetidos == true)
                    {
                        resultado = false;
                        return (false, "El documento tiene descuentos repetidos.", 0);
                    }
                }

            }
            //Validar que no esta usando cuenta de tiendas para sacar nota de remision de almacen
            if (resultado)
            {
                if (await cliente.ExisteCliente(_context, DVTA.codcliente) == false)
                {
                    resultado = false;
                    return (false, "La cuenta de usuario actual es para ventas de otra agencia, no puede generar notas de remision de esta agencia con esta cuenta. Por favor cambia a una cuenta de usuario de la agencia actual.", 0);
                }
            }
            //VALIDAR ALERTAR SI NO IGUALA CON LA PROFORMA
            if (resultado)
            {

                double sbt_proforma = 0;
                double desc_proforma = 0;
                double tot_proforma = 0;
                double DIF_SBTTL = 0;
                double DIF_DESC = 0;
                double DIF_TTL = 0;
                double tot_remision = 0;
                double monto_conversion_sbttl = 0;
                double monto_conversion_desc = 0;
                double monto_conversion_ttl = 0;

                // Desde 08/01/2024 validar tambien que si es una proforma AL CONTADO CON ANTICIPO ESTE VALIDE QUE EL MONTO DEL SUBTOTAL, DESCUENTOS Y TOTAL SEAN IGUALES 
                // LA NOTA DE REMISION Y PROFORMA
                sbt_proforma = (double)await ventas.SubTotal_Proforma(_context, cod_proforma);
                desc_proforma = (double)await ventas.Descuentos_Proforma(_context, cod_proforma);
                tot_proforma = (double)await ventas.Total_Proforma(_context, cod_proforma);

                // si la moneda de la proforma no es la misma que la de la NR, realizar la conversion de la PF a la de la NR
                if (!codmoneda_pf.Equals(DVTA.codmoneda))
                {
                    // sbttl
                    monto_conversion_sbttl = (double)await tipocambio._conversion(_context, DVTA.codmoneda, codmoneda_pf, DVTA.fechadoc, (decimal)sbt_proforma);
                    monto_conversion_sbttl = Math.Round(monto_conversion_sbttl, 2);
                    // desc
                    monto_conversion_desc = (double)await tipocambio._conversion(_context, DVTA.codmoneda, codmoneda_pf, DVTA.fechadoc, (decimal)desc_proforma);
                    monto_conversion_desc = Math.Round(monto_conversion_desc, 2);
                    // ttl
                    monto_conversion_ttl = (double)await tipocambio._conversion(_context, DVTA.codmoneda, codmoneda_pf, DVTA.fechadoc, (decimal)tot_proforma);
                    monto_conversion_ttl = Math.Round(monto_conversion_ttl, 2);
                }
                else
                {
                    monto_conversion_sbttl = sbt_proforma;
                    monto_conversion_desc = desc_proforma;
                    monto_conversion_ttl = tot_proforma;
                }

                // sbttl
                DIF_SBTTL = Math.Round(monto_conversion_sbttl, 2, MidpointRounding.AwayFromZero) - Math.Round(Convert.ToDouble(DVTA.subtotaldoc), 2, MidpointRounding.AwayFromZero);
                DIF_SBTTL = Math.Abs(DIF_SBTTL);
                DIF_SBTTL = Math.Round(DIF_SBTTL, 2);

                // desc
                DIF_DESC = Math.Round(monto_conversion_desc, 2, MidpointRounding.AwayFromZero) - Math.Round(Convert.ToDouble(DVTA.totdesctos_extras), 2, MidpointRounding.AwayFromZero);
                DIF_DESC = Math.Abs(DIF_DESC);
                DIF_DESC = Math.Round(DIF_DESC, 2);

                // ttl
                DIF_TTL = Math.Round(monto_conversion_ttl, 2, MidpointRounding.AwayFromZero) - Math.Round(Convert.ToDouble(DVTA.totaldoc), 2, MidpointRounding.AwayFromZero);
                DIF_TTL = Math.Abs(DIF_TTL);
                DIF_TTL = Math.Round(DIF_TTL, 2);

                // Si es venta al CONTADO CON ANTICIPO NO CONTRA ENTREGA
                if (DVTA.tipo_vta == "0" && DVTA.contra_entrega == "NO")
                {
                    //venta al CONTADO CON ANTICIPO NO CONTRA ENTREGA
                    // sbttl
                    if (DIF_SBTTL >= 0.01)
                    {
                        resultado = false;
                        return (false, "El monto del subtotal de la nota de remision es de: " + DVTA.subtotaldoc + " (" + DVTA.codmoneda + ")" + " no iguala con el monto subtotal de la proforma transferida cuyo monto es: " + monto_conversion_sbttl + " (" + codmoneda_pf + ") la diferencia es de: " + DIF_SBTTL + ". Consulte con el administrador de sistemas!!!", 0);
                    }
                    else
                    {
                        resultado = true;
                    }

                    // desc
                    if (resultado)
                    {
                        if (DIF_DESC >= 0.01)
                        {
                            resultado = false;
                            return (false, "El monto de los descuentos extras de la nota de remision es de: " + DVTA.totdesctos_extras + " (" + DVTA.codmoneda + ")" + " no iguala con el monto de los descuentos extra de la proforma transferida cuyo monto es: " + monto_conversion_desc + " (" + codmoneda_pf + ") la diferencia es de: " + DIF_DESC + ". Consulte con el administrador de sistemas!!!", 0);
                        }
                        else
                        {
                            resultado = true;
                        }
                    }

                    // ttl
                    if (resultado)
                    {
                        if (DIF_TTL >= 0.01)
                        {
                            resultado = false;
                            return (false, "El monto del total de la nota de remision es de: " + DVTA.totaldoc + " (" + DVTA.codmoneda + ")" + " no iguala con el monto total de la proforma transferida cuyo monto es: " + monto_conversion_ttl + " (" + codmoneda_pf + ") la diferencia es de: " + DIF_TTL + ". Consulte con el administrador de sistemas!!!", 0);
                        }
                        else
                        {
                            resultado = true;
                        }
                    }
                }
                else
                {
                    // VENTA CONTADO - CONTRA ENTREGA O CREDITO
                    if (DIF_TTL > 0.5)
                    {
                        if (!sin_validar_monto_total)
                        {
                            resultado = false;
                            return (false, "El monto total de la nota de remision es de: " + DVTA.totaldoc + " (" + DVTA.codmoneda + ")" + " no iguala con el monto total de la proforma transferida cuyo monto es: " + monto_conversion_ttl + " (" + DVTA.codmoneda + ") la diferencia es de: " + DIF_TTL + ". Consulte con el administrador de sistemas!!!", 147);
                            // pedir clave
                        }
                    }
                    else
                    {
                        resultado = true;
                    }
                }
            }
            if (resultado)
            {
                if (!sin_validar_doc_ant_inv)
                {
                    if (await restricciones.ValidarModifDocAntesInventario(_context, Convert.ToInt32(DVTA.codalmacen), DVTA.fechadoc.Date) == true)
                    { }
                    else
                    {
                        resultado = false;
                        return (false, "No puede modificar datos anteriores al ultimo inventario, Para eso necesita una autorizacion especial.", 48);
                    }
                }

            }
            //Validar que no se venda a credito a clientes sin nombre
            if (resultado)
            {
                if (DVTA.tipo_vta == "1")
                {//vta credito
                    if (await cliente.EsClienteSinNombre(_context, DVTA.codcliente) == true)
                    {
                        resultado = false;
                        return (false, "No puede hacer una venta a credito a un cliente Sin Nombre.", 0);
                    }
                }
            }
            if (resultado)
            {
                if (await hardCoded.ValidarDsctosVtaContraEntrega(DVTA.contra_entrega == "SI", tabladescuentos, DVTA.tipo_vta == "0" ? "CONTADO":"CREDITO") == false)
                {
                    resultado = false;
                    return (false, "El documento tiene descuentos no permitidos para ventas Contra Entrega.", 0);
                }
            }
            //'//////////////////////////////////////////////////////////////////////
            //'//si es contado validar que tenga los anticipos asignados
            //'//REVISADO EL 20-07-2018
            //'//////////////////////////////////////////////////////////////////////
            //implementado en fecha 30-11-2019
            if (resultado)
            {
                //llenar precios permitidos
                var dt_audit = await _context.veproforma
                                       .Where(pf => pf.contra_entrega == true && pf.tipopago == 0 && pf.pago_contado_anticipado == true && pf.id == id_proforma && pf.numeroid == nroid_proforma)
                                       .Select(pf => new { pf.codigo, pf.id, pf.numeroid, pf.tipopago, pf.contra_entrega, pf.pago_contado_anticipado })
                                       .ToListAsync();
                if (dt_audit.Count > 0)
                {
                    resultado = false;
                    return (false, "No se puede generar la nota de remision porque la proforma esta grabada como CONTADO - CONTRA ENTREGA y al mismo tiempo se grabo como CONTADO CON PAGO ANTICIPADO, por favor consulte con el responsable del sistema!!!", 0);
                }
            }
            if (resultado)
            {
                if (DVTA.tipo_vta == "0")
                {
                    if (DVTA.contra_entrega == "SI")
                    {
                        //SI ES CONTRA ENTREGA NO HAY PAGOS CON ANTICIPO ENTONCES QUE DEJE SACAR 
                        //NO MAS LA NOTA DE REMISION
                        //EN EL RESUMEN DIARIO NO DEDE INCLUIR
                        //SEGUN LO INSTRUIDO POR MARIELITA MONTAÑO EN FECHA: 20-07-2018
                        resultado = true;
                    }
                    else
                    {
                        //SI ES CONTADO - PERO NO CONTRA ENTREGA SE HA TENIDO QUE PAGAR CON ANTICIPOS
                        //A CONTINUACION SE REVISARA ESO
                        if (!await Validar_Anticipos_Aplicados(_context, id_proforma, nroid_proforma, codempresa, DVTA))
                        {
                            resultado = false;
                            return (false, "La proforma es venta al Contado y no se pago con anticipo, por lo cual no se puede emitir la nota de remision. De lo contratio modifique la proforma como venta: Contado - Contra Entrega para luego pagar con cobranza contra entrega.", 0);
                        }
                        else
                        {
                            resultado = true;
                        }

                    }
                }
            }
            ////////////////////////////////
            return (resultado, "OK", 0);
        }

        private async Task<DatosDocVta> ConvertirVeremisionADatosDocVta(veremision veremision)
        {
            // Simulación de operación asincrónica (por ejemplo, alguna consulta a la base de datos)
            await Task.Delay(10); // Simula una pequeña demora
            return new DatosDocVta
            {
                estado_doc_vta = veremision.anulada ? "Anulada" : "Activa",
                coddocumento = veremision.codigo,
                id = veremision.id,
                numeroid = veremision.numeroid.ToString(),
                fechadoc = veremision.fecha,
                codcliente = veremision.codcliente,
                nombcliente = veremision.nomcliente,
                nitfactura = veremision.nit,
                tipo_doc_id = veremision.tipo_docid?.ToString(),
                codcliente_real = veremision.codcliente_real,
                nomcliente_real = "", // Asigna según lo que necesites
                codtarifadefecto = 0, // Asigna según lo que necesites
                codmoneda = veremision.codmoneda,
                subtotaldoc = (double)veremision.subtotal,
                totaldoc = (double)veremision.total,
                tipo_vta = veremision.tipopago.ToString(),
                codalmacen = veremision.codalmacen.ToString(),
                codvendedor = veremision.codvendedor.ToString(),
                preciovta = "", // Asigna según lo que necesites
                desctoespecial = "", // Asigna según lo que necesites
                preparacion = veremision.preparacion,
                tipo_cliente = "", // Asigna según lo que necesites
                cliente_habilitado = "", // Asigna según lo que necesites
                contra_entrega = (bool)veremision.contra_entrega ? "SI" : "NO",
                vta_cliente_en_oficina = false, // Asigna según lo que necesites
                estado_contra_entrega = veremision.estado_contra_entrega,
                desclinea_segun_solicitud = veremision.desclinea_segun_solicitud ?? false,
                idsol_nivel = veremision.idsoldesctos,
                nroidsol_nivel = veremision.nroidsoldesctos?.ToString(),
                pago_con_anticipo = false, // Asigna según lo que necesites
                niveles_descuento = "", // Asigna según lo que necesites

                // datos al pie de la proforma
                transporte = veremision.transporte,
                nombre_transporte = veremision.nombre_transporte,
                fletepor = veremision.fletepor,
                tipoentrega = veremision.tipoentrega,
                direccion = veremision.direccion,
                ubicacion = "", // Asigna según lo que necesites
                latitud = "", // Asigna según lo que necesites
                longitud = "", // Asigna según lo que necesites
                nroitems = 0, // Asigna según lo que necesites
                totdesctos_extras = (double)veremision.descuentos,
                totrecargos = (double)veremision.recargos,

                // complemento mayorista-dimediado / o complemento para descto por importe
                tipo_complemento = "", // Asigna según lo que necesites
                idpf_complemento = "", // Asigna según lo que necesites
                nroidpf_complemento = "0", // Asigna según lo que necesites

                // para facturación mostrador
                idFC_complementaria = "", // Asigna según lo que necesites
                nroidFC_complementaria = "0", // Asigna según lo que necesites
                nrocaja = "", // Asigna según lo que necesites
                nroautorizacion = "", // Asigna según lo que necesites
                fechalimite_dosificacion = DateTime.MinValue, // Asigna según lo que necesites
                tipo_caja = "", // Asigna según lo que necesites
                version_codcontrol = "", // Asigna según lo que necesites
                nrofactura = "", // Asigna según lo que necesites
                nroticket = "", // Asigna según lo que necesites
                idanticipo = "", // Asigna según lo que necesites
                noridanticipo = "0", // Asigna según lo que necesites
                monto_anticipo = 0, // Asigna según lo que necesites
                idpf_solurgente = "", // Asigna según lo que necesites
                noridpf_solurgente = "0" // Asigna según lo que necesites
            };
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


        private async Task<List<itemDataMatriz>> ConvertirListaVeremision1AListaItemDataMatriz(List<veremision1> listaVeremision1)
        {
            // Simulación de operación asincrónica (por ejemplo, alguna consulta a la base de datos)
            await Task.Delay(10); // Simula una pequeña demora
            if (listaVeremision1 == null)
            {
                return new List<itemDataMatriz>();
            }

            return listaVeremision1
                .Select(ver => ConvertirVeremision1AItemDataMatriz(ver))
                .ToList();
        }


        private async Task<List<verecargosDatos>> ConvertirListaVerecargoremiAListaVerecargosDatos(List<verecargoremi>? listaVerecargoremi)
        {
            // Simulación de operación asincrónica (por ejemplo, alguna consulta a la base de datos)
            await Task.Delay(10); // Simula una pequeña demora
            if (listaVerecargoremi == null)
            {
                return new List<verecargosDatos>();
            }

            return listaVerecargoremi
                .Select(ver => ConvertirVerecargoremiAVerecargosDatos(ver))
                .ToList();
        }


        private async Task<List<vedesextraDatos>> ConvertirVedesextraRemiADatos(List<vedesextraremi> tabladescuentosremi)
        {
            // Simulación de operación asincrónica (por ejemplo, alguna consulta a la base de datos)
            await Task.Delay(10); // Simula una pequeña demora
            if (tabladescuentosremi == null)
            {
                return new List<vedesextraDatos>();
            }

            return tabladescuentosremi.Select(remi => new vedesextraDatos
            {
                coddesextra = remi.coddesextra,
                descripcion = "", // Asigna la descripción según lo que necesites
                porcen = remi.porcen,
                montodoc = remi.montodoc,
                codcobranza = remi.codcobranza ?? 0, // Si es nulo, asigna un valor predeterminado
                codcobranza_contado = remi.codcobranza_contado ?? 0, // Si es nulo, asigna un valor predeterminado
                codanticipo = remi.codanticipo ?? 0 // Si es nulo, asigna un valor predeterminado
            }).ToList();
        }


        private async Task<(bool bandera, string msg)> Validar_Saldos_Negativos_Doc(DBContext _context, string codempresa, string usuario, string codcliente_real, DatosDocVta DVTA, List<itemDataMatriz> tabladetalle, string id, int numeroid)
        {
            bool resultado = true;
            string msg = "";

            if (tabladetalle.Count > 0)
            {
                ResultadoValidacion objres = new ResultadoValidacion();
                validar_Vta.InicializarResultado(objres);
                (objres, dtnegativos) = await validar_Vta.Validar_Saldos_Negativos_Doc_Remision(_context, tabladetalle, DVTA, dtnegativos, codempresa, usuario, id, numeroid);
                if (objres.resultado == false)
                {
                    resultado = objres.resultado;
                    return (resultado, "Si existen negativos.");
                }
            }
            return (resultado, msg);
        }


        DatosDocVta objDocVta = new DatosDocVta();
        private async Task<object?> Llenar_Datos_Del_Documento(DBContext _context, string codempresa, DatosDocVta DVTA, List<itemDataMatriz> tabladetalle)
        {
            // Llena los datos de la proforma
            objDocVta.coddocumento = DVTA.coddocumento;
            objDocVta.estado_doc_vta = "NUEVO";
            objDocVta.id = DVTA.id;
            objDocVta.numeroid = DVTA.numeroid.ToString();
            objDocVta.fechadoc = Convert.ToDateTime(DVTA.fechadoc);
            objDocVta.codcliente = DVTA.codcliente;
            objDocVta.nombcliente = DVTA.nombcliente;
            objDocVta.nitfactura = DVTA.nitfactura;
            objDocVta.codcliente_real = DVTA.codcliente_real;
            objDocVta.nomcliente_real = DVTA.nomcliente_real;
            objDocVta.codmoneda = DVTA.codmoneda;
            objDocVta.codtarifadefecto = await validar_Vta.Precio_Unico_Del_Documento(_context, tabladetalle, codempresa);
            objDocVta.subtotaldoc = (double)DVTA.subtotaldoc;
            objDocVta.totdesctos_extras = (double)DVTA.totdesctos_extras;
            objDocVta.totrecargos = (double)DVTA.totrecargos;
            objDocVta.totaldoc = (double)DVTA.totaldoc;
            //if (DVTA.tipo_vta == ""1)
            //{
            //    objDocVta.tipo_vta = "1";
            //}
            //else
            //{
            //    objDocVta.tipo_vta = "0";
            //}
            // objDocVta.tipo_vta = DVTA.tipo_vta;
            if (DVTA.tipo_vta == "0")
            {
                objDocVta.tipo_vta = "CONTADO";
            }
            else
            {
                objDocVta.tipo_vta = "CREDITO";
            }


            objDocVta.codalmacen = DVTA.codalmacen.ToString();
            objDocVta.codvendedor = DVTA.codvendedor.ToString();
            objDocVta.preciovta = objDocVta.codtarifadefecto.ToString();
            objDocVta.desctoespecial = DVTA.desctoespecial;
            objDocVta.preparacion = DVTA.preparacion;
            if (DVTA.contra_entrega == "SI")
            {
                objDocVta.contra_entrega = "SI";
            }
            else
            {
                objDocVta.contra_entrega = "NO";
            }
            objDocVta.estado_contra_entrega = DVTA.estado_contra_entrega;

            objDocVta.desclinea_segun_solicitud = (bool)DVTA.desclinea_segun_solicitud;
            objDocVta.idsol_nivel = DVTA.idpf_solurgente;
            objDocVta.nroidsol_nivel = DVTA.nroidsol_nivel.ToString();

            // Modificado en fecha: 18-05-2022
            if (objDocVta.desclinea_segun_solicitud)
            {
                objDocVta.codcliente_real = await ventas.Cliente_Referencia_Solicitud_Descuentos(_context, objDocVta.idsol_nivel, Convert.ToInt32(objDocVta.nroidsol_nivel));
                objDocVta.nomcliente_real = await cliente.Razonsocial(_context, objDocVta.codcliente_real);
            }
            else
            {
                objDocVta.codcliente_real = DVTA.codcliente_real;
                objDocVta.nomcliente_real = await cliente.Razonsocial(_context, objDocVta.codcliente_real);
            }

            objDocVta.niveles_descuento = DVTA.niveles_descuento;

            // Datos al pie de la proforma
            objDocVta.transporte = DVTA.transporte;
            objDocVta.nombre_transporte = DVTA.nombre_transporte;
            objDocVta.fletepor = DVTA.fletepor;
            objDocVta.tipoentrega = DVTA.tipoentrega;
            objDocVta.direccion = DVTA.direccion;

            objDocVta.nroitems = tabladetalle.Count;

            // Datos del complemento mayosita - dimediado
            objDocVta.idpf_complemento = DVTA.idpf_complemento;
            objDocVta.nroidpf_complemento = DVTA.nroidpf_complemento;
            objDocVta.tipo_cliente = DVTA.tipo_cliente;
            objDocVta.cliente_habilitado = DVTA.cliente_habilitado;

            objDocVta.latitud = DVTA.latitud;
            objDocVta.longitud = DVTA.longitud;
            objDocVta.ubicacion = DVTA.ubicacion;

            objDocVta.pago_con_anticipo = DVTA.pago_con_anticipo;
            objDocVta.vta_cliente_en_oficina = DVTA.vta_cliente_en_oficina;

            // Para facturación mostrador
            objDocVta.idFC_complementaria = "";
            objDocVta.nroidFC_complementaria = "0";
            objDocVta.nrocaja = "";
            objDocVta.nroautorizacion = "";
            objDocVta.tipo_caja = "";
            //sia_DAL.Datos.Instancia.FechaDelServidor.AddDays(10);
            objDocVta.version_codcontrol = "";
            objDocVta.nrofactura = "0";
            objDocVta.nroticket = "";
            objDocVta.idanticipo = "";
            objDocVta.noridanticipo = "0";
            objDocVta.monto_anticipo = 0;
            objDocVta.idpf_solurgente = "";
            objDocVta.noridpf_solurgente = "0";

            return objDocVta;
        }



        private async Task<(bool EsValido, string msg)> preciosvalidos(DBContext _context, string opcion, string codcliente, List<itemDataMatriz> tabladetalle)
        {
            try
            {
                bool resultado = true;
                bool bandera = true;
                string cadena = "";
                //llenar precios del doc
                List<int> precios = new List<int>();

                foreach (var i in tabladetalle)
                {
                    if (!await YahayPrecio(i.codtarifa, precios))
                    {
                        precios.Add(i.codtarifa);
                    }
                }
                //llenar precios permitidos
                var tabla = await _context.veclienteprecio
                                       .Where(precio => precio.codcliente == codcliente)
                                       .OrderBy(precio => precio.codtarifa)
                                       .Select(precio => precio.codtarifa)
                                       .ToListAsync();

                foreach (var codtarifa in tabla)
                {
                    cadena = cadena + " - " + codtarifa;
                }

                foreach (var i in precios)
                {
                    bandera = false;
                    foreach (var k in tabla)
                    {
                        if (i == k)
                        {
                            bandera = true;
                        }
                    }
                    if (bandera == false)
                    {
                        resultado = false;
                        cadena = "El documento contiene Items a precio " + precios + " el cual no esta permitido para este cliente." + Environment.NewLine + "Los precios Permitidos son: " + cadena;
                        return (resultado, cadena);
                    }
                }


                if (resultado == true)
                {
                    if (opcion == "1")
                    {
                        cadena = "Los precios Permitidos son: " + cadena;
                        resultado = true;
                        return (resultado, cadena);
                    }
                }
                // Todo está correcto
                return (resultado, cadena);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return (false, "Error Servidor al verificar si los precios son validos.");
            }
        }



        private async Task<bool> montosminimosvalidoscomp(DBContext _context, bool cliente_nuevo, string tipo_de_pago, string contra_entrega, string codmoneda, DateTime fecha, List<itemDataMatriz> tabladetalle, DataTable list_PF_Complementaria)
        {
            try
            {
                bool resultado = true;
                bool bandera = true;
                decimal SUBTTL_GRAL_PEDIDO = 0;
                DataTable tabla = new DataTable();

                if (tabladetalle.Count > 0)
                {
                    string cadena = "";
                    List<int> precios = new List<int>();
                    List<decimal> totales = new List<decimal>();
                    decimal montomin = 0;
                    decimal dif = 0;
                    //sacar precios
                    foreach (var i in tabladetalle)
                    {
                        if (precios.Contains(i.codtarifa))
                        { }
                        else { precios.Add(i.codtarifa); }
                        SUBTTL_GRAL_PEDIDO += Convert.ToDecimal(i.total);
                    }
                    //sacar precios de complementarias
                    foreach (DataRow lista in list_PF_Complementaria.Rows)
                    {
                        int codremision = (int)lista["codremision"];
                        int codproforma = (int)lista["codproforma"];
                        if (codremision == 0)
                        {
                            //aun no han sacado su nr
                            tabla.Clear();
                            var sql1 = await _context.veproforma1
                                           .Where(vp1 => vp1.codproforma == codproforma)
                                           .Select(vp1 => vp1.codtarifa)
                                           .Distinct().ToListAsync();
                            var result1 = sql1.Distinct().ToList();
                            tabla = funciones.ToDataTable(result1);
                            foreach (DataRow tbl in tabla.Rows)
                            {
                                if (precios.Contains((int)tbl["codtarifa"]))
                                { }
                                else
                                {
                                    precios.Add((int)tbl["codtarifa"]);
                                }
                            }
                        }
                        else
                        {
                            tabla.Clear();
                            var sql1 = await _context.veremision1
                                           .Where(vp1 => vp1.codremision == codremision)
                                           .Select(vp1 => vp1.codtarifa)
                                           .Distinct().ToListAsync();
                            var result1 = sql1.Distinct().ToList();
                            tabla = funciones.ToDataTable(result1);
                            foreach (DataRow tbl in tabla.Rows)
                            {
                                if (precios.Contains((int)tbl["codtarifa"]))
                                { }
                                else
                                {
                                    precios.Add((int)tbl["codtarifa"]);
                                }
                            }
                        }
                    }
                    //sacartotales
                    for (int i = 0; i < precios.Count; i++)
                    {
                        totales.Add(0);
                        foreach (var k in tabladetalle)
                        {
                            if (precios[i] == k.codtarifa)
                            {
                                totales[i] = totales[i] + Convert.ToDecimal(k.total);
                            }
                        }
                    }
                    //sacartotales de complementarias
                    foreach (DataRow lista in list_PF_Complementaria.Rows)
                    {
                        int codremision = (int)lista["codremision"];
                        int codproforma = (int)lista["codproforma"];
                        if (codremision == 0)
                        {
                            //aun no han sacado su nr
                            tabla.Clear();
                            var sql1 = await _context.veproforma1
                                           .Where(vp1 => vp1.codproforma == codproforma)
                                           .Select(vp1 => new { vp1.codtarifa, total = vp1.totalaut })
                                            .ToListAsync();
                            var result1 = sql1.Distinct().ToList();
                            tabla = funciones.ToDataTable(result1);

                            for (int i = 0; i < precios.Count; i++)
                            {
                                foreach (DataRow k in tabla.Rows)
                                {
                                    if (precios[i] == (int)k["codtarifa"])
                                    {
                                        totales[i] = totales[i] + Convert.ToDecimal((decimal)k["total"]);
                                    }
                                }
                            }
                        }
                        else
                        {
                            tabla.Clear();
                            var sql1 = await _context.veremision1
                                           .Where(vp1 => vp1.codremision == codremision)
                                           .Select(vp1 => new { vp1.codtarifa, total = vp1.total })
                                           .ToListAsync();
                            var result1 = sql1.Distinct().ToList();
                            tabla = funciones.ToDataTable(result1);

                            for (int i = 0; i < precios.Count; i++)
                            {
                                foreach (DataRow k in tabla.Rows)
                                {
                                    if (precios[i] == (int)k["codtarifa"])
                                    {
                                        totales[i] = totales[i] + Convert.ToDecimal((decimal)k["total"]);
                                    }
                                }
                            }
                        }

                    }
                    //comparar y mostrar
                    if (contra_entrega == "SI")
                    {
                        tabla.Clear();
                        var sql1 = await _context.intarifa
                                       //.Where(vp1 => vp1.codremision == codremision)
                                       .Select(it => new { it.codigo, montomin = it.min_contra_entrega, moneda = it.codmoneda_min_contra_entrega })
                                       .OrderBy(it => it.codigo)
                                       .Distinct().ToListAsync();
                        var result1 = sql1.Distinct().ToList();
                        tabla = funciones.ToDataTable(result1);

                    }
                    else
                    {
                        if (tipo_de_pago == "0")
                        {
                            //contado
                            if (cliente_nuevo)
                            {
                                tabla.Clear();
                                var sql1 = await _context.intarifa
                                               //.Where(vp1 => vp1.codremision == codremision)
                                               .Select(it => new { it.codigo, montomin = it.min_nuevo_contado, moneda = it.codmoneda_min_nuevo_contado })
                                               .OrderBy(it => it.codigo)
                                               .Distinct().ToListAsync();
                                var result1 = sql1.Distinct().ToList();
                                tabla = funciones.ToDataTable(result1);
                            }
                            else
                            {

                                tabla.Clear();
                                var sql1 = await _context.intarifa
                                               //.Where(vp1 => vp1.codremision == codremision)
                                               .Select(it => new { it.codigo, montomin = it.min_contado, moneda = it.codmoneda_min_contado })
                                               .OrderBy(it => it.codigo)
                                               .Distinct().ToListAsync();
                                var result1 = sql1.Distinct().ToList();
                                tabla = funciones.ToDataTable(result1);
                            }
                        }
                        else
                        {
                            //credito
                            if (cliente_nuevo)
                            {
                                tabla.Clear();
                                var sql1 = await _context.intarifa
                                               //.Where(vp1 => vp1.codremision == codremision)
                                               .Select(it => new { it.codigo, montomin = it.min_nuevo_credito, moneda = it.codmoneda_min_nuevo_credito })
                                               .OrderBy(it => it.codigo)
                                               .Distinct().ToListAsync();
                                var result1 = sql1.Distinct().ToList();
                                tabla = funciones.ToDataTable(result1);
                            }
                            else
                            {
                                tabla.Clear();
                                var sql1 = await _context.intarifa
                                               //.Where(vp1 => vp1.codremision == codremision)
                                               .Select(it => new { it.codigo, montomin = it.min_credito, moneda = it.codmoneda_min_credito })
                                               .OrderBy(it => it.codigo)
                                               .Distinct().ToListAsync();
                                var result1 = sql1.Distinct().ToList();
                                tabla = funciones.ToDataTable(result1);
                            }
                        }
                    }
                    //obtener el precio que tiene monto min mayor para validar con ese
                    int CODTARIFA_PARA_VALIDAR = await validar_Vta.Tarifa_Monto_Min_Mayor(_context, objDocVta, precios);
                    List<int> precios_para_validar = new List<int>();
                    DataRow[] registro;
                    precios_para_validar.Add(CODTARIFA_PARA_VALIDAR);

                    for (int i = 0; i < precios_para_validar.Count; i++)
                    {
                        // Convertir el valor del índice a cadena y usarlo en la consulta
                        registro = tabla.Select($"codigo='{precios_para_validar[i]}'");

                        // Verificar si se recuperaron los datos del tipo de precio
                        if (registro.Length > 0)
                        {
                            // Convertir los valores necesarios y realizar la conversión de moneda
                            montomin = await tipocambio._conversion(_context, codmoneda, registro[0]["moneda"].ToString(), fecha.Date, Convert.ToDecimal(registro[0]["montomin"]));
                            dif = SUBTTL_GRAL_PEDIDO - montomin;
                            if (dif < 0)
                            {
                                resultado = false;
                                break;
                            }
                        }
                        else
                        {
                            montomin = 99999999;
                            cadena += "No se encontró en la tabla de precios, los parámetros del tipo de precio:" + precios_para_validar[i].ToString() + ", consulte con el administrador del sistema!!!";
                            resultado = false;
                        }
                    }
                }
                else
                {
                    resultado = true;
                }


                // Todo está correcto
                return (resultado);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return (false);
            }
        }

        private async Task<bool> Validar_Monto_Minimo_Para_Aplicar_Descuentos_Especiales_Complementarios(DBContext _context, string codmoneda, DateTime fecha, List<itemDataMatriz> tabladetalle, DataTable list_PF_Complementaria)
        {
            try
            {
                bool resultado = true;
                bool bandera = true;
                DataTable tabla = new DataTable();

                if (tabladetalle.Count > 0)
                {
                    string cadena = "";
                    List<int> descuentos = new List<int>();
                    List<decimal> totales = new List<decimal>();
                    decimal montomin = 0;
                    decimal dif = 0;
                    //sacar descuentos
                    foreach (var i in tabladetalle)
                    {
                        if (i.coddescuento > 0)
                        {
                            if (!await YahayPrecio(i.coddescuento, descuentos))
                            {
                                descuentos.Add(i.coddescuento);
                            }
                        }

                    }
                    //sacar descuentos de complementarias
                    foreach (DataRow lista in list_PF_Complementaria.Rows)
                    {
                        int codremision = (int)lista["codremision"];
                        int codproforma = (int)lista["codproforma"];
                        if (codremision == 0)
                        {
                            //aun no han sacado su nr
                            tabla.Clear();
                            var sql2 = await _context.veproforma1
                                           .Where(vp1 => vp1.codproforma == codproforma)
                                           .Select(vp1 => vp1.coddescuento)
                                           .Distinct().ToListAsync();
                            var result2 = sql2.Distinct().ToList();
                            tabla = funciones.ToDataTable(result2);
                            foreach (DataRow tbl in tabla.Rows)
                            {
                                if ((int)tbl["coddescuento"] > 0)
                                {
                                    if (!await YahayPrecio((int)tbl["coddescuento"], descuentos))
                                    {
                                        descuentos.Add((int)tbl["coddescuento"]);
                                    }
                                }

                            }
                        }
                        else
                        {
                            tabla.Clear();
                            var sql2 = await _context.veremision1
                                           .Where(vp1 => vp1.codremision == codremision)
                                           .Select(vp1 => vp1.coddescuento)
                                           .Distinct().ToListAsync();
                            var result2 = sql2.Distinct().ToList();
                            tabla = funciones.ToDataTable(result2);
                            foreach (DataRow tbl in tabla.Rows)
                            {
                                if ((int)tbl["coddescuento"] > 0)
                                {
                                    if (!await YahayPrecio((int)tbl["coddescuento"], descuentos))
                                    {
                                        descuentos.Add((int)tbl["coddescuento"]);
                                    }
                                }
                            }
                        }
                    }
                    //sacartotales
                    for (int i = 0; i < descuentos.Count; i++)
                    {
                        totales.Add(0);
                        foreach (var k in tabladetalle)
                        {
                            if (descuentos[i] == k.coddescuento)
                            {
                                totales[i] = totales[i] + Convert.ToDecimal(k.total);
                            }
                        }
                    }
                    //sacartotales de complementarias
                    foreach (DataRow lista in list_PF_Complementaria.Rows)
                    {
                        int codremision = (int)lista["codremision"];
                        int codproforma = (int)lista["codproforma"];
                        if (codremision == 0)
                        {
                            //aun no han sacado su nr
                            tabla.Clear();
                            var sql2 = await _context.veproforma1
                                           .Where(vp1 => vp1.codproforma == codproforma)
                                           .Select(vp1 => new { vp1.coddescuento, total = vp1.totalaut })
                                            .ToListAsync();
                            var result2 = sql2.Distinct().ToList();
                            tabla = funciones.ToDataTable(result2);

                            for (int i = 0; i < descuentos.Count; i++)
                            {
                                foreach (DataRow k in tabla.Rows)
                                {
                                    if (descuentos[i] == (int)k["coddescuento"])
                                    {
                                        totales[i] = totales[i] + Convert.ToDecimal((decimal)k["total"]);
                                    }
                                }
                            }
                        }
                        else
                        {
                            tabla.Clear();
                            var sql2 = await _context.veremision1
                                           .Where(vp1 => vp1.codremision == codremision)
                                           .Select(vp1 => new { vp1.coddescuento, total = vp1.total })
                                           .ToListAsync();
                            var result2 = sql2.Distinct().ToList();
                            tabla = funciones.ToDataTable(result2);

                            for (int i = 0; i < descuentos.Count; i++)
                            {
                                foreach (DataRow k in tabla.Rows)
                                {
                                    if (descuentos[i] == (int)k["coddescuento"])
                                    {
                                        totales[i] = totales[i] + Convert.ToDecimal((decimal)k["total"]);
                                    }
                                }
                            }
                        }

                    }
                    //comparar y mostrar
                    //obtener el precio que tiene monto min mayor para validar con ese
                    DataRow[] registro;

                    tabla.Clear();
                    var sql1 = await _context.vedescuento
                                   //.Where(vp1 => vp1.codremision == codremision)
                                   .Select(it => new { it.codigo, it.monto, it.moneda })
                                   .OrderBy(it => it.codigo)
                                   .Distinct().ToListAsync();
                    var result1 = sql1.Distinct().ToList();
                    tabla = funciones.ToDataTable(result1);

                    for (int i = 0; i < descuentos.Count; i++)
                    {
                        // Convertir el valor del índice a cadena y usarlo en la consulta
                        registro = tabla.Select($"codigo='{descuentos[i]}'");

                        // Verificar si se recuperaron los datos del tipo de precio
                        if (registro.Length > 0)
                        {
                            // Convertir los valores necesarios y realizar la conversión de moneda
                            montomin = await tipocambio._conversion(_context, codmoneda, registro[0]["moneda"].ToString(), fecha.Date, Convert.ToDecimal(registro[0]["monto"]));
                            dif = totales[i] - montomin;
                            if (dif < 0)
                            {
                                resultado = false;
                                break;
                            }
                        }
                        else
                        {
                            montomin = 99999999;
                            cadena += "No se encontró en la tabla de descuentos, los parámetros del tipo de descuentos:" + descuentos[i].ToString() + ", consulte con el administrador del sistema!!!";
                            resultado = false;
                        }
                    }
                }
                else
                {
                    resultado = true;
                }
                return (resultado);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return (false);
            }
        }

        private async Task<bool> empaquesdoc(DBContext _context, int codalmacen, string codcliente_real, List<itemDataMatriz> tabladetalle)
        {
            bool resultado = false;
            foreach (var i in tabladetalle)
            {
                if (await restricciones.cumpleempaque(_context, i.coditem, i.codtarifa, i.coddescuento, Convert.ToDecimal(i.cantidad), codalmacen, codcliente_real))
                {
                    resultado = true;
                }
                else
                {
                    resultado = false;
                    break;
                }
            }
            return resultado;
        }

        

        private async Task<bool> Validar_Monto_Minimos_Segun_Lista_Precio(DBContext _context, bool cliente_nuevo, string tipo_de_pago, string contra_entrega, string codmoneda, DateTime fecha, List<itemDataMatriz> tabladetalle, DataTable list_PF_Complementaria)
        {
            try
            {
                bool resultado = true;
                bool bandera = true;
                decimal SUBTTL_GRAL_PEDIDO = 0;
                DataTable tabla = new DataTable();

                if (tabladetalle.Count > 0)
                {
                    string cadena = "";
                    List<int> precios = new List<int>();
                    List<int> precios_list = new List<int>();
                    List<decimal> totales = new List<decimal>();
                    decimal montomin = 0;
                    decimal dif = 0;
                    //sacar precios
                    foreach (var i in tabladetalle)
                    {
                        if (!await YahayPrecio(i.codtarifa, precios))
                        {
                            precios.Add(i.codtarifa);
                        }
                        SUBTTL_GRAL_PEDIDO += Convert.ToDecimal(i.total);
                    }

                    //sacartotales
                    for (int i = 0; i < precios.Count; i++)
                    {
                        totales.Add(0);
                        foreach (var k in tabladetalle)
                        {
                            if (precios[i] == k.codtarifa)
                            {
                                totales[i] = totales[i] + Convert.ToDecimal(k.total);
                            }
                        }
                    }
                    //comparar y mostrar
                    if (contra_entrega == "SI")
                    {
                        tabla.Clear();
                        var sql1 = await _context.intarifa
                                       //.Where(vp1 => vp1.codremision == codremision)
                                       .Select(it => new { it.codigo, montomin = it.min_contra_entrega, moneda = it.codmoneda_min_contra_entrega })
                                       .OrderBy(it => it.codigo)
                                       .Distinct().ToListAsync();
                        var result1 = sql1.Distinct().ToList();
                        tabla = funciones.ToDataTable(result1);

                    }
                    else
                    {
                        if (tipo_de_pago == "0")
                        {
                            //contado
                            if (cliente_nuevo)
                            {
                                tabla.Clear();
                                var sql1 = await _context.intarifa
                                               //.Where(vp1 => vp1.codremision == codremision)
                                               .Select(it => new { it.codigo, montomin = it.min_nuevo_contado, moneda = it.codmoneda_min_nuevo_contado })
                                               .OrderBy(it => it.codigo)
                                               .Distinct().ToListAsync();
                                var result1 = sql1.Distinct().ToList();
                                tabla = funciones.ToDataTable(result1);
                            }
                            else
                            {

                                tabla.Clear();
                                var sql1 = await _context.intarifa
                                               //.Where(vp1 => vp1.codremision == codremision)
                                               .Select(it => new { it.codigo, montomin = it.min_contado, moneda = it.codmoneda_min_contado })
                                               .OrderBy(it => it.codigo)
                                               .Distinct().ToListAsync();
                                var result1 = sql1.Distinct().ToList();
                                tabla = funciones.ToDataTable(result1);
                            }
                        }
                        else
                        {
                            //credito
                            if (cliente_nuevo)
                            {
                                tabla.Clear();
                                var sql1 = await _context.intarifa
                                               //.Where(vp1 => vp1.codremision == codremision)
                                               .Select(it => new { it.codigo, montomin = it.min_nuevo_credito, moneda = it.codmoneda_min_nuevo_credito })
                                               .OrderBy(it => it.codigo)
                                               .Distinct().ToListAsync();
                                var result1 = sql1.Distinct().ToList();
                                tabla = funciones.ToDataTable(result1);
                            }
                            else
                            {
                                tabla.Clear();
                                var sql1 = await _context.intarifa
                                               //.Where(vp1 => vp1.codremision == codremision)
                                               .Select(it => new { it.codigo, montomin = it.min_credito, moneda = it.codmoneda_min_credito })
                                               .OrderBy(it => it.codigo)
                                               .Distinct().ToListAsync();
                                var result1 = sql1.Distinct().ToList();
                                tabla = funciones.ToDataTable(result1);
                            }
                        }
                    }
                    //obtener el precio que tiene monto min mayor para validar con ese
                    int CODTARIFA_PARA_VALIDAR = await validar_Vta.Tarifa_Monto_Min_Mayor(_context, objDocVta, precios);
                    List<int> precios_para_validar = new List<int>();
                    DataRow[] registro;
                    precios_para_validar.Add(CODTARIFA_PARA_VALIDAR);

                    for (int i = 0; i < precios_para_validar.Count; i++)
                    {
                        // Convertir el valor del índice a cadena y usarlo en la consulta
                        registro = tabla.Select($"codigo='{precios_para_validar[i]}'");

                        // Verificar si se recuperaron los datos del tipo de precio
                        if (registro.Length > 0)
                        {
                            // Convertir los valores necesarios y realizar la conversión de moneda
                            montomin = await tipocambio._conversion(_context, codmoneda, registro[0]["moneda"].ToString(), fecha.Date, Convert.ToDecimal(registro[0]["montomin"]));
                            dif = SUBTTL_GRAL_PEDIDO - montomin;
                            if (dif < 0)
                            {
                                resultado = false;
                                break;
                            }
                        }
                        else
                        {
                            montomin = 99999999;
                            cadena += "No se encontró en la tabla de precios, los parámetros del tipo de precio:" + precios_para_validar[i].ToString() + ", consulte con el administrador del sistema!!!";
                            resultado = false;
                        }
                    }
                }
                else
                {
                    resultado = true;
                }


                // Todo está correcto
                return (resultado);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return (false);
            }
        }

        private async Task<bool> Validar_Monto_Minimo_Para_Aplicar_Descuentos_Especiales(DBContext _context, string codmoneda, DateTime fecha, List<itemDataMatriz> tabladetalle, DataTable list_PF_Complementaria)
        {
            try
            {
                bool resultado = true;
                bool bandera = true;
                DataTable tabla = new DataTable();

                if (tabladetalle.Count > 0)
                {
                    string cadena = "";
                    List<int> descuentos = new List<int>();
                    List<decimal> totales = new List<decimal>();
                    decimal montomin = 0;
                    decimal dif = 0;
                    //sacar descuentos
                    foreach (var i in tabladetalle)
                    {
                        if (i.coddescuento > 0)
                        {
                            if (!await YahayPrecio(i.coddescuento, descuentos))
                            {
                                descuentos.Add(i.coddescuento);
                            }
                        }

                    }
                    //sacartotales
                    for (int i = 0; i < descuentos.Count; i++)
                    {
                        totales.Add(0);
                        foreach (var k in tabladetalle)
                        {
                            if (descuentos[i] == k.coddescuento)
                            {
                                totales[i] = totales[i] + Convert.ToDecimal(k.total);
                            }
                        }
                    }
                    //comparar y mostrar
                    //obtener el precio que tiene monto min mayor para validar con ese
                    DataRow[] registro;
                    tabla.Clear();
                    var sql1 = await _context.vedescuento
                                   //.Where(vp1 => vp1.codremision == codremision)
                                   .Select(it => new { it.codigo, it.monto, it.moneda })
                                   .OrderBy(it => it.codigo)
                                   .Distinct().ToListAsync();
                    var result1 = sql1.Distinct().ToList();
                    tabla = funciones.ToDataTable(result1);

                    for (int i = 0; i < descuentos.Count; i++)
                    {
                        // Convertir el valor del índice a cadena y usarlo en la consulta
                        registro = tabla.Select($"codigo='{descuentos[i]}'");

                        // Verificar si se recuperaron los datos del tipo de precio
                        if (registro.Length > 0)
                        {
                            // Convertir los valores necesarios y realizar la conversión de moneda
                            montomin = await tipocambio._conversion(_context, codmoneda, registro[0]["moneda"].ToString(), fecha.Date, Convert.ToDecimal(registro[0]["monto"]));
                            dif = totales[i] - montomin;
                            if (dif < 0)
                            {
                                resultado = false;
                                break;
                            }
                        }
                        else
                        {
                            montomin = 99999999;
                            cadena += "No se encontró en la tabla de descuentos, los parámetros del tipo de descuentos:" + descuentos[i].ToString() + ", consulte con el administrador del sistema!!!";
                            resultado = false;
                        }
                    }
                }
                else
                {
                    resultado = true;
                }
                return (resultado);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return (false);
            }
        }

        private async Task<(bool EsValido, string msg, int codigo_control)> Validar_Montos_Minimos_Para_Desctos_Extras(DBContext _context, string codcliente_real, string tipopago, decimal subtotal, string codmoneda, DateTime fecha, List<vedesextraDatos> tabladescuentos)
        {
            try
            {
                bool resultado = true;
                bool bandera = true;
                decimal _total = 0;
                decimal _montoMIN = 0;
                decimal _dif = 0;
                string cadena = "";
                //llenar precios del doc
                List<int> precios = new List<int>();

                if (await cliente.Controla_Monto_Minimo(_context, codcliente_real))
                {
                    if (tipopago == "0")
                    {
                        //CONTADO
                        foreach (var i in tabladescuentos)
                        {
                            _total = subtotal;
                            _montoMIN = await ventas.MontoMinimoContadoDescuentoExtra(_context, i.coddesextra, codmoneda, fecha.Date);
                            _dif = _montoMIN - _total;
                            if (_montoMIN == 0)
                            {
                                resultado = true;
                            }
                            else
                            {
                                if (_total < _montoMIN)
                                {
                                    resultado = false;
                                    cadena = "El descuento: " + i.coddesextra + "-" + i.descripcion + " no cumple el monto minimo para su aplicacion, el monto mínimo es: " + _montoMIN.ToString() + "(" + codmoneda + ") y el subtotal es: " + _total.ToString() + "(" + codmoneda + "). Verifique si la proforma tiene complemento!!!";
                                    return (resultado, cadena, 65);
                                }
                            }
                        }
                    }
                    else
                    {
                        //CREDITO
                        foreach (var i in tabladescuentos)
                        {
                            _total = subtotal;
                            _montoMIN = await ventas.MontoMinimoCreditoDescuentoExtra(_context, i.coddesextra, codmoneda, fecha.Date);
                            _dif = _montoMIN - _total;
                            if (_montoMIN == 0)
                            {
                                resultado = true;
                            }
                            else
                            {
                                if (_total < _montoMIN)
                                {
                                    resultado = false;
                                    cadena = "El descuento: " + i.coddesextra + "-" + i.descripcion + " no cumple el monto minimo para su aplicacion, el monto mínimo es: " + _montoMIN.ToString() + "(" + codmoneda + ") y el subtotal es: " + _total.ToString() + "(" + codmoneda + "). Verifique si la proforma tiene complemento!!!";
                                    return (resultado, cadena, 65);
                                }
                            }
                        }
                    }
                }
                // Todo está correcto
                return (resultado, cadena, 0);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return (false, "Error Servidor al validar Montos minimos para Descuentos Extras.", 0);
            }
        }

        private async Task<(bool EsValido, string msg)> ValidarDescuentosHabilitado(DBContext _context, string codcliente_real, List<vedesextraDatos> tabladescuentos)
        {
            try
            {
                bool resultado = true;
                string cadena = "";
                foreach (var i in tabladescuentos)
                {
                    if (await cliente.Cliente_Tiene_Descto_Extra_Asignado(_context, i.coddesextra, codcliente_real))
                    {
                        resultado = true;
                    }
                    else
                    {
                        resultado = false;
                        cadena = "Este cliente: " + codcliente_real + " no tiene habilitado el descuento " + i.coddesextra + " " + await nombres.nombredesextra(_context, i.coddesextra);
                        return (resultado, cadena);
                    }
                }
                return (resultado, cadena);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return (false, "Error en Servidor al validar descuentos Habilitaos para cliente.");
            }
        }

        private async Task<(bool EsValido, string msg)> ValidarDescuentosValidos(DBContext _context, string codempresa, string codcliente_real, DatosDocVta DVTA, List<itemDataMatriz> tabladetalle, List<vedesextraDatos> tabladescuentos)
        {
            try
            {
                bool resultado = true;
                string cadena = "";
                int precio_main = await Determinar_El_Precio_Principal_De_La_Proforma(_context, codempresa, DVTA, tabladetalle);
                bool es_contra_entrega = false;
                foreach (var i in tabladescuentos)
                {
                    //verificar que los desctos esten habilitados para el precio principal de la proforma
                    if (!await ventas.Descuento_Extra_Habilitado_Para_Precio(_context, i.coddesextra, precio_main))
                    {
                        resultado = false;
                        cadena = "El descuento: " + i.coddesextra + " no esta habilitado para el precio principal de la proforma: " + precio_main + " Verifique esta situacion!!!";
                        return (resultado, cadena);
                    }
                    es_contra_entrega = DVTA.contra_entrega == "SI";
                    if (!await restricciones.Validar_Contraentrega_Descuento(_context, es_contra_entrega, i.coddesextra))
                    {
                        resultado = false;
                        cadena = "No se puede asignar ese descuento a una venta Contra Entrega.";
                        return (resultado, cadena);
                    }
                    if (await cliente.Cliente_Tiene_Descto_Extra_Asignado(_context, i.coddesextra, codcliente_real))
                    {
                        //control implementado en fecha: 01-09-2022
                        //verifica si los desctos extras son los correctos segun el tipo de precio PRINCIPAL del pedido
                        if (!await ventas.TarifaValidaDescuento(_context, precio_main, i.coddesextra))
                        {
                            resultado = false;
                            cadena = "Se tiene un tipo de precio no permitido para los descuentos otorgados.";
                            return (resultado, cadena);
                        }
                        foreach (var j in tabladetalle)
                        {
                            if (!await ventas.DescuentoEspecialValidoDescuento(_context, j.coddescuento, i.coddesextra))
                            {
                                resultado = false;
                                cadena = "El item " + j.coditem + " tiene un tipo de Descuento Especial no permitido para los descuentos otorgados.";
                                return (resultado, cadena);
                            }
                            else if (!await ventas.DescuentoExtra_ItemValido(_context, j.coditem, i.coddesextra))
                            {
                                resultado = false;
                                cadena = "El item " + j.coditem + " no esta permitido para los descuentos otorgados.";
                                return (resultado, cadena);
                            }
                        }
                    }
                    else
                    {
                        resultado = false;
                        cadena = "Este cliente no tiene habilitado el descuento " + i.coddesextra + " " + nombres.nombredesextra(_context, i.coddesextra);
                        return (resultado, cadena);
                    }
                    if (!resultado)
                    {
                        break;
                    }
                }
                return (resultado, cadena);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return (false, "Error Servidor al verificar si los Descuentos son validos.");
            }
        }

        private async Task<bool> MontosMinimosEntregaValidos(DBContext _context, string tipo_de_pago, string contra_entrega, string codmoneda, DateTime fecha, List<itemDataMatriz> tabladetalle)
        {
            try
            {
                bool resultado = true;
                bool bandera = true;
                decimal SUBTTL_GRAL_PEDIDO = 0;
                DataTable tabla = new DataTable();

                if (tabladetalle.Count > 0)
                {
                    string cadena = "";
                    List<int> precios = new List<int>();
                    List<int> precios_list = new List<int>();
                    List<decimal> totales = new List<decimal>();
                    decimal montomin = 0;
                    decimal dif = 0;
                    //sacar precios
                    foreach (var i in tabladetalle)
                    {
                        if (!await YahayPrecio(i.codtarifa, precios))
                        {
                            precios.Add(i.codtarifa);
                        }
                        SUBTTL_GRAL_PEDIDO += Convert.ToDecimal(i.total);
                    }

                    //sacartotales
                    for (int i = 0; i < precios.Count; i++)
                    {
                        totales.Add(0);
                        foreach (var k in tabladetalle)
                        {
                            if (precios[i] == k.codtarifa)
                            {
                                totales[i] = totales[i] + Convert.ToDecimal(k.total);
                            }
                        }
                    }
                    //comparar y mostrar
                    if (contra_entrega == "SI")
                    {
                        tabla.Clear();
                        var sql1 = await _context.intarifa
                                       //.Where(vp1 => vp1.codremision == codremision)
                                       .Select(it => new { it.codigo, montomin = it.min_contra_entrega, moneda = it.codmoneda_min_contra_entrega })
                                       .OrderBy(it => it.codigo)
                                       .Distinct().ToListAsync();
                        var result1 = sql1.Distinct().ToList();
                        tabla = funciones.ToDataTable(result1);

                    }
                    else
                    {
                        if (tipo_de_pago == "0")
                        {
                            //contado
                            tabla.Clear();
                            var sql1 = await _context.intarifa
                                           //.Where(vp1 => vp1.codremision == codremision)
                                           .Select(it => new { it.codigo, montomin = it.min_entrega_contado, moneda = it.codmoneda_min_entrega_contado })
                                           .OrderBy(it => it.codigo)
                                           .Distinct().ToListAsync();
                            var result1 = sql1.Distinct().ToList();
                            tabla = funciones.ToDataTable(result1);
                        }
                        else
                        {
                            //credito
                            tabla.Clear();
                            var sql1 = await _context.intarifa
                                               //.Where(vp1 => vp1.codremision == codremision)
                                               .Select(it => new { it.codigo, montomin = it.min_entrega_credito, moneda = it.codmoneda_min_entrega_credito })
                                               .OrderBy(it => it.codigo)
                                               .Distinct().ToListAsync();
                            var result1 = sql1.Distinct().ToList();
                            tabla = funciones.ToDataTable(result1);
                        }
                    }
                    //obtener el precio que tiene monto min mayor para validar con ese
                    //int CODTARIFA_PARA_VALIDAR = await validar_Vta.Tarifa_Monto_Min_Mayor(_context, objDocVta, precios);
                    //List<int> precios_para_validar = new List<int>();
                    DataRow[] registro;
                    //precios_para_validar.Add(CODTARIFA_PARA_VALIDAR);

                    for (int i = 0; i < precios.Count; i++)
                    {
                        // Convertir el valor del índice a cadena y usarlo en la consulta
                        registro = tabla.Select($"codigo='{precios[i]}'");

                        // Verificar si se recuperaron los datos del tipo de precio
                        if (registro.Length > 0)
                        {
                            // Convertir los valores necesarios y realizar la conversión de moneda
                            montomin = await tipocambio._conversion(_context, codmoneda, registro[0]["moneda"].ToString(), fecha.Date, Convert.ToDecimal(registro[0]["montomin"]));
                            dif = totales[i] - montomin;
                            if (dif < 0)
                            {
                                resultado = false;
                                break;
                            }
                        }
                        else
                        {
                            montomin = 99999999;
                            cadena += "No se encontró en la tabla de precios, los parámetros del tipo de precio:" + precios[i].ToString() + ", consulte con el administrador del sistema!!!";
                            resultado = false;
                        }
                    }
                }
                else
                {
                    resultado = true;
                }
                // Todo está correcto
                return (resultado);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return (false);
            }
        }


        private async Task<bool> DescuentosRepetidos(DBContext _context, string codempresa, List<vedesextraDatos> tabladescuentos)
        {
            try
            {
                bool resultado = false;
                int coddesextra_depositos = await configuracion.emp_coddesextra_x_deposito(_context, codempresa);
                List<int> listado = new List<int>();
                foreach (var i in tabladescuentos)
                {
                    if (i.coddesextra != coddesextra_depositos)
                    {
                        if (listado.Contains(i.coddesextra))
                        {
                            resultado = true;
                            break;
                        }
                        else { listado.Add(i.coddesextra); }
                    }
                }
                // Todo está correcto
                return (resultado);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return (true);
            }
        }

        private async Task<bool> YahayPrecio(int precio, List<int> precios)
        {
            bool resultado = false;
            for (int i = 0; i < precios.Count; i++)
            {
                if (precio == (int)precios[i])
                {
                    resultado = true;
                    break;
                }
            }
            return resultado;
        }

        private async Task<int> Determinar_El_Precio_Principal_De_La_Proforma(DBContext _context, string codempresa, DatosDocVta DVTA, List<itemDataMatriz> tabladetalle)
        {
            //determinar con que precio se valiara el monto min requerido
            await Llenar_Datos_Del_Documento(_context, codempresa, DVTA, tabladetalle);
            List<int> precios = new List<int>();
            precios = await validar_Vta.Lista_Precios_En_El_Documento(tabladetalle);
            int CODTARIFA_PARA_VALIDAR = await validar_Vta.Tarifa_Monto_Min_Mayor(_context, objDocVta, precios);
            return CODTARIFA_PARA_VALIDAR;
        }

        private verecargosDatos ConvertirVerecargoremiAVerecargosDatos(verecargoremi verecargoremi)
        {
            if (verecargoremi == null)
            {
                return null;
            }

            return new verecargosDatos
            {
                codrecargo = verecargoremi.codrecargo,
                descripcion = "", // Asigna un valor adecuado o busca una forma de obtener la descripción si es necesario
                porcen = verecargoremi.porcen,
                monto = verecargoremi.monto ?? 0, // Utiliza un valor predeterminado si es null
                moneda = verecargoremi.moneda,
                montodoc = verecargoremi.montodoc,
                codcobranza = verecargoremi.codcobranza ?? 0 // Utiliza un valor predeterminado si es null
            };
        }

        private itemDataMatriz ConvertirVeremision1AItemDataMatriz(veremision1 ver)
        {
            if (ver == null)
            {
                return null;
            }

            return new itemDataMatriz
            {
                coditem = ver.coditem,
                descripcion = "", // Asigna un valor adecuado o busca una forma de obtener la descripción si es necesario
                medida = "", // Asigna un valor adecuado o busca una forma de obtener la medida si es necesario
                udm = ver.udm,
                porceniva = (double)(ver.porceniva ?? 0),
                empaque = null, // Asigna un valor adecuado si es necesario
                cantidad_pedida = 0, // Asigna un valor adecuado si es necesario
                cantidad = (double)ver.cantidad,
                porcen_mercaderia = 0, // Asigna un valor adecuado si es necesario
                codtarifa = ver.codtarifa,
                coddescuento = ver.coddescuento,
                preciolista = (double)ver.preciolista,
                niveldesc = ver.niveldesc,
                porcendesc = (double)(ver.preciodesc ?? 0), // Asigna un valor adecuado si es necesario
                preciodesc = (double)(ver.preciodesc ?? 0),
                precioneto = (double)ver.precioneto,
                total = (double)ver.total,
                cumple = true,
                cumpleMin = true,
                cumpleEmp = true,
                nroitem = 0,
                porcentaje = 0,
                monto_descto = 0,
                subtotal_descto_extra = 0
            };
        }

        private async Task<bool> Validar_Anticipos_Aplicados(DBContext _context, string id, int numeroid, string codempresa, DatosDocVta DVTA)
        {
            try
            {
                bool resultado = false;
                //obtener los anticipos aplicados a la proforma 
                //que se pretende sacar nota de remision
                var dt_anticipos = await anticipos_vta_contado.Anticipos_Aplicados_a_Proforma(_context, id, numeroid);
                if (dt_anticipos.Count > 0)
                {
                    ResultadoValidacion objres = new ResultadoValidacion();
                    objres = await anticipos_vta_contado.Validar_Anticipo_Asignado_2(_context, true, DVTA, dt_anticipos, codempresa);
                    //'Desde 15/01/2024 se cambio esta funcion porque no estaba validando correctamente la transformacion de moneda de los anticipos a aplicarse ya se en $us o BS
                    if (objres.resultado == true)
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
            catch (Exception)
            {
                return false;    // si pasa algo que envie false para mandar alerta y no continue.
            }
        }









        private async Task<(string resp, int codNRemision, int numeroId, bool mostrarModificarPlanCuotas, List<planPago_object>? plandeCuotas)> Grabar_Documento(DBContext _context, string id, string usuario, bool desclinea_segun_solicitud, int codProforma, string codempresa, SaveNRemisionCompleta datosRemision)
        {
            veremision veremision = datosRemision.veremision;
            List<veremision1> veremision1 = datosRemision.veremision1;
            var vedesextraremi = datosRemision.vedesextraremi;
            var verecargoremi = datosRemision.verecargoremi;
            var veremision_iva = datosRemision.veremision_iva;
            var veremision_chequerechazado = datosRemision.veremision_chequerechazado;



            ////////////////////   GRABAR DOCUMENTO
            //obtener id actual

            int idnroactual = await documento.ventasnumeroid(_context, id);
            if (await documento.existe_notaremision(_context,id,idnroactual + 1))
            {
                return ("Ese numero de documento, ya existe, por favor consulte con el administrador del sistema.", 0, 0, false,null);
            }
            veremision.fechareg = await funciones.FechaDelServidor(_context);
            veremision.horareg = datos_proforma.getHoraActual();
            veremision.fecha_anulacion = veremision.fecha.Date;
            veremision.version_tarifa = await ventas.VersionTarifaActual(_context);
            veremision.descarga = true;
            veremision.numeroid = idnroactual + 1;
            // fin de obtener id actual


            // verificacion para ver si el documento descarga mercaderia
            // bool descarga = await ventas.iddescarga(_context, id);


            if (desclinea_segun_solicitud == false)
            {
                veremision.idsoldesctos = "0";
                veremision.nroidsoldesctos = 0;
            }


            // accion de guardar

            // guarda cabecera (veremision)
            _context.veremision.Add(veremision);
            await _context.SaveChangesAsync();

            var codNRemision = veremision.codigo;

            // actualiza numero id
            var numeracion = _context.venumeracion.FirstOrDefault(n => n.id == id);
            numeracion.nroactual += 1;
            await _context.SaveChangesAsync(); // Guarda los cambios en la base de datos



            // guarda detalle (veremision1)
            // actualizar codigoproforma para agregar
            veremision1 = veremision1.Select(p => { p.codremision = codNRemision; return p; }).ToList();
            // actualiza grupomer antes de guardado
            veremision1 = await ventas.Remision_Cargar_Grupomer(_context, veremision1);
            // actualizar peso del detalle.
            veremision1 = await ventas.Actualizar_Peso_Detalle_Remision(_context, veremision1);

            _context.veremision1.AddRange(veremision1);
            await _context.SaveChangesAsync();



            // actualizar proforma a transferida
            if (codProforma!=0)
            {
                try
                {
                    var proforma = await _context.veproforma.Where(i => i.codigo == codProforma).FirstOrDefaultAsync();

                    if (proforma != null)
                    {
                        proforma.transferida = true;
                        await _context.SaveChangesAsync();
                    }
                    else
                    {
                        return ("No se pudo encontrar la proforma para transferirla, por favor consulte con el administrador del sistema.", 0, 0, false, null);
                    }
                }
                catch (Exception ex)
                {
                    return ("Error al transferir la proforma, por favor consulte con el administrador del sistema: " + ex.Message, 0, 0, false, null);
                }
            }

            try
            {
                if (vedesextraremi!= null)
                {
                    // grabar descto si hay descuentos
                    if (vedesextraremi.Count() > 0)
                    {
                        await grabardesextra(_context, codNRemision, vedesextraremi);
                    }
                }
            }
            catch (Exception ex)
            {
                return ("Error al Guardar descuentos extras de Nota de Remision, por favor consulte con el administrador del sistema: " +ex.Message, 0, 0, false, null);
            }
            try
            {
                if (verecargoremi != null)
                {
                    // grabar recargo si hay recargos
                    if (verecargoremi.Count > 0)
                    {
                        await grabarrecargo(_context, codNRemision, verecargoremi);
                    }
                }
            }
            catch (Exception ex)
            {
                return ("Error al Guardar recargos de Nota de Remision, por favor consulte con el administrador del sistema: " + ex.Message, 0, 0, false, null);
            }
            try
            {
                if (veremision_iva != null)
                {
                    // grabar iva
                    if (veremision_iva.Count > 0)
                    {
                        await grabariva(_context, codNRemision, veremision_iva);
                    }
                }
            }
            catch (Exception ex)
            {
                return ("Error al Guardar iva de Nota de Remision, por favor consulte con el administrador del sistema: " + ex.Message, 0, 0, false, null);
            }


            


            // ####################################
            // generar plan de pagos si es que es a credito
            // #################################
            List<planPago_object> planCuotasGenerada = new List<planPago_object>();
            bool mostrarModificarPlanCuotas = false;
            if (veremision.tipopago == 1)  // si el tipo pago es a credito == 1
            {
                // SI LA PROFORMA ES DE UNA NOTA ANTERIOR ANULADA SE DEBE HACER EL PLAN DE PAGOS SEGUN LA FECHA
                // DE LA PRIMERA NOTA DE REMISION
                if (await ventas.ProformaTieneNRAnulada(_context, codProforma))
                {
                    // con fecha de ntoa anulada
                    DateTime fecha_antigua = await ventas.ProformaFechaMasAntiguaNR(_context, codProforma);

                    // #si es PP genrarar con su monto y su fecha no importan las complementarias ni influye
                    if (await ventas.remision_es_PP(_context,codNRemision))
                    {
                        
                        if (await ventas.generarcuotaspago(_context,codNRemision,4, (double)veremision.total, (double)veremision.total,veremision.codmoneda, veremision.codcliente, fecha_antigua,false,codempresa))
                        {
                            // modificarplandepago()
                            // ENVIAR BOOL PARA MOSTRAR PLAN DE CUOTAS
                            mostrarModificarPlanCuotas = true;
                            planCuotasGenerada = await verPlandePago(_context, codNRemision, veremision.usuarioreg);
                        }

                    }
                    else
                    {
                        // #si no es PP perocomo es complementaria hacer todo el el chenko
                        if (await ventas.proforma_es_complementaria(_context,codProforma) == false)
                        {
                            if (await ventas.generarcuotaspago(_context, codNRemision, 4, (double)veremision.total, (double)veremision.total, veremision.codmoneda, veremision.codcliente, fecha_antigua, false, codempresa))
                            {
                                // modificarplandepago()
                                // ENVIAR BOOL PARA MOSTRAR PLAN DE CUOTAS
                                mostrarModificarPlanCuotas = true;
                                planCuotasGenerada = await verPlandePago(_context, codNRemision, veremision.usuarioreg);
                            }
                        }
                        else
                        {
                            var lista = await ventas.lista_PFNR_complementarias_noPP(_context, codProforma);
                            if (await ventas.generarcuotaspago(_context, codNRemision, 4, await ventas.MontoTotalComplementarias(_context,lista), (double)veremision.total, veremision.codmoneda, veremision.codcliente, await ventas.FechaComplementariaDeMayorMonto(_context,lista), false, codempresa))
                            {
                                // modificarplandepago(codigo.Text, codcliente.Text, codmoneda.Text, total.Text)
                                mostrarModificarPlanCuotas = true;
                                planCuotasGenerada = await verPlandePago(_context, codNRemision, veremision.usuarioreg);
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
                                    if (reg.codremision == codNRemision)
                                    {
                                        // la actual no hacer pues ya fue hecha
                                    }
                                    else
                                    {
                                        // EN UNA REUNION CON GERENCIA, SE DECIDIO ANULAR ESTA ACCION.
                                        /*
                                        if (await ventas.revertirpagos(_context, reg.codremision,4))
                                        {
                                            await ventas.generarcuotaspago(_context, reg.codremision, 4, await ventas.MontoTotalComplementarias(_context, lista), await ventas.TotalNRconNC_ND(_context, reg.codremision), veremision.codmoneda, veremision.codcliente, await ventas.FechaComplementariaDeMayorMonto(_context, lista), false, codempresa);
                                        }
                                        */
                                    }
                                }
                            }

                        }
                    }
                }
                else
                {
                    // procedimiento normal
                    // #si es PP genrarar con su monto y su fecha no importan las complementarias ni influye
                    if (await ventas.remision_es_PP(_context,codNRemision))
                    {
                        if (await ventas.generarcuotaspago(_context, codNRemision, 4, (double)veremision.total, (double)veremision.total, veremision.codmoneda, veremision.codcliente, veremision.fecha.Date, false, codempresa))
                        {
                            // modificarplandepago(codigo.Text, codcliente.Text, codmoneda.Text, total.Text)
                            mostrarModificarPlanCuotas = true;
                            planCuotasGenerada = await verPlandePago(_context, codNRemision, veremision.usuarioreg);
                        }
                    }
                    else
                    {
                        // #si no es PP perocomo es complementaria hacer todo el el chenko
                        if (await ventas.proforma_es_complementaria(_context,codProforma) == false)
                        {
                            if (await ventas.generarcuotaspago(_context, codNRemision, 4, (double)veremision.total, (double)veremision.total, veremision.codmoneda, veremision.codcliente, veremision.fecha.Date, false, codempresa))
                            {
                                // modificarplandepago(codigo.Text, codcliente.Text, codmoneda.Text, total.Text)
                                mostrarModificarPlanCuotas = true;
                                planCuotasGenerada = await verPlandePago(_context, codNRemision, veremision.usuarioreg);
                            }

                        }
                        else
                        {
                            var lista = await ventas.lista_PFNR_complementarias_noPP(_context, codProforma);
                            if (await ventas.generarcuotaspago(_context, codNRemision, 4, await ventas.MontoTotalComplementarias(_context, lista), (double)veremision.total, veremision.codmoneda, veremision.codcliente, await ventas.FechaComplementariaDeMayorMonto(_context, lista), false, codempresa))
                            {
                                // modificarplandepago(codigo.Text, codcliente.Text, codmoneda.Text, total.Text)
                                mostrarModificarPlanCuotas = true;
                                planCuotasGenerada = await verPlandePago(_context, codNRemision, veremision.usuarioreg);
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
                                    if (reg.codremision == codNRemision)
                                    {
                                        // la actual no hacer pues ya fue hecha
                                    }
                                    else
                                    {
                                        // EN UNA REUNION CON GERENCIA, SE DECIDIO ANULAR ESTA ACCION.
                                        /*
                                        if (await ventas.revertirpagos(_context, reg.codremision, 4))
                                        {
                                            await ventas.generarcuotaspago(_context, reg.codremision, 4, await ventas.MontoTotalComplementarias(_context, lista), await ventas.TotalNRconNC_ND(_context, reg.codremision), veremision.codmoneda, veremision.codcliente, await ventas.FechaComplementariaDeMayorMonto(_context, lista), false, codempresa);
                                        }
                                        */
                                    }
                                }
                            }
                        }
                    }


                }
            }

            //###################################
            //  FIN
            //###################################


            return ("ok", codNRemision, veremision.numeroid, mostrarModificarPlanCuotas, planCuotasGenerada);
        }


        private async Task grabardesextra(DBContext _context, int codRemi, List<vedesextraremi> vedesextraremi)
        {
            var descExtraAnt = await _context.vedesextraremi.Where(i => i.codremision == codRemi).ToListAsync();
            if (descExtraAnt.Count() > 0)
            {
                _context.vedesextraremi.RemoveRange(descExtraAnt);
                await _context.SaveChangesAsync();
            }
            vedesextraremi = vedesextraremi.Select(p => { p.codremision = codRemi; return p; }).ToList();
            _context.vedesextraremi.AddRange(vedesextraremi);
            await _context.SaveChangesAsync();
        }


        private async Task grabarrecargo(DBContext _context, int codRemi, List<verecargoremi> verecargoremi)
        {
            var recargosAnt = await _context.verecargoremi.Where(i => i.codremision == codRemi).ToListAsync();
            if (recargosAnt.Count() > 0)
            {
                _context.verecargoremi.RemoveRange(recargosAnt);
                await _context.SaveChangesAsync();
            }
            verecargoremi = verecargoremi.Select(p => { p.codremision = codRemi; return p; }).ToList();
            _context.verecargoremi.AddRange(verecargoremi);
            await _context.SaveChangesAsync();
        }

        private async Task grabariva(DBContext _context, int codRemi, List<veremision_iva> veremision_iva)
        {
            var ivaAnt = await _context.veremision_iva.Where(i => i.codremision == codRemi).ToListAsync();
            if (ivaAnt.Count() > 0)
            {
                _context.veremision_iva.RemoveRange(ivaAnt);
                await _context.SaveChangesAsync();
            }
            veremision_iva = veremision_iva.Select(p => { p.codremision = codRemi; return p; }).ToList();
            _context.veremision_iva.AddRange(veremision_iva);
            await _context.SaveChangesAsync();
        }
        private async Task<List<planPago_object>> verPlandePago(DBContext _context, int codRemision, string usuario)
        {
            // log de ingreso
            // prgmodifplanpago_Load
            var planPagos = await _context.coplancuotas.Where(i => i.coddocumento == codRemision && i.codtipodoc==4)
                .Select(i => new planPago_object
                {
                    nrocuota = i.nrocuota,
                    vencimiento = i.vencimiento,
                    monto = i.monto,
                    moneda = i.moneda
                })
                .ToListAsync();

            return planPagos;
        }

        [HttpGet]
        [Route("impresionNotaRemision/{userConn}/{codClienteReal}/{codEmpresa}/{codclientedescripcion}/{preparacion}/{codigoNR}")]
        public async Task<ActionResult<List<object>>> impresionNotaRemision(string userConn, string codClienteReal, string codEmpresa, string codclientedescripcion, string preparacion, int codigoNR)
        {
            // lista de impresoras disponibles, aca deben ir de momento las impresoras matriciales de notas de remision, nombre que tienen.
            /*
            var impresorasDisponibles = new Dictionary<int, string>
            {
                { 311, "EPSON LX-350" },  
                { 411, "EPSON LX-350" },
                { 811, "EPSON LX-350" }
            };
            */
            try
            {
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);
                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    
                    var veremision = await _context.veremision.Where(i => i.codigo == codigoNR).FirstOrDefaultAsync();
                    if (veremision != null)
                    {
                        

                        System.Drawing.Printing.PrinterSettings config = new System.Drawing.Printing.PrinterSettings();

                        // Asignar el nombre de la impresora
                        string impresora = await _context.inalmacen.Where(i => i.codigo ==veremision.codalmacen).Select(i => i.impresora_nr).FirstOrDefaultAsync() ?? "";
                        // string pathFile = await mostrardocumento_directo(_context, codClienteReal, codEmpresa, codclientedescripcion, preparacion, veremision);

                        if (impresora == "")
                        {
                            return BadRequest(new { resp = "No se encontró una impresora registrada para este código de almacen, consulte con el administrador." });
                        }
                        config.PrinterName = impresora;

                        // Comprobar si la impresora está instalada
                        if (config.IsValid)
                        {
                            // generamos el archivo .txt y regresamos la ruta
                            string pathFile = await mostrardocumento_directo(_context, codClienteReal, codEmpresa, codclientedescripcion, preparacion, veremision);
                            // Configurar e iniciar el trabajo de impresión
                            // Aquí iría el código para configurar el documento a imprimir y lanzar la impresión
                            bool impremiendo = await RawPrinterHelper.SendFileToPrinterAsync(config.PrinterName, pathFile);
                            // bool impremiendo = await RawPrinterHelper.PrintFileAsync(config.PrinterName, pathFile);

                            // luego de mandar a imprimir eliminamos el archivo
                            if (System.IO.File.Exists(pathFile))
                            {
                                System.IO.File.Delete(pathFile);
                                Console.WriteLine("File deleted successfully.");
                            }
                            else
                            {
                                Console.WriteLine("File not found.");
                            }
                            if (impremiendo)
                            {
                                return Ok(new { resp = "Se envió el documento a la impresora: " + impresora });
                            }
                            else
                            {
                                return BadRequest(new { resp = "No se puedo realizar la impresion, comuniquese con el Administrador de Sistemas." });
                            }
                        }
                        else
                        {
                            return BadRequest(new { resp = "La impresora no está disponible." });
                        }
                    }
                    return BadRequest(new { resp = "No se encontro la nota de remision" });
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return Problem("Error en el servidor al imprimir NR: " + ex.Message);
                throw;
            }
        }





        private async Task<string> mostrardocumento_directo(DBContext _context, string codClienteReal, string codEmpresa, string codclientedescripcion, string preparacion, veremision veremision)
        {
            // If validarimp() Then

            //#################################################
            //mandar impresion
            impresion imp = new impresion();
            //parametros
            string imp_titulo;
            string imp_empresa;
            string imp_usuario;
            string imp_nit;
            string imp_codvendedor;
            string imp_tdc;
            string imp_monedabase;
            string imp_codalmacen;
            string imp_fecha;
            string imp_telefono;
            string imp_ptoventa;
            string imp_codcliente;
            string imp_cliente;
            string imp_direccion;
            string imp_aclaracion_direccion;
            string imp_tipopago;
            string imp_subtotal;
            string imp_descuentos;
            string imp_recargos;
            string imp_totalimp;
            string imp_totalliteral;
            string imp_proforma;
            string imp_pesototal;
            string imp_dsctosdescrip;
            string imp_planpagos = "";
            string imp_flete;
            string imp_transporte;
            string imp_obs;
            string imp_iva;
            string imp_facturacion;
            string imp_nota_plan_pagos;
            string imp_codcliente_real;
            string imp_nit_cliente;
            string imp_complemento_nit_cliente;
            string imp_razonsocial;

            bool es_casual = false;
            if (veremision.codcliente != codClienteReal)
            {
                es_casual = true;
            }

            // Modificaicon del titulo desde fecha: 04-10-2019 se decidio en reunion con JRA Mareln Cinthya V y Emilio
            if (veremision.tipopago == 1 && veremision.contra_entrega == false)
            {
                // CREDITO
                imp_titulo = "NOTA DE REMISION " + veremision.id + "-" + veremision.numeroid + " - PAGARE";
            }
            else
            {
                //TODO LO QUE ES PAGO INMEDIATO NO DEBE DECIR PAGARE: ESTO ES:
                //CONTADO CONTRA ENTREGA
                //CONTADO NO CONTRA ENTREGA
                //CREDITO CONTRA ENTREGA
                imp_titulo = "NOTA DE REMISION " + veremision.id + "-" + veremision.numeroid;
            }
            imp_empresa = await nombres.nombreempresa(_context, codEmpresa);
            imp_usuario = veremision.usuarioreg;

            imp_nit = "N.I.T.: " + await empresa.NITempresa(_context, codEmpresa);

            imp_codvendedor = veremision.codvendedor.ToString();
            imp_tdc = veremision.tdc.ToString();
            imp_monedabase = await Empresa.monedabase(_context, codEmpresa);
            imp_codalmacen = veremision.codalmacen.ToString();
            imp_fecha = veremision.fecha.ToShortDateString();
            imp_aclaracion_direccion = await ventas.aclaracion_direccion_direccion(_context, codClienteReal, veremision.direccion);

            imp_codcliente_real = codClienteReal;

            // recortar de la direccion el punto de venta
            string _direcc = "";
            if (veremision.direccion.Contains("(") && veremision.direccion.Contains(")"))
            {
                // Dim x As Integer = direccion.Text.IndexOf("(")
                if (es_casual)
                {
                    /*
                     
                    'si el cliente es casual, poner la direccion del cliente casual y no del cliente referencia por instruccion Gerencia dsd 05-07-2022
                    'rdireccion.Text = Chr(34) & direccion.Text & Chr(34)
                    '_direcc = sia_funciones.Cliente.Instancia.direccioncliente(codcliente.Text, Me.Usar_Bd_Opcional)
                    '_direcc &= " (" & sia_funciones.Cliente.Instancia.PuntoDeVentaCliente_Segun_Direccion(codcliente.Text, _direcc) & ")"
                    '_direcc = _direcc.Substring(0, _direcc.IndexOf("(") - 1)
                     
                     */

                    // Desde 10-10-2022 si la venta es casual la direccion se pondra la del almacen
                    _direcc = await almacen.direccionalmacen(_context, veremision.codalmacen);
                    // definir con que punto de venta se creara el cliente
                    var dt1_linq = await _context.inalmacen
                        .Where(p1 => p1.codigo == veremision.codalmacen)
                        .Join(_context.adarea,
                              p1 => p1.codarea,
                              p2 => p2.codigo,
                              (p1, p2) => new
                              {
                                  codarea = p2.codigo,
                                  descripcion = p2.descripcion,
                              })
                        .FirstOrDefaultAsync();
                    int codpto_vta = 0;
                    if (dt1_linq != null)
                    {
                        if (dt1_linq.codarea == 300)
                        {
                            codpto_vta = 300;
                        }else if(dt1_linq.codarea == 400)
                        {
                            codpto_vta = 400;
                        }
                        else
                        {
                            codpto_vta = 800;
                        }
                    }
                    _direcc = _direcc + " (" + await cliente.PuntoDeVenta_Casual(_context, codpto_vta) + ")";
                    _direcc = _direcc.Substring(0, _direcc.IndexOf("(") - 1);
                }
                else
                {
                    _direcc = veremision.direccion.Substring(0, veremision.direccion.IndexOf("(") - 1);
                }
            }
            else
            {
                if (es_casual)
                {
                    // _direcc = sia_funciones.Cliente.Instancia.direccioncliente(codcliente.Text, Me.Usar_Bd_Opcional)
                    _direcc = await almacen.direccionalmacen(_context, veremision.codalmacen);
                }
                else
                {
                    _direcc = veremision.direccion;
                }
            }

            if (await cliente.EsClienteSinNombre(_context,veremision.codcliente))
            {
                // PONE EL NOMBRE AL CUAL SE HIZO EL PEDIDO
                imp_codcliente = veremision.nomcliente;
                imp_cliente = veremision.nomcliente;
                imp_direccion = veremision.direccion;
                imp_ptoventa = veremision.direccion;
                imp_telefono = "";
                // estos datos son para la impresion de la parte del contrato que esta en la nota de remision
                imp_razonsocial = veremision.nomcliente;
                imp_nit_cliente = veremision.nit;
                imp_complemento_nit_cliente = veremision.complemento_ci;
            }
            else
            {
                imp_codcliente = veremision.codcliente;
                imp_cliente = codclientedescripcion;
                imp_telefono = await ventas.telefonocliente_direccion(_context, codClienteReal, _direcc);
                imp_ptoventa = await ventas.ptoventacliente_direccion(_context, codClienteReal, veremision.direccion);

                // estos datos son para la impresion de la parte del contrato que esta en la nota de remision
                imp_razonsocial = await cliente.Razonsocial(_context, codClienteReal);
                imp_nit_cliente = await cliente.NIT(_context, codClienteReal);
                imp_complemento_nit_cliente = veremision.complemento_ci;

                if (await ventas.DireccionNotaRemisionEsCentral(_context,veremision.id, veremision.numeroid))
                {
                    // imp_direccion = "CENTRAL -" & direccion.Text
                    if (es_casual)
                    {
                        /*
                         
                        '_direcc = sia_funciones.Cliente.Instancia.direccioncliente(codcliente.Text, Me.Usar_Bd_Opcional)
                        '_direcc &= " (" & sia_funciones.Cliente.Instancia.PuntoDeVentaCliente_Segun_Direccion(codcliente.Text, _direcc) & ")"
                        'imp_direccion = "CENTRAL -" & _direcc
                         
                         */
                        // Desde 10-10-2022 si la venta es casual la direccion se pondra la del almacen
                        _direcc = await almacen.direccionalmacen(_context, veremision.codalmacen);
                        // definir con que punto de venta se creara el cliente
                        var dt1_linq = await _context.inalmacen
                        .Where(p1 => p1.codigo == veremision.codalmacen)
                        .Join(_context.adarea,
                              p1 => p1.codarea,
                              p2 => p2.codigo,
                              (p1, p2) => new
                              {
                                  codarea = p2.codigo,
                                  descripcion = p2.descripcion,
                              })
                        .FirstOrDefaultAsync();
                        int codpto_vta = 0;
                        if (dt1_linq != null)
                        {
                            if (dt1_linq.codarea == 300)
                            {
                                codpto_vta = 300;
                            }
                            else if (dt1_linq.codarea == 400)
                            {
                                codpto_vta = 400;
                            }
                            else
                            {
                                codpto_vta = 800;
                            }
                        }
                        _direcc = _direcc + " (" + await cliente.PuntoDeVenta_Casual(_context, codpto_vta) + ")";
                        imp_direccion = "CENTRAL -" + _direcc;
                    }
                    else
                    {
                        imp_direccion = "CENTRAL -" + veremision.direccion;
                    }
                }
                else
                {
                    // imp_direccion = "SUC -" & direccion.Text
                    if (es_casual)
                    {
                        /*
                        
                        '_direcc = sia_funciones.Cliente.Instancia.direccioncliente(codcliente.Text, Me.Usar_Bd_Opcional)
                        '_direcc &= " (" & sia_funciones.Cliente.Instancia.PuntoDeVentaCliente_Segun_Direccion(codcliente.Text, _direcc) & ")"
                        'imp_direccion = "SUC -" & _direcc

                         */

                        // Desde 10-10-2022 si la venta es casual la direccion se pondra la del almacen
                        _direcc = await almacen.direccionalmacen(_context, veremision.codalmacen);
                        // definir con que punto de venta se creara el cliente
                        var dt1_linq = await _context.inalmacen
                        .Where(p1 => p1.codigo == veremision.codalmacen)
                        .Join(_context.adarea,
                              p1 => p1.codarea,
                              p2 => p2.codigo,
                              (p1, p2) => new
                              {
                                  codarea = p2.codigo,
                                  descripcion = p2.descripcion,
                              })
                        .FirstOrDefaultAsync();
                        int codpto_vta = 0;
                        if (dt1_linq != null)
                        {
                            if (dt1_linq.codarea == 300)
                            {
                                codpto_vta = 300;
                            }
                            else if (dt1_linq.codarea == 400)
                            {
                                codpto_vta = 400;
                            }
                            else
                            {
                                codpto_vta = 800;
                            }
                        }
                        _direcc = _direcc + " (" + await cliente.PuntoDeVenta_Casual(_context, codpto_vta) + ")";
                        imp_direccion = "SUC -" + _direcc;
                    }
                    else
                    {
                        imp_direccion = "SUC -" + veremision.direccion;
                    }
                }
            }

            imp_tipopago = veremision.tipopago == 0 ? "CONTADO" : "CREDITO";
            imp_subtotal = veremision.subtotal.ToString();
            imp_descuentos = veremision.descuentos.ToString();
            imp_recargos = veremision.recargos.ToString();
            imp_totalimp = veremision.total.ToString();

            imp_totalliteral = "SON: " + funciones.ConvertDecimalToWords(veremision.total).ToUpper() + " " + await nombres.nombremoneda(_context, veremision.codmoneda);

            imp_proforma = "PROF: " + await datosproforma(_context, veremision.codproforma ?? 0);
            imp_flete = veremision.fletepor;
            imp_transporte = veremision.transporte + "  NOMB. TRANSPORTE: " + veremision.nombre_transporte;
            imp_pesototal = (veremision.peso ?? 0).ToString();
            // imp_proforma = "PROF: " & sia_funciones.Ventas.Instancia.proforma_de_remision(tabla.Rows(0)("codigo"))
            imp_dsctosdescrip = await ventas.descuentosstr(_context, veremision.codigo, "NR", "Descripcion Completa");

            //###########################################################################################
            // verificar si la proforma esta cancelada con anticipo
            //###########################################################################################
            string cadena_anticipos = "";
            bool Pagado_Con_Anticipo = false;

            var tblanticipos = await _context.veproforma_anticipo.Where(i => i.codproforma == veremision.codproforma).ToListAsync();
            if (tblanticipos.Count() > 0)
            {
                Pagado_Con_Anticipo = true;
                foreach (var reg in tblanticipos)
                {
                    string docanticipo = await cobranzas.IdNroid_Anticipo(_context, reg.codanticipo ?? 0);
                    cadena_anticipos = cadena_anticipos + "(" + docanticipo + ")";
                }
            }
            else
            {
                cadena_anticipos = "";
            }
            /*
             
            '###########################################################################################
            'Desde 07/02/2024 por intruccion Sup de Stock se cambiara el detalle de impresion de la observacion de una NR
            'imp_obs = IIf(venta.Checked, "Venta -", "No es venta -") & tipoentrega.Text & " - "
            'If tipopago.SelectedIndex = 0 Then
            '    '%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
            '    '%%   ES CONTADO
            '    '%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
            '    If contra_entrega.Checked = True Then
            '        imp_obs &= "-" & "VENTA CONTADO - CONTRA ENTREGA" & cmbestado_contra_entrega.Text
            '    Else
            '        If Pagado_Con_Anticipo Then
            '            imp_obs &= "-" & "VENTA CONTADO - YA FUE CANCELADO CON ANTIPO: " & cadena_anticipos
            '        Else
            '            imp_obs &= "-" & "VENTA CONTADO - NO CANCELADO" & cmbestado_contra_entrega.Text
            '        End If
            '    End If
            'Else
            '    '%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
            '    '%%   ES CREDITO
            '    '%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
            '    If contra_entrega.Checked = True Then
            '        imp_obs &= "-" & "VENTA CREDITO - CONTRA ENTREGA" & " " & cmbestado_contra_entrega.Text
            '    Else
            '        imp_obs &= "-" & "VENTA ENTREGA CREDITO"
            '    End If
            'End If
            'imp_obs = imp_obs & obs.Text
             
             
             
             
            'Desde 7 / 2 / 2024 por intruccion Sup de Stock se cambiara el detalle de impresion de la observacion de una NR
            'imp_obs = IIf(venta.Checked, "Venta -", "No es venta -") & tipoentrega.Text & " - "
             */
            if (veremision.tipopago == 0)
            {
                //%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
                //%%   ES CONTADO
                //%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
                if (veremision.contra_entrega == true)
                {
                    imp_obs = "CONTADO - CONTRA ENTREGA " + veremision.estado_contra_entrega;
                }
                else
                {
                    if (Pagado_Con_Anticipo)
                    {
                        imp_obs = "CONTADO - CANCELADO CON ANTIPO: " + cadena_anticipos;
                    }
                    else
                    {
                        imp_obs = "CONTADO - NO CANCELADO " + veremision.estado_contra_entrega;
                    }
                }
            }
            else
            {
                //%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
                //%%   ES CREDITO
                //%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
                if (veremision.contra_entrega == true)
                {
                    imp_obs = "CREDITO - CONTRA ENTREGA " + veremision.estado_contra_entrega;
                }
                else
                {
                    imp_obs = "CREDITO";
                }
            }
            imp_obs = imp_obs + veremision.obs;

            imp_facturacion = veremision.nomcliente + " - " + veremision.nit;
            if (veremision.tipopago == 1)
            {
                if (await ventas.proforma_es_complementaria(_context,veremision.codproforma ?? 0))
                {
                    var lista = await ventas.lista_PFNR_complementarias(_context, veremision.codproforma ?? 0);
                    string plan = "";
                    foreach (var reg in lista)
                    {
                        if (reg.codremision == 0)
                        {
                            // nada
                        }
                        else
                        {
                            if (plan == "")
                            {
                                plan = await ventas.remision_id_nro(_context, veremision.codigo) + " " + await ventas.planpagosstr(_context, reg.codremision);
                            }
                            else
                            {
                                plan = plan + "\n\r" + await ventas.remision_id_nro(_context, reg.codremision) + " " + await ventas.planpagosstr(_context, reg.codremision);
                            }
                        }
                    }
                    if (await ventas.EsRemisionEspecial(_context,veremision.codigo))
                    {
                        plan = plan + " NOTA: Pasado el plazo de cancelacion los precios se recalcularan automaticamente a precios sin descuento.";
                    }
                    imp_planpagos = plan;
                }
                else
                {
                    string plan = await ventas.planpagosstr(_context,veremision.codigo);
                    if (await ventas.EsRemisionEspecial(_context,veremision.codigo))
                    {
                        plan = plan + " NOTA: Pasado el plazo de cancelacion los precios se recalcularan automaticamente a precios sin descuento.";
                    }
                    imp_planpagos = plan;
                }
            }
            // mostrardetalle(codigo.Text)
            /*
            
            Dim dt As New DataTable
            dt = midatasetdetalle.Tables(0)
            '##poner peso
            For i As Integer = 0 To dt.Rows.Count - 1
                dt.Rows(i)("preciodesc") = CDbl(dt.Rows(i)("cantidad")) * sia_funciones.Items.Instancia.itempeso(CStr(dt.Rows(i)("coditem")))
            Next
            '##fin poner peso

             */
            imp_iva = (veremision.iva ?? 0).ToString();

            if ((double)await tipocambio._conversion(_context,await Empresa.monedabase(_context,codEmpresa), veremision.codmoneda, veremision.fecha.Date,veremision.total) >= await configuracion.emp_monto_rnd100011(_context, codEmpresa))
            {
                imp_nota_plan_pagos = "DEBE PAGAR ESTE DOCUMENTO POR MEDIO DE UNA ENTIDAD BANCARIA (RND 10.00.1.11)";
            }
            else
            {
                imp_nota_plan_pagos = "";
            }

            // cambiar el codigo de cliente_real al codigo de cliente casual 
            string codcliente_real = "";
            if (es_casual)
            {
                codcliente_real = veremision.codcliente;
            }
            else
            {
                codcliente_real = codClienteReal;
            }
            /*
             
            If sia_funciones.Empresa.Instancia.HojaReportes(sia_compartidos.temporales.Instancia.codempresa) = 0 Then
                If sia_funciones.Empresa.Instancia.EsArgentina(sia_compartidos.temporales.Instancia.codempresa) Then
                    imp.imprimir_veremision(Application.StartupPath, dt, imp_titulo, imp_empresa, imp_usuario, imp_nit, imp_codvendedor, imp_tdc, imp_monedabase, imp_codalmacen, imp_fecha, imp_telefono, imp_ptoventa, imp_codcliente, imp_cliente, imp_direccion, imp_tipopago, imp_subtotal, imp_descuentos, imp_recargos, imp_totalimp, imp_totalliteral, imp_proforma, imp_pesototal, imp_dsctosdescrip, imp_planpagos, imp_flete, imp_transporte, imp_obs, preparacion.Text, imp_iva, imp_facturacion, True, imp_nota_plan_pagos, imp_aclaracion_direccion, sia_funciones.Cliente.Instancia.NombreComercial(imp_codcliente), codcliente_real, imp_razonsocial, imp_nit_cliente, imp_complemento_nit_cliente, Me.Usar_Bd_Opcional, IIf(contra_entrega.Checked = True, True, False))
                Else
                    '//si es cliente sin nombre
                    If sia_funciones.Cliente.Instancia.EsClienteSinNombre(codcliente.Text, False) Then
                        imp.imprimir_veremision(Application.StartupPath, dt, imp_titulo, imp_empresa, imp_usuario, imp_nit, imp_codvendedor, imp_tdc, imp_monedabase, imp_codalmacen, imp_fecha, imp_telefono, imp_ptoventa, imp_codcliente, imp_cliente, imp_direccion, imp_tipopago, imp_subtotal, imp_descuentos, imp_recargos, imp_totalimp, imp_totalliteral, imp_proforma, imp_pesototal, imp_dsctosdescrip, imp_planpagos, imp_flete, imp_transporte, imp_obs, preparacion.Text, imp_iva, imp_facturacion, False, imp_nota_plan_pagos, imp_aclaracion_direccion, imp_codcliente, codcliente_real, imp_razonsocial, imp_nit_cliente, imp_complemento_nit_cliente, Me.Usar_Bd_Opcional, IIf(contra_entrega.Checked = True, True, False))
                    Else
                        imp.imprimir_veremision(Application.StartupPath, dt, imp_titulo, imp_empresa, imp_usuario, imp_nit, imp_codvendedor, imp_tdc, imp_monedabase, imp_codalmacen, imp_fecha, imp_telefono, imp_ptoventa, imp_codcliente, imp_cliente, imp_direccion, imp_tipopago, imp_subtotal, imp_descuentos, imp_recargos, imp_totalimp, imp_totalliteral, imp_proforma, imp_pesototal, imp_dsctosdescrip, imp_planpagos, imp_flete, imp_transporte, imp_obs, preparacion.Text, imp_iva, imp_facturacion, False, imp_nota_plan_pagos, imp_aclaracion_direccion, sia_funciones.Cliente.Instancia.NombreComercial(codcliente_real), imp_codcliente_real, imp_razonsocial, imp_nit_cliente, imp_complemento_nit_cliente, Me.Usar_Bd_Opcional, IIf(contra_entrega.Checked = True, True, False))
                    End If

                End If
            Else
                If sia_funciones.Empresa.Instancia.EsArgentina(sia_compartidos.temporales.Instancia.codempresa) Then
                    imp.imprimir_veremision_1225(Application.StartupPath, dt, imp_titulo, imp_empresa, imp_usuario, imp_nit, imp_codvendedor, imp_tdc, imp_monedabase, imp_codalmacen, imp_fecha, imp_telefono, imp_ptoventa, imp_codcliente, imp_cliente, imp_direccion, imp_tipopago, imp_subtotal, imp_descuentos, imp_recargos, imp_totalimp, imp_totalliteral, imp_proforma, imp_pesototal, imp_dsctosdescrip, imp_planpagos, imp_flete, imp_transporte, imp_obs, preparacion.Text, imp_iva, imp_facturacion, True, imp_nota_plan_pagos)
                Else
                    imp.imprimir_veremision_1225(Application.StartupPath, dt, imp_titulo, imp_empresa, imp_usuario, imp_nit, imp_codvendedor, imp_tdc, imp_monedabase, imp_codalmacen, imp_fecha, imp_telefono, imp_ptoventa, imp_codcliente, imp_cliente, imp_direccion, imp_tipopago, imp_subtotal, imp_descuentos, imp_recargos, imp_totalimp, imp_totalliteral, imp_proforma, imp_pesototal, imp_dsctosdescrip, imp_planpagos, imp_flete, imp_transporte, imp_obs, preparacion.Text, imp_iva, imp_facturacion, False, imp_nota_plan_pagos)
                End If
            End If

             
             */

            // obtener detalle en data Table
            DataTable dt = await obtenerDetalleDataTable(_context, veremision.codigo);

            // directorio

            string currentDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string outputDirectory = Path.Combine(currentDirectory, "OutputFiles");
            // si es cliente sin nombre
            string ruta = "";
            if (await cliente.EsClienteSinNombre(_context,veremision.codcliente))
            {
                //imp.imprimir_veremision()
                ruta = imp.imprimir_veremision(outputDirectory, dt, imp_titulo, imp_empresa, imp_usuario, imp_nit, imp_codvendedor, imp_tdc, imp_monedabase, imp_codalmacen, imp_fecha, imp_telefono, imp_ptoventa, imp_codcliente, imp_cliente, imp_direccion, imp_tipopago, imp_subtotal, imp_descuentos, imp_recargos, imp_totalimp, imp_totalliteral, imp_proforma, imp_pesototal, imp_dsctosdescrip, imp_planpagos, imp_flete, imp_transporte, imp_obs, preparacion, imp_iva, imp_facturacion, false, imp_nota_plan_pagos, imp_aclaracion_direccion, imp_codcliente, codcliente_real, imp_razonsocial, imp_nit_cliente, imp_complemento_nit_cliente, false, veremision.contra_entrega == true ? true : false);

            }
            else
            {
                ruta = imp.imprimir_veremision(outputDirectory, dt, imp_titulo, imp_empresa, imp_usuario, imp_nit, imp_codvendedor, imp_tdc, imp_monedabase, imp_codalmacen, imp_fecha, imp_telefono, imp_ptoventa, imp_codcliente, imp_cliente, imp_direccion, imp_tipopago, imp_subtotal, imp_descuentos, imp_recargos, imp_totalimp, imp_totalliteral, imp_proforma, imp_pesototal, imp_dsctosdescrip, imp_planpagos, imp_flete, imp_transporte, imp_obs, preparacion, imp_iva, imp_facturacion, false, imp_nota_plan_pagos, imp_aclaracion_direccion, await cliente.NombreComercial(_context,codcliente_real), imp_codcliente_real, imp_razonsocial, imp_nit_cliente, imp_complemento_nit_cliente, false, veremision.contra_entrega == true ? true : false);
            }
            return ruta;
        }






        private async Task<DataTable> obtenerDetalleDataTable(DBContext _context, int codigo)
        {
            var detalleRemision = await _context.veremision1.Where(i => i.codremision == codigo)
                .Join(_context.initem,
                    r => r.coditem,
                    i => i.codigo,
                    (r, i) => new
                    {
                        coditem = r.coditem,
                        descipcion = i.descripcion,
                        medida = i.medida,
                        udm = r.udm,
                        porceniva = r.porceniva ?? 0,
                        niveldesc = r.niveldesc,
                        cantidad = r.cantidad,
                        codtarifa = r.codtarifa,
                        coddescuento = r.coddescuento,
                        precioneto = r.precioneto,
                        preciodesc = r.preciodesc ?? 0,
                        preciolista = r.preciolista,
                        total = r.total,
                        cumple = 1,
                        peso = r.peso
                    })
                .OrderBy(i => i.coditem)
                .ToListAsync();

            // convertir a dataTable
            // Crear un DataTable y definir sus columnas
            DataTable dataTable = new DataTable();
            dataTable.Columns.Add("coditem", typeof(string));
            dataTable.Columns.Add("descripcion", typeof(string));
            dataTable.Columns.Add("medida", typeof(string));
            dataTable.Columns.Add("udm", typeof(string));
            dataTable.Columns.Add("porceniva", typeof(decimal));
            dataTable.Columns.Add("niveldesc", typeof(string));
            dataTable.Columns.Add("cantidad", typeof(decimal));
            dataTable.Columns.Add("codtarifa", typeof(string));
            dataTable.Columns.Add("coddescuento", typeof(string));
            dataTable.Columns.Add("precioneto", typeof(decimal));
            dataTable.Columns.Add("preciodesc", typeof(decimal));
            dataTable.Columns.Add("preciolista", typeof(decimal));
            dataTable.Columns.Add("total", typeof(decimal));
            dataTable.Columns.Add("cumple", typeof(int));
            dataTable.Columns.Add("peso", typeof(decimal));

            // Rellenar el DataTable con los resultados de la consulta
            foreach (var item in detalleRemision)
            {
                dataTable.Rows.Add(
                    item.coditem,
                    item.descipcion,
                    item.medida,
                    item.udm,
                    item.porceniva,
                    item.niveldesc,
                    item.cantidad,
                    item.codtarifa,
                    item.coddescuento,
                    item.precioneto,
                    item.preciodesc,
                    item.preciolista,
                    item.total,
                    item.cumple,
                    item.peso
                );
            }
            return dataTable;
        }
        private async Task<string> datosproforma(DBContext _context, int codigo)
        {
            var data = await _context.veproforma.Where(i => i.codigo == codigo).Select(i => new
            {
                i.id,
                i.numeroid
            }).FirstOrDefaultAsync();
            if (data != null)
            {
                return data.id + "-" + data.numeroid;
            }
            return "";
        }


    }

    public class planPago_object
    {
        public int nrocuota { get; set; }
        public DateTime vencimiento { get; set; }
        public decimal monto { get; set; }
        public string moneda { get; set; }
    }
}
