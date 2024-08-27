using Microsoft.EntityFrameworkCore.Migrations;


#nullable disable

namespace TrackWeatherWeb.Migrations
{
    /// <inheritdoc />
    public partial class First : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Requests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Coordinate_x = table.Column<decimal>(type: "decimal(18,6)", nullable: true),
                    Coordinate_y = table.Column<decimal>(type: "decimal(18,6)", nullable: true),
                    Comment = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Requests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Password = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            var passwordHash = BCrypt.Net.BCrypt.HashPassword("Admin123");
            migrationBuilder.InsertData(
                table: "Users",
                columns: ["Name", "Email", "Role", "Password"],
                values: ["Admin", "admin@example.com", "Admin", passwordHash]
            );
			migrationBuilder.InsertData(
	            table: "Requests",
	            columns: ["Id", "Email", "Coordinate_x", "Coordinate_y", "Comment", "Timestamp"],
	            values: [1, "user@example.com", 54.017703655095126m, 38.01406279530195m, "Raining, need transport", DateTime.Parse("2024-08-27 10:30:00")]
            );

			migrationBuilder.InsertData(
				table: "Requests",
				columns: ["Id", "Email", "Coordinate_x", "Coordinate_y", "Comment", "Timestamp"],
				values: [2, "user2@example.com", 48.909172455411614m, 40.46727250624546m, "Raining, need transport", DateTime.Parse("2024-08-27 10:35:00")]
			);
		}

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Requests");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
