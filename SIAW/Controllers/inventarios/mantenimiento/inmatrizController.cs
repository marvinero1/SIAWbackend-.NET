using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIAW.Data;
using SIAW.Models;
using System.Net;

namespace SIAW.Controllers.inventarios.mantenimiento
{
    [Route("api/inventario/mant/inmatriz/[controller]")]
    [ApiController]
    public class inmatrizController : ControllerBase
    {
        private readonly DBContext _context;
        private readonly string connectionString;
        private VerificaConexion verificador;
        private readonly IConfiguration _configuration;
        public inmatrizController(IConfiguration configuration)
        {
            connectionString = ConnectionController.ConnectionString;
            _context = DbContextFactory.Create(connectionString);
            _configuration = configuration;
            verificador = new VerificaConexion(_configuration);
        }

        // GET: api/inmatriz
        [HttpGet("{conexionName}")]
        public async Task<ActionResult<IEnumerable<inmatriz>>> Getinmatriz(string conexionName)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.inmatriz == null)
                    {
                        return Problem("Entidad inmatriz es null.");
                    }
                    var result = await _context.inmatriz.OrderBy(hoja => hoja.hoja).ToListAsync();
                    return Ok(result);
                }
                return BadRequest("Se perdio la conexion con el servidor");
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }


        }

        // GET: api/inmatriz/5
        [HttpGet("{conexionName}/{hoja}")]
        public async Task<ActionResult<inmatriz>> Getinmatriz(string conexionName, string hoja)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.inmatriz == null)
                    {
                        return Problem("Entidad inmatriz es null.");
                    }
                    var inmatriz = _context.inmatriz.Where(objeto => objeto.hoja == hoja);

                    if (inmatriz.Count() == 0)
                    {
                        return NotFound("No se encontro un registro con este código");
                    }

                    return Ok(inmatriz);
                }
                return BadRequest("Se perdio la conexion con el servidor");
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }

        // PUT: api/inmatriz/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{conexionName}/{hoja}/{linea}")]
        public async Task<IActionResult> Putinmatriz(string conexionName, string hoja, int linea, inmatriz inmatriz)
        {
            if (verificador.VerConnection(conexionName, connectionString))
            {
                var matriz = _context.inmatriz.FirstOrDefault(objeto => objeto.hoja == hoja && objeto.linea == linea);

                if (matriz == null)
                {
                    return NotFound("No existe un registro con esa información");
                }

                _context.Entry(inmatriz).State = EntityState.Modified;

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


        // PUT: api/inmatriz/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{conexionName}")]
        [Route("inmatrizVarios")]
        public async Task<IActionResult> Putinmatriz(string conexionName, List<inmatriz> inmatrizList)
        {
            if (verificador.VerConnection(conexionName, connectionString))
            {
                foreach (var inmatriz in inmatrizList)
                {
                    var matriz = _context.inmatriz.FirstOrDefault(objeto => objeto.hoja == inmatriz.hoja && objeto.linea == inmatriz.linea);
                    if (matriz == null)
                    {
                        return NotFound("No existe un registro");
                    }
                    // Actualiza las propiedades que necesites
                    matriz.A = inmatriz.A;
                    matriz.B = inmatriz.B;
                    matriz.C = inmatriz.C;
                    matriz.D = inmatriz.D;
                    matriz.E = inmatriz.E;
                    matriz.F = inmatriz.F;
                    matriz.G = inmatriz.G;
                    matriz.H = inmatriz.H;
                    matriz.I = inmatriz.I;
                    matriz.J = inmatriz.J;
                    matriz.K = inmatriz.K;
                    matriz.L = inmatriz.L;
                    matriz.M = inmatriz.M;

                    _context.Entry(matriz).State = EntityState.Modified;
                }

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



        // POST: api/inmatriz
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost("{conexionName}")]
        public async Task<ActionResult<inmatriz>> Postinmatriz(string conexionName, inmatriz inmatriz)
        {
            if (verificador.VerConnection(conexionName, connectionString))
            {
                if (_context.inmatriz == null)
                {
                    return Problem("Entidad inmatriz es null.");
                }
                _context.inmatriz.Add(inmatriz);
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

        // DELETE: api/inmatriz/5
        [HttpDelete("{conexionName}/{hoja}/{linea}")]
        public async Task<IActionResult> Deleteinmatriz(string conexionName, string hoja, int linea)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.inmatriz == null)
                    {
                        return Problem("Entidad inmatriz es null.");
                    }
                    inmatriz inmatriz = _context.inmatriz.FirstOrDefault(objeto => objeto.hoja == hoja && objeto.linea == linea);
                    if (inmatriz == null)
                    {
                        return NotFound("No existe un registro con ese código");
                    }

                    _context.inmatriz.Remove(inmatriz);
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
