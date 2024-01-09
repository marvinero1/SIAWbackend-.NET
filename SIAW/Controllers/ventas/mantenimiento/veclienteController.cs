using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using siaw_DBContext.Data;
using siaw_DBContext.Models;
using Microsoft.AspNetCore.Authorization;
using siaw_funciones;

namespace SIAW.Controllers.ventas.mantenimiento
{
    [Route("api/venta/mant/[controller]")]
    [ApiController]
    public class veclienteController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        private readonly Cliente cliente = new Cliente();
        public veclienteController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }


        // GET: api/vecliente/5
        [HttpGet("{userConn}/{codigo}")]
        public async Task<ActionResult<vecliente>> Getvecliente(string userConn, string codigo)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.vecliente == null)
                    {
                        return BadRequest(new { resp = "Entidad vecliente es null." });
                    }
                    //var vecliente = await _context.vecliente.FindAsync(codigo);
                    var vecliente = await _context.vecliente
                        .Join(_context.vetienda, c => c.codigo, v => v.codcliente, (cliente, vivienda) => new { Cliente = cliente, Vivienda = vivienda })
                        .Where(joined => joined.Cliente.codigo == codigo && joined.Vivienda.central == true)
                        .FirstOrDefaultAsync();

                    if (vecliente == null)
                    {
                        return NotFound( new { resp = "No se encontro un registro con este código" });
                    }

                    return Ok(vecliente);
                }
                
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }




        // GET: api/vecliente/5
        [HttpGet]
        [Route("getTipoSegunClientesIguales/{userConn}/{codcliente}")]
        public async Task<ActionResult<vecliente>> getTipoSegunClientesIguales(string userConn, string codcliente)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                string resultado = await cliente.TipoSegunClientesIguales(userConnectionString, codcliente);

                return Ok(new { resultado = resultado});

            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }



        /// <summary>
        /// Obtiene algunos datos de todos los registros de la tabla vecliente para catalogo
        /// </summary>
        /// <param name="userConn"></param>
        /// <returns></returns>
        // GET: api/vecliente/5
        [HttpGet]
        [Route("catalogo/{userConn}")]
        public async Task<ActionResult<vecliente>> Getvecliente_catalogo(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.vecliente == null)
                    {
                        return BadRequest(new { resp = "Entidad vecliente es null." });
                    }
                    //var vecliente = await _context.vecliente.FindAsync(codigo);
                    var query = _context.vecliente
                    .OrderBy(c => c.codigo)
                    .ToList() // Cargamos todos los registros en memoria
                    //.Where(c => IsNumeric(c.codigo)) // Filtramos en memoria
                    .Select(c => new
                    {
                        codigo=c.codigo,
                        nombre=c.nombre_comercial+" - "+c.razonsocial,
                        nit=c.nit,
                        habilitado = c.habilitado,
                        codvendedor = c.codvendedor,
                        nombre_comercial = c.nombre_comercial,
                        direccion_titular = c.direccion_titular
                    });

                    var result = query.ToList();

                    if (result == null)
                    {
                        return NotFound( new { resp = "No se encontro un registro con este código" });
                    }

                    return Ok(result);
                }
                
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }




        // GET: api/vecliente/5
        [HttpGet]
        [Route("mostrar_rutas/{userConn}/{codcliente}")]
        public async Task<ActionResult<vecliente>> mostrar_rutas(string userConn, string codcliente)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var resultado = await _context.veruta
                    .Join(_context.veruta_cliente, p1 => p1.codigo, p2 => p2.codruta, (p1, p2) => new { p1, p2 })
                    .Where(joinResult => joinResult.p2.codcliente == "300012")
                    .OrderBy(joinResult => joinResult.p1.semana)
                    .ThenBy(joinResult => joinResult.p1.dia)
                    .Select(joinResult => new
                    {
                        codruta = joinResult.p2.codruta,
                        descruta = joinResult.p1.descripcion,
                        joinResult.p1.semana,
                        joinResult.p1.dia
                    })
                    .ToListAsync();

                    return Ok(resultado);   // actualizado con exito
                }

            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }





        // PUT: api/vecliente/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPut("{userConn}/{codigo}")]
        public async Task<IActionResult> Putvecliente(string userConn, string codigo, vecliente vecliente)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            //var _context = _userConnectionManager.GetUserConnection(userId);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (codigo != vecliente.codigo)
                {
                    return BadRequest( new { resp = "Error con Id en datos proporcionados." });
                }

                _context.Entry(vecliente).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!veclienteExists(codigo, _context))
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



        // PUT: api/vecliente/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPut]
        [Route("clienteCasual/{userConn}/{codigo}/{casual}")]
        public async Task<IActionResult> PutClienteCasual(string userConn, string codigo, bool casual)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                var vecliente = await _context.vecliente.Where(i => i.codigo == codigo).FirstOrDefaultAsync();
                if (vecliente == null)
                {
                    return NotFound( new { resp = "No existe un registro con ese código" });
                }
                vecliente.casual = casual;
                _context.Entry(vecliente).State = EntityState.Modified;

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



        // POST: api/vecliente
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPost("{userConn}")]
        public async Task<ActionResult<vecliente>> Postvecliente(string userConn, vecliente vecliente)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (_context.vecliente == null)
                {
                    return BadRequest(new { resp = "Entidad vecliente es null." });
                }
                _context.vecliente.Add(vecliente);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (veclienteExists(vecliente.codigo, _context))
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

        // DELETE: api/vecliente/5
        [Authorize]
        [HttpDelete("{userConn}/{codigo}")]
        public async Task<IActionResult> Deletevecliente(string userConn, string codigo)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.vecliente == null)
                    {
                        return BadRequest(new { resp = "Entidad vecliente es null." });
                    }
                    var vecliente = await _context.vecliente.FindAsync(codigo);
                    if (vecliente == null)
                    {
                        return NotFound( new { resp = "No existe un registro con ese código" });
                    }

                    _context.vecliente.Remove(vecliente);
                    await _context.SaveChangesAsync();

                    return Ok( new { resp = "208" });   // eliminado con exito
                }
                
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }

        private bool veclienteExists(string codigo, DBContext _context)
        {
            return (_context.vecliente?.Any(e => e.codigo == codigo)).GetValueOrDefault();
        }
        private static bool IsNumeric(string input)
        {
            return int.TryParse(input, out _);
        }
    }
}
