﻿using MessagePack;
using Microsoft.Build.Framework;
using Microsoft.CodeAnalysis;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using siaw_DBContext.Data;
using siaw_DBContext.Models;
using siaw_DBContext.Models_Extra;
using System.Collections;
using System.Data;
using System.Text.RegularExpressions;

namespace siaw_funciones
{
    public class Cliente
    {
        //Clase necesaria para el uso del DBContext del proyecto siaw_Context
        TipoCambio tipocambio = new TipoCambio();
        Seguridad seguridad = new Seguridad();
        Funciones funciones = new Funciones();
        Items items = new Items();
        Configuracion configuracion = new Configuracion();
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
        public async Task<string> niveldesccliente(DBContext _context, string codcliente, string coditem, int codtarifa, string opcion_nivel, bool opcional = false)
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
                if (desitem == true)
                {
                    //obtener el desitem del precio

                    var result1 = await _context.vedescliente
                            .Where(v => v.cliente == codcliente && v.coditem == coditem)
                            .Select(parametro => new
                            {
                                parametro.nivel,
                                parametro.nivel_anterior,
                            })
                            .FirstOrDefaultAsync();
                    if (result1 != null)
                    {
                        if (opcion_nivel == "ANTERIOR")
                        {
                            nivel = result1.nivel_anterior;
                        }
                        else
                        {
                            nivel = result1.nivel;
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
        public async Task<decimal> porcendesccliente(DBContext _context, string codcliente, string coditem, int codtarifa, string opcion_nivel, bool opcional = false)
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

                if (desitem == true)
                {
                    //obtener el porcentaje del descuento de nivel

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
                else
                {
                    porcentaje = 0;
                }

            }
            resultado = porcentaje;
            return resultado;
        }
        public async Task<decimal> CantidadVendida_PF(DBContext _context, string coditem, string codcliente, DateTime desde, DateTime hasta)
        {
            decimal resultado;
            //decimal cant_item;

            try
            {
                // Cantidad vendida con el código de ese item
                decimal cant_item = (decimal)(await _context.veproforma1
                .Join(_context.veproforma,
                    detalle => detalle.codproforma,
                    proforma => proforma.codigo,
                    (detalle, proforma) => new { Detalle = detalle, Proforma = proforma })
                .Where(joinResult =>
                    joinResult.Proforma.anulada == false &&
                    joinResult.Proforma.codcliente_real == codcliente &&
                    joinResult.Proforma.aprobada == true &&
                    joinResult.Proforma.fecha >= desde.Date && joinResult.Proforma.fecha <= hasta.Date &&
                    joinResult.Detalle.coditem == coditem)
                .SumAsync(joinResult => (double?)joinResult.Detalle.cantidad) ?? 0);
                resultado = cant_item; // + cant_partes + cant_conj;
                return resultado;
            }
            catch (Exception ex)
            {
                return 0;
            }
        }
        public async Task<decimal> CantidadVendida_NR(DBContext _context, string coditem, string codcliente, DateTime desde, DateTime hasta)
        {
            decimal resultado;
            //decimal cant_item;

            try
            {
                // Cantidad vendida con el código de ese item
                decimal cant_item = (decimal)(await _context.veremision1
                .Join(_context.veremision,
                    detalle => detalle.codremision,
                    remision => remision.codigo,
                    (detalle, remision) => new { Detalle = detalle, Remision = remision })
                .Where(joinResult =>
                    joinResult.Remision.anulada == false &&
                    joinResult.Remision.codcliente_real == codcliente &&
                    joinResult.Remision.anulada == false &&
                    joinResult.Remision.transferida == true &&
                    joinResult.Remision.fecha >= desde.Date && joinResult.Remision.fecha <= hasta.Date &&
                    joinResult.Detalle.coditem == coditem)
                .SumAsync(joinResult => (double?)joinResult.Detalle.cantidad) ?? 0);
                resultado = cant_item; // + cant_partes + cant_conj;
                return resultado;
            }
            catch (Exception ex)
            {
                return 0;
            }
        }


        public async Task<(decimal precio, bool exito)> Preciodesc(DBContext context, string codcliente, int codalmacen, int codtarifa, string coditem, string desc_linea_seg_solicitud, string desc_linea, string opcion_nivel)
        {
            try
            {
                if (codtarifa == 0 || string.IsNullOrWhiteSpace(coditem) || string.IsNullOrWhiteSpace(codcliente) || codalmacen == 0)
                {
                    return (0, false); // Valores no válidos, retorno inmediato
                }

                var preciofinal = new SqlParameter("@preciofinal", SqlDbType.Decimal)
                {
                    Direction = ParameterDirection.Output,
                    Precision = 18,
                    Scale = 10
                };

                await context.Database.ExecuteSqlRawAsync(
                    "EXEC preciocliente @cliente, @almacen, @tarifa, @item, @nivel_desc_segun_solicitud, @nivel_desc_solicitud, @opcion_nivel_desctos, @preciofinal OUTPUT",
                    new SqlParameter("@cliente", codcliente),
                    new SqlParameter("@almacen", codalmacen),
                    new SqlParameter("@tarifa", codtarifa),
                    new SqlParameter("@item", coditem),
                    new SqlParameter("@nivel_desc_segun_solicitud", desc_linea_seg_solicitud),
                    new SqlParameter("@nivel_desc_solicitud", desc_linea),
                    new SqlParameter("@opcion_nivel_desctos", opcion_nivel),
                    preciofinal);

                // Convertir el valor de salida
                decimal preciofinal1 = (decimal)Convert.ToSingle(preciofinal.Value);

                return (preciofinal1, true); // Retorno exitoso con el precio
            }
            catch (Exception)
            {
                return (0, false); // En caso de error, retornar 0 y exito = false
            }
        }


        public async Task<(decimal precio, bool exito)> Preciocondescitem(DBContext context, string codcliente, int codalmacen, int codtarifa, string coditem, int coddescuento, string desc_linea_seg_solicitud, string desc_linea, string opcion_nivel)
        {
            try
            {
                // Validación inicial de parámetros
                if (codtarifa == 0 || string.IsNullOrWhiteSpace(coditem) || string.IsNullOrWhiteSpace(codcliente) || codalmacen == 0)
                {
                    return (0, false); // Parámetros no válidos
                }

                var preciofinal = new SqlParameter("@preciofinal", SqlDbType.Decimal)
                {
                    Direction = ParameterDirection.Output,
                    Precision = 18,
                    Scale = 10
                };

                // Ejecución de la consulta
                await context.Database.ExecuteSqlRawAsync(
                    "EXEC preciocondesc @cliente, @almacen, @tarifa, @item, @descuento, @nivel_desc_segun_solicitud, @nivel_desc_solicitud, @opcion_nivel_desctos, @preciofinal OUTPUT",
                    new SqlParameter("@cliente", codcliente),
                    new SqlParameter("@almacen", codalmacen),
                    new SqlParameter("@tarifa", codtarifa),
                    new SqlParameter("@item", coditem),
                    new SqlParameter("@descuento", coddescuento),
                    new SqlParameter("@nivel_desc_segun_solicitud", desc_linea_seg_solicitud),
                    new SqlParameter("@nivel_desc_solicitud", desc_linea),
                    new SqlParameter("@opcion_nivel_desctos", opcion_nivel),
                    preciofinal);

                // Convertir el valor de salida
                decimal preciofinal1 = (decimal)Convert.ToSingle(preciofinal.Value);

                return (preciofinal1, true); // Retorno exitoso con el precio calculado
            }
            catch (Exception)
            {
                return (0, false); // Retorno en caso de error
            }
        }


        public async Task<decimal> Redondear_5_Decimales(DBContext context, decimal numero)
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
                    preciofinal1 = (decimal)(redondeado.Value);
                    resultado = preciofinal1;

                }
                return resultado;
            }
            catch (Exception)
            {
                return -1;
            }
        }

        public async Task<decimal> Redondear_0_Decimales(DBContext context, decimal numero)
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
                    var redondeado = new SqlParameter("@resultado", SqlDbType.Decimal)
                    {
                        Direction = ParameterDirection.Output,
                        Precision = 18,
                        Scale = 5
                    };
                    await context.Database.ExecuteSqlRawAsync
                        ("EXEC Redondeo_Decimales_SIA_0_decimales_SQL @minumero, @resultado OUTPUT",
                            new SqlParameter("@minumero", numero),
                            redondeado);
                    preciofinal1 = (decimal)Convert.ToSingle(redondeado.Value);
                    resultado = preciofinal1;

                }
                return resultado;
            }
            catch (Exception)
            {
                return -1;
            }
        }

        public async Task<bool> EsClienteCompetencia(DBContext _context, string nit_cliente)
        {
            try
            {
                bool resultado = false;

                string codcliente_seg_nit = await Cliente_Segun_Nit(_context, nit_cliente);
                string cadena_nit = nit_cliente;
                //1ro del nit saco su cod cliente
                string maincode = await CodigoPrincipal(_context, codcliente_seg_nit);
                //2do veo todos los codigos iguales
                string samecode = await CodigosIguales(_context, maincode);
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
            catch (Exception)
            {
                return false;
            }

        }

        public async Task<bool> Cliente_Competencia_Controla_Descto_Nivel(DBContext _context, string nit_cliente)
        {
            //verifica si al cliente se le debe controlar 
            //si sus descuento de linea  a nivel de item estan con Z o X
            var dt = await _context.cpcompetencia
                .Join(
                    _context.vecompetencia_control,
                    p1 => p1.codgrupo_control,
                    p2 => p2.codigo,
                    (p1, p2) => new { p1, p2 }
                )
                .Where(joined => joined.p1.nit == nit_cliente)
                .Select(joined => new
                {
                    Codigo = joined.p1.Codigo,
                    nit = joined.p1.nit,
                    controla_descto_nivel = joined.p2.controla_descto_nivel
                })
                .FirstOrDefaultAsync();
            if (dt != null)
            {
                if (dt.controla_descto_nivel == null)
                {
                    return true;
                }
                else
                {
                    if (dt.controla_descto_nivel == true)
                    {
                        return true;
                    }
                    return false;
                }
            }
            return true;
        }
        public async Task<string> Cliente_Segun_Nit(DBContext _context, string nit)
        {
            try
            {
                string resultado = "";
                var regex = new Regex(@"^\d+$"); // Expresión regular para verificar si la cadena contiene solo dígitos
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
            catch (Exception)
            {
                return "";
            }

        }
        public async Task<string> CodigosIguales(DBContext _context, string codcliente)
        {
            try
            {
                string resultado = "";
                var regex = new Regex(@"^\d+$"); // Expresión regular para verificar si la cadena contiene solo dígitos

                string maincode = await CodigoPrincipal(_context, codcliente);
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
            catch (Exception)
            {
                return "";
            }

        }

        public async Task<string> TipoSegunClientesIguales(DBContext _context, string codcliente)
        {
            string codigo_principal = await CodigoPrincipal(_context, codcliente);
            string nit1 = await NIT(_context, codigo_principal);
            string nit2 = await NIT(_context, codcliente);
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


        public async Task<string> CodigoPrincipal(DBContext _context, string codcliente)
        {
            try
            {
                string resultado = "";
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
            catch (Exception)
            {
                return "";
            }

        }

        public async Task<bool> ExisteCliente(DBContext _context, string codcliente)
        {
            try
            {
                var result = await _context.vecliente
                        .Where(codigo => codigo.codigo == codcliente.Trim())
                       .CountAsync();
                if (result > 0)
                {
                    return true;
                }
                return false;
            }
            catch (Exception)
            {
                return false;
            }

        }

        public async Task<string> NIT(DBContext _context, string codcliente)
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
        public async Task<string> Tipo_Cliente(DBContext _context, string codcliente, bool opcional = false)
        {
            string resultado = "";
            try
            {
                //using (_context)
                ////using (var context = DbContextFactory.Create(userConnectionString))
                //{
                //var tipo = null;
                if (!opcional)
                {
                    var tipo_cli = await _context.vecliente
                    .Where(c => c.codigo == codcliente)
                    .Select(c => c.tipo)
                    .FirstOrDefaultAsync();

                    if (tipo_cli != null)
                    {
                        resultado = tipo_cli;
                    }
                }
                else
                {
                    var tipo_cli = await _context.vecliente
                    .Where(c => c.codigo == codcliente)
                    .Select(c => c.tipo)
                    .FirstOrDefaultAsync();

                    if (tipo_cli != null)
                    {
                        resultado = tipo_cli;
                    }
                }
                //}
            }
            catch (Exception)
            {
                resultado = "";
            }
            return resultado;
        }
        public async Task<bool> ActualizarParametrosDePrincipal(DBContext _context, string codcliente)
        {

            string codigo_principal = await CodigoPrincipal(_context, codcliente);
            if (codigo_principal.Trim() == "")
            {
                return false;
            }
            if (codigo_principal.Trim() == codcliente.Trim())
            {
                return true;
            }
            bool existCli = await ExisteCliente(_context, codcliente);
            if (!existCli)
            {
                return false;
            }
            using (var dbContexTransaction = _context.Database.BeginTransaction())
            {
                try
                {
                    // actualizar precios permitidos
                    var veclienteprecio = await _context.veclienteprecio
                        .Where(c => c.codcliente == codcliente).ToListAsync();
                    if (veclienteprecio.Count() > 0)
                    {
                        _context.veclienteprecio.RemoveRange(veclienteprecio);
                        await _context.SaveChangesAsync();
                    }
                    var veclienteprecioAdd = await _context.veclienteprecio
                        .Where(c => c.codcliente == codigo_principal)
                        .Select(c => new veclienteprecio
                        {
                            codcliente = codcliente,
                            codtarifa = c.codtarifa
                        }).ToListAsync();
                    _context.veclienteprecio.AddRange(veclienteprecioAdd);
                    await _context.SaveChangesAsync();

                    // actualizar descuentos permitidos
                    var vecliente_desextra = await _context.vecliente_desextra
                        .Where(c => c.codcliente == codcliente).ToListAsync();
                    if (vecliente_desextra.Count() > 0)
                    {
                        _context.vecliente_desextra.RemoveRange(vecliente_desextra);
                        await _context.SaveChangesAsync();
                    }
                    var vecliente_desextraAdd = await _context.vecliente_desextra
                        .Where(c => c.codcliente == codigo_principal)
                        .Select(c => new vecliente_desextra
                        {
                            codcliente = codcliente,
                            coddesextra = c.coddesextra,
                            dias = c.dias
                        }).ToListAsync();
                    _context.vecliente_desextra.AddRange(vecliente_desextraAdd);
                    await _context.SaveChangesAsync();

                    // actualizar descuentos de nivel
                    // añadido en fecha: 22-09-2022
                    // primero asignar el descto del cliente pivote
                    var vedescliente = await _context.vedescliente
                        .Where(c => c.cliente == codigo_principal).ToListAsync();
                    if (vedescliente.Count() > 0)
                    {
                        _context.vedescliente.RemoveRange(vedescliente);
                        await _context.SaveChangesAsync();
                    }
                    // ahhora insertar en el codigo del principal los desctos del cliente pivote DESNIV
                    var vedesclienteAdd = await _context.vedescliente
                        .Where(c => c.cliente == "DESNIV")
                        .Select(c => new vedescliente
                        {
                            cliente = codigo_principal,
                            coditem = c.coditem,
                            nivel = c.nivel,
                            estado = c.estado,
                            nivel_anterior = c.nivel_anterior,
                            nivel_actual_copia = c.nivel_actual_copia
                        }).ToListAsync();
                    _context.vedescliente.AddRange(vedesclienteAdd);
                    await _context.SaveChangesAsync();



                    var vedescliente2 = await _context.vedescliente
                        .Where(c => c.cliente == codcliente).ToListAsync();
                    if (vedescliente2.Count() > 0)
                    {
                        _context.vedescliente.RemoveRange(vedescliente2);
                        await _context.SaveChangesAsync();
                    }
                    var vedesclienteAdd2 = vedesclienteAdd
                        .Select(c => new vedescliente
                        {
                            cliente = codcliente,
                            coditem = c.coditem,
                            nivel = c.nivel,
                            estado = c.estado,
                            nivel_anterior = c.nivel_anterior,
                            nivel_actual_copia = c.nivel_actual_copia
                        }).ToList();
                    _context.vedescliente.AddRange(vedesclienteAdd2);
                    await _context.SaveChangesAsync();

                    dbContexTransaction.Commit();
                    return true;
                }
                catch (Exception)
                {
                    dbContexTransaction.Rollback();
                    return false;
                    throw;
                }
            }
        }
        public async Task<string> NivelDescclienteLinea(DBContext _context, string codcliente, string codlinea, int codtarifa)
        {
            //verifica si el cliente esta en la tabla de los clasificados como competencia segun su nit
            try
            {
                string resultado = "";
                if (string.IsNullOrWhiteSpace(codcliente))
                {
                    resultado = "Z";
                }
                else
                {
                    //using (_context)
                    //{
                    var tabla = await _context.intarifa.Where(t => t.codigo == codtarifa).Select(t => t.desitem).FirstOrDefaultAsync();

                    if (tabla != null && tabla)
                    {
                        var qry = from p1 in _context.vedescliente
                                  join p2 in _context.initem on p1.coditem equals p2.codigo
                                  where p2.codlinea == codlinea && p1.cliente == codcliente
                                  orderby p1.nivel
                                  select new { p1.coditem, p1.nivel };

                        var result = await qry.FirstOrDefaultAsync();

                        resultado = result != null ? result.nivel : "Z";
                    }
                    else
                    {
                        resultado = "Z";
                    }
                    //}
                }

                return resultado;
            }
            catch (Exception)
            {
                return "Z";
            }

        }

        public async Task<string> NiveldescClientesugeridoSegunAuditoria(DBContext _context, string codcliente, string codlinea, int codtarifa)
        {
            //verifica si el cliente esta en la tabla de los clasificados como competencia segun su nit
            try
            {
                string resultado = "";
                if (string.IsNullOrWhiteSpace(codcliente))
                {
                    resultado = "Z";
                }
                else
                {
                    //using (_context)
                    //{
                    var tabla = await _context.intarifa.Where(t => t.codigo == codtarifa).Select(t => t.desitem).FirstOrDefaultAsync();

                    if (tabla != null && tabla)
                    {
                        var qry = from p1 in _context.adaudit_compraslinea
                                  join p2 in _context.ingrupo on p1.codgrupo equals p2.codigo
                                  join p3 in _context.inlinea on p2.codigo equals p3.codgrupo
                                  where p3.codigo == codlinea && p1.codcliente == codcliente
                                  orderby p1.codcliente
                                  select new { p1.codcliente, p1.Cnivelpos };

                        var result = await qry.FirstOrDefaultAsync();

                        resultado = result != null ? result.Cnivelpos : "Z";
                    }
                    else
                    {
                        resultado = "Z";
                    }
                    //}
                }

                return resultado;
            }
            catch (Exception)
            {
                return "Z";
            }

        }
        public async Task<string> niveldesccliente_segun_solicitud(DBContext _context, string idsolicitud, int nroidsolicitud, string codcliente, string coditem, int codtarifa)
        {
            if (codcliente.Trim() == "")
            {
                return "X";
            }
            var tabla = await _context.intarifa.Where(i=> i.codigo==codtarifa).FirstOrDefaultAsync();
            if (tabla!= null)
            {
                if (tabla.desitem)
                {
                    var resultado = await _context.vesoldsctos
                    .Join(_context.vesoldsctos1, p1 => p1.codigo, p2 => p2.codsoldsctos, (p1, p2) => new { p1, p2 })
                    .Join(_context.inlinea, p => p.p2.codgrupo, p3 => p3.codgrupo, (p, p3) => new { p.p1, p.p2, p3 })
                    .Join(_context.initem, p => p.p3.codigo, p4 => p4.codlinea, (p, p4) => new { p.p1, p.p2, p.p3, p4 })
                    .Join(_context.vedesitem, p => p.p4.codigo, p5 => p5.coditem, (p, p5) => new { p.p1, p.p2, p.p3, p.p4, p5 })
                    .Where(result => result.p5.nivel == result.p2.nivsol &&
                                    result.p1.id == idsolicitud &&
                                    result.p1.numeroid == nroidsolicitud &&
                                    result.p4.codigo == coditem &&
                                    result.p1.codcliente == codcliente)
                    .Select(result => new
                    {
                        id = result.p1.id,
                        numeroid = result.p1.numeroid,
                        codcliente = result.p1.codcliente,
                        codgrupo = result.p2.codgrupo,
                        coditem = result.p4.codigo,
                        nivsol = result.p2.nivsol,
                        descuento = result.p5.descuento
                    })
                    .FirstOrDefaultAsync();

                    if (resultado != null)
                    {
                        return resultado.nivsol;
                    }
                    //return "X";
                }
                //return "X";
            }
            return "X";
        }

        public async Task<double> porcen_desccliente_segun_solicitud(DBContext _context, string idsolicitud, int nroidsolicitud, string codcliente, string coditem, int codtarifa)
        {
            if (codcliente.Trim() == "")
            {
                return 0;
            }
            var tabla = await _context.intarifa.Where(i => i.codigo == codtarifa).FirstOrDefaultAsync();
            if (tabla != null)
            {
                if (tabla.desitem)
                {
                    var resultado = await _context.vesoldsctos
                    .Join(_context.vesoldsctos1, p1 => p1.codigo, p2 => p2.codsoldsctos, (p1, p2) => new { p1, p2 })
                    .Join(_context.inlinea, p => p.p2.codgrupo, p3 => p3.codgrupo, (p, p3) => new { p.p1, p.p2, p3 })
                    .Join(_context.initem, p => p.p3.codigo, p4 => p4.codlinea, (p, p4) => new { p.p1, p.p2, p.p3, p4 })
                    .Join(_context.vedesitem, p => p.p4.codigo, p5 => p5.coditem, (p, p5) => new { p.p1, p.p2, p.p3, p.p4, p5 })
                    .Where(result => result.p5.nivel == result.p2.nivsol &&
                                    result.p1.id == idsolicitud &&
                                    result.p1.numeroid == nroidsolicitud &&
                                    result.p4.codigo == coditem &&
                                    result.p1.codcliente == codcliente)
                    .Select(result => new
                    {
                        id = result.p1.id,
                        numeroid = result.p1.numeroid,
                        codcliente = result.p1.codcliente,
                        codgrupo = result.p2.codgrupo,
                        coditem = result.p4.codigo,
                        nivsol = result.p2.nivsol,
                        descuento = result.p5.descuento
                    })
                    .FirstOrDefaultAsync();

                    if (resultado != null)
                    {
                        return (double)resultado.descuento;
                    }
                    //return 0;
                }
                //return 0;
            }
            return 0;
        }

        public async Task<string> CodigosIgualesMismoNIT(DBContext _context, string codcliente)
        {
            try
            {
                string resultado = "";
                string nit = "";
                nit = await NIT(_context, codcliente);

                var regex = new Regex(@"^\d+$"); // Expresión regular para verificar si la cadena contiene solo dígitos
                string maincode = await CodigoPrincipal(_context, codcliente);
                //using (_context)
                ////using (var _context = DbContextFactory.Create(userConnectionString))
                //{
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
                    if (nit == await NIT(_context, filteredResult[0]))
                    //if (filteredResult.Any())
                    {
                        //resultado = "'" + string.Join("','", filteredResult) + "'";
                        resultado = "" + string.Join(",", filteredResult) + "";
                    }
                }
                return resultado;
                // }
            }
            catch (Exception)
            {
                return "";
            }

        }
        public async Task<List<string>> CodigosIgualesListAsync(DBContext _context, string codcliente)
        {
            List<string> resultado = new List<string>();
            try
            {

                var regex = new Regex(@"^\d+$"); // Expresión regular para verificar si la cadena contiene solo dígitos
                string codcliente_principal = await CodigoPrincipal(_context, codcliente);
                //using (_context)
                //using (var _context = DbContextFactory.Create(userConnectionString))
                //{
                var clientesIguales = await _context.veclientesiguales
                .Where(cliente => cliente.codcliente_a == codcliente_principal)
                .ToListAsync(); // Cargar datos en memoria

                var filteredResult = clientesIguales
                    .Where(cliente => regex.IsMatch(cliente.codcliente_b))
                    .OrderBy(cliente => cliente.codcliente_b)
                    .Select(cliente => cliente.codcliente_b)
                    .ToList();
                resultado = filteredResult;
                if (resultado.Count == 0)
                {
                    resultado.Add(codcliente);
                }
                //}
                return resultado;
            }
            catch (Exception)
            {
                resultado.Add(codcliente);
                return resultado;
            }

        }
        public async Task<(double resp,string message)> Cliente_Saldo_Pendiente_Nacional(DBContext _context, string cliente_principal_local, string codmoneda)
        {
            string _codigo_Principal = await CodigoPrincipal(_context, cliente_principal_local);
            decimal saldo_x_pagar_nal = 0;
            if (await Cliente_Tiene_Sucursal_Nacional(_context,cliente_principal_local))
            {
                //si el cliente es parte de una agrupacion cial a nivel nacional entre agencias de Pertec a nivel nacional
                string casa_matriz_Nacional = await CodigoPrincipal_Nacional(_context, cliente_principal_local);
                var dt_sucursales = await _context.veclientesiguales_nacion.Where(i => i.codcliente_a == casa_matriz_Nacional)
                    .Select(i => new
                    {
                        i.codcliente_a,
                        i.codcliente_b,
                        i.codalmacen_a,
                        i.codalmacen_b,
                        saldo = 0
                    }).ToListAsync();
                foreach (var reg in dt_sucursales)
                {
                    decimal saldo_x_pagar_en_ag = 0;
                    if (cliente_principal_local == reg.codcliente_b)
                    {
                        //no se actualiza porque ya se actualizo lineas mas arriba
                    }
                    else
                    {
                        //buscar los datos de conexion de la sucursal
                        var dt_conexion = await _context.ad_conexion_vpn
                            .Where(c => c.agencia.StartsWith("credito") && c.codalmacen == reg.codalmacen_b)
                            .FirstOrDefaultAsync();
                        if (dt_conexion == null)
                        {
                            return (-1, "No se encontro la configuracion de conexion para la sucursal!!!");
                        }
                        else
                        {
                            //OBTENER CADENA DE CONEXION
                            var newCadConexVPN = seguridad.Getad_conexion_vpnFromDatabase(dt_conexion.contrasena_sql,dt_conexion.servidor_sql, dt_conexion.usuario_sql,dt_conexion.bd_sql);
                            //alistar la cadena de conexion para conectar a la ag
                            using (var _contextVPN = DbContextFactory.Create(newCadConexVPN))
                            {
                                _codigo_Principal = await CodigoPrincipal(_contextVPN, reg.codcliente_b);
                                string cliente_principal = "";
                                string _CodigosIguales = "";
                                // Solo considerarlo el principal si Tiene el mismo NIT, caso contrario usar solo el codigo individual de cliente
                                if (await NIT(_contextVPN,_codigo_Principal) == await NIT(_contextVPN,reg.codcliente_b))
                                {
                                    cliente_principal = _codigo_Principal;
                                    _CodigosIguales = await CodigosIgualesMismoNIT(_contextVPN, reg.codcliente_b);   //<------solo los de mismo NIT
                                }
                                else
                                {
                                    cliente_principal = reg.codcliente_b;
                                    _CodigosIguales = "'" + reg.codcliente_b + "'";
                                }

                                //obtener el saldo pendiente de pago de todo el grupo cial
                                saldo_x_pagar_en_ag = await _contextVPN.coplancuotas
                                    .Join(
                                        _contextVPN.veremision,
                                        p1 => p1.coddocumento,
                                        p2 => p2.codigo,
                                        (p1, p2) => new { p1, p2 }
                                    )
                                    .Where(joined => joined.p1.moneda == codmoneda &&
                                           _CodigosIguales.Contains(joined.p1.cliente)&&
                                           joined.p2.anulada == false)
                                    .Select(joined => (joined.p1.monto - joined.p1.montopagado) ?? 0)
                                    .SumAsync();
                                //acumulativo saldo nacional
                                saldo_x_pagar_nal += saldo_x_pagar_en_ag;
                            }
                        }
                    }
                }
            }

            // QUITAR
            return ((double)saldo_x_pagar_nal, "");


        }




        public async Task<(double resp, string message)> Cliente_Proformas_Aprobadas_Nacional(DBContext _context, string cliente_principal_local, string codmoneda)
        {
            string _codigo_Principal = await CodigoPrincipal(_context, cliente_principal_local);
            decimal ttl_prof_aprobadas_nal = 0;
            DateTime fecha = DateTime.Now;
            if (await Cliente_Tiene_Sucursal_Nacional(_context, cliente_principal_local))
            {
                //si el cliente es parte de una agrupacion cial a nivel nacional entre agencias de Pertec a nivel nacional
                string casa_matriz_Nacional = await CodigoPrincipal_Nacional(_context, cliente_principal_local);
                var dt_sucursales = await _context.veclientesiguales_nacion.Where(i => i.codcliente_a == casa_matriz_Nacional)
                    .Select(i => new
                    {
                        i.codcliente_a,
                        i.codcliente_b,
                        i.codalmacen_a,
                        i.codalmacen_b,
                        saldo = 0
                    }).ToListAsync();
                foreach (var reg in dt_sucursales)
                {
                    decimal ttl_prof_aprobadas_en_ag = 0;
                    if (cliente_principal_local == reg.codcliente_b)
                    {
                        //no se actualiza porque ya se actualizo lineas mas arriba
                    }
                    else
                    {
                        //buscar los datos de conexion de la sucursal
                        var dt_conexion = await _context.ad_conexion_vpn
                            .Where(c => c.agencia.StartsWith("credito") && c.codalmacen == reg.codalmacen_b)
                            .FirstOrDefaultAsync();
                        if (dt_conexion == null)
                        {
                            return (-1, "No se encontro la configuracion de conexion para la sucursal!!!");
                        }
                        else
                        {
                            //OBTENER CADENA DE CONEXION
                            var newCadConexVPN = seguridad.Getad_conexion_vpnFromDatabase(dt_conexion.contrasena_sql, dt_conexion.servidor_sql, dt_conexion.usuario_sql, dt_conexion.bd_sql);
                            //alistar la cadena de conexion para conectar a la ag
                            using (var _contextVPN = DbContextFactory.Create(newCadConexVPN))
                            {
                                _codigo_Principal = await CodigoPrincipal(_contextVPN, reg.codcliente_b);
                                string cliente_principal = "";
                                string _CodigosIguales = "";
                                // Solo considerarlo el principal si Tiene el mismo NIT, caso contrario usar solo el codigo individual de cliente
                                if (await NIT(_contextVPN, _codigo_Principal) == await NIT(_contextVPN, reg.codcliente_b))
                                {
                                    cliente_principal = _codigo_Principal;
                                    _CodigosIguales = await CodigosIgualesMismoNIT(_contextVPN, reg.codcliente_b);   //<------solo los de mismo NIT
                                }
                                else
                                {
                                    cliente_principal = reg.codcliente_b;
                                    _CodigosIguales = "'" + reg.codcliente_b + "'";
                                }

                                //obtener el saldo pendiente de pago de todo el grupo cial
                                var dt = await _contextVPN.veproforma
                                    .Where(p1 => _CodigosIguales.Contains(p1.codcliente) &&
                                                 p1.anulada == false && p1.aprobada == true && p1.transferida == false &&
                                                 p1.tipopago == 1 && p1.codmoneda == codmoneda)
                                    .Select(p1 => new {
                                        p1.codcliente,
                                        p1.total,
                                        p1.codmoneda,
                                        p1.aprobada,
                                        p1.transferida
                                    })
                                    .ToListAsync();
                                //proformas aprobadas
                                foreach (var item in dt)
                                {
                                    if (item.codmoneda == codmoneda)
                                    {
                                        ttl_prof_aprobadas_en_ag += item.total;
                                    }
                                    else
                                    {
                                        decimal monto_convertido = await tipocambio._conversion(_context,codmoneda,item.codmoneda,fecha, item.total);
                                        ttl_prof_aprobadas_en_ag += Math.Round(monto_convertido, 2);
                                    }
                                }

                                //acumulativo proformas aprobadas nal
                                ttl_prof_aprobadas_nal += ttl_prof_aprobadas_en_ag;
                            }
                        }
                    }
                }
            }

            // QUITAR
            return ((double)ttl_prof_aprobadas_nal, "");


        }



        public async Task<string> CodigoPrincipal_Nacional(DBContext _context, string codcliente)
        {
            var resultado = await _context.veclientesiguales_nacion.Where(i => i.codcliente_b == codcliente).Select(i => i.codcliente_a).FirstOrDefaultAsync();
            if (resultado == null)
            {
                return codcliente;
            }
            if (resultado == "")
            {
                return codcliente;
            }
            return resultado;
        }

        public async Task<bool> Cliente_Tiene_Sucursal_Nacional(DBContext _context, string codcliente)
        {
            var resultado = await _context.veclientesiguales_nacion.Where(i => i.codcliente_b == codcliente).Select(i => i.codcliente_a).FirstOrDefaultAsync();
            if (resultado != null)
            {
                return true;
            }
            return false;
        }
        public async Task<bool> DiscriminaIVA(DBContext _context, string codcliente)
        {
            var resultado = await _context.vecliente.Where(i => i.codigo == codcliente).Select(i=>i.discrimina_iva).FirstOrDefaultAsync() ?? false;
            return resultado;
        }
        public async Task<bool> Es_Cliente_Casual(DBContext _context, string codcliente)
        {
            var resultado = await _context.vecliente.Where(i => i.codigo == codcliente).Select(i => i.casual).FirstOrDefaultAsync() ?? false;
            return resultado;
        }

        public async Task<string> monedacliente(DBContext _context, string codcliente, string usuario, string codempresa)
        {
            var resultado = await _context.vecliente.Where(i => i.codigo == codcliente).Select(i => i.moneda).FirstOrDefaultAsync() ?? "";
            if (resultado == "")
            {
                resultado = await tipocambio.monedatdc(_context, usuario,codempresa);
            }
            return resultado;
        }
        public async Task<bool> Controla_Monto_Minimo(DBContext _context, string codcliente)
        {
            bool resultado = false;
            try
            {
                //using (_context)
                ////using (var _context = DbContextFactory.Create(userConnectionString))
                //{
                var cliente = await _context.vecliente
                                .Where(v => v.codigo == codcliente)
                                .Select(v => new { v.codigo, v.controla_monto_minimo })
                                .FirstOrDefaultAsync();
                if (cliente != null)
                {
                    if (cliente.controla_monto_minimo.HasValue)
                    {
                        resultado = cliente.controla_monto_minimo.Value;
                    }
                    else
                    {
                        resultado = true;
                    }
                }
                else
                {
                    resultado = true;
                }
                //}
            }
            catch (Exception)
            {
                return true;
            }
            return resultado;
        }
        public async Task<bool> Controla_empaque_minimo(DBContext _context, string codcliente)
        {
            bool resultado = false;
            try
            {
                //using (_context)
                ////using (var _context = DbContextFactory.Create(userConnectionString))
                //{
                var cliente = await _context.vecliente
                                .Where(v => v.codigo == codcliente)
                                .Select(v => new { v.codigo, v.controla_empaque_minimo })
                                .FirstOrDefaultAsync();
                if (cliente != null)
                {
                    if (cliente.controla_empaque_minimo.HasValue)
                    {
                        resultado = cliente.controla_empaque_minimo.Value;
                    }
                    else
                    {
                        resultado = true;
                    }
                }
                else
                {
                    resultado = true;
                }
                //}
            }
            catch (Exception)
            {
                return true;
            }
            return resultado;
        }
        public async Task<bool> Controla_empaque_cerrado(DBContext _context, string codcliente)
        {
            try
            {
                var resultado = await _context.vecliente.Where(i => i.codigo == codcliente).Select(i => i.controla_empaque_cerrado).FirstOrDefaultAsync() ?? true;
                return resultado;
            }
            catch (Exception)
            {
                return true;
            }
            
        }
        public async Task<bool> Permite_Descuento_caja_cerrada(DBContext _context, string codcliente)
        {
            try
            {
                var cliente = await _context.vecliente
                                        .Where(v => v.codigo == codcliente)
                                        .Select(v => v.permite_desc_caja_cerrada ?? false)
                                        .FirstOrDefaultAsync();

                return cliente;
            }
            catch (Exception)
            {
                return false;
            }
        }
        public async Task<bool> Permite_items_repetidos(DBContext _context, string codcliente)
        {
            bool resultado = false;
            try
            {
                //using (_context)
                ////using (var _context = DbContextFactory.Create(userConnectionString))
                //{
                var cliente = await _context.vecliente
                                .Where(v => v.codigo == codcliente)
                                .Select(v => new { v.codigo, v.permite_items_repetidos })
                                .FirstOrDefaultAsync();
                if (cliente != null)
                {
                    if (cliente.permite_items_repetidos.HasValue)
                    {
                        resultado = cliente.permite_items_repetidos.Value;
                    }
                    else
                    {
                        resultado = true;
                    }
                }
                else
                {
                    resultado = false;
                }
                //}
            }
            catch (Exception)
            {
                return true;
            }
            return resultado;
        }
        public async Task<bool> ClienteCompetenciaPermiteDesctoLinea(DBContext _context, string nit_cliente)
        {
            //verifica si el cliente esta en la tabla de los clasificados como competencia segun su nit
            try
            {
                bool resultado = false;
                //using (_context)
                ////using (var _context = DbContextFactory.Create(userConnectionString))
                //{
                var permite_descto_linea = await _context.cpcompetencia
                 .Join(_context.vecompetencia_control,
                 p1 => p1.codgrupo_control,
                 p2 => p2.codigo,
                 (p1, p2) => new { p1, p2 })
                 .Where(joined => joined.p1.nit == nit_cliente)
                 .Select(joined => joined.p2.permite_descto_linea)
                 .FirstOrDefaultAsync();
                if (permite_descto_linea != null)
                {
                    resultado = (bool)permite_descto_linea;
                }
                else { resultado = false; }

                //}
                return resultado;
            }
            catch (Exception)
            {
                return false;
            }

        }
        public async Task<bool> ClienteCompetenciaPermiteDesctoProveedor(DBContext _context, string nit_cliente)
        {
            //verifica si el cliente esta en la tabla de los clasificados como competencia segun su nit
            try
            {
                bool resultado = false;
                //using (_context)
                ////using (var _context = DbContextFactory.Create(userConnectionString))
                //{
                var permite_descto_proveedor = await _context.cpcompetencia
                 .Join(_context.vecompetencia_control,
                 p1 => p1.codgrupo_control,
                 p2 => p2.codigo,
                 (p1, p2) => new { p1, p2 })
                 .Where(joined => joined.p1.nit == nit_cliente)
                 .Select(joined => joined.p2.permite_descto_proveedor)
                 .FirstOrDefaultAsync();
                if (permite_descto_proveedor != null)
                {
                    resultado = (bool)permite_descto_proveedor;
                }
                else { resultado = false; }

                //}
                return resultado;
            }
            catch (Exception)
            {
                return false;
            }

        }
        public async Task<bool> ClienteCompetenciaPermiteDesctoVolumen(DBContext _context, string nit_cliente)
        {
            //verifica si el cliente esta en la tabla de los clasificados como competencia segun su nit
            try
            {
                bool resultado = false;
                //using (_context)
                ////using (var _context = DbContextFactory.Create(userConnectionString))
                //{
                var permite_descto_volumen = await _context.cpcompetencia
                 .Join(_context.vecompetencia_control,
                 p1 => p1.codgrupo_control,
                 p2 => p2.codigo,
                 (p1, p2) => new { p1, p2 })
                 .Where(joined => joined.p1.nit == nit_cliente)
                 .Select(joined => joined.p2.permite_descto_volumen)
                 .FirstOrDefaultAsync();
                if (permite_descto_volumen != null)
                {
                    resultado = (bool)permite_descto_volumen;
                }
                else { resultado = false; }

                //}
                return resultado;
            }
            catch (Exception)
            {
                return false;
            }

        }
        public async Task<bool> ClienteCompetenciaPermiteDesctoPromocion(DBContext _context, string nit_cliente)
        {
            //verifica si el cliente esta en la tabla de los clasificados como competencia segun su nit
            try
            {
                bool resultado = false;
                //using (_context)
                //// using (var _context = DbContextFactory.Create(userConnectionString))
                //{
                var permite_descto_promocion = await _context.cpcompetencia
                 .Join(_context.vecompetencia_control,
                 p1 => p1.codgrupo_control,
                 p2 => p2.codigo,
                 (p1, p2) => new { p1, p2 })
                 .Where(joined => joined.p1.nit == nit_cliente)
                 .Select(joined => joined.p2.permite_descto_promocion)
                 .FirstOrDefaultAsync();
                if (permite_descto_promocion != null)
                {
                    resultado = (bool)permite_descto_promocion;
                }
                else { resultado = false; }

                // }
                return resultado;
            }
            catch (Exception)
            {
                return false;
            }

        }
        public async Task<bool> ClienteCompetenciaPermiteDesctoExtra(DBContext _context, string nit_cliente)
        {
            //verifica si el cliente esta en la tabla de los clasificados como competencia segun su nit
            try
            {
                bool resultado = false;
                //using (_context)
                ////using (var _context = DbContextFactory.Create(userConnectionString))
                //{
                var permite_descto_extra = await _context.cpcompetencia
                 .Join(_context.vecompetencia_control,
                 p1 => p1.codgrupo_control,
                 p2 => p2.codigo,
                 (p1, p2) => new { p1, p2 })
                 .Where(joined => joined.p1.nit == nit_cliente)
                 .Select(joined => joined.p2.permite_descto_extra)
                 .FirstOrDefaultAsync();
                if (permite_descto_extra != null)
                {
                    resultado = (bool)permite_descto_extra;
                }
                else { resultado = false; }

                // }
                return resultado;
            }
            catch (Exception)
            {
                return false;
            }

        }
        public async Task<int> Almacen_Casa_Matriz_Nacional(DBContext _context, string codcliente)
        {
            var resultado = await _context.veclientesiguales_nacion.Where(i => i.codcliente_a == codcliente).Select(i => i.codalmacen_a).FirstOrDefaultAsync() ?? 0;
            return resultado;
        }
        public async Task<int> almacen_de_cliente(DBContext _context, string codcliente)
        {
            var resultado = await _context.vevendedor
                .Join(_context.vecliente,
                v => v.codigo,
                c => c.codvendedor,
                (v,c) => new {v,c})
                .Where(i => i.c.codigo == codcliente)
                .Select(i => i.v.almacen)
                .FirstOrDefaultAsync();
            return resultado;
        }
        public async Task<string> UltimoEnvioPor(DBContext _context, string codcliente)
        {
            string resultado = await _context.vedespacho
                .Where(i => i.codcliente == codcliente
                && i.fdespachado != null)
                .OrderByDescending(i => i.fdespachado)
                .Select(i => i.tipotrans + " - " + i.nombtrans)
                .FirstOrDefaultAsync() ?? "";
            return resultado;
        }
        public async Task<bool> EsClienteSinNombre(DBContext _context, string codcliente)
        {
            var codcliente_sn = await _context.vecliente_sinnombre
                .Where(i => i.codcliente == codcliente)
                .CountAsync();
            if (codcliente_sn > 0)
            {
                return true;
            }
            return false;
        }
        public async Task<bool> EsClienteCasual(DBContext _context, string codcliente)
        {
            bool resultado = false;

            try
            {
                //using (_context)
                ////using (var _context = DbContextFactory.Create(userConnectionString))
                //{
                var cliente = await _context.vecliente
                                .Where(v => v.codigo == codcliente)
                                .Select(v => new { v.codigo, v.casual })
                                .FirstOrDefaultAsync();

                if (cliente != null)
                {
                    if (cliente.casual.HasValue)
                    {
                        resultado = cliente.casual.Value;
                    }
                    else
                    {
                        resultado = false;
                    }
                }
                else
                {
                    resultado = true;
                }
                //}
            }
            catch (Exception)
            {
                return false;
            }
            return resultado;
        }
        public async Task<bool> EsClienteFinal(DBContext _context, string codcliente)
        {
            bool resultado = false;

            try
            {
                //using (_context)
                ////using (var _context = DbContextFactory.Create(userConnectionString))
                //{
                var cliente = await _context.vecliente
                                .Where(v => v.codigo == codcliente)
                                .Select(v => new { v.codigo, v.es_cliente_final })
                                .FirstOrDefaultAsync();

                if (cliente != null)
                {
                    if (cliente.es_cliente_final.HasValue)
                    {
                        resultado = cliente.es_cliente_final.Value;
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
                //}
            }
            catch (Exception)
            {
                return false;
            }
            return resultado;
        }
        public async Task<string> Razonsocial(DBContext _context, string codcliente)
        {
            string resultado = await _context.vecliente
                .Where(i => i.codigo == codcliente)
                .Select(i => i.razonsocial)
                .FirstOrDefaultAsync() ?? "";
            return resultado;
        }
        public async Task<string> direccioncliente(DBContext _context, string codcliente)
        {
            string resultado = await _context.vetienda
                .Where(i => i.codcliente == codcliente && i.central == true)
                .Select(i => i.direccion)
                .FirstOrDefaultAsync() ?? "---";
            return resultado;
        }
        public async Task<int> PuntoDeVentaCliente(DBContext _context, string codcliente)
        {
            int resultado = 0;
            try
            {
                var ptovta = await _context.vetienda.Where(i => i.codcliente == codcliente && i.central == true).Select(i => i.codptoventa).FirstOrDefaultAsync();
                resultado = ptovta;
                return resultado;
            }
            catch (Exception)
            {
                return 0;
            }
        }
        public async Task<DateTime> FechaApertura(DBContext _context, string codcliente)
        {
            DateTime resultado = DateTime.Now;
            try
            {
                var result = await _context.vecliente
                    .Where(v => v.codigo == codcliente)
                    .Select(v => v.fapertura)
                    .FirstOrDefaultAsync();

                if (result.HasValue)
                {
                    resultado = result.Value;
                }
            }
            catch (Exception ex)
            {
                resultado = DateTime.Now;
            }
            return resultado;
        }
        public async Task<string> PuntoDeVentaCliente_Segun_Direccion(DBContext _context, string codcliente, string direccion)
        {
            //modo 0: ambos 1: provinvcia 2 = pto
            if (codcliente.Trim() == "")
            {
                return "NSE";
            }
            try
            {
                var codptoventa = await _context.vetienda.Where(i => i.direccion == direccion && i.codcliente == codcliente)
                .Select(i => i.codptoventa).FirstOrDefaultAsync();

                var resultado = await _context.veptoventa
                    .Where(i => i.codigo == codptoventa)
                    .Select(i => new
                    {
                        i.descripcion,
                        i.codprovincia
                    })
                    .FirstOrDefaultAsync();
                if (resultado == null)
                {
                    return "";
                }
                return resultado.descripcion + " - " + resultado.codprovincia;
            }
            catch (Exception)
            {
                return "NSE";
            }
            
        }

        public async Task<string> TelefonoPrincipal(DBContext _context, string codcliente)
        {
            string resultado = await _context.vetienda
                .Where(i => i.codcliente == codcliente && i.central == true)
                .Select(i => i.telefono)
                .FirstOrDefaultAsync() ?? "---";
            return resultado;
        }
        public async Task<string> CelularPrincipal(DBContext _context, string codcliente)
        {
            string resultado = await _context.vetienda
                .Where(i => i.codcliente == codcliente && i.central == true)
                .Select(i => i.celular)
                .FirstOrDefaultAsync() ?? "---";
            return resultado;
        }
        public async Task<string> UbicacionCliente(DBContext _context, string codcliente)
        {
            string resultado = await _context.vetienda
                .Where(t => t.central == true && t.codcliente == codcliente)
                .Join(_context.veptoventa,
                      t => t.codptoventa,
                      v => v.codigo,
                      (t, v) => new { t, v })
                .Join(_context.adprovincia,
                      tv => tv.v.codprovincia,
                      p => p.codigo,
                      (tv, p) => new { tv, p })
                .Join(_context.addepto,
                      tvp => tvp.p.coddepto,
                      d => d.codigo,
                      (tvp, d) => d.codigo + " " + tvp.p.codigo + " " + tvp.tv.v.descripcion)
                .FirstOrDefaultAsync() ?? "---";
            return resultado;
        }
        public async Task<(string latitud, string longitud)> latitud_longitud_cliente(DBContext _context, string codcliente)
        {
            var resultado = await _context.vetienda
                .Where(i => i.codcliente == codcliente && i.central == true)
                .Select(i => new
                {
                    i.latitud,
                    i.longitud
                })
                .FirstOrDefaultAsync();
            if (resultado == null)
            {
                return ("0", "0");
            }
            return (resultado.latitud, resultado.longitud);
        }

        public async Task<bool> EsClienteNuevo(DBContext _context, string codcliente)
        {
            try
            {
                var situacion = await _context.vecliente
                .Where(i => i.codigo == codcliente)
                .Select(i => i.situacion)
                .FirstOrDefaultAsync();
                if (situacion == null)  // en caso de no encontrar configuracion que se tome como cliente nuevo
                {
                    return true;
                }
                if (situacion != "HABITUAL")
                {
                    return true;
                }
                return false;
            }
            catch (Exception)
            {
                // en caso de error que se tome como cliente nuevo
                return true;
            }
        }

        public async Task<int> Codigo_PuntoDeVentaCliente_Segun_Direccion(DBContext _context, string codcliente, string direccion)
        {
            if (codcliente.Trim() == "")
            {
                return 0;
            }
            var result = await _context.vetienda
                .Where(i => i.direccion == direccion && i.codcliente == codcliente)
                .FirstOrDefaultAsync();
            if (result == null)
            {
                return 0;
            }
            return result.codptoventa;
        }

        public async Task<string> Ubicacion_PtoVenta(DBContext _context, int codptovta)
        {
            var result = await _context.veptoventa
                .Where(i => i.codigo == codptovta)
                .FirstOrDefaultAsync();
            if (result == null)
            {
                return "NSE";
            }
            return result.ubicacion;
        }

        public async Task<bool> EsClientePertec(DBContext _context, string codcliente)
        {
            bool resultado = false;

            try
            {
                //using (_context)
                ////using (var _context = DbContextFactory.Create(userConnectionString))
                //{
                var cliente = await _context.vecliente
                                .Where(v => v.codigo == codcliente)
                                .Select(v => new { v.codigo, v.cliente_pertec })
                                .FirstOrDefaultAsync();

                if (cliente != null)
                {
                    if (cliente.cliente_pertec.HasValue)
                    {
                        resultado = cliente.cliente_pertec.Value;
                    }
                    else
                    {
                        resultado = false;
                    }
                }
                else
                {
                    resultado = true;
                }
                //}
            }
            catch (Exception)
            {
                return false;
            }
            return resultado;
        }

        public async Task<bool> Cliente_Tiene_Descto_Extra_Asignado(DBContext _context, int coddesextra, string codcliente)
        {
            try
            {
                int situacion = await _context.vecliente_desextra
                .Where(i => i.codcliente == codcliente && i.coddesextra == coddesextra)
                .CountAsync();
                if (situacion > 0)
                {
                    return true;
                }
                return false;
            }
            catch (Exception)
            {
                return false;
            }
            
        }

        public async Task<int> NroDeTiendas(DBContext _context, string codcliente)
        {
            int resultado = 0;
            try
            {
                resultado = await _context.vetienda.CountAsync(v => v.codcliente == codcliente);
            }
            catch (Exception ex)
            {
                resultado = 0;
            }
            return resultado;
        }

        public async Task<string> Maximo_Vta_Moneda(DBContext _context, string codcliente, string usuario, string codempresa)
        {
            var resultado = await _context.vecliente.Where(i => i.codigo == codcliente).Select(i => i.codmoneda_maximo_vta).FirstOrDefaultAsync() ?? "";
            if (resultado == "")
            {
                resultado = await tipocambio.monedatdc(_context, usuario, codempresa);
            }
            return resultado;
        }

        public async Task<bool> clientehabilitado(DBContext _context, string codcliente)
        {
            var resultado = await _context.vecliente.Where(i => i.codigo == codcliente).Select(i => i.habilitado).FirstOrDefaultAsync() ?? false;
            return resultado;
        }
        /*
        public async Task<int> SemanaVentasUrgentes(DBContext _context, string codcliente, DateTime fecha)
        {
            var resultado = await _context.vecliente.Where(i => i.codigo == codcliente).Select(i => i.habilitado).FirstOrDefaultAsync() ?? false;
            return resultado;
        }
        */
        public async Task<int> DiaVentasUrgentes(DBContext _context, string codcliente, DateTime fecha)
        {//esta funcion devuelve el numero de pedidos proformas urgentes que un cliente realizo en una fecha determinada
            int resultado = 0;
            try
            {
                resultado = await _context.veproforma
                    .Where(pf => pf.anulada == false && pf.codcliente == codcliente && pf.preparacion == "URGENTE" && pf.fecha == fecha.Date && pf.aprobada == true)
                    .CountAsync();
            }
            catch (Exception ex)
            {
                resultado = 0;
            }
            return resultado;
        }

        public async Task<int> SemanaVentasUrgentes(DBContext _context, string codcliente, DateTime fecha)
        {//esta funcion devuelve el numero de pedidos proformas urgentes que un cliente realizo en una fecha determinada
            int resultado = 0;
            DateTime desde = funciones.PrincipioDeSemana(fecha);
            DateTime hasta = funciones.FinDeSemana(fecha);
            try
            {
                resultado = await _context.veproforma
                    .Where(pf => pf.anulada == false && pf.codcliente == codcliente && pf.preparacion == "URGENTE" && pf.fecha >= desde.Date && pf.fecha <= hasta.Date && pf.aprobada == true)
                    .CountAsync();
            }
            catch (Exception ex)
            {
                resultado = 0;
            }
            return resultado;
        }

        public async Task<bool> direccion_es_valida(DBContext _context, string codcliente, string direccion)
        {
            bool resultado = true;

            try
            {
                var query = await _context.vetienda
                    .Join(_context.veptoventa, t => t.codptoventa, p => p.codigo, (t, p) => new { t, p })
                    .Join(_context.adprovincia, tp => tp.p.codprovincia, v => v.codigo, (tp, v) => new { tp.t, tp.p, v })
                    .Join(_context.addepto, tpv => tpv.v.coddepto, d => d.codigo, (tpv, d) => new { tpv.t, tpv.p, tpv.v, d })
                    .Where(tpvd => tpvd.t.codcliente == codcliente)
                    .Select(tpvdd => tpvdd.t.direccion + " (" + tpvdd.p.descripcion + " - " + tpvdd.v.codigo + ")")
                    .Where(d => d.Contains(direccion.Trim()))
                    .ToListAsync(); // Ejecutamos la consulta y cargamos los resultados en memoria

                resultado = query.Count > 0; // Contamos los resultados en memoria y devolvemos true si hay al menos uno
            }
            catch (Exception)
            {
                return false;
            }
            return resultado;
        }

        public async Task<bool> direccion_con_pto_vta_es_valida(DBContext _context, string codcliente, string direccion)
        {
            bool resultado = false;

            try
            {
                var query = _context.vetienda
                    .Where(p1 => p1.codcliente == codcliente)
                    .Join(_context.veptoventa, p1 => p1.codptoventa, p2 => p2.codigo, (p1, p2) => new
                    {
                        Direccion = p1.direccion,
                        Descripcion = p2.descripcion,
                        CodProvincia = p2.codprovincia
                    })
                .Select(p => $"{p.Direccion} ({p.Descripcion} - {p.CodProvincia})");

                var resultados = await query.ToListAsync();

                resultado = resultados.Any(r => r == direccion);
            }
            catch (Exception)
            {
                return false;
            }
            return resultado;
        }

        public async Task<bool> ClienteControlaMaximo(DBContext _context, string codcliente)
        {
            bool resultado = true;

            try
            {
                var cliente = await _context.vecliente
                                .Where(v => v.codigo == codcliente)
                                .Select(v => new { v.codigo, v.controla_maximo })
                                .FirstOrDefaultAsync();

                if (cliente != null)
                {
                    if (cliente.controla_maximo.HasValue)
                    {
                        resultado = cliente.controla_maximo.Value;
                    }
                    else
                    {
                        resultado = true;
                    }
                }
                else
                {
                    resultado = true;
                }
            }
            catch (Exception)
            {
                return true;
            }
            return resultado;
        }

        public async Task<double> CantidadVendida(DBContext _context, string coditem, string codcliente, DateTime desde, DateTime hasta)
        {
            double resultado = 0;
            double cant_item = 0;
            double cant_partes = 0;
            double cant_conj = 0;

            try
            {
                //Cantidad vendida con código de ese item
                var total = await _context.vefactura
                .Where(c => !c.anulada && c.codcliente == codcliente && c.fecha >= desde.Date && c.fecha <= hasta.Date)
                .Join(_context.vefactura1,
                      c => c.codigo,
                      d => d.codfactura,
                      (c, d) => new { c, d })
                .Where(x => x.d.coditem == coditem)
                .Select(x => x.d.cantidad)
                .SumAsync();

                cant_item = (double)total;

                //Cantidad mínima vendida de sus partes
                if (await items.itemesconjunto(_context, coditem))
                {
                    var dt_partes = await _context.inkit
                        .Where(i => i.codigo == coditem)
                        .Select(i => new { i.item, i.cantidad })
                        .ToListAsync();

                    if (dt_partes.Count > 0)
                    {
                        double min = double.MaxValue;
                        foreach (var partes in dt_partes)
                        {
                            double venta = await CantidadVendida_Item_Partes(_context, partes.item, codcliente, desde, hasta) / (double)partes.cantidad;
                            if (venta < min)
                            {
                                min = venta;
                            }
                        }
                        cant_partes = min;
                    }
                    else
                    {
                        cant_partes = 0;
                    }
                }
                else
                {
                    cant_partes = 0;
                }

                //Cantidad vendida como parte de un conjunto
                var dt_conjuntos = await _context.inkit
                         .Where(i => i.item == coditem)
                         .Select(i => new { i.codigo, i.cantidad })
                         .ToListAsync();
                if (dt_conjuntos.Count > 0)
                {
                    double venta_total = 0;
                    foreach (var cjto in dt_conjuntos)
                    {
                        double venta = await CantidadVendida_Item(_context, cjto.codigo, codcliente, desde, hasta) * (double)cjto.cantidad;
                        venta_total += venta;
                    }
                    cant_conj = venta_total;
                }
                else
                {
                    cant_conj = 0;
                }

                resultado = cant_item + cant_partes + cant_conj;
            }
            catch (Exception ex)
            {
                resultado = 0;
            }
            return resultado;
        }

        public async Task<double> CantidadVendida_Item_Partes(DBContext _context, string coditem, string codcliente, DateTime desde, DateTime hasta)
        {
            double resultado = 0;
            double cant_item = 0;
            double cant_partes = 0;
            try
            {
                //Cantidad vendida con código de ese item
                var total = await _context.vefactura
                .Where(c => !c.anulada && c.codcliente == codcliente && c.fecha >= desde.Date && c.fecha <= hasta.Date)
                .Join(_context.vefactura1,
                      c => c.codigo,
                      d => d.codfactura,
                      (c, d) => new { c, d })
                .Where(x => x.d.coditem == coditem)
                .Select(x => x.d.cantidad)
                .SumAsync();

                cant_item = (double)total;

                //Cantidad mínima vendida de sus partes
                if (await items.itemesconjunto(_context, coditem))
                {
                    var dt_partes = await _context.inkit
                        .Where(i => i.codigo == coditem)
                        .Select(i => new { i.item, i.cantidad })
                        .ToListAsync();

                    if (dt_partes.Count > 0)
                    {
                        double min = double.MaxValue;
                        foreach (var partes in dt_partes)
                        {
                            double venta = await CantidadVendida_Item_Partes(_context, partes.item, codcliente, desde, hasta) / (double)partes.cantidad;
                            if (venta < min)
                            {
                                min = venta;
                            }
                        }
                        cant_partes = min;
                    }
                    else
                    {
                        cant_partes = 0;
                    }
                }
                else
                {
                    cant_partes = 0;
                }
                resultado = cant_item + cant_partes;
            }
            catch (Exception ex)
            {
                resultado = 0;
            }
            return resultado;
        }

        public async Task<double> CantidadVendida_Item(DBContext _context, string coditem, string codcliente, DateTime desde, DateTime hasta)
        {
            double resultado = 0;
            double cant_item = 0;
            try
            {
                //Cantidad vendida con código de ese item
                var total = await _context.vefactura
                .Where(c => !c.anulada && c.codcliente == codcliente && c.fecha >= desde.Date && c.fecha <= hasta.Date)
                .Join(_context.vefactura1,
                      c => c.codigo,
                      d => d.codfactura,
                      (c, d) => new { c, d })
                .Where(x => x.d.coditem == coditem)
                .Select(x => x.d.cantidad)
                .SumAsync();

                cant_item = (double)total;
                resultado = cant_item;
            }
            catch (Exception ex)
            {
                resultado = 0;
            }
            return resultado;
        }

        public async Task<bool> EsContraEntrega(DBContext _context, string codcliente)
        {
            try
            {
                var cliente = await _context.vecliente
                    .Where(v => v.codigo == codcliente)
                    .Select(v => v.contra_entrega)
                    .FirstOrDefaultAsync();

                return cliente.HasValue ? cliente.Value : false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        //devuelve nuevo codigo para cliente
        public async Task<string> Ultimo_Codigo_Numerico(DBContext _context)
        {
            int inicial = await configuracion.getemp_numeracion_clientes_desde(_context);
            int final = await configuracion.getemp_numeracion_clientes_hasta(_context);


            var data = await _context.vecliente
                    .ToListAsync();

            var result = data
                .Where(c => IsNumeric(c.codigo) && !c.codigo.Contains("E"))
                .Where(c => int.Parse(c.codigo) >= inicial && int.Parse(c.codigo) <= final)
                .OrderByDescending(c => c.codigo)
                .Select(c => c.codigo)
                .FirstOrDefault();
            if (result == null)
            {
                result = "0";
            }
            return result;
        }

        public async Task<(bool value,string msg)> Actualizar_Credito_Sucursales_Nacional(DBContext _context, string cliente_principal_local, string codmoneda, double Credito_Actual, double Saldo_Local, double Saldo_Demas_Agencias)
        {
            string _codigo_Principal = await CodigoPrincipal(_context, cliente_principal_local);
            if (await Cliente_Tiene_Sucursal_Nacional(_context, cliente_principal_local))
            {
                // si el cliente es parte de una agrupacion cial a nivel nacional entre agencias de Pertec a nivel nacional
                string casa_matriz_Nacional = await CodigoPrincipal_Nacional(_context, cliente_principal_local);
                var dt_sucursales = await _context.veclientesiguales_nacion
                    .Where(i => i.codcliente_a == casa_matriz_Nacional && i.codcliente_a != i.codcliente_b)
                    .Select(i => new
                    {
                        i.codcliente_a,
                        i.codcliente_b,
                        i.codalmacen_a,
                        i.codalmacen_b,
                        saldo = 0.0
                    }).ToListAsync();
                foreach (var reg in dt_sucursales)
                {
                    if (await almacen_de_cliente(_context, cliente_principal_local) == reg.codalmacen_b)
                    {
                        // si es el el almacen local no es necesario actualizar
                    }
                    else
                    {
                        // buscar los datos de conexion de la sucursal
                        var dt_conexion = await _context.ad_conexion_vpn
                            .Where(c => c.agencia.StartsWith("credito") && c.codalmacen == reg.codalmacen_b)
                            .FirstOrDefaultAsync();
                        if (dt_conexion == null)
                        {
                            return (false, "No se encontro la configuracion de conexion para la sucursal!!!");
                        }
                        // alistar la cadena de conexion para conectar a la ag
                        string cadConection = seguridad.Getad_conexion_vpnFromDatabase(dt_conexion.contrasena_sql, dt_conexion.servidor_sql, dt_conexion.usuario_sql, dt_conexion.bd_sql);
                        using (var _contextVPN = DbContextFactory.Create(cadConection))
                        {
                            _codigo_Principal = await CodigoPrincipal(_contextVPN, reg.codcliente_b);
                            // Solo considerarlo el principal si Tiene el mismo NIT, caso contrario usar solo el codigo individual de cliente
                            string cliente_principal = "";
                            string _CodigosIguales = "";
                            if (await NIT(_contextVPN,_codigo_Principal) == await NIT(_contextVPN,reg.codcliente_b))
                            {
                                cliente_principal = _codigo_Principal;
                                _CodigosIguales = await CodigosIgualesMismoNIT(_contextVPN,reg.codcliente_b); // <------solo los de mismo NIT
                            }
                            else
                            {
                                cliente_principal = reg.codcliente_b;
                                _CodigosIguales = "'" + reg.codcliente_b + "'";
                            }

                            // obtener el saldo pendiente de pago de todo el grupo cial
                            double credito_dispo_ags = (Credito_Actual - (Saldo_Local + Saldo_Demas_Agencias));
                            try
                            {
                                var clientes = _context.vecliente
                                    .Where(c => _CodigosIguales.Contains(c.codigo))
                                    .ToList();

                                foreach (var cliente in clientes)
                                {
                                    cliente.credito = (decimal?)Credito_Actual;
                                    cliente.creditodisp = (decimal?)credito_dispo_ags;
                                }

                                _context.SaveChanges();
                            }
                            catch (Exception)
                            {
                                return (false, "Ocurrio un error al actualizar el saldo de sucursales en otras agencias!!!");
                            }

                        }


                    }
                }
            }
            return (true, "");
        }


        public async Task<List<string>> CodigosIgualesMismoNIT_List(DBContext _context, string codcliente)
        {
            List<string> resultado = new List<string>();
            try
            {
                string nit = await NIT(_context, codcliente);


                var regex = new Regex(@"^\d+$"); // Expresión regular para verificar si la cadena contiene solo dígitos
                string codcliente_principal = await CodigoPrincipal(_context, codcliente);
                var clientesIguales = _context.veclientesiguales
                .Where(cliente => cliente.codcliente_b.All(char.IsDigit)
                                  && cliente.codcliente_a == codcliente_principal)
                .Select(cliente => new
                {
                    cliente.codcliente_a,
                    cliente.codcliente_b
                });

                if (clientesIguales.Count() <= 0)
                {
                    resultado.Add(codcliente);
                }
                else
                {
                    foreach (var item in clientesIguales)
                    {
                        if (NIT(_context, item.codcliente_b).ToString() == nit)
                        {
                            resultado.Add(item.codcliente_b);
                        }
                    }
                }

            }
            catch (Exception)
            {
                resultado.Add(codcliente);
            }
            if (resultado.Count() <= 0)
            {
                resultado.Add(codcliente);
            }
            return resultado;
        }


        public async Task<int> Vendedor_de_cliente(DBContext _context, string codcliente)
        {
            var resultado = await _context.vecliente.Where(i => i.codigo == codcliente).FirstOrDefaultAsync();
            if (resultado != null)
            {
                return resultado.codvendedor;
            }
            return 0;
        }
        public async Task<int> almacen_de_cliente_Integer(DBContext _context, string codcliente)
        {
            var resultado = await _context.vevendedor
                .Join(_context.vecliente,
                    v => v.codigo,
                    c => c.codvendedor,
                    (v, c) => new { Vendedor = v, Cliente = c })
                .Where(vc => vc.Cliente.codigo == codcliente)
                .Select(vc => vc.Vendedor.almacen)
                .FirstOrDefaultAsync();
            return resultado;
        }
        private static bool IsNumeric(string input)
        {
            return int.TryParse(input, out _);
        }

        public async Task<bool> EstaEnCodigosIguales(DBContext _context, string codcliente)
        {
            try
            {
                var c = await _context.veclientesiguales.Where(i => i.codcliente_a == codcliente || i.codcliente_b == codcliente).CountAsync();
                if (c <= 0)
                {
                    return false;
                }
                return true;
            }
            catch (Exception)
            {
                return false;
                // throw;
            }
        }
        public async Task<bool> ClientesIguales_Insertar(DBContext _context, string codcliente_a, string codcliente_b, int codalmacen)
        {
            veclientesiguales data = new veclientesiguales();
            data.codcliente_a = codcliente_a;
            data.codcliente_b = codcliente_b;
            data.codalmacen = codalmacen;
            _context.veclientesiguales.Add(data);
            await _context.SaveChangesAsync();
            return true;
        }


        public async Task<decimal> cliente_debe_vencido(DBContext _context, string codigoCliente, string moneda, DateTime fecha)
        {
            decimal resultado = 0;

            try
            {
                var cuotas = await _context.coplancuotas
                    .Where(c => c.montopagado < c.monto && c.vencimiento <= fecha.Date && c.cliente == codigoCliente)
                    .GroupBy(c => new { c.cliente, c.moneda })
                    .Select(g => new
                    {
                        Cliente = g.Key.cliente,
                        Debe = g.Sum(c => c.monto - c.montopagado),
                        Moneda = g.Key.moneda
                    })
                    .ToListAsync();

                if (cuotas.Any())
                {
                    foreach (var cuota in cuotas)
                    {
                        resultado += Convert.ToDecimal(tipocambio._conversion(_context, moneda, cuota.Moneda, DateTime.Now.Date, (decimal)cuota.Debe));
                    }
                }
                else
                {
                    resultado = 0;
                }
            }
            catch (Exception)
            {
                resultado = 0;
            }

            return resultado;
        }
        public async Task<string> Cliente_Nit_facturado(DBContext _context, string nit, string nomcliente)
        {
            string resultado = "";
            try
            {
                var result = await _context.vefactura
                    .Where(v => v.nit == nit && v.nomcliente == nomcliente)
                    .Select(v => v.codcliente + " - " + v.nomcliente + " " + v.nit)
                    .FirstOrDefaultAsync();

                if (result != null)
                {
                    resultado = result;
                }
            }
            catch (Exception)
            {
                resultado = "";
            }
            return resultado;
        }
        public async Task<decimal> Maximo_Vta(DBContext _context, string codcliente)
        {
            var resultado = await _context.vecliente.Where(i => i.codigo == codcliente).Select(i => i.maximo_vta).FirstOrDefaultAsync() ?? 0;
            return resultado;
        }
        public async Task<string> TipoDeFactura(DBContext _context, string codcliente)
        {
            var tipofactura = await _context.vecliente
                    .Where(item => item.codigo == codcliente)
                    .Select(item => item.tipofactura)
                    .FirstOrDefaultAsync();

            if (tipofactura == null)
            {
                return "";
            }
            return tipofactura;
        }

        public async Task<string> Maximo_Vta_Moneda(DBContext _context, string codcliente)
        {
            var resultado = await _context.vecliente.Where(i => i.codigo == codcliente).Select(i => i.codmoneda_maximo_vta).FirstOrDefaultAsync() ?? "";
            //if (resultado == "")
            //{
            //    resultado = await tipocambio.monedatdc(_context, usuario, codempresa);
            //}
            return resultado;
        }
        public async Task<string> NombreComercial(DBContext _context, string codcliente)
        {
            try
            {
                var resultado = await _context.vecliente.Where(i => i.codigo == codcliente).Select(i => i.nombre_comercial).FirstOrDefaultAsync() ?? "";
                return resultado;
            }
            catch (Exception)
            {
                return "";
            }
        }
        public async Task<string> PuntoDeVenta_Casual(DBContext _context, int codptoventa)
        {
            try
            {
                var resultado = await _context.veptoventa.Where(i => i.codigo == codptoventa).Select(i => i.descripcion + " - " + i.codprovincia).FirstOrDefaultAsync() ?? "";
                return resultado;
            }
            catch (Exception)
            {
                return "";
            }
        }

        ///////////////////////////////////////////////////////////////////////////////
        // esta funcion devuelve los dias asignados para el desto pp de un cliente
        ///////////////////////////////////////////////////////////////////////////////
        public async Task<int> dias_pronto_pago(DBContext _context, string codcliente)
        {
            try
            {
                int resultado = await _context.vecliente_desextra
                    .Join(
                        _context.vedesextra,
                        p1 => p1.coddesextra,
                        p2 => p2.codigo,
                        (p1, p2) => new { p1, p2 }
                    )
                    .Where(joined => joined.p1.codcliente == codcliente && joined.p2.prontopago == true)
                    .OrderBy(joined => joined.p1.coddesextra)
                    .Select(joined => joined.p1.dias)
                    .FirstOrDefaultAsync() ?? 0;

                return resultado;
            }
            catch (Exception)
            {
                return 0;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////
        // Esta funcion devuelve la condicion frente al IVA
        ///////////////////////////////////////////////////////////////////////////////
        public async Task<string> CondicionFrenteAlIva(DBContext _context, string codcliente)
        {
            try
            {
                string condicion = await _context.vecliente.Where(i=> i.codigo == codcliente).Select(i=> i.condicion).FirstOrDefaultAsync() ?? "";
                return condicion;
            }
            catch (Exception)
            {
                return "";
            }
        }


        public async Task<bool> Es_Cliente_Competencia(DBContext _context, string nit_cliente)
        {
            string codcliente_seg_nit = await Cliente_Segun_Nit(_context,nit_cliente);
            string cadena_nits = "";
            // 1ro del nit saco su cod cliente
            string maincode = await CodigoPrincipal(_context,codcliente_seg_nit);
            // 2do veo todos los codigos iguales
            string samecode = await CodigosIguales(_context, maincode);

            var tbl = await _context.vecliente.Where(i => samecode.Contains(i.codigo)).Select(i => new
            {
                i.codigo,
                i.nit
            }).ToListAsync();
            foreach (var reg in tbl)
            {
                cadena_nits = cadena_nits + ", '" + reg.nit + "'";
            }

            // verifica si el cliente esta en la tabla de los clasificados como competencia segun su nit
            var result = await _context.cpcompetencia
                .Where(p1 => cadena_nits.Contains(p1.nit))
                .Join(_context.vecompetencia_control,
                      p1 => p1.codgrupo_control,
                      p2 => p2.codigo,
                      (p1, p2) => new
                      {
                          p1.Codigo,
                          p1.nit,
                          p2
                      })
                .CountAsync();
            if (result > 0)
            {
                return true;
            }
            return false;
        }
        public async Task<bool> Cliente_Competencia_Permite_Descto_Caja_Cerrada(DBContext _context, string nit_cliente)
        {
            bool resultado = false;
            var consulta = await _context.cpcompetencia
                .Join(_context.vecompetencia_control,
                p1 => p1.codgrupo_control,
                p2 => p2.codigo,
                (p1, p2) => new
                {
                    p1.Codigo,
                    p1.nit,
                    p2.permite_descto_caja_cerrada,
                }
                )
                .Where(i => i.nit == nit_cliente).FirstOrDefaultAsync();
            if (consulta != null)
            {
                if (consulta.permite_descto_caja_cerrada == null)
                {
                    resultado = true;
                }
                else
                {
                    if (consulta.permite_descto_caja_cerrada == true)
                    {
                        resultado = true;
                    }
                    else
                    {
                        resultado = false;
                    }
                }
            }
            else
            {
                resultado = true;
            }
            return resultado;
        }


        public async Task MarcarClienteHabitual(DBContext _context, string codcliente)
        {
            try
            {
                var cliente = await _context.vecliente.Where(i => i.codigo == codcliente).FirstOrDefaultAsync();
                if (cliente != null)
                {
                    cliente.situacion = "HABITUAL";
                    await _context.SaveChangesAsync();
                }
                else
                {
                    Console.WriteLine("Cliente not found.");
                }
            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task<bool> Eliminar_Promocion_Clientes_Excluidos(DBContext _context)
        {
            bool resultado = true;
            try
            {
                // Ejecutar el procedimiento almacenado usando Entity Framework
                await _context.Database.ExecuteSqlRawAsync("EXEC sp_EliminarClientesExcluidosPromocion");
            }
            catch (Exception ex)
            {
                // Manejo de errores
                Console.WriteLine("Error al ejecutar el procedimiento almacenado: " + ex.Message);
                resultado = false;
            }

            return resultado;
        }


        public async Task<bool> Cliente_Permite_Descto_Extra_Extraordinario(DBContext _context, int coddesextra, string codcliente)
        {
            try
            {
                int situacion = await _context.vedesextra_extraordinario_cliente
                .Where(i => i.codcliente == codcliente && i.coddesextra == coddesextra)
                .CountAsync();
                if (situacion > 0)
                {
                    return true;
                }
                return false;
            }
            catch (Exception)
            {
                return false;
            }

        }

        public async Task<string> RubroConDescripcion(DBContext _context, string codcliente)
        {
            try
            {
                string resultado = await _context.vecliente
                    .Join(_context.verubro,
                          c => c.codrubro,
                          r => r.codigo,
                          (c, r) => new { c, r })
                    .Where(joined => joined.c.codigo == codcliente)
                    .Select(joined => joined.c.codrubro.ToString() + " " + joined.r.descripcion)
                    .FirstOrDefaultAsync() ?? "";
                return resultado;
            }
            catch (Exception)
            {
                return "";
            }
        }
        public async Task<string> Opcion_Niveles_Descuento_Proforma(DBContext _context, int codproforma)
        {
            string resultado = "ACTUAL";
            var dt = await _context.veproforma.Where(i => i.codigo == codproforma).Select(i => i.niveles_descuento).FirstOrDefaultAsync();
            if (dt != null)
            {
                if (dt.Trim().Length == 0)
                {
                    resultado = "ACTUAL";
                }
                else
                {
                    resultado = dt;
                }
            }
            else
            {
                resultado = "ACTUAL";
            }
            return resultado;
        }

    }
}