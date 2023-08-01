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
    public class adtipocambioController : ControllerBase
    {
        private readonly DBContext _context;
        private readonly string connectionString;
        private VerificaConexion verificador;
        private readonly IConfiguration _configuration;
        public adtipocambioController(IConfiguration configuration)
        {
            connectionString = ConnectionController.ConnectionString;
            _context = DbContextFactory.Create(connectionString);
            _configuration = configuration;
            verificador = new VerificaConexion(_configuration);
        }

        // GET: api/adtipocambio
        [HttpGet("{conexionName}")]
        public async Task<ActionResult<IEnumerable<adtipocambio>>> Getadtipocambio(string conexionName)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.adtipocambio == null)
                    {
                        return Problem("Entidad adtipocambio es null.");
                    }
                    var result = await _context.adtipocambio.ToListAsync();
                    return Ok(result);
                }
                return BadRequest("Se perdio la conexion con el servidor");
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }


        }

        /// <summary>
        /// Obtiene los registros por fecha
        /// </summary>
        /// <param name="conexionName"></param>
        /// <param name="fecha"></param>
        /// <returns></returns>
        // GET: api/adtipocambio/5
        [HttpGet]
        [Route("getTipocambioFecha/{conexionName}/{fecha}")]
        public async Task<ActionResult<adtipocambio>> Getadtipocambio(string conexionName, DateTime fecha)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.adtipocambio == null)
                    {
                        return Problem("Entidad adtipocambio es null.");
                    }
                    //var adtipocambio = await _context.adtipocambio.FindAsync(codigo);

                    var adtipocambio = await _context.adtipocambio.Where(x => x.fecha == fecha).OrderByDescending(f => f.fecha).ToListAsync();

                    if (adtipocambio.Count() == 0)
                    {
                        return NotFound("No se encontro un registro con este código");
                    }

                    return Ok(adtipocambio);
                }
                return BadRequest("Se perdio la conexion con el servidor");
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }



        /// <summary>
        /// Obtiene una lista de la tabla adtipocambio, buscando la última fecha con registros
        /// </summary>
        /// <returns></returns>
        // GET: api/adtipocambio/5
        [HttpGet]
        [Route("getTipocambioAnt/{conexionName}")]
        public async Task<ActionResult<adtipocambio>> GetTipocambAnt(string conexionName)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.adtipocambio == null)
                    {
                        return Problem("Entidad adtipocambio es null.");
                    }
                    DateTime fechaActual = DateTime.Now;
                    var f = fechaActual.ToString("yyyy-MM-dd"); //importante
                    DateTime fe = DateTime.Parse(f);  //importante

                    var resultado = _context.adtipocambio.Where(x => x.fecha == fe).ToList();
                    do
                    {
                        fe = fe.AddDays(-1);
                        resultado = _context.adtipocambio.Where(x => x.fecha == fe).ToList();
                    } while (resultado.Count == 0);

                    
                    return Ok(resultado);
                }
                return BadRequest("Se perdio la conexion con el servidor");
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }



        // PUT: api/adtipocambio/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{conexionName}/{moneda}/{fecha}/{codalmacen}")]
        public async Task<IActionResult> Putadtipocambio(string conexionName, string moneda, DateTime fecha, int codalmacen, adtipocambio adtipocambio)
        {
            if (verificador.VerConnection(conexionName, connectionString))
            {
                var tipocambio = _context.adtipocambio.FirstOrDefault(objeto => objeto.moneda == moneda && objeto.fecha == fecha && objeto.codalmacen == codalmacen);
                if (tipocambio == null)
                {
                    return NotFound("No existe un registro con esa información");
                }
                tipocambio.factor = adtipocambio.factor;
                _context.Entry(tipocambio).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    return BadRequest("Existen problemas con el Servidor.");
                }

                return Ok("Datos actualizados correctamente.");
            }
            return BadRequest("Se perdio la conexion con el servidor");


        }

        // POST: api/adtipocambio
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost("{conexionName}")]
        public async Task<ActionResult<adtipocambio>> Postadtipocambio(string conexionName, adtipocambio adtipocambio)
        {
            if (verificador.VerConnection(conexionName, connectionString))
            {
                if (_context.adtipocambio == null)
                {
                    return Problem("Entidad adtipocambio es null.");
                }
                _context.adtipocambio.Add(adtipocambio);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    return BadRequest("Error en el Servidor");
                }

                return Ok("Registrado con Exito :D");

            }
            return BadRequest("Se perdio la conexion con el servidor");
        }




        // POST: api/adtipocambio
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        [Route("addlisttipocambio/{conexionName}")]
        public async Task<ActionResult<adtipocambio>> PostListadtipocambio(string conexionName, List<adtipocambio> adtipocambio)
        {
            if (verificador.VerConnection(conexionName, connectionString))
            {
                if (_context.adtipocambio == null)
                {
                    return Problem("Entidad adtipocambio es null.");
                }
                _context.adtipocambio.AddRange(adtipocambio);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    return BadRequest("Error en el Servidor");
                }

                return Ok("Registrado con Exito :D");

            }
            return BadRequest("Se perdio la conexion con el servidor");
        }



        // DELETE: api/adtipocambio/5
        [HttpDelete("{conexionName}/{moneda}/{fecha}/{codalmacen}")]
        public async Task<IActionResult> Deleteadtipocambio(string conexionName, string moneda, DateTime fecha, int codalmacen)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.adtipocambio == null)
                    {
                        return Problem("Entidad adtipocambio es null.");
                    }
                   
                    adtipocambio adtipocambio = _context.adtipocambio.FirstOrDefault(objeto => objeto.moneda == moneda && objeto.fecha == fecha && objeto.codalmacen == codalmacen);
                    if (adtipocambio == null)
                    {
                        return NotFound("Revise los datos ingresados.");
                    }

                    _context.adtipocambio.Remove(adtipocambio);
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

    }
}
