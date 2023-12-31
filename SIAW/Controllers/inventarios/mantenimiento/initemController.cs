﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NuGet.Configuration;
using siaw_DBContext.Data;
using siaw_DBContext.Models;
using System.Net;
using Microsoft.AspNetCore.Authorization;

namespace SIAW.Controllers.inventarios.mantenimiento
{
    [Route("api/inventario/mant/[controller]")]
    [ApiController]
    public class initemController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
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

                //var _context = _userConnectionManager.GetUserConnection(userId);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.initem == null)
                    {
                        return Problem("Entidad initem es null.");
                    }
                    //var result = await _context.initem.OrderByDescending(fechareg => fechareg.fechareg).ToListAsync();
                    //return Ok(result);
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
                return BadRequest("Error en el servidor");
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

                //var _context = _userConnectionManager.GetUserConnection(userId);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.initem == null)
                    {
                        return Problem("Entidad initem es null.");
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
                        return NotFound("No se encontro un registro con este código");
                    }

                    return Ok(result);
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
        public async Task<ActionResult<IEnumerable<initem>>> Getinitem_catalogo_resumido(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                //var _context = _userConnectionManager.GetUserConnection(userId);

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
                        return Problem("Entidad initem es null.");
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


        // GET: api/catalogo2
        [HttpGet]
        [Route("catalogo2/{userConn}")]
        public async Task<ActionResult<IEnumerable<initem>>> Getinitem_catalogo(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                //var _context = _userConnectionManager.GetUserConnection(userId);

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
                        return Problem("Entidad initem es null.");
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


        // GET: api/catalogo3
        [HttpGet]
        [Route("catalogo3/{userConn}")]
        public async Task<ActionResult<IEnumerable<initem>>> Getinitem_catalogo3(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                //var _context = _userConnectionManager.GetUserConnection(userId);

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
                        return Problem("Entidad initem es null.");
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


        // PUT: api/initem/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPut("{userConn}/{codigo}")]
        public async Task<IActionResult> Putinitem(string userConn, string codigo, initem initem)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            //var _context = _userConnectionManager.GetUserConnection(userId);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (codigo != initem.codigo)
                {
                    return BadRequest("Error con Id en datos proporcionados.");
                }

                _context.Entry(initem).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!initemExists(codigo, _context))
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

        // POST: api/initem
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPost("{userConn}")]
        public async Task<ActionResult<initem>> Postinitem(string userConn, initem initem)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            //var _context = _userConnectionManager.GetUserConnection(userId);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (_context.initem == null)
                {
                    return Problem("Entidad initem es null.");
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

        // DELETE: api/initem/5
        [Authorize]
        [HttpDelete("{userConn}/{codigo}")]
        public async Task<IActionResult> Deleteinitem(string userConn, string codigo)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                //var _context = _userConnectionManager.GetUserConnection(userId);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.initem == null)
                    {
                        return Problem("Entidad initem es null.");
                    }
                    var initem = await _context.initem.FindAsync(codigo);
                    if (initem == null)
                    {
                        return NotFound("No existe un registro con ese código");
                    }

                    _context.initem.Remove(initem);
                    await _context.SaveChangesAsync();

                    return Ok("208");   // eliminado con exito
                }
                

            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
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

                //var _context = _userConnectionManager.GetUserConnection(userId);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.inctrlstock == null)
                    {
                        return Problem("Entidad inctrlstock es null.");
                    }
                    var result = await _context.inctrlstock.OrderBy(id => id.id).ToListAsync();
                    return Ok(result);
                }
                
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
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

                //var _context = _userConnectionManager.GetUserConnection(userId);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.inctrlstock == null)
                    {
                        return Problem("Entidad inctrlstock es null.");
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
                        return NotFound("No se encontro un registro con este código");
                    }

                    return Ok(result);
                }
                
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }

        // PUT: api/inctrlstock/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPut("{userConn}/{id}")]
        public async Task<IActionResult> Putinctrlstock(string userConn, int id, inctrlstock inctrlstock)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            //var _context = _userConnectionManager.GetUserConnection(userId);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (id != inctrlstock.id)
                {
                    return BadRequest("Error con Id en datos proporcionados.");
                }

                _context.Entry(inctrlstock).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!inctrlstockExists(id, _context))
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

        // POST: api/inctrlstock
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPost("{userConn}")]
        public async Task<ActionResult<inctrlstock>> Postinctrlstock(string userConn, inctrlstock inctrlstock)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            //var _context = _userConnectionManager.GetUserConnection(userId);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (_context.inctrlstock == null)
                {
                    return Problem("Entidad inctrlstock es null.");
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

        // DELETE: api/inctrlstock/5
        [Authorize]
        [HttpDelete("{userConn}/{id}")]
        public async Task<IActionResult> Deleteinctrlstock(string userConn, int id)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                //var _context = _userConnectionManager.GetUserConnection(userId);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.inctrlstock == null)
                    {
                        return Problem("Entidad inctrlstock es null.");
                    }
                    var inctrlstock = await _context.inctrlstock.FindAsync(id);
                    if (inctrlstock == null)
                    {
                        return NotFound("No existe un registro con ese código");
                    }

                    _context.inctrlstock.Remove(inctrlstock);
                    await _context.SaveChangesAsync();

                    return Ok("208");   // eliminado con exito
                }
                

            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
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

                //var _context = _userConnectionManager.GetUserConnection(userId);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.initem_max == null)
                    {
                        return Problem("Entidad initem_max es null.");
                    }
                    var result = await _context.initem_max.OrderBy(id => id.id).ToListAsync();
                    return Ok(result);
                }
                
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
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

                //var _context = _userConnectionManager.GetUserConnection(userId);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.initem_max == null)
                    {
                        return Problem("Entidad initem_max es null.");
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
                        return NotFound("No se encontro un registro con este código");
                    }

                    return Ok(result);
                }
                
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }

        // PUT: api/initem_max/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPut("{userConn}/{id}")]
        public async Task<IActionResult> Putinitem_max(string userConn, int id, initem_max initem_max)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            //var _context = _userConnectionManager.GetUserConnection(userId);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (id != initem_max.id)
                {
                    return BadRequest("Error con Id en datos proporcionados.");
                }

                _context.Entry(initem_max).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!initem_maxExists(id, _context))
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

        // POST: api/initem_max
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPost("{userConn}")]
        public async Task<ActionResult<initem_max>> Postinitem_max(string userConn, initem_max initem_max)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            //var _context = _userConnectionManager.GetUserConnection(userId);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (_context.initem_max == null)
                {
                    return Problem("Entidad initem_max es null.");
                }
                _context.initem_max.Add(initem_max);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (initem_maxExists(initem_max.id, _context))
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

        // DELETE: api/initem_max/5
        [Authorize]
        [HttpDelete("{userConn}/{id}")]
        public async Task<IActionResult> Deleteinitem_max(string userConn, int id)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                //var _context = _userConnectionManager.GetUserConnection(userId);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.initem_max == null)
                    {
                        return Problem("Entidad initem_max es null.");
                    }
                    var initem_max = await _context.initem_max.FindAsync(id);
                    if (initem_max == null)
                    {
                        return NotFound("No existe un registro con ese código");
                    }

                    _context.initem_max.Remove(initem_max);
                    await _context.SaveChangesAsync();

                    return Ok("208");   // eliminado con exito
                }
                

            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }

        private bool initem_maxExists(int id, DBContext _context)
        {
            return (_context.initem_max?.Any(e => e.id == id)).GetValueOrDefault();

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

                //var _context = _userConnectionManager.GetUserConnection(userId);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.initem_controltarifa == null)
                    {
                        return Problem("Entidad initem_controltarifa es null.");
                    }
                    var result = await _context.initem_controltarifa.OrderBy(id => id.id).ToListAsync();
                    return Ok(result);
                }
                
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
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

                //var _context = _userConnectionManager.GetUserConnection(userId);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.initem_controltarifa == null)
                    {
                        return Problem("Entidad initem_controltarifa es null.");
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
                        return NotFound("No se encontro un registro con este código");
                    }

                    return Ok(result);
                }
                
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }

        // PUT: api/initem_controltarifa/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPut("{userConn}/{id}")]
        public async Task<IActionResult> Putinitem_controltarifa(string userConn, int id, initem_controltarifa initem_controltarifa)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            //var _context = _userConnectionManager.GetUserConnection(userId);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (id != initem_controltarifa.id)
                {
                    return BadRequest("Error con Id en datos proporcionados.");
                }

                _context.Entry(initem_controltarifa).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!initem_controltarifaExists(id, _context))
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

        // POST: api/initem_controltarifa
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPost("{userConn}")]
        public async Task<ActionResult<initem_controltarifa>> Postinitem_controltarifa(string userConn, initem_controltarifa initem_controltarifa)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            //var _context = _userConnectionManager.GetUserConnection(userId);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (_context.initem_controltarifa == null)
                {
                    return Problem("Entidad initem_controltarifa es null.");
                }
                _context.initem_controltarifa.Add(initem_controltarifa);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (initem_controltarifaExists(initem_controltarifa.id, _context))
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

        // DELETE: api/initem_controltarifa/5
        [Authorize]
        [HttpDelete("{userConn}/{id}")]
        public async Task<IActionResult> Deleteinitem_controltarifa(string userConn, int id)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                //var _context = _userConnectionManager.GetUserConnection(userId);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.initem_controltarifa == null)
                    {
                        return Problem("Entidad initem_controltarifa es null.");
                    }
                    var initem_controltarifa = await _context.initem_controltarifa.FindAsync(id);
                    if (initem_controltarifa == null)
                    {
                        return NotFound("No existe un registro con ese código");
                    }

                    _context.initem_controltarifa.Remove(initem_controltarifa);
                    await _context.SaveChangesAsync();

                    return Ok("208");   // eliminado con exito
                }
                

            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }

        private bool initem_controltarifaExists(int id, DBContext _context)
        {
            return (_context.initem_controltarifa?.Any(e => e.id == id)).GetValueOrDefault();

        }

    }
}
