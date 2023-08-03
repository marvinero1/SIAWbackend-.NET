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
        private readonly UserConnectionManager _userConnectionManager;
        public selogController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }

        // GET: api/selog
        [HttpGet]
        [Route("getselogfecha/{userConn}/{fecha}")]
        public async Task<ActionResult<IEnumerable<selog>>> Getselog(string userConn, DateTime fecha)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                //var _context = _userConnectionManager.GetUserConnection(userId);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.selog == null)
                    {
                        return Problem("Entidad selog es null.");
                    }
                    var result = await _context.selog.Where(x => x.fecha == fecha).OrderByDescending(f => f.hora).ToListAsync();
                    return Ok(result);
                }
                
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }


        }

       
        // POST: api/selog
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost("{userConn}")]
        public async Task<ActionResult<selog>> Postselog(string userConn, selog selog)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            //var _context = _userConnectionManager.GetUserConnection(userId);

            using (var _context = DbContextFactory.Create(userConnectionString))
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
            
        }
    }
}
