﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using siaw_DBContext.Data;
using siaw_DBContext.Models;
using Microsoft.AspNetCore.Authorization;

namespace SIAW.Controllers.ventas.mantenimiento
{
    [Route("api/venta/mant/[controller]")]
    [ApiController]
    public class velugarController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        public velugarController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }

        // GET: api/velugar
        [HttpGet("{userConn}")]
        public async Task<ActionResult<IEnumerable<velugar>>> Getvelugar(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                //var _context = _userConnectionManager.GetUserConnection(userId);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.velugar == null)
                    {
                        return Problem("Entidad velugar es null.");
                    }


                    var result = await _context.velugar
                        .OrderBy(codigo => codigo.codigo)

                        .GroupJoin(
                            _context.pepersona,
                            vl => vl.codpersona,
                            pp => pp.codigo,
                            (vl, ppGroup) => new { vl, ppGroup }
                        )
                        .SelectMany(
                            x => x.ppGroup.DefaultIfEmpty(),
                            (x, pp) => new { x.vl, pp }
                        )
                        .GroupJoin(
                            _context.vezona,
                            x => x.vl.codzona,
                            vz => vz.codigo,
                            (x, vzGroup) => new { x.vl, x.pp, vzGroup }
                        )
                        .SelectMany(
                            x => x.vzGroup.DefaultIfEmpty(),
                            (x, vz) => new
                            {
                                codigo = x.vl.codigo,
                                descripcion = x.vl.descripcion,
                                direccion = x.vl.direccion,
                                obs = x.vl.obs,
                                horareg = x.vl.horareg,
                                fechareg = x.vl.fechareg,
                                usuarioreg = x.vl.usuarioreg,
                                codzona = x.vl.codzona,
                                zonaDescrip = (vz != null) ? vz.descripcion : string.Empty,
                                latitud = x.vl.latitud,
                                longitud = x.vl.longitud,
                                puntear = x.vl.puntear,
                                codpersona = x.vl.codpersona,
                                personaDescrip = ((x.pp != null) ? x.pp.nombre1 : string.Empty) + " " + ((x.pp != null) ? x.pp.apellido1 : string.Empty) + " " + ((x.pp != null) ? x.pp.apellido2 : string.Empty)
                                
                            }
                        )

                        .ToListAsync();


                    return Ok(result);
                }

            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }


        }

        // GET: api/velugar/5
        [HttpGet("{userConn}/{codigo}")]
        public async Task<ActionResult<velugar>> Getvelugar(string userConn, string codigo)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                //var _context = _userConnectionManager.GetUserConnection(userId);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.velugar == null)
                    {
                        return Problem("Entidad velugar es null.");
                    }
                    var velugar = await _context.velugar
                        .Where(i => i.codigo==codigo)
                        .GroupJoin(
                            _context.pepersona,
                            vl => vl.codpersona,
                            pp => pp.codigo,
                            (vl, ppGroup) => new { vl, ppGroup }
                        )
                        .SelectMany(
                            x => x.ppGroup.DefaultIfEmpty(),
                            (x, pp) => new { x.vl, pp }
                        )
                        .GroupJoin(
                            _context.vezona,
                            x => x.vl.codzona,
                            vz => vz.codigo,
                            (x, vzGroup) => new { x.vl, x.pp, vzGroup }
                        )
                        .SelectMany(
                            x => x.vzGroup.DefaultIfEmpty(),
                            (x, vz) => new
                            {
                                codigo = x.vl.codigo,
                                descripcion = x.vl.descripcion,
                                direccion = x.vl.direccion,
                                obs = x.vl.obs,
                                horareg = x.vl.horareg,
                                fechareg = x.vl.fechareg,
                                usuarioreg = x.vl.usuarioreg,
                                codzona = x.vl.codzona,
                                zonaDescrip = (vz != null) ? vz.descripcion : string.Empty,
                                latitud = x.vl.latitud,
                                longitud = x.vl.longitud,
                                puntear = x.vl.puntear,
                                codpersona = x.vl.codpersona,
                                personaDescrip = ((x.pp != null) ? x.pp.nombre1 : string.Empty) + " " + ((x.pp != null) ? x.pp.apellido1 : string.Empty) + " " + ((x.pp != null) ? x.pp.apellido2 : string.Empty)

                            }
                        )
                        .FirstOrDefaultAsync();

                    if (velugar == null)
                    {
                        return NotFound("No se encontro un registro con este código");
                    }

                    return Ok(velugar);
                }

            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }

        // GET: api/catalogo
        [HttpGet]
        [Route("catalogo/{userConn}")]
        public async Task<ActionResult<IEnumerable<velugar>>> Getvelugar_catalogo(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                //var _context = _userConnectionManager.GetUserConnection(userId);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var query = _context.velugar
                    .OrderBy(i => i.codigo)
                    .Select(i => new
                    {
                        i.codigo,
                        i.descripcion
                    });

                    var result = query.ToList();

                    if (result.Count() == 0)
                    {
                        return Problem("Entidad velugar es null.");
                    }
                    return Ok(result);
                }

            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
                throw;
            }
        }

        // PUT: api/velugar/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPut("{userConn}/{codigo}")]
        public async Task<IActionResult> Putvelugar(string userConn, string codigo, velugar velugar)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            //var _context = _userConnectionManager.GetUserConnection(userId);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (codigo != velugar.codigo)
                {
                    return BadRequest("Error con Id en datos proporcionados.");
                }

                _context.Entry(velugar).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!velugarExists(codigo, _context))
                    {
                        return NotFound("No existe un registro con ese código");
                    }
                    else
                    {
                        throw;
                    }
                }

                return Ok("206");   // actualizado con exito
            }



        }

        // POST: api/velugar
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPost("{userConn}")]
        public async Task<ActionResult<velugar>> Postvelugar(string userConn, velugar velugar)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            //var _context = _userConnectionManager.GetUserConnection(userId);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (_context.velugar == null)
                {
                    return Problem("Entidad velugar es null.");
                }
                _context.velugar.Add(velugar);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (velugarExists(velugar.codigo, _context))
                    {
                        return Conflict("Ya existe un registro con ese código");
                    }
                    else
                    {
                        throw;
                    }
                }

                return Ok("204");   // creado con exito

            }

        }

        // DELETE: api/velugar/5
        [Authorize]
        [HttpDelete("{userConn}/{codigo}")]
        public async Task<IActionResult> Deletevelugar(string userConn, string codigo)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                //var _context = _userConnectionManager.GetUserConnection(userId);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.velugar == null)
                    {
                        return Problem("Entidad velugar es null.");
                    }
                    var velugar = await _context.velugar.FindAsync(codigo);
                    if (velugar == null)
                    {
                        return NotFound("No existe un registro con ese código");
                    }

                    _context.velugar.Remove(velugar);
                    await _context.SaveChangesAsync();

                    return Ok("208");   // eliminado con exito
                }


            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }

        private bool velugarExists(string codigo, DBContext _context)
        {
            return (_context.velugar?.Any(e => e.codigo == codigo)).GetValueOrDefault();

        }
    }
}
