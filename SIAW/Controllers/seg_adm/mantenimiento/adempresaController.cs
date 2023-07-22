using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIAW.Data;
using SIAW.Models;

namespace SIAW.Controllers.seg_adm.mantenimiento
{
    [Route("api/seg_adm/mant/adempresa/[controller]")]
    [ApiController]
    public class adempresaController : ControllerBase
    {
        private readonly DBContext _context;
        private readonly string connectionString;
        private VerificaConexion verificador;
        private readonly IConfiguration _configuration;
        public adempresaController(IConfiguration configuration)
        {
            connectionString = ConnectionController.ConnectionString;
            _context = DbContextFactory.Create(connectionString);
            _configuration = configuration;
            verificador = new VerificaConexion(_configuration);
        }

        // GET: api/adempresa
        [HttpGet("{conexionName}")]
        public async Task<ActionResult<IEnumerable<adempresa>>> Getadempresa(string conexionName)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.adempresa == null)
                    {
                        return Problem("Entidad adempresa es null.");
                    }
                    var result = await _context.adempresa.OrderByDescending(fechareg => fechareg.Fechareg).ToListAsync();
                    return Ok(result);
                }
                return BadRequest("Se perdio la conexion con el servidor");
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }


        }

        // GET: api/adempresa/5
        [HttpGet("{conexionName}/{codigo}")]
        public async Task<ActionResult<adempresa>> Getadempresa(string conexionName, string codigo)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.adempresa == null)
                    {
                        return Problem("Entidad adempresa es null.");
                    }
                    var adempresa = await _context.adempresa.FindAsync(codigo);

                    if (adempresa == null)
                    {
                        return NotFound("No se encontro un registro con este código");
                    }

                    return Ok(adempresa);
                }
                return BadRequest("Se perdio la conexion con el servidor");
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }

        // PUT: api/adempresa/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{conexionName}/{codigo}")]
        public async Task<IActionResult> Putadempresa(string conexionName, string codigo, adempresa adempresa)
        {
            if (verificador.VerConnection(conexionName, connectionString))
            {
                if (codigo != adempresa.codigo)
                {
                    return BadRequest("Error con Id en datos proporcionados.");
                }

                _context.Entry(adempresa).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!adempresaExists(codigo))
                    {
                        return NotFound("No existe un registro con ese código");
                    }
                    else
                    {
                        throw;
                    }
                }

                return Ok("Datos actualizados correctamente.");
            }
            return BadRequest("Se perdio la conexion con el servidor");


        }

        // POST: api/adempresa
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost("{conexionName}")]
        public async Task<ActionResult<adempresa>> Postadempresa(string conexionName, adempresa adempresa)
        {
            if (verificador.VerConnection(conexionName, connectionString))
            {
                if (_context.adempresa == null)
                {
                    return Problem("Entidad adempresa es null.");
                }
                _context.adempresa.Add(adempresa);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (adempresaExists(adempresa.codigo))
                    {
                        return Conflict("Ya existe un registro con ese código");
                    }
                    else
                    {
                        throw;
                    }
                }

                return Ok("Registrado con Exito :D");

            }
            return BadRequest("Se perdio la conexion con el servidor");
        }

        // DELETE: api/adempresa/5
        [HttpDelete("{conexionName}/{codigo}")]
        public async Task<IActionResult> Deleteadempresa(string conexionName, string codigo)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.adempresa == null)
                    {
                        return Problem("Entidad adempresa es null.");
                    }
                    var adempresa = await _context.adempresa.FindAsync(codigo);
                    if (adempresa == null)
                    {
                        return NotFound("No existe un registro con ese código");
                    }

                    _context.adempresa.Remove(adempresa);
                    await _context.SaveChangesAsync();

                    return Ok("Datos eliminados con exito");
                }
                return BadRequest("Se perdio la conexion con el servidor");

            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }

        private bool adempresaExists(string codigo)
        {
            return (_context.adempresa?.Any(e => e.codigo == codigo)).GetValueOrDefault();

        }


        // GET: api/adempresaGetDetalle
        [HttpGet]
        [Route("adempresaGetDetalle/{conexionName}")]
        public async Task<ActionResult<IEnumerable<adempresa>>> Getadempresa_ListDetalle(string conexionName)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
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
                        return Problem("Revise los datos ingresados.");
                    }
                    return Ok(query);
                }
                return BadRequest("Se perdio la conexion con el servidor");
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
                throw;
            }
        }


        // GET: api/adempresaGetDetalle/1
        [HttpGet]
        [Route("adempresaGetDetalle/{conexionName}/{codigo}")]
        public async Task<ActionResult<IEnumerable<adempresa>>> Getadempresa_Detalle(string conexionName, string codigo)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
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
                        return Problem("Revise los datos ingresados.");
                    }
                    return Ok(query);
                }
                return BadRequest("Se perdio la conexion con el servidor");
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
                throw;
            }
        }


    }
}
