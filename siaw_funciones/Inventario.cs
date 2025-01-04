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


        public async Task<bool> DesconsolidarTomaInventario(DBContext _context, int codinventario, int codgrupo)
        {
            // obtener el codigo del inventario fisico
            var infisico = await _context.infisico
                    .Where(i => i.codinvconsol == codinventario && i.codgrupoper == codgrupo && i.consolidado == true)
                    .FirstOrDefaultAsync();


            // restar las cantidades del inventario si son negativas sumar

            if (infisico != null)
            {
                int codigo = infisico.codigo;
                var infisico1 = await _context.infisico1
                    .Where(i => i.codfisico == codigo)
                    .ToListAsync();

                foreach (var item in infisico1)
                {
                    var ininvconsol1 = await _context.ininvconsol1
                        .Where(i => i.codinvconsol == codinventario && i.coditem == item.coditem)
                        .FirstOrDefaultAsync() ;

                    if (item.cantrevis > 0)
                    {
                        ininvconsol1.cantreal = ininvconsol1.cantreal - item.cantrevis;
                    }
                    else
                    {
                        ininvconsol1.cantreal = ininvconsol1.cantreal - Math.Abs(item.cantrevis);
                    }
                    ininvconsol1.dif = ininvconsol1.cantsist - ininvconsol1.cantreal;
                    _context.Entry(ininvconsol1).State = EntityState.Modified;
                    await _context.SaveChangesAsync();
                }
                infisico.consolidado = false;
                _context.Entry(infisico).State = EntityState.Modified;
                await _context.SaveChangesAsync();
            }

            return true;
        }

        public async Task<bool> PermitirNegativos(DBContext _context, string codempresa)
        {
            try
            {
                bool resultado = false;
                var query = await _context.adparametros
                    .Where(i => i.codempresa == codempresa)
                        .Select(i => new
                        {
                            i.negativos
                        })
                    .FirstOrDefaultAsync();
                if (query != null)
                {
                    resultado = (bool)query.negativos;
                }
                else
                {
                    resultado = false;
                }
                return resultado;
            }
            catch (Exception)
            {
                return false;
            }

        }
        public async Task<DateTime> FechaUltimoInventarioFisico(DBContext _context, int codalmacen)
        {
            var infisico = await _context.ininvconsol
                .Where(i => i.codalmacen == codalmacen)
                .OrderByDescending(i => i.fechafin)
                .Select(i => (DateTime?)i.fechafin)
                .FirstOrDefaultAsync();
            if (infisico == null)
            {
                return new DateTime(1900, 1, 1);
            }

            return infisico.Value.Date;
        }

        public async Task<string> Codigo_UDM_SIN(DBContext _context, string unidaddm)
        {
            try
            {
                int resultado = 0;
                var query = await _context.inudemed
                    .Where(i => i.Codigo == unidaddm)
                        .Select(i => new
                        {
                            i.codmedida_sin
                        })
                    .FirstOrDefaultAsync();
                if (query != null)
                {
                    resultado = query.codmedida_sin ?? 0;
                }
                else
                {
                    resultado = 0;
                }
                return resultado.ToString();
            }
            catch (Exception)
            {
                return "0";
            }

        }

        public async Task<bool> ConceptoEsUsuarioFinal(DBContext _context, int codigo)
        {
            bool flag = false;
            try
            {
                flag = await _context.inconcepto.Where(i => i.codigo == codigo).Select(i => i.usuario_final).FirstOrDefaultAsync()??false;

            }
            catch (Exception ex)
            {
                Console.WriteLine("Error en concepto es usuario final: " + ex.Message);
                flag = false;
            }
            return flag;
        }
        public async Task<bool> Concepto_Es_Entrega_Cliente(DBContext _context, int codigo)
        {
            bool flag = false;
            try
            {
                flag = await _context.inconcepto.Where(i => i.codigo == codigo).Select(i => i.cliente).FirstOrDefaultAsync() ?? false;

            }
            catch (Exception ex)
            {
                Console.WriteLine("Error en concepto es entrega cliente: " + ex.Message);
                flag = false;
            }
            return flag;
        }
        public async Task<bool> concepto_espara_despacho(DBContext _context, int codconcepto)
        {
            int consulta = 0;
            try
            {
                consulta = await _context.inconcepto_despacho.Where(i => i.codconcepto == codconcepto).CountAsync();
                if (consulta > 0)
                {
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error en concepto es entrega cliente: " + ex.Message);
                return false;
            }
        }
    }
}
