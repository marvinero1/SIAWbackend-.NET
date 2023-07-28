using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIAW.Data;
using SIAW.Models;
using SIAW.Models_Extra;
using System.Net;

namespace SIAW.Controllers.seg_adm.operacion
{
    [Route("api/seg_adm/oper/[controller]")]
    [ApiController]
    public class prgaccesosrolController : ControllerBase
    {
        private readonly DBContext _context;
        private readonly string connectionString;
        private VerificaConexion verificador;
        private readonly IConfiguration _configuration;
        public prgaccesosrolController(IConfiguration configuration)
        {
            connectionString = ConnectionController.ConnectionString;
            _context = DbContextFactory.Create(connectionString);
            _configuration = configuration;
            verificador = new VerificaConexion(_configuration);
        }


        // GET: api/semodulo
        [HttpGet]
        [Route("semodulo/{conexionName}")]
        public async Task<ActionResult<IEnumerable<semodulo>>> Getsemodulo(string conexionName)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.semodulo == null)
                    {
                        return Problem("Entidad semodulo es null.");
                    }
                    var result = await _context.semodulo.OrderBy(codigo => codigo.codigo).ToListAsync();
                    return Ok(result);
                }
                return BadRequest("Se perdio la conexion con el servidor");
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }


        }

        // GET: api/semodulo/5
        [HttpGet]
        [Route("semodulo/{conexionName}/{codigo}")]
        public async Task<ActionResult<semodulo>> Getsemodulo(string conexionName, int codigo)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.semodulo == null)
                    {
                        return Problem("Entidad semodulo es null.");
                    }
                    var semodulo = await _context.semodulo.FindAsync(codigo);

                    if (semodulo == null)
                    {
                        return NotFound("No se encontro un registro con este código");
                    }

                    return Ok(semodulo);
                }
                return BadRequest("Se perdio la conexion con el servidor");
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }

        // PUT: api/semodulo/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut]
        [Route("semodulo/{conexionName}/{codigo}")]
        public async Task<IActionResult> Putsemodulo(string conexionName, int codigo, semodulo semodulo)
        {
            if (verificador.VerConnection(conexionName, connectionString))
            {
                if (codigo != semodulo.codigo)
                {
                    return BadRequest("Error con Id en datos proporcionados.");
                }

                _context.Entry(semodulo).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!semoduloExists(codigo))
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

        // POST: api/semodulo
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        [Route("semodulo/{conexionName}")]
        public async Task<ActionResult<semodulo>> Postsemodulo(string conexionName, semodulo semodulo)
        {
            if (verificador.VerConnection(conexionName, connectionString))
            {
                if (_context.semodulo == null)
                {
                    return Problem("Entidad semodulo es null.");
                }
                _context.semodulo.Add(semodulo);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (semoduloExists(semodulo.codigo))
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

        // DELETE: api/semodulo/5
        [HttpDelete]
        [Route("semodulo/{conexionName}/{codigo}")]
        public async Task<IActionResult> Deletesemodulo(string conexionName, int codigo)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.semodulo == null)
                    {
                        return Problem("Entidad semodulo es null.");
                    }
                    var semodulo = await _context.semodulo.FindAsync(codigo);
                    if (semodulo == null)
                    {
                        return NotFound("No existe un registro con ese código");
                    }

                    _context.semodulo.Remove(semodulo);
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

        private bool semoduloExists(int codigo)
        {
            return (_context.semodulo?.Any(e => e.codigo == codigo)).GetValueOrDefault();

        }








        // GET: api/seclasificacion
        [HttpGet]
        [Route("seclasificacion/{conexionName}")]
        public async Task<ActionResult<IEnumerable<seclasificacion>>> Getseclasificacion(string conexionName)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.seclasificacion == null)
                    {
                        return Problem("Entidad seclasificacion es null.");
                    }
                    var result = await _context.seclasificacion.OrderBy(codigo => codigo.codigo).ToListAsync();
                    return Ok(result);
                }
                return BadRequest("Se perdio la conexion con el servidor");
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }


        }

        // GET: api/seclasificacion/5
        [HttpGet]
        [Route("seclasificacion/{conexionName}/{codigo}")]
        public async Task<ActionResult<seclasificacion>> Getseclasificacion(string conexionName, int codigo)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.seclasificacion == null)
                    {
                        return Problem("Entidad seclasificacion es null.");
                    }
                    var seclasificacion = await _context.seclasificacion.FindAsync(codigo);

                    if (seclasificacion == null)
                    {
                        return NotFound("No se encontro un registro con este código");
                    }

                    return Ok(seclasificacion);
                }
                return BadRequest("Se perdio la conexion con el servidor");
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }

        // PUT: api/seclasificacion/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut]
        [Route("seclasificacion/{conexionName}/{codigo}")]
        public async Task<IActionResult> Putseclasificacion(string conexionName, int codigo, seclasificacion seclasificacion)
        {
            if (verificador.VerConnection(conexionName, connectionString))
            {
                if (codigo != seclasificacion.codigo)
                {
                    return BadRequest("Error con Id en datos proporcionados.");
                }

                _context.Entry(seclasificacion).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!seclasificacionExists(codigo))
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

        // POST: api/seclasificacion
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        [Route("seclasificacion/{conexionName}")]
        public async Task<ActionResult<seclasificacion>> Postseclasificacion(string conexionName, seclasificacion seclasificacion)
        {
            if (verificador.VerConnection(conexionName, connectionString))
            {
                if (_context.seclasificacion == null)
                {
                    return Problem("Entidad seclasificacion es null.");
                }
                _context.seclasificacion.Add(seclasificacion);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (seclasificacionExists(seclasificacion.codigo))
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

        // DELETE: api/seclasificacion/5
        [HttpDelete]
        [Route("seclasificacion/{conexionName}/{codigo}")]
        public async Task<IActionResult> Deleteseclasificacion(string conexionName, int codigo)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.seclasificacion == null)
                    {
                        return Problem("Entidad seclasificacion es null.");
                    }
                    var seclasificacion = await _context.seclasificacion.FindAsync(codigo);
                    if (seclasificacion == null)
                    {
                        return NotFound("No existe un registro con ese código");
                    }

                    _context.seclasificacion.Remove(seclasificacion);
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

        private bool seclasificacionExists(int codigo)
        {
            return (_context.seclasificacion?.Any(e => e.codigo == codigo)).GetValueOrDefault();

        }






        // GET: api/seprograma
        [HttpGet]
        [Route("seprograma/{conexionName}")]
        public async Task<ActionResult<IEnumerable<seprograma>>> Getseprograma(string conexionName)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.seprograma == null)
                    {
                        return Problem("Entidad seprograma es null.");
                    }
                    var result = await _context.seprograma.OrderBy(codigo => codigo.codigo).ToListAsync();
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
        /// Obtiene todos los datos de la tabla seprograma de acuerdo al codclasificacion y codmodulo
        /// </summary>
        /// <param name="conexionName"></param>
        /// <param name="codclasificacion"></param>
        /// <param name="codmodulo"></param>
        /// <returns></returns>
        // GET: api/seprograma
        [HttpGet]
        [Route("seprograma/{conexionName}/{codclasificacion}/{codmodulo}")]
        public async Task<ActionResult<IEnumerable<seprograma>>> Getseprograma2(string conexionName, int codclasificacion, int codmodulo)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.seprograma == null)
                    {
                        return Problem("Entidad seprograma es null.");
                    }

                    var programas = _context.seprograma
                        .Where(p => p.codclasificacion == codclasificacion && p.codmodulo == codmodulo)
                        .OrderBy(p => p.descripcion)
                        .Select(p => new
                        {
                            p.codigo,
                            p.nombre,
                            p.descripcion
                        })
                    .ToList();


                    if (programas.Count() == 0)
                    {
                        return NotFound("No se encontro un registro con los datos proporcionados.");
                    }

                    return Ok(programas);
                }
                return BadRequest("Se perdio la conexion con el servidor");
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }


      
        // PUT: api/seprograma/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut]
        [Route("seprograma/{conexionName}/{codigo}")]
        public async Task<IActionResult> Putseprograma(string conexionName, int codigo, seprograma seprograma)
        {
            if (verificador.VerConnection(conexionName, connectionString))
            {
                if (codigo != seprograma.codigo)
                {
                    return BadRequest("Error con Id en datos proporcionados.");
                }

                _context.Entry(seprograma).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!seprogramaExists(codigo))
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

        // POST: api/seprograma
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        [Route("seprograma/{conexionName}")]
        public async Task<ActionResult<seprograma>> Postseprograma(string conexionName, seprograma seprograma)
        {
            if (verificador.VerConnection(conexionName, connectionString))
            {
                if (_context.seprograma == null)
                {
                    return Problem("Entidad seprograma es null.");
                }
                _context.seprograma.Add(seprograma);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (seprogramaExists(seprograma.codigo))
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

        // DELETE: api/seprograma/5
        [HttpDelete]
        [Route("seprograma/{conexionName}/{codigo}")]
        public async Task<IActionResult> Deleteseprograma(string conexionName, int codigo)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.seprograma == null)
                    {
                        return Problem("Entidad seprograma es null.");
                    }
                    var seprograma = await _context.seprograma.FindAsync(codigo);
                    if (seprograma == null)
                    {
                        return NotFound("No existe un registro con ese código");
                    }

                    _context.seprograma.Remove(seprograma);
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

        private bool seprogramaExists(int codigo)
        {
            return (_context.seprograma?.Any(e => e.codigo == codigo)).GetValueOrDefault();

        }




        // GET: api/serolprogs
        [HttpGet]
        [Route("serolprogs/{conexionName}")]
        public async Task<ActionResult<IEnumerable<serolprogs>>> Getserolprogs(string conexionName)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.serolprogs == null)
                    {
                        return Problem("Entidad serolprogs es null.");
                    }
                    var result = await _context.serolprogs.OrderBy(codigo => codigo.codigo).ToListAsync();
                    return Ok(result);
                }
                return BadRequest("Se perdio la conexion con el servidor");
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }

        // GET: api/serolprogs/5
        [HttpGet]
        [Route("serolprogs/{conexionName}/{codigo}")]
        public async Task<ActionResult<serolprogs>> Getserolprogs1(string conexionName, int codigo)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.serolprogs == null)
                    {
                        return Problem("Entidad serolprogs es null.");
                    }
                    var serolprogs = await _context.serolprogs.FindAsync(codigo);

                    if (serolprogs == null)
                    {
                        return NotFound("No se encontro un registro con este código");
                    }

                    return Ok(serolprogs);
                }
                return BadRequest("Se perdio la conexion con el servidor");
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }

        /// <summary>
        /// Obtiene los programas habilitados a un rol
        /// </summary>
        /// <param name="conexionName"></param>
        /// <param name="codRol"></param>
        /// <returns></returns>
        // GET: api/serolprogs/5
        [HttpGet]
        [Route("serolprogs/rolGet/{conexionName}/{codRol}")]
        public async Task<ActionResult<serolprogs>> Getserolprogs2(string conexionName, string codRol)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.serolprogs == null)
                    {
                        return Problem("Entidad serolprogs es null.");
                    }
                    //var serolprogs = await _context.serolprogs.FindAsync(codigo);
                    var codProgramas = from serolprogs in _context.serolprogs
                                       where serolprogs.codrol == codRol
                                       select serolprogs.codprograma;


                    if (codProgramas == null)
                    {
                        return NotFound("No se encontro un registro con este código");
                    }

                    return Ok(codProgramas);
                }
                return BadRequest("Se perdio la conexion con el servidor");
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }



        /// <summary>
        /// Obtiene los programas habilitados a un rol con check en base a los programas filtrados
        /// </summary>
        /// <param name="conexionName"></param>
        /// <param name="codRol"></param>
        /// <param name="codclasificacion"></param>
        /// <param name="codmodulo"></param>
        /// <returns></returns>
        // GET: api/serolprogs/5
        [HttpGet]
        [Route("serolprogs/rolGetCheck/{conexionName}/{codRol}/{codclasificacion}/{codmodulo}")]
        public async Task<ActionResult<serolprogs>> Getserolprogs3(string conexionName, string codRol, int codclasificacion, int codmodulo)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    var programas = _context.seprograma
                        .Where(p => p.codclasificacion == codclasificacion && p.codmodulo == codmodulo)
                        .OrderBy(p => p.descripcion)
                        .Select(p => new
                        {
                            p.codigo,
                            p.nombre,
                            p.descripcion
                        })
                        .ToList();

                    //var serolprogs = await _context.serolprogs.FindAsync(codigo);
                    var codProgramas = from serolprogs in _context.serolprogs
                                       where serolprogs.codrol == codRol
                                       select serolprogs.codprograma;

                    var resultado = programas.Select(p => new
                    {
                        p.codigo,
                        p.nombre,
                        p.descripcion,
                        activo = codProgramas.Contains(p.codigo)
                    }).ToList();


                    if (resultado == null)
                    {
                        return NotFound("No se encontro un registro con este código");
                    }

                    return Ok(resultado);
                }
                return BadRequest("Se perdio la conexion con el servidor");
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }




        /// <summary>
        /// Verifica si un rol tiene acceso a una ventana
        /// </summary>
        /// <param name="conexionName"></param>
        /// <param name="acceso"></param>
        /// <returns></returns>
        // POST: api/Verificaserolprogs/DPD/5
        [HttpPost]
        [Route("verificaserolprogs/{conexionName}")]
        public async Task<ActionResult<serolprogs>> VerificaAcceso(string conexionName, Getserolprogs acceso)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    var result = from sp in _context.serolprogs
                                 where sp.codrol == acceso.codrol &&
                                       sp.codprograma == (from p in _context.seprograma
                                                          where p.nombre == acceso.programa
                                                          select p.codigo).FirstOrDefault()
                                 select sp;

                    if (result.Count() == 0)
                    {
                        return NotFound(false);  // no acceso
                    }

                    return Ok(true); //acceso
                }
                return BadRequest("Se perdio la conexion con el servidor");
            }
            catch (Exception)
            {
                return BadRequest("Revise el Servidor.");
                throw;
            }
        }



        // PUT: api/serolprogs/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut]
        [Route("serolprogs/{conexionName}/{codigo}")]
        public async Task<IActionResult> Putserolprogs(string conexionName, int codigo, serolprogs serolprogs)
        {
            if (verificador.VerConnection(conexionName, connectionString))
            {
                if (codigo != serolprogs.codigo)
                {
                    return BadRequest("Error con Id en datos proporcionados.");
                }

                _context.Entry(serolprogs).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!serolprogsExists(codigo))
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

        // POST: api/serolprogs
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        [Route("serolprogs/{conexionName}")]
        public async Task<ActionResult<serolprogs>> Postserolprogs(string conexionName, serolprogs serolprogs)
        {
            if (verificador.VerConnection(conexionName, connectionString))
            {
                if (_context.serolprogs == null)
                {
                    return Problem("Entidad serolprogs es null.");
                }
                _context.serolprogs.Add(serolprogs);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (serolprogsExists(serolprogs.codigo))
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

        // DELETE: api/serolprogs/5
        [HttpDelete]
        [Route("serolprogs/{conexionName}/{codigo}")]
        public async Task<IActionResult> Deleteserolprogs(string conexionName, int codigo)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.serolprogs == null)
                    {
                        return Problem("Entidad serolprogs es null.");
                    }
                    var serolprogs = await _context.serolprogs.FindAsync(codigo);
                    if (serolprogs == null)
                    {
                        return NotFound("No existe un registro con ese código");
                    }

                    _context.serolprogs.Remove(serolprogs);
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

        private bool serolprogsExists(int codigo)
        {
            return (_context.serolprogs?.Any(e => e.codigo == codigo)).GetValueOrDefault();

        }


    }
}
