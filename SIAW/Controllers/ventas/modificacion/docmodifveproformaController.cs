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



        public docmodifveproformaController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }

        [HttpGet]
        [Route("getUltiProfId/{userConn}/{idProforma}/{nroidProforma}/{codempresa}")]
        public async Task<object> getUltiProfId(string userConn, string idProforma, int nroidProforma, string codempresa)
        {
            try
            {
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var result = await _context.veproforma.OrderByDescending(i => i.codigo)
                        .Select(i => new
                        {
                            i.codigo,
                            i.id,
                            i.numeroid
                        }).FirstOrDefaultAsync();
                    return Ok(result);
                }
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
                throw;
            }
        }

        [HttpGet]
        [Route("obtProfxModif/{userConn}/{idProforma}/{nroidProforma}/{codempresa}")]
        public async Task<object> obtProfxModif(string userConn, string idProforma, int nroidProforma, string codempresa)
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


                    // obtener razon social de cliente
                    var codclientedescripcion = await cliente.Razonsocial(_context, cabecera.codcliente);
                    // obtener tipo cliente
                    var tipo_cliente = await cliente.Tipo_Cliente(_context, cabecera.codcliente);
                    // establecer ubicacion
                    if (cabecera.ubicacion == null || cabecera.ubicacion=="")
                    {
                        cabecera.ubicacion = "NSE";
                    }
                    // texto Confirmada 
                    string descConfirmada = "";
                    if ((cabecera.confirmada ?? false) == true)
                    {
                        descConfirmada = "CONFIRMADA " + cabecera.hora_confirmada + " " + (cabecera.fecha_confirmada ?? new DateTime(1900, 1, 1)).ToShortDateString();
                    }

                   bool cliHabilitado = await cliente.clientehabilitado(_context,cabecera.codcliente);

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
                    var profEtiqueta = await _context.veproforma_etiqueta.Where(i =>i.id_proforma==idProforma && i.nroid_proforma == nroidProforma).ToListAsync();
                    // veetiqueta proforma
                    var etiquetaProf = await _context.veetiqueta_proforma.Where(i => i.id == idProforma && i.numeroid == nroidProforma).ToListAsync();


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

                        cabecera = cabecera,
                        detalle = detalle,
                        descuentos = descuentosExtra,
                        recargos = recargos,
                        iva = iva,
                        profEtiqueta = profEtiqueta,
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
                if (reg.codservicio!=null && reg.codservicio > 0)
                {
                    reg.descservicio = await nombres.nombre_servicio(_context, reg.codservicio ?? 0);
                }
            }
            return dtvalidar;
        }

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
