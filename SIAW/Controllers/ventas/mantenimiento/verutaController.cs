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
    public class verutaController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        public verutaController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }

        // GET: api/veruta
        [HttpGet("{userConn}")]
        public async Task<ActionResult<IEnumerable<veruta>>> Getveruta(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.veruta == null)
                    {
                        return BadRequest(new { resp = "Entidad veruta es null." });
                    }
                    var result = await _context.veruta
                        .Join(
                        _context.vevendedor,
                        vr => vr.codvendedor,
                        vv => vv.codigo,
                        (vr, vv) => new {vr, vv}
                        )
                        .Join(
                        _context.pepersona,
                        x => x.vv.codpersona,
                        pp => pp.codigo,
                        (x, pp) => new 
                            {
                                codigo = x.vr.codigo, 
                                codvendedor = x.vr.codvendedor,
                                nomvendedor = pp.nombre1 + " " + pp.apellido1,
                                semana = x.vr.semana,
                                dia = x.vr.dia,
                                descripcion = x.vr.descripcion,
                                genera_hoja_de_ruta = x.vr.genera_hoja_de_ruta,
                                fecha = x.vr.fecha
                            }
                        )
                        .OrderBy(codigo => codigo.codigo)
                        .ToListAsync();
                    return Ok(result);
                }

            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }


        }

        // GET: api/veruta/5
        [HttpGet("{userConn}/{codigo}")]
        public async Task<ActionResult<veruta>> Getveruta(string userConn, string codigo)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.veruta == null)
                    {
                        return BadRequest(new { resp = "Entidad veruta es null." });
                    }
                    var veruta = await _context.veruta
                        .Where(v => v.codigo == codigo)
                        .Join(
                        _context.vevendedor,
                        vr => vr.codvendedor,
                        vv => vv.codigo,
                        (vr, vv) => new { vr, vv }
                        )
                        .Join(
                        _context.pepersona,
                        x => x.vv.codpersona,
                        pp => pp.codigo,
                        (x, pp) => new
                        {
                            codigo = x.vr.codigo,
                            codvendedor = x.vr.codvendedor,
                            nomvendedor = pp.nombre1 + " " + pp.apellido1,
                            semana = x.vr.semana,
                            dia = x.vr.dia,
                            descripcion = x.vr.descripcion,
                            genera_hoja_de_ruta = x.vr.genera_hoja_de_ruta,
                            fecha = x.vr.fecha
                        }
                        )
                        .FirstOrDefaultAsync();

                    if (veruta == null)
                    {
                        return NotFound( new { resp = "No se encontro un registro con este código" });
                    }
                    return Ok(veruta);
                }
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }

        // GET: api/catalogo
        [HttpGet]
        [Route("catalogo/{userConn}")]
        public async Task<ActionResult<IEnumerable<veruta>>> Getveruta_catalogo(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var query = _context.veruta
                    .OrderBy(i => i.codigo)
                    .Select(i => new
                    {
                        i.codigo,
                        i.descripcion
                    });
                    var result = query.ToList();

                    if (result.Count() == 0)
                    {
                        return BadRequest(new { resp = "Entidad veruta es null." });
                    }
                    return Ok(result);
                }
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
                throw;
            }
        }

        // PUT: api/veruta/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPut("{userConn}/{codigo}")]
        public async Task<IActionResult> Putveruta(string userConn, string codigo, veruta veruta)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (codigo != veruta.codigo)
                {
                    return BadRequest( new { resp = "Error con Id en datos proporcionados." });
                }
                if (!verutaExists(codigo, _context))
                {
                    return NotFound( new { resp = "No existe un registro con ese código" });
                }
                _context.Entry(veruta).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    return Problem("Error en el servidor");
                    throw;

                }
                return Ok( new { resp = "206" });   // actualizado con exito
            }
        }

        // POST: api/veruta
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPost("{userConn}")]
        public async Task<ActionResult<veruta>> Postveruta(string userConn, veruta veruta)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (_context.veruta == null)
                {
                    return BadRequest(new { resp = "Entidad veruta es null." });
                }
                if (verutaExists(veruta.codigo, _context))
                {
                    return Conflict( new { resp = "Ya existe un registro con ese código" });
                }
                _context.veruta.Add(veruta);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    return Problem("Error en el servidor");
                    throw;
                }
                return Ok( new { resp = "204" });   // creado con exito
            }
        }

        // DELETE: api/veruta/5
        [Authorize]
        [HttpDelete("{userConn}/{codigo}")]
        public async Task<IActionResult> Deleteveruta(string userConn, string codigo)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.veruta == null)
                    {
                        return BadRequest(new { resp = "Entidad veruta es null." });
                    }
                    var veruta = await _context.veruta.FindAsync(codigo);
                    if (veruta == null)
                    {
                        return NotFound( new { resp = "No existe un registro con ese código" });
                    }
                    _context.veruta.Remove(veruta);
                    await _context.SaveChangesAsync();
                    return Ok( new { resp = "208" });   // eliminado con exito
                }
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }

        private bool verutaExists(string codigo, DBContext _context)
        {
            return (_context.veruta?.Any(e => e.codigo == codigo)).GetValueOrDefault();

        }
    }

















    [Route("api/venta/mant/[controller]")]
    [ApiController]
    public class veruta_clienteController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        private readonly HojadeRuta hojadeRuta = new HojadeRuta();
        public veruta_clienteController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }

        // GET: api/veruta_cliente
        [HttpGet]
        [Route("clientEnRuta/{userConn}/{codruta}")]
        public async Task<ActionResult<IEnumerable<veruta_cliente>>> Getveruta_cliente(string userConn, string codruta)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var result = await _context.veruta_cliente
                        .Join(
                            _context.vecliente,
                            r => r.codcliente,
                            c => c.codigo,
                            (r, c) => new
                            {
                                codruta = r.codruta,
                                orden = r.orden,
                                codcliente = r.codcliente,
                                razonsocial = c.razonsocial,
                                obs = r.obs,
                                tpo_atencion = r.tpo_atencion,
                                tpo_total = r.tpo_total,
                                tipo_horario = r.tipo_horario,
                                direccion = r.direccion
                            }
                        )
                        .Where(x => x.codruta == codruta)
                        .OrderBy(x => x.orden)
                        .ToListAsync();
                    return Ok(result);
                }

            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }


        }



        // POST: api/veruta_cliente
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPost("{userConn}/{codruta}/{codvendedor}")]
        public async Task<ActionResult<veruta_cliente>> Postveruta_cliente(string userConn, string codruta, int codvendedor, veruta_cliente veruta_cliente)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                try
                {
                    var veruta_clienteExists = await _context.veruta_cliente
                    .Where(x => x.codruta == veruta_cliente.codruta && x.codcliente == veruta_cliente.codruta)
                    .FirstOrDefaultAsync();
                    if (veruta_clienteExists != null)
                    {
                        return Conflict(new { resp = "Ya existe un registro con ese código de ruta y código de cliente juntos" });
                    }
                    _context.veruta_cliente.Add(veruta_cliente);
                    await _context.SaveChangesAsync();



                    // primero borrar de las hojas de rutas
                    DateTime fechaMananaLocal = DateTime.Now.AddDays(1);
                    string fechaFormateada = fechaMananaLocal.ToString("yyyy-MM-dd");

                    var codHojasDeRutaAEliminar = _context.vehojaderuta
                        .Where(h => h.codruta == codruta && h.codvendedor == codvendedor && h.fecha >= fechaMananaLocal)
                        .Select(h => h.codigo)
                        .ToList();

                    var clientesAEliminar = _context.vehojaderuta_cliente
                        .Where(vc => vc.codcliente == veruta_cliente.codcliente && codHojasDeRutaAEliminar.Contains((int)vc.codhojaderuta))
                        .ToList();

                    _context.vehojaderuta_cliente.RemoveRange(clientesAEliminar);
                    _context.SaveChanges();

                    // modificar las hojas de ruta

                    var Rutas_Del_Cliente = await _context.veruta
                        .Join(_context.veruta_cliente,
                              p1 => p1.codigo,
                              p2 => p2.codruta,
                              (p1, p2) => new { p1, p2 })
                        .Where(x => x.p2.codcliente == veruta_cliente.codcliente)
                        .OrderBy(x => x.p2.codruta)
                        .Select(x => new { x.p2.codruta, x.p2.codcliente })
                        .Distinct()
                        .ToListAsync();



                    return Ok( new { resp = "204" });   // creado con exito
                }
                catch (Exception)
                {
                    return Problem("Error en el servidor");
                    throw;
                }
                
            }
        }

        private async Task<bool> generar_ruta_x_periodo_cliente(DBContext _context, string codcliente, int codvendedor)
        {
            DateTime mifecha_inicio = DateTime.Now.AddDays(1);
            DateTime mifecha_fin = DateTime.Now.AddDays(1);

            
            return true;

        }


        private async Task<bool> adicionar_cliente_a_ruta(DBContext _context, DateTime fecha, string codcliente, int codvendedor)
        {
            int codalmacen = 0;
            int CODIGO_CABECERA = 0;
            string COD_HOJA_DE_RUTA = "";
            
            if (await hojadeRuta.ExisteRutaAsignada(_context, fecha, codvendedor))
            {
                COD_HOJA_DE_RUTA = await hojadeRuta.CodHoja_de_Ruta_Generada(_context,fecha,codvendedor);

            }
            return true;

        }


        // DELETE: api/veruta_cliente/5
        [Authorize]
        [HttpDelete("{userConn}/{codigo}")]
        public async Task<IActionResult> Deleteveruta_cliente(string userConn, string codigo)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.veruta_cliente == null)
                    {
                        return BadRequest(new { resp = "Entidad veruta_cliente es null." });
                    }
                    var veruta_cliente = await _context.veruta_cliente.FindAsync(codigo);
                    if (veruta_cliente == null)
                    {
                        return NotFound( new { resp = "No existe un registro con ese código" });
                    }
                    _context.veruta_cliente.Remove(veruta_cliente);
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
