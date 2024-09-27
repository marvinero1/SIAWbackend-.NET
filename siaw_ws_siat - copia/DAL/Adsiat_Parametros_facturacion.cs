using Microsoft.EntityFrameworkCore;
using siaw_DBContext.Data;
using siaw_DBContext.Models;

namespace siaw_ws_siat
{
    public class Adsiat_Parametros_facturacion
    {
        //#region SINGLETON PATTERN
        //private adsiat_parametros_facturacion() { }

        //public static readonly adsiat_parametros_facturacion Instancia = new adsiat_parametros_facturacion();
        //#endregion

        public class DatosParametrosFacturacionSIAT
        {
            public string CodEmpresa { get; set; }
            public string CodSistema { get; set; }
            public int CodAlmacen { get; set; }
            public int CodSucursal { get; set; }
            public int CodAmbiente { get; set; }
            public int CodModalidad { get; set; }
            public int CodTipoEmision { get; set; }
            public int CodTipoFactura { get; set; }
            public int CodTipoDocSector { get; set; }
            public int CodPuntoVta { get; set; }
            public string CodActividad { get; set; }
            public string NitCliente { get; set; }
            public int CantItemsXFactura { get; set; }

            public bool SinActivo { get; set; }
            public bool InternetActivo { get; set; }
        }

        private string sql = "";

        public async Task<bool> InsertarParametros(DBContext dbContext, DatosParametrosFacturacionSIAT datos)
        {
            bool resultado = true;

            if (resultado)
            {
                var paramFacturacion = await dbContext.adsiat_parametros_facturacion
                    .Where(p => p.codalmacen == datos.CodAlmacen)
                    .ToListAsync();
                dbContext.adsiat_parametros_facturacion.RemoveRange(paramFacturacion);
                await dbContext.SaveChangesAsync();
            }

            if (resultado)
            {
                var entity = new siaw_DBContext.Models.adsiat_parametros_facturacion
                {
                    codempresa = datos.CodEmpresa,
                    codsistema = datos.CodSistema,
                    codalmacen = datos.CodAlmacen,
                    codsucursal = datos.CodSucursal,
                    ambiente = datos.CodAmbiente,
                    modalidad = datos.CodModalidad,
                    tipo_emision = datos.CodTipoEmision,
                    tipo_factura = datos.CodTipoFactura,
                    tipo_doc_sector = datos.CodTipoDocSector,
                    punto_vta = datos.CodPuntoVta,
                    codactividad = datos.CodActividad,
                    nit_cliente = datos.NitCliente,
                    servicio_internet_activo = datos.InternetActivo ? true : false,
                    servicio_sin_activo = datos.SinActivo ? true : false,
                    nro_max_items_factura_siat = datos.CantItemsXFactura
                };

                await dbContext.adsiat_parametros_facturacion.AddAsync(entity);
                await dbContext.SaveChangesAsync();
            }

            return resultado;
        }

        public async Task<int> Modalidad(DBContext dbContext, int codAlmacen)
        {
            var param = await dbContext.adsiat_parametros_facturacion
                .Where(p => p.codalmacen == codAlmacen)
                .Select(p => p.modalidad)
                .FirstOrDefaultAsync();

            return param ?? 0;
        }

        public async Task<int> Ambiente(DBContext dbContext, int codAlmacen)
        {
            var param = await dbContext.adsiat_parametros_facturacion
                .Where(p => p.codalmacen == codAlmacen)
                .Select(p => p.ambiente)
                .FirstOrDefaultAsync();

            return param ?? 0;
        }

        public async Task<int> Sucursal(DBContext dbContext, int codAlmacen)
        {
            try
            {
                int param = await dbContext.adsiat_parametros_facturacion
                .Where(p => p.codalmacen == codAlmacen)
                .Select(p => p.codsucursal)
                .FirstOrDefaultAsync();

                return param;
            }
            catch (Exception)
            {
                return 0;
            }
        }

        public async Task<int> TipoEmision(int codAlmacen, DBContext dbContext)
        {
            var param = await dbContext.adsiat_parametros_facturacion
                .Where(p => p.codalmacen == codAlmacen)
                .Select(p => p.tipo_emision)
                .FirstOrDefaultAsync();

            return param ?? 0;
        }

        public async Task<int> TipoFactura(DBContext dbContext, int codAlmacen)
        {
            var param = await dbContext.adsiat_parametros_facturacion
                .Where(p => p.codalmacen == codAlmacen)
                .Select(p => p.tipo_factura)
                .FirstOrDefaultAsync();

            return param ?? 0;
        }

        public async Task<bool> ServiciosSinActivo(DBContext dbContext, int codAlmacen)
        {
            var param = await dbContext.adsiat_parametros_facturacion
                .Where(p => p.codalmacen == codAlmacen)
                .Select(p => p.servicio_sin_activo)
                .FirstOrDefaultAsync();

            return param ?? false;
        }

        public async Task<bool> ServiciosInternetActivo(DBContext dbContext, int codAlmacen)
        {
            var param = await dbContext.adsiat_parametros_facturacion
                .Where(p => p.codalmacen == codAlmacen)
                .Select(p => p.servicio_internet_activo)
                .FirstOrDefaultAsync();

            return param ?? false;
        }

        public async Task<int> TipoDocSector(DBContext dbContext, int codAlmacen)
        {
            var param = await dbContext.adsiat_parametros_facturacion
                .Where(p => p.codalmacen == codAlmacen)
                .Select(p => p.tipo_doc_sector)
                .FirstOrDefaultAsync();

            return param ?? 0;
        }

        public async Task<int> PuntoDeVta(DBContext dbContext, int codAlmacen)
        {
            var param = await dbContext.adsiat_parametros_facturacion
                .Where(p => p.codalmacen == codAlmacen)
                .Select(p => p.punto_vta)
                .FirstOrDefaultAsync();

            return param ?? 0;
        }

        public async Task<string> NitCliente(DBContext dbContext, int codAlmacen)
        {
            var param = await dbContext.adsiat_parametros_facturacion
                .Where(p => p.codalmacen == codAlmacen)
                .Select(p => p.nit_cliente)
                .FirstOrDefaultAsync();

            return param ?? "0";
        }

        public async Task<int> CantItemsXFactura(DBContext dbContext, int codAlmacen)
        {
            var param = await dbContext.adsiat_parametros_facturacion
                .Where(p => p.codalmacen == codAlmacen)
                .Select(p => p.nro_max_items_factura_siat)
                .FirstOrDefaultAsync();

            return param ?? 0;
        }

        public async Task<string> Actividad(DBContext dbContext, int codAlmacen)
        {
            var param = await dbContext.adsiat_parametros_facturacion
                .Where(p => p.codalmacen == codAlmacen)
                .Select(p => p.codactividad)
                .FirstOrDefaultAsync();

            return param ?? "NSE";
        }

        public async Task<string> CUIS(DBContext dbContext, int codSucursal)
        {
            var param = await dbContext.adcuis_sia
                .Where(p => p.codsucursal == codSucursal && p.activo == true)
                .Select(p => p.cuis)
                .FirstOrDefaultAsync();

            return param ?? "NSE";
        }

        public async Task<string> CodigoSistema(DBContext dbContext, int codSucursal)
        {
            var param = await dbContext.adcuis_sia
                .Where(p => p.codsucursal == codSucursal)
                .Select(p => p.codsistema)
                .FirstOrDefaultAsync();

            return param ?? "NSE";
        }

        public async Task<int> CodigoMotivoEvento(DBContext dbContext, string descripcion)
        {
            try
            {
                int param = await dbContext.adsiat_evento
                .Where(p => p.descripcion == descripcion)
                .Select(p => p.codigoclasificador)
                .FirstOrDefaultAsync();

                return param;
            }
            catch (Exception)
            {
                return 0;
            }
        }
    }
}
