using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using siaw_DBContext.Data;
using siaw_DBContext.Models;
using System.Net;
using Microsoft.AspNetCore.Authorization;

namespace SIAW.Controllers.seg_adm.mantenimiento
{
    [Route("api/seg_adm/mant/[controller]")]
    [ApiController]
    public class adparametrosController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        public adparametrosController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }

        // GET: api/adparametros
        [HttpGet("{userConn}")]
        public async Task<ActionResult<IEnumerable<adparametros>>> Getadparametros(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.adparametros == null)
                    {
                        return BadRequest(new { resp = "Entidad adparametros es null." });
                    }
                    var result = await _context.adparametros.ToListAsync();
                    return Ok(result);
                }
                
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }


        }

        // GET: api/adparametros
        [HttpGet]
        [Route("getFirstEmpresa/{userConn}")]
        public async Task<ActionResult<IEnumerable<adparametros>>> getFirstEmpresa(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var result = await _context.adparametros.Select(i => i.codempresa).FirstOrDefaultAsync() ?? "";
                    if (result == "")
                    {
                        return BadRequest(new { resp = "No se encontraron datos." });
                    }
                    return Ok(result);
                }

            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }


        }


        // GET: api/adparametros/5
        [HttpGet("{userConn}/{codempresa}")]
        public async Task<ActionResult<adparametros>> Getadparametros(string userConn, string codempresa)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.adparametros == null)
                    {
                        return BadRequest(new { resp = "Entidad adparametros es null." });
                    }
                    var adparametros = await _context.adparametros.FindAsync(codempresa);

                    if (adparametros == null)
                    {
                        return NotFound( new { resp = "No se encontro un registro con este código" });
                    }

                    return Ok(adparametros);
                } 
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }


        // PUT: api/adparametros/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPut("{userConn}/{codempresa}")]
        public async Task<IActionResult> Putadparametros(string userConn, string codempresa, adparametros adparametros)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (codempresa != adparametros.codempresa)
                {
                    return BadRequest( new { resp = "Error con Id en datos proporcionados." });
                }

                _context.Entry(adparametros).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!adparametrosExists(codempresa, _context))
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




        // PUT: api/adparametros/5
        [Authorize]
        [HttpPut("updateCierresDiarios/{userConn}/{codempresa}/{cierres_diarios}")]
        public async Task<ActionResult<adparametros>> updateCierresDiarios(string userConn, string codempresa, bool cierres_diarios)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.adparametros == null)
                    {
                        return BadRequest(new { resp = "Entidad adparametros es null." });
                    }
                    var adparametros = await _context.adparametros.FindAsync(codempresa);

                    if (adparametros == null)
                    {
                        return NotFound( new { resp = "No se encontro un registro con este código" });
                    }
                    adparametros.cierres_diarios = cierres_diarios;
                    _context.Entry(adparametros).State = EntityState.Modified;
                    await _context.SaveChangesAsync();


                    return Ok( new { resp = "206" });   // actualizado con exito
                }

            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }




        // POST: api/adparametros
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPost("{userConn}")]
        public async Task<ActionResult<adparametros>> Postadparametros(string userConn, adparametros adparametros)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (_context.adparametros == null)
                {
                    return BadRequest(new { resp = "Entidad adparametros es null." });
                }
                _context.adparametros.Add(adparametros);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (adparametrosExists(adparametros.codempresa, _context))
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

        // DELETE: api/adparametros/5
        [Authorize]
        [HttpDelete("{userConn}/{codempresa}")]
        public async Task<IActionResult> Deleteadparametros(string userConn, string codempresa)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.adparametros == null)
                    {
                        return BadRequest(new { resp = "Entidad adparametros es null." });
                    }
                    var adparametros = await _context.adparametros.FindAsync(codempresa);
                    if (adparametros == null)
                    {
                        return NotFound( new { resp = "No existe un registro con ese código" });
                    }

                    _context.adparametros.Remove(adparametros);
                    await _context.SaveChangesAsync();

                    return Ok( new { resp = "208" });   // eliminado con exito
                }
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }

        private bool adparametrosExists(string codempresa, DBContext _context)
        {
            return (_context.adparametros?.Any(e => e.codempresa == codempresa)).GetValueOrDefault();

        }
    }








    [Route("api/seg_adm/mant/[controller]")]
    [ApiController]
    public class adparametros_complementariasController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        public adparametros_complementariasController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }

        // GET: api/adparametros_complementarias
        [HttpGet("{userConn}/{codempresa}")]
        public async Task<ActionResult<IEnumerable<adparametros_complementarias>>> Getadparametros_complementarias(string userConn, string codempresa)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.adparametros_complementarias == null)
                    {
                        return BadRequest(new { resp = "Entidad adparametros_complementarias es null." });
                    }
                    var result = await _context.adparametros_complementarias
                        .Where(e => e.codempresa == codempresa)
                        .OrderBy(e => e.sindesc)
                        .ThenBy(e => e.codtarifa)
                        .ToListAsync();
                    if (result.Count() == 0)
                    {
                        return NotFound(new { resp = "No se encontraron registros con los datos proporcionados." });
                    }
                    return Ok(result);
                }
                
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }


        }



        // POST: api/adparametros_complementarias
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPost("{userConn}")]
        public async Task<ActionResult<adparametros_complementarias>> Postadparametros_complementarias(string userConn, adparametros_complementarias adparametros_complementarias)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (_context.adparametros_complementarias == null)
                {
                    return BadRequest(new { resp = "Entidad adparametros_complementarias es null." });
                }
                _context.adparametros_complementarias.Add(adparametros_complementarias);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (adparametros_complementariasExists(adparametros_complementarias.codigo, _context))
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

        // DELETE: api/adparametros_complementarias/5
        [Authorize]
        [HttpDelete("{userConn}/{codigo}")]
        public async Task<IActionResult> Deleteadparametros_complementarias(string userConn, int codigo)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.adparametros_complementarias == null)
                    {
                        return BadRequest(new { resp = "Entidad adparametros_complementarias es null." });
                    }
                    var adparametros_complementarias = await _context.adparametros_complementarias.FindAsync(codigo);
                    if (adparametros_complementarias == null)
                    {
                        return NotFound( new { resp = "No existe un registro con ese código" });
                    }

                    _context.adparametros_complementarias.Remove(adparametros_complementarias);
                    await _context.SaveChangesAsync();

                    return Ok( new { resp = "208" });   // eliminado con exito
                }
                

            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }

        private bool adparametros_complementariasExists(int codigo, DBContext _context)
        {
            return (_context.adparametros_complementarias?.Any(e => e.codigo == codigo)).GetValueOrDefault();

        }

    }








    [Route("api/seg_adm/mant/[controller]")]
    [ApiController]
    public class adparametros_diasextrancController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        public adparametros_diasextrancController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }

        // GET: api/adparametros_diasextranc
        [HttpGet("{userConn}/{codempresa}")]
        public async Task<ActionResult<IEnumerable<adparametros_diasextranc>>> Getadparametros_diasextranc(string userConn, string codempresa)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.adparametros_diasextranc == null)
                    {
                        return BadRequest(new { resp = "Entidad adparametros_diasextranc es null." });
                    }
                    var result = await _context.adparametros_diasextranc
                        .Where(e => e.codempresa == codempresa)
                        .OrderBy(e => e.dias)
                        .ToListAsync();
                    if (result.Count() == 0)
                    {
                        return NotFound(new { resp = "No se encontraron registros con los datos proporcionados." });
                    }
                    return Ok(result);
                }
                
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }


        }



        // POST: api/adparametros_diasextranc
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPost("{userConn}")]
        public async Task<ActionResult<adparametros_diasextranc>> Postadparametros_diasextranc(string userConn, adparametros_diasextranc adparametros_diasextranc)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (_context.adparametros_diasextranc == null)
                {
                    return BadRequest(new { resp = "Entidad adparametros_diasextranc es null." });
                }
                _context.adparametros_diasextranc.Add(adparametros_diasextranc);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (adparametros_diasextrancExists(adparametros_diasextranc.codigo, _context))
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

        // DELETE: api/adparametros_diasextranc/5
        [Authorize]
        [HttpDelete("{userConn}/{codigo}")]
        public async Task<IActionResult> Deleteadparametros_diasextranc(string userConn, int codigo)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.adparametros_diasextranc == null)
                    {
                        return BadRequest(new { resp = "Entidad adparametros_diasextranc es null." });
                    }
                    var adparametros_diasextranc = await _context.adparametros_diasextranc.FindAsync(codigo);
                    if (adparametros_diasextranc == null)
                    {
                        return NotFound( new { resp = "No existe un registro con ese código" });
                    }

                    _context.adparametros_diasextranc.Remove(adparametros_diasextranc);
                    await _context.SaveChangesAsync();

                    return Ok( new { resp = "208" });   // eliminado con exito
                }
                

            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }

        private bool adparametros_diasextrancExists(int codigo, DBContext _context)
        {
            return (_context.adparametros_diasextranc?.Any(e => e.codigo == codigo)).GetValueOrDefault();

        }

    }




    [Route("api/seg_adm/mant/[controller]")]
    [ApiController]
    public class adparametros_tarifasfactController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        public adparametros_tarifasfactController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }

        // GET: api/adparametros_tarifasfact
        [HttpGet("{userConn}/{codempresa}")]
        public async Task<ActionResult<IEnumerable<adparametros_tarifasfact>>> Getadparametros_tarifasfact(string userConn, string codempresa)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.adparametros_tarifasfact == null)
                    {
                        return BadRequest(new { resp = "Entidad adparametros_tarifasfact es null." });
                    }

                    var consulta = from adparametros_tarifasfact in _context.adparametros_tarifasfact
                                   join intarifa in _context.intarifa on adparametros_tarifasfact.codtarifa equals intarifa.codigo
                                   where adparametros_tarifasfact.codempresa == codempresa
                                   orderby adparametros_tarifasfact.codtarifa ascending
                                   select new
                                   {
                                       codtarifa = adparametros_tarifasfact.codtarifa,
                                       descripcion = intarifa.descripcion
                                   };

                    var result = await consulta.ToListAsync();
                    if (result.Count() == 0)
                    {
                        return NotFound(new { resp = "No se encontraron registros con los datos proporcionados." });
                    }

                    return Ok(result);
                }
                
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }


        }



        // POST: api/adparametros_tarifasfact
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPost("{userConn}")]
        public async Task<ActionResult<adparametros_tarifasfact>> Postadparametros_tarifasfact(string userConn, adparametros_tarifasfact adparametros_tarifasfact)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (_context.adparametros_tarifasfact == null)
                {
                    return BadRequest(new { resp = "Entidad adparametros_tarifasfact es null." });
                }
                _context.adparametros_tarifasfact.Add(adparametros_tarifasfact);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (adparametros_tarifasfactExists(adparametros_tarifasfact.codtarifa, _context))
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

        // DELETE: api/adparametros_tarifasfact/5
        [Authorize]
        [HttpDelete("{userConn}/{codigo}/{codempresa}")]
        public async Task<IActionResult> Deleteadparametros_tarifasfact(string userConn, int codtarifa, string codempresa)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.adparametros_tarifasfact == null)
                    {
                        return BadRequest(new { resp = "Entidad adparametros_tarifasfact es null." });
                    }
                    var adparametros_tarifasfact = _context.adparametros_tarifasfact.FirstOrDefault(objeto => objeto.codtarifa == codtarifa && objeto.codempresa == codempresa);
                    if (adparametros_tarifasfact == null)
                    {
                        return NotFound( new { resp = "No existe un registro con ese código" });
                    }

                    _context.adparametros_tarifasfact.Remove(adparametros_tarifasfact);
                    await _context.SaveChangesAsync();

                    return Ok( new { resp = "208" });   // eliminado con exito
                }
                

            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }

        private bool adparametros_tarifasfactExists(int codtarifa, DBContext _context)
        {
            return (_context.adparametros_tarifasfact?.Any(e => e.codtarifa == codtarifa)).GetValueOrDefault();

        }

    }

}


