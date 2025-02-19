using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIAW.Controllers.inventarios.transaccion;
using siaw_DBContext.Data;
using siaw_DBContext.Models;
using siaw_DBContext.Models_Extra;
using siaw_funciones;

namespace SIAW.Controllers.inventarios.modificacion
{
    [Route("api/inventario/modif/[controller]")]
    [ApiController]
    public class docmodifinpedidoController : ControllerBase
    {
        private readonly Nombres nombres = new Nombres();
        private readonly Inventario inventario = new Inventario();
        private readonly Seguridad seguridad = new Seguridad();
        private readonly Log log = new Log();
        private readonly Funciones funciones = new Funciones();

        private readonly string _controllerName = "docmodifinpedidoController";

        private readonly UserConnectionManager _userConnectionManager;
        public docmodifinpedidoController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }

        [HttpGet]
        [Route("getUltiPedidoId/{userConn}")]
        public async Task<object> getUltiPedidoId(string userConn)
        {
            try
            {
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var ultimoRegistro = await _context.inpedido
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
        [Route("obtPedidoxModif/{userConn}/{idPed}/{nroIdPed}")]
        public async Task<object> obtPedidoxModif(string userConn, string idPed, int nroIdPed)
        {
            try
            {
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);
                string codalmacendescripcion = "";
                string codalmdestinodescripcion = "";
                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var cabecera = await _context.inpedido.Where(i => i.id == idPed && i.numeroid == nroIdPed).FirstOrDefaultAsync();

                    if (cabecera == null)
                    {
                        return BadRequest(new { resp = "No se encontraron datos con los datos proporcionado." });
                    }

                    codalmacendescripcion = await nombres.nombrealmacen(_context, cabecera.codalmacen);
                    codalmdestinodescripcion = await nombres.nombrealmacen(_context, cabecera.codalmdestino);

                    List<tablaDetalleModifPedido> detalle = await _context.inpedido1.Where(i => i.codpedido == cabecera.codigo)
                        .Join(_context.initem,
                            p => p.coditem,
                            i => i.codigo,
                            (p, i) => new { p, i })
                        .Select(i => new tablaDetalleModifPedido
                        {
                            codpedido = i.p.codpedido,
                            coditem = i.p.coditem,
                            descripcion = i.i.descripcion,
                            medida = i.i.medida,
                            udm = i.p.udm,
                            cantidad = i.p.cantidad,
                            codproveedor = i.p.codproveedor ?? 0
                        }).ToListAsync();

                    foreach (var reg in detalle)
                    {
                        var ultCompra = await obtener_datos_ultima_compra(_context, reg.coditem, cabecera.codalmacen);
                        reg.fecha_ultima_compra = ultCompra.fechaUltComp;
                        reg.precio_ultima_compra = ultCompra.precioUltComp;
                        reg.cantidad_ultima_compra = ultCompra.cantUltComp;

                        reg.smax = await inventario.itemstockmax_origen_destino(_context, cabecera.codalmacen, cabecera.codalmdestino, reg.coditem);
                        reg.smin = await inventario.itemstockmin_origen_destino(_context, cabecera.codalmacen, cabecera.codalmdestino, reg.coditem);

                        reg.seleccion = false;
                        reg.desccodproveedor = "";

                        if (reg.codproveedor == null || reg.codproveedor == 0)
                        {
                            // nada
                        }
                        else
                        {
                            reg.desccodproveedor = await nombres.nombreproveedor(_context, reg.codproveedor);
                        }

                    }
                    bool grabarReadOnly = false;
                    bool eliminarReadOnly = false;

                    if (await seguridad.periodo_fechaabierta_context(_context, cabecera.fecha.Date, 2))
                    {
                        grabarReadOnly = false;
                        eliminarReadOnly = false;
                    }
                    else
                    {
                        grabarReadOnly = true;
                        eliminarReadOnly = true;
                    }

                    return Ok(new
                    {
                        codalmacendescripcion,
                        codalmdestinodescripcion,

                        grabarReadOnly,
                        eliminarReadOnly,

                        cabecera,
                        detalle
                    });
                }
            }
            catch (Exception ex)
            {
                return Problem("Error en el servidor al obtener Datos: " + ex.Message);
                throw;
            }
        }

        private async Task<detalleUltCompra> obtener_datos_ultima_compra(DBContext _context, string coditem, int codalmacen)
        {
            detalleUltCompra datosUltComp = new detalleUltCompra();
            var resultado = await _context.inmovimiento
                .Join(_context.inmovimiento1,
                      p1 => p1.codigo,
                      p2 => p2.codmovimiento,
                      (p1, p2) => new { p1, p2 })
                .Where(x => x.p1.anulada == false &&
                            x.p2.coditem == coditem &&
                            x.p1.codalmacen == codalmacen &&
                            x.p1.codconcepto == 60)
                .OrderByDescending(x => x.p1.fecha)
                .Select(x => new
                {
                    x.p1.fid,
                    x.p1.fnumeroid,
                    x.p2.coditem,
                    x.p2.cantidad
                })
                .FirstOrDefaultAsync();
            if (resultado != null)
            {
                var dt_ing = await _context.cpembarque
                    .Join(_context.cpembarque1,
                          p1 => p1.codigo,
                          p2 => p2.codembarque,
                          (p1, p2) => new { p1, p2 })
                    .Where(x => x.p1.id == resultado.fid &&
                                x.p1.numeroid == resultado.fnumeroid &&
                                x.p1.anulada == false &&
                                x.p2.coditem == coditem)
                    .Select(x => new
                    {
                        x.p1.id,
                        x.p1.numeroid,
                        x.p1.fecha,
                        x.p2.precio,
                        x.p2.cantidad
                    })
                    .FirstOrDefaultAsync();
                if (dt_ing != null)
                {
                    // fecha compra
                    datosUltComp.fechaUltComp = dt_ing.fecha;
                    // precio unit
                    datosUltComp.precioUltComp = dt_ing.precio;
                    // cantidad
                    datosUltComp.cantUltComp = dt_ing.cantidad;
                }
                else
                {
                    datosUltComp.fechaUltComp = new DateTime(1900, 1, 1);
                    datosUltComp.cantUltComp = 0;
                    datosUltComp.precioUltComp = 0;
                }
            }
            else
            {
                datosUltComp.fechaUltComp = new DateTime(1900, 1, 1);
                datosUltComp.cantUltComp = 0;
                datosUltComp.precioUltComp = 0;
            }
            return datosUltComp;
        }




        [HttpPost]
        [Route("grabarDocumento/{userConn}/{codempresa}")]
        public async Task<ActionResult<object>> grabarDocumento(string userConn, string codempresa, requestGabrarPedido dataGrabar)
        {
            try
            {
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);
                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    inpedido cabecera = dataGrabar.cabecera;
                    cabecera.id = cabecera.id.Trim();
                    cabecera.obs = cabecera.obs.Trim();
                    cabecera.fecha = cabecera.fecha.Date;

                    List<inpedido1> detalle = dataGrabar.tablaDetalle;
                    // valida detalle
                    var detalleValido = await validarDetalle(_context, detalle);
                    if (detalleValido.valido == false)
                    {
                        return BadRequest(new { detalleValido.valido, resp = detalleValido.msg });
                    }
                    // valida cabecera
                    var cabeceraValido = await validardatos(_context, cabecera);
                    if (cabeceraValido.valido == false)
                    {
                        return BadRequest(new { cabeceraValido.valido, resp = cabeceraValido.msg });
                    }
                    // manda a grabar
                    var docGrabado = await guardarModificarPedido(_context, cabecera, detalle);
                    if (docGrabado.valido == false)
                    {
                        return BadRequest(new { docGrabado.valido, resp = docGrabado.msg });
                    }

                    await log.RegistrarEvento(_context, cabecera.usuarioreg, Log.Entidades.SW_Pedido, docGrabado.codigoPedido.ToString(), cabecera.id, docGrabado.numeroID.ToString(), _controllerName, "Modificar Pedido", Log.TipoLog.Edicion);

                    return Ok(new
                    {
                        docGrabado.valido,
                        resp = "Se guardo el pedido " + cabecera.id + " - " + docGrabado.numeroID + " con exito.",
                        docGrabado.codigoPedido,
                    });
                }
            }
            catch (Exception ex)
            {
                return Problem("Error en el servidor al guardar Documento: " + ex.Message);
                throw;
            }
        }

        private async Task<(bool valido, string msg)> validarDetalle(DBContext _context, List<inpedido1> tablaDetalle)
        {
            int indice = 1;
            if (tablaDetalle.Count() <= 0)
            {
                return (false, "No tiene ningun item en su pedido.");
            }
            foreach (var reg in tablaDetalle)
            {
                if (string.IsNullOrWhiteSpace(reg.coditem))
                {
                    return (false, "No eligio El Item en la Linea " + indice + " .");
                }
                if (reg.coditem.Trim().Length < 1)
                {
                    return (false, "No eligio El Item en la Linea " + indice + " .");
                }

                if (string.IsNullOrWhiteSpace(reg.udm))
                {
                    return (false, "No puso la Unidad de Medida en la Linea " + indice + " .");
                }
                if (reg.udm.Trim().Length < 1)
                {
                    return (false, "No puso la Unidad de Medida en la Linea " + indice + " .");
                }

                if (reg.cantidad == null)
                {
                    return (false, "No puso la cantidad en la Linea " + indice + " .");
                }
                if (reg.cantidad <= 0)
                {
                    return (false, "La cantidad en la Linea " + indice + " No puede ser menor o igual a 0.");
                }
                indice++;
            }
            return (true, "");
        }

        private async Task<(bool valido, string msg)> validardatos(DBContext _context, inpedido cabecera)
        {
            // validar casillas vacias
            if (string.IsNullOrWhiteSpace(cabecera.id))
            {
                return (false, "No puede dejar el tipo de pedido en blanco.");
            }
            if (cabecera.numeroid == null || cabecera.numeroid <= 0)
            {
                return (false, "No puede dejar el número ID de pedido en blanco.");
            }
            if (cabecera.codalmacen == null || cabecera.codalmacen <= 0)
            {
                return (false, "No puede dejar la casilla de Almacen en blanco.");
            }
            if (cabecera.codalmdestino == null || cabecera.codalmdestino <= 0)
            {
                return (false, "No puede dejar la casilla de Almacen Destino en blanco.");
            }
            if (cabecera.codalmacen == cabecera.codalmdestino)
            {
                return (false, "No puede hacer un pedido al mismo almacen en que se encuentra.");
            }
            if (cabecera.obs.Trim().Length == 0)
            {
                return (false, "No puede dejar la casilla de Observacion en blanco.");
            }
            if (await seguridad.periodo_fechaabierta_context(_context, cabecera.fecha.Date, 2) == false)
            {
                return (false, "No puede crear documentos para ese periodo de fechas.");
            }
            return (true, "");
        }

        private async Task<(bool valido, string msg, int codigoPedido, int numeroID)> guardarModificarPedido(DBContext _context, inpedido inpedido, List<inpedido1> inpedido1)
        {
            int codPedido = 0;
            using (var dbContexTransaction = _context.Database.BeginTransaction())
            {
                try
                {
                    // actualiza cabecera
                    try
                    {
                        _context.Entry(inpedido).State = EntityState.Modified;
                        await _context.SaveChangesAsync();
                    }
                    catch (Exception ex)
                    {
                        return (false, "Error al grabar la cabecera del Pedido: " + ex.Message, 0, 0);
                    }
                    codPedido = inpedido.codigo;
                    inpedido1 = inpedido1.Select(p => { p.codpedido = codPedido; return p; }).ToList();

                    // guardaar detalle
                    var pedidoAnt = await _context.inpedido1.Where(i => i.codpedido == codPedido).ToListAsync();
                    if (pedidoAnt.Count() > 0)
                    {
                        _context.inpedido1.RemoveRange(pedidoAnt);
                        await _context.SaveChangesAsync();
                    }

                    _context.inpedido1.AddRange(inpedido1);
                    await _context.SaveChangesAsync();
                    dbContexTransaction.Commit();
                }
                catch (Exception ex)
                {
                    dbContexTransaction.Rollback();
                    return (false, $"Error en el servidor al guardar Pedido: {ex.Message}", 0, 0);
                    throw;
                }
            }
            return (true, "", codPedido, inpedido.numeroid ?? -1);
        }

        [HttpDelete]
        [Route("eliminarPedido/{userConn}/{codPedido}/{usuario}")]
        public async Task<object> eliminarPedido(string userConn, int codPedido, string usuario)
        {
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);
            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                using (var dbContexTransaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        // elimina Detalle
                        var detallePedido = await _context.inpedido1.Where(i => i.codpedido == codPedido).ToListAsync();
                        if (detallePedido.Count() > 0)
                        {
                            _context.inpedido1.RemoveRange(detallePedido);
                            await _context.SaveChangesAsync();
                        }

                        // eliminar Cabecera
                        var cabeceraPedido = await _context.inpedido.Where(i => i.codigo == codPedido).FirstOrDefaultAsync();
                        if (cabeceraPedido != null)
                        {
                            await log.RegistrarEvento(_context, usuario, Log.Entidades.SW_Pedido, cabeceraPedido.codigo.ToString(), cabeceraPedido.id, cabeceraPedido.numeroid.ToString(), _controllerName, "Eliminar Pedido", Log.TipoLog.Eliminacion);

                            _context.inpedido.Remove(cabeceraPedido);
                            await _context.SaveChangesAsync();
                        }

                        

                        dbContexTransaction.Commit();
                        return Ok(new { resp = "Se eliminó el Pedido con Exito." });
                    }
                    catch (Exception ex)
                    {
                        dbContexTransaction.Rollback();
                        return Problem("Error en el servidor al eliminar Documento: " + ex.Message);
                        throw;
                    }
                }
            }
        }

        [HttpPut]
        [Route("anularPedido/{userConn}/{codPedido}/{usuario}")]
        public async Task<object> anularPedido(string userConn, int codPedido, string usuario)
        {
            try
            {
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);
                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var pedido = await _context.inpedido.Where(i => i.codigo == codPedido).FirstOrDefaultAsync();
                    if (pedido == null)
                    {
                        return BadRequest(new { resp = "No se pudo obtener la informacion con el código proporcionado, consulte con el Administrador." });
                    }
                    if (pedido.anulado == true)
                    {
                        return BadRequest(new { resp = "Este pedido ya esta Anulado." });
                    }
                    pedido.anulado = true;
                    pedido.fechareg = await funciones.FechaDelServidor(_context);
                    await _context.SaveChangesAsync();

                    // guardar logs
                    await log.RegistrarEvento(_context, usuario, Log.Entidades.SW_Pedido, pedido.codigo.ToString(), pedido.id, pedido.numeroid.ToString(), _controllerName, "Grabar", Log.TipoLog.Anulacion);
                    return Ok(new { resp = "Pedido anulado exitosamente" });
                }
            }
            catch (Exception ex)
            {
                return Problem("Error en el servidor al anular Documento: " + ex.Message);
                throw;
            }
        }

        [HttpPut]
        [Route("habilitarPedido/{userConn}/{codPedido}/{usuario}")]
        public async Task<object> habilitarPedido(string userConn, int codPedido, string usuario)
        {
            try
            {
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);
                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var pedido = await _context.inpedido.Where(i => i.codigo == codPedido).FirstOrDefaultAsync();
                    if (pedido == null)
                    {
                        return BadRequest(new { resp = "No se pudo obtener la informacion con el código proporcionado, consulte con el Administrador." });
                    }
                    if (pedido.anulado == false)
                    {
                        return BadRequest(new { resp = "Este pedido ya esta Habilitado!!!" });
                    }
                    pedido.anulado = false;
                    pedido.fechareg = await funciones.FechaDelServidor(_context);
                    await _context.SaveChangesAsync();

                    // guardar logs
                    await log.RegistrarEvento(_context, usuario, Log.Entidades.SW_Pedido, pedido.codigo.ToString(), pedido.id, pedido.numeroid.ToString(), _controllerName, "Grabar", Log.TipoLog.Habilitacion);
                    return Ok(new { resp = "Pedido habilitado exitosamente" });
                }
            }
            catch (Exception ex)
            {
                return Problem("Error en el servidor al anular Documento: " + ex.Message);
                throw;
            }
        }



    }
    public class tablaDetalleModifPedido
    {
        public int codpedido { get; set; } = 0;
        public string coditem { get; set; } = "";
        public string descripcion { get; set; } = "";
        public string medida { get; set; } = "";
        public string udm { get; set; } = "";
        public decimal cantidad { get; set; } = 0;
        public DateTime fecha_ultima_compra { get; set; } = new DateTime(1900, 1, 1);
        public decimal cantidad_ultima_compra { get; set; } = 0;
        public decimal precio_ultima_compra { get; set; } = 0;
        public decimal smax { get; set; } = 0;
        public decimal smin { get; set; } = 0;
        public int codproveedor { get; set; } = 0;
        public string desccodproveedor { get; set; } = "";
        public bool seleccion { get; set; } = false;
    }
    public class detalleUltCompra
    {
        public DateTime fechaUltComp { get; set; } = new DateTime(1900, 1, 1);
        public decimal cantUltComp { get; set; } = 0;
        public decimal precioUltComp { get; set; } = 0;
    }

}
