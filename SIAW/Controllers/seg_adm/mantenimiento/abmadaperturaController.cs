using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using siaw_DBContext.Data;
using siaw_DBContext.Models;
using Microsoft.AspNetCore.Authorization;
using siaw_funciones;
using System.Linq;
using Microsoft.Win32;

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
                        return BadRequest(new { resp = "No se encontraron registros." });
                    }
                    return Ok(result);
                }
                
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
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
                return Problem("Error en el servidor");
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

                    var modulosCerrados1 = await _context.adapertura
                       .Where(p0 => p0.codigo == codigo)
                       .Join(_context.adapertura1, p0 => p0.codigo, p1 => p1.codigo, (p0, p1) => new 
                           {
                               ano = p0.ano,
                               mes = p0.mes,
                               codigo = p1.codigo,
                               sistema = p1.sistema
                           }
                        )
                       .OrderBy(result => result.sistema)
                       .ToListAsync();
                    if (modulosCerrados1.Count()==0)
                    {
                        return NotFound(new { resp = "No se tienen modulos cerrados para este periodo de tiempo." });
                    }
                    // obtenemos todos los modulos.
                    var modulos = await _context.semodulo.OrderBy(codigo => codigo.codigo).ToListAsync();

                    var codDefect = modulosCerrados1[0].codigo;
                    var anoDefect = modulosCerrados1[0].ano;
                    var mesDefect = modulosCerrados1[0].mes;


                    var resultadoFinal = modulos
                    .GroupJoin(
                        modulosCerrados1,
                        modulo => modulo.codigo,
                        cerrado => cerrado.sistema,
                        (modulo, cerradoGroup) => new
                        {
                            ano = cerradoGroup.Select(c => c?.ano).FirstOrDefault() ?? anoDefect,
                            mes = cerradoGroup.Select(c => c?.mes).FirstOrDefault() ?? mesDefect,
                            codigo = cerradoGroup.Select(c => c?.codigo).FirstOrDefault() ?? codDefect,
                            sistema = modulo.codigo,
                            descripcion = modulo.descripcion,
                            check = cerradoGroup.Any() // True si hay coincidencia, false si no hay coincidencia
                        }
                    )
                    .OrderBy(result => result.sistema)
                    .ToList();


                    return Ok(resultadoFinal);
                }
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
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
                        var registrosAEliminar = await _context.adapertura1.Where(p => p.codigo == codigo).ToListAsync();
                        if (registrosAEliminar.Count()>0)
                        {
                            _context.adapertura1.RemoveRange(registrosAEliminar);
                            await _context.SaveChangesAsync();
                        }
                        // agregar los nuevos campos
                        _context.adapertura1.AddRange(adapertura1);

                        await _context.SaveChangesAsync();

                        dbContexTransaction.Commit();
                        return Ok( new { resp = "204" });   // creado con exito
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
    }
}
