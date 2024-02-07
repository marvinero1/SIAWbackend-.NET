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
    public class Depositos_Cliente
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


        public async Task<bool> Cobranza_Credito_Se_Dio_Descto_Deposito(DBContext _context, int codcobranza)
        {
            var resultadoes = await _context.cocobranza_deposito
                .Where(i => i.codcobranza == codcobranza)
                .CountAsync();
            if (resultadoes > 0)
            {
                return true;
            }
            return false;
        }
        public async Task<bool> Cobranza_Contado_Ce_Se_Dio_Descto_Deposito(DBContext _context, int codcobranza_contado)
        {
            var resultadoes = await _context.cocobranza_deposito
                .Where(i => i.codcobranza_contado == codcobranza_contado)
                .CountAsync();
            if (resultadoes > 0)
            {
                return true;
            }
            return false;
        }
        public async Task<bool> Anticipo_Contado_Aplicado_A_Proforma_Se_Dio_Descto_Deposito(DBContext _context, int codanticipo)
        {
            var resultadoes = await _context.cocobranza_deposito
                .Where(i => i.codanticipo == codanticipo)
                .CountAsync();
            if (resultadoes > 0)
            {
                return true;
            }
            return false;
        }
    }
}
