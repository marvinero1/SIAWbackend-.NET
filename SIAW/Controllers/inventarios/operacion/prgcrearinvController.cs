using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using siaw_DBContext.Models;
using siaw_funciones;
using Microsoft.AspNetCore.Authorization;

namespace SIAW.Controllers.inventarios.operacion
{
    [Route("api/inventario/oper/[controller]")]
    [ApiController]
    public class prgcrearinvController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        private readonly Inventario inventario = new Inventario();
        private readonly Seguridad seguridad = new Seguridad();
        public prgcrearinvController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }


        // GET: api/catalogo
        [HttpGet]
        [Route("catalogointipoinv/{userConn}")]
        public async Task<ActionResult<IEnumerable<intipoinv>>> Getintipoinv_catalogo(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var query = _context.intipoinv
                    .OrderBy(i => i.id)
                    .Select(i => new
                    {
                        id = i.id,
                        descripcion = i.descripcion,
                        nroactual = i.nroactual
                    });

                    var result = query.ToList();

                    if (result.Count() == 0)
                    {
                        return Problem("Entidad intipoinv es null.");
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



        // POST: api/ininvconsol
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPost("{userConn}/{id}/{numeroid}")]
        public async Task<ActionResult<ininvconsol>> Postininvconsol(string userConn, ininvconsol ininvconsol)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            bool existeinv = await inventario.existeinv(userConnectionString, ininvconsol.id, (int)ininvconsol.numeroid);
            bool periodo_fechaabierta = await seguridad.periodo_fechaabierta(userConnectionString, ininvconsol.fechafin, 2);

            if (existeinv)
            {
                return Ok(new {resp = "Ya existe un Inventario con ese Id y Numero Id" });
            }
            if (!periodo_fechaabierta)
            {
                return Ok(new { resp = "No puede crear documentos para ese periodo de fechas. Periodo Cerrado" });
            }

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (_context.ininvconsol == null)
                {
                    return Problem("Entidad ininvconsol es null.");
                }
                using (var dbContexTransaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        _context.ininvconsol.Add(ininvconsol);
                        await _context.SaveChangesAsync();

                        var intipoinv = await _context.intipoinv.Where(i => i.id == ininvconsol.id).FirstOrDefaultAsync();
                        intipoinv.nroactual = (int)ininvconsol.numeroid;

                        _context.Entry(intipoinv).State = EntityState.Modified;
                        await _context.SaveChangesAsync();

                        dbContexTransaction.Commit();
                        return Ok("204");   // creado con exito
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
