using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIAW.Data;
using SIAW.Models;

namespace SIAW.Controllers.seg_adm.logs
{
    [Route("api/seg_adm/logs/[controller]")]
    [ApiController]
    public class selogController : ControllerBase
    {
        private readonly DBContext _context;
        private readonly string connectionString;
        private VerificaConexion verificador;
        private readonly IConfiguration _configuration;
        public selogController(IConfiguration configuration)
        {
            connectionString = ConnectionController.ConnectionString;
            _context = DbContextFactory.Create(connectionString);
            _configuration = configuration;
            verificador = new VerificaConexion(_configuration);
        }

        // GET: api/selog
        [HttpGet]
        [Route("getselogfecha/{conexionName}/{fecha}")]
        public async Task<ActionResult<IEnumerable<selog>>> Getselog(string conexionName, DateTime fecha)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.selog == null)
                    {
                        return Problem("Entidad selog es null.");
                    }
                    var result = await _context.selog.Where(x => x.fecha == fecha).OrderByDescending(f => f.hora).ToListAsync();
                    return Ok(result);
                }
                return BadRequest("Se perdio la conexion con el servidor");
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }


        }

       
        // POST: api/selog
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost("{conexionName}")]
        public async Task<ActionResult<selog>> Postselog(string conexionName, selog selog)
        {
            if (verificador.VerConnection(conexionName, connectionString))
            {
                if (_context.selog == null)
                {
                    return Problem("Entidad selog es null.");
                }
                _context.selog.Add(selog);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    return BadRequest("Error en el Servidor");
                }

                return Ok("Registrado con Exito :D");

            }
            return BadRequest("Se perdio la conexion con el servidor");
        }
    }
}
