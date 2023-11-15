using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using siaw_DBContext.Models;
using siaw_funciones;
using Microsoft.AspNetCore.Authorization;
using System.Web.Http.Results;
using siaw_DBContext.Data;

namespace SIAW.Controllers.inventarios.operacion
{
    [Route("api/inventario/oper/[controller]")]
    [ApiController]
    public class prgsaldoinvController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        private readonly Cliente cliente = new Cliente();
        public prgsaldoinvController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }

        // PUT: api/ininvconsol
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPut("{userConn}/{codigo}/{codalmacen}")]
        public async Task<ActionResult<ininvconsol>> Put_prgsaldoinv(string userConn, int codigo, int codalmacen)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                using (var dbContexTransaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        await initem_usar_en_movimiento(_context);
                        await add_ininvconsol1(_context, codigo);
                        await ininvconsol1_cantsist_0(_context, codigo);
                        await update_ininvconsol1(_context, codigo, codalmacen);
                        await verificar_decimales(_context, codigo);
                        await ininvconsol1_dif(_context, codigo);

                        dbContexTransaction.Commit();
                        return Ok(new {resp = "Se actualizaron los saldos con exito." });
                    }
                    catch (Exception)
                    {
                        dbContexTransaction.Rollback();
                        return Problem("Error en el servidor");
                        throw;
                    }
                }
            }
        }





        private async Task<bool> initem_usar_en_movimiento(DBContext _context)
        {
            try
            {
                var initem = await _context.initem.Where(i => i.usar_en_movimiento == null).ToListAsync();
                if (initem.Count > 0)
                {
                    foreach (var item in initem)
                    {
                        item.usar_en_movimiento = true;
                    }
                    await _context.SaveChangesAsync();
                }

                return true;   // Actualizaciones con exito
            }
            catch (Exception)
            {
                throw;
            }
        }

        private async Task<bool> add_ininvconsol1(DBContext _context, int codigo)
        {
            try
            {
                // Obtén los registros a insertar
                var registrosAInsertar = await _context.initem
                    .Where(item => item.kit == false && item.usar_en_movimiento == true && !_context.ininvconsol1.Any(ic => ic.coditem == item.codigo && ic.codinvconsol == codigo))
                    .Select(item => new ininvconsol1
                    {
                        codinvconsol = codigo,
                        coditem = item.codigo,
                        cantreal = 0,
                        cantsist = 0,
                        dif = 0,
                        udm = item.unidad
                    })
                    .ToListAsync();
                // Inserta los registros en la tabla ininvconsol1
                _context.ininvconsol1.AddRange(registrosAInsertar);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                throw;
            }
        }

        private async Task<bool> ininvconsol1_cantsist_0(DBContext _context, int codigo)
        {
            try
            {
                var ininvconsol1 = await _context.ininvconsol1.Where(i => i.codinvconsol == codigo).ToListAsync();
                if (ininvconsol1.Count > 0)
                {
                    foreach (var item in ininvconsol1)
                    {
                        item.cantsist = 0;
                    }
                    await _context.SaveChangesAsync();
                }
                return true;   // Actualizaciones con exito
            }
            catch (Exception)
            {
                throw;
            }
        }

        private async Task<bool> update_ininvconsol1(DBContext _context, int codigo, int codalmacen)
        {
            try
            {
                // Recupera los registros que deseas actualizar
                var registrosAActualizar = await _context.ininvconsol1
                    .Where(ic => ic.codinvconsol == codigo &&
                                 _context.instoactual
                                    .Any(sa => sa.coditem == ic.coditem &&
                                               sa.codalmacen == codalmacen &&
                                               sa.cantidad != null))
                    .ToListAsync();

                // Actualiza las propiedades de los registros
                foreach (var registro in registrosAActualizar)
                {
                    var cantidad = await _context.instoactual
                        .Where(sa => sa.coditem == registro.coditem &&
                                     sa.codalmacen == codalmacen &&
                                     sa.cantidad != null)
                        .Select(sa => sa.cantidad)
                        .FirstOrDefaultAsync();

                    if (cantidad != null)
                    {
                        registro.cantsist = cantidad.Value;
                    }
                }
                // Guarda los cambios en la base de datos
                await _context.SaveChangesAsync();

                return true;   // Actualizaciones con exito
            }
            catch (Exception)
            {
                throw;
            }
        }

        private async Task<bool> verificar_decimales(DBContext _context, int codigo)
        {
            try
            {
                var registros = await _context.ininvconsol1
                        .Where(ic => ic.codinvconsol == codigo &&
                                     (ic.cantsist - Math.Floor(ic.cantsist)) != 0 &&
                                     ic.udm != "KG")
                        .OrderBy(ic => ic.coditem)
                        .ToListAsync();
                if (registros.Count > 0)
                {
                    foreach (var registro in registros)
                    {
                        decimal cantsist_con_decimal = registro.cantsist;
                        int cantsist_entero = (int)await cliente.Redondear_0_Decimales(_context, cantsist_con_decimal);
                        registro.cantsist = cantsist_con_decimal;
                    }
                    await _context.SaveChangesAsync();
                }
                return true; // Actualizaciones con exito
            }
            catch (Exception)
            {
                throw;
            }
        }



        // despues de Verificar Decimales:
        private async Task<bool> ininvconsol1_dif(DBContext _context, int codigo)
        {
            try
            {
                var ininvconsol1 = await _context.ininvconsol1.Where(i => i.codinvconsol == codigo).ToListAsync();
                if (ininvconsol1.Count > 0)
                {
                    foreach (var item in ininvconsol1)
                    {
                        item.dif = item.cantsist - item.cantreal;
                    }
                    await _context.SaveChangesAsync();
                }
                return true;   // Actualizaciones con exito
            }
            catch (Exception)
            {
                throw;
            }
        }

    }
}
