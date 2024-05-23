using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using siaw_DBContext.Data;
using siaw_DBContext.Models;
using Microsoft.AspNetCore.Authorization;
using siaw_funciones;
using siaw_DBContext.Models_Extra;

namespace SIAW.Controllers.ventas.transaccion
{
    [Route("api/venta/transac/[controller]")]
    [ApiController]
    public class prgveproforma_anticipoController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        private readonly TipoCambio tipoCambio = new TipoCambio();
        private readonly Ventas ventas = new Ventas();
        private readonly Cobranzas cobranzas = new Cobranzas();
        private readonly Cliente cliente = new Cliente();
        private readonly Anticipos_Vta_Contado anticipos_vta_contado = new Anticipos_Vta_Contado();
        public prgveproforma_anticipoController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }

        // GET: api/vedesextra
        [HttpGet]
        [Route("buscar_anticipos_asignados/{userConn}/{idpf}/{nroidpf}")]
        public async Task<ActionResult<IEnumerable<object>>> buscar_anticipos_asignados(string userConn, string idpf, int nroidpf)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var resultado = await _context.veproforma_anticipo
                        .Join(_context.veproforma, p1 => p1.codproforma, p2 => p2.codigo, (p1, p2) => new { p1, p2 })
                        .Join(_context.coanticipo, p => p.p1.codanticipo, p3 => p3.codigo, (p, p3) => new { p.p1, p.p2, p3 })
                        .Where(p => p.p2.id == idpf && p.p2.numeroid == nroidpf)
                        .OrderBy(p => p.p1.fechareg)
                        .Select(p => new
                        {
                            id = p.p3.id,
                            numeroid = p.p3.numeroid,
                            codvendedor = p.p3.codvendedor,
                            codigo = p.p1.codigo,
                            codproforma = p.p1.codproforma,
                            codanticipo = p.p1.codanticipo,
                            monto = p.p1.monto,
                            tdc = p.p1.tdc,
                            fechareg = p.p1.fechareg,
                            usuarioreg = p.p1.usuarioreg,
                            horareg = p.p1.horareg,
                            docanticipo = p.p3.id + "-" + p.p3.numeroid
                        }).ToListAsync();

                    return Ok(resultado);
                }
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }

        // GET: api/vedesextra
        [HttpPost]
        [Route("validaAsignarAnticipo/{userConn}/{txtcodmoneda_proforma}/{txtcodmoneda_anticipo}/{txtmonto_asignar}/{txtttl_proforma}")]
        public async Task<ActionResult<IEnumerable<object>>> validaAsignarAnticipo(string userConn, string txtcodmoneda_proforma, string txtcodmoneda_anticipo, double txtmonto_asignar, double txtttl_proforma, List<tabla_veproformaAnticipo> tabla_veproformaAnticipo)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    double ttl = await totalizar_asignacion(_context, txtcodmoneda_proforma, tabla_veproformaAnticipo);
                    string msgAlerta = "";
                    if (txtcodmoneda_proforma == txtcodmoneda_anticipo)
                    {
                        // No convertir el monto a asignar
                        ttl += txtmonto_asignar;
                        ttl = Math.Round(ttl, 2);
                    }
                    else
                    {
                        // si la moneda del anticipo y proforma no son iguales entonces convertir el monto asignar a la moneda de la proforma
                        if (txtcodmoneda_proforma != txtcodmoneda_anticipo)
                        {
                            msgAlerta = "La moneda del anticipo es diferente a la moneda de la proforma, el monto a asignar se convertirá a la moneda de la proforma.";
                        }
                        double monto_asignar = (double)await tipoCambio._conversion(_context, txtcodmoneda_proforma, txtcodmoneda_anticipo, DateTime.Now, (decimal)txtmonto_asignar);
                        ttl += monto_asignar;
                        ttl = Math.Round(ttl, 2);
                    }

                    if (ttl > txtttl_proforma)
                    {
                        return BadRequest(new { resp = "El monto que desea asignar mas el monto ya asignado supera el total de la proforma" });
                    }
                    return Ok(new {value = true, msg = msgAlerta });
                }
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }

        private async Task<double> totalizar_asignacion(DBContext _context, string codmoneda_proforma, List<tabla_veproformaAnticipo> tabla_veproformaAnticipo)
        {
            double resultado = 0;
            foreach (var reg in tabla_veproformaAnticipo)
            {
                if (reg.monto != null)
                {
                    // Desde 14/12/2023 realizar la conversion del monto asignado segun la moneda del anticipo y proforma
                    if (reg.codmoneda == codmoneda_proforma)
                    {
                        resultado += reg.monto;
                    }
                    else
                    {
                        resultado += (double)(await tipoCambio._conversion(_context, codmoneda_proforma, reg.codmoneda, DateTime.Now, (decimal)(reg.monto)));
                    }
                    resultado = Math.Round(resultado, 2);
                }
            }
            return resultado;
        }


        // GET: api/vedesextra
        [HttpPost]
        [Route("getTotabilizarAsignacion/{userConn}/{txtcodmoneda_proforma}")]
        public async Task<ActionResult<IEnumerable<object>>> getTotabilizarAsignacion(string userConn, string txtcodmoneda_proforma, List<tabla_veproformaAnticipo> tabla_veproformaAnticipo)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    double ttl = await totalizar_asignacion(_context, txtcodmoneda_proforma, tabla_veproformaAnticipo);
                    
                    return Ok(ttl);
                }
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }



        // GET: api/vedesextra
        [HttpPost]
        [Route("preparaParaAdd_monto/{userConn}/{pf_id}/{pf_nroid}/{txtcodmoneda_proforma}/{txtcodmoneda_anticipo}")]
        public async Task<ActionResult<IEnumerable<object>>> preparaParaAdd_monto(string userConn, string pf_id, int pf_nroid, string txtcodmoneda_proforma, string txtcodmoneda_anticipo, tabla_veproformaAnticipo tabla_veproformaAnticipo)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    tabla_veproformaAnticipo.codproforma = await ventas.codproforma(_context, pf_id, pf_nroid);
                    tabla_veproformaAnticipo.codanticipo = await cobranzas.CodAnticipo(_context, tabla_veproformaAnticipo.id_anticipo, tabla_veproformaAnticipo.nroid_anticipo);
                    tabla_veproformaAnticipo.docanticipo = tabla_veproformaAnticipo.id_anticipo + "-" + tabla_veproformaAnticipo.nroid_anticipo;

                    if (txtcodmoneda_proforma != txtcodmoneda_anticipo)
                    {
                        double monto_asignar = (double)await tipoCambio._conversion(_context, txtcodmoneda_proforma, txtcodmoneda_anticipo, DateTime.Now, (decimal)tabla_veproformaAnticipo.monto);
                        monto_asignar = Math.Round(monto_asignar, 2);
                        tabla_veproformaAnticipo.monto = monto_asignar;
                    }
                    tabla_veproformaAnticipo.tdc = 1;
                    tabla_veproformaAnticipo.codmoneda = txtcodmoneda_proforma;

                    return Ok(tabla_veproformaAnticipo);
                }
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }





        /// PESTAÑA NRO 2
        [Authorize]
        [HttpPut]
        [Route("btnrefrescar_Anticipos/{userConn}/{codcliente}/{fdesde}/{fhasta}/{nit}/{codclienteReal}/{codigoempresa}")]
        public async Task<ActionResult<IEnumerable<tabla_anticipos_pendientes>>> btnrefrescar_Anticipos(string userConn, string codcliente, DateTime fdesde, DateTime fhasta, string nit, string codclienteReal, string codigoempresa)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var resultados = await refrescatabla_anticipos_pendientesr_anticipos_pendientes(_context, codcliente, fdesde, fhasta, nit, codclienteReal, codigoempresa);
                    if (resultados.tabla_anticipos_pendientes == null)
                    {
                        return BadRequest(new { resp = resultados.mensaje });
                    }
                    return Ok (resultados.tabla_anticipos_pendientes);
                }
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }

        private async Task<(List<tabla_anticipos_pendientes> ? tabla_anticipos_pendientes, string mensaje)> refrescatabla_anticipos_pendientesr_anticipos_pendientes(DBContext _context, string codcliente, DateTime fdesde, DateTime fhasta, string nit, string codclienteReal, string codigoempresa)
        {
            // List<tabla_veproformaAnticipo> tabla_veproformaAnticipo
            var valida = validar_refrescar(fdesde, fhasta, nit, codclienteReal);
            if (!valida.bandera)
            {
                return (null, valida.mensaje);
            }
            //est primera ves se obtiene los datos para actualizar los saldos de los anticipos

            var tabla_veproformaAnticipo = await buscar_anticipos_pendientes(_context, fdesde, fhasta, nit, codcliente);
            await Actualizar_Restantes(_context, codigoempresa, tabla_veproformaAnticipo);

            //esta segunda vez se obtiene para mostrar los anticipos pendientes con sus saldos restantes correctos
            tabla_veproformaAnticipo = await buscar_anticipos_pendientes(_context, fdesde, fhasta, nit, codcliente);

            return (tabla_veproformaAnticipo, "");
        }

        private (bool bandera, string mensaje) validar_refrescar(DateTime fdesde, DateTime fhasta, string nit, string codclienteReal)
        {
            if (fdesde > fhasta)
            {
                return (false, "La fecha inicial no puede ser mayor a la fecha final!!!");
            }
            if (nit.Trim().Length == 0)
            {
                return (false, "Debe ingresar el NIT del cliente del cual se registro anticipo para venta contado!!!");
            }
            if (codclienteReal.Trim().Length == 0)
            {
                return (false, "Debe ingresar el codigo del cliente real!!!");
            }
            return (true, "");
        }


        private async Task<List<tabla_anticipos_pendientes>> buscar_anticipos_pendientes(DBContext _context,DateTime fdesde, DateTime fhasta, string nit, string codcliente)
        {
            var resultado = await _context.coanticipo
                .Where(p1 => p1.anulado == false &&
                              p1.fecha >= fdesde &&
                              p1.fecha <= fhasta &&
                              p1.codcliente == codcliente)
                .OrderByDescending(p => p.fecha)
                .Select(p1 => new tabla_anticipos_pendientes
                {
                    codanticipo = p1.codigo,
                    id = p1.id,
                    numeroid = p1.numeroid,
                    docanticipo = p1.id + "-" + p1.numeroid,
                    codcliente = p1.codcliente,
                    codvendedor = p1.codvendedor,
                    codcliente_real = p1.codcliente_real,
                    nit = p1.nit,
                    nomcliente_nit = p1.nomcliente_nit,
                    fecha = p1.fecha,
                    monto = p1.monto,
                    montorest = p1.montorest,
                    codmoneda = p1.codmoneda,
                    anulado = p1.anulado,
                    desc_anulado = p1.anulado ? "Si": "No",
                    para_venta_contado = p1.para_venta_contado,
                    pvc = (p1.para_venta_contado ?? false) ? "Si": "No",
                    fechareg = p1.fechareg,
                    horareg = p1.horareg,
                    usuarioreg = p1.usuarioreg
                }).ToListAsync();

            // Desde 05/12/2023 no validar que el cliente sea sin nombre sino que valide que si los codigos son diferentes entonces que busque ademas por NIT
            if (await cliente.EsClienteSinNombre(_context, codcliente))
            {
                resultado = resultado.Where(i => i.nit == nit).ToList();
            }
            return resultado;
        }


        private async Task Actualizar_Restantes(DBContext _context, string codigoempresa, List<tabla_anticipos_pendientes> tabla_anticipos_pendientes)
        {
            foreach (var reg in tabla_anticipos_pendientes)
            {
                await anticipos_vta_contado.ActualizarMontoRestAnticipo(_context, reg.id, reg.numeroid, 0, reg.codanticipo, 0, codigoempresa);
            }
        }

    }


    


    public class tabla_anticipos_pendientes
    {
        public int codanticipo { get; set; }
        public string id { get; set; }
        public int numeroid { get; set; }

        public string docanticipo { get; set; }
        public string codcliente { get; set; }

        public int codvendedor { get; set; }
        public string codcliente_real { get; set; }
        public string nit { get; set; }
        public string nomcliente_nit { get; set; }
        public DateTime fecha { get; set; }
        public decimal ? monto { get; set; }
        public decimal ? montorest { get; set; }

        public string codmoneda { get; set; }
        public bool anulado { get; set; }
        public string desc_anulado { get; set; }
        public bool ? para_venta_contado { get; set; }

        public string pvc { get; set; }
        public DateTime fechareg { get; set; }
        public string horareg { get; set; }
        public string usuarioreg { get; set; }
    }

}
