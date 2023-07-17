using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIAW.Data;
using SIAW.Models;

namespace SIAW.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class adareaController : ControllerBase
    {
        private readonly DBContext _context;
        private readonly string connectionString;
        private VerificaConexion objeto;
        private readonly IConfiguration _configuration;
        public adareaController(IConfiguration configuration)
        {
            connectionString = ConnectionController.ConnectionString;
            _context = DbContextFactory.Create(connectionString);
            _configuration = configuration;
            objeto = new VerificaConexion(_configuration);
        }

        // GET: api/adarea
        [HttpGet("/adareaGetAll/{conexionName}")]
        public async Task<ActionResult<IEnumerable<adarea>>> Getadarea(string conexionName)
        {
            try
            {
                if (objeto.VerConnection(conexionName, connectionString))
                {
                    if (_context.adarea == null)
                    {
                        return NotFound();
                    }
                    return await _context.adarea.ToListAsync();
                }
                return BadRequest("Se perdio la conexion con el servidor");
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
            

        }

        // GET: api/adarea/5
        [HttpGet("{id}")]
        public async Task<ActionResult<adarea>> Getadarea(int id)
        {
            try
            {
                if (_context.adarea == null)
                {
                    return NotFound();
                }
                var adarea = await _context.adarea.FindAsync(id);

                if (adarea == null)
                {
                    return NotFound();
                }

                return adarea;
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }

        // PUT: api/adarea/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> Putadarea(int id, adarea adarea)
        {
            if (id != adarea.codigo)
            {
                return BadRequest();
            }

            _context.Entry(adarea).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!adareaExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();

        }

        // POST: api/adarea
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<adarea>> Postadarea(adarea adarea)
        {
            if (_context.adarea == null)
            {
                return Problem("Entity set 'PSContext.adarea'  is null.");
            }
            _context.adarea.Add(adarea);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                if (adareaExists(adarea.codigo))
                {
                    return Conflict();
                }
                else
                {
                    throw;
                }
            }

            return CreatedAtAction("Getadarea", new { id = adarea.codigo }, adarea);

        }

        // DELETE: api/adarea/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Deleteadarea(int id)
        {
            try
            {
                if (_context.adarea == null)
                {
                    return NotFound();
                }
                var adarea = await _context.adarea.FindAsync(id);
                if (adarea == null)
                {
                    return NotFound();
                }

                _context.adarea.Remove(adarea);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }

        private bool adareaExists(int id)
        {
            return (_context.adarea?.Any(e => e.codigo == id)).GetValueOrDefault();

        }
    }
}
