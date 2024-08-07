﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using siaw_DBContext.Data;
using siaw_DBContext.Models;
using System.Net;
using Microsoft.AspNetCore.Authorization;

namespace SIAW.Controllers.inventarios.mantenimiento
{
    [Route("api/inventario/mant/[controller]")]
    [ApiController]
    public class inkitController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        public inkitController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }

        // GET: api/inkit
        [HttpGet("{userConn}")]
        public async Task<ActionResult<IEnumerable<inkit>>> Getinkit(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.inkit == null)
                    {
                        return BadRequest(new { resp = "Entidad inkit es null." });
                    }
                    var result = await _context.inkit.OrderBy(codigo => codigo.codigo).ToListAsync();
                    return Ok(result);
                }
                
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }


        }

        // GET: api/inkit/5
        [HttpGet("{userConn}/{codigo}")]
        public async Task<ActionResult<inkit>> Getinkit(string userConn, string codigo, string item)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.inkit == null)
                    {
                        return BadRequest(new { resp = "Entidad inkit es null." });
                    }
                    var inkit = _context.inkit.FirstOrDefault(objeto => objeto.codigo == codigo && objeto.item == item);

                    if (inkit == null)
                    {
                        return NotFound( new { resp = "No se encontro un registro con este código" });
                    }

                    return Ok(inkit);
                }
                
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }

        /// <summary>
        /// Obtiene todos los registros de la tabla inkit (item) con initem, dependiendo del codigo de item
        /// </summary>
        /// <param name="userConn"></param>
        /// <param name="coditem"></param>
        /// <returns></returns>
        // GET: api/initem_inkit
        [HttpGet]
        [Route("initem_inkit/{userConn}/{coditem}")]
        public async Task<ActionResult<IEnumerable<inkit>>> Getinitem_inkit(string userConn, string coditem)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var query = from k in _context.inkit
                                join i in _context.initem on k.item equals i.codigo
                                where k.codigo == coditem
                                orderby k.item
                                select new
                                {
                                    k.codigo,
                                    k.item,
                                    i.descripcion,
                                    i.medida,
                                    k.cantidad,
                                    k.unidad
                                };

                    var result = query.ToList();

                    if (result.Count() == 0)
                    {
                        return BadRequest( new { resp = "No se encontraron registros con esos datos." });
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


        // PUT: api/inkit/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPut("{userConn}/{codigo}/{item}")]
        public async Task<IActionResult> Putinkit(string userConn, string codigo, string item, inkit inkit)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                var kit = _context.inkit.FirstOrDefault(objeto => objeto.codigo == codigo && objeto.item == item);
                if (kit == null)
                {
                    return NotFound( new { resp = "No existe un registro con esa información" });
                }

                _context.Entry(inkit).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!inkitExists(codigo, item, _context))
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


        // PUT: api/inkit/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPut]
        [Route("updateListinkit/{userConn}/{codigo}")]
        public async Task<IActionResult> PutListinkit(string userConn, string codigo, List<inkit> inkit)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            var objetoDiferente = inkit.FirstOrDefault(inkit => inkit.codigo != codigo);
            if (objetoDiferente != null)
            {
                return BadRequest(new { resp = "Se encontró un componente con el código: " + objetoDiferente.item + ", que no pertenece al conjunto: " + codigo });

            }
            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                using (var dbContexTransaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        var kits = await _context.inkit
                        .Where(i => i.codigo == codigo)
                        .ToListAsync();
                        if (kits.Count() == 0)
                        {
                            return BadRequest(new { resp = "El Item " + codigo + " no tiene componentes registrados para ser modificados" });
                        }
                        // eliminanos componentes
                        _context.inkit.RemoveRange(kits);
                        await _context.SaveChangesAsync();

                        // agregamos componentes modificados
                        _context.inkit.AddRange(inkit);
                        await _context.SaveChangesAsync();

                        dbContexTransaction.Commit();

                        return Ok(new { resp = "206" });   // actualizado con exito
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        dbContexTransaction.Rollback();
                        return Problem("Error en el servidor");
                        throw;
                    }
                }
                    
            }
        }


        // POST: api/inkit
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPost("{userConn}")]
        public async Task<ActionResult<inkit>> Postinkit(string userConn, inkit inkit)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (_context.inkit == null)
                {
                    return BadRequest(new { resp = "Entidad inkit es null." });
                }
                _context.inkit.Add(inkit);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (inkitExists(inkit.codigo, inkit.item, _context))
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

        // DELETE: api/inkit/5
        [Authorize]
        [HttpDelete("{userConn}/{codigo}/{item}")]
        public async Task<IActionResult> Deleteinkit(string userConn, string codigo, string item)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.inkit == null)
                    {
                        return BadRequest(new { resp = "Entidad inkit es null." });
                    }
                    var inkit = _context.inkit.FirstOrDefault(objeto => objeto.codigo == codigo && objeto.item == item);
                    if (inkit == null)
                    {
                        return NotFound( new { resp = "No existe un registro con ese código" });
                    }

                    _context.inkit.Remove(inkit);
                    await _context.SaveChangesAsync();

                    return Ok( new { resp = "208" });   // eliminado con exito
                }
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }



        // DELETE: api/inkit/5
        [Authorize]
        [HttpDelete]
        [Route("deleteListinkit/{userConn}/{codigo}")]
        public async Task<IActionResult> deleteListinkit(string userConn, string codigo)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.inkit == null)
                    {
                        return BadRequest(new { resp = "Entidad inkit es null." });
                    }

                    var kits = await _context.inkit
                        .Where(i => i.codigo == codigo)
                        .ToListAsync();
                    if (kits.Count() == 0)
                    {
                        return BadRequest(new { resp = "El Item " + codigo + " no tiene componentes registrados para ser eliminados" });
                    }
                    // eliminanos componentes
                    _context.inkit.RemoveRange(kits);
                    await _context.SaveChangesAsync();

                    return Ok(new { resp = "208" });   // eliminado con exito
                }
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }



        private bool inkitExists(string codigo, string item, DBContext _context)
        {
            return (_context.inkit?.Any(e => e.codigo == codigo && e.item == item)).GetValueOrDefault();

        }
    }
}
