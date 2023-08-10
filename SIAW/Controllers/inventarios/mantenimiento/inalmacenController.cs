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
    public class inalmacenController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        public inalmacenController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }

        // GET: api/inalmacen
        [HttpGet("{userConn}")]
        public async Task<ActionResult<IEnumerable<inalmacen>>> Getinalmacen(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                //var _context = _userConnectionManager.GetUserConnection(userId);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.inalmacen == null)
                    {
                        return Problem("Entidad inalmacen es null.");
                    }

                    List<inalmacen> linalmacen = _context.inalmacen.ToList();
                    List<adarea> ladarea = _context.adarea.ToList();
                    List<peplanporcen> lpeplanporcen = _context.peplanporcen.ToList();

                    var query = from alm in linalmacen
                                join are in ladarea
                                on alm.codarea equals are.codigo into table1
                                from are in table1.DefaultIfEmpty()
                                join plp in lpeplanporcen
                                on alm.codplanporcen equals plp.codigo into table2
                                from plp in table2.DefaultIfEmpty()
                                orderby alm.fechareg descending
                                select new 
                                {
                                    codigo = alm.codigo,
                                    descripcion = alm.descripcion,
                                    codarea = alm.codarea,
                                    descarea = are.descripcion,
                                    direccion = alm.direccion,
                                    telefono = alm.telefono,
                                    email = alm.email,
                                    tienda = alm.tienda,
                                    codplanporcen = alm.codplanporcen,
                                    descplanporcen = plp.descripcion,
                                    nropersonas = alm.nropersonas,
                                    estandar = alm.estandar,
                                    monestandar = alm.monestandar,
                                    minimo = alm.minimo,
                                    moneda = alm.moneda,
                                    nropatronal = alm.nropatronal,
                                    horareg = alm.horareg,
                                    fechareg = alm.fechareg,
                                    usuarioreg = alm.usuarioreg,
                                    lugar = alm.lugar,
                                    sucursallc = alm.sucursallc,
                                    min_solurgente = alm.min_solurgente,
                                    codmoneda_min_solurgente = alm.codmoneda_min_solurgente,
                                    pesomin = alm.pesomin,
                                    pesoest = alm.pesoest,
                                    porcenmin = alm.porcenmin,
                                    porcenmin_rendi = alm.porcenmin_rendi,
                                    pesoest_rendi = alm.pesoest_rendi,
                                    pesomin_rendi = alm.pesomin_rendi,
                                    idcuenta_caja_mn = alm.idcuenta_caja_mn,
                                    idcuenta_caja_me = alm.idcuenta_caja_me,
                                    graficar = alm.graficar,
                                    analizar_rendimiento = alm.analizar_rendimiento,
                                    actividad = alm.actividad,
                                    latitud = alm.latitud,
                                    longitud = alm.longitud,
                                    fax = alm.fax
                                };
                    return Ok(query);

                }
                

            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }


        }

        // GET: api/inalmacen/5
        [HttpGet("{userConn}/{codigo}")]
        public async Task<ActionResult<inalmacen>> Getinalmacen(string userConn, int codigo)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                //var _context = _userConnectionManager.GetUserConnection(userId);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.inalmacen == null)
                    {
                        return Problem("Entidad inalmacen es null.");
                    }
                    //var inalmacen = await _context.inalmacen.FindAsync(codigo);

                    List<inalmacen> linalmacen = _context.inalmacen.ToList();
                    List<adarea> ladarea = _context.adarea.ToList();
                    List<peplanporcen> lpeplanporcen = _context.peplanporcen.ToList();

                    var query = from alm in linalmacen
                                join are in ladarea
                                on alm.codarea equals are.codigo into table1
                                from are in table1.DefaultIfEmpty()
                                join plp in lpeplanporcen
                                on alm.codplanporcen equals plp.codigo into table2
                                from plp in table2.DefaultIfEmpty()
                                where alm.codigo == codigo
                                select new 
                                {
                                    codigo = alm.codigo,
                                    descripcion = alm.descripcion,
                                    codarea = alm.codarea,
                                    descarea = are.descripcion,
                                    direccion = alm.direccion,
                                    telefono = alm.telefono,
                                    email = alm.email,
                                    tienda = alm.tienda,
                                    codplanporcen = alm.codplanporcen,
                                    descplanporcen = plp.descripcion,
                                    nropersonas = alm.nropersonas,
                                    estandar = alm.estandar,
                                    monestandar = alm.monestandar,
                                    minimo = alm.minimo,
                                    moneda = alm.moneda,
                                    nropatronal = alm.nropatronal,
                                    horareg = alm.horareg,
                                    fechareg = alm.fechareg,
                                    usuarioreg = alm.usuarioreg,
                                    lugar = alm.lugar,
                                    sucursallc = alm.sucursallc,
                                    min_solurgente = alm.min_solurgente,
                                    codmoneda_min_solurgente = alm.codmoneda_min_solurgente,
                                    pesomin = alm.pesomin,
                                    pesoest = alm.pesoest,
                                    porcenmin = alm.porcenmin,
                                    porcenmin_rendi = alm.porcenmin_rendi,
                                    pesoest_rendi = alm.pesoest_rendi,
                                    pesomin_rendi = alm.pesomin_rendi,
                                    idcuenta_caja_mn = alm.idcuenta_caja_mn,
                                    idcuenta_caja_me = alm.idcuenta_caja_me,
                                    graficar = alm.graficar,
                                    analizar_rendimiento = alm.analizar_rendimiento,
                                    actividad = alm.actividad,
                                    latitud = alm.latitud,
                                    longitud = alm.longitud,
                                    fax = alm.fax
                                };
                    //return Request.CreateResponse(HttpStatusCode.OK, query);

                    if (query.Count() == 0)
                    {
                        return NotFound("No se encontro un registro con este código");
                    }

                    return Ok(query);
                }
                
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }

        // PUT: api/inalmacen/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPut("{userConn}/{codigo}")]
        public async Task<IActionResult> Putinalmacen(string userConn, int codigo, inalmacen inalmacen)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            //var _context = _userConnectionManager.GetUserConnection(userId);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (codigo != inalmacen.codigo)
                {
                    return BadRequest("Error con Id en datos proporcionados.");
                }

                _context.Entry(inalmacen).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!inalmacenExists(codigo, _context))
                    {
                        return NotFound("No existe un registro con ese código");
                    }
                    else
                    {
                        throw;
                    }
                }

                return Ok("206");   // actualizado con exito
            }
            


        }

        // POST: api/inalmacen
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPost("{userConn}")]
        public async Task<ActionResult<inalmacen>> Postinalmacen(string userConn, inalmacen inalmacen)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            //var _context = _userConnectionManager.GetUserConnection(userId);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (_context.inalmacen == null)
                {
                    return Problem("Entidad inalmacen es null.");
                }
                _context.inalmacen.Add(inalmacen);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (inalmacenExists(inalmacen.codigo, _context))
                    {
                        return Conflict("Ya existe un registro con ese código");
                    }
                    else
                    {
                        throw;
                    }
                }

                return Ok("204");   // creado con exito

            }
            
        }

        // DELETE: api/inalmacen/5
        [Authorize]
        [HttpDelete("{userConn}/{codigo}")]
        public async Task<IActionResult> Deleteinalmacen(string userConn, int codigo)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                //var _context = _userConnectionManager.GetUserConnection(userId);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.inalmacen == null)
                    {
                        return Problem("Entidad inalmacen es null.");
                    }
                    var inalmacen = await _context.inalmacen.FindAsync(codigo);
                    if (inalmacen == null)
                    {
                        return NotFound("No existe un registro con ese código");
                    }

                    _context.inalmacen.Remove(inalmacen);
                    await _context.SaveChangesAsync();

                    return Ok("208");   // eliminado con exito
                }
                

            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }

        private bool inalmacenExists(int codigo, DBContext _context)
        {
            return (_context.inalmacen?.Any(e => e.codigo == codigo)).GetValueOrDefault();

        }
    }
}
