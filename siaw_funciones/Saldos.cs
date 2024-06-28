using Microsoft.Data.SqlClient;
using siaw_DBContext.Data;
using siaw_DBContext.Models;
using siaw_DBContext.Models_Extra;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Runtime.Intrinsics.Arm;
using NuGet.Configuration;


namespace siaw_funciones
{
    public class Saldos
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

        private empaquesFunciones empaque_func = new empaquesFunciones();
        private Empresa empresa = new Empresa();
        private Items items = new Items();
        public async Task<decimal> SaldoItem_CrtlStock_Para_Ventas(string userConnectionString, string agencia, int codalmacen, string coditem, string codempresa, string usuario)
        {
            //List<sldosItemCompleto> saldos;
            decimal resultado = 0;
            //precio unitario del item
            resultado = await SaldosCompletoResult(userConnectionString, codalmacen, coditem, codempresa, usuario);
            //resultado = tabla;

            return resultado;
        }
        public async Task<List<sldosItemCompleto>> SaldoItem_Crtlstock_Lista_Para_Ventas(string userConnectionString, string agencia, int codalmacen, string coditem, string codempresa, string usuario)
        {

            List<sldosItemCompleto> resultado;
            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                //precio unitario del item
                resultado = await SaldosCompleto(userConnectionString, agencia, codalmacen, coditem, codempresa, usuario);
                //resultado = tabla;
            }
            return resultado;
        }
        public async Task<decimal> SaldoItem_CrtlStock_Para_Ventas_Sam(DBContext _context, string codigo, int codalmacen, bool ctrlSeguridad, string idproforma, int numeroidproforma, bool include_saldos_a_cubrir, string codempresa, string usuario)
        {
            List<sldosItemCompleto> saldos;
            decimal resultado = 0;
            //using (var _context = DbContextFactory.Create(userConnectionString))
            //{
            //precio unitario del item
            //saldos = await SaldosCompleto(userConnectionString, agencia, codalmacen, coditem, codempresa, usuario);
            saldos = await SaldoItem_Crtlstock_Tabla_Para_Ventas_Sam(_context, codigo, codalmacen, ctrlSeguridad, idproforma, numeroidproforma, include_saldos_a_cubrir, codempresa, usuario);
            //resultado = tabla;
            //}
            //for (int i = 0; (i <= (saldos.Count - 1)); i++)
            //{
            resultado = (decimal)saldos[7].valor;
            //}

            return resultado;
        }
        public async Task<List<sldosItemCompleto>> SaldoItem_Crtlstock_Tabla_Para_Ventas_Sam(DBContext _context, string coditem, int codalmacen, bool ctrlSeguridad, string idproforma, int numeroidproforma, bool include_saldos_a_cubrir, string codempresa, string usuario)
        {
            try
            {
                List<sldosItemCompleto> listaSaldos = new List<sldosItemCompleto>();
                sldosItemCompleto saldoItemTotal = new sldosItemCompleto();
                sldosItemCompleto var8 = new sldosItemCompleto();
                saldoItemTotal.descripcion = "Total Saldo";
                bool Es_Ag_Local = true;
                string cadena_items_conjunto = "";
                bool item_reserva_para_conjunto = false;
                int nro_items_del_conjunto = 0;
                string CODITEM_REVISAR = "";
                bool RESTRINGIR_VENTA_SUELTA = true;
                double CANTIDAD = 0;
                double CANTIDAD_PROFOR_APROBADAS = 0;
                var context_original = _context;

                //string userConnectionString = _userConnectionManager.GetUserConnection(userConn);
                //var conexion = _context;
                //var conexion = DbContextFactory.Create(userConnectionString)
                bool Es_Kit = await items.EsKit(_context, coditem);  // verifica si el item es kit o no 
                bool obtener_saldos_otras_ags_localmente = await Obtener_Saldos_Otras_Agencias_Localmente_context(_context, codempresa); // si se obtener las cantidades reservadas de las proformas o no
                bool obtener_cantidades_aprobadas_de_proformas = await Obtener_Cantidades_Aprobadas_De_Proformas(_context, codempresa); // si se obtener las cantidades reservadas de las proformas o no

                string codItemVta = "";

                if (string.IsNullOrWhiteSpace(coditem) || string.IsNullOrWhiteSpace(codalmacen.ToString().Trim()))
                {
                    goto mi_fin;
                }
                var hay_reg_instoactual = _context.instoactual
                .Where(i => i.codalmacen == codalmacen && i.coditem == coditem)
                .ToList();

                // Verificar si no se encontraron resultados
                if (hay_reg_instoactual.Count == 0)
                {
                    goto mi_fin;
                }

                if (await empresa.AlmacenLocalEmpresa_context(_context, codempresa) == codalmacen)
                {
                    Es_Ag_Local = true;
                }
                else
                {
                    Es_Ag_Local = false;
                }

                if (Es_Ag_Local || obtener_saldos_otras_ags_localmente)
                {
                    //usar el mismo context recibido
                }
                else
                {//actualizar el context del almacen recibido
                 // var conexion = DbContextFactory.Create(userConnectionString);
                    var conexion = empaque_func.Getad_conexion_vpnFromDatabase_Sam(_context, codalmacen);
                    if (conexion != null)
                    {
                        //el context sera el mismo q se recibio al inicio
                        _context = DbContextFactory.Create(conexion);
                    }

                }
                /////////////////////////////////////////////////////////////////////////////////////////////
                if (Es_Kit)
                {
                    // SI ES KIT, OBTENER EL NÚMERO DE ITEMS QUE FORMAN EL KIT
                    var conjunto = _context.inkit.Where(k => k.codigo == coditem).ToList();
                    nro_items_del_conjunto = conjunto.Count;
                    item_reserva_para_conjunto = false;

                    foreach (var item in conjunto)
                    {
                        if (cadena_items_conjunto.Trim().Length == 0)
                        {
                            cadena_items_conjunto = "'" + item.item + "'";
                        }
                        else
                        {
                            cadena_items_conjunto += ",'" + item.item + "'";
                        }
                    }
                    //SI ES VENTA EN COJUNTO DE MAS DE 2 ITEMS QUIERE DECIR QUE ES UN COJJUNTO Y NO SE RESTRINGE
                    //EJEMPLO 35CH RESTRINGIR_VENTA_SUELTA FALSE
                    //05CT RESTRINGIR_VENTA_SUELTA TRUE
                    RESTRINGIR_VENTA_SUELTA = nro_items_del_conjunto > 1 ? false : true;
                }
                else
                {
                    nro_items_del_conjunto = 0;

                    // SI NO ES KIT PERO EL ITEM SE USA PARA ARMAR CONJUNTOS
                    //var query = from ctrlstock in _context.inctrlstock
                    //            where ctrlstock.coditem == coditem
                    //            select ctrlstock.coditem;

                    //query = query.Union(
                    //            from reserva in _context.inreserva
                    //            where reserva.coditem == coditem && reserva.codalmacen == codalmacen
                    //            select reserva.coditem.ToString());

                    //var count = query.Count();

                    /////////////////////////////////////////////////////////////////
                    //var query1 = _context.inctrlstock
                    //    .Where(ctrlstock => ctrlstock.coditem == coditem)
                    //    .Select(ctrlstock => ctrlstock.coditem);

                    //var query2 = _context.inreserva
                    //    .Where(reserva => reserva.coditem == coditem && reserva.codalmacen == codalmacen)
                    //    .Select(reserva => reserva.coditem);

                    //// Convertir el tipo de datos de una de las columnas para que coincida con el otro
                    //var query2Converted = query2.Select(item => item.ToString());

                    //var concatenatedQuery = query1.Concat(query2Converted);

                    //var count = concatenatedQuery.Count();

                    var query1 = _context.inctrlstock
                        .Where(ctrlstock => ctrlstock.coditem == coditem)
                        .Select(ctrlstock => ctrlstock.coditem);

                    var query2 = _context.inreserva
                        .Where(reserva => reserva.coditem == coditem && reserva.codalmacen == codalmacen)
                        .Select(reserva => reserva.coditem);

                    var combinedResults = query1.ToList().Concat(query2.ToList());

                    var count = combinedResults.Count();

                    item_reserva_para_conjunto = count > 0 ? true : false;
                    RESTRINGIR_VENTA_SUELTA = count > 0 ? true : false;
                }

                //CASO_1: si no es KIT y el KIT ESTA COMPUESTO DE UNO O MAS ITEMS
                /////////////////////////////////////////////////////////////////////////////////////////////

                // obtiene saldos de agencia del item seleccionado
                instoactual instoactual = await getEmpaquesItemSelect_Sam(_context, coditem, codalmacen, Es_Kit);
                codItemVta = instoactual.coditem;
                if (Es_Kit)
                {
                    //saldosDetalleItem.txtReservaProf = "(-) PROFORMAS APROBADAS ITEM(" + instoactual.coditem + ") DEL CJTO(" + coditem + ")";
                    sldosItemCompleto saldosDetalleItem = new sldosItemCompleto();
                    saldosDetalleItem.descripcion = "(+) SALDO ACTUAL ITEM (" + instoactual.coditem + ") DEL CJTO(" + coditem + ")";
                    saldosDetalleItem.valor = (double)instoactual.cantidad;
                    listaSaldos.Add(saldosDetalleItem);
                }
                else
                {
                    //saldosDetalleItem.txtReservaProf = "(-) PROFORMAS APROBADAS: " + instoactual.coditem;
                    sldosItemCompleto saldosDetalleItem = new sldosItemCompleto();
                    saldosDetalleItem.descripcion = "(+)SALDO ACTUAL ITEM: " + instoactual.coditem;
                    saldosDetalleItem.valor = (double)instoactual.cantidad;
                    listaSaldos.Add(saldosDetalleItem);
                }
                saldoItemTotal.valor = (double)instoactual.cantidad;


                // obtiene reservas en proforma
                List<saldosObj> saldosReservProformas = await getReservasProf_Sam(_context, coditem, codalmacen, obtener_cantidades_aprobadas_de_proformas, Es_Kit);


                string codigoBuscado = instoactual.coditem;

                var reservaProf = saldosReservProformas.FirstOrDefault(obj => obj.coditem == codigoBuscado);

                instoactual.coditem = coditem;
                if (Es_Kit)
                {
                    sldosItemCompleto saldosDetalleItem = new sldosItemCompleto();
                    saldosDetalleItem.descripcion = "(-) PROFORMAS APROBADAS ITEM(" + instoactual.coditem + ") DEL CJTO(" + coditem + ")";
                    saldosDetalleItem.valor = (double)reservaProf.TotalP * -1;
                    listaSaldos.Add(saldosDetalleItem);
                }
                else
                {
                    sldosItemCompleto saldosDetalleItem = new sldosItemCompleto();
                    saldosDetalleItem.descripcion = "(-) PROFORMAS APROBADAS: " + instoactual.coditem;
                    saldosDetalleItem.valor = (double)reservaProf.TotalP * -1;
                    listaSaldos.Add(saldosDetalleItem);
                }

                saldoItemTotal.valor -= (double)reservaProf.TotalP;  // reduce saldo total
                var8.valor = saldoItemTotal.valor;



                // (-) RESERVA STOCK MINIMO DE TIENDAS
                bool Reserva_Stock_Max_Min = await get_if_reservaStock_item_Sam(_context, coditem);
                //bool ctrlSeguridad = await empresa.ControlarStockSeguridad2(userConnectionString, codempresa);
                if (Reserva_Stock_Max_Min && ctrlSeguridad)
                {
                    double STOCK_MINIMO = 0;
                    if (Es_Kit)
                    {
                        STOCK_MINIMO = await get_stock_Para_Tiendas_Sam(_context, codItemVta, codalmacen);
                    }
                    else
                    {
                        STOCK_MINIMO = await get_stock_Para_Tiendas_Sam(_context, codItemVta, codalmacen);
                    }
                    sldosItemCompleto saldoReserStockMaxMinTienda = new sldosItemCompleto();
                    saldoReserStockMaxMinTienda.descripcion = "(-) RESERVA STOCK MINIMO DE TIENDAS";
                    saldoReserStockMaxMinTienda.valor = STOCK_MINIMO * -1;
                    listaSaldos.Add(saldoReserStockMaxMinTienda);
                    saldoItemTotal.valor -= STOCK_MINIMO;  // reduce saldo total

                }


                // pivote variable para agregar a la lista
                sldosItemCompleto var1 = new sldosItemCompleto();

                // obtiene items si no son kit, sus reservas para armar conjuntos.
                double CANTIDAD_RESERVADA = 0;
                if (include_saldos_a_cubrir && RESTRINGIR_VENTA_SUELTA)
                {
                    CANTIDAD_RESERVADA = await getReservasCjtos_Sam(_context, coditem, codalmacen, codempresa, Es_Kit, (double)instoactual.cantidad, (double)reservaProf.TotalP);
                    var1.descripcion = "(-) SALDO RESERVADO PARA ARMAR CJTOS";
                    var1.valor = CANTIDAD_RESERVADA * -1;
                    listaSaldos.Add(var1);
                    saldoItemTotal.valor -= CANTIDAD_RESERVADA;  // reduce saldo total
                }
                else
                {
                    CANTIDAD_RESERVADA = 0;
                    var1.descripcion = "(-) SALDO RESERVADO PARA ARMAR CJTOS (no reserva en vta de cjto)";
                    var1.valor = CANTIDAD_RESERVADA * -1;
                    listaSaldos.Add(var1);
                    saldoItemTotal.valor -= CANTIDAD_RESERVADA;  // reduce saldo total
                }

                // obtiene el saldo minimo que debe mantenerse en agencia
                sldosItemCompleto var2 = new sldosItemCompleto();
                double Saldo_Minimo_Item = await empaque_func.getSaldoMinimo_Sam(_context, coditem);
                var2.descripcion = "(-) SALDO MINIMO DEL ITEM";
                var2.valor = Saldo_Minimo_Item * -1;
                listaSaldos.Add(var2);
                saldoItemTotal.valor -= Saldo_Minimo_Item;  // reduce saldo total

                // obtiene reserva NM ingreso para sol-Urgente
                bool validar_ingresos_solurgentes = await empaque_func.getValidaIngreSolurgente_Sam(_context, codempresa);

                double total_reservado = 0;
                double total_para_esta = 0;
                double total_proforma = 0;
                if (validar_ingresos_solurgentes)
                {
                    //  RESTAR LAS CANTIDADES DE INGRESO POR NOTAS DE MOVIMIENTO URGENTES
                    // de facturas que aun no estan aprobadas
                    string resp_total_reservado = await getSldIngresoReservNotaUrgent_Sam(_context, coditem, codalmacen);
                    total_reservado = double.Parse(resp_total_reservado);


                    //AUMENTAR CANTIDAD PARA ESTA PROFORMA DE INGRESO POR NOTAS DE MOVIMIENTO URGENTES
                    total_para_esta = await getSldReservNotaUrgentUnaProf_Sam(_context, coditem, codalmacen, "''", 0);


                    //AUMENTAR LA CANTIDAD DE LA PROFORMA DE ESTA NOTA QUE PUEDE ESTAR COMO RESERVADA.
                    total_proforma = await getSldReservProf_Sam(_context, coditem, codalmacen, "''", 0);

                }

                sldosItemCompleto var3 = new sldosItemCompleto();
                var3.descripcion = "(-) RESERVA NM INGRESO PARA SOL-URGENTE";
                var3.valor = total_reservado * -1;
                listaSaldos.Add(var3);
                saldoItemTotal.valor -= total_reservado;  // reduce saldo total

                sldosItemCompleto var4 = new sldosItemCompleto();
                var4.descripcion = "(+) INGRESOS SOLICITUDES URGENTE DE PROFORMA : -0";
                var4.valor = total_para_esta;
                listaSaldos.Add(var4);
                saldoItemTotal.valor -= total_para_esta;  // reduce saldo total

                sldosItemCompleto var5 = new sldosItemCompleto();
                var5.descripcion = "(+) CANTIDAD RESERVADA PROFORMA APROBADA: -0";
                var5.valor = total_proforma;
                listaSaldos.Add(var5);
                saldoItemTotal.valor -= total_proforma;  // reduce saldo total

                mi_fin:
                _context = context_original;
                if (saldoItemTotal.valor < 0)
                {
                    saldoItemTotal.valor = 0;
                }
                listaSaldos.Add(saldoItemTotal);


                return (listaSaldos);

            }
            catch (Exception)
            {
                return null;
            }
        }
        private async Task<instoactual> getEmpaquesItemSelect(string conexion, string coditem, int codalmacen, bool eskit)
        {
            instoactual instoactual = new instoactual();

            if (!eskit)  // como no es kit obtiene los datos de stock directamente
            {
                //verificar si el item tiene saldos para ese almacen
                instoactual = await empaque_func.GetSaldosActual(conexion, codalmacen, coditem);
            }
            else // como es kit se debe buscar sus piezas sin importar la cantidad de estas que tenga
            {
                List<inkit> kitItems = await empaque_func.GetItemsKit(conexion, coditem);  // se tiene la lista de piezas
                foreach (inkit kit in kitItems) // se recorre la lista de piezas para consultar sus saldos disponibles de cada una (SE DEBE BASAR EL STOCK EN BASE AL MENOR NUMERO)
                {
                    var pivot = await empaque_func.GetSaldosActual(conexion, codalmacen, kit.item);
                    var cantDisp = pivot.cantidad / kit.cantidad;
                    pivot.cantidad = cantDisp;
                    if (instoactual.cantidad == null)
                    {
                        instoactual = pivot;
                    }
                    else
                    {
                        if (instoactual.cantidad > cantDisp)
                        {
                            instoactual = pivot;
                        }
                    }
                }
                /*
                using (var _context = DbContextFactory.Create(conexion))
                {
                    var menorSaldos = await _context.inkit
                    .Join(_context.instoactual,
                        kit => kit.item,
                        insto => insto.coditem,
                        (kit, insto) => new { kit, insto })
                    .Where(joined => joined.kit.codigo == coditem && joined.insto.codalmacen == codalmacen)
                    .Select(joined => new instoactual
                    {
                        codalmacen = codalmacen,
                        coditem = joined.kit.item,
                        cantidad = joined.insto.cantidad / joined.kit.cantidad
                    })
                    .OrderBy(result => result.cantidad)
                    .FirstOrDefaultAsync();
                    return menorSaldos;
                }
                */

            }

            return instoactual;
        }
        private async Task<instoactual> getEmpaquesItemSelect_Sam(DBContext _context, string coditem, int codalmacen, bool eskit)
        {
            instoactual instoactual = new instoactual();

            if (!eskit)  // como no es kit obtiene los datos de stock directamente
            {
                //verificar si el item tiene saldos para ese almacen
                instoactual = await empaque_func.GetSaldosActual_Sam(_context, codalmacen, coditem);
            }
            else // como es kit se debe buscar sus piezas sin importar la cantidad de estas que tenga
            {
                List<inkit> kitItems = await empaque_func.GetItemsKit_Sam(_context, coditem);  // se tiene la lista de piezas
                foreach (inkit kit in kitItems) // se recorre la lista de piezas para consultar sus saldos disponibles de cada una (SE DEBE BASAR EL STOCK EN BASE AL MENOR NUMERO)
                {
                    var pivot = await empaque_func.GetSaldosActual_Sam(_context, codalmacen, kit.item);
                    var cantDisp = pivot.cantidad / kit.cantidad;
                    pivot.cantidad = cantDisp;
                    if (instoactual.cantidad == null)
                    {
                        instoactual = pivot;
                    }
                    else
                    {
                        if (instoactual.cantidad > cantDisp)
                        {
                            instoactual = pivot;
                        }
                    }
                }
                /*
                using (var _context = DbContextFactory.Create(conexion))
                {
                    var menorSaldos = await _context.inkit
                    .Join(_context.instoactual,
                        kit => kit.item,
                        insto => insto.coditem,
                        (kit, insto) => new { kit, insto })
                    .Where(joined => joined.kit.codigo == coditem && joined.insto.codalmacen == codalmacen)
                    .Select(joined => new instoactual
                    {
                        codalmacen = codalmacen,
                        coditem = joined.kit.item,
                        cantidad = joined.insto.cantidad / joined.kit.cantidad
                    })
                    .OrderBy(result => result.cantidad)
                    .FirstOrDefaultAsync();
                    return menorSaldos;
                }
                */

            }

            return instoactual;
        }
        private async Task<List<saldosObj>> getReservasProf(string conexion, string coditem, int codalmacen, bool obtener_cantidades_aprobadas_de_proformas, bool eskit)
        {
            List<saldosObj> saldosReservProformas;
            if (obtener_cantidades_aprobadas_de_proformas)
            {
                saldosReservProformas = await empaque_func.GetSaldosReservaProforma(conexion, codalmacen, coditem, eskit);
            }
            else
            {
                saldosReservProformas = await empaque_func.GetSaldosReservaProformaFromInstoactual(conexion, codalmacen, coditem, eskit);
            }
            return saldosReservProformas;
        }
        private async Task<List<saldosObj>> getReservasProf_Sam(DBContext _context, string coditem, int codalmacen, bool obtener_cantidades_aprobadas_de_proformas, bool eskit)
        {
            List<saldosObj> saldosReservProformas;
            if (obtener_cantidades_aprobadas_de_proformas)
            {
                saldosReservProformas = await empaque_func.GetSaldosReservaProforma_Sam(_context, codalmacen, coditem, eskit);
            }
            else
            {
                saldosReservProformas = await empaque_func.GetSaldosReservaProformaFromInstoactual_Sam(_context, codalmacen, coditem, eskit);
            }
            return saldosReservProformas;
        }
        private async Task<double> getReservasCjtos(string userConnectionString, string coditem, int codalmacen, string codempresa, bool eskit, double _saldoActual, double reservaProf)
        {
            List<inctrlstock> itemsinReserva = null;
            double CANTIDAD_RESERVADA = 0;

            /*if (!eskit)  // si no es kit debe verificar si el item es utilizado para armar conjuntos
            {
                
            }*/

            // pregunta si incluye saldos a cubrir y si debe restringir venta suelta
            // incluye saldos a cubrir esta por defecto en true en la funcion revisar bien
            // restringir venta suelta eso varia dependiendo de items verificar mas
            // if (include_saldos_a_cubrir && RESTRINGIR_VENTA_SUELTA){}
            // lo siguiente debe estar dentro de esto

            bool Reserva_Tuercas_En_Porcentaje = await empaque_func.reserv_tuer_porcen(userConnectionString, codempresa);

            if (Reserva_Tuercas_En_Porcentaje)
            {
                itemsinReserva = await empaque_func.ReservaItemsinKit1(userConnectionString, coditem);

                /*
                var cantidadReservadaTasks = itemsinReserva.Select(async item =>
                {
                    instoactual itemRef = await empaque_func.GetSaldosActual(userConnectionString, codalmacen, item.coditemcontrol);
                    return (double)(itemRef.cantidad * (item.porcentaje / 100));
                });

                var cantidadReservadaArray = await Task.WhenAll(cantidadReservadaTasks);
                CANTIDAD_RESERVADA = cantidadReservadaArray.Sum();
                */
                foreach (var item in itemsinReserva)
                {
                    instoactual itemRef = await empaque_func.GetSaldosActual(userConnectionString, codalmacen, item.coditemcontrol);
                    double cubrir_item = (double)(itemRef.cantidad * (item.porcentaje / 100));
                    //cubrir_item = Math.Floor(cubrir_item);
                    CANTIDAD_RESERVADA += cubrir_item;
                }
                if (CANTIDAD_RESERVADA < 0)
                {
                    CANTIDAD_RESERVADA = 0;
                }
            }
            else
            {
                List<inreserva> reserva2 = await empaque_func.ReservaItemsinKit2(userConnectionString, coditem, codalmacen);
                if (reserva2.Count > 0)
                {
                    double cubrir_item = (double)reserva2[0].cantidad;
                    CANTIDAD_RESERVADA += cubrir_item;
                }
                if (CANTIDAD_RESERVADA < 0)
                {
                    CANTIDAD_RESERVADA = 0;
                }

                double resul = 0;
                double reserva_para_cjto = 0;
                double CANTIDAD_RESERVADA_DINAMICA = 0;

                inreserva_area reserva = await empaque_func.Obtener_Cantidad_Segun_SaldoActual_PromVta_SMin_PorcenVta(userConnectionString, coditem, codalmacen);


                if (reserva != null)
                {
                    if ((double)reserva.porcenvta > 0.5)
                    {
                        reserva.porcenvta = (decimal)0.5;
                    }

                    if (_saldoActual >= (double)reserva.smin)
                    {
                        resul = (double)(reserva.porcenvta * reserva.promvta);
                        resul = (double)Math.Round(resul, 2);
                        reserva_para_cjto = _saldoActual - resul - reservaProf;
                    }
                    else
                    {
                        reserva_para_cjto = _saldoActual;
                    }


                    CANTIDAD_RESERVADA_DINAMICA = reserva_para_cjto;
                    if (CANTIDAD_RESERVADA_DINAMICA > 0)
                    {
                        CANTIDAD_RESERVADA = (double)CANTIDAD_RESERVADA_DINAMICA;
                    }
                }
            }
            return CANTIDAD_RESERVADA;
        }
        private async Task<double> getReservasCjtos_Sam(DBContext _context, string coditem, int codalmacen, string codempresa, bool eskit, double _saldoActual, double reservaProf)
        {
            List<inctrlstock> itemsinReserva = null;
            double CANTIDAD_RESERVADA = 0;

            /*if (!eskit)  // si no es kit debe verificar si el item es utilizado para armar conjuntos
            {
                
            }*/

            // pregunta si incluye saldos a cubrir y si debe restringir venta suelta
            // incluye saldos a cubrir esta por defecto en true en la funcion revisar bien
            // restringir venta suelta eso varia dependiendo de items verificar mas
            // if (include_saldos_a_cubrir && RESTRINGIR_VENTA_SUELTA){}
            // lo siguiente debe estar dentro de esto

            bool Reserva_Tuercas_En_Porcentaje = await empaque_func.reserv_tuer_porcen_Sam(_context, codempresa);

            if (Reserva_Tuercas_En_Porcentaje)
            {
                itemsinReserva = await empaque_func.ReservaItemsinKit1_Sam(_context, coditem);

                /*
                var cantidadReservadaTasks = itemsinReserva.Select(async item =>
                {
                    instoactual itemRef = await empaque_func.GetSaldosActual(userConnectionString, codalmacen, item.coditemcontrol);
                    return (double)(itemRef.cantidad * (item.porcentaje / 100));
                });

                var cantidadReservadaArray = await Task.WhenAll(cantidadReservadaTasks);
                CANTIDAD_RESERVADA = cantidadReservadaArray.Sum();
                */
                foreach (var item in itemsinReserva)
                {
                    instoactual itemRef = await empaque_func.GetSaldosActual_Sam(_context, codalmacen, item.coditemcontrol);
                    double cubrir_item = (double)(itemRef.cantidad * (item.porcentaje / 100));
                    //cubrir_item = Math.Floor(cubrir_item);
                    CANTIDAD_RESERVADA += cubrir_item;
                }
                if (CANTIDAD_RESERVADA < 0)
                {
                    CANTIDAD_RESERVADA = 0;
                }
            }
            else
            {
                List<inreserva> reserva2 = await empaque_func.ReservaItemsinKit2_Sam(_context, coditem, codalmacen);
                if (reserva2.Count > 0)
                {
                    double cubrir_item = (double)reserva2[0].cantidad;
                    CANTIDAD_RESERVADA += cubrir_item;
                }
                if (CANTIDAD_RESERVADA < 0)
                {
                    CANTIDAD_RESERVADA = 0;
                }

                double resul = 0;
                double reserva_para_cjto = 0;
                double CANTIDAD_RESERVADA_DINAMICA = 0;

                inreserva_area reserva = await empaque_func.Obtener_Cantidad_Segun_SaldoActual_PromVta_SMin_PorcenVta_Sam(_context, coditem, codalmacen);


                if (reserva != null)
                {
                    if ((double)reserva.porcenvta > 0.5)
                    {
                        reserva.porcenvta = (decimal)0.5;
                    }

                    if (_saldoActual >= (double)reserva.smin)
                    {
                        resul = (double)(reserva.porcenvta * reserva.promvta);
                        resul = (double)Math.Round(resul, 2);
                        reserva_para_cjto = _saldoActual - resul - reservaProf;
                    }
                    else
                    {
                        reserva_para_cjto = _saldoActual;
                    }


                    CANTIDAD_RESERVADA_DINAMICA = reserva_para_cjto;
                    if (CANTIDAD_RESERVADA_DINAMICA > 0)
                    {
                        CANTIDAD_RESERVADA = (double)CANTIDAD_RESERVADA_DINAMICA;
                    }
                }
            }
            return CANTIDAD_RESERVADA;
        }
        private async Task<string> getSldIngresoReservNotaUrgent(string userConnectionString, string coditem, int codalmacen)
        {
            // verifica si es almacen o tienda
            string respuestaValor = "";

            bool esAlmacen = await empaque_func.esAlmacen(userConnectionString, codalmacen);

            if (esAlmacen)
            {
                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var respuesta = new SqlParameter("@respuesta", SqlDbType.VarChar, 20)
                    {
                        Direction = ParameterDirection.Output
                    };

                    await _context.Database.ExecuteSqlRawAsync(
                        "EXEC SP001_SaldoReservadoNotasUrgentes_Almacenes @coditem, @codalmacen, @respuesta OUTPUT",
                        new SqlParameter("@coditem", SqlDbType.NVarChar) { Value = coditem },
                        new SqlParameter("@codalmacen", SqlDbType.Int) { Value = codalmacen },
                        respuesta
                    );

                    respuestaValor = respuesta.Value.ToString();
                }
            }
            else
            {
                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var respuesta = new SqlParameter("@respuesta", SqlDbType.VarChar, 20)
                    {
                        Direction = ParameterDirection.Output
                    };

                    await _context.Database.ExecuteSqlRawAsync(
                        "EXEC SP001_SaldoReservadoNotasUrgentes_Tiendas @coditem, @codalmacen, @respuesta OUTPUT",
                        new SqlParameter("@coditem", SqlDbType.NVarChar) { Value = coditem },
                        new SqlParameter("@codalmacen", SqlDbType.Int) { Value = codalmacen },
                        respuesta
                    );

                    respuestaValor = respuesta.Value.ToString();
                }
            }
            return respuestaValor;
        }
        private async Task<string> getSldIngresoReservNotaUrgent_Sam(DBContext _context, string coditem, int codalmacen)
        {
            // verifica si es almacen o tienda
            string respuestaValor = "";

            bool esAlmacen = await empaque_func.esAlmacen_Sam(_context, codalmacen);

            if (esAlmacen)
            {
                //using (var _context = DbContextFactory.Create(userConnectionString))
                //{
                var respuesta = new SqlParameter("@respuesta", SqlDbType.VarChar, 20)
                {
                    Direction = ParameterDirection.Output
                };

                await _context.Database.ExecuteSqlRawAsync(
                    "EXEC SP001_SaldoReservadoNotasUrgentes_Almacenes @coditem, @codalmacen, @respuesta OUTPUT",
                    new SqlParameter("@coditem", SqlDbType.NVarChar) { Value = coditem },
                    new SqlParameter("@codalmacen", SqlDbType.Int) { Value = codalmacen },
                    respuesta
                );

                respuestaValor = respuesta.Value.ToString();
                //}
            }
            else
            {
                //using (var _context = DbContextFactory.Create(userConnectionString))
                //{
                var respuesta = new SqlParameter("@respuesta", SqlDbType.VarChar, 20)
                {
                    Direction = ParameterDirection.Output
                };

                await _context.Database.ExecuteSqlRawAsync(
                    "EXEC SP001_SaldoReservadoNotasUrgentes_Tiendas @coditem, @codalmacen, @respuesta OUTPUT",
                    new SqlParameter("@coditem", SqlDbType.NVarChar) { Value = coditem },
                    new SqlParameter("@codalmacen", SqlDbType.Int) { Value = codalmacen },
                    respuesta
                );

                respuestaValor = respuesta.Value.ToString();
                //}
            }
            return respuestaValor;
        }
        private async Task<double> getSldReservNotaUrgentUnaProf(string userConnectionString, string coditem, int codalmacen, string idProf, int nroIdProf)
        {
            // verifica si es almacen o tienda
            double total_para_esta = 0;

            bool esAlmacen = await empaque_func.esAlmacen(userConnectionString, codalmacen);

            if (esAlmacen)
            {
                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var respuesta = new SqlParameter("@respuesta", SqlDbType.Decimal)
                    {
                        Direction = ParameterDirection.Output,
                        Precision = 18,
                        Scale = 2
                    };

                    var resultado = await _context.Database
                        .ExecuteSqlRawAsync("EXEC SP001_SaldoReservadoNotasUrgente_Una_proforma_Almacenes @coditem, @codalmacen, @id_prof, @nroid_prof, @respuesta OUTPUT",
                            new SqlParameter("@coditem", SqlDbType.NVarChar) { Value = coditem },
                            new SqlParameter("@codalmacen", SqlDbType.Int) { Value = codalmacen },
                            new SqlParameter("@id_prof", SqlDbType.NVarChar) { Value = idProf },
                            new SqlParameter("@nroid_prof", SqlDbType.Int) { Value = nroIdProf },
                            respuesta);

                    total_para_esta = Convert.ToSingle(respuesta.Value);
                }
            }
            else
            {
                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var respuesta = new SqlParameter("@respuesta", SqlDbType.Decimal)
                    {
                        Direction = ParameterDirection.Output,
                        Precision = 18,
                        Scale = 2
                    };

                    var resultado = await _context.Database
                        .ExecuteSqlRawAsync("EXEC SP001_SaldoReservadoNotasUrgente_Una_proforma_Tiendas @coditem, @codalmacen, @id_prof, @nroid_prof, @respuesta OUTPUT",
                            new SqlParameter("@coditem", SqlDbType.NVarChar) { Value = coditem },
                            new SqlParameter("@codalmacen", SqlDbType.Int) { Value = codalmacen },
                            new SqlParameter("@id_prof", SqlDbType.NVarChar) { Value = idProf },
                            new SqlParameter("@nroid_prof", SqlDbType.Int) { Value = nroIdProf },
                            respuesta);

                    total_para_esta = Convert.ToSingle(respuesta.Value);
                }
            }
            return total_para_esta;
        }
        private async Task<double> getSldReservNotaUrgentUnaProf_Sam(DBContext _context, string coditem, int codalmacen, string idProf, int nroIdProf)
        {
            // verifica si es almacen o tienda
            double total_para_esta = 0;

            bool esAlmacen = await empaque_func.esAlmacen_Sam(_context, codalmacen);

            if (esAlmacen)
            {
                //using (var _context = DbContextFactory.Create(userConnectionString))
                //{
                var respuesta = new SqlParameter("@respuesta", SqlDbType.Decimal)
                {
                    Direction = ParameterDirection.Output,
                    Precision = 18,
                    Scale = 2
                };

                var resultado = await _context.Database
                    .ExecuteSqlRawAsync("EXEC SP001_SaldoReservadoNotasUrgente_Una_proforma_Almacenes @coditem, @codalmacen, @id_prof, @nroid_prof, @respuesta OUTPUT",
                        new SqlParameter("@coditem", SqlDbType.NVarChar) { Value = coditem },
                        new SqlParameter("@codalmacen", SqlDbType.Int) { Value = codalmacen },
                        new SqlParameter("@id_prof", SqlDbType.NVarChar) { Value = idProf },
                        new SqlParameter("@nroid_prof", SqlDbType.Int) { Value = nroIdProf },
                        respuesta);

                total_para_esta = Convert.ToSingle(respuesta.Value);
                //}
            }
            else
            {
                //using (var _context = DbContextFactory.Create(userConnectionString))
                //{
                var respuesta = new SqlParameter("@respuesta", SqlDbType.Decimal)
                {
                    Direction = ParameterDirection.Output,
                    Precision = 18,
                    Scale = 2
                };

                var resultado = await _context.Database
                    .ExecuteSqlRawAsync("EXEC SP001_SaldoReservadoNotasUrgente_Una_proforma_Tiendas @coditem, @codalmacen, @id_prof, @nroid_prof, @respuesta OUTPUT",
                        new SqlParameter("@coditem", SqlDbType.NVarChar) { Value = coditem },
                        new SqlParameter("@codalmacen", SqlDbType.Int) { Value = codalmacen },
                        new SqlParameter("@id_prof", SqlDbType.NVarChar) { Value = idProf },
                        new SqlParameter("@nroid_prof", SqlDbType.Int) { Value = nroIdProf },
                        respuesta);

                total_para_esta = Convert.ToSingle(respuesta.Value);
                //}
            }
            return total_para_esta;
        }
        private async Task<double> getSldReservProf(string userConnectionString, string coditem, int codalmacen, string idProf, int nroIdProf)
        {
            // verifica si es almacen o tienda
            double respuestaValor = 0;

            bool esAlmacen = await empaque_func.esAlmacen(userConnectionString, codalmacen);

            if (esAlmacen)
            {
                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var respuesta = new SqlParameter("@respuesta", SqlDbType.Decimal)
                    {
                        Direction = ParameterDirection.Output,
                        Precision = 18,
                        Scale = 2
                    };

                    await _context.Database.ExecuteSqlRawAsync(
                        "EXEC SP001_Cantidad_Reservada_En_Proforma_Almacenes @coditem, @codalmacen, @id_prof, @nroid_prof, @respuesta OUTPUT",
                        new SqlParameter("@coditem", SqlDbType.NVarChar) { Value = coditem },
                        new SqlParameter("@codalmacen", SqlDbType.Int) { Value = codalmacen },
                        new SqlParameter("@id_prof", SqlDbType.NVarChar) { Value = idProf }, // Reemplaza idProf con el valor correcto
                        new SqlParameter("@nroid_prof", SqlDbType.Int) { Value = nroIdProf }, // Reemplaza nroIdProf con el valor correcto
                        respuesta
                    );

                    respuestaValor = Convert.ToSingle(respuesta.Value);
                }
            }
            else
            {
                using (var _context = DbContextFactory.Create(userConnectionString))
                {

                    var respuesta = new SqlParameter("@respuesta", SqlDbType.Decimal)
                    {
                        Direction = ParameterDirection.Output,
                        Precision = 18,
                        Scale = 2
                    };

                    await _context.Database.ExecuteSqlRawAsync(
                        "EXEC SP001_Cantidad_Reservada_En_Proforma_Tiendas @coditem, @codalmacen, @id_prof, @nroid_prof, @respuesta OUTPUT",
                        new SqlParameter("@coditem", SqlDbType.NVarChar) { Value = coditem },
                        new SqlParameter("@codalmacen", SqlDbType.Int) { Value = codalmacen },
                        new SqlParameter("@id_prof", SqlDbType.NVarChar) { Value = idProf }, // Reemplaza idProf con el valor correcto
                        new SqlParameter("@nroid_prof", SqlDbType.Int) { Value = nroIdProf }, // Reemplaza nroIdProf con el valor correcto
                        respuesta
                    );

                    respuestaValor = Convert.ToSingle(respuesta.Value);
                }
            }
            return respuestaValor;
        }
        private async Task<double> getSldReservProf_Sam(DBContext _context, string coditem, int codalmacen, string idProf, int nroIdProf)
        {
            // verifica si es almacen o tienda
            double respuestaValor = 0;

            bool esAlmacen = await empaque_func.esAlmacen_Sam(_context, codalmacen);

            if (esAlmacen)
            {
                //using (var _context = DbContextFactory.Create(userConnectionString))
                //{
                var respuesta = new SqlParameter("@respuesta", SqlDbType.Decimal)
                {
                    Direction = ParameterDirection.Output,
                    Precision = 18,
                    Scale = 2
                };

                await _context.Database.ExecuteSqlRawAsync(
                    "EXEC SP001_Cantidad_Reservada_En_Proforma_Almacenes @coditem, @codalmacen, @id_prof, @nroid_prof, @respuesta OUTPUT",
                    new SqlParameter("@coditem", SqlDbType.NVarChar) { Value = coditem },
                    new SqlParameter("@codalmacen", SqlDbType.Int) { Value = codalmacen },
                    new SqlParameter("@id_prof", SqlDbType.NVarChar) { Value = idProf }, // Reemplaza idProf con el valor correcto
                    new SqlParameter("@nroid_prof", SqlDbType.Int) { Value = nroIdProf }, // Reemplaza nroIdProf con el valor correcto
                    respuesta
                );

                respuestaValor = Convert.ToSingle(respuesta.Value);
                //}
            }
            else
            {
                //using (var _context = DbContextFactory.Create(userConnectionString))
                //{

                var respuesta = new SqlParameter("@respuesta", SqlDbType.Decimal)
                {
                    Direction = ParameterDirection.Output,
                    Precision = 18,
                    Scale = 2
                };

                await _context.Database.ExecuteSqlRawAsync(
                    "EXEC SP001_Cantidad_Reservada_En_Proforma_Tiendas @coditem, @codalmacen, @id_prof, @nroid_prof, @respuesta OUTPUT",
                    new SqlParameter("@coditem", SqlDbType.NVarChar) { Value = coditem },
                    new SqlParameter("@codalmacen", SqlDbType.Int) { Value = codalmacen },
                    new SqlParameter("@id_prof", SqlDbType.NVarChar) { Value = idProf }, // Reemplaza idProf con el valor correcto
                    new SqlParameter("@nroid_prof", SqlDbType.Int) { Value = nroIdProf }, // Reemplaza nroIdProf con el valor correcto
                    respuesta
                );

                respuestaValor = Convert.ToSingle(respuesta.Value);
                //}
            }
            return respuestaValor;
        }
        public async Task<List<sldosItemCompleto>> SaldosCompleto(string userConnectionString, string agencia, int codalmacen, string coditem, string codempresa, string usuario)
        {
            try
            {
                List<sldosItemCompleto> listaSaldos = new List<sldosItemCompleto>();
                sldosItemCompleto saldoItemTotal = new sldosItemCompleto();
                sldosItemCompleto var8 = new sldosItemCompleto();
                saldoItemTotal.descripcion = "Total Saldo";

               

                //string userConnectionString = _userConnectionManager.GetUserConnection(userConn);
                var conexion = userConnectionString;
                bool eskit = await empaque_func.GetEsKit(conexion, coditem);  // verifica si el item es kit o no 
                bool obtener_cantidades_aprobadas_de_proformas = await empaque_func.IfGetCantidadAprobadasProformas(userConnectionString, codempresa); // si se obtender las cantidades reservadas de las proformas o no

                string codItemVta = "";

                // Falta validacion para saber si traera datos de manera local o por vpn
                // Obtener el contexto de base de datos correspondiente al usuario
                bool usar_bd_opcional = await Obtener_Saldos_Otras_Agencias_Localmente(userConnectionString, codempresa);

                if (usar_bd_opcional)
                {
                    conexion = empaque_func.Getad_conexion_vpnFromDatabase(userConnectionString, agencia);
                    if (conexion == null)
                    {
                        conexion = userConnectionString;
                    }
                }



                // obtiene saldos de agencia del item seleccionado
                instoactual instoactual = await getEmpaquesItemSelect(conexion, coditem, codalmacen, eskit);
                codItemVta = instoactual.coditem;
                if (eskit)
                {
                    //saldosDetalleItem.txtReservaProf = "(-) PROFORMAS APROBADAS ITEM(" + instoactual.coditem + ") DEL CJTO(" + coditem + ")";
                    sldosItemCompleto saldosDetalleItem = new sldosItemCompleto();
                    saldosDetalleItem.descripcion = "(+) SALDO ACTUAL ITEM (" + instoactual.coditem + ") DEL CJTO(" + coditem + ")";
                    saldosDetalleItem.valor = (double)instoactual.cantidad;
                    listaSaldos.Add(saldosDetalleItem);
                }
                else
                {
                    //saldosDetalleItem.txtReservaProf = "(-) PROFORMAS APROBADAS: " + instoactual.coditem;
                    sldosItemCompleto saldosDetalleItem = new sldosItemCompleto();
                    saldosDetalleItem.descripcion = "(+)SALDO ACTUAL ITEM: " + instoactual.coditem;
                    saldosDetalleItem.valor = (double)instoactual.cantidad;
                    listaSaldos.Add(saldosDetalleItem);
                }
                saldoItemTotal.valor = (double)instoactual.cantidad;


                // obtiene reservas en proforma
                List<saldosObj> saldosReservProformas = await getReservasProf(conexion, coditem, codalmacen, obtener_cantidades_aprobadas_de_proformas, eskit);


                string codigoBuscado = instoactual.coditem;

                var reservaProf = saldosReservProformas.FirstOrDefault(obj => obj.coditem == codigoBuscado);

                instoactual.coditem = coditem;
                if (eskit)
                {
                    sldosItemCompleto saldosDetalleItem = new sldosItemCompleto();
                    saldosDetalleItem.descripcion = "(-) PROFORMAS APROBADAS ITEM(" + instoactual.coditem + ") DEL CJTO(" + coditem + ")";
                    saldosDetalleItem.valor = (double)reservaProf.TotalP * -1;
                    listaSaldos.Add(saldosDetalleItem);
                }
                else
                {
                    sldosItemCompleto saldosDetalleItem = new sldosItemCompleto();
                    saldosDetalleItem.descripcion = "(-) PROFORMAS APROBADAS: " + instoactual.coditem;
                    saldosDetalleItem.valor = (double)reservaProf.TotalP * -1;
                    listaSaldos.Add(saldosDetalleItem);
                }

                saldoItemTotal.valor -= (double)reservaProf.TotalP;  // reduce saldo total
                var8.valor = saldoItemTotal.valor;



                // (-) RESERVA STOCK MINIMO DE TIENDAS
                bool Reserva_Stock_Max_Min = await get_if_reservaStock_item(userConnectionString, coditem);
                bool ctrlSeguridad = await empresa.ControlarStockSeguridad(userConnectionString, codempresa);
                if (Reserva_Stock_Max_Min && ctrlSeguridad)
                {
                    double STOCK_MINIMO = 0;
                    if (eskit) 
                    {
                        STOCK_MINIMO = await get_stock_Para_Tiendas(userConnectionString, codItemVta, codalmacen);
                    }
                    else
                    {
                        STOCK_MINIMO = await get_stock_Para_Tiendas(userConnectionString, codItemVta, codalmacen);
                    }
                    sldosItemCompleto saldoReserStockMaxMinTienda = new sldosItemCompleto();
                    saldoReserStockMaxMinTienda.descripcion = "(-) RESERVA STOCK MINIMO DE TIENDAS";
                    saldoReserStockMaxMinTienda.valor = STOCK_MINIMO * -1;
                    listaSaldos.Add(saldoReserStockMaxMinTienda);
                    saldoItemTotal.valor -= STOCK_MINIMO;  // reduce saldo total

                }


                // pivote variable para agregar a la lista
                sldosItemCompleto var1 = new sldosItemCompleto();

                // obtiene items si no son kit, sus reservas para armar conjuntos.
                double CANTIDAD_RESERVADA = await getReservasCjtos(userConnectionString, coditem, codalmacen, codempresa, eskit, (double)instoactual.cantidad, (double)reservaProf.TotalP);
                var1.descripcion = "(-) SALDO RESERVADO PARA ARMAR CJTOS";
                var1.valor = CANTIDAD_RESERVADA * -1;
                listaSaldos.Add(var1);
                saldoItemTotal.valor -= CANTIDAD_RESERVADA;  // reduce saldo total

                // obtiene el saldo minimo que debe mantenerse en agencia
                sldosItemCompleto var2 = new sldosItemCompleto();
                double Saldo_Minimo_Item = await empaque_func.getSaldoMinimo(userConnectionString, coditem);
                var2.descripcion = "(-) SALDO MINIMO DEL ITEM";
                var2.valor = Saldo_Minimo_Item * -1;
                listaSaldos.Add(var2);
                saldoItemTotal.valor -= Saldo_Minimo_Item;  // reduce saldo total

                // obtiene reserva NM ingreso para sol-Urgente
                bool validar_ingresos_solurgentes = await empaque_func.getValidaIngreSolurgente(userConnectionString, codempresa);

                double total_reservado = 0;
                double total_para_esta = 0;
                double total_proforma = 0;
                if (validar_ingresos_solurgentes)
                {
                    //  RESTAR LAS CANTIDADES DE INGRESO POR NOTAS DE MOVIMIENTO URGENTES
                    // de facturas que aun no estan aprobadas
                    string resp_total_reservado = await getSldIngresoReservNotaUrgent(userConnectionString, coditem, codalmacen);
                    total_reservado = double.Parse(resp_total_reservado);


                    //AUMENTAR CANTIDAD PARA ESTA PROFORMA DE INGRESO POR NOTAS DE MOVIMIENTO URGENTES
                    total_para_esta = await getSldReservNotaUrgentUnaProf(userConnectionString, coditem, codalmacen, "''", 0);


                    //AUMENTAR LA CANTIDAD DE LA PROFORMA DE ESTA NOTA QUE PUEDE ESTAR COMO RESERVADA.
                    total_proforma = await getSldReservProf(userConnectionString, coditem, codalmacen, "''", 0);

                }

                sldosItemCompleto var3 = new sldosItemCompleto();
                var3.descripcion = "(-) RESERVA NM INGRESO PARA SOL-URGENTE";
                var3.valor = total_reservado * -1;
                listaSaldos.Add(var3);
                saldoItemTotal.valor -= total_reservado;  // reduce saldo total

                sldosItemCompleto var4 = new sldosItemCompleto();
                var4.descripcion = "(+) INGRESOS SOLICITUDES URGENTE DE PROFORMA : -0";
                var4.valor = total_para_esta;
                listaSaldos.Add(var4);
                saldoItemTotal.valor -= total_para_esta;  // reduce saldo total

                sldosItemCompleto var5 = new sldosItemCompleto();
                var5.descripcion = "(+) CANTIDAD RESERVADA PROFORMA APROBADA: -0";
                var5.valor = total_proforma;
                listaSaldos.Add(var5);
                saldoItemTotal.valor -= total_proforma;  // reduce saldo total


                listaSaldos.Add(saldoItemTotal);


                return (listaSaldos);

            }
            catch (Exception)
            {
                return null;
            }
        }


        public async Task<double> get_stock_Para_Tiendas(string userConnectionString, string coditem, int codalmacen)
        {
            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                var respuesta = new SqlParameter("@cantidad", SqlDbType.Float)
                {
                    Direction = ParameterDirection.Output,
                    Precision = 18,
                    //Scale = 2
                };

                var resultado = await _context.Database
                    .ExecuteSqlRawAsync("EXEC stock_para_tiendas @codigo, @codalmacen, @cantidad OUTPUT",
                        new SqlParameter("@codigo", SqlDbType.NVarChar) { Value = coditem },
                        new SqlParameter("@codalmacen", SqlDbType.Int) { Value = codalmacen },
                        respuesta);

                return Convert.ToSingle(respuesta.Value);
            }

        }
        public async Task<double> get_stock_Para_Tiendas_Sam(DBContext _context, string coditem, int codalmacen)
        {
            //using (var _context = DbContextFactory.Create(userConnectionString))
            //{
            var respuesta = new SqlParameter("@cantidad", SqlDbType.Float)
            {
                Direction = ParameterDirection.Output,
                Precision = 18,
                //Scale = 2
            };

            var resultado = await _context.Database
                .ExecuteSqlRawAsync("EXEC stock_para_tiendas @codigo, @codalmacen, @cantidad OUTPUT",
                    new SqlParameter("@codigo", SqlDbType.NVarChar) { Value = coditem },
                    new SqlParameter("@codalmacen", SqlDbType.Int) { Value = codalmacen },
                    respuesta);

            return Convert.ToSingle(respuesta.Value);
            //}

        }

        public async Task<bool> get_if_reservaStock_item(string userConnectionString, string coditem)
        {
            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                var porcen_maximo = await _context.initem
                    .Where(item => item.codigo == coditem)
                    .Select(item => item.reservastock)
                    .FirstOrDefaultAsync();

                if (porcen_maximo == null)
                {
                    return false;
                }

                return (bool)porcen_maximo;
            }

        }
        public async Task<bool> get_if_reservaStock_item_Sam(DBContext _context, string coditem)
        {
            //using (var _context = DbContextFactory.Create(userConnectionString))
            //{
            var porcen_maximo = await _context.initem
                .Where(item => item.codigo == coditem)
                .Select(item => item.reservastock)
                .FirstOrDefaultAsync();

            if (porcen_maximo == null)
            {
                return false;
            }

            return (bool)porcen_maximo;
            //}

        }

        public async Task<double> get_Porcentaje_Maximo_de_Venta_Respecto_Del_Saldo(DBContext _context, int codalmacen, string coditem)
        {
            var porcen_maximo = await _context.initem_max_vta
                    .Where(item => item.codalmacen == codalmacen && item.coditem == coditem)
                    .Select(item => item.porcen_maximo)
                    .FirstOrDefaultAsync();

            if (porcen_maximo == null)
            {
                return 0;
            }

            return (double)porcen_maximo;
        }

         
        public async Task<bool> get_usar_bd_opcional(string userConnectionString, string usuario)
        {
            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                var usar_bd_opcional = await _context.adusparametros
                    .Where(item => item.usuario == usuario)
                    .Select(item => item.usar_bd_opcional)
                    .FirstOrDefaultAsync();

                if (usar_bd_opcional == null)
                {
                    return false;
                }

                return (bool)usar_bd_opcional;
            }
                
        }
        public async Task<bool> Obtener_Saldos_Otras_Agencias_Localmente(string userConnectionString, string codempresa)
        {
            using (var _context = DbContextFactory.Create(userConnectionString))
            {
            var usar_bd_opcional = await _context.adparametros
                .Where(item => item.codempresa == codempresa)
                .Select(item => item.obtener_saldos_otras_ags_localmente)
                .FirstOrDefaultAsync() ?? false;

            return usar_bd_opcional;
            }

        }
        public async Task<bool> Obtener_Saldos_Otras_Agencias_Localmente_context(DBContext _context, string codempresa)
        {
            //using (var _context = DbContextFactory.Create(userConnectionString))
            //{
                var usar_bd_opcional = await _context.adparametros
                    .Where(item => item.codempresa == codempresa)
                    .Select(item => item.obtener_saldos_otras_ags_localmente)
                    .FirstOrDefaultAsync() ?? false;

                return usar_bd_opcional;
            //}

        }
        public async Task<bool> Obtener_Cantidades_Aprobadas_De_Proformas(DBContext _context, string codempresa)
        {
            try
            {
                bool resultado = false;
                //using (_context)
                ////using (var _context = DbContextFactory.Create(userConnectionString))
                //{
                var result = await _context.adparametros
                    .Where(v => v.codempresa == codempresa)
                    .Select(parametro => parametro.obtener_cantidades_aprobadas_de_proformas)
                   .FirstOrDefaultAsync();
                if (result != null)
                {
                    resultado = (bool)result;
                }
                return resultado;
                //}
            }
            catch (Exception)
            {
                return false;
            }
        }
        public async Task<object> infoitem(string userConnectionString, string coditem, bool ControlarStockSeguridad, string codempresa, string usuario)
        {
            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                // primero obtener informacion del item
                var infItem = await _context.initem.Where(i => i.codigo== coditem)
                    .Select(i => new 
                    {
                        i.descripcorta, 
                        i.medida
                    })
                    .FirstOrDefaultAsync();
                if (infItem == null)
                {
                    return (new { resp = "" });
                }
                // obtener informacion del stock actual
                var stocksItem = await _context.instoactual.Where(i => i.coditem == coditem)
                    .OrderBy(i => i.codalmacen)
                    .Select(i => new instoactual
                    {
                        codalmacen = i.codalmacen,
                        cantidad = i.cantidad,
                        udm = i.udm,
                    }).ToListAsync();

                foreach (var stock in stocksItem)
                {
                    stock.cantidad = await SaldosCompletoResult(userConnectionString, stock.codalmacen, coditem, codempresa, usuario);
                }

                return (new { 
                    resp = "",
                    itemInfo = "Item: " + coditem + " " + infItem.descripcorta + " " + infItem.medida,
                    itemStocks = stocksItem,
                });
            }
        }



        public async Task<decimal> SaldosCompletoResult(string userConnectionString, int codalmacen, string coditem, string codempresa, string usuario)
        {
            try
            {
                string conexion = userConnectionString;

                decimal saldoItemTotal = 0;
                bool eskit = await empaque_func.GetEsKit(conexion, coditem);  // verifica si el item es kit o no 
                bool obtener_cantidades_aprobadas_de_proformas = await empaque_func.IfGetCantidadAprobadasProformas(userConnectionString, codempresa); // si se obtender las cantidades reservadas de las proformas o no


                // obtiene saldos de agencia del item seleccionado
                instoactual instoactual = await getEmpaquesItemSelect(conexion, coditem, codalmacen, eskit);
                saldoItemTotal = (decimal)instoactual.cantidad;


                // obtiene reservas en proforma
                List<saldosObj> saldosReservProformas = await getReservasProf(conexion, coditem, codalmacen, obtener_cantidades_aprobadas_de_proformas, eskit);


                string codigoBuscado = instoactual.coditem;

                var reservaProf = saldosReservProformas.FirstOrDefault(obj => obj.coditem == codigoBuscado);


                saldoItemTotal -= (decimal)reservaProf.TotalP;  // reduce saldo total



                // (-) RESERVA STOCK MINIMO DE TIENDAS
                bool Reserva_Stock_Max_Min = await get_if_reservaStock_item(userConnectionString, coditem);
                bool ctrlSeguridad = await empresa.ControlarStockSeguridad(userConnectionString, codempresa);
                if (Reserva_Stock_Max_Min && ctrlSeguridad)
                {
                    double STOCK_MINIMO = 0;
                    if (eskit)
                    {
                        STOCK_MINIMO = await get_stock_Para_Tiendas(userConnectionString, coditem, codalmacen);
                    }
                    else
                    {
                        STOCK_MINIMO = await get_stock_Para_Tiendas(userConnectionString, coditem, codalmacen);
                    }
                    saldoItemTotal -= (decimal)STOCK_MINIMO;  // reduce saldo total

                }


                // obtiene items si no son kit, sus reservas para armar conjuntos.
                double CANTIDAD_RESERVADA = await getReservasCjtos(userConnectionString, coditem, codalmacen, codempresa, eskit, (double)instoactual.cantidad, (double)reservaProf.TotalP);
                saldoItemTotal -= (decimal)CANTIDAD_RESERVADA;  // reduce saldo total

                // obtiene el saldo minimo que debe mantenerse en agencia
                double Saldo_Minimo_Item = await empaque_func.getSaldoMinimo(userConnectionString, coditem);
                saldoItemTotal -= (decimal)Saldo_Minimo_Item;  // reduce saldo total

                // obtiene reserva NM ingreso para sol-Urgente
                bool validar_ingresos_solurgentes = await empaque_func.getValidaIngreSolurgente(userConnectionString, codempresa);

                double total_reservado = 0;
                double total_para_esta = 0;
                double total_proforma = 0;
                if (validar_ingresos_solurgentes)
                {
                    //  RESTAR LAS CANTIDADES DE INGRESO POR NOTAS DE MOVIMIENTO URGENTES
                    // de facturas que aun no estan aprobadas
                    string resp_total_reservado = await getSldIngresoReservNotaUrgent(userConnectionString, coditem, codalmacen);
                    total_reservado = double.Parse(resp_total_reservado);


                    //AUMENTAR CANTIDAD PARA ESTA PROFORMA DE INGRESO POR NOTAS DE MOVIMIENTO URGENTES
                    total_para_esta = await getSldReservNotaUrgentUnaProf(userConnectionString, coditem, codalmacen, "''", 0);


                    //AUMENTAR LA CANTIDAD DE LA PROFORMA DE ESTA NOTA QUE PUEDE ESTAR COMO RESERVADA.
                    total_proforma = await getSldReservProf(userConnectionString, coditem, codalmacen, "''", 0);

                }

                saldoItemTotal -= (decimal)total_reservado;  // reduce saldo total

                saldoItemTotal += (decimal)total_para_esta;  // reduce saldo total

                saldoItemTotal += (decimal)total_proforma;  // reduce saldo total




                return (saldoItemTotal);

            }
            catch (Exception)
            {
                return 0;
            }
        }
        public async Task<List<Dtnegativos>> ValidarNegativosDocVenta(DBContext _context, List<itemDataMatriz> tabladetalle, int codalmacen, string idproforma, int numeroidproforma, List<string> mensajes, List<string> negativos, string cod_empresa, string usrreg)
        {
            //1RA parte DESARMADO DE CONJUNTOS
            bool resultado = true;
            bool controlarStockSeguridad = false;
            List<Dtnegativos> dtunido = new List<Dtnegativos>();
            List<Validar_Vta.Dt_desglosado> dt_desglosado = new List<Validar_Vta.Dt_desglosado>();
            int bandera = -1;
            // obtener informacion del stock actual
            foreach (var detalle in tabladetalle)
            {
                if (await items.itemesconjunto(_context, detalle.coditem))
                {
                    //obtener partes
                    var dt_partes = await _context.inkit
                      .Where(i => i.codigo == detalle.coditem)
                      .OrderBy(i => i.item)
                      .Select(i => new { i.item, i.cantidad })
                      .ToListAsync();
                    //adicionar partes
                    if (dt_partes.Count == 1)
                    {
                        foreach (var partes in dt_partes)
                        {
                            // Asignar valores a las propiedades
                            Validar_Vta.Dt_desglosado nuevaFila = new Validar_Vta.Dt_desglosado
                            {
                                kit = "SI",
                                nro_partes = dt_partes.Count,
                                coditem_cjto = "",
                                coditem_suelto = detalle.coditem,
                                codigo = partes.item,
                                cantidad = (double)(detalle.cantidad * Convert.ToDouble(partes.cantidad)),
                                cantidad_conjunto = 0,
                                cantidad_suelta = (double)(detalle.cantidad * Convert.ToDouble(partes.cantidad))
                            };
                            dt_desglosado.Add(nuevaFila);
                        }
                    }
                    else if (dt_partes.Count > 1)
                    {
                        foreach (var partes in dt_partes)
                        {
                            // Asignar valores a las propiedades
                            Validar_Vta.Dt_desglosado nuevaFila = new Validar_Vta.Dt_desglosado
                            {
                                kit = "SI",
                                nro_partes = dt_partes.Count,
                                coditem_cjto = detalle.coditem,
                                coditem_suelto = "",
                                codigo = partes.item,
                                cantidad = (double)(detalle.cantidad * Convert.ToDouble(partes.cantidad)),
                                cantidad_conjunto = (double)(detalle.cantidad * Convert.ToDouble(partes.cantidad)),
                                cantidad_suelta = 0
                            };
                            dt_desglosado.Add(nuevaFila);
                        }
                    }
                }
                else
                {
                    // Asignar valores a las propiedades
                    Validar_Vta.Dt_desglosado nuevaFila = new Validar_Vta.Dt_desglosado
                    {
                        kit = "NO",
                        nro_partes = 0,
                        coditem_cjto = "",
                        coditem_suelto = detalle.coditem,
                        codigo = detalle.coditem,
                        cantidad = (double)detalle.cantidad,
                        cantidad_conjunto = 0,
                        cantidad_suelta = (double)detalle.cantidad
                    };
                    dt_desglosado.Add(nuevaFila);
                }
            }
            //2DA PARTE UNIR PARTE TOTALIZAR LOS ITEMS(EJEMPLO TODAS LAS TUERCAS PEDIDAS EN CJTO)
            foreach (var desglosado in dt_desglosado)
            {
                bandera = -1;
                foreach (var unido in dtunido)
                {
                    if (unido.codigo == desglosado.codigo && unido.nro_partes == desglosado.nro_partes)
                    {
                        bandera = dtunido.IndexOf(unido);
                        break;
                    }
                }
                if (bandera < 0)
                {
                    Dtnegativos registro = new Dtnegativos
                    {
                        kit = desglosado.kit,
                        nro_partes = desglosado.nro_partes,
                        coditem_suelto = desglosado.coditem_suelto,
                        coditem_cjto = desglosado.coditem_cjto,
                        codigo = desglosado.codigo,
                        descitem = await items.itemdescripcion(_context, desglosado.codigo) + " (" + await items.itemmedida(_context, desglosado.codigo) + ")",
                        cantidad = Convert.ToDecimal(desglosado.cantidad),
                        cantidad_conjunto = Convert.ToDecimal(desglosado.cantidad_conjunto),
                        cantidad_suelta = Convert.ToDecimal(desglosado.cantidad_suelta),
                        saldo_descontando_reservas = 0,
                        saldo_sin_descontar_reservas = 0,
                        cantidad_reservada_para_cjtos = 0,
                        obs = ""
                    };
                    dtunido.Add(registro);
                }
                else
                {
                    // Incrementar las cantidades existentes en dtunido
                    dtunido[bandera].cantidad += Convert.ToDecimal(desglosado.cantidad);
                    dtunido[bandera].cantidad_conjunto += Convert.ToDecimal(desglosado.cantidad_conjunto);
                    dtunido[bandera].cantidad_suelta += Convert.ToDecimal(desglosado.cantidad_suelta);
                }
            }
            controlarStockSeguridad = await empresa.ControlarStockSeguridad_context(_context, cod_empresa);
            //3ERA PARTE VALIDAR LAS CANTIDADES PEDIDAS
            decimal _Saldo_Actual_Item_Con_Descto_Reservas = 0;
            decimal _Saldo_Actual_Item_Sin_Descontando_Reservas = 0;
            decimal _Cantidad_Reservad_Para_Cjto = 0;
            string coditem_dv = "";

            var dtunidoOrdenado = dtunido.OrderByDescending(x => x.kit)
                              .ThenByDescending(x => x.nro_partes)
                              .ThenBy(x => x.coditem_cjto)
                              .ThenBy(x => x.codigo)
                              .ToList();
            dtunido.Clear();
            dtunido.AddRange(dtunidoOrdenado);

            List<string> lista_1 = new List<string>();
            List<string> lista_2 = new List<string>();
            int nro_encontrados = 0;
            //generar una lista con los items repetidos
            foreach (var unido in dtunido)
            {
                if ((lista_1.Count == 0))
                {
                    lista_1.Add(unido.codigo);
                }
                else if (lista_1.Contains(unido.codigo))
                {
                    lista_1.Add(unido.codigo);
                    lista_2.Add(unido.codigo);
                }
                else
                {
                    lista_1.Add(unido.codigo);
                }

            }
            //obtener los saldos de los items
            foreach (var unido in dtunido)
            {
                coditem_dv = unido.codigo;

                _Saldo_Actual_Item_Con_Descto_Reservas = await SaldoItem_CrtlStock_Para_Ventas_Sam(_context, coditem_dv, codalmacen, controlarStockSeguridad, idproforma, numeroidproforma, true, cod_empresa, usrreg);
                _Saldo_Actual_Item_Con_Descto_Reservas = Math.Round(_Saldo_Actual_Item_Con_Descto_Reservas, 2, MidpointRounding.AwayFromZero);

                _Saldo_Actual_Item_Sin_Descontando_Reservas = await SaldoItem_CrtlStock_Para_Ventas_Sam(_context, coditem_dv, codalmacen, controlarStockSeguridad, idproforma, numeroidproforma, false, cod_empresa, usrreg);
                _Saldo_Actual_Item_Sin_Descontando_Reservas = Math.Round(_Saldo_Actual_Item_Sin_Descontando_Reservas, 2, MidpointRounding.AwayFromZero);

                _Cantidad_Reservad_Para_Cjto = _Saldo_Actual_Item_Sin_Descontando_Reservas - _Saldo_Actual_Item_Con_Descto_Reservas;

                unido.saldo_descontando_reservas = _Saldo_Actual_Item_Con_Descto_Reservas;
                unido.saldo_sin_descontar_reservas = _Saldo_Actual_Item_Sin_Descontando_Reservas;
                unido.cantidad_reservada_para_cjtos = _Cantidad_Reservad_Para_Cjto;
            }
            //recalcular el saldo escalaonado de los items repetidos
            int saldo_item = 0;
            foreach (var itemLista in lista_2)
            {
                saldo_item = 0;
                nro_encontrados = 0;

                foreach (var unido in dtunido)
                {
                    if (itemLista == unido.codigo)
                    {
                        if (nro_encontrados == 0)
                        {
                            // No modifica el saldo
                            saldo_item = Convert.ToInt32(unido.saldo_sin_descontar_reservas) - Convert.ToInt32(unido.cantidad);
                        }
                        else if (nro_encontrados > 0)
                        {
                            unido.saldo_sin_descontar_reservas = saldo_item;
                            saldo_item = Convert.ToInt32(unido.saldo_sin_descontar_reservas) - Convert.ToInt32(unido.cantidad);

                            // Esto es para evitar que el saldo para venta suelta salga negativo
                            // Si: saldo_sin_descontar_reservas es mayor a cantidad_reservada_para_cjtos, se hace la diferencia
                            // Sino, simplemente el saldo para venta suelta será CERO
                            if (Convert.ToInt32(unido.saldo_sin_descontar_reservas) > Convert.ToInt32(unido.cantidad_reservada_para_cjtos))
                            {
                                unido.saldo_descontando_reservas = Convert.ToInt32(unido.saldo_sin_descontar_reservas) - Convert.ToInt32(unido.cantidad_reservada_para_cjtos);
                            }
                            else
                            {
                                unido.saldo_descontando_reservas = 0;
                            }
                        }

                        nro_encontrados++;
                    }
                }
            }
            //verificar si genera saldo neg
            foreach (var unido in dtunido)
            {
                unido.obs = "Positivo";

                // Verifica si es mercadería, si no es mercadería ('ANTICIPO','GRECARGO','RPP-DECI','RPP-ENT1','RPP-ENTE','V-COMPUT','V-MOTOCI','V-VEHICU','ZRECARGO','ZRECDEVO','ZVARIOSA')
                // El saldo siempre es positivo
                if (await items.item_tipo(_context, unido.codigo.ToString()) == "MERCADERIA")
                {
                    // Verificar si la cantidad pedida es en conjunto o suelto
                    // Si la cantidad pedida es en conjunto se valida con el saldo total real
                    // Si la cantidad pedida es en suelto se valida con el saldo destinado para sueltos
                    if (Convert.ToInt32(unido.cantidad_conjunto) > 0)
                    {
                        if (Convert.ToInt32(unido.cantidad) > Convert.ToInt32(unido.saldo_sin_descontar_reservas))
                        {
                            unido.obs = "Genera Negativo";
                        }
                        else
                        {
                            unido.obs = "Positivo";
                        }
                    }
                    else if (Convert.ToInt32(unido.cantidad_suelta) > 0)
                    {
                        if (Convert.ToInt32(unido.cantidad_suelta) > Convert.ToInt32(unido.saldo_descontando_reservas))
                        {
                            unido.obs = "Genera Negativo";
                        }
                        else
                        {
                            unido.obs = "Positivo";
                        }
                    }
                }
            }
            //devolver el resultado
            goto fin;

            foreach (var unido in dtunido)
            {
                coditem_dv = unido.codigo;
                if (await items.controla_negativo(_context, unido.codigo))
                {
                    _Saldo_Actual_Item_Con_Descto_Reservas = await SaldoItem_CrtlStock_Para_Ventas_Sam(_context, unido.codigo, codalmacen, controlarStockSeguridad, idproforma, numeroidproforma, true, cod_empresa, usrreg);
                    _Saldo_Actual_Item_Con_Descto_Reservas = Math.Round(_Saldo_Actual_Item_Con_Descto_Reservas, 2, MidpointRounding.AwayFromZero);

                    _Saldo_Actual_Item_Sin_Descontando_Reservas = await SaldoItem_CrtlStock_Para_Ventas_Sam(_context, coditem_dv, codalmacen, controlarStockSeguridad, idproforma, numeroidproforma, false, cod_empresa, usrreg);
                    _Saldo_Actual_Item_Sin_Descontando_Reservas = Math.Round(_Saldo_Actual_Item_Sin_Descontando_Reservas, 2, MidpointRounding.AwayFromZero);

                    _Cantidad_Reservad_Para_Cjto = _Saldo_Actual_Item_Sin_Descontando_Reservas - _Saldo_Actual_Item_Con_Descto_Reservas;

                    unido.saldo_descontando_reservas = _Saldo_Actual_Item_Con_Descto_Reservas;
                    unido.saldo_sin_descontar_reservas = _Saldo_Actual_Item_Sin_Descontando_Reservas;
                    unido.cantidad_reservada_para_cjtos = _Cantidad_Reservad_Para_Cjto;
                    unido.obs = "Positivo";
                    //si el pedido es en conjjunto, validar con el saldo destinado para conjunto
                    if (unido.cantidad_conjunto > 0)
                    {
                        if (unido.cantidad_conjunto > _Cantidad_Reservad_Para_Cjto)
                        {
                            unido.obs = "Genera Negativo";
                        }
                        else
                        {
                            unido.obs = "";
                        }

                    }
                    else if (unido.cantidad_suelta > 0)
                    {
                        if (unido.cantidad_suelta > _Saldo_Actual_Item_Con_Descto_Reservas)
                        {
                            unido.obs = "Genera Negativo";
                        }
                        else
                        {
                            unido.obs = "";
                        }
                    }
                }

            }
            /////
            fin:
            return dtunido;
        }


    }
}
