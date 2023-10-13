using MessagePack;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using siaw_DBContext.Data;
using siaw_DBContext.Models;
using siaw_DBContext.Models_Extra;
using System.Data;
using System.Text.RegularExpressions;

namespace siaw_funciones
{
    public class Cliente
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
        public async Task<bool> EsClienteCompetencia(string userConnectionString, string nit_cliente)
        {
            try
            {
                bool resultado = false;
                string codcliente_seg_nit = await Cliente_Segun_Nit(userConnectionString, nit_cliente);
                string cadena_nit = nit_cliente;
                //1ro del nit saco su cod cliente
                string maincode = await CodigoPrincipal(userConnectionString, codcliente_seg_nit);
                //2do veo todos los codigos iguales
                string samecode = await CodigosIguales(userConnectionString, maincode);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var cadena_nits = await _context.vecliente
                                     .Where(v => samecode.Contains(v.codigo))
                                     .Select(v => v.nit)
                                     .ToListAsync();

                    if (cadena_nits.Any())
                    {
                        //cadena_nit = "'" + string.Join("','", cadena_nits) + "'";
                        cadena_nit = "" + string.Join(",", cadena_nits) + "";
                    }


                    var nitsArray = cadena_nit.Split(',').Select(n => n.Trim()).ToArray();

                    var query = await _context.cpcompetencia
                                        .Join(_context.vecompetencia_control,
                                              p1 => p1.codgrupo_control,
                                              p2 => p2.codigo,
                                              (p1, p2) => new { cpcompetencia = p1, vecompetencia_control = p2 })
                                        .Where(x => nitsArray.Contains(x.cpcompetencia.nit))
                                        .ToListAsync();

                    //resultado = query.Any();
                    if (query.Any())
                    {
                        resultado = true;
                    }
                    else
                    {
                        resultado = false;
                    }
                    return resultado;
                }
            }
            catch (Exception)
            {
                return false;
            }

        }
        public async Task<string> Cliente_Segun_Nit(string userConnectionString, string nit)
        {
            try
            {
                string resultado = "";
                var regex = new Regex(@"^\d+$"); // Expresión regular para verificar si la cadena contiene solo dígitos
                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var result = await _context.vecliente
                    .Where(cliente => cliente.nit == nit.Trim())
                    .ToListAsync(); // Cargar datos en memoria

                    var filteredResult = result
                        .Where(cliente => regex.IsMatch(cliente.codigo))
                        .OrderBy(cliente => cliente.codigo)
                        .Select(cliente => new vecliente
                        {
                            codigo = cliente.codigo,
                            razonsocial = cliente.razonsocial,
                            nit = cliente.nit
                        })
                        .FirstOrDefault();

                    if (filteredResult != null)
                    {
                        resultado = filteredResult.codigo;
                    }
                    return resultado;
                }
            }
            catch (Exception)
            {
                return "";
            }

        }
        public async Task<string> CodigosIguales(string userConnectionString, string codcliente)
        {
            try
            {
                string resultado = "";
                var regex = new Regex(@"^\d+$"); // Expresión regular para verificar si la cadena contiene solo dígitos
                string maincode = await CodigoPrincipal(userConnectionString, codcliente);
                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var clientesIguales = await _context.veclientesiguales
                    .Where(cliente => cliente.codcliente_a == maincode)
                    .ToListAsync(); // Cargar datos en memoria

                    var filteredResult = clientesIguales
                        .Where(cliente => regex.IsMatch(cliente.codcliente_b))
                        .OrderBy(cliente => cliente.codcliente_b)
                        .Select(cliente => cliente.codcliente_b)
                        .ToList();

                    if (filteredResult != null)
                    {
                        if (filteredResult.Any())
                        {
                            //resultado = "'" + string.Join("','", filteredResult) + "'";
                            resultado = "" + string.Join(",", filteredResult) + "";
                        }
                    }
                    return resultado;
                }
            }
            catch (Exception)
            {
                return "";
            }

        }

        public async Task<string> TipoSegunClientesIguales(string userConnectionString, string codcliente)
        {
            string codigo_principal = await CodigoPrincipal(userConnectionString, codcliente);
            string nit1 = await NIT(userConnectionString, codigo_principal);
            string nit2 = await NIT(userConnectionString, codcliente);
            string resultado = "";
            if (codigo_principal.Trim() == codcliente.Trim())
            {
                resultado = "Casa Matriz de: " + codcliente;
            }
            else
            {
                if (nit1.Trim() == nit2.Trim())
                {
                    //si es mismo NIT es sucursal
                    resultado = "Sucursal de: " + codigo_principal;
                }
                else
                {
                    resultado = "Parte del Grupo Cial. Con Casa Matriz: " + codigo_principal;
                }
            }
            return resultado;
        }


        public async Task<string> CodigoPrincipal(string userConnectionString, string codcliente)
        {
            try
            {
                string resultado = "";
                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var result = await _context.veclientesiguales
                        .Where(cliente => cliente.codcliente_b == codcliente.Trim())
                        .OrderBy(cliente => cliente.codcliente_a)
                        .Select(cliente => cliente.codcliente_a)
                       .FirstOrDefaultAsync();
                    if (result != null)
                    {
                        resultado = result;
                    }
                    return resultado;
                }
            }
            catch (Exception)
            {
                return "";
            }

        }
       
        public async Task<string> NIT(string userConnectionString, string codcliente)
        {
            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                var nit = await _context.vecliente
                    .Where(item => item.codigo == codcliente)
                    .Select(item => item.nit)
                    .FirstOrDefaultAsync();

                if (nit == null)
                {
                    return "";
                }
                return nit;
            }
        }

    }
}