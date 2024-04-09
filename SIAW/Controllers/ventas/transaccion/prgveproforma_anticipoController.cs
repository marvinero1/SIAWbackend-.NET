﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using siaw_DBContext.Data;
using siaw_DBContext.Models;
using Microsoft.AspNetCore.Authorization;
using siaw_funciones;

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
                    if (txtcodmoneda_proforma == txtcodmoneda_anticipo)
                    {
                        // No convertir el monto a asignar
                        ttl += txtmonto_asignar;
                        ttl = Math.Round(ttl, 2);
                    }
                    else
                    {
                        // si la moneda del anticipo y proforma no son iguales entonces convertir el monto asignar a la moneda de la proforma
                        double monto_asignar = (double)await tipoCambio._conversion(_context, txtcodmoneda_proforma, txtcodmoneda_anticipo, DateTime.Now, (decimal)txtmonto_asignar);
                        ttl += monto_asignar;
                        ttl = Math.Round(ttl, 2);
                    }

                    if (ttl > txtttl_proforma)
                    {
                        return BadRequest(new { resp = "El monto que desea asignar mas el monto ya asignado supera el total de la proforma" });
                    }
                    return Ok(true);
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



        private async Task<double> refrescar_anticipos_pendientes(DBContext _context, string codmoneda_proforma, List<tabla_veproformaAnticipo> tabla_veproformaAnticipo)
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

    }


    public class tabla_veproformaAnticipo
    {
        public int ? codproforma { get; set; }
        public int ? codanticipo { get; set; }
        public string ? docanticipo { get; set; }

        public string id_anticipo { get; set; }
        public int nroid_anticipo { get; set; }

        public double monto { get; set; }
        public double ? tdc { get; set; }
        public string codmoneda { get; set; }
        public DateTime fechareg { get; set; }
        public string usuarioreg { get; set; }
        public string horareg { get; set; }
        public string codvendedor { get; set; }
    }
}