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
using SIAW.Controllers.inventarios.transaccion;

namespace SIAW.Controllers.inventarios.modificacion
{
    [Route("api/inventario/modif/[controller]")]
    [ApiController]
    public class docmodifinmovimientoController : ControllerBase
    {
        private readonly Log log = new Log();
        private readonly Documento documento = new Documento();
        private readonly Funciones funciones = new Funciones();
        private readonly Saldos saldos = new Saldos();
        private readonly Inventario inventario = new Inventario();
        private readonly Almacen almacen = new Almacen();
        private readonly Seguridad seguridad = new Seguridad();
        private readonly Restricciones restricciones = new Restricciones();
        private readonly Items item = new Items();
        private readonly Ventas ventas = new Ventas();
        private readonly Nombres nombres = new Nombres();
        private readonly Personal personal = new Personal();
        private readonly Cliente cliente = new Cliente();


        private readonly string _controllerName = "docinmovimientoController";

        private readonly UserConnectionManager _userConnectionManager;
        public docmodifinmovimientoController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }


        [HttpGet]
        [Route("getUltiNMId/{userConn}")]
        public async Task<object> getUltiNMId(string userConn)
        {
            try
            {
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var ultimoRegistro = await _context.inmovimiento
                            .OrderByDescending(i => i.codigo)
                            .Select(i => new
                            {
                                i.codigo,
                                i.id,
                                i.numeroid
                            }).FirstOrDefaultAsync();
                    return Ok(ultimoRegistro);
                }
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
                throw;
            }
        }

        [HttpGet]
        [Route("obtNMxModif/{userConn}/{codNotMov}/{tienePermisoModifAntUltInv}")]
        public async Task<object> obtNMxModif(string userConn, int codNotMov, bool tienePermisoModifAntUltInv)
        {
            try
            {
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);
                int codpersonadesde = 0;
                string codpersonadesdedesc = "";
                int codalmdestino = 0;
                string codclientedescripcion = "";
                string estadodoc = "";
                string codalmacendescripcion = "";
                string codalmdestinodescripcion = "";
                string codalmorigendescripcion = "";
                string codconceptodescripcion = "";

                int codigo_proforma = 0;

                bool codalmorigenReadOnly = false;
                bool codalmdestinoReadOnly = false;
                bool codpersonadesdeReadOnly = false;
                bool codclienteReadOnly = false;

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var cabecera = await _context.inmovimiento.Where(i => i.codigo == codNotMov).FirstOrDefaultAsync();
                    if (cabecera.codpersona == null)
                    {
                        codpersonadesde = 0;
                        codpersonadesdedesc = "";
                    }
                    else
                    {
                        if (cabecera.codpersona == 0)
                        {
                            codpersonadesde = (int)cabecera.codpersona;
                            codpersonadesdedesc = "";
                        }
                        else
                        {
                            codpersonadesde = (int)cabecera.codpersona;
                            // buscar_nombre_persona()
                            var descripcion = await _context.pepersona.Where(i => i.codigo == cabecera.codpersona).Select(i => i.apellido1 + " , " + i.nombre1).FirstOrDefaultAsync();
                            if (descripcion == null)
                            {
                                codpersonadesde = 0;
                                codalmdestino = 0;
                            }
                            else
                            {
                                codpersonadesde = (int)cabecera.codpersona;
                                codpersonadesdedesc = descripcion;
                                codalmdestino = await personal.codalmacen(_context, codpersonadesde);
                            }
                            // verificar_concepto_usr_final_despues_de_mostrar_datos()
                            if (await inventario.ConceptoEsUsuarioFinal(_context,cabecera.codconcepto))
                            {
                                codalmorigenReadOnly = true;
                                codalmdestinoReadOnly = true;
                                codpersonadesdeReadOnly = false;
                            }
                            else
                            {
                                codpersonadesdeReadOnly = true;
                            }
                        }
                    }

                    // codcliente
                    if (cabecera.codcliente == null)
                    {
                        cabecera.codcliente = "0";
                        codclientedescripcion = "";
                    }
                    else
                    {
                        if (cabecera.codcliente.Trim() == "0")
                        {
                            codclientedescripcion = "";
                        }
                        else
                        {
                            codclientedescripcion = await cliente.Razonsocial(_context,cabecera.codcliente);
                            // verificar_concepto_entrega_cliente_despues_de_mostrar_datos
                            if (await inventario.Concepto_Es_Entrega_Cliente(_context, cabecera.codconcepto))
                            {
                                codalmorigenReadOnly = true;
                                codalmdestinoReadOnly = true;
                                codclienteReadOnly = false;
                            }
                            else
                            {
                                codclienteReadOnly = true;
                            }
                        }
                    }

                    if (cabecera.fecha_inicial == null)
                    {
                        cabecera.fecha_inicial = cabecera.fecha;
                    }
                    if (cabecera.anulada == true)
                    {
                        estadodoc = "ANULADA";
                    }
                    try
                    {
                        codigo_proforma = await ventas.codproforma(_context, cabecera.idproforma_sol, cabecera.numeroidproforma_sol ?? 0);
                    }
                    catch (Exception)
                    {
                        cabecera.idproforma_sol = "";
                        cabecera.numeroidproforma_sol = 0;
                    }


                    // habilita solo casillas editables

                    codalmacendescripcion = await nombres.nombrealmacen(_context, cabecera.codalmacen);
                    codalmdestinodescripcion = await nombres.nombrealmacen(_context, cabecera.codalmdestino ?? 0);
                    codalmorigendescripcion = await nombres.nombrealmacen(_context, cabecera.codalmorigen ?? 0);

                    // mostrar el campo especial de catalogo
                    var consultaConcepto = await _context.inconcepto.Where(i => i.codigo == cabecera.codconcepto).Select(i => new
                    {
                        i.codigo,
                        i.descripcion
                    }).FirstOrDefaultAsync();

                    if (consultaConcepto == null)
                    {
                        codconceptodescripcion = "";
                    }
                    else
                    {
                        codconceptodescripcion = consultaConcepto.descripcion;
                    }


                    var dataPorConcepto = await actualizarconcepto(_context, "", cabecera.codconcepto, cabecera.codalmacen);

                    bool btnGrabarReadOnly = false;
                    bool btnAnularReadOnly = false;
                    bool btnHabilitarReadOnly = false;
                    // mostrar detalle del documento actual
                    List<tablaDetalleNM> tabladetalle = await mostrardetalle(_context, cabecera.codigo);
                    if (await seguridad.periodo_fechaabierta_context(_context,cabecera.fecha.Date,2))
                    {
                        btnGrabarReadOnly = false;
                        btnAnularReadOnly = false;
                    }
                    else
                    {
                        btnGrabarReadOnly = true;
                        btnAnularReadOnly = true;
                    }

                    if (await restricciones.ValidarModifDocAntesInventario(_context, cabecera.codalmacen, cabecera.fecha.Date))
                    {
                        // verificar si periodo abiero
                        if (await seguridad.periodo_fechaabierta_context(_context,cabecera.fecha.Date,2))
                        {
                            btnGrabarReadOnly = false;
                            btnAnularReadOnly = false;
                            btnHabilitarReadOnly = false;
                        }
                        else
                        {
                            btnGrabarReadOnly = true;
                            btnAnularReadOnly = true;
                            btnHabilitarReadOnly = true;
                        }
                    }
                    else
                    {
                        btnGrabarReadOnly = true;
                        btnAnularReadOnly = true;
                        if (tienePermisoModifAntUltInv)  // si tiene permiso permite habilitar
                        {
                            btnGrabarReadOnly = false;
                            btnAnularReadOnly = false;
                        }
                    }

                    bool esTienda = await almacen.Es_Tienda(_context, cabecera.codalmorigen ?? 0);


                    return Ok(new
                    {
                        codpersonadesde,
                        codpersonadesdedesc,
                        codalmdestino,
                        codclientedescripcion,
                        estadodoc,
                        codalmacendescripcion,
                        codalmdestinodescripcion,
                        codalmorigendescripcion,
                        codconceptodescripcion,
                        codigo_proforma,
                        codalmorigenReadOnly,
                        codalmdestinoReadOnly,
                        codpersonadesdeReadOnly,
                        codclienteReadOnly,
                        btnGrabarReadOnly,
                        btnAnularReadOnly,
                        btnHabilitarReadOnly,
                        esTienda,
                        // ///////////////////////////////////////////////////////////////
                        cabecera,
                        tabladetalle,

                    });
                }

            }
            catch (Exception ex)
            {
                return Problem("Error en el servidor al obtener Datos: " + ex.Message);
                throw;
            }
        }

        private async Task<List<tablaDetalleNM>> mostrardetalle(DBContext _context, int codigo)
        {
            List<tablaDetalleNM> tabladetalle = await _context.inmovimiento1
                .Where(i => i.codmovimiento == codigo)
                .Join(_context.initem,
                m => m.coditem,
                i => i.codigo,
                (m,i) => new {m,i})
                .Select(i => new tablaDetalleNM
                {
                    coditem = i.m.coditem,
                    descripcion = i.i.descripcion,
                    medida = i.i.medida,
                    udm = i.m.udm,
                    codaduana = i.m.codaduana,
                    cantidad = i.m.cantidad,
                    costo = 0,
                    cantidad_revisada = i.m.cantidad,
                    nuevo = "no",
                }).ToListAsync();
            return tabladetalle;
        }

        private async Task<dataPorConcepto> actualizarconcepto(DBContext _context, string opcion, int codconcepto, int codalmacen)
        {
            bool codalmdestinoReadOnly, codalmorigenReadOnly, traspaso, fidEnable, fnumeroidEnable, codpersonadesdeReadOnly = new bool();
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
        [Route("grabarDocumento/{userConn}/{codempresa}/{traspaso}")]
        public async Task<ActionResult<object>> grabarDocumento(string userConn, string codempresa, bool traspaso, requestGabrar dataGrabar)
        {
            try
            {
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);
                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    bool NMAAnulado = await _context.inmovimiento.Where(i=> i.codigo == dataGrabar.cabecera.codigo).Select(i => i.anulada).FirstOrDefaultAsync() ?? true;

                    if (NMAAnulado == true)
                    {
                        return BadRequest(new
                        {
                            resp = "Esta Nota de Movimiento esta Anulada, no puede ser modificada.",
                            valido = false,
                        });
                    }

                    List<tablaDetalleNM> tablaDetalle = dataGrabar.tablaDetalle;
                    // calculartotal.PerformClick()
                    // borrar items con cantidad 0 o menor
                    tablaDetalle = tablaDetalle.Where(i => i.cantidad > 0).ToList();


                    var guardarDoc = await editardatos(_context, codempresa, traspaso, dataGrabar.cabecera, tablaDetalle);
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
                    string mensajeConfirmacion = "Se modifico la nota de movimiento : " + dataGrabar.cabecera.id + " - " + guardarDoc.numeroID + " y se volvera a recuperar en pantalla con la informacion modificada.";
                    await log.RegistrarEvento(_context, dataGrabar.cabecera.usuarioreg, Log.Entidades.SW_Nota_Movimiento, guardarDoc.codigoNM.ToString(), dataGrabar.cabecera.id, guardarDoc.numeroID.ToString(), _controllerName, "Grabar", Log.TipoLog.Creacion);

                    List<string> alertas = new List<string>();
                    alertas.Add("Desea Exportar el documento? ");

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
                            if (cambios > 0)
                            {
                                await log.RegistrarEvento(_context, dataGrabar.cabecera.usuarioreg, Log.Entidades.SW_Nota_Movimiento, guardarDoc.codigoNM.ToString(), dataGrabar.cabecera.id, guardarDoc.numeroID.ToString(), _controllerName, "Grabar enlace NM: " + dataGrabar.cabecera.id + "-" + guardarDoc.numeroID.ToString() + " con SU: " + doc_solurgente.id + "-" + doc_solurgente.nroId, Log.TipoLog.Modificacion);
                                alertas.Add("La nota de movimiento fue enlazada con la solicitud urgente: " + doc_solurgente.id + "-" + doc_solurgente.nroId);
                            }
                        }
                    }
                    // registrar la nota en despachos si es que es un concepto habilitado para registrar en despachos
                    List<string> mensajesDesp = await Registrar_Nota_En_Despachos(_context, dataGrabar.cabecera.codconcepto, dataGrabar.cabecera.id, dataGrabar.cabecera.numeroid, dataGrabar.cabecera.usuarioreg);
                    alertas.AddRange(mensajesDesp);

                    return Ok(new { resp = mensajeConfirmacion, alertas = alertas });

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
            if (await inventario.concepto_espara_despacho(_context, codconcepto))
            {
                // alerta insertar en despachos
                alertas.Add("Se grabara la nota de movimiento : " + id + " - " + numeroid + ". en los despachos. ");
                // intentar añadir
                if (await inventario.Existe_Nota_De_Movimiento(_context, id, numeroid))
                {
                    if (await nota_en_despachos(_context, id, numeroid))
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
                    Codcliente = p1.codalmdestino ?? 0,
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
                    fdespacho = new DateTime(2000 - 01 - 01),
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
                            newReg.frefacturacion = new DateTime(1900 - 01 - 01).Date;
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

        private async Task<bool> nota_en_despachos(DBContext _context, string idnm, int nroidnmov)
        {
            var consulta = await _context.vedespacho.Where(i => i.id == idnm && i.nroid == nroidnmov).CountAsync();
            bool resultado = true;
            if (consulta > 0)
            {
                resultado = true;
            }
            else
            {
                resultado = false;
            }
            return resultado;
        }


        private async Task<(bool valido, string msg, List<Dtnegativos>? dtnegativos, int codigoNM, int numeroID)> editardatos(DBContext _context, string codempresa, bool traspaso, inmovimiento inmovimiento, List<tablaDetalleNM> tablaDetalle)
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


                    // #######################################
                    // ##RUTINA QUE ACTUALIZA EL SALDO ACTUAL  (DESCONTAR)
                    // sia_funciones.Saldos.Instancia.Inmovimiento_ActualizarSaldo(codigo.Text, sia_funciones.Saldos.modo_actualizacion.eliminar, tabladetalle)
                    await saldos.Inmovimiento_ActualizarSaldo(_context, inmovimiento.codigo, Saldos.ModoActualizacion.Eliminar);
                    // #######################################


                    await inventario.actaduanamovimiento(_context, inmovimiento.codigo, "eliminar", inmovimiento.fecha);


                    // prepara detalle Nota Movimiento, ademas no se guardan items con cantidad revisada en 0
                    List<inmovimiento1> inmovimiento1 = tablaDetalle.Where(i => i.cantidad_revisada != 0).Select(i => new inmovimiento1
                    {
                        codmovimiento = inmovimiento.codigo,
                        coditem = i.coditem,
                        cantidad = i.cantidad_revisada,
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
                    using (var dbContexTransaction = _context.Database.BeginTransaction())
                    {
                        try
                        {
                            // actualiza cabecera
                            try
                            {
                                _context.Entry(inmovimiento).State = EntityState.Modified;
                                await _context.SaveChangesAsync();
                            }
                            catch (Exception ex)
                            {
                                return (false, "Error al grabar la cabecera de la nota de movimiento: " + ex.Message, null, 0, 0);
                            }





                            // guarda detalle (veproforma1)
                            // actualizar codigoNM para agregar por si acaso
                            inmovimiento1 = inmovimiento1.Select(p => { p.codmovimiento = inmovimiento.codigo; return p; }).ToList();

                            // elimina primero, luego guardar detalle o items nuevos
                            var detalleNMAnt = await _context.inmovimiento1.Where(i => i.codmovimiento == inmovimiento.codigo).ToListAsync();
                            if (detalleNMAnt.Count() > 0)
                            {
                                _context.inmovimiento1.RemoveRange(detalleNMAnt);
                                await _context.SaveChangesAsync();
                            }
                            _context.inmovimiento1.AddRange(inmovimiento1);
                            await _context.SaveChangesAsync();
                            dbContexTransaction.Commit();
                        }
                        catch (Exception ex)
                        {
                            dbContexTransaction.Rollback();
                            return (false, $"Error en el servidor al guardar Proforma: {ex.Message}", null, 0, 0);
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

                    if (await saldos.Inmovimiento_ActualizarSaldo(_context, inmovimiento.codigo, Saldos.ModoActualizacion.Crear) == false)
                    {
                        await log.RegistrarEvento(_context, inmovimiento.usuarioreg, Log.Entidades.SW_Nota_Movimiento, inmovimiento.codigo.ToString(), inmovimiento.id, inmovimiento.numeroid.ToString(), _controllerName, "No actualizo stock en cantidad de algun item en NM.", Log.TipoLog.Creacion);
                    }
                    // #######################################

                    await inventario.actaduanamovimiento(_context, inmovimiento.codigo, "crear", inmovimiento.fecha);

                    //Desde 29/09/2024 Registrar el log de items modificados registrando la cantidad del item que era originalmente antes de la modificacion
                    foreach (var reg in tablaDetalle)
                    {
                        if (reg.nuevo == "no")
                        {
                            // actualiza el item ya existente
                            if (reg.cantidad_revisada == 0) // se elimino Item
                            {
                                await log.RegistrarEvento(_context, inmovimiento.usuarioreg, Log.Entidades.SW_Nota_Movimiento, inmovimiento.codigo.ToString(), inmovimiento.id, inmovimiento.numeroid.ToString(), _controllerName, "Item Eliminado: " + reg.coditem + " De: " + Math.Round(reg.cantidad, 2) + " A " + reg.cantidad_revisada, Log.TipoLog.Eliminacion); 
                            }
                            else
                            {
                                await log.RegistrarEvento(_context, inmovimiento.usuarioreg, Log.Entidades.SW_Nota_Movimiento, inmovimiento.codigo.ToString(), inmovimiento.id, inmovimiento.numeroid.ToString(), _controllerName, "Item Modificado: " + reg.coditem + " De: " + Math.Round(reg.cantidad, 2) + " A " + reg.cantidad_revisada, Log.TipoLog.Modificacion);
                            }
                        }
                        else
                        {
                            // inserta añadir el item nuevo
                            await log.RegistrarEvento(_context, inmovimiento.usuarioreg, Log.Entidades.SW_Nota_Movimiento, inmovimiento.codigo.ToString(), inmovimiento.id, inmovimiento.numeroid.ToString(), _controllerName, "Item Añadido: " + reg.coditem + " De: " + Math.Round(reg.cantidad, 2) + " A " + reg.cantidad_revisada, Log.TipoLog.Modificacion);
                        }
                    }




                    return (true, "", null, inmovimiento.codigo, inmovimiento.numeroid);

                }
                return (false, validaCabecera.msg, null, 0, 0);
            }

            return (false, validaDetalle.msg, validaDetalle.dtnegativos, 0, 0);
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
            if (inmovimiento.fnumeroid == null || inmovimiento.fnumeroid < 0)
            {
                return (false, "No puede dejar la casilla de Numero de Documento de origen  en blanco.");
            }

            if (await inventario.concepto_espara_despacho(_context, inmovimiento.codconcepto) && inmovimiento.codconcepto == 10 && await almacen.Es_Tienda(_context, (int)inmovimiento.codalmdestino))
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
                if (inmovimiento.numeroidproforma == null || inmovimiento.numeroidproforma <= 0)
                {
                    return (false, "No puede dejar la casilla de Numero de la PF que hizo la solicitud urgente en blanco o con valor 0.");
                }
                if (await ventas.proforma_es_sol_urgente(_context, inmovimiento.idproforma_sol, inmovimiento.numeroidproforma_sol ?? 0) == false)
                {
                    return (false, "La proforma enlazada es NO es de una solicitud urgente de Tienda,verifique esta situacion.");
                }
            }

            if (await seguridad.periodo_abierto_context(_context, inmovimiento.fecha.Year, inmovimiento.fecha.Month, 2) == false)
            {
                return (false, "No puede crear documentos para ese periodo de fechas.");
            }

            if (await restricciones.ValidarModifDocAntesInventario(_context, inmovimiento.codalmacen, inmovimiento.fecha) == false)
            {
                return (false, "No puede modificar datos anteriores al ultimo inventario, Para eso necesita una autorizacion especial.");
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

            if (inmovimiento.numeroidproforma == null)
            {
                inmovimiento.numeroidproforma = 0;
            }
            if (inmovimiento.numeroidproforma_sol == null)
            {
                inmovimiento.numeroidproforma_sol = 0;
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
                if (await item.iteminactivo(_context, reg.coditem))
                {
                    return (false, "El item de la linea " + indice + " -> " + reg.coditem + " no puede ser movido por que esta inactivo.", null);
                }
                if (await item.item_usar_en_movimiento(_context, reg.coditem) == false)
                {
                    return (false, "El item de la linea " + indice + " -> " + reg.coditem + " no puede usarse en notas de movimiento.", null);
                }
                if (string.IsNullOrWhiteSpace(reg.udm))
                {
                    return (false, "No puso la Unidad de Medida en la Linea " + indice + " .", null);
                }
                if (reg.cantidad_revisada == null)
                {
                    return (false, "No puso la cantidad en la Linea " + indice + " .", null);
                }
                if (reg.cantidad_revisada <= 0)
                {
                    // si es item nuevo alerta si la cantidad revisada es cero
                    // si no es nuevo, si se permite cantidad_revisada=0
                    if (reg.nuevo == "si")
                    {
                        return (false, "No puso la cantidad revisada en la Linea " + indice + " .", null);
                    }
                }
                indice++;
            }

            // validar negativos
            if ((factor == -1) && (await inventario.PermitirNegativos(_context, codempresa) == false))
            {
                List<string> msgs = new List<string>();
                List<string> negs = new List<string>();

                var validaNegativos = await Validar_Saldos_Negativos_Doc(_context, codempresa, inmovimiento, tablaDetalle);
                if (validaNegativos.resultado == false)   ////////////////////////////// VERIFICAR SI DEBE PERMITIR GRABAR CON CLAVE, EN EL SIA LO HACE
                {
                    return (false, validaNegativos.msg, validaNegativos.dtnegativos);
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
            respValidaDecimales validaDecimales = validar_cantidades_decimales(tablaDetalle);
            if (validaDecimales.cumple == false)
            {
                // VALIDAR CANTIDADES
                // VERIFICAR QUE DEBE PEDIR CONTRASEÑA
                return (false, "No se puede grabar el documento debido a que se encontró cantidades en decimales para items que son en PZ!!!", null);
            }


            return (true, "", null);
        }


        private respValidaDecimales validar_cantidades_decimales(List<tablaDetalleNM> tablaDetalle)
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
                string valorCadena = reg.cantidad.ToString(culture);
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
                        string obs = "Item: " + reg.coditem + " = " + reg.cantidad + " (PZ)";
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


        private async Task<(bool resultado, string msg, List<Dtnegativos> dtnegativos)> Validar_Saldos_Negativos_Doc(DBContext _context, string codempresa, inmovimiento inmovimiento, List<tablaDetalleNM> tablaDetalle)
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
            return (resultado, msg, dtnegativos);
        }


    }




   
    

}
