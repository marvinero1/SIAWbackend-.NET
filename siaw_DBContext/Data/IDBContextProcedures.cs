using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace siaw_DBContext.Data
{
    public partial interface IDBContextProcedures
    {
        Task<int> SP001_Cantidad_Reservada_En_Proforma_AlmacenesAsync(string coditem, int? codalmacen, string id_prof, int? nroid_prof, OutputParameter<decimal?> respuesta, OutputParameter<int> returnValue = null, CancellationToken cancellationToken = default);
        Task<int> SP001_Cantidad_Reservada_En_Proforma_TiendasAsync(string coditem, int? codalmacen, string id_prof, int? nroid_prof, OutputParameter<decimal?> respuesta, OutputParameter<int> returnValue = null, CancellationToken cancellationToken = default);
        Task<int> SP001_SaldoReservadoNotasUrgente_Una_proforma_AlmacenesAsync(string coditem, int? codalmacen, string id_prof, int? nroid_prof, OutputParameter<decimal?> respuesta, OutputParameter<int> returnValue = null, CancellationToken cancellationToken = default);
        Task<int> SP001_SaldoReservadoNotasUrgente_Una_proforma_TiendasAsync(string coditem, int? codalmacen, string id_prof, int? nroid_prof, OutputParameter<decimal?> respuesta, OutputParameter<int> returnValue = null, CancellationToken cancellationToken = default);
        Task<int> SP001_SaldoReservadoNotasUrgentes_AlmacenesAsync(string coditem, int? codalmacen, OutputParameter<string> respuesta, OutputParameter<int> returnValue = null, CancellationToken cancellationToken = default);
        Task<int> SP001_SaldoReservadoNotasUrgentes_TiendasAsync(string coditem, int? codalmacen, OutputParameter<string> respuesta, OutputParameter<int> returnValue = null, CancellationToken cancellationToken = default);
    }
}
