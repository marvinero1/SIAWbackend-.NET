using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using siaw_DBContext.Data;
using siaw_DBContext.Models;
using Microsoft.AspNetCore.Authorization;
using siaw_funciones;
using siaw_DBContext.Models_Extra;
using static SIAW.Controllers.inventarios.operacion.prgconsolinvController;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace SIAW.Controllers.inventarios.transaccion
{
    [Route("api/inventario/transac/[controller]")]
    [ApiController]
    public class prggeneraajusteController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        private readonly Inventario inventario = new Inventario();
        public prggeneraajusteController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }


        // POST: api/
        [Authorize]
        [HttpPost("{userConn}")]
        public async Task<ActionResult<string>> generaAjustes(string userConn, ListAjusteInmov listAjusteInmov)
        {
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                using (var dbContexTransaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        // insertar cabecera de nota de movimiento
                        inmovimiento inmovimiento = listAjusteInmov.inmovimientoCab;
                        int CodCabinMovi = await addCabInmovi(_context, inmovimiento);
                        if (CodCabinMovi==-1)
                        {
                            dbContexTransaction.Rollback();
                            return BadRequest("Ya existe una nota de movimiento con ese id y numero id");
                        }
                        
                        // añadir detalle nota de movimiento
                        List<detalleIninvconsol1> detalleInvConsol = listAjusteInmov.detalleInvConsol;
                        bool ifAddCuerpoInmovi = await addCuerpoInmovi(_context,CodCabinMovi, detalleInvConsol);

                        //actualizar stocks
                        int factor = inmovimiento.factor;
                        int almacen = inmovimiento.codalmacen;

                        bool ifActualizaStocks = await actualizarStock(_context, almacen, detalleInvConsol);

                        // actualizar el numero actual de intipomovimiento
                        bool actualizaNum = await updateNumintipomovimiento(_context, inmovimiento);


                        // actualizar peso de cabecera nota de movimiento
                        inmovimiento.peso = await inventario.Peso_Movimiento(_context, CodCabinMovi);
                        _context.Entry(inmovimiento).State = EntityState.Modified;
                        await _context.SaveChangesAsync();

                        dbContexTransaction.Commit();
                        return Ok(new { resp = "Datos guardados con Exito"});
                    }
                    catch (Exception)
                    {
                        dbContexTransaction.Rollback();
                        return Problem("Error en el servidor.");
                    }
                }
            }     
        }


        private async Task<int> addCabInmovi(DBContext _context, inmovimiento inmovimiento)
        {
            try
            {
                var intipomovimiento = await _context.inmovimiento
                    .Where(i => i.id==inmovimiento.id && i.numeroid == inmovimiento.numeroid)
                    .FirstOrDefaultAsync();
                if (intipomovimiento == null)
                {
                    await _context.inmovimiento.AddAsync(inmovimiento);
                    await _context.SaveChangesAsync();
                    return inmovimiento.codigo;   // Agregado con exito
                }
                return -1;
            }
            catch (Exception)
            {
                throw;
            }
        }

        private async Task<bool> addCuerpoInmovi(DBContext _context, int CodCabinMovi, List<detalleIninvconsol1> detalleInvConsol)
        {
            try
            {
                var inmovimiento1 = detalleInvConsol
                            .Select(i => new inmovimiento1
                            {
                                codmovimiento = CodCabinMovi,
                                coditem = i.coditem,
                                cantidad = Math.Abs(i.dif),
                                udm = i.udm,
                                codaduana = ""
                            }).ToList();

                await _context.inmovimiento1.AddRangeAsync(inmovimiento1);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                throw;
            }
        }

        private async Task<bool> actualizarStock(DBContext _context, int almacen, List<detalleIninvconsol1> detalleInvConsol)
        {
            try
            {
                foreach (var item in detalleInvConsol)
                {
                    var query = await _context.instoactual.Where(i => i.coditem == item.coditem && i.codalmacen == almacen).FirstOrDefaultAsync();
                    // dif = cant sist - conteo 
                    // si son negativos (se ebe aumentar porque son ingresos)
                    // si son positivos (se debe restar ya que son egresos)

                    query.cantidad = query.cantidad + (item.dif * (-1));
                    
                    _context.Entry(query).State = EntityState.Modified;
                    await _context.SaveChangesAsync();
                }
                return true;
            }
            catch (Exception)
            {
                throw;
            }
        }

        private async Task<bool> updateNumintipomovimiento(DBContext _context, inmovimiento inmovimiento)
        {
            try
            {
                var query = await _context.intipomovimiento.Where(i => i.id == inmovimiento.id).FirstOrDefaultAsync();
                query.nroactual = inmovimiento.numeroid;
                _context.Entry(query).State = EntityState.Modified;
                await _context.SaveChangesAsync();
                return true;   // Actualizado con exito
            }
            catch (Exception)
            {
                throw;
            }
        }
    }


    public class ListAjusteInmov
    {
        public inmovimiento inmovimientoCab { get; set; }
        public List<detalleIninvconsol1> detalleInvConsol { get; set; }
    }
}
