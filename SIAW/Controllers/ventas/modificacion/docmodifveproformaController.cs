using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NuGet.Configuration;
//using SIAW.Data;
//using SIAW.Models;
//using SIAW.Models_Extra;
using siaw_DBContext.Data;
using siaw_DBContext.Models;
using siaw_DBContext.Models_Extra;
using System.Data;
using System.Drawing;
using System.Security.Policy;
using System.Text;
using System.Web.Http.Results;
using siaw_funciones;
using LibSIAVB;
using static siaw_funciones.Validar_Vta;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.CodeAnalysis.Differencing;
using static System.Net.Mime.MediaTypeNames;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Humanizer;
using System.Net;
using System.Drawing.Drawing2D;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Linq;
using System.Security.Cryptography;
using System.Xml;
using NuGet.Packaging;
using System.Xml.Linq;
using Humanizer;
using System.Globalization;

namespace SIAW.Controllers.ventas.modificacion
{
    [Route("api/venta/modif/[controller]")]
    [ApiController]
    public class docmodifveproformaController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        private readonly siaw_funciones.Configuracion configuracion = new siaw_funciones.Configuracion();
        private readonly siaw_funciones.Cliente cliente = new siaw_funciones.Cliente();
        private readonly Anticipos_Vta_Contado anticipos_vta_contado = new Anticipos_Vta_Contado();
        private readonly siaw_funciones.TipoCambio tipocambio = new siaw_funciones.TipoCambio();
        private readonly siaw_funciones.Nombres nombres = new siaw_funciones.Nombres();
        private readonly siaw_funciones.Empresa empresa = new siaw_funciones.Empresa();
        private readonly siaw_funciones.Almacen almacen = new siaw_funciones.Almacen();
        private readonly siaw_funciones.datosProforma datos_proforma = new siaw_funciones.datosProforma();
        private readonly siaw_funciones.Ventas ventas = new siaw_funciones.Ventas();
        private readonly Depositos_Cliente depositos_cliente = new Depositos_Cliente();
        private readonly Seguridad seguridad = new Seguridad();
        private readonly siaw_funciones.Validar_Vta validar_Vta = new siaw_funciones.Validar_Vta();
        private readonly siaw_funciones.Despachos despachos = new siaw_funciones.Despachos();
        private readonly siaw_funciones.Cobranzas cobranzas = new siaw_funciones.Cobranzas();

        private readonly Log log = new Log();
        private readonly string _controllerName = "docmodifveproformaController";



        public docmodifveproformaController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }

        [HttpGet]
        [Route("getUltiProfId/{userConn}/{usuario}")]
        public async Task<object> getUltiProfId(string userConn, string usuario)
        {
            try
            {
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    int codvendedor = await seguridad.usuario_es_vendedor(_context,usuario);
                    if (codvendedor > 0)
                    {
                        var ultimoRegistro = await _context.veproforma.Where(v => v.codvendedor == codvendedor)
                            .OrderByDescending(i => i.codigo)
                            .Select(i => new
                            {
                                i.codigo,
                                i.id,
                                i.numeroid
                            }).FirstOrDefaultAsync();
                        return Ok(ultimoRegistro);
                    }
                    else
                    {
                        var ultimoRegistro = await _context.veproforma.OrderByDescending(i => i.codigo)
                        .Select(i => new
                        {
                            i.codigo,
                            i.id,
                            i.numeroid
                        }).FirstOrDefaultAsync();
                        return Ok(ultimoRegistro);
                    }
                }
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
                throw;
            }
        }

        [HttpGet]
        [Route("getUltiProfId_Ant_no/{userConn}")]
        public async Task<object> getUltiProfId_Ant_no(string userConn)
        {
            try
            {
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var ultimoRegistro = await _context.veproforma.OrderByDescending(i => i.codigo)
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
        [Route("obtProfxModif/{userConn}/{idProforma}/{nroidProforma}/{usuario}")]
        public async Task<object> obtProfxModif(string userConn, string idProforma, int nroidProforma, string usuario)
        {
            try
            {
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    // obtener cabecera.
                    var cabecera = await _context.veproforma
                        .Where(i => i.id == idProforma && i.numeroid == nroidProforma)
                        .FirstOrDefaultAsync();

                    if (cabecera == null)
                    {
                        return BadRequest(new { resp = "No se encontró una proforma con los datos proporcionados, revise los datos" });
                    }

                    if (cabecera.tipo_complementopf == 0)
                    {
                        cabecera.tipo_complementopf = 3;
                    }
                    if (cabecera.tipo_complementopf == 1 || cabecera.tipo_complementopf == 2)
                    {
                        cabecera.tipo_complementopf = cabecera.tipo_complementopf - 1;
                    }


                    int codvendedorClienteProf = await ventas.Vendedor_de_Cliente_De_Proforma(_context, idProforma, nroidProforma);
                    if (! await seguridad.autorizado_vendedores(_context, usuario, codvendedorClienteProf, codvendedorClienteProf))
                    {
                        return BadRequest(new { resp = "No esta autorizado para ver esta información." });
                    }
                    

                    // obtener razon social de cliente
                    var codclientedescripcion = await cliente.Razonsocial(_context, cabecera.codcliente);
                    // obtener tipo cliente
                    var tipo_cliente = await cliente.Tipo_Cliente(_context, cabecera.codcliente);
                    // establecer ubicacion
                    if (cabecera.ubicacion == null || cabecera.ubicacion == "")
                    {
                        cabecera.ubicacion = "NSE";
                    }
                    // texto Confirmada 
                    string descConfirmada = "";
                    if ((cabecera.confirmada ?? false) == true)
                    {
                        descConfirmada = "CONFIRMADA " + cabecera.hora_confirmada + " " + (cabecera.fecha_confirmada ?? new DateTime(1900, 1, 1)).ToShortDateString();
                    }

                    string estadodoc = "";
                    if (cabecera.anulada)
                    {
                        estadodoc = "ANULADA";
                    }
                    else
                    {
                        if (cabecera.transferida)
                        {
                            estadodoc = "TRANSFERIDA";
                        }
                        else
                        {
                            if (cabecera.aprobada)
                            {
                                estadodoc = "APROBADA";
                            }
                            else
                            {
                                estadodoc = "NO APROBADA";
                            }
                        }
                    }

                    bool cliHabilitado = await cliente.clientehabilitado(_context, cabecera.codcliente);

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
                            cantidad = (double)i.p.cantidad,
                            udm = i.p.udm,
                            precioneto = (double)i.p.precioneto,
                            preciodesc = (double)(i.p.preciodesc ?? 0),
                            niveldesc = i.p.niveldesc,
                            preciolista = (double)i.p.preciolista,
                            codtarifa = i.p.codtarifa,
                            coddescuento = i.p.coddescuento,
                            total = (double)i.p.total,
                            // cantaut = i.p.cantaut,
                            // totalaut = i.p.totalaut,
                            // obs = i.p.obs,
                            porceniva = (double)(i.p.porceniva ?? 0),
                            cantidad_pedida = (double)(i.p.cantidad_pedida ?? 0),
                            // peso = i.p.peso,
                            nroitem = i.p.nroitem ?? 0,
                            // id = i.p.id,
                            porcen_mercaderia = 0,
                            porcendesc = 0
                        })
                        .ToListAsync();

                    

                    // obtener recargos
                    var recargos = await _context.verecargoprof.Where(i => i.codproforma == codProforma).ToListAsync();

                    // obtener descuentos
                    var descuentosExtra = await _context.vedesextraprof
                        .Join(_context.vedesextra,
                        p => p.coddesextra,
                        e => e.codigo,
                        (p, e) => new { p, e })
                        .Where(i => i.p.codproforma == codProforma)
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
                    var iva = await _context.veproforma_iva.Where(i => i.codproforma == codProforma).ToListAsync();
                    // veproforma etiqueta
                    var profEtiqueta = await _context.veproforma_etiqueta.Where(i => i.id_proforma == idProforma && i.nroid_proforma == nroidProforma).ToListAsync();
                    // veetiqueta proforma
                    var etiquetaProf = await _context.veetiqueta_proforma.Where(i => i.id == idProforma && i.numeroid == nroidProforma).ToListAsync();
                    // etiqueta de proforma si es venta casual
                    var veprofEtiqueta = await _context.veproforma_etiqueta.Where(i => i.id_proforma == idProforma && i.nroid_proforma == nroidProforma).FirstOrDefaultAsync();

                    // calcular el porcendesc
                    string codclienteReal = cabecera.codcliente;
                    if (veprofEtiqueta != null)
                    {
                        codclienteReal = veprofEtiqueta.codcliente_real;
                    }
                    foreach (var reg in detalle)
                    {
                        reg.porcendesc = (double)await cliente.porcendesccliente(_context, codclienteReal, reg.coditem, reg.codtarifa, cabecera.niveles_descuento);
                    }

                    // OBTENER ANTICIPOS
                    var dt_anticipo_pf = await anticipos_vta_contado.Anticipos_Aplicados_a_Proforma(_context, idProforma, nroidProforma);
                    foreach (var reg in dt_anticipo_pf)
                    {
                        reg.docanticipo = reg.id_anticipo + "-" + reg.nroid_anticipo;
                    }
                    //totalizar_anticipos_asignados()
                    string anticiposTot = await totalizar_anticipos_asignados(_context, cabecera.codmoneda, dt_anticipo_pf);

                    // obtener validaciones
                    var dtvalidar = await Recuperar_Validacion(_context, codProforma);

                    return Ok(new
                    {
                        codclientedescripcion,
                        tipo_cliente,
                        descConfirmada,
                        habilitado = cliHabilitado,
                        anticiposTot,
                        estadodoc,

                        cabecera = cabecera,
                        detalle = detalle,
                        descuentos = descuentosExtra,
                        recargos = recargos,
                        iva = iva,
                        profEtiqueta = profEtiqueta,
                        veprofEtiqueta = veprofEtiqueta,
                        etiquetaProf = etiquetaProf,
                        anticipos = dt_anticipo_pf,
                        detalleValida = dtvalidar
                    });
                }
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
                throw;
            }
        }

        private async Task<string> totalizar_anticipos_asignados(DBContext _context, string codmoneda, List<vedetalleanticipoProforma> dt_anticipo_pf)
        {
            double ttl_aplicado = 0;
            foreach (var reg in dt_anticipo_pf)
            {
                // Desde 14/12/2023 realizar la conversion del monto asignado segun la moneda del anticipo y proforma
                if (reg.codmoneda == codmoneda)
                {
                    ttl_aplicado += reg.monto;
                }
                else
                {
                    ttl_aplicado += (double)(await tipocambio._conversion(_context, codmoneda, reg.codmoneda, DateTime.Now.Date, (decimal)reg.monto));
                    ttl_aplicado = Math.Round(ttl_aplicado, 2);
                }
            }
            return ttl_aplicado.ToString("#,0.00", new System.Globalization.CultureInfo("en-US"));
        }
        private async Task<List<DataValidar>> Recuperar_Validacion(DBContext _context, int codigodoc)
        {
            // recuperar el detalle de la validacion
            var dtvalidar = await _context.veproforma_valida
                .Join(_context.vetipo_control_vtas,
                    p1 => p1.codcontrol,
                    p2 => p2.codcontrol,
                    (p1, p2) => new
                    {
                        p1,
                        p2
                    })
                .Where(joined => joined.p1.codproforma == codigodoc)
                .OrderBy(joined => joined.p2.orden)
                .Select(joined => new DataValidar
                {
                    codproforma = joined.p1.codproforma,
                    orden = joined.p2.orden,
                    codcontrol = joined.p1.codcontrol,
                    desc_grabar = "", // as desc_grabar
                    grabar = joined.p2.grabar,
                    grabar_aprobar = joined.p2.grabar_aprobar,
                    nit = joined.p1.nit,
                    nroitems = joined.p1.nroitems,
                    subtotal = joined.p1.subtotal,
                    recargos = joined.p1.recargos,
                    descuentos = joined.p1.descuentos,

                    total = joined.p1.total,
                    valido = joined.p1.valido,
                    observacion = joined.p1.observacion,
                    obsdetalle = joined.p1.obsdetalle,
                    codservicio = joined.p1.codservicio,
                    descservicio = "", // as descservicio
                    descripcion = joined.p2.descripcion,
                    datoa = joined.p1.datoa,
                    datob = joined.p1.datob,
                    clave_servicio = joined.p1.clave_servicio,
                    accion = "" // as accion
                })
                .ToListAsync();

            foreach (var reg in dtvalidar)
            {
                if (reg.codservicio != null && reg.codservicio > 0)
                {
                    reg.descservicio = await nombres.nombre_servicio(_context, reg.codservicio ?? 0);
                }
            }
            return dtvalidar;
        }


        //[Authorize]
        [HttpPut]
        [Route("anularProforma/{userConn}/{codProforma}/{usuario}/{codempresa}")]
        public async Task<object> anularProforma(string userConn, int codProforma, string usuario, string codempresa,RequestAnularProf requestAnularProf)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                var veproformaModif = await _context.veproforma.Where(i => i.codigo == codProforma)
                    .Select(i => new
                    {
                        i.anulada,
                        i.transferida,
                        i.aprobada,
                        i.id,
                        i.numeroid
                    }).FirstOrDefaultAsync();
                if (veproformaModif == null)
                {
                    return NotFound(new { resp = "No existe un registro con ese código" });
                }
                if (veproformaModif.anulada == true)
                {
                    return BadRequest(new { resp = "Esta Proforma ya esta Anulada." });
                }
                if (veproformaModif.transferida == true)
                {
                    return BadRequest(new { resp = "Esta Proforma ya fue transferida, no puede ser anulada. Para anularla anule el documento al cual fue transferida." });
                }
                if (veproformaModif.aprobada == true)
                {
                    return BadRequest(new { resp = "Esta Proforma esta aprobada por tanto no puede ser anulada, debe proceder a desaprobar!!!" });
                }


                if (await ventas.anular_proforma(_context,codProforma))
                {
                    await log.RegistrarEvento(_context, usuario, Log.Entidades.SW_Proforma, codProforma.ToString(), veproformaModif.id, veproformaModif.numeroid.ToString(), this._controllerName, "Anular", Log.TipoLog.Anulacion);
                    await despachos.eliminar_prof_de_despachos(_context, veproformaModif.id, veproformaModif.numeroid);
                    // Desde 03/04/2024 al anular una proforma se debe actualizar los saldos de los anticipos que tuviera enlazada la proforma
                    await Actualizar_saldos_anticipos(_context, codempresa, requestAnularProf.dt_anticipo_pf, requestAnularProf.dt_anticipo_pf_inicial);
                    return Ok(new { resp = "Se Anulo la Proforma con exito. " });  
                }
                return BadRequest(new { resp = "No se pudo Anular esta Proforma." });  
            }
        }


        //[Authorize]
        [HttpPost]
        [QueueFilter(1)] // Limitar a 1 solicitud concurrente
        [Route("guardarProforma/{userConn}/{codProforma}/{codempresa}/{paraAprobar}/{codcliente_real}")]
        public async Task<object> guardarProforma(string userConn, int codProforma, string codempresa, bool paraAprobar, string codcliente_real, SaveProformaCompleta datosProforma)
        {
            bool check_desclinea_segun_solicitud = false;  // de momento no se utiliza, si se llegara a utilizar, se debe pedir por ruta
            veproforma veproforma = datosProforma.veproforma;
            List<veproforma1> veproforma1 = datosProforma.veproforma1;
            if (datosProforma.veetiqueta_proforma == null)
            {
                return BadRequest(new { resp = "Hay un problema con la etiqueta, intente grabarla de nuevo." });
            }
            if (datosProforma.veetiqueta_proforma.id == null || datosProforma.veetiqueta_proforma.id == "")
            {
                return BadRequest(new { resp = "Hay un problema con la etiqueta, no se proporciono el ID." });
            }
            if (datosProforma.veetiqueta_proforma.numeroid == null || datosProforma.veetiqueta_proforma.numeroid <= 0)
            {
                return BadRequest(new { resp = "Hay un problema con la etiqueta, no se proporciono el numero ID." });
            }
            if (datosProforma.veetiqueta_proforma.codcliente == null || datosProforma.veetiqueta_proforma.codcliente == "")
            {
                return BadRequest(new { resp = "Hay un problema con la etiqueta, no se proporciono el código de cliente." });
            }
            if (datosProforma.veetiqueta_proforma.linea1 == null || datosProforma.veetiqueta_proforma.linea1 == "")
            {
                return BadRequest(new { resp = "Hay un problema con la etiqueta, no se proporciono la línea 1." });
            }
            if (datosProforma.veetiqueta_proforma.linea2 == null)
            {
                return BadRequest(new { resp = "Hay un problema con la etiqueta, no se proporciono la línea 2." });
            }
            if (datosProforma.veetiqueta_proforma.representante == null || datosProforma.veetiqueta_proforma.representante == "")
            {
                return BadRequest(new { resp = "Hay un problema con la etiqueta, no se proporciono el representante." });
            }
            if (datosProforma.veetiqueta_proforma.ciudad == null || datosProforma.veetiqueta_proforma.ciudad == "")
            {
                return BadRequest(new { resp = "Hay un problema con la etiqueta, no se proporciono la ciudad." });
            }
            if (datosProforma.veetiqueta_proforma.celular == null)
            {
                return BadRequest(new { resp = "Hay un problema con la etiqueta, no se proporciono el celular." });
            }
            if (datosProforma.veetiqueta_proforma.telefono == null)
            {
                return BadRequest(new { resp = "Hay un problema con la etiqueta, no se proporciono el telefono." });
            }
            if (datosProforma.veetiqueta_proforma.latitud_entrega == null || datosProforma.veetiqueta_proforma.latitud_entrega == "")
            {
                return BadRequest(new { resp = "Hay un problema con la etiqueta, no se proporciono la latitud de entrega." });
            }
            if (datosProforma.veetiqueta_proforma.longitud_entrega == null || datosProforma.veetiqueta_proforma.longitud_entrega == "")
            {
                return BadRequest(new { resp = "Hay un problema con la etiqueta, no se proporciono la longitud de entrega." });
            }
            if (datosProforma.veetiqueta_proforma.longitud_entrega != "0" && datosProforma.veetiqueta_proforma.latitud_entrega != "0")
            {
                if (datosProforma.veetiqueta_proforma.longitud_entrega == datosProforma.veetiqueta_proforma.latitud_entrega)
                {
                    return BadRequest(new { resp = "Hay un problema con la etiqueta, se esta intentando guardar el mismo dato en la longitud y latitud de entrega." });
                }
            }
            /*
            List<veproforma_valida> veproforma_valida = datosProforma.veproforma_valida;
            List<veproforma_anticipo> veproforma_anticipo = datosProforma.veproforma_anticipo;
            List<vedesextraprof> vedesextraprof = datosProforma.vedesextraprof;
            List<verecargoprof> verecargoprof = datosProforma.verecargoprof;
            List<veproforma_iva> veproforma_iva = datosProforma.veproforma_iva;

            */
            if (veproforma.tdc == null)
            {
                return BadRequest(new { resp = "No se esta recibiendo el tipo de cambio verifique esta situación." });
            }

            if (veproforma.tdc == 0)
            {
                return BadRequest(new { resp = "El tipo de cambio esta como 0 verifique esta situación." });
            }

            if (codProforma != veproforma.codigo)
            {
                return BadRequest(new { resp = "Existe un problema con los codigos de Proforma, consulte al administrador de Sistemas!!!" });
            }

            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            List<string> msgAlerts = new List<string>();


            using (var _context = DbContextFactory.Create(userConnectionString))
            {

                // ###############################
                // ACTUALIZAR DATOS DE CODIGO PRINCIPAL SI ES APLICABLE
                await cliente.ActualizarParametrosDePrincipal(_context, veproforma.codcliente);
                // ###############################
                datosProforma.veproforma.paraaprobar = paraAprobar;
                datosProforma.veetiqueta_proforma.codigo = 0;

                //###############################
                // validacion 

                if (veproforma1.Count() <= 0)
                {
                    return BadRequest(new { resp = "No hay ningun item en su documento!!!" });
                }

                // VALIDACIONES OBTENIDAS POR BASE DE DATOS 
                var profAnulAprobTrans = await _context.veproforma.Where(i=> i.codigo == veproforma.codigo).Select(i=> new
                {
                    i.codigo,
                    i.anulada,
                    i.aprobada,
                    i.transferida
                }).FirstOrDefaultAsync();

                if (profAnulAprobTrans.anulada)
                {
                    return BadRequest(new { resp = "Esta Proforma esta Anulada y por lo tanto no puede ser modificada!!!" });
                }

                if (profAnulAprobTrans.aprobada)
                {
                    if (profAnulAprobTrans.transferida)
                    {
                        return BadRequest(new { resp = "Esta Proforma ya fue aprobada y transferida, no puede ser modificada. Para modificarla debe desaprobar la Proforma." });
                    }
                    else
                    {
                        return BadRequest(new { resp = "Esta Proforma ya fue aprobada, aunque no ha sido transferida aun, no puede ser modificada. Para modificarla debe desaprobar la Proforma." });
                    }
                }
                // fin de validacion 
                //###############################



                // ###############################  SE PUEDE LLAMAR DESDE FRONT END PARA LUEGO IR DIRECTO AL GRABADO ???????

                // RECALCULARPRECIOS(True, True);
                datosProforma.veproforma.nomcliente = datosProforma.veproforma.nomcliente.Trim();
                datosProforma.veproforma.nit = datosProforma.veproforma.nit.Trim();

                if (datosProforma.veproforma.idsoldesctos == null)
                {
                    datosProforma.veproforma.idsoldesctos = "";
                }
                if (datosProforma.veproforma.estado_contra_entrega == null)
                {
                    datosProforma.veproforma.estado_contra_entrega = "";
                }
                if (datosProforma.veproforma.contra_entrega == null)
                {
                    datosProforma.veproforma.contra_entrega = false;
                }

                if (datosProforma.veproforma.tipo_complementopf >= 0 && datosProforma.veproforma.tipo_complementopf <= 1)
                {
                    datosProforma.veproforma.tipo_complementopf = datosProforma.veproforma.tipo_complementopf + 1;
                }
                if (datosProforma.veproforma.tipo_complementopf == null)
                {
                    datosProforma.veproforma.tipo_complementopf = 0;
                }
                if (datosProforma.veproforma.tipo_complementopf >= 3)
                {
                    datosProforma.veproforma.tipo_complementopf = 0;
                }
                if (datosProforma.veproforma.pago_contado_anticipado == null)
                {
                    datosProforma.veproforma.pago_contado_anticipado = false;
                }
                if (datosProforma.veproforma.obs == null)
                {
                    datosProforma.veproforma.obs = "---";
                }
                if (datosProforma.veproforma.obs2 == null)
                {
                    datosProforma.veproforma.obs2 = "";
                }
                if (datosProforma.veproforma.odc == null)
                {
                    datosProforma.veproforma.odc = "";
                }
                if (datosProforma.veproforma.porceniva == null)
                {
                    datosProforma.veproforma.porceniva = 0;
                }

                datosProforma.veproforma.fechareg = DateTime.Today.Date;
                // datosProforma.veproforma.fechaaut = new DateTime(1900, 1, 1);     // PUEDE VARIAR SI ES PARA APROBAR


                datosProforma.veproforma.horareg = DateTime.Now.ToString("HH:mm");
                //datosProforma.veproforma.horaaut = "00:00";                       // PUEDE VARIAR SI ES PARA APROBAR

                if (veproforma.confirmada == true)
                {
                    datosProforma.veproforma.fecha_confirmada = DateTime.Today.Date;
                    datosProforma.veproforma.hora_confirmada = DateTime.Now.ToString("HH:mm");
                }
                else
                {
                    datosProforma.veproforma.fecha_confirmada = new DateTime(1900, 1, 1);
                    datosProforma.veproforma.hora_confirmada = "00:00";
                }
                /*
                if (paraAprobar)
                {
                    datosProforma.veproforma.fechaaut = DateTime.Today.Date;
                    datosProforma.veproforma.horaaut = DateTime.Now.ToString("HH:mm");
                }*/


                string msgCondClienteAlert = "";
                string lbltipo_cliente = await cliente.Tipo_Cliente(_context, veproforma.codcliente);
                if (lbltipo_cliente == "COMPETENCIA" || lbltipo_cliente == "V°B° PRESIDENCIA")
                {
                    msgCondClienteAlert = "Verifique las condiciones de venta ya que se trata de un cliente de tipo: " + lbltipo_cliente;
                }




                // ESTA VALIDACION ES MOMENTANEA, DESPUES SE DEBE COLOCAR SU PROPIA RUTA PARA VALIDAR, YA QUE PEDIRA CLAVE.
                var validacion_inicial = await Validar_Datos_Cabecera(_context, codempresa, codcliente_real, veproforma);
                if (!validacion_inicial.bandera)
                {
                    return BadRequest(new { resp = validacion_inicial.msg });
                }

                int codprofGrbd = 0;
                int numeroIdGrbd = 0;
                bool actualizaSaldos = false;
                using (var dbContexTransaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        var result = await Grabar_Documento(_context, codProforma, codempresa, datosProforma);
                        if (result.resp != "ok")
                        {
                            dbContexTransaction.Rollback();
                            return BadRequest(new { resp = result.resp });
                        }
                        codprofGrbd = result.codprof;
                        numeroIdGrbd = result.numeroId;

                        await log.RegistrarEvento(_context, veproforma.usuarioreg, Log.Entidades.SW_Proforma, result.codprof.ToString(), veproforma.id, result.numeroId.ToString(), this._controllerName, "Grabar", Log.TipoLog.Modificacion);

                        //Grabar Etiqueta
                        if (datosProforma.veetiqueta_proforma != null)
                        {
                            veetiqueta_proforma dt_etiqueta = datosProforma.veetiqueta_proforma;

                            if (dt_etiqueta.celular == null)
                            {
                                dt_etiqueta.celular = "---";
                            }
                            dt_etiqueta.numeroid = result.numeroId;
                            var etiqueta = await _context.veetiqueta_proforma.Where(i => i.id == dt_etiqueta.id && i.numeroid == dt_etiqueta.numeroid).FirstOrDefaultAsync();
                            if (etiqueta != null)
                            {
                                _context.veetiqueta_proforma.Remove(etiqueta);
                                await _context.SaveChangesAsync();
                            }
                            _context.veetiqueta_proforma.Add(dt_etiqueta);
                            await _context.SaveChangesAsync();
                        }

                        ////ACTUALIZAR PESO
                        ///ya se guarda el documento con el peso calculado.



                        /*
                         //enlazar sol desctos con proforma   FALTA 
                        If desclinea_segun_solicitud.Checked = True And idsoldesctos.Text.Trim.Length > 0 And nroidsoldesctos.Text.Trim.Length > 0 Then
                            If Not sia_funciones.Ventas.Instancia.Enlazar_Proforma_Nueva_Con_SolDesctos_Nivel(codigo.Text, idsoldesctos.Text, nroidsoldesctos.Text) Then
                                MessageBox.Show("No se pudo realizar el enlace de esta proforma con la solicitud de descuentos de nivel, verifique el enlace en la solicitu de descuentos!!!", "ErroR de Enlace", MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1)
                            End If
                        End If
                         */



                        // devolver mensajes pero como alerta Extra
                        string msgAler1 = "";
                        // enlazar sol desctos con proforma
                        if (check_desclinea_segun_solicitud == true && veproforma.idsoldesctos.Trim().Length > 0 && veproforma.nroidsoldesctos > 0)
                        {
                            if (!await ventas.Enlazar_Proforma_Nueva_Con_SolDesctos_Nivel(_context, result.codprof, veproforma.idsoldesctos, veproforma.nroidsoldesctos ?? 0))
                            {
                                msgAler1 = "Se grabo la Proforma, pero No se pudo realizar el enlace de esta proforma con la solicitud de descuentos de nivel, verifique el enlace en la solicitu de descuentos!!!";
                                msgAlerts.Add(msgAler1);
                            }
                        }

                        // grabar la etiqueta dsd 16-05-2022        
                        // solo si es cliente casual, y el cliente referencia o real es un no casual
                        //If sia_funciones.Cliente.Instancia.Es_Cliente_Casual(codcliente.Text) = True And sia_funciones.Cliente.Instancia.Es_Cliente_Casual(codcliente_real) = False Then

                        // Desde 10-10-2022 se definira si una venta es casual o no si el codigo de cliente y el codigo de cliente real son diferentes entonces es una venta casual
                        string msgAlert2 = "";

                        // ELIMINAR PRIMERO LA ETIQUETA SI O SI PARA LUEGO VERIFICAR SI SE PUEDE HACER LA ETIQUETA DE CLIENTES CASUALES
                        var profEtiqueta = await _context.veproforma_etiqueta.Where(i => i.id_proforma == veproforma.id && i.nroid_proforma == result.numeroId).FirstOrDefaultAsync();
                        if (profEtiqueta != null)
                        {
                            _context.veproforma_etiqueta.Remove(profEtiqueta);
                            await _context.SaveChangesAsync();
                        }

                        if (veproforma.codcliente != codcliente_real)
                        {
                            if (!await Grabar_Proforma_Etiqueta(_context, veproforma.id, result.numeroId, check_desclinea_segun_solicitud, codcliente_real, veproforma))
                            {
                                msgAlert2 = "No se pudo grabar la etiqueta Cliente Casual/Referencia de la proforma!!!";
                                msgAlerts.Add(msgAlert2);
                            }
                        }

                        if (paraAprobar)
                        {

                            // *****************O J O *************************************************************************************************************
                            // IMPLEMENTADO EN FECHA 26-04-2018 LLAMA A LA FUNNCION QUE VALIDA LO QUE SE VALIDA DESDE LA VENTANA DE APROBACION DE PROFORMAS
                            // *****************O J O *************************************************************************************************************


                            string mensajeAprobacion = "";
                            var resultValApro = await Validar_Aprobar_Proforma(_context, veproforma.id, result.numeroId, result.codprof, codempresa, datosProforma.tabladescuentos, datosProforma.DVTA, datosProforma.tablarecargos);

                            msgAlerts.AddRange(resultValApro.msgsAlert);


                            if (resultValApro.resp)
                            {
                                // verifica antes si la proforma esta grabar para aprobar
                                if (await ventas.proforma_para_aprobar(_context, result.codprof))
                                {
                                    // **aprobar la proforma
                                    var profforAprobar = await _context.veproforma.Where(i => i.codigo == result.codprof).FirstOrDefaultAsync();
                                    profforAprobar.aprobada = true;
                                    profforAprobar.fechaaut = DateTime.Today.Date;
                                    profforAprobar.horaaut = datos_proforma.getHoraActual();
                                    profforAprobar.usuarioaut = veproforma.usuarioreg;
                                    _context.Entry(profforAprobar).State = EntityState.Modified;
                                    await _context.SaveChangesAsync();

                                    // %%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
                                    // realizar la reserva de la mercaderia
                                    // Desde 15/11/2023 registrar en el log si por alguna razon no actualiza en instoactual correctamente al disminuir el saldo de cantidad y la reserva en proforma
                                    
                                    actualizaSaldos = true;
                                    // %%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%

                                    // Desde 30/07/2024 validar si la proforma en cuestion a esta en despachos y si lo fuera actualizar los datos del despacho como ser:
                                    // ESTADO(PENDIENTE), estado, peso,tipoentrega,preparacion,nroitems,codcliente,nomcliente,total,codmoneda,codvendedor,codalmacen
                                    // Tambien debe registrar un log de despacho a como PENDIENTE
                                    if (await despachos.proforma_en_despachos(_context, veproforma.id, result.numeroId))
                                    {
                                        var despacho = _context.vedespacho
                                            .Where(v => v.id == veproforma.id && v.nroid == result.numeroId)
                                            .FirstOrDefault();

                                        if (despacho != null)
                                        {
                                            despacho.estado = "PENDIENTE";
                                            despacho.peso = veproforma.peso;
                                            despacho.tipoentrega = veproforma.tipoentrega;
                                            despacho.preparacion = veproforma.preparacion;
                                            despacho.codcliente = veproforma.codcliente;
                                            despacho.nomcliente = veproforma.nomcliente;
                                            despacho.total = veproforma.total;
                                            despacho.codmoneda = veproforma.codmoneda;
                                            despacho.codvendedor = veproforma.codvendedor;
                                            despacho.codalmacen = veproforma.codalmacen;

                                            _context.Entry(despacho).State = EntityState.Modified;
                                            var actualizados = await _context.SaveChangesAsync();

                                            if (actualizados > 0)
                                            {
                                                // Los cambios se guardaron correctamente
                                                // registrar log
                                                Console.WriteLine("Los cambios se guardaron correctamente.");
                                                if (await despachos.cadena_insertar_log_estado_pedido(_context, veproforma.id, result.numeroId, "PENDIENTE", veproforma.usuarioreg))
                                                {
                                                    string msgDespachos = "La proforma paso a estado pendiente en los DESPACHOS!!!";
                                                    msgAlerts.Add(msgDespachos);
                                                }
                                            }
                                            else
                                            {
                                                // No se guardaron cambios
                                                Console.WriteLine("No se realizaron cambios.");
                                            }
                                        }
                                    }

                                    mensajeAprobacion = "La proforma fue grabada para aprobar y tambien aprobada.";
                                    // Desde 23/11/2023 guardar el log de grabado aqui
                                    await log.RegistrarEvento(_context, veproforma.usuarioreg, Log.Entidades.SW_Proforma, result.codprof.ToString(), veproforma.id, result.numeroId.ToString(), this._controllerName, "Grabar Para Aprobar", Log.TipoLog.Creacion);

                                }
                                else
                                {
                                    mensajeAprobacion = "La proforma no se grabo para aprobar por lo cual no se puede aprobar.";
                                }

                            }
                            else
                            {
                                mensajeAprobacion = "La proforma solo se grabo para aprobar, pero no se pudo aprobar porque no cumple con las condiciones de aprobacion!!! Revise la proforma en la ventana de modificacion de proformas.";
                                var desaprobarProforma = await _context.veproforma.Where(i => i.codigo == result.codprof).FirstOrDefaultAsync();
                                desaprobarProforma.aprobada = false;
                                desaprobarProforma.fechaaut = new DateTime(1900, 1, 1);
                                desaprobarProforma.horaaut = "00:00";
                                desaprobarProforma.usuarioaut = "";
                                _context.Entry(desaprobarProforma).State = EntityState.Modified;
                                await _context.SaveChangesAsync();
                            }
                            msgAlerts.Add(mensajeAprobacion);

                        }
                        /*
                         
                         '//validar lo que se validaba en la ventana de aprobar proforma
                            Dim mi_idpf As String = sia_funciones.Ventas.Instancia.proforma_id(_CODPROFORMA)
                            Dim mi_nroidpf As String = sia_funciones.Ventas.Instancia.proforma_numeroid(_CODPROFORMA)

                            '//validar lo que se validaba en la ventana de aprobar proforma
                            Dim dt As New DataTable
                            Dim qry As String = ""
                            Dim coddesextra As String = sia_funciones.Configuracion.Instancia.emp_coddesextra_x_deposito(sia_compartidos.temporales.Instancia.codempresa)

                            qry = "select * from vedesextraprof where  coddesextra='" & coddesextra & "' and codproforma=(select codigo from veproforma where id='" & mi_idpf & "' and numeroid='" & mi_nroidpf & "')"
                            dt.Clear()
                            dt = sia_DAL.Datos.Instancia.ObtenerDataTable(qry)
                            '//verificar si la proforma tiene descto por deposito
                            If dt.Rows.Count > 0 Then
                                If Not Me.Validar_Desctos_Por_Depositos_Solo_Al_Grabar(id.Text, numeroid.Text) Then
                                    If sia_funciones.Ventas.Instancia.Eliminar_Descuento_Deposito_De_Proforma(id.Text, numeroid.Text, Me.Name) Then
                                        MessageBox.Show("Se verifico que la proforma fue grabada con montos de descuentos por deposito incorrectos, por lo que se procedio a eliminar los descuentos por deposito de la proforma; " & mi_idpf & "-" & mi_nroidpf, "Alerta", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1)
                                    End If
                                End If
                            End If

                         */


                        dbContexTransaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        dbContexTransaction.Rollback();
                        return Problem($"Error en el servidor: {ex.Message}");
                        throw;
                    }

                    
                }
                try
                {
                    if (actualizaSaldos)
                    {
                        if (await ventas.aplicarstocksproforma(_context, codprofGrbd, codempresa) == false)
                        {
                            await log.RegistrarEvento(_context, veproforma.usuarioreg, Log.Entidades.SW_Proforma, codprofGrbd.ToString(), veproforma.id, numeroIdGrbd.ToString(), this._controllerName, "No actualizo stock al sumar cantidad de reserva en PF.", Log.TipoLog.Creacion);
                        }
                    }

                }
                catch (Exception ex)
                {
                    return Problem($"Se grabo la proforma, Error en el servidor al actualizar Saldos: {ex.Message}");
                    throw;
                }
                return Ok(new { resp = "Se Grabo la Proforma de manera Exitosa", codProf = codprofGrbd, alerts = msgAlerts });
            }
        }


        private async Task<(bool resp, List<string> msgsAlert)> Validar_Aprobar_Proforma(DBContext _context, string id_pf, int nroid_pf, int cod_proforma, string codempresa, List<vedesextraDatos> tabladescuentos, DatosDocVta DVTA, List<verecargosDatos> tablarecargos)
        {
            bool resultado = true;
            List<string> msgsAlert = new List<string>();
            var dt_pf = await _context.veproforma.Where(i => i.codigo == cod_proforma).FirstOrDefaultAsync();

            ////////////////////////////////////////////////////////////////////////////////
            // validar el monto de desctos por deposito aplicado
            var respdepCoCred = await depositos_cliente.Validar_Desctos_x_Deposito_Otorgados_De_Cobranzas_Credito(_context, id_pf, nroid_pf, codempresa);
            if (!respdepCoCred.result)
            {
                resultado = false;
                if (respdepCoCred.msgAlert != "")
                {
                    msgsAlert.Add(respdepCoCred.msgAlert);
                }
            }

            var respdepCoCont = await depositos_cliente.Validar_Desctos_x_Deposito_Otorgados_De_Cbzas_Contado_CE(_context, id_pf, nroid_pf, codempresa);
            if (!respdepCoCont.result)
            {
                resultado = false;
                if (respdepCoCont.msgAlert != "")
                {
                    msgsAlert.Add(respdepCoCont.msgAlert);
                }
            }

            var respdepAntProfCont = await depositos_cliente.Validar_Desctos_x_Deposito_Otorgados_De_Anticipos_Que_Pagaron_Proformas_Contado(_context, id_pf, nroid_pf, codempresa);
            if (!respdepAntProfCont.result)
            {
                resultado = false;
                if (respdepAntProfCont.msgAlert != "")
                {
                    msgsAlert.Add(respdepAntProfCont.msgAlert);
                }
            }

            if (resultado == false)
            {
                msgsAlert.Add("No se puede aprobar la proforma, porque tiene descuentos por deposito en montos no validos!!!");
                return (false, msgsAlert);
            }
            ////////////////////////////////////////////////////////////////////////////////


            //======================================================================================
            /////////////////VALIDAR DESCTOS POR DEPOSITO APLICADOS
            //======================================================================================

            var validDescDepApli = await Validar_Descuentos_Por_Deposito_Excedente(_context, codempresa, tabladescuentos, DVTA);
            if (!validDescDepApli.result)
            {
                resultado = false;
                msgsAlert.Add(validDescDepApli.msgAlert);
                msgsAlert.Add("La proforma no puede ser aprobada, porque tiene descuentos por deposito en montos no validos!!!");
                return (false, msgsAlert);
            }
            //======================================================================================
            ///////////////VALIDAR RECARGOS POR DEPOSITO APLICADOS
            //======================================================================================
            var validRecargoDepExcedente = await Validar_Recargos_Por_Deposito_Excedente(_context, codempresa, tablarecargos, DVTA);
            if (!validRecargoDepExcedente.result)
            {
                resultado = false;
                msgsAlert.Add(validDescDepApli.msgAlert);
                msgsAlert.Add("La proforma no puede ser aprobada, porque tiene recargos por descuentos por deposito excedentes en montos no validos!!!");
                return (false, msgsAlert);
            }

            //////////////////////////////////////////////

            // mostrar mensaje de credito disponible
            /*
            
            If IsDBNull(reg("contra_entrega")) Then
                qry = "update veproforma set contra_entrega=0 where id='" & id_pf & "' and numeroid='" & nroid_pf & "'"
                sia_DAL.Datos.Instancia.EjecutarComando(qry)
            End If

             */

            // tipo pago CONTADO
            if (DVTA.tipo_vta == "CONTADO")
            {
                ////////////////////////////////////////////////////////////////////////////////////////////
                // se añadio en fecha 15-3-2016
                // es venta al contado y no necesita validar el credito
                // TODA VENTA AL CONTADO DEBE TENER ASIGNADO ID-NROID DE ANTICIPO SI ES PAGO ANTICIPADO
                // SI SE HABILITO LA OPCION DE PAGO ANTICIPADO
                ////////////////////////////////////////////////////////////////////////////////////////////
                var dt_anticipos = await anticipos_vta_contado.Anticipos_Aplicados_a_Proforma(_context, id_pf, nroid_pf);
                if (dt_anticipos.Count > 0)
                {
                    ResultadoValidacion objres = new ResultadoValidacion();
                    objres = await anticipos_vta_contado.Validar_Anticipo_Asignado_2(_context, true, DVTA, dt_anticipos, codempresa);
                    if (objres.resultado)
                    {
                        // Desde 15/01/2024 se cambio esta funcion porque no estaba validando correctamente la transformacion de moneda de los anticipos a aplicarse ya se en $us o BS
                        // If sia_funciones.Anticipos_Vta_Contado.Instancia.Validar_Anticipo_Asignado(True, dt_anticipos, reg("codcliente"), reg("nomcliente"), reg("total")) = True Then
                        goto finalizar_ok;
                    }
                    else
                    {
                        if (dt_anticipos != null)
                        {
                            return (false, msgsAlert);
                        }
                    }
                }
                goto finalizar_ok;

            }

        finalizar_ok:
            return (true, msgsAlert);

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

        private async Task<bool> Grabar_Proforma_Etiqueta(DBContext _context, string idProf, int nroidpf, bool desclinea_segun_solicitud, string codcliente_real, veproforma dtpf)
        {
            try
            {
                veproforma_etiqueta datospfe = new veproforma_etiqueta();
                // obtener datos de la etiqueta
                var dt_etiqueta = await _context.veetiqueta_proforma.Where(i => i.id == idProf && i.numeroid == nroidpf).FirstOrDefaultAsync();
                // obtener datos de proforma
                /*
                var dtpf = await _context.veproforma.Where(i => i.id == idProf && i.numeroid == nroidpf)
                    .Select(i => new
                    {
                        i.codigo,
                        i.id,
                        i.numeroid,
                        i.fecha,
                        i.codcliente,
                        i.direccion,
                        i.latitud_entrega,
                        i.longitud_entrega,
                        i.codalmacen
                    })
                    .FirstOrDefaultAsync();
                */
                datospfe.id_proforma = idProf;
                datospfe.nroid_proforma = nroidpf;
                datospfe.codalmacen = dtpf.codalmacen;
                datospfe.codcliente_casual = dtpf.codcliente;
                if (desclinea_segun_solicitud == true && dtpf.idsoldesctos.Trim().Length > 0 && (dtpf.nroidsoldesctos > 0 || dtpf.nroidsoldesctos != null))
                {
                    datospfe.codcliente_real = await ventas.Cliente_Referencia_Solicitud_Descuentos(_context, dtpf.idsoldesctos, dtpf.nroidsoldesctos ?? 0);
                }
                else
                {
                    datospfe.codcliente_real = codcliente_real;
                }
                datospfe.fecha = dtpf.fecha;
                datospfe.direccion = dtpf.direccion;
                if (dt_etiqueta != null)
                {
                    datospfe.ciudad = dt_etiqueta.ciudad;
                }
                else
                {
                    datospfe.ciudad = "";
                }

                datospfe.latitud_entrega = dtpf.latitud_entrega;
                datospfe.longitud_entrega = dtpf.longitud_entrega;
                datospfe.horareg = dtpf.horareg;
                datospfe.fechareg = dtpf.fechareg;
                datospfe.usuarioreg = dtpf.usuarioreg;

                // insertar proforma_etiqueta (datospfe)
                _context.veproforma_etiqueta.Add(datospfe);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private async Task<(bool bandera, string msg)> Validar_Datos_Cabecera(DBContext _context, string codempresa, string codcliente_real, veproforma veproforma)
        {
            // VALIDACIONES PARA EVITAR NULOS
            if (veproforma.id == null) { return (false, "No se esta recibiendo el ID del documento, Consulte con el Administrador del sistema."); }
            if (veproforma.numeroid == null) { return (false, "No se esta recibiendo el número de ID del documento, Consulte con el Administrador del sistema."); }
            if (veproforma.codalmacen == null) { return (false, "No se esta recibiendo el codigo de Almacen, Consulte con el Administrador del sistema."); }
            if (veproforma.codvendedor == null) { return (false, "No se esta recibiendo el código de vendedor, Consulte con el Administrador del sistema."); }
            if (veproforma.preparacion == null) { return (false, "No se esta recibiendo el tipo de preparación, Consulte con el Administrador del sistema."); }
            if (veproforma.tipopago == null) { return (false, "No se esta recibiendo el tipo de pago, Consulte con el Administrador del sistema."); }
            if (veproforma.contra_entrega == null) { return (false, "No se esta recibiendo si la venta es contra entrega o no, Consulte con el Administrador del sistema."); }
            if (veproforma.estado_contra_entrega == null) { return (false, "No se esta recibiendo el estado contra entrega, Consulte con el Administrador del sistema."); }
            if (veproforma.id == null) { return (false, "No se esta recibiendo el ID del documento, Consulte con el Administrador del sistema."); }
            if (veproforma.codcliente == null) { return (false, "No se esta recibiendo el codigo de cliente, Consulte con el Administrador del sistema."); }
            if (veproforma.nomcliente == null) { return (false, "No se esta recibiendo el nombre del cliente, Consulte con el Administrador del sistema."); }
            if (veproforma.tipo_docid == null) { return (false, "No se esta recibiendo el tipo de documento, Consulte con el Administrador del sistema."); }
            if (veproforma.nit == null) { return (false, "No se esta recibiendo el NIT/CI del cliente, Consulte con el Administrador del sistema."); }
            if (veproforma.email == null) { return (false, "No se esta recibiendo el Email del cliente, Consulte con el Administrador del sistema."); }
            if (veproforma.codmoneda == null) { return (false, "No se esta recibiendo el codigo de moneda, Consulte con el Administrador del sistema."); }

            if (veproforma.pago_contado_anticipado == null) { return (false, "No se esta recibiendo si el pago de realiza de forma anticipada o No, Consulte con el Administrador del sistema."); }
            if (veproforma.idpf_complemento == null) { return (false, "No se esta recibiendo el ID de proforma complemento, Consulte con el Administrador del sistema."); }
            if (veproforma.nroidpf_complemento == null) { return (false, "No se esta recibiendo el Número ID de proforma complemento, Consulte con el Administrador del sistema."); }
            if (veproforma.tipo_complementopf == null) { return (false, "No se esta recibiendo el tipo de proforma complemento, Consulte con el Administrador del sistema."); }
            if (veproforma.niveles_descuento == null) { return (false, "No se esta recibiendo el nivel de descuento actual del cliente, Consulte con el Administrador del sistema."); }




            // POR AHORA VALIDACIONES QUE REQUIERAN CONSULTA A BASE DE DATOS.
            string id = veproforma.id;
            int codalmacen = veproforma.codalmacen;
            int codvendedor = veproforma.codvendedor;
            string codcliente = veproforma.codcliente;
            string nomcliente = veproforma.nomcliente;
            string nit = veproforma.nit;
            string codmoneda = veproforma.codmoneda;
            //string codcliente_real = veproforma.codcliente_real;

            veproforma.direccion = (veproforma.direccion == "") ? "---" : veproforma.direccion;
            veproforma.obs = (veproforma.obs == "") ? "---" : veproforma.obs;

            string direccion = veproforma.direccion;
            string obs = veproforma.obs;

            int tipo_doc_id = veproforma.tipo_docid ?? -2;

            // verificar si se puede realizar ventas a codigos SN
            if (await cliente.EsClienteSinNombre(_context, codcliente))
            {
                if (nit.Trim().Length > 0)
                {
                    if (int.TryParse(nit, out int result))
                    {
                        if (result > 0)
                        {
                            // ya no se podra hacer ventas a codigo sin nombre dsd mayo 2022, se debe asignar codigo al cliente
                            if (!await configuracion.emp_permitir_facturas_sin_nombre(_context, codempresa))
                            {
                                return (false, "No se puede realizar facturas a codigos SIN NOMBRE y con NIT diferente de Cero, si va a facturar a un NIT/CI dfte. de cero debe crear al cliente!!!.");
                            }
                        }
                        // si se puede facturar a codigo SIN NOMBRE CON NIT CERO
                    }
                    // igual devuelve valido si el nit no es numerico, mas abajo se validara si es correcto
                }
                // igual devuelve valido si NO INGRESO EL nit, mas abajo se validara si es correcto
            }

            if (!await cliente.clientehabilitado(_context, codcliente_real))
            {
                return (false, "Ese Cliente: " + codcliente_real + " no esta habilitado.");
            }
            // Desde 14 / 08 / 2023 validar que el cliente casual este habilitado
            if (!await cliente.clientehabilitado(_context, codcliente))
            {
                return (false, "Ese Cliente: " + codcliente + " no esta habilitado.");
            }

            tipo_doc_id = tipo_doc_id + 1;
            var respNITValido = await ventas.Validar_NIT_Correcto(_context, nit, tipo_doc_id.ToString());
            if (!respNITValido.EsValido)
            {
                return (false, "Verifique que el NIT tenga el formato correcto!!! " + respNITValido.Mensaje);
            }

            if (veproforma.tipopago == 1 && veproforma.contra_entrega == true) // 0 = CONTADO, 1 = CREDITO
            {
                return (false, "LA PROFORMA NO PUEDE SER TIPO CREDITO Y CONTADO CONTRA ENTREGA, VERIFIQUE ESTA SITUACIÓN.");
            }
            if (veproforma.tipopago == 0 && veproforma.pago_contado_anticipado == true && veproforma.contra_entrega == true) // 0 = CONTADO, 1 = CREDITO
            {
                return (false, "LA PROFORMA NO PUEDE SER TIPO CONTADO CONTRA-ENTREGA CON ANTICIPOS, VERIFIQUE ESTA SITUACIÓN.");
            }
            if (veproforma.tipopago == 1 && veproforma.pago_contado_anticipado == true) // 0 = CONTADO, 1 = CREDITO
            {
                return (false, "LA PROFORMA NO PUEDE SER TIPO CREDITO Y TENER ANTICIPOS, VERIFIQUE ESTA SITUACIÓN.");
            }




            if (veproforma.contra_entrega == true)
            {
                if (veproforma.estado_contra_entrega == "")
                {
                    return (false, "Debe especificar el estado de pago del pedido si este es: CONTRA ENTREGA ");
                }
            }
            //verifica si el usuario definio como se entregara el pedido
            if (veproforma.tipoentrega != "RECOGE CLIENTE" && veproforma.tipoentrega != "ENTREGAR")
            {
                return (false, "Debe definir si el pedido: Recogera el Cliente o si Pertec Realizara la Entrega");
            }
            //verificar si elegio enlazar con proforma complemento hayan los datos
            if (veproforma.tipo_complementopf > 0)
            {
                if (veproforma.idpf_complemento.Trim().Length == 0)
                {
                    return (false, "Ha elegido complementar la proforma pero no indico el Id de la proforma con la cual desdea complementar!!!");
                }
                if (veproforma.nroidpf_complemento.ToString().Trim().Length == 0)
                {
                    return (false, "Ha elegido complementar la proforma pero no indico el NroId de la proforma con la cual desdea complementar!!!");
                }
            }
            //validar email
            if (veproforma.email.Trim().Length == 0)
            {
                return (false, "Si no especifica una direccion de email valida, no se podra enviar la factura en formato digital.");
            }
            //validar tipo de preparacion "CAJA CERRADA RECOJE CLIENTE/RECOJE CLIENTE"
            //Dsd 29 - 11 - 2022 se corrigio la palabra RECOJE por RECOGE para que se igual al campo tipoentrega.text
            if (veproforma.preparacion == "CAJA CERRADA RECOGE CLIENTE")
            {
                if (veproforma.tipoentrega != "RECOGE CLIENTE")
                {
                    return (false, "El tipo de preparacion de la proforma es: CAJA CERRADA RECOGE CLIENTE, por tanto el tipo de entrega debe ser: RECOGE CLIENTE. Verifique esta situacion!!!");
                }
            }

            //validar el NIT en el SIN
            /*
            If resultado Then
                If Not Validar_NIT_En_El_SIN() Then
                    cmbtipo_docid.Focus()
                    resultado = False
                End If
            End If
             */
            return (true, "Error al guardar todos los datos.");
        }













        private async Task<(string resp, int codprof, int numeroId)> Grabar_Documento(DBContext _context, int codProforma, string codempresa, SaveProformaCompleta datosProforma)
        {
            veproforma veproforma = datosProforma.veproforma;
            List<veproforma1> veproforma1 = datosProforma.veproforma1;
            var veproforma_valida = datosProforma.veproforma_valida;
            var dt_anticipo_pf = datosProforma.dt_anticipo_pf;
            List<tabla_veproformaAnticipo> dt_anticipo_pf_inicial = new List<tabla_veproformaAnticipo>();
            var vedesextraprof = datosProforma.vedesextraprof;
            var verecargoprof = datosProforma.verecargoprof;
            var veproforma_iva = datosProforma.veproforma_iva;

            ////////////////////   GRABAR DOCUMENTO

            /*
            ///////////////////////////////////////////////   FALTA VALIDACIONES


            If Not Validar_Detalle() Then
                Return False
            End If

            If Not Validar_Datos() Then
                Return False
            End If

            //************************************************
            //control implementado en fecha: 09-10-2020

            If Not Me.Validar_Saldos_Negativos(False) Then
                If MessageBox.Show("La proforma genera saldos negativos, esta seguro de grabar la proforma???", "Validar Negativos", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1) = Windows.Forms.DialogResult.No Then
                    Return False
                End If
            End If






            */
            //************************************************

            // Actualizar cabecera (veproforma)
            try
            {
                _context.Entry(veproforma).State = EntityState.Modified;
                await _context.SaveChangesAsync();

            }
            catch (Exception)
            {

                throw;
            }
            


            // guarda detalle (veproforma1)
            // actualizar codigoproforma para agregar
            veproforma1 = veproforma1.Select(p => { p.codproforma = codProforma; return p; }).ToList();
            // colocar obs como vacio no nulo
            veproforma1 = veproforma1.Select(o => { o.obs = ""; return o; }).ToList();
            // actualizar peso del detalle.
            veproforma1 = await ventas.Actualizar_Peso_Detalle_Proforma(_context, veproforma1);

            // grabar detalle de proforma

            await grabarDetalleProf(_context, codProforma, veproforma1);



            //======================================================================================
            // grabar detalle de validacion
            //======================================================================================

            veproforma_valida = veproforma_valida.Select(p => { p.codproforma = codProforma; return p; }).ToList();

            // grabar validaciones

            await grabarValidaciones(_context, codProforma, veproforma_valida);





            // grabar descto por deposito si hay descuentos
            if (vedesextraprof != null)
            {
                if (vedesextraprof.Count() > 0)
                {
                    await grabardesextra(_context, codProforma, vedesextraprof);
                }
            }

            if (verecargoprof != null)
            {
                // grabar recargo si hay recargos
                if (verecargoprof.Count > 0)
                {
                    await grabarrecargo(_context, codProforma, verecargoprof);
                }
            }

            if (veproforma_iva != null)
            {
                // grabar iva
                if (veproforma_iva.Count > 0)
                {
                    await grabariva(_context, codProforma, veproforma_iva);
                }
            }
            

            //======================================================================================
            //grabar anticipos aplicados
            //======================================================================================
            try
            {
                var anticiposprevios = await _context.veproforma_anticipo.Where(i => i.codproforma == codProforma).ToListAsync();
                var dt_anticipo_pf_inicial1 = await _context.veproforma_anticipo.Where(i => i.codproforma == codProforma).ToListAsync();

                var newDetalAnt = dt_anticipo_pf_inicial1
                        .Select(i => new tabla_veproformaAnticipo
                        {
                            codproforma = i.codproforma,
                            codanticipo = i.codanticipo,
                            id_anticipo = "",
                            nroid_anticipo = 0,
                            monto = (double)i.monto,
                            tdc = (double?)i.tdc,
                            fechareg = (DateTime)i.fechareg,
                            usuarioreg = i.usuarioreg,
                            horareg = i.horareg
                        }).ToList();

                dt_anticipo_pf_inicial = newDetalAnt;

                string[] doc_cbza = new string[2];
                foreach (var reg in dt_anticipo_pf_inicial)
                {
                    doc_cbza = await cobranzas.Id_Nroid_Anticipo(_context, (int)reg.codanticipo);
                    reg.id_anticipo = doc_cbza[0];
                    reg.nroid_anticipo = int.Parse(doc_cbza[1]);
                }

                if (anticiposprevios.Count() > 0)
                {
                    _context.veproforma_anticipo.RemoveRange(anticiposprevios);
                    await _context.SaveChangesAsync();
                }
                if (dt_anticipo_pf != null)
                {
                    if (dt_anticipo_pf.Count() > 0)
                    {

                        var newData = dt_anticipo_pf
                            .Select(i => new veproforma_anticipo
                            {
                                codproforma = codProforma,
                                codanticipo = i.codanticipo,
                                monto = (decimal?)i.monto,
                                tdc = (decimal?)i.tdc,

                                fechareg = i.fechareg,
                                usuarioreg = veproforma.usuarioreg,
                                horareg = datos_proforma.getHoraActual()
                            }).ToList();
                        _context.veproforma_anticipo.AddRange(newData);
                        await _context.SaveChangesAsync();

                    }
                }
            }
            catch (Exception)
            {
                throw;
            }

            //======================================================================================
            //grabar diferencias de anticipos aplicados
            //======================================================================================

            try
            {
                var diferencias_previos = await _context.veproforma_anticipo_diferencias.Where(i => i.codproforma == codProforma).ToListAsync();
                if (diferencias_previos.Count() > 0)
                {
                    _context.veproforma_anticipo_diferencias.RemoveRange(diferencias_previos);
                    await _context.SaveChangesAsync();
                }
                //obtener si hay diferencia enntre el total de aplicado de anticipo contra el total de la proforma
                decimal ttl_anticipos_aplicados = 0;
                decimal ttl_pf = 0;
                decimal diferencia_ant_pf = 0;
                bool anticipo_mayor = true;

                if (dt_anticipo_pf != null)
                {
                    if (dt_anticipo_pf.Count() > 0)
                    {
                        foreach (var ant in dt_anticipo_pf)
                        {
                            ttl_anticipos_aplicados += Math.Round(Convert.ToDecimal(ant.monto), 2);
                        }
                        ttl_pf = Math.Round(veproforma.total, 2);
                        diferencia_ant_pf = Math.Round(ttl_anticipos_aplicados - ttl_pf, 2);
                        if (ttl_anticipos_aplicados != ttl_pf)
                        {
                            anticipo_mayor = ttl_anticipos_aplicados > ttl_pf;

                            var newData = new veproforma_anticipo_diferencias
                            {
                                codproforma = codProforma,
                                monto = diferencia_ant_pf,
                                tdc = 1,
                                fechareg = DateTime.Parse(datos_proforma.getFechaActual()),
                                usuarioreg = veproforma.usuarioreg,
                                horareg = datos_proforma.getHoraActual(),
                                anticipo_aplicado_mayor = anticipo_mayor
                            };
                            await _context.veproforma_anticipo_diferencias.AddAsync(newData);
                            await _context.SaveChangesAsync();
                        }
                    }
                }
                
            }
            catch (Exception)
            {

                throw;
            }

            bool resultado = new bool();
            //======================================================================================
            // insertar en la tabla de descuentos por deposito pendientes
            //======================================================================================
            if (await ventas.Grabar_Descuento_Por_deposito_Pendiente(_context, codProforma, codempresa, veproforma.usuarioreg, vedesextraprof))
            {
                resultado = true;
            }
            else
            {
                resultado = false;
            }

            // ======================================================================================
            // actualizar saldo restante de anticipos aplicados
            // ======================================================================================
            //if (resultado)
            //{
            //    foreach (var reg in dt_anticipo_pf)
            //    {
            //        if (!await anticipos_vta_contado.ActualizarMontoRestAnticipo(_context, reg.id_anticipo, reg.nroid_anticipo, reg.codproforma ?? 0, reg.codanticipo ?? 0, reg.monto, codempresa))
            //        {
            //            resultado = false;
            //        }
            //    }
            //}
            //    '//======================================================================================
            //'//Desde 03/04/2024 actualizar saldo restante de anticipos aplicados INIALMENTE, YA QUE PUEDE SER QUE UNA PROFORMA LO QUITEN LOS ANTICIPOS
            //'Y SE DEBE LIBERAR EL SALDOS DE LOS ANTICIPOS INICIALES
            //'//======================================================================================
            if (resultado)
            {
                // Desde 03/04/2024 al anular una proforma se debe actualizar los saldos de los anticipos que tuviera enlazada la proforma
                if (!await Actualizar_saldos_anticipos(_context, codempresa, dt_anticipo_pf, dt_anticipo_pf_inicial))
                {
                    resultado = false;
                }
            }


            return ("ok", codProforma, veproforma.numeroid);


        }




















        private async Task grabarDetalleProf(DBContext _context, int codProf, List<veproforma1> veproforma1)
        {
            var detalleProfAnt = await _context.veproforma1.Where(i => i.codproforma == codProf).ToListAsync();
            if (detalleProfAnt.Count() > 0)
            {
                _context.veproforma1.RemoveRange(detalleProfAnt);
                await _context.SaveChangesAsync();
            }
            _context.veproforma1.AddRange(veproforma1);
            await _context.SaveChangesAsync();
        }
        private async Task grabarValidaciones(DBContext _context, int codProf, List<veproforma_valida> veproforma_valida)
        {
            var validacionAnt = await _context.veproforma_valida.Where(i => i.codproforma == codProf).ToListAsync();
            if (validacionAnt.Count() > 0)
            {
                _context.veproforma_valida.RemoveRange(validacionAnt);
                await _context.SaveChangesAsync();
            }
            _context.veproforma_valida.AddRange(veproforma_valida);
            await _context.SaveChangesAsync();
        }
        private async Task grabardesextra(DBContext _context, int codProf, List<vedesextraprof> vedesextraprof)
        {
            var descExtraAnt = await _context.vedesextraprof.Where(i => i.codproforma == codProf).ToListAsync();
            if (descExtraAnt.Count() > 0)
            {
                _context.vedesextraprof.RemoveRange(descExtraAnt);
                await _context.SaveChangesAsync();
            }
            vedesextraprof = vedesextraprof.Select(p => { p.codproforma = codProf; return p; }).ToList();
            vedesextraprof = vedesextraprof.Select(p => { p.id = 0; return p; }).ToList();
            _context.vedesextraprof.AddRange(vedesextraprof);
            await _context.SaveChangesAsync();
        }


        private async Task grabarrecargo(DBContext _context, int codProf, List<verecargoprof> verecargoprof)
        {
            var recargosAnt = await _context.verecargoprof.Where(i => i.codproforma == codProf).ToListAsync();
            if (recargosAnt.Count() > 0)
            {
                _context.verecargoprof.RemoveRange(recargosAnt);
                await _context.SaveChangesAsync();
            }
            verecargoprof = verecargoprof.Select(p => { p.codproforma = codProf; return p; }).ToList();
            _context.verecargoprof.AddRange(verecargoprof);
            await _context.SaveChangesAsync();
        }

        private async Task grabariva(DBContext _context, int codProf, List<veproforma_iva> veproforma_iva)
        {
            var ivaAnt = await _context.veproforma_iva.Where(i => i.codproforma == codProf).ToListAsync();
            if (ivaAnt.Count() > 0)
            {
                _context.veproforma_iva.RemoveRange(ivaAnt);
                await _context.SaveChangesAsync();
            }
            veproforma_iva = veproforma_iva.Select(p => { p.codproforma = codProf; return p; }).ToList();
            _context.veproforma_iva.AddRange(veproforma_iva);
            await _context.SaveChangesAsync();
        }

        private async Task<bool> Actualizar_saldos_anticipos(DBContext _context, string codempresa, List<tabla_veproformaAnticipo>? dt_anticipo_pf, List<tabla_veproformaAnticipo>? dt_anticipo_pf_inicial)
        {
            //======================================================================================
            // actualizar saldo restante de anticipos aplicados
            //======================================================================================
            bool resultado = true;
            foreach (var reg in dt_anticipo_pf)
            {
                // añadir detalle al documento
                if (!await anticipos_vta_contado.ActualizarMontoRestAnticipo(_context, reg.id_anticipo, reg.nroid_anticipo, reg.codproforma ?? 0, reg.codanticipo ?? 0, reg.monto, codempresa))
                {
                    resultado = false;
                }
            }
            //======================================================================================
            // Desde 03/04/2024 actualizar saldo restante de anticipos aplicados INIALMENTE, YA QUE PUEDE SER QUE UNA PROFORMA LO QUITEN LOS ANTICIPOS
            // Y SE DEBE LIBERAR EL SALDOS DE LOS ANTICIPOS INICIALES
            //======================================================================================
            if (resultado)
            {
                foreach (var reg in dt_anticipo_pf_inicial)
                {
                    // añadir detalle al documento
                    if (!await anticipos_vta_contado.ActualizarMontoRestAnticipo(_context, reg.id_anticipo, reg.nroid_anticipo, 0, reg.codanticipo ?? 0, 0, codempresa))
                    {
                        resultado = false;
                    }
                }
            }
            return resultado;

        }

    }

    public class RequestAnularProf
    {
        public List<tabla_veproformaAnticipo>? dt_anticipo_pf { get; set; }
        public List<tabla_veproformaAnticipo>? dt_anticipo_pf_inicial { get; set; }
    }

    public class DataValidar
    {
        public int codproforma { get; set; }
        public int? orden { get; set; }
        public string codcontrol { get; set; }
        public string desc_grabar { get; set; }
        public bool? grabar { get; set; }
        public string grabar_aprobar { get; set; }
        public string nit { get; set; }
        public int? nroitems { get; set; }
        public decimal? subtotal { get; set; }
        public decimal? recargos { get; set; } 
        public decimal? descuentos { get; set; } 

        public decimal? total { get; set; }
        public string valido { get; set; }
        public string observacion { get; set; }
        public string obsdetalle { get; set; }
        public int? codservicio { get; set; }
        public string descservicio { get; set; }
        public string descripcion { get; set; }
        public string datoa { get; set; }
        public string datob { get; set; }
        public string clave_servicio { get; set; } 
        public string accion { get; set; } 
    }
}
