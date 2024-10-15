using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIAW.Controllers.ventas.modificacion;
using siaw_DBContext.Data;
using siaw_DBContext.Models;
using siaw_funciones;

namespace SIAW.Controllers.ventas.transaccion
{
    [Route("api/venta/transac/[controller]")]
    [ApiController]
    public class docvefacturamos_cufdController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;

        private readonly siaw_funciones.Ventas ventas = new siaw_funciones.Ventas();
        private readonly siaw_funciones.Configuracion configuracion = new siaw_funciones.Configuracion();
        private readonly siaw_funciones.Documento documento = new siaw_funciones.Documento();
        private readonly siaw_funciones.TipoCambio tipocambio = new siaw_funciones.TipoCambio();
        private readonly siaw_funciones.Funciones funciones = new siaw_funciones.Funciones();

        private readonly Empresa empresa = new Empresa();
        private readonly Nombres nombres = new Nombres();
        private readonly Cliente cliente = new Cliente();

        private readonly string _controllerName = "docvefacturamos_cufdController";


        public docvefacturamos_cufdController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }

        [HttpGet]
        [Route("getCodVendedorbyPass/{userConn}/{txtclave}")]
        public async Task<ActionResult<IEnumerable<object>>> getCodVendedorbyPass(string userConn, string txtclave)
        {
            try
            {
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    int codvendedor = await ventas.VendedorClave(_context,txtclave);
                    if (codvendedor == -1)
                    {
                        return BadRequest(new
                        {
                            resp = "No se encontró un codigo de vendedor asociada a la clave ingresada."
                        });
                    }
                    return Ok(new
                    {
                        codvendedor
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
        [Route("getParametrosIniciales/{userConn}/{usuario}/{codempresa}")]
        public async Task<ActionResult<IEnumerable<object>>> getParametrosIniciales(string userConn, string usuario, string codempresa)
        {
            try
            {
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    string id = await configuracion.usr_idfactura(_context, usuario);
                    int numeroid = 0;
                    if (id != "")
                    {
                        numeroid = (await documento.ventasnumeroid(_context, id)) + 1;
                    }
                    DateTime fecha = await funciones.FechaDelServidor(_context);
                    // codvendedor.Text =  sia_compartidos.temporales.instancia.ucodvendedor
                    string codmoneda = await tipocambio.monedafact(_context, codempresa);
                    decimal tdc = await tipocambio._tipocambio(_context, await Empresa.monedabase(_context, codempresa), codmoneda, fecha);
                    int codalmacen = await configuracion.usr_codalmacen(_context, usuario);
                    int codtarifadefect = await configuracion.usr_codtarifa(_context,usuario);
                    int coddescuentodefect = await configuracion.usr_coddescuento(_context, usuario);

                    int codtipopago = await configuracion.parametros_ctasporcobrar_tipopago(_context,codempresa);
                    string codtipopagodescripcion = await nombres.nombretipopago(_context, codtipopago);

                    string idcuenta = await configuracion.usr_idcuenta(_context, usuario);
                    string idcuentadescripcion = await nombres.nombrecuenta_fondos(_context, idcuenta);

                    string codcliente = await configuracion.usr_codcliente(_context, usuario);
                    string codclientedescripcion = await nombres.nombrecliente(_context,codcliente);

                }
                return Ok();

            }
            catch (Exception ex)
            {
                return Problem($"Error en el servidor: {ex.Message}");
                throw;
            }
        }

        [HttpGet]
        [Route("transfProforma/{userConn}/{idProf}/{nroIdPof}")]
        public async Task<object> transfProforma(string userConn, string idProf, int nroIdPof)
        {
            try
            {
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var cabecera = await _context.veproforma.Where(i => i.id == idProf && i.numeroid == nroIdPof).FirstOrDefaultAsync();
                    if (cabecera == null)
                    {
                        return BadRequest(new { resp = "La proforma " + idProf + "-" + nroIdPof  + " no fue encontrada." });
                    }
                    // muestra todos los datos del registro actual en las casilla
                    string codclientedescripcion = await nombres.nombrecliente(_context,cabecera.codcliente);
                    string condicion = await cliente.CondicionFrenteAlIva(_context, cabecera.codcliente);

                    // cargar recargos
                    var recargos = await cargarrecargoproforma(_context, cabecera.codigo);
                    // cargar descuentos
                    var descuentos = await cargardesextraproforma(_context, cabecera.codigo);
                    // cargar IVA
                    var iva = await cargarivaproforma(_context, cabecera.codigo);

                    // mostrar detalle del documento actual
                    var detalle = await transferirdetalleproforma(_context, cabecera.codigo);

                    return Ok(new
                    {
                        codclientedescripcion,
                        condicion,

                        cabecera,
                        detalle,
                        recargos,
                        descuentos,
                        iva
                    });
                }
            }
            catch (Exception ex)
            {
                return Problem($"Error en el servidor: {ex.Message}");
                throw;
            }
        }

        private async Task<List<dataDetalleFactura>> transferirdetalleproforma(DBContext _context, int codigo)
        {
            var detalle = await _context.veproforma1.Where(i => i.codproforma == codigo)
                .Join(_context.initem,
                        p => p.coditem,
                        i => i.codigo,
                        (p, i) => new { p, i })
                .Select(i => new dataDetalleFactura
                {
                    coditem = i.p.coditem,
                    descripcion = i.i.descripcion,
                    medida = i.i.medida,
                    udm = i.p.udm,
                    porceniva = i.p.porceniva ?? 0,

                    niveldesc = i.p.niveldesc,
                    cantidad = i.p.cantidad,
                    codtarifa = i.p.codtarifa,
                    coddescuento = i.p.coddescuento,
                    precioneto = i.p.precioneto,

                    preciodesc = i.p.preciodesc ?? 0,
                    preciolista = i.p.preciolista,
                    total = i.p.total,
                    // cumple = i.p.c
                }).ToListAsync();
            return detalle;
        }

        private async Task<object> cargarrecargoproforma(DBContext _context, int codigo)
        {
            var registros = await _context.verecargoprof.Where(i => i.codproforma == codigo).Select(i => new
            {
                codrecargo = i.codrecargo,
                descripcion = "",
                porcen = i.porcen,
                monto = i.monto,
                moneda = i.moneda,
                montodoc = i.montodoc,
                codcobranza = i.codcobranza
            }).ToListAsync();
            return registros;
        }
        private async Task<object> cargardesextraproforma(DBContext _context, int codigo)
        {
            var descuentosExtra = await _context.vedesextraprof
                        .Join(_context.vedesextra,
                        p => p.coddesextra,
                        e => e.codigo,
                        (p, e) => new { p, e })
                        .Where(i => i.p.codproforma == codigo)
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
                        })
                        .ToListAsync();


            return descuentosExtra;
        }
        private async Task<object> cargarivaproforma(DBContext _context, int codigo)
        {
            var registros = await _context.veproforma_iva.Where(i => i.codproforma == codigo).Select(i => new
            {
                codfactura = 0,
                porceniva = i.porceniva,
                total = i.total,
                porcenbr = i.porcenbr,
                br = i.br,
                iva = i.iva
            }).ToListAsync();
            return registros;
        }


        [HttpGet]
        [Route("transfFactura/{userConn}/{idFact}/{nroIdFact}")]
        public async Task<object> transfFactura(string userConn, string idFact, int nroIdFact)
        {
            try
            {
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var cabecera = await _context.vefactura.Where(i => i.id == idFact && i.numeroid == nroIdFact).FirstOrDefaultAsync();
                    if (cabecera == null)
                    {
                        return BadRequest(new { resp = "La Factura " + idFact + "-" + nroIdFact + " no fue encontrada." });
                    }
                    // muestra todos los datos del registro actual en las casilla
                    string codclientedescripcion = await nombres.nombrecliente(_context, cabecera.codcliente);
                    string condicion = await cliente.CondicionFrenteAlIva(_context, cabecera.codcliente);

                    // cargar recargos
                    var recargos = await cargarrecargo(_context, cabecera.codigo);
                    // cargar descuentos
                    var descuentos = await cargardesextra(_context, cabecera.codigo);
                    // cargar IVA
                    var iva = await cargariva(_context, cabecera.codigo);

                    // mostrar detalle del documento actual
                    var detalle = await transferirdetallefactura(_context, cabecera.codigo);

                    return Ok(new
                    {
                        codclientedescripcion,
                        condicion,

                        cabecera,
                        detalle,
                        recargos,
                        descuentos,
                        iva
                    });
                }

            }
            catch (Exception ex)
            {
                return Problem($"Error en el servidor: {ex.Message}");
                throw;
            }
        }

        private async Task<List<dataDetalleFactura>> transferirdetallefactura(DBContext _context, int codigo)
        {
            var detalle = await _context.vefactura1.Where(i => i.codfactura == codigo)
                .Join(_context.initem,
                        p => p.coditem,
                        i => i.codigo,
                        (p, i) => new { p, i })
                .Select(i => new dataDetalleFactura
                {
                    coditem = i.p.coditem,
                    descripcion = i.i.descripcion,
                    medida = i.i.medida,
                    udm = i.p.udm,
                    porceniva = i.p.porceniva ?? 0,

                    niveldesc = i.p.niveldesc,
                    cantidad = i.p.cantidad,
                    codtarifa = i.p.codtarifa,
                    coddescuento = i.p.coddescuento,
                    precioneto = i.p.precioneto,

                    preciodesc = i.p.preciodesc ?? 0,
                    preciolista = i.p.preciolista,
                    total = i.p.total,
                    // cumple = i.p.c
                }).ToListAsync();
            return detalle;
        }


        private async Task<object> cargarrecargo(DBContext _context, int codigo)
        {
            var registros = await _context.verecargofact.Where(i => i.codfactura == codigo).Select(i => new
            {
                codrecargo = i.codrecargo,
                descripcion = "",
                porcen = i.porcen,
                monto = i.monto,
                moneda = i.moneda,
                montodoc = i.montodoc,
                codcobranza = i.codcobranza
            }).ToListAsync();
            return registros;
        }
        private async Task<object> cargardesextra(DBContext _context, int codigo)
        {
            var descuentosExtra = await _context.vedesextrafact
                        .Join(_context.vedesextra,
                        p => p.coddesextra,
                        e => e.codigo,
                        (p, e) => new { p, e })
                        .Where(i => i.p.codfactura == codigo)
                        .Select(i => new
                        {
                            codproforma = 0,
                            i.p.coddesextra,
                            descripcion = i.e.descripcion,
                            i.p.porcen,
                            i.p.montodoc,
                            codcobranza = 0, //i.p.codcobranza,
                            codcobranza_contado = 0, //i.p.codcobranza_contado,
                            codanticipo = 0 //i.p.codanticipo,
                        })
                        .ToListAsync();


            return descuentosExtra;
        }
        private async Task<object> cargariva(DBContext _context, int codigo)
        {
            var registros = await _context.vefactura_iva.Where(i => i.codfactura == codigo).OrderBy(i=> i.porceniva).Select(i => new
            {
                codfactura = i.codfactura,
                porceniva = i.porceniva,
                total = i.total,
                porcenbr = i.porcenbr,
                br = i.br,
                iva = i.iva
            }).ToListAsync();
            return registros;
        }



    }
    public class dataDetalleFactura
    {
        public int codfactura { get; set; } = 0;
        public string coditem { get; set; } = "";
        public string descripcion { get; set; } = "";
        public string medida { get; set; } = "";
        public string udm { get; set; } = "";
        public decimal porceniva { get; set; } = 0;

        public string niveldesc { get; set; } = "";
        public decimal cantidad { get; set; } = 0;
        public int codtarifa { get; set; } = 0;
        public int coddescuento { get; set; } = 0;
        public decimal precioneto { get; set; } = 0;

        public decimal preciodesc { get; set; } = 0;
        public decimal preciolista { get; set; } = 0;
        public decimal total { get; set; } = 0;
        public bool cumple { get; set; } = true;

        public decimal distdescuento { get; set; } = 0;
        public decimal distrecargo { get; set; } = 0;
        public decimal preciodist { get; set; } = 0;
        public decimal totaldist { get; set; } = 0;
        public string codaduana { get; set; } = "";
        public int codgrupomer {  get; set; } = 0;
        public decimal peso { get; set; } = 0;
        public string codproducto_sin { get; set; } = "";
    }

}
