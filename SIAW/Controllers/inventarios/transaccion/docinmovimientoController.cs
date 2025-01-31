using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using siaw_DBContext.Data;
using siaw_DBContext.Models;
using Microsoft.AspNetCore.Authorization;
using siaw_funciones;
using siaw_DBContext.Models_Extra;
using System.Runtime.Intrinsics.X86;
using Microsoft.EntityFrameworkCore.Storage;
using System.Globalization;
using Polly;
using LibSIAVB;
using System.Data;
using Polly.Caching;
using SIAW.Controllers.ventas.transaccion;

namespace SIAW.Controllers.inventarios.transaccion
{
    [Route("api/inventario/transac/[controller]")]
    [ApiController]
    public class docinmovimientoController : ControllerBase
    {
        private readonly Dui dui = new Dui();
        private readonly Items item = new Items();
        private readonly Inventario inventario = new Inventario();
        private readonly Saldos saldos = new Saldos();
        private readonly Seguridad seguridad = new Seguridad();
        private readonly Almacen almacen = new Almacen();
        private readonly Ventas ventas = new Ventas();
        private readonly Restricciones restricciones = new Restricciones();
        private readonly Documento documento = new Documento();
        private readonly Funciones funciones = new Funciones();
        private readonly Empresa empresa = new Empresa();
        private readonly HardCoded hardCoded = new HardCoded();
        private readonly Configuracion configuracion = new Configuracion();
        private readonly Nombres nombres = new Nombres();
        private readonly Log log = new Log();
        private readonly Empresa empresa1 = new Empresa();
        private readonly Cliente cliente = new Cliente();

        private readonly func_encriptado encripVB = new func_encriptado();

        private readonly string _controllerName = "docinmovimientoController";

        private readonly UserConnectionManager _userConnectionManager;
        public docinmovimientoController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }

        [HttpGet]
        [Route("getParametrosInicialesNM/{userConn}/{usuario}/{codempresa}")]
        public async Task<ActionResult<object>> getParametrosInicialesNM(string userConn, string usuario, string codempresa)
        {
            try
            {
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);
                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    int codvendedor = await configuracion.usr_codvendedor(_context, usuario);
                    int codalmacen = await configuracion.usr_codalmacen(_context, usuario);
                    string codalmacendescripcion = await nombres.nombrealmacen(_context, codalmacen);
                    bool chkdesglozar_cjtos = false;
                    string id = await configuracion.usr_idmovimiento(_context, usuario);
                    int numeroid = await documento.movimientonumeroid(_context,id) + 1;
                    bool cargar_proforma = false;
                    bool cvenumeracion1 = false;
                    var obtener_cantidades_aprobadas_de_proformas = await saldos.Obtener_Cantidades_Aprobadas_De_Proformas(_context, codempresa);
                    bool es_ag_local=  false;
                    bool es_tienda = false;
                    bool ver_ch_es_para_invntario = await configuracion.usr_ver_check_es_para_inventario(_context, usuario);

                    var dataPorConcepto = await actualizarconcepto(_context, "limpiar", 0, codalmacen);

                    return Ok(new
                    {
                        codvendedor,
                        codalmacen,
                        codalmacendescripcion,
                        chkdesglozar_cjtos,
                        id,
                        numeroid,
                        cargar_proforma,
                        cvenumeracion1,
                        obtener_cantidades_aprobadas_de_proformas,
                        es_ag_local,
                        es_tienda,
                        ver_ch_es_para_invntario,
                        // para habilitar o deshabilitar inputs y botones
                        dataPorConcepto
                    });
                }
            }
            catch (Exception ex)
            {
                return Problem("Error en el servidor al obtener parametros iniciales NM: " + ex.Message);
                throw;
            }
        }


        [HttpGet]
        [Route("permGrabarAntInventario/{userConn}/{codalmacen}/{fecha}/{id}/{numeroid}")]
        public async Task<object> permModifNMAntInventario(string userConn, int codalmacen, DateTime fecha, string id, int numeroid)
        {
            try
            {
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);
                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (await restricciones.ValidarModifDocAntesInventario(_context, codalmacen, fecha.Date) == false)
                    {
                        return StatusCode(203, new
                        {
                            valido = false,
                            resp = "No puede modificar datos anteriores al ultimo inventario, Para eso necesita una autorizacion especial.",
                            servicio = 48,
                            descServicio = "MODIFICACION ANTERIOR A INVENTARIO",
                            datosDoc = id + "-" + numeroid + ": " + id + "-" + numeroid,
                            datoA = id,
                            datoB = numeroid
                        });
                    }
                    return Ok(new { valido = true });
                }
            }
            catch (Exception ex)
            {
                return Problem("Error en el servidor al verificar si NM es antes de Inv.: " + ex.Message);
                throw;
            }
        }

        [HttpPost]
        [Route("grabarDocumento/{userConn}/{codempresa}/{traspaso}")]
        public async Task<ActionResult<object>> grabarDocumento(string userConn, string codempresa, bool traspaso, requestGabrar dataGrabar)
        {
            try
            {
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);
                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    List<tablaDetalleNM> tablaDetalle = dataGrabar.tablaDetalle;
                    // calculartotal.PerformClick()
                    // borrar items con cantidad 0 o menor
                    tablaDetalle = tablaDetalle.Where(i => i.cantidad > 0).ToList();
                    /*
                    if (await restricciones.ValidarModifDocAntesInventario(_context, dataGrabar.cabecera.codalmacen, dataGrabar.cabecera.fecha) == false)
                    {
                        if (tienePermisoModifAntUltInv == false)
                        {
                            return StatusCode(203, new
                            {
                                valido = false,
                                resp = "No puede modificar datos anteriores al ultimo inventario, Para eso necesita una autorizacion especial.",
                                servicio = 48,
                                descServicio = "MODIFICACION ANTERIOR A INVENTARIO",
                                datosDoc = dataGrabar.cabecera.id + "-" + dataGrabar.cabecera.numeroid + ": " + dataGrabar.cabecera.id + "-" + dataGrabar.cabecera.numeroid,
                                datoA = dataGrabar.cabecera.id,
                                datoB = dataGrabar.cabecera.numeroid
                            });
                        }
                        
                    }
                    */
                    var guardarDoc = await guardarNuevoDocumento(_context, codempresa, traspaso, dataGrabar.cabecera, tablaDetalle);
                    if (guardarDoc.valido == false)
                    {
                        if (guardarDoc.dtnegativos != null)
                        {
                            return StatusCode(203, new
                            {
                                resp = guardarDoc.msg,
                                negativos = guardarDoc.dtnegativos,
                                valido = guardarDoc.valido
                            });
                        }
                        return BadRequest(new
                        {
                            resp = guardarDoc.msg,
                            negativos = guardarDoc.dtnegativos,
                            valido = guardarDoc.valido
                        });
                    }
                    string mensajeConfirmacion = "Se genero la nota de movimiento : " + dataGrabar.cabecera.id + " - " + guardarDoc.numeroID + ". Desea Exportar el documento? ";
                    await log.RegistrarEvento(_context, dataGrabar.cabecera.usuarioreg, Log.Entidades.SW_Nota_Movimiento, guardarDoc.codigoNM.ToString(), dataGrabar.cabecera.id, guardarDoc.numeroID.ToString(), _controllerName, "Grabar", Log.TipoLog.Creacion);

                    List<string> alertas = new List<string>();

                    // Desde 19-06-2023 Al grabar obtener si la proforma del origen de la tienda es una solicitud urgente
                    // si es asi debe enlazar la nota de movimiento en la solicitud urgente
                    if (dataGrabar.cabecera.idproforma.Length > 0 && (dataGrabar.cabecera.numeroidproforma != null || dataGrabar.cabecera.numeroidproforma > 0))
                    {
                        // verificar si la proforma esta vinculada a una solicitud urgente
                        var doc_solurgente = await ventas.Solicitud_Urgente_IdNroid_de_Proforma(_context, dataGrabar.cabecera.idproforma, (int)dataGrabar.cabecera.numeroidproforma);
                        if (doc_solurgente.id.Trim() != "")
                        {
                            alertas.Add("La proforma es una solicitud urgente!!!");
                            var insolUrgente = await _context.insolurgente.Where(i => i.id == doc_solurgente.id && i.numeroid == doc_solurgente.nroId).FirstOrDefaultAsync();
                            insolUrgente.idnm = dataGrabar.cabecera.id;
                            insolUrgente.numeroidnm = dataGrabar.cabecera.numeroid;
                            var cambios = await _context.SaveChangesAsync();
                            if (cambios>0)
                            {
                                await log.RegistrarEvento(_context, dataGrabar.cabecera.usuarioreg, Log.Entidades.SW_Nota_Movimiento, guardarDoc.codigoNM.ToString(), dataGrabar.cabecera.id, guardarDoc.numeroID.ToString(), _controllerName, "Grabar enlace NM: " + dataGrabar.cabecera.id + "-" + guardarDoc.numeroID.ToString() + " con SU: " + doc_solurgente.id + "-" + doc_solurgente.nroId, Log.TipoLog.Creacion);
                                alertas.Add("La nota de movimiento fue enlazada con la solicitud urgente: " + doc_solurgente.id + "-" + doc_solurgente.nroId);
                            }
                        }
                    }

                    // registrar la nota en despachos si es que es un concepto habilitado para registrar en despachos
                    List<string>mensajesDesp = await Registrar_Nota_En_Despachos(_context,dataGrabar.cabecera.codconcepto, dataGrabar.cabecera.id, dataGrabar.cabecera.numeroid, dataGrabar.cabecera.usuarioreg);
                    alertas.AddRange(mensajesDesp);

                    return Ok(new { resp = mensajeConfirmacion, alertas = alertas, guardarDoc.codigoNM });
                }
            }
            catch (Exception ex)
            {
                return Problem("Error en el servidor al guardar Documento: " + ex.Message);
                throw;
            }
        }

        private async Task<List<string>> Registrar_Nota_En_Despachos(DBContext _context, int codconcepto, string id, int numeroid, string usuario)
        {
            List<string> alertas = new List<string>();
            if (await inventario.concepto_espara_despacho(_context,codconcepto))
            {
                // alerta insertar en despachos
                alertas.Add("Se grabara la nota de movimiento : " + id + " - " + numeroid + ". en los despachos. ");
                // intentar añadir
                if (await inventario.Existe_Nota_De_Movimiento(_context, id,numeroid))
                {
                    if (await nota_en_despachos(_context,id,numeroid))
                    {
                        alertas.Add("La nota de movimiento ya esta en despachos, verifique esta situacion !!!");
                    }
                    else
                    {
                        var resultAddNE = await anadir_nota_entrega(_context, id, numeroid, usuario);
                        if (resultAddNE.result)
                        {
                            alertas.Add("La nota de movimiento fue añadida a la lista de preparados, verifique por favor.");
                        }
                        else
                        {
                            alertas.Add(resultAddNE.msg);
                            alertas.Add("Ocurrio un error y la nota de movimiento no se añadio a la lista para despachar. Consulte con el administrador de sistemas.");
                        }
                    }
                }
                else
                {
                    alertas.Add("La nota de movimiento no existe, o no es una nota de entrega por reposicion de stock o nota de entrega por solicitud urgente.");
                }
            }
            return alertas;
        }

        private async Task<(bool result, string msg)> anadir_nota_entrega(DBContext _context, string idnm, int nroidnmov, string usuario)
        {
            // añade una nota de movimiento especifica
            var tbl_aux = await _context.inmovimiento
                .Where(p1 => p1.anulada == false && p1.id == idnm && p1.numeroid == nroidnmov)
                .Select(p1 => new addNMDespachos
                {
                    codmovimiento = p1.codigo,
                    codigo = 0,
                    preparacion = "NORMAL",
                    tipoentrega = "NORMAL",
                    codalmacen = p1.codalmacen,
                    codvendedor = p1.codvendedor ?? 0,
                    doc = p1.id + "-" + p1.numeroid.ToString(),
                    fecha = p1.fecha,
                    id = p1.id,
                    numeroid = p1.numeroid,
                    Codcliente = p1.codalmdestino ??0,
                    nomcliente = "",
                    odc = "",
                    frecibido = p1.fecha,
                    hrecibido = p1.horareg,
                    hojas = 0.00,
                    estado = "DESPACHAR",
                    preparapor = 100,
                    nombpersona = "",
                    peso = p1.peso ?? 0,
                    total = "0",
                    codmoneda = "",
                    nroitems = 0,
                    bolsas = 0.00,
                    cajas = 0.0,
                    amarres = 0.0,
                    bultos = 0.0,
                    resdespacho = 0,
                    fterminado = p1.fecha,
                    hterminado = p1.horareg,
                    guia = "",
                    nombtrans = "",
                    tipotrans = "",
                    fdespacho = new DateTime(2000-01-01),
                    nombchofer = "",
                    celchofer = "",
                    nroplaca = "",
                    monto_flete = "0.0",
                    hdespacho = "12:00",
                    tarribo = "",
                    obs = ""
                }).ToListAsync();

            foreach (var reg in tbl_aux)
            {
                if (await nota_en_despachos(_context, reg.id, reg.numeroid) == false)
                {
                    reg.nroitems = await nroitems_nota_mov(_context, reg.id, reg.numeroid);
                    reg.hojas = 0;
                    reg.nomcliente = await nombres.nombrealmacen(_context, reg.Codcliente);
                }
            }
            using (var dbContexTransaction = _context.Database.BeginTransaction())
            {
                foreach (var reg in tbl_aux)
                {
                    // se insertan las nuevas proformas
                    // pero antes de insertar verifica si la proforma ya existe
                    try
                    {
                        if (await nota_en_despachos(_context, reg.id, reg.numeroid) == false)
                        {
                            reg.estado = "PREPARADO";
                            vedespacho newReg = new vedespacho();
                            newReg.frefacturacion = new DateTime(1900-01-01).Date;
                            newReg.hrefacturacion = "00:00";
                            newReg.fanulado = new DateTime(1900 - 01 - 01).Date;
                            newReg.hanulado = "00:00";
                            newReg.codproforma = reg.codmovimiento;

                            newReg.id = reg.id;
                            newReg.nroid = reg.numeroid;
                            newReg.fecha = reg.fecha.Date;
                            newReg.tipoentrega = reg.tipoentrega;
                            newReg.preparacion = reg.preparacion;

                            newReg.codcliente = reg.Codcliente.ToString();
                            newReg.nomcliente = reg.nomcliente;
                            newReg.total = decimal.Parse(reg.total);
                            newReg.codmoneda = reg.codmoneda;
                            newReg.codvendedor = reg.codvendedor;

                            newReg.codalmacen = reg.codalmacen;
                            newReg.nombchofer = reg.nombchofer;
                            newReg.celchofer = reg.celchofer;
                            newReg.nroplaca = reg.nroplaca;
                            newReg.monto_flete = decimal.Parse(reg.monto_flete);

                            newReg.frecibido = await funciones.FechaDelServidor(_context);
                            newReg.hrecibido = await funciones.hora_del_servidor_cadena(_context);
                            newReg.hojas = (decimal)reg.hojas;
                            newReg.estado = reg.estado;
                            newReg.preparapor = reg.preparapor;

                            newReg.peso = reg.peso;
                            newReg.bolsas = (decimal?)reg.bolsas;
                            newReg.cajas = (decimal?)reg.cajas;
                            newReg.amarres = (decimal?)reg.amarres;
                            newReg.bultos = (decimal?)reg.bultos;

                            newReg.resdespacho = reg.resdespacho;
                            newReg.fterminado = reg.fterminado.Date;
                            newReg.hterminado = reg.hterminado;
                            newReg.guia = reg.guia;
                            newReg.nombtrans = reg.nombtrans;

                            newReg.tipotrans = reg.tipotrans;
                            newReg.fdespacho = reg.fdespacho.Date;
                            newReg.hdespacho = reg.hdespacho;
                            newReg.tarribo = reg.tarribo;
                            newReg.obs = reg.obs;

                            newReg.horareg = await funciones.hora_del_servidor_cadena(_context);
                            newReg.fechareg = await funciones.FechaDelServidor(_context);
                            newReg.usuarioreg = usuario;
                            newReg.nroitems = reg.nroitems;
                            try
                            {
                                _context.vedespacho.Add(newReg);
                                await _context.SaveChangesAsync();
                            }
                            catch (Exception ex)
                            {
                                return (false, "Ha ocurrido un error al insertar la nota de movimiento: " + reg.id + "-" + reg.numeroid + ex.Message);
                            }


                            // actualizar log de estado de pedidos
                            // inserta en esta tabla cada cambio de estado
                            try
                            {
                                velog_estado_pedido newLogEstPed = await cadena_insertar_log_estado_pedido(_context, reg.id, reg.numeroid, reg.estado, usuario);
                                _context.velog_estado_pedido.Add(newLogEstPed);
                                await _context.SaveChangesAsync();
                            }
                            catch (Exception ex)
                            {
                                return (false, "Ha ocurrido un error al actualizar log_estado_pedidos: " + reg.id + "-" + reg.numeroid + ex.Message);
                            }

                            dbContexTransaction.Commit();
                        }

                        

                    }
                    catch (Exception ex)
                    {
                        dbContexTransaction.Rollback();
                        return (false, "No se pudo obtener los datos de la proforma. " + ex.Message);
                    }
                }
            }
            return (true, "");    
        }

        private async Task<velog_estado_pedido> cadena_insertar_log_estado_pedido(DBContext _context, string idprof, int nroidprof, string estado, string usuario)
        {
            velog_estado_pedido newLogEstPed = new velog_estado_pedido();
            newLogEstPed.idproforma = idprof;
            newLogEstPed.nroidproforma = nroidprof;
            newLogEstPed.estado = estado;
            newLogEstPed.horareg = await funciones.hora_del_servidor_cadena(_context);
            newLogEstPed.fechareg = await funciones.FechaDelServidor(_context);
            newLogEstPed.usuarioreg = usuario;
            return newLogEstPed;
        }

        private async Task<int> nroitems_nota_mov(DBContext _context, string id, int nroid)
        {
            try
            {
                // determina el nro de items en el detalle de una proforma
                var nroitems = await _context.inmovimiento
                    .Where(p1 => p1.id == "id" && p1.numeroid == nroid)
                    .Join(_context.inmovimiento1,
                          p1 => p1.codigo,
                          p2 => p2.codmovimiento,
                          (p1, p2) => p2.coditem)
                    .CountAsync();
                return nroitems;

            }
            catch (Exception)
            {
                return 0;
            }
        }

        private async Task<bool> nota_en_despachos(DBContext _context, string idnm, int nroidnmov)
        {
            var consulta = await _context.vedespacho.Where(i => i.id == idnm && i.nroid == nroidnmov).CountAsync();
            bool resultado = true;
            if (consulta > 0) {
                resultado = true;
            }
            else
            {
                resultado = false;
            }
            return resultado;
        }



        private async Task<(bool valido, string msg, List<Dtnegativos>? dtnegativos, int codigoNM, int numeroID)> guardarNuevoDocumento(DBContext _context, string codempresa, bool traspaso, inmovimiento inmovimiento, List<tablaDetalleNM> tablaDetalle)
        {
            int factor = inmovimiento.factor;
            // preparacion de datos 
            inmovimiento.fecha = inmovimiento.fecha.Date;
            inmovimiento.factor = (short)factor;
            var fechaPrueba = await funciones.FechaDelServidor(_context);
            inmovimiento.horareg = await funciones.hora_del_servidor_cadena(_context);
            inmovimiento.fechareg = await funciones.FechaDelServidor(_context);
            inmovimiento.anulada = false;
            inmovimiento.contabilizada = false;
            inmovimiento.peso = 0;
            inmovimiento.fecha_inicial = await funciones.FechaDelServidor(_context);



            var validaDetalle = await validarDetalle(_context, factor, codempresa, inmovimiento, tablaDetalle);
            if (validaDetalle.valido)
            {
                var validaCabecera = await validardatos(_context, factor, traspaso, inmovimiento);
                if (validaCabecera.valido)
                {
                    // prepara detalle Nota Movimiento
                    List<inmovimiento1> inmovimiento1 = tablaDetalle.Select(i => new inmovimiento1
                    {
                        codmovimiento = 0,
                        coditem = i.coditem,
                        cantidad = i.cantidad,
                        udm = i.udm,
                        codaduana = i.codaduana,
                    }).ToList();
                    // coloca pesos
                    foreach (var reg in inmovimiento1)
                    {
                        var pesoItem = await _context.initem.Where(i => i.codigo == reg.coditem).Select(i => i.peso).FirstOrDefaultAsync() ?? 0;
                        reg.peso = pesoItem * reg.cantidad;
                    }
                    // coloca peso general
                    inmovimiento.peso = inmovimiento1.Sum(i => i.peso);
                    int codNotaMovimiento = 0;
                    using (var dbContexTransaction = _context.Database.BeginTransaction())
                    {
                        try
                        {
                            inmovimiento.numeroid = await documento.movimientonumeroid(_context, inmovimiento.id) + 1;
                            if (inmovimiento.numeroid <= 0)
                            {
                                return (false, "Error al generar numero ID, consulte con el Administrador", null,0,0);
                            }
                            if (await documento.existe_movimiento(_context,inmovimiento.id, inmovimiento.numeroid))
                            {
                                return (false, "Ese numero de documento, ya existe, por favor consulte con el administrador del sistema.", null,0,0);
                            }

                            // agregar cabecera
                            try
                            {
                                _context.inmovimiento.Add(inmovimiento);
                                await _context.SaveChangesAsync();
                            }
                            catch (Exception ex)
                            {
                                return (false, "Error al grabar la cabecera de la nota de movimiento: " + ex.Message, null,0,0);
                            }
                            codNotaMovimiento = inmovimiento.codigo;





                            // actualiza numero id
                            var numeracion = _context.intipomovimiento.FirstOrDefault(n => n.id == inmovimiento.id);
                            numeracion.nroactual += 1;
                            await _context.SaveChangesAsync(); // Guarda los cambios en la base de datos


                            int validaCantProf = await _context.inmovimiento.Where(i => i.id == inmovimiento.id && i.numeroid == inmovimiento.numeroid).CountAsync();
                            if (validaCantProf > 1)
                            {
                                return (false, "Se detecto más de un número del mismo documento, por favor consulte con el administrador del sistema.", null,0,0);
                            }

                            // guarda detalle (veproforma1)
                            // actualizar codigoNM para agregar
                            inmovimiento1 = inmovimiento1.Select(p => { p.codmovimiento = codNotaMovimiento; return p; }).ToList();
                            // guardar detalle
                            _context.inmovimiento1.AddRange(inmovimiento1);
                            await _context.SaveChangesAsync();
                            dbContexTransaction.Commit();
                        }
                        catch (Exception ex)
                        {
                            dbContexTransaction.Rollback();
                            return (false,$"Error en el servidor al guardar Proforma: {ex.Message}", null,0,0);
                            throw;
                        }

                    }

                    /*
                          DESPUES DE GUARDAR DETALLE CABECERA Y NUMERO DE ID
                        '//#######################################
                        '//##RUTINA QUE ACTUALIZA EL SALDO ACTUAL
                        'sia_funciones.Saldos.Instancia.Inmovimiento_ActualizarSaldo(codigo.Text, sia_funciones.Saldos.modo_actualizacion.crear, tabladetalle)
                        ''Desde 23/11/2023 verificar si la actualizacion de saldo es true oi false para registrar un registro de que no se pudo actualizar
                         */

                    // ACTUALIZAR POR PROCEDIMIENTO ALMACENADO

                    if (await saldos.Inmovimiento_ActualizarSaldo(_context,codNotaMovimiento,Saldos.ModoActualizacion.Crear) == false)
                    {
                        await log.RegistrarEvento(_context, inmovimiento.usuarioreg, Log.Entidades.SW_Nota_Movimiento, codNotaMovimiento.ToString(), inmovimiento.id, inmovimiento.numeroid.ToString(), _controllerName, "No actualizo stock en cantidad de algun item en NM.", Log.TipoLog.Creacion);
                    }
                    // #######################################

                    await inventario.actaduanamovimiento(_context, codNotaMovimiento, "crear", inmovimiento.fecha);

                    // Pasar a transferido proforma de solicitud urgente
                    // actualizar proforma a transferida
                    if (inmovimiento.idproforma_sol != "" && inmovimiento.numeroidproforma_sol != null && inmovimiento.numeroidproforma_sol != 0)
                    {
                        if (await ventas.proforma_es_sol_urgente(_context, inmovimiento.idproforma_sol, (int)inmovimiento.numeroidproforma_sol))
                        {
                            var datoProf = await _context.veproforma.Where(i => i.id == inmovimiento.idproforma_sol && i.numeroid == inmovimiento.numeroidproforma_sol).FirstOrDefaultAsync();
                            datoProf.transferida = true;
                            await _context.SaveChangesAsync(); // Guarda los cambios en la base de datos
                            await log.RegistrarEvento(_context, inmovimiento.usuarioreg, Log.Entidades.SW_Nota_Movimiento, datoProf.codigo.ToString(), inmovimiento.idproforma_sol, inmovimiento.numeroidproforma_sol.ToString(), _controllerName, "Pasar a transferida Proforma", Log.TipoLog.Creacion);
                            // actualizar revertir stock de proforma
                            try
                            {
                                await ventas.revertirstocksproforma(_context, datoProf.codigo, codempresa);
                            }
                            catch (Exception)
                            {

                            }
                        }
                    }
                    return (true, "", null, codNotaMovimiento, inmovimiento.numeroid);

                }
                return (false, validaCabecera.msg, null,0,0);
            }

            return (false, validaDetalle.msg, validaDetalle.dtnegativos,0,0);
        }



        private async Task<(bool valido, string msg)> validardatos(DBContext _context, int factor, bool traspaso, inmovimiento inmovimiento)
        {
            inmovimiento = quitarEspacios(factor, traspaso, inmovimiento);
            // validar codigo vacio
            if (inmovimiento.codvendedor == null || inmovimiento.codvendedor <= 0)
            {
                return (false, "No puede dejar la casilla de Codigo de vendedor del Documento en blanco.");
            }
            if (inmovimiento.codconcepto == null || inmovimiento.codconcepto <= 0)
            {
                return (false, "No puede dejar la casilla de Concepto del Documento en blanco.");
            }
            if (await inventario.ConceptoEsUsuarioFinal(_context, inmovimiento.codconcepto))
            {
                if (inmovimiento.codpersona == null || inmovimiento.codpersona == 0)
                {
                    return (false, "Si el concepto es usuario final, debe definir la persona responsable o usuario final.");
                }
                if (inmovimiento.codalmdestino == null || inmovimiento.codalmdestino == 0)
                {
                    return (false, "Debe definir el almacen destino el cual es el almacen de la persona responsable o usuario final.");
                }
            }
            if (await inventario.Concepto_Es_Entrega_Cliente(_context, inmovimiento.codconcepto))
            {
                if (string.IsNullOrWhiteSpace(inmovimiento.codcliente))
                {
                    return (false, "Si el concepto es entrega a cliente, debe definir el codigo del cliente al cual se realiza la entrega.");
                }
                if (inmovimiento.codalmdestino == null || inmovimiento.codalmdestino == 0)
                {
                    return (false, "Debe definir el almacen destino el cual es el almacen del cliente.");
                }
            }
            if (string.IsNullOrEmpty(inmovimiento.id))
            {
                return (false, "No puede dejar la casilla de ID de Documento en blanco.");
            }
            if (inmovimiento.numeroid <= 0)
            {
                return (false, "No puede dejar la casilla de Numero de Documento en blanco.");
            }
            if (inmovimiento.codalmacen <= 0)
            {
                return (false, "No puede dejar la casilla de Almacen en blanco.");
            }
            if (inmovimiento.codalmorigen == null || inmovimiento.codalmorigen <= 0)
            {
                return (false, "No puede dejar la casilla de Almacen de Origen en blanco.");
            }
            if (inmovimiento.codalmdestino == null || inmovimiento.codalmdestino <= 0)
            {
                return (false, "No puede dejar la casilla de Almacen de Destino en blanco.");
            }
            if (string.IsNullOrWhiteSpace(inmovimiento.obs))
            {
                return (false, "No puede dejar la casilla de Observacion en blanco.");
            }
            if (inmovimiento.obs.Length > 60)
            {
                return (false, "La observacion es demasiado larga por favor reduzcala.");
            }
            if (string.IsNullOrWhiteSpace(inmovimiento.fid))
            {
                return (false, "No puede dejar la casilla de ID de Documento de origen en blanco.");
            }
            if (inmovimiento.fnumeroid == null || inmovimiento.fnumeroid< 0)
            {
                return (false, "No puede dejar la casilla de Numero de Documento de origen  en blanco.");
            }

            if (await inventario.concepto_espara_despacho(_context,inmovimiento.codconcepto) && inmovimiento.codconcepto == 10 && await almacen.Es_Tienda(_context,(int)inmovimiento.codalmdestino))
            {
                if (string.IsNullOrWhiteSpace(inmovimiento.idproforma_sol))
                {
                    return (false, "No puede dejar la casilla de ID de la PF de Almacen en blanco.");
                }
                if (inmovimiento.numeroidproforma_sol == null || inmovimiento.numeroidproforma_sol <= 0)
                {
                    return (false, "No puede dejar la casilla de Numero de la PF de Almacen en blanco o con valor 0.");
                }
                if (string.IsNullOrWhiteSpace(inmovimiento.idproforma))
                {
                    return (false, "No puede dejar la casilla de ID de la PF que hizo la solicitud urgente en blanco.");
                }
                if (inmovimiento.numeroidproforma == null || inmovimiento.numeroidproforma<= 0)
                {
                    return (false, "No puede dejar la casilla de Numero de la PF que hizo la solicitud urgente en blanco o con valor 0.");
                }
                if (await ventas.proforma_es_sol_urgente(_context,inmovimiento.idproforma_sol, inmovimiento.numeroidproforma_sol ?? 0) == false)
                {
                    return (false, "La proforma enlazada es NO es de una solicitud urgente de Tienda,verifique esta situacion.");
                }
            }

            if (await seguridad.periodo_abierto_context(_context,inmovimiento.fecha.Year, inmovimiento.fecha.Month,2) == false)
            {
                return (false, "No puede crear documentos para ese periodo de fechas.");
            }


            // SE NECESITA PERMISO ESPECIAL      // consultar sobre esto quiza no deberia
            // siempre y cuando se haya devuelto false,
            /*
             
            Dim control As New sia_compartidos.prgcontrasena("48", id.Text & "-" & numeroid.Text & ": " & id.Text & "-" & numeroid.Text, id.Text, numeroid.Text)
                
             */
            return (true, "");
        }
        private inmovimiento quitarEspacios(int factor, bool traspaso, inmovimiento inmovimiento)
        {
            inmovimiento.id = inmovimiento.id.Trim();
            inmovimiento.fid = inmovimiento.fid.Trim();
            if (factor == -1)
            {
                if (inmovimiento.fid == "")
                {
                    inmovimiento.fid = "-";
                }
                if (inmovimiento.fnumeroid == null)
                {
                    inmovimiento.fnumeroid = 0;
                }
            }
            if (traspaso == false)
            {
                if (inmovimiento.fid == "")
                {
                    inmovimiento.fid = "-";
                }
                if (inmovimiento.fnumeroid == null)
                {
                    inmovimiento.fnumeroid = 0;
                }
            }
            inmovimiento.obs = inmovimiento.obs.Trim();
            inmovimiento.obs = seguridad.FiltrarCadena(inmovimiento.obs);

            if (inmovimiento.numeroidproforma == null)
            {
                inmovimiento.numeroidproforma = 0;
            }
            if (inmovimiento.numeroidproforma_sol == null)
            {
                inmovimiento.numeroidproforma_sol = 0;
            }
            return inmovimiento;
        }

        private string hay_item_duplicados(List<tablaDetalleNM> tablaDetalle)
        {
            /*
            string cadena_item = "";
            List<string> lista_items = new List<string>();
            foreach (var reg in tablaDetalle)
            {
                if (lista_items.Contains(reg.coditem) == false)
                {
                    lista_items.Add(reg.coditem);
                }
                else
                {
                    if (cadena_item.Trim().Length == 0)
                    {
                        cadena_item = "'" + reg.coditem + "'";
                    }
                    else
                    {
                        cadena_item = ", '" + reg.coditem + "'";
                    }
                }
            }

            */
            var lista_repetidos = tablaDetalle
                .GroupBy(reg => reg.coditem) // Agrupa por coditem
                .Where(grupo => grupo.Count() > 1) // Filtra los grupos con más de un elemento
                .Select(grupo => grupo.Key) // Obtén el coditem repetido
                .ToList(); // Convierte a lista

            var cadena_repetidos = string.Join(", ", lista_repetidos); // Convierte la lista a un string separado por comas

            return cadena_repetidos;
        }

        private async Task<(bool valido, string msg, List<Dtnegativos>? dtnegativos)> validarDetalle(DBContext _context, int factor, string codempresa, inmovimiento inmovimiento, List<tablaDetalleNM> tablaDetalle)
        {
            int indice = 1;

            if (tablaDetalle.Count() == 0)
            {
                return (false, "No existen items en el detalle.", null);
            }
            string item_repetidos = hay_item_duplicados(tablaDetalle);
            if (item_repetidos.Trim().Length > 0)
            {
                return (false, "Los siguientes items estan repetidos: " + item_repetidos, null);
            }


            foreach (var reg in tablaDetalle)
            {
                if (reg.codaduana == null)
                {
                    reg.codaduana = " ";
                }

                if (string.IsNullOrWhiteSpace(reg.coditem))
                {
                    return (false, "No eligio El Item en la Linea: " + indice, null);
                }
                if (await item.iteminactivo(_context,reg.coditem))
                {
                    return (false, "El item de la linea " + indice + " -> " + reg.coditem + " no puede ser movido por que esta inactivo.", null);
                }
                if (await item.item_usar_en_movimiento(_context,reg.coditem) == false)
                {
                    return (false, "El item de la linea " + indice + " -> " + reg.coditem + " no puede usarse en notas de movimiento.", null);
                }
                if (string.IsNullOrWhiteSpace(reg.udm))
                {
                    return (false, "No puso la Unidad de Medida en la Linea " + indice + " .", null);
                }
                if (reg.cantidad == null)
                {
                    return (false, "No puso la cantidad en la Linea " + indice + " .", null);
                }
                if (reg.cantidad <= 0)
                {
                    return (false, "No puso la cantidad en la Linea " + indice + " .", null);
                }
                indice++;
            }

            // validar negativos
            if ((factor == -1) && (await inventario.PermitirNegativos(_context,codempresa) == false))
            {
                List<string> msgs = new List<string>();
                List<string> negs = new List<string>();

                var validaNegativos = await Validar_Saldos_Negativos_Doc(_context, codempresa, inmovimiento, tablaDetalle);
                if (validaNegativos.resultado == false)   ////////////////////////////// VERIFICAR SI DEBE PERMITIR GRABAR CON CLAVE, EN EL SIA LO HACE
                {
                    return(false, validaNegativos.msg, validaNegativos.dtnegativos);
                }
            }

            /*
             
            If resultado Then
                For i = 0 To tabladetalle.Rows.Count - 1
                    tabladetalle.Rows(i)("descripcion") = sia_funciones.Items.Instancia.itemdescripcion(tabladetalle.Rows(i)("coditem"))
                    tabladetalle.Rows(i)("medida") = sia_funciones.Items.Instancia.itemmedida(tabladetalle.Rows(i)("coditem"))
                Next
            End If

             */

            // llama a la funcion que valida si hay cantidades decimales
            respValidaDecimales validaDecimales = validar_cantidades_decimales(tablaDetalle, true);
            if (validaDecimales.cumple == false)
            {
                // VALIDAR CANTIDADES
                // VERIFICAR QUE DEBE PEDIR CONTRASEÑA
                return (false, "No se puede grabar el documento debido a que se encontró cantidades en decimales para items que son en PZ!!!", null);
            }


            return (true,"", null);
        }





        private async Task<(bool resultado, string msg, List<Dtnegativos> dtnegativos)> Validar_Saldos_Negativos_Doc(DBContext _context, string codempresa, inmovimiento inmovimiento ,List<tablaDetalleNM> tablaDetalle)
        {
            bool resultado = true;
            string msg = "";
            List<Dtnegativos> dtnegativos = new List<Dtnegativos>();
            if (tablaDetalle.Count() > 0)
            {
                List<string> msgs = new List<string>();
                List<string> negs = new List<string>();
                
                // Desde 28-10-2020 se debe enviar el dato de id y numeroid de proforma si la nota es para una solicitud urgente de los campos id_proforma_dsd y numeroidproforma_dsd
                if (inmovimiento.numeroidproforma_sol == null)
                {
                    inmovimiento.numeroidproforma_sol = 0;
                }
                // dtnegativos = sia_funciones.Saldos.Instancia.ValidarNegativosDocVenta(tabladetalle, False, Convert.ToInt32(codalmacen.Text), "", 0, msgs, negs, sia_compartidos.temporales.Instancia.codempresa, sia_compartidos.temporales.Instancia.usuario)
                List<itemDataMatriz> detalleItemDataMatriz = tablaDetalle.Select(i => new itemDataMatriz
                {
                    coditem = i.coditem,
                    descripcion = i.descripcion,
                    medida = i.medida,
                    udm = i.udm,
                    cantidad = (double)i.cantidad,
                }).ToList();
                dtnegativos = await saldos.ValidarNegativosDocVenta(_context, detalleItemDataMatriz, inmovimiento.codalmacen, inmovimiento.idproforma_sol, inmovimiento.numeroidproforma_sol ?? 0, msgs, negs, codempresa, inmovimiento.usuarioreg);
                // debemos devolver estos negativos

                foreach (var reg in dtnegativos)
                {
                    if (reg.obs == "Genera Negativo")
                    {
                        if (reg.cantidad_conjunto > 0)
                        {
                            negs.Add(reg.coditem_cjto);
                        }
                        if (reg.cantidad_suelta > 0)
                        {
                            negs.Add(reg.coditem_suelto);
                        }
                    }
                }
                // si ningun item genera negativo recorrer toda la lista 1 poner cumple=1 lo que significa que todo esta bien
                if (negs.Count() > 0)
                {
                    resultado = false;
                    msg = "Los items del documento resaltados de color azul generaran negativos.";
                }
            }
            return (resultado,msg, dtnegativos);
        }




        [HttpGet]
        [Route("eligeConcepto/{userConn}/{codConcepto}/{codalmacen}")]
        public async Task<ActionResult<dataPorConcepto>> eligeConcepto(string userConn, int codConcepto, int codalmacen)
        {
            try
            {
                /*
                 
                codconcepto.Text = sia_compartidos.temporales.Instancia.catalogo1
                codconceptodescripcion.Text = sia_compartidos.temporales.Instancia.catalogo2
                Me.Text = "Nota de Movimiento de Mercaderia - " & codconceptodescripcion.Text

                 */
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);
                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    dataPorConcepto dataPorConcepto = await actualizarconcepto(_context, "limpiar", codConcepto, codalmacen);
                    dataPorConcepto.codclienteReadOnly = true;
                    dataPorConcepto.codpersonadesdeReadOnly = true;

                    /*
                     
                    If sia_funciones.Inventario.Instancia.ConceptoEsUsuarioFinal(codconcepto.Text) Then
                        Me.verificar_concepto_usr_final()
                    Else
                        Me.verificar_concepto_entrega_cliente()
                    End If

                     */

                    if (codConcepto == 10)
                    {
                        dataPorConcepto.cargar_proformaEnabled = false;
                        dataPorConcepto.cvenumeracion1Enabled = false;
                        dataPorConcepto.id_proforma_solReadOnly = false;
                        dataPorConcepto.numeroidproforma_solReadOnly = false;
                    }
                    else
                    {
                        dataPorConcepto.cargar_proformaEnabled = true;
                        dataPorConcepto.cvenumeracion1Enabled = true;
                        dataPorConcepto.id_proforma_solReadOnly = true;
                        dataPorConcepto.numeroidproforma_solReadOnly = true;
                    }

                    dataPorConcepto.conceptoEsAjuste = await inventario.ConceptoEsAjuste(_context, codConcepto);
                    return Ok(dataPorConcepto);
                }
            }
            catch (Exception ex)
            {
                return Problem("Error en el servidor al elegir concepto: " + ex.Message);
                throw;
            }
        }

        private async Task<dataPorConcepto> actualizarconcepto(DBContext _context, string opcion, int codconcepto, int codalmacen)
        {
            bool codalmdestinoReadOnly, codalmorigenReadOnly, traspaso, fidEnable, fnumeroidEnable , codpersonadesdeReadOnly = new bool();
            fidEnable = true;  // se cambio a true ya que en el front Angular asi lo requiere al revez, como si fuera read only.
                                // lo mismo para los botones (variables) que usan Enable. En el SIA esta al revez
            fnumeroidEnable = true;
            traspaso = false;
            codpersonadesdeReadOnly = true;

            int codalmdestinoText = 0, codalmorigenText = 0;
            int factor = 0;
            if (codconcepto == 0)
            {
                codalmdestinoReadOnly = true;
                codalmorigenReadOnly = true;
                codconcepto = 0;
                if (opcion == "limpiar")
                {
                    codalmdestinoText = 0;
                    codalmorigenText = 0;
                }
                factor = 0;
            }
            else
            {
                var datos = await _context.inconcepto.Where(i => i.codigo == codconcepto).FirstOrDefaultAsync();
                if (datos == null)
                {
                    codalmdestinoReadOnly = true;
                    codalmorigenReadOnly = true;
                    codconcepto = 0;
                    if (opcion == "limpiar")
                    {
                        codalmdestinoText = 0;
                        codalmorigenText = 0;
                    }
                    factor = 0;
                }
                else
                {
                    factor = datos.factor;
                    traspaso = datos.traspaso;
                    if (traspaso)
                    {
                        switch (factor)
                        {
                            case 1:
                                codalmorigenReadOnly = false;
                                codalmdestinoReadOnly = true;
                                codalmorigenText = 0;
                                codalmdestinoText = codalmacen;
                                break;
                            case -1:
                                codalmorigenReadOnly = true;
                                codalmdestinoReadOnly = false;
                                codalmdestinoText = 0;
                                codalmorigenText = codalmacen;
                                break;
                            case 0:
                                codalmorigenReadOnly = false;
                                codalmdestinoReadOnly = false;
                                break;
                            default:
                                codalmorigenReadOnly = true;
                                codalmdestinoReadOnly = true;
                                break;
                        }

                    }
                    else
                    {
                        codalmorigenReadOnly = true;
                        codalmdestinoReadOnly = true;
                        fidEnable = true;
                        fnumeroidEnable = true;
                        codalmorigenText = codalmacen;
                        codalmdestinoText = codalmacen;
                        codpersonadesdeReadOnly = true;
                    }
                }
            }
            return new dataPorConcepto
            {
                codalmdestinoReadOnly = codalmdestinoReadOnly,
                codalmorigenReadOnly = codalmorigenReadOnly,
                traspaso = traspaso,
                fidEnable = fidEnable,
                fnumeroidEnable = fnumeroidEnable,
                codpersonadesdeReadOnly = codpersonadesdeReadOnly,
                codalmdestinoText = codalmdestinoText,
                codalmorigenText = codalmorigenText,
                factor = factor
            };
            
        }




        [HttpPost]
        [Route("copiarAduana/{userConn}")]
        public async Task<ActionResult<object>> copiarAduana(string userConn, List<tablaDetalleNM> tablaDetalle)
        {
            try
            {
                if (tablaDetalle.Count() > 0)
                {
                    string codAduana = tablaDetalle[0].codaduana;
                    tablaDetalle.ToList().ForEach(reg => reg.codaduana = codAduana);
                    return Ok(tablaDetalle);
                }
                return BadRequest(new { resp = "No se esta recibiendo nada en el detalle, verifique esta situación" });
            }
            catch (Exception ex)
            {
                return Problem("Error en el servidor al copiar Aduana: " + ex.Message);
                throw;
            }
        }



        [HttpPost]
        [Route("ponerDui/{userConn}/{codalmacen}")]
        public async Task<ActionResult<object>> ponerDui(string userConn, int codalmacen, List<tablaDetalleNM> tablaDetalle)
        {
            try
            {
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);
                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (tablaDetalle.Count() > 0)
                    {
                        foreach (var reg in tablaDetalle)
                        {
                            // reg.codaduana =
                            reg.codaduana = await dui.dui_correspondiente(_context, reg.coditem, codalmacen);
                            //return Ok(resultado);
                        }
                        return Ok(tablaDetalle);
                    }
                    return BadRequest(new { resp = "No se esta recibiendo nada en el detalle, verifique esta situación" });
                }
            }
            catch (Exception ex)
            {
                return Problem("Error en el servidor al colocar DUI en el detalle: " + ex.Message);
                throw;
            }
        }




        [HttpPost]
        [Route("getValidaCantDecimal/{userConn}/{nuevaNM}")]
        public async Task<ActionResult<bool>> getValidaCantDecimal(string userConn, bool nuevaNM, List<tablaDetalleNM> tablaDetalle)
        {
            try
            {
                bool camposInvalidos = tablaDetalle.Any(i =>
                    string.IsNullOrEmpty(i.coditem) ||
                    string.IsNullOrEmpty(i.udm)
                    );
                if (camposInvalidos)
                {
                    return BadRequest(new { resp = "Existen codigos de item o unidades de medida que se estan recibiendo vacio o nulo, consulte con el administrador." });
                }
                respValidaDecimales resultado = validar_cantidades_decimales(tablaDetalle, nuevaNM);
                return Ok(new
                {
                    resultado.cabecera,
                    resultado.detalleObs,
                    resultado.cumple,
                    resp = resultado.alerta
                });
            }
            catch (Exception ex)
            {
                return Problem("Error en el servidor al validar cantidades decimales: " + ex.Message);
                throw;
            }

        }

        private respValidaDecimales validar_cantidades_decimales(List<tablaDetalleNM> tablaDetalle, bool nuevaNM)
        {
            // Crear una instancia de CultureInfo basada en la cultura actual
            CultureInfo culture = (CultureInfo)CultureInfo.CurrentCulture.Clone();

            // Establecer el separador decimal como punto
            culture.NumberFormat.NumberDecimalSeparator = ".";

            string cadena_items_decimales = "";
            List<string> observacionesDecimales = new List<string>();
            // valiar cantidades si hay decimales
            foreach (var reg in tablaDetalle)
            {
                bool esDecimal = false;
                ////////////////////////////
                string valorCadena = "";
                if (nuevaNM)
                {
                    valorCadena = reg.cantidad.ToString(culture);
                }
                else
                {
                    valorCadena = reg.cantidad_revisada.ToString(culture);
                }
                string[] partes = valorCadena.Split('.');

                if (partes.Length > 1)
                {
                    if (partes[1] != null && partes[1] != "0")
                    {
                        esDecimal = true;
                    }
                }

                ////////////////////////////
                if (reg.udm == "PZ")
                {
                    if (esDecimal)
                    {
                        string obs = "Item: " + reg.coditem + " = " + valorCadena + " (PZ)";
                        observacionesDecimales.Add(obs);

                        cadena_items_decimales = cadena_items_decimales + reg.coditem + " | ";
                    }
                }
            }
            string alerta = "";
            string cabecera = "";
            bool cumple = true;
            if (cadena_items_decimales.Trim().Length > 0)
            {
                cabecera = "Los siguientes items son en PZ y las cantidades en la nota estan con decimales, lo cual no esta permitido verifique esta situacion!!!";
                alerta = "Verifique la pestaña de observaciones, se econtraron cantidades en decimales para items que son en PZ!!!. " + cadena_items_decimales;
                cumple = false;
            }
            return new respValidaDecimales
            {
                cabecera = cabecera,
                detalleObs = observacionesDecimales,
                alerta = alerta,
                cumple = cumple

            };
        }




        //Boton Totalizar
        [HttpPost]
        [Route("Totalizar/{userConn}/{nuevaNM}")]
        public async Task<object> Totalizar(string userConn, requestTotalizar requestTotalizar, bool nuevaNM)
        {
            decimal totalcant = 0;
            string total = "";
            // Crear una instancia de CultureInfo basada en la cultura actual
            CultureInfo culture = (CultureInfo)CultureInfo.CurrentCulture.Clone();

            // Establecer el separador decimal como punto
            culture.NumberFormat.NumberDecimalSeparator = ".";
            try
            {
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    List<tablaDetalleNM> tabladetalle = requestTotalizar.tabladetalle;

                    if (tabladetalle.Count > 0)
                    {
                        /*
                        foreach (var detalle in tabladetalle)
                        {
                            totalcant = totalcant + detalle.cantidad;
                        }
                        */
                        if (nuevaNM)
                        {
                            totalcant = tabladetalle.Sum(i => i.cantidad);
                        }
                        else
                        {
                            totalcant = tabladetalle.Sum(i => i.cantidad_revisada);
                        }
                    }
                    total = totalcant.ToString(culture);
                    return Ok(new
                    {
                        total = total
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return Problem("Error en el servidor al totalizar nota de movimiento: " + ex.Message);
            }
        }


        // boton Validar Saldos
        [HttpPost]
        [Route("ValidarSaldos/{userConn}")]
        public async Task<ActionResult<List<object>>> ValidarSaldos(string userConn, requestValidaSaldos requestValidaSaldos)
        {
            try
            {
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);
                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    string codempresa = requestValidaSaldos.codempresa;
                    int codalmorigen = requestValidaSaldos.codalmorigen;
                    int codalmdestino = requestValidaSaldos.codalmdestino;
                    int codconcepto = requestValidaSaldos.codconcepto;
                    string usuario = requestValidaSaldos.usuario;
                    List<tablaDetalleNM> tabladetalle = requestValidaSaldos.tabladetalle;

                    var result = await Revisar_ValidarSaldos(_context, codempresa, usuario, codalmorigen, codalmdestino, codconcepto, tabladetalle);
                    return Ok(new
                    {
                        cumple = result.resultado,
                        alerta = result.alerta,
                        tabladetalle = result.tabladetalle
                    });
                }
            }
            catch (Exception ex)
            {
                return Problem("Error en el servidor al validar saldos de los items en la nota de movimiento. " + ex.Message);
                throw;
            }
        }

        private async Task<(bool resultado, string alerta, string observacion, List<dt_disminuir> dt_disminuir, List<tablaDetalleNM> tabladetalle)> Revisar_ValidarSaldos(DBContext _context, string codempresa, string usuario, int codalmorigen, int codalmdestino, int codconcepto, List<tablaDetalleNM> tablaDetalle)
        {
            bool resultado = true;
            string msg_obs_revision_saldos_kg = "";
            string msg_obs_revision_saldos_pz = "";
            string alerta = "";
            string obs = "";
            List<dt_disminuir> dt_disminuir = new List<dt_disminuir>();

            var revisar_saldos_result = await revisar_saldos(_context, codempresa, usuario, codalmorigen, codalmdestino, codconcepto, tablaDetalle);
            dt_disminuir = revisar_saldos_result.dt_disminuir;
            msg_obs_revision_saldos_kg = revisar_saldos_result.msg1;
            msg_obs_revision_saldos_pz = revisar_saldos_result.msg2;

            foreach (var reg1 in dt_disminuir)
            {
                foreach (var reg in tablaDetalle)
                {
                    if (reg.coditem == reg1.coditem)
                    {
                        decimal cantidad = Convert.ToDecimal(reg.cantidad);
                        if (cantidad <= 0)
                        {
                            reg.cantidad = 0;
                        }
                        else if (cantidad > reg1.cantidad)
                        {
                            reg.cantidad = cantidad - reg1.cantidad;
                        }
                        else if (cantidad <= reg1.cantidad)
                        {
                            reg.cantidad = 0;
                        }
                    }
                }
            }

            if (dt_disminuir.Count > 0)
            {
                resultado = false;
                alerta = "Por la no existencia de saldos de grampas, se ha disminuido el envio de tuercas, verificar esta situacion!!!";
            }
            //si hay observaciones
            if (!string.IsNullOrEmpty(msg_obs_revision_saldos_kg) || !string.IsNullOrEmpty(msg_obs_revision_saldos_pz))
            {
                resultado = false;
                obs += "=============================================\n";
                obs += "OBSERVACIONES ITEMS EN (KG)\n";
                obs += "=============================================\n";
                obs += msg_obs_revision_saldos_kg + "\n\n";
                obs += "=============================================\n";
                obs += "OBSERVACIONES ITEMS EN (PZ)\n";
                obs += "=============================================\n";
                obs += msg_obs_revision_saldos_pz + "\n";
            }
            return (resultado, alerta, obs, dt_disminuir, revisar_saldos_result.tabladetalle);
        }

        private async Task<(bool resultado, string alerta, string msg1, string msg2, List<dt_disminuir> dt_disminuir, List<tablaDetalleNM> tabladetalle)> revisar_saldos(DBContext _context, string codempresa, string usuario, int codalmorigen, int codalmdestino, int codconcepto, List<tablaDetalleNM> tabladetalle)
        {
            bool resultado = true;
            string alerta = "";
            string msg_obs_revision_saldos_kg = "";
            string msg_obs_revision_saldos_pz = "";
            string txtentero1 = "";
            string txtdecimal1 = "";
            List<dt_disminuir> dt_disminuir = new List<dt_disminuir>();
            if (!string.IsNullOrWhiteSpace(codalmorigen.ToString()) && !string.IsNullOrWhiteSpace(codalmdestino.ToString()) && !string.IsNullOrWhiteSpace(codconcepto.ToString()))
            {
                string miudm = "";
                double saldo;
                dt_disminuir.Clear();
                int cantidad_disminuir = 0;
                int cantidad_cjto = 0;
                string cadena_saldo = "";
                string[] vector_saldo = new string[2];
                string cadena_cantidad = "";
                string[] vector_cantidad = new string[2];
                double mi_cantidad = 0;

                bool Es_Ag_Local = await empresa.AlmacenLocalEmpresa(_context, codempresa) == codalmorigen;
                bool Es_Tienda = await almacen.Es_Tienda(_context, codalmorigen);

                bool obtener_saldos_otras_ags_localmente = await saldos.Obtener_Saldos_Otras_Agencias_Localmente_context(_context, codempresa);
                bool obtener_cantidades_aprobadas_de_proformas = await saldos.Obtener_Cantidades_Aprobadas_De_Proformas(_context, codempresa);
                int AlmacenLocalEmpresa = await empresa.AlmacenLocalEmpresa_context(_context, codempresa);

                foreach (var row in tabladetalle)
                {
                    string coditem = row.coditem;
                    double cantidad = Convert.ToDouble(row.cantidad);

                    if (await hardCoded.NoTomarSaldosACubrir(coditem))
                    {
                        var resultadoSaldos = await saldos.SaldoItem_CrtlStock_Para_Ventas_Sam(_context, coditem, codalmorigen, Es_Tienda, false, "---", 0, false, codempresa, usuario, obtener_saldos_otras_ags_localmente, obtener_cantidades_aprobadas_de_proformas, AlmacenLocalEmpresa);
                        saldo = (double)resultadoSaldos.cantidad_ag_local_incluye_cubrir;
                    }
                    else
                    {
                        var resultadoSaldos = await saldos.SaldoItem_CrtlStock_Para_Ventas_Sam(_context, coditem, codalmorigen, Es_Tienda, false, "---", 0, true, codempresa, usuario, obtener_saldos_otras_ags_localmente, obtener_cantidades_aprobadas_de_proformas, AlmacenLocalEmpresa);
                        saldo = (double)resultadoSaldos.cantidad_ag_local_incluye_cubrir;
                    }

                    cantidad_disminuir = 0;
                    cantidad_cjto = Convert.ToInt32(row.cantidad);


                    miudm = row.udm.ToString().ToUpper();
                    if (miudm == "KG")
                    {
                        saldo = Math.Round(saldo, 2);
                    }
                    else
                    {
                        saldo = Math.Round(saldo, 0);
                    }


                    ////////////////////////////////////////////////////////
                    //SI LA CANTIDAD ES MAYOR AL SALDO (disminuir la cantidad pedida)
                    ////////////////////////////////////////////////////////
                    if (cantidad > saldo)
                    {
                        if (saldo < 0)
                        {
                            saldo = 0;
                            cantidad_disminuir = cantidad_cjto;
                        }
                        else
                        {
                            cantidad_disminuir = cantidad_cjto - (int)saldo;
                        }

                        row.cantidad = (decimal)saldo;

                        if (saldo == 0 && await item.es_item_de_control(_context, coditem))
                        {
                            var milista = await item.EsParteDeConjuntos(_context, coditem);

                            foreach (string parte in milista)
                            {
                                var dtpartes = await _context.inkit
                                   .Where(inkit => inkit.codigo == parte && inkit.item != coditem)
                                   .Select(inkit => new { coditem = inkit.codigo, item = inkit.item, cantidad = inkit.cantidad })
                                   .ToListAsync();

                                foreach (var parteRow in dtpartes)
                                {
                                    dt_disminuir.Add(new dt_disminuir
                                    {
                                        coditem_kit = coditem,
                                        cantidad_kit = cantidad_cjto,
                                        coditem = parteRow.item,
                                        cantidad = (decimal)(parteRow.cantidad * cantidad_disminuir)
                                    });
                                }
                            }
                        }
                    }
                    else
                    {
                        ///////////////////////////////////////////////////////////////////////
                        //SI LA CANTIDAD PEDIDA ES MEEEEENNNNOOORRRR  AL SALDO DISPONIBLE
                        ///////////////////////////////////////////////////////////////////////
                        miudm = row.udm.ToString().ToUpper();

                        if (miudm == "KG")
                        {
                            mi_cantidad = Math.Round(cantidad, 2);
                            row.cantidad = (decimal)mi_cantidad;

                            if (string.IsNullOrWhiteSpace(msg_obs_revision_saldos_kg))
                            {
                                msg_obs_revision_saldos_kg = "LAS CANTIDADES DE LOS SIGUIENTES ITEMS EN (KG) SE REDONDEARON A DOS DECIMALES:";
                                msg_obs_revision_saldos_kg += $"\n{coditem} = {cantidad} => {mi_cantidad} (KG)";
                            }
                            else
                            {
                                msg_obs_revision_saldos_kg += $"\n{coditem} = {cantidad} => {mi_cantidad} (KG)";
                            }
                        }
                        else
                        {
                            cadena_cantidad = row.cantidad.ToString();
                            vector_cantidad = cadena_cantidad.Split('.');

                            txtentero1 = vector_cantidad[0];
                            txtdecimal1 = vector_cantidad.Length > 1 ? vector_cantidad[1] : "0";

                            row.cantidad = Convert.ToDecimal(txtentero1);

                            if (Convert.ToInt32(txtdecimal1) > 0)
                            {
                                if (string.IsNullOrWhiteSpace(msg_obs_revision_saldos_pz))
                                {
                                    msg_obs_revision_saldos_pz = "LAS CANTIDADES DE LOS SIGUIENTES ITEMS (PZ) SE MODIFICARON, SE ELIMINARON LOS DECIMALES:";
                                    msg_obs_revision_saldos_pz += $"\n{coditem} = {saldo} => {txtentero1} (PZ)";
                                }
                                else
                                {
                                    msg_obs_revision_saldos_pz += $"\n{coditem} = {saldo} => {txtentero1} (PZ)";
                                }
                            }
                        }
                    }
                }
                alerta = "";
                resultado = true;
                return (resultado, alerta, msg_obs_revision_saldos_kg, msg_obs_revision_saldos_pz, dt_disminuir, tabladetalle);
            }
            else
            {
                alerta = "Debe indicar el almacen de origen, destino y el concepto de la nota de movimiento.";
                resultado = false;
                return (resultado, alerta, msg_obs_revision_saldos_kg, msg_obs_revision_saldos_pz, dt_disminuir, tabladetalle);
            }
        }


        // boton Cargar de proforma
        [HttpPost]
        [Route("CargardeProforma/{userConn}")]
        public async Task<ActionResult<List<object>>> CargardeProforma(string userConn, requestCargarProforma requestCargarProforma)
        {
            bool resultado = true;
            bool es_proforma_de_sol_urgente = true;
            string documento = "proforma";
            int codalmorigen = 0;
            int codproforma = 0;
            string obs = "";
            try
            {
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);
                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    string codempresa = requestCargarProforma.codempresa;
                    string id_proforma_sol = requestCargarProforma.id_proforma_sol;
                    int numeroidproforma_sol = requestCargarProforma.numeroidproforma_sol;
                    int codconcepto = requestCargarProforma.codconcepto;
                    string usuario = requestCargarProforma.usuario;
                    bool desglozar_cjtos = requestCargarProforma.desglozar_cjtos;
                    List<tablaDetalleNM> tablaDetalle = new List<tablaDetalleNM>();

                    var result = await validar_proforma_urgente(_context, id_proforma_sol, numeroidproforma_sol, codconcepto);
                    if (result.resultado)
                    {
                        var codigo_proforma = await _context.veproforma.Where(i => i.id == id_proforma_sol && i.numeroid == numeroidproforma_sol && i.aprobada == true && i.es_sol_urgente == true)
                        .Select(i => new
                        {
                            i.codigo,
                            i.codalmacen,
                            i.obs
                        }).FirstOrDefaultAsync();

                        if (codigo_proforma == null)
                        {
                            return BadRequest(new { resp = "La proforma que intenta buscar no se encuentra aprobada. No se puede Transferir." });
                        }
                        else
                        {
                            //transferir
                            //transferirdatosproforma
                            codproforma = codigo_proforma.codigo;
                            codalmorigen = codigo_proforma.codalmacen;
                            obs = codigo_proforma.obs;
                            if (desglozar_cjtos)
                            {
                                //implemmentado en fecha 19-11-2021
                                //transferirdetalleproforma_desglozado(codigodoc)
                                //se totaliza los items para que no hayan codigo repetidos
                                // Consulta para la primera parte del UNION
                                var parteNoDesglosada = from p1 in _context.veproforma1
                                                        join p2 in _context.initem on p1.coditem equals p2.codigo
                                                        where p1.codproforma == codproforma &&
                                                              !_context.initem.Any(item => item.codigo == p1.coditem && item.kit == true)
                                                        select new
                                                        {
                                                            coditem = p1.coditem,
                                                            descripcion = p2.descripcion,
                                                            medida = p2.medida,
                                                            udm = p2.unidad,
                                                            codaduana = "",
                                                            cantaut = p1.cantaut
                                                        };

                                // Consulta para la segunda parte del UNION
                                var parteDesglosada = from p1 in _context.veproforma1
                                                      join p2 in _context.initem on p1.coditem equals p2.codigo
                                                      join p3 in _context.inkit on p1.coditem equals p3.codigo
                                                      where p1.codproforma == codproforma &&
                                                            _context.initem.Any(item => item.codigo == p1.coditem && item.kit == true)
                                                      select new
                                                      {
                                                          coditem = p3.item,
                                                          descripcion = p2.descripcion,
                                                          medida = p2.medida,
                                                          udm = p2.unidad,
                                                          codaduana = "",
                                                          cantaut = p1.cantaut * p3.cantidad
                                                      };

                                // Combinar ambas consultas usando UNION
                                var consultaUnion = parteNoDesglosada.Union(parteDesglosada);

                                // Agrupar y proyectar en tablaDetalleNM
                                List<tablaDetalleNM> detalle = await consultaUnion
                                    .GroupBy(x => new { x.coditem, x.descripcion, x.medida, x.udm, x.codaduana })
                                    .Select(g => new tablaDetalleNM
                                    {
                                        coditem = g.Key.coditem,
                                        descripcion = g.Key.descripcion,
                                        medida = g.Key.medida,
                                        udm = g.Key.udm,
                                        codaduana = g.Key.codaduana,
                                        cantidad = (decimal)g.Sum(x => x.cantaut) // Aquí se asigna la suma al campo cantidad
                                    })
                                    .OrderBy(x => x.coditem)
                                    .ToListAsync();

                                if (detalle == null || detalle.Count == 0)
                                {
                                    return BadRequest(new { resp = "Esta proforma no tiene items." });
                                }
                                else
                                {
                                    tablaDetalle = detalle;

                                    //foreach (var dato in detalle)
                                    //{
                                    //    var nuevoRegistro = new tablaDetalleNM
                                    //    {
                                    //        coditem = dato.coditem,
                                    //        descripcion = dato.descripcion,
                                    //        medida = dato.medida,
                                    //        udm = dato.udm,
                                    //        codaduana = dato.codaduana,
                                    //        cantidad = dato.cantidad
                                    //    };

                                    //    tablaDetalle.Add(nuevoRegistro);
                                    //}
                                }
                            }
                            else
                            {
                                // transferirdetalleproforma(codigodoc);
                                var detalle = await _context.veproforma1
                                    .Join(
                                        _context.initem,
                                        p1 => p1.coditem,
                                        p2 => p2.codigo,
                                        (p1, p2) => new { p1, p2 }
                                    )
                                    .Where(joined => joined.p1.codproforma == codproforma)
                                    .Select(joined => new tablaDetalleNM
                                    {
                                        coditem = joined.p1.coditem,
                                        descripcion = joined.p2.descripcion,
                                        medida = joined.p2.medida,
                                        udm = joined.p2.unidad,
                                        codaduana = string.Empty, // Valor constante
                                        cantidad = (decimal)joined.p1.cantaut
                                    })
                                    .ToListAsync();

                                if (detalle == null || detalle.Count == 0)
                                {
                                    return BadRequest(new { resp = "Esta proforma no tiene items." });
                                }
                                else
                                {
                                    tablaDetalle = detalle;

                                    //foreach (var dato in detalle)
                                    //{
                                    //    var nuevoRegistro = new tablaDetalleNM
                                    //    {
                                    //        coditem = dato.coditem,
                                    //        descripcion = dato.descripcion,
                                    //        medida = dato.medida,
                                    //        udm = dato.udm,
                                    //        codaduana = dato.codaduana,
                                    //        cantidad = dato.cantidad
                                    //    };

                                    //    tablaDetalle.Add(nuevoRegistro);
                                    //}
                                }
                            }

                            return Ok(new
                            {
                                resultado = result.resultado,
                                alerta = result.alerta,
                                codalmorigen = codalmorigen,
                                obs = obs,
                                tabladetalle = tablaDetalle
                            });
                        }
                    }
                    else
                    {
                        return BadRequest(new { resp = result.alerta });
                    }

                }
            }
            catch (Exception ex)
            {
                return Problem("Error en el servidor al cargar lso datos desde una proforma urgente. " + ex.Message);
                throw;
            }
        }
        private async Task<(bool resultado, string alerta)> validar_proforma_urgente(DBContext _context, string id_proforma_sol, int numeroidproforma_sol, int codconcepto)
        {
            bool resultado = true;
            string alerta = "";

            if (String.IsNullOrEmpty(id_proforma_sol))
            {
                resultado = false;
                alerta = "Debe poner el ID del tipo de Proforma de la que desea transferir.";
                return (resultado, alerta);
            }
            if (numeroidproforma_sol <= 0)
            {
                resultado = false;
                alerta = "Debe poner el numero de Proforma de la que desea transferir.";
                return (resultado, alerta);
            }
            if (await ventas.Existe_Proforma(_context, id_proforma_sol, numeroidproforma_sol) == false)
            {
                resultado = false;
                alerta = "La proforma no existe, no puede cargar esta proforma.";
                return (resultado, alerta);
            }
            if (await Ventas.proforma_anulada(_context, await ventas.codproforma(_context, id_proforma_sol, numeroidproforma_sol)) == true)
            {
                resultado = false;
                alerta = "La proforma esta anulada, no puede cargar esta proforma.";
                return (resultado, alerta);
            }
            if (await ventas.proforma_aprobada(_context, await ventas.codproforma(_context, id_proforma_sol, numeroidproforma_sol)) == false)
            {
                resultado = false;
                alerta = "La proforma no esta aprobada, no puede cargar esta proforma.";
                return (resultado, alerta);
            }
            if (await ventas.proforma_para_aprobar(_context, await ventas.codproforma(_context, id_proforma_sol, numeroidproforma_sol)) == false)
            {
                resultado = false;
                alerta = "La proforma no esta para aprobar, no puede cargar esta proforma.";
                return (resultado, alerta);
            }
            if (await ventas.proforma_transferida(_context, await ventas.codproforma(_context, id_proforma_sol, numeroidproforma_sol)) == true)
            {
                resultado = false;
                alerta = "La proforma ya esta transferida, no puede cargar esta proforma.";
                return (resultado, alerta);
            }
            if (await ventas.proforma_es_sol_urgente(_context, id_proforma_sol, numeroidproforma_sol) == false)
            {
                resultado = false;
                alerta = "La proforma seleccionada no es de una solicitud urgente de tienda!!!.";
                return (resultado, alerta);
            }
            if (codconcepto != 10)
            {
                resultado = false;
                alerta = "Solo puede cargar una proforma si el concepto es para una venta urgente.";
                return (resultado, alerta);
            }
            resultado = true;
            alerta = "";
            return (resultado, alerta);
        }



        // IMPORTAR 
        [HttpPost]
        [Route("importNMinJson")]
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
            string pathDescFile = "";

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
                pathDescFile = Path.Combine(_targetDirectory, primerArchivo);
                string xmlDecript = await encripVB.DecryptData(pathDescFile);
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

                List<Dictionary<string, object>> cabeceraList = DataTableToListConverter.ConvertToList(cabeceraTabla);
                List<Dictionary<string, object>> detalleList = DataTableToListConverter.ConvertToList(detalleTabla);
                return Ok(new
                {
                    cabeceraList,
                    detalleList
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
                System.IO.File.Delete(pathDescFile);
            }

        }

        [HttpPost]
        [Route("getDescMedDetalle/{userConn}")]
        public async Task<IActionResult> getDescMedDetalle(string userConn, List<tablaDetalleNM> tablaDetalle)
        {
            try
            {
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);
                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    // Obtener los datos de initem solo una vez
                    var itemsDict = await _context.initem
                        .Where(i => tablaDetalle.Select(td => td.coditem).Contains(i.codigo))
                        .ToDictionaryAsync(i => i.codigo, i => new { i.descripcion, i.medida });

                    // Asignar los valores en tablaDetalle
                    foreach (var reg in tablaDetalle)
                    {
                        if (itemsDict.TryGetValue(reg.coditem, out var datos))
                        {
                            reg.descripcion = datos.descripcion;
                            reg.medida = datos.medida;
                        }
                    }
                    return Ok(tablaDetalle);
                }
            }
            catch (Exception)
            {

                throw;
            }
        }

        // EXPORTAR ZIP
        [HttpGet]
        [Route("exportNM/{userConn}/{codNM}")]
        public async Task<IActionResult> exportProforma(string userConn, int codNM)
        {
            try
            {
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);
                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var result = await cargariedataset(_context, codNM);
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


        private async Task<(bool resp, DataSet iedataset, string id, int numeroid)> cargariedataset(DBContext _context, int codNM)
        {
            DataSet iedataset = new DataSet();

            try
            {
                iedataset.Clear();
                iedataset.Reset();

                // cargar cabecera
                var dataNM = await _context.inmovimiento.Where(i => i.codigo == codNM).ToListAsync();
                if (dataNM.Any())
                {
                    DataTable cabeceraTable = dataNM.ToDataTable();
                    cabeceraTable.TableName = "cabecera";
                    iedataset.Tables.Add(cabeceraTable);
                    iedataset.Tables["cabecera"].Columns.Add("documento", typeof(string));
                    iedataset.Tables["cabecera"].Rows[0]["documento"] = "NOTA";

                }
                string id = dataNM[0].id;
                int numeroid = dataNM[0].numeroid;
                /*
                // Añadir campo identificador
                iedataset.Tables["cabecera"].Columns.Add("documento", typeof(string));
                iedataset.Tables["cabecera"].Rows[0]["documento"] = "PROFORMA";

                */

                // Cargar detalle usando LINQ y Entity Framework
                var dataDetalle = await _context.inmovimiento1
                    .Where(p => p.codmovimiento == codNM)
                    .Join(_context.initem,
                          p => p.coditem,
                          i => i.codigo,
                          (p, i) => new
                          {
                              p.coditem,
                              i.descripcion,
                              i.medida,
                              p.cantidad,
                              p.udm,
                              p.codaduana,
                          })
                    .OrderBy(p => p.coditem)
                    .ToListAsync();
                
                DataTable detalleTable = dataDetalle.ToDataTable();   // convertir a dataTable
                detalleTable.TableName = "detalle";
                iedataset.Tables.Add(detalleTable);
                
                return (true, iedataset, id, numeroid);
            }
            catch (Exception)
            {
                return (false, iedataset, "", 0);
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

        private async Task<(bool resp, string filePath)> exportar_encriptado(string xmlText, string id, int numeroid)
        {
            ziputil zUtil = new ziputil();
            try
            {
                string currentDirectory = AppDomain.CurrentDomain.BaseDirectory;
                string outputDirectory = Path.Combine(currentDirectory, "OutputFiles");

                Directory.CreateDirectory(outputDirectory); // Crear el directorio si no existe
                string outName = Path.Combine(outputDirectory, id + "-" + numeroid + ".enc");

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


        [HttpGet]
        [Route("getDataImpNM/{userConn}/{codNM}/{codempresa}/{usua}/{codtarifa}")]
        public async Task<ActionResult<List<object>>> getDataImpNM(string userConn, int codNM, string codempresa, string usua, int codtarifa)
        {
            try
            {
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);
                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    string empresa = "";
                    string titulo = "";
                    string usuario = "";
                    string nit = "";
                    string rcodconcepto = "";
                    string rcodconceptodescripcion = "";
                    // string rcorrelativo = "";
                    string rfecha = "";
                    string rcodalmacen = "";
                    string rcodvendedor = "";
                    string rcodalmorigen = "";
                    string rcodalmdestino = "";
                    string robs = "";
                    string rctiponm = "";
                    string rpesototal = "";
                    string rnomcliente = "";

                    // obtener los datos de cabecera
                    var cabecera = await _context.inmovimiento.Where(i => i.codigo == codNM).FirstOrDefaultAsync();
                    if (cabecera == null)
                    {
                        return BadRequest(new { resp = "No se encontraron datos con el codigo proporcionado, consulte con el Administrador." });
                    }

                    // busca los datos del concepto
                    var tbl = await _context.inconcepto.Where(i => i.codigo == cabecera.codconcepto).Select(i => new
                    {
                        i.codigo,
                        i.descripcion,
                        i.factor
                    }).FirstOrDefaultAsync();
                    if (tbl != null)
                    {
                        if (tbl.factor == 1)
                        {
                            // nota de ingreso
                            rctiponm = "NOTA DE INGRESO";
                        }
                        else if(tbl.factor == -1)
                        {
                            // nota de salida
                            rctiponm = "NOTA DE SALIDA";
                        }
                        rcodconceptodescripcion = tbl.descripcion;
                    }
                    else
                    {
                        rctiponm = "NOTA DE MOVIMIENTO";
                        // rcodconceptodescripcion.Text = Chr(34) & codconceptodescripcion.Text & Chr(34)
                    }
                    titulo = cabecera.id + "-" + cabecera.numeroid;
                    empresa = await nombres.nombreempresa(_context, codempresa);
                    usuario = usua;
                    nit = "N.I.T.: " + await empresa1.NITempresa(_context, codempresa);
                    rcodconcepto = cabecera.codconcepto.ToString();
                    // rcorrelativo.Text = Chr(34) & CInt(correlativo.Text).ToString("00000000") & Chr(34)
                    rfecha = cabecera.fecha.ToShortDateString();
                    rcodalmacen = cabecera.codalmacen.ToString();
                    rcodvendedor = cabecera.codvendedor + " Prof:" + cabecera.idproforma + "-" + cabecera.numeroidproforma;
                    rcodalmorigen = cabecera.codalmorigen.ToString();
                    rcodalmdestino = cabecera.codalmdestino.ToString();
                    robs = cabecera.obs;
                    rpesototal = (cabecera.peso ?? 0).ToString("####,##0.000", new CultureInfo("en-US"));

                    // si es la entrega a un cliente 
                    if (cabecera.codconcepto == 104)
                    {
                        rnomcliente = await cliente.Razonsocial(_context, cabecera.codcliente);
                    }

                    ////////////////////////////////////////
                    //// Fin de pasar valores a las variables
                    ////////////////////////////////////////
                    var tablaDetalle = await _context.inmovimiento1.Where(i => i.codmovimiento == codNM)
                            .Join(_context.initem,
                                m => m.coditem,
                                i => i.codigo,
                                (m, i) => new tablaDetalleNM
                                {
                                    coditem = m.coditem,
                                    descripcion = i.descripcion,
                                    medida = i.medida,
                                    udm = m.udm,
                                    cantidad = m.cantidad,
                                    costo = 0
                                }
                            )
                            .OrderBy(i => i.coditem)
                            .ToListAsync();


                    string alerta = "";

                    // preguntar si es un ajuste para añadir la columna de costo
                    if (await inventario.ConceptoEsAjuste(_context, cabecera.codconcepto))
                    {
                        // pedir codtarifa
                        if (await ventas.UsuarioTarifa_Permitido(_context, usuario, codtarifa))
                        {
                            if (codtarifa > 0)
                            {
                                foreach (var reg in tablaDetalle)
                                {
                                    reg.costo = await ventas.preciodelistaitem(_context, codtarifa, reg.coditem);
                                }
                            }
                        }
                        else
                        {
                            alerta = "Este usuario no esta habilitado para ver ese tipo de Precio";
                        }
                    }

                    return Ok(new
                    {
                        alerta,

                        empresa,
                        titulo,
                        usuario,
                        nit,
                        rcodconcepto,
                        rcodconceptodescripcion,
                        rfecha,
                        rcodalmacen,
                        rcodvendedor,
                        rcodalmorigen,
                        rcodalmdestino,
                        robs,
                        rctiponm,
                        rpesototal,
                        rnomcliente,

                        tablaDetalle

                    });


                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return Problem("Error en el servidor al obtener datos para imprimir por vista previa NM: " + ex.Message);
                throw;
            }
        }



        [HttpPost]
        [Route("impresionNotaMovimiento/{userConn}")]
        public async Task<ActionResult<List<object>>> impresionNotaMovimiento(string userConn, requestImprimir requestImprimir)
        {
            int codigoNM = requestImprimir.codigoNM;
            string codEmpresa = requestImprimir.codEmpresa;
            int codtarifa = requestImprimir.codtarifa;
            string usuario = requestImprimir.usuario;
            string codconceptodescripcion = requestImprimir.codconceptodescripcion;
            string codclientedescripcion = requestImprimir.codclientedescripcion;
            double total = requestImprimir.total;
            try
            {
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);
                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var codAlm = await _context.inmovimiento.Where(i => i.codigo == codigoNM).Select(i => i.codalmacen).FirstOrDefaultAsync();
                    System.Drawing.Printing.PrinterSettings config = new System.Drawing.Printing.PrinterSettings();

                    // Asignar el nombre de la impresora
                    string impresora = await _context.inalmacen.Where(i => i.codigo == codAlm).Select(i => i.impresora_nr).FirstOrDefaultAsync() ?? "";
                    // string pathFile = await mostrardocumento_directo(_context, codClienteReal, codEmpresa, codclientedescripcion, preparacion, veremision);

                    if (impresora == "")
                    {
                        return BadRequest(new { resp = "No se encontró una impresora registrada para este código de almacen, consulte con el administrador." });
                    }
                    config.PrinterName = impresora;
                    if (config.IsValid)
                    {
                        // generamos el archivo .txt y regresamos la ruta
                        
                        inmovimiento cabecera = await _context.inmovimiento.Where(i => i.codigo == codigoNM).FirstOrDefaultAsync();

                        var tablaDetalle = await _context.inmovimiento1.Where(i => i.codmovimiento == codigoNM)
                            .Join(_context.initem,
                                m => m.coditem,
                                i => i.codigo,
                                (m, i) => new tablaDetalleNM
                                {
                                    coditem = m.coditem,
                                    descripcion = i.descripcion,
                                    medida = i.medida,
                                    udm = m.udm,
                                    codaduana = m.codaduana,
                                    cantidad = m.cantidad,
                                    costo = 0
                                }
                            )
                            .OrderBy(i => i.coditem)
                            .ToListAsync();

                        //*/////////////////
                        int codconcepto = cabecera.codconcepto;

                        var pathFile = await mostrardocumento_directo(_context, codEmpresa, codconcepto, codtarifa, usuario, codconceptodescripcion, codclientedescripcion, total, cabecera, tablaDetalle);

                        // Configurar e iniciar el trabajo de impresión
                        // Aquí iría el código para configurar el documento a imprimir y lanzar la impresión
                        if (pathFile.resultado == false)
                        {
                            return BadRequest(new { resp = pathFile.msg });
                        }
                        bool impremiendo = await RawPrinterHelper.SendFileToPrinterAsync(config.PrinterName, pathFile.msg);
                        
                        // bool impremiendo = await RawPrinterHelper.PrintFileAsync(config.PrinterName, pathFile);

                        // luego de mandar a imprimir eliminamos el archivo
                        if (System.IO.File.Exists(pathFile.msg))
                        {
                            System.IO.File.Delete(pathFile.msg);
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
                return BadRequest(new { resp = "No se encontro la nota de movimiento" });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return Problem("Error en el servidor al imprimir NM: " + ex.Message);
                throw;
            }
        }


        // imprimir documento formato matricial
        // el codigo tarifa debe pedir Marvin por frontEnd antes de venir a esta ruta para imprimir
        private async Task<(bool resultado, string msg)> mostrardocumento_directo(DBContext _context, string codempresa, int codconcepto, int codtarifa, string usuario, string codconceptodescripcion, string codclientedescripcion, double total, inmovimiento cabecera, List<tablaDetalleNM> tablaDetalle) 
        {
            //#################################################
            //mandar impresion
            impresion imp = new impresion();

            bool es_ajuste = false;
            string alertaPrecio = "";
            //parametros
            string imp_titulo;
            string imp_tiponm = "";
            string imp_id_concepto;
            string imp_conceptodescripcion = "";
            string imp_empresa;
            string imp_usuario;
            string imp_nit;
            string imp_codvendedor;
            string imp_codalmacen;
            string imp_codalmacen_origen;
            string imp_codalmacen_destino;
            string imp_fecha;
            string imp_total;
            string imp_pesototal;
            string imp_obs;
            string imp_fecha_impresion;
            string imp_nomclient;

            // preguntar si es un ajuste para añadir la columna de costo
            if (await inventario.ConceptoEsAjuste(_context,codconcepto))
            {
                // pedir codtarifa
                es_ajuste = true;
                if (await ventas.UsuarioTarifa_Permitido(_context,usuario, codtarifa))
                {
                    if (codtarifa > 0)
                    {
                        foreach (var reg in tablaDetalle)
                        {
                            reg.costo = await ventas.preciodelistaitem(_context, codtarifa, reg.coditem);
                        }
                    }
                    else
                    {
                        return (false, "El código de tarifa no puede ser menor o igual a 0, consulte con el administrador");
                    }
                }
                else
                {
                    alertaPrecio = "Este usuario no esta habilitado para ver ese tipo de Precio";
                }
            }
            else
            {
                es_ajuste = false;
            }

            DateTime fecha_serv = await funciones.FechaDelServidor(_context);
            imp_fecha_impresion = fecha_serv.Date.ToShortDateString();
            imp_titulo = cabecera.id + "-" + cabecera.numeroid;

            imp_id_concepto = codconcepto.ToString();

            // busca los datos del concepto
            var datos = await _context.inconcepto.Where(i => i.codigo == codconcepto).Select(i => new
            {
                i.codigo,
                i.descripcion,
                i.factor
            }).FirstOrDefaultAsync();

            if (datos != null)
            {
                switch (datos.factor)
                {
                    case 1:
                        // nota de ingreso
                        imp_tiponm = "NOTA DE INGRESO";
                        break;
                    case -1:
                        // nota de salida
                        imp_tiponm = "NOTA DE SALIDA";
                        break;
                    default:
                        imp_conceptodescripcion = datos.descripcion;
                        break;
                }
            }
            else
            {
                imp_tiponm = "NOTA DE MOVIMIENTO";
                imp_conceptodescripcion = codconceptodescripcion;
            }

            imp_empresa = await nombres.nombreempresa(_context, codempresa);
            imp_usuario = cabecera.usuarioreg;
            imp_nit = "N.I.T.: " + await empresa.NITempresa(_context, codempresa);

            imp_codalmacen = cabecera.codalmacen.ToString();
            imp_codalmacen_origen = cabecera.codalmorigen.ToString();
            imp_codalmacen_destino = cabecera.codalmdestino.ToString();
            imp_fecha = cabecera.fecha.Date.ToShortDateString();

            imp_codvendedor = cabecera.codvendedor + " Prof:" + cabecera.idproforma + "-" + cabecera.numeroidproforma;

            imp_total = total.ToString("####,##0.00000", new CultureInfo("en-US"));
            imp_pesototal = (cabecera.peso ?? 0).ToString("####,##0.00000", new CultureInfo("en-US"));
            imp_obs = cabecera.obs;

            if (codconcepto == 104)
            {
                string subString = codclientedescripcion.Substring(6); // Índice 6 para empezar desde el 7° carácter
                imp_nomclient = subString;
            }
            else
            {
                imp_nomclient = "";
            }

            // obtener detalle en data Table
            DataTable dt = obtenerDetalleDataTable(_context, tablaDetalle);

            string currentDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string outputDirectory = Path.Combine(currentDirectory, "OutputFiles");
            string ruta = "";

            if (await empresa.HojaReportes(_context,codempresa) == 0)
            {
                ruta = imp.imprimir_inmovimiento(outputDirectory, dt, imp_titulo, imp_tiponm, imp_id_concepto, imp_conceptodescripcion, imp_empresa,
                    imp_usuario, imp_nit, imp_codvendedor, imp_codalmacen, imp_codalmacen_origen, imp_codalmacen_destino, imp_fecha, imp_total, imp_pesototal,
                    imp_obs, imp_fecha_impresion, false, es_ajuste, imp_nomclient);
            }




            return (true,ruta);
        }
         
        private DataTable obtenerDetalleDataTable(DBContext _context, List<tablaDetalleNM> tablaDetalle)
        {
            // convertir a dataTable
            // Crear un DataTable y definir sus columnas
            DataTable dataTable = new DataTable();
            dataTable.Columns.Add("coditem", typeof(string));
            dataTable.Columns.Add("descripcion", typeof(string));
            dataTable.Columns.Add("medida", typeof(string));
            dataTable.Columns.Add("udm", typeof(string));
            dataTable.Columns.Add("codaduana", typeof(string));
            dataTable.Columns.Add("cantidad", typeof(decimal));
            dataTable.Columns.Add("costo", typeof(double));

            foreach (var item in tablaDetalle)
            {
                dataTable.Rows.Add(
                    item.coditem,
                    item.descripcion,
                    item.medida,
                    item.udm,
                    item.codaduana,
                    item.cantidad,
                    item.costo
                );
            }
            return dataTable;
        }


    }


    public class dt_disminuir
    {
        public string coditem_kit { get; set; }
        public decimal cantidad_kit { get; set; }
        public string coditem { get; set; }
        public decimal cantidad { get; set; }

    }
    public class requestValidaSaldos
    {
        public string codempresa { get; set; }
        public string usuario { get; set; }
        public int codalmorigen { get; set; }
        public int codalmdestino { get; set; }
        public int codconcepto { get; set; }
        public List<tablaDetalleNM> tabladetalle { get; set; }

    }

    public class requestCargarProforma
    {
        public string codempresa { get; set; }
        public string usuario { get; set; }
        public string id_proforma_sol { get; set; }
        public int numeroidproforma_sol { get; set; }
        public int codconcepto { get; set; }
        public bool desglozar_cjtos { get; set; }

    }
    public class requestTotalizar
    {
        public List<tablaDetalleNM> tabladetalle { get; set; }

    }


    public class requestImprimir
    {
        public string codEmpresa {  get; set; }
        public string codclientedescripcion { get; set; }
        public int codtarifa {  get; set; }
        public string usuario { get; set; }
        public string codconceptodescripcion { get; set; }
        public double total {  get; set; }
        public int codigoNM {  get; set; }
    }
}
