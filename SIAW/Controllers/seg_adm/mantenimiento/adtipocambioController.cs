using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIAW.Data;
using SIAW.Models;
using System.Net;
using Microsoft.AspNetCore.Authorization;

namespace SIAW.Controllers.seg_adm.mantenimiento
{
    [Route("api/seg_adm/mant/[controller]")]
    [ApiController]
    public class adtipocambioController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        public adtipocambioController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }

        // GET: api/adtipocambio
        [HttpGet("{userConn}")]
        public async Task<ActionResult<IEnumerable<adtipocambio>>> Getadtipocambio(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                //var _context = _userConnectionManager.GetUserConnection(userId);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.adtipocambio == null)
                    {
                        return Problem("Entidad adtipocambio es null.");
                    }
                    var result = await _context.adtipocambio.ToListAsync();
                    return Ok(result);
                }
                
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }


        }

        /// <summary>
        /// Obtiene los registros por fecha
        /// </summary>
        /// <param name="userConn"></param>
        /// <param name="fecha"></param>
        /// <returns></returns>
        // GET: api/adtipocambio/5
        [HttpGet]
        [Route("verificaTipocambioFecha/{userConn}/{fecha}")]
        public async Task<ActionResult<adtipocambio>> Verificaadtipocambio(string userConn, DateTime fecha)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.adtipocambio == null)
                    {
                        return Problem("Entidad adtipocambio es null.");
                    }

                    var adtipocambio = await _context.adtipocambio.Where(x => x.fecha == fecha).OrderByDescending(f => f.fecha).ToListAsync();
                    var monedas = await getAllmoneda(userConnectionString);
                    if (adtipocambio.Count() == 0 && monedas.Count()!=adtipocambio.Count())
                    {
                        return Ok(false);
                    }

                    return Ok(true);
                }
                
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }



        /// <summary>
        /// Obtiene los registros por fecha
        /// </summary>
        /// <param name="userConn"></param>
        /// <param name="fecha"></param>
        /// <returns></returns>
        // GET: api/adtipocambio/5
        [HttpGet]
        [Route("getTipocambioFecha/{userConn}/{fecha}")]
        public async Task<ActionResult<adtipocambio>> Getadtipocambio(string userConn, DateTime fecha)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.adtipocambio == null)
                    {
                        return Problem("Entidad adtipocambio es null.");
                    }

                    var adtipocambio = await _context.adtipocambio
                        .Where(x => x.fecha == fecha && x.moneda=="US")
                        .FirstOrDefaultAsync();
                    if (adtipocambio == null )
                    {
                        return NotFound("701");
                    }

                    return Ok(adtipocambio);
                }

            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }



        /// <summary>
        /// Obtiene una lista de la tabla adtipocambio, buscando la última fecha con registros
        /// </summary>
        /// <returns></returns>
        // GET: api/adtipocambio/5
        [HttpGet]
        [Route("getTipocambioAnt/{userConn}")]
        public async Task<ActionResult<adtipocambio>> GetTipocambAnt(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                //var _context = _userConnectionManager.GetUserConnection(userId);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.adtipocambio == null)
                    {
                        return Problem("Entidad adtipocambio es null.");
                    }
                    DateTime fechaActual = DateTime.Now;
                    var f = fechaActual.ToString("yyyy-MM-dd"); //importante
                    DateTime fe = DateTime.Parse(f);  //importante

                    var resultado = _context.adtipocambio.Where(x => x.fecha == fe).ToList();
                    do
                    {
                        fe = fe.AddDays(-1);
                        resultado = _context.adtipocambio.Where(x => x.fecha == fe).ToList();
                    } while (resultado.Count == 0);

                    
                    return Ok(resultado);
                }
                
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }



        // PUT: api/adtipocambio/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPut("{userConn}/{moneda}/{fecha}/{codalmacen}")]
        public async Task<IActionResult> Putadtipocambio(string userConn, string moneda, DateTime fecha, int codalmacen, adtipocambio adtipocambio)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            //var _context = _userConnectionManager.GetUserConnection(userId);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                var tipocambio = _context.adtipocambio.FirstOrDefault(objeto => objeto.moneda == moneda && objeto.fecha == fecha && objeto.codalmacen == codalmacen);
                if (tipocambio == null)
                {
                    return NotFound("No existe un registro con esa información");
                }
                tipocambio.factor = adtipocambio.factor;
                _context.Entry(tipocambio).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    return BadRequest("Existen problemas con el Servidor.");
                }

                return Ok("206");   // actualizado con exito
            }
            


        }

        // POST: api/adtipocambio
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPost("{userConn}")]
        public async Task<ActionResult<adtipocambio>> Postadtipocambio(string userConn, adtipocambio adtipocambio)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            //var _context = _userConnectionManager.GetUserConnection(userId);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (_context.adtipocambio == null)
                {
                    return Problem("Entidad adtipocambio es null.");
                }
                _context.adtipocambio.Add(adtipocambio);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    return BadRequest("Error en el Servidor");
                }

                return Ok("204");   // creado con exito

            }
            
        }




        // POST: api/adtipocambio
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPost]
        [Route("addlisttipocambio/{userConn}")]
        public async Task<ActionResult<adtipocambio>> PostListadtipocambio(string userConn, List<adtipocambio> adtipocambio)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            //var _context = _userConnectionManager.GetUserConnection(userId);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (_context.adtipocambio == null)
                {
                    return Problem("Entidad adtipocambio es null.");
                }
                _context.adtipocambio.AddRange(adtipocambio);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    return BadRequest("Error en el Servidor");
                }

                return Ok("204");   // creado con exito

            }
            
        }



        // DELETE: api/adtipocambio/5
        [Authorize]
        [HttpDelete("{userConn}/{moneda}/{fecha}/{codalmacen}")]
        public async Task<IActionResult> Deleteadtipocambio(string userConn, string moneda, DateTime fecha, int codalmacen)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                //var _context = _userConnectionManager.GetUserConnection(userId);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.adtipocambio == null)
                    {
                        return Problem("Entidad adtipocambio es null.");
                    }
                   
                    var adtipocambio = _context.adtipocambio.FirstOrDefault(objeto => objeto.moneda == moneda && objeto.fecha == fecha && objeto.codalmacen == codalmacen);
                    if (adtipocambio == null)
                    {
                        return NotFound("Revise los datos ingresados.");
                    }

                    _context.adtipocambio.Remove(adtipocambio);
                    await _context.SaveChangesAsync();

                    return Ok("208");   // eliminado con exito
                }
                

            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }










        protected async Task<List<admoneda>> getAllmoneda(string userConnectionString)
        {
            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                var result = await _context.admoneda.OrderByDescending(fechareg => fechareg.fechareg).ToListAsync();
                return result;
            }
        }






    }
}
