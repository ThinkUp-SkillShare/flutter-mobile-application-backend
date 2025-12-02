using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SkillShareBackend.Migrations
{
    /// <inheritdoc />
    public partial class ModifyFileUrlLength : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Solo modificar la columna file_url
            migrationBuilder.AlterColumn<string>(
                    name: "file_url",
                    table: "group_messages",
                    type: "varchar(2000)",
                    maxLength: 2000,
                    nullable: true,
                    oldClrType: typeof(string),
                    oldType: "varchar(500)",
                    oldMaxLength: 500,
                    oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revertir el cambio
            migrationBuilder.AlterColumn<string>(
                    name: "file_url",
                    table: "group_messages",
                    type: "varchar(500)",
                    maxLength: 500,
                    nullable: true,
                    oldClrType: typeof(string),
                    oldType: "varchar(2000)",
                    oldMaxLength: 2000,
                    oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }
    }
}