using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using siaw_DBContext.Data;

namespace siaw_funciones
{
    public class Seguridad
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

        public async Task<bool> periodo_fechaabierta(string userConnectionString, DateTime fecha, int modulo)
        {
            /*
            'detalle de modulos
            '1 : administracion
            '2 : inventario
            '3 : ventas
            '4 : cuentas por cobrar
            '5 : contabilidad
            '6 : activos fijos
            '7 : costo y costeo
            '8 : compras y ctas por pagar
            '9 : personal y planillas
            '10: seguridad
            '11: compras menores
            '12: fondos
            */

            bool resultado = await periodo_abierto(userConnectionString, fecha.Year, fecha.Month, modulo);

            return resultado;
        }

        public async Task<bool> periodo_abierto(string userConnectionString, int anio, int mes, int modulo)
        {
            /*
            'detalle de modulos
            '1 : administracion
            '2 : inventario
            '3 : ventas
            '4 : cuentas por cobrar
            '5 : contabilidad
            '6 : activos fijos
            '7 : costo y costeo
            '8 : compras y ctas por pagar
            '9 : personal y planillas
            '10: seguridad
            '11: compras menores
            '12: fondos
            */

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                var count = await _context.adapertura1
                    .Join(
                        _context.adapertura,
                        a1 => a1.codigo,
                        a => a.codigo,
                        (a1, a) => new { a1, a }
                    )
                    .Where(joined => joined.a.mes == mes && joined.a.ano == anio && joined.a1.sistema == modulo)
                    .CountAsync();

                if (count > 0)
                {
                    return false;   // si hay registro quiere decir que esta cerrado
                }
                return true;   // esta abierto no hay registro
            }
        }

        public async Task<bool> AutorizacionEstaHabilitada(string userConnectionString, int nivel)
        {
            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                var autorizacion_deshabilitado = await _context.adautorizacion_deshabilitadas
                    .Where(i => i.nivel == nivel)
                    .FirstOrDefaultAsync();
                if (autorizacion_deshabilitado != null)
                {
                    return false;   /// la autorizacion esta en la tabla de deshabilitados
                }
                return true;
            }
                
        }

    }
}
