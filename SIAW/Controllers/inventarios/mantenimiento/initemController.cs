using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NuGet.Configuration;
using siaw_DBContext.Data;
using siaw_DBContext.Models;
using System.Net;
using Microsoft.AspNetCore.Authorization;
using siaw_funciones;

namespace SIAW.Controllers.inventarios.mantenimiento
{
    [Route("api/inventario/mant/[controller]")]
    [ApiController]
    public class initemController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        private readonly Items items = new Items();
        public initemController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }

        // GET: api/initem
        [HttpGet("{userConn}")]
        public async Task<ActionResult<IEnumerable<initem>>> Getinitem(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.initem == null)
                    {
                        return BadRequest(new { resp = "Entidad initem es null." });
                    }

                    var result = _context.initem
                        .Join(_context.inudemed, item => item.unidad, udemed => udemed.Codigo, (item, udemed) => new { item, udemed })
                        .Join(_context.inrosca, combined => combined.item.rosca, rosca => rosca.codigo, (combined, rosca) => new { combined.item, combined.udemed, rosca })
                        .Join(_context.interminacion, combined => combined.item.terminacion, terminacion => terminacion.codigo, (combined, terminacion) => new { combined.item, combined.udemed, combined.rosca, terminacion })
                        .Join(_context.inresistencia, combined => combined.item.codresistencia, resistencia => resistencia.codigo, (combined, resistencia) => new { combined.item, combined.udemed, combined.rosca, combined.terminacion, resistencia })
                        .Join(_context.inlinea, combined => combined.item.codlinea, linea => linea.codigo, (combined, linea) => new
                        {
                            combined.item.codigo,
                            combined.item.descripcion,
                            combined.item.descripcorta,
                            combined.item.descripabr,
                            combined.item.medida,
                            combined.item.unidad,
                            descUnidad = combined.udemed.Descripcion,
                            combined.item.rosca,
                            descRosca = combined.rosca.descripcion,
                            combined.item.terminacion,
                            descTerminacion = combined.terminacion.descripcion,
                            combined.item.peso,
                            combined.item.codlinea,
                            descLinea = linea.descripcion,
                            combined.item.clasificacion,
                            combined.item.kit,
                            combined.item.estadocv,
                            combined.item.costo,
                            combined.item.monedacosto,
                            combined.item.codresistencia,
                            descResistencia = combined.resistencia.descripcion,
                            combined.item.horareg,
                            combined.item.fechareg,
                            combined.item.usuarioreg,
                            combined.item.enlinea,
                            combined.item.saldominimo,
                            combined.item.codigobarra,
                            combined.item.reservastock,
                            combined.item.iva,
                            combined.item.codmoneda_valor_criterio,
                            combined.item.porcen_gac,
                            combined.item.nandina,
                            combined.item.usar_en_movimiento,
                            combined.item.paga_comision,
                            combined.item.porcen_saldo_restringido,
                            combined.item.controla_negativo,
                            combined.item.tipo,
                            combined.item.codproducto_sin
                        })
                        .OrderBy(item => item.codigo)
                        .Select(item => new
                        {
                            item.codigo,
                            item.descripcion,
                            item.descripcorta,
                            item.descripabr,
                            item.medida,
                            item.unidad,
                            item.descUnidad,
                            item.rosca,
                            item.descRosca,
                            item.terminacion,
                            item.descTerminacion,
                            item.peso,
                            item.codlinea,
                            item.descLinea,
                            item.clasificacion,
                            item.kit,
                            item.estadocv,
                            item.costo,
                            item.monedacosto,
                            item.codresistencia,
                            item.descResistencia,
                            item.horareg,
                            item.fechareg,
                            item.usuarioreg,
                            item.enlinea,
                            item.saldominimo,
                            item.codigobarra,
                            item.reservastock,
                            item.iva,
                            item.codmoneda_valor_criterio,
                            item.porcen_gac,
                            item.nandina,
                            item.usar_en_movimiento,
                            item.paga_comision,
                            item.porcen_saldo_restringido,
                            item.controla_negativo,
                            item.tipo,
                            item.codproducto_sin
                        })
                        .ToList();     


                    return Ok(result);
                }
                
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }


        }

        // GET: api/initem/5
        [HttpGet("{userConn}/{codigo}")]
        public async Task<ActionResult<initem>> Getinitem(string userConn, string codigo)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.initem == null)
                    {
                        return BadRequest(new { resp = "Entidad initem es null." });
                    }
                    //var initem = await _context.initem.FindAsync(codigo);

                    var result = _context.initem
                        .Join(_context.inudemed, item => item.unidad, udemed => udemed.Codigo, (item, udemed) => new { item, udemed })
                        .Join(_context.inrosca, combined => combined.item.rosca, rosca => rosca.codigo, (combined, rosca) => new { combined.item, combined.udemed, rosca })
                        .Join(_context.interminacion, combined => combined.item.terminacion, terminacion => terminacion.codigo, (combined, terminacion) => new { combined.item, combined.udemed, combined.rosca, terminacion })
                        .Join(_context.inresistencia, combined => combined.item.codresistencia, resistencia => resistencia.codigo, (combined, resistencia) => new { combined.item, combined.udemed, combined.rosca, combined.terminacion, resistencia })
                        .Join(_context.inlinea, combined => combined.item.codlinea, linea => linea.codigo, (combined, linea) => new
                        {
                            combined.item.codigo,
                            combined.item.descripcion,
                            combined.item.descripcorta,
                            combined.item.descripabr,
                            combined.item.medida,
                            combined.item.unidad,
                            descUnidad = combined.udemed.Descripcion,
                            combined.item.rosca,
                            descRosca = combined.rosca.descripcion,
                            combined.item.terminacion,
                            descTerminacion = combined.terminacion.descripcion,
                            combined.item.peso,
                            combined.item.codlinea,
                            descLinea = linea.descripcion,
                            combined.item.clasificacion,
                            combined.item.kit,
                            combined.item.estadocv,
                            combined.item.costo,
                            combined.item.monedacosto,
                            combined.item.codresistencia,
                            descResistencia = combined.resistencia.descripcion,
                            combined.item.horareg,
                            combined.item.fechareg,
                            combined.item.usuarioreg,
                            combined.item.enlinea,
                            combined.item.saldominimo,
                            combined.item.codigobarra,
                            combined.item.reservastock,
                            combined.item.iva,
                            combined.item.codmoneda_valor_criterio,
                            combined.item.porcen_gac,
                            combined.item.nandina,
                            combined.item.usar_en_movimiento,
                            combined.item.paga_comision,
                            combined.item.porcen_saldo_restringido,
                            combined.item.controla_negativo,
                            combined.item.tipo,
                            combined.item.codproducto_sin
                        })
                        .Where(item => item.codigo == codigo)
                        .Select(item => new
                        {
                            item.codigo,
                            item.descripcion,
                            item.descripcorta,
                            item.descripabr,
                            item.medida,
                            item.unidad,
                            item.descUnidad,
                            item.rosca,
                            item.descRosca,
                            item.terminacion,
                            item.descTerminacion,
                            item.peso,
                            item.codlinea,
                            item.descLinea,
                            item.clasificacion,
                            item.kit,
                            item.estadocv,
                            item.costo,
                            item.monedacosto,
                            item.codresistencia,
                            item.descResistencia,
                            item.horareg,
                            item.fechareg,
                            item.usuarioreg,
                            item.enlinea,
                            item.saldominimo,
                            item.codigobarra,
                            item.reservastock,
                            item.iva,
                            item.codmoneda_valor_criterio,
                            item.porcen_gac,
                            item.nandina,
                            item.usar_en_movimiento,
                            item.paga_comision,
                            item.porcen_saldo_restringido,
                            item.controla_negativo,
                            item.tipo,
                            item.codproducto_sin
                        })
                        .FirstOrDefault();
                   
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


        // GET: api/catalogo
        [HttpGet]
        [Route("catalogo/{userConn}")]
        public async Task<ActionResult<IEnumerable<initem>>> Getinitem_catalogo_resumido(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var query = _context.initem
                    .OrderBy(i => i.codigo)
                    .Select(i => new
                    {
                        codigo = i.codigo,
                        descrip = i.descripcion + i.medida
                    });

                    var result = query.ToList();

                    if (result.Count() == 0)
                    {
                        return BadRequest(new { resp = "Entidad initem es null." });
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


        // GET: api/catalogo2
        [HttpGet]
        [Route("catalogo2/{userConn}")]
        public async Task<ActionResult<IEnumerable<initem>>> Getinitem_catalogo(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var query = _context.initem
                    .OrderBy(i => i.codigo)
                    .Select(i => new
                    {
                        i.codigo,
                        i.descripcion,
                        i.medida,
                        i.estadocv,
                        i.enlinea,
                        i.codlinea
                    });

                    var result = query.ToList();

                    if (result.Count() == 0)
                    {
                        return BadRequest(new { resp = "Entidad initem es null." });
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


        // GET: api/catalogo3
        [HttpGet]
        [Route("catalogo3/{userConn}")]
        public async Task<ActionResult<IEnumerable<initem>>> Getinitem_catalogo3(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var query = _context.initem
                    .OrderBy(i => i.codigo)
                    .Select(i => new
                    {
                        i.codigo,
                        i.descripcion,
                        i.medida
                    });

                    var result = query.ToList();

                    if (result.Count() == 0)
                    {
                        return BadRequest(new { resp = "Entidad initem es null." });
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


        // PUT: api/initem/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPut("{userConn}/{codigo}")]
        public async Task<IActionResult> Putinitem(string userConn, string codigo, initem initem)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                using (var dbContexTransaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        if (codigo != initem.codigo)
                        {
                            return BadRequest(new { resp = "Error con Id en datos proporcionados." });
                        }
                        if (!initemExists(codigo, _context))
                        {
                            return NotFound(new { resp = "No existe un registro con ese código" });
                        }
                        if (initem.kit == false)
                        {
                            var componentes = await _context.inkit.Where(i => i.codigo == codigo).ToListAsync();
                            if (componentes.Count() > 0)
                            {
                                _context.inkit.RemoveRange(componentes);
                                await _context.SaveChangesAsync();
                            }
                        }
                        _context.Entry(initem).State = EntityState.Modified;
                        await _context.SaveChangesAsync();

                        dbContexTransaction.Commit();
                        return Ok(new { resp = "206" });   // actualizado con exito
                    }
                    catch (Exception)
                    {
                        dbContexTransaction.Rollback();
                        return Problem("Error en el servidor");
                        throw; ;
                    }
                }     
            }
        }

        // POST: api/initem
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPost("{userConn}")]
        public async Task<ActionResult<initem>> Postinitem(string userConn, initem initem)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (_context.initem == null)
                {
                    return BadRequest(new { resp = "Entidad initem es null." });
                }
                _context.initem.Add(initem);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (initemExists(initem.codigo, _context))
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

        // DELETE: api/initem/5
        [Authorize]
        [HttpDelete("{userConn}/{codigo}")]
        public async Task<IActionResult> Deleteinitem(string userConn, string codigo)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                using (var dbContexTransaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        if (_context.initem == null)
                        {
                            return BadRequest(new { resp = "Entidad initem es null." });
                        }
                        var initem = await _context.initem.FindAsync(codigo);
                        if (initem == null)
                        {
                            return NotFound(new { resp = "No existe un registro con ese código" });
                        }

                        // elimina componentes si es kit
                        if (initem.kit == true)
                        {
                            var componentes = await _context.inkit.Where(i => i.codigo == codigo).ToListAsync();
                            if (componentes.Count() > 0)
                            {
                                _context.inkit.RemoveRange(componentes);
                                await _context.SaveChangesAsync();
                            }
                        }
                        // elimina saldos de tabla instoactual
                        await items.disminuiritem(_context, codigo);

                        // elimina de tabla initem
                        _context.initem.Remove(initem);
                        await _context.SaveChangesAsync();


                        dbContexTransaction.Commit();
                        return Ok(new { resp = "208" });   // eliminado con exito
                    }
                    catch (Exception)
                    {
                        dbContexTransaction.Rollback();
                        return Problem("Error en el servidor");
                        throw;
                    }
                }

            }
        }

        private bool initemExists(string codigo, DBContext _context)
        {
            return (_context.initem?.Any(e => e.codigo == codigo)).GetValueOrDefault();

        }
    }




    [Route("api/inventario/mant/[controller]")]
    [ApiController]
    public class inctrlstockController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        public inctrlstockController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }

        // GET: api/inctrlstock
        [HttpGet("{userConn}")]
        public async Task<ActionResult<IEnumerable<inctrlstock>>> Getinctrlstock(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.inctrlstock == null)
                    {
                        return BadRequest(new { resp = "Entidad inctrlstock es null." });
                    }
                    var result = await _context.inctrlstock.OrderBy(id => id.id).ToListAsync();
                    return Ok(result);
                }
                
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }


        }

        /// <summary>
        /// Obtiene todos los registros de la tabla inctrlstock (item) con initem, dependiendo del codigo de item
        /// </summary>
        /// <param name="userConn"></param>
        /// <param name="coditem"></param>
        /// <returns></returns>
        // GET: api/initem_inctrlstock/5
        [HttpGet]
        [Route("initem_inctrlstock/{userConn}/{coditem}")]
        public async Task<ActionResult<inctrlstock>> Getinctrlstock(string userConn, string coditem)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.inctrlstock == null)
                    {
                        return BadRequest(new { resp = "Entidad inctrlstock es null." });
                    }
                    //var inctrlstock = await _context.inctrlstock.FindAsync(id);
                    var query = from s in _context.inctrlstock
                                join i in _context.initem on s.coditemcontrol equals i.codigo
                                where s.coditem == coditem
                                orderby s.coditemcontrol
                                select new
                                {
                                    s.id,
                                    s.coditem,
                                    s.coditemcontrol,
                                    i.descripcion,
                                    i.medida,
                                    s.porcentaje
                                };

                    var result = query.ToList();

                    if (result.Count() == 0)
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

        // POST: api/inctrlstock
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPost("{userConn}")]
        public async Task<ActionResult<inctrlstock>> Postinctrlstock(string userConn, inctrlstock inctrlstock)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (_context.inctrlstock == null)
                {
                    return BadRequest(new { resp = "Entidad inctrlstock es null." });
                }
                _context.inctrlstock.Add(inctrlstock);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (inctrlstockExists(inctrlstock.id, _context))
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

        // DELETE: api/inctrlstock/5
        [Authorize]
        [HttpDelete("{userConn}/{coditem}/{coditemcontrol}")]
        public async Task<IActionResult> Deleteinctrlstock(string userConn, string coditem, string coditemcontrol)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.inctrlstock == null)
                    {
                        return BadRequest(new { resp = "Entidad inctrlstock es null." });
                    }
                    var inctrlstock = await _context.inctrlstock.
                        Where(i => i.coditem == coditem && i.coditemcontrol == coditemcontrol)
                        .FirstOrDefaultAsync();
                    if (inctrlstock == null)
                    {
                        return NotFound( new { resp = "No existe un registro con ese código" });
                    }

                    _context.inctrlstock.Remove(inctrlstock);
                    await _context.SaveChangesAsync();

                    return Ok( new { resp = "208" });   // eliminado con exito
                }
                

            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }

        private bool inctrlstockExists(int id, DBContext _context)
        {
            return (_context.inctrlstock?.Any(e => e.id == id)).GetValueOrDefault();

        }

    }





    [Route("api/inventario/mant/[controller]")]
    [ApiController]
    public class initem_maxController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        public initem_maxController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }

        // GET: api/initem_max
        [HttpGet("{userConn}")]
        public async Task<ActionResult<IEnumerable<initem_max>>> Getinitem_max(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.initem_max == null)
                    {
                        return BadRequest(new { resp = "Entidad initem_max es null." });
                    }
                    var result = await _context.initem_max.OrderBy(id => id.id).ToListAsync();
                    return Ok(result);
                }

            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }


        }

        /// <summary>
        /// Obtiene todos los registros de la tabla initem_max (item), dependiendo del codigo de item
        /// </summary>
        /// <param name="userConn"></param>
        /// <param name="coditem"></param>
        /// <returns></returns>
        // GET: api/initem_initemMax/5
        [HttpGet]
        [Route("initem_initemMax/{userConn}/{coditem}")]
        public async Task<ActionResult<initem_max>> Getinitem_max(string userConn, string coditem)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.initem_max == null)
                    {
                        return BadRequest(new { resp = "Entidad initem_max es null." });
                    }
                    var query = _context.initem_max
                    .Where(im => im.coditem == coditem)
                    .OrderBy(im => im.codalmacen)
                    .ThenBy(im => im.codtarifa)
                    .Select(im => new
                    {
                        im.id,
                        im.codalmacen,
                        im.codtarifa,
                        im.maximo,
                        im.dias
                    });
                    var result = query.ToList();

                    if (result.Count() == 0)
                    {
                        return NotFound(new { resp = "No se encontro un registro con este código" });
                    }

                    return Ok(result);
                }

            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }


        // POST: api/initem_max
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPost("{userConn}")]
        public async Task<ActionResult<initem_max>> Postinitem_max(string userConn, initem_max initem_max)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (_context.initem_max == null)
                {
                    return BadRequest(new { resp = "Entidad initem_max es null." });
                }

                var valida = await _context.initem_max
                    .Where(i => i.coditem == initem_max.coditem && i.codalmacen == initem_max.codalmacen && i.codtarifa == initem_max.codtarifa)
                    .ToListAsync();
                if (valida.Count() > 0)
                {
                    return Conflict(new { resp = "Ya existe un registro con esos datos (cod item, cod almacen y cod tarifa)" });
                }

                _context.initem_max.Add(initem_max);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    return Problem("Error en el servidor");
                }

                return Ok(new { resp = "204" });   // creado con exito

            }

        }

        // DELETE: api/initem_max/5
        [Authorize]
        [HttpDelete("{userConn}/{coditem}/{codalmacen}/{codtarifa}")]
        public async Task<IActionResult> Deleteinitem_max(string userConn, string coditem, int codalmacen, int codtarifa)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.initem_max == null)
                    {
                        return BadRequest(new { resp = "Entidad initem_max es null." });
                    }
                    var initem_max = await _context.initem_max
                    .Where(i => i.coditem == coditem && i.codalmacen == codalmacen && i.codtarifa == codtarifa)
                    .FirstOrDefaultAsync();
                    if (initem_max == null)
                    {
                        return NotFound(new { resp = "No existe un registro con los datos proporcionados" });
                    }

                    _context.initem_max.Remove(initem_max);
                    await _context.SaveChangesAsync();

                    return Ok(new { resp = "208" });   // eliminado con exito
                }


            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }
    }








    [Route("api/inventario/mant/[controller]")]
    [ApiController]
    public class initem_controltarifaController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        public initem_controltarifaController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }

        // GET: api/initem_controltarifa
        [HttpGet("{userConn}")]
        public async Task<ActionResult<IEnumerable<initem_controltarifa>>> Getinitem_controltarifa(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.initem_controltarifa == null)
                    {
                        return BadRequest(new { resp = "Entidad initem_controltarifa es null." });
                    }
                    var result = await _context.initem_controltarifa.OrderBy(id => id.id).ToListAsync();
                    return Ok(result);
                }
                
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }


        }

        /// <summary>
        /// Obtiene todos los registros de la tabla initem_controltarifa (item), dependiendo del codigo de item
        /// </summary>
        /// <param name="userConn"></param>
        /// <param name="coditem"></param>
        /// <returns></returns>
        // GET: api/initem_controltarifa/5
        [HttpGet]
        [Route("initem_controltarifa/{userConn}/{coditem}")]
        public async Task<ActionResult<initem_controltarifa>> Getinitem_controltarifa(string userConn, string coditem)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.initem_controltarifa == null)
                    {
                        return BadRequest(new { resp = "Entidad initem_controltarifa es null." });
                    }
                    var query = _context.initem_controltarifa
                    .Where(im => im.coditem == coditem)
                    .OrderBy(im => im.codtarifa_a)
                    .ThenBy(im => im.codtarifa_b)
                    .Select(im => new
                    {
                        im.id,
                        im.coditem,
                        im.codtarifa_a,
                        im.codtarifa_b
                    });

                    var result = query.ToList();

                    if (result.Count() == 0)
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

        // POST: api/initem_controltarifa
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPost("{userConn}")]
        public async Task<ActionResult<initem_controltarifa>> Postinitem_controltarifa(string userConn, initem_controltarifa initem_controltarifa)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (_context.initem_controltarifa == null)
                {
                    return BadRequest(new { resp = "Entidad initem_controltarifa es null." });
                }
                var valida = await _context.initem_controltarifa
                    .Where(i => i.coditem == initem_controltarifa.coditem && i.codtarifa_a == initem_controltarifa.codtarifa_a && i.codtarifa_b == initem_controltarifa.codtarifa_b)
                    .ToListAsync();

                if (valida.Count() > 0)
                {
                    return Conflict(new { resp = "Ya existe un registro con los datos proporcionados (cod item, cod tarifa_a y cod tarifa_b)" });
                }

                _context.initem_controltarifa.Add(initem_controltarifa);
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

        // DELETE: api/initem_controltarifa/5
        [Authorize]
        [HttpDelete("{userConn}/{coditem}/{codtarifa_a}/{codtarifa_b}")]
        public async Task<IActionResult> Deleteinitem_controltarifa(string userConn, string coditem, int codtarifa_a, int codtarifa_b)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.initem_controltarifa == null)
                    {
                        return BadRequest(new { resp = "Entidad initem_controltarifa es null." });
                    }
                    var initem_controltarifa = await _context.initem_controltarifa
                    .Where(i => i.coditem == coditem && i.codtarifa_a == codtarifa_a && i.codtarifa_b == codtarifa_b)
                    .FirstOrDefaultAsync();

                    if (initem_controltarifa == null)
                    {
                        return NotFound( new { resp = "No existe un registro con los datos proporcionados" });
                    }

                    _context.initem_controltarifa.Remove(initem_controltarifa);
                    await _context.SaveChangesAsync();

                    return Ok( new { resp = "208" });   // eliminado con exito
                }
                

            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }

        private bool initem_controltarifaExists(int id, DBContext _context)
        {
            return (_context.initem_controltarifa?.Any(e => e.id == id)).GetValueOrDefault();

        }

    }
}
