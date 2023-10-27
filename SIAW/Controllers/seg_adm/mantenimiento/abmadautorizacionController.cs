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
    public class abmadautorizacionController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        private readonly Funciones funciones = new Funciones();
        public abmadautorizacionController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }


        // GET: api/adautorizacion_deshabilitadas/5
        [HttpGet]
        [Route("getadautori_desha/{userConn}")]
        public async Task<ActionResult<adautorizacion_deshabilitadas>> getadautori_desha(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var adautorizacion_deshabilitadas = await _context.adautorizacion_deshabilitadas
                        .OrderBy(i => i.nivel)
                        .ToListAsync();

                    if (adautorizacion_deshabilitadas.Count() == 0)
                    {
                        return NotFound("No se encontro un registro con este código");
                    }

                    return Ok(adautorizacion_deshabilitadas);
                }
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }


        // GET: api/adautorizacion/5
        [HttpGet]
        [Route("getadautorizacion/{userConn}/{codigo}")]
        public async Task<ActionResult<adautorizacion>> getadautorizacion(string userConn, int codigo)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var adautorizacion = await _context.adautorizacion
                        .Join(_context.pepersona,
                        adaut => adaut.codpersona,
                        pe => pe.codigo,
                        (adaut, pe) => new
                        {
                            codigo = adaut.codigo,
                            nivel = adaut.nivel,
                            vencimiento = adaut.vencimiento,
                            codpersona = adaut.codpersona,
                            obs = adaut.obs,
                            nomPerson = pe.nombre1 + " " + pe.apellido1 + " " + pe.apellido2
                        })
                        .Where(i => i.codigo == codigo)
                        .OrderBy(i => i.codpersona)
                        .ThenBy(i => i.nivel)
                        .FirstOrDefaultAsync();

                    if (adautorizacion == null)
                    {
                        return NotFound("No se encontro un registro con este código");
                    }

                    return Ok(adautorizacion);
                }
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }

        // GET: api/adautorizacion/5
        [HttpGet]
        [Route("getadautorizacionList/{userConn}")]
        public async Task<ActionResult<adautorizacion>> getadautorizacionList(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var adautorizacion = await _context.adautorizacion
                        .Join(_context.pepersona,
                        adaut => adaut.codpersona,
                        pe => pe.codigo,
                        (adaut, pe) => new
                        {
                            codigo = adaut.codigo,
                            nivel = adaut.nivel,
                            vencimiento = adaut.vencimiento,
                            codpersona = adaut.codpersona,
                            obs = adaut.obs,
                            nomPerson = pe.nombre1 + " " + pe.apellido1 + " " + pe.apellido2
                        })
                        .OrderBy(i => i.codpersona)
                        .ThenBy(i => i.nivel)
                        .ToListAsync();

                    if (adautorizacion.Count() == 0)
                    {
                        return NotFound("No se encontro un registro con este código");
                    }

                    return Ok(adautorizacion);
                }
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }




        // PUT: api/adautorizacion/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPut]
        [Route("adautorizacion/{userConn}/{codigo}")]
        public async Task<IActionResult> Putadautorizacion(string userConn, int codigo, adautorizacion adautorizacion)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                var password = await _context.adautorizacion
                    .Where(i => i.codigo == codigo)
                    .Select(i => i.password)
                    .FirstOrDefaultAsync();

                adautorizacion.password = password;

                _context.Entry(adautorizacion).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!adautorizacionExists(codigo, _context))
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

        // POST: api/adautorizacion
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPost]
        [Route("adautorizacion/{userConn}")]
        public async Task<ActionResult<adautorizacion>> Postadautorizacion(string userConn, adautorizacion adautorizacion)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (_context.adautorizacion == null)
                {
                    return Problem("Entidad adautorizacion es null.");
                }

                var passEncript = await funciones.EncriptarMD5(adautorizacion.password);
                adautorizacion.password = passEncript;

                _context.adautorizacion.Add(adautorizacion);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (adautorizacionExists(adautorizacion.codigo, _context))
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

        // DELETE: api/adautorizacion/5
        [Authorize]
        [HttpDelete]
        [Route("adautorizacion/{userConn}/{codigo}")]
        public async Task<IActionResult> Deleteadautorizacion(string userConn, int codigo)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                //var _context = _userConnectionManager.GetUserConnection(userId);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.adautorizacion == null)
                    {
                        return Problem("Entidad adautorizacion es null.");
                    }
                    var adautorizacion = await _context.adautorizacion.FindAsync(codigo);
                    if (adautorizacion == null)
                    {
                        return NotFound("No existe un registro con ese código");
                    }

                    _context.adautorizacion.Remove(adautorizacion);
                    await _context.SaveChangesAsync();

                    return Ok("208");   // eliminado con exito
                }


            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }

        private bool adautorizacionExists(int codigo, DBContext _context)
        {
            return (_context.adautorizacion?.Any(e => e.codigo == codigo)).GetValueOrDefault();

        }




        // POST: api/adautorizacion_deshabilitadas
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPost]
        [Route("adautorizacion_deshabilitadas/{userConn}")]
        public async Task<ActionResult<adautorizacion_deshabilitadas>> Postadautorizacion_deshabilitadas(string userConn, adautorizacion_deshabilitadas adautorizacion_deshabilitadas)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (_context.adautorizacion_deshabilitadas == null)
                {
                    return Problem("Entidad adautorizacion_deshabilitadas es null.");
                }
                _context.adautorizacion_deshabilitadas.Add(adautorizacion_deshabilitadas);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (adautorizacion_deshabilitadasExists(adautorizacion_deshabilitadas.nivel, _context))
                    {
                        return Conflict("Ya existe un registro con ese código");
                    }
                    else
                    {
                        throw;
                    }
                }

                return Ok(new { resp = "204" });   // creado con exito

            }

        }

        // DELETE: api/adautorizacion_deshabilitadas/5
        [Authorize]
        [HttpDelete]
        [Route("adautorizacion_deshabilitadas/{userConn}/{nivel}")]
        public async Task<IActionResult> Deleteadautorizacion_deshabilitadas(string userConn, int nivel)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                //var _context = _userConnectionManager.GetUserConnection(userId);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.adautorizacion_deshabilitadas == null)
                    {
                        return Problem("Entidad adautorizacion_deshabilitadas es null.");
                    }
                    var adautorizacion_deshabilitadas = await _context.adautorizacion_deshabilitadas.FindAsync(nivel);
                    if (adautorizacion_deshabilitadas == null)
                    {
                        return NotFound("No existe un registro con ese código");
                    }

                    _context.adautorizacion_deshabilitadas.Remove(adautorizacion_deshabilitadas);
                    await _context.SaveChangesAsync();

                    return Ok("208");   // eliminado con exito
                }


            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }

        private bool adautorizacion_deshabilitadasExists(int nivel, DBContext _context)
        {
            return (_context.adautorizacion_deshabilitadas?.Any(e => e.nivel == nivel)).GetValueOrDefault();

        }




    }
}
