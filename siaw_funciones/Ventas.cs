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


        public async Task<int> Disminuir_Fecha_Vence(DBContext _context, int codremision, string codcliente)
        {
            int nro_dias = 0;
            var dt1 = await _context.vedesextraremi
                    .Where(v => v.codremision == codremision)
                    .ToListAsync();
            foreach (var reg1 in dt1)
            {
                var dt2 = await _context.vecliente_desextra
                    .Where(v => v.codcliente == codcliente && v.coddesextra == reg1.coddesextra).FirstOrDefaultAsync();
                if (dt2 != null)
                {
                    nro_dias += await Dias_Disminuir_Vencimiento(_context, reg1.coddesextra, dt2.dias ?? -1);
                }
            }

            int resultado = nro_dias * -1;


            return resultado;
        }

        public async Task<int> Dias_Disminuir_Vencimiento(DBContext _context, int coddesxextra, int dias)
        {
            var dt = await _context.vedesextra_modifica_dias
                    .Where(v => v.coddesextra == coddesxextra && v.dias == dias)
                    .FirstOrDefaultAsync();
            if (dt != null && dt.dias_disminuye!= null)
            {
                return (int)dt.dias_disminuye;
            }
            return 0;
        }
        public async Task<bool> Pedido_Esta_Despachado(DBContext _context, int codproforma)
        {
            bool resultado = false; 
            string id_proforma = await proforma_id(_context, codproforma);
            int nroid_proforma = await proforma_numeroid(_context, codproforma);

            var dt = await _context.vedespacho
                .Where(i => i.id == id_proforma && i.nroid == nroid_proforma && i.estado== "DESPACHADO")
                .Select(i => new
                {
                    i.id,
                    i.nroid,
                    i.estado,
                    i.fdespachado,
                    i.hdespachado
                }).FirstOrDefaultAsync();
            if (dt != null)
            {
                if (dt.estado != null)
                {
                    if (dt.estado == "DESPACHADO")
                    {
                        resultado = true;
                    }
                    else
                    {
                        resultado = false;
                    }
                }
                else
                {
                    resultado = false;
                }
            }else { resultado = false; }

            return resultado;
        }



        public async Task<string> proforma_id(DBContext _context, int codproforma)
        {
            var resultado = await _context.veproforma.Where(i=>i.codigo == codproforma).Select(i=> i.id).FirstOrDefaultAsync();
            return resultado ?? "";
        }
        public async Task<int> proforma_numeroid(DBContext _context, int codproforma)
        {
            var resultado = await _context.veproforma.Where(i => i.codigo == codproforma).Select(i => i.numeroid).FirstOrDefaultAsync();
            return resultado;
        }

        public async Task<int> codproforma_de_remision(DBContext _context, int codremision)
        {
            var resultado = await _context.veremision.Where(i => i.codigo == codremision).Select(i => i.codproforma).FirstOrDefaultAsync();
            return resultado ?? 0;
        }


        public async Task<DateTime> Obtener_Fecha_Despachado_Pedido(DBContext _context, int codproforma)
        {
            DateTime resultado = new DateTime(1900, 1, 1);
            string id_proforma = await proforma_id(_context, codproforma);
            int nroid_proforma = await proforma_numeroid(_context, codproforma);
            string estado_final_pf = await Estado_Final_Proformas(_context);

            var dt = await _context.vedespacho
                .Where(i => i.id == id_proforma && i.nroid == nroid_proforma && i.estado == estado_final_pf)
                .Select(i => new
                {
                    i.id,
                    i.nroid,
                    i.estado,
                    i.fdespacho,
                    i.hdespacho
                }).FirstOrDefaultAsync();
            if (dt != null)
            {
                if (dt.fdespacho != null)
                {
                    resultado = new DateTime(
                        ((DateTime)dt.fdespacho).Year,
                        ((DateTime)dt.fdespacho).Month,
                        ((DateTime)dt.fdespacho).Day
                        );
                }
            }

            return resultado;
        }

        public async Task<string> Estado_Final_Proformas(DBContext _context)
        {
            var resultado = await _context.adparametros.Select(i => i.estado_final_proformas).FirstOrDefaultAsync();
            return resultado ?? "DESPACHADO";
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
