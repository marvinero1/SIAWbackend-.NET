using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SIAW.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIAW.Data;
using SIAW.Models;
using SIAW.Controllers.seg_adm.login;
using ApiBackend.Controllers;
using System.Net;
using NuGet.Common;
using System.Configuration;

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
                    /*var token = TokenGenerator.GenerateTokenJwt(usuario);
                    validTokens.Add(token);   //agrega token a la lista de validos
                    return OK (token);*/
                    return Ok("200");                  //------Bienvenido
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
        /*
        [HttpPost]
        [Route("verificaToken")]
        public HttpResponseMessage IsTokenValid(tokken token)
        {
            // Verificar si el token se encuentra en la lista blanca de tokens válidos
            var exitencia = validTokens.Contains(token.token);
            return Request.CreateResponse(HttpStatusCode.OK, exitencia);
        }

        [HttpPost]
        [Route("eliminaToken")]
        public HttpResponseMessage InvalidateToken(tokken token)
        {
            // Remover el token de la lista blanca de tokens válidos
            validTokens.Remove(token.token);
            return Request.CreateResponse(HttpStatusCode.OK, "Token eliminado");
        }
        */
    }
}
