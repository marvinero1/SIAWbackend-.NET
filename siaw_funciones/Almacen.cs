using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using siaw_DBContext.Data;
using siaw_DBContext.Models;


namespace siaw_funciones
{
    public class Almacen
    {
        //Clase necesaria para el uso del DBContext del proyecto siaw_Context
        private readonly TipoCambio tipoCambio = new TipoCambio();
        public static class DbContextFactory
        {
            public static DBContext Create(string connectionString)
            {
                var optionsBuilder = new DbContextOptionsBuilder<DBContext>();
                optionsBuilder.UseSqlServer(connectionString);

                return new DBContext(optionsBuilder.Options);
            }
        }


        public async Task<bool> Es_Tienda(DBContext _context, int codalmacen)
        {
            // verifica si es tienda o no
            var esTienda = await _context.inalmacen
                .Where(i => i.codigo == codalmacen && i.tienda == true)
                .Select(i => i.codigo)
                .CountAsync();
            if (esTienda > 0)
            {
                return true;
            }
            return false;
        }
        public async Task<string> direccionalmacen(DBContext _context, int codalmacen)
        {
            try
            {
                var resultado = await _context.inalmacen
                .Where(i => i.codigo == codalmacen)
                .Select(i => i.direccion)
                .FirstOrDefaultAsync() ?? "";
                return resultado;
            }
            catch (Exception)
            {
                return "";
            }
        }
        public async Task<int> AreaAlmacen(DBContext _context, int codalmacen)
        {
            int resultado = 0;
            try
            {
                resultado = await _context.inalmacen
                .Where(i => i.codigo == codalmacen)
                .Select(i => i.codarea)
                .FirstOrDefaultAsync();
            }
            catch (Exception)
            {
                resultado = 0;
            }
            return resultado;
        }

        public async Task<string> telefonoalmacen(DBContext _context, int codalmacen)
        {
            try
            {
                var resultado = await _context.inalmacen
                .Where(i => i.codigo == codalmacen)
                .Select(i => i.telefono)
                .FirstOrDefaultAsync() ?? "";
                return resultado;
            }
            catch (Exception)
            {
                return "";
            }
        }
        public async Task<string> faxalmacen(DBContext _context, int codalmacen)
        {
            try
            {
                var resultado = await _context.inalmacen
                .Where(i => i.codigo == codalmacen)
                .Select(i => i.fax)
                .FirstOrDefaultAsync() ?? "";
                return resultado;
            }
            catch (Exception)
            {
                return "";
            }
        }
        public async Task<string> lugaralmacen(DBContext _context, int codalmacen)
        {
            try
            {
                var resultado = await _context.inalmacen
                .Where(i => i.codigo == codalmacen)
                .Select(i => i.lugar)
                .FirstOrDefaultAsync() ?? "";
                return resultado;
            }
            catch (Exception)
            {
                return "";
            }
        }

        public async Task<double> MontoMinimoAlmacen(DBContext _context, int codalmacen, int codtarifa, bool suarea, string codmoneda)
        {
            try
            {
                var consulta = await _context.insolurgente_parametros
                    .Where(i => i.codalmacen == codalmacen && i.codtarifa == codtarifa && i.suarea == suarea)
                    .Select(i => new
                    {
                        i.monto,
                        i.codmoneda
                    }).FirstOrDefaultAsync();
                if (consulta != null)
                {
                    double resultado = (double)await tipoCambio._conversion(_context, codmoneda, consulta.codmoneda, DateTime.Now.Date, (decimal)consulta.monto);
                    return resultado;
                }
                return 0;
            }
            catch (Exception)
            {
                return 0;
            }
        }
        public async Task<double> PesoMinimoAlmacen(DBContext _context, int codalmacen, int codtarifa, bool suarea)
        {
            try
            {
                double resultado = (double)(await _context.insolurgente_parametros
                    .Where(i => i.codalmacen == codalmacen && i.codtarifa == codtarifa && i.suarea == suarea)
                    .Select(i => i.peso)
                    .FirstOrDefaultAsync() ?? 0);
                return resultado;
            }
            catch (Exception)
            {
                return 0;
            }
        }

        public async Task<List<int>> Almacenes_del_Area(DBContext _context, int codarea)
        {
            List<int> resultado = new List<int>();
            try
            {
                resultado = await _context.inalmacen
                .Where(i => i.tienda==false && i.codarea == codarea)
                .OrderByDescending(i => i.codigo)
                .Select(i => i.codigo)
                .ToListAsync();
                return resultado;
            }
            catch (Exception)
            {
                throw;
            }
        }

    }
}
