using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using siaw_DBContext.Data;
using siaw_DBContext.Models;
using Microsoft.AspNetCore.Authorization;
using siaw_funciones;
using siaw_DBContext.Models_Extra;
using System.Runtime.Intrinsics.X86;
using Microsoft.EntityFrameworkCore.Storage;
using System.Globalization;
using Polly;

namespace SIAW.Controllers.inventarios.transaccion
{
    [Route("api/inventario/transac/[controller]")]
    [ApiController]
    public class docinmovimientoController : ControllerBase
    {
        private readonly Dui dui = new Dui();
        private readonly Items item = new Items();
        private readonly Inventario inventario = new Inventario();
        private readonly Saldos saldos = new Saldos();
        private readonly Seguridad seguridad = new Seguridad();
        private readonly Almacen almacen = new Almacen();
        private readonly Ventas ventas = new Ventas();
        private readonly Restricciones restricciones = new Restricciones();
        private readonly Documento documento = new Documento();
        private readonly Funciones funciones = new Funciones();
        private readonly Empresa empresa = new Empresa();
        private readonly HardCoded hardCoded = new HardCoded();
        private readonly Configuracion configuracion = new Configuracion();
        private readonly Nombres nombres = new Nombres();

        private readonly UserConnectionManager _userConnectionManager;
        public docinmovimientoController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }

        [HttpGet]
        [Route("getParametrosInicialesNM/{userConn}/{usuario}/{codempresa}")]
        public async Task<ActionResult<object>> getParametrosInicialesNM(string userConn, string usuario, string codempresa)
        {
            try
            {
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);
                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    int codvendedor = await configuracion.usr_codvendedor(_context, usuario);
                    int codalmacen = await configuracion.usr_codalmacen(_context, usuario);
                    string codalmacendescripcion = await nombres.nombrealmacen(_context, codalmacen);
                    bool chkdesglozar_cjtos = false;
                    string id = await configuracion.usr_idmovimiento(_context, usuario);
                    int numeroid = await documento.movimientonumeroid(_context,id) + 1;
                    bool cargar_proforma = false;
                    bool cvenumeracion1 = false;
                    var obtener_cantidades_aprobadas_de_proformas = await saldos.Obtener_Cantidades_Aprobadas_De_Proformas(_context, codempresa);
                    bool es_ag_local=  false;
                    bool es_tienda = false;
                    bool ver_ch_es_para_invntario = await configuracion.usr_ver_check_es_para_inventario(_context, usuario);

                    return Ok(new
                    {
                        codvendedor,
                        codalmacen,
                        codalmacendescripcion,
                        chkdesglozar_cjtos,
                        id,
                        numeroid,
                        cargar_proforma,
                        cvenumeracion1,
                        obtener_cantidades_aprobadas_de_proformas,
                        es_ag_local,
                        es_tienda,
                        ver_ch_es_para_invntario
                    });
                }
            }
            catch (Exception ex)
            {
                return Problem("Error en el servidor al obtener parametros iniciales NM: " + ex.Message);
                throw;
            }
        }



        [HttpPost]
        [Route("grabarDocumento/{userConn}")]
        public async Task<ActionResult<object>> grabarDocumento(string userConn, List<tablaDetalleNM> tablaDetalle)
        {
            try
            {
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);
                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    // calculartotal.PerformClick()
                    // borrar items con cantidad 0 o menor
                    tablaDetalle = tablaDetalle.Where(i => i.cantidad > 0).ToList();

                }
                return Ok();
            }
            catch (Exception ex)
            {
                return Problem("Error en el servidor al guardar Documento: " + ex.Message);
                throw;
            }
        }

        private async Task<(bool valido, string msg, List<Dtnegativos>? dtnegativos)> guardarNuevoDocumento(DBContext _context, int factor, string codempresa, bool traspaso, inmovimiento inmovimiento, List<tablaDetalleNM> tablaDetalle)
        {
            // preparacion de datos 
            inmovimiento.fecha = inmovimiento.fecha.Date;
            inmovimiento.factor = (short)factor;
            inmovimiento.horareg = await funciones.hora_del_servidor_cadena(_context);
            inmovimiento.fechareg = await funciones.FechaDelServidor(_context);
            inmovimiento.anulada = false;
            inmovimiento.contabilizada = false;
            inmovimiento.peso = 0;
            inmovimiento.fecha_inicial = await funciones.FechaDelServidor(_context);



            var validaDetalle = await validarDetalle(_context, factor, codempresa, inmovimiento, tablaDetalle);
            if (validaDetalle.valido)
            {
                var validaCabecera = await validardatos(_context, factor, traspaso, inmovimiento);
                if (validaCabecera.valido)
                {
                    // prepara detalle Nota Movimiento
                    List<inmovimiento1> inmovimiento1 = tablaDetalle.Select(i => new inmovimiento1
                    {
                        codmovimiento = 0,
                        coditem = i.coditem,
                        cantidad = i.cantidad,
                        udm = i.udm,
                        codaduana = i.codaduana,
                    }).ToList();
                    // coloca pesos
                    foreach (var reg in inmovimiento1)
                    {
                        var pesoItem = await _context.initem.Where(i => i.codigo == reg.coditem).Select(i => i.peso).FirstOrDefaultAsync() ?? 0;
                        reg.peso = pesoItem * reg.cantidad;
                    }
                    // coloca peso general
                    inmovimiento.peso = inmovimiento1.Sum(i => i.peso);

                    using (var dbContexTransaction = _context.Database.BeginTransaction())
                    {
                        try
                        {
                            inmovimiento.numeroid = await documento.movimientonumeroid(_context, inmovimiento.id) + 1;
                            if (inmovimiento.numeroid <= 0)
                            {
                                return (false, "Error al generar numero ID, consulte con el Administrador", null);
                            }
                            if (await documento.existe_movimiento(_context,inmovimiento.id, inmovimiento.numeroid))
                            {
                                return (false, "Ese numero de documento, ya existe, por favor consulte con el administrador del sistema.", null);
                            }

                            // agregar cabecera
                            try
                            {
                                _context.inmovimiento.Add(inmovimiento);
                                await _context.SaveChangesAsync();
                            }
                            catch (Exception ex)
                            {
                                return (false, "Error al grabar la cabecera de la nota de movimiento: " + ex.Message, null);
                            }
                            int codNotaMovimiento = inmovimiento.codigo;





                            // actualiza numero id
                            var numeracion = _context.intipomovimiento.FirstOrDefault(n => n.id == inmovimiento.id);
                            numeracion.nroactual += 1;
                            await _context.SaveChangesAsync(); // Guarda los cambios en la base de datos


                            int validaCantProf = await _context.inmovimiento.Where(i => i.id == inmovimiento.id && i.numeroid == inmovimiento.numeroid).CountAsync();
                            if (validaCantProf > 1)
                            {
                                return (false, "Se detecto más de un número del mismo documento, por favor consulte con el administrador del sistema.", null);
                            }

                            // guardar detalle
                            _context.inmovimiento1.AddRange(inmovimiento1);
                            await _context.SaveChangesAsync();
                            dbContexTransaction.Commit();
                        }
                        catch (Exception ex)
                        {
                            dbContexTransaction.Rollback();
                            return (false,$"Error en el servidor al guardar Proforma: {ex.Message}", null);
                            throw;
                        }

                        /*
                          DESPUES DE GUARDAR DETALLE CABECERA Y NUMERO DE ID
                        '//#######################################
                        '//##RUTINA QUE ACTUALIZA EL SALDO ACTUAL
                        'sia_funciones.Saldos.Instancia.Inmovimiento_ActualizarSaldo(codigo.Text, sia_funciones.Saldos.modo_actualizacion.crear, tabladetalle)
                        ''Desde 23/11/2023 verificar si la actualizacion de saldo es true oi false para registrar un registro de que no se pudo actualizar
                         */
                        /*
                        if (await saldos.Inmovimiento_ActualizarSaldo())
                        {

                        }
                        */

                    }

                }
                return (false, validaCabecera.msg, null);
            }

            return (false, validaDetalle.msg, validaDetalle.dtnegativos);
        }



        private async Task<(bool valido, string msg)> validardatos(DBContext _context, int factor, bool traspaso, inmovimiento inmovimiento)
        {
            inmovimiento = quitarEspacios(factor, traspaso, inmovimiento);
            // validar codigo vacio
            if (inmovimiento.codvendedor == null || inmovimiento.codvendedor <= 0)
            {
                return (false, "No puede dejar la casilla de Codigo de vendedor del Documento en blanco.");
            }
            if (inmovimiento.codconcepto == null || inmovimiento.codconcepto <= 0)
            {
                return (false, "No puede dejar la casilla de Concepto del Documento en blanco.");
            }
            if (await inventario.ConceptoEsUsuarioFinal(_context, inmovimiento.codconcepto))
            {
                if (inmovimiento.codpersona == null || inmovimiento.codpersona == 0)
                {
                    return (false, "Si el concepto es usuario final, debe definir la persona responsable o usuario final.");
                }
                if (inmovimiento.codalmdestino == null || inmovimiento.codalmdestino == 0)
                {
                    return (false, "Debe definir el almacen destino el cual es el almacen de la persona responsable o usuario final.");
                }
            }
            if (await inventario.Concepto_Es_Entrega_Cliente(_context, inmovimiento.codconcepto))
            {
                if (string.IsNullOrWhiteSpace(inmovimiento.codcliente))
                {
                    return (false, "Si el concepto es entrega a cliente, debe definir el codigo del cliente al cual se realiza la entrega.");
                }
                if (inmovimiento.codalmdestino == null || inmovimiento.codalmdestino == 0)
                {
                    return (false, "Debe definir el almacen destino el cual es el almacen del cliente.");
                }
            }
            if (string.IsNullOrEmpty(inmovimiento.id))
            {
                return (false, "No puede dejar la casilla de ID de Documento en blanco.");
            }
            if (inmovimiento.numeroid <= 0)
            {
                return (false, "No puede dejar la casilla de Numero de Documento en blanco.");
            }
            if (inmovimiento.codalmacen <= 0)
            {
                return (false, "No puede dejar la casilla de Almacen en blanco.");
            }
            if (inmovimiento.codalmorigen == null || inmovimiento.codalmorigen <= 0)
            {
                return (false, "No puede dejar la casilla de Almacen de Origen en blanco.");
            }
            if (inmovimiento.codalmdestino == null || inmovimiento.codalmdestino <= 0)
            {
                return (false, "No puede dejar la casilla de Almacen de Destino en blanco.");
            }
            if (string.IsNullOrWhiteSpace(inmovimiento.obs))
            {
                return (false, "No puede dejar la casilla de Observacion en blanco.");
            }
            if (inmovimiento.obs.Length > 60)
            {
                return (false, "La observacion es demasiado larga por favor reduzcala.");
            }
            if (string.IsNullOrWhiteSpace(inmovimiento.fid))
            {
                return (false, "No puede dejar la casilla de ID de Documento de origen en blanco.");
            }
            if (inmovimiento.fnumeroid == null || inmovimiento.fnumeroid<= 0)
            {
                return (false, "No puede dejar la casilla de Numero de Documento de origen  en blanco.");
            }

            if (await inventario.concepto_espara_despacho(_context,inmovimiento.codconcepto) && inmovimiento.codconcepto == 10 && await almacen.Es_Tienda(_context,inmovimiento.codalmacen))
            {
                if (string.IsNullOrWhiteSpace(inmovimiento.idproforma_sol))
                {
                    return (false, "No puede dejar la casilla de ID de la PF de Almacen en blanco.");
                }
                if (inmovimiento.numeroidproforma_sol == null || inmovimiento.numeroidproforma_sol <= 0)
                {
                    return (false, "No puede dejar la casilla de Numero de la PF de Almacen en blanco o con valor 0.");
                }
                if (string.IsNullOrWhiteSpace(inmovimiento.idproforma))
                {
                    return (false, "No puede dejar la casilla de ID de la PF que hizo la solicitud urgente en blanco.");
                }
                if (inmovimiento.numeroidproforma == null || inmovimiento.numeroidproforma<= 0)
                {
                    return (false, "No puede dejar la casilla de Numero de la PF que hizo la solicitud urgente en blanco o con valor 0.");
                }
                if (await ventas.proforma_es_sol_urgente(_context,inmovimiento.idproforma_sol, inmovimiento.numeroidproforma_sol ?? 0) == false)
                {
                    return (false, "La proforma enlazada es NO es de una solicitud urgente de Tienda,verifique esta situacion.");
                }
            }

            if (await seguridad.periodo_abierto_context(_context,inmovimiento.fecha.Year, inmovimiento.fecha.Month,2) == false)
            {
                return (false, "No puede crear documentos para ese periodo de fechas.");
            }

            if (await restricciones.ValidarModifDocAntesInventario(_context,inmovimiento.codalmacen, inmovimiento.fecha) == false)
            {
                return (false, "No puede modificar datos anteriores al ultimo inventario, Para eso necesita una autorizacion especial.");
            }


            // SE NECESITA PERMISO ESPECIAL      // consultar sobre esto quiza no deberia
            // siempre y cuando se haya devuelto false,
            /*
             
            Dim control As New sia_compartidos.prgcontrasena("48", id.Text & "-" & numeroid.Text & ": " & id.Text & "-" & numeroid.Text, id.Text, numeroid.Text)
                
             */
            return (true, "");
        }
        private inmovimiento quitarEspacios(int factor, bool traspaso, inmovimiento inmovimiento)
        {
            inmovimiento.id = inmovimiento.id.Trim();
            inmovimiento.fid = inmovimiento.fid.Trim();
            if (factor == -1)
            {
                if (inmovimiento.fid == "")
                {
                    inmovimiento.fid = "-";
                }
                if (inmovimiento.fnumeroid == null)
                {
                    inmovimiento.fnumeroid = 0;
                }
            }
            if (traspaso == false)
            {
                if (inmovimiento.fid == "")
                {
                    inmovimiento.fid = "-";
                }
                if (inmovimiento.fnumeroid == null)
                {
                    inmovimiento.fnumeroid = 0;
                }
            }
            inmovimiento.obs = inmovimiento.obs.Trim();
            inmovimiento.obs = seguridad.FiltrarCadena(inmovimiento.obs);

            if (inmovimiento.numeroidproforma == null)
            {
                inmovimiento.numeroidproforma = 0;
            }
            if (inmovimiento.numeroidproforma_sol == null)
            {
                inmovimiento.numeroidproforma_sol = 0;
            }
            return inmovimiento;
        }

        private string hay_item_duplicados(List<tablaDetalleNM> tablaDetalle)
        {
            /*
            string cadena_item = "";
            List<string> lista_items = new List<string>();
            foreach (var reg in tablaDetalle)
            {
                if (lista_items.Contains(reg.coditem) == false)
                {
                    lista_items.Add(reg.coditem);
                }
                else
                {
                    if (cadena_item.Trim().Length == 0)
                    {
                        cadena_item = "'" + reg.coditem + "'";
                    }
                    else
                    {
                        cadena_item = ", '" + reg.coditem + "'";
                    }
                }
            }

            */
            var lista_items = new HashSet<string>(); // Utiliza HashSet para asegurar unicidad
            var cadena_item = string.Join(", ", tablaDetalle
                .Where(reg => !lista_items.Contains(reg.coditem) && lista_items.Add(reg.coditem)) // Filtra y agrega únicos
                .Select(reg => $"'{reg.coditem}'"));

            return string.IsNullOrEmpty(cadena_item) ? "" : cadena_item;

        }

        private async Task<(bool valido, string msg, List<Dtnegativos>? dtnegativos)> validarDetalle(DBContext _context, int factor, string codempresa, inmovimiento inmovimiento, List<tablaDetalleNM> tablaDetalle)
        {
            int indice = 1;

            if (tablaDetalle.Count() == 0)
            {
                return (false, "No existen items en el detalle.", null);
            }
            string item_repetidos = hay_item_duplicados(tablaDetalle);
            if (item_repetidos.Trim().Length > 0)
            {
                return (false, "Los siguientes items estan repetidos: " + item_repetidos, null);
            }


            foreach (var reg in tablaDetalle)
            {
                if (reg.codaduana == null)
                {
                    reg.codaduana = " ";
                }

                if (string.IsNullOrWhiteSpace(reg.coditem))
                {
                    return (false, "No eligio El Item en la Linea: " + indice, null);
                }
                if (await item.iteminactivo(_context,reg.coditem))
                {
                    return (false, "El item de la linea " + indice + " -> " + reg.coditem + " no puede ser movido por que esta inactivo.", null);
                }
                if (await item.item_usar_en_movimiento(_context,reg.coditem) == false)
                {
                    return (false, "El item de la linea " + indice + " -> " + reg.coditem + " no puede usarse en notas de movimiento.", null);
                }
                if (string.IsNullOrWhiteSpace(reg.udm))
                {
                    return (false, "No puso la Unidad de Medida en la Linea " + indice + " .", null);
                }
                if (reg.cantidad == null)
                {
                    return (false, "No puso la cantidad en la Linea " + indice + " .", null);
                }
                if (reg.cantidad <= 0)
                {
                    return (false, "No puso la cantidad en la Linea " + indice + " .", null);
                }
                indice++;
            }

            // validar negativos
            if ((factor == -1) && (await inventario.PermitirNegativos(_context,codempresa) == false))
            {
                List<string> msgs = new List<string>();
                List<string> negs = new List<string>();

                var validaNegativos = await Validar_Saldos_Negativos_Doc(_context, codempresa, inmovimiento, tablaDetalle);
                if (validaNegativos.resultado == false)   ////////////////////////////// VERIFICAR SI DEBE PERMITIR GRABAR CON CLAVE, EN EL SIA LO HACE
                {
                    return(false, validaNegativos.msg, validaNegativos.dtnegativos);
                }
            }

            /*
             
            If resultado Then
                For i = 0 To tabladetalle.Rows.Count - 1
                    tabladetalle.Rows(i)("descripcion") = sia_funciones.Items.Instancia.itemdescripcion(tabladetalle.Rows(i)("coditem"))
                    tabladetalle.Rows(i)("medida") = sia_funciones.Items.Instancia.itemmedida(tabladetalle.Rows(i)("coditem"))
                Next
            End If

             */

            // llama a la funcion que valida si hay cantidades decimales
            respValidaDecimales validaDecimales = validar_cantidades_decimales(tablaDetalle);
            if (validaDecimales.cumple == false)
            {
                // VALIDAR CANTIDADES
                // VERIFICAR QUE DEBE PEDIR CONTRASEÑA
                return (false, "No se puede grabar el documento debido a que se encontró cantidades en decimales para items que son en PZ!!!", null);
            }


            return (true,"", null);
        }





        private async Task<(bool resultado, string msg, List<Dtnegativos> dtnegativos)> Validar_Saldos_Negativos_Doc(DBContext _context, string codempresa, inmovimiento inmovimiento ,List<tablaDetalleNM> tablaDetalle)
        {
            bool resultado = true;
            string msg = "";
            List<Dtnegativos> dtnegativos = new List<Dtnegativos>();
            if (tablaDetalle.Count() > 0)
            {
                List<string> msgs = new List<string>();
                List<string> negs = new List<string>();
                
                // Desde 28-10-2020 se debe enviar el dato de id y numeroid de proforma si la nota es para una solicitud urgente de los campos id_proforma_dsd y numeroidproforma_dsd
                if (inmovimiento.numeroidproforma_sol == null)
                {
                    inmovimiento.numeroidproforma_sol = 0;
                }
                // dtnegativos = sia_funciones.Saldos.Instancia.ValidarNegativosDocVenta(tabladetalle, False, Convert.ToInt32(codalmacen.Text), "", 0, msgs, negs, sia_compartidos.temporales.Instancia.codempresa, sia_compartidos.temporales.Instancia.usuario)
                List<itemDataMatriz> detalleItemDataMatriz = tablaDetalle.Select(i => new itemDataMatriz
                {
                    coditem = i.coditem,
                    descripcion = i.descripcion,
                    medida = i.medida,
                    udm = i.udm,
                    cantidad = (double)i.cantidad,
                }).ToList();
                dtnegativos = await saldos.ValidarNegativosDocVenta(_context, detalleItemDataMatriz, inmovimiento.codalmacen, inmovimiento.idproforma_sol, inmovimiento.numeroidproforma_sol ?? 0, msgs, negs, codempresa, inmovimiento.usuarioreg);
                // debemos devolver estos negativos

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
                // si ningun item genera negativo recorrer toda la lista 1 poner cumple=1 lo que significa que todo esta bien
                if (negs.Count() > 0)
                {
                    resultado = false;
                    msg = "Los items del documento resaltados de color azul generaran negativos.";
                }
            }
            return (resultado,msg, dtnegativos);
        }






        [HttpPost]
        [Route("copiarAduana/{userConn}")]
        public async Task<ActionResult<object>> copiarAduana(string userConn, List<tablaDetalleNM> tablaDetalle)
        {
            try
            {
                if (tablaDetalle.Count() > 0)
                {
                    string codAduana = tablaDetalle[0].codaduana;
                    tablaDetalle.ToList().ForEach(reg => reg.codaduana = codAduana);
                    return Ok(tablaDetalle);
                }
                return BadRequest(new { resp = "No se esta recibiendo nada en el detalle, verifique esta situación" });
            }
            catch (Exception ex)
            {
                return Problem("Error en el servidor al copiar Aduana: " + ex.Message);
                throw;
            }
        }



        [HttpPost]
        [Route("ponerDui/{userConn}/{codalmacen}")]
        public async Task<ActionResult<object>> ponerDui(string userConn, int codalmacen, List<tablaDetalleNM> tablaDetalle)
        {
            try
            {
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);
                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    if (tablaDetalle.Count() > 0)
                    {
                        foreach (var reg in tablaDetalle)
                        {
                            // reg.codaduana =
                            reg.codaduana = await dui.dui_correspondiente(_context, reg.coditem, codalmacen);
                            //return Ok(resultado);
                        }
                        return Ok(tablaDetalle);
                    }
                    return BadRequest(new { resp = "No se esta recibiendo nada en el detalle, verifique esta situación" });
                }
            }
            catch (Exception ex)
            {
                return Problem("Error en el servidor al colocar DUI en el detalle: " + ex.Message);
                throw;
            }
        }




        [HttpPost]
        [Route("getValidaCantDecimal/{userConn}")]
        public async Task<ActionResult<bool>> getValidaCantDecimal(string userConn, List<tablaDetalleNM> tablaDetalle)
        {
            try
            {
                bool camposInvalidos = tablaDetalle.Any(i =>
                    string.IsNullOrEmpty(i.coditem) ||
                    string.IsNullOrEmpty(i.udm)
                    );
                if (camposInvalidos)
                {
                    return BadRequest(new { resp = "Existen codigos de item o unidades de medida que se estan recibiendo vacio o nulo, consulte con el administrador." });
                }
                respValidaDecimales resultado = validar_cantidades_decimales(tablaDetalle);
                return Ok(new
                {
                    resultado.cabecera,
                    resultado.detalleObs,
                    resultado.cumple,
                    resp = resultado.alerta
                });
            }
            catch (Exception ex)
            {
                return Problem("Error en el servidor al validar cantidades decimales: " + ex.Message);
                throw;
            }

        }

        private respValidaDecimales validar_cantidades_decimales(List<tablaDetalleNM> tablaDetalle)
        {
            // Crear una instancia de CultureInfo basada en la cultura actual
            CultureInfo culture = (CultureInfo)CultureInfo.CurrentCulture.Clone();

            // Establecer el separador decimal como punto
            culture.NumberFormat.NumberDecimalSeparator = ".";

            string cadena_items_decimales = "";
            List<string> observacionesDecimales = new List<string>();
            // valiar cantidades si hay decimales
            foreach (var reg in tablaDetalle)
            {
                bool esDecimal = false;
                ////////////////////////////
                string valorCadena = reg.cantidad.ToString(culture);
                string[] partes = valorCadena.Split('.');

                if (partes.Length > 1)
                {
                    if (partes[1] != null && partes[1] != "0")
                    {
                        esDecimal = true;
                    }
                }

                ////////////////////////////
                if (reg.udm == "PZ")
                {
                    if (esDecimal)
                    {
                        string obs = "Item: " + reg.coditem + " = " + reg.cantidad + " (PZ)";
                        observacionesDecimales.Add(obs);

                        cadena_items_decimales = cadena_items_decimales + reg.coditem + " | ";
                    }
                }
            }
            string alerta = "";
            string cabecera = "";
            bool cumple = true;
            if (cadena_items_decimales.Trim().Length > 0)
            {
                cabecera = "Los siguientes items son en PZ y las cantidades en la nota estan con decimales, lo cual no esta permitido verifique esta situacion!!!";
                alerta = "Verifique la pestaña de observaciones, se econtraron cantidades en decimales para items que son en PZ!!!. " + cadena_items_decimales;
                cumple = false;
            }
            return new respValidaDecimales
            {
                cabecera = cabecera,
                detalleObs = observacionesDecimales,
                alerta = alerta,
                cumple = cumple

            };
        }




        //Boton Totalizar
        [HttpPost]
        [Route("Totalizar/{userConn}")]
        public async Task<object> Totalizar(string userConn, requestTotalizar requestTotalizar)
        {
            decimal totalcant = 0;
            string total = "";
            // Crear una instancia de CultureInfo basada en la cultura actual
            CultureInfo culture = (CultureInfo)CultureInfo.CurrentCulture.Clone();

            // Establecer el separador decimal como punto
            culture.NumberFormat.NumberDecimalSeparator = ".";
            try
            {
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    List<tablaDetalleNM> tabladetalle = requestTotalizar.tabladetalle;

                    if (tabladetalle.Count > 0)
                    {
                        foreach (var detalle in tabladetalle)
                        {
                            totalcant = totalcant + detalle.cantidad;
                        }
                    }
                    total = totalcant.ToString(culture);
                    return Ok(new
                    {
                        total = total
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return Problem("Error en el servidor al totalizar nota de movimiento: " + ex.Message);
            }
        }


        // boton Validar Saldos
        [HttpPost]
        [Route("ValidarSaldos/{userConn}")]
        public async Task<ActionResult<List<object>>> ValidarSaldos(string userConn, requestValidaSaldos requestValidaSaldos)
        {
            try
            {
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);
                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    string codempresa = requestValidaSaldos.codempresa;
                    int codalmorigen = requestValidaSaldos.codalmorigen;
                    int codalmdestino = requestValidaSaldos.codalmdestino;
                    int codconcepto = requestValidaSaldos.codconcepto;
                    string usuario = requestValidaSaldos.usuario;
                    List<tablaDetalleNM> tabladetalle = requestValidaSaldos.tabladetalle;

                    var result = await Revisar_ValidarSaldos(_context, codempresa, usuario, codalmorigen, codalmdestino, codconcepto, tabladetalle);
                    return Ok(new
                    {
                        cumple = result.resultado,
                        alerta = result.alerta,
                        tabladetalle = result.tabladetalle
                    });
                }
            }
            catch (Exception ex)
            {
                return Problem("Error en el servidor al validar saldos de los items en la nota de movimiento. " + ex.Message);
                throw;
            }
        }

        private async Task<(bool resultado, string alerta, string observacion, List<dt_disminuir> dt_disminuir, List<tablaDetalleNM> tabladetalle)> Revisar_ValidarSaldos(DBContext _context, string codempresa, string usuario, int codalmorigen, int codalmdestino, int codconcepto, List<tablaDetalleNM> tablaDetalle)
        {
            bool resultado = true;
            string msg_obs_revision_saldos_kg = "";
            string msg_obs_revision_saldos_pz = "";
            string alerta = "";
            string obs = "";
            List<dt_disminuir> dt_disminuir = new List<dt_disminuir>();

            var revisar_saldos_result = await revisar_saldos(_context, codempresa, usuario, codalmorigen, codalmdestino, codconcepto, tablaDetalle);
            dt_disminuir = revisar_saldos_result.dt_disminuir;
            msg_obs_revision_saldos_kg = revisar_saldos_result.msg1;
            msg_obs_revision_saldos_pz = revisar_saldos_result.msg2;

            foreach (var reg1 in dt_disminuir)
            {
                foreach (var reg in tablaDetalle)
                {
                    if (reg.coditem == reg1.coditem)
                    {
                        decimal cantidad = Convert.ToDecimal(reg.cantidad);
                        if (cantidad <= 0)
                        {
                            reg.cantidad = 0;
                        }
                        else if (cantidad > reg1.cantidad)
                        {
                            reg.cantidad = cantidad - reg1.cantidad;
                        }
                        else if (cantidad <= reg1.cantidad)
                        {
                            reg.cantidad = 0;
                        }
                    }
                }
            }

            if (dt_disminuir.Count > 0)
            {
                resultado = false;
                alerta = "Por la no existencia de saldos de grampas, se ha disminuido el envio de tuercas, verificar esta situacion!!!";
            }
            //si hay observaciones
            if (!string.IsNullOrEmpty(msg_obs_revision_saldos_kg) || !string.IsNullOrEmpty(msg_obs_revision_saldos_pz))
            {
                resultado = false;
                obs += "=============================================\n";
                obs += "OBSERVACIONES ITEMS EN (KG)\n";
                obs += "=============================================\n";
                obs += msg_obs_revision_saldos_kg + "\n\n";
                obs += "=============================================\n";
                obs += "OBSERVACIONES ITEMS EN (PZ)\n";
                obs += "=============================================\n";
                obs += msg_obs_revision_saldos_pz + "\n";
            }
            return (resultado, alerta, obs, dt_disminuir, revisar_saldos_result.tabladetalle);
        }

        private async Task<(bool resultado, string alerta, string msg1, string msg2, List<dt_disminuir> dt_disminuir, List<tablaDetalleNM> tabladetalle)> revisar_saldos(DBContext _context, string codempresa, string usuario, int codalmorigen, int codalmdestino, int codconcepto, List<tablaDetalleNM> tabladetalle)
        {
            bool resultado = true;
            string alerta = "";
            string msg_obs_revision_saldos_kg = "";
            string msg_obs_revision_saldos_pz = "";
            string txtentero1 = "";
            string txtdecimal1 = "";
            List<dt_disminuir> dt_disminuir = new List<dt_disminuir>();
            if (!string.IsNullOrWhiteSpace(codalmorigen.ToString()) && !string.IsNullOrWhiteSpace(codalmdestino.ToString()) && !string.IsNullOrWhiteSpace(codconcepto.ToString()))
            {

                double saldo;
                dt_disminuir.Clear();
                int cantidad_disminuir = 0;
                int cantidad_cjto = 0;
                string cadena_saldo = "";
                string[] vector_saldo = new string[2];
                string cadena_cantidad = "";
                string[] vector_cantidad = new string[2];
                double mi_cantidad = 0;

                bool Es_Ag_Local = await empresa.AlmacenLocalEmpresa(_context, codempresa) == codalmorigen;
                bool Es_Tienda = await almacen.Es_Tienda(_context, codalmorigen);

                bool obtener_saldos_otras_ags_localmente = await saldos.Obtener_Saldos_Otras_Agencias_Localmente_context(_context, codempresa);
                bool obtener_cantidades_aprobadas_de_proformas = await saldos.Obtener_Cantidades_Aprobadas_De_Proformas(_context, codempresa);
                int AlmacenLocalEmpresa = await empresa.AlmacenLocalEmpresa_context(_context, codempresa);

                foreach (var row in tabladetalle)
                {
                    string coditem = row.coditem;
                    double cantidad = Convert.ToDouble(row.cantidad);

                    if (await hardCoded.NoTomarSaldosACubrir(coditem))
                    {
                        var resultadoSaldos = await saldos.SaldoItem_CrtlStock_Para_Ventas_Sam(_context, coditem, codalmorigen, Es_Tienda, false, "---", 0, false, codempresa, usuario, obtener_saldos_otras_ags_localmente, obtener_cantidades_aprobadas_de_proformas, AlmacenLocalEmpresa);
                        saldo = (double)resultadoSaldos.cantidad_ag_local_incluye_cubrir;
                    }
                    else
                    {
                        var resultadoSaldos = await saldos.SaldoItem_CrtlStock_Para_Ventas_Sam(_context, coditem, codalmorigen, Es_Tienda, false, "---", 0, true, codempresa, usuario, obtener_saldos_otras_ags_localmente, obtener_cantidades_aprobadas_de_proformas, AlmacenLocalEmpresa);
                        saldo = (double)resultadoSaldos.cantidad_ag_local_incluye_cubrir;
                    }

                    cantidad_disminuir = 0;
                    cantidad_cjto = Convert.ToInt32(row.cantidad);
                    ////////////////////////////////////////////////////////
                    //SI LA CANTIDAD ES MAYOR AL SALDO (disminuir la cantidad pedida)
                    ////////////////////////////////////////////////////////
                    if (cantidad > saldo)
                    {
                        if (saldo < 0)
                        {
                            saldo = 0;
                            cantidad_disminuir = cantidad_cjto;
                        }
                        else
                        {
                            cantidad_disminuir = cantidad_cjto - (int)saldo;
                        }

                        row.cantidad = (decimal)saldo;

                        if (saldo == 0 && await item.es_item_de_control(_context, coditem))
                        {
                            var milista = await item.EsParteDeConjuntos(_context, coditem);

                            foreach (string parte in milista)
                            {
                                var dtpartes = await _context.inkit
                                   .Where(inkit => inkit.codigo == parte && inkit.item != coditem)
                                   .Select(inkit => new { coditem = inkit.codigo, item = inkit.item, cantidad = inkit.cantidad })
                                   .ToListAsync();

                                foreach (var parteRow in dtpartes)
                                {
                                    dt_disminuir.Add(new dt_disminuir
                                    {
                                        coditem_kit = coditem,
                                        cantidad_kit = cantidad_cjto,
                                        coditem = parteRow.item,
                                        cantidad = (decimal)(parteRow.cantidad * cantidad_disminuir)
                                    });
                                }
                            }
                        }
                    }
                    else
                    {
                        ///////////////////////////////////////////////////////////////////////
                        //SI LA CANTIDAD PEDIDA ES MEEEEENNNNOOORRRR  AL SALDO DISPONIBLE
                        ///////////////////////////////////////////////////////////////////////
                        string miudm = row.udm.ToString().ToUpper();

                        if (miudm == "KG")
                        {
                            mi_cantidad = Math.Round(cantidad, 2);
                            row.cantidad = (decimal)mi_cantidad;

                            if (string.IsNullOrWhiteSpace(msg_obs_revision_saldos_kg))
                            {
                                msg_obs_revision_saldos_kg = "LAS CANTIDADES DE LOS SIGUIENTES ITEMS EN (KG) SE REDONDEARON A DOS DECIMALES:";
                                msg_obs_revision_saldos_kg += $"\n{coditem} = {cantidad} => {mi_cantidad} (KG)";
                            }
                            else
                            {
                                msg_obs_revision_saldos_kg += $"\n{coditem} = {cantidad} => {mi_cantidad} (KG)";
                            }
                        }
                        else
                        {
                            cadena_cantidad = row.cantidad.ToString();
                            vector_cantidad = cadena_cantidad.Split('.');

                            txtentero1 = vector_cantidad[0];
                            txtdecimal1 = vector_cantidad.Length > 1 ? vector_cantidad[1] : "0";

                            row.cantidad = Convert.ToDecimal(txtentero1);

                            if (Convert.ToInt32(txtdecimal1) > 0)
                            {
                                if (string.IsNullOrWhiteSpace(msg_obs_revision_saldos_pz))
                                {
                                    msg_obs_revision_saldos_pz = "LAS CANTIDADES DE LOS SIGUIENTES ITEMS (PZ) SE MODIFICARON, SE ELIMINARON LOS DECIMALES:";
                                    msg_obs_revision_saldos_pz += $"\n{coditem} = {saldo} => {txtentero1} (PZ)";
                                }
                                else
                                {
                                    msg_obs_revision_saldos_pz += $"\n{coditem} = {saldo} => {txtentero1} (PZ)";
                                }
                            }
                        }
                    }
                }
                alerta = "";
                resultado = true;
                return (resultado, alerta, msg_obs_revision_saldos_kg, msg_obs_revision_saldos_pz, dt_disminuir, tabladetalle);
            }
            else
            {
                alerta = "Debe indicar el almacen de origen, destino y el concepto de la nota de movimiento.";
                resultado = false;
                return (resultado, alerta, msg_obs_revision_saldos_kg, msg_obs_revision_saldos_pz, dt_disminuir, tabladetalle);
            }
        }


        // boton Cargar de proforma
        [HttpPost]
        [Route("CargardeProforma/{userConn}")]
        public async Task<ActionResult<List<object>>> CargardeProforma(string userConn, requestCargarProforma requestCargarProforma)
        {
            bool resultado = true;
            bool es_proforma_de_sol_urgente = true;
            string documento = "proforma";
            int codalmorigen = 0;
            int codproforma = 0;
            string obs = "";
            try
            {
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);
                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    string codempresa = requestCargarProforma.codempresa;
                    string id_proforma_sol = requestCargarProforma.id_proforma_sol;
                    int numeroidproforma_sol = requestCargarProforma.numeroidproforma_sol;
                    int codconcepto = requestCargarProforma.codconcepto;
                    string usuario = requestCargarProforma.usuario;
                    bool desglozar_cjtos = requestCargarProforma.desglozar_cjtos;
                    List<tablaDetalleNM> tablaDetalle = new List<tablaDetalleNM>();

                    var result = await validar_proforma_urgente(_context, id_proforma_sol, numeroidproforma_sol, codconcepto);
                    if (result.resultado)
                    {
                        var codigo_proforma = await _context.veproforma.Where(i => i.id == id_proforma_sol && i.numeroid == numeroidproforma_sol && i.aprobada == true && i.es_sol_urgente == true)
                        .Select(i => new
                        {
                            i.codigo,
                            i.codalmacen,
                            i.obs
                        }).FirstOrDefaultAsync();

                        if (codigo_proforma == null)
                        {
                            return BadRequest(new { resp = "La proforma que intenta buscar no se encuentra aprobada. No se puede Transferir." });
                        }
                        else
                        {
                            //transferir
                            //transferirdatosproforma
                            codproforma = codigo_proforma.codigo;
                            codalmorigen = codigo_proforma.codalmacen;
                            obs = codigo_proforma.obs;
                            if (desglozar_cjtos)
                            {
                                //implemmentado en fecha 19-11-2021
                                //transferirdetalleproforma_desglozado(codigodoc)
                                //se totaliza los items para que no hayan codigo repetidos
                                // Consulta para la primera parte del UNION
                                var parteNoDesglosada = from p1 in _context.veproforma1
                                                        join p2 in _context.initem on p1.coditem equals p2.codigo
                                                        where p1.codproforma == codproforma &&
                                                              !_context.initem.Any(item => item.codigo == p1.coditem && item.kit == true)
                                                        select new
                                                        {
                                                            coditem = p1.coditem,
                                                            descripcion = p2.descripcion,
                                                            medida = p2.medida,
                                                            udm = p2.unidad,
                                                            codaduana = "",
                                                            cantaut = p1.cantaut
                                                        };

                                // Consulta para la segunda parte del UNION
                                var parteDesglosada = from p1 in _context.veproforma1
                                                      join p2 in _context.initem on p1.coditem equals p2.codigo
                                                      join p3 in _context.inkit on p1.coditem equals p3.codigo
                                                      where p1.codproforma == codproforma &&
                                                            _context.initem.Any(item => item.codigo == p1.coditem && item.kit == true)
                                                      select new
                                                      {
                                                          coditem = p3.item,
                                                          descripcion = p2.descripcion,
                                                          medida = p2.medida,
                                                          udm = p2.unidad,
                                                          codaduana = "",
                                                          cantaut = p1.cantaut * p3.cantidad
                                                      };

                                // Combinar ambas consultas usando UNION
                                var consultaUnion = parteNoDesglosada.Union(parteDesglosada);

                                // Agrupar y proyectar en tablaDetalleNM
                                List<tablaDetalleNM> detalle = await consultaUnion
                                    .GroupBy(x => new { x.coditem, x.descripcion, x.medida, x.udm, x.codaduana })
                                    .Select(g => new tablaDetalleNM
                                    {
                                        coditem = g.Key.coditem,
                                        descripcion = g.Key.descripcion,
                                        medida = g.Key.medida,
                                        udm = g.Key.udm,
                                        codaduana = g.Key.codaduana,
                                        cantidad = (decimal)g.Sum(x => x.cantaut) // Aquí se asigna la suma al campo cantidad
                                    })
                                    .OrderBy(x => x.coditem)
                                    .ToListAsync();

                                if (detalle == null || detalle.Count == 0)
                                {
                                    return BadRequest(new { resp = "Esta proforma no tiene items." });
                                }
                                else
                                {
                                    tablaDetalle = detalle;

                                    //foreach (var dato in detalle)
                                    //{
                                    //    var nuevoRegistro = new tablaDetalleNM
                                    //    {
                                    //        coditem = dato.coditem,
                                    //        descripcion = dato.descripcion,
                                    //        medida = dato.medida,
                                    //        udm = dato.udm,
                                    //        codaduana = dato.codaduana,
                                    //        cantidad = dato.cantidad
                                    //    };

                                    //    tablaDetalle.Add(nuevoRegistro);
                                    //}
                                }
                            }
                            else
                            {
                                // transferirdetalleproforma(codigodoc);
                                var detalle = await _context.veproforma1
                                    .Join(
                                        _context.initem,
                                        p1 => p1.coditem,
                                        p2 => p2.codigo,
                                        (p1, p2) => new { p1, p2 }
                                    )
                                    .Where(joined => joined.p1.codproforma == codproforma)
                                    .Select(joined => new tablaDetalleNM
                                    {
                                        coditem = joined.p1.coditem,
                                        descripcion = joined.p2.descripcion,
                                        medida = joined.p2.medida,
                                        udm = joined.p2.unidad,
                                        codaduana = string.Empty, // Valor constante
                                        cantidad = (decimal)joined.p1.cantaut
                                    })
                                    .ToListAsync();

                                if (detalle == null || detalle.Count == 0)
                                {
                                    return BadRequest(new { resp = "Esta proforma no tiene items." });
                                }
                                else
                                {
                                    tablaDetalle = detalle;

                                    //foreach (var dato in detalle)
                                    //{
                                    //    var nuevoRegistro = new tablaDetalleNM
                                    //    {
                                    //        coditem = dato.coditem,
                                    //        descripcion = dato.descripcion,
                                    //        medida = dato.medida,
                                    //        udm = dato.udm,
                                    //        codaduana = dato.codaduana,
                                    //        cantidad = dato.cantidad
                                    //    };

                                    //    tablaDetalle.Add(nuevoRegistro);
                                    //}
                                }
                            }

                            return Ok(new
                            {
                                resultado = result.resultado,
                                alerta = result.alerta,
                                codalmorigen = codalmorigen,
                                obs = obs,
                                tabladetalle = tablaDetalle
                            });
                        }
                    }
                    else
                    {
                        return BadRequest(new { resp = result.alerta });
                    }

                }
            }
            catch (Exception ex)
            {
                return Problem("Error en el servidor al cargar lso datos desde una proforma urgente. " + ex.Message);
                throw;
            }
        }
        private async Task<(bool resultado, string alerta)> validar_proforma_urgente(DBContext _context, string id_proforma_sol, int numeroidproforma_sol, int codconcepto)
        {
            bool resultado = true;
            string alerta = "";

            if (String.IsNullOrEmpty(id_proforma_sol))
            {
                resultado = false;
                alerta = "Debe poner el ID del tipo de Proforma de la que desea transferir.";
                return (resultado, alerta);
            }
            if (numeroidproforma_sol <= 0)
            {
                resultado = false;
                alerta = "Debe poner el numero de Proforma de la que desea transferir.";
                return (resultado, alerta);
            }
            if (await ventas.Existe_Proforma(_context, id_proforma_sol, numeroidproforma_sol) == false)
            {
                resultado = false;
                alerta = "La proforma no existe, no puede cargar esta proforma.";
                return (resultado, alerta);
            }
            if (await Ventas.proforma_anulada(_context, await ventas.codproforma(_context, id_proforma_sol, numeroidproforma_sol)) == true)
            {
                resultado = false;
                alerta = "La proforma esta anulada, no puede cargar esta proforma.";
                return (resultado, alerta);
            }
            if (await ventas.proforma_aprobada(_context, await ventas.codproforma(_context, id_proforma_sol, numeroidproforma_sol)) == false)
            {
                resultado = false;
                alerta = "La proforma no esta aprobada, no puede cargar esta proforma.";
                return (resultado, alerta);
            }
            if (await ventas.proforma_para_aprobar(_context, await ventas.codproforma(_context, id_proforma_sol, numeroidproforma_sol)) == false)
            {
                resultado = false;
                alerta = "La proforma no esta para aprobar, no puede cargar esta proforma.";
                return (resultado, alerta);
            }
            if (await ventas.proforma_transferida(_context, await ventas.codproforma(_context, id_proforma_sol, numeroidproforma_sol)) == true)
            {
                resultado = false;
                alerta = "La proforma ya esta transferida, no puede cargar esta proforma.";
                return (resultado, alerta);
            }
            if (await ventas.proforma_es_sol_urgente(_context, id_proforma_sol, numeroidproforma_sol) == false)
            {
                resultado = false;
                alerta = "La proforma seleccionada no es de una solicitud urgente de tienda!!!.";
                return (resultado, alerta);
            }
            if (codconcepto != 10)
            {
                resultado = false;
                alerta = "Solo puede cargar una proforma si el concepto es para una venta urgente.";
                return (resultado, alerta);
            }
            resultado = true;
            alerta = "";
            return (resultado, alerta);
        }


    }



    public class tablaDetalleNM
    {
        public string coditem { get; set; }
        public string descripcion { get; set; }
        public string medida { get; set; }
        public string udm { get; set; }
        public string codaduana { get; set; }
        public decimal cantidad { get; set; }
    }

    public class respValidaDecimales
    {
        public string cabecera { get; set; }
        public List<string> detalleObs { get; set; }
        public string alerta { get; set; }
        public bool cumple { get; set; }
    }


    public class dt_disminuir
    {
        public string coditem_kit { get; set; }
        public decimal cantidad_kit { get; set; }
        public string coditem { get; set; }
        public decimal cantidad { get; set; }

    }
    public class requestValidaSaldos
    {
        public string codempresa { get; set; }
        public string usuario { get; set; }
        public int codalmorigen { get; set; }
        public int codalmdestino { get; set; }
        public int codconcepto { get; set; }
        public List<tablaDetalleNM> tabladetalle { get; set; }

    }

    public class requestCargarProforma
    {
        public string codempresa { get; set; }
        public string usuario { get; set; }
        public string id_proforma_sol { get; set; }
        public int numeroidproforma_sol { get; set; }
        public int codconcepto { get; set; }
        public bool desglozar_cjtos { get; set; }

    }
    public class requestTotalizar
    {
        public List<tablaDetalleNM> tabladetalle { get; set; }

    }

}
