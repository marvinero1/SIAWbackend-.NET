using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using siaw_DBContext.Data;
using siaw_DBContext.Models;
using System.Net;
using Microsoft.AspNetCore.Authorization;
using siaw_funciones;

namespace SIAW.Controllers.inventarios.mantenimiento
{
    [Route("api/inventario/mant/[controller]")]
    [ApiController]
    public class inmatrizController : ControllerBase
    {
        private readonly Saldos saldos = new Saldos();
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

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.inmatriz == null)
                    {
                        return BadRequest(new { resp = "Entidad inmatriz es null." });
                    }
                    var result = await _context.inmatriz.OrderBy(hoja => hoja.hoja).ToListAsync();
                    return Ok(result);
                }
                
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
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

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.inmatriz == null)
                    {
                        return BadRequest(new { resp = "Entidad inmatriz es null." });
                    }
                    var inmatriz = await _context.inmatriz.Where(objeto => objeto.hoja == hoja).ToListAsync();

                    if (inmatriz.Count() == 0)
                    {
                        return NotFound( new { resp = "No se encontro un registro con este código" });
                    }

                    return Ok(inmatriz);
                }
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }


        // GET: api/inmatriz/5
        [HttpGet]
        [Route("infoItemRes/{userConn}/{codalmacen}/{coditem}")]
        public async Task<ActionResult<object>> infoItemRes(string userConn, int codalmacen, string coditem)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    float porcen_maximo = await saldos.get_Porcentaje_Maximo_de_Venta_Respecto_Del_Saldo(_context, codalmacen, coditem);
                    string porcen_maximo_text = "";
                    if (porcen_maximo >= 100)
                    {
                        porcen_maximo_text = "NO RESERVA SALDO";
                    }
                    else
                    {
                        porcen_maximo_text = "VTA HASTA:" + porcen_maximo + "% DEL SALDO";
                    }
                    var initem = await _context.initem
                        .Where(i => i.codigo == coditem)
                        .Select(i => new
                        {
                            codigo = i.codigo,
                            descripcion = i.descripcion,
                            medida = i.medida,
                            porcen_maximo = porcen_maximo_text
                        })
                        .FirstOrDefaultAsync();

                    if (initem == null)
                    {
                        return NotFound( new { resp = "No se encontro un registro con este código" });
                    }

                    return Ok(initem);
                }
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
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

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                var matriz = _context.inmatriz.FirstOrDefault(objeto => objeto.hoja == hoja && objeto.linea == linea);

                if (matriz == null)
                {
                    return NotFound( new { resp = "No existe un registro con esa información" });
                }

                _context.Entry(inmatriz).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    return Problem("Error en el servidor");
                }

                return Ok( new { resp = "206" });   // actualizado con exito
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

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                foreach (var inmatriz in inmatrizList)
                {
                    var matriz = _context.inmatriz.FirstOrDefault(objeto => objeto.hoja == inmatriz.hoja && objeto.linea == inmatriz.linea);
                    if (matriz == null)
                    {
                        return BadRequest(new { resp = "Entidad inmatriz es null." });
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
                    return Problem("Error en el servidor");
                }

                return Ok( new { resp = "206" });   // actualizado con exito
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

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (_context.inmatriz == null)
                {
                    return BadRequest(new { resp = "Entidad inmatriz es null." });
                }
                _context.inmatriz.Add(inmatriz);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    return Problem("Error en el servidor");
                }

                return Ok( new { resp = "204" });   // creado con exito

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

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.inmatriz == null)
                    {
                        return BadRequest(new { resp = "Entidad inmatriz es null." });
                    }
                    var inmatriz = _context.inmatriz.FirstOrDefault(objeto => objeto.hoja == hoja && objeto.linea == linea);
                    if (inmatriz == null)
                    {
                        return NotFound( new { resp = "No existe un registro con ese código" });
                    }

                    _context.inmatriz.Remove(inmatriz);
                    await _context.SaveChangesAsync();

                    return Ok( new { resp = "208" });   // eliminado con exito
                }
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }

    }
}
