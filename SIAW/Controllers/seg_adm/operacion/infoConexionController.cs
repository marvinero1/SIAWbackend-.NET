using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace SIAW.Controllers.seg_adm.operacion
{
    [Route("api/seg_adm/oper/[controller]")]
    [ApiController]
    public class infoConexionController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;


        public infoConexionController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }

        [HttpGet]
        [Route("getConnectionInfo/{userConn}")]
        public IActionResult getConnectionInfo(string userConn)
        {
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                var databaseName = _context.Database.GetDbConnection().Database;
                var serverName = _context.Database.GetDbConnection().DataSource;

                return Ok(new { Server = serverName, Database = databaseName });
            }
        }
    }
}
