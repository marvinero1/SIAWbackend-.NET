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

                    var query = linalmacen
                        .Join(
                            ladarea,
                            alm => alm.codarea,
                            are => are.codigo,
                            (alm, are) => new { alm, are }
                        )
                        .GroupJoin(
                            lpeplanporcen,
                            alm => alm.alm.codplanporcen,
                            plp => plp.codigo,
                            (alm, plp) => new { alm.alm, alm.are, plp }
                        )
                        .SelectMany(
                            x => x.plp.DefaultIfEmpty(),
                            (x, plp) => new
                            {
                                codigo = x.alm.codigo,
                                descripcion = x.alm.descripcion,
                                codarea = x.alm.codarea,
                                descarea = x.are?.descripcion,
                                direccion = x.alm.direccion,
                                telefono = x.alm.telefono,
                                email = x.alm.email,
                                tienda = x.alm.tienda,
                                codplanporcen = x.alm.codplanporcen,
                                descplanporcen = plp?.descripcion,
                                nropersonas = x.alm.nropersonas,
                                estandar = x.alm.estandar,
                                monestandar = x.alm.monestandar,
                                minimo = x.alm.minimo,
                                moneda = x.alm.moneda,
                                nropatronal = x.alm.nropatronal,
                                horareg = x.alm.horareg,
                                fechareg = x.alm.fechareg,
                                usuarioreg = x.alm.usuarioreg,
                                lugar = x.alm.lugar,
                                sucursallc = x.alm.sucursallc,
                                min_solurgente = x.alm.min_solurgente,
                                codmoneda_min_solurgente = x.alm.codmoneda_min_solurgente,
                                pesomin = x.alm.pesomin,
                                pesoest = x.alm.pesoest,
                                porcenmin = x.alm.porcenmin,
                                porcenmin_rendi = x.alm.porcenmin_rendi,
                                pesoest_rendi = x.alm.pesoest_rendi,
                                pesomin_rendi = x.alm.pesomin_rendi,
                                idcuenta_caja_mn = x.alm.idcuenta_caja_mn,
                                idcuenta_caja_me = x.alm.idcuenta_caja_me,
                                graficar = x.alm.graficar,
                                analizar_rendimiento = x.alm.analizar_rendimiento,
                                actividad = x.alm.actividad,
                                latitud = x.alm.latitud,
                                longitud = x.alm.longitud,
                                fax = x.alm.fax
                            }
                        )
                        .OrderByDescending(x => x.fechareg)
                        .ToList();                            
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

                    var query = linalmacen
                        .Join(
                            ladarea,
                            alm => alm.codarea,
                            are => are.codigo,
                            (alm, are) => new { alm, are }
                        )
                        .GroupJoin(
                            lpeplanporcen,
                            alm => alm.alm.codplanporcen,
                            plp => plp.codigo,
                            (alm, plp) => new { alm.alm, alm.are, plp }
                        )
                        .Where(x => x.alm.codigo==codigo)
                        .SelectMany(
                            x => x.plp.DefaultIfEmpty(),
                            (x, plp) => new
                            {
                                codigo = x.alm.codigo,
                                descripcion = x.alm.descripcion,
                                codarea = x.alm.codarea,
                                descarea = x.are?.descripcion,
                                direccion = x.alm.direccion,
                                telefono = x.alm.telefono,
                                email = x.alm.email,
                                tienda = x.alm.tienda,
                                codplanporcen = x.alm.codplanporcen,
                                descplanporcen = plp?.descripcion,
                                nropersonas = x.alm.nropersonas,
                                estandar = x.alm.estandar,
                                monestandar = x.alm.monestandar,
                                minimo = x.alm.minimo,
                                moneda = x.alm.moneda,
                                nropatronal = x.alm.nropatronal,
                                horareg = x.alm.horareg,
                                fechareg = x.alm.fechareg,
                                usuarioreg = x.alm.usuarioreg,
                                lugar = x.alm.lugar,
                                sucursallc = x.alm.sucursallc,
                                min_solurgente = x.alm.min_solurgente,
                                codmoneda_min_solurgente = x.alm.codmoneda_min_solurgente,
                                pesomin = x.alm.pesomin,
                                pesoest = x.alm.pesoest,
                                porcenmin = x.alm.porcenmin,
                                porcenmin_rendi = x.alm.porcenmin_rendi,
                                pesoest_rendi = x.alm.pesoest_rendi,
                                pesomin_rendi = x.alm.pesomin_rendi,
                                idcuenta_caja_mn = x.alm.idcuenta_caja_mn,
                                idcuenta_caja_me = x.alm.idcuenta_caja_me,
                                graficar = x.alm.graficar,
                                analizar_rendimiento = x.alm.analizar_rendimiento,
                                actividad = x.alm.actividad,
                                latitud = x.alm.latitud,
                                longitud = x.alm.longitud,
                                fax = x.alm.fax
                            }
                        )
                        .OrderByDescending(x => x.fechareg)
                        .FirstOrDefault();
                    //return Request.CreateResponse(HttpStatusCode.OK, query);

                    if (query == null)
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



        // GET: api/catalogo
        [HttpGet]
        [Route("catalogo/{userConn}")]
        public async Task<ActionResult<IEnumerable<inalmacen>>> Getinalmacen_catalogo(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                //var _context = _userConnectionManager.GetUserConnection(userId);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var query = _context.inalmacen
                    .OrderBy(i => i.codigo)
                    .Select(i => new
                    {
                        i.codigo,
                        i.descripcion
                    });

                    var result = query.ToList();

                    if (result.Count() == 0)
                    {
                        return Problem("No se encontraron registros con esos datos.");
                    }
                    return Ok(result);
                }

            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
                throw;
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
