using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using siaw_DBContext.Data;
using siaw_DBContext.Models;
using siaw_funciones;

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
        public async Task<IActionResult> Putveplanpago(string userConn, int codigo, DataVeplanpago dataveplanpago)
        {
            veplanpago veplanpago = dataveplanpago.veplanpago;
            List<veplanpago1> veplanpago1List = dataveplanpago.veplanpago1;
        // Obtener el contexto de base de datos correspondiente al usuario
        string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                using (var dbContexTransaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        if (codigo != veplanpago.codigo)
                        {
                            return BadRequest(new { resp = "Error con Id en datos proporcionados." });
                        }
                        if (!veplanpagoExists(codigo, _context))
                        {
                            return NotFound(new { resp = "No existe un registro con ese código" });
                        }

                        if (veplanpago1List.Count() > 0)
                        {
                            // Actualizar veplanpago1
                            foreach (var veplanpago1 in veplanpago1List)
                            {
                                _context.Entry(veplanpago1).State = EntityState.Modified;
                            }
                        }
                        _context.Entry(veplanpago).State = EntityState.Modified;
                        await _context.SaveChangesAsync();

                        dbContexTransaction.Commit();
                        return Ok(new { resp = "206" });   // actualizado con exito
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
    public class DataVeplanpago
    {
        public veplanpago veplanpago { get; set; }
        public List<veplanpago1> veplanpago1 { get; set; }

    }









    [Route("api/venta/mant/[controller]")]
    [ApiController]
    public class veplanpago2Controller : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        public veplanpago2Controller(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }

        // POST: api/veplanpago2
        [Authorize]
        [HttpPost("{userConn}/{codplanpago1}/{nrocuotas}")]
        public async Task<ActionResult<IEnumerable<veplanpago2>>> crearcuotas(string userConn, int codplanpago1, int nrocuotas)
        {
            if (nrocuotas < 1)
            {
                return BadRequest(new { resp = "El número de cuotas no puede ser menor a 1" });
            }
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                using (var dbContexTransaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        // actualizamos nrocuotas de veplanpago1
                        var veplanpago1Update = await _context.veplanpago1
                            .Where(v => v.codigo == codplanpago1)
                            .FirstOrDefaultAsync();
                        if (veplanpago1Update == null)
                        {
                            return BadRequest(new { resp = "No se encontraron registros (codigo veplanpago1)" });
                        }
                        veplanpago1Update.nrocuotas = (short)nrocuotas;

                        _context.Entry(veplanpago1Update).State = EntityState.Modified;
                        await _context.SaveChangesAsync();

                        // genera los planes de pago (veplanpago2)

                        var veplanpago2List = await _context.veplanpago2
                            .Where(v => v.coddetplanpago == codplanpago1)
                            .ToListAsync();
                        int veplanpago2Count = veplanpago2List.Count();
                        // verifica si ya tiene cuotas y coinciden en numero
                        if (veplanpago2Count != nrocuotas)
                        {
                            if (veplanpago2Count > 0)
                            {
                                // si hay datos se eliminan para volver a crearlos
                                _context.veplanpago2.RemoveRange(veplanpago2List);
                                await _context.SaveChangesAsync();
                            }
                            //creacion de nuevos veplanpago2
                            double porcencuota = Math.Round((Math.Floor((100.0 / nrocuotas) * 100.0)) / 100.0, 2, MidpointRounding.AwayFromZero);

                            double primeracuota = Math.Round(100.0 - (porcencuota * (nrocuotas - 1)), 2, MidpointRounding.AwayFromZero);

                            List<veplanpago2> ListaAddVeplanpago2 = new List<veplanpago2>();

                            for (int i = 1; i <= nrocuotas; i++)
                            {
                                veplanpago2 itemVeplanpago2 = new veplanpago2();
                                itemVeplanpago2.coddetplanpago = codplanpago1;
                                itemVeplanpago2.nrocuota = i;
                                itemVeplanpago2.porcen = (decimal)porcencuota;
                                itemVeplanpago2.diaspago = 15;
                                ListaAddVeplanpago2.Add(itemVeplanpago2);
                            }
                            ListaAddVeplanpago2[0].porcen = (decimal)primeracuota;

                            _context.veplanpago2.AddRange(ListaAddVeplanpago2);
                            await _context.SaveChangesAsync();

                            dbContexTransaction.Commit();

                            veplanpago2List = ListaAddVeplanpago2;
                        }
                        return Ok(veplanpago2List);
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


        // PUT: api/veplanpago2/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPut("{userConn}/{codplanpago1}")]
        public async Task<IActionResult> updateVeplanpago2(string userConn, int codplanpago1, List<veplanpago2> veplanpago2)
        {
            var sumaPorcent = veplanpago2.Sum(v => v.porcen);
            if (sumaPorcent != 100)
            {
                return BadRequest(new { resp = "La suma de los porcentajes debe ser igual al 100 %" });
            }

            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                using (var dbContexTransaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        var veplanpago2List = await _context.veplanpago2
                            .Where(v => v.coddetplanpago == codplanpago1)
                            .ToListAsync();
                        if (veplanpago2List.Count() > 0)
                        {
                            // si hay datos se eliminan para volver a crearlos
                            _context.veplanpago2.RemoveRange(veplanpago2List);
                            await _context.SaveChangesAsync();
                        }

                        // agregar los datos actualizados
                        _context.veplanpago2.AddRange(veplanpago2);
                        await _context.SaveChangesAsync();


                        dbContexTransaction.Commit();
                        return Ok(new { resp = "206" });   // actualizado con exito
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


    }



}
