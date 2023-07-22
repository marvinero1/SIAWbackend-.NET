using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.EntityFrameworkCore;
using SIAW.Controllers.seg_adm.login;
using SIAW.Data;
using SIAW.Models;
using System.Net;

namespace SIAW.Controllers.seg_adm.mantenimiento
{
    [Route("api/seg_adm/mant/adusuario/[controller]")]
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
                    var result = await _context.adusuario.OrderByDescending(fechareg => fechareg.fechareg).ToListAsync();
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
                string pass = adusuario.password;
                if (!controlPassword(longmin, num, let, pass))
                {
                    return Unauthorized("Su contraseña no cumple con requisitos de longitud, numeros o letras");
                }


                var passEncrpt = encript.EncryptToMD5Base64(pass);
                adusuario.password = passEncrpt;
                adusuario.fechareg = fechaHoy;




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
                string pass = adusuario.password;
                if (!controlPassword(longmin, num, let, pass))
                {
                    return Unauthorized("Su contraseña no cumple con requisitos de longitud, numeros o letras");
                }

                var passEncrpt = encript.EncryptToMD5Base64(pass);
                adusuario.password = passEncrpt;


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

    }
}
