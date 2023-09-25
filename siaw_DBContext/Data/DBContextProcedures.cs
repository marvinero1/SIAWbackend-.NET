﻿using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
//using SIAW.Models;
using siaw_DBContext.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace siaw_DBContext.Data
{
    public partial class DBContext
    {
        private IDBContextProcedures _procedures;

        public virtual IDBContextProcedures Procedures
        {
            get
            {
                if (_procedures is null) _procedures = new DBContextProcedures(this);
                return _procedures;
            }
            set
            {
                _procedures = value;
            }
        }

        public IDBContextProcedures GetProcedures()
        {
            return Procedures;
        }

        protected void OnModelCreatingGeneratedProcedures(ModelBuilder modelBuilder)
        {
        }
    }

    public partial class DBContextProcedures : IDBContextProcedures
    {
        private readonly DBContext _context;

        public DBContextProcedures(DBContext context)
        {
            _context = context;
        }

        public virtual async Task<int> SP001_Cantidad_Reservada_En_Proforma_AlmacenesAsync(string coditem, int? codalmacen, string id_prof, int? nroid_prof, OutputParameter<decimal?> respuesta, OutputParameter<int> returnValue = null, CancellationToken cancellationToken = default)
        {
            var parameterrespuesta = new SqlParameter
            {
                ParameterName = "respuesta",
                Precision = 18,
                Scale = 2,
                Direction = System.Data.ParameterDirection.InputOutput,
                Value = respuesta?._value ?? Convert.DBNull,
                SqlDbType = System.Data.SqlDbType.Decimal,
            };
            var parameterreturnValue = new SqlParameter
            {
                ParameterName = "returnValue",
                Direction = System.Data.ParameterDirection.Output,
                SqlDbType = System.Data.SqlDbType.Int,
            };

            var sqlParameters = new[]
            {
                new SqlParameter
                {
                    ParameterName = "coditem",
                    Size = 20,
                    Value = coditem ?? Convert.DBNull,
                    SqlDbType = System.Data.SqlDbType.NVarChar,
                },
                new SqlParameter
                {
                    ParameterName = "codalmacen",
                    Value = codalmacen ?? Convert.DBNull,
                    SqlDbType = System.Data.SqlDbType.Int,
                },
                new SqlParameter
                {
                    ParameterName = "id_prof",
                    Size = 30,
                    Value = id_prof ?? Convert.DBNull,
                    SqlDbType = System.Data.SqlDbType.NVarChar,
                },
                new SqlParameter
                {
                    ParameterName = "nroid_prof",
                    Value = nroid_prof ?? Convert.DBNull,
                    SqlDbType = System.Data.SqlDbType.Int,
                },
                parameterrespuesta,
                parameterreturnValue,
            };
            var _ = await _context.Database.ExecuteSqlRawAsync("EXEC @returnValue = [dbo].[SP001_Cantidad_Reservada_En_Proforma_Almacenes] @coditem, @codalmacen, @id_prof, @nroid_prof, @respuesta OUTPUT", sqlParameters, cancellationToken);

            respuesta.SetValue(parameterrespuesta.Value);
            returnValue?.SetValue(parameterreturnValue.Value);

            return _;
        }

        public virtual async Task<int> SP001_Cantidad_Reservada_En_Proforma_TiendasAsync(string coditem, int? codalmacen, string id_prof, int? nroid_prof, OutputParameter<decimal?> respuesta, OutputParameter<int> returnValue = null, CancellationToken cancellationToken = default)
        {
            var parameterrespuesta = new SqlParameter
            {
                ParameterName = "respuesta",
                Precision = 18,
                Scale = 2,
                Direction = System.Data.ParameterDirection.InputOutput,
                Value = respuesta?._value ?? Convert.DBNull,
                SqlDbType = System.Data.SqlDbType.Decimal,
            };
            var parameterreturnValue = new SqlParameter
            {
                ParameterName = "returnValue",
                Direction = System.Data.ParameterDirection.Output,
                SqlDbType = System.Data.SqlDbType.Int,
            };

            var sqlParameters = new[]
            {
                new SqlParameter
                {
                    ParameterName = "coditem",
                    Size = 20,
                    Value = coditem ?? Convert.DBNull,
                    SqlDbType = System.Data.SqlDbType.NVarChar,
                },
                new SqlParameter
                {
                    ParameterName = "codalmacen",
                    Value = codalmacen ?? Convert.DBNull,
                    SqlDbType = System.Data.SqlDbType.Int,
                },
                new SqlParameter
                {
                    ParameterName = "id_prof",
                    Size = 30,
                    Value = id_prof ?? Convert.DBNull,
                    SqlDbType = System.Data.SqlDbType.NVarChar,
                },
                new SqlParameter
                {
                    ParameterName = "nroid_prof",
                    Value = nroid_prof ?? Convert.DBNull,
                    SqlDbType = System.Data.SqlDbType.Int,
                },
                parameterrespuesta,
                parameterreturnValue,
            };
            var _ = await _context.Database.ExecuteSqlRawAsync("EXEC @returnValue = [dbo].[SP001_Cantidad_Reservada_En_Proforma_Tiendas] @coditem, @codalmacen, @id_prof, @nroid_prof, @respuesta OUTPUT", sqlParameters, cancellationToken);

            respuesta.SetValue(parameterrespuesta.Value);
            returnValue?.SetValue(parameterreturnValue.Value);

            return _;
        }

        public virtual async Task<int> SP001_SaldoReservadoNotasUrgente_Una_proforma_AlmacenesAsync(string coditem, int? codalmacen, string id_prof, int? nroid_prof, OutputParameter<decimal?> respuesta, OutputParameter<int> returnValue = null, CancellationToken cancellationToken = default)
        {
            var parameterrespuesta = new SqlParameter
            {
                ParameterName = "respuesta",
                Precision = 18,
                Scale = 2,
                Direction = System.Data.ParameterDirection.InputOutput,
                Value = respuesta?._value ?? Convert.DBNull,
                SqlDbType = System.Data.SqlDbType.Decimal,
            };
            var parameterreturnValue = new SqlParameter
            {
                ParameterName = "returnValue",
                Direction = System.Data.ParameterDirection.Output,
                SqlDbType = System.Data.SqlDbType.Int,
            };

            var sqlParameters = new[]
            {
                new SqlParameter
                {
                    ParameterName = "coditem",
                    Size = 20,
                    Value = coditem ?? Convert.DBNull,
                    SqlDbType = System.Data.SqlDbType.NVarChar,
                },
                new SqlParameter
                {
                    ParameterName = "codalmacen",
                    Value = codalmacen ?? Convert.DBNull,
                    SqlDbType = System.Data.SqlDbType.Int,
                },
                new SqlParameter
                {
                    ParameterName = "id_prof",
                    Size = 30,
                    Value = id_prof ?? Convert.DBNull,
                    SqlDbType = System.Data.SqlDbType.NVarChar,
                },
                new SqlParameter
                {
                    ParameterName = "nroid_prof",
                    Value = nroid_prof ?? Convert.DBNull,
                    SqlDbType = System.Data.SqlDbType.Int,
                },
                parameterrespuesta,
                parameterreturnValue,
            };
            var _ = await _context.Database.ExecuteSqlRawAsync("EXEC @returnValue = [dbo].[SP001_SaldoReservadoNotasUrgente_Una_proforma_Almacenes] @coditem, @codalmacen, @id_prof, @nroid_prof, @respuesta OUTPUT", sqlParameters, cancellationToken);

            respuesta.SetValue(parameterrespuesta.Value);
            returnValue?.SetValue(parameterreturnValue.Value);

            return _;
        }

        public virtual async Task<int> SP001_SaldoReservadoNotasUrgente_Una_proforma_TiendasAsync(string coditem, int? codalmacen, string id_prof, int? nroid_prof, OutputParameter<decimal?> respuesta, OutputParameter<int> returnValue = null, CancellationToken cancellationToken = default)
        {
            var parameterrespuesta = new SqlParameter
            {
                ParameterName = "respuesta",
                Precision = 18,
                Scale = 2,
                Direction = System.Data.ParameterDirection.InputOutput,
                Value = respuesta?._value ?? Convert.DBNull,
                SqlDbType = System.Data.SqlDbType.Decimal,
            };
            var parameterreturnValue = new SqlParameter
            {
                ParameterName = "returnValue",
                Direction = System.Data.ParameterDirection.Output,
                SqlDbType = System.Data.SqlDbType.Int,
            };

            var sqlParameters = new[]
            {
                new SqlParameter
                {
                    ParameterName = "coditem",
                    Size = 20,
                    Value = coditem ?? Convert.DBNull,
                    SqlDbType = System.Data.SqlDbType.NVarChar,
                },
                new SqlParameter
                {
                    ParameterName = "codalmacen",
                    Value = codalmacen ?? Convert.DBNull,
                    SqlDbType = System.Data.SqlDbType.Int,
                },
                new SqlParameter
                {
                    ParameterName = "id_prof",
                    Size = 30,
                    Value = id_prof ?? Convert.DBNull,
                    SqlDbType = System.Data.SqlDbType.NVarChar,
                },
                new SqlParameter
                {
                    ParameterName = "nroid_prof",
                    Value = nroid_prof ?? Convert.DBNull,
                    SqlDbType = System.Data.SqlDbType.Int,
                },
                parameterrespuesta,
                parameterreturnValue,
            };
            var _ = await _context.Database.ExecuteSqlRawAsync("EXEC @returnValue = [dbo].[SP001_SaldoReservadoNotasUrgente_Una_proforma_Tiendas] @coditem, @codalmacen, @id_prof, @nroid_prof, @respuesta OUTPUT", sqlParameters, cancellationToken);

            respuesta.SetValue(parameterrespuesta.Value);
            returnValue?.SetValue(parameterreturnValue.Value);

            return _;
        }

        public virtual async Task<int> SP001_SaldoReservadoNotasUrgentes_AlmacenesAsync(string coditem, int? codalmacen, OutputParameter<string> respuesta, OutputParameter<int> returnValue = null, CancellationToken cancellationToken = default)
        {
            var parameterrespuesta = new SqlParameter
            {
                ParameterName = "respuesta",
                Size = 20,
                Direction = System.Data.ParameterDirection.InputOutput,
                Value = respuesta?._value ?? Convert.DBNull,
                SqlDbType = System.Data.SqlDbType.VarChar,
            };
            var parameterreturnValue = new SqlParameter
            {
                ParameterName = "returnValue",
                Direction = System.Data.ParameterDirection.Output,
                SqlDbType = System.Data.SqlDbType.Int,
            };

            var sqlParameters = new[]
            {
                new SqlParameter
                {
                    ParameterName = "coditem",
                    Size = 20,
                    Value = coditem ?? Convert.DBNull,
                    SqlDbType = System.Data.SqlDbType.NVarChar,
                },
                new SqlParameter
                {
                    ParameterName = "codalmacen",
                    Value = codalmacen ?? Convert.DBNull,
                    SqlDbType = System.Data.SqlDbType.Int,
                },
                parameterrespuesta,
                parameterreturnValue,
            };
            var _ = await _context.Database.ExecuteSqlRawAsync("EXEC @returnValue = [dbo].[SP001_SaldoReservadoNotasUrgentes_Almacenes] @coditem, @codalmacen, @respuesta OUTPUT", sqlParameters, cancellationToken);

            respuesta.SetValue(parameterrespuesta.Value);
            returnValue?.SetValue(parameterreturnValue.Value);

            return _;
        }

        public virtual async Task<int> SP001_SaldoReservadoNotasUrgentes_TiendasAsync(string coditem, int? codalmacen, OutputParameter<string> respuesta, OutputParameter<int> returnValue = null, CancellationToken cancellationToken = default)
        {
            var parameterrespuesta = new SqlParameter
            {
                ParameterName = "respuesta",
                Size = 20,
                Direction = System.Data.ParameterDirection.InputOutput,
                Value = respuesta?._value ?? Convert.DBNull,
                SqlDbType = System.Data.SqlDbType.VarChar,
            };
            var parameterreturnValue = new SqlParameter
            {
                ParameterName = "returnValue",
                Direction = System.Data.ParameterDirection.Output,
                SqlDbType = System.Data.SqlDbType.Int,
            };

            var sqlParameters = new[]
            {
                new SqlParameter
                {
                    ParameterName = "coditem",
                    Size = 20,
                    Value = coditem ?? Convert.DBNull,
                    SqlDbType = System.Data.SqlDbType.NVarChar,
                },
                new SqlParameter
                {
                    ParameterName = "codalmacen",
                    Value = codalmacen ?? Convert.DBNull,
                    SqlDbType = System.Data.SqlDbType.Int,
                },
                parameterrespuesta,
                parameterreturnValue,
            };
            var _ = await _context.Database.ExecuteSqlRawAsync("EXEC @returnValue = [dbo].[SP001_SaldoReservadoNotasUrgentes_Tiendas] @coditem, @codalmacen, @respuesta OUTPUT", sqlParameters, cancellationToken);

            respuesta.SetValue(parameterrespuesta.Value);
            returnValue?.SetValue(parameterreturnValue.Value);

            return _;
        }
    }
}