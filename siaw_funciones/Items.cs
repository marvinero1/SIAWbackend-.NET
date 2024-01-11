using Microsoft.EntityFrameworkCore;
using NuGet.Configuration;
using siaw_DBContext.Data;
using siaw_DBContext.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace siaw_funciones
{
    public class Items
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

        public async Task<bool> itemventa(string userConnectionString, string coditem)
        {
            try
            {
                bool resultado = false;
                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    string estado = "4";
                    var query = await _context.initem
                        .Where(i => i.codigo == coditem)
                            .Select(i => new
                            {
                                i.estadocv
                            })
                        .FirstOrDefaultAsync();
                    if (query != null)
                    {
                        estado = query.estadocv;
                    }
                    if (estado == "1" || estado == "2")
                    {
                        resultado= true;
                    }
                    else
                    {
                        resultado= false;   
                    }                    
                }
                return resultado;
            }
            catch (Exception)
            {
                return false;
            }

        }

        public async Task<bool> disminuiritem(DBContext _context, string codigo)
        {
            var query = await _context.instoactual
                .Where(i => i.coditem == codigo)
                 .ToListAsync();
            if (query.Count() > 0)   // elimina
            {
                _context.instoactual.RemoveRange(query);
                await _context.SaveChangesAsync();
            }
            return true;

        }


    }
}
