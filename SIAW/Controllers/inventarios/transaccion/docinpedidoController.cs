using LibSIAVB;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIAW.Controllers.ventas.transaccion;
using siaw_DBContext.Data;
using siaw_DBContext.Models;
using siaw_DBContext.Models_Extra;
using siaw_funciones;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Globalization;

namespace SIAW.Controllers.inventarios.transaccion
{
    [Route("api/inventario/transac/[controller]")]
    [ApiController]
    public class docinpedidoController : ControllerBase
    {
        private readonly string _controllerName = "docinpedidoController";

        private readonly Configuracion configuracion = new Configuracion();
        private readonly Nombres nombres = new Nombres();
        private readonly Seguridad seguridad = new Seguridad();
        private readonly Documento documento = new Documento();
        private readonly Saldos saldos = new Saldos();
        private readonly Items items = new Items();
        private readonly Empresa empresa = new Empresa();
        private readonly empaquesFunciones empaque_func = new empaquesFunciones();
        private readonly Log log = new Log();
        private readonly Funciones funciones = new Funciones();
        private readonly Ventas ventas = new Ventas();

        private readonly func_encriptado encripVB = new func_encriptado();

        private readonly UserConnectionManager _userConnectionManager;
        public docinpedidoController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }
        [HttpGet]
        [Route("getParametrosInicialesPedido/{userConn}/{usuario}")]
        public async Task<ActionResult<object>> getParametrosInicialesPedido(string userConn, string usuario)
        {
            try
            {
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);
                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    int codalmacen = await configuracion.usr_codalmacen(_context, usuario);
                    string codalmacendescripcion = await nombres.nombrealmacen(_context, codalmacen);
                    return Ok(new
                    {
                        codalmacen,
                        codalmacendescripcion
                    });
                }
            }
            catch (Exception ex)
            {
                return Problem("Error en el servidor al obtener parametros iniciales Pedido: " + ex.Message);
                throw;
            }
        }




        [HttpPost]
        [Route("grabarDocumento/{userConn}/{codempresa}")]
        public async Task<ActionResult<object>> grabarDocumento(string userConn, string codempresa, requestGabrarPedido dataGrabar)
        {
            try
            {
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);
                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    inpedido cabecera = dataGrabar.cabecera;
                    cabecera.id = cabecera.id.Trim();
                    cabecera.obs = cabecera.obs.Trim();
                    cabecera.fecha = cabecera.fecha.Date;

                    List<inpedido1> detalle = dataGrabar.tablaDetalle;
                    // valida detalle
                    var detalleValido = await validarDetalle(_context,detalle);
                    if (detalleValido.valido == false)
                    {
                        return BadRequest(new { detalleValido.valido, resp = detalleValido.msg });
                    }
                    // valida cabecera
                    var cabeceraValido = await validardatos(_context,cabecera);
                    if (cabeceraValido.valido == false)
                    {
                        return BadRequest(new { cabeceraValido.valido, resp = cabeceraValido.msg });
                    }
                    // manda a grabar
                    var docGrabado = await guardarNuevoDocumento(_context, cabecera, detalle);
                    if (docGrabado.valido == false)
                    {
                        return BadRequest(new { docGrabado.valido, resp = docGrabado.msg });
                    }

                    await log.RegistrarEvento(_context, cabecera.usuarioreg, Log.Entidades.SW_Pedido, docGrabado.codigoPedido.ToString(), cabecera.id, docGrabado.numeroID.ToString(), _controllerName, "Crear Pedido", Log.TipoLog.Creacion);

                    return Ok(new
                    {
                        docGrabado.valido,
                        resp = "Se genero el pedido " + cabecera.id + " - " + docGrabado.numeroID + " con exito.",
                        docGrabado.codigoPedido,
                    });
                }
            }
            catch (Exception ex)
            {
                return Problem("Error en el servidor al guardar Documento: " + ex.Message);
                throw;
            }
        }
        private async Task<(bool valido, string msg)> validarDetalle(DBContext _context, List<inpedido1> tablaDetalle)
        {
            int indice = 1;
            if (tablaDetalle.Count() <= 0)
            {
                return (false, "No tiene ningun item en su pedido.");
            }
            foreach (var reg in tablaDetalle)
            {
                if (string.IsNullOrWhiteSpace(reg.coditem))
                {
                    return (false, "No eligio El Item en la Linea " + indice + " .");
                }
                if (reg.coditem.Trim().Length < 1)
                {
                    return (false, "No eligio El Item en la Linea " + indice + " .");
                }

                if (string.IsNullOrWhiteSpace(reg.udm))
                {
                    return (false, "No puso la Unidad de Medida en la Linea " + indice + " .");
                }
                if (reg.udm.Trim().Length < 1)
                {
                    return (false, "No puso la Unidad de Medida en la Linea " + indice + " .");
                }

                if (reg.cantidad == null)
                {
                    return (false, "No puso la cantidad en la Linea " + indice + " .");
                }
                if (reg.cantidad <= 0)
                {
                    return (false, "La cantidad en la Linea " + indice + " No puede ser menor o igual a 0.");
                }
                indice++;
            }
            return (true, "");
        }
        private async Task<(bool valido, string msg)> validardatos(DBContext _context, inpedido cabecera)
        {
            // validar casillas vacias
            if (string.IsNullOrWhiteSpace(cabecera.id))
            {
                return (false, "No puede dejar el tipo de pedido en blanco.");
            }
            if (cabecera.numeroid == null || cabecera.numeroid <= 0)
            {
                return (false, "No puede dejar el número ID de pedido en blanco.");
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
            if (await seguridad.periodo_fechaabierta_context(_context,cabecera.fecha.Date,2) == false)
            {
                return (false, "No puede crear documentos para ese periodo de fechas.");
            }
            return (true, "");
        }

        private async Task<(bool valido, string msg, int codigoPedido, int numeroID)> guardarNuevoDocumento(DBContext _context, inpedido inpedido, List<inpedido1> inpedido1)
        {
            int codPedido = 0;
            using (var dbContexTransaction = _context.Database.BeginTransaction())
            {
                try
                {
                    inpedido.numeroid = await documento.pedidonumeroid(_context, inpedido.id) + 1;
                    if (inpedido.numeroid <= 0)
                    {
                        return (false, "Error al generar numero ID, consulte con el Administrador", 0, 0);
                    }
                    if (await documento.existe_movimiento(_context, inpedido.id, inpedido.numeroid ?? -1))
                    {
                        return (false, "Ese numero de documento, ya existe, por favor consulte con el administrador del sistema.", 0, 0);
                    }

                    // agregar cabecera
                    try
                    {
                        _context.inpedido.Add(inpedido);
                        await _context.SaveChangesAsync();
                    }
                    catch (Exception ex)
                    {
                        return (false, "Error al grabar la cabecera del Pedido: " + ex.Message, 0, 0);
                    }
                    codPedido = inpedido.codigo;

                    // actualiza numero id
                    var numeracion = _context.intipopedido.FirstOrDefault(n => n.id == inpedido.id);
                    numeracion.nroactual += 1;
                    await _context.SaveChangesAsync(); // Guarda los cambios en la base de datos


                    int validaCantProf = await _context.inpedido.Where(i => i.id == inpedido.id && i.numeroid == inpedido.numeroid).CountAsync();
                    if (validaCantProf > 1)
                    {
                        return (false, "Se detecto más de un número del mismo documento, por favor consulte con el administrador del sistema.", 0, 0);
                    }

                    // guarda detalle (veproforma1)
                    // actualizar codigoNM para agregar
                    inpedido1 = inpedido1.Select(p => { p.codpedido = codPedido; return p; }).ToList();
                    // guardar detalle
                    _context.inpedido1.AddRange(inpedido1);
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
            return (true, "", codPedido, inpedido.numeroid ?? -1);
        }



        [HttpGet]
        [Route("getSaldoStock/{userConn}/{coditem}/{usuario}/{codalmacen}/{codempresa}")]
        public async Task<ActionResult<object>> getSaldoStock(string userConn, string coditem, string usuario, int codalmacen, string codempresa)
        {
            try
            {
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);
                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    // se obtienen los almacenes
                    var listaAlmacenes = await _context.adusparametros.Where(i => i.usuario == usuario)
                        .Select(i => new
                        {
                            i.codalmsald1,
                            i.codalmsald2,
                            i.codalmsald3,
                            i.codalmsald4
                        })
                        .FirstOrDefaultAsync();
                    if (listaAlmacenes == null)
                    {
                        return BadRequest(new { resp = "No se pudo obtener los almacenes de saldos para este usuario" });
                    }

                    if (await items.existeitem(_context,coditem))
                    {
                        // MINIMO Y MAXIMO DE ITEM
                        var min_max = await _context.instockalm.Where(i => i.item == coditem && i.codalmpedido == codalmacen).Select(i => new
                        {
                            i.smax,
                            i.smin
                        }).FirstOrDefaultAsync();

                        string smin = (min_max.smin ?? 0).ToString("####,##0.000", new CultureInfo("en-US"));
                        string smax = (min_max.smax ?? 0).ToString("####,##0.000", new CultureInfo("en-US"));


                        // SALDOS
                        bool Es_Ag_Local = true;
                        bool Obtengo_Saldos_Otras_Ags_Localmente = await saldos.Obtener_Saldos_Otras_Agencias_Localmente(userConnectionString, codempresa);

                        string saldo1 = "";
                        string saldo2 = "";
                        string saldo3 = "";
                        string saldo4 = "";

                        if (await empresa.AlmacenLocalEmpresa(_context,codempresa) == codalmacen)
                        {
                            Es_Ag_Local = true;
                        }
                        else
                        {
                            Es_Ag_Local = false;
                        }

                        if (Es_Ag_Local == true || Obtengo_Saldos_Otras_Ags_Localmente == true)
                        {
                            saldo1 = listaAlmacenes.codalmsald1.ToString() + " : " + (await saldos.SaldoItem(_context, listaAlmacenes.codalmsald1 ?? 0, coditem)).ToString("####,##0.00", new CultureInfo("en-US"));
                            saldo2 = listaAlmacenes.codalmsald2.ToString() + " : " + (await saldos.SaldoItem(_context, listaAlmacenes.codalmsald2 ?? 0, coditem)).ToString("####,##0.00", new CultureInfo("en-US"));
                            saldo3 = listaAlmacenes.codalmsald3.ToString() + " : " + (await saldos.SaldoItem(_context, listaAlmacenes.codalmsald3 ?? 0, coditem)).ToString("####,##0.00", new CultureInfo("en-US"));
                            saldo4 = listaAlmacenes.codalmsald4.ToString() + " : " + (await saldos.SaldoItem(_context, listaAlmacenes.codalmsald4 ?? 0, coditem)).ToString("####,##0.00", new CultureInfo("en-US"));
                        }
                        else
                        {
                            /*
                            string conexion = await empaque_func.Getad_conexion_vpnFromDatabase_Sam(_context, listaAlmacenes.codalmsald1 ?? 0);
                            using (var _context2 = DbContextFactory.Create(conexion))
                            {
                                saldo1 = listaAlmacenes.codalmsald1.ToString() + " : " + (await saldos.SaldoItem(_context2, listaAlmacenes.codalmsald1 ?? 0, coditem)).ToString("####,##0.000", new CultureInfo("en-US"));
                            }

                            conexion = await empaque_func.Getad_conexion_vpnFromDatabase_Sam(_context, listaAlmacenes.codalmsald2 ?? 0);
                            using (var _context2 = DbContextFactory.Create(conexion))
                            {
                                saldo2 = listaAlmacenes.codalmsald2.ToString() + " : " + (await saldos.SaldoItem(_context2, listaAlmacenes.codalmsald2 ?? 0, coditem)).ToString("####,##0.000", new CultureInfo("en-US"));
                            }

                            conexion = await empaque_func.Getad_conexion_vpnFromDatabase_Sam(_context, listaAlmacenes.codalmsald3 ?? 0);
                            using (var _context2 = DbContextFactory.Create(conexion))
                            {
                                saldo3 = listaAlmacenes.codalmsald3.ToString() + " : " + (await saldos.SaldoItem(_context2, listaAlmacenes.codalmsald3 ?? 0, coditem)).ToString("####,##0.000", new CultureInfo("en-US"));
                            }

                            conexion = await empaque_func.Getad_conexion_vpnFromDatabase_Sam(_context, listaAlmacenes.codalmsald4 ?? 0);
                            using (var _context2 = DbContextFactory.Create(conexion))
                            {
                                saldo4 = listaAlmacenes.codalmsald4.ToString() + " : " + (await saldos.SaldoItem(_context2, listaAlmacenes.codalmsald4 ?? 0, coditem)).ToString("####,##0.000", new CultureInfo("en-US"));
                            }
                            */

                            string[] saldosAlmacenes = new string[4];

                            // Obtener conexiones a la base de datos en paralelo
                            var conexiones = await Task.WhenAll(
                                empaque_func.Getad_conexion_vpnFromDatabase_Sam(_context, listaAlmacenes.codalmsald1 ?? 0),
                                empaque_func.Getad_conexion_vpnFromDatabase_Sam(_context, listaAlmacenes.codalmsald2 ?? 0),
                                empaque_func.Getad_conexion_vpnFromDatabase_Sam(_context, listaAlmacenes.codalmsald3 ?? 0),
                                empaque_func.Getad_conexion_vpnFromDatabase_Sam(_context, listaAlmacenes.codalmsald4 ?? 0) 
                            );

                            // Ejecutar las consultas de saldo en paralelo
                            var saldosTareas = new[]
                            {
                                Task.Run(async () =>
                                {
                                    using var _context2 = DbContextFactory.Create(conexiones[0]);
                                    saldosAlmacenes[0] = listaAlmacenes.codalmsald1.ToString() + " : " +
                                                         (await saldos.SaldoItem(_context2, listaAlmacenes.codalmsald1 ?? 0, coditem))
                                                         .ToString("####,##0.000", new CultureInfo("en-US"));
                                }),

                                Task.Run(async () =>
                                {
                                    using var _context2 = DbContextFactory.Create(conexiones[1]);
                                    saldosAlmacenes[1] = listaAlmacenes.codalmsald2.ToString() + " : " +
                                                         (await saldos.SaldoItem(_context2, listaAlmacenes.codalmsald2 ?? 0, coditem))
                                                         .ToString("####,##0.000", new CultureInfo("en-US"));
                                }),

                                Task.Run(async () =>
                                {
                                    using var _context2 = DbContextFactory.Create(conexiones[2]);
                                    saldosAlmacenes[2] = listaAlmacenes.codalmsald3.ToString() + " : " +
                                                         (await saldos.SaldoItem(_context2, listaAlmacenes.codalmsald3 ?? 0, coditem))
                                                         .ToString("####,##0.000", new CultureInfo("en-US"));
                                }),

                                Task.Run(async () =>
                                {
                                    using var _context2 = DbContextFactory.Create(conexiones[3]);
                                    saldosAlmacenes[3] = listaAlmacenes.codalmsald4.ToString() + " : " +
                                                         (await saldos.SaldoItem(_context2, listaAlmacenes.codalmsald4 ?? 0, coditem))
                                                         .ToString("####,##0.000", new CultureInfo("en-US"));
                                })
                            };

                            // Esperar a que todas las tareas finalicen
                            await Task.WhenAll(saldosTareas);

                            // Asignar los valores finales
                            saldo1 = saldosAlmacenes[0];
                            saldo2 = saldosAlmacenes[1];
                            saldo3 = saldosAlmacenes[2];
                            saldo4 = saldosAlmacenes[3];
                        }




                        return Ok(new
                        {
                            saldo1,
                            saldo2,
                            saldo3,
                            saldo4,
                            smin,
                            smax
                        });

                    }
                    else
                    {
                        return Ok(new
                        {
                            saldo1 = listaAlmacenes.codalmsald1.ToString() + " : 0",
                            saldo2 = listaAlmacenes.codalmsald2.ToString() + " : 0",
                            saldo3 = listaAlmacenes.codalmsald3.ToString() + " : 0",
                            saldo4 = listaAlmacenes.codalmsald4.ToString() + " : 0",
                            smin = "0",
                            smax = "0"
                        });
                    }
                    
                }
            }
            catch (Exception ex)
            {
                return Problem("Error en el servidor al obtener parametros iniciales Pedido: " + ex.Message);
                throw;
            }
        }



        // IMPORTAR 
        [HttpPost]
        [Route("importPedidoinJson")]
        public async Task<IActionResult> importPedidoinJson([FromForm] IFormFile file)
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
                return Problem("Error en el servidor al importar proforma en JSON: " + ex.Message);
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
        [Route("exportPedido/{userConn}/{codPedido}")]
        public async Task<IActionResult> exportPedido(string userConn, int codPedido)
        {
            try
            {
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);
                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var result = await cargariedataset(_context, codPedido);
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


        private async Task<(bool resp, DataSet iedataset, string id, int numeroid)> cargariedataset(DBContext _context, int codPedido)
        {
            DataSet iedataset = new DataSet();

            try
            {
                iedataset.Clear();
                iedataset.Reset();

                // cargar cabecera
                var dataPedido = await _context.inpedido.Where(i => i.codigo == codPedido).ToListAsync();
                if (dataPedido.Any())
                {
                    DataTable cabeceraTable = dataPedido.ToDataTable();
                    cabeceraTable.TableName = "cabecera";
                    iedataset.Tables.Add(cabeceraTable);
                    iedataset.Tables["cabecera"].Columns.Add("documento", typeof(string));
                    iedataset.Tables["cabecera"].Rows[0]["documento"] = "PEDIDO";

                }
                string id = dataPedido[0].id;
                int numeroid = (int)dataPedido[0].numeroid;
                /*
                // Añadir campo identificador
                iedataset.Tables["cabecera"].Columns.Add("documento", typeof(string));
                iedataset.Tables["cabecera"].Rows[0]["documento"] = "PROFORMA";

                */

                // Cargar detalle usando LINQ y Entity Framework
                var dataDetalle = await _context.inpedido1
                    .Where(p => p.codpedido == codPedido)
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
                string outName = Path.Combine(outputDirectory, id + "-" + numeroid + ".ped");

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
        [Route("getDataImpPedido/{userConn}/{codPedido}/{codempresa}/{usua}")]
        public async Task<ActionResult<List<object>>> getDataImpPedido(string userConn, int codPedido, string codempresa, string usua)
        {
            string tituloreporte = "PEDIDO DE MERCADERIA";
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
                    string rfecha = "";
                    string robs = "";
                    // string rcodvendedor = "";
                    //string rcodcliente = "";
                    //string rcodmoneda = "";
                    //string rtipopago = "";

                    // obtener los datos de cabecera
                    var cabecera = await _context.inpedido.Where(i => i.codigo == codPedido).FirstOrDefaultAsync();
                    if (cabecera == null)
                    {
                        return BadRequest(new { resp = "No se encontraron datos con el codigo proporcionado, consulte con el Administrador." });
                    }

                    titulo = tituloreporte + " " + cabecera.id + "-" + cabecera.numeroid;
                    rempresa = await nombres.nombreempresa(_context, codempresa);
                    usuario = usua;
                    nit = "N.I.T.: " + await empresa.NITempresa(_context, codempresa);
                    rcodigo = cabecera.codigo.ToString("00000000");
                    rcodalmacen = cabecera.codalmacen.ToString();
                    rcodalmdestino = cabecera.codalmdestino.ToString();
                    rfecha = cabecera.fecha.ToShortDateString();
                    robs = cabecera.obs;

                    ////////////////////////////////////////
                    //// Fin de pasar valores a las variables
                    ////////////////////////////////////////
                    var tablaDetalle = await _context.inpedido1.Where(i => i.codpedido == codPedido)
                            .Join(_context.initem,
                                m => m.coditem,
                                i => i.codigo,
                                (m, i) => new tablaDetallePedido
                                {
                                    coditem = m.coditem,
                                    descripcion = i.descripcion,
                                    medida = i.medida,
                                    udm = m.udm,
                                    cantidad = m.cantidad,
                                    codproveedor = m.codproveedor
                                }
                            )
                            .OrderBy(i => i.coditem)
                            .ToListAsync();

                    return Ok(new
                    {
                        empresa,
                        titulo,
                        usuario,
                        nit,
                        rcodigo,
                        rfecha,
                        rcodalmacen,
                        rcodalmdestino,
                        robs,

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






        // generar pedido automatico
        private async Task<(bool valido, string msg, int codigo, string ID, int numeroID)> GenerarPedidoAut(DBContext _context, string codempresa, string usuario, requestGenerarPedido datosPedido)
        {
            int codPedido = 0;
            int numero = 0;
            string id = datosPedido.id;
            int codalmacen = datosPedido.codalmacen;
            int codalmacendestino = datosPedido.codalmacen2;
            bool todos = datosPedido.todos;
            bool grupos = datosPedido.grupos;
            string gruposdesde = datosPedido.grupodesde;
            string gruposhasta = datosPedido.grupohasta;
            bool lineas = datosPedido.lineas;
            string lineasdesde = datosPedido.lineadesde;
            string lineashasta = datosPedido.lineahasta;
            bool items1 = datosPedido.items;
            string itemsdesde = datosPedido.itemdesde;
            string itemshasta = datosPedido.itemhasta;


            using (var dbContexTransaction = _context.Database.BeginTransaction())
            {
                try
                {
                    bool proforma_reserva = await configuracion.emp_proforma_reserva(_context, codempresa);
                    bool resultado = true;
                    bool pedidodecancelar = false;

                    //actualizarUI("Buscando Items con bajo stock");

                    //proceso que puede tardar su tiempo
                    //'aqui se saca primero las diferencias para sacar el pedido
                    //'PRIMERO SACAR DIFERENCIAS A PEDIR en tablapedido
                    // Obtener los items con bajo stock usando LINQ
                    List<ItemBajoStock> itemsBajoStock;
                    //if (proforma_reserva)
                    //{
                    if (todos)
                    {
                        itemsBajoStock = await ObtenerItemsBajoStock(_context, proforma_reserva, codalmacen, codalmacendestino, "todos", "", "");
                    }
                    else if (grupos)
                    {
                        itemsBajoStock = await ObtenerItemsBajoStock(_context, proforma_reserva, codalmacen, codalmacendestino, "grupos", gruposdesde, gruposhasta);
                    }
                    else if (lineas)
                    {
                        itemsBajoStock = await ObtenerItemsBajoStock(_context, proforma_reserva, codalmacen, codalmacendestino, "lineas", lineasdesde, lineashasta);
                    }
                    else if (items1)
                    {
                        itemsBajoStock = await ObtenerItemsBajoStock(_context, proforma_reserva, codalmacen, codalmacendestino, "items", itemsdesde, itemshasta);
                    }
                    else
                    {
                        itemsBajoStock = await ObtenerItemsBajoStock(_context, proforma_reserva, codalmacen, codalmacendestino, "todos", "", "");
                    }

                    // 🔹 Aplicar reglas de empaques (`inalmacen_automatico`)
                    var reglasEmpaque = _context.inalmacen_automatico
                        .Where(a => a.codalmacen == codalmacen)
                        .OrderBy(a => a.nro)
                        .ToList();

                    foreach (var item in itemsBajoStock)
                    {
                        foreach (var regla in reglasEmpaque)
                        {
                            //Primero validar por el campo codempaque = 37 EMP CAJA CERRADA FABRICA
                            double empaque = await ventas.Empaque(_context, (int)regla.codempaque, item.item);
                            if (empaque <= 0) continue;

                            double porcentaje = (double)(regla.porcen / 100);
                            double empaquesCerrados = item.apedir > empaque ? Math.Floor(item.apedir / empaque) : 0;
                            double apedir = item.apedir - (empaquesCerrados * empaque);

                            switch (regla.tipo)
                            {
                                case 0: // MENOR
                                    if (apedir < empaque * porcentaje)
                                    {
                                        apedir = empaque;
                                        apedir = apedir + (empaquesCerrados * empaque);
                                        item.apedir = apedir;
                                        continue;
                                    }
                                    break;
                                case 1: // MENOR O IGUAL
                                    if (apedir <= empaque * porcentaje)
                                    {
                                        apedir = empaque;
                                        apedir = apedir + (empaquesCerrados * empaque);
                                        item.apedir = apedir;
                                        continue;
                                    }
                                    else
                                    {
                                        item.apedir = apedir;
                                    }
                                    break;
                                case 2: // MAYOR O IGUAL
                                    if (apedir >= empaque * porcentaje && apedir <= empaque)
                                    {
                                        apedir = empaque;
                                        apedir = apedir + (empaquesCerrados * empaque);
                                        item.apedir = apedir;
                                        continue;
                                    }
                                    else if (apedir <= (empaque * porcentaje) && empaquesCerrados > 0)
                                    {
                                        apedir = empaque;
                                        apedir = apedir + (empaquesCerrados * empaque);
                                        item.apedir = apedir;
                                        continue;
                                    }
                                    else
                                    {
                                        item.apedir = apedir + (empaquesCerrados * empaque);
                                    }
                                    break;
                                case 3: // MAYOR
                                    if (apedir > empaque * porcentaje && apedir <= empaque)
                                    {
                                        apedir = empaque;
                                        apedir = apedir + (empaquesCerrados * empaque);
                                        item.apedir = apedir;
                                        continue;
                                    }
                                    else
                                    {
                                        item.apedir = apedir + (empaquesCerrados * empaque);
                                    }
                                    break;
                            }

                            //Luego validar por el campo codempaque = 36 EMP DIMEDIADOS
                            empaque = await ventas.Empaque(_context, (int)regla.codempaque_dimediado, item.item);
                            if (empaque <= 0) continue;

                            porcentaje = (double)(regla.porcen_dimediado / 100);
                            empaquesCerrados = item.apedir > empaque ? Math.Floor(item.apedir / empaque) : 0;
                            apedir = item.apedir - (empaquesCerrados * empaque);

                            switch (regla.tipo)
                            {
                                case 0: // MENOR
                                    if (apedir < empaque * porcentaje)
                                    {
                                        apedir = empaque;
                                        apedir = apedir + (empaquesCerrados * empaque);
                                        item.apedir = apedir;
                                        continue;
                                    }
                                    break;
                                case 1: // MENOR O IGUAL
                                    if (apedir <= empaque * porcentaje)
                                    {
                                        apedir = empaque;
                                        apedir = apedir + (empaquesCerrados * empaque);
                                        item.apedir = apedir;
                                        continue;
                                    }
                                    else
                                    {
                                        item.apedir = apedir;
                                    }
                                    break;
                                case 2: // MAYOR O IGUAL
                                    if (apedir >= empaque * porcentaje && apedir <= empaque)
                                    {
                                        apedir = empaque;
                                        apedir = apedir + (empaquesCerrados * empaque);
                                        item.apedir = apedir;
                                        continue;
                                    }
                                    else if (apedir <= (empaque * porcentaje) && empaquesCerrados > 0)
                                    {
                                        apedir = empaque;
                                        apedir = apedir + (empaquesCerrados * empaque);
                                        item.apedir = apedir;
                                        continue;
                                    }
                                    else
                                    {
                                        item.apedir = apedir + (empaquesCerrados * empaque);
                                    }
                                    break;
                                case 3: // MAYOR
                                    if (apedir > empaque * porcentaje && apedir <= empaque)
                                    {
                                        apedir = empaque;
                                        apedir = apedir + (empaquesCerrados * empaque);
                                        item.apedir = apedir;
                                        continue;
                                    }
                                    else
                                    {
                                        item.apedir = apedir + (empaquesCerrados * empaque);
                                    }
                                    break;
                            }
                        }
                    }
                    //FIN DE USAR CONTROLES INALMACEN_AUTOMATICO

                    //A LAS GRAMPAS CONTROLAR QUE ENVIE POR LO MENOS LA CANTIDAD DE TUERCAS REQUERIDAS
                    // 🔹 Ajustar cantidades de tuercas (`inkit`)
                    var aumentar = new List<(string item, double cantidad, int codalmpedido)>();

                    foreach (var item in itemsBajoStock)
                    {
                        if (string.Compare(item.item, "26IGBM14") >= 0 && string.Compare(item.item, "28IGDM63") <= 0)
                        {
                            //var tuercas = _context.inkit
                            //    .Where(k => k.codigo == item.item && k.item != item.item)
                            //    .Select(k => new { k.item, k.cantidad })
                            //    .ToList();

                            var codigoSubquery = await _context.inkit
                                .Where(k => k.item == item.item)
                                .Select(k => k.codigo)
                                .ToListAsync(); // Se obtiene la lista de códigos

                            var tuercas = await _context.inkit
                               .Where(k => codigoSubquery.Contains(k.codigo) && k.item != item.item)
                               .Select(k => new
                               {
                                   k.item,
                                   k.cantidad
                               })
                                .ToListAsync();


                            foreach (var tuerca in tuercas)
                            {
                                var existente = aumentar.FirstOrDefault(a => a.item == tuerca.item);
                                if (existente.item != null)
                                {
                                    aumentar = aumentar
                                        .Select(a => a.item == tuerca.item
                                            ? (a.item, a.cantidad + ((double)tuerca.cantidad * item.apedir), a.codalmpedido)
                                            : a)
                                        .ToList();
                                }
                                else
                                {
                                    aumentar.Add((tuerca.item, (double)tuerca.cantidad * item.apedir, item.codalmpedido));
                                }
                            }
                        }
                    }

                    // 🔹 Agregar tuercas al pedido si es necesario
                    foreach (var extra in aumentar)
                    {
                        var existente = itemsBajoStock.FirstOrDefault(i => i.item == extra.item);
                        if (existente != null)
                        {
                            existente.apedir = Math.Max(existente.apedir, extra.cantidad);
                        }
                        else
                        {
                            itemsBajoStock.Add(new ItemBajoStock
                            {
                                item = extra.item,
                                apedir = extra.cantidad,
                                codalmpedido = extra.codalmpedido,
                                udm = "PZ"
                            });
                        }
                    }

                    //actualizarUI("Creando Notas de Pedido");

                    // 🔹 Insertar en `inpedido` (Cabecera del pedido)

                    numero = await documento.pedidonumeroid(_context, id);
                    var nuevoPedido = new inpedido
                    {
                        id = id,
                        numeroid = numero + 1,
                        codalmacen = codalmacen,
                        codalmdestino = codalmacendestino,
                        fecha = DateTime.Today,
                        obs = "PEDIDO AUTOMÁTICO DE FECHA " + DateTime.Now.ToShortDateString(),
                        horareg = DateTime.Now.ToString("HH:mm"),
                        fechareg = DateTime.Today,
                        anulado = false,
                        usuarioreg = usuario
                    };

                    _context.inpedido.Add(nuevoPedido);
                    _context.SaveChanges();

                    codPedido = nuevoPedido.codigo;

                    // 🔹 Insertar detalles en `inpedido1`
                    var detallesPedido = itemsBajoStock
                        .Where(x => x.apedir > 0)
                        .Select(x => new inpedido1
                        {
                            codpedido = codPedido,
                            coditem = x.item,
                            cantidad = (decimal)x.apedir,
                            udm = x.udm,
                            codproveedor = 0
                        })
                        .ToList();

                    _context.inpedido1.AddRange(detallesPedido);
                    _context.SaveChanges();

                    // 🔹 Actualizar `intipopedido`
                    var tipoPedido = _context.intipopedido.FirstOrDefault(t => t.id == id);
                    if (tipoPedido != null)
                    {
                        tipoPedido.nroactual += 1;
                        _context.SaveChanges();
                    }

                    dbContexTransaction.Commit();
                    //actualizarUI("Terminado");
                }
                catch (Exception ex)
                {
                    dbContexTransaction.Rollback();
                    return (false, $"Error en el servidor al guardar Pedido: {ex.Message}", 0, "", 0);
                    throw;
                }
            }
            return (true, "", codPedido, id, numero + 1);
        }



        [HttpPost]
        [Route("GenerarPedidoAutomatico/{userConn}/{codempresa}/{usuario}")]
        public async Task<ActionResult<object>> GenerarPedidoAutomatico(string userConn, string codempresa, string usuario, requestGenerarPedido datosPedido)
        {
            try
            {
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);
                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    datosPedido.id = datosPedido.id.Trim();
                    datosPedido.lineadesde = datosPedido.lineadesde.Trim();
                    datosPedido.lineahasta = datosPedido.lineahasta.Trim();
                    datosPedido.itemdesde = datosPedido.itemdesde.Trim();
                    datosPedido.itemhasta = datosPedido.itemhasta.Trim();

                    if (string.IsNullOrWhiteSpace(userConn))
                    {
                        return (false, "No se recibio la variable de la cadena de conexion.");
                    }
                    if (string.IsNullOrWhiteSpace(usuario))
                    {
                        return (false, "No se recibio la variable usuario.");
                    }
                    if (string.IsNullOrWhiteSpace(codempresa))
                    {
                        return (false, "No se recibio la variable Empresa.");
                    }
                    if (datosPedido == null)
                    {
                        return (false, "No se recibio el Request de la solicitud.");
                    }

                    var Valido = await validarGenerarPedido(_context, datosPedido);
                    if (Valido.valido == false)
                    {
                        return BadRequest(new { Valido.valido, resp = Valido.msg });
                    }
                    //actualizar sotcks
                    var registros = _context.instoactual.Where(x => x.proformas == null).ToList();

                    foreach (var registro in registros)
                    {
                        registro.proformas = 0;
                    }
                    _context.SaveChanges();

                    // valida cabecera
                    var Negativos = await ValidarNegativos(_context, datosPedido.codalmacen);
                    if (Negativos.valido == false)
                    {
                        return BadRequest(new { Negativos.valido, resp = Negativos.msg });
                    }
                    //definir que proceso debe llamar al terminar el proceso principal

                    // manda a grabar
                    var Generado = await GenerarPedidoAut(_context, codempresa, usuario, datosPedido);
                    if (Generado.valido == false)
                    {
                        return BadRequest(new { Generado.valido, resp = Generado.msg });
                    }
                    await log.RegistrarEvento(_context, usuario, Log.Entidades.SW_Pedido, Generado.codigo.ToString(), Generado.ID, Generado.numeroID.ToString(), this._controllerName, "Se Genero y Grabo el Pedido Automatico", Log.TipoLog.Creacion);
                    return Ok(new
                    {
                        Generado.valido,
                        resp = "Se genero el pedido " + Generado.ID + " - " + Generado.numeroID + " con exito."
                    });
                }
            }
            catch (Exception ex)
            {
                return Problem("Error en el servidor al Generar Pedido: " + ex.Message);
                throw;
            }
        }


        private async Task<(bool valido, string msg)> validarGenerarPedido(DBContext _context, requestGenerarPedido datosPedido)
        {

            if (datosPedido.codalmacen <= 0)
            {
                return (false, "Debe poner el codigo del Almacen del cual desea generar el pedido automatico.");
            }
            if (datosPedido.codalmacen2 <= 0)
            {
                return (false, "Debe poner el codigo del Almacen al cual desea hacer el pedido.");
            }
            if (string.IsNullOrWhiteSpace(datosPedido.id))
            {
                return (false, "Debe poner el id del pedido automatico.");
            }

            if (datosPedido.grupos == true)
            {
                if (string.IsNullOrWhiteSpace(datosPedido.grupodesde))
                {
                    return (false, "Debe poner el codigo del Grupo desde el cual desea realizar el pedido.");
                }
                if (string.IsNullOrWhiteSpace(datosPedido.grupohasta))
                {
                    return (false, "Debe poner el codigo del Grupo hasta el cual desea realizar el pedido.");
                }
            }
            if (datosPedido.lineas == true)
            {
                if (string.IsNullOrWhiteSpace(datosPedido.lineadesde))
                {
                    return (false, "Debe poner el codigo de Linea desde el cual desea realizar el pedido.");
                }
                if (string.IsNullOrWhiteSpace(datosPedido.lineahasta))
                {
                    return (false, "Debe poner el codigo de Linea hasta el cual desea realizar el pedido.");
                }
            }
            if (datosPedido.items == true)
            {
                if (string.IsNullOrWhiteSpace(datosPedido.itemdesde))
                {
                    return (false, "Debe poner el codigo del Item desde el cual desea realizar el pedido.");
                }
                if (string.IsNullOrWhiteSpace(datosPedido.itemhasta))
                {
                    return (false, "Debe poner el codigo del Item hasta el cual desea realizar el pedido.");
                }
            }
            if (!(datosPedido.grupos || datosPedido.lineas || datosPedido.items))
            {
                return (false, "Debe seleccionar al menos un tipo de pedido: Todos, Grupos, Líneas o Items.");
            }
            return (true, "");
        }


        private async Task<(bool valido, string msg)> ValidarNegativos(DBContext _context, int codalmacen)
        {
            bool resultado = false;
            var itemsNegativos = _context.initem
            .Where(i => i.tipo == "MERCADERIA" && i.usar_en_movimiento == true)
            .Join(
                _context.instoactual.Where(s => s.codalmacen == codalmacen && (s.cantidad - s.proformas) < 0),
                i => i.codigo,
                s => s.coditem,
                (i, s) => new
                {
                    i.codigo,
                    i.descripcion,
                    i.medida,
                    cantidad = s.cantidad ?? 0,
                    proformas = s.proformas ?? 0
                }
            )
            .OrderBy(i => i.codigo)
            .ToList();

            if (itemsNegativos.Any())
            {
                var lista = new List<string>
        {
            "Estos items tienen saldo negativo, por favor corrijalos antes de generar el pedido:",
            "",
            funciones.Rellenar("CODIGO", 10, " ", false) + " " +
            funciones.Rellenar("DESCRIPCION", 30, " ", false) + " " +
            funciones.Rellenar("MEDIDA", 15, " ", false) + " " +
            funciones.Rellenar("CANTIDAD", 12, " ", true) + " " +
            funciones.Rellenar("PROFORMAS", 12, " ", true),
            "---------- ------------------------------ --------------- ------------ ------------"
        };

                foreach (var item in itemsNegativos)
                {
                    if (Math.Abs((decimal)(item.cantidad - (decimal)item.proformas)) >= 1)
                    {
                        resultado = false;
                        string cantidadTexto = item.cantidad.ToString("#,##0.00", CultureInfo.InvariantCulture);
                        string proformasTexto = item.proformas.ToString("#,##0.00", CultureInfo.InvariantCulture);
                        lista.Add(
                            funciones.Rellenar(item.codigo, 10, " ", false) + " " +
                            funciones.Rellenar(item.descripcion, 30, " ", false) + " " +
                            funciones.Rellenar(item.medida, 15, " ", false) + " " +
                            funciones.Rellenar(cantidadTexto, 12, " ", true) + " " +
                            funciones.Rellenar(proformasTexto, 12, " ", true)
                        );
                    }
                }

                lista.Add("---------- ------------------------------ --------------- ------------ ------------");

                if (!resultado)
                {
                    return (false, "Items Negativos" + Environment.NewLine + lista);
                }
            }
            return (true, "");

        }


        private async Task<List<ItemBajoStock>> ObtenerItemsBajoStock(DBContext _context, bool proforma_reserva, int codalmacen, int codalmacen2, string opcion, string filtro_desde, string filtro_hasta)
        {
            List<ItemBajoStock> itemsBajoStock = new List<ItemBajoStock>();

            if (proforma_reserva)
            {
                if (opcion == "todos")
                {
                    itemsBajoStock = await _context.instockalm
                    .Join(_context.instoactual,
                        a => new { a.item, a.codalmacen },
                        s => new { item = s.coditem, s.codalmacen },
                        (a, s) => new { a, s })
                    .Where(x => x.a.codalmpedido == codalmacen2 &&
                                x.s.codalmacen == codalmacen &&
                                x.a.smax > 0 &&
                                ((x.a.smax - ((x.s.cantidad ?? 0) - (x.s.proformas ?? 0))) > 0) &&
                                (((x.s.cantidad ?? 0) - (x.s.proformas ?? 0)) <= x.a.ptopedido))
                    .Select(x => new ItemBajoStock
                    {
                        item = x.a.item,
                        apedir = (double)(x.a.smax - ((x.s.cantidad ?? 0) - (x.s.proformas ?? 0))),
                        codalmpedido = x.a.codalmpedido,
                        udm = x.s.udm,
                        activo = false
                    })
                    .ToListAsync();

                    foreach (var reg in itemsBajoStock)
                    {
                        reg.activo = await items.item_usar_en_movimiento(_context, reg.item);
                    }
                    // Aplicar filtro si es necesario
                    itemsBajoStock = itemsBajoStock.Where(x => x.activo).ToList();
                    return itemsBajoStock;
                }
                if (opcion == "grupos")
                {
                    itemsBajoStock = await _context.instockalm
                    .Join(_context.instoactual,
                        a => new { a.item, a.codalmacen },
                        s => new { item = s.coditem, s.codalmacen },
                        (a, s) => new { a, s })
                    .Join(_context.initem,
                      x => x.a.item,
                      i => i.codigo,
                      (x, i) => new { x.a, x.s, i })
                    .Join(_context.inlinea,
                          x => x.i.codlinea,
                          l => l.codigo,
                          (x, l) => new { x.a, x.s, l })
                    .Where(x => x.a.codalmpedido == codalmacen2 &&
                                x.l.codgrupomer >= Convert.ToInt32(filtro_desde) &&
                                x.l.codgrupomer <= Convert.ToInt32(filtro_hasta) &&
                                x.s.codalmacen == codalmacen &&
                                x.a.smax > 0 &&
                                ((x.a.smax - ((x.s.cantidad ?? 0) - (x.s.proformas ?? 0))) > 0) &&
                                (((x.s.cantidad ?? 0) - (x.s.proformas ?? 0)) <= x.a.ptopedido))
                    .Select(x => new ItemBajoStock
                    {
                        item = x.a.item,
                        apedir = (double)(x.a.smax - ((x.s.cantidad ?? 0) - (x.s.proformas ?? 0))),
                        codalmpedido = x.a.codalmpedido,
                        udm = x.s.udm,
                        activo = false
                    })
                    .ToListAsync();

                    foreach (var reg in itemsBajoStock)
                    {
                        reg.activo = await items.item_usar_en_movimiento(_context, reg.item);
                    }
                    // Aplicar filtro si es necesario
                    itemsBajoStock = itemsBajoStock.Where(x => x.activo).ToList();
                    return itemsBajoStock;

                }
                if (opcion == "lineas")
                {
                    itemsBajoStock = await _context.instockalm
                    .Join(_context.instoactual,
                        a => new { a.item, a.codalmacen },
                        s => new { item = s.coditem, s.codalmacen },
                        (a, s) => new { a, s })
                    .Join(_context.initem,
                      x => x.a.item,
                      i => i.codigo,
                      (x, i) => new { x.a, x.s, i })
                    .Where(x => x.a.codalmpedido == codalmacen2 &&
                                string.Compare(x.i.codlinea, filtro_desde) >= 0 &&
                                string.Compare(x.i.codlinea, filtro_hasta) <= 0 &&
                                x.s.codalmacen == codalmacen &&
                                x.a.smax > 0 &&
                                ((x.a.smax - ((x.s.cantidad ?? 0) - (x.s.proformas ?? 0))) > 0) &&
                                (((x.s.cantidad ?? 0) - (x.s.proformas ?? 0)) <= x.a.ptopedido))
                    .Select(x => new ItemBajoStock
                    {
                        item = x.a.item,
                        apedir = (double)(x.a.smax - ((x.s.cantidad ?? 0) - (x.s.proformas ?? 0))),
                        codalmpedido = x.a.codalmpedido,
                        udm = x.s.udm,
                        activo = false
                    })
                    .ToListAsync();

                    foreach (var reg in itemsBajoStock)
                    {
                        reg.activo = await items.item_usar_en_movimiento(_context, reg.item);
                    }
                    // Aplicar filtro si es necesario
                    itemsBajoStock = itemsBajoStock.Where(x => x.activo).ToList();
                    return itemsBajoStock;
                }
                if (opcion == "items")
                {
                    itemsBajoStock = await _context.instockalm
                    .Join(_context.instoactual,
                        a => new { a.item, a.codalmacen },
                        s => new { item = s.coditem, s.codalmacen },
                        (a, s) => new { a, s })
                    .Where(x => x.a.codalmpedido == codalmacen2 &&
                                string.Compare(x.a.item, filtro_desde) >= 0 &&
                                string.Compare(x.a.item, filtro_hasta) <= 0 &&
                                x.s.codalmacen == codalmacen &&
                                x.a.smax > 0 &&
                                ((x.a.smax - ((x.s.cantidad ?? 0) - (x.s.proformas ?? 0))) > 0) &&
                                (((x.s.cantidad ?? 0) - (x.s.proformas ?? 0)) <= x.a.ptopedido))
                    .Select(x => new ItemBajoStock
                    {
                        item = x.a.item,
                        apedir = (double)(x.a.smax - ((x.s.cantidad ?? 0) - (x.s.proformas ?? 0))),
                        codalmpedido = x.a.codalmpedido,
                        udm = x.s.udm,
                        activo = false
                    })
                    .ToListAsync();

                    foreach (var reg in itemsBajoStock)
                    {
                        reg.activo = await items.item_usar_en_movimiento(_context, reg.item);
                    }
                    // Aplicar filtro si es necesario
                    itemsBajoStock = itemsBajoStock.Where(x => x.activo).ToList();
                    return itemsBajoStock;
                }

            }
            else//QUE NO TOMA EN CUENTA PROFORMAS
            {
                if (opcion == "todos")
                {
                    itemsBajoStock = await _context.instockalm
                    .Join(_context.instoactual,
                        a => new { a.item, a.codalmacen },
                        s => new { item = s.coditem, s.codalmacen },
                        (a, s) => new { a, s })
                    .Where(x => x.a.codalmpedido == codalmacen2 &&
                                x.s.codalmacen == codalmacen &&
                                x.a.smax > 0 &&
                                (x.a.smax - (x.s.cantidad ?? 0) > 0) &&
                                ((x.s.cantidad ?? 0) <= x.a.ptopedido))
                    .Select(x => new ItemBajoStock
                    {
                        item = x.a.item,
                        apedir = (double)(x.a.smax - x.s.cantidad ?? 0),
                        codalmpedido = x.a.codalmpedido,
                        udm = x.s.udm
                    })
                    .ToListAsync();

                    foreach (var reg in itemsBajoStock)
                    {
                        reg.activo = await items.item_usar_en_movimiento(_context, reg.item);
                    }
                    // Aplicar filtro si es necesario
                    itemsBajoStock = itemsBajoStock.Where(x => x.activo).ToList();
                    return itemsBajoStock;
                }
                if (opcion == "grupos")
                {
                    itemsBajoStock = await _context.instockalm
                    .Join(_context.instoactual,
                        a => new { a.item, a.codalmacen },
                        s => new { item = s.coditem, s.codalmacen },
                        (a, s) => new { a, s })
                    .Join(_context.initem,
                      x => x.a.item,
                      i => i.codigo,
                      (x, i) => new { x.a, x.s, i })
                    .Join(_context.inlinea,
                          x => x.i.codlinea,
                          l => l.codigo,
                          (x, l) => new { x.a, x.s, l })
                    .Where(x => x.a.codalmpedido == codalmacen2 &&
                                x.l.codgrupomer >= Convert.ToInt32(filtro_desde) &&
                                x.l.codgrupomer <= Convert.ToInt32(filtro_hasta) &&
                                x.s.codalmacen == codalmacen &&
                                x.a.smax > 0 &&
                                (x.a.smax - (x.s.cantidad ?? 0) > 0) &&
                                ((x.s.cantidad ?? 0) <= x.a.ptopedido))
                    .Select(x => new ItemBajoStock
                    {
                        item = x.a.item,
                        apedir = (double)(x.a.smax - x.s.cantidad ?? 0),
                        codalmpedido = x.a.codalmpedido,
                        udm = x.s.udm
                    })
                    .ToListAsync();

                    foreach (var reg in itemsBajoStock)
                    {
                        reg.activo = await items.item_usar_en_movimiento(_context, reg.item);
                    }
                    // Aplicar filtro si es necesario
                    itemsBajoStock = itemsBajoStock.Where(x => x.activo).ToList();
                    return itemsBajoStock;
                }
                if (opcion == "lineas")
                {
                    itemsBajoStock = await _context.instockalm
                    .Join(_context.instoactual,
                        a => new { a.item, a.codalmacen },
                        s => new { item = s.coditem, s.codalmacen },
                        (a, s) => new { a, s })
                    .Join(_context.initem,
                      x => x.a.item,
                      i => i.codigo,
                      (x, i) => new { x.a, x.s, i })
                    .Where(x => x.a.codalmpedido == codalmacen2 &&
                                string.Compare(x.i.codlinea, filtro_desde) >= 0 &&
                                string.Compare(x.i.codlinea, filtro_hasta) <= 0 &&
                                x.s.codalmacen == codalmacen &&
                                x.a.smax > 0 &&
                                (x.a.smax - (x.s.cantidad ?? 0) > 0) &&
                                ((x.s.cantidad ?? 0) <= x.a.ptopedido))
                    .Select(x => new ItemBajoStock
                    {
                        item = x.a.item,
                        apedir = (double)(x.a.smax - x.s.cantidad ?? 0),
                        codalmpedido = x.a.codalmpedido,
                        udm = x.s.udm
                    })
                    .ToListAsync();

                    foreach (var reg in itemsBajoStock)
                    {
                        reg.activo = await items.item_usar_en_movimiento(_context, reg.item);
                    }
                    // Aplicar filtro si es necesario
                    itemsBajoStock = itemsBajoStock.Where(x => x.activo).ToList();
                    return itemsBajoStock;
                }
                if (opcion == "items")
                {
                    itemsBajoStock = await _context.instockalm
                    .Join(_context.instoactual,
                        a => new { a.item, a.codalmacen },
                        s => new { item = s.coditem, s.codalmacen },
                        (a, s) => new { a, s })
                    .Where(x => x.a.codalmpedido == codalmacen2 &&
                                string.Compare(x.a.item, filtro_desde) >= 0 &&
                                string.Compare(x.a.item, filtro_hasta) <= 0 &&
                                x.s.codalmacen == codalmacen &&
                                x.a.smax > 0 &&
                                (x.a.smax - (x.s.cantidad ?? 0) > 0) &&
                                ((x.s.cantidad ?? 0) <= x.a.ptopedido))
                    .Select(x => new ItemBajoStock
                    {
                        item = x.a.item,
                        apedir = (double)(x.a.smax - x.s.cantidad ?? 0),
                        codalmpedido = x.a.codalmpedido,
                        udm = x.s.udm
                    })
                    .ToListAsync();

                    foreach (var reg in itemsBajoStock)
                    {
                        reg.activo = await items.item_usar_en_movimiento(_context, reg.item);
                    }
                    // Aplicar filtro si es necesario
                    itemsBajoStock = itemsBajoStock.Where(x => x.activo).ToList();
                    return itemsBajoStock;
                }
            }
            return itemsBajoStock;
        }


    }

    public class requestGabrarPedido
    {
        public inpedido cabecera { get; set; }
        public List<inpedido1> tablaDetalle { get; set; }
    }
    public class tablaDetallePedido
    {
        public int codpedido { get; set; }
        public string coditem { get; set; }
        public string descripcion { get; set; }
        public string medida { get; set; }
        public decimal cantidad { get; set; }
        public string udm { get; set; }
        public int? codproveedor { get; set; }
    }

    public class ItemBajoStock
    {
        public string item { get; set; }       // Código del ítem
        public double apedir { get; set; }    // Cantidad a pedir
        public int codalmpedido { get; set; }  // Código del almacén destino
        public string udm { get; set; }        // Unidad de medida
        public bool activo { get; set; }
    }

    public class requestGenerarPedido
    {
        public int codalmacen { get; set; }
        public int codalmacen2 { get; set; }
        public string id { get; set; }
        public bool todos { get; set; }
        public bool grupos { get; set; }
        public bool lineas { get; set; }
        public bool items { get; set; }
        public string grupodesde { get; set; }
        public string grupohasta { get; set; }
        public string lineadesde { get; set; }
        public string lineahasta { get; set; }
        public string itemdesde { get; set; }
        public string itemhasta { get; set; }
    }
}
