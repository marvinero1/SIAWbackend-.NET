using siaw_DBContext.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using siaw_DBContext.Models;
using siaw_DBContext.Models_Extra;
using System.Data;
using Microsoft.Data.SqlClient;

namespace siaw_funciones
{
    public class Restricciones
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
        private Funciones funciones = new Funciones();
        private Cliente cliente = new Cliente();
        private Items items = new Items();
        private Saldos saldos = new Saldos();
        private Ventas ventas = new Ventas();
        public async Task<double> empaqueminimo(DBContext _context, string codigo, int codtarifa, int coddescuento)
        {
            // sacar el empaque de tarifa
            var canttarif = await _context.veempaque1
                .Join(_context.intarifa,
                    e => e.codempaque,
                    t => t.codempaque,
                    (e, t) => new { e, t })
                .Where(x => x.e.item == codigo && x.t.codigo == codtarifa)
                .Select(x => x.e.cantidad)
                .FirstOrDefaultAsync() ?? 0;

            // sacar el empaque del descuento
            var cantdesc = await _context.veempaque1
                .Join(_context.vedescuento,
                    e => e.codempaque,
                    d => d.codempaque,
                    (e, d) => new { e, d })
                .Where(x => x.e.item == codigo && x.d.codigo == coddescuento)
                .Select(x => x.e.cantidad)
                .FirstOrDefaultAsync() ?? 0;

            if (cantdesc > canttarif)
            {
                return (double)cantdesc;
            }
            return (double)canttarif;
        }
        public async Task<bool> cumpleempaque(DBContext _context, string codigo, int tarifa, int descuento, decimal cantidad, int almacen, string codcliente)
        {
            try
            {
                bool resultado = false;
                if (cantidad <= 0)
                {
                    resultado = true;
                }
                else
                {
                    decimal empaque, canttarif, cantdesc;
                    bool ultimo = false;

                    var cant_emp = _context.veempaque1
                   .Join(_context.intarifa,
                       e => e.codempaque,
                       t => t.codempaque,
                       (e, t) => new { E = e, T = t })
                   .Where(x => x.E.item == codigo && x.T.codigo == tarifa)
                   .Select(x => x.E.cantidad)
                   .FirstOrDefault();

                    if (cant_emp == null)
                    {
                        canttarif = 0;
                    }
                    else { canttarif = (decimal)cant_emp; }


                    DataTable tabla = new DataTable();
                    DataRow[] registro;

                    var query = _context.veempaque1
                        .Join(_context.vedescuento,
                            e => e.codempaque,
                            d => d.codempaque,
                            (e, d) => new { e, d })
                            .Where(x => x.e.item == codigo && x.d.codigo == descuento)
                            .Select(x => new { x.e.cantidad, x.d.ultimos });

                    var result = query.Distinct().ToList();
                    tabla = funciones.ToDataTable(result);
                    bool cliente_permite_desc_caja_cerrado = await cliente.Permite_Descuento_caja_cerrada(_context, codcliente);
                    if (tabla.Rows.Count < 1)
                    {
                        cantdesc = 0;
                    }
                    else
                    {
                        cantdesc = decimal.Parse((string)tabla.Rows[0]["cantidad"]);
                        ultimo = bool.Parse((string)tabla.Rows[0]["ultimos"]);
                    }

                    if (cantdesc > canttarif)
                    {
                        if (cliente_permite_desc_caja_cerrado)
                        {
                            empaque = canttarif;
                        }
                        else
                        {
                            empaque = cantdesc;
                        }

                    }
                    else
                    {
                        empaque = canttarif;
                    }

                    if (cantidad >= empaque)
                    {
                        resultado = true;
                    }
                    else
                    {
                        if (ultimo)
                        {
                            var cant_instoactual = await _context.instoactual
                                    .Where(v => v.coditem == codigo && v.codalmacen == almacen)
                                    .Select(v => v.cantidad)
                                    .FirstOrDefaultAsync();
                            if (cantidad >= (decimal)cant_instoactual)
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
                    }
                    tabla.Dispose();

                }

                return resultado;
            }
            catch (Exception)
            {
                return false;
            }

        }
        public async Task<List<Validar_Vta.Dtnocumplen>> ValidarMaximoPorcentajeDeMercaderiaVenta(DBContext _context, string codcliente_real, bool Usar_Bd_Opcional, List<itemDataMatrizMaxVta> dt, int codalmacen, decimal porcentajeMaximo, string id_pf, int nro_id_pf, string cod_empresa, string usrreg)
        {
            string cadena_items = "";
            int bandera = -1;
            List<Validar_Vta.Dtnocumplen> dtunido = new List<Validar_Vta.Dtnocumplen>();

            foreach (var detalle in dt)
            {
                if (cadena_items.Trim().Length == 0)
                {
                    cadena_items = "'" + detalle.coditem + "'";
                }
                else
                {
                    cadena_items += ",'" + detalle.coditem + "'";
                }
            }
            List<string> noCumplen = new List<string>();
            DataTable dtsaldo_restringido = new DataTable();
            // Consulta utilizando LINQ en Entity Framework
            var dtsaldo_restring = _context.initem_max_vta
                .Where(item => item.codalmacen == codalmacen && cadena_items.Contains(item.coditem))
                .OrderBy(item => item.coditem)
                .ToList();

            // Convierte la lista resultante a un DataTable
            dtsaldo_restringido = funciones.ToDataTable(dtsaldo_restring);

            foreach (var detalle in dt)
            {
                //ver si ya esta
                bandera = -1;
                foreach (var unido in dtunido)
                {
                    if (unido.codigo == detalle.coditem)
                    {
                        bandera = dtunido.IndexOf(unido);
                        break;
                    }
                }
                if (bandera < 0)
                {
                    //if (detalle.coddescuento.va)
                    //{

                    //}
                    //else
                    //{

                    //}
                    Validar_Vta.Dtnocumplen registro = new Validar_Vta.Dtnocumplen
                    {
                        codigo = detalle.coditem,
                        descripcion = await items.itemdescripcion(_context, detalle.coditem) + " (" + await items.itemmedida(_context, detalle.coditem) + ")",
                        cantidad = (decimal)detalle.cantidad,
                        cantidad_pf_anterior = (decimal)detalle.cantidad_pf_anterior,
                        cantidad_pf_total = (decimal)detalle.cantidad_pf_total,
                        porcen_venta = 0,
                        coddescuento = detalle.coddescuento,
                        codtarifa = detalle.codtarifa,
                        saldo = 0,
                        porcen_maximo = 0,
                        cantidad_permitida_seg_porcen = 0,
                        empaque_precio = 0,
                        obs = ""
                    };
                    dtunido.Add(registro);
                }
                else
                {
                    // Incrementar las cantidades existentes en dtunido
                    dtunido[bandera].cantidad = dtunido[bandera].cantidad + (decimal)detalle.cantidad;
                    dtunido[bandera].cantidad_pf_anterior = dtunido[bandera].cantidad_pf_anterior + (decimal)detalle.cantidad_pf_anterior;
                    dtunido[bandera].cantidad_pf_total = dtunido[bandera].cantidad_pf_total + (decimal)detalle.cantidad_pf_total;
                }
            }
            //obtener la lista de items que no se reservar
            List<string> lst_items_no_reserva = new List<string>();
            lst_items_no_reserva.Clear();
            lst_items_no_reserva = lista_items_no_reserva_saldo(_context);

            //obtener los codigos de desctos especiales que huberia
            List<int> lst_desctos_especiales = new List<int>();
            lst_desctos_especiales.Clear();
            lst_desctos_especiales = lista_descuentos_especiales(_context);

            //######################################################################################################
            //## verificar sobre la tabla unidos
            //#######################################
            decimal saldo;
            decimal CANTIDAD_PERMITIDA_SEG_PORCEN = 0;
            decimal empaque_precio = 0;
            List<double> lista_empaque_alternativo_precio;
            bool cantidad_es_multiplo = true;
            DataTable dtsaldo = new DataTable();

            /////////////////////////////////////////////////////////////////////////////////////////////////////////
            //revisar todo el detalle de items que no vendad mas del porcentaje del saldo permitido
            ////////////////////////////////
            DataRow[] mi_reg_maxvta = null;
            double porcen_vta_dias = 0;
            foreach (var unido in dtunido)
            {

                saldo = await saldos.SaldoItem_CrtlStock_Para_Ventas_Sam(_context, unido.codigo, codalmacen, true, id_pf, nro_id_pf, true, cod_empresa, usrreg);
                if (saldo < 0) { saldo = 0; }
                saldo = Math.Round(saldo, 2);
                unido.saldo = (decimal)saldo;
                //////////////////////////////////////
                ///////////////////// OBTENER EL PORCENTAJE DE RESTRICCION
                //verificar el porcentaje restringido de saldo
                //implementado en fecha 18-09-2020

                mi_reg_maxvta = dtsaldo_restringido.Select("coditem='" + unido.codigo + "'");
                if (mi_reg_maxvta != null && mi_reg_maxvta.Length > 0)
                {
                    porcentajeMaximo = (decimal)mi_reg_maxvta[0]["porcen_maximo"];
                }
                else
                {
                    porcentajeMaximo = 999;
                }

                unido.porcen_maximo = porcentajeMaximo;
                ///////////////////////////////////////////////////////////////////////////////////////////////

                ////////////////// OBTENER EL PORCENTAJE DE VENTA REALIZADA EN LOS DIAS PARAMETRIZABLES
                //verificar el porcentaje de venta del cliente ya vendido en el rango de dias
                //implementado en fecha 26-01-2022
                if (saldo == 0)
                {
                    unido.porcen_venta = 0;
                }
                else
                {
                    porcen_vta_dias = 0;
                    porcen_vta_dias = (double)Math.Round(unido.cantidad_pf_total * 100 / saldo, 2, MidpointRounding.AwayFromZero);
                    unido.porcen_venta = (decimal)porcen_vta_dias;
                }
                ///////////////////////////////////////////////////////////////////////////////////////////////

                ///////////////////////////////////////////////////////////////////////////////////////////////
                //     CANTIDAD DEL SALDO QUE SE PUEDE VENDER SEGUN SU PORCENTAJE

                CANTIDAD_PERMITIDA_SEG_PORCEN = Math.Round((saldo / 100) * porcentajeMaximo, 2, MidpointRounding.AwayFromZero);
                unido.cantidad_permitida_seg_porcen = CANTIDAD_PERMITIDA_SEG_PORCEN;
                //poner el empaque minimo de acuerdo al precio
                empaque_precio = await ventas.EmpaquePrecio(_context, unido.codigo, unido.codtarifa);
                unido.empaque_precio = (int)empaque_precio;
                //obtener la lista de empaque alternativos con las cantidades por empaque y precio e item
                lista_empaque_alternativo_precio = await ventas.Empaques_Alternativos_Lista(_context, unido.codtarifa, unido.codigo);
                //si la cantidad del pedido es 0 no hay nada que validar
                if (unido.cantidad_pf_total == 0 || unido.cantidad == 0)
                {
                    unido.obs = "Cumple";
                    //goto SIGUIENTE;
                }
                //si el saldo es cero, no hay saldo
                else if (Convert.ToDouble(unido.saldo) == 0)
                {
                    unido.obs = "No hay Saldo!!!";
                    //goto SIGUIENTE;
                }
                else
                {
                    cantidad_es_multiplo = unido.cantidad_pf_total % empaque_precio == 0 || unido.cantidad % empaque_precio == 0;
                    //28-01-2022 si el item se vende al 100% no controlar
                    if (unido.porcen_maximo == 100 && unido.cantidad_pf_anterior > 0 && unido.cantidad <= CANTIDAD_PERMITIDA_SEG_PORCEN)
                    {
                        unido.obs = "Cumple";
                    }
                    else if (!lst_items_no_reserva.Contains(unido.codigo))
                    {
                        //si el item esta en la lisa de items que no se reservan saldo no controla nada o no verifica nada
                        //pero si no esta en la lista de los que no se controla el saldo, es decir se controla saldo
                        //que deje vender un emapque minimo 
                        //SI LA CANTIDAD MAXIMA DE VENTA ES CERO,
                        //no se añade a la lista de items que pedira clave por vender mas del porcentae permitido
                        //añadir el empaque alternativo en fecha: 11-01-2021
                        //si la cantidad que se quiere vender es mayor a la permitida seg porcentaje pero es igual a: 1 SOLO empaque minimo SEGUN EL TIPO DE PRECIO o a su alternativo del empaque
                        //si la cantidad a venderen la proforma es mayor al x porciento del saldo permitido del item
                        //pero ademas es mayor a 1 empaque minimo entonces si pedira clave
                        //añadido el 31-03-2021
                        if (CANTIDAD_PERMITIDA_SEG_PORCEN == 0)
                        {
                            unido.obs = "Cumple";
                        }
                        else if (unido.cantidad_pf_total <= CANTIDAD_PERMITIDA_SEG_PORCEN)
                        {
                            unido.obs = "Cumple";
                        }
                        else
                        {
                            if (unido.cantidad == empaque_precio || lista_empaque_alternativo_precio.Contains((double)unido.cantidad))
                            {
                                unido.obs = "Cumple";
                            }
                            else
                            {
                                noCumplen.Add("La cantidad total pedida del item: " + unido.codigo + " sumando la cant. de la PF actual y las PF anteriores a la fecha, sobrepasa el porcentaje de:" + porcentajeMaximo + "% del saldo maximo que se puede vender.");
                                unido.obs = "No cumple sobrepasa el porcentaje del saldo maximo que se puede vender. La cantidad total pedida del item sumando la cant. de la PF actual y las PF anteriores a la fecha, sobrepasa el porcentaje del saldo maximo que se puede vender.";
                            }
                        }
                    }
                }
                SIGUIENTE:;
            }

            return dtunido;
        }
        public List<string> lista_items_no_reserva_saldo(DBContext _context)
        {
            List<string> resultado = new List<string>();

            var dt = _context.initem_noreserva_saldo.OrderBy(item => item.coditem)
                                                     .Select(item => new { coditem = item.coditem, coditem1 = item.coditem1, coditem2 = item.coditem2 })
                                                     .ToList();
            foreach (var reg in dt)
            {
                if (!resultado.Contains(reg.coditem))
                {
                    resultado.Add(reg.coditem);
                }

                if (!resultado.Contains(reg.coditem1))
                {
                    resultado.Add(reg.coditem1);
                }

                if (!resultado.Contains(reg.coditem2))
                {
                    resultado.Add(reg.coditem2);
                }
            }
            return resultado;
        }
        public List<int> lista_descuentos_especiales(DBContext _context)
        {
            List<int> resultado = new List<int>();
            var dt = _context.vedescuento.OrderBy(item => item.codigo)
                                          .Select(item => new { Codigo = item.codigo })
                                          .ToList();

            foreach (var reg in dt)
            {
                if (!resultado.Contains(reg.Codigo))
                {
                    resultado.Add(reg.Codigo);
                }
            }
            return resultado;
        }
        public async Task<bool> Validar_Contraentrega_Descuento(DBContext _context, bool ContraEntrega, int coddescuento)
        {
            bool resultado = true;
            if (ContraEntrega)
            {
                double precioFinal;

                var contra_entrega = new SqlParameter("@contra_entrega", SqlDbType.Int) { Value = 1 };
                var descuento = new SqlParameter("@coddescuento", SqlDbType.Int) { Value = coddescuento };
                var result = new SqlParameter("@resultado", SqlDbType.Int) { Direction = ParameterDirection.Output };

                await _context.Database
                    .ExecuteSqlRawAsync("EXECUTE val_contraentrega @contra_entrega, @coddescuento, @resultado OUTPUT",
                        contra_entrega,
                        descuento,
                        result);
                if ((int)result.Value > 0)
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
                resultado = true;
            }
            return resultado;
        }

        public async Task<bool> Validar_Cliente_Contraentrega(DBContext _context, bool ContraEntrega, string codcliente)
        {
            bool resultado = true;
            if (ContraEntrega)
            {
                //si el pedido es contra entreg verificar si el cliente esta habilitado contra entrega
                if (!await cliente.EsContraEntrega(_context, codcliente))
                {//si el cliente no esta habilitado para comprar CONTRA ENTREGA y el pedido es contra entrega entonces devuelve error
                    resultado = false;
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
            return resultado;
        }


    }
}
