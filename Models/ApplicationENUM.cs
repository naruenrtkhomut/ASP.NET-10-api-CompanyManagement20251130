namespace api.Models
{
    public class ApplicationENUM
    {
        public enum DATABASE_CONNECTION
        {
            POSTGRESQL_COMPANY_MANAGEMENT = 0
        }
        public enum DATABASE_POSTGRSQL_COMPANY_MANAGEMENT_PRC_ADMIN_IN_MODE
        {
            ADDING_DATABASE_SHARDING = 1000,
            ADDING_ADMIN = 1001,
            GETTING_ALL_DATABASE_SHARDING = 2000,
            GETTING_SHARDING_DATABASE = 2001
        }
    }
}
