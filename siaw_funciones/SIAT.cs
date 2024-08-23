using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using siaw_DBContext.Data;
using siaw_DBContext.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace siaw_funciones
{
    public class SIAT
    {
        private readonly Empresa empresa = new Empresa();
        private readonly Funciones funciones = new Funciones();
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

        /*
        public async Task<double> Redondeo_Decimales_SIA_5_decimales_SQL(double minumero)
        {
            double resultado = Math.Round(minumero, 5);
            return resultado;
        }
        */
        public async Task<decimal> Redondeo_Decimales_SIA_2_decimales_SQL(DBContext context,double numero)
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
                        Scale = 2
                    };
                    await context.Database.ExecuteSqlRawAsync
                        ("EXEC Redondeo_Decimales_SIA_2_decimales_SQL @minumero, @resultado OUTPUT",
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

        public async Task<decimal> Redondeo_Decimales_SIA_5_decimales_SQL(DBContext context, decimal numero)
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
        public async Task<int> Nro_Maximo_Items_Factura_Segun_SIAT(DBContext _context, string codempresa)
        {
            try
            {
                int resultado = 0;
                //using (_context)
                //{
                var result = await _context.adsiat_parametros_facturacion
                    .Where(v => v.codempresa == codempresa)
                    .Select(v => v.nro_max_items_factura_siat)
                    .FirstOrDefaultAsync();
                if (result != null)
                {
                    resultado = result ?? 0;
                }
                else { resultado = 0; }
                //}
                return resultado;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return 0;
            }
        }
        public async Task<double> Redondear_SIAT(DBContext _context, string codempresa, double minumero)
        {
            double resultado = 0;
            // El Servicio de Impuestos Nacionales utiliza en la emisión de facturas electrónicas en linea montos 
            // expresados con dos decimales y utiliza el redondeo tradicional o HALF-UP. En este caso, el redondeo 
            // se realiza al número superior cuando el decimal sea igual o superior a 5 y al número inferior 
            // cuando el decimal sea igual o inferior a 5.
            // Ej:

            // 3.14159  será redondeado a 3.14
            // 3.14559  será redondeado a 3.15
            int CODALMACEN = await empresa.AlmacenLocalEmpresa(_context, codempresa);
            int _codDocSector = await _context.adsiat_parametros_facturacion.Where(i => i.codalmacen == CODALMACEN).Select(I => I.tipo_doc_sector).FirstOrDefaultAsync() ?? -1;

            if (_codDocSector == 1)
            {
                // 35 : FACTURA COMPRA VENTA (2 DECIMALES)
                resultado = Math.Round(minumero, 2, MidpointRounding.AwayFromZero);
            }
            else if(_codDocSector == 35)
            {
                // 35 : FACTURA COMPRA VENTA BONIFICACIONES(5 DECIMALES) 
                resultado = Math.Round(minumero, 5, MidpointRounding.AwayFromZero);
            }
            else if (_codDocSector == 24)
            {
                // 35 : notas de credito (2 DECIMALES)
                resultado = Math.Round(minumero, 5, MidpointRounding.AwayFromZero);
            }
            else
            {
                resultado = Math.Round(minumero, 2, MidpointRounding.AwayFromZero);
            }
            return resultado;
        }

        public async Task<Datos_Dosificacion_Activa> Obtener_Cufd_Dosificacion_Activa(DBContext _context, DateTime fecha, int codalmacen)
        {
            Datos_Dosificacion_Activa miresultado = new Datos_Dosificacion_Activa();
            var dt = await _context.vedosificacion.Where(i => i.fechainicio == fecha.Date && i.almacen == codalmacen && i.activa == true).ToListAsync();
            if (dt.Count() == 1) {
                // solo si hay un registro devuelve datos en otro caso por seguridad no devuelve nada
                // por ejemplo si hay mas de 1 dosificacion activa hay algo mal
                miresultado.nrocaja = dt[0].nrocaja;
                miresultado.codcontrol = dt[0].codigo_control;
                miresultado.cufd = dt[0].cufd;
                miresultado.fechainicio = (dt[0].fechainicio ?? fecha).Date;
                miresultado.activa = dt[0].activa;
                miresultado.tipo = dt[0].tipo;
                miresultado.resultado = true;
            }
            else
            {
                miresultado.codcontrol = "";
                miresultado.cufd = "";
                miresultado.tipo = "";
                miresultado.fechainicio = new DateTime(1900, 1, 1);
                miresultado.activa = false;
                miresultado.resultado = false;
            }
            return miresultado;
        }

        public async Task<string> generar_leyenda_aleatoria(DBContext _context, int almacen)
        {
            string resultado = "";
            Datos_Pametros_Facturacion_Ag ParamFacturacion = new Datos_Pametros_Facturacion_Ag();
            ParamFacturacion = await Obtener_Parametros_Facturacion(_context, almacen);
            string ultima_leyenda = await Leyenda_Ultima_Factura(_context, almacen);

        CONTINUAR_AQUI:
            var dt_leyendas = await Obtener_leyendas_Siat(_context, ParamFacturacion.codactividad);
            //obtener la leyenda para dosificacion de la tabla adsiat_leyenda_factura y seleccionar una de ellas aleatoriamente
            if (dt_leyendas != null)
            {
                if (dt_leyendas.Count() > 0)
                {
                    int cant = dt_leyendas.Count();
                    Random random = new Random();
                    int aleatorio = random.Next(0, cant-1);
                    string mileyenda = dt_leyendas[aleatorio].descripcionleyenda;
                    if (mileyenda == ultima_leyenda)
                    {
                        dt_leyendas.Clear();
                        goto CONTINUAR_AQUI;
                    }
                    else
                    {
                        resultado = mileyenda;
                    }
                }
                else
                {
                    resultado = "Ley N° 453: El proveedor deberá entregar el producto en las modalidades y términos ofertados o convenidos.";
                }
            }
            else
            {
                resultado = "Ley N° 453: El proveedor deberá entregar el producto en las modalidades y términos ofertados o convenidos.";
            }
            return resultado;
        }

        public async Task<Datos_Pametros_Facturacion_Ag> Obtener_Parametros_Facturacion(DBContext _context, int almacen)
        {
            Datos_Pametros_Facturacion_Ag miresultado = new Datos_Pametros_Facturacion_Ag();
            var dt = await _context.adsiat_parametros_facturacion.Where(i=> i.codalmacen == almacen).FirstOrDefaultAsync();
            if (dt != null)
            {
                miresultado.codsucursal = dt.codsucursal.ToString();
                miresultado.ambiente = (dt.ambiente ?? 0).ToString();
                miresultado.modalidad = (dt.modalidad ?? 0).ToString();
                miresultado.tipoemision = (dt.tipo_emision ?? 0).ToString();
                miresultado.tipofactura = (dt.tipo_factura ?? 0).ToString();
                miresultado.tiposector = (dt.tipo_doc_sector ?? 0).ToString();
                miresultado.ptovta = (dt.punto_vta ?? 0).ToString();
                miresultado.codactividad = dt.codactividad;
                miresultado.nit_cliente = dt.nit_cliente;
                miresultado.codsistema = dt.codsistema;
                miresultado.resultado = true;
            }
            else
            {
                miresultado.codsucursal = "";
                miresultado.ambiente = "";
                miresultado.modalidad = "";
                miresultado.tipoemision = "";
                miresultado.tipofactura = "";
                miresultado.tiposector = "";
                miresultado.ptovta = "";
                miresultado.codactividad = "";
                miresultado.nit_cliente = "";
                miresultado.codsistema = "";
                miresultado.resultado = false;
            }
            return miresultado;
        }
        public async Task<List<adsiat_leyenda_factura>?> Obtener_leyendas_Siat(DBContext _context, string codigo_actividad)
        {
            try
            {
                var resultado = await _context.adsiat_leyenda_factura.Where(i => i.codigoactividad == int.Parse(codigo_actividad)).ToListAsync();
                return resultado;
            }
            catch (Exception)
            {
                return null;
            }
        }
        public async Task<string> Leyenda_Ultima_Factura(DBContext _context, int almacen)
        {
            string resultado = "Sin Leyenda";
            try
            {
                var dt = await _context.vefactura.Where(i=> i.codalmacen == almacen && i.leyenda != "").OrderByDescending(i=> i.codigo).FirstOrDefaultAsync();
                if (dt != null)
                {
                    resultado = dt.leyenda;
                }
            }
            catch (Exception)
            {
                resultado = "";
            }
            return resultado;
        }
        public async Task<string> Generar_Codigo_Factura_Web(DBContext _context, int codfactura, int codalmacen)
        {
            string resultado = "";
            // obtener el ID-Numeroid de la factura
            var id_nroid_fact = await Ventas.id_nroid_factura_cuf(_context, codfactura);
            resultado += id_nroid_fact.numeroId.ToString();

            string codalmacenText = funciones.Rellenar(codalmacen.ToString(), 5, "0", true);
            resultado += codalmacenText;

            /////////////////////////////////////////////////////////////////////////////////////////////////
            // se obtiene el modulo 11 de la cadena y se lo adjunta al final de la cadena
            string digito_verificador_Mod11 = Calcular_Digito_Mod11(resultado, 1, 9, false);
            resultado += digito_verificador_Mod11;
            /////////////////////////////////////////////////////////////////////////////////////////////////
            // se aplica a ala cadena resultande Base 16
            // convertir la cadena tipo numero decimala  base 16
            string codigo_factura_web_resultado = Transformar_Decimal_A_Base16(resultado);
            /////////////////////////////////////////////////////////////////////////////////////////////////
            // concatenar el codigo de control de CUFD asignado
            // codigo_factura_web_resultado &= idFC

            return codigo_factura_web_resultado;
        }



        public string Calcular_Digito_Mod11(string cadena, int numDig, int limMult, bool x10)
        {
            int mult, suma, i, n, dig;
            int digito = 0;
            int multiplicacion = 0;

            if (!x10)
            {
                numDig = 1;
            }

            for (n = 1; n <= numDig; n++)
            {
                suma = 0;
                mult = 2;

                int aux_largo = cadena.Length - 1;

                for (i = cadena.Length - 1; i >= 0; i--)
                {
                    digito = int.Parse(cadena.Substring(i, 1));
                    multiplicacion = mult * digito;
                    suma += multiplicacion;

                    mult += 1;

                    if (mult > limMult)
                    {
                        mult = 2;
                    }
                }

                if (x10)
                {
                    dig = ((suma * 10) % 11) % 10;
                }
                else
                {
                    dig = suma % 11;
                }

                if (dig == 10)
                {
                    cadena += "1";
                }

                if (dig == 11)
                {
                    cadena += "0";
                }

                if (dig < 10)
                {
                    cadena += dig.ToString();
                }
            }

            // Devolver solo el dígito
            return cadena.Substring(cadena.Length - numDig, 1);
        }

        public string Transformar_Decimal_A_Base16(string cadena_numero_dec)
        {
            System.Numerics.BigInteger Numero;
            int Base = 16;
            string resultado = "";

            Numero = System.Numerics.BigInteger.Parse(cadena_numero_dec);

            if (Base <= 1)
            {
                Base = 2;
            }

            resultado = Convertir_Base10_A_Base16(Numero, Base);

            // Quitar el caracter cero de la primera posición
            if (resultado.Length > 0 && resultado[0] == '0')
            {
                resultado = resultado.Substring(1);
            }

            return resultado;
        }

        public string Convertir_Base10_A_Base16(System.Numerics.BigInteger Numero, int Base)
        {
            // Este algoritmo tiene la misma estructura que SoloBinario
            // Simplemente que se generalizó para que pueda
            // Transformarse un número a cualquier base

            System.Numerics.BigInteger Temporal;
            System.Numerics.BigInteger Cociente;
            int Modulo;
            string ModuloTexto = "";

            Temporal = Numero;
            // Un número "10 = a" en otra base, nada más
            // Por eso esta porción de código
            Modulo = (int)(Temporal % Base);
            switch (Modulo)
            {
                case 10:
                    ModuloTexto = "A";
                    break;
                case 11:
                    ModuloTexto = "B";
                    break;
                case 12:
                    ModuloTexto = "C";
                    break;
                case 13:
                    ModuloTexto = "D";
                    break;
                case 14:
                    ModuloTexto = "E";
                    break;
                case 15:
                    ModuloTexto = "F";
                    break;
                case 16:
                    ModuloTexto = "G";
                    break;
                default:
                    // Para los casos menores a 10
                    ModuloTexto = Modulo.ToString();
                    break;
            }

            // La Base no puede ser negativa ni menor a 2
            if (Base <= 1)
            {
                Base = 2;
            }

            if (Numero != 0)
            {
                if ((Numero % Base) <= (Base - 1))
                {
                    // Si el número no es divisible por la base
                    // Con esta restricción tendremos un número
                    // Divisible exactamente por la base
                    Numero = Numero - (Numero % Base);
                }
                Cociente = Numero / Base;
                return Convertir_Base10_A_Base16(Cociente, Base) + ModuloTexto;
            }
            else
            {
                if (Numero == 0)
                {
                    return "0";
                }
                else
                {
                    return "";
                }
            }
        }


    }

    public class Datos_Dosificacion_Activa
    {
        public string cufd { get; set; }
        public string codcontrol { get; set; }
        public DateTime fechainicio { get; set; }
        public bool activa { get; set; }
        public int nrocaja { get; set; }

        public string tipo { get; set; }
        public bool resultado { get; set; } = false;
    }

    public class Datos_Pametros_Facturacion_Ag
    {
        public string codsucursal { get; set; } = "";
        public string ambiente { get; set; }
        public string modalidad { get; set; } = "";
        public string tipoemision { get; set; } = "";
        public string tipofactura { get; set; } = "";

        public string tiposector { get; set; } = "";
        public string ptovta { get; set; } = "";
        public string codactividad { get; set; } = "";
        public bool resultado { get; set; } = false;
        public string nit_cliente { get; set; } = "";
        public string codsistema { get; set; } = "";
    }

}
