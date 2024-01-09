using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using siaw_DBContext.Data;
using siaw_DBContext.Models;
using Microsoft.AspNetCore.Authorization;

namespace SIAW.Controllers.seg_adm.operacion
{
    [Route("api/seg_adm/oper/[controller]")]
    [ApiController]
    public class prgverificapesocjtoController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        public prgverificapesocjtoController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }


        // GET: api/initemKits
        [HttpGet]
        [Route("initemKits/{userConn}")]
        public async Task<ActionResult<IEnumerable<initem>>> initemKits(string userConn)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var itemsKit = await _context.initem
                        .Where(i => i.kit == true)
                        .OrderBy(codigo => codigo.codigo)
                        .Select(i => new ResultadoConsulta
                        {
                            Codigo = i.codigo,
                            Kit = i.kit,
                            Peso = i.peso,
                            PesoTotalPartes = 0,
                            Diferencia = 0
                        })
                        .ToListAsync();

                    foreach (var item in itemsKit)
                    {
                        decimal sumPesoTot = 0;
                        var detalles = await _context.inkit
                            .Where(ink => ink.codigo == item.Codigo)
                            .Join(
                            _context.initem,
                            ink => ink.item,
                            item => item.codigo,
                            (ink, item) => new
                            {
                                cantidad = ink.cantidad,
                                peso = item.peso
                            }
                            )
                            .ToListAsync();
                        if ( detalles.Count > 0)
                        {
                            foreach (var i in detalles)
                            {
                                if (i.peso != null && i.cantidad !=null)
                                {
                                    sumPesoTot = (decimal)(sumPesoTot + (i.peso * i.cantidad));
                                }
                            }
                            item.PesoTotalPartes = sumPesoTot;
                            item.Diferencia = (decimal)(item.Peso - item.PesoTotalPartes);
                        }
                        
                    }

                    return Ok(itemsKit);
                }

            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }



    }


    public class ResultadoConsulta
    {
        public string Codigo { get; set; }
        public bool Kit { get; set; }
        public decimal? Peso { get; set; }
        public decimal PesoTotalPartes { get; set; }
        public decimal Diferencia { get; set; }
    }
}
