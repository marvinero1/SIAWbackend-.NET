using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using siaw_DBContext.Data;
using siaw_DBContext.Models;

namespace SIAW.Controllers.ventas.mantenimiento
{
    [Route("api/venta/mant/[controller]")]
    [ApiController]
    public class veplanpagoController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        public veplanpagoController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }

        // GET: api/veplanpago
        [HttpGet("{userConn}")]
        public async Task<ActionResult<IEnumerable<veplanpago>>> Getveplanpago(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                //var _context = _userConnectionManager.GetUserConnection(userId);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var result = await _context.veplanpago.OrderByDescending(fechareg => fechareg.fechareg).ToListAsync();
                    /*

                    var verecargo = await _context.verecargo
                        .Join(_context.admoneda,
                        vr => vr.moneda,
                        am => am.codigo,
                        (vr, am) => new
                        {
                            codigo = vr.codigo,
                            descorta = vr.descorta,
                            descripcion = vr.descripcion,
                            porcentaje = vr.porcentaje,
                            monto = vr.monto,
                            moneda = vr.moneda,
                            mondesc = am.descripcion,
                            montopor = vr.montopor,
                            modificable = vr.modificable,
                            horareg = vr.horareg,
                            fechareg = vr.fechareg,
                            usuarioreg = vr.usuarioreg
                        })
                        .Where(c => c.codigo == codigo)
                        .FirstOrDefaultAsync();
                    */
                    return Ok(result);
                }
                
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }


        }

        // GET: api/veplanpago/5
        [HttpGet("{userConn}/{codigo}")]
        public async Task<ActionResult<veplanpago>> Getveplanpago(string userConn, int codigo)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.veplanpago == null)
                    {
                        return BadRequest(new { resp = "Entidad veplanpago es null." });
                    }
                    var veplanpago = await _context.veplanpago.FindAsync(codigo);

                    if (veplanpago == null)
                    {
                        return NotFound( new { resp = "No se encontro un registro con este código" });
                    }

                    return Ok(veplanpago);
                }
                
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }




        // GET: api/veplanpago
        [HttpGet]
        [Route("getveplanpagoAll/{userConn}")]
        public async Task<ActionResult<IEnumerable<veplanpago>>> GetveplanpagoAll(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var result = await _context.veplanpago
                        .Join(_context.admoneda,
                        vr => vr.moneda,
                        am => am.codigo,
                        (vr, am) => new
                        {
                            codigo = vr.codigo,
                            descripcion = vr.descripcion,
                            estadop = vr.estadop,
                            moneda = vr.moneda,
                            mondesc = am.descripcion,
                            horareg = vr.horareg,
                            fechareg = vr.fechareg,
                            Usuarioreg = vr.Usuarioreg,
                            diasextrappnc = vr.diasextrappnc,
                        })
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

        // GET: api/veplanpago/5
        [HttpGet]
        [Route("veplanpagoCabecera/{userConn}/{codigo}")]
        public async Task<ActionResult<veplanpago>> GetveplanpagoCabecera(string userConn, int codigo)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.veplanpago == null)
                    {
                        return BadRequest(new { resp = "Entidad veplanpago es null." });
                    }
                    var veplanpago = await _context.veplanpago
                        .Join(_context.admoneda,
                        vr => vr.moneda,
                        am => am.codigo,
                        (vr, am) => new
                        {
                            codigo = vr.codigo,
                            descripcion = vr.descripcion,
                            estadop = vr.estadop,
                            moneda = vr.moneda,
                            mondesc = am.descripcion,
                            horareg = vr.horareg,
                            fechareg = vr.fechareg,
                            Usuarioreg = vr.Usuarioreg,
                            diasextrappnc = vr.diasextrappnc,
                        })
                        .Where(c => c.codigo == codigo)
                        .FirstOrDefaultAsync();

                    if (veplanpago == null)
                    {
                        return NotFound( new { resp = "No se encontro un registro con este código" });
                    }

                    return Ok(veplanpago);
                }

            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }

        // GET: api/veplanpago/5
        [HttpGet]
        [Route("veplanpago1Cuerpo/{userConn}/{codigo}")]
        public async Task<ActionResult<veplanpago1>> Getveplanpago1Cuerpo(string userConn, int codigo)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var veplanpago1 = await _context.veplanpago1
                        .Where(c => c.codplanpago == codigo)
                        .ToListAsync();
                    return Ok(veplanpago1);
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
        public async Task<ActionResult<IEnumerable<veplanpago>>> catalogoveplanpago(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var veplanpago = await _context.veplanpago
                        .Select(c => new
                        {
                            c.codigo,
                            c.descripcion,
                            c.estadop
                        })
                        .ToListAsync();

                    return Ok(veplanpago);
                }
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
                throw;
            }
        }

        // PUT: api/veplanpago/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPut("{userConn}/{codigo}")]
        public async Task<IActionResult> Putveplanpago(string userConn, int codigo, veplanpago veplanpago)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (codigo != veplanpago.codigo)
                {
                    return BadRequest( new { resp = "Error con Id en datos proporcionados." });
                }

                _context.Entry(veplanpago).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!veplanpagoExists(codigo, _context))
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

        // POST: api/veplanpago
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPost("{userConn}")]
        public async Task<ActionResult<veplanpago>> Postveplanpago(string userConn, veplanpago veplanpago)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (_context.veplanpago == null)
                {
                    return BadRequest(new { resp = "Entidad veplanpago es null." });
                }
                _context.veplanpago.Add(veplanpago);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (veplanpagoExists(veplanpago.codigo, _context))
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

        // DELETE: api/veplanpago/5
        [Authorize]
        [HttpDelete("{userConn}/{codigo}")]
        public async Task<IActionResult> Deleteveplanpago(string userConn, int codigo)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.veplanpago == null)
                    {
                        return BadRequest(new { resp = "Entidad veplanpago es null." });
                    }
                    var veplanpago = await _context.veplanpago.FindAsync(codigo);
                    if (veplanpago == null)
                    {
                        return NotFound( new { resp = "No existe un registro con ese código" });
                    }

                    _context.veplanpago.Remove(veplanpago);
                    await _context.SaveChangesAsync();

                    return Ok( new { resp = "208" });   // eliminado con exito
                }
                

            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }

        private bool veplanpagoExists(int codigo, DBContext _context)
        {
            return (_context.veplanpago?.Any(e => e.codigo == codigo)).GetValueOrDefault();

        }




        // POST: api/veplanpago1
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPost]
        [Route("addveplanpago1/{userConn}")]
        public async Task<ActionResult<veplanpago>> Postveplanpago1(string userConn, veplanpago1 veplanpago1)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);
            bool haySolapamientos = false;
            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                var nuevoMontoDel = veplanpago1.montodel;
                var nuevoMontoAl = veplanpago1.montoal;


                var rangos = await _context.veplanpago1
                    .Where(v => v.codplanpago == veplanpago1.codplanpago)
                    .ToListAsync();

                if (nuevoMontoDel > nuevoMontoAl)
                {
                    return Conflict(new { resp = "El inicio del rango de Montos no puede ser mayor al final del mismo." });
                }

                // ver que no interfiera con los otros rangos
                haySolapamientos = rangos.Any(p =>
                (nuevoMontoAl < p.montoal && nuevoMontoAl > p.montodel) ||
                (nuevoMontoDel < p.montoal && nuevoMontoDel > p.montodel));
                if (haySolapamientos)
                {
                    return Conflict(new { resp = "El rango que desea añadir interfiere con uno de los rangos ya creados." });
                }

                // ver que no encierre a otro rango
                haySolapamientos = rangos.Any(p =>
                (nuevoMontoDel < p.montodel && nuevoMontoAl > p.montoal));
                if (haySolapamientos)
                {
                    return Conflict(new { resp = "El rango que desea añadir interfiere con uno de los rangos ya creados." });
                }

                // ve que no haya uno igual
                haySolapamientos = rangos.Any(p =>
                (nuevoMontoAl == p.montoal && nuevoMontoDel == p.montodel));
                if (haySolapamientos)
                {
                    return Conflict(new { resp = "El rango que desea añadir interfiere con uno de los rangos ya creados." });
                }

                // ve que no haya uno con el mismo montodel
                haySolapamientos = rangos.Any(p =>
                (nuevoMontoDel == p.montodel));
                if (haySolapamientos)
                {
                    return Conflict(new { resp = "El rango que desea añadir interfiere con uno de los rangos ya creados." });
                }

                // ve que no haya uno con el mismo montoal
                haySolapamientos = rangos.Any(p =>
                (nuevoMontoAl == p.montoal));
                if (haySolapamientos)
                {
                    return Conflict(new { resp = "El rango que desea añadir interfiere con uno de los rangos ya creados." });
                }

                // todo validado entonces añadir
                
                _context.veplanpago1.Add(veplanpago1);
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

        // DELETE: api/veplanpago/5
        [Authorize]
        [HttpDelete]
        [Route("deleteveplanpago1/{userConn}/{codigo}/{codplanpago}")]
        public async Task<IActionResult> Deleteveplanpago1(string userConn, int codigo, int codplanpago)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                using (var dbContexTransaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        // verifica si existe el veplanpago1
                        var veplanpago1 = await _context.veplanpago1
                            .Where(c => c.codplanpago == codplanpago && c.codigo == codigo)
                            .FirstOrDefaultAsync();
                        if (veplanpago1 == null)
                        {
                            return NotFound( new { resp = "No se encontraron registros con los datos proporcionados." });
                        }

                        // se elimina primero veplanpago2 que seria cuerpo de veplanpago1
                        var veplanpago2 = await _context.veplanpago2
                            .Where(c => c.coddetplanpago == codigo)
                            .ToListAsync();
                        if (veplanpago2.Count() > 0)
                        {
                            _context.veplanpago2.RemoveRange(veplanpago2);
                            await _context.SaveChangesAsync();
                        }
                        // se elimina veplanpago1
                        _context.veplanpago1.Remove(veplanpago1);
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


    }
}
