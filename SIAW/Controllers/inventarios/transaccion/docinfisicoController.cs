using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using siaw_DBContext.Data;
using siaw_DBContext.Models;
using Microsoft.AspNetCore.Authorization;
using siaw_funciones;
using siaw_DBContext.Models_Extra;

namespace SIAW.Controllers.inventarios.transaccion
{
    [Route("api/[controller]")]
    [ApiController]
    public class docinfisicoController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        private readonly Inventario inventario = new Inventario();
        public docinfisicoController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }

        // GET: api/
        [HttpGet]
        [Route("verifInvGrup/{userConn}/{id}/{numeroid}/{nro}")]
        public async Task<ActionResult<bool>> verifInvGrup(string userConn, string id, int numeroid, int nro)
        {
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                try
                {
                    var result = await _context.ininvconsol
                        .Where(i => i.id == id && i.numeroid == numeroid)
                        .FirstOrDefaultAsync();
                    if (result == null)
                    {
                        return BadRequest("Ese documento de inventario no existe (Doc Inventario)");
                    }
                    int res_codinvconsol = result.codigo;
                    var dataGroup = await _context.ingrupoper
                        .Where(i => i.codinvconsol == res_codinvconsol && i.nro == nro)
                        .FirstOrDefaultAsync();
                    if (dataGroup == null)
                    {
                        return BadRequest("Ese documento de inventario no existe (Grupo)");
                    }
                    int res_codgrupo = dataGroup.codigo;
                    return Ok(new {codinvconsol = res_codinvconsol, codgrupo = res_codgrupo});
                }
                catch (Exception)
                {
                    Problem("Error en el servidor");
                    throw;
                }
            }
        }

        // GET: api/
        [HttpGet]
        [Route("verifRegInvExist/{userConn}/{id}/{numeroid}/{nro}")]
        public async Task<ActionResult<bool>> verifRegInvExist(string userConn, int codinventario, int codgrupo)
        {
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            try
            {
                bool existe = await inventario.RegistroInventarioExiste(userConnectionString, codinventario, codgrupo);
                if (existe)
                {
                    return Ok(new { resp = "Ya se registro esa toma de inventario, Para llevar a cabo modificaciones por favor entre a Revision de Toma de Inventario." });
                }
            }
            catch (Exception)
            {

                throw;
            }
        }


    }
}
