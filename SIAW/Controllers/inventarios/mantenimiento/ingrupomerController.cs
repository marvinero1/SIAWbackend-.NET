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
    public class ingrupomerController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        public ingrupomerController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }

        // GET: api/ingrupomer
        [HttpGet("{userConn}")]
        public async Task<ActionResult<IEnumerable<ingrupomer>>> Getingrupomer(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.ingrupomer == null)
                    {
                        return BadRequest(new { resp = "Entidad ingrupomer es null." });
                    }
                    var result = await _context.ingrupomer.OrderBy(codigo => codigo.codigo).ToListAsync();
                    return Ok(result);
                }
                
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }


        }

        // GET: api/ingrupomer/5
        [HttpGet("{userConn}/{codigo}")]
        public async Task<ActionResult<ingrupomer>> Getingrupomer(string userConn, int codigo)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.ingrupomer == null)
                    {
                        return BadRequest(new { resp = "Entidad ingrupomer es null." });
                    }
                    var ingrupomer = await _context.ingrupomer.FindAsync(codigo);

                    if (ingrupomer == null)
                    {
                        return NotFound( new { resp = "No se encontro un registro con este código" });
                    }

                    return Ok(ingrupomer);
                }
                
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }

        // PUT: api/ingrupomer/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPut("{userConn}/{codigo}")]
        public async Task<IActionResult> Putingrupomer(string userConn, int codigo, ingrupomer ingrupomer)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (codigo != ingrupomer.codigo)
                {
                    return BadRequest( new { resp = "Error con Id en datos proporcionados." });
                }

                _context.Entry(ingrupomer).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ingrupomerExists(codigo, _context))
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

        // POST: api/ingrupomer
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPost("{userConn}")]
        public async Task<ActionResult<ingrupomer>> Postingrupomer(string userConn, ingrupomer ingrupomer)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (_context.ingrupomer == null)
                {
                    return BadRequest(new { resp = "Entidad ingrupomer es null." });
                }
                _context.ingrupomer.Add(ingrupomer);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (ingrupomerExists(ingrupomer.codigo, _context))
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

        // DELETE: api/ingrupomer/5
        [Authorize]
        [HttpDelete("{userConn}/{codigo}")]
        public async Task<IActionResult> Deleteingrupomer(string userConn, int codigo)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.ingrupomer == null)
                    {
                        return BadRequest(new { resp = "Entidad ingrupomer es null." });
                    }
                    var ingrupomer = await _context.ingrupomer.FindAsync(codigo);
                    if (ingrupomer == null)
                    {
                        return NotFound( new { resp = "No existe un registro con ese código" });
                    }

                    _context.ingrupomer.Remove(ingrupomer);
                    await _context.SaveChangesAsync();

                    return Ok( new { resp = "208" });   // eliminado con exito
                }
                

            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }

        private bool ingrupomerExists(int codigo, DBContext _context)
        {
            return (_context.ingrupomer?.Any(e => e.codigo == codigo)).GetValueOrDefault();

        }
    }
}
