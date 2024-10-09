using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using siaw_DBContext.Models;

namespace SIAW.Controllers.ventas
{
    [Route("api/venta/[controller]")]
    [ApiController]
    public class usoSIAWController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        public usoSIAWController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }
        [HttpGet]
        [Route("ventasbyVendedorSIAW/{userConn}")]
        public async Task<ActionResult<IEnumerable<object>>> getCodVendedorbyPass(string userConn)
        {
            try
            {
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

               DateTime fechaAhora = DateTime.Now.Date;

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    /*
                    var result = _context.adusuario
                        .Where(pl => new[] { "41marcoa", "41jconde", "41jquispe", "41mcopana", "41jhonnyp", "pymes42", "31servclie", "operador3", "31percy", "opergps", "41gchura" }
                        .Contains(pl.login))
                        .GroupJoin(_context.selog,
                            p1 => p1.login,
                            p2 => p2.usuario,
                            (p1, p2Group) => new { p1, p2Group = p2Group.DefaultIfEmpty() })
                        .SelectMany(grp => grp.p2Group.DefaultIfEmpty(), (grp, p2) => new { grp.p1, p2 })
                        .GroupJoin(_context.veproforma,
                            p2 => new { IdDoc = p2.p2.id_doc, NumeroIdDoc = p2.p2.numeroid_doc },
                            p3 => new { IdDoc = p3.Id, NumeroIdDoc = p3.Numeroid },
                            (grp, p3Group) => new { grp.p1, grp.p2, p3Group = p3Group.DefaultIfEmpty() })
                        .SelectMany(grp => grp.p3Group.DefaultIfEmpty(), (grp, p3) => new { grp.p1, grp.p2, p3 })
                        .GroupJoin(_context.pepersona,
                            p1 => p1.Persona,
                            p4 => p4.Codigo,
                            (grp, p4Group) => new { grp.p1, grp.p2, grp.p3, p4Group = p4Group.DefaultIfEmpty() })
                        .SelectMany(grp => grp.p4Group.DefaultIfEmpty(), (grp, p4) => new
                        {
                            persona = p4 != null ? p4.Nombre1 + " " + p4.Apellido1 + " " + p4.Apellido2 : string.Empty,
                            usuario = grp.p1.Login,
                            fecha_grabacion = grp.p2?.Fecha ?? fechaAhora,
                            total_PF_grabadas_SIAW = grp.p3Group != null ? grp.p3Group.Count() : 0 // Si p3Group es una colección
                        })
                        .GroupBy(res => new { res.persona, res.usuario, res.fecha_grabacion })
                        .Select(g => new
                        {
                            persona = g.Key.persona,
                            usuario = g.Key.usuario,
                            fecha_grabacion = g.Key.fecha_grabacion,
                            total_PF_grabadas_SIAW = g.Sum(x => x.total_PF_grabadas_SIAW)
                        })
                        .OrderBy(res => res.usuario)
                        .ThenBy(res => res.fecha_grabacion);

                    return Ok(result);*/
                    return Ok();
                }
            }
            catch (Exception ex)
            {
                return Problem($"Error en el servidor: {ex.Message}");
                throw;
            }
        }
    }
}
