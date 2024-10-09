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
    }
}
