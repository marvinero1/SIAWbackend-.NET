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
using System.Globalization;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Storage;

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
        private Log log = new Log();
        private Almacen almacen = new Almacen();

        
        public async Task<decimal> saldoitem_crtlstock(DBContext _context, string miempresa, string codigo, int codalmacen, bool ctrlSeguridad, string usuario, bool ctlrStockACubrir = true)
        {
            decimal resultado = 0;
            bool obtener_saldos_otras_ags_localmente = await Obtener_Saldos_Otras_Agencias_Localmente_context(_context, miempresa);
            bool obtener_cantidades_aprobadas_de_proformas = await Obtener_Cantidades_Aprobadas_De_Proformas(_context, miempresa);
            bool Es_Ag_Local = true;
            bool Es_Tienda = false;
            int AlmacenLocalEmpresa = await empresa.AlmacenLocalEmpresa_context(_context, miempresa);
            if (AlmacenLocalEmpresa == codalmacen)
            {
                Es_Ag_Local = true;
            }
            else
            {
                Es_Ag_Local = false;
            }

            if (await almacen.Es_Tienda(_context,codalmacen))
            {
                Es_Tienda = true;
            }
            else
            {
                Es_Tienda = false;
            }
            var dt = await SaldoItem_Crtlstock_Tabla_Para_Ventas_Sam(_context, codigo, codalmacen, Es_Tienda, ctrlSeguridad, "", 0, ctlrStockACubrir, miempresa, usuario, obtener_saldos_otras_ags_localmente, obtener_cantidades_aprobadas_de_proformas, AlmacenLocalEmpresa);
            if (ctlrStockACubrir)
            {
                resultado = dt.detalleSaldos.Sum(i => i.cantidad_ag_local_incluye_cubrir);
            }
            else
            {
                resultado = dt.detalleSaldos.Sum(i => i.cantidad_ag_local);
            }
            if (resultado < 0)
            {
                resultado = 0;
            }
            return resultado;
        }

        public async Task<decimal> SaldoItem_CrtlStock_Para_Ventas(string userConnectionString, string agencia, int codalmacen, string coditem, string codempresa, string usuario)
        {
            //List<sldosItemCompleto> saldos;
            decimal resultado = 0;
            //precio unitario del item
            resultado = await SaldosCompletoResult(userConnectionString, codalmacen, coditem, codempresa, usuario);
            //resultado = tabla;

            return resultado;

        }
        public async Task<(decimal cantidad_ag_local, decimal cantidad_ag_local_incluye_cubrir)> SaldoItem_CrtlStock_Para_Ventas_Sam(DBContext _context, string codigo, int codalmacen, bool esTienda, bool ctrlSeguridad, string idproforma, int numeroidproforma, bool include_saldos_a_cubrir, string codempresa, string usuario, bool obtener_saldos_otras_ags_localmente, bool obtener_cantidades_aprobadas_de_proformas, int AlmacenLocalEmpresa)
        {

            //List<sldosItemCompleto> saldos;
            //decimal resultado = 0;
            //using (var _context = DbContextFactory.Create(userConnectionString))
            //{
            //precio unitario del item
            //saldos = await SaldosCompleto(userConnectionString, agencia, codalmacen, coditem, codempresa, usuario);
            var resultados = await SaldoItem_Crtlstock_Tabla_Para_Ventas_Sam(_context, codigo, codalmacen, esTienda, ctrlSeguridad, idproforma, numeroidproforma, include_saldos_a_cubrir, codempresa, usuario, obtener_saldos_otras_ags_localmente, obtener_cantidades_aprobadas_de_proformas, AlmacenLocalEmpresa);
            
            var detalleSaldos = resultados.detalleSaldos;
            // var detalleSaldosVariables = resultados.detalleSaldosVariables;

            if (detalleSaldos == null)
            {
                return (0,0);
            }
            decimal cantidad_ag_local = detalleSaldos.Sum(i => i.cantidad_ag_local);
            decimal cantidad_ag_local_incluye_cubrir = detalleSaldos.Sum(i => i.cantidad_ag_local_incluye_cubrir);

            cantidad_ag_local = cantidad_ag_local < 0 ? 0 : cantidad_ag_local;
            cantidad_ag_local_incluye_cubrir = cantidad_ag_local_incluye_cubrir < 0 ? 0 : cantidad_ag_local_incluye_cubrir;

            return (cantidad_ag_local, cantidad_ag_local_incluye_cubrir);
        }

        public async Task<(List<saldoItem>? detalleSaldos, List<saldoVariable>? detalleSaldosVariables)> SaldoItem_Crtlstock_Tabla_Para_Ventas_Sam(DBContext _context, string coditem, int codalmacen, bool esTienda, bool ctrlSeguridad, string idproforma, int numeroidproforma, bool include_saldos_a_cubrir, string codempresa, string usuario, bool obtener_saldos_otras_ags_localmente, bool obtener_cantidades_aprobadas_de_proformas, int AlmacenLocalEmpresa)
        {
            try
            {
                // Obtener la conexión gestionada por DbContext
                var connection = _context.Database.GetDbConnection();

                //await connection.OpenAsync();

                if (connection.State != ConnectionState.Open)
                {
                    await connection.OpenAsync();
                }
                // Obtener la transacción actual de DbContext, si existe
                var currentTransaction = _context.Database.CurrentTransaction;


                var detalleSaldos = new List<saldoItem> ();
                var detalleSaldosVariables = new List<saldoVariable> ();

                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "SP_SaldoItem_Crtlstock_Tabla_Para_Ventas";
                    cmd.CommandType = CommandType.StoredProcedure;

                    // Asignar la transacción actual al comando, si existe
                    if (currentTransaction != null)
                    {
                        cmd.Transaction = currentTransaction.GetDbTransaction();
                    }

                    // Añadir los parámetros al comando
                    cmd.Parameters.Add(new SqlParameter("@coditem", coditem));
                    cmd.Parameters.Add(new SqlParameter("@codalmacen", codalmacen));
                    cmd.Parameters.Add(new SqlParameter("@idproforma", idproforma));
                    cmd.Parameters.Add(new SqlParameter("@numeroidproforma", numeroidproforma));
                    cmd.Parameters.Add(new SqlParameter("@Es_tienda", esTienda));
                    cmd.Parameters.Add(new SqlParameter("@obtener_cantidades_aprobadas_de_proformas", obtener_cantidades_aprobadas_de_proformas));
                    cmd.Parameters.Add(new SqlParameter("@ctrlSeguridad", ctrlSeguridad));
                    cmd.Parameters.Add(new SqlParameter("@include_saldos_a_cubrir", include_saldos_a_cubrir));
                    cmd.Parameters.Add(new SqlParameter("@codempresa", codempresa));

                    // Parámetro de salida
                    var outputCantidad = new SqlParameter("@CANTIDAD", SqlDbType.Decimal)
                    {
                        Direction = ParameterDirection.Output,
                        Precision = 18,
                        Scale = 2
                    };
                    cmd.Parameters.Add(outputCantidad);

                    // Ejecutar el comando y capturar el resultado
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var saldoItem = new saldoItem
                            {
                                codalmacen = reader.GetInt32(reader.GetOrdinal("codalmacen")),
                                coditem = reader.GetString(reader.GetOrdinal("coditem")),
                                descripcion = reader.GetString(reader.GetOrdinal("descripcion")),
                                cantidad_ag_local = reader.GetDecimal(reader.GetOrdinal("cantidad_ag_local")),
                                cantidad_ag_local_incluye_cubrir = reader.GetDecimal(reader.GetOrdinal("cantidad_ag_local_incluye_cubrir"))
                            };

                            detalleSaldos.Add(saldoItem);
                        }

                        // Avanzar al siguiente conjunto de resultados (Tabla DetalleVenta)
                        if (await reader.NextResultAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var detalleVariableSaldo = new saldoVariable
                                {
                                    promvta = reader.GetDecimal(reader.GetOrdinal("promvta")),
                                    smin = reader.GetDecimal(reader.GetOrdinal("smin")),
                                    porcenvta = reader.GetDecimal(reader.GetOrdinal("porcenvta")),
                                    saldo = reader.GetDecimal(reader.GetOrdinal("saldo")),
                                    reserva_para_cjto = reader.GetDecimal(reader.GetOrdinal("reserva_para_cjto")),
                                    saldo_para_vta_sueltos = reader.GetDecimal(reader.GetOrdinal("saldo_para_vta_sueltos"))
                                };

                                detalleSaldosVariables.Add(detalleVariableSaldo);
                            }
                        }
                    }
                }

                connection.Close();



                return (detalleSaldos, detalleSaldosVariables);

            }
            catch (Exception)
            {
                return (null, null);
            }
        }


        public async Task<decimal> SaldoItem_Crtlstock_Tabla_Para_Ventas_Sam_anterior(DBContext _context, string coditem, int codalmacen, bool ctrlSeguridad, string idproforma, int numeroidproforma, bool include_saldos_a_cubrir, string codempresa, string usuario, bool obtener_saldos_otras_ags_localmente, bool obtener_cantidades_aprobadas_de_proformas, int AlmacenLocalEmpresa)
        {
            try
            {
                bool Es_Ag_Local = true;
                string cadena_items_conjunto = "";
                bool item_reserva_para_conjunto = false;
                int nro_items_del_conjunto = 0;
                bool RESTRINGIR_VENTA_SUELTA = true;
                var context_original = _context;

                //string userConnectionString = _userConnectionManager.GetUserConnection(userConn);
                //var conexion = _context;
                //var conexion = DbContextFactory.Create(userConnectionString)
                decimal saldoItemTotal = 0;

                bool Es_Kit = await items.EsKit(_context, coditem);  // verifica si el item es kit o no 
                // SE MOVIO A FUERA DEL LLAMADO DE LA FUNCION PARA OPTIMIZAR
                // bool obtener_saldos_otras_ags_localmente = await Obtener_Saldos_Otras_Agencias_Localmente_context(_context, codempresa); // si se obtener las cantidades reservadas de las proformas o no
                // bool obtener_cantidades_aprobadas_de_proformas = await Obtener_Cantidades_Aprobadas_De_Proformas(_context, codempresa); // si se obtener las cantidades reservadas de las proformas o no

                string codItemVta = "";

                if (string.IsNullOrWhiteSpace(coditem) || string.IsNullOrWhiteSpace(codalmacen.ToString().Trim()))
                {
                    goto mi_fin;
                }
                var hay_reg_instoactual = await _context.instoactual
                .Where(i => i.codalmacen == codalmacen && i.coditem == coditem)
                .ToListAsync();

                // Verificar si no se encontraron resultados
                if (hay_reg_instoactual.Count == 0)
                {
                    goto mi_fin;
                }
                // SE MOVIO A FUERA DEL LLAMADO DE LA FUNCION PARA OPTIMIZAR
                // if (await empresa.AlmacenLocalEmpresa_context(_context, codempresa) == codalmacen)
                if (AlmacenLocalEmpresa == codalmacen)
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
                    var conexion = await empaque_func.Getad_conexion_vpnFromDatabase_Sam(_context, codalmacen);
                    if (conexion != "")
                    {
                        //el context sera el mismo q se recibio al inicio
                        _context = DbContextFactory.Create(conexion);
                    }

                }
                /////////////////////////////////////////////////////////////////////////////////////////////
                if (Es_Kit)
                {
                    // SI ES KIT, OBTENER EL NÚMERO DE ITEMS QUE FORMAN EL KIT
                    var conjunto = await _context.inkit.Where(k => k.codigo == coditem).ToListAsync();
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

                    var query1 = await _context.inctrlstock
                        .Where(ctrlstock => ctrlstock.coditem == coditem)
                        .Select(ctrlstock => ctrlstock.coditem).ToListAsync();

                    var query2 = await _context.inreserva
                        .Where(reserva => reserva.coditem == coditem && reserva.codalmacen == codalmacen)
                        .Select(reserva => reserva.coditem).ToListAsync();

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
                    saldoItemTotal = instoactual.cantidad ?? 0;
                }
                else
                {
                    saldoItemTotal = instoactual.cantidad ?? 0;
                }

                // obtiene reservas en proforma
                List<saldosObj> saldosReservProformas = await getReservasProf_Sam(_context, coditem, codalmacen, obtener_cantidades_aprobadas_de_proformas, Es_Kit);
                // List<saldosObj> saldosReservProformas = await getReservasProf_Sam(_context, codItemVta, codalmacen, obtener_cantidades_aprobadas_de_proformas, Es_Kit);


                string codigoBuscado = instoactual.coditem;

                var reservaProf = saldosReservProformas.FirstOrDefault(obj => obj.coditem == codigoBuscado);

                saldoItemTotal -= reservaProf.TotalP;  // reduce saldo total


                // (-) RESERVA STOCK MINIMO DE TIENDAS
                // bool Reserva_Stock_Max_Min = await get_if_reservaStock_item_Sam(_context, coditem);
                bool Reserva_Stock_Max_Min = await get_if_reservaStock_item_Sam(_context, codItemVta);
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
                    saldoItemTotal -= (decimal)STOCK_MINIMO;  // reduce saldo total

                }

                double CANTIDAD_RESERVADA = 0;
                if (include_saldos_a_cubrir && RESTRINGIR_VENTA_SUELTA)
                {
                    // obtiene items si no son kit, sus reservas para armar conjuntos.
                    // double CANTIDAD_RESERVADA = await getReservasCjtos_Sam(_context, coditem, codalmacen, codempresa, Es_Kit, (double)saldoItemTotal, (double)reservaProf.TotalP);
                    CANTIDAD_RESERVADA = await getReservasCjtos_Sam(_context, codItemVta, codalmacen, codempresa, Es_Kit, (double)saldoItemTotal, (double)reservaProf.TotalP);
                }
                saldoItemTotal -= (decimal)CANTIDAD_RESERVADA;  // reduce saldo total
                // obtiene el saldo minimo que debe mantenerse en agencia
                // double Saldo_Minimo_Item = await empaque_func.getSaldoMinimo_Sam(_context, coditem);
                double Saldo_Minimo_Item = await empaque_func.getSaldoMinimo_Sam(_context, codItemVta);

                saldoItemTotal -= (decimal)Saldo_Minimo_Item;  // reduce saldo total

                // obtiene reserva NM ingreso para sol-Urgente
                bool validar_ingresos_solurgentes = await empaque_func.getValidaIngreSolurgente_Sam(_context, codempresa);

                double total_reservado = 0;
                double total_para_esta = 0;
                double total_proforma = 0;
                if (validar_ingresos_solurgentes)
                {
                    //  RESTAR LAS CANTIDADES DE INGRESO POR NOTAS DE MOVIMIENTO URGENTES
                    // de facturas que aun no estan aprobadas
                    // string resp_total_reservado = await getSldIngresoReservNotaUrgent_Sam(_context, coditem, codalmacen);
                    total_reservado = await getSldIngresoReservNotaUrgent_Sam(_context, codItemVta, codalmacen);
                    // total_reservado = double.Parse(resp_total_reservado);


                    //AUMENTAR CANTIDAD PARA ESTA PROFORMA DE INGRESO POR NOTAS DE MOVIMIENTO URGENTES
                    // total_para_esta = await getSldReservNotaUrgentUnaProf_Sam(_context, coditem, codalmacen, idproforma, numeroidproforma);
                    total_para_esta = await getSldReservNotaUrgentUnaProf_Sam(_context, codItemVta, codalmacen, idproforma, numeroidproforma);


                    //AUMENTAR LA CANTIDAD DE LA PROFORMA DE ESTA NOTA QUE PUEDE ESTAR COMO RESERVADA.
                    // total_proforma = await getSldReservProf_Sam(_context, coditem, codalmacen, idproforma, numeroidproforma);
                    total_proforma = await getSldReservProf_Sam(_context, codItemVta, codalmacen, idproforma, numeroidproforma);

                }

                saldoItemTotal -= (decimal)total_reservado;  // reduce saldo total

                saldoItemTotal += (decimal)total_para_esta;  // reduce saldo total

                saldoItemTotal += (decimal)total_proforma;  // reduce saldo total

                mi_fin:
                _context = context_original;
                if (saldoItemTotal < 0)
                {
                    saldoItemTotal = 0;
                }
                return (saldoItemTotal);

            }
            catch (Exception)
            {
                return 0;
            }
        }
        private async Task<instoactual> getEmpaquesItemSelect(string conexion, string coditem, int codalmacen, bool eskit)
        {
            // ***************************///////////////////************************
            // ***************************///////////////////************************
            // Desde 13/08/2024 en los items que son ganchos J validar el saldo segun el gancho J suelto y segun la tabla inkit_saldo_base
            using (var _context = DbContextFactory.Create(conexion))
            {
                var dt_ganchos = await _context.inkit_saldo_base.Where(i => i.codigo == coditem).OrderBy(i => i.codigo).FirstOrDefaultAsync();
                if (dt_ganchos != null)
                {
                    coditem = dt_ganchos.item;       // hacemos que solo traiga el saldo actual del gancho J si esta registrado aca
                    eskit = false;
                }
            }
                

            // ***************************///////////////////************************
            // ***************************///////////////////************************


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
            // ***************************///////////////////************************
            // ***************************///////////////////************************
            // Desde 13/08/2024 en los items que son ganchos J validar el saldo segun el gancho J suelto y segun la tabla inkit_saldo_base

            var dt_ganchos = await _context.inkit_saldo_base.Where(i => i.codigo == coditem).OrderBy(i => i.codigo).FirstOrDefaultAsync();
            if (dt_ganchos != null)
            {
                coditem = dt_ganchos.item;       // hacemos que solo traiga el saldo actual del gancho J si esta registrado aca
                eskit = false;
            }

            // ***************************///////////////////************************
            // ***************************///////////////////************************

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
                        // reserva_para_cjto = _saldoActual - resul - reservaProf;
                        reserva_para_cjto = _saldoActual - resul;
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
                        // reserva_para_cjto = _saldoActual - resul - reservaProf;
                        reserva_para_cjto = _saldoActual - resul;
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
        private async Task<double> getSldIngresoReservNotaUrgent(string userConnectionString, string coditem, int codalmacen)
        {
            // verifica si es almacen o tienda
            double respuestaValor = 0;

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
                    if (double.TryParse(respuesta.Value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out double parsedValue))
                    {
                        respuestaValor = parsedValue;
                    }
                    // respuestaValor = respuesta.Value.ToString();
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
                    if (double.TryParse(respuesta.Value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out double parsedValue))
                    {
                        respuestaValor = parsedValue;
                    }
                    // respuestaValor = respuesta.Value.ToString();
                }
            }
            return respuestaValor;
        }
        private async Task<double> getSldIngresoReservNotaUrgent_Sam(DBContext _context, string coditem, int codalmacen)
        {
            // verifica si es almacen o tienda
            double respuestaValor = 0;

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

                // respuestaValor = respuesta.Value.ToString();
                if (double.TryParse(respuesta.Value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out double parsedValue))
                {
                    respuestaValor = parsedValue;
                }
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
                if (double.TryParse(respuesta.Value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out double parsedValue))
                {
                    respuestaValor = parsedValue;
                }
                // respuestaValor = respuesta.Value.ToString();
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
                // bool Reserva_Stock_Max_Min = await get_if_reservaStock_item(userConnectionString, coditem);
                bool Reserva_Stock_Max_Min = await get_if_reservaStock_item(userConnectionString, codigoBuscado);

                bool ctrlSeguridad = await empresa.ControlarStockSeguridad(userConnectionString, codempresa);
                if (Reserva_Stock_Max_Min && ctrlSeguridad)
                {
                    double STOCK_MINIMO = 0;
                    if (eskit)
                    {
                        // STOCK_MINIMO = await get_stock_Para_Tiendas(userConnectionString, coditem, codalmacen);
                        STOCK_MINIMO = await get_stock_Para_Tiendas(userConnectionString, codigoBuscado, codalmacen);
                    }
                    else
                    {
                        // STOCK_MINIMO = await get_stock_Para_Tiendas(userConnectionString, coditem, codalmacen);
                        STOCK_MINIMO = await get_stock_Para_Tiendas(userConnectionString, codigoBuscado, codalmacen);
                    }
                    saldoItemTotal -= (decimal)STOCK_MINIMO;  // reduce saldo total

                }


                // obtiene items si no son kit, sus reservas para armar conjuntos.
                // double CANTIDAD_RESERVADA = await getReservasCjtos(userConnectionString, coditem, codalmacen, codempresa, eskit, (double)instoactual.cantidad, (double)reservaProf.TotalP);
                double CANTIDAD_RESERVADA = await getReservasCjtos(userConnectionString, codigoBuscado, codalmacen, codempresa, eskit, (double)saldoItemTotal, (double)reservaProf.TotalP);

                saldoItemTotal -= (decimal)CANTIDAD_RESERVADA;  // reduce saldo total

                // obtiene el saldo minimo que debe mantenerse en agencia
                // double Saldo_Minimo_Item = await empaque_func.getSaldoMinimo(userConnectionString, coditem);
                double Saldo_Minimo_Item = await empaque_func.getSaldoMinimo(userConnectionString, codigoBuscado);
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
                    // string resp_total_reservado = await getSldIngresoReservNotaUrgent(userConnectionString, coditem, codalmacen);
                    total_reservado = await getSldIngresoReservNotaUrgent(userConnectionString, codigoBuscado, codalmacen);
                    // total_reservado = double.Parse(resp_total_reservado);


                    //AUMENTAR CANTIDAD PARA ESTA PROFORMA DE INGRESO POR NOTAS DE MOVIMIENTO URGENTES
                    // total_para_esta = await getSldReservNotaUrgentUnaProf(userConnectionString, coditem, codalmacen, "''", 0);
                    total_para_esta = await getSldReservNotaUrgentUnaProf(userConnectionString, codigoBuscado, codalmacen, "''", 0);

                    //AUMENTAR LA CANTIDAD DE LA PROFORMA DE ESTA NOTA QUE PUEDE ESTAR COMO RESERVADA.
                    // total_proforma = await getSldReservProf(userConnectionString, coditem, codalmacen, "''", 0);
                    total_proforma = await getSldReservProf(userConnectionString, codigoBuscado, codalmacen, "''", 0);
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

        public async Task<decimal> SaldoItem(DBContext _context, int codalmacen, string coditem)
        {
            try
            {
                bool kit = await _context.initem.Where(i => i.codigo == coditem).Select(i => i.kit).FirstOrDefaultAsync();
                decimal cantidad = 0;

                if (kit)
                {
                    var dt = await _context.instoactual
                        .Where(i=> i.codalmacen == codalmacen)
                        .Join(_context.inkit,
                        s => s.coditem,
                        k => k.item,
                        (s,k) => new {s,k})
                        .Where(sk => sk.k.codigo == coditem)
                        .Select(i => new
                        {
                            coditem = i.s.coditem,
                            total = i.s.cantidad/i.k.cantidad,
                        })
                        .OrderByDescending(i => i.total)
                        .ToListAsync();

                    cantidad = dt[0].total ?? 0;
                }
                else
                {
                    // si no es kit tomar directo de stock actual
                    cantidad = await _context.instoactual.Where(i => i.coditem == coditem && i.codalmacen == codalmacen).Select(i => i.cantidad).FirstOrDefaultAsync() ?? 0;
                }
                return cantidad;
            }
            catch (Exception)
            {
                return 0;
            }

        }



        public async Task<List<Dtnegativos>> ValidarNegativosDocVenta_paraProbar(DBContext _context, List<itemDataMatriz> tabladetalle, int codalmacen, string idproforma, int numeroidproforma, List<string> mensajes, List<string> negativos, string cod_empresa, string usrreg)
        {
            bool controlarStockSeguridad = await empresa.ControlarStockSeguridad_context(_context, cod_empresa);
            List<Dtnegativos> dtunido = new List<Dtnegativos>();
            List<Dtnegativos> dicUnido = new List<Dtnegativos>();
            //Dictionary<string, Dtnegativos> dicUnido = new Dictionary<string, Dtnegativos>();

            foreach (var detalle in tabladetalle)
            {
                var esConjunto = await items.itemesconjunto(_context, detalle.coditem);
                List<Validar_Vta.Dt_desglosado> dt_desglosado = new List<Validar_Vta.Dt_desglosado>();

                if (esConjunto)
                {
                    var dt_partes = await _context.inkit
                        .Where(i => i.codigo == detalle.coditem)
                        .OrderBy(i => i.item)
                        .Select(i => new { i.item, i.cantidad })
                        .ToListAsync();

                    foreach (var partes in dt_partes)
                    {
                        dt_desglosado.Add(new Validar_Vta.Dt_desglosado
                        {
                            kit = "SI",
                            nro_partes = dt_partes.Count,
                            coditem_cjto = dt_partes.Count > 1 ? detalle.coditem : "",
                            coditem_suelto = dt_partes.Count == 1 ? detalle.coditem : "",
                            codigo = partes.item,
                            cantidad = detalle.cantidad * (double)(partes.cantidad ?? 0),
                            cantidad_conjunto = dt_partes.Count > 1 ? detalle.cantidad * (double)(partes.cantidad??0) : 0,
                            cantidad_suelta = dt_partes.Count == 1 ? detalle.cantidad * (double)(partes.cantidad??0) : 0
                        });
                    }
                }
                else
                {
                    dt_desglosado.Add(new Validar_Vta.Dt_desglosado
                    {
                        kit = "NO",
                        nro_partes = 0,
                        coditem_cjto = "",
                        coditem_suelto = detalle.coditem,
                        codigo = detalle.coditem,
                        cantidad = detalle.cantidad,
                        cantidad_conjunto = 0,
                        cantidad_suelta = detalle.cantidad
                    });
                }

                foreach (var desglosado in dt_desglosado)
                {
                    var registroExistente = dicUnido.FirstOrDefault(i => i.codigo == desglosado.codigo && i.nro_partes == desglosado.nro_partes);
                    if (registroExistente == null)
                    {
                        Dtnegativos newReg = new Dtnegativos();
                        newReg.kit = desglosado.kit;
                        newReg.nro_partes = desglosado.nro_partes;
                        newReg.coditem_suelto = desglosado.coditem_suelto;
                        newReg.coditem_cjto = desglosado.coditem_cjto;
                        newReg.codigo = desglosado.codigo;
                        newReg.descitem = await items.itemdescripcion(_context, desglosado.codigo) + " (" + await items.itemmedida(_context, desglosado.codigo) + ")";
                        newReg.cantidad = (decimal)desglosado.cantidad;
                        newReg.cantidad_conjunto = (decimal)desglosado.cantidad_conjunto;
                        newReg.cantidad_suelta = (decimal)desglosado.cantidad_suelta;

                        dicUnido.Add(newReg);
                    }
                    else
                    {
                        registroExistente.cantidad += (decimal)desglosado.cantidad;
                        registroExistente.cantidad_conjunto += (decimal)desglosado.cantidad_conjunto;
                        registroExistente.cantidad_suelta += (decimal)desglosado.cantidad_suelta;
                    }
                    /*
                    if (dicUnido.ContainsKey(desglosado.codigo) && desglosado.nro_partes == dicUnido[desglosado.codigo].nro_partes)
                    {
                        dicUnido[desglosado.codigo].cantidad += (decimal)desglosado.cantidad;
                        dicUnido[desglosado.codigo].cantidad_conjunto += (decimal)desglosado.cantidad_conjunto;
                        dicUnido[desglosado.codigo].cantidad_suelta += (decimal)desglosado.cantidad_suelta;
                    }
                    else
                    {
                        dicUnido[desglosado.codigo] = new Dtnegativos
                        {
                            kit = desglosado.kit,
                            nro_partes = desglosado.nro_partes,
                            coditem_suelto = desglosado.coditem_suelto,
                            coditem_cjto = desglosado.coditem_cjto,
                            codigo = desglosado.codigo,
                            descitem = await items.itemdescripcion(_context, desglosado.codigo) + " (" + await items.itemmedida(_context, desglosado.codigo) + ")",
                            cantidad = (decimal)desglosado.cantidad,
                            cantidad_conjunto = (decimal)desglosado.cantidad_conjunto,
                            cantidad_suelta = (decimal)desglosado.cantidad_suelta
                        };
                    }
                    */
                }
            }

            var dtunidoOrdenado = dicUnido
                .OrderByDescending(x => x.kit)
                .ThenByDescending(x => x.nro_partes)
                .ThenBy(x => x.coditem_cjto)
                .ThenBy(x => x.codigo)
                .ToList();
            bool obtener_saldos_otras_ags_localmente = await Obtener_Saldos_Otras_Agencias_Localmente_context(_context, cod_empresa); // si se obtener las cantidades reservadas de las proformas o no
            bool obtener_cantidades_aprobadas_de_proformas = await Obtener_Cantidades_Aprobadas_De_Proformas(_context, cod_empresa); // si se obtener las cantidades reservadas de las proformas o no
            var esTienda = await almacen.Es_Tienda(_context, codalmacen);
            // SE MOVIO A FUERA DEL LLAMADO DE LA FUNCION PARA OPTIMIZAR
            int AlmacenLocalEmpresa = await empresa.AlmacenLocalEmpresa_context(_context, cod_empresa);

            foreach (var unido in dtunidoOrdenado)
            {
                
                var resultadoSaldos = await SaldoItem_CrtlStock_Para_Ventas_Sam(_context, unido.codigo, codalmacen, esTienda, controlarStockSeguridad, idproforma, numeroidproforma, true, cod_empresa, usrreg, obtener_saldos_otras_ags_localmente, obtener_cantidades_aprobadas_de_proformas, AlmacenLocalEmpresa);

                // unido.saldo_descontando_reservas = await SaldoItem_CrtlStock_Para_Ventas_Sam(_context, unido.codigo, codalmacen, esTienda, controlarStockSeguridad, idproforma, numeroidproforma, true, cod_empresa, usrreg, obtener_saldos_otras_ags_localmente, obtener_cantidades_aprobadas_de_proformas, AlmacenLocalEmpresa);
                // unido.saldo_sin_descontar_reservas = await SaldoItem_CrtlStock_Para_Ventas_Sam(_context, unido.codigo, codalmacen, esTienda, controlarStockSeguridad, idproforma, numeroidproforma, false, cod_empresa, usrreg, obtener_saldos_otras_ags_localmente, obtener_cantidades_aprobadas_de_proformas, AlmacenLocalEmpresa);

                unido.saldo_descontando_reservas = resultadoSaldos.cantidad_ag_local_incluye_cubrir;
                unido.saldo_sin_descontar_reservas = resultadoSaldos.cantidad_ag_local;


                unido.cantidad_reservada_para_cjtos = unido.saldo_sin_descontar_reservas - unido.saldo_descontando_reservas;

                unido.obs = "Positivo";
                if (await items.item_tipo(_context, unido.codigo) == "MERCADERIA")
                {
                    if (unido.cantidad_conjunto > 0 && unido.cantidad > unido.saldo_sin_descontar_reservas)
                    {
                        unido.obs = "Genera Negativo";
                    }
                    else if (unido.cantidad_suelta > 0 && unido.cantidad_suelta > unido.saldo_descontando_reservas)
                    {
                        unido.obs = "Genera Negativo";
                    }
                }
            }

            return dtunidoOrdenado;
            // return dtunido;
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

            bool obtener_saldos_otras_ags_localmente = await Obtener_Saldos_Otras_Agencias_Localmente_context(_context, cod_empresa); // si se obtener las cantidades reservadas de las proformas o no
            bool obtener_cantidades_aprobadas_de_proformas = await Obtener_Cantidades_Aprobadas_De_Proformas(_context, cod_empresa); // si se obtener las cantidades reservadas de las proformas o no
            var esTienda = await almacen.Es_Tienda(_context, codalmacen);

            //obtener los saldos de los items
            foreach (var unido in dtunido)
            {
                coditem_dv = unido.codigo;
                // SE MOVIO A FUERA DEL LLAMADO DE LA FUNCION PARA OPTIMIZAR

                int AlmacenLocalEmpresa = await empresa.AlmacenLocalEmpresa_context(_context, cod_empresa);

                var resultadoSaldos = await SaldoItem_CrtlStock_Para_Ventas_Sam(_context, coditem_dv, codalmacen, esTienda, controlarStockSeguridad, idproforma, numeroidproforma, true, cod_empresa, usrreg, obtener_saldos_otras_ags_localmente, obtener_cantidades_aprobadas_de_proformas, AlmacenLocalEmpresa);

                // _Saldo_Actual_Item_Con_Descto_Reservas = await SaldoItem_CrtlStock_Para_Ventas_Sam(_context, coditem_dv, codalmacen, esTienda, controlarStockSeguridad, idproforma, numeroidproforma, true, cod_empresa, usrreg, obtener_saldos_otras_ags_localmente, obtener_cantidades_aprobadas_de_proformas, AlmacenLocalEmpresa);

                _Saldo_Actual_Item_Con_Descto_Reservas = resultadoSaldos.cantidad_ag_local_incluye_cubrir;
                _Saldo_Actual_Item_Con_Descto_Reservas = Math.Round(_Saldo_Actual_Item_Con_Descto_Reservas, 2, MidpointRounding.AwayFromZero);

                // _Saldo_Actual_Item_Sin_Descontando_Reservas = await SaldoItem_CrtlStock_Para_Ventas_Sam(_context, coditem_dv, codalmacen, esTienda, controlarStockSeguridad, idproforma, numeroidproforma, false, cod_empresa, usrreg, obtener_saldos_otras_ags_localmente, obtener_cantidades_aprobadas_de_proformas, AlmacenLocalEmpresa);

                _Saldo_Actual_Item_Sin_Descontando_Reservas = resultadoSaldos.cantidad_ag_local;
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
                    // SE MOVIO A FUERA DEL LLAMADO DE LA FUNCION PARA OPTIMIZAR
                    int AlmacenLocalEmpresa = await empresa.AlmacenLocalEmpresa_context(_context, cod_empresa);

                    var resultadoSaldos = await SaldoItem_CrtlStock_Para_Ventas_Sam(_context, unido.codigo, codalmacen, esTienda, controlarStockSeguridad, idproforma, numeroidproforma, true, cod_empresa, usrreg, obtener_saldos_otras_ags_localmente, obtener_cantidades_aprobadas_de_proformas, AlmacenLocalEmpresa);

                    // _Saldo_Actual_Item_Con_Descto_Reservas = await SaldoItem_CrtlStock_Para_Ventas_Sam(_context, unido.codigo, codalmacen, esTienda, controlarStockSeguridad, idproforma, numeroidproforma, true, cod_empresa, usrreg, obtener_saldos_otras_ags_localmente, obtener_cantidades_aprobadas_de_proformas, AlmacenLocalEmpresa);
                    _Saldo_Actual_Item_Con_Descto_Reservas = resultadoSaldos.cantidad_ag_local_incluye_cubrir;
                    _Saldo_Actual_Item_Con_Descto_Reservas = Math.Round(_Saldo_Actual_Item_Con_Descto_Reservas, 2, MidpointRounding.AwayFromZero);

                    // _Saldo_Actual_Item_Sin_Descontando_Reservas = await SaldoItem_CrtlStock_Para_Ventas_Sam(_context, coditem_dv, codalmacen, esTienda, controlarStockSeguridad, idproforma, numeroidproforma, false, cod_empresa, usrreg, obtener_saldos_otras_ags_localmente, obtener_cantidades_aprobadas_de_proformas, AlmacenLocalEmpresa);
                    _Saldo_Actual_Item_Sin_Descontando_Reservas = resultadoSaldos.cantidad_ag_local;
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





        public async Task<(bool resultado, string msg)> SaldoActual_Disminuir_new(DBContext _context, int codalmacen, string coditem, double cantidad)
        {
            try
            {
                bool resultado = true;
                var itemsActualizados = new List<instoactual>();
                if (await items.itemesconjunto(_context, coditem))
                {
                    var tabla_kit = await _context.inkit.Where(i => i.codigo == coditem)
                        .Select(i => new
                        {
                            i.item,
                            i.cantidad
                        }).ToListAsync();
                    var itemsInsto = await _context.instoactual
                        .Where(i => i.codalmacen == codalmacen && tabla_kit.Select(k => k.item).Contains(i.coditem))
                        .ToListAsync();

                    foreach (var reg in tabla_kit)
                    {
                        var instoActualItem = itemsInsto.FirstOrDefault(i => i.coditem == reg.item);
                        if (instoActualItem != null)
                        {
                            instoActualItem.cantidad -= (decimal?)(cantidad * (double)(reg.cantidad ?? 0));
                            itemsActualizados.Add(instoActualItem);
                        }
                        else
                        {
                            throw new Exception($"Item {reg.item} no encontrado en almacen {codalmacen}");
                        }
                    }
                }
                else
                {
                    var instoActualItem = await _context.instoactual
                        .Where(i => i.codalmacen == codalmacen && i.coditem == coditem)
                        .FirstOrDefaultAsync();

                    // Verificar si el item existe antes de modificarlo
                    if (instoActualItem != null)
                    {
                        instoActualItem.cantidad -= (decimal?)cantidad;
                        itemsActualizados.Add(instoActualItem);
                    }
                    else
                    {
                        resultado = false;
                    }
                }
                // Guardar todos los cambios de una vez al final
                if (resultado)
                {
                    _context.instoactual.UpdateRange(itemsActualizados);
                    await _context.SaveChangesAsync();
                }
                return (resultado, "");
            }
            catch (Exception ex)
            {
                string mensajeError = ex.Message.Length > 500 ? ex.Message.Substring(0, 500) : ex.Message;
                return (false, mensajeError);
            }
        }


        public async Task<(bool resultado,string msg)> SaldoActual_Disminuir(DBContext _context, int codalmacen, string coditem, double cantidad)
        {
            try
            {
                bool resultado = true;
                var itemsActualizados = new List<instoactual>();
                if (await items.itemesconjunto(_context, coditem))
                {
                    var tabla_kit = await _context.inkit.Where(i => i.codigo == coditem)
                        .Select(i => new
                        {
                            i.item,
                            i.cantidad
                        }).ToListAsync();
                    foreach (var reg in tabla_kit)
                    {
                        var instoActualItem = await _context.instoactual
                            .Where(i => i.codalmacen == codalmacen && i.coditem == reg.item)
                            .FirstOrDefaultAsync();

                        // Verificar si el item existe antes de modificarlo
                        if (instoActualItem != null)
                        {
                            instoActualItem.cantidad -= (decimal?)(cantidad * (double)(reg.cantidad ?? 0));
                            _context.Entry(instoActualItem).State = EntityState.Modified;
                        }
                        else
                        {
                            // Manejar el caso de item no encontrado (loggear o manejar la excepción)
                            throw new Exception($"Item {reg.item} no encontrado en almacen {codalmacen}");
                        }
                    }
                }
                else
                {
                    var instoActualItem = await _context.instoactual
                        .Where(i => i.codalmacen == codalmacen && i.coditem == coditem)
                        .FirstOrDefaultAsync();

                    // Verificar si el item existe antes de modificarlo
                    if (instoActualItem != null)
                    {
                        instoActualItem.cantidad -= (decimal?)cantidad;
                        _context.Entry(instoActualItem).State = EntityState.Modified;
                    }
                    else
                    {
                        // Manejar el caso de item no encontrado
                        resultado = false;
                        // throw new Exception($"Item {coditem} no encontrado en almacen {codalmacen}");
                    }
                }
                // Guardar todos los cambios de una vez al final
                if (resultado)
                {
                    await _context.SaveChangesAsync();
                }
                return (resultado, "");
            }
            catch (Exception ex)
            {
                string mensajeError = ex.Message.Length > 500 ? ex.Message.Substring(0, 500) : ex.Message;
                return (false, mensajeError);
            }
        }

        public async Task<(bool resultado, string msg)> SaldoActual_Aumentar_new(DBContext _context, int codalmacen, string coditem, double cantidad)
        {
            try
            {
                bool resultado = true;
                var itemsActualizados = new List<instoactual>();
                if (await items.itemesconjunto(_context, coditem))
                {
                    var tabla_kit = await _context.inkit.Where(i => i.codigo == coditem)
                        .Select(i => new
                        {
                            i.item,
                            i.cantidad
                        }).ToListAsync();
                    var itemsInsto = await _context.instoactual
                        .Where(i => i.codalmacen == codalmacen && tabla_kit.Select(k => k.item).Contains(i.coditem))
                        .ToListAsync();
                    foreach (var reg in tabla_kit)
                    {
                        var instoActualItem = itemsInsto.FirstOrDefault(i => i.coditem == reg.item);
                        if (instoActualItem != null)
                        {
                            instoActualItem.cantidad += (decimal?)(cantidad * (double)(reg.cantidad ?? 0));
                            itemsActualizados.Add(instoActualItem);
                        }
                        else
                        {
                            throw new Exception($"Item {reg.item} no encontrado en almacen {codalmacen}");
                        }
                    }
                }
                else
                {
                    var instoActualItem = await _context.instoactual
                                .Where(i => i.codalmacen == codalmacen && i.coditem == coditem)
                                .FirstOrDefaultAsync();
                    if (instoActualItem != null)
                    {
                        instoActualItem.cantidad += (decimal?)cantidad;
                        itemsActualizados.Add(instoActualItem);
                    }
                    else
                    {
                        // Manejar el caso de item no encontrado
                        resultado = false;
                        // throw new Exception($"Item {coditem} no encontrado en almacen {codalmacen}");
                    }
                }
                if (resultado)
                {
                    _context.instoactual.UpdateRange(itemsActualizados);
                    await _context.SaveChangesAsync();
                }
                return (resultado, "");
            }
            catch (Exception ex)
            {
                string mensajeError = ex.Message.Length > 500 ? ex.Message.Substring(0, 500) : ex.Message;
                return (false, mensajeError);
            }
        }


        public async Task<(bool resultado, string msg)> SaldoActual_Aumentar(DBContext _context, int codalmacen, string coditem, double cantidad)
        {
            try
            {
                bool resultado = true;
                if (await items.itemesconjunto(_context, coditem))
                {
                    var tabla_kit = await _context.inkit.Where(i => i.codigo == coditem)
                        .Select(i => new
                        {
                            i.item,
                            i.cantidad
                        }).ToListAsync();
                    foreach (var reg in tabla_kit)
                    {
                        var instoActualItem = await _context.instoactual
                               .Where(i => i.codalmacen == codalmacen && i.coditem == reg.item)
                               .FirstOrDefaultAsync();
                        if (instoActualItem != null)
                        {
                            instoActualItem.cantidad = instoActualItem.cantidad + (decimal?)(cantidad * (double)(reg.cantidad ?? 0));
                            _context.Entry(instoActualItem).State = EntityState.Modified;
                        }
                        else
                        {
                            throw new Exception($"Item {reg.item} no encontrado en almacen {codalmacen}");
                        }
                    }
                }
                else
                {
                    var instoActualItem = await _context.instoactual
                                .Where(i => i.codalmacen == codalmacen && i.coditem == coditem)
                                .FirstOrDefaultAsync();
                    if (instoActualItem != null)
                    {
                        instoActualItem.cantidad = instoActualItem.cantidad + (decimal?)(cantidad);

                        _context.Entry(instoActualItem).State = EntityState.Modified;
                    }
                    else
                    {
                        // Manejar el caso de item no encontrado
                        resultado = false;
                        // throw new Exception($"Item {coditem} no encontrado en almacen {codalmacen}");
                    }
                }
                if (resultado)
                {
                    await _context.SaveChangesAsync();
                }
                return (resultado, "");
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        public async Task<decimal> stockminimo_item(DBContext _context, int codalmacen, string coditem)
        {
            if (coditem.Trim() == "" || codalmacen <= 0)
            {
                return 0;
            }
            // ver si es kit
            try
            {
                decimal cantidad = await _context.instockalm.Where(i => i.item == coditem && i.codalmacen == codalmacen).Select(i => i.smin).FirstOrDefaultAsync() ?? 0;
                return cantidad;
            }
            catch (Exception)
            {
                return 0;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////
        // funcion que muestra devuelve el stock maximo de un item en un almacen 
        ///////////////////////////////////////////////////////////////////////////////
        public async Task<decimal> stockmaximo_item(DBContext _context, int codalmacen, string coditem)
        {
            decimal resultado = 0;

            if (coditem.Trim() == "" || codalmacen <= 0)
            {
                return 0;
            }
            if (await items.itemesconjunto(_context, coditem))
            {
                var dt_partes = await _context.inkit.Where(i => i.codigo == coditem).Select(i => new
                {
                    i.item,
                    i.cantidad
                }).ToListAsync();
                foreach (var reg in dt_partes)
                {
                    // var sm_parte = await _context.instockalm.Where(i => i.item == coditem && i.codalmacen == codalmacen).Select(i => i.smax).FirstOrDefaultAsync() ?? 0;
                    decimal sm_parte = await stockmaximo_item(_context, codalmacen, reg.item);
                    if (reg.cantidad > 0)
                    {
                        sm_parte = sm_parte/(decimal)reg.cantidad;
                    }
                    if (sm_parte > resultado)
                    {
                        resultado = sm_parte;
                    }
                }
            }
            else
            {
                try
                {
                    resultado = await _context.instockalm.Where(i => i.item == coditem && i.codalmacen == codalmacen).Select(i => i.smax).FirstOrDefaultAsync() ?? 0;
                }
                catch (Exception)
                {
                    resultado = 0;
                }
            }
            return resultado;
        }




        public async Task<bool> Grabar_Proforma_SolUrgente_en_Destino(DBContext _context, string codempresa)
        {
            try
            {
                bool resultado = await _context.adparametros.Where(i => i.codempresa == codempresa).Select(i => i.grabar_proforma_solurgente_en_destino).FirstOrDefaultAsync() ?? false;
                return resultado;
            }
            catch (Exception)
            {
                return false;
            }
        }


        public async Task<bool> Veremision_ActualizarSaldo(DBContext _context, int codigo, ModoActualizacion modo)
        {
            try
            {
                bool resultado = false;

                var prm_codigo = new SqlParameter("@codigo", SqlDbType.Int) { Value = codigo };
                var prm_modo = new SqlParameter("@modo_actualizacion", SqlDbType.NVarChar, 10) { Value = modo };
                var result_sp = new SqlParameter("@Resultado", SqlDbType.Bit) { Direction = ParameterDirection.Output };

                await _context.Database.ExecuteSqlRawAsync(
                    "EXECUTE Veremision_ActualizarSaldo @codigo, @modo_actualizacion, @Resultado OUTPUT",
                    prm_codigo, prm_modo, result_sp);

                resultado = (bool)result_sp.Value;

                return resultado;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> Vefactura_ActualizarSaldo(DBContext _context, int codigo, ModoActualizacion modo)
        {
            try
            {
                bool resultado = false;

                var prm_codigo = new SqlParameter("@codigo", SqlDbType.Int) { Value = codigo };
                var prm_modo = new SqlParameter("@modo_actualizacion", SqlDbType.NVarChar, 10) { Value = modo };
                var result_sp = new SqlParameter("@Resultado", SqlDbType.Bit) { Direction = ParameterDirection.Output };

                await _context.Database.ExecuteSqlRawAsync(
                    "EXECUTE Vefactura_ActualizarSaldo @codigo, @modo_actualizacion, @Resultado OUTPUT",
                    prm_codigo, prm_modo, result_sp);

                resultado = (bool)result_sp.Value;

                return resultado;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> Inmovimiento_ActualizarSaldo(DBContext _context, int codigo, ModoActualizacion modo)
        {
            try
            {
                bool resultado = false;

                var prm_codigo = new SqlParameter("@codigo", SqlDbType.Int) { Value = codigo };
                var prm_modo = new SqlParameter("@modo_actualizacion", SqlDbType.NVarChar, 30) { Value = modo };
                var result_sp = new SqlParameter("@Resultado", SqlDbType.Bit) { Direction = ParameterDirection.Output };

                await _context.Database.ExecuteSqlRawAsync(
                    "EXECUTE Inmovimiento_ActualizarSaldo @codigo, @modo_actualizacion, @Resultado OUTPUT",
                    prm_codigo, prm_modo, result_sp);

                resultado = (bool)result_sp.Value;

                return resultado;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return false;
            }
        }


        public enum ModoActualizacion
        {
            Crear,
            CrearSoloModificados,
            Eliminar,
            EliminarSoloModificados
        }
    }




}
public class saldoItem
{
    public int codalmacen { get; set; }
    public string coditem { get; set; }
    public string descripcion { get; set; }
    public decimal cantidad_ag_local { get; set; }
    public decimal cantidad_ag_local_incluye_cubrir { get; set; }
}
public class saldoVariable
{
    public decimal promvta { get; set; }
    public decimal smin { get; set; }
    public decimal porcenvta { get; set; }
    public decimal saldo { get; set; }
    public decimal reserva_para_cjto { get; set; }
    public decimal saldo_para_vta_sueltos { get; set; }
}