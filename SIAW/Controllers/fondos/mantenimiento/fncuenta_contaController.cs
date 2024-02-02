using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using siaw_DBContext.Data;
using siaw_DBContext.Models;
using Microsoft.AspNetCore.Authorization;

namespace SIAW.Controllers.fondos.mantenimiento
{
    [Route("api/fondos/mant/[controller]")]
    [ApiController]
    public class fncuenta_contaController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        public fncuenta_contaController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }

        // GET: api/fncuenta_conta
        [Authorize]
        [HttpPost]
        [Route("consulta/{userConn}")]
        public async Task<ActionResult<IEnumerable<fncuenta_conta>>> Getfncuenta_conta(string userConn, obtenerDatosFncuenta_cnt fncuentaData )
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    string idcuenta = fncuentaData.idcuenta;
                    string codunidad = fncuentaData.codunidad;
                    int codalmacen = fncuentaData.codalmacen;



                    if (_context.fncuenta_conta == null)
                    {
                        return BadRequest(new { resp = "Entidad fncuenta_conta es null." });
                    }
                    var result = await _context.fncuenta_conta
                        .Where(f => f.idcuenta == idcuenta && f.codunidad == codunidad && f.codalmacen == codalmacen)
                        .FirstOrDefaultAsync();
                    if (result == null)
                    {
                        return BadRequest(new { resp = "No se encontro información con los datos proporcionados." });
                    }
                    return Ok(result);
                }
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }

        // POST: api/fncuenta_conta
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPost("{userConn}")]
        public async Task<ActionResult<fncuenta_conta>> Postfncuenta_conta(string userConn, fncuenta_conta fncuenta_conta)
        {
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            string idcuenta = fncuenta_conta.idcuenta;
            string codunidad = fncuenta_conta.codunidad;
            int codalmacen = fncuenta_conta.codalmacen;
            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                try
                {
                    var result = await _context.fncuenta_conta
                        .Where(f => f.idcuenta == idcuenta && f.codunidad == codunidad && f.codalmacen == codalmacen)
                        .FirstOrDefaultAsync();
                    if (result == null) // si no hay datos se agrega
                    {
                        _context.fncuenta_conta.Add(fncuenta_conta);
                        await _context.SaveChangesAsync();

                        return Ok(new { resp = "204" });   // creado con exito
                    }

                    // si hay datos solo se actualiza
                    result.cta = fncuenta_conta.cta;
                    result.cta_aux = fncuenta_conta.cta_aux;
                    result.cta_cc = fncuenta_conta.cta_cc;
                    _context.Entry(result).State = EntityState.Modified;
                    await _context.SaveChangesAsync();

                    return Ok(new { resp = "206" });   // actualizado con exito
                }
                catch (Exception)
                {
                    return Problem("Error en el servidor");
                }
            }
        }

    }

    public class obtenerDatosFncuenta_cnt
    {
        public string idcuenta { get; set; }
        public string codunidad { get; set; }
        public int codalmacen { get; set; }
    }

}
