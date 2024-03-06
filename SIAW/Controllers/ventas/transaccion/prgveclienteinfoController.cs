using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using siaw_DBContext.Data;
using siaw_DBContext.Models;
using System.Net;
using Microsoft.AspNetCore.Authorization;
using siaw_funciones;
using MessagePack;
using static siaw_funciones.Validar_Vta;
using Microsoft.CodeAnalysis.Differencing;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Diagnostics.Contracts;

namespace SIAW.Controllers.ventas.transaccion
{
    [Route("api/venta/transac/[controller]")]
    [ApiController]
    public class prgveclienteinfoController : ControllerBase
    {
        private readonly Cliente cliente = new Cliente();
        private readonly Empresa empresa = new Empresa();
        private readonly Creditos creditos = new Creditos();
        private readonly Cobranzas cobranzas = new Cobranzas();
        private readonly Ventas ventas = new Ventas();
        private readonly UserConnectionManager _userConnectionManager;
        public prgveclienteinfoController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }

        // GET: api/infoCliente
        [HttpGet("{userConn}/{codcliente}/{codempresa}/{usuario}")]
        public async Task<ActionResult<IEnumerable<inmatriz>>> getInfoCliente(string userConn, string codcliente, string codempresa, string usuario)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    // info cliente
                    var datosCli = await datosCliente(_context, codcliente);

                    // creditos
                    var creditosCli = await creditosCliente(_context, codcliente, codempresa, usuario);

                    //  anticipos y cobranzas no distribuidas 
                    var antCobNoDistriCli = await antCobnoDistCli(_context, codcliente);

                    // lista precos habilitados
                    var preciosCli = await tarifasCliente(_context, codcliente);

                    // proformas aprobadas no transferidas
                    var profAproNoTranfCli = await profApronoTransf(_context, codcliente);

                    // proformas no aprobadas no anuladas
                    var profNoAproNoAnulad = await profnoApronoAnulad(_context, codcliente);

                    // tiendas y titulares
                    var tiendasTitulaesCli = await tiendasTitualesCliente(_context, codcliente);

                    // ultimo envio
                    var ultimoEnvioCli = await ultimoEnvio(_context, codcliente);

                    // ultimas compras
                    var ultiComprCli = await ultimasCompras(_context, codcliente);

                    // promocion especial
                    var promEspecialCli = await promEspecial(_context, codcliente);

                    // condiciones de cliente final
                    var inforCliFinal = await infCliFinal(_context, codcliente);

                    // otas condiciones de venta
                    var otrasCondVentaCli = await otrasCondVent(_context, codcliente);
                }

            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }


        }


        private async Task<object> datosCliente(DBContext _context, string codcliente)
        {
            var data = await _context.vecliente
                .Where(i=> i.codigo ==  codcliente)
                .Select (i=> new
                {
                    codcliente = i.codigo,
                    cliente_pertec = (i.cliente_pertec ?? false) == true ? "***SI ES CLIENTE PERTEC***" : "NO ES CLIENTE PERTEC",
                    razonsocial = i.razonsocial,
                    habilitado = (i.habilitado??false) == true ? "Si" : "No",
                    nit = i.nit,
                    fapertura = i.fapertura,
                    casilla = i.casilla,
                    email = i.email,
                    garantia = i.garantia,
                    obs = i.obs,
                    tipo = i.tipo,
                    casual = (i.casual ?? false) == true ? "Si" : "No",
                    contra_entrega = (i.contra_entrega ?? false) == true ? "SIEMPRE CONTRA-ENTREGA" : "",
                    codvendedor = i.codvendedor,
                    controla_empaque_cerrado = (i.controla_empaque_cerrado ?? false) == true ? "DEBE CUMPLIR EMPAQUES CERRADOS" : "NO ES NECESARIO QUE CUMPLA EMPAQUES CERRADOS",
                })
                .FirstOrDefaultAsync();

            return data;

        }

        private async Task<object> creditosCliente(DBContext _context, string codcliente, string codempresa, string usuario)
        {
            var respuestaJson = new Dictionary<string, object>();
            var codigoPrincipal_local = await cliente.CodigoPrincipal(_context, codcliente);
            string cliente_principal_local = codcliente;
            string CodigosIguales_local = "'" + codcliente + "'";
            string moneda_credito = "";
            // Solo considerarlo el principal si Tiene el mismo NIT, caso contrario usar solo el codigo individual de cliente
            if (await cliente.NIT(_context,codigoPrincipal_local) == await cliente.NIT(_context,codcliente))
            {
                cliente_principal_local = codigoPrincipal_local;
                CodigosIguales_local = await cliente.CodigosIgualesMismoNIT(_context, codcliente);  //< ------solo los de mismo NIT
            }

            if (await cliente.Cliente_Tiene_Sucursal_Nacional(_context,cliente_principal_local))
            {
                // si el cliente es parte de una agrupacion cial a nivel nacional entre agencias de Pertec a nivel nacional
                string casa_matriz_Nacional = await cliente.CodigoPrincipal_Nacional(_context, cliente_principal_local);
                int CODALM = 0;
                double credito_principal = 0;
                double saldo_local = 0;
                double saldo_x_pagar_nal = 0;
                string monedacliente = await cliente.monedacliente(_context, cliente_principal_local, usuario, codempresa);
                string monedaext = await empresa.monedaext(_context, codempresa);

                if (casa_matriz_Nacional.Trim().Length > 0)
                {
                    CODALM = await cliente.Almacen_Casa_Matriz_Nacional(_context, casa_matriz_Nacional);
                }

                if (CODALM == await cliente.almacen_de_cliente(_context,cliente_principal_local))
                {
                    //busca en el credito en la conexion local
                    credito_principal = await creditos.credito(_context, cliente_principal_local);
                }
                else
                {
                    //buscara el credito en la agencia donde esta la casa matriz
                    credito_principal = await creditos.Obtener_Credito_Casa_Matriz(_context, cliente_principal_local,"US");
                }

                ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////7
                //BUSCA EL SALDO LOCAL
                //obtener el saldo pendiente de pago de todo el grupo cial
                saldo_local = (double)(await _context.coplancuotas
                    .Join(_context.veremision,
                        p1 => p1.coddocumento,
                        p2 => p2.codigo,
                        (p1, p2) => new { p1, p2 })
                    .Where(joinResult =>
                        joinResult.p1.moneda == monedacliente &&
                        joinResult.p1.cliente.Contains(CodigosIguales_local) &&
                        joinResult.p2.anulada == false)
                    .Select(joinResult => (decimal?)(joinResult.p1.monto - joinResult.p1.montopagado))
                    .SumAsync() ?? 0);

                //busca el SALDO NACIONAL si tiene sucursales en otras agencia
                //implementado el 09-05-2020
                var respSld = await cliente.Cliente_Saldo_Pendiente_Nacional(_context, await creditos.CodigoPrincipalCreditos(_context, cliente_principal_local), monedacliente);

                respuestaJson.Add("CASA_MATRIZ_CREDITOS", casa_matriz_Nacional);
                respuestaJson.Add("AG_CASA_MATRIZ", CODALM);
                respuestaJson.Add("CLIENTES_MISMO_NIT", CodigosIguales_local);


                if (respSld.resp == -1)
                {
                    saldo_x_pagar_nal = 0;
                }
                else
                {
                    saldo_x_pagar_nal = respSld.resp;
                }

                if (credito_principal > 0)
                {
                    string[] datos_credito = new string[3];
                    datos_credito = await creditos.Credito_Otorgado_Vigente(_context, await creditos.CodigoPrincipalCreditos(_context, codcliente));
                    respuestaJson.Add("CREDITO", credito_principal);
                    respuestaJson.Add("OTORGADO", credito_principal);
                    respuestaJson.Add("VENCIMIENTO", datos_credito[2]);

                    respuestaJson.Add("TTL_POR_PAGAR_LOCAL", saldo_local);
                    respuestaJson.Add("TTL_POR_OTRAS_AGS", saldo_x_pagar_nal);
                    moneda_credito = await creditos.Credito_Fijo_Asignado_Vigente_Moneda(_context, codcliente);

                    respuestaJson.Add("CREDITO_DISPONIBLE", credito_principal - (saldo_local + saldo_x_pagar_nal) + " " + moneda_credito + " ");
                }
                else
                {
                    respuestaJson.Add("CREDITO", "NO TIENE");
                    respuestaJson.Add("CREDITO_DISPONIBLE", 0);
                }
            }
            else
            {
                respuestaJson.Add("CASA_MATRIZ_CREDITOS", cliente_principal_local);
                respuestaJson.Add("CLIENTES_MISMO_NIT", CodigosIguales_local);
                if (await creditos.Cliente_Tiene_Linea_De_Credito_Valida(_context,await creditos.CodigoPrincipalCreditos(_context, codcliente)))
                {
                    var creditodisp_cliente = await _context.vecliente
                        .Where(i => i.codigo == codcliente).Select(i => i.creditodisp).FirstOrDefaultAsync() ?? 0;

                    string[] datos_credito = new string[3];
                    datos_credito = await creditos.Credito_Otorgado_Vigente(_context, await creditos.CodigoPrincipalCreditos(_context, codcliente));
                    respuestaJson.Add("CREDITO", datos_credito[0]);
                    respuestaJson.Add("OTORGADO", datos_credito[1]);
                    respuestaJson.Add("VENCIMIENTO", datos_credito[2]);

                    moneda_credito = await creditos.Credito_Fijo_Asignado_Vigente_Moneda(_context, codcliente);
                    respuestaJson.Add("CREDITO_DISPONIBLE", creditodisp_cliente + " " + moneda_credito + " ");
                }
                else
                {
                    respuestaJson.Add("CREDITO", "NO TIENE");
                    respuestaJson.Add("CREDITO_DISPONIBLE", 0);
                }
            }



            return respuestaJson;

        }

        private async Task<object> antCobnoDistCli(DBContext _context, string codcliente)
        {
            var total = await cobranzas.CobranzasSinDistribuir(_context, codcliente);
            return (new
            {
                ANTICIPOS_COBRANZAS_NO_DISTRIBUIDAS = total
            });
        }

        private async Task<object> tarifasCliente(DBContext _context, string codcliente)
        {
            var tarifas = await _context.veclienteprecio.Where(i => i.codcliente == codcliente)
                .OrderBy(i => i.codtarifa).Select(i => i.codtarifa).ToListAsync();

            return (new
            {
                LISTA_PRECIOS_HABILITADAS = tarifas
            });
        }

        private async Task<object> profApronoTransf(DBContext _context, string codcliente)
        {
            var detailProfs = await _context.veproforma
                .Where(i => i.aprobada == true && i.transferida == false && i.codcliente == codcliente)
                .Select(i => new
                {
                    dataInf = i.id + "-" + i.numeroid + " " + i.fecha.ToShortDateString() + " Tot: " + i.total
                })
                .ToListAsync();

            var total = await _context.veproforma
                .Where(i => i.aprobada == true && i.transferida == false && i.codcliente == codcliente)
                .SumAsync(i => i.total);

            var creditodisp_cliente = await _context.vecliente
                        .Where(i => i.codigo == codcliente).Select(i => i.creditodisp).FirstOrDefaultAsync() ?? 0;

            return (new
            {
                dataInf = detailProfs,
                total = total,
                CREDITO_DISP_REAL = creditodisp_cliente - total
            });
        }

        private async Task<object> profnoApronoAnulad(DBContext _context, string codcliente)
        {
            var detailProfs = await _context.veproforma.Where(i => i.aprobada == false && i.anulada == false && i.codcliente == codcliente)
                .OrderByDescending(i => i.fecha)
                .Select(i => new
                {
                    dataInf = i.id + "-" + i.numeroid + " " + i.fecha.ToShortDateString() + " Tot: " + i.total + "  UsrReg: " + i.usuarioreg
                })
                .ToListAsync();

            var total = await _context.veproforma
                .Where(i => i.aprobada == false && i.anulada == false && i.codcliente == codcliente)
                .SumAsync(i => i.total);

            var creditodisp_cliente = await _context.vecliente
                        .Where(i => i.codigo == codcliente).Select(i => i.creditodisp).FirstOrDefaultAsync() ?? 0;

            return (new
            {
                dataInf = detailProfs,
                total = total,
                CREDITO_DISP_REAL = creditodisp_cliente - total
            });
        }

        private async Task<object> tiendasTitualesCliente(DBContext _context, string codcliente)
        {
            var resultados = await _context.vetienda
                .GroupJoin(
                    _context.vetitular,
                    vd => vd.codigo,
                    vt => vt.codtienda,
                    (vd, vtGroup) => new { vd, vtGroup }
                )
                .SelectMany(
                    x => x.vtGroup.DefaultIfEmpty(),
                    (x, vt) => new { Tienda = x.vd, Titular = vt }
                )
                .Where(x => x.Tienda.codcliente == codcliente)
                .Select(x => new { x.Tienda, x.Titular })
                .ToListAsync();

            return resultados;
        }

        private async Task<object> ultimoEnvio(DBContext _context, string codcliente)
        {
            var ultimoEnv = await cliente.UltimoEnvioPor(_context, codcliente);
            return (new
            {
                ULTIMO_ENVIO_POR = ultimoEnv
            });
        }

        private async Task<object> ultimasCompras(DBContext _context, string codcliente)
        {
            var ultCompr = await _context.veremision
                .Where(v => v.anulada == false && v.codcliente == codcliente)
                .OrderByDescending(v => v.fecha)
                .Take(5)
                .Select(v => new ultiComp
                {
                    CODIGO = v.codigo,
                    ID = v.id,
                    NUMEROID = v.numeroid,
                    FECHA = v.fecha,
                    TIPO = ""
                }).ToListAsync();
            foreach (var reg in ultCompr)
            {
                if (await ventas.remision_es_PP(_context,reg.CODIGO))
                {
                    reg.TIPO = "Pronto Pago";
                }
                else
                {
                    reg.TIPO = "Credito";
                }
            }
            return ultCompr;
        }

        private async Task<object> promEspecial(DBContext _context, string codcliente)
        {
            double prom = await ventas.promocion_monto(_context, codcliente,DateTime.Now);
            double usado = await ventas.promocion_usado(_context, codcliente,DateTime.Now);
            return (new
            {
                Monto_Total = prom,
                Usado = usado,
                Disponible = (prom - usado)
            });
        }

        private async Task<object> infCliFinal(DBContext _context, string codcliente)
        {
            var informacion = await _context.vecliente.Where(i => i.codigo == codcliente)
                .Select(i => new
                {
                    Es_Client_Final = i.es_cliente_final==true ? "Si": "No",
                    Es_Empresa = i.es_empresa == true ? "Si" : "No",
                    Controla_Empaque_Cerrado = i.controla_empaque_cerrado == true ? "Si" : "No",
                    Controla_Empaque_Minimo = (i.controla_empaque_minimo??false) == true ? "Si" : "No",
                    Controla_Monto_Minimo = i.controla_monto_minimo == true ? "Si" : "No",
                    Controla_Precios = i.controla_precios == true ? "Si" : "No",
                    Controla_Rango_Vta_Medio_Mayoreo = i.permitir_vta_rango_mediomay == true ? "Si" : "No",
                    Controla_Rango_Vta_Minorista = i.permitir_vta_rango_minorista == true ? "Si" : "No",
                }).FirstOrDefaultAsync();
            return informacion;
        }

        private async Task<object> otrasCondVent(DBContext _context, string codcliente)
        {
            var inform = await _context.vecliente.Where(i => i.codigo == codcliente)
                .Select(i => new
                {
                    Tipo_Cliente = i.tipo,
                    Situacion = i.situacion,
                    Ventas_Solo_Pronto_Pago = i.solo_pp == true ? "Si":"No",
                    Ventas_Solo_Contra_Entrega = i.contra_entrega == true ? "Si" : "No",
                    Tipo_Venta = i.tipoventa == 0 ? "Solo Contado": i.tipoventa == 1 ? "Tipo Venta: Solo Credito": "Contado y Credito"
                })
                .FirstOrDefaultAsync();
            return inform;
        }
    }


    public class ultiComp
    {
        public int CODIGO { get; set; }
        public string ID { get; set; }
        public int NUMEROID { get; set; }
        public DateTime FECHA { get; set; }
        public string TIPO { get; set; }
        
    }
}
