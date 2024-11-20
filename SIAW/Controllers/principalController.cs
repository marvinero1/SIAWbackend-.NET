using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using siaw_funciones;

namespace SIAW.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class principalController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;

        private readonly Empresa empresa = new Empresa();
        private readonly TipoCambio tipoCambio = new TipoCambio();

        public principalController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }
        [HttpGet]
        [Route("getParamIniciales/{userConn}/{usuario}")]
        public async Task<object> getParamIniciales(string userConn, string usuario)
        {
            try
            {
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    string codempresa = await _context.adempresa.Select(i => i.codigo).FirstOrDefaultAsync(); 

                    var monBase = await Empresa.monedabase(_context, codempresa);

                    // ACA FALTAN COSAS QUE HACER SI ES QUE NO HAY EL TIPO DE CAMBIO, ES DECIR SI ES LA PRIMERA VEZ QUE SE ABRE EL SIAW

                    var monedatdc = await tipoCambio.monedatdc(_context, usuario, codempresa);

                    // FALTA ####verificar crecaion de nuevo periodo adpaertura

                    var codAlmacenUsr = await _context.adusparametros.Where(i => i.usuario == usuario).Select(i => i.codalmacen).FirstOrDefaultAsync();

                    return Ok(new
                    {
                        codempresa,
                        monBase,
                        monedatdc,
                        codAlmacenUsr,
                    });
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
