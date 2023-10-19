using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using siaw_DBContext.Data;
using siaw_DBContext.Models;
using Microsoft.AspNetCore.Authorization;

namespace SIAW.Controllers.seg_adm.mantenimiento
{
    [Route("api/seg_adm/mant/[controller]")]
    [ApiController]
    public class abmadaperturaController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
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


        // GET: api/semodulo
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



    }
}
