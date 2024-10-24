﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Servp_FacturaCompraVta;
using System.Net;
using System.ServiceModel;
using System.Xml;
using Microsoft.EntityFrameworkCore;
using siaw_DBContext.Data;
using siaw_ws_siat;
using siaw_funciones;

public class ServFacturas
{
    //#region PATRON SINGLETON

    //private ServCodigos() { }

    //public static readonly ServCodigos Instancia = new ServCodigos();

    //#endregion

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
    public class ResultadoValidarFacturaPaquete
    {
        public string CodDescripcion { get; set; }
        public string CodEstado { get; set; }
        public string CodEstadoDesc { get; set; }
        public bool Transaccion { get; set; }
        public string CodRecepcion { get; set; }
        public string CodRecepcionDesc { get; set; }
        public List<string> ListaMsg { get; set; } = new List<string>();
    }

    public class ResultadoAnulacionFactura
    {
        public string CodDescripcion { get; set; }
        public string CodEstado { get; set; }
        public string CodEstadoDesc { get; set; }
        public bool Transaccion { get; set; }
        public List<string> ListaMsg { get; set; } = new List<string>();
    }

    public class ResultadoObtencionCUFD
    {
        public string Cod { get; set; }
        public string CodControl { get; set; }
        public string Direccion { get; set; }
        public string FechaVigencia { get; set; }
        public bool Transaccion { get; set; }
        public List<string> ListaMsg { get; set; } = new List<string>();
    }

    public class ResultadoEstadoFactura
    {
        public string CodDescripcion { get; set; }
        public string CodEstado { get; set; }
        public string CodEstadoDesc { get; set; }
        public string CodRecepcion { get; set; }
        public bool Transaccion { get; set; }
    }
    public class ResultadoRecepcionFactura
    {
        public string CodDescripcion { get; set; }
        public int CodEstado { get; set; }
        public string CodEstadoDesc { get; set; }
        public bool Transaccion { get; set; }
        public string CodRecepcion { get; set; }
        public string CodRecepcionDesc { get; set; }
        public List<string> ListaMsg { get; set; } = new List<string>();
    }
    public class ResultadoRecepcionFacturaPaquete
    {
        public string CodDescripcion { get; set; }
        public string CodEstado { get; set; }
        public string CodEstadoDesc { get; set; }
        public bool Transaccion { get; set; }
        public string CodRecepcion { get; set; }
        public string CodRecepcionDesc { get; set; }
        public List<string> ListaMsg { get; set; } = new List<string>();
    }
    public class ResultadoReversionAnulacionFactura
    {
        public string CodDescripcion { get; set; }
        public string CodEstado { get; set; }
        public string CodEstadoDesc { get; set; }
        public bool Transaccion { get; set; }
        public string CodRecepcion { get; set; }
        public string CodRecepcionDesc { get; set; }
        public List<string> ListaMsg { get; set; } = new List<string>();
    }


    private int _codSucursal = 0;
    private int _codAmbiente = 0;
    private string _codSistema = "";

    private string endpointAddress = "";
    private string token = "";

    private verificarComunicacionResponse MyResult_Comunicacion;
    private respuestaComunicacion MyResult_Comunicacion_resp;

    private respuestaRecepcion MyResult_AnulacionFactura;
    private mensajeServicio MyResult_AnulacionFactura_Lista;

    private respuestaRecepcion MyResult_EstadoFactura;

    private respuestaRecepcion MyResult_RecepcionFactura;
    private recepcionFacturaResponse MyResult_RecepcionFactura_resp;

    private mensajeServicio MyResult_RecepcionFactura_Lista;

    private respuestaRecepcion MyResult_RecepcionFactura_Paquete;
    private mensajeServicio MyResult_RecepcionFactura_Lista_Paquete;

    private respuestaRecepcion MyResult_ValidarFactura_Paquete;
    private mensajeServicio MyResult_ValidarFactura_Lista_Paquete;

    private respuestaRecepcion MyResult_ReversionAnulacionFactura;
    private mensajeServicio MyResult_ReversionAnulacionFactura_Lista;

    private readonly Adsiat_Parametros_facturacion adsiat_parametros_facturacion = new Adsiat_Parametros_facturacion();
    private readonly Adsiat_Endpoint adsiat_endpoint = new Adsiat_Endpoint();
    private readonly Adsiat_Token adsiat_token = new Adsiat_Token();
   // private readonly Funciones_SIAT funciones_SIAT = new Funciones_SIAT();
    private async Task<bool> Obtener_EndPoint_Token(DBContext _context, int almacen)
    {
        _codSucursal = await adsiat_parametros_facturacion.Sucursal(_context, almacen);
        _codAmbiente = await adsiat_parametros_facturacion.Ambiente(_context, almacen);
        _codSistema = await adsiat_parametros_facturacion.CodigoSistema(_context, _codSucursal);
        endpointAddress = await adsiat_endpoint.Obtener_End_Point(_context, 5, _codAmbiente);
        token = "TokenApi " + await adsiat_token.Obtener_Token_Delegado_Activo(_context, _codSistema, _codAmbiente);
        return false;
    }
    public async Task<bool> VerificarComunicacion(DBContext _context, int almacen)
    {
        var ini = await Obtener_EndPoint_Token(_context, almacen);
        bool miRespuesta = false;
        var binding = new BasicHttpBinding
        {
            SendTimeout = TimeSpan.FromSeconds(1000),
            MaxBufferSize = int.MaxValue,
            MaxReceivedMessageSize = int.MaxValue,
            AllowCookies = true,
            ReaderQuotas = XmlDictionaryReaderQuotas.Max
        };

        ServicePointManager.Expect100Continue = true;
        ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072; // TLS 1.2
        binding.Security.Mode = BasicHttpSecurityMode.Transport;

        var address = new EndpointAddress(endpointAddress);
        var servicio = new ServicioFacturacionClient(binding, address);
        servicio.Endpoint.EndpointBehaviors.Add(new CustomAuthenticationBehaviour(token));
        try
        {
            MyResult_Comunicacion = await servicio.verificarComunicacionAsync();
            MyResult_Comunicacion_resp = MyResult_Comunicacion.@return;
            miRespuesta = MyResult_Comunicacion_resp.transaccion;
            return miRespuesta;

        }
        catch (Exception e)
        {
            return false;
        }
    }
    public async Task<ResultadoRecepcionFactura> Recepcion_Factura(DBContext _context,string fecha_envio, int almacen, int codAmbiente, int codDocSector,int codEmision, int codModalidad, int codPtoVta, string codSistema,
                                                                        int codSucursal, string cuis, string cufd, long nit, int tipoFacturaDocumento, byte[] archivo, string hashArchivo)
    {
        var ini = await Obtener_EndPoint_Token(_context, almacen);
        var miRespuesta = new ResultadoRecepcionFactura();

        // Configuración del binding y del servicio
        var binding = new BasicHttpBinding
        {
            SendTimeout = TimeSpan.FromSeconds(1000),
            MaxBufferSize = int.MaxValue,
            MaxReceivedMessageSize = int.MaxValue,
            AllowCookies = true,
            ReaderQuotas = System.Xml.XmlDictionaryReaderQuotas.Max
            //Security = { Mode = BasicHttpSecurityMode.Transport }
        };

        ServicePointManager.Expect100Continue = true;
        ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072; // TLS 1.2
        binding.Security.Mode = BasicHttpSecurityMode.Transport;

        var address = new EndpointAddress(endpointAddress);
        var servicio = new ServicioFacturacionClient(binding, address);
        servicio.Endpoint.EndpointBehaviors.Add(new CustomAuthenticationBehaviour(token));

        // Preparación de la solicitud
        var solRecepcion = new solicitudRecepcionFactura
        {
            codigoAmbiente = codAmbiente,
            codigoDocumentoSector = codDocSector,
            codigoEmision = codEmision,
            codigoModalidad = codModalidad,
            codigoPuntoVentaSpecified = true,
            codigoPuntoVenta = codPtoVta,
            codigoSistema = codSistema,
            codigoSucursal = codSucursal,
            cufd = cufd,
            cuis = cuis,
            nit = nit,
            tipoFacturaDocumento = tipoFacturaDocumento,
            fechaEnvio = fecha_envio,
            archivo = archivo,
            hashArchivo = hashArchivo
        };

        try
        {
            // Llamada al servicio
            MyResult_RecepcionFactura_resp = await servicio.recepcionFacturaAsync(solRecepcion);
            MyResult_RecepcionFactura = MyResult_RecepcionFactura_resp.RespuestaServicioFacturacion;
            // Procesamiento de la respuesta
            miRespuesta.CodDescripcion = MyResult_RecepcionFactura.codigoDescripcion;
            miRespuesta.CodEstado = MyResult_RecepcionFactura.codigoEstado;
            //miRespuesta.codEstado_desc = await sia_DAL.adsiat_mensaje_servicio.Instancia.DescripcionCodigoAsync(miRespuesta.CodEstado);
            miRespuesta.CodEstadoDesc = "";
            miRespuesta.CodRecepcion = MyResult_RecepcionFactura.codigoRecepcion;
            //miRespuesta.CodRecepcionDesc = await sia_DAL.adsiat_mensaje_servicio.Instancia.DescripcionCodigoAsync(miRespuesta.codRecepcion);
            miRespuesta.CodRecepcionDesc = "";
            miRespuesta.Transaccion = MyResult_RecepcionFactura .transaccion;

            // Crear lista de mensajes
            miRespuesta.ListaMsg.Clear();
            if (MyResult_RecepcionFactura.mensajesList != null)
            {
                foreach (var mensaje in MyResult_RecepcionFactura.mensajesList)
                {
                    miRespuesta.ListaMsg.Add(mensaje.codigo + " - " + mensaje.descripcion);
                }
            }
        }
        catch (Exception e)
        {
            // Manejo de errores
            miRespuesta.CodDescripcion = "Error al enviar la factura al servidor del SIN!!! " + e.Message;
            miRespuesta.CodEstado = -111;
            miRespuesta.CodEstadoDesc = "Error";
            miRespuesta.CodRecepcion = "-1";
            miRespuesta.CodRecepcionDesc = "Error en Recepción de Factura";
            miRespuesta.Transaccion = false;
            miRespuesta.ListaMsg.Add("Error - " + e.Message);
        }

        return miRespuesta;
    }


}

