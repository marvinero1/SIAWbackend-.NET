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


namespace SIAW.Controllers.z_pruebas
{
    [Route("api/pruebas/[controller]")]
    [ApiController]
    public class generadorNRemiController : ControllerBase
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

        private readonly Documento documento = new Documento();
        private readonly Depositos_Cliente depositos_Cliente = new Depositos_Cliente();
        private readonly Inventario inventario = new Inventario();
        private readonly Restricciones restricciones = new Restricciones();
        private readonly HardCoded hardCoded = new HardCoded();

        private readonly Log log = new Log();
        private readonly string _controllerName = "veremisionController";

        // Definir la política de reintento como una propiedad global
        private readonly AsyncRetryPolicy _retryPolicy;

        public generadorNRemiController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
            // Inicializar el nombre del controlador en el constructor
        }
        [HttpPost]
        [Route("generarNotasRemi/{userConn}/{usuario}/{codempresa}/{fechaInicio}/{fechaFin}")]
        public async Task<ActionResult<List<sldosItemCompleto>>> generarNotasRemi(string userConn, string usuario, string codempresa, DateTime fechaInicio, DateTime fechaFin)
        {
            try
            {
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);
                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    List<string> confirmaciones = new List<string>();
                    var drNRemisiones = await _context.veremision.Where(i => i.fecha >= fechaInicio && i.fecha <= fechaFin && i.anulada == false && !i.codcliente.StartsWith("SN") && (i.id.StartsWith("NR")))
                        .Select(i => new
                        {
                            i.codigo,
                            i.id,
                            i.numeroid,
                            i.fecha,
                            i.codalmacen,
                            i.nit,
                            i.subtotal,
                            i.descuentos,
                            i.total
                        }).ToListAsync();
                    foreach (var reg in drNRemisiones)
                    {

                    }
                    return Ok();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return Problem("Error en el servidor : " + ex.Message);
                throw;
            }
        }
        private async Task<object> transferirdoc(DBContext _context, string idNRemi, int nroidNRemi, string usuario)
        {
            var cabecera = await _context.veremision
                .Where(i => i.id == idNRemi && i.numeroid == nroidNRemi)
                .FirstOrDefaultAsync();
            return cabecera;

        }



        private async Task<object> grabarNotaRemision(string userConn, string id, string usuario, bool desclinea_segun_solicitud, int codProforma, string id_pf, int nroid_pf, string codempresa, string id_solurg, int nroid_solurg, bool sin_validar, bool sin_validar_empaques, bool sin_validar_negativos, bool sin_validar_monto_min_desc, bool sin_validar_monto_total, bool sin_validar_doc_ant_inv, SaveNRemisionCompleta datosRemision)
        {
            bool resultado = false;
            // borrar los items con cantidad cero
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

            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);
            using (var _context = DbContextFactory.Create(userConnectionString))
            {

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
                        await dbContexTransaction.CommitAsync();
                    }
                    catch (Exception ex)
                    {
                        await dbContexTransaction.RollbackAsync();
                        return Problem($"Error en el servidor al grabar NR: {ex.Message}");
                        throw;
                    }
                }

                /*

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
                            actualizaNR = await saldos.Veremision_ActualizarSaldo(_context, usuario, codNRemision, Saldos.ModoActualizacion.Crear);
                        }
                        catch (Exception ex)
                        {
                            // return ("Error al Actualizar stock de NR desde Nota de Remision, por favor consulte con el administrador del sistema: " + ex.Message, 0, 0, false, null);
                            Console.WriteLine(ex.ToString());
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
                */
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
                    /*
                    resultado = true;
                    // devolver
                    string msgAlertGrabado = "Se grabo la Nota de Remision " + datosRemision.veremision.id + "-" + numeroId + " con Exito.";

                    //Actualizar Credito
                    //sia_funciones.Creditos.Instancia.Actualizar_Credito_2020(codcliente.Text, sia_compartidos.temporales.Instancia.usuario, sia_compartidos.temporales.Instancia.codempresa, True, Me.Usar_Bd_Opcional)

                    // Actualizar Credito
                    await creditos.Actualizar_Credito_2023(_context, datosRemision.veremision.codcliente, usuario, codempresa, true);

                    // enlazar a la solicitud urgente
                    string msgSolUrg = "";
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
                    */

                    return Ok(new
                    {
                        codNotRemision = codNRemision,
                        nroIdRemision = numeroId,
                        mostrarVentanaModifPlanCuotas = mostrarModificarPlanCuotas,
                        planCuotas = plandeCuotas,
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



        private async Task<(string resp, int codNRemision, int numeroId)> Grabar_Documento(DBContext _context, string id, string usuario, bool desclinea_segun_solicitud, int codProforma, string codempresa, SaveNRemisionCompleta datosRemision)
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
            if (await documento.existe_notaremision(_context, id, idnroactual + 1))
            {
                return ("Ese numero de documento, ya existe, por favor consulte con el administrador del sistema.", 0, 0);
            }
            veremision.fechareg = await funciones.FechaDelServidor(_context);
            veremision.horareg = datos_proforma.getHoraActual();
            veremision.fecha_anulacion = veremision.fecha.Date;
            veremision.version_tarifa = await ventas.VersionTarifaActual(_context);
            veremision.descarga = true;
            veremision.numeroid = idnroactual + 1;
            // fin de obtener id actual

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
            if (codProforma != 0)
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
                        return ("No se pudo encontrar la proforma para transferirla, por favor consulte con el administrador del sistema.", 0, 0);
                    }
                }
                catch (Exception ex)
                {
                    return ("Error al transferir la proforma, por favor consulte con el administrador del sistema: " + ex.Message, 0, 0);
                }
            }

            try
            {
                if (vedesextraremi != null)
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
                return ("Error al Guardar descuentos extras de Nota de Remision, por favor consulte con el administrador del sistema: " + ex.Message, 0, 0);
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
                return ("Error al Guardar recargos de Nota de Remision, por favor consulte con el administrador del sistema: " + ex.Message, 0, 0);
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
                return ("Error al Guardar iva de Nota de Remision, por favor consulte con el administrador del sistema: " + ex.Message, 0, 0);
            }





            // ####################################
            // generar plan de pagos si es que es a credito
            // #################################

            /*

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
                    if (await ventas.remision_es_PP(_context, codNRemision))
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
                        // #si no es PP perocomo es complementaria hacer todo el el chenko
                        if (await ventas.proforma_es_complementaria(_context, codProforma) == false)
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
                                        if (await ventas.revertirpagos(_context, reg.codremision,4))
                                        {
                                            await ventas.generarcuotaspago(_context, reg.codremision, 4, await ventas.MontoTotalComplementarias(_context, lista), await ventas.TotalNRconNC_ND(_context, reg.codremision), veremision.codmoneda, veremision.codcliente, await ventas.FechaComplementariaDeMayorMonto(_context, lista), false, codempresa);
                                        }
                                        */
            /*
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
                    if (await ventas.remision_es_PP(_context, codNRemision))
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
                        if (await ventas.proforma_es_complementaria(_context, codProforma) == false)
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
            /*
                                    }
                                }
                            }
                        }
                    }


                }
            }

            */

            //###################################
            //  FIN
            //###################################


            return ("ok", codNRemision, veremision.numeroid);
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
            var planPagos = await _context.coplancuotas.Where(i => i.coddocumento == codRemision && i.codtipodoc == 4)
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

    }


    public class planPago_object
    {
        public int nrocuota { get; set; }
        public DateTime vencimiento { get; set; }
        public decimal monto { get; set; }
        public string moneda { get; set; }
    }

}
