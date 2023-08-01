using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIAW.Data;
using SIAW.Models;
using System.Net;
using Microsoft.AspNetCore.Authorization;

namespace SIAW.Controllers.ventas.mantenimiento
{
    [Authorize]
    [Route("api/venta/mant/[controller]")]
    [ApiController]
    public class vedesitemController : ControllerBase
    {
        private readonly DBContext _context;
        private readonly string connectionString;
        private VerificaConexion verificador;
        private readonly IConfiguration _configuration;
        public vedesitemController(IConfiguration configuration)
        {
            connectionString = ConnectionController.ConnectionString;
            _context = DbContextFactory.Create(connectionString);
            _configuration = configuration;
            verificador = new VerificaConexion(_configuration);
        }

        /// <summary>
        /// Obtiene todos los datos de la tabla vedesitem ordenado por codalmacen y coditem
        /// </summary>
        /// <param name="conexionName"></param>
        /// <returns></returns>
        // GET: api/vedesitem
        [HttpGet("{conexionName}")]
        public async Task<ActionResult<IEnumerable<vedesitem>>> Getvedesitem(string conexionName)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.vedesitem == null)
                    {
                        return Problem("Entidad vedesitem es null.");
                    }
                    var result = await _context.vedesitem.OrderBy(codalmacen => codalmacen.codalmacen).ThenBy(coditem => coditem.coditem).ToListAsync();
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
        /// Obtiene todos los datos de un registro de la tabla vedesitem
        /// </summary>
        /// <param name="conexionName"></param>
        /// <param name="codalmacen"></param>
        /// <param name="coditem"></param>
        /// <param name="nivel"></param>
        /// <returns></returns>
        // GET: api/vedesitem/5
        [HttpGet("{conexionName}/{codalmacen}/{coditem}/{nivel}")]
        public async Task<ActionResult<vedesitem>> Getvedesitem(string conexionName, int codalmacen, string coditem, string nivel)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.vedesitem == null)
                    {
                        return Problem("Entidad vedesitem es null.");
                    }
                    var vedesitem = _context.vedesitem.FirstOrDefault(objeto => objeto.codalmacen == codalmacen && objeto.coditem == coditem && objeto.nivel == nivel);

                    if (vedesitem == null)
                    {
                        return NotFound("No se encontro un registro con este código");
                    }

                    return Ok(vedesitem);
                }
                return BadRequest("Se perdio la conexion con el servidor");
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }



        // PUT: api/vedesitem/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{conexionName}/{codalmacen}/{coditem}/{nivel}")]
        public async Task<IActionResult> Putvedesitem(string conexionName, int codalmacen, string coditem, string nivel, vedesitem vedesitem)
        {
            if (verificador.VerConnection(conexionName, connectionString))
            {
                var desitem = _context.vedesitem.FirstOrDefault(objeto => objeto.codalmacen == codalmacen && objeto.coditem == coditem && objeto.nivel == nivel);
                if (desitem == null)
                {
                    return NotFound("No existe un registro con esa información");
                }

                _context.Entry(desitem).State = EntityState.Modified;

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

        // POST: api/vedesitem
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost("{conexionName}")]
        public async Task<ActionResult<vedesitem>> Postvedesitem(string conexionName, vedesitem vedesitem)
        {
            if (verificador.VerConnection(conexionName, connectionString))
            {
                if (_context.vedesitem == null)
                {
                    return Problem("Entidad vedesitem es null.");
                }
                _context.vedesitem.Add(vedesitem);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    return BadRequest("Existen problemas con el Servidor.");
                }

                return Ok("Registrado con Exito :D");

            }
            return BadRequest("Se perdio la conexion con el servidor");
        }

        // DELETE: api/vedesitem/5
        [HttpDelete("{conexionName}/{codalmacen}/{coditem}/{nivel}")]
        public async Task<IActionResult> Deletevedesitem(string conexionName, int codalmacen, string coditem, string nivel)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.vedesitem == null)
                    {
                        return Problem("Entidad vedesitem es null.");
                    }
                    var vedesitem = _context.vedesitem.FirstOrDefault(objeto => objeto.codalmacen == codalmacen && objeto.coditem == coditem && objeto.nivel == nivel);
                    if (vedesitem == null)
                    {
                        return NotFound("No existe un registro con ese código");
                    }

                    _context.vedesitem.Remove(vedesitem);
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
