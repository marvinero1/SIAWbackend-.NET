using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using siaw_DBContext.Data;
using siaw_DBContext.Models;
using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;

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

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.adtipocambio == null)
                    {
                        return BadRequest(new { resp = "Entidad adtipocambio es null." });
                    }
                    var result = await _context.adtipocambio.ToListAsync();
                    return Ok(result);
                }
                
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
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
                        return BadRequest(new { resp = "Entidad adtipocambio es null." });
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
                return Problem("Error en el servidor");
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
                        return BadRequest(new { resp = "Entidad adtipocambio es null." });
                    }
                    var adtipocambio = await _context.adtipocambio
                        .Where(x => x.fecha == fecha)
                        .ToListAsync();
                    if (adtipocambio == null )
                    {
                        return NotFound( new { resp = "701" });
                    }
                    return Ok(adtipocambio);
                }
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
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

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.adtipocambio == null)
                    {
                        return BadRequest(new { resp = "Entidad adtipocambio es null." });
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
                return Problem("Error en el servidor");
            }
        }



        /// <summary>
        /// Obtiene una El valor de la moneda
        /// </summary>
        /// <returns></returns>
        // GET: api/adtipocambio/5
        [HttpGet]
        [Route("getmonedaValor/{userConn}/{monBase}/{moneda}/{fecha}")]
        public async Task<ActionResult<adtipocambio>> getmonedaValor(string userConn, string monBase, string moneda, DateTime fecha)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);


                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    //var respuesta = new SqlParameter("@respuesta", System.Data.SqlDbType.Decimal);
                    SqlParameter resultado = new SqlParameter("@v_resultado", System.Data.SqlDbType.Decimal);
                    resultado.Direction = System.Data.ParameterDirection.Output;
                    resultado.Precision = 18;
                    resultado.Scale = 2;

                    // Llama al procedimiento almacenado
                    await _context.Database.ExecuteSqlRawAsync(
                        "EXEC SIA00002_TipoCambio @v_resultado OUTPUT, @p_monBase, @p_moneda, @p_fecha",
                        resultado,
                        new SqlParameter("@p_monBase", monBase),
                        new SqlParameter("@p_moneda", moneda),
                        new SqlParameter("@p_fecha", fecha));

                    return Ok(new { valor = resultado.Value });


                }

            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
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

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                var tipocambio = _context.adtipocambio.FirstOrDefault(objeto => objeto.moneda == moneda && objeto.fecha == fecha && objeto.codalmacen == codalmacen);
                if (tipocambio == null)
                {
                    return NotFound( new { resp = "No existe un registro con esa información" });
                }
                tipocambio.factor = adtipocambio.factor;
                _context.Entry(tipocambio).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    return Problem("Error en el servidor");
                }

                return Ok( new { resp = "206" });   // actualizado con exito
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

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (_context.adtipocambio == null)
                {
                    return BadRequest(new { resp = "Entidad adtipocambio es null." });
                }
                _context.adtipocambio.Add(adtipocambio);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    return Problem("Error en el servidor");
                }

                return Ok( new { resp = "204" });   // creado con exito

            }
            
        }


        // POST: api/adtipocambio
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPost]
        [Route("addTipoCambioAlm/{userConn}/{monBase}/{fecha}/{usuario}")]
        public async Task<ActionResult<adtipocambio>> PostaddTipoCambioAlm(string userConn, string monBase, DateTime fecha, string usuario)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                var almacenes = await _context.inalmacen
                    .OrderBy(a => a.codigo)
                    .Select(a => a.codigo)
                    .ToListAsync();

                var monedas = await _context.admoneda
                    .Where(a => a.codigo != monBase)
                    .ToListAsync();

                List<adtipocambio> tiposCambioAdd= new List<adtipocambio>();

                foreach (var mon in monedas)
                {
                    foreach (var almacen in almacenes)
                    {
                        adtipocambio objeto = new adtipocambio();
                        var verifica = await _context.adtipocambio
                            .Where(a => a.fecha == fecha && a.moneda == mon.codigo && a.codalmacen == almacen)
                            .FirstOrDefaultAsync();
                        if (verifica == null)
                        {
                            objeto.monedabase = monBase;
                            objeto.factor = 1;
                            objeto.moneda = mon.codigo;
                            objeto.fecha = fecha;
                            objeto.usuarioreg = usuario;
                            objeto.codalmacen = almacen;
                            tiposCambioAdd.Add(objeto);
                        }
                    }
                }
                _context.adtipocambio.AddRange(tiposCambioAdd);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    return Problem("Error en el servidor");
                }

                return Ok( new { resp = "204" });   // creado con exito

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

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (_context.adtipocambio == null)
                {
                    return BadRequest(new { resp = "Entidad adtipocambio es null." });
                }
                _context.adtipocambio.AddRange(adtipocambio);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    return Problem("Error en el servidor");
                }
                return Ok( new { resp = "204" });   // creado con exito
            }
        }



        // DELETE: api/adtipocambio/5
        [Authorize]
        [HttpDelete]
        [Route("deletetipoCambioFecha/{userConn}/{monedabase}/{fecha}")]
        public async Task<IActionResult> Deleteadtipocambio(string userConn, string monedabase, DateTime fecha)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.adtipocambio == null)
                    {
                        return BadRequest(new { resp = "Entidad adtipocambio es null." });
                    }
                   
                    var adtipocambio = await _context.adtipocambio
                        .Where(a => a.monedabase == monedabase && a.fecha == fecha)
                        .ToListAsync();
                    if (adtipocambio.Count() == 0)
                    {
                        return NotFound(new { resp = "No se encontraron registros con los datos proporcionados." });
                    }

                    _context.adtipocambio.RemoveRange(adtipocambio);
                    await _context.SaveChangesAsync();
                    return Ok( new { resp = "208" });   // eliminado con exito
                }
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }




        // PUT: api/adtipocambio
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPut]
        [Route("Updatelisttipocambio/{userConn}/{monedabase}/{fecha}")]
        public async Task<ActionResult<adtipocambio>> UpdateListadtipocambio(string userConn, string monedabase, DateTime fecha, List<adtipocambio> adtipocambio)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                var dataVerif = await _context.adtipocambio
                        .Where(a => a.monedabase == monedabase && a.fecha == fecha)
                        .ToListAsync();
                if (dataVerif.Count() == 0)
                {
                    return NotFound(new { resp = "No se encontraron registros con los datos proporcionados." });
                }
                using (var dbContexTransaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        // eliminar grupo que se tiene 
                        _context.adtipocambio.RemoveRange(adtipocambio);
                        await _context.SaveChangesAsync();

                        // volver a agregar los datos
                        _context.adtipocambio.AddRange(adtipocambio);
                        await _context.SaveChangesAsync();
                        
                        dbContexTransaction.Commit();
                        return Ok( new { resp = "206" });   // creado con exito
                    }
                    catch (DbUpdateException)
                    {
                        dbContexTransaction.Rollback();
                        return Problem("Error en el servidor");
                        throw;
                    }
                }
                    
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
