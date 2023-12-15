using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using siaw_DBContext.Data;
using siaw_DBContext.Models;
using Microsoft.AspNetCore.Authorization;
using System.Web.Http.Results;

namespace SIAW.Controllers.ventas.mantenimiento
{
    [Route("api/venta/mant/[controller]")]
    [ApiController]
    public class veclientesigualesController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        public veclientesigualesController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }

        // GET: api/veclientesiguales
        [HttpGet("{userConn}")]
        public async Task<ActionResult<IEnumerable<veclientesiguales>>> Getveclientesiguales(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                //var _context = _userConnectionManager.GetUserConnection(userId);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var resultado = await _context.veclientesiguales
                        .Join(
                            _context.vecliente,
                            i => i.codcliente_a,
                            c1 => c1.codigo,
                            (i, c1) => new { i, c1 }
                        )
                        .Join(
                            _context.vecliente,
                            x => x.i.codcliente_b,
                            c2 => c2.codigo,
                            (x, c2) => new
                            {
                                x.i.codcliente_a,
                                razonsocial_a = x.c1.razonsocial,
                                x.i.codcliente_b,
                                razonsocial_b = c2.razonsocial
                            }
                        )
                        .OrderBy(x => x.codcliente_a)
                        .ThenBy(x => x.codcliente_b)
                    .ToListAsync();

                    if (resultado.Count() == 0)
                    {
                        return Problem("Entidad veclientesiguales es null.");
                    }
                    //return Ok(new { contador = resultado.Count(), resp = resultado });
                    return Ok(resultado);
                }
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
                throw;
            }
        }



        // POST: api/veclientesiguales
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPost("{userConn}")]
        public async Task<ActionResult<veclientesiguales>> Postveclientesiguales(string userConn, veclientesiguales veclientesiguales)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            //var _context = _userConnectionManager.GetUserConnection(userId);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                var consulta = await _context.veclientesiguales
                        .Where(i => i.codcliente_a == veclientesiguales.codcliente_a && i.codcliente_b == veclientesiguales.codcliente_b)
                        .FirstOrDefaultAsync();

                if( consulta != null )
                {
                    return Conflict(new { resp = "Esos clientes ya estan registrados" });
                }


                //validar el almacen de codcliente_a
                var valida1 = await validaAlmacen(_context, veclientesiguales.codcliente_a, (int)veclientesiguales.codalmacen);
                if( valida1 != null)
                {
                    if (!valida1.bandera)
                    {
                        return Conflict(new { resp = "El almacen del cliente A: " + valida1.codigo + " es: " + valida1.almacen + " verifique los datos." });
                    }
                }


                //validar el almacen de codcliente_b
                var valida2 = await validaAlmacen(_context, veclientesiguales.codcliente_b, (int)veclientesiguales.codalmacen);
                if (valida2 != null)
                {
                    if (!valida2.bandera)
                    {
                        return Conflict(new { resp = "El almacen del cliente B: " + valida2.codigo + " es: " + valida2.almacen + " verifique los datos." });
                    }
                }


                _context.veclientesiguales.Add(veclientesiguales);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    return Problem("Error en el servidor");
                }
                return Ok("204");   // creado con exito
            }
        }


        private async Task<datacliIgual?> validaAlmacen(DBContext _context, string codigo, int almacen)
        {
            var resultado = await _context.vecliente
                .Join(
                    _context.vevendedor,
                    p1 => p1.codvendedor,
                    p2 => p2.codigo,
                    (p1, p2) => new datacliIgual
                    {
                        codigo = p1.codigo,
                        codvendedor = p1.codvendedor,
                        almacen = p2.almacen,
                        bandera = false
                    }
                )
                .Where(x => x.codigo == codigo)
                .FirstOrDefaultAsync();
            if (resultado == null)
            {
                return null;
            }
            if (resultado.almacen != almacen)
            {
                return resultado;
            }
            resultado.bandera = true;
            return resultado;
        }

        // DELETE: api/veclientesiguales/5
        [Authorize]
        [HttpDelete("{userConn}/{coda}/{codb}")]
        public async Task<IActionResult> Deleteveclientesiguales(string userConn, string coda, string codb)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.veclientesiguales == null)
                    {
                        return Problem("Entidad veclientesiguales es null.");
                    }

                    var veclientesiguales = await _context.veclientesiguales
                        .Where(i => i.codcliente_a == coda && i.codcliente_b == codb)
                        .FirstOrDefaultAsync();


                    if (veclientesiguales == null)
                    {
                        return NotFound("No existe un registro con los datos proporcionados");
                    }

                    _context.veclientesiguales.Remove(veclientesiguales);
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

    public class datacliIgual
    {
        public string codigo { get; set; }
        public int codvendedor { get; set; }
        public int almacen { get; set; }
        public bool bandera { get; set; }
    }

}
