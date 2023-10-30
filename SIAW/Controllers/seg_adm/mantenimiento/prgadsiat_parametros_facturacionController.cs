using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using siaw_DBContext.Data;
using siaw_DBContext.Models;
using Microsoft.AspNetCore.Authorization;

namespace SIAW.Controllers.seg_adm.mantenimiento
{
    [Route("api/seg_adm/mant/[controller]")]
    [ApiController]
    public class prgadsiat_parametros_facturacionController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        public prgadsiat_parametros_facturacionController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }


        // GET: api/adsiat_tipoemision
        [HttpGet]
        [Route("tipoEmision/{userConn}")]
        public async Task<ActionResult<IEnumerable<adsiat_tipoemision>>> Getadsiat_tipoemision(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.adsiat_tipoemision == null)
                    {
                        return Problem("Entidad adsiat_tipoemision es null.");
                    }
                    var result = await _context.adsiat_tipoemision
                        .OrderBy(x => x.codigoclasificador)
                        .Select (x => new
                        {
                            codigoclasificador = x.codigoclasificador,
                            descripcion = x.descripcion
                        })
                        .ToListAsync();
                    return Ok(result);
                }
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }

        // GET: api/adsiat_tipodocsector
        [HttpGet]
        [Route("tipoDocSector/{userConn}")]
        public async Task<ActionResult<IEnumerable<adsiat_tipodocsector>>> Getadsiat_tipodocsector(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.adsiat_tipodocsector == null)
                    {
                        return Problem("Entidad adsiat_tipodocsector es null.");
                    }
                    var result = await _context.adsiat_tipodocsector
                                            .OrderBy(x => x.codigoclasificador)
                                            .Select(x => new
                                            {
                                                codigoclasificador = x.codigoclasificador,
                                                descripcion = x.descripcion
                                            })
                                            .ToListAsync();
                return Ok(result);
                }
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }

        // GET: api/adsiat_tipofactura
        [HttpGet]
        [Route("tipoFactura/{userConn}")]
        public async Task<ActionResult<IEnumerable<adsiat_tipofactura>>> Getadsiat_tipofactura(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.adsiat_tipofactura == null)
                    {
                        return Problem("Entidad adsiat_tipofactura es null.");
                    }
                    var result = await _context.adsiat_tipofactura
                        .OrderBy(x => x.codigoclasificador)
                        .Select(x => new
                        {
                            codigoclasificador = x.codigoclasificador,
                            descripcion = x.descripcion
                        })
                        .ToListAsync();


                    return Ok(result);
                }
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }


        // GET: api/adsiat_sucursal
        [HttpGet]
        [Route("sucursales/{userConn}")]
        public async Task<ActionResult<IEnumerable<adsiat_sucursal>>> Getadsiat_sucursal(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.adsiat_sucursal == null)
                    {
                        return Problem("Entidad adsiat_sucursal es null.");
                    }
                    var result = await _context.adsiat_sucursal
                        .OrderBy(x => x.codsucursal)
                        .Select(x => new
                        {
                            codsucursal = x.codsucursal
                        })
                        .ToListAsync();
                    return Ok(result);
                }
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }



        // GET: api/adsiat_actividad
        [HttpGet]
        [Route("actividades/{userConn}")]
        public async Task<ActionResult<IEnumerable<adsiat_actividad>>> Getadsiat_actividad(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.adsiat_actividad == null)
                    {
                        return Problem("Entidad adsiat_actividad es null.");
                    }
                    var result = await _context.adsiat_actividad
                        .OrderBy(x => x.codigocaeb)
                        .Select(x => new
                        {
                            codigocaeb = x.codigocaeb,
                            descripcion = x.descripcion,
                            tipoactividad = x.tipoactividad
                        })
                        .ToListAsync();
                    return Ok(result);
                }
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }


        // GET: api/adsiat_parametros_facturacion
        [HttpGet]
        [Route("paramFacturacion/{userConn}/{codalmlocal}")]
        public async Task<ActionResult<IEnumerable<adsiat_parametros_facturacion>>> Getadsiat_parametros_facturacion(string userConn, int codalmlocal)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.adsiat_parametros_facturacion == null)
                    {
                        return Problem("Entidad adsiat_parametros_facturacion es null.");
                    }
                    var result = await _context.adsiat_parametros_facturacion
                        .Where(x => x.codalmacen == codalmlocal)
                        .FirstOrDefaultAsync();
                    return Ok(result);
                }
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }



        // GET: api/adcuis_sia
        [HttpGet]
        [Route("ptoVentaAlmacen/{userConn}/{sucursal}")]
        public async Task<ActionResult<IEnumerable<adcuis_sia>>> Getadcuis_sia(string userConn, int sucursal)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.adcuis_sia == null)
                    {
                        return Problem("Entidad adcuis_sia es null.");
                    }
                    var result = await _context.adcuis_sia
                        .Where(x => x.codsucursal == sucursal)
                        .FirstOrDefaultAsync();
                    return Ok(result);
                }
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }


        // PUT: api/adsiat_parametros_facturacion/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPut]
        [Route("updateServInternet/{userConn}/{servInternet}")]
        public async Task<IActionResult> Putadsiat_parametros_facturacion_internet(string userConn, bool servInternet)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                var adsiat_parametros_facturacion = await _context.adsiat_parametros_facturacion.FirstOrDefaultAsync();
                adsiat_parametros_facturacion.servicio_internet_activo=servInternet;

                _context.Entry(adsiat_parametros_facturacion).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    return Problem("Error en el Servidor");
                }

                return Ok("206");   // actualizado con exito
            }
        }


        // PUT: api/adsiat_parametros_facturacion/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPut]
        [Route("updateServSIN/{userConn}/{servSin}")]
        public async Task<IActionResult> Putadsiat_parametros_facturacion_sin(string userConn, bool servSin)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                var adsiat_parametros_facturacion = await _context.adsiat_parametros_facturacion.FirstOrDefaultAsync();
                adsiat_parametros_facturacion.servicio_sin_activo = servSin;

                _context.Entry(adsiat_parametros_facturacion).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    return Problem("Error en el Servidor");
                }

                return Ok("206");   // actualizado con exito
            }
        }


    }
}
