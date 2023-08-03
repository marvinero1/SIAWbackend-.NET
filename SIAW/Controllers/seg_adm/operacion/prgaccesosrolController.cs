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
        private readonly UserConnectionManager _userConnectionManager;
        public prgaccesosrolController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }


        // GET: api/semodulo
        [HttpGet]
        [Route("semodulo/{userConn}")]
        public async Task<ActionResult<IEnumerable<semodulo>>> Getsemodulo(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                //var _context = _userConnectionManager.GetUserConnection(userId);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.semodulo == null)
                    {
                        return Problem("Entidad semodulo es null.");
                    }
                    var result = await _context.semodulo.OrderBy(codigo => codigo.codigo).ToListAsync();
                    return Ok(result);
                }
                
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }


        }

        // GET: api/semodulo/5
        [HttpGet]
        [Route("semodulo/{userConn}/{codigo}")]
        public async Task<ActionResult<semodulo>> Getsemodulo(string userConn, int codigo)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                //var _context = _userConnectionManager.GetUserConnection(userId);

                using (var _context = DbContextFactory.Create(userConnectionString))
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
                
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }

        // PUT: api/semodulo/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut]
        [Route("semodulo/{userConn}/{codigo}")]
        public async Task<IActionResult> Putsemodulo(string userConn, int codigo, semodulo semodulo)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            //var _context = _userConnectionManager.GetUserConnection(userId);

            using (var _context = DbContextFactory.Create(userConnectionString))
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
                    if (!semoduloExists(codigo, _context))
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
            


        }

        // POST: api/semodulo
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        [Route("semodulo/{userConn}")]
        public async Task<ActionResult<semodulo>> Postsemodulo(string userConn, semodulo semodulo)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            //var _context = _userConnectionManager.GetUserConnection(userId);

            using (var _context = DbContextFactory.Create(userConnectionString))
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
                    if (semoduloExists(semodulo.codigo, _context))
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
            
        }

        // DELETE: api/semodulo/5
        [HttpDelete]
        [Route("semodulo/{userConn}/{codigo}")]
        public async Task<IActionResult> Deletesemodulo(string userConn, int codigo)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                //var _context = _userConnectionManager.GetUserConnection(userId);

                using (var _context = DbContextFactory.Create(userConnectionString))
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
                

            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }

        private bool semoduloExists(int codigo, DBContext _context)
        {
            return (_context.semodulo?.Any(e => e.codigo == codigo)).GetValueOrDefault();

        }








        // GET: api/seclasificacion
        [HttpGet]
        [Route("seclasificacion/{userConn}")]
        public async Task<ActionResult<IEnumerable<seclasificacion>>> Getseclasificacion(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                //var _context = _userConnectionManager.GetUserConnection(userId);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.seclasificacion == null)
                    {
                        return Problem("Entidad seclasificacion es null.");
                    }
                    var result = await _context.seclasificacion.OrderBy(codigo => codigo.codigo).ToListAsync();
                    return Ok(result);
                }
                
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }


        }

        // GET: api/seclasificacion/5
        [HttpGet]
        [Route("seclasificacion/{userConn}/{codigo}")]
        public async Task<ActionResult<seclasificacion>> Getseclasificacion(string userConn, int codigo)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                //var _context = _userConnectionManager.GetUserConnection(userId);

                using (var _context = DbContextFactory.Create(userConnectionString))
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
                
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }

        // PUT: api/seclasificacion/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut]
        [Route("seclasificacion/{userConn}/{codigo}")]
        public async Task<IActionResult> Putseclasificacion(string userConn, int codigo, seclasificacion seclasificacion)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            //var _context = _userConnectionManager.GetUserConnection(userId);

            using (var _context = DbContextFactory.Create(userConnectionString))
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
                    if (!seclasificacionExists(codigo, _context))
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
            


        }

        // POST: api/seclasificacion
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        [Route("seclasificacion/{userConn}")]
        public async Task<ActionResult<seclasificacion>> Postseclasificacion(string userConn, seclasificacion seclasificacion)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            //var _context = _userConnectionManager.GetUserConnection(userId);

            using (var _context = DbContextFactory.Create(userConnectionString))
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
                    if (seclasificacionExists(seclasificacion.codigo, _context))
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
            
        }

        // DELETE: api/seclasificacion/5
        [HttpDelete]
        [Route("seclasificacion/{userConn}/{codigo}")]
        public async Task<IActionResult> Deleteseclasificacion(string userConn, int codigo)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                //var _context = _userConnectionManager.GetUserConnection(userId);

                using (var _context = DbContextFactory.Create(userConnectionString))
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
                

            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }

        private bool seclasificacionExists(int codigo, DBContext _context)
        {
            return (_context.seclasificacion?.Any(e => e.codigo == codigo)).GetValueOrDefault();

        }






        // GET: api/seprograma
        [HttpGet]
        [Route("seprograma/{userConn}")]
        public async Task<ActionResult<IEnumerable<seprograma>>> Getseprograma(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                //var _context = _userConnectionManager.GetUserConnection(userId);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.seprograma == null)
                    {
                        return Problem("Entidad seprograma es null.");
                    }
                    var result = await _context.seprograma.OrderBy(codigo => codigo.codigo).ToListAsync();
                    return Ok(result);
                }
                
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }

        /// <summary>
        /// Obtiene todos los datos de la tabla seprograma de acuerdo al codclasificacion y codmodulo
        /// </summary>
        /// <param name="userConn"></param>
        /// <param name="codclasificacion"></param>
        /// <param name="codmodulo"></param>
        /// <returns></returns>
        // GET: api/seprograma
        [HttpGet]
        [Route("seprograma/{userConn}/{codclasificacion}/{codmodulo}")]
        public async Task<ActionResult<IEnumerable<seprograma>>> Getseprograma2(string userConn, int codclasificacion, int codmodulo)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                //var _context = _userConnectionManager.GetUserConnection(userId);

                using (var _context = DbContextFactory.Create(userConnectionString))
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
                
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }


      
        // PUT: api/seprograma/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut]
        [Route("seprograma/{userConn}/{codigo}")]
        public async Task<IActionResult> Putseprograma(string userConn, int codigo, seprograma seprograma)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            //var _context = _userConnectionManager.GetUserConnection(userId);

            using (var _context = DbContextFactory.Create(userConnectionString))
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
                    if (!seprogramaExists(codigo, _context))
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
            


        }

        // POST: api/seprograma
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        [Route("seprograma/{userConn}")]
        public async Task<ActionResult<seprograma>> Postseprograma(string userConn, seprograma seprograma)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            //var _context = _userConnectionManager.GetUserConnection(userId);

            using (var _context = DbContextFactory.Create(userConnectionString))
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
                    if (seprogramaExists(seprograma.codigo, _context))
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
            
        }

        // DELETE: api/seprograma/5
        [HttpDelete]
        [Route("seprograma/{userConn}/{codigo}")]
        public async Task<IActionResult> Deleteseprograma(string userConn, int codigo)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                //var _context = _userConnectionManager.GetUserConnection(userId);

                using (var _context = DbContextFactory.Create(userConnectionString))
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
                

            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }

        private bool seprogramaExists(int codigo, DBContext _context)
        {
            return (_context.seprograma?.Any(e => e.codigo == codigo)).GetValueOrDefault();

        }




        // GET: api/serolprogs
        [HttpGet]
        [Route("serolprogs/{userConn}")]
        public async Task<ActionResult<IEnumerable<serolprogs>>> Getserolprogs(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                //var _context = _userConnectionManager.GetUserConnection(userId);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.serolprogs == null)
                    {
                        return Problem("Entidad serolprogs es null.");
                    }
                    var result = await _context.serolprogs.OrderBy(codigo => codigo.codigo).ToListAsync();
                    return Ok(result);
                }
                
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }

        // GET: api/serolprogs/5
        [HttpGet]
        [Route("serolprogs/{userConn}/{codigo}")]
        public async Task<ActionResult<serolprogs>> Getserolprogs1(string userConn, int codigo)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                //var _context = _userConnectionManager.GetUserConnection(userId);

                using (var _context = DbContextFactory.Create(userConnectionString))
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
                
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }

        /// <summary>
        /// Obtiene los programas habilitados a un rol
        /// </summary>
        /// <param name="userConn"></param>
        /// <param name="codRol"></param>
        /// <returns></returns>
        // GET: api/serolprogs/5
        [HttpGet]
        [Route("serolprogs/rolGet/{userConn}/{codRol}")]
        public async Task<ActionResult<serolprogs>> Getserolprogs2(string userConn, string codRol)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                //var _context = _userConnectionManager.GetUserConnection(userId);

                using (var _context = DbContextFactory.Create(userConnectionString))
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
                
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }



        /// <summary>
        /// Obtiene los programas habilitados a un rol con check en base a los programas filtrados
        /// </summary>
        /// <param name="userConn"></param>
        /// <param name="codRol"></param>
        /// <param name="codclasificacion"></param>
        /// <param name="codmodulo"></param>
        /// <returns></returns>
        // GET: api/serolprogs/5
        [HttpGet]
        [Route("serolprogs/rolGetCheck/{userConn}/{codRol}/{codclasificacion}/{codmodulo}")]
        public async Task<ActionResult<serolprogs>> Getserolprogs3(string userConn, string codRol, int codclasificacion, int codmodulo)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                //var _context = _userConnectionManager.GetUserConnection(userId);

                using (var _context = DbContextFactory.Create(userConnectionString))
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
                
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }




        /// <summary>
        /// Verifica si un rol tiene acceso a una ventana
        /// </summary>
        /// <param name="userConn"></param>
        /// <param name="acceso"></param>
        /// <returns></returns>
        // POST: api/Verificaserolprogs/DPD/5
        [HttpPost]
        [Route("verificaserolprogs/{userConn}")]
        public async Task<ActionResult<serolprogs>> VerificaAcceso(string userConn, Getserolprogs acceso)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                //var _context = _userConnectionManager.GetUserConnection(userId);

                using (var _context = DbContextFactory.Create(userConnectionString))
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
        [Route("serolprogs/{userConn}/{codigo}")]
        public async Task<IActionResult> Putserolprogs(string userConn, int codigo, serolprogs serolprogs)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            //var _context = _userConnectionManager.GetUserConnection(userId);

            using (var _context = DbContextFactory.Create(userConnectionString))
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
                    if (!serolprogsExists(codigo, _context))
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
            


        }

        // POST: api/serolprogs
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        [Route("serolprogs/{userConn}")]
        public async Task<ActionResult<serolprogs>> Postserolprogs(string userConn, serolprogs serolprogs)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            //var _context = _userConnectionManager.GetUserConnection(userId);

            using (var _context = DbContextFactory.Create(userConnectionString))
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
                    if (serolprogsExists(serolprogs.codigo, _context))
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
            
        }

        // DELETE: api/serolprogs/5
        [HttpDelete]
        [Route("serolprogs/{userConn}/{codigo}")]
        public async Task<IActionResult> Deleteserolprogs(string userConn, int codigo)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                //var _context = _userConnectionManager.GetUserConnection(userId);

                using (var _context = DbContextFactory.Create(userConnectionString))
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
                

            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }

        private bool serolprogsExists(int codigo, DBContext _context)
        {
            return (_context.serolprogs?.Any(e => e.codigo == codigo)).GetValueOrDefault();

        }


    }
}
