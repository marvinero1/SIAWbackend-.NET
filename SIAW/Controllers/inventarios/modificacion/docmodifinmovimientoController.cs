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
using LibSIAVB;
using System.Data;

namespace SIAW.Controllers.inventarios.modificacion
{
    [Route("api/inventario/modif/[controller]")]
    [ApiController]
    public class docmodifinmovimientoController : ControllerBase
    {
        private readonly Log log = new Log();
        private readonly Documento documento = new Documento();
        private readonly Funciones funciones = new Funciones();
        private readonly Saldos saldos = new Saldos();
        private readonly Inventario inventario = new Inventario();
        private readonly Almacen almacen = new Almacen();
        private readonly Seguridad seguridad = new Seguridad();
        private readonly Restricciones restricciones = new Restricciones();
        private readonly Items item = new Items();
        private readonly Ventas ventas = new Ventas();

        private readonly string _controllerName = "docinmovimientoController";

        private readonly UserConnectionManager _userConnectionManager;
        public docmodifinmovimientoController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }


        [HttpPost]
        [Route("grabarDocumento/{userConn}/{codempresa}/{traspaso}")]
        public async Task<ActionResult<object>> grabarDocumento(string userConn, string codempresa, bool traspaso, requestGabrar dataGrabar)
        {
            try
            {

                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);
                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    bool NMAAnulado = await _context.inmovimiento.Where(i=> i.codigo == dataGrabar.cabecera.codigo).Select(i => i.anulada).FirstOrDefaultAsync() ?? true;

                    if (NMAAnulado == true)
                    {
                        return BadRequest(new
                        {
                            resp = "Esta Nota de Movimiento esta Anulada, no puede ser modificada.",
                            valido = false,
                        });
                    }

                    List<tablaDetalleNM> tablaDetalle = dataGrabar.tablaDetalle;
                    // calculartotal.PerformClick()
                    // borrar items con cantidad 0 o menor
                    tablaDetalle = tablaDetalle.Where(i => i.cantidad > 0).ToList();


                    var guardarDoc = await editardatos(_context, codempresa, traspaso, dataGrabar.cabecera, tablaDetalle);
                    if (guardarDoc.valido == false)
                    {
                        if (guardarDoc.dtnegativos != null)
                        {
                            return StatusCode(203, new
                            {
                                resp = guardarDoc.msg,
                                negativos = guardarDoc.dtnegativos,
                                valido = guardarDoc.valido
                            });
                        }
                        return BadRequest(new
                        {
                            resp = guardarDoc.msg,
                            negativos = guardarDoc.dtnegativos,
                            valido = guardarDoc.valido
                        });
                    }
                    await log.RegistrarEvento(_context, dataGrabar.cabecera.usuarioreg, Log.Entidades.SW_Nota_Movimiento, guardarDoc.codigoNM.ToString(), dataGrabar.cabecera.id, guardarDoc.numeroID.ToString(), _controllerName, "Grabar", Log.TipoLog.Creacion);
                }
                return Ok(new { resp = "Nota de Movimiento creada exitosamente." });
            }
            catch (Exception ex)
            {
                return Problem("Error en el servidor al guardar Documento: " + ex.Message);
                throw;
            }
        }

        private async Task<(bool valido, string msg, List<Dtnegativos>? dtnegativos, int codigoNM, int numeroID)> editardatos(DBContext _context, string codempresa, bool traspaso, inmovimiento inmovimiento, List<tablaDetalleNM> tablaDetalle)
        {
            int factor = inmovimiento.factor;
            // preparacion de datos 
            inmovimiento.fecha = inmovimiento.fecha.Date;
            inmovimiento.factor = (short)factor;
            var fechaPrueba = await funciones.FechaDelServidor(_context);
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


                    // #######################################
                    // ##RUTINA QUE ACTUALIZA EL SALDO ACTUAL  (DESCONTAR)
                    // sia_funciones.Saldos.Instancia.Inmovimiento_ActualizarSaldo(codigo.Text, sia_funciones.Saldos.modo_actualizacion.eliminar, tabladetalle)
                    await saldos.Inmovimiento_ActualizarSaldo(_context, inmovimiento.codigo, Saldos.ModoActualizacion.Eliminar);
                    // #######################################


                    await inventario.actaduanamovimiento(_context, inmovimiento.codigo, "eliminar", inmovimiento.fecha);


                    // prepara detalle Nota Movimiento, ademas no se guardan items con cantidad revisada en 0
                    List<inmovimiento1> inmovimiento1 = tablaDetalle.Where(i => i.cantidad_revisada != 0).Select(i => new inmovimiento1
                    {
                        codmovimiento = inmovimiento.codigo,
                        coditem = i.coditem,
                        cantidad = i.cantidad_revisada,
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
                            // actualiza cabecera
                            try
                            {
                                _context.Entry(inmovimiento).State = EntityState.Modified;
                                await _context.SaveChangesAsync();
                            }
                            catch (Exception ex)
                            {
                                return (false, "Error al grabar la cabecera de la nota de movimiento: " + ex.Message, null, 0, 0);
                            }





                            // guarda detalle (veproforma1)
                            // actualizar codigoNM para agregar por si acaso
                            inmovimiento1 = inmovimiento1.Select(p => { p.codmovimiento = inmovimiento.codigo; return p; }).ToList();

                            // elimina primero, luego guardar detalle o items nuevos
                            var detalleNMAnt = await _context.inmovimiento1.Where(i => i.codmovimiento == inmovimiento.codigo).ToListAsync();
                            if (detalleNMAnt.Count() > 0)
                            {
                                _context.inmovimiento1.RemoveRange(detalleNMAnt);
                                await _context.SaveChangesAsync();
                            }
                            _context.inmovimiento1.AddRange(inmovimiento1);
                            await _context.SaveChangesAsync();
                            dbContexTransaction.Commit();
                        }
                        catch (Exception ex)
                        {
                            dbContexTransaction.Rollback();
                            return (false, $"Error en el servidor al guardar Proforma: {ex.Message}", null, 0, 0);
                            throw;
                        }

                    }

                    /*
                          DESPUES DE GUARDAR DETALLE CABECERA Y NUMERO DE ID
                        '//#######################################
                        '//##RUTINA QUE ACTUALIZA EL SALDO ACTUAL
                        'sia_funciones.Saldos.Instancia.Inmovimiento_ActualizarSaldo(codigo.Text, sia_funciones.Saldos.modo_actualizacion.crear, tabladetalle)
                        ''Desde 23/11/2023 verificar si la actualizacion de saldo es true oi false para registrar un registro de que no se pudo actualizar
                         */

                    // ACTUALIZAR POR PROCEDIMIENTO ALMACENADO

                    if (await saldos.Inmovimiento_ActualizarSaldo(_context, inmovimiento.codigo, Saldos.ModoActualizacion.Crear) == false)
                    {
                        await log.RegistrarEvento(_context, inmovimiento.usuarioreg, Log.Entidades.SW_Nota_Movimiento, inmovimiento.codigo.ToString(), inmovimiento.id, inmovimiento.numeroid.ToString(), _controllerName, "No actualizo stock en cantidad de algun item en NM.", Log.TipoLog.Creacion);
                    }
                    // #######################################

                    await inventario.actaduanamovimiento(_context, inmovimiento.codigo, "crear", inmovimiento.fecha);

                    //Desde 29/09/2024 Registrar el log de items modificados registrando la cantidad del item que era originalmente antes de la modificacion
                    foreach (var reg in tablaDetalle)
                    {
                        if (reg.nuevo == "no")
                        {
                            // actualiza el item ya existente
                            if (reg.cantidad_revisada == 0) // se elimino Item
                            {
                                await log.RegistrarEvento(_context, inmovimiento.usuarioreg, Log.Entidades.SW_Nota_Movimiento, inmovimiento.codigo.ToString(), inmovimiento.id, inmovimiento.numeroid.ToString(), _controllerName, "Item Eliminado: " + reg.coditem + " De: " + Math.Round(reg.cantidad, 2) + " A " + reg.cantidad_revisada, Log.TipoLog.Eliminacion); 
                            }
                            else
                            {
                                await log.RegistrarEvento(_context, inmovimiento.usuarioreg, Log.Entidades.SW_Nota_Movimiento, inmovimiento.codigo.ToString(), inmovimiento.id, inmovimiento.numeroid.ToString(), _controllerName, "Item Modificado: " + reg.coditem + " De: " + Math.Round(reg.cantidad, 2) + " A " + reg.cantidad_revisada, Log.TipoLog.Modificacion);
                            }
                        }
                        else
                        {
                            // inserta añadir el item nuevo
                            await log.RegistrarEvento(_context, inmovimiento.usuarioreg, Log.Entidades.SW_Nota_Movimiento, inmovimiento.codigo.ToString(), inmovimiento.id, inmovimiento.numeroid.ToString(), _controllerName, "Item Añadido: " + reg.coditem + " De: " + Math.Round(reg.cantidad, 2) + " A " + reg.cantidad_revisada, Log.TipoLog.Modificacion);
                        }
                    }




                    return (true, "", null, inmovimiento.codigo, inmovimiento.numeroid);

                }
                return (false, validaCabecera.msg, null, 0, 0);
            }

            return (false, validaDetalle.msg, validaDetalle.dtnegativos, 0, 0);
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
            if (inmovimiento.fnumeroid == null || inmovimiento.fnumeroid < 0)
            {
                return (false, "No puede dejar la casilla de Numero de Documento de origen  en blanco.");
            }

            if (await inventario.concepto_espara_despacho(_context, inmovimiento.codconcepto) && inmovimiento.codconcepto == 10 && await almacen.Es_Tienda(_context, (int)inmovimiento.codalmdestino))
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
                if (inmovimiento.numeroidproforma == null || inmovimiento.numeroidproforma <= 0)
                {
                    return (false, "No puede dejar la casilla de Numero de la PF que hizo la solicitud urgente en blanco o con valor 0.");
                }
                if (await ventas.proforma_es_sol_urgente(_context, inmovimiento.idproforma_sol, inmovimiento.numeroidproforma_sol ?? 0) == false)
                {
                    return (false, "La proforma enlazada es NO es de una solicitud urgente de Tienda,verifique esta situacion.");
                }
            }

            if (await seguridad.periodo_abierto_context(_context, inmovimiento.fecha.Year, inmovimiento.fecha.Month, 2) == false)
            {
                return (false, "No puede crear documentos para ese periodo de fechas.");
            }

            if (await restricciones.ValidarModifDocAntesInventario(_context, inmovimiento.codalmacen, inmovimiento.fecha) == false)
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

            if (inmovimiento.numeroidproforma == null)
            {
                inmovimiento.numeroidproforma = 0;
            }
            if (inmovimiento.numeroidproforma_sol == null)
            {
                inmovimiento.numeroidproforma_sol = 0;
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
            var lista_repetidos = tablaDetalle
                .GroupBy(reg => reg.coditem) // Agrupa por coditem
                .Where(grupo => grupo.Count() > 1) // Filtra los grupos con más de un elemento
                .Select(grupo => grupo.Key) // Obtén el coditem repetido
                .ToList(); // Convierte a lista

            var cadena_repetidos = string.Join(", ", lista_repetidos); // Convierte la lista a un string separado por comas

            return cadena_repetidos;
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
                if (await item.iteminactivo(_context, reg.coditem))
                {
                    return (false, "El item de la linea " + indice + " -> " + reg.coditem + " no puede ser movido por que esta inactivo.", null);
                }
                if (await item.item_usar_en_movimiento(_context, reg.coditem) == false)
                {
                    return (false, "El item de la linea " + indice + " -> " + reg.coditem + " no puede usarse en notas de movimiento.", null);
                }
                if (string.IsNullOrWhiteSpace(reg.udm))
                {
                    return (false, "No puso la Unidad de Medida en la Linea " + indice + " .", null);
                }
                if (reg.cantidad_revisada == null)
                {
                    return (false, "No puso la cantidad en la Linea " + indice + " .", null);
                }
                if (reg.cantidad_revisada <= 0)
                {
                    // si es item nuevo alerta si la cantidad revisada es cero
                    // si no es nuevo, si se permite cantidad_revisada=0
                    if (reg.nuevo == "si")
                    {
                        return (false, "No puso la cantidad revisada en la Linea " + indice + " .", null);
                    }
                }
                indice++;
            }

            // validar negativos
            if ((factor == -1) && (await inventario.PermitirNegativos(_context, codempresa) == false))
            {
                List<string> msgs = new List<string>();
                List<string> negs = new List<string>();

                var validaNegativos = await Validar_Saldos_Negativos_Doc(_context, codempresa, inmovimiento, tablaDetalle);
                if (validaNegativos.resultado == false)   ////////////////////////////// VERIFICAR SI DEBE PERMITIR GRABAR CON CLAVE, EN EL SIA LO HACE
                {
                    return (false, validaNegativos.msg, validaNegativos.dtnegativos);
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


            return (true, "", null);
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


        private async Task<(bool resultado, string msg, List<Dtnegativos> dtnegativos)> Validar_Saldos_Negativos_Doc(DBContext _context, string codempresa, inmovimiento inmovimiento, List<tablaDetalleNM> tablaDetalle)
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
            return (resultado, msg, dtnegativos);
        }


    }




   
    

}
