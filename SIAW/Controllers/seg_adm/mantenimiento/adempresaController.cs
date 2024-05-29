using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using siaw_DBContext.Data;
using siaw_DBContext.Models;
using Microsoft.AspNetCore.Authorization;

namespace SIAW.Controllers.seg_adm.mantenimiento
{
    [Route("api/seg_adm/mant/[controller]")]
    [ApiController]
    public class adempresaController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        public adempresaController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }

        // GET: api/adempresa
        [HttpGet("{userConn}")]
        public async Task<ActionResult<IEnumerable<adempresa>>> Getadempresa(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.adempresa == null)
                    {
                        return BadRequest(new { resp = "Entidad adempresa es null." });
                    }
                    var result = await _context.adempresa.OrderByDescending(fechareg => fechareg.Fechareg).ToListAsync();
                    return Ok(result);
                }
                
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }


        }

        // GET: api/adempresa/5
        [HttpGet("{userConn}/{codigo}")]
        public async Task<ActionResult<adempresa>> Getadempresa(string userConn, string codigo)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.adempresa == null)
                    {
                        return BadRequest(new { resp = "Entidad adempresa es null." });
                    }
                    var adempresa = await _context.adempresa.FindAsync(codigo);

                    if (adempresa == null)
                    {
                        return NotFound( new { resp = "No se encontro un registro con este código" });
                    }

                    return Ok(adempresa);
                }
                
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }


        // GET: api/adempresa/5
        [HttpGet]
        [Route("getNomEmpresa/{userConn}/{codigo}")]
        public async Task<ActionResult<adempresa>> getNomEmpresa(string userConn, string codigo)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var adempresa = await _context.adempresa
                        .Where(i => i.codigo == codigo)
                        .Select(i => new
                        {
                            nombre = i.descripcion
                        })
                        .FirstOrDefaultAsync();

                    if (adempresa == null)
                    {
                        return NotFound( new { resp = "No se encontro un registro con este código" });
                    }

                    return Ok(adempresa);
                }
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }


        // GET: api/adempresa/5
        [HttpGet]
        [Route("getcodMon/{userConn}/{codEmpresa}")]
        public async Task<ActionResult<adempresa>> getcodMon(string userConn, string codEmpresa)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.adempresa == null)
                    {
                        return BadRequest(new { resp = "Entidad adempresa es null." });
                    }
                    var codMoneda = await _context.adempresa.Where(i => i.codigo == codEmpresa).Select(i => i.moneda).FirstOrDefaultAsync();

                    if (codMoneda == null)
                    {
                        return NotFound(new { resp = "No se encontro un registro con este código" });
                    }

                    return Ok(new {moneda = codMoneda });
                }

            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }


        // GET: api/adparametros
        [HttpGet]
        [Route("getFirstEmpresa/{userConn}")]
        public async Task<ActionResult<IEnumerable<adparametros>>> getFirstEmpresa(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var result = await _context.adempresa.Select(i => i.codigo).FirstOrDefaultAsync() ?? "";
                    if (result == "")
                    {
                        return BadRequest(new { resp = "No se encontraron datos." });
                    }
                    return Ok(new
                    {
                        empresa = result
                    });
                }

            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }


        // PUT: api/adempresa/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPut("{userConn}/{codigo}")]
        public async Task<IActionResult> Putadempresa(string userConn, string codigo, adempresa adempresa)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (codigo != adempresa.codigo)
                {
                    return BadRequest( new { resp = "Error con Id en datos proporcionados." });
                }

                _context.Entry(adempresa).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!adempresaExists(codigo, _context))
                    {
                        return NotFound( new { resp = "No existe un registro con ese código" });
                    }
                    else
                    {
                        return Problem("Error en el servidor");
                        throw;
                    }
                }

                return Ok( new { resp = "206" });   // actualizado con exito
            }
        }

        // POST: api/adempresa
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPost("{userConn}")]
        public async Task<ActionResult<adempresa>> Postadempresa(string userConn, adempresa adempresa)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (_context.adempresa == null)
                {
                    return BadRequest(new { resp = "Entidad adempresa es null." });
                }
                _context.adempresa.Add(adempresa);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (adempresaExists(adempresa.codigo, _context))
                    {
                        return Conflict( new { resp = "Ya existe un registro con ese código" });
                    }
                    else
                    {
                        return Problem("Error en el servidor");
                        throw;
                    }
                }

                return Ok( new { resp = "204" });   // creado con exito

            }
            
        }

        // DELETE: api/adempresa/5
        [Authorize]
        [HttpDelete("{userConn}/{codigo}")]
        public async Task<IActionResult> Deleteadempresa(string userConn, string codigo)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.adempresa == null)
                    {
                        return BadRequest(new { resp = "Entidad adempresa es null." });
                    }
                    var adempresa = await _context.adempresa.FindAsync(codigo);
                    if (adempresa == null)
                    {
                        return NotFound( new { resp = "No existe un registro con ese código" });
                    }

                    _context.adempresa.Remove(adempresa);
                    await _context.SaveChangesAsync();

                    return Ok( new { resp = "208" });   // eliminado con exito
                }
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }

        private bool adempresaExists(string codigo, DBContext _context)
        {
            return (_context.adempresa?.Any(e => e.codigo == codigo)).GetValueOrDefault();

        }


        // GET: api/adempresaGetDetalle
        [HttpGet]
        [Route("adempresaGetDetalle/{userConn}")]
        public async Task<ActionResult<IEnumerable<adempresa>>> Getadempresa_ListDetalle(string userConn)
        {
            try
            {
                // Obtene   r el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    List<adempresa> ladempresa = _context.adempresa.ToList();
                    List<admoneda> ladmonena = _context.admoneda.ToList();
                    List<inalmacen> linalmacen = _context.inalmacen.ToList();

                    var query = from em in ladempresa
                                join mon in ladmonena
                                on em.moneda equals mon.codigo into table1
                                from mon in table1.DefaultIfEmpty()
                                join alm in linalmacen
                                on em.codalmacen equals alm.codigo into table2
                                from alm in table2.DefaultIfEmpty()
                                select new
                                {
                                    codigo = em.codigo,
                                    descripcion = em.descripcion,
                                    nit = em.nit,
                                    fingestion = em.fingestion,
                                    iniciogestion = em.iniciogestion,
                                    codMoneda = em.moneda,
                                    moneda = mon.descripcion,
                                    monedapol = em.monedapol,
                                    plancuenta = em.plancuenta,
                                    direccion = em.direccion,
                                    Horareg = em.Horareg,
                                    Fechareg = em.Fechareg,
                                    Usuarioreg = em.Usuarioreg,
                                    actividad = em.actividad,
                                    codalmacen = em.codalmacen,
                                    descrAlmacen = alm.descripcion,
                                    municipio = em.municipio
                                };

                    if (query.Count() == 0)
                    {
                        return NotFound(new { resp = "No se encontraron registros." });
                    }
                    return Ok(query);
                }
                
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
                throw;
            }
        }


        // GET: api/adempresaGetDetalle/1
        [HttpGet]
        [Route("adempresaGetDetalle/{userConn}/{codigo}")]
        public async Task<ActionResult<IEnumerable<adempresa>>> Getadempresa_Detalle(string userConn, string codigo)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    List<adempresa> ladempresa = _context.adempresa.ToList();
                    List<admoneda> ladmonena = _context.admoneda.ToList();
                    List<inalmacen> linalmacen = _context.inalmacen.ToList();

                    var query = from em in ladempresa
                                join mon in ladmonena
                                on em.moneda equals mon.codigo into table1
                                from mon in table1.DefaultIfEmpty()
                                join alm in linalmacen
                                on em.codalmacen equals alm.codigo into table2
                                from alm in table2.DefaultIfEmpty()
                                where em.codigo == codigo
                                select new
                                {
                                    codigo = em.codigo,
                                    descripcion = em.descripcion,
                                    nit = em.nit,
                                    fingestion = em.fingestion,
                                    iniciogestion = em.iniciogestion,
                                    codMoneda = em.moneda,
                                    moneda = mon.descripcion,
                                    monedapol = em.monedapol,
                                    plancuenta = em.plancuenta,
                                    direccion = em.direccion,
                                    Horareg = em.Horareg,
                                    Fechareg = em.Fechareg,
                                    Usuarioreg = em.Usuarioreg,
                                    actividad = em.actividad,
                                    codalmacen = em.codalmacen,
                                    descrAlmacen = alm.descripcion,
                                    municipio = em.municipio
                                };

                    if (query == null)
                    {
                        return NotFound(new { resp = "No existe un registro con esa información" });
                    }
                    return Ok(query);
                }
                
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
                throw;
            }
        }


    }
}
