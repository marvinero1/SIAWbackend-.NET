﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using siaw_DBContext.Data;
using siaw_DBContext.Models;
using Microsoft.AspNetCore.Authorization;

namespace SIAW.Controllers.fondos.mantenimiento
{
    [Route("api/fondos/mant/[controller]")]
    [ApiController]
    public class fndeudor_compuestoController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        public fndeudor_compuestoController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }

        // GET: api/fndeudor_compuesto
        [HttpGet("{userConn}")]
        public async Task<ActionResult<IEnumerable<fndeudor_compuesto>>> Getfndeudor_compuesto(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.fndeudor_compuesto == null)
                    {
                        return BadRequest(new { resp = "Entidad fndeudor_compuesto es null." });
                    }
                    var result = await _context.fndeudor_compuesto.OrderBy(id => id.id).ToListAsync();
                    return Ok(result);
                }
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }

        // GET: api/fndeudor_compuesto/5
        [HttpGet("{userConn}/{id}")]
        public async Task<ActionResult<fndeudor_compuesto>> Getfndeudor_compuesto(string userConn, string id)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.fndeudor_compuesto == null)
                    {
                        return BadRequest(new { resp = "Entidad fndeudor_compuesto es null." });
                    }
                    var fndeudor_compuesto = await _context.fndeudor_compuesto.FindAsync(id);

                    if (fndeudor_compuesto == null)
                    {
                        return NotFound(new { resp = "No se encontro un registro con este código" });
                    }

                    return Ok(fndeudor_compuesto);
                }

            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }


        // PUT: api/fndeudor_compuesto/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPut("{userConn}/{id}")]
        public async Task<IActionResult> Putfndeudor_compuesto(string userConn, string id, fndeudor_compuesto fndeudor_compuesto)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (id != fndeudor_compuesto.id)
                {
                    return BadRequest(new { resp = "Error con Id en datos proporcionados." });
                }

                _context.Entry(fndeudor_compuesto).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!fndeudor_compuestoExists(id, _context))
                    {
                        return NotFound(new { resp = "No existe un registro con ese código" });
                    }
                    else
                    {
                        return Problem("Error en el servidor");
                        throw;
                    }
                }

                return Ok(new { resp = "206" });   // actualizado con exito
            }



        }

        // POST: api/fndeudor_compuesto
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPost("{userConn}")]
        public async Task<ActionResult<fndeudor_compuesto>> Postfndeudor_compuesto(string userConn, fndeudor_compuesto fndeudor_compuesto)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (_context.fndeudor_compuesto == null)
                {
                    return BadRequest(new { resp = "Entidad fndeudor_compuesto es null." });
                }
                _context.fndeudor_compuesto.Add(fndeudor_compuesto);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (fndeudor_compuestoExists(fndeudor_compuesto.id, _context))
                    {
                        return Conflict(new { resp = "Ya existe un registro con ese código" });
                    }
                    else
                    {
                        return Problem("Error en el servidor");
                        throw;
                    }
                }

                return Ok(new { resp = "204" });   // creado con exito

            }

        }

        // DELETE: api/fndeudor_compuesto/5
        [Authorize]
        [HttpDelete("{userConn}/{id}")]
        public async Task<IActionResult> Deletefndeudor_compuesto(string userConn, string id)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.fndeudor_compuesto == null)
                    {
                        return BadRequest(new { resp = "Entidad fndeudor_compuesto es null." });
                    }
                    var fndeudor_compuesto = await _context.fndeudor_compuesto.FindAsync(id);
                    if (fndeudor_compuesto == null)
                    {
                        return NotFound(new { resp = "No existe un registro con ese código" });
                    }

                    _context.fndeudor_compuesto.Remove(fndeudor_compuesto);
                    await _context.SaveChangesAsync();

                    return Ok(new { resp = "208" });   // eliminado con exito
                }


            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }

        private bool fndeudor_compuestoExists(string id, DBContext _context)
        {
            return (_context.fndeudor_compuesto?.Any(e => e.id == id)).GetValueOrDefault();

        }
    }
}