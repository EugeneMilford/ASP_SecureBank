using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecureBank.API.Migrations
{
    /// <inheritdoc />
    public partial class UsersAdded : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // First create the users table
            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FirstName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Password = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "User")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.UserId);
                });

            // Add unique constraint to email
            migrationBuilder.CreateIndex(
                name: "IX_users_Email",
                table: "users",
                column: "Email",
                unique: true);

            // Insert a default user for existing accounts (temporary measure)
            migrationBuilder.Sql(@"
                INSERT INTO users (FirstName, LastName, Email, Password, PhoneNumber, CreatedDate, Role)
                VALUES ('Default', 'User', 'default@securebank.com', 'TempPassword123!', '0000000000', GETDATE(), 'User')
            ");

            // Add UserId column to accounts table with default value pointing to the default user
            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "accounts",
                type: "int",
                nullable: false,
                defaultValue: 1); // Points to the default user we just created

            // Update existing accounts to use the default user
            migrationBuilder.Sql("UPDATE accounts SET UserId = 1 WHERE UserId = 0");

            // Create the foreign key constraint
            migrationBuilder.CreateIndex(
                name: "IX_accounts_UserId",
                table: "accounts",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_accounts_users_UserId",
                table: "accounts",
                column: "UserId",
                principalTable: "users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop the foreign key constraint
            migrationBuilder.DropForeignKey(
                name: "FK_accounts_users_UserId",
                table: "accounts");

            // Drop the index
            migrationBuilder.DropIndex(
                name: "IX_accounts_UserId",
                table: "accounts");

            // Drop the UserId column from accounts
            migrationBuilder.DropColumn(
                name: "UserId",
                table: "accounts");

            // Drop the users table
            migrationBuilder.DropTable(
                name: "users");
        }
    }
}