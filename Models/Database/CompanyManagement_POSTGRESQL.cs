using Newtonsoft.Json.Linq;
using Npgsql;
using System.Threading.Tasks;

namespace api.Models.Database
{
    public class CompanyManagement_POSTGRESQL
    {
        /** store procedure for admin mode */
        public async Task<JObject>? Prc_Admin(ApplicationENUM.DATABASE_POSTGRSQL_COMPANY_MANAGEMENT_PRC_ADMIN_IN_MODE inMode, JObject? inData = null)
        {
            JObject getData = new JObject()
            {
                { "result", 0 },
                { "message", "Database connection not open" }
            };
#pragma warning disable CS8602
            NpgsqlConnection getConn = await Program.configuration.GetDatabaseConnection_POSTGRESQL(ApplicationENUM.DATABASE_CONNECTION.POSTGRESQL_COMPANY_MANAGEMENT);
#pragma warning restore CS8602
            if (getConn.State != System.Data.ConnectionState.Open) return getData;
            try
            {
                NpgsqlCommand getComm = new NpgsqlCommand("CALL public.prc_admin(@mode, @in_data, null)", getConn);
                getComm.Parameters.Add(new NpgsqlParameter("@mode", NpgsqlTypes.NpgsqlDbType.Integer) { Value = inMode });
                if (inData == null) getComm.Parameters.Add(new NpgsqlParameter("@in_data", NpgsqlTypes.NpgsqlDbType.Jsonb) { Value = DBNull.Value });
                else getComm.Parameters.Add(new NpgsqlParameter("@in_data", NpgsqlTypes.NpgsqlDbType.Jsonb) { Value = inMode.ToString() });
                NpgsqlDataReader getReader = await getComm.ExecuteReaderAsync();
                if (await getReader.ReadAsync())
                {
                    if (!getReader.IsDBNull(0))
                    {
                        try
                        {
                            getData = Newtonsoft.Json.JsonConvert.DeserializeObject<JObject>(getReader.GetString(0)) ?? getData;
                        }
                        catch (Exception getReaderError)
                        {
                            getData["message"] = getReaderError.Message;
                        }
                    }
                }
                await getReader.CloseAsync();
                await getReader.DisposeAsync();
            }
            catch (Exception getCommandError)
            {
                getData["message"] = getCommandError.Message;
                return getData;
            }
            return getData;
        } 
    }
}
