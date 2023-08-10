using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIAW.Data;
using SIAW.Models;
using System.Net;
using Microsoft.AspNetCore.Authorization;

namespace SIAW.Controllers.inventarios.mantenimiento
{
    [Route("api/inventario/mant/[controller]")]
    [ApiController]
    public class inmatrizController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        public inmatrizController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }

        // GET: api/inmatriz
        [HttpGet("{userConn}")]
        public async Task<ActionResult<IEnumerable<inmatriz>>> Getinmatriz(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                //var _context = _userConnectionManager.GetUserConnection(userId);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.inmatriz == null)
                    {
                        return Problem("Entidad inmatriz es null.");
                    }
                    var result = await _context.inmatriz.OrderBy(hoja => hoja.hoja).ToListAsync();
                    return Ok(result);
                }
                
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }


        }

        // GET: api/inmatriz/5
        [HttpGet("{userConn}/{hoja}")]
        public async Task<ActionResult<inmatriz>> Getinmatriz(string userConn, string hoja)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                //var _context = _userConnectionManager.GetUserConnection(userId);

                using (var _context = DbContextFactory.Create(userConnectionString))
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
                
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }

        // PUT: api/inmatriz/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPut("{userConn}/{hoja}/{linea}")]
        public async Task<IActionResult> Putinmatriz(string userConn, string hoja, int linea, inmatriz inmatriz)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            //var _context = _userConnectionManager.GetUserConnection(userId);

            using (var _context = DbContextFactory.Create(userConnectionString))
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

                return Ok("206");   // actualizado con exito
            }
            
        }


        // PUT: api/inmatriz/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPut]
        [Route("inmatrizVarios/{userConn}")]
        public async Task<IActionResult> Putinmatriz(string userConn, List<inmatriz> inmatrizList)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            //var _context = _userConnectionManager.GetUserConnection(userId);

            using (var _context = DbContextFactory.Create(userConnectionString))
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

                return Ok("206");   // actualizado con exito
            }
            
        }



        // POST: api/inmatriz
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPost("{userConn}")]
        public async Task<ActionResult<inmatriz>> Postinmatriz(string userConn, inmatriz inmatriz)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            //var _context = _userConnectionManager.GetUserConnection(userId);

            using (var _context = DbContextFactory.Create(userConnectionString))
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

                return Ok("204");   // creado con exito

            }
            
        }

        // DELETE: api/inmatriz/5
        [Authorize]
        [HttpDelete("{userConn}/{hoja}/{linea}")]
        public async Task<IActionResult> Deleteinmatriz(string userConn, string hoja, int linea)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                //var _context = _userConnectionManager.GetUserConnection(userId);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.inmatriz == null)
                    {
                        return Problem("Entidad inmatriz es null.");
                    }
                    var inmatriz = _context.inmatriz.FirstOrDefault(objeto => objeto.hoja == hoja && objeto.linea == linea);
                    if (inmatriz == null)
                    {
                        return NotFound("No existe un registro con ese código");
                    }

                    _context.inmatriz.Remove(inmatriz);
                    await _context.SaveChangesAsync();

                    return Ok("208");   // eliminado con exito
                }
                

            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }

    }
}
