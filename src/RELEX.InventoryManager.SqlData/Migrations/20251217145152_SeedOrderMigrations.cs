using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RELEX.InventoryManager.SqlData.Migrations
{
    /// <inheritdoc />
    public partial class SeedOrderMigrations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
            CREATE OR REPlACE PROCEDURE inv.SeedOrders(seedNumber integer)
            LANGUAGE SQL
            AS $$
	            INSERT INTO inv.""Orders"" 
		            (""Id"", ""LocationCode"", ""ProductCode"", ""OrderDate"", ""Quantity"", ""SubmittedBy"", ""SubmittedAt"")
		
	            WITH randomOrders AS (
	                SELECT
			            gen_random_uuid() as ""Id"",
		  	            (ARRAY['Lisbon-001', 'Porto-001', 'Coimbra-001', 'Sintra-001', 'Aveiro-001'])[floor(random() * 5 + 1)] as ""LocationCode"",
		  	            (ARRAY['bananas-001', 'bananas-002', 'apples-001', 'rice-001', 'potatoes-001'])[floor(random() * 5 + 1)] as ""ProductCode"",
		  	            gs % 100 ""Quantity"",
		  	            'user_' || gs || '@store.com' as ""SubmittedBy"",
			            NOW() - (random() * (interval '1000 days')) as ""SubmittedAt""
	                FROM generate_series(1, seedNumber) AS gs
	            )
	            SELECT ""Id"", ""LocationCode"", ""ProductCode"", ""SubmittedAt"", ""Quantity"", ""SubmittedBy"", ""SubmittedAt""
	            FROM randomOrders;
            $$;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE inv.SeedOrders");
        }
    }
}
