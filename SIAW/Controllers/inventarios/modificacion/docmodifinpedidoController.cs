using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace SIAW.Controllers.inventarios.modificacion
{
    [Route("api/inventario/modif/[controller]")]
    [ApiController]
    public class docmodifinpedidoController : ControllerBase
    {
        private readonly string _controllerName = "docmodifinpedidoController";

        private readonly UserConnectionManager _userConnectionManager;
        public docmodifinpedidoController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }




    }
}
