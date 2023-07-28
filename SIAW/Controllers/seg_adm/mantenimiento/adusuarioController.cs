using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.EntityFrameworkCore;
using SIAW.Controllers.seg_adm.login;
using SIAW.Data;
using SIAW.Models;
using SIAW.Models_Extra;
using System.Net;

namespace SIAW.Controllers.seg_adm.mantenimiento
{
    [Route("api/seg_adm/mant/[controller]")]
    [ApiController]
    public class adusuarioController : ControllerBase
    {
        private readonly DBContext _context;
        private readonly string connectionString;
        private VerificaConexion verificador;
        private readonly IConfiguration _configuration;
        encriptacion encript = new encriptacion();
        public adusuarioController(IConfiguration configuration)
        {
            connectionString = ConnectionController.ConnectionString;
            _context = DbContextFactory.Create(connectionString);
            _configuration = configuration;
            verificador = new VerificaConexion(_configuration);
        }

        // GET: api/adusuario
        [HttpGet("{conexionName}")]
        public async Task<ActionResult<IEnumerable<adusuario>>> Getadusuario(string conexionName)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.adusuario == null)
                    {
                        return Problem("Entidad adusuario es null.");
                    }
                    var result = await _context.adusuario.OrderByDescending(fechareg_siaw => fechareg_siaw.fechareg_siaw).ToListAsync();
                    return Ok(result);
                }
                return BadRequest("Se perdio la conexion con el servidor");
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }


        }

        // GET: api/adusuario/5
        [HttpGet("{conexionName}/{login}")]
        public async Task<ActionResult<adusuario>> Getadusuario(string conexionName, string login)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.adusuario == null)
                    {
                        return Problem("Entidad adusuario es null.");
                    }
                    var adusuario = await _context.adusuario.FindAsync(login);

                    if (adusuario == null)
                    {
                        return NotFound("No se encontro un registro con este código");
                    }

                    return Ok(adusuario);
                }
                return BadRequest("Se perdio la conexion con el servidor");
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }

        // PUT: api/adusuario/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{conexionName}/{login}")]
        public async Task<IActionResult> Putadusuario(string conexionName, string login, adusuario adusuario)
        {
            if (verificador.VerConnection(conexionName, connectionString))
            {
                if (login != adusuario.login)
                {
                    return BadRequest("Error con Id en datos proporcionados.");
                }
                DateTime fechaActual = DateTime.Now;
                var f = fechaActual.ToString("yyyy-MM-dd"); //importante
                DateTime fechaHoy = DateTime.Parse(f);  //importante
                adusuario.activo = (bool)adusuario.activo;


                var rolUser = _context.serol.FirstOrDefault(e => e.codigo == adusuario.codrol);
                if (rolUser == null)
                {
                    return NotFound("No se encontro un registro con los datos proporcionados (rol).");
                }


                int longmin = (int)rolUser.long_minima;
                bool num = (bool)rolUser.con_numeros;
                bool let = (bool)rolUser.con_letras;
                string pass = adusuario.password_siaw;
                if (!controlPassword(longmin, num, let, pass))
                {
                    return Unauthorized("Su contraseña no cumple con requisitos de longitud, numeros o letras");
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
                    if (!adusuarioExists(login))
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

        // POST: api/adusuario
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost("{conexionName}")]
        public async Task<ActionResult<adusuario>> Postadusuario(string conexionName, adusuario adusuario)
        {
            if (verificador.VerConnection(conexionName, connectionString))
            {
                if (_context.adusuario == null)
                {
                    return Problem("Entidad adusuario es null.");
                }

                adusuario.activo = (bool)adusuario.activo;

                var rolUser = _context.serol.FirstOrDefault(e => e.codigo == adusuario.codrol);
                if (rolUser == null)
                {
                    return NotFound("No se encontro un registro con los datos proporcionados (rol).");
                }
                int longmin = (int)rolUser.long_minima;
                bool num = (bool)rolUser.con_numeros;
                bool let = (bool)rolUser.con_letras;
                string pass = adusuario.password_siaw;
                if (!controlPassword(longmin, num, let, pass))
                {
                    return Unauthorized("Su contraseña no cumple con requisitos de longitud, numeros o letras");
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
                    if (adusuarioExists(adusuario.login))
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

        // DELETE: api/adusuario/5
        [HttpDelete("{conexionName}/{login}")]
        public async Task<IActionResult> Deleteadusuario(string conexionName, string login)
        {
            try
            {
                if (verificador.VerConnection(conexionName, connectionString))
                {
                    if (_context.adusuario == null)
                    {
                        return Problem("Entidad adusuario es null.");
                    }
                    var adusuario = await _context.adusuario.FindAsync(login);
                    if (adusuario == null)
                    {
                        return NotFound("No existe un registro con ese código");
                    }

                    _context.adusuario.Remove(adusuario);
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

        private bool adusuarioExists(string login)
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
        /// <param name="conexionName"></param>
        /// <param name="login"></param>
        /// <returns></returns>
        [HttpPut]
        [Route("adusuarioEstado/{conexionName}/{login}")]
        public async Task<IActionResult> actualizarEstado(string conexionName, string login, adusuario usu)
        {
            if (verificador.VerConnection(conexionName, connectionString))
            {
                var usuario = _context.adusuario.FirstOrDefault(e => e.login == login);
                if (usuario == null)
                {
                    return NotFound("No existe un registro con esa información");
                }

                usuario.activo = usu.activo;

                _context.Entry(usuario).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!adusuarioExists(login))
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


        /// <summary>
        /// Cambiar la contraseña del Usuario 
        /// </summary>
        /// <param name="conexionName"></param>
        /// <param name="login"></param>
        /// <param name="usu"></param>
        /// <returns></returns>
        [HttpPut]
        [Route("adusuarioPassword/{conexionName}/{login}")]
        public async Task<IActionResult> actualizarContraseña(string conexionName, string login, [FromBody] usuarioPassword usu)
        {
            if (verificador.VerConnection(conexionName, connectionString))
            {
                DateTime fechaActual = DateTime.Now;
                var f = fechaActual.ToString("yyyy-MM-dd"); //importante
                DateTime fechaHoy = DateTime.Parse(f);  //importante

                var usuario = _context.adusuario.FirstOrDefault(e => e.login == login);
                if (usuario == null)
                {
                    return NotFound("No existe un registro con esa información");
                }

                var passAntEncrpt = encript.EncryptToMD5Base64(usu.passwordAnt);

                var rolUser = _context.serol.FirstOrDefault(e => e.codigo == usuario.codrol);
                if (rolUser == null)
                {
                    return NotFound("No se encontro un registro con los datos proporcionados (rol).");
                }
                if (passAntEncrpt != usuario.password_siaw)
                {
                    return Unauthorized("Su contraseña no corresponde a la actual que tiene.");
                }
                int longmin = (int)rolUser.long_minima;
                bool num = (bool)rolUser.con_numeros;
                bool let = (bool)rolUser.con_letras;
                string pass = usu.passwordNew;
                if (!controlPassword(longmin, num, let, pass))
                {
                    return Unauthorized("Su contraseña no cumple con requisitos de longitud, numeros o letras");
                }


                var passEncrpt = encript.EncryptToMD5Base64(pass);

                if (passAntEncrpt == passEncrpt)
                {
                    return Unauthorized("Su nueva contraseña no debe ser igual a la anterior.");
                }


                usuario.password_siaw = passEncrpt;
                usuario.fechareg_siaw = fechaHoy;


                _context.Entry(usuario).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!adusuarioExists(login))
                    {
                        return NotFound("No existe un registro con ese código");
                    }
                    else
                    {
                        return BadRequest("Existen problemas con el Servidor.");
                        throw;
                    }
                }

                return Ok("Datos actualizados correctamente.");
            }
            return BadRequest("Se perdio la conexion con el servidor");
        }



        /// <summary>
        /// Obtiene una lista completa de los usuarios junto con la su rol y la persona a la que pertenece
        /// </summary>
        /// <param name="conexionName"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("adusuarioGetListDetalle/{conexionName}")]
        public async Task<IActionResult> usuarioGetListDetalle(string conexionName)
        {
            if (verificador.VerConnection(conexionName, connectionString))
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
                    return BadRequest("Revise la ruta del Servidor.");
                    throw;
                }
            }
            return BadRequest("Se perdio la conexion con el servencimiento_siawvidor");
        }

        /// <summary>
        /// Obtiene los datos de un usuario junto con la su rol y la persona a la que pertenece
        /// </summary>
        /// <param name="conexionName"></param>
        /// <param name="login"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("adusuarioGetDetalle/{conexionName}/{login}")]
        public async Task<IActionResult> usuarioGetDetalle(string conexionName, string login)
        {
            if (verificador.VerConnection(conexionName, connectionString))
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
                        return BadRequest("Revise los datos ingresados.");
                    }

                    return Ok(query);
                }
                catch (Exception)
                {
                    return BadRequest("Revise la ruta del Servidor.");
                    throw;
                }
            }
            return BadRequest("Se perdio la conexion con el servidor");
        }


    }
}
