using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SIAW.Models;
using SIAW.Models_Extra;
using System.Data;
using System.Reflection.Metadata;

namespace SIAW.Funciones
{
    public class Cliente
    {
        // ///////////////////////////////////////////////////////////////////////////////
        // Esta funcion devuelve el nivel de dscuentos de un cliente segun un item
        // ///////////////////////////////////////////////////////////////////////////////
        public async Task<string> niveldesccliente(string userConnectionString, string codcliente, string coditem, int codtarifa, string opcion_nivel, bool opcional = false)
        {
            string resultado;
            string columna_nivel = "";
            string nivel = "X";

            if (codcliente.Trim() == "")
            {
                nivel = "X";
            }
            else
            {
                //obtener el desitem del precio
                bool desitem = false;
                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var result = await _context.intarifa
                        .Where(v => v.codigo == codtarifa)
                        .Select(parametro => new
                        {
                            parametro.desitem
                        })
                        .FirstOrDefaultAsync();
                    if (result != null)
                    {
                        desitem = result.desitem;
                    }

                }

                if (desitem == true)
                {
                    //obtener el desitem del precio

                    using (var _context = DbContextFactory.Create(userConnectionString))
                    {
                        var result = await _context.vedescliente
                            .Where(v => v.cliente == codcliente && v.coditem == coditem)
                            .Select(parametro => new
                            {
                                parametro.nivel,
                                parametro.nivel_anterior,
                            })
                            .FirstOrDefaultAsync();
                        if (result != null)
                        {
                            if (opcion_nivel == "ANTERIOR")
                            {
                                nivel = result.nivel_anterior;
                            }
                            else
                            {
                                nivel = result.nivel;
                            }
                        }

                    }
                }
                else
                {
                    nivel = "X";
                }

            }
            resultado = nivel;
            return resultado;
        }
        public async Task<decimal> porcendesccliente(string userConnectionString, string codcliente, string coditem, int codtarifa, string opcion_nivel, bool opcional = false)
        {
            decimal resultado;
            string columna_nivel = "";
            decimal porcentaje = 0;

            if (codcliente.Trim() == "")
            {
                porcentaje = 0;
            }
            else
            {
                //obtener el desitem del precio
                bool desitem = false;
                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var result = await _context.intarifa
                        .Where(v => v.codigo == codtarifa)
                        .Select(parametro => new
                        {
                            parametro.desitem
                        })
                        .FirstOrDefaultAsync();
                    if (result != null)
                    {
                        desitem = result.desitem;
                    }

                }

                if (desitem == true)
                {
                    //obtener el porcentaje del descuento de nivel

                    using (var _context = DbContextFactory.Create(userConnectionString))
                    {
                        if (opcion_nivel == "ANTERIOR")
                        {
                            var descuento = await _context.vedescliente
                        .Join(_context.vedesitem, c => c.coditem, d => d.coditem, (c, d) => new { c, d })
                        .Join(_context.vecliente, cd => cd.c.cliente, l => l.codigo, (cd, l) => new { cd, l })
                        .Join(_context.vevendedor, cdl => cdl.l.codvendedor, v => v.codigo, (cdl, v) => new { cdl, v })
                        .Where(result => result.cdl.cd.c.cliente == codcliente
                        && result.cdl.cd.d.coditem == coditem
                        && result.cdl.cd.c.nivel_anterior == result.cdl.cd.d.nivel
                        && result.v.almacen == result.cdl.cd.d.codalmacen)
                        .Select(result => result.cdl.cd.d.descuento)
                            .FirstOrDefaultAsync();

                            porcentaje = descuento;
                        }
                        else
                        {
                            var descuento = await _context.vedescliente
                        .Join(_context.vedesitem, c => c.coditem, d => d.coditem, (c, d) => new { c, d })
                        .Join(_context.vecliente, cd => cd.c.cliente, l => l.codigo, (cd, l) => new { cd, l })
                        .Join(_context.vevendedor, cdl => cdl.l.codvendedor, v => v.codigo, (cdl, v) => new { cdl, v })
                        .Where(result => result.cdl.cd.c.cliente == codcliente
                        && result.cdl.cd.d.coditem == coditem
                        && result.cdl.cd.c.nivel == result.cdl.cd.d.nivel
                        && result.v.almacen == result.cdl.cd.d.codalmacen)
                        .Select(result => result.cdl.cd.d.descuento)
                            .FirstOrDefaultAsync();

                            porcentaje = descuento;
                        }
                    }
                }
                else
                {
                    porcentaje = 0;
                }

            }
            resultado = porcentaje;
            return resultado;
        }
        public async Task<decimal> Preciodesc(string userConnectionString, string codcliente, int codalmacen, int codtarifa, string coditem, string desc_linea_seg_solicitud, string desc_linea, string opcion_nivel)
        {
            try
            {
                decimal resultado = 0;
                decimal preciocdescuento = 0;

                if (codtarifa == 0 || coditem == "" || codcliente.Trim() == "" || codalmacen == 0)
                {
                    preciocdescuento = 0;
                }
                else
                {
                    decimal preciofinal1 = 0;
                    using (var context = DbContextFactory.Create(userConnectionString))
                    {

                        var preciofinal = new SqlParameter("@preciofinal", SqlDbType.Decimal)
                        {
                            Direction = ParameterDirection.Output,
                            Precision = 18,
                            Scale = 5
                        };
                        await context.Database.ExecuteSqlRawAsync
                            ("EXEC preciocliente @cliente, @almacen, @tarifa, @item, @nivel_desc_segun_solicitud, @nivel_desc_solicitud, @opcion_nivel_desctos, @preciofinal OUTPUT",
                                new SqlParameter("@cliente", codcliente),
                                new SqlParameter("@almacen", codalmacen),
                                new SqlParameter("@tarifa", codtarifa),
                                new SqlParameter("@item", coditem),
                                new SqlParameter("@nivel_desc_segun_solicitud", desc_linea_seg_solicitud),
                                new SqlParameter("@nivel_desc_solicitud", desc_linea),
                                new SqlParameter("@opcion_nivel_desctos", opcion_nivel),
                                preciofinal);
                        preciofinal1 = (decimal)Convert.ToSingle(preciofinal.Value);
                    }
                    preciocdescuento = preciofinal1;

                }
                resultado = preciocdescuento;
                return resultado;
            }
            catch (Exception)
            {
                return 0;
                //return BadRequest("Error en el servidor");
            }
        }

        public async Task<decimal> Preciocondescitem(string userConnectionString, string codcliente, int codalmacen, int codtarifa, string coditem, int coddescuento, string desc_linea_seg_solicitud, string desc_linea, string opcion_nivel)
        {
            try
            {
                decimal resultado = 0;
                decimal preciocondescitem = 0;

                if (codtarifa == 0 || coditem == "" || codcliente.Trim() == "" || codalmacen == 0)
                {
                    preciocondescitem = 0;
                }
                else
                {
                    decimal preciofinal1 = 0;
                    using (var context = DbContextFactory.Create(userConnectionString))
                    {

                        var preciofinal = new SqlParameter("@preciofinal", SqlDbType.Decimal)
                        {
                            Direction = ParameterDirection.Output,
                            Precision = 18,
                            Scale = 5
                        };
                        await context.Database.ExecuteSqlRawAsync
                            ("EXEC preciocondesc @cliente, @almacen, @tarifa, @item,@descuento, @nivel_desc_segun_solicitud, @nivel_desc_solicitud, @opcion_nivel_desctos, @preciofinal OUTPUT",
                                new SqlParameter("@cliente", codcliente),
                                new SqlParameter("@almacen", codalmacen),
                                new SqlParameter("@tarifa", codtarifa),
                                new SqlParameter("@item", coditem),
                                new SqlParameter("@descuento", coddescuento),
                                new SqlParameter("@nivel_desc_segun_solicitud", desc_linea_seg_solicitud),
                                new SqlParameter("@nivel_desc_solicitud", desc_linea),
                                new SqlParameter("@opcion_nivel_desctos", opcion_nivel),
                                preciofinal);
                        preciofinal1 = (decimal)Convert.ToSingle(preciofinal.Value);
                    }
                    preciocondescitem = preciofinal1;

                }
                resultado = preciocondescitem;
                return resultado;
            }
            catch (Exception)
            {
                return 0;
                //return BadRequest("Error en el servidor");
            }
        }

        public async Task<decimal> Redondear_5_Decimales(string userConnectionString, decimal numero)
        {
            try
            {
                decimal resultado = 0;

                if (numero == 0 || numero < 0)
                {
                    resultado = 0;
                }
                else
                {
                    decimal preciofinal1 = 0;
                    using (var context = DbContextFactory.Create(userConnectionString))
                    {

                        var redondeado = new SqlParameter("@resultado", SqlDbType.Decimal)
                        {
                            Direction = ParameterDirection.Output,
                            Precision = 18,
                            Scale = 5
                        };
                        await context.Database.ExecuteSqlRawAsync
                            ("EXEC Redondeo_Decimales_SIA_5_decimales_SQL @minumero, @resultado OUTPUT",
                                new SqlParameter("@minumero", numero),
                                redondeado);
                        preciofinal1 = (decimal)Convert.ToSingle(redondeado.Value);
                    }
                    resultado = preciofinal1;

                }
                return resultado;
            }
            catch (Exception)
            {
                return -1;
            }
        }
    }
}
