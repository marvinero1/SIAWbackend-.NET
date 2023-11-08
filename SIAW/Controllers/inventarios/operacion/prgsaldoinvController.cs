using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using siaw_DBContext.Models;
using siaw_funciones;
using Microsoft.AspNetCore.Authorization;
using System.Web.Http.Results;

namespace SIAW.Controllers.inventarios.operacion
{
    [Route("api/inventario/oper/[controller]")]
    [ApiController]
    public class prgsaldoinvController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        public prgsaldoinvController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }



    }
}
