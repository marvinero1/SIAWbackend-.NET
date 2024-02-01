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
    public class fndeudor_contaController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        public fndeudor_contaController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }

        // GET: api/fndeudor_conta
        [HttpGet("{userConn}/{iddeudor}/{codunidad}/{codalmacen}")]
        public async Task<ActionResult<IEnumerable<fndeudor_conta>>> Getfndeudor_conta(string userConn, string iddeudor, string codunidad, int codalmacen)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.fndeudor_conta == null)
                    {
                        return BadRequest(new { resp = "Entidad fndeudor_conta es null." });
                    }
                    var result = await _context.fndeudor_conta
                        .Where(f => f.iddeudor == iddeudor && f.codunidad == codunidad && f.codalmacen == codalmacen)
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

        // POST: api/fndeudor_conta
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPost("{userConn}")]
        public async Task<ActionResult<fndeudor_conta>> Postfndeudor_conta(string userConn, fndeudor_conta fndeudor_conta)
        {
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            string iddeudor = fndeudor_conta.iddeudor;
            string codunidad = fndeudor_conta.codunidad;
            int codalmacen = fndeudor_conta.codalmacen;
            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                try
                {
                    var result = await _context.fndeudor_conta
                        .Where(f => f.iddeudor == iddeudor && f.codunidad == codunidad && f.codalmacen == codalmacen)
                        .FirstOrDefaultAsync();
                    if (result == null) // si no hay datos se agrega
                    {
                        _context.fndeudor_conta.Add(fndeudor_conta);
                        await _context.SaveChangesAsync();

                        return Ok(new { resp = "204" });   // creado con exito
                    }

                    // si hay datos solo se actualiza
                    result.cta_porrendir_mn = fndeudor_conta.cta_porrendir_mn;
                    result.cta_porrendir_mn_aux = fndeudor_conta.cta_porrendir_mn_aux;
                    result.cta_porrendir_mn_cc = fndeudor_conta.cta_porrendir_mn_cc;

                    result.cta_porrendir_me = fndeudor_conta.cta_porrendir_me;
                    result.cta_porrendir_me_aux = fndeudor_conta.cta_porrendir_me_aux;
                    result.cta_porrendir_me_cc = fndeudor_conta.cta_porrendir_me_cc;

                    result.cta_porcobrar_mn = fndeudor_conta.cta_porcobrar_mn;
                    result.cta_porcobrar_mn_aux = fndeudor_conta.cta_porcobrar_mn_aux;
                    result.cta_porcobrar_mn_cc = fndeudor_conta.cta_porcobrar_mn_cc;

                    result.cta_porcobrar_me = fndeudor_conta.cta_porcobrar_me;
                    result.cta_porcobrar_me_aux = fndeudor_conta.cta_porcobrar_me_aux;
                    result.cta_porcobrar_me_cc = fndeudor_conta.cta_porcobrar_me_cc;

                    result.cta_porpagar_mn = fndeudor_conta.cta_porpagar_mn;
                    result.cta_porpagar_mn_aux = fndeudor_conta.cta_porpagar_mn_aux;
                    result.cta_porpagar_mn_cc = fndeudor_conta.cta_porpagar_mn_cc;

                    result.cta_porpagar_me = fndeudor_conta.cta_porpagar_me;
                    result.cta_porpagar_me_aux = fndeudor_conta.cta_porpagar_me_aux;
                    result.cta_porpagar_me_cc = fndeudor_conta.cta_porpagar_me_cc;

                    result.cta_prestamo_mn = fndeudor_conta.cta_prestamo_mn;
                    result.cta_prestamo_mn_aux = fndeudor_conta.cta_prestamo_mn_aux;
                    result.cta_prestamo_mn_cc = fndeudor_conta.cta_prestamo_mn_cc;

                    result.cta_prestamo_me = fndeudor_conta.cta_prestamo_me;
                    result.cta_prestamo_me_aux = fndeudor_conta.cta_prestamo_me_aux;
                    result.cta_prestamo_me_cc = fndeudor_conta.cta_prestamo_me_cc;

                    result.cta_anticipo_mn = fndeudor_conta.cta_anticipo_mn;
                    result.cta_anticipo_mn_aux = fndeudor_conta.cta_anticipo_mn_aux;
                    result.cta_anticipo_mn_cc = fndeudor_conta.cta_anticipo_mn_cc;

                    result.cta_anticipo_me = fndeudor_conta.cta_anticipo_me;
                    result.cta_anticipo_me_aux = fndeudor_conta.cta_anticipo_me_aux;
                    result.cta_anticipo_me_cc = fndeudor_conta.cta_anticipo_me_cc;

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


        // POST: api/fndeudor_conta
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPost]
        [Route("copiar/{userConn}")]
        public async Task<ActionResult<fndeudor_conta>> PostCopiarfndeudor_conta(string userConn, fndeudor_conta fndeudor_conta)
        {
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            string iddeudor = fndeudor_conta.iddeudor;
            string codunidad = fndeudor_conta.codunidad;

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                using (var dbContexTransaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        var result = await _context.fndeudor_conta
                            .Where(f => f.iddeudor == iddeudor && f.codunidad == codunidad)
                            .ToListAsync();
                        if (result.Count() > 0) // si no hay datos se agrega
                        {
                            _context.fndeudor_conta.RemoveRange(result);
                            await _context.SaveChangesAsync();
                        }

                        var data_ingresar = await _context.inalmacen
                            .OrderBy(i => i.codigo)
                            .Select(i => new fndeudor_conta
                            {
                                iddeudor = iddeudor,
                                codunidad = codunidad,
                                codalmacen = i.codigo,
                                cta_porrendir_mn = fndeudor_conta.cta_porrendir_mn,
                                cta_porrendir_mn_aux = fndeudor_conta.cta_porrendir_mn_aux,
                                cta_porrendir_mn_cc = fndeudor_conta.cta_porrendir_mn_cc,

                                cta_porrendir_me = fndeudor_conta.cta_porrendir_me,
                                cta_porrendir_me_aux = fndeudor_conta.cta_porrendir_me_aux,
                                cta_porrendir_me_cc = fndeudor_conta.cta_porrendir_me_cc,

                                cta_porcobrar_mn = fndeudor_conta.cta_porcobrar_mn,
                                cta_porcobrar_mn_aux = fndeudor_conta.cta_porcobrar_mn_aux,
                                cta_porcobrar_mn_cc = fndeudor_conta.cta_porcobrar_mn_cc,

                                cta_porcobrar_me = fndeudor_conta.cta_porcobrar_me,
                                cta_porcobrar_me_aux = fndeudor_conta.cta_porcobrar_me_aux,
                                cta_porcobrar_me_cc = fndeudor_conta.cta_porcobrar_me_cc,

                                cta_porpagar_mn = fndeudor_conta.cta_porpagar_mn,
                                cta_porpagar_mn_aux = fndeudor_conta.cta_porpagar_mn_aux,
                                cta_porpagar_mn_cc = fndeudor_conta.cta_porpagar_mn_cc,

                                cta_porpagar_me = fndeudor_conta.cta_porpagar_me,
                                cta_porpagar_me_aux = fndeudor_conta.cta_porpagar_me_aux,
                                cta_porpagar_me_cc = fndeudor_conta.cta_porpagar_me_cc,

                                cta_prestamo_mn = fndeudor_conta.cta_prestamo_mn,
                                cta_prestamo_mn_aux = fndeudor_conta.cta_prestamo_mn_aux,
                                cta_prestamo_mn_cc = fndeudor_conta.cta_prestamo_mn_cc,

                                cta_prestamo_me = fndeudor_conta.cta_prestamo_me,
                                cta_prestamo_me_aux = fndeudor_conta.cta_prestamo_me_aux,
                                cta_prestamo_me_cc = fndeudor_conta.cta_prestamo_me_cc,

                                cta_anticipo_mn = fndeudor_conta.cta_anticipo_mn,
                                cta_anticipo_mn_aux = fndeudor_conta.cta_anticipo_mn_aux,
                                cta_anticipo_mn_cc = fndeudor_conta.cta_anticipo_mn_cc,

                                cta_anticipo_me = fndeudor_conta.cta_anticipo_me,
                                cta_anticipo_me_aux = fndeudor_conta.cta_anticipo_me_aux,
                                cta_anticipo_me_cc = fndeudor_conta.cta_anticipo_me_cc
                            }).ToArrayAsync();



                        _context.fndeudor_conta.AddRange(data_ingresar);
                        await _context.SaveChangesAsync();



                        dbContexTransaction.Commit();
                        return Ok(new { resp = "204" });   // actualizado con exito
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
