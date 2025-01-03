﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using siaw_DBContext.Data;
using siaw_DBContext.Models;

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

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.selog == null)
                    {
                        return BadRequest(new { resp = "Entidad selog es null." });
                    }
                    var result = await _context.selog.Where(x => x.fecha == fecha).OrderByDescending(f => f.hora).ToListAsync();
                    return Ok(result);
                }
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }


        // GET: api/selog
        [HttpGet]
        [Route("getseloguserfecha/{userConn}/{usuario}/{fecha}")]
        public async Task<ActionResult<IEnumerable<selog>>> getseloguserfecha(string userConn, string usuario, DateTime fecha)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.selog == null)
                    {
                        return BadRequest(new { resp = "Entidad selog es null." });
                    }
                    var result = await _context.selog.Where(x => x.fecha == fecha && x.usuario == usuario).OrderByDescending(f => f.hora).ToListAsync();
                    return Ok(result);
                }
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }


        // POST: api/selog
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost("{userConn}")]
        public async Task<ActionResult<selog>> Postselog(string userConn, selog selog)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (_context.selog == null)
                {
                    return BadRequest(new { resp = "Entidad selog es null." });
                }
                _context.selog.Add(selog);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    return Problem("Error en el servidor");
                }

                return Ok( new { resp = "204" });   // creado con exito

            }
            
        }
    }
}
