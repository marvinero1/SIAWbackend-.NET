using LibSIAVB;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIAW.Controllers.ventas.modificacion;
using siaw_DBContext.Data;
using siaw_DBContext.Models;
using siaw_DBContext.Models_Extra;
using siaw_funciones;
using System.Data;
using System.Web.Http.Results;
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

        private readonly Documento documento = new Documento();
        private readonly Log log = new Log();

        public veremisionController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
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
            catch (Exception)
            {
                return Problem("Error en el servidor");
                throw;
            }
        }

        [HttpGet]
        [Route("validaTranferencia/{userConn}/{idProforma}/{nroidProforma}")]
        public async Task<object> validaTranferencia(string userConn, string idProforma, int nroidProforma)
        {
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
            catch (Exception)
            {
                return Problem("Error en el servidor");
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
                    var data = await transferirdatosproforma(_context, codproforma);
                    return Ok(new
                    {
                        msgAlert = msgAlert,
                        txtid_solurgente,
                        txtnroid_solurgente,

                        data
                    });
                }
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
                throw;
            }
        }







        private async Task<object> transferirdatosproforma(DBContext _context, int codproforma)
        {
            // obtener cabecera.
            var cabecera = await _context.veproforma
                .Where(i => i.codigo == codproforma)
                .FirstOrDefaultAsync();

            string _codcliente_real = cabecera.codcliente_real;


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
                .Select(i => new itemDataMatriz
                {
                    //codproforma = i.p.codproforma,
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



                    // cantaut = i.p.cantaut,
                    // totalaut = i.p.totalaut,
                    // obs = i.p.obs,
                    // cantidad_pedida = (double)(i.p.cantidad_pedida ?? 0),
                    // peso = i.p.peso,
                    // nroitem = i.p.nroitem ?? 0,
                    // id = i.p.id,
                    // porcen_mercaderia = 0,
                    // porcendesc = 0
                })
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
            });
        }





        //[Authorize]
        [HttpPost]
        [QueueFilter(1)] // Limitar a 1 solicitud concurrente
        [Route("grabarNotaRemision/{userConn}/{id}/{usuario}/{desclinea_segun_solicitud}/{codProforma}/{codempresa}/{id_solurg}/{nroid_solurg}")]
        public async Task<object> grabarNotaRemision(string userConn, string id, string usuario, bool desclinea_segun_solicitud, int codProforma, string codempresa, string id_solurg, int nroid_solurg, SaveNRemisionCompleta datosRemision)
        {
            // borrar los items con cantidad cero
            datosRemision.veremision1.RemoveAll(i => i.cantidad <= 0);
            if (datosRemision.veremision1.Count() <= 0)
            {
                return BadRequest(new { resp = "No hay ningun item en su documento." });
            }
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);
            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                using (var dbContexTransaction = _context.Database.BeginTransaction())
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
                        // El front debe llamar a las funciones de totalizar antes de mandar a grabar
                        var doc_grabado = await Grabar_Documento(_context, id, usuario, desclinea_segun_solicitud, codProforma, codempresa, datosRemision);
                        // si doc_grabado.mostrarModificarPlanCuotas  == true    Marvin debe desplegar ventana para modificar plan de cuotas, es ventana nueva aparte
                        if (doc_grabado.resp != "ok")
                        {
                            return BadRequest(new {resp = doc_grabado.resp, msgAlert = "No se pudo Grabar la Nota de Remision con Exito." });
                        }
                        await log.RegistrarEvento(_context, usuario, Log.Entidades.Nota_Remision, doc_grabado.codNRemision.ToString(), datosRemision.veremision.id, doc_grabado.numeroId.ToString(), "veremisionController", "Grabar", Log.TipoLog.Creacion);
                        // devolver
                        string msgAlertGrabado = "Se grabo la Nota de Remision " + datosRemision.veremision.id + "-" + doc_grabado.numeroId + " con Exito.";

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
                                insolUrg.fnumeroid = doc_grabado.numeroId;
                                await _context.SaveChangesAsync();
                                msgSolUrg = "La nota de remision fue enlazada con la solicitud urgente: " + id_solurg + "-" + nroid_solurg;
                            }
                            catch (Exception)
                            {
                                msgSolUrg = "La nota de remision no pudo ser enlazada a la solicitud Urgente, contacte con el administrador de sistemas.";
                                //throw;
                            }
                            
                        }

                        /*
                        If tipopago.SelectedIndex = 1 Then
                            '##### CONTABILIZAR
                            If sia_funciones.Seguridad.Instancia.rol_contabiliza(sia_funciones.Seguridad.Instancia.usuario_rol(sia_compartidos.temporales.Instancia.usuario)) Then
                                If sia_funciones.Configuracion.Instancia.emp_preg_cont_ventascredito(sia_compartidos.temporales.Instancia.codempresa) Then
                                    If MessageBox.Show("Desea contabilizar este documento ?", "Confirmacion", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) = Windows.Forms.DialogResult.Yes Then
                                        Dim frm As New sia_compartidos.prgDatosContabilizar
                                        frm.ShowDialog()
                                        If frm.eligio Then
                                            If frm.nuevo Then
                                                sia_funciones.Contabilidad.Instancia.Contabilizar_Venta_a_Credito(sia_compartidos.temporales.Instancia.codempresa, sia_funciones.Empresa.Instancia.monedabase(sia_compartidos.temporales.Instancia.codempresa), sia_funciones.Documento.Instancia.Codigo_Remision(id.Text, CInt(numeroid.Text)), frm.id_elegido, frm.tipo_elegido, 0, True, False) ', True, True, False, True)
                                            Else
                                                sia_funciones.Contabilidad.Instancia.Contabilizar_Venta_a_Credito(sia_compartidos.temporales.Instancia.codempresa, sia_funciones.Empresa.Instancia.monedabase(sia_compartidos.temporales.Instancia.codempresa), sia_funciones.Documento.Instancia.Codigo_Remision(id.Text, CInt(numeroid.Text)), "", "", frm.codigo_elegido, True, False) ', True, True, False, True)
                                            End If
                                        Else
                                        End If
                                        frm.Dispose()
                                    End If
                                End If
                            End If
                            '##### FIN CONTABILIZAR
                        End If



                        limpiardoc()
                        mostrardatos("0")
                        leerparametros()
                        ponerpordefecto()
                        id.Focus()

                         */


                        dbContexTransaction.Commit();
                        return Ok(new 
                        { 
                            resp = msgAlertGrabado, 
                            codNotRemision = doc_grabado.codNRemision, 
                            nroIdRemision = doc_grabado.numeroId,
                            mostrarVentanaModifPlanCuotas = doc_grabado.mostrarModificarPlanCuotas,
                            planCuotas = doc_grabado.plandeCuotas,
                            msgSolUrg = msgSolUrg
                        });
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







        private async Task<(string resp, int codNRemision, int numeroId, bool mostrarModificarPlanCuotas, List<object>? plandeCuotas)> Grabar_Documento(DBContext _context, string id, string usuario, bool desclinea_segun_solicitud, int codProforma, string codempresa, SaveNRemisionCompleta datosRemision)
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
            bool descarga = await ventas.iddescarga(_context, id);


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
                catch (Exception)
                {
                    return ("Error al transferir la proforma, por favor consulte con el administrador del sistema.", 0, 0, false, null);
                }
            }

            try
            {
                // grabar descto si hay descuentos
                if (vedesextraremi.Count() > 0)
                {
                    await grabardesextra(_context, codNRemision, vedesextraremi);
                }
            }
            catch (Exception)
            {
                return ("Error al Guardar descuentos extras de Nota de Remision, por favor consulte con el administrador del sistema.", 0, 0, false, null);
            }
            try
            {
                // grabar recargo si hay recargos
                if (verecargoremi.Count > 0)
                {
                    await grabarrecargo(_context, codNRemision, verecargoremi);
                }
            }
            catch (Exception)
            {
                return ("Error al Guardar recargos de Nota de Remision, por favor consulte con el administrador del sistema.", 0, 0, false, null);
            }
            try
            {
                // grabar iva
                if (veremision_iva.Count > 0)
                {
                    await grabariva(_context, codNRemision, veremision_iva);
                }
            }
            catch (Exception)
            {
                return ("Error al Guardar iva de Nota de Remision, por favor consulte con el administrador del sistema.", 0, 0, false, null);
            }


            // actualizar stock actual si es que descarga mercaderia
            try
            {
                if (descarga)
                {
                    // Desde 15/11/2023 registrar en el log si por alguna razon no actualiza en instoactual correctamente al disminuir el saldo de cantidad y la reserva en proforma
                    if (await saldos.Veremision_ActualizarSaldo(_context,usuario,codNRemision, Saldos.ModoActualizacion.Crear)==false)
                    {
                        await log.RegistrarEvento(_context, usuario, Log.Entidades.Nota_Remision, codNRemision.ToString(), veremision.id, veremision.numeroid.ToString(), "veremisionController", "No actualizo stock al restar cantidad en NR.", Log.TipoLog.Creacion);
                    }
                    else
                    {
                        if (await ventas.revertirstocksproforma(_context,codProforma,codempresa) == false)
                        {
                            await log.RegistrarEvento(_context, usuario, Log.Entidades.Nota_Remision, codNRemision.ToString(), veremision.id, veremision.numeroid.ToString(), "veremisionController", "No actualizo stock al restar reserva en PF.", Log.TipoLog.Creacion);
                        }
                    }
                }
            }
            catch (Exception)
            {

                throw;
            }


            // ####################################
            // generar plan de pagos si es que es a credito
            // #################################
            List<object> planCuotasGenerada = new List<object>();
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
                            planCuotasGenerada = (List<object>)await verPlandePago(_context, codNRemision, veremision.usuarioreg);
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
                                planCuotasGenerada = (List<object>)await verPlandePago(_context, codNRemision, veremision.usuarioreg);
                            }
                        }
                        else
                        {
                            var lista = await ventas.lista_PFNR_complementarias_noPP(_context, codProforma);
                            if (await ventas.generarcuotaspago(_context, codNRemision, 4, await ventas.MontoTotalComplementarias(_context,lista), (double)veremision.total, veremision.codmoneda, veremision.codcliente, await ventas.FechaComplementariaDeMayorMonto(_context,lista), false, codempresa))
                            {
                                // modificarplandepago(codigo.Text, codcliente.Text, codmoneda.Text, total.Text)
                                mostrarModificarPlanCuotas = true;
                                planCuotasGenerada = (List<object>)await verPlandePago(_context, codNRemision, veremision.usuarioreg);
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
                                        if (await ventas.revertirpagos(_context, reg.codremision,4))
                                        {
                                            await ventas.generarcuotaspago(_context, reg.codremision, 4, await ventas.MontoTotalComplementarias(_context, lista), await ventas.TotalNRconNC_ND(_context, reg.codremision), veremision.codmoneda, veremision.codcliente, await ventas.FechaComplementariaDeMayorMonto(_context, lista), false, codempresa);
                                        }
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
                            planCuotasGenerada = (List<object>)await verPlandePago(_context, codNRemision, veremision.usuarioreg);
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
                                planCuotasGenerada = (List<object>)await verPlandePago(_context, codNRemision, veremision.usuarioreg);
                            }

                        }
                        else
                        {
                            var lista = await ventas.lista_PFNR_complementarias_noPP(_context, codProforma);
                            if (await ventas.generarcuotaspago(_context, codNRemision, 4, await ventas.MontoTotalComplementarias(_context, lista), (double)veremision.total, veremision.codmoneda, veremision.codcliente, await ventas.FechaComplementariaDeMayorMonto(_context, lista), false, codempresa))
                            {
                                // modificarplandepago(codigo.Text, codcliente.Text, codmoneda.Text, total.Text)
                                mostrarModificarPlanCuotas = true;
                                planCuotasGenerada = (List<object>)await verPlandePago(_context, codNRemision, veremision.usuarioreg);
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
                                        if (await ventas.revertirpagos(_context, reg.codremision, 4))
                                        {
                                            await ventas.generarcuotaspago(_context, reg.codremision, 4, await ventas.MontoTotalComplementarias(_context, lista), await ventas.TotalNRconNC_ND(_context, reg.codremision), veremision.codmoneda, veremision.codcliente, await ventas.FechaComplementariaDeMayorMonto(_context, lista), false, codempresa);
                                        }
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
        private async Task<object> verPlandePago(DBContext _context, int codRemision, string usuario)
        {
            // log de ingreso
            // prgmodifplanpago_Load
            var planPagos = await _context.coplancuotas.Where(i => i.coddocumento == codRemision && i.codtipodoc==4)
                .Select(i => new
                {
                    i.nrocuota,
                    i.vencimiento,
                    i.monto,
                    i.moneda
                })
                .ToListAsync();

            return planPagos;
        }

        [HttpGet]
        [Route("impresionNotaRemision/{userConn}/{codClienteReal}/{codEmpresa}/{codclientedescripcion}/{preparacion}/{codigoNR}")]
        public async Task<ActionResult<List<object>>> impresionNotaRemision(string userConn, string codClienteReal, string codEmpresa, string codclientedescripcion, string preparacion, int codigoNR)
        {
            // lista de impresoras disponibles, aca deben ir de momento las impresoras matriciales de notas de remision, nombre que tienen.
            var impresorasDisponibles = new Dictionary<int, string>
            {
                { 311, "EPSON LX-350" },  
                { 411, "EPSON LX-350" },
                { 811, "EPSON LX-350" }
            };
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
                        config.PrinterName = impresorasDisponibles[veremision.codalmacen];

                        // Comprobar si la impresora está instalada
                        if (config.IsValid)
                        {
                            // generamos el archivo .txt y regresamos la ruta
                            string pathFile = await mostrardocumento_directo(_context, codClienteReal, codEmpresa, codclientedescripcion, preparacion, veremision);
                            // Configurar e iniciar el trabajo de impresión
                            // Aquí iría el código para configurar el documento a imprimir y lanzar la impresión
                            bool impremiendo = await RawPrinterHelper.SendFileToPrinterAsync(config.PrinterName, pathFile);
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
                                return Ok(new { resp = "Imprimiendo Documento ...." });
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
            catch (Exception)
            {
                return Problem("Error en el servidor");
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
                imp_ptoventa = await ventas.ptoventacliente_direccion(_context, codClienteReal, _direcc);

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
                    }).ToListAsync();

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
}
