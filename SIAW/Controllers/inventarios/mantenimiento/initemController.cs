using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NuGet.Configuration;
using SIAW.Data;
using SIAW.Models;
using System.Net;

namespace SIAW.Controllers.inventarios.mantenimiento
{
    [Route("api/inventario/mant/initem/[controller]")]
    [ApiController]
    public class initemController : ControllerBase
    {
        private readonly DBContext _context;
        private readonly string connectionString;
        private VerificaConexion verificador;
        private readonly IConfiguration _configuration;
        public initemController(IConfiguration configuration)
        {
            connectionString = ConnectionController.ConnectionString;
            _context = DbContextFactory.Create(connectionString);
            _configuration = configuration;
            verificador = new VerificaConexion(_configuration);
        }

        // GET: api/initem
        [HttpGet("{conexionName}")]
        public async Task<ActionResult<IEnumerable<initem>>> Getinitem(string conexionName)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.initem == null)
                    {
                        return Problem("Entidad initem es null.");
                    }
                    //var result = await _context.initem.OrderByDescending(fechareg => fechareg.fechareg).ToListAsync();
                    //return Ok(result);
                    var result = from item in _context.initem
                                 join udemed in _context.inudemed on item.unidad equals udemed.Codigo
                                 join rosca in _context.inrosca on item.rosca equals rosca.codigo
                                 join terminacion in _context.interminacion on item.terminacion equals terminacion.codigo
                                 join resistencia in _context.inresistencia on item.codresistencia equals resistencia.codigo
                                 join linea in _context.inlinea on item.codlinea equals linea.codigo
                                 orderby item.codigo
                                 select new
                                 {
                                     codigo = item.codigo,
                                     descripcion = item.descripcion,
                                     descripcorta = item.descripcorta,
                                     descripabr = item.descripabr,
                                     medida = item.medida,
                                     unidad = item.unidad,
                                     descUnidad = udemed.Descripcion,
                                     rosca = item.rosca,
                                     descRosca = rosca.descripcion,
                                     terminacion = item.terminacion,
                                     descTerminacion = terminacion.descripcion,
                                     peso = item.peso,
                                     codlinea = item.codlinea,
                                     descLinea = linea.descripcion,
                                     clasificacion = item.clasificacion,
                                     kit = item.kit,
                                     estadocv = item.estadocv,
                                     costo = item.costo,
                                     monedacosto = item.monedacosto,
                                     codresistencia = item.codresistencia,
                                     descResistencia = resistencia.descripcion,
                                     horareg = item.horareg,
                                     fechareg = item.fechareg,
                                     usuarioreg = item.usuarioreg,
                                     enlinea = item.enlinea,
                                     saldominimo = item.saldominimo,
                                     codigobarra = item.codigobarra,
                                     reservastock = item.reservastock,
                                     iva = item.iva,
                                     codmoneda_valor_criterio = item.codmoneda_valor_criterio,
                                     porcen_gac = item.porcen_gac,
                                     nandina = item.nandina,
                                     usar_en_movimiento = item.usar_en_movimiento,
                                     paga_comision = item.paga_comision,
                                     porcen_saldo_restringido = item.porcen_saldo_restringido,
                                     controla_negativo = item.controla_negativo,
                                     tipo = item.tipo,
                                     codproducto_sin = item.codproducto_sin
                                 };


                    return Ok(result);
                }
                return BadRequest("Se perdio la conexion con el servidor");
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }


        }

        // GET: api/initem/5
        [HttpGet("{conexionName}/{codigo}")]
        public async Task<ActionResult<initem>> Getinitem(string conexionName, string codigo)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.initem == null)
                    {
                        return Problem("Entidad initem es null.");
                    }
                    //var initem = await _context.initem.FindAsync(codigo);

                    var resultado = from item in _context.initem
                                    join udemed in _context.inudemed on item.unidad equals udemed.Codigo
                                    join rosca in _context.inrosca on item.rosca equals rosca.codigo
                                    join terminacion in _context.interminacion on item.terminacion equals terminacion.codigo
                                    join resistencia in _context.inresistencia on item.codresistencia equals resistencia.codigo
                                    join linea in _context.inlinea on item.codlinea equals linea.codigo
                                    where item.codigo == codigo
                                    select new
                                    {
                                        codigo = item.codigo,
                                        descripcion = item.descripcion,
                                        descripcorta = item.descripcorta,
                                        descripabr = item.descripabr,
                                        medida = item.medida,
                                        unidad = item.unidad,
                                        descUnidad = udemed.Descripcion,
                                        rosca = item.rosca,
                                        descRosca = rosca.descripcion,
                                        terminacion = item.terminacion,
                                        descTerminacion = terminacion.descripcion,
                                        peso = item.peso,
                                        codlinea = item.codlinea,
                                        descLinea = linea.descripcion,
                                        clasificacion = item.clasificacion,
                                        kit = item.kit,
                                        estadocv = item.estadocv,
                                        costo = item.costo,
                                        monedacosto = item.monedacosto,
                                        codresistencia = item.codresistencia,
                                        descResistencia = resistencia.descripcion,
                                        horareg = item.horareg,
                                        fechareg = item.fechareg,
                                        usuarioreg = item.usuarioreg,
                                        enlinea = item.enlinea,
                                        saldominimo = item.saldominimo,
                                        codigobarra = item.codigobarra,
                                        reservastock = item.reservastock,
                                        iva = item.iva,
                                        codmoneda_valor_criterio = item.codmoneda_valor_criterio,
                                        porcen_gac = item.porcen_gac,
                                        nandina = item.nandina,
                                        usar_en_movimiento = item.usar_en_movimiento,
                                        paga_comision = item.paga_comision,
                                        porcen_saldo_restringido = item.porcen_saldo_restringido,
                                        controla_negativo = item.controla_negativo,
                                        tipo = item.tipo,
                                        codproducto_sin = item.codproducto_sin
                                    };

                    var initem = resultado.FirstOrDefault();

                    if (initem == null)
                    {
                        return NotFound("No se encontro un registro con este código");
                    }

                    return Ok(initem);
                }
                return BadRequest("Se perdio la conexion con el servidor");
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }


        // GET: api/catalogo
        [HttpGet]
        [Route("catalogo/{conexionName}")]
        public async Task<ActionResult<IEnumerable<initem>>> Getinitem_catalogo_resumido(string conexionName)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    var query = _context.initem
                    .OrderBy(i => i.codigo)
                    .Select(i => new
                    {
                        codigo = i.codigo,
                        descrip = i.descripcion + i.medida
                    });

                    var result = query.ToList();

                    if (result.Count() == 0)
                    {
                        return Problem("Entidad initem es null.");
                    }
                    return Ok(result);
                }
                return BadRequest("Se perdio la conexion con el servidor");
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
                throw;
            }
        }


        // GET: api/catalogo2
        [HttpGet]
        [Route("catalogo2/{conexionName}")]
        public async Task<ActionResult<IEnumerable<initem>>> Getinitem_catalogo(string conexionName)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    var query = _context.initem
                    .OrderBy(i => i.codigo)
                    .Select(i => new
                    {
                        i.codigo,
                        i.descripcion,
                        i.medida,
                        i.estadocv,
                        i.enlinea,
                        i.codlinea
                    });

                    var result = query.ToList();

                    if (result.Count() == 0)
                    {
                        return Problem("Entidad initem es null.");
                    }
                    return Ok(result);
                }
                return BadRequest("Se perdio la conexion con el servidor");
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
                throw;
            }
        }

        // PUT: api/initem/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{conexionName}/{codigo}")]
        public async Task<IActionResult> Putinitem(string conexionName, string codigo, initem initem)
        {
            if (verificador.VerConnection(conexionName, connectionString))
            {
                if (codigo != initem.codigo)
                {
                    return BadRequest("Error con Id en datos proporcionados.");
                }

                _context.Entry(initem).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!initemExists(codigo))
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

        // POST: api/initem
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost("{conexionName}")]
        public async Task<ActionResult<initem>> Postinitem(string conexionName, initem initem)
        {
            if (verificador.VerConnection(conexionName, connectionString))
            {
                if (_context.initem == null)
                {
                    return Problem("Entidad initem es null.");
                }
                _context.initem.Add(initem);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (initemExists(initem.codigo))
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

        // DELETE: api/initem/5
        [HttpDelete("{conexionName}/{codigo}")]
        public async Task<IActionResult> Deleteinitem(string conexionName, string codigo)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.initem == null)
                    {
                        return Problem("Entidad initem es null.");
                    }
                    var initem = await _context.initem.FindAsync(codigo);
                    if (initem == null)
                    {
                        return NotFound("No existe un registro con ese código");
                    }

                    _context.initem.Remove(initem);
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

        private bool initemExists(string codigo)
        {
            return (_context.initem?.Any(e => e.codigo == codigo)).GetValueOrDefault();

        }
    }




    [Route("api/inventario/mant/inctrlstock/[controller]")]
    [ApiController]
    public class inctrlstockController : ControllerBase
    {
        private readonly DBContext _context;
        private readonly string connectionString;
        private VerificaConexion verificador;
        private readonly IConfiguration _configuration;
        public inctrlstockController(IConfiguration configuration)
        {
            connectionString = ConnectionController.ConnectionString;
            _context = DbContextFactory.Create(connectionString);
            _configuration = configuration;
            verificador = new VerificaConexion(_configuration);
        }

        // GET: api/inctrlstock
        [HttpGet("{conexionName}")]
        public async Task<ActionResult<IEnumerable<inctrlstock>>> Getinctrlstock(string conexionName)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.inctrlstock == null)
                    {
                        return Problem("Entidad inctrlstock es null.");
                    }
                    var result = await _context.inctrlstock.OrderBy(id => id.id).ToListAsync();
                    return Ok(result);
                }
                return BadRequest("Se perdio la conexion con el servidor");
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }


        }

        /// <summary>
        /// Obtiene todos los registros de la tabla inctrlstock (item) con initem, dependiendo del codigo de item
        /// </summary>
        /// <param name="conexionName"></param>
        /// <param name="coditem"></param>
        /// <returns></returns>
        // GET: api/initem_inctrlstock/5
        [HttpGet]
        [Route("initem_inctrlstock/{conexionName}/{coditem}")]
        public async Task<ActionResult<inctrlstock>> Getinctrlstock(string conexionName, string coditem)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.inctrlstock == null)
                    {
                        return Problem("Entidad inctrlstock es null.");
                    }
                    //var inctrlstock = await _context.inctrlstock.FindAsync(id);
                    var query = from s in _context.inctrlstock
                                join i in _context.initem on s.coditemcontrol equals i.codigo
                                where s.coditem == coditem
                                orderby s.coditemcontrol
                                select new
                                {
                                    s.id,
                                    s.coditem,
                                    s.coditemcontrol,
                                    i.descripcion,
                                    i.medida,
                                    s.porcentaje
                                };

                    var result = query.ToList();

                    if (result.Count() == 0)
                    {
                        return NotFound("No se encontro un registro con este código");
                    }

                    return Ok(result);
                }
                return BadRequest("Se perdio la conexion con el servidor");
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }

        // PUT: api/inctrlstock/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{conexionName}/{id}")]
        public async Task<IActionResult> Putinctrlstock(string conexionName, int id, inctrlstock inctrlstock)
        {
            if (verificador.VerConnection(conexionName, connectionString))
            {
                if (id != inctrlstock.id)
                {
                    return BadRequest("Error con Id en datos proporcionados.");
                }

                _context.Entry(inctrlstock).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!inctrlstockExists(id))
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

        // POST: api/inctrlstock
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost("{conexionName}")]
        public async Task<ActionResult<inctrlstock>> Postinctrlstock(string conexionName, inctrlstock inctrlstock)
        {
            if (verificador.VerConnection(conexionName, connectionString))
            {
                if (_context.inctrlstock == null)
                {
                    return Problem("Entidad inctrlstock es null.");
                }
                _context.inctrlstock.Add(inctrlstock);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (inctrlstockExists(inctrlstock.id))
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

        // DELETE: api/inctrlstock/5
        [HttpDelete("{conexionName}/{id}")]
        public async Task<IActionResult> Deleteinctrlstock(string conexionName, int id)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.inctrlstock == null)
                    {
                        return Problem("Entidad inctrlstock es null.");
                    }
                    var inctrlstock = await _context.inctrlstock.FindAsync(id);
                    if (inctrlstock == null)
                    {
                        return NotFound("No existe un registro con ese código");
                    }

                    _context.inctrlstock.Remove(inctrlstock);
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

        private bool inctrlstockExists(int id)
        {
            return (_context.inctrlstock?.Any(e => e.id == id)).GetValueOrDefault();

        }

    }





    [Route("api/inventario/mant/initem_max/[controller]")]
    [ApiController]
    public class initem_maxController : ControllerBase
    {
        private readonly DBContext _context;
        private readonly string connectionString;
        private VerificaConexion verificador;
        private readonly IConfiguration _configuration;
        public initem_maxController(IConfiguration configuration)
        {
            connectionString = ConnectionController.ConnectionString;
            _context = DbContextFactory.Create(connectionString);
            _configuration = configuration;
            verificador = new VerificaConexion(_configuration);
        }

        // GET: api/initem_max
        [HttpGet("{conexionName}")]
        public async Task<ActionResult<IEnumerable<initem_max>>> Getinitem_max(string conexionName)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.initem_max == null)
                    {
                        return Problem("Entidad initem_max es null.");
                    }
                    var result = await _context.initem_max.OrderBy(id => id.id).ToListAsync();
                    return Ok(result);
                }
                return BadRequest("Se perdio la conexion con el servidor");
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }


        }

        /// <summary>
        /// Obtiene todos los registros de la tabla initem_max (item), dependiendo del codigo de item
        /// </summary>
        /// <param name="conexionName"></param>
        /// <param name="coditem"></param>
        /// <returns></returns>
        // GET: api/initem_initemMax/5
        [HttpGet]
        [Route("initem_initemMax/{conexionName}/{coditem}")]
        public async Task<ActionResult<initem_max>> Getinitem_max(string conexionName, string coditem)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.initem_max == null)
                    {
                        return Problem("Entidad initem_max es null.");
                    }
                    var query = _context.initem_max
                    .Where(im => im.coditem == coditem)
                    .OrderBy(im => im.codalmacen)
                    .ThenBy(im => im.codtarifa)
                    .Select(im => new
                    {
                        im.id,
                        im.codalmacen,
                        im.codtarifa,
                        im.maximo,
                        im.dias
                    });
                    var result = query.ToList();

                    if (result.Count() == 0)
                    {
                        return NotFound("No se encontro un registro con este código");
                    }

                    return Ok(result);
                }
                return BadRequest("Se perdio la conexion con el servidor");
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }

        // PUT: api/initem_max/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{conexionName}/{id}")]
        public async Task<IActionResult> Putinitem_max(string conexionName, int id, initem_max initem_max)
        {
            if (verificador.VerConnection(conexionName, connectionString))
            {
                if (id != initem_max.id)
                {
                    return BadRequest("Error con Id en datos proporcionados.");
                }

                _context.Entry(initem_max).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!initem_maxExists(id))
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

        // POST: api/initem_max
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost("{conexionName}")]
        public async Task<ActionResult<initem_max>> Postinitem_max(string conexionName, initem_max initem_max)
        {
            if (verificador.VerConnection(conexionName, connectionString))
            {
                if (_context.initem_max == null)
                {
                    return Problem("Entidad initem_max es null.");
                }
                _context.initem_max.Add(initem_max);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (initem_maxExists(initem_max.id))
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

        // DELETE: api/initem_max/5
        [HttpDelete("{conexionName}/{id}")]
        public async Task<IActionResult> Deleteinitem_max(string conexionName, int id)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.initem_max == null)
                    {
                        return Problem("Entidad initem_max es null.");
                    }
                    var initem_max = await _context.initem_max.FindAsync(id);
                    if (initem_max == null)
                    {
                        return NotFound("No existe un registro con ese código");
                    }

                    _context.initem_max.Remove(initem_max);
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

        private bool initem_maxExists(int id)
        {
            return (_context.initem_max?.Any(e => e.id == id)).GetValueOrDefault();

        }

    }








    [Route("api/inventario/mant/initem_controltarifa/[controller]")]
    [ApiController]
    public class initem_controltarifaController : ControllerBase
    {
        private readonly DBContext _context;
        private readonly string connectionString;
        private VerificaConexion verificador;
        private readonly IConfiguration _configuration;
        public initem_controltarifaController(IConfiguration configuration)
        {
            connectionString = ConnectionController.ConnectionString;
            _context = DbContextFactory.Create(connectionString);
            _configuration = configuration;
            verificador = new VerificaConexion(_configuration);
        }

        // GET: api/initem_controltarifa
        [HttpGet("{conexionName}")]
        public async Task<ActionResult<IEnumerable<initem_controltarifa>>> Getinitem_controltarifa(string conexionName)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.initem_controltarifa == null)
                    {
                        return Problem("Entidad initem_controltarifa es null.");
                    }
                    var result = await _context.initem_controltarifa.OrderBy(id => id.id).ToListAsync();
                    return Ok(result);
                }
                return BadRequest("Se perdio la conexion con el servidor");
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }


        }

        /// <summary>
        /// Obtiene todos los registros de la tabla initem_controltarifa (item), dependiendo del codigo de item
        /// </summary>
        /// <param name="conexionName"></param>
        /// <param name="coditem"></param>
        /// <returns></returns>
        // GET: api/initem_controltarifa/5
        [HttpGet]
        [Route("initem_controltarifa/{conexionName}/{coditem}")]
        public async Task<ActionResult<initem_controltarifa>> Getinitem_controltarifa(string conexionName, string coditem)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.initem_controltarifa == null)
                    {
                        return Problem("Entidad initem_controltarifa es null.");
                    }
                    var query = _context.initem_controltarifa
                    .Where(im => im.coditem == coditem)
                    .OrderBy(im => im.codtarifa_a)
                    .ThenBy(im => im.codtarifa_b)
                    .Select(im => new
                    {
                        im.id,
                        im.coditem,
                        im.codtarifa_a,
                        im.codtarifa_b
                    });

                    var result = query.ToList();

                    if (result.Count() == 0)
                    {
                        return NotFound("No se encontro un registro con este código");
                    }

                    return Ok(result);
                }
                return BadRequest("Se perdio la conexion con el servidor");
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }

        // PUT: api/initem_controltarifa/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{conexionName}/{id}")]
        public async Task<IActionResult> Putinitem_controltarifa(string conexionName, int id, initem_controltarifa initem_controltarifa)
        {
            if (verificador.VerConnection(conexionName, connectionString))
            {
                if (id != initem_controltarifa.id)
                {
                    return BadRequest("Error con Id en datos proporcionados.");
                }

                _context.Entry(initem_controltarifa).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!initem_controltarifaExists(id))
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

        // POST: api/initem_controltarifa
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost("{conexionName}")]
        public async Task<ActionResult<initem_controltarifa>> Postinitem_controltarifa(string conexionName, initem_controltarifa initem_controltarifa)
        {
            if (verificador.VerConnection(conexionName, connectionString))
            {
                if (_context.initem_controltarifa == null)
                {
                    return Problem("Entidad initem_controltarifa es null.");
                }
                _context.initem_controltarifa.Add(initem_controltarifa);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (initem_controltarifaExists(initem_controltarifa.id))
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

        // DELETE: api/initem_controltarifa/5
        [HttpDelete("{conexionName}/{id}")]
        public async Task<IActionResult> Deleteinitem_controltarifa(string conexionName, int id)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.initem_controltarifa == null)
                    {
                        return Problem("Entidad initem_controltarifa es null.");
                    }
                    var initem_controltarifa = await _context.initem_controltarifa.FindAsync(id);
                    if (initem_controltarifa == null)
                    {
                        return NotFound("No existe un registro con ese código");
                    }

                    _context.initem_controltarifa.Remove(initem_controltarifa);
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

        private bool initem_controltarifaExists(int id)
        {
            return (_context.initem_controltarifa?.Any(e => e.id == id)).GetValueOrDefault();

        }

    }
}
