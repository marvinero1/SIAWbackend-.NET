using MessagePack;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Polly.Caching;
using SIAW.Controllers.ventas.modificacion;
using siaw_DBContext.Data;
using siaw_DBContext.Models;
using siaw_DBContext.Models_Extra;
using siaw_funciones;
using System.Runtime.Intrinsics.Arm;
using static siaw_funciones.Validar_Vta;

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
        private readonly siaw_funciones.Items items = new siaw_funciones.Items();
        private readonly siaw_funciones.SIAT siat = new siaw_funciones.SIAT();
        private readonly siaw_funciones.Validar_Vta validar_vta = new siaw_funciones.Validar_Vta();

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
                    int codvendedor = await ventas.VendedorClave(_context, txtclave);
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
                    int codtarifadefect = await configuracion.usr_codtarifa(_context, usuario);
                    int coddescuentodefect = await configuracion.usr_coddescuento(_context, usuario);

                    int codtipopago = await configuracion.parametros_ctasporcobrar_tipopago(_context, codempresa);
                    string codtipopagodescripcion = await nombres.nombretipopago(_context, codtipopago);

                    string idcuenta = await configuracion.usr_idcuenta(_context, usuario);
                    string idcuentadescripcion = await nombres.nombrecuenta_fondos(_context, idcuenta);

                    string codcliente = await configuracion.usr_codcliente(_context, usuario);
                    string codclientedescripcion = await nombres.nombrecliente(_context, codcliente);

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
                    var cabecera = await _context.veproforma.Where(i => i.id == idProf && i.numeroid == nroIdPof).Select(i => new
                    {
                        codigo = i.codigo,
                        ids_proforma = i.id,
                        nro_id_proforma = i.numeroid,
                        codalmacen = i.codalmacen,
                        codcliente = i.codcliente,
                        nomcliente = i.nomcliente,
                        nit = i.nit,
                        complemento_ci = i.complemento_ci,
                        email = i.email,
                        codmoneda = i.codmoneda,
                        tipopago = 0,
                        tdc = i.tdc,
                        total = i.total,
                        subtotal = i.subtotal,
                        transporte = i.transporte,
                        fletepor = i.fletepor,
                        direccion = i.direccion,
                        iva = i.iva,
                        descuentos = i.descuentos,
                        recargos = i.recargos,

                    }).FirstOrDefaultAsync();
                    if (cabecera == null)
                    {
                        return BadRequest(new { resp = "La proforma " + idProf + "-" + nroIdPof + " no fue encontrada." });
                    }
                    // muestra todos los datos del registro actual en las casilla
                    string codclientedescripcion = await nombres.nombrecliente(_context, cabecera.codcliente);
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

        private async Task<List<recargosData>> cargarrecargoproforma(DBContext _context, int codigo)
        {
            var registros = await _context.verecargoprof.Where(i => i.codproforma == codigo).Select(i => new recargosData
            {
                codrecargo = i.codrecargo,
                descripcion = "",
                porcen = i.porcen,
                monto = i.monto,
                moneda = i.moneda,
                montodoc = i.montodoc,
                codcobranza = i.codcobranza ?? 0
            }).ToListAsync();
            return registros;
        }
        private async Task<List<descuentosData>> cargardesextraproforma(DBContext _context, int codigo)
        {
            var descuentosExtra = await _context.vedesextraprof
                        .Join(_context.vedesextra,
                        p => p.coddesextra,
                        e => e.codigo,
                        (p, e) => new { p, e })
                        .Where(i => i.p.codproforma == codigo)
                        .Select(i => new descuentosData
                        {
                            codproforma = i.p.codproforma,
                            coddesextra = i.p.coddesextra,
                            descripcion = i.e.descripcion,
                            porcen = i.p.porcen,
                            montodoc = i.p.montodoc,
                            codcobranza = i.p.codcobranza ?? 0,
                            codcobranza_contado = i.p.codcobranza_contado ?? 0,
                            codanticipo = i.p.codanticipo ?? 0,
                        })
                        .ToListAsync();


            return descuentosExtra;
        }
        private async Task<List<ivaData>> cargarivaproforma(DBContext _context, int codigo)
        {
            var registros = await _context.veproforma_iva.Where(i => i.codproforma == codigo).Select(i => new ivaData
            {
                codfactura = 0,
                porceniva = i.porceniva,
                total = i.total ?? 0,
                porcenbr = i.porcenbr ?? 0,
                br = i.br ?? 0,
                iva = i.iva ?? 0
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
                    var cabecera = await _context.vefactura.Where(i => i.id == idFact && i.numeroid == nroIdFact).Select(i => new
                    {
                        codigo = i.codigo,
                        ids_proforma = "",
                        nro_id_proforma = 0,
                        codalmacen = i.codalmacen,
                        codcliente = i.codcliente,
                        nomcliente = i.nomcliente,
                        nit = i.nit,
                        complemento_ci = i.complemento_ci,
                        email = i.email,
                        codmoneda = i.codmoneda,
                        tipopago = i.tipopago,
                        tdc = i.tdc,
                        total = i.total,
                        subtotal = i.subtotal,
                        transporte = i.transporte,
                        fletepor = i.fletepor,
                        direccion = i.direccion,
                        iva = i.iva,
                        descuentos = i.descuentos,
                        recargos = i.recargos,

                    }).FirstOrDefaultAsync();
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


        private async Task<List<recargosData>> cargarrecargo(DBContext _context, int codigo)
        {
            var registros = await _context.verecargofact.Where(i => i.codfactura == codigo).Select(i => new recargosData
            {
                codrecargo = i.codrecargo,
                descripcion = "",
                porcen = i.porcen,
                monto = i.monto ?? 0,
                moneda = i.moneda,
                montodoc = i.montodoc,
                codcobranza = i.codcobranza ?? 0
            }).ToListAsync();
            return registros;
        }
        private async Task<List<descuentosData>> cargardesextra(DBContext _context, int codigo)
        {
            var descuentosExtra = await _context.vedesextrafact
                        .Join(_context.vedesextra,
                        p => p.coddesextra,
                        e => e.codigo,
                        (p, e) => new { p, e })
                        .Where(i => i.p.codfactura == codigo)
                        .Select(i => new descuentosData
                        {
                            codproforma = 0,
                            coddesextra = i.p.coddesextra,
                            descripcion = i.e.descripcion,
                            porcen = i.p.porcen,
                            montodoc = i.p.montodoc,
                            codcobranza = 0, //i.p.codcobranza,
                            codcobranza_contado = 0, //i.p.codcobranza_contado,
                            codanticipo = 0 //i.p.codanticipo,
                        })
                        .ToListAsync();
            return descuentosExtra;
        }
        private async Task<List<ivaData>> cargariva(DBContext _context, int codigo)
        {
            var registros = await _context.vefactura_iva.Where(i => i.codfactura == codigo).OrderBy(i => i.porceniva).Select(i => new ivaData
            {
                codfactura = i.codfactura ?? 0,
                porceniva = i.porceniva ?? 0,
                total = i.total ?? 0,
                porcenbr = i.porcenbr ?? 0,
                br = i.br ?? 0,
                iva = i.iva ?? 0
            }).ToListAsync();
            return registros;
        }






        //////////////////////////////////////////////////////////////////////////
        //// TOTALES Y SUBTOTALES
        //////////////////////////////////////////////////////////////////////////

        // RUTAS CONTROLADORES

        [HttpPost]
        [Route("sumas_Subtotal/{userConn}/{codempresa}/{usuario}")]
        public async Task<object> sumas_Subtotal(string userConn, string codempresa, string usuario, List<dataDetalleFactura> tabla_detalle)
        {
            if (tabla_detalle.Count() < 1)
            {
                return BadRequest(new { resp = "No se esta recibiendo ningun dato, verifique esta situación." });
            }
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                double tot = (double)tabla_detalle.Sum(i => (i.cantidad * i.preciolista));
                double totlinea = (double)tabla_detalle.Sum(i => (i.cantidad * i.preciodesc));
                double subtotal = (double)tabla_detalle.Sum(i => i.total);

                // para el desgloce
                // sacar precios
                var descuentos = tabla_detalle
                    .GroupBy(obj => obj.coddescuento)
                    .Select(grp => grp.First())
                    .Select(i => i.coddescuento)
                    .ToList();

                // sacartotales
                List<double> totales = new List<double>();

                for (int i = 0; i < descuentos.Count(); i++)
                {
                    double total = (double)tabla_detalle
                        .Where(row => row.coddescuento == descuentos[i])
                    .Sum(row => row.cantidad * (row.preciodesc - row.precioneto));

                    totales.Add(total);
                }
                var desglose = totales.Select((i, index) => new
                {
                    total = i,
                    descuento = descuentos[index]
                }).ToList();

                return Ok(new
                {
                    resul = tabla_detalle,
                    a = tot,
                    b = tot - totlinea,
                    c = totlinea,
                    d = totlinea - subtotal,
                    e = subtotal,
                    desgloce = desglose
                });
            }

        }




        [HttpPost]
        [Route("recarcularRecargosFact/{userConn}/{codempresa}/{descuentos}/{codcliente}/{codmoneda}")]
        public async Task<object> recarcularRecargosFact(string userConn, string codempresa, double descuentos, string codcliente, string codmoneda, RequestTotalizar RequestRecargos)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                List<dataDetalleFactura> tabla_detalle = RequestRecargos.detalleItems;
                var tablarecargos = RequestRecargos.recargosTabla;

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var result = await versubtotal(_context, tabla_detalle);
                    double subtotal = result.st;
                    double peso = result.peso;
                    var respRecargo = await verrecargos(_context, subtotal, codmoneda, tablarecargos);
                    double recargo = respRecargo.total;
                    tablarecargos = respRecargo.tablarecargos;

                    var total = await vertotal(_context, subtotal, recargo, descuentos, codcliente, tabla_detalle);
                    return new
                    {
                        subtotal = subtotal,
                        peso = peso,
                        recargo = recargo,
                        total = total.TotalGen,
                        tablaRecargos = tablarecargos
                    };
                }
            }
            catch (Exception ex)
            {
                return Problem("Error en el servidor al recalcular recargos: " + ex.Message);
                throw;
            }
        }

        //[Authorize]
        [HttpPost]
        [Route("recarcularDescuentosFact/{userConn}/{codempresa}/{recargos}/{cmbtipo_complementopf}/{codcliente_real}/{codmoneda}")]
        public async Task<object> recarcularDescuentosFact(string userConn, string codempresa, double recargos, int cmbtipo_complementopf, string codcliente_real, string codmoneda, string codcliente, string nit, RequestTotalizar RequestDescuentos)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                List<dataDetalleFactura> tabla_detalle = RequestDescuentos.detalleItems;
                List<descuentosData>? tabladescuentos = RequestDescuentos.descuentosTabla;


                List<itemDataMatriz> data = tabla_detalle.Select(x => new itemDataMatriz
                {
                    coditem = x.coditem,
                    descripcion = x.descripcion,
                    medida = x.medida,
                    udm = x.udm,
                    porceniva = (double)x.porceniva,
                    niveldesc = x.niveldesc,
                    cantidad = (double)x.cantidad,
                    codtarifa = x.codtarifa,
                    coddescuento = x.coddescuento,
                    precioneto = (double)x.precioneto,
                    preciodesc = (double)x.preciodesc,
                    preciolista = (double)x.preciolista,
                    total = (double)x.total,
                    cumple = x.cumple,
                    monto_descto = 0,
                    subtotal_descto_extra = 0,

                }).ToList();

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (tabladescuentos != null)
                    {
                        /////////////////////////////////////////////////////////////////
                        // verificar que no haya mas de un tipo de precio en el detalle
                        DatosDocVta DVTA = new DatosDocVta(); // dentro de la funcion Validar_Precios_Del_Documento, no se hace nada con DVTA por lo que mandamos asi vacio.
                        ResultadoValidacion misrep = await validar_vta.Validar_Precios_Del_Documento(_context, DVTA, data, codempresa);
                        if (misrep.resultado == false)
                        {
                            return BadRequest(new { resp = misrep.observacion + " - " + misrep.obsdetalle });
                        }

                        // obtiene el precio unico del documento
                        int _CODTARIFA = await validar_vta.Precio_Unico_Del_Documento(_context, data, codempresa);

                        if (_CODTARIFA == -1)
                        {
                            return BadRequest(new { resp = "Verifique que en detalle del pedido solo exista un tipo de precio!!!" });
                        }
                        /////////////////////////////////////////////////////////////////



                        var result = await versubtotal(_context, tabla_detalle);
                        double subtotal = result.st;
                        double peso = result.peso;
                        // var respDescuento = await verdesextra(_context, codempresa, veproforma.nit, veproforma.codmoneda, cmbtipo_complementopf, veproforma.idpf_complemento, veproforma.nroidpf_complemento ?? 0, subtotal, veproforma.fecha, tabladescuentos, tabla_detalle);
                        var respDescuento = await verdesextra(_context, codempresa, subtotal, codmoneda, codcliente, nit, data, tabladescuentos);

                        double descuento = respDescuento.respdescuentos;
                        tabladescuentos = respDescuento.tabladescuentos;

                        var total = await vertotal(_context, subtotal, recargos, descuento, codcliente, tabla_detalle);
                        return new
                        {
                            subtotal = subtotal,
                            peso = peso,
                            descuento = descuento,
                            total = total.TotalGen,
                            tablaDescuentos = tabladescuentos
                        };
                    }
                    return BadRequest(new { resp = "No se seleccionaron descuentos extras" });
                }
            }
            catch (Exception ex)
            {
                return Problem("Error en el servidor al recalcular descuentos: " + ex.Message);
                throw;
            }
        }



        [HttpPost]
        [QueueFilter(1)] // Limitar a 1 solicitud concurrente
        [Route("totabilizarFact/{userConn}/{codempresa}/{usuario}")]
        public async Task<object> totabilizarFact(string userConn, string codempresa, string usuario, string codmoneda, string codcliente, int codalmacen, string nit, RequestTotalizar requestTotalizar)
        {
            List<dataDetalleFactura> tabla_detalle = requestTotalizar.detalleItems;
            List<descuentosData>? tabladescuentos = requestTotalizar.descuentosTabla;
            List<recargosData>? tablarecargos = requestTotalizar.recargosTabla;
            if (tabla_detalle.Count() <= 0)
            {
                return BadRequest(new { resp = "No hay ningun item en su documento!!!" });
            }
            try
            {
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);
                
                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    DateTime fecha = await funciones.FechaDelServidor(_context);
                    fecha = fecha.Date;
                    List <itemDataMatriz> data = tabla_detalle.Select(x => new itemDataMatriz
                    {
                        coditem = x.coditem,
                        descripcion = x.descripcion,
                        medida = x.medida,
                        udm = x.udm,
                        porceniva = (double)x.porceniva,
                        niveldesc = x.niveldesc,
                        cantidad = (double)x.cantidad,
                        codtarifa = x.codtarifa,
                        coddescuento = x.coddescuento,
                        precioneto = (double)x.precioneto,
                        preciodesc = (double)x.preciodesc,
                        preciolista = (double)x.preciolista,
                        total = (double)x.total,
                        cumple = x.cumple,
                        monto_descto = 0,
                        subtotal_descto_extra = 0,

                    }).ToList();

                    ////////////////////////////////////////////////////////////
                    // verificar los precios permitidos al usuario
                    string cadena_precios_no_autorizados_al_us = await validar_vta.Validar_Precios_Permitidos_Usuario(_context, usuario, data);
                    if (cadena_precios_no_autorizados_al_us.Trim().Length > 0)
                    {
                        return BadRequest(new { resp = "El documento tiene items a precio(s): " + cadena_precios_no_autorizados_al_us + " los cuales no estan asignados al usuario " + usuario + ", verifique esta situacion!!!" });
                    }
                    ////////////////////////////////////////////////////////////
                    foreach (var reg in tabla_detalle)
                    {
                        reg.cantidad = Math.Round(reg.cantidad, 2, MidpointRounding.AwayFromZero);

                        // MODIFICADO EN FECHA:  09-01-2021
                        reg.niveldesc = await cliente.niveldesccliente(_context, codcliente, reg.coditem, reg.codtarifa, "ACTUAL");
                        if (reg.codtarifa > 0)
                        {
                            string monBaseTarif = await ventas.monedabasetarifa(_context, reg.codtarifa);
                            reg.preciolista = await tipocambio._conversion(_context, codmoneda, monBaseTarif, fecha, (decimal)(await ventas.preciodelistaitem(_context, reg.codtarifa, reg.coditem)));

                            // si hay descuento
                            reg.preciodesc = await siat.Redondeo_Decimales_SIA_5_decimales_SQL(_context, (await tipocambio._conversion(_context, codmoneda, monBaseTarif, fecha, (decimal)(await ventas.preciocliente(_context, codcliente, codalmacen, reg.codtarifa, reg.coditem, "NO", reg.niveldesc, "ACTUAL")))));
                            reg.precioneto = await siat.Redondeo_Decimales_SIA_5_decimales_SQL(_context, (await tipocambio._conversion(_context, codmoneda, monBaseTarif, fecha, (decimal)(await ventas.preciocondescitem(_context, codcliente, codalmacen, reg.codtarifa, reg.coditem, reg.coddescuento, "NO", reg.niveldesc, "ACTUAL")))));

                            if (reg.cantidad > 0)
                            {
                                reg.total = await siat.Redondeo_Decimales_SIA_5_decimales_SQL(_context, (reg.cantidad * reg.precioneto));
                            }
                            else
                            {
                                reg.total = 0;
                            }
                        }
                        else
                        {
                            reg.preciolista = 0;
                            reg.preciodesc = 0;
                            reg.precioneto = 0;
                            reg.total = 0;
                        }
                    }


                    var resultSubTotal = await versubtotal(_context, tabla_detalle);
                    var resultDesc = await verdesextra(_context, codempresa, resultSubTotal.st, codmoneda, codcliente, nit, data, tabladescuentos);
                    var resultRec = await verrecargos(_context, resultSubTotal.st, codmoneda, tablarecargos);
                    var resultTotal = await vertotal(_context, resultSubTotal.st, resultRec.total, resultDesc.respdescuentos, codcliente, tabla_detalle);
                    return Ok(new
                    {
                        tabla_detalle,
                        resultSubTotal,
                        resultDesc,
                        resultRec,
                        resultTotal,
                    });
                }

            }
            catch (Exception ex)
            {
                return Problem("Error en el servidor al recalcular descuentos: " + ex.Message);
                throw;
            }
        }


        // FUNCIONES PRIVATE PARA LAS OPERACIONES

        private async Task<(double st, double peso)> versubtotal(DBContext _context, List<dataDetalleFactura> tabla_detalle)
        {
            // filtro de codigos de items
            tabla_detalle = tabla_detalle.Where(item => item.coditem != null && item.coditem.Length >= 8).ToList();
            // calculo subtotal
            double peso = 0;
            double st = 0;

            foreach (var reg in tabla_detalle)
            {
                st = st + (double)reg.total;
                peso = peso + (await items.itempeso(_context, reg.coditem)) * (double)reg.cantidad;
            }

            // desde 08/01/2023 redondear el resultado a dos decimales con el SQLServer
            // REVISAR SI HAY OTRO MODO NO DA CON LINQ.
            st = (double)await siat.Redondeo_Decimales_SIA_2_decimales_SQL(_context, (double)st);
            return (st, peso);
        }
        private async Task<(double total, List<recargosData>? tablarecargos)> verrecargos(DBContext _context, double subtotal, string codmoneda, List<recargosData>? tablarecargos)
        {
            DateTime fecha = await funciones.FechaDelServidor(_context);
            double total = 0;
            if (tablarecargos == null)
            {
                return (0, tablarecargos);
            }
            foreach (var reg in tablarecargos)
            {
                if (reg.porcen > 0)
                {
                    reg.montodoc = (decimal)(subtotal / 100) * reg.porcen;
                }
                else
                {
                    reg.montodoc = await tipocambio._conversion(_context, codmoneda, reg.moneda, fecha.Date, reg.monto);
                }
                reg.montodoc = await siat.Redondeo_Decimales_SIA_2_decimales_SQL(_context, (double)reg.montodoc);
                total = total + (double)reg.montodoc;
            }
            total = (double)await siat.Redondeo_Decimales_SIA_2_decimales_SQL(_context, total);
            return (total, tablarecargos);
        }
        private async Task<(double respdescuentos, List<descuentosData> tabladescuentos)> verdesextra(DBContext _context, string codempresa, double subtotal, string codmoneda, string codcliente, string nit, List<itemDataMatriz> dataDetalle, List<descuentosData> tabladescuentos)
        {
            int coddesextra_depositos = await configuracion.emp_coddesextra_x_deposito(_context, codempresa);
            DateTime fecha = await funciones.FechaDelServidor(_context);
            //calcular el monto  de descuento segun el porcentaje
            ////////////////////////////////////////////////////////////////////////////////
            //primero calcular los montos de los que se aplican en el detalle o son
            //diferenciados por item
            ////////////////////////////////////////////////////////////////////////////////
            
            foreach (var reg in tabladescuentos)
            {
                // verifica si el descuento es diferenciado por item
                if (await ventas.DescuentoExtra_Diferenciado_x_item(_context, reg.coddesextra))
                {
                    var resp = await ventas.DescuentoExtra_CalcularMonto(_context, reg.coddesextra, dataDetalle, codcliente, nit);
                    double monto_desc = resp.resultado;
                    reg.montodoc = await siat.Redondeo_Decimales_SIA_2_decimales_SQL(_context, monto_desc);
                }
            }

            ////////////////////////////////////////////////////////////////////////////////
            // los que se aplican en el SUBTOTAL
            ////////////////////////////////////////////////////////////////////////////////
            foreach (var reg in tabladescuentos)
            {
                if (!await ventas.DescuentoExtra_Diferenciado_x_item(_context, reg.coddesextra))
                {
                    if (reg.aplicacion == "SUBTOTAL")
                    {
                        if (coddesextra_depositos == reg.coddesextra)
                        {
                            // el monto por descuento de deposito ya esta calculado
                            // pero se debe verificar si este monto de este descuento esta en la misma moneda que la proforma
                            if (reg.codmoneda != codmoneda)
                            {
                                double monto_cambio = (double)await tipocambio._conversion(_context, codmoneda, reg.codmoneda, fecha, reg.montodoc);
                                reg.montodoc = await siat.Redondeo_Decimales_SIA_2_decimales_SQL(_context, monto_cambio);
                                reg.codmoneda = codmoneda;
                            }
                        }
                        else
                        {
                            // este descuento se aplica sobre el subtotal de la venta
                            reg.montodoc = (decimal)(subtotal / 100) * reg.porcen;
                            reg.montodoc = await siat.Redondeo_Decimales_SIA_2_decimales_SQL(_context, (double)reg.montodoc);
                        }
                    }
                }
            }
            // totalizar los descuentos que se aplicar al subtotal
            double total_desctos1 = 0;
            foreach (var reg in tabladescuentos)
            {
                if (reg.aplicacion == "SUBTOTAL")
                {
                    total_desctos1 += (double)reg.montodoc;
                }
            }
            total_desctos1 = (double)await siat.Redondeo_Decimales_SIA_2_decimales_SQL(_context, (double)total_desctos1);

            ////////////////////////////////////////////////////////////////////////////////
            // los que se aplican en el TOTAL
            ////////////////////////////////////////////////////////////////////////////////
            double total_preliminar = subtotal - total_desctos1;
            foreach (var reg in tabladescuentos)
            {
                if (!await ventas.DescuentoExtra_Diferenciado_x_item(_context, reg.coddesextra))
                {
                    if (reg.aplicacion == "TOTAL")
                    {
                        if (coddesextra_depositos == reg.coddesextra)
                        {
                            //el descuento se aplica sobre el monto del deposito
                            //ya esta calculado
                        }
                        else
                        {
                            //este descuento se aplica sobre el subtotal de la venta
                            reg.montodoc = await siat.Redondeo_Decimales_SIA_2_decimales_SQL(_context, (double)((total_preliminar / 100) * (double)reg.porcen));
                        }
                    }
                }
            }
            double total_desctos2 = 0;
            foreach (var reg in tabladescuentos)
            {
                if (reg.aplicacion == "TOTAL")
                {
                    total_desctos2 += (double)reg.montodoc;
                }
            }
            double totalDescuentos = (total_desctos1 + total_desctos2);
            return (totalDescuentos, tabladescuentos);
            //return totalDescuentos;
        }
        private async Task<(double totalIva, double TotalGen, List<veproforma_iva> tablaiva)> vertotal(DBContext _context, double subtotal, double recargos, double descuentos, string codcliente, List<dataDetalleFactura> tabladetalle)
        {
            double suma = subtotal + recargos - descuentos;
            double totalIva = 0;
            if (suma < 0)
            {
                suma = 0;
            }
            List<veproforma_iva> tablaiva = new List<veproforma_iva>();
            if (await cliente.DiscriminaIVA(_context, codcliente))
            {
                // Calculo de ivas
                tablaiva = await CalcularTablaIVA(subtotal, recargos, descuentos, tabladetalle);
                //fin calculo ivas
                totalIva = await veriva(tablaiva);
                suma = suma + totalIva;
            }
            

            return (totalIva, suma, tablaiva);
        }

        private async Task<List<veproforma_iva>> CalcularTablaIVA(double subtotal, double recargos, double descuentos, List<dataDetalleFactura> tabladetalle)
        {
            List<clsDobleDoble> lista = new List<clsDobleDoble>();

            foreach (var reg in tabladetalle)
            {
                bool encontro = false;
                foreach (var item in lista)
                {
                    if (item.dobleA == (double)reg.porceniva)
                    {
                        encontro = true;
                        item.dobleB = item.dobleB + (double)reg.total;
                        break;
                    }
                }
                if (!encontro)
                {
                    clsDobleDoble newReg = new clsDobleDoble();
                    newReg.dobleA = (double)reg.porceniva;
                    newReg.dobleB = (double)reg.total;
                    lista.Add(newReg);
                }
            }
            // pasar a tabla
            var tablaiva = lista.Select(i => new veproforma_iva
            {
                codproforma = 0,
                porceniva = (decimal)i.dobleA,
                total = (decimal)i.dobleB,
                porcenbr = 0,
                br = 0,
                iva = 0
            }).ToList();

            //calcular porcentaje de br
            double porcenbr = 0;
            try
            {
                if (subtotal > 0)
                {
                    porcenbr = ((recargos - descuentos) * 100) / subtotal;
                }
            }
            catch (Exception)
            {
                porcenbr = 0;
            }
            //calcular en la tabla
            foreach (var reg in tablaiva)
            {
                reg.porcenbr = (decimal)porcenbr;
                reg.br = (reg.total / 100) * (decimal)porcenbr;
                reg.iva = ((reg.total + reg.br) / 100) * reg.porceniva;
            }
            return tablaiva;
        }
        private async Task<double> veriva(List<veproforma_iva> tablaiva)
        {
            var total = tablaiva.Sum(i => i.iva) ?? 0;
            return (double)total;
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
        public int codgrupomer { get; set; } = 0;
        public decimal peso { get; set; } = 0;
        public string codproducto_sin { get; set; } = "";
    }
    public class recargosData
    {
        public int codrecargo { get; set; } = 0;
        public string descripcion { get; set; } = "";
        public decimal porcen { get; set; } = 0;
        public decimal monto { get; set; } = 0;
        public string moneda { get; set; } = "";
        public decimal montodoc { get; set; } = 0;
        public int codcobranza { get; set; } = 0;
    }

    public class descuentosData
    {
        public int codproforma { get; set; } = 0;
        public int coddesextra { get; set; } = 0;
        public string descripcion { get; set; } = "";
        public decimal porcen { get; set; } = 0;
        public decimal montodoc { get; set; } = 0;
        public int codcobranza { get; set; } = 0;
        public int codcobranza_contado { get; set; } = 0;
        public int codanticipo { get; set; } = 0;

        public string aplicacion { get; set; } = "";
        public string codmoneda { get; set; } = "";
    }

    public class ivaData
    {
        public int codfactura { get; set; } = 0;
        public decimal porceniva { get; set; } = 0;
        public decimal total { get; set; } = 0;
        public decimal porcenbr { get; set; } = 0;
        public decimal br { get; set; } = 0;
        public decimal iva { get; set; } = 0;
    }

    public class RequestTotalizar
    {
        public List<dataDetalleFactura> detalleItems { get; set; }
        public List<recargosData>? recargosTabla { get; set; }
        public List<descuentosData>? descuentosTabla { get; set; }
    }
}
