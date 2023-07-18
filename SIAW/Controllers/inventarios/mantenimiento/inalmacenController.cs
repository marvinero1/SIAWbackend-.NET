using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIAW.Data;
using SIAW.Models;
using System.Net;

namespace SIAW.Controllers.inventarios.mantenimiento
{
    [Route("api/inventario/mant/inalmacen/[controller]")]
    [ApiController]
    public class inalmacenController : ControllerBase
    {
        private readonly DBContext _context;
        private readonly string connectionString;
        private VerificaConexion verificador;
        private readonly IConfiguration _configuration;
        public inalmacenController(IConfiguration configuration)
        {
            connectionString = ConnectionController.ConnectionString;
            _context = DbContextFactory.Create(connectionString);
            _configuration = configuration;
            verificador = new VerificaConexion(_configuration);
        }

        // GET: api/inalmacen
        [HttpGet("{conexionName}")]
        public async Task<ActionResult<IEnumerable<inalmacen>>> Getinalmacen(string conexionName)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.inalmacen == null)
                    {
                        return Problem("Entidad inalmacen es null.");
                    }

                    List<inalmacen> linalmacen = _context.inalmacen.ToList();
                    List<adarea> ladarea = _context.adarea.ToList();
                    List<peplanporcen> lpeplanporcen = _context.peplanporcen.ToList();

                    var query = from alm in linalmacen
                                join are in ladarea
                                on alm.codarea equals are.codigo into table1
                                from are in table1.DefaultIfEmpty()
                                join plp in lpeplanporcen
                                on alm.codplanporcen equals plp.codigo into table2
                                from plp in table2.DefaultIfEmpty()
                                orderby alm.fechareg descending
                                select new 
                                {
                                    codigo = alm.codigo,
                                    descripcion = alm.descripcion,
                                    codarea = alm.codarea,
                                    descarea = are.descripcion,
                                    direccion = alm.direccion,
                                    telefono = alm.telefono,
                                    email = alm.email,
                                    tienda = alm.tienda,
                                    codplanporcen = alm.codplanporcen,
                                    descplanporcen = plp.descripcion,
                                    nropersonas = alm.nropersonas,
                                    estandar = alm.estandar,
                                    monestandar = alm.monestandar,
                                    minimo = alm.minimo,
                                    moneda = alm.moneda,
                                    nropatronal = alm.nropatronal,
                                    horareg = alm.horareg,
                                    fechareg = alm.fechareg,
                                    usuarioreg = alm.usuarioreg,
                                    lugar = alm.lugar,
                                    sucursallc = alm.sucursallc,
                                    min_solurgente = alm.min_solurgente,
                                    codmoneda_min_solurgente = alm.codmoneda_min_solurgente,
                                    pesomin = alm.pesomin,
                                    pesoest = alm.pesoest,
                                    porcenmin = alm.porcenmin,
                                    porcenmin_rendi = alm.porcenmin_rendi,
                                    pesoest_rendi = alm.pesoest_rendi,
                                    pesomin_rendi = alm.pesomin_rendi,
                                    idcuenta_caja_mn = alm.idcuenta_caja_mn,
                                    idcuenta_caja_me = alm.idcuenta_caja_me,
                                    graficar = alm.graficar,
                                    analizar_rendimiento = alm.analizar_rendimiento,
                                    actividad = alm.actividad,
                                    latitud = alm.latitud,
                                    longitud = alm.longitud,
                                    fax = alm.fax
                                };
                    return Ok(query);

                }
                return BadRequest("Se perdio la conexion con el servidor");

            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }


        }

        // GET: api/inalmacen/5
        [HttpGet("{conexionName}/{codigo}")]
        public async Task<ActionResult<inalmacen>> Getinalmacen(string conexionName, int codigo)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.inalmacen == null)
                    {
                        return Problem("Entidad inalmacen es null.");
                    }
                    //var inalmacen = await _context.inalmacen.FindAsync(codigo);

                    List<inalmacen> linalmacen = _context.inalmacen.ToList();
                    List<adarea> ladarea = _context.adarea.ToList();
                    List<peplanporcen> lpeplanporcen = _context.peplanporcen.ToList();

                    var query = from alm in linalmacen
                                join are in ladarea
                                on alm.codarea equals are.codigo into table1
                                from are in table1.DefaultIfEmpty()
                                join plp in lpeplanporcen
                                on alm.codplanporcen equals plp.codigo into table2
                                from plp in table2.DefaultIfEmpty()
                                where alm.codigo == codigo
                                select new 
                                {
                                    codigo = alm.codigo,
                                    descripcion = alm.descripcion,
                                    codarea = alm.codarea,
                                    descarea = are.descripcion,
                                    direccion = alm.direccion,
                                    telefono = alm.telefono,
                                    email = alm.email,
                                    tienda = alm.tienda,
                                    codplanporcen = alm.codplanporcen,
                                    descplanporcen = plp.descripcion,
                                    nropersonas = alm.nropersonas,
                                    estandar = alm.estandar,
                                    monestandar = alm.monestandar,
                                    minimo = alm.minimo,
                                    moneda = alm.moneda,
                                    nropatronal = alm.nropatronal,
                                    horareg = alm.horareg,
                                    fechareg = alm.fechareg,
                                    usuarioreg = alm.usuarioreg,
                                    lugar = alm.lugar,
                                    sucursallc = alm.sucursallc,
                                    min_solurgente = alm.min_solurgente,
                                    codmoneda_min_solurgente = alm.codmoneda_min_solurgente,
                                    pesomin = alm.pesomin,
                                    pesoest = alm.pesoest,
                                    porcenmin = alm.porcenmin,
                                    porcenmin_rendi = alm.porcenmin_rendi,
                                    pesoest_rendi = alm.pesoest_rendi,
                                    pesomin_rendi = alm.pesomin_rendi,
                                    idcuenta_caja_mn = alm.idcuenta_caja_mn,
                                    idcuenta_caja_me = alm.idcuenta_caja_me,
                                    graficar = alm.graficar,
                                    analizar_rendimiento = alm.analizar_rendimiento,
                                    actividad = alm.actividad,
                                    latitud = alm.latitud,
                                    longitud = alm.longitud,
                                    fax = alm.fax
                                };
                    //return Request.CreateResponse(HttpStatusCode.OK, query);

                    if (query.Count() == 0)
                    {
                        return NotFound("No se encontro un registro con este código");
                    }

                    return Ok(query);
                }
                return BadRequest("Se perdio la conexion con el servidor");
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }

        // PUT: api/inalmacen/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{conexionName}/{codigo}")]
        public async Task<IActionResult> Putinalmacen(string conexionName, int codigo, inalmacen inalmacen)
        {
            if (verificador.VerConnection(conexionName, connectionString))
            {
                if (codigo != inalmacen.codigo)
                {
                    return BadRequest("Error con Id en datos proporcionados.");
                }

                _context.Entry(inalmacen).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!inalmacenExists(codigo))
                    {
                        return NotFound("No existe un registro con ese código");
                    }
                    else
                    {
                        throw;
                    }
                }

                return Ok("Datos actualizados correctamente.");
            }
            return BadRequest("Se perdio la conexion con el servidor");


        }

        // POST: api/inalmacen
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost("{conexionName}")]
        public async Task<ActionResult<inalmacen>> Postinalmacen(string conexionName, inalmacen inalmacen)
        {
            if (verificador.VerConnection(conexionName, connectionString))
            {
                if (_context.inalmacen == null)
                {
                    return Problem("Entidad inalmacen es null.");
                }
                _context.inalmacen.Add(inalmacen);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (inalmacenExists(inalmacen.codigo))
                    {
                        return Conflict("Ya existe un registro con ese código");
                    }
                    else
                    {
                        throw;
                    }
                }

                return Ok("Registrado con Exito :D");

            }
            return BadRequest("Se perdio la conexion con el servidor");
        }

        // DELETE: api/inalmacen/5
        [HttpDelete("{conexionName}/{codigo}")]
        public async Task<IActionResult> Deleteinalmacen(string conexionName, int codigo)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.inalmacen == null)
                    {
                        return Problem("Entidad inalmacen es null.");
                    }
                    var inalmacen = await _context.inalmacen.FindAsync(codigo);
                    if (inalmacen == null)
                    {
                        return NotFound("No existe un registro con ese código");
                    }

                    _context.inalmacen.Remove(inalmacen);
                    await _context.SaveChangesAsync();

                    return Ok("Datos eliminados con exito");
                }
                return BadRequest("Se perdio la conexion con el servidor");

            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }

        private bool inalmacenExists(int codigo)
        {
            return (_context.inalmacen?.Any(e => e.codigo == codigo)).GetValueOrDefault();

        }
    }
}
