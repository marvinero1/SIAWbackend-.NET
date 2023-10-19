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
    public class adusuario_idproformaController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        public adusuario_idproformaController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }

        // GET: api/adusuario_idproforma
        [HttpGet("{userConn}")]
        public async Task<ActionResult<IEnumerable<adusuario_idproforma>>> Getadusuario_idproforma(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                //var _context = _userConnectionManager.GetUserConnection(userId);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.adusuario_idproforma == null)
                    {
                        return Problem("Entidad adusuario_idproforma es null.");
                    }
                    var result = await _context.adusuario_idproforma
                        .OrderBy(x => x.usuario)
                        .ThenBy(x => x.idproforma)
                        .ToListAsync();
                    return Ok(result);
                }

            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }




        // GET: api/catalogo
        [HttpGet]
        [Route("catalogoVenumeracionProf/{userConn}")]
        public async Task<ActionResult<IEnumerable<venumeracion>>> catalogoVenumeracionProf(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                //var _context = _userConnectionManager.GetUserConnection(userId);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var query = _context.venumeracion
                    .Where(i => i.tipodoc == 2 && i.habilitado == true)
                    .OrderBy(i => i.id)
                    .Select(i => new
                    {
                        id = i.id,
                        descrip = i.descripcion
                    });

                    var result = query.ToList();

                    if (result.Count() == 0)
                    {
                        return Problem("Entidad venumeracion-proforma es null.");
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




        // POST: api/adusuario_idproforma
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPost("{userConn}")]
        public async Task<ActionResult<adusuario_idproforma>> Postadusuario_idproforma(string userConn, adusuario_idproforma adusuario_idproforma)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            string id_asignado = await if_id_asignado(userConnectionString, adusuario_idproforma.idproforma);
            if (id_asignado != "aceptado")
            {
                return Ok(new { codigo = 706,resul = id_asignado});
            }
            string usuario_idasignado = await usuario_tienen_id_asignado(userConnectionString, adusuario_idproforma.usuario);
            if (usuario_idasignado != "aceptado")
            {
                return Ok(new { codigo = 708, resul = usuario_idasignado });
            }

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (_context.adusuario_idproforma == null)
                {
                    return Problem("Entidad adusuario_idproforma es null.");
                }

                _context.adusuario_idproforma.Add(adusuario_idproforma);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (adusuario_idproformaExists(adusuario_idproforma.id, _context))
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

        private bool adusuario_idproformaExists(int codigo, DBContext _context)
        {
            return (_context.adusuario_idproforma?.Any(e => e.id == codigo)).GetValueOrDefault();

        }


        // DELETE: api/adusuario_idproforma/5
        [Authorize]
        [HttpDelete("{userConn}/{id}")]
        public async Task<IActionResult> Deleteadusuario_idproforma(string userConn, int id)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.adusuario_idproforma == null)
                    {
                        return Problem("Entidad adusuario_idproforma es null.");
                    }
                    var adusuario_idproforma = await _context.adusuario_idproforma.FindAsync(id);
                    if (adusuario_idproforma == null)
                    {
                        return NotFound("No existe un registro con ese código");
                    }

                    _context.adusuario_idproforma.Remove(adusuario_idproforma);
                    await _context.SaveChangesAsync();

                    return Ok("208");   // eliminado con exito
                }
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }






        private async Task<string> if_id_asignado (string userConnectionString, string id)
        {
            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                var adusuario_idproforma = await _context.adusuario_idproforma
                    .Where(item => item.idproforma == id)
                    .Select(item => new 
                    {
                        usuario = item.usuario,
                        idproforma = item.idproforma
                    })
                    .FirstOrDefaultAsync();
                if (adusuario_idproforma == null)
                {
                    return "aceptado";
                }
                return "El id: " + adusuario_idproforma.idproforma + " ya esta asignado al usuario: " + adusuario_idproforma.usuario;
            }
        }


        private async Task<string> usuario_tienen_id_asignado(string userConnectionString, string usuario)
        {
            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                var adusuario_idproforma = await _context.adusuario_idproforma
                    .Where(item => item.usuario == usuario)
                    .Select(item => new
                    {
                        usuario = item.usuario,
                        idproforma = item.idproforma
                    })
                    .FirstOrDefaultAsync();
                if (adusuario_idproforma == null)
                {
                    return "aceptado";
                }
                return "El usuario: " + adusuario_idproforma.usuario + " ya tiene asignado uno o varios id.";
            }
        }


    }
}
