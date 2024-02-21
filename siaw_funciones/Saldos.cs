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
            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                //precio unitario del item
                resultado = await SaldosCompletoResult(userConnectionString, codalmacen, coditem, codempresa, usuario);
                //resultado = tabla;
            }
            //for (int i = 0; (i <= (saldos.Count - 1)); i++)
            //{
            //resultado = (decimal)saldos[7].valor;
            //}

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
                    if (instoactual == null)
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
        private async Task<float> getReservasCjtos(string userConnectionString, string coditem, int codalmacen, string codempresa, bool eskit, float _saldoActual, float reservaProf)
        {
            List<inctrlstock> itemsinReserva = null;
            float CANTIDAD_RESERVADA = 0;

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
                    return (float)(itemRef.cantidad * (item.porcentaje / 100));
                });

                var cantidadReservadaArray = await Task.WhenAll(cantidadReservadaTasks);
                CANTIDAD_RESERVADA = cantidadReservadaArray.Sum();
                */
                foreach (var item in itemsinReserva)
                {
                    instoactual itemRef = await empaque_func.GetSaldosActual(userConnectionString, codalmacen, item.coditemcontrol);
                    float cubrir_item = (float)(itemRef.cantidad * (item.porcentaje / 100));
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
                    float cubrir_item = (float)reserva2[0].cantidad;
                    CANTIDAD_RESERVADA += cubrir_item;
                }
                if (CANTIDAD_RESERVADA < 0)
                {
                    CANTIDAD_RESERVADA = 0;
                }

                float resul = 0;
                float reserva_para_cjto = 0;
                float CANTIDAD_RESERVADA_DINAMICA = 0;

                inreserva_area reserva = await empaque_func.Obtener_Cantidad_Segun_SaldoActual_PromVta_SMin_PorcenVta(userConnectionString, coditem, codalmacen);


                if (reserva != null)
                {
                    if ((float)reserva.porcenvta > 0.5)
                    {
                        reserva.porcenvta = (decimal)0.5;
                    }

                    if (_saldoActual >= (double)reserva.smin)
                    {
                        resul = (float)(reserva.porcenvta * reserva.promvta);
                        resul = (float)Math.Round(resul, 2);
                        reserva_para_cjto = _saldoActual - resul - reservaProf;
                    }
                    else
                    {
                        reserva_para_cjto = _saldoActual;
                    }


                    CANTIDAD_RESERVADA_DINAMICA = reserva_para_cjto;
                    if (CANTIDAD_RESERVADA_DINAMICA > 0)
                    {
                        CANTIDAD_RESERVADA = (float)CANTIDAD_RESERVADA_DINAMICA;
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
        private async Task<float> getSldReservNotaUrgentUnaProf(string userConnectionString, string coditem, int codalmacen, string idProf, int nroIdProf)
        {
            // verifica si es almacen o tienda
            float total_para_esta = 0;

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
        private async Task<float> getSldReservProf(string userConnectionString, string coditem, int codalmacen, string idProf, int nroIdProf)
        {
            // verifica si es almacen o tienda
            float respuestaValor = 0;

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
                bool usar_bd_opcional = await get_usar_bd_opcional(userConnectionString, usuario);

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
                    saldosDetalleItem.valor = (float)instoactual.cantidad;
                    listaSaldos.Add(saldosDetalleItem);
                }
                else
                {
                    //saldosDetalleItem.txtReservaProf = "(-) PROFORMAS APROBADAS: " + instoactual.coditem;
                    sldosItemCompleto saldosDetalleItem = new sldosItemCompleto();
                    saldosDetalleItem.descripcion = "(+)SALDO ACTUAL ITEM: " + instoactual.coditem;
                    saldosDetalleItem.valor = (float)instoactual.cantidad;
                    listaSaldos.Add(saldosDetalleItem);
                }
                saldoItemTotal.valor = (float)instoactual.cantidad;


                // obtiene reservas en proforma
                List<saldosObj> saldosReservProformas = await getReservasProf(conexion, coditem, codalmacen, obtener_cantidades_aprobadas_de_proformas, eskit);


                string codigoBuscado = instoactual.coditem;

                var reservaProf = saldosReservProformas.FirstOrDefault(obj => obj.coditem == codigoBuscado);

                instoactual.coditem = coditem;
                if (eskit)
                {
                    sldosItemCompleto saldosDetalleItem = new sldosItemCompleto();
                    saldosDetalleItem.descripcion = "(-) PROFORMAS APROBADAS ITEM(" + instoactual.coditem + ") DEL CJTO(" + coditem + ")";
                    saldosDetalleItem.valor = (float)reservaProf.TotalP * -1;
                    listaSaldos.Add(saldosDetalleItem);
                }
                else
                {
                    sldosItemCompleto saldosDetalleItem = new sldosItemCompleto();
                    saldosDetalleItem.descripcion = "(-) PROFORMAS APROBADAS: " + instoactual.coditem;
                    saldosDetalleItem.valor = (float)reservaProf.TotalP * -1;
                    listaSaldos.Add(saldosDetalleItem);
                }

                saldoItemTotal.valor -= (float)reservaProf.TotalP;  // reduce saldo total
                var8.valor = saldoItemTotal.valor;



                // (-) RESERVA STOCK MINIMO DE TIENDAS
                bool Reserva_Stock_Max_Min = await get_if_reservaStock_item(userConnectionString, coditem);
                bool ctrlSeguridad = await empresa.ControlarStockSeguridad(userConnectionString, codempresa);
                if (Reserva_Stock_Max_Min && ctrlSeguridad)
                {
                    float STOCK_MINIMO = 0;
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
                float CANTIDAD_RESERVADA = await getReservasCjtos(userConnectionString, coditem, codalmacen, codempresa, eskit, (float)instoactual.cantidad, (float)reservaProf.TotalP);
                var1.descripcion = "(-) SALDO RESERVADO PARA ARMAR CJTOS";
                var1.valor = CANTIDAD_RESERVADA * -1;
                listaSaldos.Add(var1);
                saldoItemTotal.valor -= CANTIDAD_RESERVADA;  // reduce saldo total

                // obtiene el saldo minimo que debe mantenerse en agencia
                sldosItemCompleto var2 = new sldosItemCompleto();
                float Saldo_Minimo_Item = await empaque_func.getSaldoMinimo(userConnectionString, coditem);
                var2.descripcion = "(-) SALDO MINIMO DEL ITEM";
                var2.valor = Saldo_Minimo_Item * -1;
                listaSaldos.Add(var2);
                saldoItemTotal.valor -= Saldo_Minimo_Item;  // reduce saldo total

                // obtiene reserva NM ingreso para sol-Urgente
                bool validar_ingresos_solurgentes = await empaque_func.getValidaIngreSolurgente(userConnectionString, codempresa);

                float total_reservado = 0;
                float total_para_esta = 0;
                float total_proforma = 0;
                if (validar_ingresos_solurgentes)
                {
                    //  RESTAR LAS CANTIDADES DE INGRESO POR NOTAS DE MOVIMIENTO URGENTES
                    // de facturas que aun no estan aprobadas
                    string resp_total_reservado = await getSldIngresoReservNotaUrgent(userConnectionString, coditem, codalmacen);
                    total_reservado = float.Parse(resp_total_reservado);


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


        public async Task<float> get_stock_Para_Tiendas(string userConnectionString, string coditem, int codalmacen)
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


        public async Task<float> get_Porcentaje_Maximo_de_Venta_Respecto_Del_Saldo(DBContext _context, int codalmacen, string coditem)
        {
            var porcen_maximo = await _context.initem_max_vta
                    .Where(item => item.codalmacen == codalmacen && item.coditem == coditem)
                    .Select(item => item.porcen_maximo)
                    .FirstOrDefaultAsync();

            if (porcen_maximo == null)
            {
                return 0;
            }

            return (float)porcen_maximo;
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

                return usar_bd_opcional;
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
                    float STOCK_MINIMO = 0;
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
                float CANTIDAD_RESERVADA = await getReservasCjtos(userConnectionString, coditem, codalmacen, codempresa, eskit, (float)instoactual.cantidad, (float)reservaProf.TotalP);
                saldoItemTotal -= (decimal)CANTIDAD_RESERVADA;  // reduce saldo total

                // obtiene el saldo minimo que debe mantenerse en agencia
                float Saldo_Minimo_Item = await empaque_func.getSaldoMinimo(userConnectionString, coditem);
                saldoItemTotal -= (decimal)Saldo_Minimo_Item;  // reduce saldo total

                // obtiene reserva NM ingreso para sol-Urgente
                bool validar_ingresos_solurgentes = await empaque_func.getValidaIngreSolurgente(userConnectionString, codempresa);

                float total_reservado = 0;
                float total_para_esta = 0;
                float total_proforma = 0;
                if (validar_ingresos_solurgentes)
                {
                    //  RESTAR LAS CANTIDADES DE INGRESO POR NOTAS DE MOVIMIENTO URGENTES
                    // de facturas que aun no estan aprobadas
                    string resp_total_reservado = await getSldIngresoReservNotaUrgent(userConnectionString, coditem, codalmacen);
                    total_reservado = float.Parse(resp_total_reservado);


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


    }
}
