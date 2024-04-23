using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using siaw_DBContext.Data;
using siaw_DBContext.Models;
using System.Net;
using Microsoft.AspNetCore.Authorization;
using siaw_funciones;
using System.Threading;
using System.Drawing;

namespace SIAW.Controllers.ventas.transaccion
{
    [Route("api/venta/transac/[controller]")]
    [ApiController]
    public class prgestadoclienteController : ControllerBase
    {
        private readonly siaw_funciones.Cliente cliente = new siaw_funciones.Cliente();
        private readonly siaw_funciones.TipoCambio tipoCambio = new siaw_funciones.TipoCambio();
        private readonly siaw_funciones.Anticipos_Vta_Contado anticipos_Vta_Contado = new Anticipos_Vta_Contado();
        private readonly siaw_funciones.Funciones funciones = new Funciones();
        private readonly UserConnectionManager _userConnectionManager;
        public prgestadoclienteController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }


        [HttpGet]
        [Route("getEstadoPagosCliente/{userConn}/{codcliente}/{fechal}/{usuario}/{codempresa}")]
        public async Task<ActionResult<object>> getEstadoPagosCliente(string userConn, string codcliente, DateTime fechal, string usuario, string codempresa)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var resultados = await mostrar(_context,codcliente,fechal,usuario,codempresa);
                    return Ok(resultados);
                }
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }


        [HttpPost]
        [Route("calcularSeleccion/{userConn}/{usuario}/{codempresa}")]
        public async Task<ActionResult<object>> calcularSeleccion(string userConn, string usuario, string codempresa, tablaEstadoCliente tablaEstadoCliente)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    string moneda = await tipoCambio.monedatdc(_context, usuario, codempresa);
                    double monto = (double)await tipoCambio._conversion(_context, moneda, tablaEstadoCliente.moneda, DateTime.Now, (decimal)tablaEstadoCliente.diferencia);
                    string montoSeleccionado = monto.ToString("#,0.00", new System.Globalization.CultureInfo("en-US")) + " " + moneda;
                    return Ok(new { montoSeleccionado = montoSeleccionado });
                }
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }




        private async Task<object> mostrar(DBContext _context, string codcliente, DateTime fechal, string usuario, string codempresa)
        {
            List<tablaEstadoCliente> tablaEstadoCliente = new List<tablaEstadoCliente>();
            // Pagos
            tablaEstadoCliente = await _context.coplancuotas.Join(_context.veremision,
                               p => p.coddocumento,
                               r => r.codigo,
                               (p, r) => new { p, r })
                        .Where(x => x.p.cliente == codcliente &&
                                    x.p.vencimiento <= fechal &&
                                    x.p.monto > x.p.montopagado)
                        .OrderBy(x => x.r.id)
                        .ThenBy(x => x.r.numeroid)
                        .ThenBy(x => x.p.nrocuota)
                        .Select(x => new tablaEstadoCliente
                        {
                            id = x.r.id,
                            numeroid = x.r.numeroid,
                            fecha = x.r.fecha,
                            nrocuota = x.p.nrocuota,
                            monto = (double)x.p.monto,
                            montopagado = (double)(x.p.montopagado ?? 0),
                            diferencia = (double)(x.p.monto - (x.p.montopagado ?? 0)),
                            moneda = x.p.moneda,
                            vencimiento = x.p.vencimiento,
                            diasvenc = (DateTime.Now.Date - Convert.ToDateTime(x.p.vencimiento)).Days    // antes ponerdias()
                        }).ToListAsync();
            tablaEstadoCliente = await poneracumulado(tablaEstadoCliente);
            string lcredito = await ponerlimitecredito(_context, codcliente);
            string montototal = await sacarmontototal(_context, usuario, codempresa, tablaEstadoCliente);
            // hasta aca tabla detalle

            // cheques
            var dt_cheques = await _context.fncheque_cliente
                .Where(i => i.anulado == false && i.codcliente == codcliente && i.montorest > 0)
                .Select(i => new
                {
                    i.id,
                    i.numeroid,
                    i.nrorecibo,
                    i.codbanco,
                    i.monto,
                    i.montorest,
                    i.codmoneda,
                    i.fecha,
                    i.fechapago,
                    i.obs
                })
                .OrderBy(i => i.fechapago).ToListAsync();

            // anticipos
            var dt_anticipos = await _context.coanticipo
                .Where(i => i.montorest > 0 && i.codcliente == codcliente && i.anulado == false)
                .Select(i => new
                {
                    codanticipo = i.codigo,
                    i.id,
                    i.numeroid,
                    i.codcliente,
                    i.para_venta_contado,
                    i.nrorecibo,
                    i.fecha,
                    i.monto,
                    i.montorest
                }).ToListAsync();
            foreach (var reg in dt_anticipos)
            {
                await anticipos_Vta_Contado.ActualizarMontoRestAnticipo(_context, reg.id, reg.numeroid, 0, reg.codanticipo, 0, codempresa);
            }
            dt_anticipos.Clear();
            dt_anticipos = await _context.coanticipo
                .Where(i => i.montorest > 0 && i.codcliente == codcliente && i.anulado == false)
                .Select(i => new
                {
                    codanticipo = i.codigo,
                    i.id,
                    i.numeroid,
                    i.codcliente,
                    i.para_venta_contado,
                    i.nrorecibo,
                    i.fecha,
                    i.monto,
                    i.montorest
                }).ToListAsync();

            // obtener venta en semana urgente
            var nroVentasUrg = await SemanaVentasUrgentes(_context, codcliente, fechal);
            return (new
            {
                tablaEstadoCliente = tablaEstadoCliente,
                tablaCheques = dt_cheques,
                tablaAnticipos = dt_anticipos,
                totCredito = lcredito,
                montototal = montototal,
                nroVentasUrgSem = nroVentasUrg
            });
        }


        private async Task<List<tablaEstadoCliente>> poneracumulado(List<tablaEstadoCliente> tablaEstadoCliente)
        {
            if (tablaEstadoCliente.Count() > 0)
            {
                double monto = 0;
                string id = tablaEstadoCliente[0].id;
                int numeroid = tablaEstadoCliente[0].numeroid;
                foreach (var reg in tablaEstadoCliente)
                {
                    if (reg.id == id && reg.numeroid == numeroid)
                    {
                        monto = monto + reg.diferencia;
                        reg.total = monto;
                    }
                    else
                    {
                        id = reg.id;
                        numeroid = reg.numeroid;
                        monto = 0;
                        monto = monto + reg.diferencia;
                        reg.total = monto;
                    }
                }
            }
            return tablaEstadoCliente;
        }

        private async Task<string> ponerlimitecredito(DBContext _context, string codcliente)
        {
            var result = await _context.vecliente.Where(i => i.codigo == codcliente)
                .Select(i => new
                {
                    i.credito,
                    i.moneda
                })
                .FirstOrDefaultAsync();
            if (result != null)
            {
                return result.credito + " " + result.moneda;
            }
            return "0" + " " + result.moneda;
        }

        private async Task<string> sacarmontototal(DBContext _context, string usuario, string codempresa, List<tablaEstadoCliente> tablaEstadoCliente)
        {
            double monto = 0;
            var moneda = await tipoCambio.monedatdc(_context, usuario, codempresa);
            foreach (var reg in tablaEstadoCliente)
            {
                monto = monto + (double)await tipoCambio._conversion(_context, moneda, reg.moneda, DateTime.Now, (decimal)reg.diferencia);
            }
            return monto.ToString("#,0.00", new System.Globalization.CultureInfo("en-US")) + " " + moneda;
        }


        private async Task<int> SemanaVentasUrgentes(DBContext _context, string codcliente, DateTime fecha)
        {
            DateTime desde = funciones.PrincipioDeSemana(fecha);
            DateTime hasta = funciones.FinDeSemana(fecha);
            try
            {
                var resultado = await _context.veproforma
                    .Where(i => i.anulada == false && i.codcliente == codcliente && i.preparacion == "URGENTE" && i.fecha > desde && i.fecha < hasta && i.aprobada == true)
                    .CountAsync();
                return resultado;
            }
            catch (Exception)
            {
                return 0;
            }
        }

    }


    public class tablaEstadoCliente
    {
        public string id { get; set; }
        public int numeroid { get; set; }
        public DateTime fecha { get; set; }
        public int nrocuota { get; set; }
        public double monto { get; set; }
        public double montopagado { get; set; }
        public double diferencia { get; set; }
        public string moneda { get; set; }
        public DateTime vencimiento { get; set; }
        public int diasvenc { get; set; } = 0;
        public double total { get; set; } = 0;
    }
}
