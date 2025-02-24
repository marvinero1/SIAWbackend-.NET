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
    public class venumeracionController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        public venumeracionController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }

        // GET: api/venumeracion
        [HttpGet("{userConn}")]
        public async Task<ActionResult<IEnumerable<venumeracion>>> Getvenumeracion(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.venumeracion == null)
                    {
                        return BadRequest(new { resp = "Entidad venumeracion es null." });
                    }
                    var result = await _context.venumeracion
                        .Join(_context.adunidad,
                        vn => vn.codunidad,
                        au => au.codigo,
                        (vn, au) => new
                        {
                            id = vn.id,
                            descripcion = vn.descripcion,
                            nroactual = vn.nroactual,
                            tipodoc = vn.tipodoc,
                            habilitado = vn.habilitado,
                            descarga = vn.descarga,
                            horareg = vn.horareg,
                            fechareg = vn.fechareg,
                            usuarioreg = vn.usuarioreg,
                            codunidad = vn.codunidad,
                            unidadDesc = au.descripcion,
                            reversion = vn.reversion,
                            tipo = vn.tipo,
                            codalmacen = vn.codalmacen
                        })
                        .OrderBy(id => id.id)
                        .ToListAsync();
                    return Ok(result);
                }
                
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }

        // GET: api/venumeracion/5
        [HttpGet("{userConn}/{id}")]
        public async Task<ActionResult<venumeracion>> Getvenumeracion(string userConn, string id)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.venumeracion == null)
                    {
                        return BadRequest(new { resp = "Entidad venumeracion es null." });
                    }
                    var venumeracion = await _context.venumeracion
                        .Join(_context.adunidad,
                        vn => vn.codunidad,
                        au => au.codigo,
                        (vn, au) => new
                        {
                            id = vn.id,
                            descripcion = vn.descripcion,
                            nroactual = vn.nroactual,
                            tipodoc = vn.tipodoc,
                            habilitado = vn.habilitado,
                            descarga = vn.descarga,
                            horareg = vn.horareg,
                            fechareg = vn.fechareg,
                            usuarioreg = vn.usuarioreg,
                            codunidad = vn.codunidad,
                            unidadDesc = au.descripcion,
                            reversion = vn.reversion,
                            tipo = vn.tipo,
                            codalmacen = vn.codalmacen
                        })
                        .Where(v => v.id == id)
                        .FirstOrDefaultAsync();

                    if (venumeracion == null)
                    {
                        return NotFound( new { resp = "No se encontro un registro con este código" });
                    }

                    return Ok(venumeracion);
                }
                
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }


        /// <summary>
        /// Obtiene algunos datos de la tabla venumeracion para catalogo por tipodoc y si esta habilitado
        /// </summary>
        /// <param name="userConn"></param>
        /// <param name="tipodoc"></param>
        /// <returns></returns>
        // GET: api/catalogo
        [HttpGet]
        [Route("catalogo/{userConn}/{tipodoc}")]
        public async Task<ActionResult<IEnumerable<venumeracion>>> Getvenumeracion_catalogo(string userConn, int tipodoc)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var query = _context.venumeracion
                    .Where(i => i.tipodoc == tipodoc)
                    .Where(i => i.habilitado == true)
                    .OrderBy(i => i.id)
                    .Select(i => new
                    {
                        i.id,
                        i.descripcion,
                        i.nroactual,
                    });

                    var result = query.ToList();

                    if (result.Count() == 0)
                    {
                        return BadRequest(new { resp = "Entidad venumeracion es null." });
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

        // GET: api/catalogo
        [HttpGet]
        [Route("catalogoGeneral/{userConn}")]
        public async Task<ActionResult<IEnumerable<venumeracion>>> catalogoGeneral(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var result = await _context.venumeracion
                    .OrderBy(e => e.id)
                    .Select(e => new
                    {
                        e.id,
                        e.descripcion,
                        e.nroactual,
                    })
                    .ToListAsync();

                    return Ok(result);
                }

            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
                throw;
            }
        }

        // GET: api/adsiat_tipodocidentidad
        [HttpGet]
        [Route("catalogoNumProfxUsuario/{userConn}/{codUsuario}")]
        public async Task<ActionResult<IEnumerable<venumeracion>>> catalogoNumProf(string userConn, string codUsuario)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var resultado = await _context.venumeracion
                        .Where(v => v.tipodoc == 2 && v.habilitado == true &&
                                    _context.adusuario_idproforma
                                        .Where(a => a.usuario == codUsuario && a.para_web==true)
                                        .Select(a => a.idproforma)
                                        .Contains(v.id))
                        .OrderBy(v => v.id)
                        .Select(v => new
                        {
                            v.id,
                            v.descripcion
                        })
                        .ToListAsync();
                    return Ok(resultado);
                }
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }



        // PUT: api/venumeracion/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPut("{userConn}/{id}")]
        public async Task<IActionResult> Putvenumeracion(string userConn, string id, venumeracion venumeracion)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (id != venumeracion.id)
                {
                    return BadRequest( new { resp = "Error con Id en datos proporcionados." });
                }

                _context.Entry(venumeracion).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!venumeracionExists(id, _context))
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

        // POST: api/venumeracion
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPost("{userConn}")]
        public async Task<ActionResult<venumeracion>> Postvenumeracion(string userConn, venumeracion venumeracion)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (_context.venumeracion == null)
                {
                    return BadRequest(new { resp = "Entidad venumeracion es null." });
                }
                _context.venumeracion.Add(venumeracion);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (venumeracionExists(venumeracion.id, _context))
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

        // DELETE: api/venumeracion/5
        [Authorize]
        [HttpDelete("{userConn}/{id}")]
        public async Task<IActionResult> Deletevenumeracion(string userConn, string id)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.venumeracion == null)
                    {
                        return BadRequest(new { resp = "Entidad venumeracion es null." });
                    }
                    var venumeracion = await _context.venumeracion.FindAsync(id);
                    if (venumeracion == null)
                    {
                        return NotFound( new { resp = "No existe un registro con ese código" });
                    }

                    _context.venumeracion.Remove(venumeracion);
                    await _context.SaveChangesAsync();

                    return Ok( new { resp = "208" });   // eliminado con exito
                }
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }

        private bool venumeracionExists(string id, DBContext _context)
        {
            return (_context.venumeracion?.Any(e => e.id == id)).GetValueOrDefault();

        }
    }
}
