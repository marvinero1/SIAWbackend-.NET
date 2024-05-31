using Microsoft.EntityFrameworkCore;
using siaw_DBContext.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace siaw_funciones
{
    public class HardCoded
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
        private Almacen almacen = new Almacen();
        public async Task<decimal> MaximoPorcentajeDeVentaPorMercaderia(DBContext _context, int codalmacen)
        {
            decimal resultado = 0;

            //using (_context)
            ////using (var _context = DbContextFactory.Create(userConnectionString))
            //{
            if (await almacen.Es_Tienda(_context,codalmacen))
            {
                resultado = 999;
            }
            else
            {
                resultado = 30;
            }

            //}
            return resultado;
        }

        public string CuentaDeEfectivoVtaContado(string codalmacen)
        {
            string resultado = "";
            switch (codalmacen)
            {
                case "311":
                    resultado = "AG-310 M/N";
                    break;
                case "321":
                    resultado = "AG-321 M/N";
                    break;
                case "331":
                    resultado = "AG-331 M/N";
                    break;
                case "341":
                    resultado = "AG-341 M/N";
                    break;
                case "351":
                    resultado = "AG-351 M/N";
                    break;
                case "411":
                    resultado = "AG-410 M/N";
                    break;
                case "421":
                    resultado = "AG-421 M/N";
                    break;
                case "431":
                    resultado = "AG-431 M/N";
                    break;
                case "441":
                    resultado = "AG-441 M/N";
                    break;
                case "451":
                    resultado = "AG-451 M/N";
                    break;
                case "811":
                    resultado = "AG-810 M/N";
                    break;
                case "821":
                    resultado = "AG-821 M/N";
                    break;
                case "831":
                    resultado = "AG-831 M/N";
                    break;
                case "841":
                    resultado = "AG-841 M/N";
                    break;
                case "851":
                    resultado = "AG-851 M/N";
                    break;
                default:
                    resultado = "AG-310 M/N";
                    break;
            }
            return resultado;
        }



    }

}
