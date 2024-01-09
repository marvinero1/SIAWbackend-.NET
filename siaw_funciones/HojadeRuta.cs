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
    public class HojadeRuta
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
        public async Task<bool> Hay_Hoja_de_Rutas_Generada(DBContext _context, DateTime fecha_inicial, int codvendedor)
        {
            bool resultado = false;
            
            return resultado;
        }


        public async Task<bool> ExisteRutaAsignada(DBContext _context, DateTime fecha, int codvendedor)
        {
            try
            {
                var count = await _context.vehojaderuta
                .Where(v => v.fecha == fecha && v.codvendedor == codvendedor)
                .CountAsync();
                if (count > 0)
                {
                    return true;
                }
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<string> CodHoja_de_Ruta_Generada(DBContext _context, DateTime fecha, int codvendedor)
        {
            var codRuta = "";
            codRuta = await _context.vehojaderuta
                .Where(v => v.codvendedor == codvendedor && v.fecha == fecha)
                .Select(v => v.codruta)
                .FirstOrDefaultAsync();
            if (codRuta==null)
            {
                return "";
            }
            return codRuta;
        }

    }
}
