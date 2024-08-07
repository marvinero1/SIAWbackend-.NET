using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using siaw_DBContext.Data;
using siaw_DBContext.Models;
using siaw_funciones;
using System.Linq;

namespace SIAW.Controllers.ventas.modificacion
{
    [Route("api/venta/modif/[controller]")]
    [ApiController]
    public class docmodifveremisionController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;

        private readonly siaw_funciones.Ventas ventas = new siaw_funciones.Ventas();
        private readonly siaw_funciones.Seguridad seguridad = new siaw_funciones.Seguridad();
        private readonly siaw_funciones.Cliente cliente = new siaw_funciones.Cliente();

        private readonly Restricciones restricciones = new Restricciones();
        private readonly string _controllerName = "docmodifveremisionController";

        public docmodifveremisionController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }

        [HttpGet]
        [Route("getUltimoCodNR/{userConn}/{usuario}")]
        public async Task<object> getUltimoCodNR(string userConn, string usuario)
        {
            try
            {
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var vendedor = await seguridad.usuario_es_vendedor(_context, usuario);
                    int ultimo = 0;
                    if (vendedor > 0)
                    {
                        ultimo = await _context.veremision
                            .Where(v => v.codvendedor == vendedor)
                            .MaxAsync(v => v.codigo);
                    }
                    else
                    {
                        ultimo = await _context.veremision
                            .MaxAsync(v => v.codigo);
                    }
                    return Ok(new
                    {
                        codUltimoNR = ultimo
                    });
                }

            }
            catch (Exception ex)
            {
                return Problem($"Error en el servidor: {ex.Message}");
                throw;
            }
        }

        [HttpGet]
        [Route("getCodigoNR/{userConn}/{usuario}/{id}/{nroId}")]
        public async Task<object> getCodigoNR(string userConn, string usuario, string id, int nroId)
        {
            try
            {
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {

                    var codigoNR = await _context.veremision.Where(i => i.id == id && i.numeroid == nroId).Select(i => new
                    {
                        i.codigo
                    }).FirstOrDefaultAsync();
                    if (codigoNR == null)
                    {
                        return BadRequest(new { resp = "No se encontro ese número de documento." });
                    }
                    var codVendedor = await ventas.Vendedor_de_Cliente_De_Remision(_context, id, nroId);
                    if (await seguridad.autorizado_vendedores(_context,usuario,codVendedor,codVendedor) == false)
                    {
                        return BadRequest(new { resp = "No esta autorizado para ver esa informacion." });
                    }
                    return Ok(new
                    {
                        codigoNR
                    });
                    
                }

            }
            catch (Exception ex)
            {
                return Problem($"Error en el servidor: {ex.Message}");
                throw;
            }
        }

        [HttpGet]
        [Route("mostrarDatosNR/{userConn}/{codigodoc}/{usuario}")]
        public async Task<object> mostrarDatosNR(string userConn, int codigodoc, string usuario)
        {
            try
            {
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (await ventas.ValidarMostrarDocumento(_context, TipoDocumento_Ventas.Remision, codigodoc, usuario) == false)
                    {
                        return BadRequest(new { resp = "No le esta permitido ver este tipo de precio" });
                    }

                    // cabecera
                    var cabecera = await _context.veremision.Where(i => i.codigo == codigodoc).FirstOrDefaultAsync();
                    if (cabecera == null)
                    {
                        return BadRequest(new { resp = "No se encontró una Nota de Remision con los datos proporcionados, revise los datos" });
                    }
                    // obtener razon social de cliente
                    var codclientedescripcion = await cliente.Razonsocial(_context, cabecera.codcliente);
                    string _codcliente_real = "";
                    if (cabecera.codcliente_real != null)
                    {
                        _codcliente_real = cabecera.codcliente_real;
                    }
                    else
                    {
                        _codcliente_real = cabecera.codcliente;
                    }
                    string _codcliente_real_descripcion = await cliente.NombreComercial(_context, _codcliente_real) + " - " + await cliente.Razonsocial(_context, _codcliente_real);

                    // COMPLEMENTO CI
                    if (cabecera.complemento_ci == null)
                    {
                        cabecera.complemento_ci = "";
                    }
                    // PROFORMA COMPLEMENTARIA
                    string complemento = "";
                    if (await ventas.proforma_es_complementaria(_context, cabecera.codproforma ?? 0))
                    {
                        complemento = "COMPLEMENTARIA";
                    }
                    string estadodoc = "";
                    if (cabecera.anulada == true)
                    {
                        estadodoc = "ANULADA";
                    }

                    // cargar recargos  cargarrecargo
                    var recargos = await cargarrecargo(_context, codigodoc);
                    // cargar descuentos   cargardesextra
                    var descuentos = await cargardesextra(_context, codigodoc);
                    // cargar iva  cargariva
                    var iva = await cargariva(_context, codigodoc);

                    /*
                    Me.limpiarchequerechazado()
                    Me.cargarchequerechazado(codigodoc)
                     */

                    // obtener Detalle
                    var detalle = await mostrardetalle(_context, codigodoc);

                    // verificar periodo abierto por fecha de nota de remision
                    bool periodoHabilitado = new bool();
                    if (await seguridad.periodo_fechaabierta_context(_context, cabecera.fecha.Date, 3))
                    {
                        periodoHabilitado = true;
                    }
                    else
                    {
                        periodoHabilitado = false;
                    }
                    // verificar la modificacion antes de inventario
                    bool modifAntesInv = new bool();
                    string msgValModAntesInv = "";
                    if (await restricciones.ValidarModifDocAntesInventario(_context,cabecera.codalmacen, cabecera.fecha))
                    {
                        modifAntesInv = true;
                    }
                    else
                    {
                        modifAntesInv = false;
                        msgValModAntesInv = "Esta nota es anterior a el ultimo inventario fisico, para poder modifiarla. Necesita autorizacion especial. Necesita usted modfiicar esta nota ?";
                    }


                    return Ok(new
                    {
                        codclientedescripcion,
                        _codcliente_real,
                        _codcliente_real_descripcion,
                        complemento,
                        estadodoc,
                        periodoHabilitado,
                        modifAntesInv,
                        msgValModAntesInv,

                        cabecera,
                        detalle,
                        recargos,
                        descuentos,
                        iva,

                    });

                }

            }
            catch (Exception ex)
            {
                return Problem($"Error en el servidor: {ex.Message}");
                throw;
            }
        }
        private async Task<object> cargarrecargo(DBContext _context, int codigoNR)
        {
            var recargos = await _context.verecargoremi.Where(i => i.codremision == codigoNR)
                .Join(_context.verecargo,
                r => r.codrecargo,
                v => v.codigo,
                (r,v) => new {r,v})
                .Select(i => new
                {
                    codrecargo = i.r.codrecargo,
                    descripcion = i.v.descripcion,
                    porcen = i.r.porcen,
                    monto = i.r.monto,
                    moneda = i.r.moneda,
                    montodoc = i.r.montodoc,
                    codcobranza = i.r.codcobranza
                })
                .ToListAsync();
            return recargos;
        }
        private async Task<object> cargardesextra(DBContext _context, int codigoNR)
        {
            var descuentos = await _context.vedesextraremi
                       .Join(_context.vedesextra,
                       p => p.coddesextra,
                       e => e.codigo,
                       (p, e) => new { p, e })
                       .Where(i => i.p.codremision == codigoNR)
                       .Select(i => new
                       {
                           coddesextra = i.p.coddesextra,
                           descripcion = i.e.descripcion,
                           porcen = i.p.porcen,
                           montodoc = i.p.montodoc,
                           codcobranza = 0,
                           codcobranza_contado = 0,
                           codanticipo = 0,
                       })
                       .ToListAsync();
            return descuentos;
        }
        private async Task<object> cargariva(DBContext _context, int codigoNR)
        {
            var ivaNotaRem = await _context.veremision_iva.Where(i => i.codremision == codigoNR).OrderBy(i => i.porceniva).ToListAsync();
            return ivaNotaRem;
        }

        private async Task<object> mostrardetalle(DBContext _context, int codigoNR)
        {
            var detalle = await _context.veremision1.Where(i => i.codremision == codigoNR)
                .Join(_context.initem,
                        p => p.coditem,
                        i => i.codigo,
                        (p, i) => new { p, i })
                .Select(i => new
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
                .ToListAsync();
            return detalle;
        }

        [HttpGet]
        [Route("anularNR/{userConn}/{codRemision}")]
        public async Task<object> anularNR(string userConn, int codRemision)
        {
            try
            {
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var datosRemision = await _context.veremision.Where(i => i.codigo == codRemision).Select(i => new
                    {
                        i.codalmacen,
                        i.fecha,
                        i.anulada,
                        i.transferida,
                        i.id,
                        i.numeroid,
                        i.obs,
                        i.codproforma
                    }).FirstOrDefaultAsync();
                    if (datosRemision == null)
                    {
                        return BadRequest(new { resp = "No se encontraron datos con el codigo de Nota de Remision Proporcionado, consulte al administrador de Sistema" });
                    }
                    if (await restricciones.ValidarModifDocAntesInventario(_context, datosRemision.codalmacen, datosRemision.fecha))
                    {
                        // nada
                    }
                    else
                    {
                        return BadRequest(new { resp = "No puede modificar datos anteriores al ultimo inventario, Para eso necesita una autorizacion especial." });
                        // aca debe llamar a autorizacion especial  // verificar con Marvin
                    }

                    if (datosRemision.anulada)
                    {
                        return BadRequest(new { resp = "Esta Nota de Remision ya esta Anulada." });
                    }
                    if (await ventas.RemisionEstaPagadaEnParte(_context,codRemision) || await ventas.Remision_Contado_Contra_Entrega_Esta_Pagada_2(_context,codRemision))
                    {
                        return BadRequest(new { resp = "Esta Nota de Remision tiene un monto pagado, no puede ser Anulada. Para anularla anule el/los documento/s al cual fue transferido. " + await ventas.facturas_de_una_nr(_context, codRemision) });
                    }
                    if (datosRemision.transferida)
                    {
                        return BadRequest(new { resp = "Esta Nota de Remision ya fue transferida, no puede ser Anulada. Para anularla anule el/los documento/s al cual fue transferido. " + await ventas.facturas_de_una_nr(_context, codRemision) });
                    }
                    // verificar si es nota de reversion
                    if (await ventas.remision_es_reversion_pp(_context,datosRemision.id, datosRemision.numeroid))
                    {
                        return BadRequest(new { resp = "La nota de remision es reversion de: " + datosRemision.obs + "para anular debe ingresar el permiso especial." });
                        // aca debe llamar a autorizacion especial  // verificar con Marvin
                    }
                    if (await ventas.proforma_es_complementaria(_context, datosRemision.codproforma ?? 0))
                    {
                        var lista = await ventas.lista_PFNR_complementarias(_context, datosRemision.codproforma ?? 0);
                        // anular complementaria
                        //anularcomplementaria()
                    }
                    else
                    {
                        // anular normal
                        await anularnormal(_context,codRemision,datosRemision.codproforma ?? 0);
                    }
                    return Ok("Pendiente");

                }

            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
                throw;
            }
        }

        private async Task<string> anularnormal(DBContext _context, int codigoNR, int codigoPF)
        {
            if (await ventas.revertirpagos(_context, codigoNR, 4))
            {
                // si era complementaria revertir pagos de su compleentos y
                // hacerles nuevos planes de pago
                if (codigoPF != 0)
                {
                    var proforma = await _context.veproforma.Where(p => p.codigo == codigoPF).FirstOrDefaultAsync();

                    if (proforma != null)
                    {
                        proforma.transferida = false;
                        var confirmacion = await _context.SaveChangesAsync();
                        if (confirmacion > 0)
                        {
                            // Se actualizaron registros en la base de datos
                            // Aqui no se quitara el codigod eproforma para tener el enlace
                            // If sia_DAL.Datos.Instancia.EjecutarComando("UPDATE veremision SET anulada=1, fecha_anulacion='" & sia_DAL.Datos.Instancia.FechaISO(sia_funciones.Funciones.Instancia.fecha_del_servidor()) & "', codproforma=0 WHERE codigo= " + codigo.Text) Then


                        }
                        else
                        {
                            return "No se pudo Anular esta Nota de Remision.";
                        }
                    }
                }
                else
                {

                }
            }
            else
            {
                return "No se pudo Anular esta Nota de Remision. Ya que no se pudo revertir los pagos hechos a la misma.";
            }
            return "ok";
        }


    }
}
