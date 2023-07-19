using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIAW.Data;
using SIAW.Models;

namespace SIAW.Controllers.inventarios.mantenimiento
{
    [Route("api/inventario/mant/inlinea/[controller]")]
    [ApiController]
    public class inlineaController : ControllerBase
    {
        private readonly DBContext _context;
        private readonly string connectionString;
        private VerificaConexion verificador;
        private readonly IConfiguration _configuration;
        public inlineaController(IConfiguration configuration)
        {
            connectionString = ConnectionController.ConnectionString;
            _context = DbContextFactory.Create(connectionString);
            _configuration = configuration;
            verificador = new VerificaConexion(_configuration);
        }

        // GET: api/inlinea
        [HttpGet("{conexionName}")]
        public async Task<ActionResult<IEnumerable<inlinea>>> Getinlinea(string conexionName)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.inlinea == null)
                    {
                        return Problem("Entidad inlinea es null.");
                    }
                    var result = await _context.inlinea.OrderBy(codigo => codigo.codigo).ToListAsync();
                    return Ok(result);
                }
                return BadRequest("Se perdio la conexion con el servidor");
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }


        }

        // GET: api/inlinea/5
        [HttpGet("{conexionName}/{codigo}")]
        public async Task<ActionResult<inlinea>> Getinlinea(string conexionName, string codigo)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.inlinea == null)
                    {
                        return Problem("Entidad inlinea es null.");
                    }
                    var inlinea = await _context.inlinea.FindAsync(codigo);

                    if (inlinea == null)
                    {
                        return NotFound("No se encontro un registro con este código");
                    }

                    return Ok(inlinea);
                }
                return BadRequest("Se perdio la conexion con el servidor");
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }

        // PUT: api/inlinea/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{conexionName}/{codigo}")]
        public async Task<IActionResult> Putinlinea(string conexionName, string codigo, inlinea inlinea)
        {
            if (verificador.VerConnection(conexionName, connectionString))
            {
                if (codigo != inlinea.codigo)
                {
                    return BadRequest("Error con Id en datos proporcionados.");
                }

                _context.Entry(inlinea).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!inlineaExists(codigo))
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

        // POST: api/inlinea
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost("{conexionName}")]
        public async Task<ActionResult<inlinea>> Postinlinea(string conexionName, inlinea inlinea)
        {
            if (verificador.VerConnection(conexionName, connectionString))
            {
                if (_context.inlinea == null)
                {
                    return Problem("Entidad inlinea es null.");
                }
                _context.inlinea.Add(inlinea);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (inlineaExists(inlinea.codigo))
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

        // DELETE: api/inlinea/5
        [HttpDelete("{conexionName}/{codigo}")]
        public async Task<IActionResult> Deleteinlinea(string conexionName, string codigo)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.inlinea == null)
                    {
                        return Problem("Entidad inlinea es null.");
                    }
                    var inlinea = await _context.inlinea.FindAsync(codigo);
                    if (inlinea == null)
                    {
                        return NotFound("No existe un registro con ese código");
                    }

                    _context.inlinea.Remove(inlinea);
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

        private bool inlineaExists(string codigo)
        {
            return (_context.inlinea?.Any(e => e.codigo == codigo)).GetValueOrDefault();

        }
    }








    [Route("api/inventario/mant/insubgrupoVta/[controller]")]
    [ApiController]
    public class insubgrupo_vtaController : ControllerBase
    {
        private readonly DBContext _context;
        private readonly string connectionString;
        private VerificaConexion verificador;
        private readonly IConfiguration _configuration;
        public insubgrupo_vtaController(IConfiguration configuration)
        {
            connectionString = ConnectionController.ConnectionString;
            _context = DbContextFactory.Create(connectionString);
            _configuration = configuration;
            verificador = new VerificaConexion(_configuration);
        }

        // GET: api/insubgrupo_vta
        [HttpGet("{conexionName}")]
        public async Task<ActionResult<IEnumerable<insubgrupo_vta>>> Getinsubgrupo_vta(string conexionName)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.insubgrupo_vta == null)
                    {
                        return Problem("Entidad insubgrupo_vta es null.");
                    }
                    var result = await _context.insubgrupo_vta.OrderBy(codigo => codigo.codigo).ToListAsync();
                    return Ok(result);
                }
                return BadRequest("Se perdio la conexion con el servidor");
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }


        }

        // GET: api/insubgrupo_vta/5
        [HttpGet("{conexionName}/{codigo}")]
        public async Task<ActionResult<insubgrupo_vta>> Getinsubgrupo_vta(string conexionName, string codigo)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.insubgrupo_vta == null)
                    {
                        return Problem("Entidad insubgrupo_vta es null.");
                    }
                    var insubgrupo_vta = await _context.insubgrupo_vta.FindAsync(codigo);

                    if (insubgrupo_vta == null)
                    {
                        return NotFound("No se encontro un registro con este código");
                    }

                    return Ok(insubgrupo_vta);
                }
                return BadRequest("Se perdio la conexion con el servidor");
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }

        // PUT: api/insubgrupo_vta/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{conexionName}/{codigo}")]
        public async Task<IActionResult> Putinsubgrupo_vta(string conexionName, string codigo, insubgrupo_vta insubgrupo_vta)
        {
            if (verificador.VerConnection(conexionName, connectionString))
            {
                if (codigo != insubgrupo_vta.codigo)
                {
                    return BadRequest("Error con Id en datos proporcionados.");
                }

                _context.Entry(insubgrupo_vta).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!insubgrupo_vtaExists(codigo))
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

        // POST: api/insubgrupo_vta
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost("{conexionName}")]
        public async Task<ActionResult<insubgrupo_vta>> Postinsubgrupo_vta(string conexionName, insubgrupo_vta insubgrupo_vta)
        {
            if (verificador.VerConnection(conexionName, connectionString))
            {
                if (_context.insubgrupo_vta == null)
                {
                    return Problem("Entidad insubgrupo_vta es null.");
                }
                _context.insubgrupo_vta.Add(insubgrupo_vta);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (insubgrupo_vtaExists(insubgrupo_vta.codigo))
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

        // DELETE: api/insubgrupo_vta/5
        [HttpDelete("{conexionName}/{codigo}")]
        public async Task<IActionResult> Deleteinsubgrupo_vta(string conexionName, string codigo)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.insubgrupo_vta == null)
                    {
                        return Problem("Entidad insubgrupo_vta es null.");
                    }
                    var insubgrupo_vta = await _context.insubgrupo_vta.FindAsync(codigo);
                    if (insubgrupo_vta == null)
                    {
                        return NotFound("No existe un registro con ese código");
                    }

                    _context.insubgrupo_vta.Remove(insubgrupo_vta);
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

        private bool insubgrupo_vtaExists(string codigo)
        {
            return (_context.insubgrupo_vta?.Any(e => e.codigo == codigo)).GetValueOrDefault();

        }

    }


}
