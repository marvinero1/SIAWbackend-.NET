using Microsoft.EntityFrameworkCore;
using siaw_DBContext.Data;
using siaw_DBContext.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace siaw_funciones
{
    public class Ventas
    {
        //Clase necesaria para el uso del DBContext del proyecto siaw_Context
        Depositos_Cliente depositos_cliente = new Depositos_Cliente();
        public static class DbContextFactory
        {
            public static DBContext Create(string connectionString)
            {
                var optionsBuilder = new DbContextOptionsBuilder<DBContext>();
                optionsBuilder.UseSqlServer(connectionString);

                return new DBContext(optionsBuilder.Options);
            }
        }
        private Configuracion configuracion = new Configuracion();
        public async Task<string> monedabasetarifa(string userConnectionString, int codtarifa)
        {
            string resultado = "";

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                var result = await _context.intarifa
                    .Where(v => v.codigo == codtarifa)
                    .Select(parametro => new
                    {
                        parametro.monedabase
                    })
                    .FirstOrDefaultAsync();
                if (result != null)
                {
                    resultado = result.monedabase;
                }

            }
            return resultado;
        }


        public async Task<bool> Grabar_Descuento_Por_deposito_Pendiente(DBContext _context, int codproforma, string codempresa, string usuarioreg, List<vedesextraprof> vedesextraprof)
        {
            int coddesextra_depositos = await configuracion.emp_coddesextra_x_deposito(_context, codempresa);
            decimal PORCEN_DESCTO = await DescuentoExtra_Porcentaje(_context, coddesextra_depositos);

            bool ya_aplico = false;

            var decuentoxDep = vedesextraprof.Where(i => i.coddesextra == coddesextra_depositos).FirstOrDefault();
            if (decuentoxDep != null)
            {
                if (decuentoxDep.codcobranza != 0)
                {
                    if (await depositos_cliente.Cobranza_Credito_Se_Dio_Descto_Deposito(_context, (int)decuentoxDep.codcobranza))
                    {
                        ya_aplico = true;
                    }
                }
                else if (decuentoxDep.codcobranza_contado != 0)
                {
                    if (await depositos_cliente.Cobranza_Contado_Ce_Se_Dio_Descto_Deposito(_context, (int)decuentoxDep.codcobranza_contado))
                    {
                        ya_aplico = true;
                    }
                }
                else if (decuentoxDep.codanticipo != 0)
                {
                    if (await depositos_cliente.Anticipo_Contado_Aplicado_A_Proforma_Se_Dio_Descto_Deposito(_context, (int)decuentoxDep.codanticipo))
                    {
                        ya_aplico = true;
                    }
                }

                // se registra solo si no aplico antes
                if (!ya_aplico)
                {
                    var dtcbza = new dtcbza();
                    if (decuentoxDep.codcobranza != 0)
                    {
                        dtcbza = await _context.cocobranza
                            .Where(i => i.codigo == decuentoxDep.codcobranza)
                            .Select(i => new dtcbza
                            {
                                codigo = i.codigo,
                                id = i.id,
                                numeroid = i.numeroid,
                                cliente = i.cliente,
                                nit = i.nit
                            }).FirstOrDefaultAsync();
                    }
                    else if (decuentoxDep.codcobranza_contado != 0)
                    {
                        dtcbza = await _context.cocobranza_contado
                            .Where(i => i.codigo == decuentoxDep.codcobranza_contado)
                            .Select(i => new dtcbza
                            {
                                codigo = i.codigo,
                                id = i.id,
                                numeroid = i.numeroid,
                                cliente = i.cliente,
                                nit = i.nit
                            }).FirstOrDefaultAsync();
                    }
                    else
                    {
                        dtcbza = await _context.coanticipo
                            .Where(i => i.codigo == decuentoxDep.codanticipo)
                            .Select(i => new dtcbza
                            {
                                codigo = i.codigo,
                                id = i.id,
                                numeroid = i.numeroid,
                                cliente = i.codcliente,
                                nit = i.nit
                            }).FirstOrDefaultAsync();
                    }
                }
            }

            return true;
        }

        public async Task<decimal> DescuentoExtra_Porcentaje(DBContext _context, int coddesextra)
        {
            var result = await _context.vedesextra
                    .Where(v => v.codigo == coddesextra)
                    .Select(parametro => parametro.porcentaje)
                    .FirstOrDefaultAsync();
            return result;
        }
    }

    public class dtcbza
    {
        public int codigo { get; set; }
        public string id { get; set; }
        public int numeroid { get; set; }
        public string cliente { get; set; }
        public string nit { get; set; }
    }
}
