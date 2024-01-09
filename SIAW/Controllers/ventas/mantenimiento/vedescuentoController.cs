using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using siaw_DBContext.Data;
using siaw_DBContext.Models;
using Microsoft.AspNetCore.Authorization;

namespace SIAW.Controllers.ventas.mantenimiento
{
    [Route("api/venta/mant/[controller]")]
    [ApiController]
    public class vedescuentoController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        public vedescuentoController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }

        // GET: api/vedescuento
        [HttpGet("{userConn}")]
        public async Task<ActionResult<IEnumerable<vedescuento>>> Getvedescuento(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.vedescuento == null)
                    {
                        return BadRequest(new { resp = "Entidad vedescuento es null." });
                    }

                    var result = await _context.vedescuento
                        .Join(
                            _context.veempaque,
                            vd => vd.codempaque,
                            ve => ve.codigo,
                            (vd, ve) => new { vd, ve }
                        )
                        .Join(
                            _context.admoneda,
                            x => x.vd.moneda,
                            am => am.codigo,
                            (x, am) => new { x.vd, x.ve, am }
                        )
                        .OrderBy(x => x.vd.codigo)
                        .Select(x => new
                        {
                            codigo = x.vd.codigo,
                            descripcion = x.vd.descripcion,
                            codempaque = x.vd.codempaque,
                            empaqueDescrip = x.ve.descripcion,

                            monto = x.vd.monto,
                            moneda = x.vd.moneda,

                            monedaDescrip = x.am.descripcion,

                            descuento = x.vd.descuento,
                            ultimos = x.vd.ultimos,
                            horareg = x.vd.horareg,
                            fechareg = x.vd.fechareg,
                            usuarioreg = x.vd.usuarioreg,
                            habilitado = x.vd.habilitado,
                            desde_fecha = x.vd.desde_fecha,
                            hasta_fecha = x.vd.hasta_fecha
                        })
                        .ToListAsync();




                    return Ok(result);
                }
                
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }


        }

        // GET: api/vedescuento/5
        [HttpGet("{userConn}/{codigo}")]
        public async Task<ActionResult<vedescuento>> Getvedescuento(string userConn, int codigo)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.vedescuento == null)
                    {
                        return BadRequest(new { resp = "Entidad vedescuento es null." });
                    }
                    var vedescuento = await _context.vedescuento
                        .Where(i => i.codigo == codigo)
                        .Join(
                            _context.veempaque,
                            vd => vd.codempaque,
                            ve => ve.codigo,
                            (vd, ve) => new { vd, ve }
                        )
                        .Join(
                            _context.admoneda,
                            x => x.vd.moneda,
                            am => am.codigo,
                            (x, am) => new { x.vd, x.ve, am }
                        )
                        .Select(x => new
                        {
                            codigo = x.vd.codigo,
                            descripcion = x.vd.descripcion,
                            codempaque = x.vd.codempaque,
                            empaqueDescrip = x.ve.descripcion,

                            monto = x.vd.monto,
                            moneda = x.vd.moneda,

                            monedaDescrip = x.am.descripcion,

                            descuento = x.vd.descuento,
                            ultimos = x.vd.ultimos,
                            horareg = x.vd.horareg,
                            fechareg = x.vd.fechareg,
                            usuarioreg = x.vd.usuarioreg,
                            habilitado = x.vd.habilitado,
                            desde_fecha = x.vd.desde_fecha,
                            hasta_fecha = x.vd.hasta_fecha
                        })
                        .FirstOrDefaultAsync();

                    if (vedescuento == null)
                    {
                        return NotFound( new { resp = "No se encontro un registro con este código" });
                    }

                    return Ok(vedescuento);
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
        public async Task<ActionResult<IEnumerable<vedescuento>>> Getvedescuento_catalogo(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var query = _context.vedescuento
                    .OrderBy(i => i.codigo)
                    .Select(i => new
                    {
                        i.codigo,
                        i.descripcion
                    });

                    var result = query.ToList();
                    return Ok(result);
                }
                
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
                throw;
            }
        }

        // PUT: api/vedescuento/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPut("{userConn}/{codigo}")]
        public async Task<IActionResult> Putvedescuento(string userConn, int codigo, vedescuento vedescuento)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (codigo != vedescuento.codigo)
                {
                    return BadRequest( new { resp = "Error con Id en datos proporcionados." });
                }

                _context.Entry(vedescuento).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!vedescuentoExists(codigo, _context))
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

        // POST: api/vedescuento
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPost("{userConn}")]
        public async Task<ActionResult<vedescuento>> Postvedescuento(string userConn, vedescuento vedescuento)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (_context.vedescuento == null)
                {
                    return BadRequest(new { resp = "Entidad vedescuento es null." });
                }
                _context.vedescuento.Add(vedescuento);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (vedescuentoExists(vedescuento.codigo, _context))
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

        // DELETE: api/vedescuento/5
        [Authorize]
        [HttpDelete("{userConn}/{codigo}")]
        public async Task<IActionResult> Deletevedescuento(string userConn, int codigo)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.vedescuento == null)
                    {
                        return BadRequest(new { resp = "Entidad vedescuento es null." });
                    }
                    var vedescuento = await _context.vedescuento.FindAsync(codigo);
                    if (vedescuento == null)
                    {
                        return NotFound( new { resp = "No existe un registro con ese código" });
                    }

                    _context.vedescuento.Remove(vedescuento);
                    await _context.SaveChangesAsync();

                    return Ok( new { resp = "208" });   // eliminado con exito
                }
                

            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }

        private bool vedescuentoExists(int codigo, DBContext _context)
        {
            return (_context.vedescuento?.Any(e => e.codigo == codigo)).GetValueOrDefault();

        }




        // GET: api/vedescuento
        [HttpGet]
        [Route("vedescuento_tarifa/{userConn}/{coddescuento}")]
        public async Task<ActionResult<IEnumerable<vedescuento_tarifa>>> Getvedescuento_tarifa(string userConn, int coddescuento)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.vedescuento == null)
                    {
                        return BadRequest(new { resp = "Entidad vedescuento es null." });
                    }

                    var result = await _context.vedescuento_tarifa
                        .Where(i => i.coddescuento == coddescuento)
                        .Join(
                            _context.intarifa,
                            c => c.codtarifa,
                            t => t.codigo,
                            (c, t) => new { c, t }
                        )
                        .OrderBy(x => x.c.codtarifa)
                        .Select(x => new
                        {
                            coddescuento = x.c.coddescuento,
                            codtarifa = x.c.codtarifa,
                            descripcion = x.t.descripcion
                        })
                        .ToListAsync();

                    return Ok(result);
                }
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }


        }



        // POST: api/vedescuento_tarifa
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPost]
        [Route("vedescuento_tarifa/{userConn}")]
        public async Task<ActionResult<vedescuento_tarifa>> Postvedescuento_tarifa(string userConn, vedescuento_tarifa vedescuento_tarifa)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                var valida = await _context.vedescuento_tarifa
                    .Where(i => i.coddescuento == vedescuento_tarifa.coddescuento && i.codtarifa == vedescuento_tarifa.codtarifa)
                    .FirstOrDefaultAsync();
                if (valida != null)
                {
                    return Conflict( new { resp = "Ya existe un registro con los datos proporcionados"});
                }
                _context.vedescuento_tarifa.Add(vedescuento_tarifa);
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

        // DELETE: api/vedescuento_tarifa/5
        [Authorize]
        [HttpDelete]
        [Route("vedescuento_tarifa/{userConn}/{coddescuento}/{codtarifa}")]
        public async Task<IActionResult> Deletevedescuento_tarifa(string userConn, int coddescuento, int codtarifa)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var vedescuento_tarifa = await _context.vedescuento_tarifa
                        .Where(i => i.coddescuento == coddescuento && i.codtarifa == codtarifa)
                        .FirstOrDefaultAsync();
                    if (vedescuento_tarifa == null)
                    {
                        return NotFound( new { resp = "No se encontraron registros con los datos proporcionados." });
                    }

                    _context.vedescuento_tarifa.Remove(vedescuento_tarifa);
                    await _context.SaveChangesAsync();

                    return Ok( new { resp = "208" });   // eliminado con exito
                }
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }



        // DELETE: api/vedescuento_tarifa/5
        [Authorize]
        [HttpDelete]
        [Route("deleteTodo_vedescuento_tarifa/{userConn}/{coddescuento}")]
        public async Task<IActionResult> deleteTodo_vedescuento_tarifa(string userConn, int coddescuento)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var vedescuento_tarifa = await _context.vedescuento_tarifa
                        .Where(i => i.coddescuento == coddescuento)
                        .ToListAsync();
                    if (vedescuento_tarifa.Count() > 0)
                    {
                        _context.vedescuento_tarifa.RemoveRange(vedescuento_tarifa);
                        await _context.SaveChangesAsync();
                        return Ok( new { resp = "208" });   // eliminado con exito

                    }
                    return NotFound( new { resp = "No se encontraron registros con los datos proporcionados." });
                }
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }




        // GET: api/vedescuento1
        [HttpGet]
        [Route("vedescuento1/{userConn}/{coddescuento}")]
        public async Task<ActionResult<IEnumerable<vedescuento1>>> Getvedescuento1(string userConn, int coddescuento)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.vedescuento == null)
                    {
                        return BadRequest(new { resp = "Entidad vedescuento es null." });
                    }

                    var result = await _context.vedescuento1
                        .Join(
                            _context.inlinea,
                            d => d.codlinea,
                            l => l.codigo,
                            (d, l) => new
                            {
                                d.codigo,
                                d.coddescuento,
                                d.codlinea,
                                d.descuento,
                                l.descripcion
                            }
                        )
                        .Where(x => x.coddescuento == coddescuento)
                        .OrderBy(x => x.codlinea)
                        .ToListAsync();

                    return Ok(result);
                }
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }


        }




        // POST: api/vedescuento1
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPost]
        [Route("vedescuento1/{userConn}")]
        public async Task<ActionResult<vedescuento1>> Postvedescuento1(string userConn, vedescuento1 vedescuento1)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                var ultimoRegistro = await _context.vedescuento1.OrderByDescending(v => v.codigo).FirstOrDefaultAsync();
                if (ultimoRegistro == null)
                {
                    return NotFound( new { resp = "No existe ningun registro (vedescuento1)." });
                }
                var newCodigo = ultimoRegistro.codigo + 1;

                var valida = await _context.vedescuento1
                    .Where(i => i.codlinea == vedescuento1.codlinea && i.coddescuento == vedescuento1.coddescuento)
                    .FirstOrDefaultAsync();
                if (valida != null)
                {
                    return Conflict( new { resp = "Ya existe un registro con los datos proporcionados"});
                }
                vedescuento1.codigo = newCodigo;   
                _context.vedescuento1.Add(vedescuento1);
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







        // DELETE: api/vedescuento1/5
        [Authorize]
        [HttpDelete]
        [Route("vedescuento1/{userConn}/{codigo}/{coddescuento}")]
        public async Task<IActionResult> vedescuento1(string userConn, int codigo, int coddescuento)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                using (var dbContexTransaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        // verifica vedecuento 1
                        var vedescuento1 = await _context.vedescuento1
                        .Where(i => i.codigo == codigo && i.coddescuento == coddescuento)
                        .FirstOrDefaultAsync();
                        if (vedescuento1 == null)
                        {
                            return NotFound( new { resp = "No se encontraron registros con los datos proporcionados." });
                        }


                        // delete vedescuento2
                        var vedescuento2 = await _context.vedescuento2
                        .Where(i => i.coddescuento1 == codigo)
                        .ToListAsync();
                        if (vedescuento2.Count() > 0)
                        {
                            _context.vedescuento2.RemoveRange(vedescuento2);
                            await _context.SaveChangesAsync();
                        }


                        // delete vedecuento 1
                        _context.vedescuento1.Remove(vedescuento1);
                        await _context.SaveChangesAsync();

                        dbContexTransaction.Commit();
                        return Ok( new { resp = "208" });   // eliminado con exito
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








        // REVISAR Y VALIUDA  BOTON LIMPIAR
        // DELETE: api/vedescuento1/5
        [Authorize]
        [HttpDelete]
        [Route("deleteTodo_vedescuento1/{userConn}/{coddescuento}")]
        public async Task<IActionResult> deleteTodo_vedescuento1(string userConn, int coddescuento)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                using (var dbContexTransaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        // eliminar primero las ramas (items)

                        var codigosDescuento1 = await _context.vedescuento1
                        .Where(d1 => d1.coddescuento == coddescuento)
                        .Select(d1 => d1.codigo)
                        .ToListAsync();
                        if (codigosDescuento1.Count() == 0)
                        {
                            return NotFound( new { resp = "No se encontraron registros con los datos proporcionados." });
                        }
                        var descuentos2AEliminar = await _context.vedescuento2
                            .Where(d2 => codigosDescuento1.Contains(d2.coddescuento1))
                            .ToListAsync();
                        if (descuentos2AEliminar.Count() > 0)
                        {
                            _context.vedescuento2.RemoveRange(descuentos2AEliminar);
                            await _context.SaveChangesAsync();
                        }

                        // eliminar luego las lineas 
                        var descuentosAEliminar = await _context.vedescuento1
                            .Where(d1 => d1.coddescuento == coddescuento)
                            .ToListAsync();
                        _context.vedescuento1.RemoveRange(descuentosAEliminar);
                        await _context.SaveChangesAsync();
                        dbContexTransaction.Commit();

                        return Ok( new { resp = "208" });   // eliminado con exito
                    }
                    catch (Exception)
                    {
                        dbContexTransaction.Rollback();
                        return Problem("Error en el servidor");
                    }
                }
                    
            }
        }

    }
}
