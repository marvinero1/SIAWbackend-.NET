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

        public async Task<bool> RegistroInventarioExiste(string userConnectionString, int codinventario, int codgrupo)
        {
            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                var infisico = await _context.infisico
                    .Where(i => i.codinvconsol == codinventario && i.codgrupoper== codgrupo)
                    .FirstOrDefaultAsync();
                if (infisico != null)
                {
                    return true;  // hay inventario registrado
                }
                return false;   // no hay inventario registrado
            }
        }



        public async Task<decimal> Peso_Movimiento(DBContext _context, int codmovimiento)
        {
            try
            {
                var resultado = await _context.inmovimiento1
                .Where(d => d.codmovimiento == codmovimiento)
                .Join(
                    _context.initem,
                    d => d.coditem,
                    i => i.codigo,
                    (d, i) => d.cantidad * i.peso
                )
                .SumAsync();
                return (decimal)resultado;
            }
            catch (Exception)
            {
                return 0;
            }
        }

        public async Task<DateTime> InventarioFecha(string userConnectionString, int codinvconsol)
        {
            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                var infisico = await _context.ininvconsol
                    .Where(i => i.codigo == codinvconsol)
                    .Select(i => i.fechainicio)
                    .FirstOrDefaultAsync();
                return infisico;
            }
        }
        public async Task<bool> Usar_Item_En_Notas_Movto(DBContext _context, string coditem)
        {
            var codigo = await _context.initem
                    .Where(i => i.codigo == coditem && i.usar_en_movimiento == true)
                    .Select(i => i.codigo)
                    .FirstOrDefaultAsync();
            if (codigo == null)
            {
                return false;
            }
            return true;
        }
    }
}
