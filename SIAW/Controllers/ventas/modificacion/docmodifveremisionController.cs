using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace SIAW.Controllers.ventas.modificacion
{
    [Route("api/venta/modif/[controller]")]
    [ApiController]
    public class docmodifveremisionController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;

        private readonly string _controllerName = "docmodifveremisionController";

        public docmodifveremisionController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }
    }
}
