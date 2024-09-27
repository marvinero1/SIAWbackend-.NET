using Servp_ObtencionCodigos;
using System.Net;
using System.ServiceModel;
using System.Xml;
using Microsoft.EntityFrameworkCore;
using siaw_DBContext.Data;
using siaw_ws_siat;

public class ServCodigos
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
    public class ResultadoVerificarNIT
    {
        public string Codigo { get; set; }
        public string Descripcion { get; set; }
        public bool Transaccion { get; set; }
        public List<string> ListaMsg { get; set; } = new List<string>();
    }

    public class ResultadoObtenerCUFD_Ag
    {
        public bool Transaccion { get; set; }
        public string CUFD { get; set; } = "";
        public string CodigoControl { get; set; } = "";
        public DateTime FechaVigencia { get; set; }
        public List<string> ListaMsg { get; set; } = new List<string>();
        public string Direccion { get; set; } = "";
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

    public class ResultadoObtencionCUI
    {
        public string Codigo { get; set; }
        public string FechaVigencia { get; set; }
        public bool Transaccion { get; set; }
        public List<string> ListaMsg { get; set; } = new List<string>();
    }

    private int _codSucursal = 0;
    private int _codAmbiente = 0;
    private string _codSistema = "";

    private string endpointAddress = "";
    private string token = "";

    private respuestaComunicacion MyResult;
    private respuestaCuisMasivo MyResult1;

    private cuisResponse MyResult_Cui;
    private respuestaCuis MyResult_Cui_resp;

    //Para CUFD por almacen
    private respuestaCufd MyResult_Cufd;
    private mensajeServicio MyResult_Cufd_Detalle;

    //Para CUIS masivo
    private respuestaCuisMasivo MyResult_Cuis_Masivo;
    private respuestaCuisMasivo MyResult_Cui_Masivo;
    private respuestaListaRegistroCuisSoapDto MyResult_Cui_Masivo_Detalle;

    //Para CUFD Masivo
    private respuestaCufdMasivo MyResult_Cufd_Masivo;
    private respuestaListaRegistroCufdSoapDto MyResult_Cufd_Masivo_Detalle;

    //Para CUFD una agencia
    private mensajeServicio MyResult_Cufd_Lista;

    //Para validar nit
    private verificarNitResponse MyResult_Verificar_NIT;
    private respuestaVerificarNit MyResult_Verificar_NIT_resp;
    private mensajeServicio MyResult_Verificar_Nit_Lista;

    //Para CUI una agencia
    private mensajeServicio MyResult_Cui_Lista;

    private readonly Adsiat_Parametros_facturacion adsiat_parametros_facturacion = new Adsiat_Parametros_facturacion();
    private readonly Adsiat_Endpoint adsiat_endpoint = new Adsiat_Endpoint();
    private readonly Adsiat_Token adsiat_token = new Adsiat_Token();
    private async Task<bool> Obtener_EndPoint_Token(DBContext _context, int almacen)
    {
        _codSucursal = await adsiat_parametros_facturacion.Sucursal(_context, almacen);
        _codAmbiente = await adsiat_parametros_facturacion.Ambiente(_context, almacen);
        _codSistema = await adsiat_parametros_facturacion.CodigoSistema(_context, _codSucursal);
        endpointAddress = await adsiat_endpoint.Obtener_End_Point(_context, 1, _codAmbiente);
        token = "TokenApi " + await adsiat_token.Obtener_Token_Delegado_Activo(_context, _codSistema, _codAmbiente);
        return false;
    }
    public async Task<ResultadoObtencionCUI> Solicitar_CUIS(DBContext _context, int codambiente, int codmodalidad, int codptovta, string codsistema, int codsucursal, string nit, int almacen )
    {
        var ini = await Obtener_EndPoint_Token(_context, almacen);
        var miRespuesta = new ResultadoObtencionCUI();
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
        var servicio = new ServicioFacturacionCodigosClient(binding, address);
        servicio.Endpoint.EndpointBehaviors.Add(new CustomAuthenticationBehaviour(token));

        try
        {
            var solCui = new solicitudCuis
            {
                codigoAmbiente = codambiente,
                codigoModalidad = codmodalidad,
                codigoPuntoVenta = codptovta,
                codigoSistema = codsistema,
                codigoSucursal = codsucursal,
                codigoPuntoVentaSpecified = true,
                nit = Convert.ToInt32(nit)
            };

            MyResult_Cui = await servicio.cuisAsync(solCui);
            MyResult_Cui_resp = MyResult_Cui.RespuestaCuis;
            miRespuesta.Transaccion = MyResult_Cui_resp.transaccion;

            if (miRespuesta.Transaccion)
            {
                miRespuesta.Codigo = MyResult_Cui_resp.codigo;
                miRespuesta.FechaVigencia = MyResult_Cui_resp.fechaVigencia.ToShortDateString();
            }
            else
            {
                miRespuesta.Codigo = MyResult_Cui_resp.codigo;
                miRespuesta.FechaVigencia = MyResult_Cui_resp.fechaVigencia.ToShortDateString();
                miRespuesta.ListaMsg.Clear();

                for (int y = 0; y < MyResult_Cui_resp.mensajesList.Length; y++)
                {
                    MyResult_Cui_Lista = MyResult_Cui_resp.mensajesList[y];
                    miRespuesta.ListaMsg.Add(MyResult_Cui_Lista.codigo + "-" + MyResult_Cui_Lista.descripcion);
                }
            }
        }
        catch (Exception e)
        {
            miRespuesta.Codigo = "Error al obtener nuevo Cufd. " + e.Message;
            miRespuesta.FechaVigencia = "20000101";
            miRespuesta.Transaccion = false;
            miRespuesta.ListaMsg.Clear();
        }

        return miRespuesta;
    }
    public async Task<ResultadoVerificarNIT> Verificar_Nit(DBContext _context, int codambiente, int codmodalidad, string codsistema, int codsucursal,string cuis, long nit, long nit_verificar, int almacen)
    {
        var ini = await Obtener_EndPoint_Token(_context, almacen);
        var miRespuesta = new ResultadoVerificarNIT();
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
        var servicio = new ServicioFacturacionCodigosClient(binding, address);
        servicio.Endpoint.EndpointBehaviors.Add(new CustomAuthenticationBehaviour(token));

        try
        {
            var sol_Veri_nit = new solicitudVerificarNit
            {
                codigoAmbiente = codambiente,
                codigoModalidad = codmodalidad,
                codigoSistema = codsistema,
                codigoSucursal = codsucursal,
                cuis = cuis,
                nit = Convert.ToInt32(nit),
                nitParaVerificacion = Convert.ToInt32(nit_verificar)
            };

            MyResult_Verificar_NIT = await servicio.verificarNitAsync(sol_Veri_nit);
            MyResult_Verificar_NIT_resp = MyResult_Verificar_NIT.RespuestaVerificarNit;
            miRespuesta.Transaccion = MyResult_Verificar_NIT_resp.transaccion;

            if (miRespuesta.Transaccion)
            {
                miRespuesta.ListaMsg.Clear();

                for (int y = 0; y < MyResult_Verificar_NIT_resp.mensajesList.Length; y++)
                {
                    MyResult_Verificar_Nit_Lista = MyResult_Verificar_NIT_resp.mensajesList[y];
                    miRespuesta.ListaMsg.Add(MyResult_Verificar_Nit_Lista.codigo + "-" + MyResult_Verificar_Nit_Lista.descripcion);
                    miRespuesta.Codigo = MyResult_Verificar_Nit_Lista.codigo.ToString();
                    miRespuesta.Descripcion = MyResult_Verificar_Nit_Lista.descripcion;
                }
            }
            else
            {
                miRespuesta.ListaMsg.Clear();

                for (int y = 0; y < MyResult_Verificar_NIT_resp.mensajesList.Length; y++)
                {
                    MyResult_Verificar_Nit_Lista = MyResult_Verificar_NIT_resp.mensajesList[y];
                    miRespuesta.ListaMsg.Add(MyResult_Verificar_Nit_Lista.codigo + "-" + MyResult_Verificar_Nit_Lista.descripcion);
                    miRespuesta.Codigo = MyResult_Verificar_Nit_Lista.codigo.ToString();
                    miRespuesta.Descripcion = MyResult_Verificar_Nit_Lista.descripcion;
                }
            }
        }
        catch (Exception e)
        {
            miRespuesta.Codigo = "0 " + e.Message;
            miRespuesta.Descripcion = "0";
            miRespuesta.Transaccion = false;
            miRespuesta.ListaMsg.Clear();
        }

        return miRespuesta;
    }
}
