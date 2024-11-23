using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using NuGet.Packaging;
using Polly;
using SIAW.Controllers.ventas.transaccion;
using siaw_DBContext.Data;
using siaw_DBContext.Models;
using siaw_funciones;
using siaw_ws_siat;
using System.Collections.Generic;
using System.Data;
using System.Drawing.Printing;
using System.Drawing;
using System.Linq;

namespace SIAW.Controllers.ventas.modificacion
{
    [Route("api/venta/modif/[controller]")]
    [ApiController]
    public class docmodifvefactura_nsfController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        private readonly siaw_funciones.Configuracion configuracion = new siaw_funciones.Configuracion();
        private readonly siaw_funciones.Ventas ventas = new siaw_funciones.Ventas();
        private readonly siaw_funciones.Cliente cliente = new siaw_funciones.Cliente();
        private readonly siaw_funciones.Funciones funciones = new siaw_funciones.Funciones();
        private readonly siaw_funciones.Cobranzas cobranzas = new siaw_funciones.Cobranzas();
        private readonly siaw_funciones.TipoCambio tipocambio = new siaw_funciones.TipoCambio();
        private readonly siaw_funciones.datosProforma datos_proforma = new siaw_funciones.datosProforma();
        private readonly siaw_funciones.Saldos saldos = new siaw_funciones.Saldos();
        private readonly siaw_funciones.Items items = new siaw_funciones.Items();
        private readonly siaw_funciones.Validar_Vta validar_Vta = new siaw_funciones.Validar_Vta();

        private readonly Seguridad seguridad = new Seguridad();
        private readonly SIAT siat = new SIAT();
        private readonly Empresa empresa = new Empresa();
        private readonly Documento documento = new Documento();
        private readonly Log log = new Log();
        private readonly Nombres nombres = new Nombres();
        private readonly Contabilidad contabilidad = new Contabilidad();
        private readonly Almacen almacen = new Almacen();
        private readonly Restricciones restricciones = new Restricciones();

        private readonly ServFacturas serv_Facturas = new ServFacturas();
        private readonly Funciones_SIAT funciones_SIAT = new Funciones_SIAT();
        private readonly Adsiat_Parametros_facturacion adsiat_Parametros_Facturacion = new Adsiat_Parametros_facturacion();
        private readonly Adsiat_Mensaje_Servicio adsiat_Mensaje_Servicio = new Adsiat_Mensaje_Servicio();
        private readonly GZip gzip = new GZip();

        private readonly impresoraTermica_3 impresoraTermica = new impresoraTermica_3();

        private readonly string _controllerName = "docmodifvefactura_nsfController";

        public docmodifvefactura_nsfController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }

        [HttpGet]
        [Route("getDataInicial/{userConn}/{usuario}")]
        public async Task<object> getDataInicial(string userConn, string usuario)
        {
            try
            {
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    // tipo pago?
                    // MARVIN DEBE COLOCAR DE MANERA HADCODE EN LA LISTA CONTADO - CREDITO
                    // id's ?

                    // tipo doc id's?

                    // ultimo codigo de factura grabado.
                    var ultimo = await _context.vefactura.MaxAsync(i => i.codigo);

                    // poner almacen saldo ? 

                    // si puede anular ?
                    bool btnanular_en_el_sin = await configuracion.Usuario_Ver_Boton_Anular_SIN(_context, usuario);
                    // si puede generar XML? 
                    bool btn_generar_xml_firmar_enviar = await configuracion.Usuario_Ver_Boton_Generar_XML(_context, usuario);

                    return Ok(new
                    {
                        codUltimaFact = ultimo,
                        btnanular_en_el_sin,
                        btn_generar_xml_firmar_enviar
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
        [Route("getCodigoFact/{userConn}/{id}/{nroId}")]
        public async Task<object> getCodigoFact(string userConn, string id, int nroId)
        {
            try
            {
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var codigoFact = await _context.vefactura.Where(i => i.id == id && i.numeroid == nroId).Select(i => new
                    {
                        i.codigo
                    }).FirstOrDefaultAsync();
                    if (codigoFact == null)
                    {
                        return BadRequest(new { resp = "No se encontro ese número de documento." });
                    }
                    return Ok(new
                    {
                        codigoFact
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
        [Route("mostrarDatosFact/{userConn}/{codigodoc}/{usuario}")]
        public async Task<object> mostrarDatosFact(string userConn, int codigodoc, string usuario)
        {
            try
            {
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    bool verDetalle = true;
                    if (await ventas.ValidarMostrarDocumento(_context,TipoDocumento_Ventas.Factura, codigodoc,usuario) == false)
                    {
                        verDetalle = false;
                    }
                    // cabecera
                    var cabecera = await _context.vefactura.Where(i => i.codigo == codigodoc).FirstOrDefaultAsync();

                    cabecera.condicion = cabecera.condicion == null ? "" : cabecera.condicion;
                    string notadebito = "";
                    try
                    {
                        notadebito = cabecera.notadebito == true ? "NOTA DE DEBITO" : "FACTURA";
                    }
                    catch (Exception)
                    {
                        notadebito = "FACTURA";
                    }
                    cabecera.codigocontrol = cabecera.codigocontrol == null ? "" : cabecera.codigocontrol;
                    cabecera.nroautorizacion = cabecera.nroautorizacion == null ? "" : cabecera.nroautorizacion;

                    string txtversion_codcontrol = "";
                    // mostrar la version del codigo de control si es que genera el nro de autorizacion
                    if (await ventas.DosificacionGeneraCodigo(_context,cabecera.nrocaja, cabecera.codalmacen))
                    {
                        txtversion_codcontrol = (await ventas.version_codcontrol_de_nroautorizacion(_context, cabecera.nrocaja, cabecera.nroautorizacion)).ToString();
                    }
                    else
                    {
                        txtversion_codcontrol = "No Genera";
                    }
                    cabecera.nrolugar = cabecera.nrolugar == null ? "" : cabecera.nrolugar;
                    cabecera.tipo = cabecera.tipo == null ? "" : cabecera.tipo;
                    cabecera.codtipo_comprobante = cabecera.codtipo_comprobante == null ? "" : cabecera.codtipo_comprobante;

                    cabecera.idcuenta = cabecera.idcuenta == null ? "" : cabecera.idcuenta;
                    cabecera.codtipopago = cabecera.codtipopago == null ? 0 : cabecera.codtipopago;

                    // codtipopagomostrar()

                    cabecera.codbanco = cabecera.codbanco == null ? "" : cabecera.codbanco;
                    cabecera.codcuentab = cabecera.codcuentab == null ? "" : cabecera.codcuentab;
                    cabecera.nrocheque = cabecera.nrocheque == null ? "" : cabecera.nrocheque;

                    string codtipo_comprobantedescripcion = await nombres.nombretipo_comprobante(_context, cabecera.codtipo_comprobante);
                    string codtipopagodescripcion = await nombres.nombretipopago(_context, cabecera.codtipopago ?? 0);
                    string codbancodescripcion = await nombres.nombrebanco(_context, cabecera.codbanco);
                    string codcuentabdescripcion = await nombres.nombrecuentabancaria(_context, cabecera.codcuentab);

                    cabecera.idfc = cabecera.idfc == null ? "" : cabecera.idfc;
                    cabecera.numeroidfc = cabecera.numeroidfc == null ? 0 : cabecera.numeroidfc;

                    string estadodoc = cabecera.anulada == true ? "ANULADA" : "";

                    cabecera.contra_entrega = cabecera.contra_entrega == null ? false : cabecera.contra_entrega;

                    // recupera los datos del estado de pago si es contraentrega
                    /*
                     
                    If contra_entrega.Checked = True Then
                        cmbestado_contra_entrega.Enabled = True
                        If tablacabecera.Columns.Contains("estado_contra_entrega") Then
                            If tablacabecera.Rows(0)("estado_contra_entrega") = "" Then
                                cmbestado_contra_entrega.SelectedIndex = -1
                            Else
                                cmbestado_contra_entrega.SelectedIndex = cmbestado_contra_entrega.FindStringExact(tablacabecera.Rows(0)("estado_contra_entrega"))
                            End If
                        End If
                    Else
                        cmbestado_contra_entrega.Enabled = True
                        cmbestado_contra_entrega.SelectedIndex = -1
                    End If


                    If tipopago.SelectedIndex = 0 Then
                        If sia_funciones.Seguridad.Instancia.rol_contabiliza(sia_funciones.Seguridad.Instancia.usuario_rol(sia_compartidos.temporales.Instancia.usuario)) Then
                            If tablacabecera.Rows(0)("contabilizado") Then
                                btnContabilizar.Enabled = False
                                btnDescontabilizar.Enabled = True
                            Else
                                btnContabilizar.Enabled = True
                                btnDescontabilizar.Enabled = False
                            End If
                        Else
                            btnContabilizar.Enabled = False
                            btnDescontabilizar.Enabled = False
                        End If
                    Else
                        btnContabilizar.Enabled = False
                        btnDescontabilizar.Enabled = False
                    End If
                     
                     */
                    // recuperar el CUFD
                    cabecera.cufd = cabecera.cufd == null ? "" : cabecera.cufd;
                    // recuperar el CUF
                    cabecera.cuf = cabecera.cuf == null ? "" : cabecera.cuf;
                    // recuperar el Codigo Factura Web
                    cabecera.codfactura_web = cabecera.codfactura_web == null ? "" : cabecera.codfactura_web;
                    // recuperar el cod_recepcion_siat
                    cabecera.cod_recepcion_siat = cabecera.cod_recepcion_siat == null ? "" : cabecera.cod_recepcion_siat;
                    // recuperar el cod_recepcion_siat
                    cabecera.cod_estado_siat = cabecera.cod_estado_siat == null ? 0 : cabecera.cod_estado_siat;

                    string txt_desc_estado_siat = await adsiat_Mensaje_Servicio.Descripcion_Codigo(_context, cabecera.cod_estado_siat ?? 0);
                    if (txt_desc_estado_siat == "NSE")
                    {
                        switch (cabecera.cod_estado_siat)
                        {
                            case 690:
                                txt_desc_estado_siat = "VALIDA";
                                break;
                            case 908:
                                txt_desc_estado_siat = "VALIDADA";
                                break;
                            case 2170:
                                txt_desc_estado_siat = "VALIDA OBSERVADA";
                                break;
                            case 691:
                                txt_desc_estado_siat = "ANULADA";
                                break;
                            case 905:
                                txt_desc_estado_siat = "ANULACION CONFIRMADA";
                                break;
                            case 2654:
                                txt_desc_estado_siat = "ANULADO OBSERVADO";
                                break;
                            default:
                                txt_desc_estado_siat = "SIN ESTADO";
                                break;
                        }
                    }


                    // cargar recargos
                    List<recargosData> recargos = await cargarrecargo(_context, codigodoc);
                    // cargar descuentos
                    List<descuentosData> descuentos = await cargardesextra(_context, codigodoc);
                    // cargar IVA
                    List<ivaData> ivas = await cargariva(_context, codigodoc);
                    // cargar detalle
                    List<dataDetalleFactura> detalle = await mostrardetalle(_context, codigodoc);

                    bool periodoAbierto = await seguridad.periodo_fechaabierta(userConnectionString, cabecera.fecha, 3);

                    return Ok(new
                    {
                        verDetalle,
                        notadebito,
                        txtversion_codcontrol,
                        codtipo_comprobantedescripcion,
                        codtipopagodescripcion,
                        codbancodescripcion,
                        codcuentabdescripcion,
                        estadodoc,
                        txt_desc_estado_siat,


                        cabecera = cabecera,
                        detalle = detalle,
                        recargos = recargos,
                        descuentos = descuentos,
                        ivaData = ivas,
                    });

                }

            }
            catch (Exception ex)
            {
                return Problem($"Error en el servidor: {ex.Message}");
                throw;
            }
        }


        private async Task<List<recargosData>> cargarrecargo(DBContext _context, int codigo)
        {
            List<recargosData> recargos = await _context.verecargoremi.Where(i => i.codremision == codigo)
                .Join(_context.verecargo,
                r => r.codrecargo,
                v => v.codigo,
                (r, v) => new { r, v })
                .Select(i => new recargosData
                {
                    codrecargo = i.r.codrecargo,
                    descripcion = i.v.descripcion,
                    porcen = i.r.porcen,
                    monto = i.r.monto ?? 0,
                    moneda = i.r.moneda,
                    montodoc = i.r.montodoc,
                    codcobranza = i.r.codcobranza ?? 0
                }).ToListAsync();
            return recargos;
        }

        private async Task<List<descuentosData>> cargardesextra(DBContext _context, int codigo)
        {
            List<descuentosData> descuentos = await _context.vedesextrafact
                       .Join(_context.vedesextra,
                       p => p.coddesextra,
                       e => e.codigo,
                       (p, e) => new { p, e })
                       .Where(i => i.p.codfactura == codigo)
                       .Select(i => new descuentosData
                       {
                           coddesextra = i.p.coddesextra,
                           descripcion = i.e.descripcion,
                           porcen = i.p.porcen,
                           montodoc = i.p.montodoc
                       })
                       .ToListAsync();
            return descuentos;
        }

        private async Task<List<ivaData>> cargariva(DBContext _context, int codigo)
        {
            List<ivaData> ivaFact = await _context.vefactura_iva.Where(i => i.codfactura == codigo)
                .Select(i => new ivaData
                {
                    codfactura = i.codfactura ?? 0,
                    porceniva = i.porceniva ?? 0,
                    total = i.total ?? 0,
                    porcenbr = i.porcenbr ?? 0,
                    br = i.br ?? 0,
                    iva = i.iva ?? 0
                })
                .OrderBy(i => i.porceniva).ToListAsync();
            return ivaFact;
        }

        private async Task<List<dataDetalleFactura>> mostrardetalle(DBContext _context, int codigo)
        {
            List<dataDetalleFactura> detalle = await _context.vefactura1.Where(i => i.codfactura == codigo)
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
                    preciodist = i.p.preciodist ?? 0,
                    totaldist = i.p.totaldist ?? 0,
                })
                .ToListAsync();
            return detalle;
        }















        [HttpGet]
        [Route("refrescarLogs/{userConn}/{id}/{nroId}")]
        public async Task<object> refrescarLogs(string userConn, string id, int nroId)
        {
            try
            {
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    List<selog_siat> logs = await _context.selog_siat.Where(i => i.id_doc == id && i.numeroid_doc == nroId.ToString())
                        .OrderBy(i => i.fecha)
                        .ThenBy(i => i.hora)
                        .ToListAsync();
                    return Ok(logs);
                }

            }
            catch (Exception ex)
            {
                return Problem($"Error en el servidor: {ex.Message}");
                throw;
            }
        }








        [HttpPost]
        [Route("reenviarFacturaEmail/{userConn}/{codempresa}/{usuario}/{codFactura}")]
        public async Task<object> reenviarFacturaEmail(string userConn, string codempresa, string usuario, int codFactura, [FromForm] IFormFile pdfFile)
        {
            if (pdfFile == null || pdfFile.Length == 0)
            {
                return BadRequest(new { resp = "No se ha proporcionado un archivo PDF válido." });
            }
            try
            {
                List<string> eventos = new List<string>();
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    //verificar si se envia el mail
                    if (await configuracion.emp_enviar_factura_por_email(_context, codempresa) == false)
                    {
                        string mi_msg = " Envio de facturas en PDF mas archivo XML por email esta deshabilitado!!!";
                        eventos.Add(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fff") + " - " + mi_msg);
                        await log.RegistrarEvento_Siat(_context, usuario, Log.Entidades.SW_Factura, codFactura.ToString(), pdfFile.FileName, pdfFile.FileName, _controllerName, mi_msg, Log.TipoLog_Siat.Creacion);
                        return StatusCode(203, new { eventos = mi_msg, resp = "" });
                    }

                    string email_enviador = await configuracion.Obtener_Email_Origen_Envia_Facturas(_context);
                    string _email_origen_credencial = email_enviador;
                    string _pwd_email_credencial_origen = await configuracion.Obtener_Clave_Email_Origen_Envia_Facturas(_context);

                    if (email_enviador.Trim().Length == 0)
                    {
                        string mi_msg = "No se encontro en la configuracion el email que envia las facturas, consulte con el administrador del sistema.";
                        eventos.Add(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fff") + " - " + mi_msg);
                        await log.RegistrarEvento_Siat(_context, usuario, Log.Entidades.SW_Factura, codFactura.ToString(), "", "", _controllerName, mi_msg, Log.TipoLog_Siat.Creacion);
                        return StatusCode(203, new { eventos = mi_msg, resp = "" });
                    }
                    /*
                     If _email_origen_credencial.Trim.Length = 0 Then
                        mi_msg = "No se encontro en la configuracion el email credencial que envia las facturas, consulte con el administrador del sistema."
                        Registrar_Evento(mi_msg)
                        sia_log.Log.Instancia.RegistrarEvento_Siat(sia_compartidos.temporales.Instancia.usuario, sia_log.Entidades.Factura, CodFacturas_Grabadas(0).ToString, "", "", Me.Name, mi_msg, sia_log.TipoLog.Creacion)
                        Return False
                    End If
                     */
                    if (_pwd_email_credencial_origen.Trim().Length == 0)
                    {
                        string mi_msg = "No se encontro la credencial del email que envia las facturas, consulte con el administrador del sistema.";
                        eventos.Add(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fff") + " - " + mi_msg);
                        await log.RegistrarEvento_Siat(_context, usuario, Log.Entidades.SW_Factura, codFactura.ToString(), "", "", _controllerName, mi_msg, Log.TipoLog_Siat.Creacion);
                        return StatusCode(203, new { eventos = mi_msg, resp = "" });
                    }

                    var DTFC = await _context.vefactura.Where(i => i.codigo == codFactura).Select(i => new
                    {
                        i.codigo,
                        i.id,
                        i.numeroid,
                        i.fecha,
                        i.codcliente,
                        i.nomcliente,
                        i.nit,
                        i.total,
                        i.codmoneda,
                        i.email,
                        i.nrofactura,
                        i.codalmacen,
                    }).FirstOrDefaultAsync();
                    if (DTFC == null)
                    {
                        return BadRequest(new { resp = "No se encontraron datos con el codigo de proform, consulte con el administrador del sistema." });
                    }
                    string titulo = "Pertec SRL le envia adjunto su factura de compra Nro.: " + DTFC.nrofactura.ToString();


                    string detalle = "Señor:";
                    detalle += Environment.NewLine + DTFC.nomcliente;
                    detalle += Environment.NewLine + "Presente.-";
                    detalle += Environment.NewLine;
                    detalle += Environment.NewLine;
                    detalle += Environment.NewLine + "Pertec S.R.L. informa que el día de hoy se emitió la factura electrónica adjunta al presente";
                    detalle += Environment.NewLine + "mensaje. Dicho documento puede ser impreso y utilizado como un documento válido para Crédito Fiscal.";
                    detalle += Environment.NewLine;
                    detalle += Environment.NewLine + "Si tiene problemas en descargar la factura, también puede obtenerla desde nuestra página web:";
                    detalle += Environment.NewLine + "www.pertec.com.bo o a través de su Whatsapp.";
                    detalle += Environment.NewLine;
                    detalle += Environment.NewLine + "En caso de consultas o errores, por favor comunicarse dentro del mes de emisión de la factura";
                    detalle += Environment.NewLine + "con su ejecutivo de ventas y/o Departamento de Servicio al Cliente.";
                    detalle += Environment.NewLine;
                    detalle += Environment.NewLine + "Agradeciendo su preferencia, nos es grato saludarlo.";
                    detalle += Environment.NewLine;
                    detalle += Environment.NewLine + "Atentamente,";
                    detalle += Environment.NewLine + "PERTEC S.R.L.";

                    string direcc_mail_cliente = DTFC.email;

                    // genera XML
                    var datos_certificado_digital = await Definir_Certificado_A_Utilizar(_context, DTFC.codalmacen, codempresa);
                    var datosXML = await Generar_XML_de_Factura(_context, codFactura, codempresa, usuario,
                                DTFC.codalmacen, datos_certificado_digital.ruta_certificado, datos_certificado_digital.Clave_Certificado_Digital);
                    eventos.AddRange(datosXML.eventos);
                    if (datosXML.resul == false)
                    {
                        return StatusCode(203, new { eventos, resp = "No se pudo generar el archivo XML de la Factura!!!" });
                    }
                    string nomArchXML = datosXML.nomArchivoXML;
                    // obtiene PDF de front
                    byte[] pdfBytes;
                    using (var memoryStream = new MemoryStream())
                    {
                        await pdfFile.CopyToAsync(memoryStream);
                        pdfBytes = memoryStream.ToArray();
                    }

                    string currentDirectory = AppDomain.CurrentDomain.BaseDirectory;
                    string pathDirectory = Path.Combine(currentDirectory, "certificado");
                    // rutaFacturaXmlSigned = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "certificado", nomArchivoXML);
                    byte[] xmlFile = System.IO.File.ReadAllBytes(Path.Combine(pathDirectory, nomArchXML));

                    // solo por pruebas cambiaremos el email destino del cliente por uno de nosotros, comentar en produccion
                    direcc_mail_cliente = "analista.nal.informatica1@pertec.com.bo";

                    var resultado = await funciones.EnviarEmailFacturas(direcc_mail_cliente, _email_origen_credencial, _pwd_email_credencial_origen, titulo, detalle, pdfBytes, pdfFile.FileName, xmlFile, nomArchXML, true);
                    if (resultado.result == false)
                    {
                        // envio fallido
                        string mi_msg = "No se pudo enviar la factura y el archivo XML al email del cliente!!!";
                        eventos.Add(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fff") + " - " + mi_msg);
                        await log.RegistrarEvento_Siat(_context, usuario, Log.Entidades.SW_Factura, codFactura.ToString(), "", "", _controllerName, mi_msg, Log.TipoLog_Siat.Creacion);
                        return BadRequest(new { eventos = eventos, resp = resultado.msg });

                    }
                    string mi_msg1 = "La factura y el archivo XML fueron enviados exitosamente al email del cliente!!!";
                    eventos.Add(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fff") + " - " + mi_msg1);
                    await log.RegistrarEvento_Siat(_context, usuario, Log.Entidades.SW_Factura, codFactura.ToString(), "", "", _controllerName, mi_msg1, Log.TipoLog_Siat.Creacion);

                    return Ok(new { eventos = eventos, resp = "El envio del email ha sido exitoso!!!" });

                }

            }
            catch (Exception)
            {

                throw;
            }
        }



        private async Task<(bool result, List<string> msgAlertas, List<string> eventos, string ruta_certificado, string Clave_Certificado_Digital)> Definir_Certificado_A_Utilizar(DBContext _context, int codalmacen, string codempresa)
        {
            List<string> msgAlertas = new List<string>();
            List<string> eventos = new List<string>();
            string ruta_certificado = "";
            string Clave_Certificado_Digital = "";
            if (codalmacen == 0)
            {
                msgAlertas.Add("No se encontró el almacén, lo cual se necesita para definir el certificado digital a utilizar!!!");
                ruta_certificado = "";
                Clave_Certificado_Digital = "";
                eventos.Add(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fff") + " - No se encontró el almacén, lo cual se necesita para definir el certificado digital a utilizar!!!");
                return (false, msgAlertas, eventos, ruta_certificado, Clave_Certificado_Digital);
            }

            int _codAmbiente = await adsiat_Parametros_Facturacion.Ambiente(_context, codalmacen);

            if (_codAmbiente == 1)
            {
                // Certificado para producción
                ruta_certificado = await configuracion.Dircertif_Produccion(_context, codempresa);
                string cadena_descifrada = seguridad.XorString(await configuracion.Pwd_Certif_Produccion(_context, codempresa), "devstring").Trim();
                Clave_Certificado_Digital = cadena_descifrada;
                eventos.Add(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fff") + " - Facturar NR ha establecido el certificado digital de producción para realizar las firmas digitales!!!");
                return (true, msgAlertas, eventos, ruta_certificado, Clave_Certificado_Digital);
            }
            else
            {
                // Certificado para pruebas
                ruta_certificado = await configuracion.Dircertif_Pruebas(_context, codempresa);
                string cadena_descifrada = seguridad.XorString(await configuracion.Pwd_Certif_Pruebas(_context, codempresa), "devstring").Trim();
                Clave_Certificado_Digital = cadena_descifrada;
                eventos.Add(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fff") + " - Facturar NR ha establecido el certificado digital de pruebas para realizar las firmas digitales!!!");
                return (true, msgAlertas, eventos, ruta_certificado, Clave_Certificado_Digital);
            }
        }






        private async Task<(bool resul, List<string> eventos, string nomArchivoXML)> Generar_XML_de_Factura(DBContext _context, int codFactura, string codempresa, string usuario, int codalmacen, string ruta_certificado, string Clave_Certificado_Digital)
        {
            // para devolver lista de registros logs
            List<string> eventos = new List<string>();
            List<string> msgAlertas = new List<string>();
            bool resultado = true;
            string id = "";
            int numeroid = 0;
            string mensaje = "";
            string nit = await empresa.NITempresa(_context, codempresa);
            string rutaFacturaXml = "";
            string rutaFacturaXmlSigned = "";
            string ruta_factura_xml = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "certificado");
            string ruta_factura_xml_signed = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "certificado");


            var Pametros_Facturacion_Ag1 = await siat.Obtener_Parametros_Facturacion(_context, codalmacen);
            ////////////////////////////////////////////////////////////////////////////////////////////
            //CREAR XML - FIRMARLO
            //////////////////////////////////////////////////////////////////////////////////////////////
            string nomArchivoXML = "";
            try
            {
                bool miresultado = true;
                var docfc = await ventas.id_nroid_factura(_context, Convert.ToInt32(codFactura));
                id = docfc.id;
                numeroid = docfc.numeroId;
                // Generar XML Serializado
                int codDocSector = await adsiat_Parametros_Facturacion.TipoDocSector(_context, codalmacen);
                if (codDocSector == 1)
                {
                    //1: FACTURA COMPRA VENTA (2 DECIMALES)
                    // miresultado = await siat.Generar_XML_Factura_Serializado(id, numeroid, codempresa, false);
                    miresultado = false;
                }
                else
                {
                    //35: FACTURA COMPRA VENTA BONIFICACIONES (2 DECIMALES)
                    miresultado = await funciones_SIAT.Generar_XML_Factura_Compra_Venta_Bonificaciones_Serializado(_context, id, numeroid, codempresa, false, usuario);
                }
                if (miresultado)
                {
                    mensaje = "XML generado exitosamente!!!";
                    eventos.Add(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fff") + " - .... " + id + "-" + numeroid + " " + mensaje);
                    await log.RegistrarEvento_Siat(_context, usuario, Log.Entidades.SW_Factura, codFactura.ToString(), id, numeroid.ToString(), _controllerName, mensaje, Log.TipoLog_Siat.Envio_Factura);
                }
                // Firmar XML
                //definir el nombre del archivo
                nomArchivoXML = $"{id}_{numeroid}_Dsig.xml";
                rutaFacturaXml = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "certificado", $"{id}_{numeroid}.xml");
                rutaFacturaXmlSigned = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "certificado", nomArchivoXML);
                if (miresultado)
                {
                    miresultado = await funciones_SIAT.Firmar_XML_Con_SHA256(rutaFacturaXml, ruta_certificado, Clave_Certificado_Digital, rutaFacturaXmlSigned);
                    if (miresultado)
                    {
                        mensaje = "XML Firmado exitosamente";
                        eventos.Add(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fff") + " - .... " + id + "-" + numeroid + " " + mensaje);
                        await log.RegistrarEvento_Siat(_context, usuario, Log.Entidades.SW_Factura, codFactura.ToString(), id, numeroid.ToString(), _controllerName, mensaje, Log.TipoLog_Siat.Envio_Factura);
                    }
                    else
                    {
                        mensaje = "XML no se pudo firmar";
                        eventos.Add(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fff") + " - .... " + id + "-" + numeroid + " " + mensaje);
                        await log.RegistrarEvento_Siat(_context, usuario, Log.Entidades.SW_Factura, codFactura.ToString(), id, numeroid.ToString(), _controllerName, mensaje, Log.TipoLog_Siat.Envio_Factura);
                        resultado = false;
                    }
                }
            }
            catch (Exception ex)
            {
                resultado = false;
                Console.WriteLine(ex.ToString());
            }
            return (resultado, eventos, nomArchivoXML);
        }





        [HttpPost]
        [Route("enviarEmailAnulacion/{userConn}/{txt_cod_estado_siat}/{usuario}/{codFactura}/{codempresa}")]
        public async Task<object> enviarEmailAnulacion(string userConn, int txt_cod_estado_siat, string usuario, int codFactura, string codempresa)
        {
            try
            {
                List<string> eventos = new List<string>();
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);
                string mi_msg = "";

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var dataFact = await _context.vefactura.Where(i => i.codigo == codFactura).Select(i => new
                    {
                        i.id,
                        i.numeroid,
                        i.codcliente,
                        i.nrofactura,
                        i.nomcliente,
                        i.cuf,
                        i.email,
                    }).FirstOrDefaultAsync();
                    if (dataFact == null)
                    {
                        return BadRequest(new { resp = "No se encontró informacion con el codigo de factura proporcionado, consulte con el administrador" });
                    }
                    string idFactura = dataFact.id;
                    int nroIdFact = dataFact.numeroid;
                    string codCliente = dataFact.codcliente;


                    if (txt_cod_estado_siat != 905)
                    {
                        mi_msg = " La factura no a sido anulada!!!";
                        eventos.Add(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fff") + " - " + mi_msg);
                        await log.RegistrarEvento_Siat(_context, usuario, Log.Entidades.SW_Factura, codFactura.ToString(), idFactura, nroIdFact.ToString(), _controllerName, mi_msg, Log.TipoLog_Siat.Envio_Factura);
                        return BadRequest(new { resp = "No se puede enviar el Mail de Anulacion porque la factura no ha sido anulada.!!!", eventos });
                    }
                    // verificar si se envia el mail
                    if (!await configuracion.emp_enviar_factura_por_email(_context, codempresa))
                    {
                        mi_msg = " Envio de email esta deshabilitado!!!";
                        eventos.Add(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fff") + " - " + mi_msg);
                        await log.RegistrarEvento_Siat(_context, usuario, Log.Entidades.SW_Factura, codFactura.ToString(), idFactura, nroIdFact.ToString(), _controllerName, mi_msg, Log.TipoLog_Siat.Envio_Factura);
                        return BadRequest(new { resp = "Envio de email email esta deshabilitado!!!", eventos });
                    }



                    string email_enviador = await configuracion.Obtener_Email_Origen_Envia_Facturas(_context);
                    string _email_origen_credencial = email_enviador;
                    string _pwd_email_credencial_origen = await configuracion.Obtener_Clave_Email_Origen_Envia_Facturas(_context);

                    if (email_enviador.Trim().Length == 0)
                    {
                        mi_msg = "No se encontro en la configuracion el email que envia las facturas, consulte con el administrador del sistema.";
                        eventos.Add(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fff") + " - " + mi_msg);
                        await log.RegistrarEvento_Siat(_context, usuario, Log.Entidades.SW_Factura, codFactura.ToString(), codCliente, idFactura + "-" + nroIdFact.ToString(), _controllerName, mi_msg, Log.TipoLog_Siat.Creacion);
                        return StatusCode(203, new { eventos = eventos, resp = "" });
                    }

                    if (_pwd_email_credencial_origen.Trim().Length == 0)
                    {
                        mi_msg = "No se encontro la credencial del email que envia las facturas, consulte con el administrador del sistema.";
                        eventos.Add(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fff") + " - " + mi_msg);
                        await log.RegistrarEvento_Siat(_context, usuario, Log.Entidades.SW_Factura, codFactura.ToString(), codCliente, idFactura + "-" + nroIdFact.ToString(), _controllerName, mi_msg, Log.TipoLog_Siat.Creacion);
                        return StatusCode(203, new { eventos = eventos, resp = "" });
                    }

                    string titulo = "Pertec SRL le informa que su factura de compra Nro.: " + dataFact.nrofactura.ToString() + " a sido anulada.";


                    string detalle = "Señor:";
                    detalle += Environment.NewLine + dataFact.nomcliente;
                    detalle += Environment.NewLine + "Presente.-";
                    detalle += Environment.NewLine;
                    detalle += Environment.NewLine;
                    detalle += Environment.NewLine + "Pertec S.R.L. informa que el dia de hoy se anulo la factura eléctronica Nro.: " + dataFact.nrofactura.ToString() + ".";
                    detalle += Environment.NewLine + "con Codigo de Autorizacion:";
                    detalle += Environment.NewLine + dataFact.cuf + ". ";
                    detalle += Environment.NewLine;
                    detalle += Environment.NewLine + "En caso de consultas o errores, por favor comunicarse dentro del mes de emisión de la factura";
                    detalle += Environment.NewLine + "con su ejecutivo de ventas y/o Departamento de Servicio al Cliente.";
                    detalle += Environment.NewLine;
                    detalle += Environment.NewLine + "Agradeciendo su preferencia, nos es grato saludarlo.";
                    detalle += Environment.NewLine;
                    detalle += Environment.NewLine + "Atentamente,";
                    detalle += Environment.NewLine + "PERTEC S.R.L.";

                    string direcc_mail_cliente = dataFact.email;


                    // solo por pruebas cambiaremos el email destino del cliente por uno de nosotros, comentar en produccion
                    direcc_mail_cliente = "analista.nal.informatica1@pertec.com.bo";

                    var resultado = await funciones.EnviarEmailFacturas(direcc_mail_cliente, _email_origen_credencial, _pwd_email_credencial_origen, titulo, detalle, null, "", null, "", false);
                    if (resultado.result == false)
                    {
                        // envio fallido
                        return BadRequest(new { eventos = eventos, resp = "Ocurrio un error al enviar el email, consulte con el administrador del sistema!!!" });
                    }
                    return Ok(new { eventos = eventos, resp = "El envio del email ha sido exitoso!!!" });

                }

            }
            catch (Exception ex)
            {
                return Problem($"Error en el servidor: {ex.Message}");
                throw;
            }
        }





        [HttpPost]
        [Route("enviarEmailReverAnulacion/{userConn}/{txt_cod_estado_siat}/{usuario}/{codFactura}/{codempresa}")]
        public async Task<object> enviarEmailReverAnulacion(string userConn, int txt_cod_estado_siat, string usuario, int codFactura, string codempresa)
        {
            try
            {
                List<string> eventos = new List<string>();
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);
                string mi_msg = "";

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var dataFact = await _context.vefactura.Where(i => i.codigo == codFactura).Select(i => new
                    {
                        i.id,
                        i.numeroid,
                        i.codcliente,
                        i.nrofactura,
                        i.nomcliente,
                        i.cuf,
                        i.email,
                    }).FirstOrDefaultAsync();
                    if (dataFact == null)
                    {
                        return BadRequest(new { resp = "No se encontró informacion con el codigo de factura proporcionado, consulte con el administrador" });
                    }
                    string idFactura = dataFact.id;
                    int nroIdFact = dataFact.numeroid;
                    string codCliente = dataFact.codcliente;


                    if (txt_cod_estado_siat != 907)
                    {
                        mi_msg = " La factura no a sido revertida!!!";
                        eventos.Add(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fff") + " - " + mi_msg);
                        await log.RegistrarEvento_Siat(_context, usuario, Log.Entidades.SW_Factura, codFactura.ToString(), idFactura, nroIdFact.ToString(), _controllerName, mi_msg, Log.TipoLog_Siat.Envio_Factura);
                        return BadRequest(new { resp = "No se puede enviar el Mail de Reversion de anulacion porque la factura no ha sido revertida.!!!", eventos });
                    }
                    // verificar si se envia el mail
                    if (! await configuracion.emp_enviar_factura_por_email(_context,codempresa))
                    {
                        mi_msg = " Envio de email esta deshabilitado!!!";
                        eventos.Add(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fff") + " - " + mi_msg);
                        await log.RegistrarEvento_Siat(_context, usuario, Log.Entidades.SW_Factura, codFactura.ToString(), idFactura, nroIdFact.ToString(), _controllerName, mi_msg, Log.TipoLog_Siat.Envio_Factura);
                        return BadRequest(new { resp = "Envio de email email esta deshabilitado!!!", eventos });
                    }

                    

                    string email_enviador = await configuracion.Obtener_Email_Origen_Envia_Facturas(_context);
                    string _email_origen_credencial = email_enviador;
                    string _pwd_email_credencial_origen = await configuracion.Obtener_Clave_Email_Origen_Envia_Facturas(_context);

                    if (email_enviador.Trim().Length == 0)
                    {
                        mi_msg = "No se encontro en la configuracion el email que envia las facturas, consulte con el administrador del sistema.";
                        eventos.Add(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fff") + " - " + mi_msg);
                        await log.RegistrarEvento_Siat(_context, usuario, Log.Entidades.SW_Factura, codFactura.ToString(), codCliente, idFactura + "-" + nroIdFact.ToString(), _controllerName, mi_msg, Log.TipoLog_Siat.Creacion);
                        return StatusCode(203, new { eventos = eventos, resp = "" });
                    }

                    if (_pwd_email_credencial_origen.Trim().Length == 0)
                    {
                        mi_msg = "No se encontro la credencial del email que envia las facturas, consulte con el administrador del sistema.";
                        eventos.Add(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fff") + " - " + mi_msg);
                        await log.RegistrarEvento_Siat(_context, usuario, Log.Entidades.SW_Factura, codFactura.ToString(), codCliente, idFactura + "-" + nroIdFact.ToString(), _controllerName, mi_msg, Log.TipoLog_Siat.Creacion);
                        return StatusCode(203, new { eventos = eventos, resp = "" });
                    }

                    string titulo = "Pertec SRL le informa que su factura de compra Nro.: " + dataFact.nrofactura.ToString() + " la anulacion a sido Revertida.";


                    string detalle = "Señor:";
                    detalle += Environment.NewLine + dataFact.nomcliente;
                    detalle += Environment.NewLine + "Presente.-";
                    detalle += Environment.NewLine;
                    detalle += Environment.NewLine;
                    detalle += Environment.NewLine + "Pertec S.R.L. informa que el dia de hoy se revertio la anulacion de la factura eléctronica Nro.: " + dataFact.nrofactura.ToString() + ".";
                    detalle += Environment.NewLine + "con Codigo de Autorizacion:";
                    detalle += Environment.NewLine + dataFact.cuf + ". ";
                    detalle += Environment.NewLine;
                    detalle += Environment.NewLine + "En caso de consultas o errores, por favor comunicarse dentro del mes de emisión de la factura";
                    detalle += Environment.NewLine + "con su ejecutivo de ventas y/o Departamento de Servicio al Cliente.";
                    detalle += Environment.NewLine;
                    detalle += Environment.NewLine + "Agradeciendo su preferencia, nos es grato saludarlo.";
                    detalle += Environment.NewLine;
                    detalle += Environment.NewLine + "Atentamente,";
                    detalle += Environment.NewLine + "PERTEC S.R.L.";

                    string direcc_mail_cliente = dataFact.email;
                    

                    // solo por pruebas cambiaremos el email destino del cliente por uno de nosotros, comentar en produccion
                    direcc_mail_cliente = "analista.nal.informatica1@pertec.com.bo";

                    var resultado = await funciones.EnviarEmailFacturas(direcc_mail_cliente, _email_origen_credencial, _pwd_email_credencial_origen, titulo, detalle, null, "", null, "",false);
                    if (resultado.result == false)
                    {
                        // envio fallido
                        return BadRequest(new { eventos = eventos, resp = "Ocurrio un error al enviar el email, consulte con el administrador del sistema!!!" });
                    }
                    return Ok(new { eventos = eventos, resp = "El envio del email ha sido exitoso!!!" });

                }

            }
            catch (Exception ex)
            {
                return Problem($"Error en el servidor: {ex.Message}");
                throw;
            }
        }


        [HttpGet]
        [Route("autorizaReimpresion/{userConn}/{codalmacen}/{nroautorizacion}/{codFactura}")]
        public async Task<object> autorizaReimpresion(string userConn, int codalmacen, string nroautorizacion, int codFactura)
        {
            try
            {
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (await ventas.nroautorizacion_tipofactura(_context, nroautorizacion) == 1)
                    {
                        return BadRequest(new { resp = "Atencion!!! como la factura es de una dosificacion manual, no se puede imprimir." });
                    }
                    // SE NECESITA PERMISO ESPECIAL
                    // se verifica si es tienda o noy se devuelve a front end
                    bool esTienda = await almacen.Es_Tienda(_context, codalmacen);
                    var datosFact = await _context.vefactura.Where(i => i.codigo == codFactura).Select(i => new
                    {
                        i.id,
                        i.numeroid,
                        i.codcliente,
                        i.nomcliente,
                        i.subtotal
                    }).FirstOrDefaultAsync();
                    return Ok(new
                    {
                        esTienda = esTienda,

                        servicio = 28,
                        descServicio = "REIMPRESION DE FACTURA",
                        datosDoc = datosFact.id + "-" + datosFact.numeroid + ": " + datosFact.codcliente + "-" + datosFact.nomcliente + " Total: " + datosFact.subtotal,
                        dato_a = datosFact.id,
                        dato_b = datosFact.numeroid
                    });
                }

            }
            catch (Exception ex)
            {
                return Problem($"Error en el servidor: {ex.Message}");
                throw;
            }
        }






        [HttpPost]
        [Route("HabilitarFactura/{userConn}/{usuario}/{codFactura}/{codempresa}/{sin_validar_doc_ant_inv}")]
        public async Task<object> HabilitarFactura(string userConn, string usuario, int codFactura, string codempresa, bool sin_validar_modif_inventario)
        {
            List<string> eventos = new List<string>();
            string mi_msg = "";
            bool resultado = true;
            string mensaje = "";
            int codigo_control = 0;
            // VALIDACIONES 
            if (string.IsNullOrWhiteSpace(userConn)) { return BadRequest(new { resp = "No se ha proporcionado el valor del dato 'UserConn'. Consulte con el Administrador del sistema." }); }
            if (string.IsNullOrWhiteSpace(usuario)) { return BadRequest(new { resp = "No se ha proporcionado el valor del dato 'Usuario'. Consulte con el Administrador del sistema." }); }
            if (string.IsNullOrWhiteSpace(codempresa)) { return BadRequest(new { resp = "No se ha proporcionado el valor del dato 'Empresa'. Consulte con el Administrador del sistema." }); }
            if (codFactura <= 0) { return BadRequest(new { resp = "El valor de 'Codigo de Factura' no puede ser cero o menor a cero. Consulte con el Administrador del sistema." }); }
            if (sin_validar_modif_inventario == null) { return BadRequest(new { resp = "No se ha proporcionado el valor del dato 'Sin Validar Inventario'. Consulte con el Administrador del sistema." }); }

            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);
            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                try
                {
                    var dataFact = await _context.vefactura.Where(i => i.codigo == codFactura).Select(i => new
                    {
                        i.id,
                        i.numeroid,
                        i.codremision,
                        i.codalmacen,
                        i.codcliente,
                        i.nrofactura,
                        i.nomcliente,
                        i.cuf,
                        i.fecha,
                        i.anulada,
                        i.descarga,
                    }).FirstOrDefaultAsync();
                    if (dataFact == null)
                    {
                        mensaje = "No se encontró informacion con el codigo de factura proporcionado, consulte con el administrador";
                        resultado = false;
                        codigo_control = 0;
                        return StatusCode(203, new { resp = resultado, mensaje, eventos, codigo_control });
                    }
                    string idFactura = "";
                    if (dataFact.id == null)
                    {
                        idFactura = "";
                    }
                    else
                    {
                        idFactura = dataFact.id;
                    }

                    int nroIdFact = dataFact.numeroid;
                    int codremision = (int)dataFact.codremision;
                    int codalmacen = dataFact.codalmacen;
                    string codCliente = dataFact.codcliente;
                    DateTime fecha = dataFact.fecha;
                    bool anulada = dataFact.anulada;
                    bool descarga = dataFact.descarga;

                    if (await seguridad.periodo_fechaabierta_context(_context, fecha, 3))
                    { }
                    else
                    {
                        resultado = false;
                        mensaje = "No puede modificar documentos para ese periodo de fechas.";
                        codigo_control = 0;
                        //return BadRequest(new { resp = false, msgAlert = "No puede modificar documentos para ese periodo de fechas." });
                        return StatusCode(203, new { resp = resultado, mensaje, eventos, codigo_control });
                    }

                    if (sin_validar_modif_inventario == false)
                    {
                        bool validar_modif_inventario = await restricciones.ValidarModifDocAntesInventario(_context, codalmacen, fecha);
                        if (validar_modif_inventario)
                        {
                        }
                        else
                        {
                            mensaje = "No puede modificar datos anteriores al ultimo inventario, Para eso necesita una autorizacion especial.";
                            codigo_control = 48;
                            resultado = false;
                            return StatusCode(203, new { resp = resultado, mensaje, eventos, codigo_control });
                        }
                    }

                    if (!anulada)
                    {
                        //return BadRequest(new { resp = false, msgAlert = "Esta Factura esta Habilitada." }); 
                        mensaje = "Esta Factura esta Habilitada.";
                        codigo_control = 0;
                        resultado = false;
                        return StatusCode(203, new { resp = resultado, mensaje, eventos, codigo_control });

                    }
                    else
                    {
                        if (codremision > 0)
                        {
                            // Cambiar el dato  de trasnferida a la nota de remision
                            // 'Desde 01-12-2022 cuando se habilita una factura ya no se cambiara el datos de fechareg ni horareg, debido a que esto causa en ocasion que al anular una factura que aun no a sido VALIDADA EN EL SIN
                            try
                            {
                                var remision = await _context.veremision.Where(i => i.codigo == codremision).FirstOrDefaultAsync();
                                remision.transferida = true;
                                await _context.SaveChangesAsync();
                            }
                            catch (Exception ex)
                            {
                                //return BadRequest(new { resp = false, msgAlert = "No se pudo cambiar a transferida la nota de remision de la factura." });
                                mensaje = "No se pudo cambiar a transferida la nota de remision de la factura.";
                                codigo_control = 0;
                                resultado = false;
                                return StatusCode(203, new { resp = resultado, mensaje, eventos, codigo_control });
                                // throw;
                            }

                            try
                            {
                                var factura = await _context.vefactura.Where(i => i.codigo == codFactura).FirstOrDefaultAsync();
                                factura.anulada = false;
                                await _context.SaveChangesAsync();
                                mi_msg = idFactura + "-" + nroIdFact.ToString() + " Habiliacion local Exitosa de Factura";
                                eventos.Add(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fff") + " - " + mi_msg);
                            }
                            catch (Exception ex)
                            {
                                // return BadRequest(new { resp = false, msgAlert = "No se pudo Habilitar la factura." });
                                mensaje = "No se pudo Habilitar la factura.";
                                codigo_control = 0;
                                resultado = false;
                                return StatusCode(203, new { resp = resultado, mensaje, eventos, codigo_control });
                                //throw;
                            }
                            try
                            {
                                var remito = await _context.veremito.Where(i => i.idfactura == idFactura && i.numeroidfactura == nroIdFact).FirstOrDefaultAsync();
                                remito.anulado = false;
                                remito.horareg = await funciones.hora_del_servidor_cadena(_context);
                                remito.fechareg = await funciones.FechaDelServidor(_context);
                                remito.usuarioreg = usuario;
                                await _context.SaveChangesAsync();

                                if (descarga)
                                {
                                    var actualiza_saldo = await saldos.Vefactura_ActualizarSaldo(_context, codFactura, Saldos.ModoActualizacion.Crear);
                                    if (!actualiza_saldo)
                                    {
                                        mi_msg = idFactura + "-" + nroIdFact.ToString() + " No se pudo actualizar el saldo de la factura.";
                                        eventos.Add(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fff") + " - " + mi_msg);
                                        await log.RegistrarEvento(_context, usuario, Log.Entidades.SW_Factura, codFactura.ToString(), idFactura, nroIdFact.ToString(), _controllerName, mi_msg, Log.TipoLog.Modificacion);
                                    }

                                }
                                //No se hizo esta funcion porque es para uso de argentina
                                //sia_funciones.Inventario.Instancia.actaduanafactura(codigo.Text, "crear", fecha.Value.Date)
                                mi_msg = "Habilitar";
                                await log.RegistrarEvento(_context, usuario, Log.Entidades.SW_Factura, codFactura.ToString(), idFactura, nroIdFact.ToString(), _controllerName, mi_msg, Log.TipoLog.Modificacion);
                            }
                            catch (Exception ex)
                            {
                                // return BadRequest(new { resp = false, msgAlert = "No se pudo Habilitar la factura." });
                                mensaje = "No se pudo Habilitar la factura.";
                                codigo_control = 0;
                                resultado = false;
                                return StatusCode(203, new { resp = resultado, mensaje, eventos, codigo_control });
                                throw;
                            }

                            return Ok(new
                            {
                                resp = resultado,
                                msgAlert = "Se Habilito la Factura en el SIAW con exito."
                            });
                        }
                        else
                        {
                            try
                            {
                                var factura = await _context.vefactura.Where(i => i.codigo == codFactura).FirstOrDefaultAsync();
                                factura.anulada = false;
                                await _context.SaveChangesAsync();
                                mi_msg = idFactura + "-" + nroIdFact.ToString() + " Habiliacion local Exitosa de Factura";
                                eventos.Add(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fff") + " - " + mi_msg);
                            }
                            catch (Exception ex)
                            {
                                //return BadRequest(new { resp = false, msgAlert = "No se pudo Habilitar la factura." });
                                mensaje = "No se pudo Habilitar la factura.";
                                codigo_control = 0;
                                resultado = false;
                                return StatusCode(203, new { resp = resultado, mensaje, eventos, codigo_control });
                                //throw;
                            }
                            try
                            {
                                var remito = await _context.veremito.Where(i => i.idfactura == idFactura && i.numeroidfactura == nroIdFact).FirstOrDefaultAsync();
                                remito.anulado = false;
                                remito.horareg = await funciones.hora_del_servidor_cadena(_context);
                                remito.fechareg = await funciones.FechaDelServidor(_context);
                                remito.usuarioreg = usuario;
                                await _context.SaveChangesAsync();

                                if (descarga)
                                {
                                    var actualiza_saldo = await saldos.Vefactura_ActualizarSaldo(_context, codFactura, Saldos.ModoActualizacion.Crear);
                                    if (!actualiza_saldo)
                                    {
                                        mi_msg = idFactura + "-" + nroIdFact.ToString() + " No se pudo actualizar el saldo de la factura.";
                                        eventos.Add(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fff") + " - " + mi_msg);
                                        await log.RegistrarEvento(_context, usuario, Log.Entidades.SW_Factura, codFactura.ToString(), idFactura, nroIdFact.ToString(), _controllerName, mi_msg, Log.TipoLog.Modificacion);
                                    }

                                }
                                //No se hizo esta funcion porque es para uso de argentina
                                //sia_funciones.Inventario.Instancia.actaduanafactura(codigo.Text, "crear", fecha.Value.Date)
                                mi_msg = "Habilitar";
                                await log.RegistrarEvento(_context, usuario, Log.Entidades.SW_Factura, codFactura.ToString(), idFactura, nroIdFact.ToString(), _controllerName, mi_msg, Log.TipoLog.Modificacion);
                            }
                            catch (Exception ex)
                            {
                                // return BadRequest(new { resp = false, msgAlert = "No se pudo Habilitar la factura." });
                                mensaje = "No se pudo Habilitar la factura.";
                                codigo_control = 0;
                                resultado = false;
                                return StatusCode(203, new { resp = resultado, mensaje, eventos, codigo_control });
                                throw;
                            }

                            return Ok(new
                            {
                                resp = resultado,
                                eventos = eventos,
                                msgAlert = "Se Habilito la Factura en el SIAW con exito."
                            });
                        }
                    }

                    /////**************************
                }
                catch (Exception ex)
                {
                    return Problem($"Error en el servidor al Habilitar FC: {ex.Message}");
                }
            }

        }

        [HttpPost]
        [Route("Cambiar_en_Linea_SIN/{userConn}/{usuario}/{codFactura}/{codempresa}/{en_linea}/{sin_validar_pedir_clave}")]
        public async Task<object> Cambiar_en_Linea_SIN(string userConn, string usuario, int codFactura, string codempresa, bool en_linea_SIN, bool sin_validar_pedir_clave)
        {
            List<string> eventos = new List<string>();
            bool resultado = true;
            string mensaje = "";
            int codigo_control = 0;
            // VALIDACIONES 
            if (string.IsNullOrWhiteSpace(userConn)) { return BadRequest(new { resp = "No se ha proporcionado el valor del dato 'UserConn'. Consulte con el Administrador del sistema." }); }
            if (string.IsNullOrWhiteSpace(usuario)) { return BadRequest(new { resp = "No se ha proporcionado el valor del dato 'Usuario'. Consulte con el Administrador del sistema." }); }
            if (string.IsNullOrWhiteSpace(codempresa)) { return BadRequest(new { resp = "No se ha proporcionado el valor del dato 'Empresa'. Consulte con el Administrador del sistema." }); }
            if (codFactura <= 0) { return BadRequest(new { resp = "El valor de 'Codigo de Factura' no puede ser cero o menor a cero. Consulte con el Administrador del sistema." }); }
            if (en_linea_SIN == null) { return BadRequest(new { resp = "No se ha proporcionado el valor del dato 'En linea SIN'. Consulte con el Administrador del sistema." }); }
            if (sin_validar_pedir_clave == null) { return BadRequest(new { resp = "No se ha proporcionado el valor del dato 'Sin Validar Clave'. Consulte con el Administrador del sistema." }); }

            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);
            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                try
                {
                    var dataFact = await _context.vefactura.Where(i => i.codigo == codFactura).FirstOrDefaultAsync();

                    if (dataFact == null)
                    {
                        //return BadRequest(new { resp = mi_msg, eventos });
                        mensaje = "No se encontró informacion con el codigo de factura proporcionado, consulte con el administrador";
                        resultado = false;
                        codigo_control = 0;
                        eventos.Add(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fff") + " - " + mensaje);
                        return StatusCode(203, new { resp = resultado, mensaje, eventos, codigo_control });
                    }

                    if (sin_validar_pedir_clave == false)
                    {
                        mensaje = "Para esto necesita una autorizacion especial.";
                        resultado = false;
                        codigo_control = 142;
                        //eventos.Add(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fff") + " - " + mensaje);
                        return StatusCode(203, new { resp = resultado, mensaje, eventos, codigo_control });
                    }
                    string idFactura = dataFact.id;
                    int nroIdFact = dataFact.numeroid;
                    if (en_linea_SIN)
                    {
                        try
                        {
                            dataFact.en_linea_SIN = false;
                            await _context.SaveChangesAsync();
                            mensaje = "La factura fue cambiada a: Fuera de Linea en el SIN";
                            eventos.Add(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fff") + " - " + mensaje);
                            await log.RegistrarEvento_Siat(_context, usuario, Log.Entidades.SW_Factura, codFactura.ToString(), idFactura, nroIdFact.ToString(), _controllerName, mensaje, Log.TipoLog_Siat.Modificacion_Estado_En_Linea);
                        }
                        catch (Exception ex)
                        {
                            mensaje = "Ocurrio un Error y no se pudo cambiar el estado de la factura!!!";
                            resultado = false;
                            codigo_control = 0;
                            eventos.Add(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fff") + " - " + mensaje);
                            await log.RegistrarEvento_Siat(_context, usuario, Log.Entidades.SW_Factura, codFactura.ToString(), idFactura, nroIdFact.ToString(), _controllerName, mensaje, Log.TipoLog_Siat.Modificacion_Estado_En_Linea);
                            return StatusCode(203, new { resp = resultado, mensaje, eventos, codigo_control });
                        }
                    }
                    else
                    {
                        try
                        {
                            dataFact.en_linea_SIN = true;
                            await _context.SaveChangesAsync();
                            mensaje = "La factura fue cambiada a: En Linea en el SIN";
                            eventos.Add(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fff") + " - " + mensaje);
                            await log.RegistrarEvento_Siat(_context, usuario, Log.Entidades.SW_Factura, codFactura.ToString(), idFactura, nroIdFact.ToString(), _controllerName, mensaje, Log.TipoLog_Siat.Modificacion_Estado_En_Linea);
                        }
                        catch (Exception ex)
                        {
                            mensaje = "Ocurrio un Error y no se pudo cambiar el estado de la factura!!!";
                            resultado = false;
                            codigo_control = 0;
                            eventos.Add(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fff") + " - " + mensaje);
                            await log.RegistrarEvento_Siat(_context, usuario, Log.Entidades.SW_Factura, codFactura.ToString(), idFactura, nroIdFact.ToString(), _controllerName, mensaje, Log.TipoLog_Siat.Modificacion_Estado_En_Linea);
                            return StatusCode(203, new { resp = resultado, mensaje, eventos, codigo_control });
                        }
                    }
                    return Ok(new
                    {
                        resp = resultado,
                        eventos = eventos,
                        msgAlert = mensaje
                    });
                    /////**************************
                }
                catch (Exception ex)
                {
                    return Problem($"Error en el servidor al Habilitar FC: {ex.Message}");
                }
            }

        }


        [HttpPost]
        [Route("Cambiar_en_Linea/{userConn}/{usuario}/{codFactura}/{codempresa}/{en_linea}/{sin_validar_pedir_clave}")]
        public async Task<object> Cambiar_en_Linea(string userConn, string usuario, int codFactura, string codempresa, bool en_linea, bool sin_validar_pedir_clave)
        {
            List<string> eventos = new List<string>();
            bool resultado = true;
            string mensaje = "";
            int codigo_control = 0;
            // VALIDACIONES 
            if (string.IsNullOrWhiteSpace(userConn)) { return BadRequest(new { resp = "No se ha proporcionado el valor del dato 'UserConn'. Consulte con el Administrador del sistema." }); }
            if (string.IsNullOrWhiteSpace(usuario)) { return BadRequest(new { resp = "No se ha proporcionado el valor del dato 'Usuario'. Consulte con el Administrador del sistema." }); }
            if (string.IsNullOrWhiteSpace(codempresa)) { return BadRequest(new { resp = "No se ha proporcionado el valor del dato 'Empresa'. Consulte con el Administrador del sistema." }); }
            if (codFactura <= 0) { return BadRequest(new { resp = "El valor de 'Codigo de Factura' no puede ser cero o menor a cero. Consulte con el Administrador del sistema." }); }
            if (en_linea == null) { return BadRequest(new { resp = "No se ha proporcionado el valor del dato 'En linea'. Consulte con el Administrador del sistema." }); }
            if (sin_validar_pedir_clave == null) { return BadRequest(new { resp = "No se ha proporcionado el valor del dato 'Sin Validar Clave'. Consulte con el Administrador del sistema." }); }

            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);
            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                try
                {
                    var dataFact = await _context.vefactura.Where(i => i.codigo == codFactura).FirstOrDefaultAsync();

                    if (dataFact == null)
                    {
                        //return BadRequest(new { resp = mi_msg, eventos });
                        mensaje = "No se encontró informacion con el codigo de factura proporcionado, consulte con el administrador";
                        resultado = false;
                        codigo_control = 0;
                        eventos.Add(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fff") + " - " + mensaje);
                        return StatusCode(203, new { resp = resultado, mensaje, eventos, codigo_control });
                    }

                    if (sin_validar_pedir_clave == false)
                    {
                        mensaje = "Para esto necesita una autorizacion especial.";
                        resultado = false;
                        codigo_control = 142;
                        //eventos.Add(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fff") + " - " + mensaje);
                        return StatusCode(203, new { resp = resultado, mensaje, eventos, codigo_control });
                    }
                    string idFactura = dataFact.id;
                    int nroIdFact = dataFact.numeroid;
                    if (en_linea)
                    {
                        try
                        {
                            dataFact.en_linea = false;
                            await _context.SaveChangesAsync();
                            mensaje = "La factura fue cambiada a: Fuera de Linea";
                            eventos.Add(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fff") + " - " + mensaje);
                            await log.RegistrarEvento_Siat(_context, usuario, Log.Entidades.SW_Factura, codFactura.ToString(), idFactura, nroIdFact.ToString(), _controllerName, mensaje, Log.TipoLog_Siat.Modificacion_Estado_En_Linea);
                        }
                        catch (Exception ex)
                        {
                            mensaje = "Ocurrio un Error y no se pudo cambiar el estado de la factura!!!";
                            resultado = false;
                            codigo_control = 0;
                            eventos.Add(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fff") + " - " + mensaje);
                            await log.RegistrarEvento_Siat(_context, usuario, Log.Entidades.SW_Factura, codFactura.ToString(), idFactura, nroIdFact.ToString(), _controllerName, mensaje, Log.TipoLog_Siat.Modificacion_Estado_En_Linea);
                            return StatusCode(203, new { resp = resultado, mensaje, eventos, codigo_control });
                        }
                    }
                    else
                    {
                        try
                        {
                            dataFact.en_linea = true;
                            await _context.SaveChangesAsync();
                            mensaje = "La factura fue cambiada a: En Linea";
                            eventos.Add(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fff") + " - " + mensaje);
                            await log.RegistrarEvento_Siat(_context, usuario, Log.Entidades.SW_Factura, codFactura.ToString(), idFactura, nroIdFact.ToString(), _controllerName, mensaje, Log.TipoLog_Siat.Modificacion_Estado_En_Linea);
                        }
                        catch (Exception ex)
                        {
                            mensaje = "Ocurrio un Error y no se pudo cambiar el estado de la factura!!!";
                            resultado = false;
                            codigo_control = 0;
                            eventos.Add(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fff") + " - " + mensaje);
                            await log.RegistrarEvento_Siat(_context, usuario, Log.Entidades.SW_Factura, codFactura.ToString(), idFactura, nroIdFact.ToString(), _controllerName, mensaje, Log.TipoLog_Siat.Modificacion_Estado_En_Linea);
                            return StatusCode(203, new { resp = resultado, mensaje, eventos, codigo_control });
                        }
                    }
                    return Ok(new
                    {
                        resp = resultado,
                        eventos = eventos,
                        msgAlert = mensaje
                    });
                    /////**************************
                }
                catch (Exception ex)
                {
                    return Problem($"Error en el servidor al Habilitar FC: {ex.Message}");
                }
            }

        }



        [HttpPost]
        [Route("Cambiar_Fecha_Anulacion/{userConn}/{usuario}/{codFactura}/{codempresa}/{fecha}/{sin_validar_pedir_clave}")]
        public async Task<object> Cambiar_Fecha_Anulacion(string userConn, string usuario, int codFactura, string codempresa, string fecha, bool sin_validar_pedir_clave)
        {
            List<string> eventos = new List<string>();
            bool resultado = true;
            string mensaje = "";
            int codigo_control = 0;
            DateTime fecha_anulacion;
            // VALIDACIONES 
            if (string.IsNullOrWhiteSpace(userConn)) { return BadRequest(new { resp = "No se ha proporcionado el valor del dato 'UserConn'. Consulte con el Administrador del sistema." }); }
            if (string.IsNullOrWhiteSpace(usuario)) { return BadRequest(new { resp = "No se ha proporcionado el valor del dato 'Usuario'. Consulte con el Administrador del sistema." }); }
            if (string.IsNullOrWhiteSpace(codempresa)) { return BadRequest(new { resp = "No se ha proporcionado el valor del dato 'Empresa'. Consulte con el Administrador del sistema." }); }
            if (codFactura <= 0) { return BadRequest(new { resp = "El valor de 'Codigo de Factura' no puede ser cero o menor a cero. Consulte con el Administrador del sistema." }); }
            if (fecha == null) { return BadRequest(new { resp = "No se ha proporcionado el valor del dato 'Fecha Anulacion'. Consulte con el Administrador del sistema." }); }
            if (!DateTime.TryParse(fecha, out fecha_anulacion)) { return BadRequest(new { resp = "La fecha de anulacion es invalida. Consulte con el Administrador del sistema." }); }
            if (sin_validar_pedir_clave == null) { return BadRequest(new { resp = "No se ha proporcionado el valor del dato 'Sin Validar Clave'. Consulte con el Administrador del sistema." }); }

            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);
            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                try
                {
                    var dataFact = await _context.vefactura.Where(i => i.codigo == codFactura).FirstOrDefaultAsync();

                    if (dataFact == null)
                    {
                        //return BadRequest(new { resp = mi_msg, eventos });
                        mensaje = "No se encontró informacion con el codigo de factura proporcionado, consulte con el administrador";
                        resultado = false;
                        codigo_control = 0;
                        eventos.Add(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fff") + " - " + mensaje);
                        return StatusCode(203, new { resp = resultado, mensaje, eventos, codigo_control });
                    }

                    if (sin_validar_pedir_clave == false)
                    {
                        mensaje = "Para esto necesita una autorizacion especial.";
                        resultado = false;
                        codigo_control = 75;
                        //eventos.Add(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fff") + " - " + mensaje);
                        return StatusCode(203, new { resp = resultado, mensaje, eventos, codigo_control });
                    }
                    string idFactura = dataFact.id;
                    int nroIdFact = dataFact.numeroid;

                    try
                    {
                        dataFact.fecha_anulacion = fecha_anulacion;
                        dataFact.fechareg = await funciones.FechaDelServidor(_context);
                        await _context.SaveChangesAsync();
                        mensaje = "Cambio fecha anulacion a: " + fecha_anulacion.ToShortDateString();
                        eventos.Add(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fff") + " - " + mensaje);
                        await log.RegistrarEvento(_context, usuario, Log.Entidades.SW_Factura, codFactura.ToString(), idFactura, nroIdFact.ToString(), _controllerName, mensaje, Log.TipoLog.Modificacion);
                    }
                    catch (Exception ex)
                    {
                        mensaje = "Ocurrio un Error y no se pudo cambiar la fecha de anulacion de la factura!!!";
                        resultado = false;
                        codigo_control = 0;
                        eventos.Add(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fff") + " - " + mensaje);
                        await log.RegistrarEvento(_context, usuario, Log.Entidades.SW_Factura, codFactura.ToString(), idFactura, nroIdFact.ToString(), _controllerName, mensaje, Log.TipoLog.Modificacion);
                        return StatusCode(203, new { resp = resultado, mensaje, eventos, codigo_control });
                    }

                    return Ok(new
                    {
                        resp = resultado,
                        eventos = eventos,
                        msgAlert = mensaje
                    });
                    /////**************************
                }
                catch (Exception ex)
                {
                    return Problem($"Error en el servidor al cambiar fecha anulacion FC: {ex.Message}");
                }
            }

        }









        [HttpPost]
        [Route("imprimirSolAnulacion/{userConn}/{codFactura}/{codempresa}")]
        public async Task<object> imprimirSolAnulacion(string userConn, int codFactura, string codempresa, dataSolAnulacion dataSolAnulacion)
        {
            if (dataSolAnulacion.nom_solicita_anulacion.Trim().Length == 0)
            {
                return BadRequest(new { resp = "Debe ingresar el nombre de la persona que solicita la anulacion!!!" });
            }
            if (dataSolAnulacion.ci_solicita_anulacion.Trim().Length == 0)
            {
                return BadRequest(new { resp = "Debe ingresar el Nro. documento de identidad de la persona que solicita la anulacion!!!" });
            }
            try
            {
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);
                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var codAlmacen = await _context.vefactura.Where(i => i.codigo == codFactura).Select(i => i.codalmacen).FirstOrDefaultAsync();
                    // string nombreImpresora = "EPSON TM-T88V Receipt5";

                    var nombreImpresora = await _context.inalmacen.Where(i => i.codigo == codAlmacen).Select(i => i.impresora_nr).FirstOrDefaultAsync();
                    if (nombreImpresora == null)
                    {
                        return BadRequest(new { resp = "No se encontró una impresora registrada en la base de datos." });
                    }
                    Font fuente = new Font("Consolas", 10);
                    await impresoraTermica.ImprimirSolicitudAnulacion(_context, codempresa, nombreImpresora, fuente, codFactura, dataSolAnulacion.nom_solicita_anulacion, dataSolAnulacion.ci_solicita_anulacion);
                    return Ok("Imprimiendo Recibo.");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al imprimir: {ex.Message}");
            }
        }


    }
    public class dataSolAnulacion
    {
        public string nom_solicita_anulacion { get; set; } = "";
        public string ci_solicita_anulacion { get; set; } = "";
    }

    public class impresoraTermica_3
    {
        private readonly Empresa empresa = new Empresa();
        private readonly Funciones funciones = new Funciones();
        private readonly Nombres nombres = new Nombres();
        private readonly Almacen almacen = new Almacen();

        public async Task ImprimirSolicitudAnulacion(DBContext _context, string codempresa, string nombreImpresora, Font fuente, int codFactura, string nom_solicita_anulacion, string ci_solicita_anulacion)
        {
            string nomEmpresa = await nombres.nombreempresa(_context, codempresa);
            string nitEmpresa = await empresa.NITempresa(_context, codempresa);

            var datosFact = await _context.vefactura.Where(i => i.codigo == codFactura).Select(i => new
            {
                i.id,
                i.numeroid,
                i.nrofactura,
                i.fecha,
                i.nomcliente,
                i.nit,
                i.codalmacen,
                i.cuf,

            }).FirstOrDefaultAsync();

            string lugarAlmacen = await almacen.lugaralmacen(_context, datosFact.codalmacen);
            DateTime fechaHoy = await funciones.FechaDelServidor(_context);

            PrintDocument pd = new PrintDocument
            {
                PrinterSettings = { PrinterName = nombreImpresora },
                DocumentName = "SolicitudAnulacion"
            };

            if (!pd.PrinterSettings.IsValid)
            {
                throw new Exception("La impresora no está disponible o no es válida.");
            }

            pd.PrintPage += async (sender, e) =>
            {
                try
                {
                    // Definir las coordenadas para imprimir
                    float x = 10;
                    float y = 4;
                    int NC = 47;  // Ancho del área de impresión
                    float lineOffset;
                    string cadena = "";
                    // Imprimir el texto pasado como parámetro
                    // Configuración de fuentes
                    Font printFont = new Font("Consolas", 7, FontStyle.Regular, GraphicsUnit.Point);
                    Font barcodeFont = new Font("Courier New", 16);   // Substituted to Barcode1 Font

                    e.Graphics.PageUnit = GraphicsUnit.Point;
                    printFont = new Font("Arial", 10, FontStyle.Bold, GraphicsUnit.Point);
                    lineOffset = printFont.GetHeight(e.Graphics);

                    // --------------------------------------------------------------------------------

                    // imprimir el ancticipo si tiene

                    //nombre empresa
                    cadena = nomEmpresa;
                    e.Graphics.DrawString(cadena, printFont, Brushes.Black, x, y);

                    // nit
                    y += lineOffset + 2; // Espacio adicional después del subtítulo
                    printFont = new Font("Consolas", 7, FontStyle.Regular, GraphicsUnit.Point);
                    lineOffset = printFont.GetHeight(e.Graphics);
                    cadena = "NIT:" + nitEmpresa;
                    e.Graphics.DrawString(cadena, printFont, Brushes.Black, x, y);

                    //
                    NC = 37;
                    /*
                    
                    'y += lineOffset
                    'printFont = New System.Drawing.Font("Consolas", 9, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point)
                    'lineOffset = printFont.GetHeight(e.Graphics)
                    'cadena = sia_funciones.Funciones.Instancia.centrarcadena("01234567890123456789012345678901234567890123456789", NC, " ")
                    'e.Graphics.DrawString(cadena, printFont, System.Drawing.Brushes.Black, x, y)
                     
                    */

                    // titulo
                    y += lineOffset;
                    printFont = new Font("Consolas", 9, FontStyle.Bold, GraphicsUnit.Point);
                    lineOffset = printFont.GetHeight(e.Graphics);
                    cadena = funciones.CentrarCadena("SOLICITUD DE ANULACION", NC, " ");
                    e.Graphics.DrawString(cadena, printFont, Brushes.Black, x, y);

                    // 
                    y += lineOffset;
                    printFont = new Font("Consolas", 9, FontStyle.Bold, GraphicsUnit.Point);
                    lineOffset = printFont.GetHeight(e.Graphics);
                    cadena = funciones.CentrarCadena("DE FACTURA", NC, " ");
                    e.Graphics.DrawString(cadena, printFont, Brushes.Black, x, y);

                    // lugar de emision
                    y += lineOffset;
                    printFont = new Font("Consolas", 9, FontStyle.Regular, GraphicsUnit.Point);
                    lineOffset = printFont.GetHeight(e.Graphics);
                    cadena = funciones.CentrarCadena(lugarAlmacen, NC, " ");
                    e.Graphics.DrawString(cadena, printFont, Brushes.Black, x, y);

                    // fecha
                    y += lineOffset;
                    printFont = new Font("Consolas", 9, FontStyle.Regular, GraphicsUnit.Point);
                    lineOffset = printFont.GetHeight(e.Graphics);
                    cadena = funciones.CentrarCadena("Fecha Hoy: " + fechaHoy.Date.ToShortDateString(), NC, " ");
                    e.Graphics.DrawString(cadena, printFont, Brushes.Black, x, y);


                    // datos de la solicitud de anulacion
                    NC = 43;
                    /*
                     
                    'y += lineOffset
                    'printFont = New System.Drawing.Font("Consolas", 7.5, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point)
                    'lineOffset = printFont.GetHeight(e.Graphics)
                    'cadena = sia_funciones.Funciones.Instancia.centrarcadena("01234567890123456789012345678901234567890123456789", NC, " ")
                    'e.Graphics.DrawString(cadena, printFont, System.Drawing.Brushes.Black, x, y)
                     
                    */

                    y += lineOffset;
                    y += lineOffset;

                    cadena = "Yo: " + nom_solicita_anulacion;
                    printFont = new Font("Consolas", (float)7.5, FontStyle.Regular, GraphicsUnit.Point);
                    lineOffset = printFont.GetHeight(e.Graphics);
                    e.Graphics.DrawString(cadena, printFont, Brushes.Black, x, y);

                    y += lineOffset;
                    cadena = "con C.I.: " + ci_solicita_anulacion;
                    printFont = new Font("Consolas", (float)7.5, FontStyle.Regular, GraphicsUnit.Point);
                    lineOffset = printFont.GetHeight(e.Graphics);
                    e.Graphics.DrawString(cadena, printFont, Brushes.Black, x, y);

                    y += lineOffset;
                    cadena = "Solicito y/o autorizo la anulacion de";
                    printFont = new Font("Consolas", (float)7.5, FontStyle.Regular, GraphicsUnit.Point);
                    lineOffset = printFont.GetHeight(e.Graphics);
                    e.Graphics.DrawString(cadena, printFont, Brushes.Black, x, y);

                    y += lineOffset;
                    cadena = "La Factura Nro.: " + datosFact.nrofactura;
                    printFont = new Font("Consolas", (float)7.5, FontStyle.Regular, GraphicsUnit.Point);
                    lineOffset = printFont.GetHeight(e.Graphics);
                    e.Graphics.DrawString(cadena, printFont, Brushes.Black, x, y);

                    y += lineOffset;
                    cadena = "A Nombre de: " + datosFact.nomcliente;
                    printFont = new Font("Consolas", (float)7.5, FontStyle.Regular, GraphicsUnit.Point);
                    var resultado_filas = funciones.Dividir_cadena_en_filas(cadena, NC);
                    for (int i = 0; i < resultado_filas.Length; i++)
                    {
                        if (resultado_filas[i] != null)
                        {
                            e.Graphics.DrawString(resultado_filas[i], printFont, Brushes.Black, x, y);
                            y += lineOffset;
                        }
                    }

                    cadena = "NIT/CI: " + datosFact.nit;
                    printFont = new Font("Consolas", (float)7.5, FontStyle.Regular, GraphicsUnit.Point);
                    lineOffset = printFont.GetHeight(e.Graphics);
                    e.Graphics.DrawString(cadena, printFont, Brushes.Black, x, y);


                    y += lineOffset;
                    cadena = "CUF: " + datosFact.cuf;
                    printFont = new Font("Consolas", (float)7.5, FontStyle.Regular, GraphicsUnit.Point);
                    var resultado_filas2 = funciones.Dividir_cadena_en_filas(cadena, NC);
                    for (int i = 0; i < resultado_filas2.Length; i++)
                    {
                        if (resultado_filas2[i] != null)
                        {
                            e.Graphics.DrawString(resultado_filas2[i], printFont, Brushes.Black, x, y);
                            y += lineOffset;
                        }
                    }

                    cadena = "De fecha: " + datosFact.fecha.Date.ToShortDateString();
                    printFont = new Font("Consolas", (float)7.5, FontStyle.Regular, GraphicsUnit.Point);
                    lineOffset = printFont.GetHeight(e.Graphics);
                    e.Graphics.DrawString(cadena, printFont, Brushes.Black, x, y);

                    // id-numeroiddoc
                    y += lineOffset;
                    cadena = datosFact.id + "-" + datosFact.numeroid;
                    printFont = new Font("Consolas", (float)7.5, FontStyle.Regular, GraphicsUnit.Point);
                    lineOffset = printFont.GetHeight(e.Graphics);
                    e.Graphics.DrawString(cadena, printFont, Brushes.Black, x, y);


                    // punteado para la firma
                    y += lineOffset;
                    y += lineOffset;
                    y += lineOffset;
                    cadena = funciones.CentrarCadena( "................................", NC, " ");
                    printFont = new Font("Consolas", (float)7.5, FontStyle.Regular, GraphicsUnit.Point);
                    lineOffset = printFont.GetHeight(e.Graphics);
                    e.Graphics.DrawString(cadena, printFont, Brushes.Black, x, y);

                    // firma
                    y += lineOffset;
                    cadena = funciones.CentrarCadena("Firma", NC, " ");
                    printFont = new Font("Consolas", (float)7.5, FontStyle.Regular, GraphicsUnit.Point);
                    lineOffset = printFont.GetHeight(e.Graphics);
                    e.Graphics.DrawString(cadena, printFont, Brushes.Black, x, y);


                    y += lineOffset;

                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            };
            pd.Print();
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

}
