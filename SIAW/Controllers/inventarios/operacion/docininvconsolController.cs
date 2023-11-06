using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using siaw_DBContext.Models;
using siaw_funciones;
using Microsoft.AspNetCore.Authorization;
using System.Web.Http.Results;

namespace SIAW.Controllers.inventarios.operacion
{
    [Route("api/inventario/oper/[controller]")]
    [ApiController]
    public class docininvconsolController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        private readonly Inventario inventario = new Inventario();
        private readonly Nombres nombres = new Nombres();
        public docininvconsolController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }

        // GET: api/existeInventario
        [HttpGet]
        [Route("existeInventario/{userConn}/{id}/{numeroid}")]
        public async Task<ActionResult<bool>> existeInventario(string userConn, string id, int numeroid)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                bool existeinv = await inventario.existeinv(userConnectionString, id, numeroid);
                 return Ok(existeinv);
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
                throw;
            }
        }

        // GET: api/invFisConsolIfAbierto
        [HttpGet]
        [Route("invFisConsolIfAbierto/{userConn}/{codigo}")]
        public async Task<ActionResult<bool>> invFisConsolIfAbierto(string userConn, int codigo)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                bool existeinv = await inventario.InventarioFisicoConsolidadoEstaAbierto(userConnectionString, codigo);
                return Ok(existeinv);
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
                throw;
            }
        }

        // GET: api/cargarCabecera
        [HttpGet]
        [Route("cargarCabecera/{userConn}/{id}/{numeroid}")]
        public async Task<ActionResult<bool>> cargarCabecera(string userConn, string id, int numeroid)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);
                var result = new data_ininvconsol_cab();
                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    result = await _context.ininvconsol
                    .Where(i => i.id == id && i.numeroid == numeroid)
                    .Select(i => new data_ininvconsol_cab
                    {
                        codigo = i.codigo,
                        id = i.id,
                        numeroid = (int)i.numeroid,
                        fechainicio = i.fechainicio,
                        fechafin = i.fechafin,
                        obs = i.obs,
                        codpersona = i.codpersona,
                        descpersona = "",
                        codalmacen = i.codalmacen,
                        descalmacen = "",
                        horareg = i.horareg,
                        fechareg = i.fechareg,
                        usuarioreg = i.usuarioreg,
                        abierto = (bool)i.abierto
                    })
                    .FirstOrDefaultAsync();

                    if (result == null)
                    {
                        return NotFound("No se encontraron registros con los datos proporcionados.");
                    }

                    
                }
                result.descpersona = await nombres.nombre_persona(userConnectionString, result.codpersona);
                result.descalmacen = await nombres.nombre_almacen(userConnectionString, result.codalmacen);

                return Ok(result);
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
                throw;
            }
        }


        // GET: api/mostrardetalle
        [HttpGet]
        [Route("mostrardetalle/{userConn}/{codigo}")]
        public async Task<ActionResult<bool>> mostrardetalle(string userConn, int codigo)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var result = await _context.ininvconsol1
                    .Join(_context.initem, c => c.coditem, i => i.codigo, (c,i) => new
                    {
                        c,i
                    })
                    .Where(x => x.c.codinvconsol == codigo)
                    .OrderBy(x => x.c.coditem)
                    .Select(x => new
                    {
                        x.c.codinvconsol,
                        x.c.coditem,
                        x.i.descripcion,
                        x.i.medida,
                        x.c.cantreal,
                        x.c.udm,
                        x.c.cantsist,
                        x.c.dif
                    })
                    .ToListAsync();

                    if (result.Count() == 0)
                    {
                        return NotFound("No se encontraron registros con los datos proporcionados.");
                    }
                    return Ok(result);
                }
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
                throw;
            }
        }


        // PUT: api/ininvconsol/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPut]
        [Route("updateAbiertoCerrado/{userConn}/{codigo}/{abierto}")]
        public async Task<IActionResult> updateTarifa1(string userConn, int codigo, bool abierto)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                var ininvconsol = await _context.ininvconsol.Where(i => i.codigo == codigo).FirstOrDefaultAsync();
                if (ininvconsol == null)
                {
                    return NotFound("No se Encontraron registros con los datos proporcionados.");
                }

                ininvconsol.abierto = abierto;

                _context.Entry(ininvconsol).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    return Problem("Error en el Servidor.");
                }
                if (abierto)
                {
                    return Ok(new {resp = "Se abrio el inventario. Ahora puede modificarlo." });
                }
                return Ok(new { resp = "Se cerró el inventario. Ya no puede modificarlo." });
            }
        }





    }

    public class data_ininvconsol_cab
    {
        public int codigo { get; set; }
        public string id { get; set; }
        public int numeroid { get; set; }
        public DateTime fechainicio { get; set; }
        public DateTime fechafin { get; set; }
        public string obs { get; set; }
        public int codpersona { get; set; }
        public string descpersona { get; set; }
        public int codalmacen { get; set; }
        public string descalmacen { get; set; }
        public string horareg { get; set; }
        public DateTime fechareg { get; set; }
        public string usuarioreg { get; set; }
        public bool abierto { get; set; }
    }
}
