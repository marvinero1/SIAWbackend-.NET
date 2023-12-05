﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using siaw_DBContext.Models;
using Microsoft.AspNetCore.Mvc;
using siaw_DBContext.Data;
using SIAW.Controllers.seg_adm.login;
using System.Net;


// para generar token
using siaw_DBContext.Models_Extra;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace SIAW.Controllers
{
    [Route("api/seg_adm/[controller]")]
    [ApiController]
    public class loginController : ControllerBase
    {
        private static List<string> validTokens = new List<string>();

        //private readonly DBContext _context;
        private readonly string connectionString;
        //private VerificaConexion verificador;
        private readonly IConfiguration _configuration;

        encriptacion encript = new encriptacion();

        private readonly UserConnectionManager _userConnectionManager;

        public loginController(UserConnectionManager userConnectionManager, IConfiguration configuration)
        {
            connectionString = ConnectionController.ConnectionString;
           
            
            
            //_context = DbContextFactory.Create(connectionString);
            _configuration = configuration;
            //verificador = new VerificaConexion(_configuration);

            _userConnectionManager = userConnectionManager;
        }


        /// <summary>
        /// Autenticación de usuario, devuelve token para consultas.
        /// </summary>
        /// <param name="conexionName"></param>
        /// <param name="login"></param>
        /// <returns></returns>
        /// <exception cref="HttpResponseException"></exception>
        [HttpPost]
        [Route("authenticate/{userConn}")]
        public async Task<IActionResult> Authenticate(string userConn, LoginRequest login)
        {
            using (var _context = DbContextFactory.Create(connectionString))
            {
                encriptacion encript = new encriptacion();
                if (login == null)
                {
                    return BadRequest("Revise los datos ingresados");
                }

                try
                {
                    var data = _context.adusuario.FirstOrDefault(e => e.login == login.login);
                    if (data == null)
                    {
                        return NotFound("201");         //-----No se encontro un registro con los datos proporcionados (usuario).
                    }
                    if (data.password_siaw != encript.EncryptToMD5Base64(login.password))
                    {
                        return Unauthorized("203");           //-----Contraseña Erronea.
                    }
                    if (data.activo == false)
                    {
                        return Unauthorized("207");           //-----Usuario no activo.
                    }
                    var rolUser = _context.serol.FirstOrDefault(e => e.codigo == data.codrol);
                    if (rolUser == null)
                    {
                        return NotFound("213");                //-----No se encontro un registro con los datos proporcionados (rol).
                    }  

                    int dias = (int)rolUser.dias_cambio;

                    if (!verificaFechaPass(data.fechareg_siaw, dias))
                    {
                        return Unauthorized("205");          //------Su contraseña ya venció, registre una nueva.
                    }
                    var usuario = login.login;
                    var jwtToken = GenerateToken(login, connectionString);
                    validTokens.Add(jwtToken);   //agrega token a la lista de validos
                    //return OK (token);
                    guardaStringConection(userConn, connectionString);
                    return Ok(new {token= jwtToken });                  //------Bienvenido
                }
                catch (Exception)
                {
                    return BadRequest("Reviso los datos ingresados.");
                    throw;
                }
            }

        }

        private void guardaStringConection(string userConn, string connectioString)
        {
            _userConnectionManager.SetUserConnection(userConn, connectioString);
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
        [Route("logout/{userConn}/{token}")]
        public async Task<IActionResult> InvalidateToken(string userConn, string token)
        {
            try
            {
                // Remover el token de la lista blanca de tokens válidos
                validTokens.Remove(token);
                _userConnectionManager.RemoveUserConnection(userConn);
                return Ok("Logout exitoso");
            }
            catch (Exception)
            {
                return BadRequest("No se pudo realizar el logout");
                throw;
            }
            
        }
        



        private string GenerateToken(LoginRequest login, string cadConection)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, login.login),
                //new Claim("ConnectionString", cadConection) // Agregar la cadena de conexión como claim
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration.GetSection("JWT:Key").Value));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

            var securityToken = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.Now.AddDays(1),
                //expires: DateTime.Now.AddMinutes(30),
                signingCredentials: creds);

            string token = new JwtSecurityTokenHandler().WriteToken(securityToken);

            return token;
        }



        /// <summary>
        /// Cambiar la contraseña del Usuario antes de Login
        /// </summary>
        /// <param name="userConn"></param>
        /// <param name="login"></param>
        /// <param name="usu"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("changePassword/{login}")]
        public async Task<IActionResult> actualizarContraseña(string login, [FromBody] usuarioPassword usu)
        {
            using (var _context = DbContextFactory.Create(connectionString))
            {
                DateTime fechaActual = DateTime.Now;
                var f = fechaActual.ToString("yyyy-MM-dd"); //importante
                DateTime fechaHoy = DateTime.Parse(f);  //importante

                var usuario = _context.adusuario.FirstOrDefault(e => e.login == login);
                if (usuario == null)
                {
                    return NotFound("201");         //-----No se encontro un registro con los datos proporcionados (usuario).
                }

                var passAntEncrpt = encript.EncryptToMD5Base64(usu.passwordAnt);

                var rolUser = _context.serol.FirstOrDefault(e => e.codigo == usuario.codrol);
                if (rolUser == null)
                {
                    return Unauthorized("213");        //-----No se encontro un registro con los datos proporcionados (rol).
                }
                if (passAntEncrpt != usuario.password_siaw)
                {
                    return Unauthorized("203");    //-----Contraseña Erronea.
                }
                int longmin = (int)rolUser.long_minima;
                bool num = (bool)rolUser.con_numeros;
                bool let = (bool)rolUser.con_letras;
                string pass = usu.passwordNew;
                if (!controlPassword(longmin, num, let, pass))
                {
                    return Unauthorized("209");    //-----Su contraseña no cumple con requisitos de longitud, numeros o letras
                }


                var passEncrpt = encript.EncryptToMD5Base64(pass);

                if (passAntEncrpt == passEncrpt)
                {
                    return Unauthorized("211");    //-----Su nueva contraseña no debe ser igual a la anterior.
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
                    if (!adusuarioExists(login, _context))
                    {
                        return NotFound("No existe un registro con ese código");
                    }
                    else
                    {
                        return Problem("Existen problemas con el Servidor.");
                        throw;
                    }
                }

                return Ok(new { resp = "206" });
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



    }
}
