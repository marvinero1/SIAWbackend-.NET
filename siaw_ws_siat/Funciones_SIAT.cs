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
using Microsoft.AspNetCore.Http;
using NuGet.Configuration;
using System.Runtime.Intrinsics.Arm;
using Microsoft.Extensions.Logging;

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
        private readonly ServFacturas servfacturas = new ServFacturas();
        private readonly Log log = new Log();
        private readonly Empresa empresa = new Empresa();
        private readonly Funciones funciones = new Funciones();
        private readonly SIAT siat = new SIAT();
        private readonly Nombres nombres = new Nombres();
        private readonly Almacen almacen = new Almacen();
        private readonly Inventario inventario = new Inventario();
        private readonly Ventas ventas = new Ventas();


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
        public async Task<bool> Verificar_NIT_SIN_Antes(DBContext _context, int codalm, long miNIT, long miNITAverificar, string usuario)
        {
            bool resultado = false;

            int codAmbiente = await adsiat_parametros_facturacion.Ambiente(_context, codalm);
            int codModalidad = await adsiat_parametros_facturacion.Modalidad(_context, codalm);
            int codSucursal = await adsiat_parametros_facturacion.Sucursal(_context, codalm);
            string codSistema = await adsiat_parametros_facturacion.CodigoSistema(_context, codSucursal);
            string cuis = await adsiat_parametros_facturacion.CUIS(_context, codSucursal);
            long nit = miNIT;
            long nitAverificar = miNITAverificar;
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
                    await log.RegistrarEvento_Siat(_context, usuario, Log.Entidades.SW_Factura, nitAverificar.ToString(), nitAverificar.ToString(), nitAverificar.ToString(), "VerificarNIT", mensaje, Log.TipoLog_Siat.Validar_Nit);

                }
                resultado = true;
            }
            else
            {
                foreach (var mensaje in resRecepcion.ListaMsg)
                {
                    await log.RegistrarEvento_Siat(_context, usuario, Log.Entidades.SW_Factura, nitAverificar.ToString(), nitAverificar.ToString(), nitAverificar.ToString(), "VerificarNIT", mensaje, Log.TipoLog_Siat.Validar_Nit);
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
        public async Task<string> Verificar_NIT_SIN_2024(DBContext _context, int codalm, long miNIT, long miNITAverificar, string usuario)
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
            long nit = miNIT;
            long nitAverificar = miNITAverificar;
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
                    await log.RegistrarEvento_Siat(_context, usuario, Log.Entidades.SW_Factura, nitAverificar.ToString(), nitAverificar.ToString(), nitAverificar.ToString(), "VerificarNIT", mensaje, Log.TipoLog_Siat.Validar_Nit);

                }
                resultado = "VALIDO";
            }
            else
            {
                foreach (var mensaje in resRecepcion.ListaMsg)
                {
                    await log.RegistrarEvento_Siat(_context, usuario, Log.Entidades.SW_Factura, nitAverificar.ToString(), nitAverificar.ToString(), nitAverificar.ToString(), "VerificarNIT", mensaje, Log.TipoLog_Siat.Validar_Nit);
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
        public async Task<bool> Verificar_NIT_SIN_Factura(DBContext _context, int codalm, long miNIT, long miNITAverificar, string usuario)
        {
            bool resultado = false;

            int codAmbiente = await adsiat_parametros_facturacion.Ambiente(_context, codalm);
            int codModalidad = await adsiat_parametros_facturacion.Modalidad(_context, codalm);
            int codSucursal = await adsiat_parametros_facturacion.Sucursal(_context, codalm);
            string codSistema = await adsiat_parametros_facturacion.CodigoSistema(_context, codSucursal);
            string cuis = await adsiat_parametros_facturacion.CUIS(_context, codSucursal);
            long nit = miNIT;
            long nitAverificar = miNITAverificar;
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
                    await log.RegistrarEvento_Siat(_context, usuario, Log.Entidades.SW_Factura, nitAverificar.ToString(), nitAverificar.ToString(), nitAverificar.ToString(), "VerificarNIT", mensaje, Log.TipoLog_Siat.Validar_Nit);

                }
                resultado = true;
            }
            else
            {
                foreach (var mensaje in resRecepcion.ListaMsg)
                {
                    await log.RegistrarEvento_Siat(_context, usuario, Log.Entidades.SW_Factura, nitAverificar.ToString(), nitAverificar.ToString(), nitAverificar.ToString(), "VerificarNIT", mensaje, Log.TipoLog_Siat.Validar_Nit);
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
                         // Selecciona todas las propiedades de vefactura1 usando el operador 'new' para un objeto anónimo
                         //vefactura1 = x.vefactura1, // Aquí se incluyen todas las columnas de vefactura1
                                                    // También selecciona las columnas que deseas de initem
                         descripabr = x.initem.descripcion,
                         descripcion = x.initem.descripabr,
                         descripcorta = x.initem.descripcorta,

                         codfactura  = x.vefactura1.codfactura,
                         coditem = x.vefactura1.coditem,
                         cantidad = x.vefactura1.cantidad,
                         udm = x.vefactura1.udm,
                         precioneto = x.vefactura1.precioneto,
                         preciolista = x.vefactura1.preciolista,
                         preciodesc = x.vefactura1.preciodesc,
                         niveldesc = x.vefactura1.niveldesc,
                         codtarifa = x.vefactura1.codtarifa,
                         coddescuento = x.vefactura1.coddescuento,
                         total = x.vefactura1.total,
                         distdescuento = x.vefactura1.distdescuento,
                         distrecargo = x.vefactura1.distrecargo,
                         preciodist = x.vefactura1.preciodist,
                         totaldist = x.vefactura1.totaldist,
                         porceniva = x.vefactura1.porceniva,
                         codaduana = x.vefactura1.codaduana,
                         codgrupomer = x.vefactura1.codgrupomer,
                         peso = x.vefactura1.peso,
                         codproducto_sin = x.vefactura1.codproducto_sin

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
                    //DateTime fechaFinal = DateTime.Parse(fechaTimestamp);
                    DateTime fechaFinal = DateTime.ParseExact(fechaTimestamp, "yyyy-MM-ddTHH:mm:ss.fff", System.Globalization.CultureInfo.InvariantCulture);
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
                                if (await servfacturas.VerificarComunicacion(_context, (int)dtvefactura.Rows[0]["codalmacen"]) && await adsiat_parametros_facturacion.ServiciosSinActivo(_context, (int)dtvefactura.Rows[0]["codalmacen"]))
                                {//'preguntar si el codigoTipoDocumentoIdentidad es NIT o no para enviar el parametro 1:CI, 5:NIT
                                    if (await Verificar_NIT_SIN_Factura(_context, (int)dtvefactura.Rows[0]["codalmacen"], long.Parse(nit_empresa), long.Parse((string)dtvefactura.Rows[0]["nit"]),usuario))
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
                        if (!Directory.Exists(Path.Combine(AppContext.BaseDirectory, "certificado")))
                        {
                            Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory, "certificado"));
                        }
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
        public async Task<(bool resul, List<string> msgAlertas, List<string> eventos)> ENVIAR_FACTURA_AL_SIN(DBContext _context, string codigocontrol, string codempresa, string usuario, string micufd, long miNIT, string miCUF, byte[] archivogzip, string mihashArchivo, int codalm, int codFactura, string idFac, int nroIdFac)
        {
            // para devolver lista de registros logs
            List<string> eventos = new List<string>();
            List<string> msgAlertas = new List<string>();
            bool resultado = false;
            var resRecepcion = new ServFacturas.ResultadoRecepcionFactura();

            int codAmbiente = await adsiat_parametros_facturacion.Ambiente(_context, codalm);
            int codDocSector = await adsiat_parametros_facturacion.TipoDocSector(_context, codalm);
            int codEmision = await adsiat_parametros_facturacion.TipoEmision(_context, codalm);
            int codModalidad = await adsiat_parametros_facturacion.Modalidad(_context, codalm);
            int codPtoVta = await adsiat_parametros_facturacion.PuntoDeVta(_context, codalm);
            int codSucursal = await adsiat_parametros_facturacion.Sucursal(_context, codalm);
            string codSistema = await adsiat_parametros_facturacion.CodigoSistema(_context, codSucursal);
            string cuis = await adsiat_parametros_facturacion.CUIS(_context, codSucursal);
            long nit = miNIT;
            string cufd = micufd;
            int tipoFacturaDocumento = await adsiat_parametros_facturacion.TipoFactura(_context, codalm);
            string cuf = miCUF;
            byte[] archivo = archivogzip;
            string hashArchivo = mihashArchivo;
            string lista_mensaje = "";
            string _mensaje = "";
            string fecha_envio = await FechaDelServidorTimeStamp(_context);

            resRecepcion = await servfacturas.Recepcion_Factura(_context, fecha_envio, codalm, codAmbiente, codDocSector, codEmision, codModalidad, codPtoVta, codSistema, codSucursal, cuis, cufd, nit, tipoFacturaDocumento, archivo, hashArchivo);
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
                _mensaje = "La conexion con el SIN para enviar la factura fue exitosa, el resultado del envio es: " + resRecepcion.CodEstado + "-" + resRecepcion.CodEstadoDesc + "-" + resRecepcion.CodRecepcion;
                eventos.Add(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fff") + " - " + _mensaje);
                foreach (var mensaje in resRecepcion.ListaMsg)
                {
                    await log.RegistrarEvento_Siat(_context, usuario, Log.Entidades.SW_Factura, codFactura.ToString(), idFac, nroIdFac.ToString(), "ENVIAR_FACTURA_AL_SIN", mensaje, Log.TipoLog_Siat.Creacion);
                }
                resultado = true;
            }
            else
            {
                _mensaje = "Se tienen obervaciones en el envio de la factura: " + resRecepcion.CodEstado + "-" + resRecepcion.CodEstadoDesc;
                eventos.Add(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fff") + " - " + _mensaje);
                foreach (var mensaje in resRecepcion.ListaMsg)
                {
                    await log.RegistrarEvento_Siat(_context, usuario, Log.Entidades.SW_Factura, codFactura.ToString(), idFac, nroIdFac.ToString(), "ENVIAR_FACTURA_AL_SIN", mensaje, Log.TipoLog_Siat.Creacion);
                }
                resultado = false;
                //'Desde 08/07/2023 aqui ver si conviene cambiar los siguientes campos en_linea='0' , en_linea_sin='0', volver a generar CUF y actualizar
                //'Porq en teoria la factura salio ya rechazada por lo tanto se empaquetara como fuera de linea y asi evitaremos actualizar el CUF
                //'desde el empaquetar al momento de empaquetar facturas
                //'//actualizar en_linea true porq SI hay conexion a internet
                //'/Desde 14/08/2023 solo si se recibe la respuesta 902 RECHAZADA LA FACTURA SE CAMBIARA SU CUF SINO ALERTA QUE SE COMUNIQUE CON EL AREA DE SISTEMAS
                if (resRecepcion.CodEstado == 902)
                {
                    string valNIT = await empresa.NITempresa(_context, codempresa);
                    string tipoEmision = "";
                    string msj = "";
                    Datos_Pametros_Facturacion_Ag parametrosFacturacionAg1 = new Datos_Pametros_Facturacion_Ag();
                    parametrosFacturacionAg1 = await siat.Obtener_Parametros_Facturacion(_context, codalm);

                    //int nrofactura = //ventas.factura_nrofactura(codFactura);
                    int nrofactura = 0;
                    // Buscar la factura por el código
                    var factura2 = _context.vefactura.SingleOrDefault(f => f.codigo == codFactura);
                    if (factura2 != null)
                    {
                        // Actualizar los valores
                        nrofactura = factura2.nrofactura;
                        factura2.en_linea = false;
                        factura2.en_linea_SIN = false;
                        // Guardar cambios en la base de datos
                        _context.SaveChanges();
                        msj = "El estado en línea y en línea SIN de la factura fue cambiado exitosamente a FUERA DE LÍNEA";
                        await log.RegistrarEvento_Siat(_context, usuario, Log.Entidades.SW_Factura, codFactura.ToString(), idFac, nroIdFac.ToString(), "prgfacturarNR_cufdController", msj, Log.TipoLog_Siat.Creacion);
                    }

                    if (parametrosFacturacionAg1.resultado)
                    {
                        tipoEmision = "2";
                        string valorCUF = await siat.Generar_CUF(_context, idFac, nroIdFac, codalm, valNIT, parametrosFacturacionAg1.codsucursal, parametrosFacturacionAg1.modalidad, tipoEmision, parametrosFacturacionAg1.tipofactura, parametrosFacturacionAg1.tiposector, nrofactura.ToString(), parametrosFacturacionAg1.ptovta, codigocontrol);
                        if (factura2 != null)
                        {
                            // Actualizar los valores
                            factura2.cuf = valorCUF;
                            // Guardar cambios en la base de datos
                            _context.SaveChanges();
                            msj = "El CUF de la factura fue cambiado exitosamente por: " + valorCUF;
                            eventos.Add(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fff") + " - " + _mensaje);
                            await log.RegistrarEvento_Siat(_context, usuario, Log.Entidades.SW_Factura, codFactura.ToString(), idFac, nroIdFac.ToString(), "prgfacturarNR_cufdController", msj, Log.TipoLog_Siat.Creacion);
                        }
                    }
                    else
                    {
                        // Actualizar el CUF
                        if (factura2 != null)
                        {
                            // Actualizar los valores
                            factura2.cuf = "";
                            // Guardar cambios en la base de datos
                            _context.SaveChanges();
                            //msj = "No se pudo generar el CUF correcto, se puso un CUF vacío.";
                            string cadenaMsj = $"No se pudo generar el CUF de la factura {idFac}-{nroIdFac} debido a que no se encontraron los parámetros de facturación necesarios de la agencia!!!";
                            eventos.Add(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fff") + " - " + cadenaMsj);
                            await log.RegistrarEvento_Siat(_context, usuario, Log.Entidades.SW_Factura, codFactura.ToString(), idFac, nroIdFac.ToString(), "prgfacturarNR_cufdController", cadenaMsj, Log.TipoLog_Siat.Creacion);
                        }
                        resultado = false;
                    }
                }
                else
                {
                    resultado = false;
                    msgAlertas.Add("¡COMUNÍQUESE URGENTEMENTE CON EL ÁREA DE SISTEMAS PARA LA REVISIÓN DE ESTA FACTURA!");
                }
            }

            // Actualizar la cod_recepcion_siat
            var codRecepcionSiat = resRecepcion.CodRecepcion;
            var codEstadoSiat = resRecepcion.CodEstado;
            var factura = _context.vefactura.SingleOrDefault(f => f.codigo == codFactura);
            if (factura != null)
            {
                // Actualizar los valores
                factura.cod_recepcion_siat = codRecepcionSiat;
                factura.cod_estado_siat = codEstadoSiat;
                // Guardar cambios en la base de datos
                _context.SaveChanges();
                await log.RegistrarEvento_Siat(_context, usuario, Log.Entidades.SW_Factura, codFactura.ToString(), idFac, nroIdFac.ToString(), "prgfacturarNR_cufdController", $"Cod_Recepcion|{codRecepcionSiat}|Cod_estado_siat|{codEstadoSiat}", Log.TipoLog_Siat.Creacion);
            }

            return (resultado, msgAlertas,eventos);
        }
        public async Task<string> FechaDelServidorTimeStamp(DBContext _context)
        {
            DateTime miFecha = DateTime.Now;
            string resultado = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fff");

            try
            {
                // Suponiendo que _context es tu DbContext de Entity Framework
                DateTime fecha = await funciones.FechaDelServidor_TimeStamp(_context);
                miFecha = fecha;
                resultado = miFecha.ToString("yyyy-MM-ddTHH:mm:ss.fff");
                //resultado += "1"; // Añades "1" como en tu código original
            }
            catch (Exception ex)
            {
                resultado = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fff");
            }
            return resultado;
        }

        public async Task<(bool resp, string mensaje)> Validar_NIT_En_El_SIN_Crear_Cliente(DBContext _context, string codempresa, int tipo_doc, int codalmacen, long NIT, string usuario)
        {
            bool resultado = true;
            bool nit_valido = false;
            string respuesta_SIN = "";
            bool en_linea = false;
            bool en_linea_SIN = false;
            string mensaje = "";

            respuesta_SIN = await Verificar_NIT_SIN_2024(_context, codalmacen, long.Parse(await empresa.NITempresa(_context, codempresa)), NIT, usuario);
            if (respuesta_SIN == "VALIDO")
            {//si es true es NIT activo
                nit_valido = true;
            }
            else if (respuesta_SIN == "ERROR" || respuesta_SIN == "OTRO")
            {
                mensaje = "El Numero de documento ingresado no se puede validar con el servicio de impuestos si es un NIT VALIDO o NO (Respuesta: " + respuesta_SIN + " ), se continuara con las demas validaciones.";
                resultado = true;
                return (resultado, mensaje);
                nit_valido = true;
            }
            else if (respuesta_SIN == "INVALIDO")
            {
                nit_valido = false;
            }

            if (tipo_doc != 5)
            {//Si para el SIN el numero es un NIT valido pero el tipo doc es 1 - CI entonces obligar a que cambie a NIT
                if (nit_valido == true)
                {
                    mensaje = "El numero de documento es un NIT VALIDO para el servicio de Impuestos Nacionales, cambie el tipo de documento de 5 - NIT!!!";
                    resultado = false;
                }
                else
                {
                    resultado = true;
                }
            }
            if (tipo_doc == 5)
            {//Si para el SIN el numero es un NIT valido pero el tipo doc es 1 - CI entonces obligar a que cambie a NIT
                if (nit_valido == false)
                {
                    mensaje = "El numero de documento NO es un NIT VALIDO para el servicio de Impuestos Nacionales, cambie el tipo de documento de 5-NIT a 1- CI u otro tipo de documento valido!!!";
                    resultado = false;
                }
                else
                {
                    resultado = true;
                }
            }
            return (resultado, mensaje);
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
