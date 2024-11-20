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

        private readonly ServFacturas serv_Facturas = new ServFacturas();
        private readonly Funciones_SIAT funciones_SIAT = new Funciones_SIAT();
        private readonly Adsiat_Parametros_facturacion adsiat_Parametros_Facturacion = new Adsiat_Parametros_facturacion();
        private readonly Adsiat_Mensaje_Servicio adsiat_Mensaje_Servicio = new Adsiat_Mensaje_Servicio();
        private readonly GZip gzip = new GZip();

        private readonly impresoraTermica_2 impresoraTermica = new impresoraTermica_2();

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
