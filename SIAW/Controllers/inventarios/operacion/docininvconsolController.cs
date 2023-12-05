using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using siaw_DBContext.Models;
using siaw_funciones;
using Microsoft.AspNetCore.Authorization;
using System.Web.Http.Results;
using siaw_DBContext.Models_Extra;

namespace SIAW.Controllers.inventarios.operacion
{
    [Route("api/inventario/oper/[controller]")]
    [ApiController]
    public class docininvconsolController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        private readonly Inventario inventario = new Inventario();
        private readonly Nombres nombres = new Nombres();
        public docininvconsolController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }

        // GET: api/existeInventario
        [HttpGet]
        [Route("existeInventario/{userConn}/{id}/{numeroid}")]
        public async Task<ActionResult<bool>> existeInventario(string userConn, string id, int numeroid)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                bool existeinv = await inventario.existeinv(userConnectionString, id, numeroid);
                 return Ok(existeinv);
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
                throw;
            }
        }

        // GET: api/invFisConsolIfAbierto
        [HttpGet]
        [Route("invFisConsolIfAbierto/{userConn}/{codigo}")]
        public async Task<ActionResult<bool>> invFisConsolIfAbierto(string userConn, int codigo)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                bool existeinv = await inventario.InventarioFisicoConsolidadoEstaAbierto(userConnectionString, codigo);
                return Ok(existeinv);
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
                throw;
            }
        }

        // GET: api/cargarCabecera
        [HttpGet]
        [Route("cargarCabecera/{userConn}/{id}/{numeroid}")]
        public async Task<ActionResult<data_ininvconsol_cab>> cargarCabecera(string userConn, string id, int numeroid)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);
                var result = new data_ininvconsol_cab();
                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    result = await _context.ininvconsol
                    .Where(i => i.id == id && i.numeroid == numeroid)
                    .Select(i => new data_ininvconsol_cab
                    {
                        codigo = i.codigo,
                        id = i.id,
                        numeroid = (int)i.numeroid,
                        fechainicio = i.fechainicio,
                        fechafin = i.fechafin,
                        obs = i.obs,
                        codpersona = i.codpersona,
                        descpersona = "",
                        codalmacen = i.codalmacen,
                        descalmacen = "",
                        horareg = i.horareg,
                        fechareg = i.fechareg,
                        usuarioreg = i.usuarioreg,
                        abierto = (bool)i.abierto
                    })
                    .FirstOrDefaultAsync();

                    if (result == null)
                    {
                        return NotFound("No se encontraron registros con los datos proporcionados.");
                    }

                    
                }
                result.descpersona = await nombres.nombre_persona(userConnectionString, result.codpersona);
                result.descalmacen = await nombres.nombre_almacen(userConnectionString, result.codalmacen);

                return Ok(result);
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
                throw;
            }
        }


        // GET: api/mostrardetalle
        [HttpGet]
        [Route("mostrardetalle/{userConn}/{codigo}")]
        public async Task<ActionResult<List<detalleIninvconsol1>>> mostrardetalle(string userConn, int codigo)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var result = await _context.ininvconsol1
                    .Join(_context.initem, c => c.coditem, i => i.codigo, (c,i) => new
                    {
                        c,i
                    })
                    .Where(x => x.c.codinvconsol == codigo)
                    .OrderBy(x => x.c.coditem)
                    .Select(x => new detalleIninvconsol1
                    {
                        codinvconsol = x.c.codinvconsol,
                        coditem = x.c.coditem,
                        descripcion = x.i.descripcion,
                        medida = x.i.medida,
                        cantreal = x.c.cantreal,
                        udm = x.c.udm,
                        cantsist = x.c.cantsist,
                        dif = x.c.dif
                    })
                    .ToListAsync();

                    if (result.Count() == 0)
                    {
                        return NotFound("No se encontraron registros con los datos proporcionados.");
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


        // PUT: api/ininvconsol/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPut]
        [Route("updateAbiertoCerrado/{userConn}/{codigo}/{abierto}")]
        public async Task<IActionResult> updateTarifa1(string userConn, int codigo, bool abierto)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                var ininvconsol = await _context.ininvconsol.Where(i => i.codigo == codigo).FirstOrDefaultAsync();
                if (ininvconsol == null)
                {
                    return NotFound("No se Encontraron registros con los datos proporcionados.");
                }

                ininvconsol.abierto = abierto;

                _context.Entry(ininvconsol).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    return Problem("Error en el Servidor.");
                }
                if (abierto)
                {
                    return Ok(new {resp = "Se abrio el inventario. Ahora puede modificarlo." });
                }
                return Ok(new { resp = "Se cerró el inventario. Ya no puede modificarlo." });
            }
        }



        // DELETE: api/adarea/5
        [Authorize]
        [HttpDelete]
        [Route("deleteDocInvFisc/{userConn}/{codigo}")]
        public async Task<IActionResult> deleteDocInvFisc(string userConn, int codigo)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    using (var dbContexTransaction = _context.Database.BeginTransaction())
                    {
                        try
                        {
                            // Eliminar infisico1
                            var subquery = await _context.infisico
                                .Where(i => i.codinvconsol == codigo)
                                .Select(i => i.codigo)
                                .ToListAsync();

                            var infisico1 = await _context.infisico1
                                .Where(f => subquery.Contains(f.codfisico))
                                .ToListAsync();


                            //var infisico1 = await _context.infisico1.FindAsync(codigo);
                            _context.infisico1.RemoveRange(infisico1);
                            await _context.SaveChangesAsync();



                            // Eliminar infisico
                            var infisico = await _context.infisico.Where(i => i.codinvconsol == codigo).ToListAsync();
                            _context.infisico.RemoveRange(infisico);
                            await _context.SaveChangesAsync();

                            // Eliminar ingrupoper
                            var ingrupoper = await _context.ingrupoper.Where(i => i.codinvconsol == codigo).ToListAsync();
                            _context.ingrupoper.RemoveRange(ingrupoper);
                            await _context.SaveChangesAsync();

                            // Eliminar ininvconsol1
                            var ininvconsol1 = await _context.ininvconsol1.Where(i => i.codinvconsol == codigo).ToListAsync();
                            _context.ininvconsol1.RemoveRange(ininvconsol1);
                            await _context.SaveChangesAsync();

                            // Eliminar ininvconsol
                            var ininvconsol = await _context.ininvconsol.Where(i => i.codigo == codigo).ToListAsync();
                            _context.ininvconsol.RemoveRange(ininvconsol);
                            await _context.SaveChangesAsync();



                            dbContexTransaction.Commit();
                            return Ok("208");   // eliminado con exito
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
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }


        // POST: api/ininvconsol1
        [Authorize]
        [HttpPost]
        [Route("addDataininvconsol1/{userConn}/{codigo}")]
        public async Task<ActionResult<object>> addDataininvconsol1(string userConn, int codigo, List<ininvconsol1> ininvconsol1)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);
                bool resp = await addData(userConnectionString, codigo, ininvconsol1); 
                if (resp)
                {
                    return Ok("204");   // creado con exito
                }
                return BadRequest(new {resp = "Error al guardar los datos" });


            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }







        // PUT: api/ininvconsol1
        [Authorize]
        [HttpPut]
        [Route("limpiarDataininvconsol1/{userConn}/{codigo}")]
        public async Task<ActionResult<object>> limpiarDataininvconsol1(string userConn, int codigo, List<ininvconsol1> ininvconsol1)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);
                bool resp = await addData(userConnectionString, codigo, ininvconsol1);

                if (resp)
                {
                    using (var _context = DbContextFactory.Create(userConnectionString))
                    {
                        var registrosAActualizar = await _context.infisico
                            .Where(registro => registro.codinvconsol == codigo)
                            .ToListAsync();

                        foreach (var registro in registrosAActualizar)
                        {
                            registro.consolidado = false;
                        }
                        await _context.SaveChangesAsync();

                        return Ok("206");   // actualizado con exito
                    }
                }
                return BadRequest(new { resp = "Error al guardar los datos" });
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }



        private async Task<bool> addData(string userConnectionString, int codigo, List<ininvconsol1> ininvconsol1)
        {
            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                using (var dbContexTransaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        // Eliminar ininvconsol1
                        var ininvconsol1Del = await _context.ininvconsol1.Where(i => i.codinvconsol == codigo).ToListAsync();
                        if (ininvconsol1Del.Count > 0)
                        {
                            _context.ininvconsol1.RemoveRange(ininvconsol1Del);
                            await _context.SaveChangesAsync();
                        }

                        await _context.ininvconsol1.AddRangeAsync(ininvconsol1);
                        await _context.SaveChangesAsync();


                        dbContexTransaction.Commit();
                        return true;   // Insersiones con exito
                    }
                    catch (Exception)
                    {
                        dbContexTransaction.Rollback();
                        return false; // Fallo en insertar
                    }
                }
            }
        }

    }

    public class data_ininvconsol_cab
    {
        public int codigo { get; set; }
        public string id { get; set; }
        public int numeroid { get; set; }
        public DateTime fechainicio { get; set; }
        public DateTime fechafin { get; set; }
        public string obs { get; set; }
        public int codpersona { get; set; }
        public string descpersona { get; set; }
        public int codalmacen { get; set; }
        public string descalmacen { get; set; }
        public string horareg { get; set; }
        public DateTime fechareg { get; set; }
        public string usuarioreg { get; set; }
        public bool abierto { get; set; }
    }
}
