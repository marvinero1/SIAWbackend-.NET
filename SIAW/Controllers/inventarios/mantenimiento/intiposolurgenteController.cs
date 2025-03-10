﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using siaw_DBContext.Data;
using siaw_DBContext.Models;
using Microsoft.AspNetCore.Authorization;

namespace SIAW.Controllers.inventarios.mantenimiento
{
    [Route("api/inventario/mant/[controller]")]
    [ApiController]
    public class intiposolurgenteController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        public intiposolurgenteController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }

        // GET: api/intiposolurgente
        [HttpGet("{userConn}")]
        public async Task<ActionResult<IEnumerable<intiposolurgente>>> Getintiposolurgente(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.intiposolurgente == null)
                    {
                        return BadRequest(new { resp = "Entidad intiposolurgente es null." });
                    }
                    var result = await _context.intiposolurgente.OrderByDescending(fechareg => fechareg.fechareg).ToListAsync();
                    return Ok(result);
                }
                
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }


        }

        // GET: api/intiposolurgente/5
        [HttpGet("{userConn}/{id}")]
        public async Task<ActionResult<intiposolurgente>> Getintiposolurgente(string userConn, string id)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.intiposolurgente == null)
                    {
                        return BadRequest(new { resp = "Entidad intiposolurgente es null." });
                    }
                    var intiposolurgente = await _context.intiposolurgente.FindAsync(id);

                    if (intiposolurgente == null)
                    {
                        return NotFound( new { resp = "No se encontro un registro con este código" });
                    }

                    return Ok(intiposolurgente);
                }
                
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }

        // GET: api/catalogo
        [HttpGet]
        [Route("catalogo/{userConn}")]
        public async Task<ActionResult<IEnumerable<intiposolurgente>>> Getinconcepto_catalogo(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var result = await _context.intiposolurgente
                    .OrderBy(i => i.id)
                    .Select(i => new
                    {
                        i.id,
                        i.descripcion,
                        i.nroactual
                    }).ToListAsync();


                    if (result.Count() == 0)
                    {
                        return BadRequest(new { resp = "Entidad intiposolurgente es null." });
                    }
                    return Ok(result);
                }

            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
                throw;
            }
        }

        // PUT: api/intiposolurgente/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPut("{userConn}/{id}")]
        public async Task<IActionResult> Putintiposolurgente(string userConn, string id, intiposolurgente intiposolurgente)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (id != intiposolurgente.id)
                {
                    return BadRequest( new { resp = "Error con Id en datos proporcionados." });
                }

                _context.Entry(intiposolurgente).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!intiposolurgenteExists(id, _context))
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

        // POST: api/intiposolurgente
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPost("{userConn}")]
        public async Task<ActionResult<intiposolurgente>> Postintiposolurgente(string userConn, intiposolurgente intiposolurgente)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (_context.intiposolurgente == null)
                {
                    return BadRequest(new { resp = "Entidad intiposolurgente es null." });
                }
                _context.intiposolurgente.Add(intiposolurgente);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (intiposolurgenteExists(intiposolurgente.id, _context))
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

        // DELETE: api/intiposolurgente/5
        [Authorize]
        [HttpDelete("{userConn}/{id}")]
        public async Task<IActionResult> Deleteintiposolurgente(string userConn, string id)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.intiposolurgente == null)
                    {
                        return BadRequest(new { resp = "Entidad intiposolurgente es null." });
                    }
                    var intiposolurgente = await _context.intiposolurgente.FindAsync(id);
                    if (intiposolurgente == null)
                    {
                        return NotFound( new { resp = "No existe un registro con ese código" });
                    }

                    _context.intiposolurgente.Remove(intiposolurgente);
                    await _context.SaveChangesAsync();

                    return Ok( new { resp = "208" });   // eliminado con exito
                }
                

            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }

        private bool intiposolurgenteExists(string id, DBContext _context)
        {
            return (_context.intiposolurgente?.Any(e => e.id == id)).GetValueOrDefault();

        }
    }
}
