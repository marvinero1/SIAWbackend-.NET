using Microsoft.EntityFrameworkCore;
using siaw_DBContext.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class Adsiat_Endpoint
{
    //#region SINGLETON PATTERN
    //private adsiat_endpoint() { }

    //public static readonly adsiat_endpoint Instancia = new adsiat_endpoint();
    //#endregion
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
    public async Task<string> Obtener_End_Point(DBContext _context, int codigo_endpoint, int codambiente)
    {
        string resultado = "";
        if (codambiente == 1)
        {
            //produccion
            resultado = await _context.adsiat_endpoint.Where(i => i.codigo == codigo_endpoint).Select(I => I.endpoint_produccion).FirstOrDefaultAsync() ?? "NSE";
        }
        else
        {
            //pruebas
            resultado = await _context.adsiat_endpoint.Where(i => i.codigo == codigo_endpoint).Select(I => I.endpoint_pruebas).FirstOrDefaultAsync() ?? "NSE";
        }
        return resultado;
    }
}

