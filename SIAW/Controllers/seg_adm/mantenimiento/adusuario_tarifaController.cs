using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using siaw_DBContext.Data;
using siaw_DBContext.Models;
using Microsoft.AspNetCore.Authorization;
using siaw_funciones;

namespace SIAW.Controllers.seg_adm.mantenimiento
{
    [Route("api/seg_adm/mant/[controller]")]
    [ApiController]
    public class adusuario_tarifaController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        public adusuario_tarifaController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }

        // GET: api/adusuario_tarifa
        [HttpGet("{userConn}")]
        public async Task<ActionResult<IEnumerable<adusuario_tarifa>>> Getadusuario_tarifa(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                //var _context = _userConnectionManager.GetUserConnection(userId);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.adusuario_tarifa == null)
                    {
                        return Problem("Entidad adusuario_tarifa es null.");
                    }
                    var result = await _context.adusuario_tarifa
                        .OrderBy(x => x.usuario)
                        .ThenBy(x => x.codtarifa)
                        .ToListAsync();
                    return Ok(result);
                }

            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }



        // POST: api/adusuario_tarifa
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPost("{userConn}")]
        public async Task<ActionResult<adusuario_tarifa>> Postadusuario_tarifa(string userConn, adusuario_tarifa adusuario_tarifa)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            string id_asignado = await if_id_usua_asignado(userConnectionString, adusuario_tarifa.usuario, (int)adusuario_tarifa.codtarifa);
            if (id_asignado != "aceptado")
            {
                return Ok(new { codigo = 710, resul = id_asignado });
            }


            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (_context.adusuario_tarifa == null)
                {
                    return Problem("Entidad adusuario_tarifa es null.");
                }

                _context.adusuario_tarifa.Add(adusuario_tarifa);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (adusuario_tarifaExists(adusuario_tarifa.id, _context))
                    {
                        return Conflict("Ya existe un registro con ese código");
                    }
                    else
                    {
                        return BadRequest("Error en el servidor");
                        throw;
                    }

                }

                return Ok("204");   // creado con exito

            }

        }

        private bool adusuario_tarifaExists(int codigo, DBContext _context)
        {
            return (_context.adusuario_tarifa?.Any(e => e.id == codigo)).GetValueOrDefault();

        }


        // DELETE: api/adusuario_tarifa/5
        [Authorize]
        [HttpDelete("{userConn}/{id}")]
        public async Task<IActionResult> Deleteadusuario_tarifa(string userConn, int id)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.adusuario_tarifa == null)
                    {
                        return Problem("Entidad adusuario_tarifa es null.");
                    }
                    var adusuario_tarifa = await _context.adusuario_tarifa.FindAsync(id);
                    if (adusuario_tarifa == null)
                    {
                        return NotFound("No existe un registro con ese código");
                    }

                    _context.adusuario_tarifa.Remove(adusuario_tarifa);
                    await _context.SaveChangesAsync();

                    return Ok("208");   // eliminado con exito
                }
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }






        private async Task<string> if_id_usua_asignado(string userConnectionString, string usuario, int codtarifa)
        {
            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                var adusuario_tarifa = await _context.adusuario_tarifa
                    .Where(item => item.usuario == usuario && item.codtarifa == codtarifa)
                    .Select(item => new
                    {
                        usuario = item.usuario,
                        codtarifa = item.codtarifa
                    })
                    .FirstOrDefaultAsync();
                if (adusuario_tarifa == null)
                {
                    return "aceptado";
                }
                return "El usuario: " + adusuario_tarifa.usuario + " ya tiene asignado la tarifa: " + adusuario_tarifa.codtarifa;
            }
        }

    }
}
