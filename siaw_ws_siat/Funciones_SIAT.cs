using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using siaw_DBContext.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using siaw_DBContext.Models;
using siaw_funciones;
using System.Data;
using System.Xml.Serialization;
using System.Runtime.ConstrainedExecution;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Xml;
using System.Security.Cryptography.Xml;
using Microsoft.Data.SqlClient.Server;

namespace siaw_ws_siat
{
    public class Funciones_SIAT
    {
        //Clase necesaria para el uso del DBContext del proyecto siaw_Context
        public static class DbContextFactory
        {
            public static DBContext Create(string connectionString)
            {
                var optionsBuilder = new DbContextOptionsBuilder<DBContext>();
                optionsBuilder.UseSqlServer(connectionString);

                return new DBContext(optionsBuilder.Options);
            }
        }
        private readonly Adsiat_Parametros_facturacion adsiat_parametros_facturacion = new Adsiat_Parametros_facturacion();
        private readonly ServCodigos servcodigos = new ServCodigos();
        private readonly Serv_Facturas serv_facturas = new Serv_Facturas();
        private readonly Log log = new Log();
        private readonly Empresa empresa = new Empresa();
        private readonly Funciones funciones = new Funciones();
        private readonly SIAT siat = new SIAT();
        private readonly Nombres nombres = new Nombres();
        private readonly Almacen almacen = new Almacen();
        private readonly Inventario inventario = new Inventario();


        public class DatosParametrosFacturacionAg
        {
            public int? CodSucursal { get; set; }
            public int? Ambiente { get; set; }
            public int? Modalidad { get; set; } 
            public int? TipoEmision { get; set; } 
            public int? TipoFactura { get; set; }
            public int? TipoSector { get; set; } 
            public int? PtoVta { get; set; } 
            public string CodActividad { get; set; } = "";
            public bool Resultado { get; set; } = false;
            public string NitCliente { get; set; } = "";
            public string CodSistema { get; set; } = "";
        }
        public async Task<bool> Verificar_NIT_SIN_Antes(DBContext _context, int codalm, string miNIT, string miNITAverificar, string usuario)
        {
            bool resultado = false;

            int codAmbiente = await adsiat_parametros_facturacion.Ambiente(_context, codalm);
            int codModalidad = await adsiat_parametros_facturacion.Modalidad(_context, codalm);
            int codSucursal = await adsiat_parametros_facturacion.Sucursal(_context, codalm);
            string codSistema = await adsiat_parametros_facturacion.CodigoSistema(_context, codSucursal);
            string cuis = await adsiat_parametros_facturacion.CUIS(_context, codSucursal);
            string nit = miNIT;
            string nitAverificar = miNITAverificar;
            string lista_mensaje = "";
            string _mensaje = "";

            var resRecepcion = await servcodigos.Verificar_Nit(_context, codAmbiente, codModalidad, codSistema, codSucursal, cuis, nit, nitAverificar, codalm);
            lista_mensaje = "";
            foreach (var mensaje in resRecepcion.ListaMsg)
            {
                if (lista_mensaje.Trim().Length == 0)
                {
                    lista_mensaje = mensaje;
                }
                else
                {
                    lista_mensaje += Environment.NewLine + mensaje;
                }
            }
            if (resRecepcion.Transaccion)
            {
                foreach (var mensaje in resRecepcion.ListaMsg)
                {
                    await log.RegistrarEvento_Siat(_context, usuario, Log.Entidades.SW_Factura, nitAverificar, nitAverificar, nitAverificar, "VerificarNIT", mensaje, Log.TipoLog_Siat.Validar_Nit);

                }
                resultado = true;
            }
            else
            {
                foreach (var mensaje in resRecepcion.ListaMsg)
                {
                    await log.RegistrarEvento_Siat(_context, usuario, Log.Entidades.SW_Factura, nitAverificar, nitAverificar, nitAverificar, "VerificarNIT", mensaje, Log.TipoLog_Siat.Validar_Nit);
                }

                if (resRecepcion.Codigo == "992")
                {
                    // 'DSD 07/02/2023 ERROR DE SERVICIO PADRON, RETORNAR TRUE YA QUE NO SE PUDO VALIDAR CORRECTAMENTE EL NIT POR PROBLEMAS CON EL SERVICIO VERIFICAR NIT DEL SIN
                    //'994 - NIT INEXTISTENTE FALSE
                    //'992 - ERROR DE SERVICIO DE PADRON FALSE
                    //'986 - NIT ACTIVO
                   resultado = true;
                }
                else
                {
                    resultado = false;
                }
            }

            return resultado;
        }
        public async Task<string> Verificar_NIT_SIN_2024(DBContext _context, int codalm, string miNIT, string miNITAverificar, string usuario)
        {
            //Desde 20/09/2024 se detecto que muchas veces el servicio de impuestos que valida un NIT valido puede devolver el codigo 992 - ERROR DE SERVICIO DE PADRON
            //    'provocando que al seleccionar tipo carnet un numero carnet lo vea como NIT  valido lo cual no esta correcto, se cambio para que devuelva los siguientes valores segun la respuesta de impuestos
            //'994 - NIT INEXTISTENTE devolvera es INVALIDO
            //'992 - ERROR DE SERVICIO DE PADRON devolvera es ERROR
            //'986 - NIT ACTIVO devolvera VALIDO
            string resultado = "";

            int codAmbiente = await adsiat_parametros_facturacion.Ambiente(_context, codalm);
            int codModalidad = await adsiat_parametros_facturacion.Modalidad(_context, codalm);
            int codSucursal = await adsiat_parametros_facturacion.Sucursal(_context, codalm);
            string codSistema = await adsiat_parametros_facturacion.CodigoSistema(_context, codSucursal);
            string cuis = await adsiat_parametros_facturacion.CUIS(_context, codSucursal);
            string nit = miNIT;
            string nitAverificar = miNITAverificar;
            string lista_mensaje = "";
            string _mensaje = "";

            var resRecepcion = await servcodigos.Verificar_Nit(_context, codAmbiente, codModalidad, codSistema, codSucursal, cuis, nit, nitAverificar, codalm);
            lista_mensaje = "";
            foreach (var mensaje in resRecepcion.ListaMsg)
            {
                if (lista_mensaje.Trim().Length == 0)
                {
                    lista_mensaje = mensaje;
                }
                else
                {
                    lista_mensaje += Environment.NewLine + mensaje;
                }
            }
            if (resRecepcion.Transaccion)
            {
                foreach (var mensaje in resRecepcion.ListaMsg)
                {
                    await log.RegistrarEvento_Siat(_context, usuario, Log.Entidades.SW_Factura, nitAverificar, nitAverificar, nitAverificar, "VerificarNIT", mensaje, Log.TipoLog_Siat.Validar_Nit);

                }
                resultado = "VALIDO";
            }
            else
            {
                foreach (var mensaje in resRecepcion.ListaMsg)
                {
                    await log.RegistrarEvento_Siat(_context, usuario, Log.Entidades.SW_Factura, nitAverificar, nitAverificar, nitAverificar, "VerificarNIT", mensaje, Log.TipoLog_Siat.Validar_Nit);
                }

                if (resRecepcion.Codigo == "992")
                {
                    // 'DSD 07/02/2023 ERROR DE SERVICIO PADRON, RETORNAR TRUE YA QUE NO SE PUDO VALIDAR CORRECTAMENTE EL NIT POR PROBLEMAS CON EL SERVICIO VERIFICAR NIT DEL SIN
                    //'994 - NIT INEXTISTENTE FALSE
                    //'992 - ERROR DE SERVICIO DE PADRON FALSE
                    //'986 - NIT ACTIVO
                    resultado = "ERROR";
                }
                else if(resRecepcion.Codigo == "994")
                {
                    resultado = "INVALIDO";
                }
                else
                {
                    resultado = "OTRO";
                }
            }
            return resultado;
        }
        public async Task<bool> Verificar_NIT_SIN_Factura(DBContext _context, int codalm, string miNIT, string miNITAverificar, string usuario)
        {
            bool resultado = false;

            int codAmbiente = await adsiat_parametros_facturacion.Ambiente(_context, codalm);
            int codModalidad = await adsiat_parametros_facturacion.Modalidad(_context, codalm);
            int codSucursal = await adsiat_parametros_facturacion.Sucursal(_context, codalm);
            string codSistema = await adsiat_parametros_facturacion.CodigoSistema(_context, codSucursal);
            string cuis = await adsiat_parametros_facturacion.CUIS(_context, codSucursal);
            string nit = miNIT;
            string nitAverificar = miNITAverificar;
            string lista_mensaje = "";
            string _mensaje = "";

            var resRecepcion = await servcodigos.Verificar_Nit(_context, codAmbiente, codModalidad, codSistema, codSucursal, cuis, nit, nitAverificar, codalm);
            lista_mensaje = "";
            foreach (var mensaje in resRecepcion.ListaMsg)
            {
                if (lista_mensaje.Trim().Length == 0)
                {
                    lista_mensaje = mensaje;
                }
                else
                {
                    lista_mensaje += Environment.NewLine + mensaje;
                }
            }
            if (resRecepcion.Transaccion)
            {
                foreach (var mensaje in resRecepcion.ListaMsg)
                {
                    await log.RegistrarEvento_Siat(_context, usuario, Log.Entidades.SW_Factura, nitAverificar, nitAverificar, nitAverificar, "VerificarNIT", mensaje, Log.TipoLog_Siat.Validar_Nit);

                }
                resultado = true;
            }
            else
            {
                foreach (var mensaje in resRecepcion.ListaMsg)
                {
                    await log.RegistrarEvento_Siat(_context, usuario, Log.Entidades.SW_Factura, nitAverificar, nitAverificar, nitAverificar, "VerificarNIT", mensaje, Log.TipoLog_Siat.Validar_Nit);
                }

                //if (resRecepcion.Codigo == "992")
                //{
                //    // 'DSD 07/02/2023 ERROR DE SERVICIO PADRON, RETORNAR TRUE YA QUE NO SE PUDO VALIDAR CORRECTAMENTE EL NIT POR PROBLEMAS CON EL SERVICIO VERIFICAR NIT DEL SIN
                //    //'994 - NIT INEXTISTENTE FALSE
                //    //'992 - ERROR DE SERVICIO DE PADRON FALSE
                //    //'986 - NIT ACTIVO
                //    resultado = true;
                //}
                //else
                //{
                //    resultado = false;
               //}
                resultado = false;
            }

            return resultado;
        }

        public async Task<bool> Generar_XML_Factura_Compra_Venta_Bonificaciones_Serializado(DBContext _context, string idfc, int nroidfc, string micodempresa, bool es_por_paquetes, string usuario)
        {
            bool resultado = false;
            DataTable dtvefactura = new DataTable();
            DataTable dtvefactura1 = new DataTable();
            string codempresa = micodempresa;
            string nit_empresa = await empresa.NITempresa(_context, micodempresa);
            int tipo_documento = 0;
            string fechaTimestamp = "";
            try
            {
                resultado = true;
                dtvefactura.Clear();
                var datos = await _context.vefactura
                .Where(v => v.id == idfc && v.numeroid == nroidfc)
                .ToListAsync();
                var result = datos.Distinct().ToList();
                dtvefactura = funciones.ToDataTable(result);

                var ParamFcAg = await siat.Obtener_Parametros_Facturacion(_context, (int)dtvefactura.Rows[0]["codalmacen"]);

                if (dtvefactura.Rows.Count > 0)
                {
                    int codfactura = (int)dtvefactura.Rows[0]["codigo"];
                    var datos1 = await _context.vefactura1
                    .Join(_context.initem,
                          vefactura1 => vefactura1.coditem,
                          initem => initem.codigo,
                          (vefactura1, initem) => new { vefactura1, initem })
                    .Where(x => x.vefactura1.codfactura == codfactura)
                    .OrderBy(x => x.vefactura1.coditem)
                    .ThenBy(x => x.initem.medida)
                    .Select(x => new
                    {
                        x.initem.descripabr,
                        x.initem.descripcion,
                        x.initem.descripcorta,
                        x.vefactura1 // Selecciona todas las columnas de vefactura1
                    })
                    .ToListAsync();
                    var result1 = datos1.Distinct().ToList();
                    dtvefactura1 = funciones.ToDataTable(result1);
                }
                if (dtvefactura.Rows.Count > 0)
                {
                    //35: FACTURA COMPRA VENTA BONIFICACIONES(5 DECIMALES)
                    var Factura = new facturaElectronicaCompraVentaBon();
                    var Factura_cabecera = new facturaElectronicaCompraVentaBonCabecera
                    {
                        nitEmisor = nit_empresa,
                        razonSocialEmisor = await nombres.nombreempresa(_context, codempresa),
                        municipio = await empresa.municipio_empresa(_context, codempresa),
                        telefono = await almacen.telefonoalmacen(_context, (int)dtvefactura.Rows[0]["codalmacen"]),
                        numeroFactura = dtvefactura.Rows[0]["nrofactura"].ToString(),
                        cuf = (string)dtvefactura.Rows[0]["cuf"],
                        cufd = (string)dtvefactura.Rows[0]["cufd"],
                        codigoSucursal = ParamFcAg.codsucursal,
                        direccion = await almacen.direccionalmacen(_context, (int)dtvefactura.Rows[0]["codalmacen"]),
                        codigoPuntoVenta = ParamFcAg.ptovta,
                    };
                    fechaTimestamp = await funciones.Fecha_TimeStamp(Convert.ToDateTime(dtvefactura.Rows[0]["fecha"].ToString()), (string)dtvefactura.Rows[0]["horareg"]);
                    DateTime fechaFinal = DateTime.Parse(fechaTimestamp);
                    Factura_cabecera.fechaEmision = fechaFinal;
                    Factura_cabecera.nombreRazonSocial = (string?)dtvefactura.Rows[0]["nomcliente"];
                   tipo_documento = Convert.ToInt32(dtvefactura.Rows[0]["tipo_docid"]);
                    //Verificar si el numero de documento a enviar es:
                    //'1 CI - CÉDULA DE IDENTIDAD
                    //'2 CEX - CÉDULA DE IDENTIDAD DE EXTRANJERO
                    //'3 PAS -PASAPORTE
                    //'4 OD - OTRO DOCUMENTO DE IDENTIDAD
                    //'5 NIT - NÚMERO DE IDENTIFICACIÓN TRIBUTARIA
                    //' en un case validar
                    switch (tipo_documento)
                    {
                        case 1:
                            //es CÉDULA DE IDENTIDAD
                            Factura_cabecera.codigoTipoDocumentoIdentidad = "1";
                            Factura_cabecera.codigoExcepcion = "0";
                            break;
                        case 2:
                            //es CÉDULA DE IDENTIDAD DE EXTRANJERO
                            Factura_cabecera.codigoTipoDocumentoIdentidad = "2";
                            Factura_cabecera.codigoExcepcion = "0";
                            break;
                        case 3:
                            //es PASAPORTE
                            Factura_cabecera.codigoTipoDocumentoIdentidad = "3";
                            Factura_cabecera.codigoExcepcion = "0";
                            break;
                        case 4:
                            //es OTRO DOCUMENTO DE IDENTIDAD
                            Factura_cabecera.codigoTipoDocumentoIdentidad = "4";
                            Factura_cabecera.codigoExcepcion = "0";
                            break;
                        case 5:
                            //es NIT validar si efectivamente es un NIT valido
                            if (await funciones.Verificar_Conexion_Internet() && await adsiat_parametros_facturacion.ServiciosInternetActivo(_context, (int)dtvefactura.Rows[0]["codalmacen"]))
                            {//preguntar si hay conexion al SIN
                                if (await serv_facturas.VerificarComunicacion(_context, (int)dtvefactura.Rows[0]["codalmacen"]) && await adsiat_parametros_facturacion.ServiciosSinActivo(_context, (int)dtvefactura.Rows[0]["codalmacen"]))
                                {//'preguntar si el codigoTipoDocumentoIdentidad es NIT o no para enviar el parametro 1:CI, 5:NIT
                                    if (await Verificar_NIT_SIN_Factura(_context, (int)dtvefactura.Rows[0]["codalmacen"], nit_empresa, dtvefactura.Rows[0]["nit"].ToString(),usuario))
                                    {//si es true es NIT activo
                                        Factura_cabecera.codigoTipoDocumentoIdentidad = "5";
                                        Factura_cabecera.codigoExcepcion = "0";
                                    }
                                    else
                                    {//es CI
                                        Factura_cabecera.codigoTipoDocumentoIdentidad = "5";
                                        Factura_cabecera.codigoExcepcion = "1";//por defecto enviar 1 porq el NIt es invalido
                                    }
                                }
                                else
                                {
                                    Factura_cabecera.codigoTipoDocumentoIdentidad = "5";
                                    Factura_cabecera.codigoExcepcion = "1";//por defecto enviar 1 porq no hay como validar el nit segun el SIN
                                }
                            }
                            else
                            {
                                Factura_cabecera.codigoTipoDocumentoIdentidad = "5";
                                Factura_cabecera.codigoExcepcion = "1";//por defecto enviar 1 porq no hay como validar el nit segun el SIN
                            }
                            break;
                        default:
                            Factura_cabecera.codigoTipoDocumentoIdentidad = "1";
                            Factura_cabecera.codigoExcepcion = "0";
                            break;
                    }

                    Factura_cabecera.numeroDocumento = dtvefactura.Rows[0]["nit"].ToString();

                    string complemento = (string)dtvefactura.Rows[0]["complemento_ci"];
                    if (string.IsNullOrWhiteSpace(complemento))
                    {
                        Factura_cabecera.codigoCliente = dtvefactura.Rows[0]["nit"].ToString();
                    }
                    else
                    {
                        Factura_cabecera.complemento = complemento;
                        Factura_cabecera.codigoCliente = dtvefactura.Rows[0]["nit"] + complemento;
                    }

                    Factura_cabecera.codigoMetodoPago = "1";
                    Factura_cabecera.montoTotal = (decimal)dtvefactura.Rows[0]["total"];
                    Factura_cabecera.montoTotalSujetoIva = (decimal)dtvefactura.Rows[0]["total"];
                    Factura_cabecera.codigoMoneda = await siat.CodMoneda_Homolado_SIN(_context, (string)dtvefactura.Rows[0]["codmoneda"]);
                    Factura_cabecera.tipoCambio = 1;
                    Factura_cabecera.montoTotalMoneda = (decimal)dtvefactura.Rows[0]["total"];
                    //no mostrar
                    Factura_cabecera.montoGiftCard = (decimal?)0.00;
                    Factura_cabecera.descuentoAdicional = (decimal?)dtvefactura.Rows[0]["descuentos"];
                    //'no mostrar

                    //'no enviar cafc
                    //'Factura_cabecera.cafc = "False"
                    //'enviar cafc solo para pruebas de envio de paquetes por codevento  5 - 6 - 7
                    //'Factura_cabecera.cafc = "101B987CF7B3D"

                    //enviar la leyenda con la que se grabo la factura
                    Factura_cabecera.leyenda = (string)dtvefactura.Rows[0]["leyenda"];
                    Factura_cabecera.usuario = (string)dtvefactura.Rows[0]["usuarioreg"];
                    Factura_cabecera.codigoDocumentoSector = ParamFcAg.tiposector;

                    Factura.cabecera = Factura_cabecera;

                    var listaProductos = new List<facturaElectronicaCompraVentaBonDetalle>();
                    decimal descuento = 0;
                    decimal precio_unitario = 0;
                    foreach (DataRow row in dtvefactura1.Rows)
                    {
                        descuento = 0;
                        precio_unitario = 0;
                        precio_unitario = (decimal)row["preciolista"];
                        descuento = (await siat.Redondeo_Decimales_SIA_5_decimales_SQL(_context,(decimal)row["preciolista"] * (decimal)row["cantidad"]) - await siat.Redondeo_Decimales_SIA_5_decimales_SQL(_context, (decimal)row["precioneto"] * (decimal)row["cantidad"]));
                        var Factura_detalle_contenedor = new facturaElectronicaCompraVentaBonDetalle
                        {
                            actividadEconomica = ParamFcAg.codactividad,
                            codigoProductoSin = (string)row["codproducto_sin"],
                            codigoProducto = (string)row["coditem"],
                            descripcion = (string)row["descripcorta"],
                            cantidad = (decimal)row["cantidad"],
                            unidadMedida = await inventario.Codigo_UDM_SIN(_context, (string)row["udm"]),
                            precioUnitario = precio_unitario,
                            montoDescuento = descuento,
                            subTotal = (decimal)row["total"],
                            //' no incluir
                            //'Factura_detalle_contenedor.numeroSerie = "0"
                            //' no incluir
                            //'Factura_detalle_contenedor.numeroImei = "0"
                        };
                        listaProductos.Add(Factura_detalle_contenedor);
                    }

                    Factura.detalle = listaProductos.ToArray();

                    string ruta_factura_xml = "";

                    if (es_por_paquetes)
                    {
                        //Dsd 06 / 12 / 2022 cuando se empaqueta facturas se deben crear los XML en carpetas separadas segun del almacen q esten generando para que cuando abran varios empaquetadores
                        //'no se cruzen los archivos a enviar
                        string carpeta_almacen = (string)dtvefactura.Rows[0]["codalmacen"];
                        ruta_factura_xml = Path.Combine(AppContext.BaseDirectory, "certificado", "FacturasXML", carpeta_almacen);

                        if (!Directory.Exists(Path.Combine(AppContext.BaseDirectory, "certificado", "FacturasXML", carpeta_almacen)))
                        {
                            Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory, "certificado", "FacturasXML", carpeta_almacen));
                        }
                        ruta_factura_xml = Path.Combine(AppContext.BaseDirectory, "certificado", "FacturasXML", carpeta_almacen, $"{idfc}_{nroidfc}.xml");
                    }
                    else
                    {
                        ruta_factura_xml = Path.Combine(AppContext.BaseDirectory, "certificado", $"{idfc}_{nroidfc}.xml");
                    }

                    var serializer = new XmlSerializer(typeof(facturaElectronicaCompraVentaBon));
                    var UTF8SinBOM = new UTF8Encoding(false);
                    using (var writer = new StreamWriter(ruta_factura_xml, false, UTF8SinBOM))
                    {
                        serializer.Serialize(writer, Factura);
                    }

                    resultado = true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                resultado = false;
            }

            return resultado;
        }
        public async Task<bool> Firmar_XML_Con_SHA256(string _ruta_archivo_XML, string _ruta_archivo_p12, string _clave, string _archivo_firmado)
        {
            bool resultado = true;

            try
            {
                string archivoFirma = _ruta_archivo_p12;
                X509Certificate2 cert = new X509Certificate2(archivoFirma, _clave, X509KeyStorageFlags.Exportable);

                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.PreserveWhitespace = true;
                xmlDoc.Load(_ruta_archivo_XML);

                resultado = await Realizar_Firmado_XML_Con_SHA256(xmlDoc, cert);

                if (resultado)
                {
                    Encoding UTF8SinBOM = new UTF8Encoding(false);
                    using (StreamWriter writer = new StreamWriter(_archivo_firmado, false, UTF8SinBOM))
                    {
                        xmlDoc.Save(writer);
                    }
                }
                else
                {
                    resultado = false;
                }
            }
            catch (Exception ex)
            {
                resultado = false;
            }

            return resultado;
        }
        public async Task<bool> Realizar_Firmado_XML_Con_SHA256(XmlDocument xmlDoc, X509Certificate2 cert)
        {
            bool resultado = false;

            try
            {
                if (xmlDoc == null) throw new ArgumentException("xmlDoc");
                if (cert == null) throw new ArgumentException("Cert");

                string exportedKeyMaterial = cert.PrivateKey.ToXmlString(true);
                RSACryptoServiceProvider key = new RSACryptoServiceProvider(new CspParameters(24));

                key.PersistKeyInCsp = false;
                key.FromXmlString(exportedKeyMaterial);

                xmlDoc.PreserveWhitespace = true;

                SignedXml signedXml = new SignedXml(xmlDoc);
                signedXml.SigningKey = key;
                signedXml.SignedInfo.SignatureMethod = "http://www.w3.org/2001/04/xmldsig-more#rsa-sha256";

                Reference reference = new Reference();
                reference.AddTransform(new XmlDsigEnvelopedSignatureTransform());
                reference.AddTransform(new XmlDsigC14NWithCommentsTransform()); // Cambio transform

                reference.Uri = "";
                reference.DigestMethod = "http://www.w3.org/2001/04/xmlenc#sha256";
                signedXml.AddReference(reference);

                KeyInfo keyInfo = new KeyInfo();
                keyInfo.AddClause(new KeyInfoX509Data(cert));
                signedXml.KeyInfo = keyInfo;

                signedXml.ComputeSignature();

                XmlElement xmlDigitalSignature = signedXml.GetXml();
                xmlDoc.DocumentElement.AppendChild(xmlDoc.ImportNode(xmlDigitalSignature, true));

                resultado = true;
            }
            catch (Exception ex)
            {
                resultado = false;
                Console.WriteLine("Error al firmar el documento XML." + ex.Message);
            }
            return resultado;
        }


        //public async Task<Datos_Pametros_Facturacion_Ag> Obtener_Parametros_Facturacion(DBContext _context, int codalmacen)
        //{
        //    var resultado = new DatosParametrosFacturacionAg();

        //    var parametrosFacturacion = await _context.adsiat_parametros_facturacion
        //     .Where(p => p.codalmacen == codalmacen)
        //     .SingleOrDefaultAsync();

        //    if (parametrosFacturacion != null)
        //    {
        //        // Si solo hay un registro, se devuelven los datos
        //        resultado.CodSucursal = parametrosFacturacion.codsucursal;
        //        resultado.Ambiente = parametrosFacturacion.ambiente;
        //        resultado.Modalidad = parametrosFacturacion.modalidad;
        //        resultado.TipoEmision = parametrosFacturacion.tipo_emision;
        //        resultado.TipoFactura = parametrosFacturacion.tipo_factura;
        //        resultado.TipoSector = parametrosFacturacion.tipo_doc_sector;
        //        resultado.PtoVta = parametrosFacturacion.punto_vta;
        //        resultado.CodActividad = parametrosFacturacion.codactividad;
        //        resultado.NitCliente = parametrosFacturacion.nit_cliente;
        //        resultado.CodSistema = parametrosFacturacion.codsistema;
        //        resultado.Resultado = true;
        //    }
        //    else
        //    {
        //        // Si no hay registros o hay más de uno, por seguridad no se devuelven datos
        //        resultado.CodSucursal = 0;
        //        resultado.Ambiente = 0;
        //        resultado.Modalidad = 0;
        //        resultado.TipoEmision = 0;
        //        resultado.TipoFactura =0;
        //        resultado.TipoSector = 0;
        //        resultado.PtoVta = 0;
        //        resultado.CodActividad = "";
        //        resultado.NitCliente = "";
        //        resultado.CodSistema = "";
        //        resultado.Resultado = false;
        //    }
        //    return resultado;
        //}
    }
}
