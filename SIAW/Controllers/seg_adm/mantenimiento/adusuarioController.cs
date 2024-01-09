using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.EntityFrameworkCore;
using SIAW.Controllers.seg_adm.login;
using siaw_DBContext.Data;
using siaw_DBContext.Models;
using siaw_DBContext.Models_Extra;
using System.Net;
using Microsoft.AspNetCore.Authorization;

namespace SIAW.Controllers.seg_adm.mantenimiento
{
    [Route("api/seg_adm/mant/[controller]")]
    [ApiController]
    public class adusuarioController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        encriptacion encript = new encriptacion();
        public adusuarioController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;


        }

        // GET: api/adusuario
        [HttpGet("{userConn}")]
        public async Task<ActionResult<IEnumerable<adusuario>>> Getadusuario(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.adusuario == null)
                    {
                        return BadRequest(new { resp = "Entidad adusuario es null." });
                    }
                    var result = await _context.adusuario.OrderByDescending(fechareg_siaw => fechareg_siaw.fechareg_siaw).ToListAsync();
                    return Ok(result);
                }
                
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }


        }

        // GET: api/adusuario/5
        [HttpGet("{userConn}/{login}")]
        public async Task<ActionResult<adusuario>> Getadusuario(string userConn, string login)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.adusuario == null)
                    {
                        return BadRequest(new { resp = "Entidad adusuario es null." });
                    }
                    var adusuario = await _context.adusuario.FindAsync(login);

                    if (adusuario == null)
                    {
                        return NotFound( new { resp = "No se encontro un registro con este código" });
                    }

                    return Ok(adusuario);
                }
                
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }




        // GET: api/catalogo
        [HttpGet]
        [Route("catalogo/{userConn}")]
        public async Task<ActionResult<IEnumerable<adusuario>>> Getadusuario_catalogo(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var query = _context.adusuario
                        .Join(
                            _context.pepersona,
                            adu => adu.persona,
                            pe => pe.codigo,
                            (adu, pe) => new
                            {
                                adu.login,
                                pe.nombre1,
                                pe.apellido1,
                                pe.apellido2
                            }
                        )
                    .OrderBy(i => i.login);

                    var result = query.ToList();

                    if (result.Count() == 0)
                    {
                        return BadRequest(new { resp = "Entidad adusuario es null." });
                    }
                    return Ok(result);
                }

            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
                throw;
            }
        }





        // PUT: api/adusuario/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPut("{userConn}/{login}")]
        public async Task<IActionResult> Putadusuario(string userConn, string login, adusuario adusuario)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (login != adusuario.login)
                {
                    return BadRequest( new { resp = "Error con Id en datos proporcionados." });
                }
                DateTime fechaActual = DateTime.Now;
                var f = fechaActual.ToString("yyyy-MM-dd"); //importante
                DateTime fechaHoy = DateTime.Parse(f);  //importante
                adusuario.activo = (bool)adusuario.activo;


                var rolUser = _context.serol.FirstOrDefault(e => e.codigo == adusuario.codrol);
                if (rolUser == null)
                {
                    return NotFound( new { resp = "No se encontro un registro con los datos proporcionados (rol)." });
                }


                int longmin = (int)rolUser.long_minima;
                bool num = (bool)rolUser.con_numeros;
                bool let = (bool)rolUser.con_letras;
                string pass = adusuario.password_siaw;
                if (!controlPassword(longmin, num, let, pass))
                {
                    return Unauthorized( new { resp = "Su contraseña no cumple con requisitos de longitud, numeros o letras" });
                }


                var passEncrpt = encript.EncryptToMD5Base64(pass);
                adusuario.password_siaw = passEncrpt;
                adusuario.fechareg_siaw = fechaHoy;




                _context.Entry(adusuario).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!adusuarioExists(login, _context))
                    {
                        return NotFound( new { resp = "No existe un registro con ese código" });
                    }
                    else
                    {
                        return Problem("Error en el servidor");
                        throw;
                    }
                }

                return Ok( new { resp = "206" });   // actualizado con exito
            }
            


        }

        // POST: api/adusuario
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPost("{userConn}")]
        public async Task<ActionResult<adusuario>> Postadusuario(string userConn, adusuario adusuario)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (_context.adusuario == null)
                {
                    return BadRequest(new { resp = "Entidad adusuario es null." });
                }

                adusuario.activo = (bool)adusuario.activo;

                var rolUser = _context.serol.FirstOrDefault(e => e.codigo == adusuario.codrol);
                if (rolUser == null)
                {
                    return NotFound( new { resp = "No se encontro un registro con los datos proporcionados (rol)." });
                }
                int longmin = (int)rolUser.long_minima;
                bool num = (bool)rolUser.con_numeros;
                bool let = (bool)rolUser.con_letras;
                string pass = adusuario.password_siaw;
                if (!controlPassword(longmin, num, let, pass))
                {
                    return Unauthorized( new { resp = "Su contraseña no cumple con requisitos de longitud, numeros o letras" });
                }

                var passEncrpt = encript.EncryptToMD5Base64(pass);
                adusuario.password_siaw = passEncrpt;


                _context.adusuario.Add(adusuario);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    if (adusuarioExists(adusuario.login, _context))
                    {
                        return Conflict( new { resp = "Ya existe un registro con ese código" });
                    }
                    else
                    {
                        return Problem("Error en el servidor");
                        throw;
                    }
                }

                return Ok( new { resp = "204" });   // creado con exito

            }
            
        }

        // DELETE: api/adusuario/5
        [Authorize]
        [HttpDelete("{userConn}/{login}")]
        public async Task<IActionResult> Deleteadusuario(string userConn, string login)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (_context.adusuario == null)
                    {
                        return BadRequest(new { resp = "Entidad adusuario es null." });
                    }
                    var adusuario = await _context.adusuario.FindAsync(login);
                    if (adusuario == null)
                    {
                        return NotFound( new { resp = "No existe un registro con ese código" });
                    }

                    _context.adusuario.Remove(adusuario);
                    await _context.SaveChangesAsync();

                    return Ok( new { resp = "208" });   // eliminado con exito
                }
                

            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }

        private bool adusuarioExists(string login, DBContext _context)
        {
            return (_context.adusuario?.Any(e => e.login == login)).GetValueOrDefault();

        }

        public static bool controlPassword(int longmin, bool num, bool letra, string password)
        {
            if (password.Length < longmin)
            {
                return false;
            }
            if (num)
            {
                bool contieneNumero = password.Any(char.IsDigit);
                if (!contieneNumero)
                {
                    return false;
                }
            }
            if (letra)
            {
                bool contieneLetra = password.Any(char.IsLetter);
                if (!contieneLetra)
                {
                    return false;
                }
            }
            return true;

        }



        /// <summary>
        /// Cambiar el estado del Usuario para deshabilitarlo o habilitarlo
        /// </summary>
        /// <param name="userConn"></param>
        /// <param name="login"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPut]
        [Route("adusuarioEstado/{userConn}/{login}")]
        public async Task<IActionResult> actualizarEstado(string userConn, string login, adusuario usu)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                var usuario = _context.adusuario.FirstOrDefault(e => e.login == login);
                if (usuario == null)
                {
                    return NotFound( new { resp = "No existe un registro con esa información" });
                }

                usuario.activo = usu.activo;

                _context.Entry(usuario).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!adusuarioExists(login, _context))
                    {
                        return NotFound( new { resp = "No existe un registro con ese código" });
                    }
                    else
                    {
                        return Problem("Error en el servidor");
                        throw;
                    }
                }

                return Ok( new { resp = "206" });   // actualizado con exito
            }
            
        }



        


       







        /// <summary>
        /// Obtiene una lista completa de los usuarios junto con la su rol y la persona a la que pertenece
        /// </summary>
        /// <param name="userConn"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("adusuarioGetListDetalle/{userConn}")]
        public async Task<IActionResult> usuarioGetListDetalle(string userConn)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                try
                {
                    List<pepersona> lpepersona = _context.pepersona.ToList();
                    List<adusuario> ladusuario = _context.adusuario.ToList();
                    List<serol> lserol = _context.serol.ToList();


                    var query = from us in ladusuario
                                join pe in lpepersona
                                on us.persona equals pe.codigo into table1
                                from pe in table1.DefaultIfEmpty()
                                join ro in lserol
                                on us.codrol equals ro.codigo into table2
                                from ro in table2.DefaultIfEmpty()
                                orderby us.fechareg_siaw descending
                                select new
                                {
                                    login = us.login,
                                    password = us.password,
                                    persona = pe.codigo,
                                    descPersona = pe.nombre1 + " " + pe.apellido1,
                                    vencimiento = us.vencimiento,
                                    activo = us.activo,
                                    codrol = ro.codigo,
                                    descRol = ro.descripcion,
                                    horareg = us.horareg,
                                    fechareg = us.fechareg,
                                    usuarioreg = us.usuarioreg,
                                    password_siaw = us.password_siaw,
                                    fechareg_siaw = us.fechareg_siaw
                                };
                    return Ok(query);
                }
                catch (Exception)
                {
                    return Problem("Error en el servidor");
                    throw;
                }
            }
        }

        /// <summary>
        /// Obtiene los datos de un usuario junto con la su rol y la persona a la que pertenece
        /// </summary>
        /// <param name="userConn"></param>
        /// <param name="login"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("adusuarioGetDetalle/{userConn}/{login}")]
        public async Task<IActionResult> usuarioGetDetalle(string userConn, string login)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            //var _context = _userConnectionManager.GetUserConnection(userId);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                try
                {
                    List<pepersona> lpepersona = _context.pepersona.ToList();
                    List<adusuario> ladusuario = _context.adusuario.ToList();
                    List<serol> lserol = _context.serol.ToList();


                    var query = from us in ladusuario
                                join pe in lpepersona
                                on us.persona equals pe.codigo into table1
                                from pe in table1.DefaultIfEmpty()
                                join ro in lserol
                                on us.codrol equals ro.codigo into table2
                                from ro in table2.DefaultIfEmpty()
                                where us.login == login
                                select new
                                {
                                    login = us.login,
                                    password = us.password,
                                    persona = pe.codigo,
                                    descPersona = pe.nombre1 + " " + pe.apellido1,
                                    vencimiento = us.vencimiento,
                                    activo = us.activo,
                                    codrol = ro.codigo,
                                    descRol = ro.descripcion,
                                    horareg = us.horareg,
                                    fechareg = us.fechareg,
                                    usuarioreg = us.usuarioreg,
                                    password_siaw = us.password_siaw,
                                    fechareg_siaw = us.fechareg_siaw
                                };

                    if (query == null)
                    {
                        return NotFound(new { resp = "No existe un registro con esa información" });
                    }

                    return Ok(query);
                }
                catch (Exception)
                {
                    return Problem("Error en el servidor");
                    throw;
                }
            }
            
        }


    }
}
