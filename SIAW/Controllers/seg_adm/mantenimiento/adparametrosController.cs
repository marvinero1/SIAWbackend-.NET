using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIAW.Data;
using SIAW.Models;
using System.Net;
using Microsoft.AspNetCore.Authorization;

namespace SIAW.Controllers.seg_adm.mantenimiento
{
    [Authorize]
    [Route("api/seg_adm/mant/[controller]")]
    [ApiController]
    public class adparametrosController : ControllerBase
    {
        private readonly DBContext _context;
        private readonly string connectionString;
        private VerificaConexion verificador;
        private readonly IConfiguration _configuration;
        public adparametrosController(IConfiguration configuration)
        {
            connectionString = ConnectionController.ConnectionString;
            _context = DbContextFactory.Create(connectionString);
            _configuration = configuration;
            verificador = new VerificaConexion(_configuration);
        }

        // GET: api/adparametros
        [HttpGet("{conexionName}")]
        public async Task<ActionResult<IEnumerable<adparametros>>> Getadparametros(string conexionName)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.adparametros == null)
                    {
                        return Problem("Entidad adparametros es null.");
                    }
                    var result = await _context.adparametros.ToListAsync();
                    return Ok(result);
                }
                return BadRequest("Se perdio la conexion con el servidor");
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }


        }

        // GET: api/adparametros/5
        [HttpGet("{conexionName}/{codempresa}")]
        public async Task<ActionResult<adparametros>> Getadparametros(string conexionName, string codempresa)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.adparametros == null)
                    {
                        return Problem("Entidad adparametros es null.");
                    }
                    var adparametros = await _context.adparametros.FindAsync(codempresa);

                    if (adparametros == null)
                    {
                        return NotFound("No se encontro un registro con este código");
                    }

                    return Ok(adparametros);
                }
                return BadRequest("Se perdio la conexion con el servidor");
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }

        // PUT: api/adparametros/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{conexionName}/{codempresa}")]
        public async Task<IActionResult> Putadparametros(string conexionName, string codempresa, adparametros adparametros)
        {
            if (verificador.VerConnection(conexionName, connectionString))
            {
                if (codempresa != adparametros.codempresa)
                {
                    return BadRequest("Error con Id en datos proporcionados.");
                }

                _context.Entry(adparametros).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!adparametrosExists(codempresa))
                    {
                        return NotFound("No existe un registro con ese código");
                    }
                    else
                    {
                        throw;
                    }
                }

                return Ok("Datos actualizados correctamente.");
            }
            return BadRequest("Se perdio la conexion con el servidor");


        }

        // POST: api/adparametros
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost("{conexionName}")]
        public async Task<ActionResult<adparametros>> Postadparametros(string conexionName, adparametros adparametros)
        {
            if (verificador.VerConnection(conexionName, connectionString))
            {
                if (_context.adparametros == null)
                {
                    return Problem("Entidad adparametros es null.");
                }
                _context.adparametros.Add(adparametros);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (adparametrosExists(adparametros.codempresa))
                    {
                        return Conflict("Ya existe un registro con ese código");
                    }
                    else
                    {
                        throw;
                    }
                }

                return Ok("Registrado con Exito :D");

            }
            return BadRequest("Se perdio la conexion con el servidor");
        }

        // DELETE: api/adparametros/5
        [HttpDelete("{conexionName}/{codempresa}")]
        public async Task<IActionResult> Deleteadparametros(string conexionName, string codempresa)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.adparametros == null)
                    {
                        return Problem("Entidad adparametros es null.");
                    }
                    var adparametros = await _context.adparametros.FindAsync(codempresa);
                    if (adparametros == null)
                    {
                        return NotFound("No existe un registro con ese código");
                    }

                    _context.adparametros.Remove(adparametros);
                    await _context.SaveChangesAsync();

                    return Ok("Datos eliminados con exito");
                }
                return BadRequest("Se perdio la conexion con el servidor");

            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }

        private bool adparametrosExists(string codempresa)
        {
            return (_context.adparametros?.Any(e => e.codempresa == codempresa)).GetValueOrDefault();

        }
    }








    [Route("api/seg_adm/mant/adparametrosComplementarias/[controller]")]
    [ApiController]
    public class adparametros_complementariasController : ControllerBase
    {
        private readonly DBContext _context;
        private readonly string connectionString;
        private VerificaConexion verificador;
        private readonly IConfiguration _configuration;
        public adparametros_complementariasController(IConfiguration configuration)
        {
            connectionString = ConnectionController.ConnectionString;
            _context = DbContextFactory.Create(connectionString);
            _configuration = configuration;
            verificador = new VerificaConexion(_configuration);
        }

        // GET: api/adparametros_complementarias
        [HttpGet("{conexionName}/{codempresa}")]
        public async Task<ActionResult<IEnumerable<adparametros_complementarias>>> Getadparametros_complementarias(string conexionName, string codempresa)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.adparametros_complementarias == null)
                    {
                        return Problem("Entidad adparametros_complementarias es null.");
                    }
                    var result = await _context.adparametros_complementarias
                        .Where(e => e.codempresa == codempresa)
                        .OrderBy(e => e.sindesc)
                        .ThenBy(e => e.codtarifa)
                        .ToListAsync();
                    if (result.Count() == 0)
                    {
                        return NotFound("No se encontro un registro con los datos proporcionados.");
                    }
                    return Ok(result);
                }
                return BadRequest("Se perdio la conexion con el servidor");
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }


        }



        // POST: api/adparametros_complementarias
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost("{conexionName}")]
        public async Task<ActionResult<adparametros_complementarias>> Postadparametros_complementarias(string conexionName, adparametros_complementarias adparametros_complementarias)
        {
            if (verificador.VerConnection(conexionName, connectionString))
            {
                if (_context.adparametros_complementarias == null)
                {
                    return Problem("Entidad adparametros_complementarias es null.");
                }
                _context.adparametros_complementarias.Add(adparametros_complementarias);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (adparametros_complementariasExists(adparametros_complementarias.codigo))
                    {
                        return Conflict("Ya existe un registro con ese código");
                    }
                    else
                    {
                        throw;
                    }
                }

                return Ok("Registrado con Exito :D");

            }
            return BadRequest("Se perdio la conexion con el servidor");
        }

        // DELETE: api/adparametros_complementarias/5
        [HttpDelete("{conexionName}/{codigo}")]
        public async Task<IActionResult> Deleteadparametros_complementarias(string conexionName, int codigo)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.adparametros_complementarias == null)
                    {
                        return Problem("Entidad adparametros_complementarias es null.");
                    }
                    var adparametros_complementarias = await _context.adparametros_complementarias.FindAsync(codigo);
                    if (adparametros_complementarias == null)
                    {
                        return NotFound("No existe un registro con ese código");
                    }

                    _context.adparametros_complementarias.Remove(adparametros_complementarias);
                    await _context.SaveChangesAsync();

                    return Ok("Datos eliminados con exito");
                }
                return BadRequest("Se perdio la conexion con el servidor");

            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }

        private bool adparametros_complementariasExists(int codigo)
        {
            return (_context.adparametros_complementarias?.Any(e => e.codigo == codigo)).GetValueOrDefault();

        }

    }








    [Route("api/seg_adm/mant/adparametrosDiasextranc/[controller]")]
    [ApiController]
    public class adparametros_diasextrancController : ControllerBase
    {
        private readonly DBContext _context;
        private readonly string connectionString;
        private VerificaConexion verificador;
        private readonly IConfiguration _configuration;
        public adparametros_diasextrancController(IConfiguration configuration)
        {
            connectionString = ConnectionController.ConnectionString;
            _context = DbContextFactory.Create(connectionString);
            _configuration = configuration;
            verificador = new VerificaConexion(_configuration);
        }

        // GET: api/adparametros_diasextranc
        [HttpGet("{conexionName}/{codempresa}")]
        public async Task<ActionResult<IEnumerable<adparametros_diasextranc>>> Getadparametros_diasextranc(string conexionName, string codempresa)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.adparametros_diasextranc == null)
                    {
                        return Problem("Entidad adparametros_diasextranc es null.");
                    }
                    //var result = await _context.adparametros_diasextranc.OrderByDescending(fechareg => fechareg.fechareg).ToListAsync();

                    var result = await _context.adparametros_diasextranc
                        .Where(e => e.codempresa == codempresa)
                        .OrderBy(e => e.dias)
                        .ToListAsync();
                    if (result.Count() == 0)
                    {
                        return NotFound("No se encontro un registro con los datos proporcionados.");
                    }
                    return Ok(result);
                }
                return BadRequest("Se perdio la conexion con el servidor");
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }


        }

        

        // POST: api/adparametros_diasextranc
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost("{conexionName}")]
        public async Task<ActionResult<adparametros_diasextranc>> Postadparametros_diasextranc(string conexionName, adparametros_diasextranc adparametros_diasextranc)
        {
            if (verificador.VerConnection(conexionName, connectionString))
            {
                if (_context.adparametros_diasextranc == null)
                {
                    return Problem("Entidad adparametros_diasextranc es null.");
                }
                _context.adparametros_diasextranc.Add(adparametros_diasextranc);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (adparametros_diasextrancExists(adparametros_diasextranc.codigo))
                    {
                        return Conflict("Ya existe un registro con ese código");
                    }
                    else
                    {
                        throw;
                    }
                }

                return Ok("Registrado con Exito :D");

            }
            return BadRequest("Se perdio la conexion con el servidor");
        }

        // DELETE: api/adparametros_diasextranc/5
        [HttpDelete("{conexionName}/{codigo}")]
        public async Task<IActionResult> Deleteadparametros_diasextranc(string conexionName, int codigo)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.adparametros_diasextranc == null)
                    {
                        return Problem("Entidad adparametros_diasextranc es null.");
                    }
                    var adparametros_diasextranc = await _context.adparametros_diasextranc.FindAsync(codigo);
                    if (adparametros_diasextranc == null)
                    {
                        return NotFound("No existe un registro con ese código");
                    }

                    _context.adparametros_diasextranc.Remove(adparametros_diasextranc);
                    await _context.SaveChangesAsync();

                    return Ok("Datos eliminados con exito");
                }
                return BadRequest("Se perdio la conexion con el servidor");

            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }

        private bool adparametros_diasextrancExists(int codigo)
        {
            return (_context.adparametros_diasextranc?.Any(e => e.codigo == codigo)).GetValueOrDefault();

        }

    }




    [Route("api/seg_adm/mant/adparametrosTarifasfact/[controller]")]
    [ApiController]
    public class adparametros_tarifasfactController : ControllerBase
    {
        private readonly DBContext _context;
        private readonly string connectionString;
        private VerificaConexion verificador;
        private readonly IConfiguration _configuration;
        public adparametros_tarifasfactController(IConfiguration configuration)
        {
            connectionString = ConnectionController.ConnectionString;
            _context = DbContextFactory.Create(connectionString);
            _configuration = configuration;
            verificador = new VerificaConexion(_configuration);
        }

        // GET: api/adparametros_tarifasfact
        [HttpGet("{conexionName}/{codempresa}")]
        public async Task<ActionResult<IEnumerable<adparametros_tarifasfact>>> Getadparametros_tarifasfact(string conexionName, string codempresa)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.adparametros_tarifasfact == null)
                    {
                        return Problem("Entidad adparametros_tarifasfact es null.");
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
                        return NotFound("No se encontro un registro con los datos proporcionados.");
                    }

                    return Ok(result);
                }
                return BadRequest("Se perdio la conexion con el servidor");
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }


        }

        
       
        // POST: api/adparametros_tarifasfact
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost("{conexionName}")]
        public async Task<ActionResult<adparametros_tarifasfact>> Postadparametros_tarifasfact(string conexionName, adparametros_tarifasfact adparametros_tarifasfact)
        {
            if (verificador.VerConnection(conexionName, connectionString))
            {
                if (_context.adparametros_tarifasfact == null)
                {
                    return Problem("Entidad adparametros_tarifasfact es null.");
                }
                _context.adparametros_tarifasfact.Add(adparametros_tarifasfact);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (adparametros_tarifasfactExists(adparametros_tarifasfact.codtarifa))
                    {
                        return Conflict("Ya existe un registro con ese código");
                    }
                    else
                    {
                        throw;
                    }
                }

                return Ok("Registrado con Exito :D");

            }
            return BadRequest("Se perdio la conexion con el servidor");
        }

        // DELETE: api/adparametros_tarifasfact/5
        [HttpDelete("{conexionName}/{codigo}/{codempresa}")]
        public async Task<IActionResult> Deleteadparametros_tarifasfact(string conexionName, int codtarifa, string codempresa)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.adparametros_tarifasfact == null)
                    {
                        return Problem("Entidad adparametros_tarifasfact es null.");
                    }
                    var adparametros_tarifasfact = _context.adparametros_tarifasfact.FirstOrDefault(objeto => objeto.codtarifa == codtarifa && objeto.codempresa == codempresa);
                    if (adparametros_tarifasfact == null)
                    {
                        return NotFound("No existe un registro con ese código");
                    }

                    _context.adparametros_tarifasfact.Remove(adparametros_tarifasfact);
                    await _context.SaveChangesAsync();

                    return Ok("Datos eliminados con exito");
                }
                return BadRequest("Se perdio la conexion con el servidor");

            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }

        private bool adparametros_tarifasfactExists(int codtarifa)
        {
            return (_context.adparametros_tarifasfact?.Any(e => e.codtarifa == codtarifa)).GetValueOrDefault();

        }

    }

}


