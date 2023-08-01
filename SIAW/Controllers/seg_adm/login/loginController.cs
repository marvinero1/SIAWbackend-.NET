using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SIAW.Models;
using Microsoft.AspNetCore.Mvc;
using SIAW.Data;
using SIAW.Controllers.seg_adm.login;
using System.Net;


// para generar token
using SIAW.Models_Extra;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using SIAW.Models;

namespace SIAW.Controllers
{
    [Route("api/seg_adm/[controller]")]
    [ApiController]
    public class loginController : ControllerBase
    {
        private static List<string> validTokens = new List<string>();

        private readonly DBContext _context;
        private readonly string connectionString;
        private VerificaConexion verificador;
        private readonly IConfiguration _configuration;

        public loginController(IConfiguration configuration)
        {
            connectionString = ConnectionController.ConnectionString;
            _context = DbContextFactory.Create(connectionString);
            _configuration = configuration;
            verificador = new VerificaConexion(_configuration);
        }

        /// <summary>
        /// Autenticación de usuario, devuelve token para consultas.
        /// </summary>
        /// <param name="conexionName"></param>
        /// <param name="login"></param>
        /// <returns></returns>
        /// <exception cref="HttpResponseException"></exception>
        [HttpPost]
        [Route("authenticate/{conexionName}")]
        public async Task<IActionResult> Authenticate(string conexionName, LoginRequest login)
        {
            if (verificador.VerConnection(conexionName, connectionString))
            {
                encriptacion encript = new encriptacion();
                if (login == null)
                {
                    return BadRequest("Revise los datos ingresados");
                }

                try
                {
                    var x = _context.adusuario.FirstOrDefault(e => e.login == login.login);
                    if (x == null)
                    {
                        return NotFound("201");         //-----No se encontro un registro con los datos proporcionados (usuario).
                    }
                    if (x.password_siaw != encript.EncryptToMD5Base64(login.password))
                    {
                        return Unauthorized("203");           //-----Contraseña Erronea.
                    }
                    if (x.activo == false)
                    {
                        return Unauthorized("207");           //-----Usuario no activo.
                    }
                    var rolUser = _context.serol.FirstOrDefault(e => e.codigo == x.codrol);
                    if (rolUser == null)
                    {
                        return NotFound("No se encontro un registro con los datos proporcionados (rol).");
                    }

                    int dias = (int)rolUser.dias_cambio;

                    if (!verificaFechaPass(x.fechareg_siaw, dias))
                    {
                        return Unauthorized("205");          //------Su contraseña ya venció, registre una nueva.
                    }
                    var usuario = login.login;
                    var jwtToken = GenerateToken(login);
                    validTokens.Add(jwtToken);   //agrega token a la lista de validos
                    //return OK (token);
                    return Ok(new {token= jwtToken });                  //------Bienvenido
                }
                catch (Exception)
                {
                    return BadRequest("Reviso los datos ingresados.");
                    throw;
                }
            }
            return BadRequest("Se perdio la conexion con el servidor");

        }

        public static bool verificaFechaPass(DateTime fechareg, int dias)
        {
            DateTime fechaActual = DateTime.Now;
            var f = fechaActual.ToString("yyyy-MM-dd"); //importante
            DateTime fechaHoy = DateTime.Parse(f);  //importante

            DateTime fechaSumada = fechareg.AddDays((double)dias);
            if (fechaSumada <= fechaHoy)
            {
                return false;
            }
            return true;
        }
        

        [HttpPost]
        [Route("verificaToken")]
        public async Task<IActionResult> IsTokenValid(string token)
        {
            try
            {
                // Verificar si el token se encuentra en la lista blanca de tokens válidos
                var exitencia = validTokens.Contains(token);
                return Ok(exitencia);
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
                throw;
            }
        }

        [HttpPost]
        [Route("eliminaToken")]
        public async Task<IActionResult> InvalidateToken(string token)
        {
            try
            {
                // Remover el token de la lista blanca de tokens válidos
                validTokens.Remove(token);
                return Ok("Token eliminado");
            }
            catch (Exception)
            {
                return BadRequest("No se pudo eliminar el token");
                throw;
            }
            
        }
        



        private string GenerateToken(LoginRequest login)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, login.login)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration.GetSection("JWT:Key").Value));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

            var securityToken = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.Now.AddDays(1),
                signingCredentials: creds);

            string token = new JwtSecurityTokenHandler().WriteToken(securityToken);

            return token;
        }
    }
}
