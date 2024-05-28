using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using siaw_DBContext.Data;
using siaw_DBContext.Models;
using Microsoft.AspNetCore.Authorization;

namespace SIAW.Controllers.seg_adm.mantenimiento
{
    [Route("api/seg_adm/mant/[controller]")]
    [ApiController]
    public class adusparametrosController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        public adusparametrosController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }

        // GET: api/adusparametros
        [HttpGet("{userConn}")]
        public async Task<ActionResult<IEnumerable<adusparametros>>> Getadusparametros(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.adusparametros == null)
                    {
                        return BadRequest(new { resp = "Entidad adusparametros es null." });
                    }
                    var result = await _context.adusparametros.OrderBy(usuario => usuario.usuario).ToListAsync();
                    return Ok(result);
                }
                
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }


        }

        // GET: api/adusparametros/5
        [HttpGet("{userConn}/{usuario}")]
        public async Task<ActionResult<adusparametros>> Getadusparametros(string userConn, string usuario)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.adusparametros == null)
                    {
                        return BadRequest(new { resp = "Entidad adusparametros es null." });
                    }
                    var adusparametros = await _context.adusparametros.FindAsync(usuario);

                    if (adusparametros == null)
                    {
                        return NotFound( new { resp = "No se encontro un registro con este código" });
                    }

                    return Ok(adusparametros);
                }
                
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }


        /// <summary>
        /// Obtiene codigos de almacenes permitidos para obtener saldos por usuario
        /// </summary>
        /// <param name="userConn"></param>
        /// <param name="usuario"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("getalmsald/{userConn}/{usuario}")]
        public async Task<ActionResult<IEnumerable<adusparametros>>> Getvenumeracion_getalmsald(string userConn, string usuario)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var adusparametros = await _context.adusparametros
                    .Where(i => i.usuario == usuario)
                    .Select(i => new
                    {
                        usuario = i.usuario,
                        codalmsald1 = i.codalmsald1,
                        codalmsald2 = i.codalmsald2,
                        codalmsald3 = i.codalmsald3,
                        codalmsald4 = i.codalmsald4,
                        codalmsald5 = i.codalmsald5
                    })
                    .FirstOrDefaultAsync();


                    if (adusparametros == null)
                    {
                        return BadRequest(new { resp = "Entidad adusparametros es null." });
                    }
                    return Ok(adusparametros);
                }

            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
                throw;
                throw;
            }
        }

        [HttpGet]
        [Route("getInfoUserAdus/{userConn}/{usuario}")]
        public async Task<ActionResult<inconcepto>> getInfoUserAdus(string userConn, string usuario)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var codalmacen = await _context.adusparametros
                        .Where(item => item.usuario == usuario)
                        .Select(item => new
                        {
                            item.codalmacen,
                            item.codtarifa,
                            item.coddescuento,
                        }
                        )
                        .FirstOrDefaultAsync();

                    if (codalmacen == null)
                    {
                        return NotFound(new { resp = "No se encontro un registro con este código (cod almacen)" });
                    }

                    return Ok(codalmacen);
                }

            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }


        // PUT: api/adusparametros/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPut("{userConn}/{usuario}")]
        public async Task<IActionResult> Putadusparametros(string userConn, string usuario, adusparametros adusparametros)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (usuario != adusparametros.usuario)
                {
                    return BadRequest( new { resp = "Error con Id en datos proporcionados." });
                }

                _context.Entry(adusparametros).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!adusparametrosExists(usuario, _context))
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

        // POST: api/adusparametros
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPost("{userConn}")]
        public async Task<ActionResult<adusparametros>> Postadusparametros(string userConn, adusparametros adusparametros)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (_context.adusparametros == null)
                {
                    return BadRequest(new { resp = "Entidad adusparametros es null." });
                }
                _context.adusparametros.Add(adusparametros);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (adusparametrosExists(adusparametros.usuario, _context))
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

        // DELETE: api/adusparametros/5
        [Authorize]
        [HttpDelete("{userConn}/{usuario}")]
        public async Task<IActionResult> Deleteadusparametros(string userConn, string usuario)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.adusparametros == null)
                    {
                        return BadRequest(new { resp = "Entidad adusparametros es null." });
                    }
                    var adusparametros = await _context.adusparametros.FindAsync(usuario);
                    if (adusparametros == null)
                    {
                        return NotFound( new { resp = "No existe un registro con ese código" });
                    }

                    _context.adusparametros.Remove(adusparametros);
                    await _context.SaveChangesAsync();

                    return Ok( new { resp = "208" });   // eliminado con exito
                }
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }

        private bool adusparametrosExists(string usuario, DBContext _context)
        {
            return (_context.adusparametros?.Any(e => e.usuario == usuario)).GetValueOrDefault();

        }
    }
}
