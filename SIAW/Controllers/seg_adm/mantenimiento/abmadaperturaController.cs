using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using siaw_DBContext.Data;
using siaw_DBContext.Models;
using Microsoft.AspNetCore.Authorization;
using siaw_funciones;

namespace SIAW.Controllers.seg_adm.mantenimiento
{
    [Route("api/seg_adm/mant/[controller]")]
    [ApiController]
    public class abmadaperturaController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        private readonly Seguridad seguridad = new Seguridad();
        public abmadaperturaController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }


        // GET: api/adapertura
        [HttpGet]
        [Route("getadapertura/{userConn}")]
        public async Task<ActionResult<IEnumerable<adapertura>>> Getadapertura(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var result = await _context.adapertura
                        .Join(
                            _context.admes,
                            a => a.mes,
                            m => m.codigo,
                            (a, m) => new
                            {
                                a.codigo,
                                a.ano,
                                a.mes,
                                m.descripcion
                            }
                        )
                        .OrderByDescending(ano => ano.ano)
                        .ThenByDescending(mes => mes.mes)
                        .ToListAsync();

                    if (result.Count == 0)
                    {
                        return Ok("No se encontraron datos");
                    }
                    return Ok(result);
                }
                
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }


        }


        // GET: api/semodulo
        [HttpGet]
        [Route("getsemodulo/{userConn}")]
        public async Task<ActionResult<IEnumerable<semodulo>>> Getsemodulo(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var result = await _context.semodulo.OrderBy(codigo => codigo.codigo).ToListAsync();
                    return Ok(result);
                }
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }


        // GET: api/getdetalle
        [HttpGet]
        [Route("getdetalle/{userConn}/{codigo}")]
        public async Task<ActionResult<IEnumerable<semodulo>>> getdetalle(string userConn, int codigo)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var result = await _context.adapertura
                        .Where(p0 => p0.codigo == codigo)
                        .Join(_context.adapertura1, p0 => p0.codigo, p1 => p1.codigo, (p0, p1) => new { p0, p1 })
                        .Join(_context.semodulo, p01 => p01.p1.sistema, p2 => p2.codigo, (p01, p2) => new
                        {
                            ano = p01.p0.ano,
                            mes = p01.p0.mes,
                            codigo = p01.p1.codigo,
                            sistema = p01.p1.sistema,
                            descripcion = p2.descripcion
                        })
                        .OrderBy(result => result.sistema)
                        .ToListAsync();
                    return Ok(result);
                }
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }





        // POST: api/adapertura
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPost("{userConn}/{codigo}")]
        public async Task<ActionResult<adapertura>> Postadapertura(string userConn, int codigo, List<adapertura1> adapertura1)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                using (var dbContexTransaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        // Consulta para obtener los registros que deseas eliminar.
                        var registrosAEliminar = _context.adapertura1.Where(p => p.codigo == codigo);

                        // Marcar los registros como eliminados.
                        foreach (var registro in registrosAEliminar)
                        {
                            _context.adapertura1.Remove(registro);
                        }
                        // Confirmar los cambios en la base de datos.
                        await _context.SaveChangesAsync();

                        // agregar los nuevos campos
                        _context.adapertura1.AddRange(adapertura1);

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

        // GET: api/
        [HttpGet]
        [Route("verifPeriodoAbierto/{userConn}/{fecha}/{modulo}")]
        public async Task<ActionResult<bool>> verifPeriodoAbierto(string userConn, DateTime fecha, int modulo)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                try
                {
                    bool periodoAbierto = await seguridad.periodo_fechaabierta(userConnectionString, fecha, modulo);
                    return Ok(periodoAbierto);
                }
                catch (Exception)
                {
                    return Problem("Error en el Servidor");
                    throw;
                }
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }
    }
}
