using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using siaw_DBContext.Data;
using siaw_DBContext.Models;
using System.Net;
using Microsoft.AspNetCore.Authorization;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace SIAW.Controllers.ventas.mantenimiento
{
    [Route("api/venta/mant/[controller]")]
    [ApiController]
    public class vedesitemController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        public vedesitemController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }

        /// <summary>
        /// Obtiene todos los datos de la tabla vedesitem ordenado por codalmacen y coditem
        /// </summary>
        /// <param name="userConn"></param>
        /// <returns></returns>
        // GET: api/vedesitem
        [HttpGet("{userConn}")]
        public async Task<ActionResult<IEnumerable<vedesitem>>> Getvedesitem(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.vedesitem == null)
                    {
                        return BadRequest(new { resp = "Entidad vedesitem es null." });
                    }
                    var result = await _context.vedesitem.OrderBy(codalmacen => codalmacen.codalmacen).ThenBy(coditem => coditem.coditem).ToListAsync();
                    return Ok(result);
                }
                
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }


        }


        /// <summary>
        /// Obtiene todos los datos de un registro de la tabla vedesitem
        /// </summary>
        /// <param name="userConn"></param>
        /// <param name="codalmacen"></param>
        /// <param name="coditem"></param>
        /// <param name="nivel"></param>
        /// <returns></returns>
        // GET: api/vedesitem/5
        [HttpGet("{userConn}/{codalmacen}/{coditem}/{nivel}")]
        public async Task<ActionResult<vedesitem>> Getvedesitem(string userConn, int codalmacen, string coditem, string nivel)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.vedesitem == null)
                    {
                        return BadRequest(new { resp = "Entidad vedesitem es null." });
                    }
                    var vedesitem = _context.vedesitem.FirstOrDefault(objeto => objeto.codalmacen == codalmacen && objeto.coditem == coditem && objeto.nivel == nivel);

                    if (vedesitem == null)
                    {
                        return NotFound( new { resp = "No se encontro un registro con este código" });
                    }
                    return Ok(vedesitem);
                }
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }



        /// <summary>
        /// Obtiene todos los datos de un registro de la tabla vedesitem
        /// </summary>
        /// <param name="userConn"></param>
        /// <param name="codalmacen"></param>
        /// <param name="coditem"></param>
        /// <returns></returns>
        // GET: api/vedesitem/5
        [HttpGet]
        [Route("listvedesitem/{userConn}/{codalmacen}/{coditem}")]
        public async Task<ActionResult<vedesitem>> Mostrarvedesitem(string userConn, int codalmacen, string coditem)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.vedesitem == null)
                    {
                        return BadRequest(new { resp = "Entidad vedesitem es null." });
                    }
                    var vedesitem = await _context.vedesitem
                        .Where(v => v.codalmacen == codalmacen && v.coditem == coditem)
                        .OrderBy(v => v.nivel)
                        .ToListAsync();

                    if (vedesitem == null)
                    {
                        return NotFound(new { resp = "No se encontro ningun registro con este código" });
                    }
                    return Ok(vedesitem);
                }
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }




        // PUT: api/vedesitem/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPut]
        [Route("inicializar/{userConn}/{codalmacen}/{coditem}")]
        public async Task<IActionResult> inicializar(string userConn, int codalmacen, string coditem)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                using (var dbContexTransaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        var desitems = await _context.vedesitem
                            .Where(v => v.coditem == coditem && v.codalmacen == codalmacen)
                            .ToListAsync();
                        // primero borrar todos los actuales si es que tiene
                        if (desitems.Count > 0)
                        {
                            _context.vedesitem.RemoveRange(desitems);
                            await _context.SaveChangesAsync();
                        }
                        // insertar todos los periodos actuales
                        var newListvedesitem = _context.vedesnivel
                            .Select(v => new vedesitem
                            {
                                codalmacen = codalmacen,
                                coditem = coditem,
                                nivel = v.codigo,
                                desde = 0,
                                hasta = 0,
                                descuento = 0
                            });
                        _context.vedesitem.AddRange(newListvedesitem);
                        await _context.SaveChangesAsync();

                        dbContexTransaction.Commit();
                        return Ok(new { resp = "206", data = newListvedesitem });   // actualizado con exito
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        dbContexTransaction.Rollback();
                        return Problem("Error en el servidor");
                        throw;
                    }
                }
            }
        }






        // POST: api/vedesitem
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPut]
        [Route("actualizaDetalle/{userConn}")]
        public async Task<ActionResult<vedesitem>> actualizaDetalle(string userConn, List<vedesitem> vedesitem)
        {
            if (vedesitem.Count()<1)
            {
                return BadRequest(new { resp = "No se esta recibiendo ningun dato del detalle para actualizarse, favor verifique esta situación." });
            }

            if (vedesitem.Any(item => item.desde > item.hasta))
            {
                // Enviar un mensaje de error ya que 'desde' es mayor que 'hasta' en al menos uno de los elementos
                return BadRequest(new { resp = "El valor 'desde' no puede ser mayor que 'hasta'." });
            }
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                using (var dbContexTransaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        foreach (var item in vedesitem)
                        {
                            var modifvedesitem = _context.vedesitem.FirstOrDefault(x => x.codalmacen == item.codalmacen && x.coditem == item.coditem && x.nivel == item.nivel);
                            if (modifvedesitem != null)
                            {
                                modifvedesitem.desde = item.desde;
                                modifvedesitem.hasta = item.hasta;
                                modifvedesitem.descuento = item.descuento;

                                _context.Entry(modifvedesitem).State = EntityState.Modified;
                                await _context.SaveChangesAsync();
                            }
                            else
                            {
                                dbContexTransaction.Rollback();
                                return BadRequest(new { resp = "Existen datos no encontrados de acuerdo al detalle, favor inicialice primero e intentelo de nuevo." });
                            }
                        }

                        dbContexTransaction.Commit();
                        return Ok(new { resp = "206" });   // actualizado con exito
                    }
                    catch (DbUpdateException)
                    {
                        dbContexTransaction.Rollback();
                        return Problem("Error en el servidor");
                        throw;
                    }
                }
            }
        }

        // DELETE: api/vedesitem/5
        [Authorize]
        [HttpDelete("{userConn}/{codalmacen}/{coditem}")]
        public async Task<IActionResult> Deletevedesitem(string userConn, int codalmacen, string coditem)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.vedesitem == null)
                    {
                        return BadRequest(new { resp = "Entidad vedesitem es null." });
                    }
                    var vedesitem = await _context.vedesitem
                        .Where(v => v.coditem == coditem && v.codalmacen == codalmacen)
                        .ToListAsync();

                    if (vedesitem.Count() == 0)
                    {
                        return NotFound( new { resp = "No existe un registro con esos datos (almacen, item)" });
                    }

                    _context.vedesitem.RemoveRange(vedesitem);
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
