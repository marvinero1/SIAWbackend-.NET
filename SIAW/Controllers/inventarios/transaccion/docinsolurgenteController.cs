using LibSIAVB;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SIAW.Controllers.ventas.transaccion;
using siaw_DBContext.Data;
using siaw_DBContext.Models;
using siaw_DBContext.Models_Extra;
using siaw_funciones;
using System.Data;
using System.Web.Http.Results;

namespace SIAW.Controllers.inventarios.transaccion
{
    [Route("api/inventario/transac/[controller]")]
    [ApiController]
    public class docinsolurgenteController : ControllerBase
    {
        private readonly string _controllerName = "docinsolurgenteController";

        private readonly Configuracion configuracion = new Configuracion();
        private readonly Nombres nombres = new Nombres();
        private readonly Seguridad seguridad = new Seguridad();
        private readonly Items items = new Items();
        private readonly Cliente cliente = new Cliente();
        private readonly Almacen almacen = new Almacen();
        private readonly Documento documento = new Documento();
        private readonly Ventas ventas = new Ventas();
        private readonly Inventario inventario = new Inventario();
        private readonly Empresa empresa = new Empresa();
        private readonly Saldos saldos = new Saldos();
        private readonly Restricciones restricciones = new Restricciones();
        private readonly HardCoded hardCoded = new HardCoded();
        private readonly empaquesFunciones empaque_func = new empaquesFunciones();
        private readonly TipoCambio tipoCambio = new TipoCambio();
        private readonly Funciones funciones = new Funciones();
        private readonly Log log = new Log();

        private readonly func_encriptado encripVB = new func_encriptado();

        private readonly UserConnectionManager _userConnectionManager;
        public docinsolurgenteController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }


        [HttpGet]
        [Route("getParamsIniSolUrg/{userConn}/{usuario}/{codempresa}")]
        public async Task<ActionResult<object>> getParamsIniSolUrg(string userConn, string usuario, string codempresa)
        {
            try
            {
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);
                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    int codalmacen = await configuracion.usr_codalmacen(_context, usuario);
                    string codalmacendescripcion = await nombres.nombrealmacen(_context, codalmacen);
                    string codmoneda_total = await Empresa.monedabase(_context, codempresa);
                    
                    return Ok(new
                    {
                        codalmacen,
                        codalmacendescripcion,
                        codmoneda_total
                    });
                }
            }
            catch (Exception ex)
            {
                return Problem("Error en el servidor al obtener parametros iniciales Sol. Urgente: " + ex.Message);
                throw;
            }
        }

        [HttpPost]
        [Route("calcularDetalle/{userConn}/{codalmacen}/{codempresa}/{usuario}/{codtarifa}")]
        public async Task<ActionResult<object>> calcularDetalle(string userConn, int codalmacen, string codempresa, string usuario, int codtarifa, List<detalleRequest> detallaSolUrg)
        {
            try
            {
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);
                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    foreach (var reg in detallaSolUrg)
                    {
                        if (await empresa.ControlarStockSeguridad_context(_context,codempresa))
                        {
                            if (await ventas.Reservar_Para_Tiendas_En_Sol_Urgentes(_context,codtarifa))
                            {
                                reg.saldoag = await saldos.saldoitem_crtlstock(_context, codempresa, reg.coditem, codalmacen, true, usuario);
                            }
                            else
                            {
                                reg.saldoag = await saldos.saldoitem_crtlstock(_context, codempresa, reg.coditem, codalmacen, false, usuario);
                            }
                        }
                        else
                        {
                            reg.saldoag = await saldos.saldoitem_crtlstock(_context, codempresa, reg.coditem, codalmacen, false, usuario);
                        }
                        reg.stockmax = await saldos.stockmaximo_item(_context, codalmacen, reg.coditem);
                        reg.precio = (decimal?)await ventas.preciodelistaitem(_context, codtarifa, reg.coditem);
                        reg.total = reg.cantidad * reg.precio;
                    }
                    return Ok(new
                    {
                        detalle = detallaSolUrg
                    });
                }
            }
            catch (Exception ex)
            {
                return Problem("Error en el servidor al calcular Detalle de Sol. Urgente: " + ex.Message);
                throw;
            }
        }

        [HttpPost]
        [Route("calcTotArreglaDesc/{userConn}/{codempresa}")]
        public async Task<ActionResult<object>> calcTotArreglaDesc(string userConn, string codempresa, requestGabrarSolUrgente dataGrabar)
        {
            try
            {
                insolurgente cabecera = dataGrabar.cabecera;
                List<insolurgente1> detalle = dataGrabar.tablaDetalle;

                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);
                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    // antes de validar hace lo siguiente:
                    var arreglDatos = await ArreglarDescripciones(_context, codempresa, cabecera, detalle);
                    if (arreglDatos.valido == false)
                    {
                        return BadRequest(new
                        {
                            valido = false,
                            resp = arreglDatos.msg
                        });
                    }
                    detalle = arreglDatos.detalle;
                    // calcular total
                    cabecera.total = (decimal)detalle.Sum(i => i.total);

                    // calcular peso
                    double auxPeso1 = 0;
                    foreach (var reg in detalle)
                    {
                        auxPeso1 = auxPeso1 + (await items.itempeso(_context, reg.coditem) * (double)reg.cantidad);
                    }
                    cabecera.peso_pedido = (decimal?)auxPeso1;

                    List<detalleRequest> detalleComplet = detalle.Select(i => new detalleRequest{
                        codsolurgente = i.codsolurgente,
                        coditem = i.coditem,
                        descripcion = "",
                        medida = "",
                        cantidad = i.cantidad,
                        saldoag = i.saldoag,
                        stockmax = i.stockmax,
                        udm = i.udm,
                        precio = i.precio,
                        total = i.total,
                        saldodest = i.saldodest,
                        pedtotal = i.pedtotal,
                        saldoarea = i.saldoarea,
                        cantidad_pedido = i.cantidad_pedido
                    }).ToList();

                    foreach (var reg in detalleComplet)
                    {
                        reg.descripcion = await items.itemdescripcion(_context, reg.coditem);
                        reg.medida = await items.itemmedida(_context, reg.coditem);
                    }
                    return Ok(new
                    {
                        cabecera,
                        detalleComplet
                    });
                }

            }
            catch (Exception ex)
            {
                return Problem("Error en el servidor al calcular Detalle de Sol. Urgente: " + ex.Message);
                throw;
            }
        }

        [HttpPost]
        [QueueFilter(1)] // Limitar a 1 solicitud concurrente
        [Route("grabarDocumento/{userConn}/{codempresa}/{valida_aceptaSolUrg}/{solUrgcubreSaldoValido}/{solUrgVtaMaxValido}")]
        public async Task<ActionResult<object>> grabarDocumento(string userConn, string codempresa, bool valida_aceptaSolUrg, bool solUrgcubreSaldoValido, bool solUrgVtaMaxValido, requestGabrarSolUrgente dataGrabar)
        {
            insolurgente cabecera = dataGrabar.cabecera;
            List<insolurgente1> detalle = dataGrabar.tablaDetalle;
            // hacer un trim a todas las casillas
            cabecera.id = cabecera.id.Trim();
            cabecera.obs = cabecera.obs.Trim();
            cabecera.codcliente = cabecera.codcliente.Trim();
            cabecera.codmoneda = cabecera.codmoneda.Trim();
            cabecera.tpollegada = cabecera.tpollegada.Trim();
            cabecera.transporte = cabecera.transporte.Trim();
            cabecera.idnm = cabecera.idnm.Trim();
            cabecera.idfc = cabecera.idfc.Trim();
            cabecera.horareg = cabecera.horareg.Trim();
            cabecera.usuarioreg = cabecera.usuarioreg.Trim();
            cabecera.fid = cabecera.fid.Trim();
            cabecera.flete = cabecera.flete.Trim();
            cabecera.id_comple = cabecera.id_comple.Trim();
            cabecera.nomcliente = cabecera.nomcliente.Trim();
            cabecera.tipocliente = cabecera.tipocliente.Trim();
            cabecera.idproforma = cabecera.idproforma.Trim();
            cabecera.niveles_descuento = cabecera.niveles_descuento.Trim();
            if (cabecera.codtarifa == null)
            {
                cabecera.codtarifa = 0;
            }


            List<string> alertas = new List<string>();
            bool grabar_pf_sol_urgente_destino = false;
            string conexion = "";
            string nit = "";
            int codprofGrbd = 0;
            int codSolUrgente = 0;
            int nroIdSolUrgente = 0;

            try
            {
                
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);
                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    // antes de validar hace lo siguiente:
                    var arreglDatos = await ArreglarDescripciones(_context, codempresa, cabecera, detalle);
                    if (arreglDatos.valido == false)
                    {
                        return BadRequest(new
                        {
                            valido = false,
                            resp = arreglDatos.msg
                        });
                    }
                    detalle = arreglDatos.detalle;
                    // calcular total
                    cabecera.total = (decimal)detalle.Sum(i => i.total);

                    // calcular peso
                    double auxPeso1 = 0;
                    foreach (var reg in detalle)
                    {
                        auxPeso1 = auxPeso1 + (await items.itempeso(_context, reg.coditem) * (double)reg.cantidad);
                    }
                    cabecera.peso_pedido = (decimal?)auxPeso1;


                    // validacion de datos.
                    var valida1 = await validardatos(_context, cabecera, detalle);
                    if (valida1.valido == false)
                    {
                        return BadRequest(new {resp = valida1.msg});
                    }
                    var valida2 = await validardatos_2(_context, cabecera, valida_aceptaSolUrg, detalle);
                    cabecera.flete = valida2.flete;
                    if (valida2.valido == false)
                    {
                        if (valida2.pedirClave != null)
                        {
                            return StatusCode(203, new { 
                                valido = false,
                                resp = "Se requiere habilitacion con Clave.",
                                pedirClave = valida2.pedirClave,
                                alertas = valida2.msgs,
                                flete = cabecera.flete,
                            });
                        }
                        return StatusCode(203, new
                        {
                            valido = false,
                            resp = "Se encontraron las siguientes validaciones: ",
                            alertas = valida2.msgs,
                            flete = cabecera.flete,
                        });
                    }

                    var valDetalle1 = await validarDetalle(_context, codempresa, (bool)cabecera.especial, solUrgcubreSaldoValido, cabecera, detalle);
                    detalle = valDetalle1.tablaDetalle;
                    if (valDetalle1.valido == false)
                    {
                        if (valDetalle1.clave != null)
                        {
                            return StatusCode(203, new
                            {
                                valido = false,
                                resp = valDetalle1.msg,
                                pedirClave = valDetalle1.clave,
                                alertas = valDetalle1.alertas,
                                flete = cabecera.flete,
                            });
                        }
                        return StatusCode(203, new
                        {
                            valido = false,
                            resp = valDetalle1.msg,
                            alertas = valDetalle1.alertas,
                            flete = cabecera.flete,
                        });
                    }

                    var valDetalle2 = await validarDetalle_2(_context,codempresa, solUrgVtaMaxValido, cabecera, detalle);
                    if (valDetalle2.valido == false)
                    {
                        if (valDetalle2.clave != null)
                        {
                            return StatusCode(203, new
                            {
                                valido = false,
                                resp = valDetalle2.msg,
                                pedirClave = valDetalle2.clave,
                                alertas = new List<string>(),
                                flete = cabecera.flete,
                                negativos = valDetalle2.negativos,
                                maxVenta = valDetalle2.dtnocumplenMaxVta,
                            });
                        }
                        return StatusCode(203, new
                        {
                            valido = false,
                            resp = valDetalle2.msg,
                            alertas = new List<string>(),
                            flete = cabecera.flete,
                            negativos = valDetalle2.negativos,
                            maxVenta = valDetalle2.dtnocumplenMaxVta,
                        });
                    }

                    // calcular total
                    cabecera.total = (decimal)detalle.Sum(i => i.total);

                    // calcular peso
                    double auxPeso = 0;
                    foreach (var reg in detalle)
                    {
                        auxPeso = auxPeso + (await items.itempeso(_context, reg.coditem) * (double)reg.cantidad);
                    }
                    cabecera.peso_pedido = (decimal?)auxPeso;

                    var docGrabado = await guardarNuevoDocumento(_context, cabecera, detalle);
                    if (docGrabado.valido == false)
                    {
                        alertas.Add("No se pudo generar la solicitud.");
                        return BadRequest(new { valido = docGrabado.valido, resp = docGrabado.msg, alertas });
                    }
                    alertas.Add("Se genero la solicitud " + cabecera.id + " - " + docGrabado.numeroID + " con exito.");
                    // grabar la solicitud en proforma en almacen destino 15 - 10 - 2020
                    // preguntar si se puede grabar proforma de sol urgente en almacen destino
                    grabar_pf_sol_urgente_destino = await saldos.Grabar_Proforma_SolUrgente_en_Destino(_context, codempresa);
                    codSolUrgente = docGrabado.codigoSolUrg;
                    nroIdSolUrgente = docGrabado.numeroID;
                    if (grabar_pf_sol_urgente_destino)
                    {
                        // grabar la solicitud en proforma en almacen destino 15-10-2020
                        conexion = await empaque_func.Getad_conexion_vpnFromDatabase_Sam(_context, cabecera.codalmdestino);
                        nit = await cliente.NIT(_context, cabecera.codcliente);
                    }



                    
                }

                // grabar la solicitud en proforma en almacen destino 15-10-2020
                string id_proforma = "";
                int numeroIdGrbd = 0;
                bool seGraboProf = true;
                if (grabar_pf_sol_urgente_destino)
                {
                    using (var _context = DbContextFactory.Create(conexion))
                    {
                        if (conexion == null)
                        {
                            return BadRequest(new { resp = "No se pudo obtener la cadena de conexión" });
                        }

                        var grabarProf = await GrabarProformaParaAprobarEnAlmacen(_context, nit, codempresa, cabecera, detalle);
                        if (grabarProf.valido == false)
                        {
                            seGraboProf = false;
                            alertas.Add(grabarProf.msg);
                            alertas.Add("No se pudo Grabar la Proforma con Exito en Almacen");
                        }
                        codprofGrbd = grabarProf.newProf.codigo;
                        numeroIdGrbd = grabarProf.newProf.numeroid;
                        id_proforma = grabarProf.newProf.id;

                        await log.RegistrarEvento(_context, cabecera.usuarioreg, Log.Entidades.SW_Proforma, codprofGrbd.ToString(), grabarProf.newProf.id, numeroIdGrbd.ToString(), _controllerName, "Grabar Para Aprobar", Log.TipoLog.Creacion);

                        // GRabar Etiqueta
                        try
                        {
                            string telefono = await cliente.TelefonoPrincipal(_context, grabarProf.newProf.codcliente);
                            string celular = await cliente.CelularPrincipal(_context, grabarProf.newProf.codcliente);
                            string ciudad = await cliente.UbicacionCliente(_context,grabarProf.newProf.codcliente);

                            var lat_long = await cliente.latitud_longitud_cliente(_context, grabarProf.newProf.codcliente);
                            string latitud = lat_long.latitud;
                            string longitud = lat_long.longitud;

                            veetiqueta_proforma profEtiq = new veetiqueta_proforma();

                            profEtiq.id = grabarProf.newProf.id;
                            profEtiq.numeroid = grabarProf.newProf.numeroid;
                            profEtiq.codcliente = grabarProf.newProf.codcliente;
                            profEtiq.linea1 = grabarProf.newProf.nomcliente;
                            profEtiq.linea2 = "";

                            profEtiq.representante = grabarProf.newProf.direccion;
                            profEtiq.telefono = telefono;
                            profEtiq.ciudad = ciudad;
                            profEtiq.celular = celular;
                            profEtiq.latitud_entrega = latitud;

                            profEtiq.longitud_entrega = longitud;


                            // primero eliminar si es que hay una etiqueta registrada 
                            var antEtiqProf = await _context.veetiqueta_proforma
                                .Where(i => i.id == grabarProf.newProf.id && i.numeroid == grabarProf.newProf.numeroid).FirstOrDefaultAsync();
                            if (antEtiqProf != null)
                            {
                                _context.veetiqueta_proforma.Remove(antEtiqProf);
                                await _context.SaveChangesAsync();
                            }
                            _context.veetiqueta_proforma.Add(profEtiq);
                            await _context.SaveChangesAsync();


                            //            ' %%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
                            //            '//realizar la reserva de la mercaderia
                            await ventas.aplicarstocksproforma(_context, codprofGrbd, codempresa);
                            //            ' %%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
                        }
                        catch (Exception)
                        {
                            throw;
                        }
                    }

                    // ACTUALIZAR codigo de proforma de almacen grabada en la solicitud urgente
                    if (seGraboProf)
                    {
                        using (var _context = DbContextFactory.Create(userConnectionString))
                        {
                            insolurgente datos = await _context.insolurgente.Where(i => i.codigo == codSolUrgente).FirstOrDefaultAsync();
                            datos.codproforma_almacen = codprofGrbd;
                            await _context.SaveChangesAsync();

                            await log.RegistrarEvento(_context, cabecera.usuarioreg, Log.Entidades.SW_Ventana, "", cabecera.id, nroIdSolUrgente.ToString(), _controllerName, "Grabar", Log.TipoLog.Creacion);
                            alertas.Add("Se grabo la Proforma " + id_proforma + "-" + numeroIdGrbd + " para aprobar con Exito en Almacen: " + cabecera.codalmdestino);
                        }
                    }
                }
                return Ok(new
                {
                    alertas = alertas,
                    infracciones,
                });
            }
            catch (Exception ex)
            {
                return Problem("Error en el servidor al guardar Sol. Urgente: " + ex.Message);
                throw;
            }

        }


        private async Task<(bool valido, string msg, List<insolurgente1> detalle)> ArreglarDescripciones(DBContext _context, string codempresa, insolurgente cabecera, List<insolurgente1> detalle)
        {
            int codproforma = 0;
            try
            {
                codproforma = await ventas.codproforma(_context, cabecera.idproforma, cabecera.numeroidproforma ?? 0);
            }
            catch (Exception)
            {
                codproforma = 0;
                return (false, "No ha especificado ninguna proforma, desea continuar?", detalle);
            }
            foreach (var reg in detalle)
            {
                // ##el precio volver a calcular
                reg.precio = await tipoCambio._conversion(_context, await Empresa.monedabase(_context, codempresa), await ventas.monedabasetarifa(_context, cabecera.codtarifa), DateTime.Now.Date, (decimal)await ventas.preciocliente(_context, cabecera.codcliente, (await cliente.almacen_de_cliente_Integer(_context, cabecera.codcliente)), cabecera.codtarifa, reg.coditem, "NO", "Z", (await cliente.Opcion_Niveles_Descuento_Proforma(_context, codproforma))));
                reg.total = reg.cantidad * reg.precio;
                // ###fin de calcular

                /*
                    tabladetalle.Rows(i)("descripcion") = sia_funciones.Items.Instancia.itemdescripcion(tabladetalle.Rows(i)("coditem"))
                    tabladetalle.Rows(i)("medida") = sia_funciones.Items.Instancia.itemmedida(tabladetalle.Rows(i)("coditem"))
                 */

                try
                {
                    if (await empresa.ControlarStockSeguridad_context(_context, codempresa))
                    {
                        if (await ventas.Reservar_Para_Tiendas_En_Sol_Urgentes(_context, cabecera.codtarifa))
                        {
                            reg.saldoag = await saldos.saldoitem_crtlstock(_context, codempresa, reg.coditem, cabecera.codalmacen, true, cabecera.usuarioreg);
                        }
                        else
                        {
                            reg.saldoag = await saldos.saldoitem_crtlstock(_context, codempresa, reg.coditem, cabecera.codalmacen, false, cabecera.usuarioreg);
                        }
                    }
                    else
                    {
                        reg.saldoag = await saldos.saldoitem_crtlstock(_context, codempresa, reg.coditem, cabecera.codalmacen, false, cabecera.usuarioreg);
                    }
                }
                catch (Exception)
                {

                }
            }
            return (true, "", detalle);
        }

        private async Task<(bool valido, string msg)> validardatos(DBContext _context, insolurgente cabecera, List<insolurgente1> detalle)
        {
            if (string.IsNullOrWhiteSpace(cabecera.id))
            {
                return (false, "No puede dejar el tipo de pedido en blanco.");
            }
            if (cabecera.codalmacen == null || cabecera.codalmacen <= 0)
            {
                return (false, "No puede dejar la casilla de Almacen en blanco.");
            }
            if (cabecera.codalmdestino == null || cabecera.codalmdestino <= 0)
            {
                return (false, "No puede dejar la casilla de Almacen Destino en blanco.");
            }
            if (cabecera.codalmacen == cabecera.codalmdestino)
            {
                return (false, "No puede hacer un pedido al mismo almacen en que se encuentra.");
            }
            if (cabecera.codvendedor == null || cabecera.codvendedor <= 0)
            {
                return (false, "No puede dejar la casilla de Vendedor en blanco.");
            }
            if (string.IsNullOrWhiteSpace(cabecera.codcliente))
            {
                return (false, "No puede dejar la casilla de Cliente en blanco.");
            }
            if (cabecera.codtarifa == null || cabecera.codtarifa <= 0)
            {
                return (false, "No puede dejar la casilla de Tarifa en blanco.");
            }
            if (cabecera.totalventa == null || cabecera.totalventa <= 0)
            {
                return (false, "No puede dejar la casilla de total de venta en blanco.");
            }
            if (string.IsNullOrWhiteSpace(cabecera.codmoneda))
            {
                return (false, "No puede dejar la casilla de Moneda en blanco.");
            }
            if (string.IsNullOrWhiteSpace(cabecera.transporte))
            {
                return (false, "No puede dejar la casilla de Transporte en blanco.");
            }
            if (string.IsNullOrWhiteSpace(cabecera.tpollegada))
            {
                return (false, "No puede dejar la casilla de Tiempo de Llegada en blanco.");
            }
            if (string.IsNullOrWhiteSpace(cabecera.obs))
            {
                return (false, "No puede dejar la casilla de Observacion en blanco.");
            }
            if (string.IsNullOrWhiteSpace(cabecera.nomcliente))
            {
                return (false, "No puede dejar la casilla de nombre del cliente en blanco.");
            }
            if (cabecera.nomcliente.Trim().Replace(" ", "")  == "SINNOMBRE")
            {
                return (false, "Debe indicar un nombre del cliente para el cual se esta realizando la solicitud.");
            }
            if (string.IsNullOrWhiteSpace(cabecera.tipocliente))
            {
                return (false, "Debe indicar el tipo de cliente.");
            }

            if (await seguridad.periodo_fechaabierta_context(_context, cabecera.fecha.Date, 2) == false)
            {
                return (false, "No puede crear documentos para ese periodo de fechas.");
            }

            if (detalle.Count() <= 0)
            {
                return (false, "No tiene ningun item en la solicitud urgente.");
            }
            
            return (true, "");
        }

        private async Task<(bool valido, List<string> msgs, string flete, object ? pedirClave)> validardatos_2(DBContext _context, insolurgente cabecera, bool valida_aceptaSolUrg, List<insolurgente1> detalle)
        {
            int codProforma = 0;
            if (cabecera.idproforma.Trim() != "")
            {
                codProforma = await ventas.codproforma(_context, cabecera.idproforma, cabecera.numeroidproforma ?? 0);
            }
            List<string> mensajes = new List<string>();
            //////////////////////////// VER SI SE VALIDARA EN OTRO LADO, DEVUELVE OTRO MENSAJE MAS
            var validoMaxVta = await MaximoDeVenta(_context, cabecera.codalmacen, cabecera.codtarifa, cabecera.codcliente, cabecera.fecha, detalle);
            if (validoMaxVta.valido == false)
            {
                mensajes.Add(validoMaxVta.msg);
                mensajes.Add("El Documento esta vendiendo cantidades mayores a las permitidas para un solo cliente.");
                return (false, mensajes, cabecera.flete, null);
            }
            var validaMontTrans = await ValidarMontoTransporte(_context, cabecera.codalmacen, cabecera.codalmdestino, cabecera.codtarifa, cabecera.codcliente, cabecera.fecha, cabecera.codmoneda, cabecera.total, (double)cabecera.peso_pedido, cabecera.transporte, detalle);
            cabecera.flete = validaMontTrans.flete;
            if (validaMontTrans.valido == false)
            {
                mensajes.Add("La solicitud no cumple el minimo requerido, se grabara como solicitud no valida, para validarla añada solicitudes complementarias.");
            }
            if (cabecera.idproforma.Trim() != "")
            {
                if (await documento.existe_proforma(_context,cabecera.idproforma, cabecera.numeroidproforma ?? 0))
                {
                    // Desde 19/09/2024 Validar que en la factura, proforma y solicitud urgente tengan el mismo codigo de cliente
                    if (await ventas.Cliente_De_Proforma(_context, codProforma) != cabecera.codcliente)
                    {
                        mensajes.Add("El cliente de la Proforma enlazada no es el mismo codigo de cliente de la solicitud, lo cual no esta permitido, verifique esta situacion.");
                        return (false, mensajes, cabecera.flete, null);
                    }
                }
                else
                {
                    mensajes.Add("La proforma indicada no esta registrada.");
                    return (false, mensajes, cabecera.flete, null);
                }
            }

            // verificar a que precio esta la proforma y comprar con la solicitrud urgente si esta al mismo precio
            int codtarifa_proforma = await ventas.ProformaPrimeraTarifa(_context, codProforma);
            if (cabecera.codtarifa != codtarifa_proforma)
            {
                mensajes.Add("La proforma a la que hace referencia esta a precio: " + codtarifa_proforma + " y el precio al cual realiza la solicitu urgente es precio: " + cabecera.codtarifa + ", ambas deben ser al mismo precio!!!");
                return (false, mensajes, cabecera.flete, null);
            }

            if (cabecera.total < await inventario.MinimoAlmacen(_context, cabecera.codalmacen, cabecera.codmoneda, cabecera.fecha.Date))
            {
                mensajes.Add("La solicitud no cumple el minimo requerido de solicitud urgente.");
                cabecera.flete = "SOLICITUD NO VALIDA POR MINIMO DE SOLICITUD";
                if (valida_aceptaSolUrg == false)
                {
                    string codclientedescripcion = await cliente.Razonsocial(_context, cabecera.codcliente);
                    object clave = new
                    {
                        identificador = "23E",
                        valido = false,
                        resp = "La agencia puede cubrir el saldo de su solicitud urgente, Para eso necesita una autorizacion especial.",
                        servicio = 23,
                        descServicio = "ACEPTAR SOL. URGENTE AUNQUE LA AGENCIA PUEDA CUBRIR",
                        datosDoc = cabecera.id + "-" + cabecera.numeroid + ": " + cabecera.codcliente + "-" + codclientedescripcion + " Total:" + cabecera.total,
                        datoA = cabecera.id,
                        datoB = cabecera.numeroid
                    };

                    return (false, mensajes, cabecera.flete, clave);
                }
                
            }



            return (true, mensajes, cabecera.flete, null);
        }


        private async Task<(bool valido, string msg)> MaximoDeVenta(DBContext _context, int codalmacen, int codtarifa, string codcliente, DateTime fecha, List<insolurgente1> detalle)
        {
            double max = 0;
            int diascontrol = 0;
            bool resultado = true;

            foreach (var reg in detalle)
            {
                max = await items.MaximoDeVenta(_context, reg.coditem, codalmacen.ToString(), codtarifa);
                if (max > 0)  // si controla
                {
                    diascontrol = await items.MaximoDeVenta_PeriodoDeControl(_context, reg.coditem, codalmacen.ToString(), codtarifa);
                    if (diascontrol > 0)
                    {
                        double aux = await cliente.CantidadVendida(_context, reg.coditem, codcliente, fecha.Date.AddDays(-diascontrol), fecha.Date) + (double)(reg.cantidad);
                        if (aux <= max)
                        {
                            // nada
                        }
                        else
                        {
                            return (false, "Se sobrepasara el maximo de venta del item " + reg.coditem + " en " + diascontrol + " dias");
                        }
                    }
                }
            }
            return (true, "");
        }

        private async Task<(bool valido, string flete)> ValidarMontoTransporte(DBContext _context, int codalmacen, int codalmdestino, int codtarifa, string codcliente, DateTime fecha, string codmoneda_total, decimal total, double pesoTotal, string transporte, List<insolurgente1> detalle)
        {
            double MontoMin = 0;
            double PesoMin = 0;
            bool resultado = true;
            string flete = "";

            // verificar si son de la misma area
            if (await almacen.AreaAlmacen(_context,codalmacen) == await almacen.AreaAlmacen(_context, codalmdestino))
            {
                // ###########################
                // #   EN LA MISMA AREA
                // ###########################
                MontoMin = await almacen.MontoMinimoAlmacen(_context, codalmacen, codtarifa, true, codmoneda_total);
                PesoMin = await almacen.PesoMinimoAlmacen(_context, codalmacen, codtarifa, true);
                if (MontoMin > 0) // por monto
                {
                    if ((double)total < MontoMin)
                    {
                        // ####->poner flete paga el cliente
                        flete = "FLETE PAGADO POR EL CLIENTE";
                    }
                    else
                    {
                        // ####->poner flete paga la empresa
                        flete = "FLETE PAGADO POR LA EMPRESA";
                    }
                }
                else // por peso
                {
                    if (pesoTotal < PesoMin)
                    {
                        // ####->poner flete paga el cliente
                        flete = "FLETE PAGADO POR EL CLIENTE";
                    }
                    else
                    {
                        // ####->poner flete paga la empresa
                        flete = "FLETE PAGADO POR LA EMPRESA";
                    }
                }
            }
            else
            {
                // ###########################
                // #   A OTRAS AREAS
                // ###########################
                MontoMin = await almacen.MontoMinimoAlmacen(_context,codalmacen,codtarifa,false,codmoneda_total);
                PesoMin = await almacen.PesoMinimoAlmacen(_context, codalmacen, codtarifa, false);
                if (pesoTotal >= PesoMin || (double)total >= MontoMin)
                {
                    if (codtarifa == 1 || codtarifa == 4 || codtarifa == 7) // precio 1 (mayorista)
                    {
                        if (transporte == "TRANSPORTADORA")
                        {
                            flete = "FLETE PAGADO POR LA EMPRESA";
                        }
                        else
                        {
                            flete = "FLETE PAGADO POR EL CLIENTE";
                        }
                    }
                    else  // otros precios 2 y 3
                    {
                        if (await itemsEspeciales(_context,detalle))
                        {
                            if (transporte == "TRANSPORTADORA")
                            {
                                flete = "FLETE PAGADO POR LA EMPRESA";
                            }
                            else
                            {
                                flete = "FLETE PAGADO POR EL CLIENTE";
                            }
                        }
                        else
                        {
                            flete = "FLETE PAGADO POR LA EMPRESA";
                        }
                    }
                }
                else
                {
                    resultado = false; // por que en areas si hay minimo
                    flete = "SOLICITUD NO VALIDA  ";
                }
            }
            return (resultado, flete);
        }


        private async Task<bool> itemsEspeciales(DBContext _context, List<insolurgente1> detalle)
        {
            bool resultado = false;
            foreach (var reg in detalle)
            {
                if (await EstaRestringido(_context,reg.coditem))
                {
                    resultado = true;
                    break;
                }
            }
            return resultado;
        }
        private async Task<bool> EstaRestringido(DBContext _context, string coditem)
        {
            bool resultado = false;
            int codgrupo = await items.itemgrupo(_context, coditem);
            switch (codgrupo)
            {
                case 11:
                    resultado = true; 
                    break;
                case 13:
                    resultado = true;
                    break;
                case 25:
                    resultado = true;
                    break;
                case 4:
                    resultado = true;
                    break;
                default:
                    resultado = false;
                    break;
            }
            return resultado;
        }


        private async Task<(bool valido, string msg, object? clave, List<string> alertas, List<insolurgente1> tablaDetalle)> validarDetalle(DBContext _context, string codempresa, bool checkEspecial, bool solUrgcubreSaldoValido, insolurgente cabecera, List<insolurgente1> tablaDetalle)
        {
            List<string> alertas = new List<string>();
            int indice = 1;
            foreach (var reg in tablaDetalle)
            {
                if (string.IsNullOrWhiteSpace(reg.coditem))
                {
                    return (false, "No eligio El Item en la Linea " + indice + " .", null, alertas, tablaDetalle);
                }
                if (reg.coditem.Trim().Length < 1)
                {
                    return (false, "No eligio El Item en la Linea " + indice + " .", null, alertas, tablaDetalle);
                }

                if (string.IsNullOrWhiteSpace(reg.udm))
                {
                    return (false, "No puso la Unidad de Medida en la Linea " + indice + " .", null, alertas, tablaDetalle);
                }
                if (reg.udm.Trim().Length < 1)
                {
                    return (false, "No puso la Unidad de Medida en la Linea " + indice + " .", null, alertas, tablaDetalle);
                }

                if (reg.cantidad == null)
                {
                    return (false, "No puso la cantidad en la Linea " + indice + " .", null, alertas, tablaDetalle);
                }
                if (reg.cantidad <= 0)
                {
                    return (false, "La cantidad en la Linea " + indice + " No puede ser menor o igual a 0.", null, alertas, tablaDetalle);
                }

                if (reg.cantidad_pedido == null)
                {
                    return (false, "No puso la cantidad de pedido total en la Linea " + indice + " .", null, alertas, tablaDetalle);
                }
                if (reg.cantidad_pedido <= 0)
                {
                    return (false, "La cantidad de pedido total en la Linea " + indice + " No puede ser menor o igual a 0.", null, alertas, tablaDetalle);
                }
                indice++;
            }

            // validaciones con consultas a base de datos

            int area = await almacen.AreaAlmacen(_context, cabecera.codalmacen);
            List<int> almacenes_de_area = await almacen.Almacenes_del_Area(_context, area);
            foreach (var reg in tablaDetalle)
            {
                if (await empresa.ControlarStockSeguridad_context(_context,codempresa))
                {
                    if (await ventas.Reservar_Para_Tiendas_En_Sol_Urgentes(_context,cabecera.codtarifa))
                    {
                        reg.saldodest = await saldos.saldoitem_crtlstock(_context, codempresa, reg.coditem, cabecera.codalmdestino, true, cabecera.usuarioreg);
                    }
                    else
                    {
                        reg.saldodest = await saldos.saldoitem_crtlstock(_context, codempresa, reg.coditem, cabecera.codalmdestino, false, cabecera.usuarioreg);
                    }
                }
                else
                {
                    reg.saldodest = await saldos.saldoitem_crtlstock(_context, codempresa, reg.coditem, cabecera.codalmdestino, false, cabecera.usuarioreg);
                }

                // adicionar saldo de area
                double total_area = 0;
                string alerta = "";
                if (await ventas.Reservar_Para_Tiendas_En_Sol_Urgentes(_context, cabecera.codtarifa))
                {
                    foreach (var reg2 in almacenes_de_area)
                    {
                        double saldo_ag_area = 0;
                        if (reg2 == cabecera.codalmacen)
                        {
                            saldo_ag_area = 0;
                        }
                        else
                        {
                            try
                            {
                                saldo_ag_area = total_area + (double)await saldos.saldoitem_crtlstock(_context, codempresa, reg.coditem, reg2, true, cabecera.usuarioreg);
                            }
                            catch (Exception)
                            {
                                saldo_ag_area = 0;
                                alerta = "NO hay saldo para ag: " + reg2.ToString();
                            }
                        }
                        total_area += saldo_ag_area;
                    }
                    reg.saldoarea = (decimal?)total_area;
                }
                else
                {
                    foreach (var reg2 in almacenes_de_area)
                    {
                        double saldo_ag_area = 0;
                        if (reg2 == cabecera.codalmacen)
                        {
                            saldo_ag_area = 0;
                        }
                        else
                        {
                            try
                            {
                                total_area = total_area + (double)await saldos.saldoitem_crtlstock(_context, codempresa, reg.coditem, reg2, false, cabecera.usuarioreg);
                            }
                            catch (Exception)
                            {
                                saldo_ag_area = 0;
                                alerta = "NO hay saldo para ag: " + reg2.ToString();
                            }
                        }
                        total_area += saldo_ag_area;
                    }
                    reg.saldoarea = (decimal?)total_area;
                }
            }

            infracciones.Clear();
            int i = 1;
            
            foreach (var reg in tablaDetalle)
            {
                var pedidoValido = await validarpedido(_context, (decimal)reg.saldoag, (decimal)reg.cantidad, i, reg.coditem, (decimal)reg.saldodest, (decimal)reg.saldoarea, checkEspecial, solUrgcubreSaldoValido, cabecera);
                if (pedidoValido.msg != "")
                {
                    alertas.Add(pedidoValido.msg);
                }
                if (pedidoValido.valido == false)
                {
                    return (false, pedidoValido.msg, pedidoValido.clave, alertas, tablaDetalle);
                }
                i++;
            }

            return (true, "",null,alertas, tablaDetalle);
        }

        private async Task<(bool valido, string msg, object? clave, List<Dtnegativos>? negativos, List<Dtnocumplen>? dtnocumplenMaxVta)> validarDetalle_2(DBContext _context, string codempresa, bool solUrgVtaMaxValido, insolurgente cabecera, List<insolurgente1> tablaDetalle)
        {
            // validar si el pedido al almacen destino genera negativos
            var validaNegs = await Validar_Saldos_Negativos_Doc(_context, false, cabecera.codalmdestino, codempresa, cabecera.usuarioreg, tablaDetalle);
            if (validaNegs.valido == false)
            {
                return (false, validaNegs.msg, null, validaNegs.negativos, null);
            }

            var validaMaxVta = await Validar_Max_Vta(_context, cabecera.id, cabecera.numeroid, codempresa, cabecera.usuarioreg, cabecera.codtarifa, cabecera.codcliente, cabecera.codalmdestino, cabecera.codalmacen, cabecera.fecha, true, solUrgVtaMaxValido, tablaDetalle);
            if (validaMaxVta.valido == false)
            {
                return (false, validaMaxVta.msg, validaMaxVta.clave, null, validaMaxVta.dtnocumplenMaxVta);
            }
            return (true, "", null, null, null);
        }

        List<string> infracciones = new List<string>();

        private async Task<(bool valido, string msg, object? clave)> validarpedido(DBContext _context, decimal saldoag, decimal cantidad, int linea, string item, decimal saldodest, decimal saldoarea, bool checkEspecial, bool solUrgcubreSaldoValido, insolurgente cabecera)
        {
            string alerta = "";
            if (checkEspecial)
            {
                if (cantidad < (saldoag * (decimal)0.5))
                {
                    // la agencia puede cubrir el pedido
                    infracciones.Add(linea.ToString() + " " + item + " Su agencia puede cubrir el pedido.");
                    alerta = linea.ToString() + " " + item + " Su pedido es menor al 50% de su saldo actual, Su agencia puede cubrir este pedido especial.";
                    if (solUrgcubreSaldoValido == false)
                    {
                        string codclientedescripcion = await cliente.Razonsocial(_context, cabecera.codcliente);
                        object clave = new
                        {
                            identificador = "23U",
                            valido = false,
                            resp = "La agencia puede cubrir el saldo de su solicitud urgente, Para eso necesita una autorizacion especial.",
                            servicio = 23,
                            descServicio = "ACEPTAR SOL. URGENTE",
                            datosDoc = cabecera.id + "-" + cabecera.numeroid + " " + cabecera.codcliente + "-" + codclientedescripcion,
                            datoA = cabecera.id,
                            datoB = cabecera.numeroid
                        };

                        return (false, alerta, clave);
                    }
                }
            }
            else
            {
                if (cantidad < (saldoag * (decimal)0.9))
                {
                    // la agencia puede cubrir el pedido
                    infracciones.Add(linea.ToString() + " " + item + " Su agencia puede cubrir el pedido.");
                    alerta = linea.ToString() + " " + item + " Su pedido es menor al 90% de su saldo actual, Su agencia puede cubrir este pedido especial.";
                    if (solUrgcubreSaldoValido == false)
                    {
                        string codclientedescripcion = await cliente.Razonsocial(_context, cabecera.codcliente);
                        object clave = new
                        {
                            identificador = "23U",
                            valido = false,
                            resp = "La agencia puede cubrir el saldo de su solicitud urgente, Para eso necesita una autorizacion especial.",
                            servicio = 23,
                            descServicio = "ACEPTAR SOL. URGENTE",
                            datosDoc = cabecera.id + "-" + cabecera.numeroid + " " + cabecera.codcliente + "-" + codclientedescripcion,
                            datoA = cabecera.id,
                            datoB = cabecera.numeroid
                        };

                        return (false, alerta, clave);
                    }
                }
            }


            if (saldoag < 0)
            {
                infracciones.Add(linea.ToString() + " " + item + " el saldo es negativo, debe ser corregido antes de realizar el pedido.");
            }
            else
            {
                if (saldoag >= cantidad)
                {
                    // la agencia puede cubrir el pedido
                    infracciones.Add(linea.ToString() + " " + item + " Su agencia puede cubrir el pedido.");
                }
                else
                {
                    if (cabecera.codtarifa == 1 || cabecera.codtarifa == 4 || cabecera.codtarifa == 7)
                    {
                        // precio 1 o mayorista
                        if (cantidad > saldodest)
                        {
                            // en la agencia destino no alcanza
                            infracciones.Add(linea.ToString() + " " + item + "La agencia destino no tiene suficiente mercaderia para abastecer el pedido .");
                        }
                        else
                        {
                            if (saldodest > await saldos.stockminimo_item(_context,cabecera.codalmdestino,item))
                            {
                                // nada
                            }
                            else
                            {
                                // en la gencia destino el stock minimo esta al limite 
                                infracciones.Add(linea.ToString() + " " + item + "En la agencia destino el saldo esta revervado para el Stock Minimo Propio.");
                            }
                        }
                    }
                    else
                    {
                        // precio 2 y 3
                        if (cantidad < saldodest)
                        {
                            // nada
                        }
                        else
                        {
                            cantidad = cantidad - saldoag;
                            if (cantidad < saldodest)
                            {
                                // nada
                            }
                            else
                            {
                                cantidad = saldodest;
                                if (cantidad > 0)
                                {
                                    // nada
                                }
                                else
                                {
                                    // esta pidiendo cero
                                    infracciones.Add(linea.ToString() + " " + item + " no se podra abastecer ese item en la agencia destino.");
                                }
                            }
                        }
                    }
                }
            }

            // verificacion especial
            if (await almacen.AreaAlmacen(_context, cabecera.codalmacen) == await almacen.AreaAlmacen(_context,cabecera.codalmdestino))
            {
                // misma area
            }
            else
            {
                // entre areas
                if ((saldoarea + saldoag) >= cantidad)
                {
                    // su area puede abastecer
                    infracciones.Add(linea.ToString() + " " + item + " En su area se puede abastecer el pedido , no es necesario pedirlo a otra area para el item de la linea.");
                }
                else
                {
                    if (cantidad > saldodest)
                    {
                        // en la agencia destino no alcanza
                        infracciones.Add(linea.ToString() + " " + item + " En la agencia destino no alcanza la mercaderia para poder cubrir su pedido en la linea.");
                    }
                    else
                    {
                        if (saldodest < (await saldos.stockminimo_item(_context,cabecera.codalmdestino,item) * (decimal)1.2))
                        {
                            // en la agencia destino elsaldo esta muy cerca al stock minimo
                            infracciones.Add(linea.ToString() + " " + item + " En la agencia destino el saldo esta muy cerca al Stock Minimo.");
                        }
                    }
                }
            }
            return (true, alerta, null);
        }


        private async Task<(string msg, bool valido, List<Dtnegativos>? negativos)> Validar_Saldos_Negativos_Doc(DBContext _context, bool alertar_si_no_hay_negativos, int codalmdestino, string cod_empresa, string usuario, List<insolurgente1> tabladetalle)
        {
            List<string> msgs = new List<string>();
            List<string> negs = new List<string>();

            string alerta = "";
            bool resultado = true;
            if (tabladetalle.Count() > 0)
            {
                List<itemDataMatriz> detalleConvertido = tabladetalle.Select(i => new itemDataMatriz
                {
                    coditem = i.coditem,
                    cantidad = (double)i.cantidad,
                    udm = i.udm,
                    cantidad_pedida = (double)i.cantidad

                }).ToList();
                List<Dtnegativos> dtnegativos = await saldos.ValidarNegativosDocVenta(_context, detalleConvertido, codalmdestino, "", 0, msgs, negs, cod_empresa, usuario);

                foreach (var reg in dtnegativos)
                {
                    if (reg.obs == "Genera Negativo")
                    {
                        if (reg.cantidad_conjunto > 0)
                        {
                            negs.Add(reg.coditem_cjto);
                        }
                        if (reg.cantidad_suelta > 0)
                        {
                            negs.Add(reg.coditem_suelto);
                        }
                    }
                }
                if (negs.Count() == 0)
                {
                    if (alertar_si_no_hay_negativos)
                    {
                        alerta = "Ningun item del documento genera negativos.";
                        resultado = true;
                    }
                }
                else
                {
                    resultado = false;
                    alerta = "Los items detallados en la pestaña: 'Saldos Negativos' generaran saldos negativos, verifique esta situacion!!!";
                    return (alerta, resultado, dtnegativos);
                }
            }
            return (alerta, resultado, null);
        }
        private async Task<(string msg, bool valido, List<Dtnocumplen>? dtnocumplenMaxVta, object? clave)> Validar_Max_Vta(DBContext _context, string id, int numeroid, string codempresa, string usuario, int codtarifa, string codcliente, int codalmdestino, int codalmacen, DateTime fecha, bool pedir_clave, bool solUrgVtaMaxValido, List<insolurgente1> tabladetalle)
        {
            bool resultado = true;
            string alerta = "";

            List<itemDataMatrizMaxVta> dt_detalle_item = new List<itemDataMatrizMaxVta> ();

            int diascontrol = await configuracion.Dias_Proforma_Vta_Item_Cliente(_context, codempresa);
            string valida_nr_pf = await configuracion.Valida_Maxvta_NR_PF(_context, codempresa);


            foreach (var reg in tabladetalle)
            {
                // obtiene los items y la cantidad en proformas en los ultimos X dias mas lo que se quiere vender a ahora
                double cantidad_ttl_vendida_pf = 0;
                double cantidad_ttl_vendida_pf_actual = (double)reg.cantidad;
                double cantidad_ttl_vendida_pf_total = 0;

                if (diascontrol > 0 && reg.cantidad > 0)
                {
                    if (valida_nr_pf == "PF")
                    {
                        cantidad_ttl_vendida_pf = (double)await cliente.CantidadVendida_PF(_context, reg.coditem, codcliente, fecha.Date.AddDays(-diascontrol), fecha.Date);
                    }
                    else if(valida_nr_pf == "NR")
                    {
                        cantidad_ttl_vendida_pf = (double)await cliente.CantidadVendida_NR(_context, reg.coditem, codcliente, fecha.Date.AddDays(-diascontrol), fecha.Date);
                    }
                    else
                    {
                        cantidad_ttl_vendida_pf = (double)await cliente.CantidadVendida_PF(_context, reg.coditem, codcliente, fecha.Date.AddDays(-diascontrol), fecha.Date);
                    }
                    
                    cantidad_ttl_vendida_pf_total = cantidad_ttl_vendida_pf + cantidad_ttl_vendida_pf_actual;

                    // aqui verificar si la cantidad sumada con las pf q hubieran y la pf actual sobrepasan el porcentaje o no
                    // VALIDAR PORCENTAJE MAXIMO DE VENTA
                    // poner la cantidad del item obtenido de la suma de la cantidad actual de la pf y de las pf q hubiera
                    dt_detalle_item.Add(new itemDataMatrizMaxVta
                    {
                        coditem = reg.coditem,
                        cantidad = (double)reg.cantidad,
                        udm = reg.udm,
                        total = (double)reg.total,
                        codtarifa = codtarifa,
                        cantidad_pf_anterior = cantidad_ttl_vendida_pf,
                        cantidad_pf_total = cantidad_ttl_vendida_pf_total
                    });
                }
                else
                {
                    cantidad_ttl_vendida_pf_total = cantidad_ttl_vendida_pf + cantidad_ttl_vendida_pf_actual;
                    dt_detalle_item.Add(new itemDataMatrizMaxVta
                    {
                        coditem = reg.coditem,
                        cantidad = (double)reg.cantidad,
                        udm = reg.udm,
                        total = (double)reg.total,
                        codtarifa = codtarifa,
                        cantidad_pf_anterior = cantidad_ttl_vendida_pf,
                        cantidad_pf_total = cantidad_ttl_vendida_pf_total
                    });
                }
            }

            List<Dtnocumplen> dtnocumplen = await restricciones.ValidarMaximoPorcentajeDeMercaderiaVenta(_context, codcliente, false, dt_detalle_item, codalmdestino, await hardCoded.MaximoPorcentajeDeVentaPorMercaderia(_context, codalmacen), "", 0, codempresa, usuario);
            string cadena_items = "";
            dt_detalle_item.Clear();

            foreach (var reg in dtnocumplen)
            {
                if (reg.obs != "Cumple")
                {
                    resultado = false;
                    cadena_items = reg.codigo + "|";
                }
            }

            if (resultado == false)
            {
                alerta = "La solicitud urgente tiene cantidades que superan el porcentaje maximo de venta en el almacen destino, verifique esta situacion!!!";
                if (pedir_clave && solUrgVtaMaxValido == false)
                {
                    string codclientedescripcion = await cliente.Razonsocial(_context, codcliente);
                    object clave = new
                    {
                        identificador= "20U",
                        valido = false,
                        resp = alerta,
                        servicio = 20,
                        descServicio = "ACEPTAR PROFORMAS QUE PASAN EL MAXIMO % DE VENTA PERMITIDO",
                        datosDoc = id + "-" + numeroid + "|" + codcliente + "-" + codclientedescripcion + "|" + cadena_items,
                        datoA = codcliente,
                        datoB = cadena_items
                    };
                    return (alerta, false, dtnocumplen, clave);
                }
                return (alerta, false, dtnocumplen, null);
            }


            return (alerta, true, null, null);
        }


        private async Task<(bool valido, string msg, int codigoSolUrg, int numeroID)> guardarNuevoDocumento(DBContext _context, insolurgente insolurgente, List<insolurgente1> insolurgente1)
        {
            int codSolUrg = 0;
            using (var dbContexTransaction = _context.Database.BeginTransaction())
            {
                try
                {
                    insolurgente.numeroid = await documento.solurgentenumeroid(_context, insolurgente.id) + 1;
                    if (insolurgente.numeroid <= 0)
                    {
                        return (false, "Error al generar numero ID, consulte con el Administrador", 0, 0);
                    }
                    if (await documento.existe_solurgente(_context, insolurgente.id, insolurgente.numeroid))
                    {
                        return (false, "Ese numero de documento, ya existe, por favor consulte con el administrador del sistema.", 0, 0);
                    }

                    // agregar cabecera
                    try
                    {
                        _context.insolurgente.Add(insolurgente);
                        await _context.SaveChangesAsync();
                    }
                    catch (Exception ex)
                    {
                        return (false, "Error al grabar la cabecera del Pedido: " + ex.Message, 0, 0);
                    }
                    codSolUrg = insolurgente.codigo;

                    // actualiza numero id
                    var numeracion = _context.intiposolurgente.FirstOrDefault(n => n.id == insolurgente.id);
                    numeracion.nroactual += 1;
                    await _context.SaveChangesAsync(); // Guarda los cambios en la base de datos


                    int validaCantProf = await _context.insolurgente.Where(i => i.id == insolurgente.id && i.numeroid == insolurgente.numeroid).CountAsync();
                    if (validaCantProf > 1)
                    {
                        return (false, "Se detecto más de un número del mismo documento, por favor consulte con el administrador del sistema.", 0, 0);
                    }

                    // guarda detalle (veproforma1)
                    // actualizar codigoNM para agregar
                    insolurgente1 = insolurgente1.Select(p => { p.codsolurgente = codSolUrg; return p; }).ToList();
                    // guardar detalle
                    _context.insolurgente1.AddRange(insolurgente1);
                    await _context.SaveChangesAsync();
                    dbContexTransaction.Commit();
                }
                catch (Exception ex)
                {
                    dbContexTransaction.Rollback();
                    return (false, $"Error en el servidor al guardar Pedido: {ex.Message}", 0, 0);
                    throw;
                }
            }
            return (true, "", codSolUrg, insolurgente.numeroid);
        }



        // GRABADO EN ALMACEN DESTINO
        private async Task<(bool valido, string msg, veproforma? newProf)> GrabarProformaParaAprobarEnAlmacen(DBContext _context, string nit, string codempresa, insolurgente cabecera, List<insolurgente1> detalle)
        {
            int codProforma = 0;

            // obtener codcliente sin nombre id y numeroid actual de proforma
            string codcliente_sin_nombre = await _context.vecliente_urgente.Where(i => i.codalmacen == cabecera.codalmacen).Select(i => i.codcliente).FirstOrDefaultAsync();
            string nomcliente = cabecera.nomcliente;

            string direccion = await almacen.direccionalmacen(_context, cabecera.codalmacen);
            direccion = direccion + " (" + await cliente.PuntoDeVentaCliente_Segun_Direccion(_context,codcliente_sin_nombre, direccion) + ")";


            // estos dos datos por vpn segun codalmacendestino
            string id_proforma = await _context.venumeracion.Where(i => i.codalmacen == cabecera.codalmacen).Select(i => i.id).FirstOrDefaultAsync();
            int idnroactual = await _context.venumeracion.Where(i => i.id == id_proforma).Select(i => i.nroactual).FirstOrDefaultAsync();
            idnroactual = idnroactual + 1;
            decimal tdc = await tipoCambio._tipocambio(_context, await Empresa.monedabase(_context, codempresa), cabecera.codmoneda, cabecera.fecha);
            string obs_proforma = "SOLICITUD URGENTE: " + cabecera.id + "-" + cabecera.numeroid + " de AG: " + cabecera.codalmacen;

            // fin de obtener id actual

            // datos iniciales
            string hora_inicial = await funciones.hora_del_servidor_cadena(_context);
            DateTime fecha_inicial = await funciones.FechaDelServidor(_context);
            fecha_inicial = fecha_inicial.Date;

            bool contra_entrega = true; // como es tienda, toda la vida va a ser contado contraentrega.
            bool para_aprobar = true;
            bool aprobada = true;
            bool venta_cliente_oficina = false;

            string estado_contra_entrega = "";
            if (contra_entrega)
            {
                estado_contra_entrega = "POR CANCELAR";
            }

            string flete_por = cabecera.flete.Length >= 20 ? cabecera.flete.Substring(0, 20) : cabecera.flete;

            // PREPARAMOS CABECERA
            veproforma newRegistro = new veproforma();
            newRegistro.idpf_complemento = "";
            newRegistro.nroidpf_complemento = 0;
            newRegistro.complemento_ci = "";
            newRegistro.tipo_venta = 0;
            newRegistro.niveles_descuento = "ACTUAL";

            newRegistro.ubicacion = "LOCAL";
            newRegistro.usuarioaut = cabecera.usuarioreg;
            newRegistro.fechaaut = fecha_inicial;
            newRegistro.horaaut = hora_inicial;
            newRegistro.hora = hora_inicial;

            newRegistro.latitud_entrega = "0";
            newRegistro.longitud_entrega = "0";
            newRegistro.estado_contra_entrega = estado_contra_entrega;
            newRegistro.venta_cliente_oficina = venta_cliente_oficina;
            newRegistro.codcliente_real = codcliente_sin_nombre;

            newRegistro.pago_contado_anticipado = false;
            newRegistro.idanticipo = "";
            newRegistro.numeroidanticipo = 0;
            newRegistro.monto_anticipo = 0;
            newRegistro.contra_entrega = contra_entrega;

            newRegistro.confirmada = true;
            newRegistro.hora_confirmada = hora_inicial;
            newRegistro.fecha_confirmada = fecha_inicial;
            newRegistro.hora_inicial = hora_inicial;
            newRegistro.fecha_inicial = fecha_inicial;

            newRegistro.impresa = false;
            newRegistro.etiqueta_impresa = false;
            newRegistro.tipoentrega = "ENTREGAR";
            newRegistro.preparacion = "URGENTE";
            newRegistro.id = id_proforma;

            newRegistro.numeroid = idnroactual;
            newRegistro.codalmacen = cabecera.codalmdestino;
            newRegistro.codcliente = codcliente_sin_nombre;
            newRegistro.nomcliente = nomcliente;
            newRegistro.nit = nit;

            newRegistro.codvendedor = cabecera.codvendedor;
            newRegistro.codmoneda = cabecera.codmoneda;
            newRegistro.fecha = cabecera.fecha.Date;
            newRegistro.tdc = tdc;
            newRegistro.paraaprobar = para_aprobar;

            newRegistro.aprobada = aprobada;
            newRegistro.transferida = false;
            newRegistro.tipopago = cabecera.tipopago;
            newRegistro.subtotal = cabecera.total;
            newRegistro.descuentos = 0;

            newRegistro.recargos = 0;
            newRegistro.total = cabecera.total;
            newRegistro.anulada = false;
            newRegistro.transporte = cabecera.transporte;
            newRegistro.fletepor = flete_por;

            newRegistro.direccion = direccion;
            newRegistro.codcomplementaria = 0;
            newRegistro.obs = obs_proforma;
            newRegistro.horareg = hora_inicial;
            newRegistro.fechareg = fecha_inicial;

            newRegistro.usuarioreg = cabecera.usuarioreg;
            newRegistro.iva = 0;
            newRegistro.odc = nomcliente;
            newRegistro.nombre_transporte = cabecera.transporte;
            newRegistro.desclinea_segun_solicitud = false;

            newRegistro.idsoldesctos = "";
            newRegistro.nroidsoldesctos = 0;
            newRegistro.es_sol_urgente = true;
            newRegistro.tipo_docid = 5;
            newRegistro.email = "-";

            newRegistro.tipo_complementopf = 0;

            // alistar detalle proforma
            int nro_item = 1;
            List<veproforma1> detalleProf = detalle.Select(i => new veproforma1
            {
                nroitem = nro_item ++,
                porceniva = 0,
                codproforma = 0,
                coditem = i.coditem,
                cantidad_pedida = i.cantidad_pedido,

                cantidad = (decimal)i.cantidad,
                udm = i.udm,
                preciolista = (decimal)i.precio,
                niveldesc = "X",
                preciodesc = i.precio,

                precioneto = (decimal)i.precio,
                codtarifa = cabecera.codtarifa,
                coddescuento = 0,
                total = (decimal)i.total,
                cantaut = i.cantidad,

                totalaut = i.total,
                obs = ""
            }).ToList();


            // prepara Peso de items de detalle 
            detalleProf = await ventas.Actualizar_Peso_Detalle_Proforma(_context, detalleProf);
            // colocaar el Peso en la proforma
            newRegistro.peso = detalleProf.Sum(i => i.peso);

            // usar transacciones
            using (var dbContexTransaction = _context.Database.BeginTransaction())
            {
                try
                {
                    newRegistro.numeroid = await _context.venumeracion.Where(n => n.id == id_proforma).Select(i => i.nroactual).FirstOrDefaultAsync() + 1;
                    // agregar cabecera
                    try
                    {
                        _context.veproforma.Add(newRegistro);
                        await _context.SaveChangesAsync();
                    }
                    catch (Exception ex)
                    {
                        return (false, "Error al grabar la cabecera de la proforma: " + ex.Message, null);
                    }
                    codProforma = newRegistro.codigo;

                    // actualiza numero id
                    var numeracion = await _context.venumeracion.FirstOrDefaultAsync(n => n.id == id_proforma);
                    numeracion.nroactual += 1;
                    await _context.SaveChangesAsync(); // Guarda los cambios en la base de datos


                    int validaCantProf = await _context.veproforma.Where(i => i.id == id_proforma && i.numeroid == newRegistro.numeroid).CountAsync();
                    if (validaCantProf > 1)
                    {
                        return (false, "Se detecto más de un número del mismo documento, por favor consulte con el administrador del sistema.", null);
                    }

                    // guarda detalle (veproforma1)
                    // actualizar codigoProf para agregar
                    detalleProf = detalleProf.Select(p => { p.codproforma = codProforma; return p; }).ToList();
                    // guardar detalle
                    _context.veproforma1.AddRange(detalleProf);
                    await _context.SaveChangesAsync();
                    dbContexTransaction.Commit();
                }
                catch (Exception ex)
                {
                    dbContexTransaction.Rollback();
                    return (false, $"Error en el servidor al guardar Proforma: {ex.Message}", null);
                    throw;
                }
            }
            return (true, "", newRegistro);
        }

        //[Authorize]
        [HttpPost]
        [Route("recalcularDetalle/{userConn}/{codempresa}/{codtarifa}/{codalmacen}/{usuario}")]
        public async Task<object> recalcularDetalle(string userConn, string codempresa, int codtarifa, int codalmacen, string usuario, List<detalleRequest> detalleRequest)
        {
            try
            {
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);
                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    foreach (var reg in detalleRequest)
                    {
                        reg.descripcion = await items.itemdescripcion(_context, reg.coditem);
                        reg.medida = await items.itemmedida(_context, reg.coditem);
                        if (await empresa.ControlarStockSeguridad_context(_context,codempresa))
                        {
                            if (await ventas.Reservar_Para_Tiendas_En_Sol_Urgentes(_context, codtarifa))
                            {
                                reg.saldoag = await saldos.saldoitem_crtlstock(_context, codempresa, reg.coditem, codalmacen, true, usuario);
                            }
                            else
                            {
                                reg.saldoag = await saldos.saldoitem_crtlstock(_context, codempresa, reg.coditem, codalmacen, false, usuario);
                            }
                        }
                        else
                        {
                            reg.saldoag = await saldos.saldoitem_crtlstock(_context, codempresa, reg.coditem, codalmacen, false, usuario);
                        }
                    }

                    return Ok(new { detalle = detalleRequest });
                }
            }
            catch (Exception ex)
            {
                return BadRequest($"Error en el servidor al calcular Detalle: {ex.Message}");
                throw;
            }

        }

        [HttpPost]
        [Route("validarNegativ/{userConn}/{codempresa}/{codalmDestino}/{usuario}")]
        public async Task<object> validarNegativ(string userConn, string codempresa, int codalmDestino, string usuario, List<insolurgente1> insolurgente1)
        {
            try
            {
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);
                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var resultado = await Validar_Saldos_Negativos_Doc(_context, true, codalmDestino, codempresa, usuario, insolurgente1);
                    if (resultado.valido == false)
                    {
                        return StatusCode(203, new
                        {
                            valido = false,
                            resultado.msg,
                            resultado.negativos
                        });
                    }
                    return Ok(new
                    {
                        valido = true,
                        resultado.msg,
                        resultado.negativos
                    });
                }
            }
            catch (Exception ex)
            {
                return BadRequest($"Error en el servidor al validar Negativos: {ex.Message}");
                throw;
            }
        }



        [HttpPost]
        [Route("validarMaximoVta/{userConn}/{codempresa}")]
        public async Task<object> validarMaximoVta(string userConn, string codempresa,  requestGabrarSolUrgente requestGabrarSolUrgente)
        {
            List<insolurgente1> detalle = requestGabrarSolUrgente.tablaDetalle;
            insolurgente cabecera = requestGabrarSolUrgente.cabecera;
            try
            {
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);
                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    bool solUrgVtaMaxValido = false;
                    var validaMaxVta = await Validar_Max_Vta(_context, cabecera.id, cabecera.numeroid, codempresa, cabecera.usuarioreg, cabecera.codtarifa, cabecera.codcliente, cabecera.codalmdestino, cabecera.codalmacen, cabecera.fecha, false, solUrgVtaMaxValido, detalle);
                    
                    if (validaMaxVta.valido == false)
                    {
                        return StatusCode(203, new
                        {
                            valido = false,
                            resp = validaMaxVta.msg,
                            validaMaxVta.dtnocumplenMaxVta,
                        });
                    }
                    return Ok(new
                    {
                        valido = true,
                        resp = validaMaxVta.msg,
                        validaMaxVta.dtnocumplenMaxVta
                    });
                }
            }
            catch (Exception ex)
            {
                return BadRequest($"Error en el servidor al validar Negativos: {ex.Message}");
                throw;
            }
        }


        // IMPORTAR 
        [HttpPost]
        [Route("importSolUrginJson")]
        public async Task<IActionResult> importSolUrginJson([FromForm] IFormFile file)
        {

            // Guardar el archivo en una ubicación temporal
            string currentDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string outputDirectory = Path.Combine(currentDirectory, "OutputFiles");

            Directory.CreateDirectory(outputDirectory); // Crear el directorio si no existe


            if (file == null || file.Length == 0)
            {
                return BadRequest("No se cargo el archivo correctamente.");
            }
            string filePath = "";
            string pathDescFile = "";

            string _targetDirectory = "";
            try
            {
                _targetDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "OutputFiles");
                // Combina el directorio de destino con el nombre del archivo
                filePath = Path.Combine(_targetDirectory, file.FileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }
            }
            catch (Exception)
            {
                throw;
            }
            ziputil zUtil = new ziputil();

            string primerArchivo = zUtil.ObtenerPrimerArchivoEnZip(filePath);
            ///descomprimir
            try
            {
                await zUtil.DescomprimirArchivo(_targetDirectory, filePath, primerArchivo);
                pathDescFile = Path.Combine(_targetDirectory, primerArchivo);
                string xmlDecript = await encripVB.DecryptData(pathDescFile);
                //await funciones.DecryptData(Path.Combine(_targetDirectory, primerArchivo), Path.Combine(_targetDirectory, "profor.xml"), key, IV2);

                DataSet dataSet = new DataSet();

                using (StringReader stringReader = new StringReader(xmlDecript))
                {
                    dataSet.ReadXml(stringReader);
                }

                Console.WriteLine("XML convertido a DataSet exitosamente.");

                // Suponiendo que tienes un DataSet llamado dataSet y quieres convertirlo a un diccionario de tablas:
                Dictionary<string, DataTable> datosConvertidos = dataSet.ToDictionary();

                // Accede a una tabla específica por su nombre
                DataTable cabeceraTabla = datosConvertidos["cabecera"];
                DataTable detalleTabla = datosConvertidos["detalle"];

                List<Dictionary<string, object>> cabeceraList = DataTableToListConverter.ConvertToList(cabeceraTabla);
                List<Dictionary<string, object>> detalleList = DataTableToListConverter.ConvertToList(detalleTabla);
                return Ok(new
                {
                    cabeceraList,
                    detalleList
                });
            }
            catch (Exception ex)
            {
                return Problem("Error en el servidor al importar solicitud Urgente en JSON: " + ex.Message);
                throw;
            }
            finally
            {
                System.IO.File.Delete(filePath);
                System.IO.File.Delete(pathDescFile);
            }

        }


        // EXPORTAR ZIP
        [HttpGet]
        [Route("exportSolUrgente/{userConn}/{codSolUrgete}")]
        public async Task<IActionResult> exportSolUrgente(string userConn, int codSolUrgete)
        {
            try
            {
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);
                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var result = await cargariedataset(_context, codSolUrgete);
                    if (result.resp)
                    {
                        string stringDataXml = ConvertDataSetToXml(result.iedataset);
                        string id = result.id;
                        int numeroid = result.numeroid;

                        var resp_dataEncriptada = await exportar_encriptado(stringDataXml, id, numeroid);
                        if (resp_dataEncriptada.resp)
                        {
                            string zipFilePath = resp_dataEncriptada.filePath;

                            if (System.IO.File.Exists(zipFilePath))
                            {
                                byte[] fileBytes = System.IO.File.ReadAllBytes(zipFilePath);
                                string fileName = Path.GetFileName(zipFilePath);
                                try
                                {
                                    // Devuelve el archivo ZIP para descargar
                                    return File(fileBytes, "application/zip", fileName);
                                }
                                catch (Exception)
                                {
                                    return Problem("Error en el servidor");
                                    throw;
                                }
                                finally
                                {
                                    System.IO.File.Delete(zipFilePath);
                                }

                            }
                            else
                            {
                                return NotFound("El archivo ZIP no se encontró.");
                            }
                        }
                        //return Ok(stringDataXml);
                    }
                    return Ok(result.resp);
                }
            }
            catch (Exception ex)
            {
                return Problem("Error en el servidor al exportar Pedido ZIP: " + ex.Message);
                throw;
            }
        }

        private async Task<(bool resp, DataSet iedataset, string id, int numeroid)> cargariedataset(DBContext _context, int codSolUrgete)
        {
            DataSet iedataset = new DataSet();

            try
            {
                iedataset.Clear();
                iedataset.Reset();

                // cargar cabecera
                var dataPedido = await _context.insolurgente.Where(i => i.codigo == codSolUrgete).ToListAsync();
                if (dataPedido.Any())
                {
                    DataTable cabeceraTable = dataPedido.ToDataTable();
                    cabeceraTable.TableName = "cabecera";
                    iedataset.Tables.Add(cabeceraTable);
                    iedataset.Tables["cabecera"].Columns.Add("documento", typeof(string));
                    iedataset.Tables["cabecera"].Rows[0]["documento"] = "SOLURGENTE";

                }
                string id = dataPedido[0].id;
                int numeroid = (int)dataPedido[0].numeroid;
                /*
                // Añadir campo identificador
                iedataset.Tables["cabecera"].Columns.Add("documento", typeof(string));
                iedataset.Tables["cabecera"].Rows[0]["documento"] = "PROFORMA";

                */

                // Cargar detalle usando LINQ y Entity Framework
                var dataDetalle = await _context.insolurgente1
                    .Where(p => p.codsolurgente == codSolUrgete)
                    .Join(_context.initem,
                          p => p.coditem,
                          i => i.codigo,
                          (p, i) => new
                          {
                              p.coditem,
                              i.descripcion,
                              i.medida,
                              p.cantidad,
                              p.udm,
                              p.cantidad_pedido,
                          })
                    .OrderBy(p => p.coditem)
                    .ToListAsync();

                DataTable detalleTable = dataDetalle.ToDataTable();   // convertir a dataTable
                detalleTable.TableName = "detalle";
                iedataset.Tables.Add(detalleTable);

                return (true, iedataset, id, numeroid);
            }
            catch (Exception)
            {
                return (false, iedataset, "", 0);
            }
        }
        private string ConvertDataSetToXml(DataSet iedataset)
        {
            using (StringWriter sw = new StringWriter())
            {
                iedataset.WriteXml(sw, XmlWriteMode.WriteSchema);
                return sw.ToString();
            }
        }

        private async Task<(bool resp, string filePath)> exportar_encriptado(string xmlText, string id, int numeroid)
        {
            ziputil zUtil = new ziputil();
            try
            {
                string currentDirectory = AppDomain.CurrentDomain.BaseDirectory;
                string outputDirectory = Path.Combine(currentDirectory, "OutputFiles");

                Directory.CreateDirectory(outputDirectory); // Crear el directorio si no existe
                string outName = Path.Combine(outputDirectory, id + "-" + numeroid + ".enc");

                string[] archivo = new string[1];
                archivo[0] = outName;

                //await funciones.EncryptData(xmlText, outName, key, IV2);
                await encripVB.EncryptData(xmlText, outName);

                await zUtil.Comprimir(archivo, outName.Substring(0, outName.Length - 4) + ".zip", false);


                return (true, outName.Substring(0, outName.Length - 4) + ".zip");
            }
            catch (Exception)
            {
                return (false, "");
            }
        }



        [HttpGet]
        [Route("getDataImpSolUrg/{userConn}/{codSolUrg}/{codempresa}/{codmoneda_total}/{especial}")]
        public async Task<ActionResult<List<object>>> getDataImpSolUrg(string userConn, int codSolUrg, string codempresa, string codmoneda_total, bool especial, List<string> infraccioness)
        {
            try
            {
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);
                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    string rempresa = "";
                    string titulo = "";
                    string usuario = "";
                    string nit = "";

                    string rcodigo = "";
                    string rcodalmacen = "";
                    string rcodalmdestino = "";
                    string rcodalmarea = "";
                    string rfecha = "";
                    string robs = "";
                    string rcodvendedor = "";
                    string rcodcliente = "";
                    string rcodmoneda = "";
                    string rtipopago = "";

                    string rcodtarifa = "";
                    string rtotalventa = "";
                    string rtotal = "";
                    string rtransporte = "";
                    string rtpollegada = "";
                    string rinfracciones = "";
                    string rtipocliente = "";
                    string rpeso_pedido = "";
                    string robs1 = "";
                    string robs2 = "";
                    string robs3 = "";
                    string rrubro = "";
                    string cridpf_alm = "";
                    string crnroidpf_alm = "";

                    // obtener los datos de cabecera
                    var cabecera = await _context.insolurgente.Where(i => i.codigo == codSolUrg).FirstOrDefaultAsync();
                    if (cabecera == null)
                    {
                        return BadRequest(new { resp = "No se encontraron datos con el codigo proporcionado, consulte con el Administrador." });
                    }

                    titulo = cabecera.id + "-" + cabecera.numeroid + " SOLICITUD " + (especial == true ? "ESPECIAL ": "") + "DE MERCADERIA A AG: " + cabecera.codalmdestino;
                    rempresa = await nombres.nombreempresa(_context, codempresa);
                    usuario = cabecera.flete;
                    nit = "N.I.T.: " + await empresa.NITempresa(_context, codempresa);
                    rcodigo = cabecera.codigo.ToString("00000000");
                    rcodalmacen = cabecera.codalmacen.ToString();
                    rcodalmdestino = cabecera.codalmdestino.ToString();
                    rcodalmarea = (await almacen.AreaAlmacen(_context, cabecera.codalmacen)).ToString();
                    rfecha = cabecera.fecha.ToShortDateString();
                    robs = cabecera.obs;
                    rcodvendedor = cabecera.codvendedor + " " + await nombres.nombrevendedor(_context, cabecera.codvendedor);
                    rcodcliente = cabecera.tipocliente + " - " + cabecera.codcliente + " " + cabecera.nomcliente;
                    rcodmoneda = cabecera.codmoneda;
                    rrubro = await cliente.RubroConDescripcion(_context, cabecera.codcliente);

                    rcodtarifa = cabecera.codtarifa.ToString();
                    rtotalventa = cabecera.totalventa.ToString();
                    rtotal = cabecera.totalventa + " " + codmoneda_total;
                    rtransporte = cabecera.transporte;
                    rtpollegada = cabecera.tpollegada;
                    rinfracciones = armarInfraciones(infraccioness);
                    rtipocliente = cabecera.tipocliente + " Prof:" + cabecera.idproforma + "-" + cabecera.numeroidproforma;
                    rpeso_pedido = cabecera.peso_pedido.ToString();

                    cridpf_alm = cabecera.idproforma;
                    crnroidpf_alm = cabecera.numeroidproforma.ToString();

                    if (cabecera.tipopago == 0)
                    {
                        rtipopago = "CONTADO";
                    }
                    else
                    {
                        rtipopago = "CREDITO";
                    }
                    ////////////////////////////////////////
                    //// Fin de pasar valores a las variables
                    ////////////////////////////////////////
                    var tablaDetalle = await _context.insolurgente1
                        .Join(_context.initem,
                              p => p.coditem,
                              i => i.codigo,
                              (p, i) => new
                              {
                                  p.codsolurgente,
                                  p.coditem,
                                  i.descripcion,
                                  i.medida,
                                  p.cantidad,
                                  p.udm,
                                  p.saldoag,
                                  p.saldodest,
                                  p.stockmax,
                                  p.saldoarea,
                                  p.precio,
                                  p.total,
                                  p.cantidad_pedido
                              })
                        .Where(joined => joined.codsolurgente == codSolUrg)
                        .OrderBy(joined => joined.coditem)
                        .ToListAsync();

                    return Ok(new
                    {
                        rempresa,
                        titulo,
                        usuario,
                        nit,
                        rcodigo,
                        rcodalmacen,
                        rcodalmdestino,
                        rcodalmarea,
                        rfecha,
                        robs,
                        rcodvendedor,
                        rcodcliente,
                        rcodmoneda,
                        rtipopago,
                        rcodtarifa,
                        rtotalventa,
                        rtotal,
                        rtransporte,
                        rtpollegada,
                        rinfracciones,
                        rtipocliente,
                        rpeso_pedido,
                        robs1,
                        robs2,
                        rrubro,
                        cridpf_alm,
                        crnroidpf_alm,

                        tablaDetalle
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return Problem("Error en el servidor al obtener datos para imprimir por vista previa NM: " + ex.Message);
                throw;
            }
        }

        private string armarInfraciones(List<string> infrac)
        {
            string resultado = "";
            foreach (var reg in infrac)
            {
                if (resultado == "")
                {
                    resultado = reg;
                }
                else
                {
                    resultado = resultado + " * " + reg;
                }
            }
            return resultado;
        }
    }


    public class requestGabrarSolUrgente
    {
        public insolurgente cabecera { get; set; }
        public List<insolurgente1> tablaDetalle { get; set; }
    }


    public class detalleRequest
    {
        public int? codsolurgente { get; set; }
        public string coditem { get; set; }
        public string descripcion { get; set; }
        public string medida { get; set; }
        public decimal? cantidad { get; set; }
        public decimal? saldoag { get; set; }
        public decimal? stockmax { get; set; }
        public string udm { get; set; }
        public decimal? precio { get; set; }
        public decimal? total { get; set; }
        public decimal? saldodest { get; set; }
        public decimal? pedtotal { get; set; }
        public decimal? saldoarea { get; set; }
        public decimal? cantidad_pedido { get; set; }
    }

}
