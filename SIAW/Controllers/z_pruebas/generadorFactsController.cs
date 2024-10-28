using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using siaw_DBContext.Data;
using siaw_DBContext.Models;
using siaw_DBContext.Models_Extra;
using siaw_funciones;
using siaw_ws_siat;
using System.Data;

namespace SIAW.Controllers.z_pruebas
{
    [Route("api/pruebas/[controller]")]
    [ApiController]
    public class generadorFactsController : ControllerBase
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
        private readonly GZip gzip = new GZip();

        private readonly string _controllerName = "prgfacturarNR_cufdController";




    }
}
