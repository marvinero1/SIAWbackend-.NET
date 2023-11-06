using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using siaw_DBContext.Data;

namespace siaw_funciones
{
    public class Inventario
    {
        //Clase necesaria para el uso del DBContext del proyecto siaw_Context
        public static class DbContextFactory
        {
            public static DBContext Create(string connectionString)
            {
                var optionsBuilder = new DbContextOptionsBuilder<DBContext>();
                optionsBuilder.UseSqlServer(connectionString);

                return new DBContext(optionsBuilder.Options);
            }
        }


        public async Task<bool> existeinv(string userConnectionString, string id, int numeroid)
        {
            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                var ininvconsol = await _context.ininvconsol
                    .Where(i => i.id == id && i.numeroid == numeroid)
                    .FirstOrDefaultAsync();
                if (ininvconsol != null)
                {
                    return true;
                }
                return false;
            }
        }
        public async Task<bool> InventarioFisicoConsolidadoEstaAbierto(string userConnectionString, int codigo)
        {
            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                var ininvconsol = await _context.ininvconsol
                    .Where(i => i.codigo == codigo)
                    .Select(i => i.abierto)
                    .FirstOrDefaultAsync();

                return (bool)ininvconsol;
            }
        }
    }
}
