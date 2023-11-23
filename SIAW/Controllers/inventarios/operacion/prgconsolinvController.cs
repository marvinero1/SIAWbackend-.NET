using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using siaw_DBContext.Models;
using siaw_funciones;
using Microsoft.AspNetCore.Authorization;
using System.Web.Http.Results;
using siaw_DBContext.Data;
using siaw_DBContext.Models_Extra;
using Microsoft.EntityFrameworkCore.Storage;

namespace SIAW.Controllers.inventarios.operacion
{
    [Route("api/inventario/oper/[controller]")]
    [ApiController]
    public class prgconsolinvController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        private readonly Cliente cliente = new Cliente();
        public prgconsolinvController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }


        // GET: api/object
        [HttpGet("{userConn}/{codinvconsol}")]
        public async Task<ActionResult<IEnumerable<dataconsolinv>>> Getconsolinv(string userConn, int codinvconsol)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var resultado = await _context.infisico
                        .Join(
                            _context.ingrupoper,
                            f => f.codgrupoper,
                            g => g.codigo,
                            (f, g) => new { F = f, G = g }
                        )
                        .Where(joined => joined.F.codinvconsol == codinvconsol && joined.F.consolidado == false)
                        .OrderBy(joined => joined.G.nro)
                        .Select(joined => new dataconsolinv
                        {
                            codigo = joined.F.codigo,
                            nro = joined.G.nro,
                            obs = joined.G.obs,
                            consolidado = joined.F.consolidado
                        })
                        .ToListAsync();

                    return Ok(resultado);
                }
            }
            catch (Exception)
            {
                return BadRequest("Error en el servidor");
            }
        }




        // POST: api/object
        [Authorize]
        [HttpPost("pp/{userConn}/{codinvconsol}")]
        public async Task<ActionResult<IEnumerable<detalleIninvconsol1>>> getdocConsolidado(string userConn, int codinvconsol, ListDataconsolinv ListasDataConsol)
        {
            List<detalleIninvconsol1> detalleInvConsol_aux = ListasDataConsol.detalleInvConsol;
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);
            //consolidar documentos uno por uno
            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                using (var dbContexTransaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        foreach (var item in ListasDataConsol.data_consolinv)
                        {
                            // ListasDataConsol.detalleInvConsol = await consolidardoc(_context, item.codigo, codinvconsol, ListasDataConsol.detalleInvConsol);






                            // consolidad pero no repite codigos
                            var datosInfisico = await _context.infisico1
                                    .Where(i => i.codfisico == item.codigo)
                                    .GroupBy(i => i.coditem)
                                    .Select(g => new
                                    {
                                        coditem = g.Key,
                                        real = g.Sum(i => i.cantidad),
                                        revisada = g.Sum(i => i.cantrevis)
                                    })
                                    .ToListAsync();





                            foreach (var datoIn in datosInfisico)
                            {
                                if (datoIn.coditem != "")
                                {
                                    var resultadoBusqueda = detalleInvConsol_aux.Find(detalle => detalle.coditem == datoIn.coditem);
                                    if (resultadoBusqueda == null)
                                    {
                                        // añadir el item
                                        var resultado = await _context.initem
                                            .Where(i => i.codigo == datoIn.coditem)
                                            .Select(x => new detalleIninvconsol1
                                            {
                                                codinvconsol = codinvconsol,
                                                coditem = x.codigo,
                                                descripcion = x.descripcion,
                                                medida = x.medida,
                                                cantreal = datoIn.revisada,
                                                udm = x.unidad,
                                                cantsist = 0,
                                                dif = -datoIn.revisada
                                            })
                                            .FirstOrDefaultAsync();
                                        if (resultado != null)
                                        {
                                            detalleInvConsol_aux.Add(resultado);
                                        }
                                    }
                                    else
                                    {
                                        // editar el item
                                        resultadoBusqueda.cantreal = resultadoBusqueda.cantreal + datoIn.revisada;
                                        resultadoBusqueda.dif = resultadoBusqueda.cantsist - resultadoBusqueda.cantreal;
                                    }
                                }
                            }




                            // consolidar documento
                            var infisico = await _context.infisico.Where(i => i.codigo == item.codigo).FirstOrDefaultAsync();
                            infisico.consolidado = true;
                            _context.Entry(infisico).State = EntityState.Modified;
                            await _context.SaveChangesAsync();
                        }
                        dbContexTransaction.Commit();
                        return Ok(detalleInvConsol_aux);
                    }
                    catch (Exception)
                    {
                        dbContexTransaction.Rollback();
                        return BadRequest("Error en el servidor");
                        throw;
                    }
                }
            }
        }




        // GET: api/object
        [HttpGet("v3/{userConn}/{codinvconsol}")]
        public async Task<ActionResult<IEnumerable<detalleIninvconsol1>>> getdocConsolidado_v3(string userConn, int codinvconsol, ListDataconsolinv ListasDataConsol)
        {
            List<detalleIninvconsol1> detalleInvConsol_aux = ListasDataConsol.detalleInvConsol;
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);
            //consolidar documentos uno por uno
            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                try
                {
                    foreach (var item in ListasDataConsol.data_consolinv)
                    {
                        // ListasDataConsol.detalleInvConsol = await consolidardoc(_context, item.codigo, codinvconsol, ListasDataConsol.detalleInvConsol);


                        // consolidad pero no repite codigos
                        var datosInfisico = await _context.infisico1
                                .Where(i => i.codfisico == item.codigo)
                                .GroupBy(i => i.coditem)
                                .Select(g => new
                                {
                                    coditem = g.Key,
                                    real = g.Sum(i => i.cantidad),
                                    revisada = g.Sum(i => i.cantrevis)
                                })
                                .ToListAsync();



                        foreach (var datoIn in datosInfisico)
                        {
                            if (datoIn.coditem != "")
                            {
                                var resultadoBusqueda = detalleInvConsol_aux.Find(detalle => detalle.coditem == datoIn.coditem);
                                if (resultadoBusqueda == null)
                                {
                                    // añadir el item
                                    var resultado = await _context.initem
                                        .Where(i => i.codigo == datoIn.coditem)
                                        .Select(x => new detalleIninvconsol1
                                        {
                                            codinvconsol = codinvconsol,
                                            coditem = x.codigo,
                                            descripcion = x.descripcion,
                                            medida = x.medida,
                                            cantreal = datoIn.revisada,
                                            udm = x.unidad,
                                            cantsist = 0,
                                            dif = -datoIn.revisada
                                        })
                                        .FirstOrDefaultAsync();
                                    if (resultado != null)
                                    {
                                        detalleInvConsol_aux.Add(resultado);
                                    }
                                }
                                else
                                {
                                    // editar el item
                                    resultadoBusqueda.cantreal = resultadoBusqueda.cantreal + datoIn.revisada;
                                    resultadoBusqueda.dif = resultadoBusqueda.cantsist - resultadoBusqueda.cantreal;
                                }
                            }
                        }
                    }
                    return Ok(detalleInvConsol_aux);
                }
                catch (Exception)
                {
                    return BadRequest("Error en el servidor");
                    throw;
                }
            }
        }



        // POST: api/object
        [Authorize]
        [HttpPost("{userConn}/{codinvconsol}")]
        public async Task<ActionResult<IEnumerable<detalleIninvconsol1>>> getdocConsolidado_v2(string userConn, int codinvconsol, ListDataconsolinv ListasDataConsol)
        {
            List<detalleIninvconsol1> detalleInvConsol_aux = ListasDataConsol.detalleInvConsol;
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);
            //consolidar documentos uno por uno
            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                try
                {
                    var codigos = ListasDataConsol.data_consolinv.Select(obj => obj.codigo).ToList();

                    var datainfisico1 = _context.infisico1
                        .Where(i => codigos.Contains(i.codfisico))
                        .GroupBy(i => i.coditem)
                        .Select(g => new
                        {
                            coditem = g.Key,
                            real = g.Sum(i => i.cantidad),
                            revisada = g.Sum(i => i.cantrevis)
                        })
                        .ToList();

                    var dataconsol = detalleInvConsol_aux
                    .Select(dic => new
                    {
                        dic,
                        dif = datainfisico1.FirstOrDefault(dif => dif.coditem == dic.coditem)
                    })
                    .Select(x => new detalleIninvconsol1
                    {
                        codinvconsol = x.dic.codinvconsol,
                        coditem = x.dic.coditem,
                        descripcion = x.dic.descripcion,
                        medida = x.dic.medida,
                        cantreal = (x.dic.cantreal) + (x.dif?.revisada ?? 0),
                        udm = x.dic.udm,
                        cantsist = x.dic.cantsist,
                        dif = x.dic.cantsist - ((x.dic.cantreal) + (x.dif?.revisada ?? 0))
                    })
                    .ToList();


                    // Identificar elementos en datainfisico1 que no están en detalleInvConsol_aux
                    var elementosFaltantes = datainfisico1
                        .Where(dif => !detalleInvConsol_aux.Any(dic => dic.coditem == dif.coditem))
                        .Join(_context.initem, c => c.coditem, i => i.codigo, (c, i) => new
                        {
                            c,
                            i
                        })
                        .Select(dif => new detalleIninvconsol1
                        {
                            codinvconsol = codinvconsol,
                            coditem = dif.c.coditem,
                            descripcion = dif.i.descripcion,
                            medida = dif.i.medida,
                            cantreal = dif.c.revisada,
                            udm = dif.i.unidad,
                            cantsist = 0,
                            dif = -dif.c.revisada
                        })
                        .ToList();

                    // Combinar ambos resultados
                    dataconsol.AddRange(elementosFaltantes);

                    var dataPActualizar = dataconsol
                        .Select(i => new ininvconsol1
                        {
                            codinvconsol = codinvconsol,
                            coditem = i.coditem,
                            cantreal = i.cantreal,
                            udm = i.udm,
                            cantsist = i.cantsist,
                            dif = i.dif
                        }).ToList();
                    bool actualizaData = await add_and_updateConsols(_context, codinvconsol, ListasDataConsol.data_consolinv, dataPActualizar);

                    if (actualizaData)
                    {
                        return Ok(dataconsol);
                    }
                    return BadRequest("Problema al guardar los datos");

                }
                catch (Exception)
                {
                    return Problem("Error en el servidor");
                    throw;
                }
            }
        }


        private async Task<bool> add_and_updateConsols(DBContext _context, int codigo, List<dataconsolinv> data_consolinv, List<ininvconsol1> ininvconsol1)
        {
            using (var dbContexTransaction = _context.Database.BeginTransaction())
            {
                try
                {
                    foreach (var item in data_consolinv)
                    {
                        // consolidar documento
                        var infisico = await _context.infisico.Where(i => i.codigo == item.codigo).FirstOrDefaultAsync();
                        infisico.consolidado = true;
                        _context.Entry(infisico).State = EntityState.Modified;
                        await _context.SaveChangesAsync();
                    }

                    // Eliminar ininvconsol1
                    var ininvconsol1Del = await _context.ininvconsol1.Where(i => i.codinvconsol == codigo).ToListAsync();
                    if (ininvconsol1Del.Count > 0)
                    {
                        _context.ininvconsol1.RemoveRange(ininvconsol1Del);
                        await _context.SaveChangesAsync();
                    }

                    await _context.ininvconsol1.AddRangeAsync(ininvconsol1);
                    await _context.SaveChangesAsync();


                    dbContexTransaction.Commit();
                    return true;
                }
                catch (Exception)
                {
                    dbContexTransaction.Rollback();
                    return false; // Fallo en guardar cambios
                }
            }
        }


        public class dataconsolinv
        {
            public int codigo { get; set; }
            public int nro { get; set; }
            public string obs { get; set; }
            public bool consolidado { get; set; }
        }

        public class ListDataconsolinv
        {
            public List<dataconsolinv> data_consolinv { get; set; }
            public List<detalleIninvconsol1> detalleInvConsol { get; set; }
        }

    }
}