using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using siaw_DBContext.Data;
using siaw_DBContext.Models;
using Microsoft.AspNetCore.Authorization;
using siaw_funciones;
using siaw_DBContext.Models_Extra;
using System.Runtime.Intrinsics.X86;
using Microsoft.EntityFrameworkCore.Storage;
namespace SIAW.Controllers.inventarios.transaccion
{
    [Route("api/[controller]")]
    [ApiController]
    public class docinfisicorevController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        private readonly Inventario inventario = new Inventario();
        private readonly Seguridad seguridad = new Seguridad();
        public docinfisicorevController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }

        // POST: api/
        [Authorize]
        [HttpPost]
        [Route("validaInvGrup/{userConn}/{id}/{numeroid}/{nro}")]
        public async Task<ActionResult<bool>> validaInvGrup(string userConn, string id, int numeroid, int nro)
        {
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                using (var dbContexTransaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        var result = await _context.ininvconsol
                            .Where(i => i.id == id && i.numeroid == numeroid)
                            .FirstOrDefaultAsync();
                        if (result == null)
                        {
                            return BadRequest("Ese documento de inventario no existe (Doc Inventario)");
                        }
                        int res_codinvconsol = result.codigo;
                        var dataGroup = await _context.ingrupoper
                            .Where(i => i.codinvconsol == res_codinvconsol && i.nro == nro)
                            .FirstOrDefaultAsync();
                        if (dataGroup == null)
                        {
                            return BadRequest("Ese documento de inventario no existe (Grupo)");
                        }
                        int res_codgrupo = dataGroup.codigo;

                        ///////////// 2da parte de validacion

                        bool existe = await inventario.RegistroInventarioExiste(userConnectionString, res_codinvconsol, res_codgrupo);
                        if (!existe)
                        {
                            return BadRequest(new { resp = "El grupo elegido no existe o no fue registrado aun." });
                        }

                        DateTime fecha_inv = await inventario.InventarioFecha(userConnectionString, res_codinvconsol);
                        bool ifAbierto = await seguridad.periodo_fechaabierta(userConnectionString, fecha_inv, 2);

                        if (!ifAbierto)
                        {
                            return BadRequest(new { resp = "No puede crear documentos para ese periodo de fechas. Periodo Cerrado" });
                        }

                        await inventario.DesconsolidarTomaInventario(_context, res_codinvconsol, res_codgrupo);
                        dbContexTransaction.Commit();
                        return Ok(new { resp = "aceptado" });

                        



                        //return Ok(new {codinvconsol = res_codinvconsol, codgrupo = res_codgrupo});
                    }
                    catch (Exception)
                    {
                        dbContexTransaction.Rollback();
                        Problem("Error en el servidor");
                        throw;
                    }
                }
                   
            }
        }




        // GET: api/
        [HttpGet]
        [Route("datosCabecera/{userConn}/{codinventario}/{codgrupo}")]
        public async Task<ActionResult<string>> datosCabecera(string userConn, int codinventario, int codgrupo)
        {
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            try
            {
                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var ininvconsol = _context.ininvconsol
                        .Where(i => i.codigo == codinventario)
                        .FirstOrDefault();


                    var ingrupoper = _context.ingrupoper
                        .Where(i => i.codigo == codgrupo)
                        .FirstOrDefault();

                    return Ok(new
                    {
                        id = ininvconsol.id,
                        numeroid = ininvconsol.numeroid,
                        nrogrupo = ingrupoper.nro,
                        obsgrupo = ingrupoper.obs
                    });
                }
            }
            catch (Exception)
            {
                return Problem("Error en el servidor.");
                throw;
            }
        }



        // GET: api/
        [HttpGet]
        [Route("validardetalle/{userConn}/{codinventario}/{codgrupo}")]
        public async Task<ActionResult<string>> validardetalle(string userConn, int codinventario, int codgrupo, List<string> codItems)
        {
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);
            List<string> itemsObs = new List<string>();
            try
            {
                bool existe = await inventario.RegistroInventarioExiste(userConnectionString, codinventario, codgrupo);
                if (existe)
                {
                    return BadRequest(new { resp = "Ya se registro esa toma de inventario, Para llevar a cabo modificaciones por favor entre a Revision de Toma de Inventario." });
                }
                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    foreach (var item in codItems)
                    {
                        bool resultado_c = await inventario.Usar_Item_En_Notas_Movto(_context, item);
                        if (!resultado_c)
                        {
                            itemsObs.Add(item);
                        }
                    }
                    if (itemsObs.Count() > 0)
                    {
                        return BadRequest(new
                        {
                            resp = "los siguientes items no estan habilitados para movimientos: ",
                            list = itemsObs
                        });
                    }
                    return Ok(new { resp = "Todo Valido" });
                }
            }
            catch (Exception)
            {
                return Problem("Error en el servidor.");
                throw;
            }
        }




        // POST: api/
        [HttpPost]
        [Route("guardardetalle/{userConn}/{codinfisico}")]
        public async Task<ActionResult<string>> guardardetalle(string userConn, int codinfisico, List<detalleInfisico> infisicoDetalle)
        {

            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            var infisico1 = infisicoDetalle
                .Select(i => new infisico1
                {
                    codfisico = codinfisico,
                    coditem = i.coditem,
                    cantidad = i.cantidad,
                    udm = i.udm,
                    cantrevis = i.cantidad,
                    fase = 0,
                    codzona = i.codzona
                })
                .ToList();


            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                using (var dbContexTransaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        var valida = await _context.infisico1.Where(i => i.codfisico == codinfisico).ToListAsync();
                        if (valida.Count > 0)
                        {
                            _context.infisico1.RemoveRange(valida);
                            await _context.SaveChangesAsync();
                        }
                        _context.infisico1.AddRange(infisico1);
                        await _context.SaveChangesAsync();

                        dbContexTransaction.Commit();
                        return Ok(new { resp = "Datos guardados con Exito" });
                    }
                    catch (DbUpdateException)
                    {
                        dbContexTransaction.Rollback();
                        return Problem("Error en el servidor");
                    }
                }
            }
        }

    }
}
