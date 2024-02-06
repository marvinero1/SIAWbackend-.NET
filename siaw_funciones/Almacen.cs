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
        public static class DbContextFactory
        {
            public static DBContext Create(string connectionString)
            {
                var optionsBuilder = new DbContextOptionsBuilder<DBContext>();
                optionsBuilder.UseSqlServer(connectionString);

                return new DBContext(optionsBuilder.Options);
            }
        }


        public async Task<bool> Es_Tienda(string userConnectionString, int codalmacen)
        {
            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                // verifica si es tienda o no
                var esTienda = await _context.inalmacen
                    .Where(i => i.codigo == codalmacen && i.tienda==true)
                    .Select(i => i.codigo)
                    .CountAsync();
                if (esTienda>0)
                {
                    return true;
                }
                return false;
            }
        }
    }
}
